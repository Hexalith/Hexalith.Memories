"""Real loopback HTTPS provider fixtures, never live qualification evidence."""

from __future__ import annotations

from contextlib import redirect_stderr, redirect_stdout
import copy
from dataclasses import FrozenInstanceError, replace
from datetime import datetime, timedelta, timezone
import hashlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import io
import json
import multiprocessing
import os
from pathlib import Path
import shutil
import socket
import ssl
import subprocess
import sys
import tempfile
import threading
import time
import unittest
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))
from access_telemetry_c1_github_approvals import (  # noqa: E402
    ApprovalContext, CurrentRolePolicy, DECISION_SCHEMA, GitHubApprovalError,
    GitHubReviewClient, RequestLimits, ReviewBinding, ReviewLocator, consume_bundle_reviews,
)
from access_telemetry_c1_interchange import EvidenceReference, JsonSnapshot  # noqa: E402
from hexalith_github_reviews import (  # noqa: E402
    API_VERSION, GitHubReviewRetrievalError, PRODUCTION_ISSUER,
)
import hexalith_github_reviews  # noqa: E402
import access_telemetry_c1_github_approvals as approvals  # noqa: E402


NOW = datetime(2026, 10, 7, 12, tzinfo=timezone.utc)
OWNER = "github:user:6775094"
OTHER = "github:user:1002"
PRODUCER = "github:user:1003"
TOKEN = "github_pat_fixture_credential_canary_only"
PROFILE, WORKLOAD, TARGET, POLICY = (character * 64 for character in "abcd")
SOURCE, REVIEWED = "e" * 40, "f" * 40


def encode(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":")).encode("utf-8")


def reference(path, raw):
    return EvidenceReference(path, hashlib.sha256(raw).hexdigest(), len(raw))


def wire_time(value):
    return value.strftime("%Y-%m-%dT%H:%M:%SZ")


def dns_stalled_worker(channel):
    """Exercise a stalled resolver inside the real fixture HTTPS request worker."""
    original = socket.getaddrinfo

    def stalled(*args, **kwargs):
        time.sleep(1.5)
        return original(*args, **kwargs)

    socket.getaddrinfo = stalled
    hexalith_github_reviews._request_worker(channel)


def production_fixture_worker(channel):
    """Isolate production DNS while retaining hostname/443 and real system TLS."""
    original_resolver = socket.getaddrinfo
    original_context = ssl.create_default_context

    def isolated_resolver(host, port, *args, **kwargs):
        if host != "api.github.com" or port != 443:
            raise ValueError("production-host-or-port-changed")
        return original_resolver("127.0.0.1", int(os.environ["HEXALITH_TEST_TLS_PORT"]), *args, **kwargs)

    def system_context(*args, **kwargs):
        if args or kwargs != {"cadata": None}:
            raise ValueError("production-system-context-changed")
        context = original_context(*args, **kwargs)
        if not context.check_hostname or context.verify_mode != ssl.CERT_REQUIRED:
            raise ValueError("production-tls-verification-changed")
        return context

    socket.getaddrinfo = isolated_resolver
    ssl.create_default_context = system_context
    hexalith_github_reviews._request_worker(channel)


def worker_tls_failure(channel):
    """A TLS setup error containing a credential must stay inside the worker."""
    def fail(*args, **kwargs):
        raise RuntimeError(TOKEN)

    ssl.create_default_context = fail
    hexalith_github_reviews._request_worker(channel)


class FixtureServer:
    """Verified TLS, mutable authenticated review service, no credential logging."""

    def __init__(self, cert, key):
        self.responses = {}
        self.requests = []
        self.sni_names = []
        self.stop = threading.Event()
        fixture = self

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_GET(self):
                fixture.requests.append({
                    "path": self.path, "authenticated": self.headers.get("Authorization") == "Bearer " + TOKEN,
                    "accept": self.headers.get("Accept"), "version": self.headers.get("X-GitHub-Api-Version"),
                    "encoding": self.headers.get("Accept-Encoding"),
                    "host": self.headers.get("Host"),
                })
                record = fixture.responses.get(self.path, {"status": 404, "raw": b"unavailable"})
                try:
                    if fixture.stop.wait(record.get("header_delay", 0)):
                        return
                    self.send_response(record.get("status", 200))
                    for name, value in record.get("headers", {}).items():
                        self.send_header(name, value)
                    raw = record.get("raw", encode(record.get("json")))
                    if not record.get("no_length"):
                        self.send_header("Content-Length", str(len(raw)))
                    self.end_headers()
                    if fixture.stop.wait(record.get("body_delay", 0)):
                        return
                    if record.get("chunked"):
                        for chunk in (raw[:7], raw[7:]):
                            self.wfile.write(f"{len(chunk):x}\r\n".encode() + chunk + b"\r\n")
                        if not record.get("truncate_chunked"):
                            self.wfile.write(b"0\r\n\r\n")
                    elif record.get("trickle"):
                        for byte in raw:
                            self.wfile.write(bytes([byte]))
                            self.wfile.flush()
                            if fixture.stop.wait(record["trickle"]):
                                return
                    else:
                        self.wfile.write(raw)
                except (OSError, ssl.SSLError):
                    pass

        self.server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        self.server.daemon_threads = True
        tls = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
        tls.load_cert_chain(cert, key)
        tls.set_servername_callback(lambda connection, name, context: self.sni_names.append(name))
        self.server.socket = tls.wrap_socket(self.server.socket, server_side=True)
        self.thread = threading.Thread(target=self.server.serve_forever, kwargs={"poll_interval": 0.01}, daemon=True)
        self.thread.start()

    @property
    def port(self):
        return self.server.server_port

    def close(self):
        self.stop.set()
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=1)


class GitHubReviewContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.certificates = tempfile.TemporaryDirectory(prefix="c1-github-tls-")
        cls.addClassCleanup(cls.certificates.cleanup)
        directory = Path(cls.certificates.name)
        for name, san in (("valid", "IP:127.0.0.1"), ("wrong-host", "DNS:wrong.invalid"),
                          ("production-fixture", "DNS:api.github.com")):
            result = subprocess.run([
                "openssl", "req", "-x509", "-newkey", "ec", "-pkeyopt", "ec_paramgen_curve:prime256v1",
                "-nodes", "-days", "1", "-subj", "/CN=C1 local TLS fixture",
                "-addext", "subjectAltName=" + san, "-keyout", str(directory / (name + ".key")),
                "-out", str(directory / (name + ".crt")),
            ], capture_output=True, timeout=10)
            if result.returncode != 0:
                raise RuntimeError("loopback-tls-certificate-generation-failed")
        cls.cert = directory / "valid.crt"
        cls.key = directory / "valid.key"
        cls.wrong_cert = directory / "wrong-host.crt"
        cls.wrong_key = directory / "wrong-host.key"
        cls.production_cert = directory / "production-fixture.crt"
        cls.production_key = directory / "production-fixture.key"
        cls.ca_pem = cls.cert.read_text()

    def setUp(self):
        self.fixture = FixtureServer(self.cert, self.key)
        self.addCleanup(self.fixture.close)
        self.client = GitHubReviewClient(self.fixture.port, self.ca_pem)
        policy = CurrentRolePolicy(POLICY, PROFILE, WORKLOAD, TARGET, "fixture-session",
                                   frozenset({OWNER}), frozenset({OWNER}), 5, 3600, 120)
        manifest = b'{ "fixture": "opaque frozen manifest, no accepted gates" }\n'
        self.context = ApprovalContext(
            reference("retained/manifest.json", manifest), manifest, NOW - timedelta(seconds=60),
            PROFILE, WORKLOAD, TARGET, "fixture-session", SOURCE, REVIEWED, policy,
            frozenset({PRODUCER}), NOW, self.client.issuer, RequestLimits(16_384, 3),
        )
        self.operations = self.binding("operations", 101)
        self.security = self.binding("security", 102)

    def decision(self, role):
        manifest = self.context.manifest
        return {
            "schema": DECISION_SCHEMA, "role": role, "decision": "approve",
            "manifest": {"path": manifest.path, "sha256": manifest.sha256, "byteLength": manifest.byte_length},
            "profileSha256": PROFILE, "workloadSha256": WORKLOAD, "targetSha256": TARGET,
            "sessionId": "fixture-session", "sourceCommit": SOURCE, "policySha256": POLICY,
            "expiresAtUtc": wire_time(NOW + timedelta(seconds=600)),
        }

    def binding(self, role, review_id, *, principal=OWNER, decision=None):
        body = encode(self.decision(role) if decision is None else decision)
        locator = ReviewLocator("fixture-owner", "evidence", 7, review_id)
        binding = ReviewBinding(locator, principal, reference(f"retained/{role}.json", body), body)
        self.fixture.responses[locator.review_path] = {"json": {
            "id": review_id, "user": {"id": int(principal.rsplit(":", 1)[1]), "type": "User", "login": "ignored-alias"},
            "body": body.decode(), "state": "APPROVED", "pull_request_url": self.client.api_origin + locator.pull_path,
            "commit_id": REVIEWED, "submitted_at": wire_time(NOW - timedelta(seconds=30)),
            "author_association": "NONE",
        }}
        return binding

    def call(self, **overrides):
        arguments = {"client": self.client, "token": TOKEN, "context": self.context,
                     "operations": self.operations, "security": self.security}
        arguments.update(overrides)
        return consume_bundle_reviews(**arguments)

    def remote(self, binding=None):
        return self.fixture.responses[(binding or self.operations).locator.review_path]["json"]

    def refused(self, code=None, **overrides):
        with self.assertRaises(GitHubApprovalError) as result:
            self.call(**overrides)
        self.assertLess(len(str(result.exception)), 100)
        self.assertNotIn(TOKEN, str(result.exception))
        self.assertTrue(result.exception.__suppress_context__)
        if code is not None:
            self.assertEqual(str(result.exception), code)

    def process_probe(self, mode):
        """Capture OS stdout/stderr of the caller and its spawn/bootstrap workers."""
        with tempfile.TemporaryDirectory(prefix="c1-worker-output-") as directory:
            script = Path(directory) / "probe.py"
            ca = Path(directory) / "ca.pem"
            ca.write_text(self.ca_pem)
            script.write_text('''import json
import sys
import time
if __name__ == "__mp_main__":
    if sys.argv[1] in ("delayed-bootstrap", "oversized-name", "oversized-path", "oversized-argv", "oversized-authkey"):
        time.sleep(1.5)
    if sys.argv[1] == "bootstrap-failure":
        raise RuntimeError("isolated-bootstrap-refusal")
sys.path.insert(0, sys.argv[4] + "/tools")
import access_telemetry_c1_github_approvals
import hexalith_github_reviews as transport
sys.path.insert(0, sys.argv[4] + "/tests/tooling/access_telemetry_c1_interchange")
from test_github_approvals import TOKEN, worker_tls_failure
from pathlib import Path
from unittest.mock import patch
import multiprocessing
import socket

def main():
    mode = sys.argv[1]
    ca = Path(sys.argv[3]).read_text()
    if mode == "delayed-bootstrap":
        ca += "\\n" * (65_536 - len(ca))
    client = transport.GitHubReviewClient(int(sys.argv[2]), ca)
    limits = transport.RequestLimits(16_384, 0.2 if mode.startswith("oversized-") or mode == "delayed-bootstrap" else 3)
    locator = transport.ReviewLocator("fixture-owner", "evidence", 7, 101)
    if mode == "oversized-name":
        multiprocessing.current_process().name = "x" * 1_048_576
    elif mode == "oversized-path":
        sys.path.append("x" * 1_048_576)
    elif mode == "oversized-argv":
        sys.argv.append("x" * 1_048_576)
    elif mode == "oversized-authkey":
        multiprocessing.current_process().authkey = b"x" * 1_048_576
    original_pair = socket.socketpair
    def small_channel():
        pair = original_pair()
        pair[0].setsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF, 4096)
        return pair
    if mode == "tls-failure":
        transport._request_worker = worker_tls_failure
    if mode == "socket-failure":
        socket.socketpair = lambda: (_ for _ in ()).throw(OSError(TOKEN))
    elif mode == "delayed-bootstrap":
        socket.socketpair = small_channel
    started = time.monotonic()
    try:
        client.fetch_review(locator, token=TOKEN, limits=limits)
        code = "unexpected-success"
    except transport.GitHubReviewRetrievalError as error:
        code = str(error)
    print(json.dumps({"code": code, "elapsed": time.monotonic() - started,
                      "orphans": [child.pid for child in multiprocessing.active_children()]}))

if __name__ == "__main__":
    main()
''')
            result = subprocess.run([sys.executable, "-I", str(script), mode, str(self.fixture.port), str(ca), str(REPO_ROOT)],
                                    capture_output=True, text=True, timeout=6)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn(TOKEN, result.stdout + result.stderr)
        data = json.loads(result.stdout)
        self.assertEqual(data["orphans"], [])
        return data, result

    def test_owner_two_separately_submitted_reviews_yield_exact_immutable_observations(self):
        anchor = self.context._time_acquired_monotonic
        with patch.object(approvals, "_elapsed", side_effect=[anchor + 10, anchor + 11, anchor + 12]):
            pair = self.call()
        self.assertEqual(tuple(item.role for item in pair), ("operations", "security"))
        for item, binding in zip(pair, (self.operations, self.security)):
            self.assertEqual(item.reviewer_principal, OWNER)
            self.assertEqual(item.locator, binding.locator)
            self.assertEqual(item.manifest, self.context.manifest)
            self.assertEqual(item.body_sha256, binding.body_reference.sha256)
            self.assertEqual(item.response_sha256, JsonSnapshot(encode(self.remote(binding))).sha256)
            self.assertEqual(item.reviewed_commit, REVIEWED)
            self.assertEqual(item.source_commit, SOURCE)
            self.assertEqual(item.policy_sha256, POLICY)
            self.assertEqual(item.profile_sha256, PROFILE)
            self.assertEqual(item.workload_sha256, WORKLOAD)
            self.assertEqual(item.target_sha256, TARGET)
            self.assertEqual(item.session_id, "fixture-session")
            self.assertEqual(item.submitted_at_utc, NOW - timedelta(seconds=30))
            self.assertEqual(item.expires_at_utc, NOW + timedelta(seconds=600))
            self.assertEqual(item.status_request_started_at_utc,
                             NOW + timedelta(seconds=10 if item.role == "operations" else 11))
            self.assertEqual(item.checked_at_utc, NOW + timedelta(seconds=12))
            self.assertEqual(item.issuer, self.client.issuer)
            self.assertNotEqual(item.issuer, PRODUCTION_ISSUER)
            self.assertNotIn(TOKEN, repr(item))
            self.assertFalse(hasattr(item, "qualification_authorized"))
            with self.assertRaises(FrozenInstanceError):
                item.role = "security"
        self.assertEqual(len(self.fixture.requests), 2)
        for request in self.fixture.requests:
            self.assertTrue(request["authenticated"])
            self.assertEqual(request["accept"], "application/vnd.github+json")
            self.assertEqual(request["version"], API_VERSION)
            self.assertEqual(request["encoding"], "identity")

    def test_two_distinct_currently_authorized_reviewers_pass(self):
        policy = replace(self.context.policy, security_principals=frozenset({OTHER}))
        security = self.binding("security", 102, principal=OTHER)
        pair = self.call(context=replace(self.context, policy=policy), security=security)
        self.assertEqual((pair[0].reviewer_principal, pair[1].reviewer_principal), (OWNER, OTHER))

    def test_each_call_refetches_both_and_dismissal_refuses(self):
        self.call()
        self.remote(self.security)["state"] = "DISMISSED"
        self.refused("review-not-currently-approved")
        self.assertEqual([item["path"] for item in self.fixture.requests],
                         [self.operations.locator.review_path, self.security.locator.review_path] * 2)

    def test_unknown_missing_edited_pending_and_dismissed_reviews_refuse_over_tls(self):
        original = copy.deepcopy(self.remote())
        cases = ({"state": value} for value in ("DISMISSED", "PENDING", "COMMENTED", "CHANGES_REQUESTED", "APPROVE", None))
        for changes in cases:
            with self.subTest(changes=changes):
                self.fixture.responses[self.operations.locator.review_path] = {"json": {**original, **changes}}
                self.refused("review-not-currently-approved")
        for missing in ("state", "id", "user", "body", "commit_id", "pull_request_url", "submitted_at"):
            with self.subTest(missing=missing):
                value = {key: val for key, val in original.items() if key != missing}
                self.fixture.responses[self.operations.locator.review_path] = {"json": value}
                self.refused()

    def test_changed_body_exact_utf8_hash_and_length_refuse(self):
        for changed in (self.operations.retained_body + b"\n", self.operations.retained_body.replace(b"approve", b"decline")):
            with self.subTest(changed=hashlib.sha256(changed).hexdigest()):
                self.remote()["body"] = changed.decode()
                self.refused()

    def test_wrong_review_id_principal_commit_and_pr_url_refuse(self):
        original = copy.deepcopy(self.remote())
        cases = ({"id": True}, {"id": 999}, {"commit_id": SOURCE}, {"commit_id": REVIEWED.upper()},
                 {"pull_request_url": "https://api.github.com/repos/fixture-owner/evidence/pulls/7"},
                 {"pull_request_url": self.client.api_origin + "/repos/fixture-owner/evidence/pulls/8"},
                 {"user": {"id": 1002, "type": "User"}}, {"user": {"id": True, "type": "User"}},
                 {"user": {"id": 0, "type": "User"}}, {"user": {"id": 6775094, "type": "Bot"}})
        for changes in cases:
            with self.subTest(fields=tuple(changes)):
                self.fixture.responses[self.operations.locator.review_path] = {"json": {**original, **changes}}
                self.refused()

    def test_github_author_association_supplies_no_role_grant(self):
        self.remote()["author_association"] = "OWNER"
        policy = replace(self.context.policy, operations_principals=frozenset({OTHER}))
        self.refused("current-review-role-denied", context=replace(self.context, policy=policy))
        self.assertEqual(self.fixture.requests, [])

    def test_non_owner_repeated_reviewer_and_any_authenticated_producer_overlap_refuse(self):
        policy = replace(self.context.policy, operations_principals=frozenset({OTHER}), security_principals=frozenset({OTHER}))
        operations = self.binding("operations", 101, principal=OTHER)
        security = self.binding("security", 102, principal=OTHER)
        self.refused("bundle-distinct-reviewers-required", context=replace(self.context, policy=policy),
                     operations=operations, security=security)
        self.refused("bundle-producer-self-approval", context=replace(self.context, producer_principals=frozenset({OWNER})))
        self.refused("bundle-producer-self-approval", context=replace(self.context, producer_principals=frozenset({PRODUCER, OWNER})))
        self.refused("producer-principals-required", context=replace(self.context, producer_principals=frozenset()))
        self.assertEqual(self.fixture.requests, [])

    def test_distinct_ids_one_pr_and_trusted_issuer_are_mandatory(self):
        self.refused("two-distinct-reviews-on-one-pr-required", security=replace(self.security, locator=self.operations.locator))
        for field, value in (("owner", "other-owner"), ("repository", "other-repo"), ("pull_number", 8)):
            with self.subTest(field=field):
                self.refused("two-distinct-reviews-on-one-pr-required",
                             security=replace(self.security, locator=replace(self.security.locator, **{field: value})))
        self.refused("review-issuer-mismatch", context=replace(self.context, expected_issuer=PRODUCTION_ISSUER))
        self.assertEqual(self.fixture.requests, [])

    def test_manifest_reference_exact_bytes_are_required(self):
        for changes in ({"manifest_bytes": self.context.manifest_bytes + b"\n"},
                        {"manifest": replace(self.context.manifest, sha256="0" * 64)},
                        {"manifest_bytes": bytearray(self.context.manifest_bytes)}, {"manifest": None}):
            with self.subTest(fields=tuple(changes)), self.assertRaises(GitHubApprovalError):
                replace(self.context, **changes)

    def test_closed_decision_schema_and_all_exact_scope_bindings_refuse_before_retrieval(self):
        original = self.decision("operations")
        changes = ({"unknown": True}, {"schema": "other/v1"}, {"role": "platform-operations"}, {"decision": "deny"},
                   {"profileSha256": "0" * 64}, {"workloadSha256": "0" * 64}, {"targetSha256": "0" * 64},
                   {"sessionId": "other-session"}, {"sourceCommit": REVIEWED}, {"policySha256": "0" * 64},
                   {"manifest": {**original["manifest"], "path": "retained/other.json"}},
                   {"manifest": {**original["manifest"], "sha256": "0" * 64}},
                   {"manifest": {**original["manifest"], "byteLength": original["manifest"]["byteLength"] + 1}},
                   {"manifest": {**original["manifest"], "unknown": 1}})
        for change in changes:
            with self.subTest(fields=tuple(change)):
                operations = self.binding("operations", 101, decision={**original, **change})
                self.refused(operations=operations)
        for missing in original:
            with self.subTest(missing=missing):
                operations = self.binding("operations", 101, decision={key: val for key, val in original.items() if key != missing})
                self.refused(operations=operations)
        self.assertEqual(self.fixture.requests, [])

    def test_missing_role_policy_time_or_numeric_limits_refuse_without_defaults(self):
        for changes in ({"trusted_now_utc": None}, {"trusted_now_utc": NOW.replace(tzinfo=None)},
                        {"manifest_created_at_utc": NOW + timedelta(seconds=1)}, {"policy": None},
                        {"request_limits": None}, {"source_commit": "main"}, {"reviewed_commit": "latest"},
                        {"policy": replace(self.context.policy, session_id="other-session")}):
            with self.subTest(fields=tuple(changes)), self.assertRaises(GitHubApprovalError):
                replace(self.context, **changes)
        for field in ("max_status_age_seconds", "max_approval_lifetime_seconds", "max_review_lag_seconds"):
            for value in (None, True, 0, -1, float("nan"), float("inf"), 10**400):
                with self.subTest(field=field, value=value), self.assertRaises(GitHubApprovalError):
                    replace(self.context.policy, **{field: value})

        with self.assertRaisesRegex(GitHubApprovalError, "^trusted-time-acquisition-invalid$"):
            replace(self.context, _time_acquired_monotonic=10**400)
        for field in ("operations_principals", "security_principals"):
            for value in (None, frozenset(), {OWNER}, frozenset({"jpiquot"})):
                with self.subTest(field=field, kind=type(value).__name__), self.assertRaises(GitHubApprovalError):
                    replace(self.context.policy, **{field: value})

    def test_each_independent_policy_digest_mismatch_refuses_before_https(self):
        for field in ("profile_sha256", "workload_sha256", "target_sha256"):
            policy = replace(self.context.policy, **{field: "0" * 64})
            with self.subTest(field=field), self.assertRaisesRegex(GitHubApprovalError, "^current-role-policy-wrong-scope$"):
                replace(self.context, policy=policy)
        self.assertEqual(self.fixture.requests, [])

    def test_canonical_submitted_and_expiry_utc_are_required(self):
        for value in (None, "2026-10-07T11:59:30+00:00", "2026-10-07T11:59:30.000Z", "2026-02-30T12:00:00Z", "2026-10-07"):
            with self.subTest(value=value):
                self.remote()["submitted_at"] = value
                self.refused()
        for value in (None, "2026-10-07T12:10:00+00:00", "2026-10-07T12:10:00.0Z"):
            with self.subTest(expiry=value):
                operations = self.binding("operations", 101, decision={**self.decision("operations"), "expiresAtUtc": value})
                self.refused(operations=operations)

    def test_future_review_before_manifest_and_excessive_lag_refuse(self):
        for submitted, code in ((NOW + timedelta(seconds=10), "review-submitted-in-future"),
                                (NOW - timedelta(seconds=61), "review-manifest-order-or-lag-invalid")):
            with self.subTest(code=code):
                self.remote()["submitted_at"] = wire_time(submitted)
                self.refused(code)
        self.remote()["submitted_at"] = wire_time(NOW - timedelta(seconds=30))
        self.refused("review-manifest-order-or-lag-invalid",
                     context=replace(self.context, policy=replace(self.context.policy, max_review_lag_seconds=29)))

    def test_expiry_at_now_past_expiry_and_nonpositive_or_excess_lifetime_refuse(self):
        for expiry, code in ((NOW, "review-expired"), (NOW - timedelta(seconds=1), "review-expired"),
                             (NOW - timedelta(seconds=30), "review-approval-lifetime-invalid"),
                             (NOW + timedelta(seconds=4000), "review-approval-lifetime-invalid")):
            with self.subTest(code=code):
                operations = self.binding("operations", 101, decision={**self.decision("operations"), "expiresAtUtc": wire_time(expiry)})
                self.refused(code, operations=operations)

    def test_expiry_during_second_retrieval_and_status_age_from_first_start_refuse(self):
        operations = self.binding("operations", 101, decision={**self.decision("operations"), "expiresAtUtc": wire_time(NOW + timedelta(seconds=1))})
        anchor = self.context._time_acquired_monotonic
        with patch.object(approvals, "_elapsed", side_effect=[anchor, anchor + 0.5, anchor + 1]):
            self.refused("review-expired", operations=operations)
        self.operations = self.binding("operations", 101)
        context = replace(self.context, policy=replace(self.context.policy, max_status_age_seconds=2))
        with patch.object(approvals, "_elapsed", side_effect=[anchor, anchor + 1, anchor + 2.5]):
            self.refused("review-status-stale", context=context)

    def test_reusing_or_replacing_old_trusted_context_cannot_reset_time_and_expiry(self):
        anchor = self.context._time_acquired_monotonic
        with patch.object(approvals, "_elapsed", return_value=anchor):
            self.call()
        with patch.object(approvals, "_elapsed", return_value=anchor + 600):
            self.refused("review-expired")
            context = replace(self.context)
            self.assertEqual(context._time_acquired_monotonic, anchor)
            self.refused("review-expired", context=context)

    def test_exact_lag_lifetime_status_and_expiry_boundaries_are_deterministic(self):
        context = replace(self.context, policy=replace(self.context.policy,
                          max_review_lag_seconds=30, max_approval_lifetime_seconds=630, max_status_age_seconds=2))
        anchor = context._time_acquired_monotonic
        with patch.object(approvals, "_elapsed", side_effect=[anchor + 1, anchor + 2, anchor + 3]):
            pair = self.call(context=context)
        self.assertEqual(pair[0].checked_at_utc - pair[0].status_request_started_at_utc, timedelta(seconds=2))
        for limit, value, code in (("max_review_lag_seconds", 29.5, "review-manifest-order-or-lag-invalid"),
                                  ("max_approval_lifetime_seconds", 629.5, "review-approval-lifetime-invalid")):
            changed = replace(context, policy=replace(context.policy, **{limit: value}))
            with self.subTest(limit=limit), patch.object(approvals, "_elapsed", return_value=anchor):
                self.refused(code, context=changed)
        with patch.object(approvals, "_elapsed", side_effect=[anchor + 1, anchor + 2, anchor + 3.5]):
            self.refused("review-status-stale", context=context)
        with patch.object(approvals, "_elapsed", return_value=anchor + 599.5):
            self.call(context=context)
        with patch.object(approvals, "_elapsed", return_value=anchor + 600):
            self.refused("review-expired", context=context)

    def test_reviews_submitted_at_manifest_creation_are_accepted(self):
        for binding in (self.operations, self.security):
            self.remote(binding)["submitted_at"] = wire_time(self.context.manifest_created_at_utc)
        pair = self.call()
        self.assertEqual(tuple(item.role for item in pair), ("operations", "security"))
        for observation in pair:
            self.assertEqual(observation.submitted_at_utc, self.context.manifest_created_at_utc)
            self.assertEqual(observation.manifest, self.context.manifest)

    def test_elapsed_regressions_and_unavailable_values_refuse_without_partial_observations(self):
        anchor = self.context._time_acquired_monotonic
        for values, requests in (([anchor - 1], 0), ([anchor + 1, anchor], 1),
                                 ([anchor + 1, anchor + 2, anchor + 1], 2), ([float("nan")], 0)):
            before = len(self.fixture.requests)
            with self.subTest(values=values), patch.object(approvals, "_elapsed", side_effect=values):
                self.refused("trusted-time-acquisition-invalid")
            self.assertEqual(len(self.fixture.requests) - before, requests)
        self.assertEqual(multiprocessing.active_children(), [])

    def test_shared_clock_includes_suspend_like_advancement_and_unsupported_clocks_refuse(self):
        with patch.object(hexalith_github_reviews.time, "clock_gettime", return_value=1234.5) as clock:
            self.assertEqual(hexalith_github_reviews._elapsed(), 1234.5)
            clock.assert_called_once_with(time.CLOCK_BOOTTIME)
        anchor = self.context._time_acquired_monotonic
        with patch.object(approvals, "_elapsed", return_value=anchor + 3600):
            self.refused("review-expired")
        with patch.object(hexalith_github_reviews, "_elapsed", side_effect=[1000, 1000, 4600]):
            self.refused("review-request-deadline-exceeded")
        self.assertEqual(multiprocessing.active_children(), [])
        with patch.object(hexalith_github_reviews.time, "CLOCK_BOOTTIME", None):
            with self.assertRaisesRegex(GitHubApprovalError, "^trusted-time-acquisition-invalid$"):
                replace(self.context)
            with self.assertRaisesRegex(GitHubReviewRetrievalError, "^review-transport-unavailable$"):
                self.client.fetch_review(self.operations.locator, token=TOKEN, limits=self.context.request_limits)

    def test_malformed_duplicate_bom_oversized_and_nonobject_responses_refuse_over_tls(self):
        for raw in (b"{}{}", b'{"id":101,"id":101}', b"\xef\xbb\xbf{}", b'"not an object"',
                    b'{"body":"\xff"}', b'{"body":"' + b"x" * 4097 + b'"}', b"{" * 20):
            with self.subTest(raw_digest=hashlib.sha256(raw).hexdigest()):
                self.fixture.responses[self.operations.locator.review_path] = {"raw": raw}
                self.refused()

    def test_wire_violations_in_otherwise_valid_review_envelopes_refuse_over_tls(self):
        original = copy.deepcopy(self.remote())
        raw = encode(original)
        deep = 0
        for _ in range(15):
            deep = [deep]
        cases = ((b'{"id":101,' + raw[1:], "duplicate-json-field"),
                 (b"\xef\xbb\xbf" + raw, "utf8-bom-forbidden"),
                 (encode({**original, "metadata": 1.5}), "non-integer-json-number"),
                 (raw[:-1] + b',"metadata":"\\ud800"}', "unpaired-unicode-surrogate"),
                 (raw[:-1] + b',"metadata":"\xff"}', "invalid-utf8"),
                 (encode({**original, "metadata": "x" * 4097}), "string-budget-exceeded"),
                 (encode({**original, "metadata": deep}), "json-depth-exceeded"))
        for invalid, code in cases:
            with self.subTest(code=code):
                self.fixture.responses[self.operations.locator.review_path] = {"raw": invalid}
                self.refused(code)
                self.assertEqual(multiprocessing.active_children(), [])

    def test_http_denials_and_redirects_are_never_followed_and_content_is_sanitized(self):
        for status in (301, 302, 303, 307, 308, 401, 403, 404, 429, 500):
            with self.subTest(status=status):
                self.fixture.responses[self.operations.locator.review_path] = {
                    "status": status, "raw": TOKEN.encode(), "headers": {"Location": "https://invalid.example/" + TOKEN},
                }
                before = len(self.fixture.requests)
                self.refused("review-http-refused")
                self.assertEqual(len(self.fixture.requests), before + 1)

    def test_response_byte_budget_with_and_without_content_length(self):
        for no_length in (False, True):
            with self.subTest(no_length=no_length):
                self.fixture.responses[self.operations.locator.review_path] = {"raw": b"x" * 17_000, "no_length": no_length}
                self.refused("review-response-byte-budget-exceeded")

    def test_connection_close_responses_at_exact_byte_ceiling_are_accepted(self):
        originals = [encode(self.remote(binding)) for binding in (self.operations, self.security)]
        ceiling = max(map(len, originals))
        expected = []
        for binding, raw in zip((self.operations, self.security), originals):
            exact = raw.ljust(ceiling, b" ")
            self.fixture.responses[binding.locator.review_path] = {"raw": exact, "no_length": True}
            expected.append(hashlib.sha256(exact).hexdigest())
        context = replace(self.context, request_limits=RequestLimits(ceiling, 3))
        pair = self.call(context=context)
        self.assertEqual([item.response_sha256 for item in pair], expected)
        self.assertEqual(len(self.fixture.requests), 2)
        self.assertEqual(multiprocessing.active_children(), [])

    def test_incomplete_content_length_response_refuses_entire_pair(self):
        raw = encode(self.remote(self.security))
        self.fixture.responses[self.security.locator.review_path] = {
            "raw": raw, "no_length": True, "headers": {"Content-Length": str(len(raw) + 1)},
        }
        self.refused("review-response-incomplete")
        self.assertEqual(len(self.fixture.requests), 2)
        self.assertEqual(multiprocessing.active_children(), [])

    def test_truncated_chunked_review_response_refuses_entire_pair(self):
        self.fixture.responses[self.security.locator.review_path].update({
            "no_length": True, "chunked": True, "truncate_chunked": True,
            "headers": {"Transfer-Encoding": "chunked"},
        })
        self.refused("review-transport-unavailable")
        self.assertEqual(len(self.fixture.requests), 2)
        self.assertEqual(multiprocessing.active_children(), [])

    def test_encoded_response_refuses(self):
        self.fixture.responses[self.operations.locator.review_path]["headers"] = {"Content-Encoding": "gzip"}
        self.refused("review-content-encoding-refused")

    def test_real_https_supported_chunked_and_unsupported_or_conflicting_framing(self):
        original = copy.deepcopy(self.remote())
        self.fixture.responses[self.operations.locator.review_path] = {
            "json": original, "no_length": True, "chunked": True, "headers": {"Transfer-Encoding": "chunked"},
        }
        self.assertEqual(self.call()[0].response_sha256, hashlib.sha256(encode(original)).hexdigest())
        for headers, no_length in (({"Transfer-Encoding": "gzip"}, True),
                                   ({"Transfer-Encoding": "gzip, chunked"}, True),
                                   ({"Transfer-Encoding": "chunked", "Content-Length": str(len(encode(original)))}, True)):
            with self.subTest(headers=headers):
                self.fixture.responses[self.operations.locator.review_path] = {
                    "json": original, "headers": headers, "no_length": no_length, "chunked": True,
                }
                self.refused("review-transport-unavailable")

    def test_real_https_creates_no_tls_secret_entries_with_sslkeylogfile(self):
        with tempfile.TemporaryDirectory(prefix="c1-keylog-") as directory:
            keylog = Path(directory) / "tls-keylog.txt"
            with patch.dict(os.environ, {"SSLKEYLOGFILE": str(keylog)}):
                self.call()
                self.assertEqual(os.environ["SSLKEYLOGFILE"], str(keylog))
            self.assertFalse(keylog.exists())

    def test_invalid_keylog_destination_does_not_disable_authenticated_tls(self):
        with tempfile.TemporaryDirectory(prefix="c1-keylog-directory-") as directory:
            with patch.dict(os.environ, {"SSLKEYLOGFILE": directory}):
                self.assertEqual(len(self.call()), 2)
                self.assertEqual(os.environ["SSLKEYLOGFILE"], directory)
            self.assertEqual(list(Path(directory).iterdir()), [])

    def test_production_transport_fixed_host_443_and_system_context_over_isolated_tls(self):
        fixture = FixtureServer(self.production_cert, self.production_key)
        self.addCleanup(fixture.close)
        fixture.responses[self.operations.locator.review_path] = {"raw": b"isolated production-constructor transport only"}
        with patch.dict(os.environ, {"SSL_CERT_FILE": str(self.production_cert), "HEXALITH_TEST_TLS_PORT": str(fixture.port)}), \
                patch.object(hexalith_github_reviews, "_request_worker", production_fixture_worker):
            raw = GitHubReviewClient().fetch_review(self.operations.locator, token=TOKEN, limits=self.context.request_limits)
        self.assertEqual(raw, b"isolated production-constructor transport only")
        self.assertEqual(fixture.sni_names, ["api.github.com"])
        self.assertEqual(fixture.requests[0]["host"], "api.github.com")
        self.assertTrue(fixture.requests[0]["authenticated"])
        self.assertEqual(len(fixture.requests), 1)

    def test_socket_and_process_construction_failures_are_sanitized_and_close_allocations(self):
        with patch.object(hexalith_github_reviews.socket, "socketpair", side_effect=OSError(TOKEN)):
            self.refused("review-transport-unavailable")
        original_pair = socket.socketpair
        allocated = []

        def track_pair():
            pair = original_pair()
            allocated.extend(pair)
            return pair

        with patch.object(hexalith_github_reviews.socket, "socketpair", side_effect=track_pair), \
                patch.object(hexalith_github_reviews.multiprocessing, "get_context") as context:
            context.return_value.Process.side_effect = RuntimeError(TOKEN)
            self.refused("review-transport-unavailable")
        self.assertEqual([channel.fileno() for channel in allocated], [-1, -1])
        self.assertEqual(multiprocessing.active_children(), [])

    def test_large_request_parameters_and_delayed_bootstrap_stay_inside_deadline_without_orphans(self):
        data, output = self.process_probe("delayed-bootstrap")
        self.assertEqual(data["code"], "review-request-deadline-exceeded")
        self.assertLess(data["elapsed"], 0.6)
        self.assertEqual(output.stderr, "")

    def test_oversized_inherited_launch_metadata_refuses_before_blocking_bootstrap(self):
        for mode in ("oversized-name", "oversized-path", "oversized-argv", "oversized-authkey"):
            with self.subTest(mode=mode):
                data, output = self.process_probe(mode)
                self.assertEqual(data["code"], "review-transport-unavailable")
                self.assertLess(data["elapsed"], 0.6)
                self.assertEqual(output.stderr, "")
        self.assertEqual(self.fixture.requests, [])

    def test_full_process_output_contains_no_credentials_on_bootstrap_and_transport_failures(self):
        self.fixture.responses[self.operations.locator.review_path] = {"status": 403, "raw": TOKEN.encode()}
        for mode, code in (("http-denial", "review-http-refused"), ("tls-failure", "review-transport-unavailable"),
                           ("socket-failure", "review-transport-unavailable"), ("bootstrap-failure", "review-transport-unavailable")):
            with self.subTest(mode=mode):
                data, output = self.process_probe(mode)
                self.assertEqual(data["code"], code)
                if mode != "bootstrap-failure":
                    self.assertEqual(output.stderr, "")

    def test_absolute_deadline_bounds_headers_stalled_body_and_slow_trickle_body(self):
        limits = RequestLimits(16_384, 0.35)
        for mode in ({"header_delay": 1.5}, {"body_delay": 1.5}, {"trickle": 0.04, "no_length": True}):
            with self.subTest(mode=tuple(mode)):
                self.fixture.responses[self.operations.locator.review_path] = {"json": self.remote(), **mode}
                started = time.monotonic()
                self.refused("review-request-deadline-exceeded", context=replace(self.context, request_limits=limits))
                self.assertLess(time.monotonic() - started, 0.8)
                self.assertEqual(multiprocessing.active_children(), [])

    def test_certificate_trust_and_hostname_failures_refuse(self):
        untrusted = GitHubReviewClient(self.fixture.port, self.wrong_cert.read_text())
        self.refused("review-transport-unavailable", client=untrusted)
        wrong = FixtureServer(self.wrong_cert, self.wrong_key)
        self.addCleanup(wrong.close)
        client = GitHubReviewClient(wrong.port, self.wrong_cert.read_text())
        self.refused("review-transport-unavailable", client=client, context=replace(self.context, expected_issuer=client.issuer))

    def test_absolute_deadline_also_bounds_dns_resolution_before_tls(self):
        started = time.monotonic()
        with patch.object(hexalith_github_reviews, "_request_worker", dns_stalled_worker):
            self.refused("review-request-deadline-exceeded",
                         context=replace(self.context, request_limits=RequestLimits(16_384, 0.35)))
        self.assertLess(time.monotonic() - started, 0.8)
        self.assertEqual(self.fixture.requests, [])

    def test_tls_handshake_and_unavailable_service_are_bounded_content_free_refusals(self):
        listener = socket.socket()
        listener.bind(("127.0.0.1", 0))
        listener.listen()
        self.addCleanup(listener.close)
        client = GitHubReviewClient(listener.getsockname()[1], self.ca_pem)
        started = time.monotonic()
        self.refused("review-request-deadline-exceeded", client=client,
                     context=replace(self.context, expected_issuer=client.issuer, request_limits=RequestLimits(16_384, 0.35)))
        self.assertLess(time.monotonic() - started, 0.8)
        closed = socket.socket()
        closed.bind(("127.0.0.1", 0))
        port = closed.getsockname()[1]
        closed.close()
        client = GitHubReviewClient(port, self.ca_pem)
        self.refused("review-transport-unavailable", client=client, context=replace(self.context, expected_issuer=client.issuer))

    def test_credentials_never_appear_in_client_observations_or_refusal_output(self):
        self.assertNotIn(TOKEN, repr(self.client))
        output = io.StringIO()
        with redirect_stdout(output), redirect_stderr(output):
            for token in (None, "", "Bearer " + TOKEN, TOKEN + "\r\nInjected: bad"):
                self.refused("review-credential-required", token=token)
            self.fixture.responses[self.operations.locator.review_path] = {"status": 403, "raw": TOKEN.encode()}
            self.refused("review-http-refused")
        self.assertEqual(output.getvalue(), "")

    def test_unsafe_locators_fixture_configuration_and_request_limits_refuse(self):
        for changes in ({"owner": "https://evil.example"}, {"repository": "../../other"}, {"repository": "."},
                        {"pull_number": True}, {"review_id": 0}, {"repository": "repo?token=" + TOKEN}):
            with self.subTest(fields=tuple(changes)), self.assertRaises(GitHubReviewRetrievalError):
                replace(self.operations.locator, **changes)
        for arguments in ((None, self.ca_pem), (True, self.ca_pem), (65536, self.ca_pem), (1234, None)):
            with self.subTest(port=arguments[0]), self.assertRaises(GitHubReviewRetrievalError):
                GitHubReviewClient(*arguments)
        for arguments in ((0, 1), (True, 1), (1_048_577, 1), (100, 0), (100, True), (100, float("inf")), (100, 10**400)):
            with self.subTest(arguments=arguments), self.assertRaises(GitHubReviewRetrievalError):
                RequestLimits(*arguments)
        production = GitHubReviewClient()
        self.assertEqual(production.issuer, PRODUCTION_ISSUER)
        self.assertEqual(production.api_origin, PRODUCTION_ISSUER)

    def test_library_has_no_target_or_legacy_authorization_dependency(self):
        with patch("subprocess.run", side_effect=AssertionError("target command attempted")), \
                patch("subprocess.Popen", side_effect=AssertionError("target command attempted")):
            pair = self.call()
        self.assertEqual(len(pair), 2)
        self.assertEqual({request["path"] for request in self.fixture.requests},
                         {self.operations.locator.review_path, self.security.locator.review_path})

    def test_missing_platform_source_is_an_import_failure_never_a_skip(self):
        with tempfile.TemporaryDirectory(prefix="c1-missing-platform-") as directory:
            tools = Path(directory) / "tools"
            tools.mkdir()
            for name in ("access_telemetry_c1_github_approvals.py", "access_telemetry_c1_interchange.py",
                         "access_telemetry_c1_approval_policy.py"):
                shutil.copyfile(REPO_ROOT / "tools" / name, tools / name)
            result = subprocess.run([sys.executable, "-I", "-c", "import sys; sys.path.insert(0, sys.argv[1]); import access_telemetry_c1_github_approvals", str(tools)],
                                    capture_output=True, text=True, timeout=5)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("ModuleNotFoundError", result.stderr)
        self.assertIn("pinned-hexalith-github-review-source-required", result.stderr)

    def test_library_imports_from_repository_root_without_caller_search_path_setup(self):
        code = '''import sys
sys.path.insert(0, sys.argv[1])
from tools.access_telemetry_c1_github_approvals import consume_bundle_reviews, GitHubReviewClient
assert callable(consume_bundle_reviews)
assert GitHubReviewClient().issuer == "https://api.github.com"
'''
        result = subprocess.run([sys.executable, "-I", "-c", code, str(REPO_ROOT)],
                                capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout + result.stderr, "")

    def test_missing_or_wrong_origin_platform_cannot_use_alternate_or_cached_modules(self):
        with tempfile.TemporaryDirectory(prefix="c1-platform-origin-") as directory:
            root = Path(directory)
            tools = root / "tools"
            unrelated = root / "unrelated"
            tools.mkdir()
            unrelated.mkdir()
            for name in ("access_telemetry_c1_github_approvals.py", "access_telemetry_c1_interchange.py",
                         "access_telemetry_c1_approval_policy.py"):
                shutil.copyfile(REPO_ROOT / "tools" / name, tools / name)
            (unrelated / "hexalith_github_reviews.py").write_text('marker = "unrelated"\n')
            expected = root / "references/Hexalith.Platform/eng/hexalith_github_reviews.py"
            code = '''import sys
sys.path[:0] = [sys.argv[1], sys.argv[2]]
if sys.argv[3] != "path":
    import hexalith_github_reviews
    if sys.argv[3] == "spoof-file":
        hexalith_github_reviews.__file__ = sys.argv[4]
import access_telemetry_c1_github_approvals
'''
            for present, mode in ((False, "path"), (False, "cached"), (True, "cached"), (True, "spoof-file")):
                if present and not expected.exists():
                    expected.parent.mkdir(parents=True)
                    shutil.copyfile(REPO_ROOT / "references/Hexalith.Platform/eng/hexalith_github_reviews.py", expected)
                with self.subTest(present=present, mode=mode):
                    result = subprocess.run([sys.executable, "-I", "-c", code, str(tools), str(unrelated), mode, str(expected)],
                                            capture_output=True, text=True, timeout=5)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn("pinned-hexalith-github-review-" + ("origin" if present else "source") + "-required", result.stderr)


if __name__ == "__main__":
    unittest.main()
