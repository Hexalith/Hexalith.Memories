"""Independent exact argv declarations with counted denial barriers per API case."""

from __future__ import annotations

from copy import deepcopy
from dataclasses import FrozenInstanceError, fields
import hashlib
import itertools
import json
import sys
import unittest

import test_capture_semantics as semantic_fixtures
from access_telemetry_c1_capture_semantics import (
    C115CommandInspection, inspect_c115_command_declarations, inspect_c115_observation_pins,
)
from access_telemetry_c1_interchange import JsonSnapshot, MAX_ARTIFACT_BYTES, parse_capture
from test_artifact_readers import blocked_fixture, capture_fixture, fixture_ps_bytes, receipt, snapshot


# Written independently from the producer source grammar. No implementation
# constants, collector, command encoder or operational consumer builds fixtures.
PROJECTION = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"
METADATA = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'
ALPHA = r'printf "%s\n%s\n" "${AccessTelemetryLifecycle__ComponentIsAlpha-__MISSING__}" "${AccessTelemetryLifecycle__AllowAlphaComponent-__MISSING__}"'
SCOPE = ("--context", "owner@memories-qualification-27-4", "-n", "hexalith-memories")


def declared_argv(pod_names):
    selected = (*SCOPE, "get", "pods", "-l", "app.kubernetes.io/name=memories-access-telemetry", "-o", PROJECTION)
    result = [("current-context", ("config", "current-context")), ("lifecycle-pods", selected)]
    for pod in pod_names:
        result.extend((
            ("daprd-version:" + pod, (*SCOPE, "exec", pod, "-c", "daprd", "--", "/daprd", "--version")),
            ("metadata:" + pod, (*SCOPE, "exec", pod, "-c", "lifecycle", "--", "/bin/sh", "-ec", METADATA)),
            ("alpha-opt-in:" + pod, (*SCOPE, "exec", pod, "-c", "lifecycle", "--", "/bin/sh", "-ec", ALPHA)),
        ))
    result.append(("lifecycle-pods-recheck", selected))
    return result


def serial_times(value):
    for number, command in enumerate(value["commands"], 1):
        command["startedAtUtc"] = f"2026-10-08T10:00:00.{number:02d}00000+00:00"
        command["finishedAtUtc"] = f"2026-10-08T10:00:00.{number:02d}50000+00:00"
    value["sourceCommands"][0]["finishedAtUtc"] = "2026-10-08T10:00:00.0050000+00:00"
    value["sourceCommands"][-1]["startedAtUtc"] = value["commands"][-1]["finishedAtUtc"]
    return value


def command_fixture(pod_names=("lifecycle-1", "lifecycle-2")):
    value = semantic_fixtures.three_pod_fixture() if "lifecycle-3" in pod_names else capture_fixture()
    pods = {pod["pod"]: pod for pod in value["observations"]["pods"]}
    removed = set(pods) - set(pod_names)
    value["observations"]["pods"] = [pods[name] for name in pod_names]
    value["commands"] = [receipt(list(arguments), purpose=purpose)
                         for purpose, arguments in declared_argv(pod_names)]
    value["sources"] = [source for source in value["sources"]
                        if not any(":" + name + ":" in source["source"] for name in removed)]
    value["resultCount"] = len(pod_names)
    return semantic_fixtures.refresh_declarations(serial_times(value))


def rehash(command):
    command["argumentsSha256"] = hashlib.sha256(fixture_ps_bytes(command["arguments"])).hexdigest()
    command["sha256"] = hashlib.sha256(("kubectl " + "\x1f".join(command["arguments"])).encode()).hexdigest()


class CaptureCommandTests(unittest.TestCase):
    # Reuse the established counted barrier harness without inheriting or
    # rediscovering its tests. Every strict, legacy and structural API call below
    # runs with filesystem/process/network/environment/clock dependencies denied.
    setUpClass = classmethod(semantic_fixtures.CaptureSemanticTests.setUpClass.__func__)
    setUp = semantic_fixtures.CaptureSemanticTests.setUp
    refuse = semantic_fixtures.CaptureSemanticTests.refuse

    def inspection(self, value):
        retained = snapshot(value)
        original_bytes = retained.raw
        result = inspect_c115_command_declarations(retained)
        expected = declared_argv(tuple(pod["pod"] for pod in value["observations"]["pods"]))
        self.assertIs(type(result), C115CommandInspection)
        self.assertIs(result.capture, retained)
        self.assertEqual(result.capture.raw, original_bytes)
        self.assertEqual(result.capture.sha256, hashlib.sha256(original_bytes).hexdigest())
        self.assertEqual(result.pod_count, len(value["observations"]["pods"]))
        self.assertEqual(result.pod_names, tuple(pod["pod"] for pod in value["observations"]["pods"]))
        self.assertEqual(result.command_count, 3 * result.pod_count + 3)
        self.assertEqual(result.command_purposes, tuple(purpose for purpose, arguments in expected))
        self.assertEqual(result.command_arguments, tuple(arguments for purpose, arguments in expected))
        self.assertEqual(tuple(field.name for field in fields(result)),
                         ("capture", "pod_count", "pod_names", "command_count", "command_purposes", "command_arguments"))
        self.assertEqual(result.capture.value["gateStatus"], "not-evaluated")
        self.assertFalse(result.capture.value["productionGatePassed"])
        self.assertEqual(result.capture.value["productionLifecycleWrites"], "not-evaluated")
        self.assertEqual(result.capture.value["independentDisposition"], "pending")
        for prohibited in ("passed", "accepted_gate", "accepted", "authority", "execution_handle"):
            self.assertFalse(hasattr(result, prohibited))
        return result

    def command_refusal(self, value, code="c115-command-arguments-mismatch"):
        retained = snapshot(value)
        original_bytes = retained.raw
        self.assertIs(parse_capture(retained), retained)
        self.assertIs(inspect_c115_observation_pins(retained).capture, retained)
        self.refuse(lambda: inspect_c115_command_declarations(retained), code)
        self.assertEqual(retained.raw, original_bytes)

    def inherited_refusal(self, value, code=None):
        retained = snapshot(value)
        original_bytes = retained.raw
        old_error = self.refuse(lambda: inspect_c115_observation_pins(retained), code)
        self.refuse(lambda: inspect_c115_command_declarations(retained), str(old_error))
        self.assertEqual(retained.raw, original_bytes)

    def test_barriers_detect_prebound_and_ambient_attempts_without_live_calls(self):
        semantic_fixtures.CaptureSemanticTests.test_barriers_detect_prebound_aliases_and_filesystem_attempts_without_live_calls(self)

    def test_one_two_three_pods_in_every_observation_order(self):
        for count in (1, 2, 3):
            for names in itertools.permutations(("lifecycle-1", "lifecycle-2", "lifecycle-3")[:count]):
                with self.subTest(pods=names):
                    result = self.inspection(command_fixture(names))
                    self.assertEqual(result.command_count, 3 * count + 3)
        self.inspection(command_fixture(("lifecycle-2",)))
        self.inspection(command_fixture(("lifecycle-3",)))

    def test_exact_retained_json_bytes_are_never_reserialized(self):
        value = command_fixture()
        raws = ((json.dumps(value, ensure_ascii=False, separators=(",", ":")) + "\n").encode(),
                (" \r\n" + json.dumps(value, ensure_ascii=True, indent=3) + "\r\n\t").encode())
        results = [inspect_c115_command_declarations(JsonSnapshot(raw)) for raw in raws]
        self.assertNotEqual(results[0].capture.sha256, results[1].capture.sha256)
        for result, raw in zip(results, raws):
            self.assertEqual(result.capture.raw, raw)
            self.assertEqual(result.command_arguments, tuple(arguments for purpose, arguments in declared_argv(result.pod_names)))

    def test_every_argument_including_later_pods_rehashed_substitution_refuses(self):
        value = command_fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        for number, command in enumerate(value["commands"]):
            for position in range(len(command["arguments"])):
                with self.subTest(purpose=command["purpose"], argument=position):
                    changed = deepcopy(value)
                    child = changed["commands"][number]
                    child["arguments"][position] = "substituted"
                    rehash(child)
                    self.command_refusal(changed)

    def test_wrong_cluster_namespace_selector_projection_and_container_refuse(self):
        mutations = ((1, 1, "other-context"), (1, 3, "other-namespace"), (1, 7, "app=other"),
                     (1, 9, "json"), (2, 7, "lifecycle"), (3, 7, "daprd"), (4, 7, "daprd"),
                     (8, 1, "other-context"), (8, 3, "other-namespace"), (8, 7, "app=other"), (8, 9, "json"))
        for number, position, replacement in mutations:
            with self.subTest(command=number, position=position):
                value = command_fixture()
                value["commands"][number]["arguments"][position] = replacement
                rehash(value["commands"][number])
                self.command_refusal(value)

    def test_cross_pod_version_metadata_and_alpha_refuse(self):
        for number in (2, 3, 4, 5, 6, 7):
            value = command_fixture()
            child = value["commands"][number]
            child["arguments"][5] = "lifecycle-2" if number < 5 else "lifecycle-1"
            rehash(child)
            with self.subTest(purpose=child["purpose"]):
                self.command_refusal(value)

    def test_both_fixed_probe_substitutions_and_exchange_refuse(self):
        for number, replacement in ((3, "printf harmless"), (3, ALPHA), (4, "printf false"), (4, METADATA),
                                     (6, "printf harmless"), (7, ALPHA.replace("__MISSING__", "false"))):
            value = command_fixture()
            value["commands"][number]["arguments"][-1] = replacement
            rehash(value["commands"][number])
            with self.subTest(command=number):
                self.command_refusal(value)

    def test_rehashed_verb_option_addition_removal_split_and_option_reorder_refuse(self):
        mutations = (
            lambda argv: argv.__setitem__(4, "delete"),
            lambda argv: argv.__setitem__(slice(0, 2), ["--context=owner@memories-qualification-27-4"]),
            lambda argv: argv.__setitem__(slice(0, 4), argv[2:4] + argv[:2]),
            lambda argv: argv.__setitem__(2, "--namespace"),
        )
        for mutate in mutations:
            value = command_fixture()
            mutate(value["commands"][1]["arguments"])
            rehash(value["commands"][1])
            self.command_refusal(value)
        complete = command_fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        for number, command in enumerate(complete["commands"]):
            for position in (None, *range(len(command["arguments"]))):
                with self.subTest(purpose=command["purpose"], removal=position):
                    value = deepcopy(complete)
                    child = value["commands"][number]
                    if position is None:
                        child["arguments"].append("--all-namespaces")
                    else:
                        child["arguments"].pop(position)
                    rehash(child)
                    self.command_refusal(value)
        for number in (3, 4):
            value = command_fixture()
            value["commands"][number]["arguments"][-1:] = ["printf", "harmless"]
            rehash(value["commands"][number])
            self.command_refusal(value)

    def test_empty_whitespace_case_and_unicode_arguments_are_not_normalized(self):
        for number, position in ((0, 0), (1, 1), (1, 3), (1, 7), (1, 9), (2, 5), (2, 7), (2, 9), (4, 11)):
            original = command_fixture()["commands"][number]["arguments"][position]
            fullwidth = "".join(chr(ord(character) + 0xFEE0) if "!" <= character <= "~" else character for character in original)
            for replacement in ("", " " + original, original + " ", original.swapcase(), fullwidth):
                with self.subTest(command=number, argument=position, replacement=replacement):
                    value = command_fixture()
                    value["commands"][number]["arguments"][position] = replacement
                    rehash(value["commands"][number])
                    self.command_refusal(value)

    def test_context_selection_per_pod_and_recheck_reorders_with_valid_chronology_refuse(self):
        for left, right in ((0, 1), (0, 8), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 8), (2, 7)):
            value = command_fixture()
            value["commands"][left], value["commands"][right] = value["commands"][right], value["commands"][left]
            with self.subTest(left=left, right=right):
                self.command_refusal(serial_times(value), "c115-command-sequence-mismatch")

    def test_complete_pod_blocks_must_follow_observation_order(self):
        value = command_fixture()
        value["observations"]["pods"].reverse()
        self.command_refusal(value, "c115-command-sequence-mismatch")
        value["commands"][2:8] = value["commands"][5:8] + value["commands"][2:5]
        self.inspection(serial_times(value))

    def test_summary_array_reordering_retains_pod_and_receipt_order(self):
        value = command_fixture(("lifecycle-3", "lifecycle-1", "lifecycle-2"))
        original = self.inspection(value)
        for name in ("sidecarImageIds", "sidecarImageDigests"):
            with self.subTest(summary=name):
                reordered = deepcopy(value)
                reordered["observations"][name].reverse()
                self.assertNotEqual(reordered["observations"][name], value["observations"][name])
                checked = self.inspection(reordered)
                self.assertEqual(checked.pod_names, original.pod_names)
                self.assertEqual(checked.command_purposes, original.command_purposes)
                self.assertEqual(checked.command_arguments, original.command_arguments)
                self.assertEqual(reordered["observations"]["pods"], value["observations"]["pods"])
                self.assertEqual(reordered["commands"], value["commands"])

    def test_missing_surplus_and_duplicate_receipts_keep_structural_refusals(self):
        for number in range(9):
            value = command_fixture()
            value["commands"].pop(number)
            with self.subTest(missing=number):
                self.inherited_refusal(value)
        value = command_fixture()
        value["commands"].insert(2, receipt(["get", "pods"], purpose="daprd-version:other-pod"))
        self.inherited_refusal(serial_times(value), "artifact-observation-receipt-summary")
        value = command_fixture()
        value["commands"].insert(2, deepcopy(value["commands"][2]))
        self.inherited_refusal(serial_times(value), "artifact-duplicate-command-purpose")

    def test_executable_substitution_keeps_existing_refusal(self):
        for number in range(9):
            value = command_fixture()
            value["commands"][number]["executable"] = "other-kubectl"
            with self.subTest(command=number):
                self.inherited_refusal(value, "artifact-literal-mismatch")

    def test_hash_and_chronology_contradictions_keep_existing_refusals(self):
        mutations = (
            lambda value: value["commands"][3].update(argumentsSha256="0" * 64),
            lambda value: value["commands"][3].update(sha256="0" * 64),
            lambda value: value["commands"][5].update(startedAtUtc=value["commands"][4]["startedAtUtc"]),
            lambda value: value["commands"][8].update(finishedAtUtc="2026-10-08T10:00:02.0000000+00:00"),
            lambda value: value["sourceCommands"][-1].update(startedAtUtc=value["commands"][8]["startedAtUtc"]),
            lambda value: value["producer"].update(argumentsSha256="0" * 64),
        )
        for mutate in mutations:
            value = command_fixture()
            mutate(value)
            self.inherited_refusal(value)

    def test_blocked_pin_summary_and_observation_contradictions_keep_existing_refusals(self):
        self.inherited_refusal(blocked_fixture(), "artifact-literal-mismatch")
        value = blocked_fixture()
        value.update(finalSourceRecheck="changed-or-unavailable", blockers=["producer-source-changed"])
        self.inherited_refusal(value, "artifact-literal-mismatch")
        for field, replacement in (("context", "other-context"), ("namespace", "other-namespace"), ("selector", "app=other")):
            value = command_fixture()
            value["target"][field] = replacement
            semantic_fixtures.refresh_declarations(value)
            self.inherited_refusal(value, "artifact-literal-mismatch")
        value = command_fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        value["observations"]["pods"][-1].update(sidecarImageId="sha256:" + "0" * 64, sidecarImageDigest="sha256:" + "0" * 64)
        semantic_fixtures.refresh_declarations(value)
        self.inherited_refusal(value, "c115-image-pin-mismatch")
        value = command_fixture()
        value["observations"]["pods"][1]["runtimeVersion"] = "1.18.2"
        semantic_fixtures.refresh_declarations(value)
        self.inherited_refusal(value, "artifact-pod-summary-contradiction")

    def test_exact_producer_arguments_retain_structural_grammar_and_session_binding(self):
        for position in (0, 1, 2, 3, 4, 5, 6, 8):
            value = command_fixture()
            value["producer"]["arguments"][position] = "substituted"
            value["producer"]["argumentsSha256"] = hashlib.sha256(fixture_ps_bytes(value["producer"]["arguments"])).hexdigest()
            with self.subTest(position=position):
                self.inherited_refusal(value, "artifact-literal-mismatch")
        for timeout in ("0", "301", "030", " 30", "30 "):
            value = command_fixture()
            value["producer"]["arguments"][-1] = timeout
            value["producer"]["argumentsSha256"] = hashlib.sha256(fixture_ps_bytes(value["producer"]["arguments"])).hexdigest()
            self.inherited_refusal(value, "artifact-producer-timeout-invalid")
        for extra in (True, False):
            value = command_fixture()
            if extra:
                value["producer"]["arguments"].append("-Extra")
            else:
                value["producer"]["arguments"].pop()
            value["producer"]["argumentsSha256"] = hashlib.sha256(fixture_ps_bytes(value["producer"]["arguments"])).hexdigest()
            self.inherited_refusal(value, "artifact-producer-argument-shape")

    def test_neutral_session_evidence_directory_timeout_and_dirty_facts_grant_no_authority(self):
        for timeout in ("1", "300"):
            value = command_fixture()
            value["qualificationSessionId"] = "unregistered-session"
            value["producer"]["arguments"][5] = value["qualificationSessionId"]
            value["producer"]["arguments"][7] = "/unread/unowned/café雪"
            value["producer"]["arguments"][-1] = timeout
            value["producer"]["argumentsSha256"] = hashlib.sha256(fixture_ps_bytes(value["producer"]["arguments"])).hexdigest()
            value.update(sourceDisposition="dirty-development", worktreeDirty=True)
            result = self.inspection(value)
            self.assertEqual(result.capture.value["sourceDisposition"], "dirty-development")

    def test_simplified_structural_fixture_and_legacy_pin_api_are_unchanged(self):
        value = capture_fixture()
        self.assertEqual(value["commands"][1]["arguments"], ["get", "pods"])
        self.command_refusal(value)

    def test_snapshot_schema_neutral_markers_and_malformed_inputs_keep_refusals(self):
        for original in (None, b"{}", {}, [], object()):
            self.refuse(lambda: inspect_c115_command_declarations(original), "artifact-snapshot-required")
        for changed in ({"schemaVersion": "hexalith.access-telemetry.c1.evidence/v1"}, {"unknown": True},
                        {"producerStatus": "accepted"}, {"productionGatePassed": True}, {"gateStatus": "passed"},
                        {"independentDisposition": "accepted"}, {"resultCount": True}):
            self.inherited_refusal(dict(command_fixture(), **changed))
        for raw in (b"{", b"\xef\xbb\xbf{}", b"\xff", b'{"duplicate":1,"duplicate":2}', b"x" * (MAX_ARTIFACT_BYTES + 1)):
            self.refuse(lambda: inspect_c115_command_declarations(JsonSnapshot(raw)))
        value = command_fixture()
        value["commands"][1]["arguments"][1] = "bearer private-test-credential"
        rehash(value["commands"][1])
        error = self.refuse(lambda: inspect_c115_command_declarations(snapshot(value)), "artifact-secret-shaped-content")
        self.assertIsNone(error.__cause__)

    def test_result_snapshot_and_derived_command_values_are_frozen(self):
        result = self.inspection(command_fixture())
        for attribute, replacement in (("capture", None), ("pod_count", 99), ("pod_names", ("changed",)),
                                       ("command_count", 99), ("command_purposes", ("changed",)), ("command_arguments", (("changed",),))):
            with self.assertRaises(FrozenInstanceError):
                setattr(result, attribute, replacement)
        with self.assertRaises(FrozenInstanceError):
            result.capture.raw = b"changed"
        for value in (result.pod_names, result.command_purposes, result.command_arguments, result.command_arguments[0]):
            with self.assertRaises(TypeError):
                value[0] = "changed"
        with self.assertRaises(TypeError):
            result.capture.value["commands"][0]["arguments"][0] = "changed"
        self.assertNotIn("sourceCommit", repr(result))
        self.assertNotIn("/fixture/archive", repr(result))
        self.assertNotIn("DAPR_API_TOKEN", repr(result))


class CaptureCommandProducerTests(unittest.TestCase):
    def test_existing_offline_producer_packets_match_strict_inspection(self):
        # The established producer harness owns disposable files and fake
        # kubectl setup. Import its module without rediscovering its test class.
        sys.path.insert(0, str(semantic_fixtures.REPO_ROOT / "tests/tooling/access_telemetry_c1"))
        import pg2_runtime_control_plane_identity_test as producer_fixtures

        harness = producer_fixtures.Pg2RuntimeControlPlaneIdentityTests()
        self.addCleanup(harness.doCleanups)
        harness.setUp()
        for count in (1, 2, 3):
            with self.subTest(pod_count=count):
                scenario = deepcopy(harness.base)
                for number in range(2, count + 1):
                    additional = deepcopy(scenario["pods"]["items"][0])
                    name = harness.pod + "-extra-" + str(number)
                    additional["metadata"].update(name=name, uid="fixture-extra-pod-uid-" + str(number))
                    scenario["pods"]["items"].append(additional)
                    for field in ("metadata", "daprdVersions", "alphaOptIn"):
                        scenario[field][name] = deepcopy(scenario[field][harness.pod])
                scenario["pods"]["items"].reverse()
                result, packets, calls, evidence = harness.run_gate(scenario)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(len(packets), 1)
                raw = next(evidence.glob("*.json")).read_bytes()

                # Install counted barriers only after offline fixture I/O.
                # Snapshot construction and strict inspection are both pure.
                barriers = CaptureCommandTests()
                barriers.setUp()
                try:
                    retained = JsonSnapshot(raw)
                    checked = inspect_c115_command_declarations(retained)
                    self.assertIs(checked.capture, retained)
                    self.assertEqual(checked.capture.raw, raw)
                    self.assertEqual(checked.capture.sha256, hashlib.sha256(raw).hexdigest())
                    self.assertEqual(checked.pod_count, count)
                    self.assertEqual(checked.command_count, 3 * count + 3)
                    self.assertEqual(checked.command_arguments, tuple(tuple(argv) for argv in calls))
                    self.assertEqual(checked.pod_names, tuple(pod["metadata"]["name"] for pod in scenario["pods"]["items"]))
                    self.assertEqual(checked.command_purposes, tuple(command["purpose"] for command in packets[0]["commands"]))
                    self.assertEqual(barriers.dependency_calls, {})
                finally:
                    barriers.doCleanups()


if __name__ == "__main__":
    unittest.main()
