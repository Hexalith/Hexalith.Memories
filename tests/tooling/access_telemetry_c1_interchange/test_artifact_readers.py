"""Independent structural fixtures; successful inspection grants no authority."""

from __future__ import annotations

import base64
from copy import deepcopy
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import unittest
from unittest.mock import patch
import urllib.request


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))

from access_telemetry_c1_interchange import (  # noqa: E402
    InterchangeFormatError, JsonSnapshot, MAX_ARTIFACT_BYTES, MAX_INTEGER,
    parse_artifact, parse_capture, parse_disposition, parse_accepted_gate,
    parse_manifest, parse_bundle_approval, parse_predecessor, powershell_json_bytes,
    j1_bytes, parse_ref,
)


PREFIX = "hexalith.access-telemetry.c1."
START = "2026-10-08T10:00:00.0000001+00:00"
FINISH = "2026-10-08T10:00:01.0000001+00:00"
SCOPE = {
    "profileId": "PG-ONPREM-2",
    "profileSha256": "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe",
    "workloadSha256": "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f",
    "qualificationSessionId": "pg2-c1-15-fixture01",
    "targetSha256": "1" * 64,
}
SOURCE_PATHS = [
    "tools/verify-access-telemetry-c1.ps1",
    "tools/access-telemetry-c1-profile.ps1",
    "tools/access-telemetry-c1-component-backend.ps1",
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
METADATA_PROBE = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'


def digest(data):
    return hashlib.sha256(data if type(data) is bytes else data.encode("utf-8")).hexdigest()


def fixture_ps_bytes(value):
    # Independent fixture encoding; production encoder is never used to make
    # an admitted artifact. Actual PowerShell is checked against literal vectors.
    text = json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    return text.replace("\x85", r"\u0085").replace("\u2028", r"\u2028").replace("\u2029", r"\u2029").encode("utf-8")


def ref(name):
    return {"path": "fixtures/" + name + ".json", "sha256": digest(name), "byteLength": 42}


def receipt(arguments, *, purpose=None, empty=False):
    result = {
        "executable": "git" if purpose is None else "kubectl",
        "arguments": arguments, "argumentsSha256": digest(fixture_ps_bytes(arguments)),
        "startedAtUtc": START, "finishedAtUtc": FINISH, "exitCode": 0,
        "stdoutSha256": digest("" if empty else "independent observed output\n"),
        "stderrSha256": digest(""),
        "streamSafety": "hash-only-source-provenance" if purpose is None else "validated",
        "resultCount": 1, "failureCount": 0, "skipCount": 0,
    }
    if purpose is not None:
        result.update(purpose=purpose, sha256=digest("kubectl " + "\x1f".join(arguments)))
    return result


def capture_fixture():
    target = {"context": "jpiquot@local", "namespace": "hexalith-memories",
              "selector": "app.kubernetes.io/name=memories-access-telemetry",
              "appId": "memories-access-telemetry", "actorType": "AccessTelemetryLifecycleActor"}
    pods = []
    for number, image_hash in enumerate((
        "b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8",
        "edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b",
    ), 1):
        pods.append({
            "pod": "lifecycle-" + str(number), "podUid": "fixture-uid-" + str(number),
            "runtimeVersion": "1.18.1", "sidecarImageId": "containerd://sha256:" + image_hash,
            "sidecarImageDigest": "sha256:" + image_hash, "appId": target["appId"],
            "schedulerConnectedAddresses": ["scheduler-0:50006"],
            "actorTypes": ["AccessTelemetryLifecycleActor"], "enabledFeatures": ["Actor.Reentrancy"],
            "alphaOptIn": {"componentIsAlpha": False, "allowAlphaComponent": False},
        })
    observations = {"pods": pods}
    for plural, singular in (("runtimeVersions", "runtimeVersion"), ("sidecarImageIds", "sidecarImageId"),
                             ("sidecarImageDigests", "sidecarImageDigest"), ("appIds", "appId")):
        observations[plural] = sorted({pod[singular] for pod in pods})
    for name in ("schedulerConnectedAddresses", "actorTypes", "enabledFeatures", "alphaOptIn"):
        observations[name] = deepcopy(pods[0][name])
    commands = [receipt(["config", "current-context"], purpose="current-context"),
                receipt(["get", "pods"], purpose="lifecycle-pods")]
    for pod in pods:
        for purpose, arguments in (
            ("daprd-version", ["exec", pod["pod"], "--", "/daprd", "--version"]),
            ("metadata", ["exec", pod["pod"], "--", "/bin/sh", "-ec", METADATA_PROBE]),
            ("alpha-opt-in", ["exec", pod["pod"], "--", "/bin/sh", "-ec", "printf false"]),
        ):
            commands.append(receipt(arguments, purpose=purpose + ":" + pod["pod"]))
    commands.append(receipt(["get", "pods"], purpose="lifecycle-pods-recheck"))
    # Initial local provenance, serial target children, then final provenance.
    # Equality at adjacent receipt boundaries is legal; overlapping calls are not.
    for number, command in enumerate(commands, 1):
        command["startedAtUtc"] = f"2026-10-08T10:00:00.{number}000001+00:00"
        command["finishedAtUtc"] = f"2026-10-08T10:00:00.{number}500001+00:00"
    source_commands = [receipt(["rev-parse", "--verify", "HEAD"], empty=True),
                       receipt(["status", "--porcelain=v1", "--untracked-files=all"], empty=True)]
    source_commands[0]["finishedAtUtc"] = "2026-10-08T10:00:00.0500001+00:00"
    source_commands[1]["startedAtUtc"] = commands[-1]["finishedAtUtc"]
    producer_sources = [{
        "source": path, "gitNormalizedSha256": digest("normalized " + path),
        "executedBytesSha256": digest("executed CRLF " + path), "gitBlobOid": "a" * 40,
        "headGitNormalizedSha256": digest("normalized " + path),
        "trackedAtHead": True, "modified": False, "matchesHead": True,
    } for path in SOURCE_PATHS]
    sources = [{"source": SOURCE_PATHS[0], "sha256": producer_sources[0]["executedBytesSha256"]}]
    for command in commands:
        purpose = command["purpose"]
        if purpose.startswith("metadata:"):
            pod = next(pod for pod in pods if purpose.endswith(":" + pod["pod"]))
            projection = {"id": pod["appId"], "runtimeVersion": pod["runtimeVersion"],
                          "schedulerConnectedAddresses": pod["schedulerConnectedAddresses"],
                          "actorTypes": pod["actorTypes"], "enabledFeatures": pod["enabledFeatures"]}
            sources.append({"source": "kubectl:" + purpose + ":allowlisted", "sha256": digest(fixture_ps_bytes(projection))})
        elif purpose.startswith("lifecycle-pods"):
            sources.append({"source": "kubectl:" + purpose + ":identity", "sha256": command["stdoutSha256"]})
        else:
            for stream in ("stdout", "stderr"):
                sources.append({"source": "kubectl:" + purpose + ":" + stream, "sha256": command[stream + "Sha256"]})
    arguments = ["-Gate", "C1.15", "-ProfileId", "PG-ONPREM-2", "-QualificationSessionId",
                 SCOPE["qualificationSessionId"], "-EvidenceDirectory", "/fixture/archive/café雪", "-CommandTimeoutSeconds", "30"]
    return {
        "schemaVersion": PREFIX + "evidence/v2", "gate": "C1.15", **SCOPE,
        "capturedAtUtc": START, "context": target["context"], "namespace": target["namespace"],
        "targetSelector": target["selector"], "producerStatus": "observed", "gateStatus": "not-evaluated",
        "productionGatePassed": False, "productionLifecycleWrites": "not-evaluated",
        "observations": observations, "blockers": [], "sources": sources, "commands": commands,
        "profileIdentity": "postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2",
        "workloadId": "adr-27.1-two-writer-500eps", "target": target,
        "targetSha256": digest(fixture_ps_bytes(target)),
        "producer": {"path": SOURCE_PATHS[0], "identity": "repository-collector",
                     "identityAuthentication": "not-evaluated", "arguments": arguments,
                     "argumentsSha256": digest(fixture_ps_bytes(arguments)),
                     "startedAtUtc": START, "finishedAtUtc": FINISH, "exitCode": 0},
        "sourceCommands": source_commands,
        "sourceCommit": "b" * 40, "sourceDisposition": "clean", "worktreeDirty": False,
        "producerSources": producer_sources, "finalSourceRecheck": "unchanged",
        "resultCount": 2, "failureCount": 0, "skipCount": 0, "independentDisposition": "pending",
    }


def blocked_fixture():
    value = capture_fixture()
    value.update(producerStatus="blocked", context=None, blockers=["producer-execution-failed"], resultCount=0, failureCount=1)
    value["producer"]["exitCode"] = 1
    value["observations"] = {name: [] for name in value["observations"] if name != "alphaOptIn"}
    value["observations"]["alphaOptIn"] = {"componentIsAlpha": None, "allowAlphaComponent": None}
    value["commands"] = value["commands"][:1]
    command = value["commands"][0]
    command.update(exitCode=None, stdoutSha256=None, stderrSha256=None, streamSafety="not-validated", resultCount=0, failureCount=1)
    value["sources"] = value["sources"][:1]
    return value


def decision_fields():
    return {"reviewerPrincipal": "github:user:6775094", "reviewerRole": "platform-operations",
            "decision": "accepted", "reasons": ["Reviewed the retained fixture evidence and its exact bytes."],
            "decidedAtUtc": "2026-10-08T10:00:02.000Z", "expiresAtUtc": "2026-10-08T11:00:02.000Z",
            "authorityReceipt": {"uri": "https://fixture.invalid/reviews/decision01?revision=3",
                                 "sha256": "c" * 64, "issuer": "isolated-fixture-issuer", "decisionId": "decision01"}}


def fixtures():
    disposition = {"schemaVersion": PREFIX + "disposition/v1", "capture": ref("capture"), "gate": "C1.15",
                   **SCOPE, "producerPrincipal": "github:user:42", "independence": True, **decision_fields()}
    gate = {"schemaVersion": PREFIX + "accepted-gate/v1", "gate": "C1.15", "status": "passed", **SCOPE,
            "registryEntrySha256": "d" * 64, "capture": ref("capture"), "disposition": ref("disposition"),
            "sourceCommit": "b" * 40, "producerPath": SOURCE_PATHS[0],
            "startedAtUtc": "2026-10-08T10:00:00.000Z", "finishedAtUtc": "2026-10-08T10:00:01.000Z",
            "resultCount": 2, "failureCount": 0, "skipCount": 0,
            "cleanup": {"required": False, "receipts": [], "finalState": "read-only-no-owned-mutation"}}
    manifest = {"schemaVersion": PREFIX + "manifest/v1", "checkpoint": "C1", **SCOPE,
                "registrySha256": "e" * 64, "policySha256": "f" * 64, "sessionAuthority": ref("session"),
                "gates": [{"gate": "C1." + str(i), "artifact": ref("gate-" + str(i))} for i in range(1, 26)]}
    approval = {"schemaVersion": PREFIX + "bundle-approval/v1", "manifest": ref("manifest"), **SCOPE, **decision_fields()}
    predecessor = {"schemaVersion": PREFIX + "predecessor/v2", "manifest": ref("manifest"),
                   "approvals": [ref("operations"), ref("security")], "status": "passed",
                   "productionLifecycleWrites": "disabled", "qualificationAuthorized": True}
    return [(parse_capture, capture_fixture()), (parse_disposition, disposition), (parse_accepted_gate, gate),
            (parse_manifest, manifest), (parse_bundle_approval, approval), (parse_predecessor, predecessor)]


def snapshot(value):
    return JsonSnapshot((json.dumps(value, ensure_ascii=False, indent=2) + "\r\n").encode("utf-8"))


def walk(value, path=()):
    yield path, value
    if type(value) is dict:
        for key, child in value.items():
            yield from walk(child, path + (key,))
    elif type(value) is list:
        for index, child in enumerate(value):
            yield from walk(child, path + (index,))


def at(value, path):
    for key in path:
        value = value[key]
    return value


class ArtifactReaderTests(unittest.TestCase):
    def refuse(self, value, reader=parse_artifact):
        with self.assertRaises(InterchangeFormatError) as caught:
            reader(snapshot(value))
        self.assertLess(len(str(caught.exception)), 80)
        self.assertNotIn("private-test-credential", str(caught.exception))
        self.assertTrue(caught.exception.__suppress_context__)

    def test_all_six_readers_return_the_identical_original_immutable_snapshot(self):
        for reader, value in fixtures():
            with self.subTest(reader=reader.__name__):
                original = snapshot(value)
                self.assertIs(reader(original), original)
                self.assertIs(parse_artifact(original), original)
                self.assertEqual(original.sha256, digest(original.raw))
                self.assertTrue(original.raw.endswith(b"\r\n"))
                with self.assertRaises(TypeError):
                    original.value["schemaVersion"] = "changed"
                for _, child in walk(value):
                    if type(child) is dict and child is not value:
                        break
                path = next(path for path, child in walk(value) if path and type(child) is dict)
                with self.assertRaises(TypeError):
                    at(original.value, path)["changed"] = True

    def test_every_nested_object_refuses_missing_unknown_and_duplicate_fields(self):
        for reader, value in fixtures():
            for path, child in walk(value):
                if type(child) is not dict:
                    continue
                for field in child:
                    with self.subTest(reader=reader.__name__, path=path, missing=field):
                        changed = deepcopy(value)
                        del at(changed, path)[field]
                        self.refuse(changed, reader)
                with self.subTest(reader=reader.__name__, path=path, mutation="unknown"):
                    changed = deepcopy(value)
                    at(changed, path)["Bearer private-test-credential"] = True
                    self.refuse(changed, reader)
                # Build an actual duplicate wire key at this exact nested object,
                # including a Unicode-escaped equivalent of the first key.
                selected = child
                def encode(item):
                    if type(item) is dict:
                        entries = [json.dumps(k) + ":" + encode(v) for k, v in item.items()]
                        if item is selected:
                            key = next(iter(item))
                            escaped = '"\\u' + format(ord(key[0]), "04x") + key[1:] + '"'
                            entries.append(escaped + ":null")
                        return "{" + ",".join(entries) + "}"
                    if type(item) is list:
                        return "[" + ",".join(encode(v) for v in item) + "]"
                    return json.dumps(item)
                with self.subTest(reader=reader.__name__, path=path, mutation="duplicate"):
                    with self.assertRaisesRegex(InterchangeFormatError, "duplicate-json-field"):
                        reader(JsonSnapshot(encode(value).encode()))

    def test_every_field_refuses_wrong_scalar_or_collection_type(self):
        for reader, value in fixtures():
            for path, child in walk(value):
                if not path:
                    continue
                changed = deepcopy(value)
                replacement = [] if type(child) is dict else {} if type(child) is list else (
                    0 if type(child) is bool else True if type(child) is int else []
                )
                at(changed, path[:-1])[path[-1]] = replacement
                with self.subTest(reader=reader.__name__, path=path):
                    self.refuse(changed, reader)

    def test_readers_require_snapshots_and_exact_roots_and_versions(self):
        readers = [reader for reader, _ in fixtures()]
        for reader in readers + [parse_artifact]:
            for raw in ({}, b"{}", None, [], "snapshot"):
                with self.subTest(reader=reader.__name__, input_type=type(raw).__name__):
                    with self.assertRaises(InterchangeFormatError):
                        reader(raw)
            for root in ([], None, True, 42, "text"):
                self.refuse(root, reader)
        for reader, value in fixtures():
            for schema in (value["schemaVersion"].upper(), value["schemaVersion"] + " ", value["schemaVersion"][:-1] + "9", "legacy", 1):
                self.refuse({**value, "schemaVersion": schema}, reader)
        for reader, value in fixtures():
            for other_reader in readers:
                if other_reader is not reader:
                    self.refuse(value, other_reader)

    def test_malformed_encoding_json_and_common_bounds_refuse_without_partial_results(self):
        for raw in (b"\xef\xbb\xbf{}", b'"\xff"', b"{}{}", b"/*comment*/{}", b'{"x":1,}',
                    b'{"x":NaN}', b'{"x":1.0}', b'{"x":9223372036854775808}', b'"\\ud800"',
                    b"[" * 15 + b"0" + b"]" * 15, b'"' + b"x" * 4097 + b'"',
                    b"{}" + b" " * MAX_ARTIFACT_BYTES):
            with self.subTest(raw_length=len(raw)):
                with self.assertRaises(InterchangeFormatError):
                    parse_artifact(JsonSnapshot(raw))

    def test_dirty_and_blocked_inspection_retains_neutral_fields(self):
        dirty = capture_fixture()
        dirty.update(worktreeDirty=True, sourceDisposition="dirty-development")
        dirty["producerSources"][0]["modified"] = True
        self.assertIs(parse_capture(original := snapshot(dirty)), original)
        # Dirty-development is sticky even when the final tree/source facts clear.
        dirty["worktreeDirty"] = False
        dirty["producerSources"][0]["modified"] = False
        parse_capture(snapshot(dirty))
        for context in (None, "different-context", "jpiquot@local"):
            blocked = blocked_fixture()
            blocked["context"] = context
            self.assertIs(parse_capture(original := snapshot(blocked)), original)
            self.assertEqual(original.value["gateStatus"], "not-evaluated")
            self.assertFalse(original.value["productionGatePassed"])
            self.assertEqual(original.value["producer"]["identityAuthentication"], "not-evaluated")

    def test_capture_count_status_and_observation_contradictions_refuse(self):
        mutations = [
            (("resultCount",), 0), (("resultCount",), 3), (("failureCount",), 1), (("skipCount",), True),
            (("blockers",), ["producer-execution-failed"]), (("producerStatus",), "passed"),
            (("producer", "exitCode"), 1), (("gateStatus",), "passed"), (("productionGatePassed",), True),
            (("independentDisposition",), "accepted"), (("context",), None),
            (("namespace",), "other"), (("targetSelector",), "other"),
            (("observations", "runtimeVersions"), ["9.0.0"]),
            (("observations", "sidecarImageDigests"), []),
            (("observations", "alphaOptIn", "componentIsAlpha"), True),
            (("observations", "pods", 0, "sidecarImageDigest"), "sha256:" + "0" * 64),
            (("observations", "pods", 0, "schedulerConnectedAddresses"), ["scheduler:65536"]),
            (("observations", "pods", 1, "podUid"), "fixture-uid-1"),
            (("observations", "pods", 1, "pod"), "lifecycle-1"),
            (("observations", "pods", 1, "actorTypes"), ["DifferentActor"]),
            (("observations", "pods", 0, "enabledFeatures"), ["duplicate", "duplicate"]),
        ]
        for path, replacement in mutations:
            changed = capture_fixture()
            at(changed, path[:-1])[path[-1]] = replacement
            with self.subTest(path=path):
                self.refuse(changed, parse_capture)
        blocked = blocked_fixture()
        for path, replacement in ((("resultCount",), 1), (("blockers",), []),
                                  (("observations", "pods"), capture_fixture()["observations"]["pods"]),
                                  (("observations", "alphaOptIn", "componentIsAlpha"), False)):
            changed = deepcopy(blocked)
            at(changed, path[:-1])[path[-1]] = replacement
            self.refuse(changed, parse_capture)

    def test_source_local_facts_clean_implications_and_schema_paths(self):
        for key, replacement in (("headGitNormalizedSha256", None), ("trackedAtHead", False),
                                 ("matchesHead", False), ("modified", True), ("gitBlobOid", "A" * 40),
                                 ("source", ".editorconfig")):
            changed = capture_fixture()
            changed["producerSources"][0][key] = replacement
            self.refuse(changed, parse_capture)
        for mutation in ("missing", "duplicate", "dirty", "hash"):
            changed = capture_fixture()
            if mutation == "missing": changed["producerSources"].pop()
            if mutation == "duplicate": changed["producerSources"][-1] = deepcopy(changed["producerSources"][0])
            if mutation == "dirty": changed["worktreeDirty"] = True
            if mutation == "hash": changed["sources"][0]["sha256"] = "0" * 64
            self.refuse(changed, parse_capture)
        dirty = capture_fixture()
        dirty["sourceDisposition"] = "dirty-development"
        source = dirty["producerSources"][0]
        source.update(trackedAtHead=False, headGitNormalizedSha256=None, matchesHead=False, modified=True)
        parse_capture(snapshot(dirty))
        source.update(trackedAtHead=True, headGitNormalizedSha256=source["gitNormalizedSha256"], matchesHead=True)
        parse_capture(snapshot(dirty))  # staged changes can still match HEAD bytes
        blocked = blocked_fixture()
        blocked.update(finalSourceRecheck="changed-or-unavailable", blockers=["producer-source-changed"])
        parse_capture(snapshot(blocked))
        blocked["blockers"] = ["producer-execution-failed"]
        self.refuse(blocked, parse_capture)

    def test_command_receipts_hashes_counts_streams_and_sources_are_consistent(self):
        for key, replacement in (("argumentsSha256", "0" * 64), ("sha256", "0" * 64),
                                 ("arguments", ["changed"]), ("stdoutSha256", None), ("stdoutSha256", digest("")),
                                 ("streamSafety", "not-validated"), ("exitCode", True), ("exitCode", 1),
                                 ("resultCount", 2), ("failureCount", 1), ("skipCount", 1), ("purpose", "arbitrary")):
            changed = capture_fixture()
            changed["commands"][0][key] = replacement
            self.refuse(changed, parse_capture)
        for name in ("sourceCommands", "commands"):
            changed = capture_fixture()
            changed[name] = []
            self.refuse(changed, parse_capture)
        for key in ("argumentsSha256", "stdoutSha256"):
            changed = capture_fixture()
            changed["sourceCommands"][0][key] = None
            self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["sources"][1]["sha256"] = "0" * 64
        self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["sources"].append(deepcopy(changed["sources"][1]))
        self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["commands"].append(deepcopy(changed["commands"][0]))
        self.refuse(changed, parse_capture)
        # A blocked target receipt can legitimately have exit zero but no result
        # (empty/whitespace output), or a missing exit after process failure.
        for exit_code in (0, None, 1):
            changed = blocked_fixture()
            command = changed["commands"][0]
            command.update(exitCode=exit_code)
            if exit_code == 0:
                command.update(streamSafety="validated", stdoutSha256=digest(""), stderrSha256=digest(""))
            parse_capture(snapshot(changed))

    def test_serial_child_ledgers_refuse_reverse_order_overlap_and_cross_ledger_overlap(self):
        for ledger in ("commands", "sourceCommands"):
            changed = capture_fixture()
            changed[ledger].reverse()
            self.refuse(changed, parse_capture)
            changed = capture_fixture()
            changed[ledger][1]["startedAtUtc"] = changed[ledger][0]["startedAtUtc"]
            self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["sourceCommands"][0]["finishedAtUtc"] = changed["commands"][0]["finishedAtUtc"]
        self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["sourceCommands"][1]["startedAtUtc"] = changed["commands"][-1]["startedAtUtc"]
        self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["sourceCommands"][0]["finishedAtUtc"] = changed["commands"][0]["startedAtUtc"]
        for earlier, later in zip(changed["commands"], changed["commands"][1:]):
            earlier["finishedAtUtc"] = later["startedAtUtc"]
        parse_capture(snapshot(changed))  # equality is admitted within/across ledgers
        changed["commands"][0]["finishedAtUtc"] = changed["commands"][0]["startedAtUtc"]
        parse_capture(snapshot(changed))  # zero-duration lexical intervals are valid

    def test_metadata_allowlisted_source_digest_is_directly_bound_to_projection(self):
        changed = capture_fixture()
        source = next(source for source in changed["sources"] if source["source"].startswith("kubectl:metadata:"))
        source["sha256"] = "0" * 64
        self.refuse(changed, parse_capture)

    def test_every_observed_pod_must_contain_the_declared_actor_even_with_recomputed_hashes(self):
        changed = capture_fixture()
        changed["observations"]["actorTypes"] = ["OtherActor"]
        for pod in changed["observations"]["pods"]:
            pod["actorTypes"] = ["OtherActor"]
            projection = {"id": pod["appId"], "runtimeVersion": pod["runtimeVersion"],
                          "schedulerConnectedAddresses": pod["schedulerConnectedAddresses"],
                          "actorTypes": pod["actorTypes"], "enabledFeatures": pod["enabledFeatures"]}
            source = next(source for source in changed["sources"]
                          if source["source"] == "kubectl:metadata:" + pod["pod"] + ":allowlisted")
            source["sha256"] = digest(fixture_ps_bytes(projection))
        self.refuse(changed, parse_capture)

    def test_hashed_process_arguments_preserve_empty_whitespace_and_refuse_nul(self):
        for ledger in ("commands", "sourceCommands"):
            for argument in ("", " ", "\t", "\r\n", " \t \n "):
                changed = capture_fixture()
                command = changed[ledger][0]
                command["arguments"].append(argument)
                command["argumentsSha256"] = digest(fixture_ps_bytes(command["arguments"]))
                if ledger == "commands":
                    command["sha256"] = digest("kubectl " + "\x1f".join(command["arguments"]))
                parse_capture(snapshot(changed))
            for argument in ("\x00", "prefix\x00suffix"):
                changed = capture_fixture()
                command = changed[ledger][0]
                command["arguments"].append(argument)
                command["argumentsSha256"] = digest(fixture_ps_bytes(command["arguments"]))
                if ledger == "commands":
                    command["sha256"] = digest("kubectl " + "\x1f".join(command["arguments"]))
                self.refuse(changed, parse_capture)

    def test_current_producer_ambiguous_kubectl_source_labels_refuse(self):
        value = capture_fixture()
        for source in value["sources"][1:3]:
            source["source"] = "kubectl:"
        self.refuse(value, parse_capture)

    def test_capture_timestamps_are_exact_round_trip_and_contained_to_seventh_digit(self):
        for timestamp in ("2026-10-08T10:00:00Z", "2026-10-08T10:00:00.000Z",
                          "2026-10-08T10:00:00.0000001Z", "2026-10-08T10:00:00.00000010+00:00",
                          "2026-02-30T10:00:00.0000001+00:00", "2026-10-08T24:00:00.0000001+00:00"):
            changed = capture_fixture()
            changed["producer"]["startedAtUtc"] = changed["capturedAtUtc"] = timestamp
            self.refuse(changed, parse_capture)
        for name in ("commands", "sourceCommands"):
            for key, timestamp in (("startedAtUtc", "2026-10-08T10:00:00.0000000+00:00"),
                                   ("finishedAtUtc", "2026-10-08T10:00:01.0000002+00:00"),
                                   ("finishedAtUtc", "2026-10-08T10:00:00.0000000+00:00")):
                changed = capture_fixture()
                changed[name][0][key] = timestamp
                self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["capturedAtUtc"] = FINISH
        self.refuse(changed, parse_capture)

    def test_disposition_precision_and_exact_order_have_no_clock_policy(self):
        reader, value = fixtures()[1]
        for first, last in (("2001-01-01T00:00:00Z", "2001-01-01T00:00:00.0000001+00:00"),
                            ("2001-01-01T00:00:00.1Z", "9999-12-31T23:59:59.9999999Z")):
            value.update(decidedAtUtc=first, expiresAtUtc=last)
            reader(snapshot(value))  # ancient and far-future dates remain inspection
        for first, last in (("2026-10-08T10:00:00.0000001Z", "2026-10-08T10:00:00.0000000Z"),
                            ("2026-10-08T10:00:00Z", "2026-10-08T10:00:00+00:00"),
                            ("2026-10-08T10:00:00-00:00", "2026-10-08T11:00:00Z"),
                            ("2026-10-08T10:00:00", "2026-10-08T11:00:00Z")):
            value.update(decidedAtUtc=first, expiresAtUtc=last)
            self.refuse(value, reader)

    def test_new_schema_time_precision_and_gate_success_counts(self):
        for reader, value in (fixtures()[2], fixtures()[4]):
            key = "startedAtUtc" if reader is parse_accepted_gate else "decidedAtUtc"
            for timestamp in ("2026-10-08T10:00:00Z", "2026-10-08T10:00:00.000+00:00", "2026-10-08T10:00:00.0000000Z"):
                self.refuse({**value, key: timestamp}, reader)
        value = fixtures()[2][1]
        parse_accepted_gate(snapshot({**value, "resultCount": MAX_INTEGER}))
        for key, replacement in (("resultCount", 0), ("resultCount", True), ("failureCount", 1), ("skipCount", False)):
            self.refuse({**value, key: replacement}, parse_accepted_gate)

    def test_disposition_independence_reasons_and_bounded_identities(self):
        value = fixtures()[1][1]
        for key, replacement in (("independence", False), ("producerPrincipal", value["reviewerPrincipal"]),
                                 ("reasons", []), ("reasons", [""]), ("reasons", [" \t "]), ("reasons", ["..."]),
                                 ("reasons", ["password=private-test-credential"]), ("decision", "passed"),
                                 ("reviewerPrincipal", "x" * 257), ("reviewerRole", "x" * 257)):
            self.refuse({**value, key: replacement}, parse_disposition)
        for decision in ("rejected", "needs-evidence"):
            parse_disposition(snapshot({**value, "decision": decision, "independence": False,
                                        "producerPrincipal": value["reviewerPrincipal"]}))
            self.refuse({**value, "decision": decision, "independence": True,
                         "producerPrincipal": value["reviewerPrincipal"]}, parse_disposition)
        parse_disposition(snapshot({**value, "reviewerPrincipal": "x" * 256, "reviewerRole": "x" * 256}))

    def test_receipt_uri_is_lexical_absolute_credential_free_with_no_origin_policy(self):
        value = fixtures()[1][1]
        for uri in ("https://fixture.invalid/a?revision=3", "https://fixture.invalid/caf%C3%A9", "urn:fixture:decision01", "custom+fixture:/decision/one",
                    "https://fixture.invalid:443/a", "https://[::1]:443/a", "https://fixture.invalid/a%5Bencoded%5D",
                    "https://fixture.invalid/a?x=plain+text#revision=3"):
            changed = deepcopy(value)
            changed["authorityReceipt"]["uri"] = uri
            parse_disposition(snapshot(changed))
        for uri in ("relative/path", "//fixture.invalid/a", "https://", "https:", "https://fixture.invalid/ bad",
                    "https://fixture.invalid/%00bad", "https://fixture.invalid/%zz", "https://fixture.invalid/%ff",
                    "https://user:private-test-credential@fixture.invalid/a", "https://user%3Apass%40fixture.invalid/a",
                    "https://fixture.invalid/a?access_token=private-test-credential", "https://fixture.invalid/a?%74oken=canary",
                    "https://fixture.invalid/a?api%5fkey=canary", "https://fixture.invalid/a?sig=canary",
                    "https://fixture.invalid/a?x=password%3Dprivate-test-credential", "https://[invalid/a",
                    "https://fixture.invalid/café", "https://fixture.invalid/a\\b", "https://fixture.invalid/a%5Cb",
                    "https://fixture.invalid/<unsafe>", "https://fixture.invalid:bad/a", "https://fixture.invalid:65536/a",
                    "https://:/a", "https://fixture.invalid/a[raw]", "https://fixture.invalid/a?x=[raw]",
                    "https://fixture.invalid/a#raw#fragment"):
            changed = deepcopy(value)
            changed["authorityReceipt"]["uri"] = uri
            self.refuse(changed, parse_disposition)

    def test_decoded_query_fragment_keys_and_values_refuse_credentials(self):
        value = fixtures()[1][1]
        for suffix in ("?x=Bearer+private-test-credential", "?x=Bearer%2Bprivate-test-credential",
                       "?x=Bearer%20private-test-credential", "#signature=private-test-credential",
                       "#sig=private-test-credential", "#%73ig=private-test-credential",
                       "#x=Bearer+private-test-credential", "#x=password%3Dprivate-test-credential",
                       "?safe=3;signature=private-test-credential"):
            changed = deepcopy(value)
            changed["authorityReceipt"]["uri"] = "https://fixture.invalid/a" + suffix
            with self.subTest(suffix=suffix):
                self.refuse(changed, parse_disposition)

    def test_decoded_quoted_and_named_json_credentials_refuse_in_reasons(self):
        value = fixtures()[1][1]
        for content in ('{"authorization":"private-test-credential"}',
                        '{"pass\\u0077ord":"private-test-credential"}',
                        'Review detail: "token": "private-test-credential"',
                        '{"name":"dapr-api-token","value":"private-test-credential"}',
                        '{"value":"private-test-credential","name":"authorization"}',
                        '[{"name":"client_secret","value":"private-test-credential"}]',
                        '{"token":"private-test-credential","token":""}',
                        '{"password":["private-test-credential"]}',
                        '{"note":"{\\"token\\":\\"private-test-credential\\"}"}'):
            with self.subTest(content=content):
                self.refuse({**value, "reasons": [content]}, parse_disposition)
        for content in ('{"token":""}', '{"authorization":null}', '{"password":[]}',
                        '{"name":"token","value":""}', '{"name":"token","value":null}'):
            parse_disposition(snapshot({**value, "reasons": [content]}))
        parse_capture(snapshot(capture_fixture()))  # exact metadata probe remains permitted

    def test_artifact_reference_and_producer_paths_apply_additional_content_guards(self):
        for reader, value in fixtures()[1:]:
            for path, child in walk(value):
                if type(child) is not dict or set(child) != {"path", "sha256", "byteLength"}:
                    continue
                for content in ("token=private-test-credential", "SECRET_CANARY", "hvs.abcdefgh"):
                    changed = deepcopy(value)
                    at(changed, path)["path"] = "fixtures/" + content + ".json"
                    # The extra guards belong to artifact readers; generic wire
                    # Ref behavior stays exactly as it was before this slice.
                    parse_ref(at(changed, path))
                    self.refuse(changed, reader)
        value = fixtures()[2][1]
        for content in ("token=private-test-credential", "SECRET_CANARY", "hvs.abcdefgh"):
            self.refuse({**value, "producerPath": "tools/" + content + ".ps1"}, parse_accepted_gate)
        value["cleanup"] = {"required": True, "receipts": [ref("cleanup")], "finalState": "restored-approved-baseline"}
        value["cleanup"]["receipts"][0]["path"] = "fixtures/SECRET_CANARY.json"
        self.refuse(value, parse_accepted_gate)

    def test_hash_commit_session_and_decoded_content_are_strict(self):
        for reader, value in fixtures()[:5]:
            for key in ("profileSha256", "workloadSha256", "targetSha256"):
                for replacement in ("A" * 64, "0" * 63, "sha256:" + "0" * 64, "0" * 64 + "\n"):
                    self.refuse({**value, key: replacement}, reader)
            for session in ("", "x" * 129, "café", "session token", "token-session", "password-session", "credential01"):
                self.refuse({**value, "qualificationSessionId": session}, reader)
        for key in ("sourceCommit",):
            value = capture_fixture()
            for commit in ("B" * 40, "b" * 39, "b" * 40 + "\n"):
                self.refuse({**value, key: commit}, parse_capture)
        for text in ("Bearer private-test-credential", "authorization=private-test-credential", "SECRET_CANARY",
                     "-----BEGIN RSA PRIVATE KEY-----", "postgresql://user:private-test-credential@host/db"):
            changed = capture_fixture()
            changed["producer"]["arguments"] = [text]
            changed["producer"]["argumentsSha256"] = digest(fixture_ps_bytes([text]))
            self.refuse(changed, parse_capture)
        for text in (METADATA_PROBE + " ", METADATA_PROBE.replace("DAPR_API_TOKEN", "private-test-credential"),
                     "dapr-api-token: private-test-credential"):
            changed = capture_fixture()
            changed["commands"][0] = receipt([text], purpose="current-context")
            self.refuse(changed, parse_capture)
        # Encoded secret names are inspected after JSON unescaping.
        value = fixtures()[1][1]
        raw = json.dumps({**value, "reasons": ["password=private-test-credential"]}).replace("password", r"pass\u0077ord").encode()
        with self.assertRaises(InterchangeFormatError) as caught:
            parse_disposition(JsonSnapshot(raw))
        self.assertNotIn("private-test-credential", str(caught.exception))

    def test_target_and_argument_hashes_preserve_order_instead_of_j1(self):
        value = capture_fixture()
        value["target"] = dict(reversed(list(value["target"].items())))
        parse_capture(snapshot(value))  # documented order, not incoming field order
        value["targetSha256"] = digest(j1_bytes(value["target"]))
        self.refuse(value, parse_capture)
        value = capture_fixture()
        value["producer"]["arguments"][-1] = "31"
        self.refuse(value, parse_capture)

    def test_producer_effective_argument_receipts_match_repeated_packet_facts(self):
        for position, replacement in ((0, "-Other"), (1, "C1.16"), (3, "PG-ONPREM-1"),
                                      (5, "different-session"), (7, ""), (8, "-DifferentTimeout"),
                                      (9, "0"), (9, "301"), (9, "030"), (9, "30.0")):
            changed = capture_fixture()
            changed["producer"]["arguments"][position] = replacement
            changed["producer"]["argumentsSha256"] = digest(fixture_ps_bytes(changed["producer"]["arguments"]))
            with self.subTest(position=position, replacement=replacement):
                self.refuse(changed, parse_capture)
        changed = capture_fixture()
        changed["producer"]["arguments"].pop()
        changed["producer"]["argumentsSha256"] = digest(fixture_ps_bytes(changed["producer"]["arguments"]))
        self.refuse(changed, parse_capture)
        for timeout in ("1", "30", "300"):
            changed = capture_fixture()
            changed["producer"]["arguments"][-1] = timeout
            changed["producer"]["argumentsSha256"] = digest(fixture_ps_bytes(changed["producer"]["arguments"]))
            parse_capture(snapshot(changed))

    def test_gate_cleanup_tuples_and_distinct_references(self):
        value = fixtures()[2][1]
        cleanup = {"required": True, "receipts": [ref("cleanup")], "finalState": "restored-approved-baseline"}
        parse_accepted_gate(snapshot({**value, "cleanup": cleanup}))
        for changed in ({**cleanup, "receipts": []}, {**cleanup, "required": False},
                        {**cleanup, "finalState": "read-only-no-owned-mutation"},
                        {**cleanup, "receipts": [ref("cleanup"), ref("cleanup")]}):
            self.refuse({**value, "cleanup": changed}, parse_accepted_gate)
        for field in ("path", "sha256"):
            changed = deepcopy(value)
            changed["disposition"][field] = changed["capture"][field]
            self.refuse(changed, parse_accepted_gate)

    def test_manifest_inventory_is_numeric_exact_and_distinct(self):
        value = fixtures()[3][1]
        for gates in (value["gates"][:-1], value["gates"] + [value["gates"][0]],
                      sorted(value["gates"], key=lambda entry: entry["gate"]), list(reversed(value["gates"]))):
            self.refuse({**value, "gates": gates}, parse_manifest)
        for field in ("path", "sha256"):
            changed = deepcopy(value)
            changed["gates"][1]["artifact"][field] = changed["gates"][0]["artifact"][field]
            self.refuse(changed, parse_manifest)

    def test_bundle_owner_exception_is_preserved_without_cross_artifact_role_checks(self):
        value = fixtures()[4][1]
        for role in ("platform-operations", "security"):
            parse_bundle_approval(snapshot({**value, "reviewerRole": role}))
        self.refuse({**value, "reviewerRole": "operations"}, parse_bundle_approval)
        # The predecessor reader checks distinct Refs only; authenticating which
        # reviewer and role each artifact carries belongs to authority assembly.
        parse_predecessor(snapshot(fixtures()[5][1]))

    def test_predecessor_requires_exact_summary_and_two_distinct_approval_refs(self):
        value = fixtures()[5][1]
        for key, replacement in (("approvals", []), ("approvals", [ref("one")]),
                                 ("approvals", [ref("one"), ref("two"), ref("three")]),
                                 ("approvals", [ref("one"), ref("one")]), ("status", "blocked"),
                                 ("productionLifecycleWrites", "enabled"), ("qualificationAuthorized", 1)):
            self.refuse({**value, key: replacement}, parse_predecessor)
        for field in ("path", "sha256"):
            changed = deepcopy(value)
            changed["approvals"][1][field] = changed["approvals"][0][field]
            self.refuse(changed, parse_predecessor)

    def test_every_reader_and_refusal_make_zero_filesystem_network_or_process_calls(self):
        retained = [(reader, snapshot(value)) for reader, value in fixtures()]
        malformed = snapshot({**fixtures()[1][1], "reviewerRole": "password=private-test-credential"})
        with (
            patch("builtins.open", side_effect=AssertionError("no filesystem")),
            patch.object(Path, "open", side_effect=AssertionError("no filesystem")),
            patch.object(os, "open", side_effect=AssertionError("no filesystem")),
            patch.object(os, "stat", side_effect=AssertionError("no filesystem")),
            patch.object(socket, "socket", side_effect=AssertionError("no network")),
            patch.object(socket, "getaddrinfo", side_effect=AssertionError("no network")),
            patch.object(urllib.request, "urlopen", side_effect=AssertionError("no network")),
            patch.object(subprocess, "Popen", side_effect=AssertionError("no process")),
            patch.object(os, "system", side_effect=AssertionError("no process")),
        ):
            for reader, original in retained:
                self.assertIs(reader(original), original)
                self.assertIs(parse_artifact(original), original)
            with self.assertRaises(InterchangeFormatError):
                parse_artifact(malformed)


class PowerShellEncodingTests(unittest.TestCase):
    def test_actual_powershell_default_encoding_matches_independent_literal_vectors(self):
        self.assertIsNotNone(shutil.which("pwsh"), "PowerShell vectors are required and cannot be skipped")
        vectors = [
            (["one"], b'["one"]'),
            (["quote\"back\\slash\x00\b\t\n\f\r café雪😀\x85\u2028\u2029", "<>&'", "\\u0085"],
             ('["quote\\"back\\\\slash\\u0000\\b\\t\\n\\f\\r café雪😀\\u0085\\u2028\\u2029","<>&\'","\\\\u0085"]').encode("utf-8")),
            ({"z": [True, None, -(2**63), 2**63 - 1], "a": {"B": 1, "b": 2}},
             b'{"z":[true,null,-9223372036854775808,9223372036854775807],"a":{"B":1,"b":2}}'),
            ({"context": "jpiquot@local", "namespace": "hexalith-memories", "selector": "app=x",
              "appId": "memories", "actorType": "Actor"},
             b'{"context":"jpiquot@local","namespace":"hexalith-memories","selector":"app=x","appId":"memories","actorType":"Actor"}'),
            ([], b"[]"), (None, b"null"), (False, b"false"),
        ]
        script = r'''
$ErrorActionPreference = 'Stop'
$value = ConvertFrom-Json -InputObject ([Console]::In.ReadToEnd()) -AsHashtable -NoEnumerate
$json = ConvertTo-Json -InputObject $value -Compress -Depth 14
[Console]::Out.Write([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json)))
'''
        for value, expected in vectors:
            with self.subTest(value_type=type(value).__name__):
                self.assertEqual(powershell_json_bytes(value), expected)
                result = subprocess.run(["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script],
                                        input=json.dumps(value, ensure_ascii=False), text=True, capture_output=True,
                                        timeout=30, check=False)
                self.assertEqual(result.returncode, 0, result.stderr)
                actual = base64.b64decode(result.stdout, validate=True)
                self.assertEqual(actual, expected)
                self.assertEqual(digest(actual), digest(expected))

    def test_powershell_and_j1_escaping_and_object_order_stay_separate(self):
        value = {"z": "\x85\u2028\u2029", "a": "café"}
        self.assertEqual(powershell_json_bytes(value), b'{"z":"\\u0085\\u2028\\u2029","a":"caf\xc3\xa9"}')
        self.assertNotEqual(powershell_json_bytes(value), j1_bytes(value))
        for invalid in (1.0, {"a": {1, 2}}, "\ud800", 2**63):
            with self.assertRaises(InterchangeFormatError):
                powershell_json_bytes(invalid)
        with self.assertRaisesRegex(InterchangeFormatError, "artifact-byte-budget-exceeded"):
            powershell_json_bytes(["x" * 4096] * 256)


if __name__ == "__main__":
    unittest.main()
