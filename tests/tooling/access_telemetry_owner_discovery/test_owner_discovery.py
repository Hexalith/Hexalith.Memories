"""Offline authorization, exact-command and retained-observation verification."""

from contextlib import ExitStack
from datetime import datetime, timedelta, timezone
import base64
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch


TOOLS = Path(__file__).resolve().parents[3] / "tools"
sys.path.insert(0, str(TOOLS))
import access_telemetry_owner_discovery as discovery


NOW = datetime(2026, 10, 15, 18, 0, tzinfo=timezone.utc)
REAL_POPEN = subprocess.Popen


class OwnerDiscoveryTests(unittest.TestCase):
    """Target queries use offline doubles; local config view tests JSONPath only."""

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        os.chmod(self.root, 0o700)
        self.evidence = self.root / "evidence"
        self.evidence.mkdir(mode=0o700)
        (self.evidence / "discovery").mkdir(mode=0o700)
        self.write(self.evidence / "discovery/.anchor", b"private-output-anchor\n")
        self.credential = self.root / "credential"
        self.ca = b"offline-ca-bytes"
        self.key = b"offline-client-private-key-values"
        self.config = {
            "apiVersion": "v1", "kind": "Config", "current-context": "owner@local",
            "clusters": [{"name": "cluster", "cluster": {
                "server": "https://192.0.2.1:6443",
                "certificate-authority-data": base64.b64encode(self.ca).decode()}}],
            "contexts": [{"name": "owner@local", "context": {"cluster": "cluster", "user": "owner"}}],
            "users": [{"name": "owner", "user": {
                "client-certificate-data": base64.b64encode(b"offline-cert").decode(),
                "client-key-data": base64.b64encode(self.key).decode()}}],
        }
        self.write(self.credential, self.config)
        self.plan_path = self.evidence / "discovery-plan.json"
        self.decision_path = self.evidence / "discovery-approval.json"
        self.status_path = self.evidence / "discovery-status.json"
        self.plan = {
            "schemaVersion": discovery.SCHEMA + "plan/v1", "planId": "offline-discovery",
            "status": "proposed", "ownerPrincipal": discovery.OWNER, "assuranceMode": discovery.ASSURANCE,
            "policyId": discovery.POLICY, "policySha256": "1" * 64,
            "profileId": discovery.PROFILE, "profileSha256": discovery.PROFILE_SHA256,
            "workloadSha256": discovery.WORKLOAD_SHA256,
            "sourceCommit": "2" * 40, "repositoryRoot": str(self.root / "checkout"),
            "evidenceRoot": str(self.evidence), "credentialPath": str(self.credential),
            "credentialMetadata": discovery._metadata(self.credential.stat()),
            "context": "owner@local", "server": "https://192.0.2.1:6443",
            "caPemSha256": discovery._sha(self.ca), "targetClassification": "unknown",
            "notBeforeUtc": "2026-10-15T17:00:00Z", "expiresAtUtc": "2026-10-16T17:00:00Z",
            "maximumDurationSeconds": 300, "maximumCommands": 12,
            "perCommandTimeoutSeconds": 20, "maximumStreamBytes": discovery.MAX_BYTES,
            "allowedAction": discovery.ACTION,
            "queries": discovery.discovery_queries(str(self.credential), "owner@local"),
            "authorization": {"approvalPath": str(self.decision_path), "statusPath": str(self.status_path),
                              "approvedPlanSha256": "outside-owner-decision-required"},
            "notGranted": discovery.NOT_GRANTED,
            "runner": {"path": str(Path(discovery.__file__).absolute()),
                       "sha256": discovery._sha(Path(discovery.__file__).read_bytes())},
        }
        self.seal()

    def write(self, path, value):
        raw = value if type(value) is bytes else discovery._encode(value)
        Path(path).write_bytes(raw)
        os.chmod(path, 0o600)
        return raw

    def seal(self):
        self.digest = discovery._sha(self.write(self.plan_path, self.plan))
        self.decision = {
            "schemaVersion": discovery.SCHEMA + "decision/v1", "planId": self.plan["planId"],
            "planSha256": self.digest, "runnerSha256": self.plan["runner"]["sha256"],
            "ownerPrincipal": discovery.OWNER, "assuranceMode": discovery.ASSURANCE,
            "decision": "approved", "allowedAction": discovery.ACTION,
            "approvedAtUtc": "2026-10-15T17:30:00Z", "expiresAtUtc": self.plan["expiresAtUtc"],
        }
        self.write(self.decision_path, self.decision)
        self.status = {"schemaVersion": discovery.SCHEMA + "status/v1", "planId": self.plan["planId"],
                       "planSha256": self.digest,
                       "decisionSha256": discovery._sha(self.decision_path.read_bytes()), "withdrawn": False}
        self.write(self.status_path, self.status)

    def run_offline(self, execute=None):
        with patch.object(discovery, "_now", return_value=NOW), \
                patch.object(discovery, "_execute", side_effect=execute or (lambda *a: (b"identity\tuid\n", b"", 0, None))):
            return discovery.collect_owner_discovery(str(self.plan_path), self.digest)

    def denied_before_spawn(self, digest=None):
        with patch.object(discovery, "_now", return_value=NOW), \
                patch.object(discovery, "_execute") as execute, \
                patch.object(discovery.subprocess, "Popen") as process:
            with self.assertRaises(discovery.DiscoveryError) as caught:
                discovery.collect_owner_discovery(str(self.plan_path), digest or self.digest)
        execute.assert_not_called()
        process.assert_not_called()
        self.assertIsNone(caught.exception.__cause__)
        self.assertIsNone(caught.exception.__context__)
        self.assertLess(len(str(caught.exception)), 100)
        return str(caught.exception)

    def test_missing_actual_decision_denies_before_process_or_target(self):
        self.decision_path.unlink()
        self.assertEqual(self.denied_before_spawn(), "owner-discovery-owner-decision-required")
        self.assertEqual(list((self.evidence / "discovery").iterdir()), [self.evidence / "discovery/.anchor"])

    def test_outside_digest_required_and_plan_changes_refuse_before_spawn(self):
        self.denied_before_spawn("0" * 64)
        self.write(self.plan_path, self.plan_path.read_bytes() + b" ")
        self.denied_before_spawn()

    def test_decision_cannot_be_policy_adoption_or_wrong_runner_owner_action(self):
        for key, value in (("decision", "pending"), ("ownerPrincipal", "role:owner"),
                           ("runnerSha256", "0" * 64), ("planSha256", "0" * 64),
                           ("allowedAction", "publish"), ("assuranceMode", "independent")):
            with self.subTest(key=key):
                self.seal()
                self.decision[key] = value
                self.write(self.decision_path, self.decision)
                self.denied_before_spawn()

    def test_missing_withdrawn_mismatched_nonboolean_and_malformed_status_denies(self):
        for value in (None, b"not-json", {**self.status, "withdrawn": True},
                      {**self.status, "withdrawn": 0}, {**self.status, "planSha256": "0" * 64},
                      {**self.status, "decisionSha256": "0" * 64}, {**self.status, "extra": False}):
            with self.subTest(value=value):
                self.seal()
                if value is None:
                    self.status_path.unlink()
                else:
                    self.write(self.status_path, value)
                self.denied_before_spawn()

    def test_duplicate_json_keys_refuse_without_leaking_candidate_text(self):
        self.write(self.status_path, b'{"withdrawn": false, "withdrawn": true, "secret": "bearer-value"}')
        self.assertNotIn("bearer", self.denied_before_spawn())

    def test_expired_not_started_and_future_approval_refuse(self):
        for now in (NOW - timedelta(days=1), NOW + timedelta(days=2)):
            with self.subTest(now=now), patch.object(discovery, "_now", return_value=now), \
                    patch.object(discovery, "_execute") as execute:
                with self.assertRaises(discovery.DiscoveryError):
                    discovery.collect_owner_discovery(str(self.plan_path), self.digest)
                execute.assert_not_called()
        self.decision["approvedAtUtc"] = "2026-10-15T19:00:00Z"
        self.write(self.decision_path, self.decision)
        self.denied_before_spawn()

    def test_plan_cannot_expand_any_bound_action_or_classification(self):
        original = json.loads(json.dumps(self.plan))
        for key, value in (("maximumCommands", 13), ("maximumDurationSeconds", 301),
                           ("perCommandTimeoutSeconds", 21), ("maximumStreamBytes", discovery.MAX_BYTES + 1),
                           ("allowedAction", "observe"), ("targetClassification", "production"),
                           ("status", "approved"), ("maximumCommands", 12.0),
                           ("profileId", "PG-ONPREM-1"), ("profileSha256", "0" * 64),
                           ("workloadSha256", "0" * 64)):
            with self.subTest(key=key, value=value):
                self.plan = json.loads(json.dumps(original))
                self.plan[key] = value
                self.seal()
                self.denied_before_spawn()

    def test_exact_twelve_queries_no_secret_log_exec_write_or_foreign_namespace(self):
        baseline = json.loads(json.dumps(self.plan))
        mutations = [lambda p: p["queries"].pop(),
                     lambda p: p["queries"].reverse(),
                     lambda p: p["queries"][0]["argv"].__setitem__(6, "delete"),
                     lambda p: p["queries"][3]["argv"].__setitem__(7, "foreign-tenant"),
                     lambda p: p["queries"][0]["argv"].__setitem__(7, "secret"),
                     lambda p: p["queries"][0]["argv"].append("--raw=/api"),
                     lambda p: p["queries"][0].__setitem__("id", "different-purpose")]
        for mutate in mutations:
            self.plan = json.loads(json.dumps(baseline))
            mutate(self.plan)
            self.seal()
            self.denied_before_spawn()

    def test_fixed_jsonpath_escapes_with_actual_local_only_kubectl_config_view(self):
        # No target, credential or network: config view evaluates the EXACT
        # projection against an inert local extension. Expected bytes are
        # literal fixture values, independent of the query-construction helper.
        fixture = self.root / "local-jsonpath-config"
        self.write(fixture, {
            "apiVersion": "v1", "kind": "Config", "clusters": [], "contexts": [],
            "users": [], "current-context": "", "preferences": {},
            "extensions": [{"name": "offline", "extension": {
                "metadata": {"name": "fixture", "uid": "uid-fixture", "labels": {
                    "environment": "Test", "hexalith.io/environment": "Qualification"}},
                "data": {"gate.json": '{"enabled":false}'},
                "spec": {"holderIdentity": "", "leaseDurationSeconds": 900},
            }}],
        })
        vectors = [
            (self.plan["queries"][0]["argv"][-1], ("metadata",),
             b"fixture\tuid-fixture\tTest\tQualification\n"),
            (self.plan["queries"][9]["argv"][-1], ("metadata", "data"),
             b'uid-fixture\t{"enabled":false}\n'),
            (self.plan["queries"][10]["argv"][-1], ("metadata", "spec"),
             b"uid-fixture\t\t900\n"),
        ]
        for projection, fields, expected in vectors:
            with self.subTest(projection=projection):
                for field in fields:
                    projection = projection.replace("." + field, ".extensions[0].extension." + field)
                process = REAL_POPEN([discovery.KUBECTL, "--kubeconfig", str(fixture),
                                      "config", "view", projection],
                                     stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                     env={"PATH": "/usr/local/bin:/usr/bin:/bin", "HOME": str(self.root),
                                          "KUBECTL_KUBERC": "false"})
                out, err = process.communicate(timeout=10)
                self.assertEqual((process.returncode, out, err), (0, expected, b""))

    def test_symlink_plan_decision_status_credential_and_ancestors_refuse(self):
        for path in (self.plan_path, self.decision_path, self.status_path, self.credential):
            with self.subTest(path=path):
                raw = path.read_bytes()
                target = self.root / "linked-target"
                self.write(target, raw)
                path.unlink()
                path.symlink_to(target)
                self.denied_before_spawn()
                path.unlink()
                self.write(path, raw)
        # Ancestor traversal cannot follow a substituted private directory.
        moved = self.root / "moved"
        self.evidence.rename(moved)
        self.evidence.symlink_to(moved, target_is_directory=True)
        self.denied_before_spawn()

    def test_unsafe_file_and_private_directory_modes_and_hardlinks_refuse(self):
        for path in (self.plan_path, self.decision_path, self.status_path, self.credential, self.evidence):
            with self.subTest(path=path):
                old = path.stat().st_mode & 0o777
                os.chmod(path, 0o755 if path.is_dir() else 0o644)
                self.denied_before_spawn()
                os.chmod(path, old)
        linked = self.root / "extra-credential-link"
        os.link(self.credential, linked)
        self.denied_before_spawn()

    def test_runner_bytes_and_credential_metadata_drift_refuse(self):
        self.plan["runner"]["sha256"] = "0" * 64
        self.seal()
        self.denied_before_spawn()
        self.plan["runner"]["sha256"] = discovery._sha(Path(discovery.__file__).read_bytes())
        self.seal()
        info = self.credential.stat()
        os.utime(self.credential, ns=(info.st_atime_ns, info.st_mtime_ns + 1000000000))
        self.denied_before_spawn()

    def test_local_api_ca_context_exec_proxy_or_external_key_drift_refuse(self):
        baseline = json.loads(json.dumps(self.config))
        changes = [lambda c: c["clusters"][0]["cluster"].__setitem__("server", "https://192.0.2.2:6443"),
                   lambda c: c["clusters"][0]["cluster"].__setitem__("certificate-authority-data", "Zm9yZWlnbg=="),
                   lambda c: c.__setitem__("current-context", "foreign"),
                   lambda c: c["clusters"][0]["cluster"].__setitem__("proxy-url", "http://proxy.invalid"),
                   lambda c: c["clusters"][0]["cluster"].__setitem__("insecure-skip-tls-verify", True),
                   lambda c: c["users"][0]["user"].__setitem__("exec", {"command": "foreign"}),
                   lambda c: c["users"][0]["user"].__setitem__("client-key", "/foreign")]
        for change in changes:
            with self.subTest(change=change):
                config = json.loads(json.dumps(baseline))
                change(config)
                self.write(self.credential, config)
                self.plan["credentialMetadata"] = discovery._metadata(self.credential.stat())
                self.seal()
                self.denied_before_spawn()

    def test_valid_yaml_credential_is_locally_parsed_and_no_values_are_retained(self):
        import yaml
        self.write(self.credential, yaml.safe_dump(self.config).encode())
        self.plan["credentialMetadata"] = discovery._metadata(self.credential.stat())
        self.seal()
        result = self.run_offline()
        for file in Path(result.receipt_path).parent.iterdir():
            self.assertNotIn(self.key, file.read_bytes())
            self.assertNotIn(base64.b64encode(self.key), file.read_bytes())

    def test_exact_query_order_receipts_raw_streams_hashes_and_private_readback(self):
        seen = []
        def execute(argv, deadline, secrets, credential_fd):
            self.assertGreater(deadline, time.monotonic())
            self.assertLessEqual(deadline - time.monotonic(), 20)
            self.assertEqual(argv[2], f"/proc/self/fd/{credential_fd}")
            logical = ["kubectl", *argv[1:]]
            logical[2] = str(self.credential)
            seen.append(logical)
            return b"\t exact stdout \n", b" exact stderr\n", 0, None
        result = self.run_offline(execute)
        self.assertEqual(seen, [q["argv"] for q in self.plan["queries"]])
        self.assertEqual(result.command_count, 12)
        self.assertEqual(result.disposition, "requires-owner-disposition")
        path = Path(result.receipt_path)
        self.assertEqual(discovery._sha(path.read_bytes()), result.receipt_sha256)
        receipt = json.loads(path.read_bytes())
        self.assertFalse(receipt["qualificationCredit"])
        self.assertEqual(receipt["baselineSourceCommit"], self.plan["sourceCommit"])
        self.assertEqual(receipt["runnerSha256"], self.plan["runner"]["sha256"])
        for file in path.parent.iterdir():
            self.assertEqual(file.stat().st_mode & 0o777, 0o600)
        self.assertEqual(path.parent.stat().st_mode & 0o777, 0o700)
        for record, query in zip(receipt["commands"], self.plan["queries"]):
            self.assertEqual(record["id"], query["id"])
            self.assertEqual(record["argv"], query["argv"])
            self.assertEqual(record["executedArgv"][0], discovery.KUBECTL)
            for name in ("stdout", "stderr"):
                ref = record[name]
                raw = (path.parent / ref["path"]).read_bytes()
                self.assertEqual(discovery._sha(raw), ref["sha256"])
                self.assertEqual(len(raw), ref["byteLength"])

    def test_nonzero_notfound_and_empty_outputs_are_observations_never_passes(self):
        result = self.run_offline(lambda *a: (b"", b"Error from server (NotFound)\n", 1, None))
        receipt = json.loads(Path(result.receipt_path).read_bytes())
        self.assertEqual(len(receipt["commands"]), 12)
        self.assertTrue(all(r["exitCode"] == 1 and r["disposition"] == "requires-owner-disposition"
                            for r in receipt["commands"]))
        self.assertFalse(receipt["qualificationCredit"])

    def test_each_run_uses_exclusive_output_and_fresh_authority(self):
        first = self.run_offline()
        second = self.run_offline()
        self.assertNotEqual(first.receipt_path, second.receipt_path)
        self.assertEqual(discovery._sha(Path(first.receipt_path).read_bytes()), first.receipt_sha256)
        self.status["withdrawn"] = True
        self.write(self.status_path, self.status)
        self.denied_before_spawn()

    def test_withdrawal_during_first_command_stops_before_next_and_retains_partial_receipt(self):
        calls = []
        def execute(*args):
            calls.append(1)
            self.status["withdrawn"] = True
            self.write(self.status_path, self.status)
            return b"first observation\n", b"", 0, None
        with self.assertRaises(discovery.DiscoveryError):
            self.run_offline(execute)
        self.assertEqual(len(calls), 1)
        receipts = list((self.evidence / "discovery").glob("discovery-*/receipt.json"))
        self.assertEqual(len(receipts), 1)
        receipt = json.loads(receipts[0].read_bytes())
        self.assertEqual(receipt["disposition"], "refused")
        self.assertEqual(len(receipt["commands"]), 1)

    def test_local_credential_change_after_command_stops_before_second_command(self):
        calls = []
        def execute(*args):
            calls.append(1)
            self.credential.touch()
            return b"observation", b"", 0, None
        with self.assertRaises(discovery.DiscoveryError):
            self.run_offline(execute)
        self.assertEqual(len(calls), 1)

    def test_expiry_during_execution_stops_before_next_command(self):
        clock = [NOW]
        calls = []
        def execute(*args):
            calls.append(1)
            clock[0] = NOW + timedelta(days=1)
            return b"partial", b"", 0, None
        with patch.object(discovery, "_now", side_effect=lambda: clock[0]), \
                patch.object(discovery, "_execute", side_effect=execute):
            with self.assertRaises(discovery.DiscoveryError):
                discovery.collect_owner_discovery(str(self.plan_path), self.digest)
        self.assertEqual(len(calls), 1)

    def test_aggregate_execution_bound_denies_before_spawn(self):
        with patch.object(discovery.time, "monotonic", side_effect=[100, 400]), \
                patch.object(discovery, "_now", return_value=NOW), \
                patch.object(discovery, "_execute") as execute:
            with self.assertRaises(discovery.DiscoveryError):
                discovery.collect_owner_discovery(str(self.plan_path), self.digest)
            execute.assert_not_called()

    def test_final_withdrawal_after_twelfth_command_discards_completion(self):
        calls = []
        def execute(*args):
            calls.append(1)
            if len(calls) == 12:
                self.status["withdrawn"] = True
                self.write(self.status_path, self.status)
            return b"observation", b"", 0, None
        with self.assertRaises(discovery.DiscoveryError):
            self.run_offline(execute)
        self.assertEqual(len(calls), 12)

    def test_terminal_status_is_checked_after_receipt_readback(self):
        original = discovery._Evidence.readback
        def readback(evidence):
            result = original(evidence)
            if any(name == "receipt.json" for name, _ in evidence.files):
                self.status["withdrawn"] = True
                self.write(self.status_path, self.status)
            return result
        with patch.object(discovery._Evidence, "readback", readback):
            with self.assertRaises(discovery.DiscoveryError):
                self.run_offline()
        self.assertEqual(len(list((self.evidence / "discovery").glob("discovery-*/final-refusal.json"))), 1)

    def test_output_byte_drift_and_cleanup_fault_cannot_return_completion(self):
        original = discovery._Evidence.readback
        corrupted = []
        def corrupt(evidence):
            if evidence.files and not corrupted:
                name, raw = evidence.files[0]
                Path(evidence.path, name).write_bytes(b"X" * len(raw))
                corrupted.append(1)
            return original(evidence)
        with patch.object(discovery._Evidence, "readback", corrupt):
            with self.assertRaises(discovery.DiscoveryError):
                self.run_offline()
        real_close = os.close
        def close_then_fail(fd):
            real_close(fd)
            raise OSError("private-content")
        with patch.object(discovery.os, "close", side_effect=close_then_fail):
            self.assertNotIn("private-content", self.denied_before_spawn())

    def test_process_timeouts_and_output_bounds_retain_partial_failure_and_stop(self):
        for reason in ("deadline-exceeded", "stream-bound-exceeded", "process-unavailable"):
            calls = []
            def execute(*args):
                calls.append(1)
                return b"bounded-partial", b"bounded-error", -9, reason
            with self.subTest(reason=reason), self.assertRaises(discovery.DiscoveryError):
                self.run_offline(execute)
            self.assertEqual(len(calls), 1)
        self.assertEqual(len(list((self.evidence / "discovery").glob("discovery-*/receipt.json"))), 3)

    def test_real_offline_child_both_stream_limits_timeout_and_credential_suppression(self):
        def capture(program, seconds=2, secrets=()):
            def launch(*args, **kwargs):
                self.assertEqual(args[0][0], discovery.KUBECTL)
                self.assertEqual(kwargs["cwd"], "/")
                self.assertNotIn("KUBECONFIG", kwargs["env"])
                self.assertNotIn("HTTP_PROXY", kwargs["env"])
                return REAL_POPEN([sys.executable, "-I", "-c", program], **kwargs)
            with ExitStack() as stack, patch.object(discovery.subprocess, "Popen", side_effect=launch):
                credential_fd = discovery._sealed_credential(self.credential.read_bytes(), stack)
                return discovery._execute(self.plan["queries"][0]["argv"], time.monotonic() + seconds,
                                          secrets, credential_fd)
        out, err, code, reason = capture("import os; os.write(1,b'raw \\t\\n'); os.write(2,b'error\\n')")
        self.assertEqual((out, err, code, reason), (b"raw \t\n", b"error\n", 0, None))
        for fd in (1, 2):
            out, err, code, reason = capture(f"import os; os.write({fd}, b'x' * 1048577)")
            self.assertEqual(reason, "stream-bound-exceeded")
            self.assertLessEqual(len(out), discovery.MAX_BYTES)
            self.assertLessEqual(len(err), discovery.MAX_BYTES)
        out, err, code, reason = capture("import os; os.write(1, b'x' * 1048576)")
        self.assertEqual((len(out), err, code, reason), (1048576, b"", 0, None))
        before = time.monotonic()
        _, _, _, reason = capture("import time; time.sleep(10)", seconds=0.1)
        self.assertEqual(reason, "deadline-exceeded")
        self.assertLess(time.monotonic() - before, 3)
        self.assertEqual(capture("print('private-credential-value')", secrets=(b"private-credential-value",))[::3],
                         (b"", "credential-output-refused"))

    def test_real_offline_timeout_kills_descendant_and_closes_owned_pipes(self):
        pid_path = self.root / "child.pid"
        children = []
        program = ("import os,time; pid=os.fork(); "
                   f"open({str(pid_path)!r},'w').write(str(pid)); time.sleep(10)")
        def launch(*args, **kwargs):
            child = REAL_POPEN([sys.executable, "-I", "-c", program], **kwargs)
            children.append(child)
            return child
        with ExitStack() as stack, patch.object(discovery.subprocess, "Popen", side_effect=launch):
            credential_fd = discovery._sealed_credential(self.credential.read_bytes(), stack)
            _, _, _, reason = discovery._execute(self.plan["queries"][0]["argv"], time.monotonic() + 0.2,
                                                 (), credential_fd)
        self.assertEqual(reason, "deadline-exceeded")
        self.assertIsNotNone(children[0].returncode)
        self.assertTrue(children[0].stdout.closed)
        self.assertTrue(children[0].stderr.closed)
        # The immediate child and all descendants share the terminated group.
        # A reparented zombie may briefly persist, but cannot execute or write.
        if pid_path.exists():
            child_pid = int(pid_path.read_text())
            if child_pid > 0 and Path(f"/proc/{child_pid}/stat").exists():
                self.assertEqual(Path(f"/proc/{child_pid}/stat").read_text().split()[2], "Z")

    def test_mutable_credential_replacement_cannot_redirect_spawned_child(self):
        # Replace the canonical pathname at the Popen boundary, after check().
        # The child consumes only sealed original bytes and cannot modify them.
        observations = []
        def launch(command, **kwargs):
            replacement = self.root / "credential-new"
            foreign = json.loads(json.dumps(self.config))
            foreign["clusters"][0]["cluster"]["server"] = "https://foreign.invalid:6443"
            self.write(replacement, foreign)
            replacement.replace(self.credential)
            fd_path = command[2]
            self.assertTrue(fd_path.startswith("/proc/self/fd/"))
            program = ("import sys,os; raw=open(sys.argv[1],'rb').read(); "
                       "assert b'foreign.invalid' not in raw; "
                       "assert b'192.0.2.1' in raw; "
                       "print('pinned-input-observed')")
            child = REAL_POPEN([sys.executable, "-I", "-c", program, fd_path], **kwargs)
            observations.append(child)
            return child
        with patch.object(discovery.subprocess, "Popen", side_effect=launch), \
                patch.object(discovery, "_now", return_value=NOW):
            with self.assertRaises(discovery.DiscoveryError):
                discovery.collect_owner_discovery(str(self.plan_path), self.digest)
        self.assertEqual(len(observations), 1)
        self.assertEqual(observations[0].returncode, 0)
        raw = next((self.evidence / "discovery").glob("discovery-*/00.stdout")).read_bytes()
        self.assertEqual(raw, b"pinned-input-observed\n")

    def test_sealed_credential_prevents_write_and_public_credential_parent_is_supported(self):
        with ExitStack() as stack:
            fd = discovery._sealed_credential(self.credential.read_bytes(), stack)
            with self.assertRaises(OSError):
                os.write(fd, b"foreign")
            with self.assertRaises(OSError):
                os.ftruncate(fd, 0)
        os.chmod(self.root, 0o755)
        # Evidence itself remains private. Its public parent is checked by
        # identity, while the credential FILE retains 0600 and owner custody.
        self.run_offline()

    def test_mid_command_runner_source_drift_stops_before_second_command(self):
        runner = self.root / "runner.py"
        self.write(runner, Path(discovery.__file__).read_bytes())
        self.plan["runner"]["path"] = str(runner)
        self.seal()
        calls = []
        def execute(*args):
            calls.append(1)
            raw = runner.read_bytes()
            self.write(runner, b"#" + raw[1:])
            return b"bounded observation", b"", 0, None
        with patch.object(discovery, "__file__", str(runner)), self.assertRaises(discovery.DiscoveryError):
            self.run_offline(execute)
        self.assertEqual(len(calls), 1)

    def test_cli_missing_approval_has_bounded_exit_two_without_receipt(self):
        self.decision_path.unlink()
        command = [sys.executable, "-I", "-B", str(Path(discovery.__file__)),
                   "--plan", str(self.plan_path), "--approved-plan-sha256", self.digest]
        result = REAL_POPEN(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        stdout, stderr = result.communicate(timeout=5)
        self.assertEqual(result.returncode, 2)
        self.assertTrue(stdout.startswith(b"owner-discovery-"))
        self.assertEqual(stderr, b"")
        self.assertNotIn(str(self.credential).encode(), stdout)


if __name__ == "__main__":
    unittest.main()
