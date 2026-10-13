"""Independent offline v2 registration fixtures with counted denial barriers."""

from __future__ import annotations

from contextlib import ExitStack, contextmanager
from copy import deepcopy
from dataclasses import FrozenInstanceError, fields, replace
import hashlib
import itertools
import json
import os
from pkgutil import resolve_name
import unittest
from unittest.mock import patch

import test_capture_semantics as semantic_fixtures
import access_telemetry_c1_producer_bindings as bindings
import access_telemetry_c1_source_provenance as provenance
from access_telemetry_c1_interchange import JsonSnapshot, MAX_ARTIFACT_BYTES
from access_telemetry_c1_producer_bindings import (
    RegistrationInspection, inspect_registration, inspect_registry, lookup_deployed_binding,
)
from test_capture_commands import command_fixture, rehash, serial_times
from test_producer_bindings import (
    PATHS, fixture_capture, fixture_sources, registry_entry, snapshot,
)


KINDS = ["current-context", "lifecycle-pods", "daprd-version", "metadata",
         "alpha-opt-in", "lifecycle-pods-recheck"]
V1 = "hexalith.access-telemetry.c1.fixture-command-contract/v1"
V2 = "hexalith.access-telemetry.c1.fixture-command-contract/v2"


def registration_capture(sources, pod_names=("lifecycle-1", "lifecycle-2")):
    """Combine independently authored argv/Pods and nineteen source receipts."""
    value = command_fixture(pod_names)
    source_facts = fixture_capture(sources)
    for name in ("sourceDisposition", "worktreeDirty", "producerSources", "finalSourceRecheck"):
        value[name] = source_facts[name]
    value["sourceCommit"] = sources[0].source_commit
    value["sources"][0] = source_facts["sources"][0]
    return value


def reference(path, retained):
    return {"path": path, "sha256": hashlib.sha256(retained.raw).hexdigest(),
            "byteLength": len(retained.raw)}


def registration_fixture(*, pod_names=("lifecycle-1", "lifecycle-2"), capture=None, sources=None,
                         command_update=None, role_update=None, entry_update=None,
                         statement_update=None, retain=snapshot):
    sources = fixture_sources() if sources is None else sources
    commit = sources[0].source_commit
    capture = registration_capture(sources, pod_names) if capture is None else capture
    command = retain(dict({
        "schemaVersion": V2, "gate": "C1.15", "profileId": "PG-ONPREM-2",
        "producerPath": PATHS[0], "executable": "kubectl", "purposes": KINDS.copy(), "readOnly": True,
    }, **(command_update or {})))
    role = retain(dict({
        "schemaVersion": "hexalith.access-telemetry.c1.fixture-review-role-policy/v1",
        "gate": "C1.15", "profileId": "PG-ONPREM-2",
        "reviewerRole": "independent-security-reviewer", "producerExcluded": True,
    }, **(role_update or {})))
    entry = registry_entry()
    entry["commandContract"] = reference("fixtures/command.json", command)
    entry["reviewRolePolicy"] = reference("fixtures/roles.json", role)
    entry.update(entry_update or {})
    statement = {key: deepcopy(value) for key, value in entry.items() if key != "registrationReceipt"}
    statement.update(schemaVersion="hexalith.access-telemetry.c1.fixture-registration/v1", sourceCommit=commit)
    statement.update(statement_update or {})
    statement = retain(statement)
    entry["registrationReceipt"] = reference("fixtures/registration.json", statement)
    return (inspect_registry(retain([entry])), retain(capture), sources, commit, statement, command, role)


class RegistrationBarriers(unittest.TestCase):
    """Share the full counted harness and also deny registration/provenance aliases."""

    refuse = semantic_fixtures.CaptureSemanticTests.refuse

    @classmethod
    def setUpClass(cls):
        for name in ("builtins.open", "subprocess.Popen", "socket.socket", "os.environ",
                     "time.time", "time.perf_counter"):
            alias = patch.object(bindings, "_denial_probe_" + name.split(".")[-1], resolve_name(name), create=True)
            alias.start()
            cls.addClassCleanup(alias.stop)

    def setUp(self):
        # Snapshot known alias targets before defining-module patches. The shared
        # harness covers the entire filesystem/process/network/config/clock API;
        # this scan adds prebound dependencies in the two registration modules.
        names = (
            "builtins.open", "io.open", "io.FileIO", "pathlib.Path.open", "pathlib.Path.read_bytes",
            "pathlib.Path.read_text", "pathlib.Path.write_bytes", "pathlib.Path.write_text",
            "pathlib.Path.stat", "pathlib.Path.lstat", "pathlib.Path.resolve",
            "pathlib.Path.iterdir", "pathlib.Path.glob", "pathlib.Path.rglob",
            "pathlib.Path.unlink", "pathlib.Path.rename", "pathlib.Path.replace", "pathlib.Path.mkdir",
            "pathlib.Path.rmdir", "pathlib.Path.touch", "pathlib.Path.chmod", "pathlib.Path.symlink_to",
            "os.open", "os.stat", "os.lstat", "os.listdir", "os.scandir", "os.readlink",
            "os.access", "os.remove", "os.unlink", "os.rename", "os.replace", "os.mkdir", "os.makedirs",
            "os.rmdir", "os.removedirs", "os.write", "os.pwrite", "os.truncate", "os.ftruncate",
            "os.chmod", "os.chown", "os.utime", "os.link", "os.symlink",
            "os.getcwd", "os.chdir", "os.system", "os.popen", "os.getenv", "shutil.which",
            "subprocess.Popen", "subprocess.run", "subprocess.call", "subprocess.check_call",
            "subprocess.check_output", "socket.socket", "socket.create_connection",
            "socket.getaddrinfo", "urllib.request.urlopen", "urllib.request.OpenerDirector.open",
            "time.time", "time.time_ns", "time.monotonic", "time.monotonic_ns",
            "time.perf_counter", "time.perf_counter_ns", "time.sleep",
            "datetime.datetime.now", "datetime.datetime.utcnow", "datetime.datetime.today",
            "datetime.date.today", "datetime.datetime", "datetime.date", "os.environ",
        )
        known = [(name, resolve_name(name)) for name in names]
        aliases = []
        for module in (bindings, provenance):
            for attribute, alias in tuple(vars(module).items()):
                for name, original in known:
                    if type(alias) is type(original) and (alias is original or alias == original):
                        # Some public names share an object (open/io.open).
                        # Bind each alias once so its counted label is stable.
                        aliases.append((module, attribute, name))
                        break
        semantic_fixtures.CaptureSemanticTests.setUp(self)
        stack = ExitStack()
        self.addCleanup(stack.close)
        for module, attribute, name in aliases:
            stack.enter_context(patch.object(module, attribute, resolve_name(name)))

        def forbidden(name):
            def call(*args, **kwargs):
                self.dependency_calls[name] = self.dependency_calls.get(name, 0) + 1
                raise AssertionError(name + " was called")
            return call

        for name in ("_git", "_open_directory"):
            stack.enter_context(patch.object(provenance, name, side_effect=forbidden(name)))


@contextmanager
def registration_denials():
    """Deny every local dependency after a provenance test has prepared fixtures."""
    barriers = RegistrationBarriers()
    barriers.setUp()
    try:
        yield barriers.dependency_calls
    finally:
        barriers.doCleanups()
        if barriers.dependency_calls:
            raise AssertionError("registration dependency attempts: " + repr(barriers.dependency_calls))


class RegistrationCommandTests(RegistrationBarriers):
    def inspection(self, inputs):
        result = inspect_registration(*inputs)
        self.assertIs(type(result), RegistrationInspection)
        self.assertEqual(tuple(field.name for field in fields(result)),
                         ("source", "statement", "command_contract", "review_role_policy", "gate",
                          "source_commit", "registration_sha256", "command_sha256", "role_policy_sha256"))
        self.assertIs(result.source.capture, inputs[1])
        for retained, actual, digest in ((inputs[4], result.statement, result.registration_sha256),
                                        (inputs[5], result.command_contract, result.command_sha256),
                                        (inputs[6], result.review_role_policy, result.role_policy_sha256)):
            self.assertIs(actual, retained)
            self.assertEqual(actual.raw, retained.raw)
            self.assertEqual(digest, hashlib.sha256(retained.raw).hexdigest())
        self.assertEqual(result.source.source_commit, inputs[3])
        self.assertEqual(len(result.source.sources), 19)
        for inspected, retained in zip(result.source.sources, sorted(inputs[2], key=lambda source: source.path)):
            self.assertEqual(inspected.executed_bytes_sha256, hashlib.sha256(retained.executed_bytes).hexdigest())
            self.assertEqual(inspected.git_normalized_sha256, hashlib.sha256(retained.blob_bytes).hexdigest())
        value = result.source.capture.value
        self.assertEqual(value["gateStatus"], "not-evaluated")
        self.assertFalse(value["productionGatePassed"])
        self.assertEqual(value["productionLifecycleWrites"], "not-evaluated")
        self.assertEqual(value["independentDisposition"], "pending")
        for name in ("accepted", "passed", "authority", "execution_handle"):
            self.assertFalse(hasattr(result, name))
        return result

    def test_one_two_three_pods_in_all_observation_orders(self):
        for count in (1, 2, 3):
            for names in itertools.permutations(("lifecycle-1", "lifecycle-2", "lifecycle-3")[:count]):
                with self.subTest(pods=names):
                    result = self.inspection(registration_fixture(pod_names=names))
                    self.assertEqual(len(result.source.capture.value["commands"]), 3 * count + 3)
                    self.assertEqual(tuple(pod["pod"] for pod in result.source.capture.value["observations"]["pods"]), names)

    def test_exact_raw_snapshot_bytes_and_frozen_result_are_retained(self):
        results = []
        for indent in (None, 3):
            def retain(value):
                return JsonSnapshot((" \r\n" + json.dumps(value, ensure_ascii=indent is None, indent=indent) + "\n\t").encode())
            inputs = registration_fixture(retain=retain)
            result = self.inspection(inputs)
            results.append(result)
            for name in ("source", "statement", "command_contract", "review_role_policy", "gate",
                         "source_commit", "registration_sha256", "command_sha256", "role_policy_sha256"):
                with self.assertRaises(FrozenInstanceError):
                    setattr(result, name, None)
            with self.assertRaises(TypeError):
                result.command_contract.value["purposes"][0] = "changed"
            self.assertNotIn("sourceCommit", repr(result))
            self.assertNotIn("DAPR_API_TOKEN", repr(result))
        self.assertNotEqual(results[0].source.capture.sha256, results[1].source.capture.sha256)
        self.assertNotEqual(results[0].command_sha256, results[1].command_sha256)

    def test_every_rehashed_argument_including_later_pods_refuses(self):
        original = registration_capture(fixture_sources(), ("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        for number, command in enumerate(original["commands"]):
            for position in range(len(command["arguments"])):
                with self.subTest(purpose=command["purpose"], argument=position):
                    value = deepcopy(original)
                    value["commands"][number]["arguments"][position] = "foreign-tenant-or-pod"
                    rehash(value["commands"][number])
                    self.refuse(lambda: inspect_registration(*registration_fixture(capture=value)),
                                "c115-command-arguments-mismatch")

    def test_probe_scope_unsafe_verb_and_cross_pod_substitution_refuse(self):
        for number, position, replacement in ((1, 1, "foreign-context"), (1, 3, "foreign-tenant"),
                                              (1, 4, "delete"), (5, 5, "lifecycle-1"),
                                              (6, -1, "printf harmless"), (7, -1, "printf false"),
                                              (8, 3, "foreign-namespace")):
            with self.subTest(command=number, argument=position):
                value = registration_capture(fixture_sources())
                value["commands"][number]["arguments"][position] = replacement
                rehash(value["commands"][number])
                self.refuse(lambda: inspect_registration(*registration_fixture(capture=value)),
                            "c115-command-arguments-mismatch")

    def test_receipt_reordering_and_later_pod_blocks_refuse(self):
        for left, right in ((0, 1), (1, 2), (2, 3), (6, 7), (7, 8)):
            value = registration_capture(fixture_sources())
            value["commands"][left], value["commands"][right] = value["commands"][right], value["commands"][left]
            self.refuse(lambda: inspect_registration(*registration_fixture(capture=serial_times(value))),
                        "c115-command-sequence-mismatch")
        value = registration_capture(fixture_sources())
        value["commands"][2:8] = value["commands"][5:8] + value["commands"][2:5]
        self.refuse(lambda: inspect_registration(*registration_fixture(capture=serial_times(value))),
                    "c115-command-sequence-mismatch")

    def test_rehashed_target_identity_and_image_pin_drift_refuse(self):
        for field in ("context", "namespace", "selector", "appId", "actorType"):
            value = registration_capture(fixture_sources())
            value["target"][field] = "foreign-tenant"
            if field in ("appId", "actorType"):
                for pod in value["observations"]["pods"]:
                    pod["appId" if field == "appId" else "actorTypes"] = "foreign-tenant" if field == "appId" else ["foreign-tenant"]
            semantic_fixtures.refresh_declarations(value)
            self.refuse(lambda: inspect_registration(*registration_fixture(capture=value)), "artifact-literal-mismatch")
        value = registration_capture(fixture_sources())
        value["observations"]["pods"][-1].update(sidecarImageId="sha256:" + "0" * 64,
                                                   sidecarImageDigest="sha256:" + "0" * 64)
        semantic_fixtures.refresh_declarations(value)
        self.refuse(lambda: inspect_registration(*registration_fixture(capture=value)), "c115-image-pin-mismatch")

    def test_v1_retains_simplified_acceptance_and_multi_pod_refusals(self):
        self.inspection(registration_fixture(capture=fixture_capture(fixture_sources()), command_update={"schemaVersion": V1}))
        for count in (1, 2, 3):
            inputs = registration_fixture(pod_names=("lifecycle-1", "lifecycle-2", "lifecycle-3")[:count],
                                          command_update={"schemaVersion": V1})
            if count == 1:
                self.inspection(inputs)
            else:
                self.refuse(lambda: inspect_registration(*inputs), "command-contract-unsupported")
        self.refuse(lambda: inspect_registration(*registration_fixture(capture=fixture_capture(fixture_sources()))),
                    "artifact-literal-mismatch")

    def test_closed_version_dispatch_and_seven_field_shape(self):
        names = ("lifecycle-1",)
        self.inspection(registration_fixture(pod_names=names, command_update={"schemaVersion": V1}))
        for update in ({"schemaVersion": V2 + " "}, {"schemaVersion": V2.upper()},
                       {"schemaVersion": "hexalith.access-telemetry.c1.fixture-command-contract/v3"},
                       {"schemaVersion": None}, {"executable": "sh"}, {"readOnly": 1},
                       {"profileId": "PG-ONPREM-1"}, {"gate": "C1.16"}, {"producerPath": PATHS[1]},
                       {"owner": "fixture-owner"}):
            self.refuse(lambda: inspect_registration(*registration_fixture(pod_names=names, command_update=update)),
                        "artifact-literal-mismatch" if "schemaVersion" in update else None)
        command = dict(registration_fixture()[5].value)
        command.pop("readOnly")
        self.refuse(lambda: inspect_registration(*registration_fixture(retain=lambda value: snapshot(
            command if type(value) is dict and value.get("schemaVersion") == V2 else value))), "artifact-field-set")

    def test_six_kinds_remain_exact_and_cleanup_is_false(self):
        for kinds in (KINDS[::-1], KINDS[:-1], KINDS + ["metadata"],
                      [kind + ":lifecycle-1" if kind in ("metadata", "daprd-version", "alpha-opt-in") else kind for kind in KINDS],
                      ["current-context", "lifecycle-pods", "daprd-version", "metadata", "alpha-opt-in",
                       "daprd-version", "metadata", "alpha-opt-in", "lifecycle-pods-recheck"]):
            self.refuse(lambda: inspect_registration(*registration_fixture(command_update={"purposes": kinds})),
                        "command-contract-unsupported")
        for schema in (V1, V2):
            self.refuse(lambda: inspect_registration(*registration_fixture(pod_names=("lifecycle-1",),
                        command_update={"schemaVersion": schema}, entry_update={"cleanupRequired": True})),
                        "command-contract-unsupported")

    def test_role_and_registration_schemas_and_exclusion_are_unchanged(self):
        for update in ({"producerExcluded": False}, {"producerExcluded": 1},
                       {"reviewerRole": "sole-owner/self-reviewed"},
                       {"schemaVersion": "hexalith.access-telemetry.c1.fixture-review-role-policy/v2"},
                       {"gate": "C1.16"}, {"profileId": "PG-ONPREM-1"}, {"owner": "fixture-owner"}):
            self.refuse(lambda: inspect_registration(*registration_fixture(role_update=update)))
        for update in ({"schemaVersion": "hexalith.access-telemetry.c1.fixture-registration/v2"},
                       {"sourceCommit": "f" * 40}, {"cleanupRequired": True}, {"owner": "fixture-owner"}):
            self.refuse(lambda: inspect_registration(*registration_fixture(statement_update=update)))

    def test_all_three_exact_ref_lengths_digests_and_snapshot_bytes_are_bound(self):
        for name in ("registrationReceipt", "commandContract", "reviewRolePolicy"):
            inputs = registration_fixture()
            entry = json.loads(inputs[0].snapshot.raw)[0]
            for field, replacement in (("sha256", "f" * 64), ("byteLength", entry[name]["byteLength"] + 1)):
                altered = deepcopy(entry)
                altered[name][field] = replacement
                changed = (inspect_registry(snapshot([altered])),) + inputs[1:]
                self.refuse(lambda: inspect_registration(*changed),
                            "reference-digest-mismatch" if field == "sha256" else "reference-length-mismatch")
        for position in (4, 5, 6):
            inputs = list(registration_fixture())
            inputs[position] = JsonSnapshot(inputs[position].raw + b" ")
            self.refuse(lambda: inspect_registration(*inputs), "reference-length-mismatch")

    def test_every_source_byte_identity_and_nineteen_source_count_remain_bound(self):
        inputs = registration_fixture()
        for number, source in enumerate(inputs[2]):
            sources = list(inputs[2])
            sources[number] = replace(source, executed_bytes=source.executed_bytes + b"changed",
                                      blob_bytes=source.blob_bytes + b"changed")
            self.refuse(lambda: inspect_registration(*(inputs[:2] + (tuple(sources),) + inputs[3:])),
                        "source-byte-identity-mismatch")
        for sources in (inputs[2][:-1], inputs[2] + (inputs[2][0],)):
            self.refuse(lambda: inspect_registration(*(inputs[:2] + (sources,) + inputs[3:])),
                        "retained-source-count-mismatch")
        sources = tuple(replace(source, source_commit="f" * 40) for source in inputs[2])
        self.refuse(lambda: inspect_registration(*(inputs[:2] + (sources,) + inputs[3:])), "retained-source-commit-mismatch")

    def test_dirty_clean_receipt_and_capture_schema_drift_refuse(self):
        for update in ({"sourceDisposition": "dirty-development", "worktreeDirty": True},
                       {"finalSourceRecheck": "changed-or-unavailable"},
                       {"sourceCommit": "f" * 40}, {"schemaVersion": "hexalith.access-telemetry.c1.evidence/v1"}):
            value = dict(registration_capture(fixture_sources()), **update)
            self.refuse(lambda: inspect_registration(*registration_fixture(capture=value)))
        for number in range(19):
            value = registration_capture(fixture_sources())
            value["producerSources"][number]["trackedAtHead"] = False
            self.refuse(lambda: inspect_registration(*registration_fixture(capture=value)))

    def test_retained_materials_share_existing_artifact_and_aggregate_budgets(self):
        inputs = registration_fixture()
        total = sum(len(raw) for raw in {inputs[0].snapshot.raw, inputs[1].raw, inputs[4].raw,
                                       inputs[5].raw, inputs[6].raw,
                                       *(source.executed_bytes for source in inputs[2]),
                                       *(source.blob_bytes for source in inputs[2])})
        # Exact boundaries use the same real accounting with a bounded limit;
        # changing the limit never changes any fixture bytes or authority facts.
        with patch.object(bindings, "MAX_AGGREGATE_BYTES", total):
            self.inspection(inputs)
        with patch.object(bindings, "MAX_AGGREGATE_BYTES", total - 1):
            self.refuse(lambda: inspect_registration(*inputs), "aggregate-byte-budget-exceeded")
        self.refuse(lambda: JsonSnapshot(b" " * (MAX_ARTIFACT_BYTES + 1)), "artifact-byte-budget-exceeded")
        self.refuse(lambda: replace(inputs[2][0], executed_bytes=b"x" * (MAX_ARTIFACT_BYTES + 1)),
                    "source-byte-budget-exceeded")

    def test_deployed_lookup_remains_unconditional_refusal(self):
        self.refuse(lambda: lookup_deployed_binding("C1.15", "PG-ONPREM-2", registration_fixture()[0]),
                    "deployed-producer-binding-unavailable")

    def test_barriers_count_all_dependency_categories_and_local_entry_points(self):
        for name, attempt in (("os.open", lambda: os.open("/never-opened", 0)),
                              ("subprocess.Popen", lambda: provenance.subprocess.Popen(["never-run"])),
                              ("socket.socket", lambda: resolve_name("socket.socket")()),
                              ("os.environ", lambda: os.environ.get("NEVER_READ")),
                              ("time.time", lambda: resolve_name("time.time")()),
                              ("builtins.open", lambda: bindings._denial_probe_open("/never-opened")),
                              ("subprocess.Popen", lambda: bindings._denial_probe_Popen(["never-run"])),
                              ("socket.socket", lambda: bindings._denial_probe_socket()),
                              ("os.environ", lambda: bindings._denial_probe_environ.get("NEVER_READ")),
                              ("time.time", lambda: bindings._denial_probe_time()),
                              ("time.perf_counter", lambda: bindings._denial_probe_perf_counter()),
                              ("_git", lambda: provenance._git(-1, "never-run")),
                              ("_open_directory", lambda: provenance._open_directory("/never-opened"))):
            with self.assertRaisesRegex(AssertionError, "was called"):
                attempt()
            self.assertEqual(self.dependency_calls, {name: 1})
            self.dependency_calls.clear()


if __name__ == "__main__":
    unittest.main()
