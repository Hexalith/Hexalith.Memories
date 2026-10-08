"""Independent declaration fixtures with denial barriers around every API case."""

from __future__ import annotations

from contextlib import ExitStack
from copy import deepcopy
from dataclasses import FrozenInstanceError, fields
from datetime import date, datetime
import hashlib
import io
import json
import os
from pathlib import Path
from pkgutil import resolve_name
import sys
import time
import unicodedata
import unittest
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))

import access_telemetry_c1_capture_semantics as capture_semantics  # noqa: E402
from access_telemetry_c1_capture_semantics import (  # noqa: E402
    C115ObservationInspection, inspect_c115_observation_pins,
)
from access_telemetry_c1_interchange import (  # noqa: E402
    InterchangeFormatError, JsonSnapshot, MAX_ARTIFACT_BYTES, parse_capture,
)
from access_telemetry_c1_producer_bindings import inspect_registry, inspect_sources  # noqa: E402
from test_artifact_readers import (  # noqa: E402
    blocked_fixture, capture_fixture, fixture_ps_bytes, snapshot,
)
from test_producer_bindings import (  # noqa: E402
    COMMIT, fixture_capture, fixture_sources, registry_entry,
)


# Literal expectations are authored from the published contract, independently
# of the implementation constants. No operational collector or encoder runs.
INDEX = "sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8"
CHILD = "sha256:edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b"


def fixture_hash(value):
    return hashlib.sha256(fixture_ps_bytes(value)).hexdigest()


def refresh_declarations(value):
    """Keep changed declarations self-consistent without production helpers."""
    target = value["target"]
    value.update(context=target["context"], namespace=target["namespace"],
                 targetSelector=target["selector"], targetSha256=fixture_hash(target))
    pods = value["observations"]["pods"]
    for plural, singular in (("runtimeVersions", "runtimeVersion"), ("sidecarImageIds", "sidecarImageId"),
                             ("sidecarImageDigests", "sidecarImageDigest"), ("appIds", "appId"),
                             ("schedulerConnectedAddresses", "schedulerConnectedAddresses"),
                             ("actorTypes", "actorTypes"), ("enabledFeatures", "enabledFeatures")):
        declared = set()
        for pod in pods:
            child = pod[singular]
            declared.update(child if type(child) is list else [child])
        value["observations"][plural] = sorted(declared)
    for source in value["sources"]:
        if source["source"].startswith("kubectl:metadata:"):
            name = source["source"].split(":")[2]
            pod = next(pod for pod in pods if pod["pod"] == name)
            projection = {"id": pod["appId"], "runtimeVersion": pod["runtimeVersion"],
                          "schedulerConnectedAddresses": pod["schedulerConnectedAddresses"],
                          "actorTypes": pod["actorTypes"], "enabledFeatures": pod["enabledFeatures"]}
            source["sha256"] = fixture_hash(projection)
    return value


def changed_target(field, replacement):
    value = capture_fixture()
    value["target"][field] = replacement
    if field == "appId":
        for pod in value["observations"]["pods"]:
            pod["appId"] = replacement
    if field == "actorType":
        for pod in value["observations"]["pods"]:
            pod["actorTypes"] = [replacement]
    return refresh_declarations(value)


def three_pod_fixture():
    """Add a third independent Pod and consistent serial command/source facts."""
    value = capture_fixture()
    third = deepcopy(value["observations"]["pods"][0])
    third.update(pod="lifecycle-3", podUid="fixture-uid-3", sidecarImageId="docker://" + INDEX)
    value["observations"]["pods"].append(third)
    additional = []
    for command in value["commands"]:
        if command["purpose"].endswith(":lifecycle-1"):
            command = deepcopy(command)
            command["purpose"] = command["purpose"].replace("lifecycle-1", "lifecycle-3")
            command["arguments"] = ["lifecycle-3" if argument == "lifecycle-1" else argument
                                    for argument in command["arguments"]]
            command["argumentsSha256"] = fixture_hash(command["arguments"])
            command["sha256"] = hashlib.sha256(("kubectl " + "\x1f".join(command["arguments"])).encode()).hexdigest()
            additional.append(command)
    value["commands"][-1:-1] = additional
    for number, command in enumerate(value["commands"], 1):
        command["startedAtUtc"] = f"2026-10-08T10:00:00.{number:02d}00000+00:00"
        command["finishedAtUtc"] = f"2026-10-08T10:00:00.{number:02d}50000+00:00"
    value["sourceCommands"][0]["finishedAtUtc"] = "2026-10-08T10:00:00.0050000+00:00"
    value["sourceCommands"][-1]["startedAtUtc"] = value["commands"][-1]["finishedAtUtc"]
    value["sources"].extend(dict(source, source=source["source"].replace("lifecycle-1", "lifecycle-3"))
                           for source in tuple(value["sources"]) if ":lifecycle-1:" in source["source"])
    value["resultCount"] = 3
    return refresh_declarations(value)


class CaptureSemanticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        # Bind representative real dependencies before any setUp patches, as
        # a module-level import alias would. They are never called unpatched.
        for name, dependency in (("_denial_probe_clock", time.time), ("_denial_probe_now", datetime.now),
                                 ("_denial_probe_datetime", datetime), ("_denial_probe_date", date),
                                 ("_denial_probe_environment", os.environ),
                                 ("_denial_probe_access", os.access), ("_denial_probe_fileio", io.FileIO),
                                 ("_denial_probe_remove", os.remove)):
            alias = patch.object(capture_semantics, name, dependency, create=True)
            alias.start()
            cls.addClassCleanup(alias.stop)

    def setUp(self):
        # Install for the entire test, including every positive, negative,
        # inherited reader and source-eligibility API call. Counts catch a
        # dependency even if an implementation accidentally swallows its error.
        self.dependency_calls = {}

        def forbidden(name):
            def call(*args, **kwargs):
                self.dependency_calls[name] = self.dependency_calls.get(name, 0) + 1
                raise AssertionError(name + " was called")
            return call

        class ForbiddenEnvironment:
            __getitem__ = forbidden("os.environ")
            __iter__ = forbidden("os.environ")
            __len__ = forbidden("os.environ")
            __contains__ = forbidden("os.environ")
            get = forbidden("os.environ")
            copy = forbidden("os.environ")

        class ForbiddenDate(date):
            today = staticmethod(forbidden("date.today"))

        class ForbiddenDateTime(datetime):
            now = staticmethod(forbidden("datetime.now"))
            utcnow = staticmethod(forbidden("datetime.utcnow"))
            today = staticmethod(forbidden("datetime.today"))

        stack = ExitStack()
        self.addCleanup(lambda: self.assertEqual(self.dependency_calls, {}))
        self.addCleanup(stack.close)
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
        )
        # Snapshot originals before patching their defining modules, then deny
        # any identical known dependency already bound in the inspector. Bound
        # datetime methods compare equal despite fresh attribute wrapper objects.
        known = [(name, resolve_name(name)) for name in names]
        known.extend((name, dependency) for name, dependency in (
            ("date.today", date.today), ("datetime.now", datetime.now),
            ("datetime.utcnow", datetime.utcnow), ("datetime.today", datetime.today),
        ))
        for attribute, alias in tuple(vars(capture_semantics).items()):
            if alias is datetime or alias is date or alias is os.environ:
                replacement = (ForbiddenDateTime if alias is datetime else
                               ForbiddenDate if alias is date else ForbiddenEnvironment())
                stack.enter_context(patch.object(capture_semantics, attribute, replacement))
                continue
            for name, original in known:
                if type(alias) is type(original) and alias == original:
                    stack.enter_context(patch.object(capture_semantics, attribute, side_effect=forbidden(name)))
                    break
        for name in names:
            stack.enter_context(patch(name, side_effect=forbidden(name)))
        stack.enter_context(patch("os.environ", ForbiddenEnvironment()))
        stack.enter_context(patch("datetime.date", ForbiddenDate))
        stack.enter_context(patch("datetime.datetime", ForbiddenDateTime))
        stack.enter_context(patch("access_telemetry_c1_interchange.date", ForbiddenDate))

    def test_barriers_detect_prebound_aliases_and_filesystem_attempts_without_live_calls(self):
        attempts = (
            ("time.time", lambda: capture_semantics._denial_probe_clock()),
            ("datetime.now", lambda: capture_semantics._denial_probe_now()),
            ("datetime.now", lambda: capture_semantics._denial_probe_datetime.now()),
            ("date.today", lambda: capture_semantics._denial_probe_date.today()),
            ("os.environ", lambda: capture_semantics._denial_probe_environment.get("NEVER_READ")),
            ("os.access", lambda: capture_semantics._denial_probe_access("/tmp", 0)),
            ("io.FileIO", lambda: capture_semantics._denial_probe_fileio("/never-created", "w")),
            ("os.remove", lambda: capture_semantics._denial_probe_remove("/never-removed")),
            ("os.access", lambda: os.access("/tmp", 0)),
            ("io.FileIO", lambda: io.FileIO("/never-created", "w")),
            ("os.rename", lambda: os.rename("/never-renamed", "/never-created")),
            ("os.write", lambda: os.write(-1, b"never-written")),
            ("pathlib.Path.unlink", lambda: Path("/never-removed").unlink()),
            ("pathlib.Path.write_bytes", lambda: Path("/never-created").write_bytes(b"never-written")),
        )
        for name, attempt in attempts:
            with self.subTest(dependency=name):
                with self.assertRaisesRegex(AssertionError, "was called"):
                    attempt()
                self.assertEqual(self.dependency_calls, {name: 1})
                # Only these deliberate sentinel probes are acknowledged; all
                # inspector API cases still require zero dependency attempts.
                self.dependency_calls.clear()

    def refuse(self, action, code=None):
        with self.assertRaises(InterchangeFormatError) as caught:
            action()
        message = str(caught.exception)
        self.assertLess(len(message), 80)
        self.assertTrue(caught.exception.__suppress_context__)
        self.assertNotIn("private-test-credential", message)
        if code is not None:
            self.assertEqual(message, code)
        return caught.exception

    def semantic_refusal(self, value, code="artifact-literal-mismatch"):
        retained = snapshot(value)
        self.assertIs(parse_capture(retained), retained)
        original_bytes = retained.raw
        self.refuse(lambda: inspect_c115_observation_pins(retained), code)
        self.assertEqual(retained.raw, original_bytes)

    def inspection(self, value):
        retained = snapshot(value)
        result = inspect_c115_observation_pins(retained)
        self.assertIs(type(result), C115ObservationInspection)
        self.assertIs(result.capture, retained)
        self.assertEqual(result.pod_count, len(value["observations"]["pods"]))
        self.assertEqual(result.pod_names, tuple(pod["pod"] for pod in value["observations"]["pods"]))
        self.assertEqual(result.capture.raw, retained.raw)
        self.assertEqual(result.capture.sha256, hashlib.sha256(retained.raw).hexdigest())
        self.assertEqual(tuple(field.name for field in fields(result)), ("capture", "pod_count", "pod_names"))
        self.assertEqual(result.capture.value["gateStatus"], "not-evaluated")
        self.assertFalse(result.capture.value["productionGatePassed"])
        self.assertEqual(result.capture.value["independentDisposition"], "pending")
        return result

    def test_published_index_and_child_coexist_with_exact_retained_bytes(self):
        result = self.inspection(capture_fixture())
        self.assertEqual(result.pod_count, 2)
        self.assertEqual(result.pod_names, ("lifecycle-1", "lifecycle-2"))
        self.assertEqual(tuple(pod["sidecarImageDigest"] for pod in result.capture.value["observations"]["pods"]),
                         (INDEX, CHILD))
        self.assertTrue(result.capture.raw.endswith(b"\r\n"))

    def test_single_pod_captures_derive_nonconstant_count_and_selected_name(self):
        for number in (0, 1):
            value = capture_fixture()
            kept = value["observations"]["pods"][number]
            removed = value["observations"]["pods"][1 - number]["pod"]
            value["observations"]["pods"] = [kept]
            value["commands"] = [command for command in value["commands"]
                                 if not command["purpose"].endswith(":" + removed)]
            value["sources"] = [source for source in value["sources"]
                                if ":" + removed + ":" not in source["source"]]
            value["resultCount"] = 1
            with self.subTest(kept=kept["pod"]):
                result = self.inspection(refresh_declarations(value))
                self.assertEqual(result.pod_count, 1)
                self.assertEqual(result.pod_names, (kept["pod"],))

    def test_three_pods_derive_the_complete_count_and_names(self):
        result = self.inspection(three_pod_fixture())
        self.assertEqual(result.pod_count, 3)
        self.assertEqual(result.pod_names, ("lifecycle-1", "lifecycle-2", "lifecycle-3"))

    def test_unapproved_digest_in_the_third_pod_is_structurally_admitted_then_refused(self):
        value = three_pod_fixture()
        value["observations"]["pods"][-1].update(
            sidecarImageId="docker://sha256:" + "0" * 64, sidecarImageDigest="sha256:" + "0" * 64)
        self.semantic_refusal(refresh_declarations(value), "c115-image-pin-mismatch")

    def test_supported_prefixes_are_per_pod_with_no_repository_allowlist(self):
        prefixes = ("", "containerd://", "docker://", "docker-pullable://unregistered.example/other/runtime@",
                    "ContainerD://", "opaque-prefix-")
        for number, prefix in enumerate(prefixes):
            with self.subTest(prefix=prefix):
                value = capture_fixture()
                pods = value["observations"]["pods"]
                pods[0]["sidecarImageId"] = prefix + INDEX
                pods[1]["sidecarImageId"] = prefixes[-number - 1] + CHILD
                self.inspection(refresh_declarations(value))

    def test_either_approved_digest_can_be_repeated_without_shared_raw_representation(self):
        for approved in (INDEX, CHILD):
            value = capture_fixture()
            for number, pod in enumerate(value["observations"]["pods"]):
                pod.update(sidecarImageDigest=approved, sidecarImageId=("" if number else "containerd://") + approved)
            self.inspection(refresh_declarations(value))

    def test_input_pod_order_and_summary_order_are_independent(self):
        value = capture_fixture()
        value["observations"]["pods"].reverse()
        value["observations"]["sidecarImageIds"].reverse()
        value["observations"]["sidecarImageDigests"].reverse()
        result = self.inspection(value)
        self.assertEqual(result.pod_names, ("lifecycle-2", "lifecycle-1"))

    def test_exact_whitespace_and_encoding_bytes_are_not_reserialized(self):
        value = capture_fixture()
        raws = ((json.dumps(value, ensure_ascii=False, separators=(",", ":")) + "\n").encode(),
                (" \r\n" + json.dumps(value, ensure_ascii=True, indent=3) + "\r\n\t").encode())
        results = [inspect_c115_observation_pins(JsonSnapshot(raw)) for raw in raws]
        self.assertNotEqual(results[0].capture.sha256, results[1].capture.sha256)
        for result, raw in zip(results, raws):
            self.assertEqual(result.capture.raw, raw)
            self.assertEqual(result.pod_names, ("lifecycle-1", "lifecycle-2"))

    def test_each_profile_and_workload_semantic_substitution_is_structurally_admitted(self):
        for field, changed in (("profileIdentity", "different-profile"), ("profileSha256", "0" * 64),
                               ("workloadId", "different-workload"), ("workloadSha256", "0" * 64)):
            with self.subTest(field=field):
                self.semantic_refusal(dict(capture_fixture(), **{field: changed}))

    def test_each_target_substitution_is_self_consistent_before_pin_refusal(self):
        for field, changed in (("context", "isolated-wrong-context"), ("namespace", "other-namespace"),
                               ("selector", "app=other"), ("appId", "other-app"),
                               ("actorType", "OtherLifecycleActor")):
            with self.subTest(field=field):
                self.semantic_refusal(changed_target(field, changed))

    def test_runtime_changes_are_structurally_valid_when_all_pods_agree(self):
        for runtime in ("1.18.2", "1.18.1+build", "1.18.1-rc.1", "01.18.1"):
            value = capture_fixture()
            for pod in value["observations"]["pods"]:
                pod["runtimeVersion"] = runtime
            with self.subTest(runtime=runtime):
                self.semantic_refusal(refresh_declarations(value))

    def test_each_pod_image_substitution_refuses_including_a_later_bad_pod(self):
        for number in (0, 1):
            value = capture_fixture()
            value["observations"]["pods"][number].update(
                sidecarImageId="containerd://sha256:" + "0" * 64, sidecarImageDigest="sha256:" + "0" * 64)
            with self.subTest(pod=number):
                self.semantic_refusal(refresh_declarations(value), "c115-image-pin-mismatch")

    def test_identity_and_target_strings_are_not_trimmed_or_case_folded(self):
        for field in ("profileIdentity", "workloadId"):
            for changed in (capture_fixture()[field].upper(), " " + capture_fixture()[field], capture_fixture()[field] + " "):
                with self.subTest(field=field, changed=changed):
                    self.semantic_refusal(dict(capture_fixture(), **{field: changed}))
        for field in ("context", "namespace", "selector", "appId", "actorType"):
            original = capture_fixture()["target"][field]
            # Actor grammar rejects spaces before semantic comparison; every
            # other target string admits them structurally and must not trim.
            for changed in (original.swapcase(),) if field == "actorType" else (original.swapcase(), " " + original, original + " "):
                with self.subTest(field=field, changed=changed):
                    self.semantic_refusal(changed_target(field, changed))

    def test_self_consistent_fullwidth_pins_are_not_unicode_normalized(self):
        def fullwidth(value):
            return "".join(chr(ord(character) + 0xFEE0) if "!" <= character <= "~" else character
                           for character in value)

        for field in ("profileIdentity", "workloadId"):
            original = capture_fixture()[field]
            changed = fullwidth(original)
            self.assertNotEqual(changed, original)
            self.assertEqual(unicodedata.normalize("NFKC", changed), original)
            with self.subTest(field=field):
                self.semantic_refusal(dict(capture_fixture(), **{field: changed}))
        for field in ("context", "namespace", "selector", "appId"):
            original = capture_fixture()["target"][field]
            changed = fullwidth(original)
            self.assertEqual(unicodedata.normalize("NFKC", changed), original)
            with self.subTest(field=field):
                self.semantic_refusal(changed_target(field, changed))

    def test_profile_id_and_one_pod_runtime_are_existing_structural_refusals(self):
        for profile in ("PG-ONPREM-1", "pg-onprem-2", " PG-ONPREM-2", "PG-ONPREM-2 "):
            value = capture_fixture()
            value["profileId"] = profile
            self.refuse(lambda: parse_capture(snapshot(value)), "artifact-literal-mismatch")
            self.refuse(lambda: inspect_c115_observation_pins(snapshot(value)), "artifact-literal-mismatch")
        for number in (0, 1):
            value = capture_fixture()
            value["observations"]["pods"][number]["runtimeVersion"] = "1.18.2"
            refresh_declarations(value)
            self.refuse(lambda: parse_capture(snapshot(value)), "artifact-pod-summary-contradiction")
            self.refuse(lambda: inspect_c115_observation_pins(snapshot(value)), "artifact-pod-summary-contradiction")

    def test_existing_summary_count_image_and_pod_contradictions_refuse(self):
        mutations = (
            lambda value: value.update(resultCount=1),
            lambda value: value["observations"].update(pods=[]),
            lambda value: value["observations"].update(runtimeVersions=["1.18.2"]),
            lambda value: value["observations"]["pods"][1].update(sidecarImageDigest=INDEX),
            lambda value: value["observations"]["pods"][1].update(pod="lifecycle-1"),
            lambda value: value["observations"]["pods"][1].update(podUid="fixture-uid-1"),
            lambda value: value["observations"]["pods"][1].update(alphaOptIn={"componentIsAlpha": True, "allowAlphaComponent": False}),
            lambda value: value["observations"]["pods"][1].update(schedulerConnectedAddresses=[]),
            lambda value: value["observations"]["pods"][1].update(actorTypes=[]),
            lambda value: value.update(targetSha256="0" * 64),
        )
        for number, mutate in enumerate(mutations):
            value = capture_fixture()
            mutate(value)
            with self.subTest(mutation=number):
                self.refuse(lambda: parse_capture(snapshot(value)))
                self.refuse(lambda: inspect_c115_observation_pins(snapshot(value)))

    def test_image_case_spaces_and_invalid_prefixes_remain_structural_refusals(self):
        for image in (INDEX.upper(), INDEX + " ", " " + INDEX, "bad prefix@" + INDEX, "x?" + INDEX):
            value = capture_fixture()
            value["observations"]["pods"][0]["sidecarImageId"] = image
            refresh_declarations(value)
            self.refuse(lambda: inspect_c115_observation_pins(snapshot(value)), "artifact-image-digest-contradiction")
        for digest in (INDEX.upper(), INDEX + " "):
            value = capture_fixture()
            value["observations"]["pods"][0].update(sidecarImageDigest=digest, sidecarImageId="containerd://" + digest)
            self.refuse(lambda: inspect_c115_observation_pins(snapshot(refresh_declarations(value))),
                        "artifact-image-digest-contradiction")

    def test_well_formed_blocked_capture_is_inspection_ineligible(self):
        for drift in (False, True):
            value = blocked_fixture()
            if drift:
                value.update(finalSourceRecheck="changed-or-unavailable", blockers=["producer-source-changed"])
            retained = snapshot(value)
            self.assertIs(parse_capture(retained), retained)
            self.refuse(lambda: inspect_c115_observation_pins(retained), "artifact-literal-mismatch")

    def test_dirty_declarations_are_inspectable_but_source_eligibility_independently_refuses(self):
        sources = fixture_sources()
        source_value = fixture_capture(sources)
        value = capture_fixture()
        value.update(sourceCommit=COMMIT, producerSources=source_value["producerSources"])
        value["sources"][0]["sha256"] = source_value["sources"][0]["sha256"]
        registry = inspect_registry(snapshot([registry_entry()]))
        # First establish genuinely matching retained fixture byte receipts.
        clean = snapshot(value)
        self.assertEqual(len(inspect_sources(registry, clean, sources, COMMIT).sources), 19)
        for changed_source in (False, True):
            dirty = deepcopy(value)
            dirty.update(sourceDisposition="dirty-development", worktreeDirty=True)
            if changed_source:
                dirty["producerSources"][0]["modified"] = True
            result = self.inspection(dirty)
            self.assertEqual(result.capture.value["sourceDisposition"], "dirty-development")
            self.refuse(lambda: inspect_sources(registry, result.capture, sources, COMMIT),
                        "observed-clean-unchanged-capture-required")

    def test_structural_command_ledger_and_arbitrary_neutral_labels_prove_only_declarations(self):
        value = capture_fixture()
        # The independently authored fixture has simplified get-pods commands,
        # arbitrary source commit/session labels and an evidence-directory argument.
        self.assertEqual(value["commands"][1]["arguments"], ["get", "pods"])
        value["qualificationSessionId"] = "unregistered-fixture-session"
        value["producer"]["arguments"][5] = value["qualificationSessionId"]
        value["producer"]["argumentsSha256"] = fixture_hash(value["producer"]["arguments"])
        result = self.inspection(value)
        self.assertEqual(result.capture.value["sourceCommit"], "b" * 40)
        self.assertFalse(hasattr(result, "passed"))
        self.assertFalse(hasattr(result, "accepted_gate"))
        self.assertFalse(hasattr(result, "execution_handle"))

    def test_snapshot_schema_type_and_neutral_markers_remain_closed(self):
        for original in (None, b"{}", {}, [], object()):
            self.refuse(lambda: inspect_c115_observation_pins(original), "artifact-snapshot-required")
        for changed in ({"schemaVersion": "hexalith.access-telemetry.c1.evidence/v1"}, {"unknown": True},
                        {"producerStatus": "accepted"}, {"productionGatePassed": True}, {"gateStatus": "passed"},
                        {"independentDisposition": "accepted"}, {"resultCount": True}, {"profileIdentity": None}):
            self.refuse(lambda: inspect_c115_observation_pins(snapshot(dict(capture_fixture(), **changed))))
        value = capture_fixture()
        del value["target"]["appId"]
        self.refuse(lambda: inspect_c115_observation_pins(snapshot(value)), "artifact-field-set")

    def test_malformed_duplicate_secret_and_byte_budget_inputs_never_return_inspection(self):
        for raw in (b"{", b"\xef\xbb\xbf{}", b"\xff", b'{"duplicate":1,"duplicate":2}',
                    b"x" * (MAX_ARTIFACT_BYTES + 1)):
            self.refuse(lambda: inspect_c115_observation_pins(JsonSnapshot(raw)))
        value = capture_fixture()
        value["profileIdentity"] = "bearer private-test-credential"
        error = self.refuse(lambda: inspect_c115_observation_pins(snapshot(value)), "artifact-secret-shaped-content")
        self.assertIsNone(error.__cause__)

    def test_result_and_retained_nested_values_are_frozen(self):
        result = self.inspection(capture_fixture())
        for field, replacement in (("capture", None), ("pod_count", 100), ("pod_names", ("changed",))):
            with self.assertRaises(FrozenInstanceError):
                setattr(result, field, replacement)
        with self.assertRaises(FrozenInstanceError):
            result.capture.raw = b"changed"
        with self.assertRaises(TypeError):
            result.pod_names[0] = "changed"
        with self.assertRaises(TypeError):
            result.capture.value["observations"]["pods"][0]["runtimeVersion"] = "1.18.2"
        with self.assertRaises(TypeError):
            result.capture.value["observations"]["pods"][0]["actorTypes"][0] = "changed"
        self.assertNotIn("sourceCommit", repr(result))


if __name__ == "__main__":
    unittest.main()
