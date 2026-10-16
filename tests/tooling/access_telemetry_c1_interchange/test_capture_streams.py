"""Independent retained-output fixtures and counted dependency-denial evidence."""

from __future__ import annotations

from copy import deepcopy
from dataclasses import FrozenInstanceError, fields
import hashlib
import itertools
import json
import sys
import unittest
from unittest.mock import patch

import test_capture_commands as command_fixtures
import test_capture_semantics as semantic_fixtures
import access_telemetry_c1_capture_streams as stream_inspector
from access_telemetry_c1_capture_semantics import inspect_c115_command_declarations
from access_telemetry_c1_capture_streams import (
    C115CommandStreams, C115StreamInspection, inspect_c115_retained_streams,
)
from access_telemetry_c1_interchange import JsonSnapshot
from test_artifact_readers import fixture_ps_bytes, receipt, snapshot


ONE_MIB = 1_048_576
THIRTY_TWO_MIB = 33_554_432


def encoded(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":")).encode()


def pod_frame(pod):
    # Authored from the producer contract, not from its parser or projections.
    return [{"name": pod["pod"], "uid": pod["podUid"],
             "labels": {"app.kubernetes.io/name": pod["appId"]}},
            {"phase": "Running", "conditions": [{"type": "Ready", "status": "True"}],
             "containerStatuses": [
                 {"name": "lifecycle", "ready": True, "imageID": "containerd://sha256:" + "a" * 64},
                 {"name": "daprd", "ready": True, "imageID": pod["sidecarImageId"]},
             ]}]


def metadata_output(pod):
    return {"id": pod["appId"], "runtimeVersion": pod["runtimeVersion"],
            "scheduler": {"connectedAddresses": list(pod["schedulerConnectedAddresses"])},
            "actors": [{"type": actor, "count": 1} for actor in pod["actorTypes"]],
            "enabledFeatures": list(pod["enabledFeatures"])}


def bind(value, streams):
    """Rehash independent receipts/sources without production hashing helpers."""
    value = deepcopy(value)
    receipts = {stream.purpose: stream for stream in streams}
    for command in value["commands"]:
        stream = receipts[command["purpose"]]
        command.update(stdoutSha256=hashlib.sha256(stream.stdout).hexdigest(),
                       stderrSha256=hashlib.sha256(stream.stderr).hexdigest())
    for source in value["sources"]:
        parts = source["source"].split(":")
        if parts[0] == "kubectl" and parts[-1] in ("stdout", "stderr", "identity"):
            stream = receipts[":".join(parts[1:-1])]
            raw = stream.stderr if parts[-1] == "stderr" else stream.stdout
            source["sha256"] = hashlib.sha256(raw).hexdigest()
    return value


def fixture(names=("lifecycle-1", "lifecycle-2")):
    value = command_fixtures.command_fixture(names)
    selection = b"\n".join(encoded(pod_frame(pod)) for pod in value["observations"]["pods"]) + b"\n"
    outputs = {"current-context": b"owner@memories-qualification-27-4\n", "lifecycle-pods": selection,
               "lifecycle-pods-recheck": selection}
    for pod in value["observations"]["pods"]:
        name = pod["pod"]
        outputs["daprd-version:" + name] = b"1.18.1\n"
        outputs["metadata:" + name] = encoded(metadata_output(pod)) + b"\n"
        outputs["alpha-opt-in:" + name] = b"false\nfalse\n"
    streams = tuple(C115CommandStreams(command["purpose"], outputs[command["purpose"]], b"")
                    for command in value["commands"])
    return bind(value, streams), streams


def changed(value, streams, number, *, stdout=None, stderr=None):
    streams = list(streams)
    original = streams[number]
    streams[number] = C115CommandStreams(original.purpose,
                                         original.stdout if stdout is None else stdout,
                                         original.stderr if stderr is None else stderr)
    streams = tuple(streams)
    return bind(value, streams), streams


def many_fixture(count):
    value, _ = fixture(("lifecycle-1",))
    pods = value["observations"]["pods"]
    for number in range(2, count + 1):
        pods.append(dict(deepcopy(pods[0]), pod="lifecycle-" + str(number), podUid="fixture-uid-" + str(number)))
    names = tuple(pod["pod"] for pod in pods)
    value["commands"] = [receipt(list(arguments), purpose=purpose)
                         for purpose, arguments in command_fixtures.declared_argv(names)]
    value["resultCount"] = count
    value["sources"] = value["sources"][:1]
    for command in value["commands"]:
        purpose = command["purpose"]
        if purpose.startswith("metadata:"):
            pod = next(pod for pod in pods if purpose == "metadata:" + pod["pod"])
            projection = {"id": pod["appId"], "runtimeVersion": pod["runtimeVersion"],
                          "schedulerConnectedAddresses": pod["schedulerConnectedAddresses"],
                          "actorTypes": pod["actorTypes"], "enabledFeatures": pod["enabledFeatures"]}
            value["sources"].append({"source": "kubectl:" + purpose + ":allowlisted",
                                     "sha256": hashlib.sha256(fixture_ps_bytes(projection)).hexdigest()})
        else:
            suffixes = ("identity",) if purpose.startswith("lifecycle-pods") else ("stdout", "stderr")
            for suffix in suffixes:
                value["sources"].append({"source": "kubectl:" + purpose + ":" + suffix, "sha256": "0" * 64})
    command_fixtures.serial_times(value)
    semantic_fixtures.refresh_declarations(value)
    selection = b"".join(encoded(pod_frame(pod)) for pod in pods)
    outputs = {"current-context": b"owner@memories-qualification-27-4\n", "lifecycle-pods": selection,
               "lifecycle-pods-recheck": selection}
    for pod in pods:
        for stem, output in (("daprd-version", b"1.18.1\n"), ("metadata", encoded(metadata_output(pod))),
                             ("alpha-opt-in", b"false\nfalse\n")):
            outputs[stem + ":" + pod["pod"]] = output
    streams = tuple(C115CommandStreams(purpose, outputs[purpose], b"")
                    for purpose, arguments in command_fixtures.declared_argv(names))
    return bind(value, streams), streams


class CaptureStreamTests(unittest.TestCase):
    refuse = semantic_fixtures.CaptureSemanticTests.refuse

    @classmethod
    def setUpClass(cls):
        # Reuse the counted harness with the new inspector as the alias target.
        # Alias originals are bound before any denial patches are installed.
        with patch.object(semantic_fixtures, "capture_semantics", stream_inspector):
            semantic_fixtures.CaptureSemanticTests.setUpClass.__func__(cls)

    def setUp(self):
        with patch.object(semantic_fixtures, "capture_semantics", stream_inspector):
            semantic_fixtures.CaptureSemanticTests.setUp(self)

    def inspect(self, value, streams):
        retained = snapshot(value)
        declaration = inspect_c115_command_declarations(retained)
        result = inspect_c115_retained_streams(retained, streams)
        self.assertIs(type(result), C115StreamInspection)
        self.assertIs(result.capture, retained)
        self.assertIs(result.streams, streams)
        self.assertEqual(result.pod_count, declaration.pod_count)
        self.assertEqual(result.pod_names, declaration.pod_names)
        self.assertEqual(result.command_count, declaration.command_count)
        self.assertEqual(tuple(field.name for field in fields(result)),
                         ("capture", "streams", "pod_count", "pod_names", "command_count"))
        self.assertEqual(result.capture.value["gateStatus"], "not-evaluated")
        self.assertFalse(result.capture.value["productionGatePassed"])
        self.assertEqual(result.capture.value["productionLifecycleWrites"], "not-evaluated")
        self.assertEqual(result.capture.value["independentDisposition"], "pending")
        for prohibited in ("passed", "accepted", "authority", "execution_handle", "accepted_gate"):
            self.assertFalse(hasattr(result, prohibited))
        for original, kept in zip(streams, result.streams):
            self.assertIs(original, kept)
            self.assertIs(original.stdout, kept.stdout)
            self.assertIs(original.stderr, kept.stderr)
        return result

    def contradiction(self, value, streams, code="c115-stream-observation-mismatch"):
        retained = snapshot(value)
        self.assertIs(inspect_c115_command_declarations(retained).capture, retained)
        before = retained.raw
        error = self.refuse(lambda: inspect_c115_retained_streams(retained, streams), code)
        self.assertIsNone(error.__cause__)
        self.assertIsNone(error.__context__)
        self.assertEqual(retained.raw, before)

    def test_barriers_detect_prebound_and_ambient_attempts_without_live_calls(self):
        with patch.object(semantic_fixtures, "capture_semantics", stream_inspector):
            semantic_fixtures.CaptureSemanticTests.test_barriers_detect_prebound_aliases_and_filesystem_attempts_without_live_calls(self)

    def test_one_two_three_pods_in_every_initial_observation_order(self):
        for count in (1, 2, 3):
            for names in itertools.permutations(("lifecycle-1", "lifecycle-2", "lifecycle-3")[:count]):
                with self.subTest(names=names):
                    result = self.inspect(*fixture(names))
                    self.assertEqual(result.command_count, 3 * count + 3)
        self.inspect(*fixture(("lifecycle-2",)))
        self.inspect(*fixture(("lifecycle-3",)))

    def test_missing_duplicate_surplus_reordered_and_mutable_collections_refuse(self):
        value, streams = fixture()
        retained = snapshot(value)
        for proposed in (list(streams), iter(streams), None, {}, b"streams"):
            self.refuse(lambda: inspect_c115_retained_streams(retained, proposed), "c115-stream-tuple-required")
        for index in range(len(streams)):
            for proposed in (streams[:index] + streams[index + 1:], streams[:index] + (streams[index],) + streams[index:],
                             streams[:index] + (streams[0],) + streams[index + 1:]):
                if proposed == streams:
                    continue
                self.refuse(lambda: inspect_c115_retained_streams(retained, proposed), "c115-stream-sequence-mismatch")
        self.refuse(lambda: inspect_c115_retained_streams(retained, streams[::-1]), "c115-stream-sequence-mismatch")
        self.refuse(lambda: inspect_c115_retained_streams(retained, (object(),) + streams[1:]), "c115-stream-tuple-required")

    def test_pair_types_are_closed_and_subclasses_refuse(self):
        class MutableBytes(bytes):
            pass

        class StreamSubclass(C115CommandStreams):
            pass

        class TupleSubclass(tuple):
            pass

        for raw in (bytearray(b"data"), memoryview(b"data"), "data", None, MutableBytes(b"data")):
            for stdout, stderr in ((raw, b""), (b"data", raw)):
                self.refuse(lambda: C115CommandStreams("current-context", stdout, stderr), "c115-stream-bytes-required")
        self.refuse(lambda: C115CommandStreams(None, b"", b""), "c115-stream-purpose-required")
        value, streams = fixture()
        retained = snapshot(value)
        for proposed in (TupleSubclass(streams), (StreamSubclass("current-context", streams[0].stdout, b""),) + streams[1:]):
            self.refuse(lambda: inspect_c115_retained_streams(retained, proposed), "c115-stream-tuple-required")

    def test_surplus_tuple_cardinality_refuses_before_any_element_inspection(self):
        value, streams = fixture()
        retained = snapshot(value)
        inspected = []

        def counted_type(value):
            if type(value) is C115CommandStreams:
                inspected.append(value)
            return type(value)

        for proposed in (streams + streams * 10_000, (object(),) + streams):
            with patch.object(stream_inspector, "type", side_effect=counted_type, create=True):
                self.refuse(lambda: inspect_c115_retained_streams(retained, proposed), "c115-stream-sequence-mismatch")
            self.assertEqual(inspected, [])

    def test_each_unrehashed_stdout_and_stderr_byte_change_refuses(self):
        value, streams = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        retained = snapshot(value)
        for number in range(len(streams)):
            for name in ("stdout", "stderr"):
                with self.subTest(command=number, stream=name):
                    _, altered = changed(value, streams, number, **{name: getattr(streams[number], name) + b" "})
                    self.refuse(lambda: inspect_c115_retained_streams(retained, altered), "c115-stream-hash-mismatch")

    def test_exact_untrimmed_capture_and_stream_bytes_are_retained(self):
        value, streams = fixture()
        for number, stream in enumerate(streams):
            value, streams = changed(value, streams, number, stdout=b"\xe2\x80\x83\r\n " + stream.stdout + b"\t\xc2\x85",
                                     stderr="safe café 雪 diagnostic\r\n".encode())
        raw = (" \r\n" + json.dumps(value, ensure_ascii=True, indent=3) + "\r\n\t").encode()
        retained = JsonSnapshot(raw)
        result = inspect_c115_retained_streams(retained, streams)
        self.assertIs(result.capture, retained)
        self.assertIs(result.streams, streams)
        self.assertEqual(result.capture.raw, raw)
        self.assertEqual(result.capture.sha256, hashlib.sha256(raw).hexdigest())
        self.assertNotIn("café", repr(result))
        self.assertNotIn("café", repr(result.streams))
        self.assertNotIn("owner@memories-qualification-27-4", repr(result))

    def test_rehashed_foreign_context_and_runtime_outputs_refuse(self):
        value, streams = fixture()
        for number, output in ((0, b"other@cluster\n"), (0, b"OWNER@MEMORIES-QUALIFICATION-27-4\n"), (0, b"owner@memories-qualification-27-4\x1c"),
                               (2, b"1.18.2\n"), (5, b"1.18.1+build\n"), (5, b"diagnostic\n1.18.1\n")):
            with self.subTest(command=number):
                self.contradiction(*changed(value, streams, number, stdout=output))

    def test_initial_running_pod_order_missing_surplus_and_duplicates_refuse(self):
        value, streams = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        frames = [pod_frame(pod) for pod in value["observations"]["pods"]]
        extra = deepcopy(frames[-1])
        extra[0].update(name="lifecycle-unreported", uid="unreported-uid")
        variations = (frames[::-1], frames[:-1], frames + [extra], frames + [frames[-1]],
                      [frames[0]], [])
        for variation in variations:
            with self.subTest(count=len(variation)):
                self.contradiction(*changed(value, streams, 1, stdout=b"".join(encoded(frame) for frame in variation) or b" \n"),
                                   code="c115-stream-observation-mismatch" if variation else "c115-stream-invalid-json")

    def test_every_initial_pod_uid_label_ready_deletion_and_images_are_checked(self):
        value, streams = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        mutations = (
            lambda frame: frame[0].update(uid="foreign-uid"),
            lambda frame: frame[0].update(name="foreign-pod"),
            lambda frame: frame[0]["labels"].update({"app.kubernetes.io/name": "foreign-app"}),
            lambda frame: frame[0].update(deletionTimestamp="2026-10-13T12:00:00Z"),
            lambda frame: frame[1]["conditions"][0].update(status="False"),
            lambda frame: frame[1]["conditions"].append({"type": "Ready", "status": "True"}),
            lambda frame: frame[1]["containerStatuses"][0].update(ready="true"),
            lambda frame: frame[1]["containerStatuses"][1].update(ready=False),
            lambda frame: frame[1]["containerStatuses"][1].update(imageID="changed-prefix-" + semantic_fixtures.INDEX),
            lambda frame: frame[1]["containerStatuses"][0].update(imageID="unhashed-image"),
            lambda frame: frame[1]["containerStatuses"].pop(0),
            lambda frame: frame[1]["containerStatuses"].append(deepcopy(frame[1]["containerStatuses"][1])),
            lambda frame: frame[1]["containerStatuses"][0].update(name="Lifecycle"),
        )
        for number in range(3):
            for mutate in mutations:
                frames = [pod_frame(pod) for pod in value["observations"]["pods"]]
                mutate(frames[number])
                with self.subTest(pod=number, mutation=mutate):
                    self.contradiction(*changed(value, streams, 1, stdout=b"".join(encoded(frame) for frame in frames)))

    def test_framing_requires_every_exact_two_object_frame(self):
        value, streams = fixture()
        frame = pod_frame(value["observations"]["pods"][0])
        malformed = (encoded({"items": [frame]}), encoded(frame[:1]), encoded(frame + [{}]), encoded([[], frame[1]]),
                     encoded([frame[0], None]), encoded([{}, frame[1]]), encoded([frame[0], {}]),
                     encoded(frame) + b"garbage", encoded(frame) + b"[{}", encoded(frame) + b", " + encoded(frame))
        for output in malformed:
            with self.subTest(output=output[:15]):
                self.contradiction(*changed(value, streams, 1, stdout=output), code=None)

    def test_pending_identity_and_incidental_fields_do_not_change_running_results(self):
        value, streams = fixture()
        frames = [pod_frame(pod) for pod in value["observations"]["pods"]]
        pending = [{"name": "unobserved-pending", "uid": "pending-uid"},
                   {"phase": "Pending", "conditions": [], "containerStatuses": []}]
        for frame in frames:
            frame[0].update(annotations={"note": "harmless"}, creationTimestamp="2026-10-13T12:00:00Z",
                            deletionTimestamp=None, numbers=[1.25, -2.0, 1e20])
            frame[1]["conditions"][0]["lastTransitionTime"] = "2026-10-13T12:00:00Z"
        value, streams = changed(value, streams, 1, stdout=b"".join(encoded(frame) for frame in [pending, *frames]))
        pending[0].update(name="changed-pending", uid=12, labels=None)
        pending[1].pop("conditions")
        pending[1].pop("containerStatuses")
        for frame in frames:
            frame[0]["annotations"]["note"] = "changed incidental annotation"
            frame[1]["conditions"][0]["lastTransitionTime"] = "2026-10-13T12:30:00Z"
            frame[1]["containerStatuses"].reverse()
        value, streams = changed(value, streams, -1, stdout=b"".join(encoded(frame) for frame in [*frames[::-1], pending]))
        self.inspect(value, streams)

    def test_initial_pending_frames_still_require_phase_and_status_arrays(self):
        value, streams = fixture()
        for status in ({"phase": "Pending"}, {"phase": 7, "conditions": [], "containerStatuses": []},
                       {"phase": "Pending", "conditions": {}, "containerStatuses": []}):
            self.contradiction(*changed(value, streams, 1, stdout=streams[1].stdout + encoded([{"name": "pending"}, status])))

    def test_rechecked_stable_images_and_every_running_identity_are_bound(self):
        value, streams = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        mutations = (
            lambda frame: frame[0].update(uid="changed-uid"),
            lambda frame: frame[0].update(name="changed-pod"),
            lambda frame: frame[0]["labels"].update({"app.kubernetes.io/name": "changed-app"}),
            lambda frame: frame[0].update(deletionTimestamp="now"),
            lambda frame: frame[1].update(phase="Pending"),
            lambda frame: frame[1]["conditions"][0].update(status="Unknown"),
            lambda frame: frame[1]["containerStatuses"][0].update(imageID="docker://sha256:" + "a" * 64),
            lambda frame: frame[1]["containerStatuses"][1].update(imageID="docker://" + semantic_fixtures.CHILD),
            lambda frame: frame[1]["containerStatuses"][0].update(ready=False),
            lambda frame: frame[1]["containerStatuses"][1].update(ready="true"),
        )
        for number in range(3):
            for mutate in mutations:
                frames = [pod_frame(pod) for pod in value["observations"]["pods"]]
                mutate(frames[number])
                with self.subTest(pod=number, mutation=mutate):
                    self.contradiction(*changed(value, streams, -1, stdout=b"".join(encoded(frame) for frame in frames)), code=None)

    def test_missing_extra_and_duplicate_recheck_frames_refuse(self):
        value, streams = fixture()
        frames = [pod_frame(pod) for pod in value["observations"]["pods"]]
        extra = deepcopy(frames[0])
        extra[0].update(name="unreported", uid="unreported-uid")
        for variation in (frames[:-1], frames + [extra], frames + [frames[0]]):
            self.contradiction(*changed(value, streams, -1, stdout=b"".join(encoded(frame) for frame in variation)), code=None)

    def test_metadata_foreign_id_runtime_scheduler_actor_and_features_refuse_for_later_pods(self):
        value, streams = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        mutations = (
            lambda metadata: metadata.update(id="foreign-app"),
            lambda metadata: metadata.update(runtimeVersion="1.18.2"),
            lambda metadata: metadata.update(runtimeVersion=" 1.18.1 "),
            lambda metadata: metadata["scheduler"].update(connectedAddresses=["other-scheduler:50006"]),
            lambda metadata: metadata["scheduler"].update(connected_addresses=["scheduler-0:50006"]),
            lambda metadata: metadata["scheduler"].pop("connectedAddresses"),
            lambda metadata: metadata.update(actors=[{"type": "OtherActor"}]),
            lambda metadata: metadata.update(enabledFeatures=["Other.Feature"]),
            lambda metadata: metadata.update(actors=[]),
            lambda metadata: metadata["actors"].append(deepcopy(metadata["actors"][0])),
            lambda metadata: metadata["scheduler"]["connectedAddresses"].append("scheduler-0:50006"),
            lambda metadata: metadata["enabledFeatures"].append("Actor.Reentrancy"),
            lambda metadata: metadata.update(enabledFeatures="Actor.Reentrancy"),
            lambda metadata: metadata.update(scheduler=[]),
            lambda metadata: metadata.update(actors=[{"type": 1}]),
        )
        for number, pod in enumerate(value["observations"]["pods"]):
            for mutate in mutations:
                metadata = metadata_output(pod)
                mutate(metadata)
                with self.subTest(pod=number, mutation=mutate):
                    self.contradiction(*changed(value, streams, 3 + 3 * number, stdout=encoded(metadata)))

    def test_metadata_arrays_compare_exact_membership_and_either_scheduler_spelling(self):
        value, streams = fixture()
        for pod in value["observations"]["pods"]:
            pod["schedulerConnectedAddresses"] += ["Scheduler-1:50006"]
            pod["actorTypes"] += ["OtherActor"]
            pod["enabledFeatures"] += ["Other.Feature"]
        semantic_fixtures.refresh_declarations(value)
        for number, pod in enumerate(value["observations"]["pods"]):
            metadata = metadata_output(pod)
            metadata["scheduler"] = {"connected_addresses": metadata["scheduler"]["connectedAddresses"][::-1]}
            metadata["actors"].reverse()
            metadata["enabledFeatures"].reverse()
            metadata.update(extra={"safeNumber": 1.125, "nested": [{"a": None}]})
            value, streams = changed(value, streams, 3 + 3 * number, stdout=encoded(metadata))
        self.inspect(value, streams)
        metadata = metadata_output(value["observations"]["pods"][1])
        metadata["scheduler"]["connectedAddresses"][1] = "scheduler-1:50006"
        self.contradiction(*changed(value, streams, 6, stdout=encoded(metadata)))

    def test_empty_feature_arrays_and_extra_finite_numbers_are_supported(self):
        value, streams = fixture()
        for pod in value["observations"]["pods"]:
            pod["enabledFeatures"] = []
        for number, pod in enumerate(value["observations"]["pods"]):
            metadata = metadata_output(pod)
            metadata.update(extra=[0, -1, 1.25, 1e30], token=None, password="", secret=[], credential={},
                            prose="{harmless}", bracket="[INFO] harmless")
            metadata["{"] = "a harmless field name"
            value, streams = changed(value, streams, 3 + number * 3, stdout=encoded(metadata))
        semantic_fixtures.refresh_declarations(value)
        self.inspect(value, streams)

    def test_rehashed_alpha_contradictions_invalid_values_and_line_counts_refuse(self):
        value, streams = fixture()
        for number in (4, 7):
            for raw in (b"true\ntrue\n", b"false\ntrue\n", b"true\nfalse\n", b"0\n0\n", b"__MISSING__\nfalse\n",
                        b"false\nfalse\nfalse\n", b"false false", b"false\rfalse", b"false\n", b"false\nfal\x00se"):
                with self.subTest(command=number, raw=raw):
                    self.contradiction(*changed(value, streams, number, stdout=raw))

    def test_alpha_boolean_case_whitespace_crlf_and_boundary_nul_match_producer(self):
        value, streams = fixture()
        for number in (4, 7):
            value, streams = changed(value, streams, number, stdout=b"\r\n\x00 FaLsE \x00\r\n\x00 FALSE \x00\r\n")
        self.inspect(value, streams)

    def test_affirmative_alpha_pairs_succeed_for_complete_one_two_three_pod_captures(self):
        for pair in ((True, True), (False, True)):
            for names in (("lifecycle-3",), ("lifecycle-2", "lifecycle-1"),
                          ("lifecycle-3", "lifecycle-1", "lifecycle-2")):
                value, streams = fixture(names)
                alpha = {"componentIsAlpha": pair[0], "allowAlphaComponent": pair[1]}
                value["observations"]["alphaOptIn"] = dict(alpha)
                for pod in value["observations"]["pods"]:
                    pod["alphaOptIn"] = dict(alpha)
                for number in range(len(names)):
                    output = (" \x00 " + str(pair[0]).upper() + " \x00\r\n " + str(pair[1]).lower() + " \r\n").encode()
                    value, streams = changed(value, streams, 4 + 3 * number, stdout=output)
                with self.subTest(alpha=pair, names=names):
                    result = self.inspect(value, streams)
                    self.assertEqual(dict(result.capture.value["observations"]["alphaOptIn"]), alpha)

    def test_invalid_utf8_and_surrogates_are_refused_on_both_streams(self):
        value, streams = fixture()
        for number in (0, 3, 7, 8):
            for name in ("stdout", "stderr"):
                for raw, code in ((b"\xff", "c115-stream-invalid-utf8"), (b"\xed\xa0\x80", "c115-stream-invalid-utf8"),
                                  (b'"\\ud800"', "c115-stream-unpaired-unicode-surrogate")):
                    self.contradiction(*changed(value, streams, number, **{name: raw}), code=code)

    def test_malformed_duplicate_decoded_keys_and_nonfinite_json_are_refused_on_both_streams(self):
        value, streams = fixture()
        for name in ("stdout", "stderr"):
            for raw, code in ((b'{"bad":}', "c115-stream-invalid-json"),
                              (b'{"a":1,"\\u0061":2}', "c115-stream-duplicate-json-field"),
                              (b'notice {"a":1,"a":2}', "c115-stream-duplicate-json-field"),
                              (b'notice "unmatched {"a":1,"a":2}', "c115-stream-duplicate-json-field"),
                              (b'{"extra":NaN}', "c115-stream-nonfinite-json-number"),
                              (b'{"extra":Infinity}', "c115-stream-nonfinite-json-number"),
                              (b'{"extra":1e9999}', "c115-stream-nonfinite-json-number"),
                              (b'[{"bad":}]', "c115-stream-invalid-json")):
                with self.subTest(stream=name, raw=raw):
                    self.contradiction(*changed(value, streams, 3, **{name: raw}), code=code)

    def test_depth_thirty_is_supported_and_thirty_one_refuses_in_discarded_fields_and_stderr(self):
        value, streams = fixture()
        metadata = metadata_output(value["observations"]["pods"][0])
        metadata["extra"] = "leaf"
        for _ in range(29):
            metadata["extra"] = [metadata["extra"]]
        value, streams = changed(value, streams, 3, stdout=encoded(metadata))
        self.inspect(value, streams)
        metadata["extra"] = [metadata["extra"]]
        self.contradiction(*changed(value, streams, 3, stdout=encoded(metadata)), code="c115-stream-json-depth-exceeded")
        for name in ("stdout", "stderr"):
            raw = b"notice " + b"[" * 31 + b"0" + b"]" * 31
            self.contradiction(*changed(value, streams, 3, **{name: raw}), code="c115-stream-json-depth-exceeded")
            raw = b'notice "unmatched ' + b"[" * 31 + b"0" + b"]" * 31
            self.contradiction(*changed(value, streams, 3, **{name: raw}), code="c115-stream-json-depth-exceeded")

    def test_overlapping_diagnostic_fragments_cannot_hide_later_pairs_duplicates_or_malformed_json(self):
        value, streams = fixture()
        fragments = (
            (b'{"name":"token","value":"private-test-credential"}', "c115-stream-secret-shaped-content"),
            (b'{"NAME":"token","VALUE":[]}', "c115-stream-secret-shaped-content"),
            (b'{"a":1,"\\u0061":2}', "c115-stream-duplicate-json-field"),
            (b'{"bad":}', "c115-stream-invalid-json"),
            (b'{"bad"}', "c115-stream-invalid-json"),
            (b'[{"bad":}]', "c115-stream-invalid-json"),
        )
        for prefix in (b'notice "unmatched {} ', b'notice "unmatched {} {} ', b'notice "path\\q {} '):
            for fragment, code in fragments:
                for name in ("stdout", "stderr"):
                    with self.subTest(prefix=prefix, fragment=fragment, stream=name):
                        self.contradiction(*changed(value, streams, 3, **{name: prefix + fragment}), code=code)
        for prose in (b'notice "{" and "ordinary text"', b'notice "[" and "ordinary text"',
                      b'notice "literal {"', b'notice "literal ["', b'notice "literal {} []"'):
            self.inspect(*changed(value, streams, 3, stderr=prose))

    def test_standalone_and_array_nonfinite_stderr_refuse_without_parsing_prose(self):
        value, streams = fixture()
        for raw in (b"NaN", b"Infinity", b"-Infinity", b"1e9999", b"[NaN]", b"[Infinity]", b"[-Infinity]", b"[1e9999]"):
            self.contradiction(*changed(value, streams, -1, stderr=raw), code="c115-stream-nonfinite-json-number")
        for raw in (b"NaN was unavailable", b"Infinity is a diagnostic label", b"value 1e9999 is untrusted text",
                    b'notice "NaN" "Infinity" "1e9999"'):
            self.inspect(*changed(value, streams, -1, stderr=raw))

    def test_quoted_non_json_diagnostic_backslashes_preserve_producer_compatibility(self):
        value, streams = fixture()
        for raw in (br'notice "path\q"', br'notice "C:\work\queue"', br'notice "path\q" {} "[INFO]"',
                    br'notice "path\q {"reason":"harmless"}', br'notice "literal {" and "plain prose"'):
            self.inspect(*changed(value, streams, 3, stderr=raw))
        self.contradiction(*changed(value, streams, 3, stderr=br'notice {"path":"bad\q"}'), code="c115-stream-invalid-json")

    def test_integer_lexemes_have_deterministic_signed_boundaries_under_ambient_limits(self):
        value, streams = fixture()
        metadata = encoded(metadata_output(value["observations"]["pods"][0]))
        old_limit = sys.get_int_max_str_digits()
        self.addCleanup(sys.set_int_max_str_digits, old_limit)
        for ambient in (0, 640, 4300):
            sys.set_int_max_str_digits(ambient)
            with patch("sys.get_int_max_str_digits", side_effect=AssertionError("ambient integer limit read")):
                for lexeme in (b"9" * 380, b"-" + b"9" * 379):
                    output = metadata[:-1] + b',"integer":' + lexeme + b"}"
                    self.inspect(*changed(value, streams, 3, stdout=output))
                for lexeme in (b"9" * 381, b"-" + b"9" * 380, b"9" * 5000):
                    output = metadata[:-1] + b',"integer":' + lexeme + b"}"
                    self.contradiction(*changed(value, streams, 3, stdout=output), code="c115-stream-integer-lexeme-too-long")
                    self.contradiction(*changed(value, streams, 3, stderr=lexeme), code="c115-stream-integer-lexeme-too-long")

    def test_credential_pairs_use_case_insensitive_keys_and_non_string_non_null_values(self):
        value, streams = fixture()
        for name_key, value_key in (("name", "value"), ("NAME", "VALUE"), ("Name", "VaLuE")):
            for child in ([], {}, False, 0, [None], {"safe": None}, "private-test-credential"):
                for name in ("stdout", "stderr"):
                    raw = encoded({name_key: "token", value_key: child})
                    self.contradiction(*changed(value, streams, 3, **{name: b'notice "unmatched {} ' + raw}),
                                       code="c115-stream-secret-shaped-content")
            for child in (None, ""):
                self.inspect(*changed(value, streams, 3, stderr=encoded({name_key: "token", value_key: child})))
        self.inspect(*changed(value, streams, 3, stderr=encoded({"token": [], "password": {}, "secret": None, "credential": ""})))

    def test_quoted_diagnostic_candidate_work_is_linear_without_a_clock(self):
        value, streams = fixture()

        class CountedPattern:
            def __init__(self, pattern, work):
                self.pattern, self.work = pattern, work

            def finditer(self, text, position=0):
                self.work.append(len(text) - position)
                return self.pattern.finditer(text, position)

            def search(self, text, position=0):
                self.work.append(len(text) - position)
                return self.pattern.search(text, position)

            def match(self, text, position=0):
                self.work.append(len(text) - position)
                return self.pattern.match(text, position)

        for repetitions in (2048, 4096):
            for raw in (b'"a" ' * repetitions, b'"' + b'{} ' * repetitions + b'"'):
                work = []
                altered_value, altered_streams = changed(value, streams, 3, stderr=raw)
                byte_count = sum(len(stream.stdout) + len(stream.stderr) for stream in altered_streams)
                with patch.object(stream_inspector, "_STRING_TOKEN", CountedPattern(stream_inspector._STRING_TOKEN, work)), \
                        patch.object(stream_inspector, "_JSON_START", CountedPattern(stream_inspector._JSON_START, work)):
                    self.inspect(altered_value, altered_streams)
                self.assertLessEqual(sum(work), 8 * byte_count)

    def test_raw_unicode_json_string_and_discarded_credentials_refuse_every_stream(self):
        value, streams = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        unsafe = (
            b"bearer private-test-credential", b"password=private-test-credential",
            b"hvs.private_test_credential", b"postgresql://owner:private-test-credential@db",
            b"C1_SECRET_CANARY_PRIVATE_TEST", b'notice {"\\u0074oken":"private-test-credential"}',
            b'notice {"name":"\\u0074oken","value":"private-test-credential"}',
            b'{"ignored":{"api_key":"private-test-credential"}}',
            b'{"ignored":"{\\"token\\":\\"private-test-credential\\"}"}',
            b'"postgresql:\\/\\/owner:private-test-credential@db"',
            b'notice "\\u0070assword" : false', b'{"authorization":[0]}',
            b'{"secret":{"nested":null}}', b'{"credential":0}',
        )
        for number in range(len(streams)):
            for name in ("stdout", "stderr"):
                for raw in unsafe:
                    with self.subTest(command=number, stream=name, raw=raw[:20]):
                        self.contradiction(*changed(value, streams, number, **{name: raw}), code="c115-stream-secret-shaped-content")

    def test_safe_nonempty_stderr_diagnostics_and_fragments_are_allowed(self):
        value, streams = fixture()
        for number in range(len(streams)):
            value, streams = changed(value, streams, number,
                                     stderr=b'[INFO] harmless diagnostics {"reason":"retry observed","duration":0.125}\n')
        self.inspect(value, streams)

    def test_each_stream_limit_includes_whitespace_and_exact_limit_is_allowed(self):
        value, streams = fixture()
        for name in ("stdout", "stderr"):
            for number in (0, len(streams) - 1):
                self.contradiction(*changed(value, streams, number, **{name: b" " * (ONE_MIB + 1)}),
                                   code="c115-stream-byte-budget-exceeded")
        value, streams = changed(value, streams, 0, stdout=streams[0].stdout + b" " * (ONE_MIB - len(streams[0].stdout)),
                                 stderr=b"x" * ONE_MIB)
        self.inspect(value, streams)

    def test_capture_limit_preserves_exact_allowed_bytes(self):
        value, streams = fixture()
        original = snapshot(value).raw
        raw = b" " * (ONE_MIB - len(original)) + original
        result = inspect_c115_retained_streams(JsonSnapshot(raw), streams)
        self.assertEqual(len(result.capture.raw), ONE_MIB)
        self.assertEqual(result.capture.raw, raw)
        self.refuse(lambda: inspect_c115_retained_streams(JsonSnapshot(b" " + raw), streams), "artifact-byte-budget-exceeded")

    def test_aggregate_budget_counts_capture_and_each_repeated_stream_occurrence(self):
        value, streams = many_fixture(11)
        identical = b"x" * ONE_MIB
        streams = tuple(C115CommandStreams(stream.purpose, stream.stdout, identical) for stream in streams)
        value = bind(value, streams)
        self.assertEqual(len(streams), 36)
        self.contradiction(value, streams, code="c115-stream-aggregate-byte-budget-exceeded")

    def test_exact_aggregate_limit_is_allowed_and_one_more_byte_refuses(self):
        value, streams = many_fixture(5)
        remaining = THIRTY_TWO_MIB - len(snapshot(value).raw) - sum(len(stream.stdout) for stream in streams)
        expanded = []
        for stream in streams:
            stderr_size = min(remaining, ONE_MIB)
            remaining -= stderr_size
            stdout_padding = min(remaining, ONE_MIB - len(stream.stdout))
            remaining -= stdout_padding
            expanded.append(C115CommandStreams(stream.purpose, stream.stdout + b" " * stdout_padding, b"x" * stderr_size))
        self.assertEqual(remaining, 0)
        streams = tuple(expanded)
        value = bind(value, streams)
        self.assertEqual(len(snapshot(value).raw) + sum(len(stream.stdout) + len(stream.stderr) for stream in streams), THIRTY_TWO_MIB)
        self.inspect(value, streams)
        last = streams[-1]
        self.contradiction(*changed(value, streams, len(streams) - 1, stderr=last.stderr + b"x"),
                           code="c115-stream-aggregate-byte-budget-exceeded")

    def test_inherited_strict_declaration_refusals_are_preserved(self):
        value, streams = fixture()
        for original in (None, b"{}", {}, [], object()):
            self.refuse(lambda: inspect_c115_retained_streams(original, streams), "artifact-snapshot-required")
        value["commands"][6]["arguments"][5] = "other-pod"
        command_fixtures.rehash(value["commands"][6])
        retained = snapshot(value)
        error = self.refuse(lambda: inspect_c115_command_declarations(retained), "c115-command-arguments-mismatch")
        self.refuse(lambda: inspect_c115_retained_streams(retained, streams), str(error))

    def test_frozen_results_pairs_and_nested_snapshot_expose_no_content_or_authority(self):
        result = self.inspect(*fixture())
        for attribute, replacement in (("capture", None), ("streams", ()), ("pod_count", 99),
                                       ("pod_names", ("changed",)), ("command_count", 99)):
            with self.assertRaises(FrozenInstanceError):
                setattr(result, attribute, replacement)
        for attribute in ("purpose", "stdout", "stderr"):
            with self.assertRaises(FrozenInstanceError):
                setattr(result.streams[0], attribute, b"changed")
        with self.assertRaises(TypeError):
            result.streams[0] = result.streams[1]
        with self.assertRaises(TypeError):
            result.capture.value["observations"]["pods"][0]["pod"] = "changed"
        self.assertNotIn("sha256:", repr(result))
        self.assertNotIn("false", repr(result.streams))
        self.assertNotIn("DAPR_API_TOKEN", repr(result))


class CaptureStreamProducerTests(unittest.TestCase):
    def test_actual_offline_producer_outputs_match_framing_trimming_diagnostics_and_rechecks(self):
        # The existing fixture owns fake kubectl and disposable capture files.
        # Instrument its fake transport only, retaining exactly the bytes the
        # unchanged PowerShell producer observes. No real cluster is contacted.
        sys.path.insert(0, str(semantic_fixtures.REPO_ROOT / "tests/tooling/access_telemetry_c1"))
        import pg2_runtime_control_plane_identity_test as producer_fixtures

        original_writer = producer_fixtures.write_fake_kubectl

        def retaining_writer(directory):
            original_writer(directory)
            fake = directory / "fake_kubectl.py"
            prefix = '''import atexit, io
_stdout, _stderr = sys.stdout, sys.stderr
sys.stdout = io.TextIOWrapper(io.BytesIO(), encoding="utf-8", write_through=True)
sys.stderr = io.TextIOWrapper(io.BytesIO(), encoding="utf-8", write_through=True)
def retain_outputs():
    for suffix, stream, destination in (("stdout", sys.stdout, _stdout), ("stderr", sys.stderr, _stderr)):
        raw = stream.buffer.getvalue()
        (log_path.parent / ("retained-" + str(len(prior_calls)) + "." + suffix)).write_bytes(raw)
        destination.buffer.write(raw)
        destination.buffer.flush()
atexit.register(retain_outputs)
'''
            fake.write_text(fake.read_text().replace("args = sys.argv[1:]", prefix + "\nargs = sys.argv[1:]", 1))

        harness = producer_fixtures.Pg2RuntimeControlPlaneIdentityTests()
        harness.setUp()
        self.addCleanup(harness.doCleanups)
        for count in (1, 2, 3):
            scenario = deepcopy(harness.base)
            alpha_pair = (False, False) if count == 1 else ((False, True) if count == 2 else (True, True))
            for number in range(2, count + 1):
                additional = deepcopy(scenario["pods"]["items"][0])
                name = harness.pod + "-extra-" + str(number)
                additional["metadata"].update(name=name, uid="fixture-extra-uid-" + str(number))
                scenario["pods"]["items"].append(additional)
                for field in ("metadata", "daprdVersions", "alphaOptIn"):
                    scenario[field][name] = deepcopy(scenario[field][harness.pod])
            scenario["context"] = "\u2003owner@memories-qualification-27-4\u0085"
            for name, metadata in scenario["metadata"].items():
                metadata["actors"] += [{"type": "zActor", "count": 2.5}, {"type": "aActor", "count": 0.125}]
                metadata["enabledFeatures"] += ["z.Feature", "AFeature", "a-feature"]
                metadata["scheduler"]["connected_addresses"] = metadata["scheduler"].pop("connectedAddresses")[::-1]
                metadata["extra"] = {"fraction": 1.25, "exponent": 1e20, "prose": "{harmless}", "note": "[INFO] harmless",
                                     "integerMax": int("9" * 380), "negativeIntegerMax": -int("9" * 379)}
                scenario["daprdVersions"][name] = "\u2003 1.18.1 \u0085"
                scenario["alphaOptIn"][name] = [" \x00 " + str(alpha_pair[0]).upper() + " \x00 ", " " + str(alpha_pair[1]).lower() + " "]
            scenario["stderrPurpose"] = "metadata"
            scenario["stderr"] = '[INFO] safe diagnostic "path\\q" {"duration":1.25,"detail":"harmless"}'
            scenario["pods"]["items"].reverse()
            scenario["podsAfter"] = deepcopy(scenario["pods"])
            scenario["podsAfter"]["items"].reverse()
            for pod in scenario["podsAfter"]["items"]:
                pod["metadata"].update(annotations={"note": "harmless"}, creationTimestamp="2026-10-13T12:00:00Z")
                pod["status"]["conditions"][0]["lastTransitionTime"] = "2026-10-13T12:00:00Z"
                pod["status"]["containerStatuses"].reverse()
            scenario["podsRaw"] = "\u2003" + "".join(json.dumps([pod["metadata"], pod["status"]]) for pod in scenario["pods"]["items"]) + "\u0085"
            scenario["podsAfterRaw"] = " ".join(json.dumps([pod["metadata"], pod["status"]]) for pod in scenario["podsAfter"]["items"])
            with self.subTest(pods=count), patch.object(producer_fixtures, "write_fake_kubectl", retaining_writer):
                completed, packets, calls, evidence = harness.run_gate(scenario)
                self.assertEqual(completed.returncode, 0, completed.stderr)
                self.assertEqual(len(packets), 1)
                raw = next(evidence.glob("*.json")).read_bytes()
                streams = tuple(C115CommandStreams(command["purpose"],
                                                    (evidence.parent / ("retained-" + str(number) + ".stdout")).read_bytes(),
                                                    (evidence.parent / ("retained-" + str(number) + ".stderr")).read_bytes())
                                for number, command in enumerate(packets[0]["commands"]))
                barriers = CaptureStreamTests()
                barriers.setUp()
                try:
                    retained = JsonSnapshot(raw)
                    result = inspect_c115_retained_streams(retained, streams)
                    self.assertIs(result.capture, retained)
                    self.assertIs(result.streams, streams)
                    self.assertEqual(result.capture.raw, raw)
                    self.assertEqual(result.pod_count, count)
                    self.assertEqual(result.command_count, len(calls))
                    self.assertEqual(result.pod_names, tuple(pod["metadata"]["name"] for pod in scenario["pods"]["items"]))
                    self.assertEqual(dict(result.capture.value["observations"]["alphaOptIn"]),
                                     {"componentIsAlpha": alpha_pair[0], "allowAlphaComponent": alpha_pair[1]})
                    self.assertEqual(barriers.dependency_calls, {})
                finally:
                    barriers.doCleanups()


if __name__ == "__main__":
    unittest.main()
