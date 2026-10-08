"""Independent isolated offline fixtures; no test supplies real registration."""

from __future__ import annotations

from contextlib import ExitStack, contextmanager
from copy import deepcopy
from dataclasses import FrozenInstanceError, replace
import hashlib
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))

from access_telemetry_c1_interchange import (  # noqa: E402
    InterchangeFormatError, JsonSnapshot, MAX_AGGREGATE_BYTES, MAX_ARTIFACT_BYTES, j1_bytes, parse_capture,
)
from access_telemetry_c1_producer_bindings import (  # noqa: E402
    RegistryInspection, SourceSnapshot, inspect_registry, inspect_sources, lookup_deployed_binding,
)


COMMIT = "0123456789abcdef0123456789abcdef01234567"
SCHEMA = "hexalith.access-telemetry.c1.evidence/v2"
PRODUCER = "tools/verify-access-telemetry-c1.ps1"
HELPERS = ["tools/access-telemetry-c1-component-backend.ps1", "tools/access-telemetry-c1-profile.ps1"]
# Authored from the contract, independently of production constants/readers.
INPUTS = [
    "deploy/dapr/components/access-telemetry-config.yaml",
    "deploy/dapr/components/access-telemetry-secrets.yaml",
    "deploy/dapr/components/access-telemetry-store.yaml",
    "deploy/kubernetes/base/access-telemetry-deployments.yaml",
    "deploy/kubernetes/base/access-telemetry-postgresql.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-clock-config.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-config-store.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml",
    "deploy/kubernetes/base/dapr/access-telemetry-store.yaml",
    "deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml",
    "deploy/kubernetes/overlays/production/kustomization.yaml",
    "deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml",
    "deploy/openbao/service-account-hardening.yaml",
    "deploy/openbao/smoke-test.yaml",
    "deploy/openbao/values.yaml",
]
PATHS = [PRODUCER, *HELPERS, *INPUTS]

ENTRY_J1 = (
    '{"captureSchema":"fixture.capture/v1","cleanupRequired":false,"commandContract":'
    '{"byteLength":1,"path":"fixtures/command.json","sha256":"' + "0" * 64 + '"},'
    '"gate":"C1.15","helperPaths":[],"inputPaths":[],"producerPath":"fixtures/producer.ps1",'
    '"profileId":"PG-ONPREM-2","registeredStory":"fixtures/stories/café.md",'
    '"registrationReceipt":{"byteLength":1,"path":"fixtures/registration.json","sha256":"'
    + "0" * 64 + '"},"reviewRolePolicy":{"byteLength":1,"path":"fixtures/roles.json",'
    '"sha256":"' + "0" * 64 + '"},"verifierPath":"fixtures/verifier.py",'
    '"verifierSchema":"fixture.verifier/v1"}'
).encode("utf-8")
ENTRY_J1_SHA = "35d330fddee6287d6bd4d4b57d718c832b8008c22ff33713e0126244a3faf7a8"
REGISTRY_J1_SHA = "ad91e1c561d5eb5be4e14f77e244507b3bc82da52f9f1a279de5e1eef70953f0"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def ps_hash(value):
    # This fixture's target/arguments are ASCII and require no special PS escapes.
    return sha(json.dumps(value, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))


def snapshot(value):
    return JsonSnapshot((json.dumps(value, ensure_ascii=False, indent=2) + "\r\n").encode("utf-8"))


def fixture_ref(name):
    return {"path": "fixtures/" + name + ".json", "sha256": "0" * 64, "byteLength": 1}


def registry_entry():
    return {
        "gate": "C1.15", "profileId": "PG-ONPREM-2",
        "registeredStory": "fixtures/stories/isolated-done-label.md",
        "registrationReceipt": fixture_ref("registration"), "producerPath": PRODUCER,
        "helperPaths": HELPERS.copy(), "inputPaths": INPUTS.copy(), "captureSchema": SCHEMA,
        "verifierPath": "fixtures/never-executed-verifier.py", "verifierSchema": "fixture.verifier/v1",
        "commandContract": fixture_ref("command"), "cleanupRequired": False,
        "reviewRolePolicy": fixture_ref("roles"),
    }


def byte_pair(path, executed=None, blob=None):
    if executed is None:
        executed = ("# ISOLATED OFFLINE FIXTURE; NO REGISTRATION: " + path + "\r\n雪\r\n").encode("utf-8")
    if blob is None:
        blob = executed.replace(b"\r\n", b"\n")
    return SourceSnapshot(path, COMMIT, executed, blob, "utf8-crlf-to-lf", "100644", None, None, False)


def fixture_sources():
    return tuple(byte_pair(path) for path in PATHS)


def fixture_capture(sources):
    # This independent one-Pod capture is constructed without any production
    # serializer, source-reader, digest helper or existing capture-fixture call.
    start = "2026-10-08T10:00:00.0000000+00:00"
    finish = "2026-10-08T10:00:01.0000000+00:00"
    target = {"context": "isolated-fixture", "namespace": "fixture-namespace", "selector": "app=fixture",
              "appId": "fixture-app", "actorType": "FixtureActor"}
    pod = {"pod": "fixture-pod", "podUid": "isolated-fixture-uid", "runtimeVersion": "1.18.1",
           "sidecarImageId": "containerd://sha256:" + "a" * 64, "sidecarImageDigest": "sha256:" + "a" * 64,
           "appId": target["appId"], "schedulerConnectedAddresses": ["fixture-scheduler:50006"],
           "actorTypes": ["FixtureActor"], "enabledFeatures": [],
           "alphaOptIn": {"componentIsAlpha": False, "allowAlphaComponent": False}}
    observations = {"pods": [pod], "runtimeVersions": [pod["runtimeVersion"]],
                    "sidecarImageIds": [pod["sidecarImageId"]], "sidecarImageDigests": [pod["sidecarImageDigest"]],
                    "appIds": [pod["appId"]], **{key: deepcopy(pod[key]) for key in
                    ("schedulerConnectedAddresses", "actorTypes", "enabledFeatures", "alphaOptIn")}}
    commands = []
    for number, purpose in enumerate(("current-context", "lifecycle-pods", "daprd-version:fixture-pod",
                                      "metadata:fixture-pod", "alpha-opt-in:fixture-pod", "lifecycle-pods-recheck"), 1):
        arguments = ["isolated-fixture-observation", purpose]
        commands.append({
            "purpose": purpose, "executable": "kubectl", "arguments": arguments,
            "argumentsSha256": ps_hash(arguments), "sha256": sha(("kubectl " + "\x1f".join(arguments)).encode()),
            "startedAtUtc": f"2026-10-08T10:00:00.{number}000000+00:00",
            "finishedAtUtc": f"2026-10-08T10:00:00.{number}500000+00:00", "exitCode": 0,
            "stdoutSha256": sha(b"ISOLATED FIXTURE OUTPUT"), "stderrSha256": sha(b""),
            "streamSafety": "validated", "resultCount": 1, "failureCount": 0, "skipCount": 0,
        })
    arguments = ["-Gate", "C1.15", "-ProfileId", "PG-ONPREM-2", "-QualificationSessionId",
                 "isolated-offline-fixture", "-EvidenceDirectory", "/isolated/fixture", "-CommandTimeoutSeconds", "30"]
    producer_sources = []
    for source in sources:
        blob = source.blob_bytes
        producer_sources.append({
            "source": source.path, "gitNormalizedSha256": sha(blob), "executedBytesSha256": sha(source.executed_bytes),
            "gitBlobOid": hashlib.sha1(b"blob " + str(len(blob)).encode("ascii") + b"\0" + blob).hexdigest(),
            "headGitNormalizedSha256": sha(blob), "trackedAtHead": True, "modified": False, "matchesHead": True,
        })
    by_path = {source.path: source for source in sources}
    source_hashes = [{"source": PRODUCER, "sha256": sha(by_path[PRODUCER].executed_bytes)}]
    for command in commands:
        purpose = command["purpose"]
        if purpose.startswith("metadata:"):
            projection = {"id": pod["appId"], "runtimeVersion": pod["runtimeVersion"],
                          "schedulerConnectedAddresses": pod["schedulerConnectedAddresses"],
                          "actorTypes": pod["actorTypes"], "enabledFeatures": pod["enabledFeatures"]}
            source_hashes.append({"source": "kubectl:" + purpose + ":allowlisted", "sha256": ps_hash(projection)})
        elif purpose.startswith("lifecycle-pods"):
            source_hashes.append({"source": "kubectl:" + purpose + ":identity", "sha256": command["stdoutSha256"]})
        else:
            source_hashes.extend({"source": "kubectl:" + purpose + ":" + stream, "sha256": command[stream + "Sha256"]}
                                 for stream in ("stdout", "stderr"))
    return {
        "schemaVersion": SCHEMA, "gate": "C1.15", "profileId": "PG-ONPREM-2", "capturedAtUtc": start,
        "context": target["context"], "namespace": target["namespace"], "targetSelector": target["selector"],
        "producerStatus": "observed", "gateStatus": "not-evaluated", "productionGatePassed": False,
        "productionLifecycleWrites": "not-evaluated", "independentDisposition": "pending",
        "observations": observations, "blockers": [], "sources": source_hashes, "commands": commands,
        "profileIdentity": "isolated-offline-fixture", "profileSha256": "b" * 64, "workloadId": "fixture-workload",
        "workloadSha256": "c" * 64, "qualificationSessionId": "isolated-offline-fixture",
        "target": target, "targetSha256": ps_hash(target),
        "producer": {"path": PRODUCER, "identity": "repository-collector", "identityAuthentication": "not-evaluated",
                     "arguments": arguments, "argumentsSha256": ps_hash(arguments), "startedAtUtc": start,
                     "finishedAtUtc": finish, "exitCode": 0},
        "sourceCommands": [{"executable": "git", "arguments": ["status"], "argumentsSha256": ps_hash(["status"]),
                            "startedAtUtc": start, "finishedAtUtc": "2026-10-08T10:00:00.0500000+00:00",
                            "exitCode": 0, "stdoutSha256": sha(b""), "stderrSha256": sha(b""),
                            "streamSafety": "hash-only-source-provenance", "resultCount": 1,
                            "failureCount": 0, "skipCount": 0}],
        "sourceCommit": COMMIT, "sourceDisposition": "clean", "worktreeDirty": False,
        "producerSources": producer_sources, "finalSourceRecheck": "unchanged", "resultCount": 1,
        "failureCount": 0, "skipCount": 0,
    }


class ProducerBindingTests(unittest.TestCase):
    def setUp(self):
        self.entry = registry_entry()
        self.registry = inspect_registry(snapshot([self.entry]))
        self.sources = fixture_sources()
        self.capture = fixture_capture(self.sources)

    @contextmanager
    def denial_barriers(self):
        counts = {}
        def forbidden(name):
            def call(*args, **kwargs):
                counts[name] = counts.get(name, 0) + 1
                raise AssertionError(name + " was called")
            return call
        class ForbiddenEnvironment:
            __getitem__ = forbidden("os.environ")
            __iter__ = forbidden("os.environ")
            __len__ = forbidden("os.environ")
            __contains__ = forbidden("os.environ")
            get = forbidden("os.environ")
            copy = forbidden("os.environ")
        with ExitStack() as stack:
            for name in ("builtins.open", "pathlib.Path.open", "pathlib.Path.read_bytes", "subprocess.Popen",
                         "subprocess.run", "os.system", "os.popen", "os.getenv", "shutil.which",
                         "socket.socket", "socket.create_connection", "urllib.request.urlopen"):
                stack.enter_context(patch(name, side_effect=forbidden(name)))
            stack.enter_context(patch("os.environ", ForbiddenEnvironment()))
            yield
        self.assertEqual(counts, {})

    def refuse(self, action, code=None):
        with self.denial_barriers(), self.assertRaises(InterchangeFormatError) as caught:
            action()
        message = str(caught.exception)
        self.assertLess(len(message), 80)
        self.assertNotIn("ISOLATED", message)
        self.assertNotIn("private-test-credential", message)
        self.assertTrue(caught.exception.__suppress_context__)
        if code is not None:
            self.assertEqual(message, code)
        return caught.exception

    def inspect(self, *, entry=None, capture=None, sources=None, commit=COMMIT):
        registry = self.registry if entry is None else inspect_registry(snapshot([entry]))
        return inspect_sources(registry, snapshot(self.capture if capture is None else capture),
                               self.sources if sources is None else sources, commit)

    def test_literal_j1_entry_inventory_and_separate_retained_hashes(self):
        value = json.loads(ENTRY_J1)
        raw = snapshot([value])
        result = inspect_registry(raw)
        self.assertEqual(j1_bytes(raw.value[0]), ENTRY_J1)
        self.assertEqual(result.entries[0].registry_entry_sha256, ENTRY_J1_SHA)
        self.assertEqual(result.registry_sha256, REGISTRY_J1_SHA)
        self.assertIs(result.snapshot, raw)
        self.assertNotEqual(raw.sha256, result.registry_sha256)
        compact = inspect_registry(JsonSnapshot(b"[" + ENTRY_J1 + b"]"))
        self.assertEqual(compact.registry_sha256, result.registry_sha256)
        self.assertNotEqual(compact.snapshot.sha256, raw.sha256)

    def test_empty_registry_and_numeric_gate_order_are_valid_inventory(self):
        empty = inspect_registry(JsonSnapshot(b"[]"))
        self.assertEqual(empty.entries, ())
        self.assertEqual(empty.registry_sha256, "4f53cda18c2baa0c0354bb5f9a3ecbe5ed12ab4d8e11ba873c2f11161202b945")
        values = [dict(self.entry, gate="C1.2"), dict(self.entry, gate="C1.10")]
        self.assertEqual(tuple(entry.gate for entry in inspect_registry(snapshot(values)).entries), ("C1.2", "C1.10"))
        for bad in (values[::-1], [values[0], values[0]]):
            self.refuse(lambda: inspect_registry(snapshot(bad)), "registry-gates-not-ordered-unique")

    def test_registry_count_order_and_uniqueness_refuse_before_entry_hashing(self):
        values = [dict(self.entry, gate="C1." + str(number)) for number in range(1, 26)]
        for entries, code in ((values + [values[-1]], "registry-gate-count-exceeded"),
                              ([values[1], values[0]], "registry-gates-not-ordered-unique"),
                              ([values[0], values[0]], "registry-gates-not-ordered-unique")):
            raw = snapshot(entries)
            with patch("access_telemetry_c1_producer_bindings.hashlib.sha256") as checksum:
                self.refuse(lambda: inspect_registry(raw), code)
                checksum.assert_not_called()

    def test_all_thirteen_fields_missing_unknown_duplicate_and_wrong_type_refuse(self):
        self.assertEqual(len(self.entry), 13)
        for key in self.entry:
            value = deepcopy(self.entry)
            del value[key]
            with self.subTest(missing=key):
                self.refuse(lambda: inspect_registry(snapshot([value])))
            value = dict(self.entry, **{key: None})
            with self.subTest(type=key):
                self.refuse(lambda: inspect_registry(snapshot([value])))
        self.refuse(lambda: inspect_registry(snapshot([dict(self.entry, passed=True)])))
        raw = json.dumps(self.entry).encode()
        self.refuse(lambda: inspect_registry(JsonSnapshot(b"[" + raw[:-1] + b',"gate":"C1.15"}]')))
        for root in ({}, None, True, 1, "inventory"):
            self.refuse(lambda: inspect_registry(snapshot(root)))
        for value in (b"[]", [], None):
            self.refuse(lambda: inspect_registry(value))

    def test_invalid_gate_profile_schema_and_cleanup_types_refuse(self):
        for gate in ("C1.0", "C1.26", "C1.01", "C1.*", " C1.15", "C1.15\n", 15, True):
            self.refuse(lambda: inspect_registry(snapshot([dict(self.entry, gate=gate)])))
        for field, value in (("profileId", "PG-ONPREM-1"), ("cleanupRequired", 0), ("cleanupRequired", "false"),
                             ("captureSchema", ""), ("verifierSchema", "bearer private-test-credential")):
            self.refuse(lambda: inspect_registry(snapshot([dict(self.entry, **{field: value})])))

    def test_three_refs_are_closed_canonical_and_bounded(self):
        for field in ("registrationReceipt", "commandContract", "reviewRolePolicy"):
            for update in ({"unknown": True}, {"path": "../escape"}, {"sha256": "A" * 64},
                           {"byteLength": True}, {"byteLength": 0}, {"byteLength": MAX_ARTIFACT_BYTES + 1}):
                value = deepcopy(self.entry)
                value[field].update(update)
                with self.subTest(field=field, update=update):
                    self.refuse(lambda: inspect_registry(snapshot([value])))
            for key in ("path", "sha256", "byteLength"):
                value = deepcopy(self.entry)
                del value[field][key]
                self.refuse(lambda: inspect_registry(snapshot([value])))

    def test_unsafe_paths_in_every_source_story_and_verifier_position_refuse(self):
        for field in ("producerPath", "registeredStory", "verifierPath", "helperPaths", "inputPaths"):
            for path in ("/absolute", "a\\b", "a//b", "a/../b", "a/./b", "~alias", "x:y", "a\x00b",
                         "a" * 513, "password=private-test-credential"):
                value = dict(self.entry, **{field: [path] if field.endswith("Paths") else path})
                with self.subTest(field=field, path=path):
                    self.refuse(lambda: inspect_registry(snapshot([value])))

    def test_duplicate_unordered_and_overlapping_source_paths_refuse(self):
        for field in ("helperPaths", "inputPaths"):
            for paths in (["fixtures/a", "fixtures/a"], ["fixtures/z", "fixtures/a"]):
                self.refuse(lambda: inspect_registry(snapshot([dict(self.entry, **{field: paths})])))
        for updates in ({"helperPaths": [PRODUCER]}, {"inputPaths": [HELPERS[0]]},
                        {"producerPath": "fixtures/a", "helperPaths": ["fixtures/a-bridge", "fixtures/a/child"]},
                        {"helperPaths": ["fixtures/a"], "inputPaths": ["fixtures/a/child"]}):
            self.refuse(lambda: inspect_registry(snapshot([dict(self.entry, **updates)])), "registry-source-path-overlap")

    def test_complete_clean_crlf_sources_compare_to_the_independent_capture(self):
        result = self.inspect()
        self.assertEqual(len(result.sources), 19)
        self.assertEqual(tuple(item.path for item in result.sources), tuple(sorted(PATHS)))
        self.assertEqual(result.source_commit, COMMIT)
        self.assertIs(result.registry.snapshot, self.registry.snapshot)
        self.assertFalse(result.capture.value["productionGatePassed"])
        by_path = {source.path: source for source in self.sources}
        for item in result.sources:
            self.assertEqual(item.executed_bytes_sha256, sha(by_path[item.path].executed_bytes))
            self.assertEqual(item.git_normalized_sha256, sha(by_path[item.path].blob_bytes))
            self.assertNotEqual(item.executed_bytes_sha256, item.git_normalized_sha256)
        self.assertEqual(result.unique_retained_byte_count,
                         len(self.registry.snapshot.raw) + len(result.capture.raw)
                         + sum(len(source.executed_bytes) + len(source.blob_bytes) for source in self.sources))

    def test_middle_registry_entry_and_reordered_sources_keep_selected_identities(self):
        entries = [dict(self.entry, gate="C1.2"), self.entry, dict(self.entry, gate="C1.25")]
        registry = inspect_registry(snapshot(entries))
        reordered = self.sources[7:] + self.sources[:7]
        result = inspect_sources(registry, snapshot(self.capture), reordered, COMMIT)
        self.assertEqual(result.entry, registry.entries[1])
        self.assertEqual(result.entry.registry_entry_sha256, self.registry.entries[0].registry_entry_sha256)
        self.assertEqual(tuple(item.gate for item in result.registry.entries), ("C1.2", "C1.15", "C1.25"))
        self.assertEqual(result.sources, self.inspect().sources)
        by_path = {source.path: source for source in reordered}
        for item in result.sources:
            self.assertEqual(item.source_commit, COMMIT)
            self.assertEqual(item.executed_bytes_sha256, sha(by_path[item.path].executed_bytes))
            self.assertEqual(item.git_normalized_sha256, sha(by_path[item.path].blob_bytes))

    def test_literal_git_blob_vectors_and_lf_lone_cr_unicode_identity(self):
        for executed, blob, oid in (
            (b"test\r\n", b"test\n", "9daeafb9864cf43055ae93beb0afd6c7d144bfa4"),
            (b"hello\n", b"hello\n", "ce013625030ba8dba906f756967f9e9ca394464a"),
            (b"", b"", "e69de29bb2d1d6434b8b29ae775ad8c2e48c5391"),
        ):
            sources = (byte_pair(PRODUCER, executed, blob), *self.sources[1:])
            result = self.inspect(sources=sources, capture=fixture_capture(sources))
            self.assertEqual(next(item for item in result.sources if item.path == PRODUCER).git_blob_oid, oid)
        for data in ("ISOLATED café 雪\rX\n".encode(), b"\xef\xbb\xbfISOLATED\n"):
            sources = (byte_pair(PRODUCER, data, data), *self.sources[1:])
            self.inspect(sources=sources, capture=fixture_capture(sources))

    def test_missing_extra_substituted_duplicate_and_mutable_sources_refuse(self):
        for sources in (self.sources[:-1], self.sources + (byte_pair("fixtures/extra.py"),),
                        (replace(self.sources[0], path=".editorconfig"), *self.sources[1:]),
                        (replace(self.sources[0], path="tools/access_telemetry_c2_producer.py"), *self.sources[1:]),
                        self.sources + (self.sources[0],), list(self.sources), (b"caller-pass",)):
            self.refuse(lambda: self.inspect(sources=sources))
        for path in (".editorconfig", "tools/access_telemetry_c2_producer.py"):
            self.refuse(lambda: self.inspect(entry=dict(self.entry, producerPath=path)))
        self.refuse(lambda: inspect_sources(inspect_registry(JsonSnapshot(b"[]")), snapshot(self.capture), self.sources, COMMIT))

    def test_source_count_refuses_before_any_source_shape_or_digest_work(self):
        sources = tuple(byte_pair("fixtures/empty-%03d.py" % number, b"", b"") for number in range(256))
        capture = snapshot(self.capture)
        with patch("access_telemetry_c1_producer_bindings._source_shape") as shape, \
                patch("access_telemetry_c1_producer_bindings.hashlib.sha256") as sha256, \
                patch("access_telemetry_c1_producer_bindings.hashlib.sha1") as sha1:
            self.refuse(lambda: inspect_sources(self.registry, capture, sources, COMMIT),
                        "retained-source-count-mismatch")
            shape.assert_not_called()
            sha256.assert_not_called()
            sha1.assert_not_called()

    def test_complete_commit_labels_are_required_on_expected_capture_and_every_source(self):
        for commit in (None, True, "0123456", COMMIT.upper(), " " + COMMIT, COMMIT + "\n"):
            self.refuse(lambda: self.inspect(commit=commit))
            self.refuse(lambda: replace(self.sources[0], source_commit=commit))
        self.refuse(lambda: self.inspect(commit="f" * 40), "capture-source-commit-mismatch")
        self.refuse(lambda: self.inspect(capture=dict(self.capture, sourceCommit="f" * 40)))
        for number in range(len(self.sources)):
            sources = list(self.sources)
            sources[number] = replace(sources[number], source_commit="f" * 40)
            self.refuse(lambda: self.inspect(sources=tuple(sources)), "retained-source-commit-mismatch")

    def test_each_source_receipt_digest_and_oid_are_recomputed(self):
        for number in range(19):
            for field in ("executedBytesSha256", "gitNormalizedSha256", "headGitNormalizedSha256", "gitBlobOid"):
                value = deepcopy(self.capture)
                value["producerSources"][number][field] = "f" * (40 if field == "gitBlobOid" else 64)
                if field == "gitNormalizedSha256":
                    value["producerSources"][number]["headGitNormalizedSha256"] = "f" * 64
                if number == 0 and field == "executedBytesSha256":
                    value["sources"][0]["sha256"] = "f" * 64
                with self.subTest(source=number, field=field):
                    self.refuse(lambda: self.inspect(capture=value))
        altered = replace(self.sources[-1], executed_bytes=b"ISOLATED substituted\r\n", blob_bytes=b"ISOLATED substituted\n")
        self.refuse(lambda: self.inspect(sources=(*self.sources[:-1], altered)), "source-byte-identity-mismatch")

    def test_every_receipt_requires_tracked_unmodified_matching_head(self):
        for number in range(19):
            for field, bad in (("trackedAtHead", False), ("modified", True), ("matchesHead", False)):
                value = deepcopy(self.capture)
                value["producerSources"][number][field] = bad
                self.refuse(lambda: self.inspect(capture=value))

    def test_dirty_blocked_recheck_and_asserted_pass_states_refuse(self):
        for update in ({"sourceDisposition": "dirty-development"}, {"worktreeDirty": True},
                       {"finalSourceRecheck": "changed-or-unavailable"}, {"producerStatus": "blocked"},
                       {"productionGatePassed": True}, {"gateStatus": "passed"},
                       {"independentDisposition": "accepted"}, {"failureCount": 1}, {"skipCount": 1}):
            self.refuse(lambda: self.inspect(capture=dict(self.capture, **update)))
        value = dict(self.capture, sourceDisposition="dirty-development")
        # The unchanged I1 reader admits dirty captures for historical inspection;
        # this byte comparison deliberately requires the stronger clean boundary.
        self.refuse(lambda: self.inspect(capture=value), "observed-clean-unchanged-capture-required")

    def test_valid_neutral_blocked_and_recheck_drift_captures_remain_ineligible(self):
        value = deepcopy(self.capture)
        value.update(producerStatus="blocked", context=None, blockers=["producer-execution-failed"],
                     resultCount=0, failureCount=1)
        value["producer"]["exitCode"] = 1
        value["observations"] = {key: [] for key in value["observations"] if key != "alphaOptIn"}
        value["observations"]["alphaOptIn"] = {"componentIsAlpha": None, "allowAlphaComponent": None}
        value["commands"] = value["commands"][:1]
        value["sources"] = value["sources"][:1]
        for recheck, blockers in (("unchanged", ["producer-execution-failed"]),
                                  ("changed-or-unavailable", ["producer-source-changed"])):
            value.update(finalSourceRecheck=recheck, blockers=blockers)
            parse_capture(snapshot(value))
            self.refuse(lambda: self.inspect(capture=value), "observed-clean-unchanged-capture-required")

    def test_registry_membership_helper_roles_and_supported_capture_schema_are_exact(self):
        for updates in ({"inputPaths": INPUTS[:-1]}, {"inputPaths": sorted(INPUTS + ["fixtures/extra.yaml"])},
                        {"helperPaths": HELPERS[:-1]},
                        {"helperPaths": sorted(HELPERS + [INPUTS[0]]), "inputPaths": INPUTS[1:]},
                        {"captureSchema": "fixture.capture/v999"}):
            self.refuse(lambda: self.inspect(entry=dict(self.entry, **updates)))
        # Future schema inventory is permitted, but comparison cannot dispatch it.
        inventory = inspect_registry(snapshot([dict(self.entry, captureSchema="fixture.capture/v999")]))
        self.assertEqual(inventory.entries[0].capture_schema, "fixture.capture/v999")
        self.refuse(lambda: self.inspect(capture=dict(self.capture, schemaVersion="fixture.capture/v999")))

    def test_transform_and_nonregular_mode_metadata_refuse(self):
        for updates in ({"normalization": "none"}, {"normalization": None},
                        {"clean_filter": "fixture-filter"}, {"clean_filter": False},
                        {"working_tree_encoding": "UTF-8"}, {"working_tree_encoding": "UTF-16"},
                        {"ident": True}, {"ident": 0}, {"git_mode": "120000"},
                        {"git_mode": "160000"}, {"git_mode": "040000"}, {"git_mode": 100644}):
            self.refuse(lambda: replace(self.sources[0], **updates))
        sources = (replace(self.sources[0], git_mode="100755"), *self.sources[1:])
        self.inspect(sources=sources)

    def test_bytes_are_immutable_bounded_utf8_and_exactly_normalized(self):
        for field in ("executed_bytes", "blob_bytes"):
            for raw in (bytearray(b"fixture"), memoryview(b"fixture"), "fixture", None, b"x" * (MAX_ARTIFACT_BYTES + 1)):
                self.refuse(lambda: replace(self.sources[0], **{field: raw}))
            sources = (replace(self.sources[0], **{field: b"\xffprivate-test-credential"}), *self.sources[1:])
            error = self.refuse(lambda: self.inspect(sources=sources), "invalid-source-utf8")
            self.assertIsNone(error.__context__)
            self.assertIsNone(error.__cause__)
        sources = (replace(self.sources[0], blob_bytes=self.sources[0].executed_bytes), *self.sources[1:])
        self.refuse(lambda: self.inspect(sources=sources), "source-normalized-bytes-mismatch")
        raw = b"ISOLATED" + b"x" * (MAX_ARTIFACT_BYTES - len(b"ISOLATED"))
        sources = (byte_pair(PRODUCER, raw, raw), *self.sources[1:])
        self.inspect(sources=sources, capture=fixture_capture(sources))

    def test_git_sha1_provider_failure_is_a_content_free_checksum_refusal(self):
        capture = snapshot(self.capture)
        def unavailable(raw, *, usedforsecurity):
            self.assertFalse(usedforsecurity)
            raise ValueError("private-test-credential " + raw.decode("utf-8"))
        with patch("access_telemetry_c1_producer_bindings.hashlib.sha1", side_effect=unavailable) as provider:
            error = self.refuse(lambda: inspect_sources(self.registry, capture, self.sources, COMMIT),
                                "git-blob-checksum-unavailable")
            self.assertEqual(provider.call_count, 1)
            self.assertIsNone(error.__context__)
            self.assertIsNone(error.__cause__)

    def test_same_bytes_deduplicate_across_forms_sources_and_json(self):
        # Both byte forms and all nineteen paths retain identical JSON bytes.
        # This does not make the fixture JSON a registration or real source.
        raw = self.registry.snapshot.raw.replace(b"\r\n", b"\n")
        registry = inspect_registry(JsonSnapshot(raw))
        sources = tuple(byte_pair(path, raw, raw) for path in PATHS)
        result = inspect_sources(registry, snapshot(fixture_capture(sources)), sources, COMMIT)
        self.assertEqual(result.unique_retained_byte_count, len(result.capture.raw) + len(raw))

    def test_exact_32_mib_shared_budget_includes_json_and_both_forms(self):
        sources = []
        for number, path in enumerate(PATHS[:15]):
            marker = ("ISOLATED%02d" % number).encode()
            raw = marker + b"x" * (MAX_ARTIFACT_BYTES - len(marker) - 2) + b"\r\n"
            sources.append(byte_pair(path, raw))
        sources.extend(byte_pair(path, b"", b"") for path in PATHS[15:])
        capture_size = len(snapshot(fixture_capture(sources)).raw)
        consumed = len(self.registry.snapshot.raw) + capture_size + sum(
            len(source.executed_bytes) + len(source.blob_bytes) for source in sources[:15])
        remaining = MAX_AGGREGATE_BYTES - consumed
        tail = b"y" if remaining % 2 == 0 else b""
        pair_length = (remaining - len(tail) + 1) // 2
        raw = b"ISOLATED-BOUNDARY" + b"x" * (pair_length - len(b"ISOLATED-BOUNDARY") - 2) + b"\r\n"
        sources[15] = byte_pair(PATHS[15], raw)
        sources[16] = byte_pair(PATHS[16], tail, tail)
        sources = tuple(sources)
        result = self.inspect(sources=sources, capture=fixture_capture(sources))
        self.assertEqual(result.unique_retained_byte_count, MAX_AGGREGATE_BYTES)
        over = list(sources)
        over[16] = byte_pair(PATHS[16], tail + b"z", tail + b"z")
        over_capture = snapshot(fixture_capture(over))
        # Budget admission must refuse before even the registry's J1 hashing.
        with patch("access_telemetry_c1_producer_bindings.hashlib.sha256", side_effect=AssertionError("digest called")):
            self.refuse(lambda: inspect_sources(self.registry, over_capture, tuple(over), COMMIT),
                        "aggregate-byte-budget-exceeded")

    def test_outputs_and_nested_containers_are_immutable(self):
        result = self.inspect()
        for instance, field in ((result, "source_commit"), (result.entry, "gate"),
                                (result.sources[0], "path"), (self.sources[0], "executed_bytes"),
                                (result.registry, "entries")):
            with self.assertRaises(FrozenInstanceError):
                setattr(instance, field, "changed")
        for values in (result.sources, result.registry.entries, result.entry.helper_paths, result.entry.input_paths):
            with self.assertRaises(TypeError):
                values[0] = "changed"
        with self.assertRaises(TypeError):
            result.capture.value["productionGatePassed"] = True
        with self.assertRaises(TypeError):
            result.registry.snapshot.value[0]["gate"] = "C1.1"
        self.assertNotIn("ISOLATED OFFLINE FIXTURE", repr(self.sources[0]))
        self.assertFalse(hasattr(result, "passed"))

    def test_manually_constructed_registry_wrappers_cannot_establish_success(self):
        for raw in (None, b"[]", {}, object()):
            wrapper = RegistryInspection(raw, self.registry.entries, self.registry.registry_sha256)
            self.refuse(lambda: inspect_sources(wrapper, snapshot(self.capture), self.sources, COMMIT),
                        "registry-snapshot-required")
        wrapper = RegistryInspection(self.registry.snapshot, (), "f" * 64)
        result = inspect_sources(wrapper, snapshot(self.capture), self.sources, COMMIT)
        self.assertEqual(result.registry.registry_sha256, self.registry.registry_sha256)
        self.assertEqual(result.entry.gate, "C1.15")
        for registry, capture in ((None, snapshot(self.capture)), (self.registry, self.capture)):
            self.refuse(lambda: inspect_sources(registry, capture, self.sources, COMMIT))

    def test_deployed_lookup_always_refuses_for_empty_fixture_and_opaque_inputs(self):
        for inventory in (None, inspect_registry(JsonSnapshot(b"[]")), self.registry, object()):
            self.refuse(lambda: lookup_deployed_binding("C1.15", "PG-ONPREM-2", inventory),
                        "deployed-producer-binding-unavailable")
        self.refuse(lambda: lookup_deployed_binding(object(), object(), object()),
                    "deployed-producer-binding-unavailable")

    def test_every_api_has_zero_filesystem_process_network_and_ambient_calls(self):
        capture = snapshot(self.capture)
        registry_raw = self.registry.snapshot
        bad_raw = snapshot([dict(self.entry, passed=True)])
        with self.denial_barriers():
            inspect_registry(registry_raw)
            inspect_sources(self.registry, capture, self.sources, COMMIT)
            byte_pair("fixtures/isolated.py")
            self.refuse(lambda: inspect_registry(bad_raw))
            self.refuse(lambda: inspect_sources(self.registry, capture, self.sources[:-1], COMMIT))
            self.refuse(lambda: inspect_sources(RegistryInspection(None, (), "f" * 64), capture, self.sources, COMMIT))
            self.refuse(lambda: lookup_deployed_binding("C1.15", "PG-ONPREM-2", self.registry))


if __name__ == "__main__":
    unittest.main()
