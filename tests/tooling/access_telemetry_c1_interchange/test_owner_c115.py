"""Owner-bound offline byte fixtures; every inspector call denies dependencies."""

from __future__ import annotations

from copy import deepcopy
from dataclasses import FrozenInstanceError, fields, replace
from datetime import date, datetime, timedelta, timezone
import hashlib
import io
import itertools
import json
import os
from pkgutil import resolve_name
import sys
import time
import unittest
from unittest.mock import patch

import test_capture_semantics as semantics
import test_capture_streams as outputs
import test_capture_commands as commands
import test_owner_run as owner_runs
import test_producer_bindings as bindings
import access_telemetry_c1_owner_c115 as owner_inspector
import access_telemetry_c1_owner_run as run_inspector
import access_telemetry_c1_interchange as interchange
import access_telemetry_c1_producer_bindings as source_inspector
import access_telemetry_c1_capture_semantics as command_inspector
import access_telemetry_c1_capture_streams as stream_inspector
from access_telemetry_c1_interchange import JsonSnapshot, InterchangeFormatError
from access_telemetry_c1_owner_c115 import (
    OwnerC115Inspection, OwnerC115InspectionError, inspect_owner_c115,
)
from access_telemetry_c1_owner_run import OwnerRunAnchors
from access_telemetry_c1_producer_bindings import inspect_registration, inspect_registry, lookup_deployed_binding
from access_telemetry_c1_capture_streams import C115CommandStreams


ONE_MIB = 1_048_576
THIRTY_TWO_MIB = 33_554_432
NOW = datetime(2026, 10, 12, 10, 0, 2, tzinfo=timezone.utc)
OWNER_FIELDS = ("policyId", "policySha256", "ownerPrincipal", "assuranceMode")
PREFIX = "hexalith.access-telemetry.c1."
COMPOSED_MODULES = (run_inspector, interchange, source_inspector, command_inspector, stream_inspector)
DEPENDENCIES = (
    "builtins.open", "io.open", "io.FileIO", "pathlib.Path.open", "pathlib.Path.read_bytes",
    "pathlib.Path.read_text", "pathlib.Path.write_bytes", "pathlib.Path.write_text",
    "pathlib.Path.stat", "pathlib.Path.lstat", "pathlib.Path.resolve", "pathlib.Path.iterdir",
    "pathlib.Path.glob", "pathlib.Path.rglob", "pathlib.Path.unlink", "pathlib.Path.rename",
    "pathlib.Path.replace", "pathlib.Path.mkdir", "pathlib.Path.rmdir", "pathlib.Path.touch",
    "pathlib.Path.chmod", "pathlib.Path.symlink_to", "os.open", "os.stat", "os.lstat", "os.listdir",
    "os.scandir", "os.readlink", "os.access", "os.remove", "os.unlink", "os.rename", "os.replace",
    "os.mkdir", "os.makedirs", "os.rmdir", "os.removedirs", "os.write", "os.pwrite", "os.truncate",
    "os.ftruncate", "os.chmod", "os.chown", "os.utime", "os.link", "os.symlink", "os.getcwd",
    "os.chdir", "os.system", "os.popen", "os.getenv", "shutil.which", "subprocess.Popen",
    "subprocess.run", "subprocess.call", "subprocess.check_call", "subprocess.check_output",
    "socket.socket", "socket.create_connection", "socket.getaddrinfo", "urllib.request.urlopen",
    "urllib.request.OpenerDirector.open", "time.time", "time.time_ns", "time.monotonic",
    "time.monotonic_ns", "time.perf_counter", "time.perf_counter_ns", "time.sleep",
)


def snapshot(value):
    return JsonSnapshot((json.dumps(value, ensure_ascii=False, separators=(",", ":")) + "\n").encode())


def ref(name, retained):
    return {"path": "fixtures/owner/" + name + ".json", "sha256": retained.sha256,
            "byteLength": len(retained.raw)}


def source_receipts(value, sources):
    value = deepcopy(value)
    value["producerSources"] = []
    for source in sources:
        blob = source.blob_bytes
        value["producerSources"].append({
            "source": source.path, "gitNormalizedSha256": hashlib.sha256(blob).hexdigest(),
            "executedBytesSha256": hashlib.sha256(source.executed_bytes).hexdigest(),
            "gitBlobOid": hashlib.sha1(b"blob " + str(len(blob)).encode() + b"\0" + blob).hexdigest(),
            "headGitNormalizedSha256": hashlib.sha256(blob).hexdigest(),
            "trackedAtHead": True, "modified": False, "matchesHead": True,
        })
    value["sources"][0]["sha256"] = next(receipt["executedBytesSha256"] for receipt in value["producerSources"]
                                           if receipt["source"] == value["producer"]["path"])
    return value


def dated(value):
    if type(value) is dict:
        return {key: dated(child) for key, child in value.items()}
    if type(value) is list:
        return [dated(child) for child in value]
    if type(value) is str and value.startswith("2026-10-08T"):
        return value.replace("2026-10-08T", "2026-10-12T", 1)
    return value


def materials(entry, run):
    """Separate fixture plan; no candidate promotes its own digest to approval."""
    owner = {name: run[name] for name in OWNER_FIELDS}
    command = snapshot({
        "schemaVersion": PREFIX + "owner-command-contract/v1", "gate": "C1.15",
        "profileId": "PG-ONPREM-2", "producerPath": bindings.PRODUCER, "executable": "kubectl",
        "purposes": ["current-context", "lifecycle-pods", "daprd-version", "metadata", "alpha-opt-in",
                     "lifecycle-pods-recheck"], "readOnly": True,
    })
    role = snapshot({"schemaVersion": PREFIX + "owner-review-role-policy/v1", "gate": "C1.15",
                     "profileId": "PG-ONPREM-2", **owner})
    entry = deepcopy(entry)
    entry["commandContract"] = ref("command", command)
    entry["reviewRolePolicy"] = ref("roles", role)
    statement = snapshot({"schemaVersion": PREFIX + "owner-registration/v1",
                          **{key: value for key, value in entry.items() if key != "registrationReceipt"},
                          "sourceCommit": run["sourceCommit"], **owner})
    entry["registrationReceipt"] = ref("registration", statement)
    return snapshot([entry]), statement, command, role


def fixture(names=("lifecycle-1", "lifecycle-2"), *, count=None):
    value, streams = outputs.fixture(names) if count is None else outputs.many_fixture(count)
    value = dated(value)
    sources = bindings.fixture_sources()
    value["sourceCommit"] = bindings.COMMIT
    value = source_receipts(value, sources)
    run = owner_runs.record()
    run.update(sessionId=value["qualificationSessionId"], sourceCommit=bindings.COMMIT,
               targetSha256=value["targetSha256"])
    retained_run = snapshot(run)
    anchors = OwnerRunAnchors(retained_run.sha256, "a" * 64, run["sessionId"], bindings.COMMIT,
                              run["targetSha256"], run["custodyRoot"])
    registry, statement, command, role = materials(bindings.registry_entry(), run)
    return {"registry": registry, "capture": snapshot(value), "sources": sources, "streams": streams,
            "run": retained_run, "anchors": anchors, "statement": statement, "command_contract": command,
            "review_role_policy": role, "approved_registry_sha256": registry.sha256,
            "now": NOW, "withdrawn": False}


def tree(retained):
    return json.loads(retained.raw)


def rebind_material(inputs, name, value):
    """Approve a separate fixture registry to reach a material's schema checks."""
    result = dict(inputs)
    result[name] = snapshot(value)
    entries = tree(inputs["registry"])
    field = {"statement": "registrationReceipt", "command_contract": "commandContract",
             "review_role_policy": "reviewRolePolicy"}[name]
    entries[0][field] = ref(name, result[name])
    if name != "statement":
        statement = tree(inputs["statement"])
        statement[field] = entries[0][field]
        result["statement"] = snapshot(statement)
        entries[0]["registrationReceipt"] = ref("registration", result["statement"])
    result["registry"] = snapshot(entries)
    result["approved_registry_sha256"] = result["registry"].sha256
    return result


class OwnerC115Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        with patch.object(semantics, "capture_semantics", owner_inspector):
            semantics.CaptureSemanticTests.setUpClass.__func__(cls)
        for module in COMPOSED_MODULES:
            for name, dependency in (("_denial_probe_clock", time.time), ("_denial_probe_now", datetime.now),
                                     ("_denial_probe_datetime", datetime), ("_denial_probe_date", date),
                                     ("_denial_probe_environment", os.environ), ("_denial_probe_fileio", io.FileIO)):
                alias = patch.object(module, name, dependency, create=True)
                alias.start()
                cls.addClassCleanup(alias.stop)

    def setUp(self):
        # Snapshot real dependencies before installing the ambient barriers.
        known = [(name, resolve_name(name)) for name in DEPENDENCIES]
        clock_methods = {date.today: "date.today", datetime.now: "datetime.now",
                         datetime.utcnow: "datetime.utcnow", datetime.today: "datetime.today"}
        environment = os.environ
        with patch.object(semantics, "capture_semantics", owner_inspector):
            semantics.CaptureSemanticTests.setUp(self)

        def forbidden(name):
            def call(*args, **kwargs):
                self.dependency_calls[name] = self.dependency_calls.get(name, 0) + 1
                raise AssertionError(name + " was called")
            return call

        self.forbidden = forbidden
        for module in COMPOSED_MODULES:
            for attribute, alias in tuple(vars(module).items()):
                if alias is environment:
                    barrier = patch.object(module, attribute, os.environ)
                    barrier.start()
                    self.addCleanup(barrier.stop)
                    continue
                for name, original in known:
                    if type(alias) is type(original) and alias == original:
                        barrier = patch.object(module, attribute, side_effect=forbidden(name))
                        barrier.start()
                        self.addCleanup(barrier.stop)
                        break

        def clock_profile(frame, event, argument):
            if event == "c_call":
                name = clock_methods.get(argument)
                if name is not None:
                    forbidden(name)()

        # Native datetime classes must retain their exact identity for the run
        # validator. Deny their clock methods at the C-call boundary, including
        # methods/actual classes bound before patching in any composed module.
        self.clock_profile = clock_profile
        previous = sys.getprofile()
        self.addCleanup(sys.setprofile, previous)
        sys.setprofile(clock_profile)

    def inspect(self, inputs=None, **changes):
        return inspect_owner_c115(**{**(fixture() if inputs is None else inputs), **changes})

    def refuse(self, inputs=None, *, code=None, **changes):
        with self.assertRaises(OwnerC115InspectionError) as caught:
            self.inspect(inputs, **changes)
        error = caught.exception
        self.assertRegex(str(error), r"\Aowner-c115-[a-z0-9-]+\Z")
        self.assertLess(len(str(error)), 120)
        self.assertNotIn("private-test-credential", str(error))
        self.assertIsNone(error.__cause__)
        self.assertIsNone(error.__context__)
        self.assertTrue(error.__suppress_context__)
        if code is not None:
            self.assertEqual(str(error), code)
        return error

    def test_counted_barriers_detect_prebound_and_ambient_calls_without_live_dependencies(self):
        with patch.object(semantics, "capture_semantics", owner_inspector):
            semantics.CaptureSemanticTests.test_barriers_detect_prebound_aliases_and_filesystem_attempts_without_live_calls(self)

    def test_composed_barriers_detect_prebound_dependencies_and_native_clock_aliases(self):
        for module in COMPOSED_MODULES:
            for name, attempt in (
                ("time.time", lambda: module._denial_probe_clock()),
                ("datetime.now", lambda: module._denial_probe_now()),
                ("datetime.now", lambda: module._denial_probe_datetime.now()),
                ("date.today", lambda: module._denial_probe_date.today()),
                ("os.environ", lambda: module._denial_probe_environment.get("NEVER_READ")),
                ("io.FileIO", lambda: module._denial_probe_fileio("/never-created", "w")),
            ):
                with self.subTest(module=module.__name__, dependency=name):
                    # A raising profiler is disabled by Python; reinstall after
                    # each deliberate detection probe, without reading a clock.
                    sys.setprofile(self.clock_profile)
                    with self.assertRaisesRegex(AssertionError, "was called"):
                        attempt()
                    self.assertEqual(self.dependency_calls, {name: 1})
                    self.dependency_calls.clear()
        sys.setprofile(self.clock_profile)
        self.assertIs(run_inspector.datetime, datetime)
        with self.assertRaisesRegex(AssertionError, "datetime.now was called"):
            run_inspector.datetime.now()
        self.assertEqual(self.dependency_calls, {"datetime.now": 1})
        self.dependency_calls.clear()
        sys.setprofile(self.clock_profile)
        self.inspect()

    def test_same_run_combines_identical_reparsed_bytes_and_discloses_self_review(self):
        inputs = fixture()
        result = self.inspect(inputs)
        self.assertIs(type(result), OwnerC115Inspection)
        capture = result.registration.source.capture
        self.assertIs(result.commands.capture, capture)
        self.assertIs(result.retained_streams.capture, capture)
        self.assertEqual(capture.raw, inputs["capture"].raw)
        self.assertIsNot(capture, inputs["capture"])
        self.assertEqual(result.owner_run.snapshot.raw, inputs["run"].raw)
        self.assertEqual(result.owner_run.requested_action, "observe")
        self.assertEqual(result.owner_principal, "github:user:6775094")
        self.assertEqual(result.assurance_mode, "sole-owner/self-reviewed")
        self.assertIsNot(result.sources, inputs["sources"])
        self.assertIsNot(result.retained_streams.streams, inputs["streams"])
        for supplied, retained in zip(inputs["sources"], result.sources):
            self.assertIsNot(supplied, retained)
            self.assertEqual(supplied, retained)
            self.assertIs(supplied.executed_bytes, retained.executed_bytes)
            self.assertIs(supplied.blob_bytes, retained.blob_bytes)
        for supplied, retained in zip(inputs["streams"], result.retained_streams.streams):
            self.assertIsNot(supplied, retained)
            self.assertEqual(supplied, retained)
            self.assertIs(supplied.stdout, retained.stdout)
            self.assertIs(supplied.stderr, retained.stderr)
        self.assertEqual(result.registration.source.registry.snapshot.raw, inputs["registry"].raw)
        self.assertEqual(result.retained_byte_count, self.byte_count(inputs))
        self.assertEqual(result.commands.command_count, 9)
        self.assertEqual(result.registration.source.source_commit, result.owner_run.snapshot.value["sourceCommit"])
        for name in ("passed", "accepted", "execute", "accepted_gate", "execution_handle", "authorized"):
            self.assertFalse(hasattr(result, name))

    def test_one_two_three_pods_in_every_observation_order_bind_complete_command_blocks(self):
        for count in (1, 2, 3):
            for names in itertools.permutations(("lifecycle-1", "lifecycle-2", "lifecycle-3")[:count]):
                with self.subTest(names=names):
                    result = self.inspect(fixture(names))
                    self.assertEqual(result.commands.pod_names, names)
                    self.assertEqual(result.retained_streams.pod_names, names)
                    self.assertEqual(result.commands.command_count, 3 * count + 3)

    def test_frozen_results_tuples_nested_trees_and_representations(self):
        inputs = fixture()
        result = self.inspect(inputs)
        for obj in (result, result.owner_run, result.registration, result.commands, result.retained_streams,
                    result.sources[0], result.retained_streams.streams[0]):
            for attribute in fields(obj):
                with self.assertRaises(FrozenInstanceError):
                    setattr(obj, attribute.name, None)
        with self.assertRaises(TypeError):
            result.sources[0] = None
        with self.assertRaises(TypeError):
            result.retained_streams.streams[0] = None
        with self.assertRaises(TypeError):
            result.commands.capture.value["observations"]["pods"][0]["pod"] = "foreign-pod"
        for concealed in ("jpiquot", "fixtures/owner", "DAPR_API_TOKEN", "OFFLINE FIXTURE", "false"):
            self.assertNotIn(concealed, repr(result))
            self.assertNotIn(concealed, repr(result.registration))

    def test_retained_sources_and_streams_are_detached_from_forged_later_input_changes(self):
        inputs = fixture()
        result = self.inspect(inputs)
        expected_sources = tuple(tuple(getattr(source, field.name) for field in fields(source))
                                 for source in result.sources)
        expected_streams = tuple(tuple(getattr(stream, field.name) for field in fields(stream))
                                 for stream in result.retained_streams.streams)
        count = result.retained_byte_count
        for source in inputs["sources"]:
            for attribute in fields(source):
                object.__setattr__(source, attribute.name, None)
        for stream in inputs["streams"]:
            for attribute in fields(stream):
                object.__setattr__(stream, attribute.name, None)
        self.assertEqual(tuple(tuple(getattr(source, field.name) for field in fields(source))
                               for source in result.sources), expected_sources)
        self.assertEqual(tuple(tuple(getattr(stream, field.name) for field in fields(stream))
                               for stream in result.retained_streams.streams), expected_streams)
        self.assertEqual(result.retained_byte_count, count)
        identity = result.registration.source.sources[0]
        retained = next(source for source in result.sources if source.path == identity.path)
        self.assertEqual(identity.executed_bytes_sha256, hashlib.sha256(retained.executed_bytes).hexdigest())

    def test_missing_fields_on_exact_snapshots_sources_streams_and_anchors_refuse_without_context(self):
        inputs = fixture()
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            retained = JsonSnapshot(inputs[name].raw)
            object.__delattr__(retained, "raw")
            self.refuse(inputs, **{name: retained}, code="owner-c115-required-field-missing")
        for attribute in fields(inputs["sources"][0]):
            source = replace(inputs["sources"][0])
            object.__delattr__(source, attribute.name)
            self.refuse(inputs, sources=(source,) + inputs["sources"][1:],
                        code="owner-c115-required-field-missing")
        for attribute in fields(inputs["streams"][0]):
            stream = replace(inputs["streams"][0])
            object.__delattr__(stream, attribute.name)
            self.refuse(inputs, streams=(stream,) + inputs["streams"][1:],
                        code="owner-c115-required-field-missing")
        for attribute in fields(inputs["anchors"]):
            anchors = replace(inputs["anchors"])
            object.__delattr__(anchors, attribute.name)
            self.refuse(inputs, anchors=anchors, code="owner-c115-required-field-missing")

    def test_missing_budget_fields_refuse_before_reparse_or_hash(self):
        inputs = fixture()
        missing_snapshot = JsonSnapshot(inputs["capture"].raw)
        object.__delattr__(missing_snapshot, "raw")
        missing_source = replace(inputs["sources"][0])
        object.__delattr__(missing_source, "blob_bytes")
        missing_stream = replace(inputs["streams"][0])
        object.__delattr__(missing_stream, "stdout")
        for changed in ({"capture": missing_snapshot}, {"sources": (missing_source,) + inputs["sources"][1:]},
                        {"streams": (missing_stream,) + inputs["streams"][1:]}):
            with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.refuse(inputs, **changed, code="owner-c115-required-field-missing")

    def test_forged_nonstring_stream_purposes_refuse_without_equality_or_dependency_calls(self):
        callbacks = []

        class CallbackPurpose:
            def __eq__(self, other):
                callbacks.append(other)
                os.system("never-executed")
                return True

        class CallbackString(str):
            def __eq__(self, other):
                callbacks.append(other)
                os.system("never-executed")
                return True

        callback = CallbackPurpose()
        with self.assertRaisesRegex(AssertionError, "os.system was called"):
            callback.__eq__("deliberate-denial-probe")
        self.assertEqual(self.dependency_calls, {"os.system": 1})
        self.dependency_calls.clear()
        callbacks.clear()
        inputs = fixture()
        for purpose in (None, 1, [], {}, object(), callback, CallbackString("current-context")):
            stream = replace(inputs["streams"][0])
            object.__setattr__(stream, "purpose", purpose)
            with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.refuse(inputs, streams=(stream,) + inputs["streams"][1:],
                            code="owner-c115-stream-purpose-required")
            self.assertEqual(callbacks, [])
            self.assertEqual(self.dependency_calls, {})

    def test_forged_json_caches_are_disregarded_for_all_six_snapshots(self):
        inputs = fixture()
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            retained = JsonSnapshot(inputs[name].raw)
            object.__setattr__(retained, "value", {"forged": "private-test-credential"})
            object.__setattr__(retained, "sha256", "f" * 64)
            result = self.inspect(inputs, **{name: retained})
            reparsed = {
                "registry": result.registration.source.registry.snapshot,
                "capture": result.commands.capture, "run": result.owner_run.snapshot,
                "statement": result.registration.statement,
                "command_contract": result.registration.command_contract,
                "review_role_policy": result.registration.review_role_policy,
            }[name]
            self.assertIsNot(reparsed, retained)
            self.assertEqual(reparsed.raw, inputs[name].raw)
            self.assertEqual(reparsed.sha256, hashlib.sha256(inputs[name].raw).hexdigest())

    def test_forged_safe_cache_cannot_admit_foreign_raw_capture(self):
        inputs = fixture()
        value = tree(inputs["capture"])
        value["qualificationSessionId"] = "foreign-tenant"
        value["producer"]["arguments"][5] = value["qualificationSessionId"]
        value["producer"]["argumentsSha256"] = semantics.fixture_hash(value["producer"]["arguments"])
        retained = snapshot(value)
        object.__setattr__(retained, "value", inputs["capture"].value)
        object.__setattr__(retained, "sha256", inputs["capture"].sha256)
        self.refuse(inputs, capture=retained, code="owner-c115-capture-run-scope-mismatch")

    def test_rehashed_cross_namespace_and_tenant_targets_refuse_before_dependencies(self):
        inputs = fixture()
        for field, replacement in (("namespace", "foreign-tenant-namespace"), ("context", "foreign-cluster"),
                                   ("selector", "tenant=foreign"), ("appId", "foreign-tenant-app"),
                                   ("actorType", "ForeignTenantActor")):
            with self.subTest(field=field):
                value = tree(inputs["capture"])
                value["target"][field] = replacement
                if field in ("appId", "actorType"):
                    for pod in value["observations"]["pods"]:
                        pod["appId" if field == "appId" else "actorTypes"] = replacement if field == "appId" else [replacement]
                semantics.refresh_declarations(value)
                self.refuse(inputs, capture=snapshot(value), code="owner-c115-capture-run-scope-mismatch")

    def test_rehashed_foreign_session_and_source_commit_refuse(self):
        inputs = fixture()
        value = tree(inputs["capture"])
        value["qualificationSessionId"] = "foreign-tenant-session"
        value["producer"]["arguments"][5] = value["qualificationSessionId"]
        value["producer"]["argumentsSha256"] = semantics.fixture_hash(value["producer"]["arguments"])
        self.refuse(inputs, capture=snapshot(value), code="owner-c115-capture-run-scope-mismatch")
        value = tree(inputs["capture"])
        value["sourceCommit"] = "d" * 40
        self.refuse(inputs, capture=snapshot(value), code="owner-c115-capture-run-scope-mismatch")

    def test_each_capture_profile_and_workload_pin_stays_bound_to_owner_run(self):
        inputs = fixture()
        for name in ("profileSha256", "workloadSha256"):
            value = tree(inputs["capture"])
            value[name] = "f" * 64
            self.refuse(inputs, capture=snapshot(value), code="owner-c115-capture-run-scope-mismatch")
        value = tree(inputs["capture"])
        value["profileIdentity"] = "foreign-profile"
        self.refuse(inputs, capture=snapshot(value))
        value = tree(inputs["capture"])
        value["workloadId"] = "foreign-workload"
        self.refuse(inputs, capture=snapshot(value))

    def test_rehashed_later_pod_argv_scope_and_fixed_probe_substitutions_refuse(self):
        inputs = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        for number, position, changed in ((8, 5, "lifecycle-1"), (9, 5, "foreign-tenant-pod"),
                                          (10, 3, "foreign-namespace"), (9, 11, "printf harmless"),
                                          (8, 4, "delete")):
            value = tree(inputs["capture"])
            value["commands"][number]["arguments"][position] = changed
            commands.rehash(value["commands"][number])
            self.refuse(inputs, capture=snapshot(value), code="owner-c115-c115-command-arguments-mismatch")

    def test_rehashed_later_pod_output_swap_and_final_recheck_drift_refuse(self):
        inputs = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        value = tree(inputs["capture"])
        for number, changed in ((8, b"1.18.2\n"), (9, outputs.encoded({"id": "foreign-tenant-app"})),
                                (10, b"true\ntrue\n")):
            changed_value, changed_streams = outputs.changed(value, inputs["streams"], number, stdout=changed)
            self.refuse(inputs, capture=snapshot(changed_value), streams=changed_streams)
        frames = [outputs.pod_frame(pod) for pod in value["observations"]["pods"]]
        frames[-1][0]["uid"] = "foreign-tenant-uid"
        changed_value, changed_streams = outputs.changed(value, inputs["streams"], -1,
                                                        stdout=b"".join(outputs.encoded(frame) for frame in frames))
        self.refuse(inputs, capture=snapshot(changed_value), streams=changed_streams,
                    code="owner-c115-c115-stream-recheck-mismatch")

    def test_final_recheck_order_permutations_preserve_identity(self):
        inputs = fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        value = tree(inputs["capture"])
        for pods in itertools.permutations(value["observations"]["pods"]):
            changed_value, changed_streams = outputs.changed(value, inputs["streams"], -1,
                stdout=b"".join(outputs.encoded(outputs.pod_frame(pod)) for pod in pods))
            self.inspect(inputs, capture=snapshot(changed_value), streams=changed_streams)

    def test_registry_approval_binds_exact_raw_bytes_including_whitespace(self):
        inputs = fixture()
        self.refuse(inputs, approved_registry_sha256="f" * 64,
                    code="owner-c115-approved-registry-digest-mismatch")
        changed = JsonSnapshot(inputs["registry"].raw + b" ")
        self.refuse(inputs, registry=changed, code="owner-c115-approved-registry-digest-mismatch")
        self.inspect(inputs, registry=changed, approved_registry_sha256=changed.sha256)
        for digest in (None, "A" * 64, "a" * 63, 0, True):
            self.refuse(inputs, approved_registry_sha256=digest)

    def test_run_missing_withdrawn_unavailable_expired_and_observe_excluded_refuse(self):
        inputs = fixture()
        self.refuse(inputs, run=None, code="owner-c115-snapshots-required")
        self.refuse(inputs, withdrawn=True, code="owner-c115-owner-run-withdrawn")
        for withdrawn in (None, 0, 1, "false"):
            self.refuse(inputs, withdrawn=withdrawn, code="owner-c115-owner-run-current-status-unavailable")
        self.refuse(inputs, now=owner_runs.REVIEW, code="owner-c115-owner-run-outside-use-window")
        self.refuse(inputs, now=owner_runs.EXPIRY, code="owner-c115-owner-run-outside-use-window")
        value = tree(inputs["run"])
        value["actions"] = ["fault"]
        retained = snapshot(value)
        self.refuse(inputs, run=retained, anchors=replace(inputs["anchors"], record_sha256=retained.sha256),
                    code="owner-c115-owner-run-action-not-included")

    def test_outside_owner_anchors_cannot_be_inferred_from_candidates(self):
        inputs = fixture()
        for name, changed in (("record_sha256", "f" * 64), ("policy_sha256", "f" * 64),
                              ("session_id", "foreign-tenant-session"), ("source_commit", "f" * 40),
                              ("target_sha256", "f" * 64), ("custody_root", "/foreign-tenant/custody")):
            self.refuse(inputs, anchors=replace(inputs["anchors"], **{name: changed}))
        for name in OWNER_FIELDS:
            value = tree(inputs["run"])
            value[name] = "foreign-policy" if name != "policySha256" else "f" * 64
            retained = snapshot(value)
            self.refuse(inputs, run=retained, anchors=replace(inputs["anchors"], record_sha256=retained.sha256))

    def test_every_owner_material_policy_and_identity_field_must_match_run(self):
        inputs = fixture()
        for material in ("statement", "review_role_policy"):
            for name in OWNER_FIELDS:
                value = tree(inputs[material])
                value[name] = "f" * 64 if name == "policySha256" else "foreign-owner-policy"
                self.refuse(rebind_material(inputs, material, value))

    def test_owner_registration_inventory_and_source_commit_match_registry(self):
        inputs = fixture()
        original = tree(inputs["statement"])
        for name, changed in (
            ("gate", "C1.14"), ("profileId", "PG-ONPREM-1"),
            ("registeredStory", "fixtures/foreign-story.md"), ("producerPath", "tools/foreign-producer.ps1"),
            ("helperPaths", original["helperPaths"][:-1]), ("inputPaths", original["inputPaths"][:-1]),
            ("captureSchema", PREFIX + "evidence/v1"), ("verifierPath", "fixtures/foreign-verifier.py"),
            ("verifierSchema", "fixture.foreign-verifier/v1"), ("cleanupRequired", True),
            ("sourceCommit", "f" * 40),
        ):
            with self.subTest(field=name):
                self.refuse(rebind_material(inputs, "statement", {**original, name: changed}),
                            code="owner-c115-artifact-literal-mismatch")

    def test_unknown_versions_legacy_schemas_and_unknown_missing_fields_refuse(self):
        inputs = fixture()
        for name, schema in (("statement", "owner-registration/v1"),
                             ("command_contract", "owner-command-contract/v1"),
                             ("review_role_policy", "owner-review-role-policy/v1")):
            original = tree(inputs[name])
            for version in (schema.replace("/v1", "/v2"), schema.replace("owner-", "fixture-")):
                self.refuse(rebind_material(inputs, name, {**original, "schemaVersion": PREFIX + version}))
            self.refuse(rebind_material(inputs, name, {**original, "ownerApproved": True}))
            for omitted in original:
                self.refuse(rebind_material(inputs, name, {key: value for key, value in original.items() if key != omitted}))

    def test_command_contract_exact_six_ordered_kinds_and_false_cleanup(self):
        inputs = fixture()
        command = tree(inputs["command_contract"])
        for purposes in (command["purposes"][::-1], command["purposes"][:-1],
                         command["purposes"] + ["metadata"],
                         ["metadata:lifecycle-1" if kind == "metadata" else kind for kind in command["purposes"]]):
            self.refuse(rebind_material(inputs, "command_contract", {**command, "purposes": purposes}))
        for name, changed in (("readOnly", False), ("readOnly", 1), ("executable", "bash"),
                              ("producerPath", "tools/foreign.ps1")):
            self.refuse(rebind_material(inputs, "command_contract", {**command, name: changed}))
        entry = tree(inputs["registry"])[0]
        entry["cleanupRequired"] = True
        registry, statement, command, role = materials(entry, tree(inputs["run"]))
        self.refuse(inputs, registry=registry, statement=statement, command_contract=command,
                    review_role_policy=role, approved_registry_sha256=registry.sha256)

    def test_each_ref_digest_length_and_statement_link_is_bound(self):
        inputs = fixture()
        for name in ("statement", "command_contract", "review_role_policy"):
            self.refuse(inputs, **{name: JsonSnapshot(inputs[name].raw + b" ")})
        for field in ("registrationReceipt", "commandContract", "reviewRolePolicy"):
            for name, changed in (("sha256", "f" * 64), ("byteLength", 1)):
                entries = tree(inputs["registry"])
                entries[0][field][name] = changed
                retained = snapshot(entries)
                self.refuse(inputs, registry=retained, approved_registry_sha256=retained.sha256)
        for field in ("commandContract", "reviewRolePolicy"):
            value = tree(inputs["statement"])
            value[field]["path"] = "fixtures/foreign-tenant/material.json"
            self.refuse(rebind_material(inputs, "statement", value),
                        code="owner-c115-registration-entry-mismatch")

    def test_source_tuple_count_set_duplicates_commit_and_normalization_refuse(self):
        inputs = fixture()
        sources = inputs["sources"]
        for changed in (list(sources), iter(sources), None, sources[:-1], sources + (sources[0],)):
            self.refuse(inputs, sources=changed, code="owner-c115-nineteen-source-tuple-required")
        for changed in ((sources[0],) + sources[:-1], (object(),) + sources[1:],
                        (replace(sources[0], source_commit="f" * 40),) + sources[1:],
                        (replace(sources[0], executed_bytes=sources[0].executed_bytes + b"x"),) + sources[1:]):
            self.refuse(inputs, sources=changed)
        source = replace(sources[0])
        object.__setattr__(source, "clean_filter", "foreign-transform")
        self.refuse(inputs, sources=(source,) + sources[1:])
        source = replace(sources[0])
        object.__setattr__(source, "git_mode", "120000")
        self.refuse(inputs, sources=(source,) + sources[1:])

    def test_unclean_nonobserved_failed_skipped_or_changed_capture_refuse(self):
        inputs = fixture()
        for changes in ({"sourceDisposition": "dirty-development", "worktreeDirty": True},
                        {"producerStatus": "blocked"}, {"failureCount": 1}, {"skipCount": 1},
                        {"finalSourceRecheck": "changed-or-unavailable", "blockers": ["producer-source-changed"]}):
            self.refuse(inputs, capture=snapshot({**tree(inputs["capture"]), **changes}))

    def test_stream_tuple_pair_sequence_and_raw_types_are_closed(self):
        inputs = fixture()
        streams = inputs["streams"]
        for changed in (None, list(streams), iter(streams)):
            self.refuse(inputs, streams=changed, code="owner-c115-stream-tuple-required")
        for changed in (streams[:-1], streams + (streams[-1],), streams[::-1], (streams[1],) + streams[1:]):
            self.refuse(inputs, streams=changed)
        self.refuse(inputs, streams=(object(),) + streams[1:], code="owner-c115-stream-pair-required")
        stream = C115CommandStreams(streams[0].purpose, streams[0].stdout, streams[0].stderr)
        for raw in (None, bytearray(b"x"), memoryview(b"x"), "x"):
            object.__setattr__(stream, "stdout", raw)
            self.refuse(inputs, streams=(stream,) + streams[1:], code="owner-c115-immutable-bytes-required")

    def test_snapshot_anchor_and_container_subclasses_cannot_extend_closed_types(self):
        class SnapshotSubclass(JsonSnapshot):
            pass

        class TupleSubclass(tuple):
            pass

        inputs = fixture()
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            self.refuse(inputs, **{name: SnapshotSubclass(inputs[name].raw)}, code="owner-c115-snapshots-required")
        self.refuse(inputs, sources=TupleSubclass(inputs["sources"]), code="owner-c115-nineteen-source-tuple-required")
        self.refuse(inputs, streams=TupleSubclass(inputs["streams"]), code="owner-c115-stream-tuple-required")
        self.refuse(inputs, anchors=None)

    def test_naive_non_utc_and_non_datetime_now_refuse(self):
        inputs = fixture()
        for now in (None, 0, "2026-10-12T10:00:02Z", NOW.replace(tzinfo=None),
                    NOW.replace(tzinfo=timezone(timedelta(hours=1)))):
            self.refuse(inputs, now=now, code="owner-c115-owner-run-aware-utc-required")

    def test_collection_and_current_time_boundaries_preserve_capture_seventh_digit(self):
        inputs = fixture()
        self.inspect(inputs, now=NOW.replace(second=1, microsecond=1))
        self.refuse(inputs, now=NOW.replace(second=1), code="owner-c115-capture-outside-collection")
        value = tree(inputs["capture"])
        value["capturedAtUtc"] = value["producer"]["startedAtUtc"] = "2026-10-12T10:00:00.0000000+00:00"
        self.inspect(inputs, capture=snapshot(value))
        value["capturedAtUtc"] = value["producer"]["startedAtUtc"] = "2026-10-12T09:59:59.9999999+00:00"
        self.refuse(inputs, capture=snapshot(value), code="owner-c115-capture-outside-collection")
        self.refuse(inputs, now=owner_runs.COLLECTION, code="owner-c115-owner-run-action-outside-phase")
        self.refuse(inputs, now=owner_runs.START - timedelta(microseconds=1),
                    code="owner-c115-owner-run-outside-use-window")

    def test_capture_beyond_collection_refuses_even_if_run_is_fresh(self):
        inputs = fixture()
        value = tree(inputs["capture"])
        value["producer"]["finishedAtUtc"] = "2026-10-15T10:00:00.0000001+00:00"
        self.refuse(inputs, capture=snapshot(value), code="owner-c115-capture-outside-collection")

    def test_exact_finish_at_now_and_collection_start_remain_valid(self):
        inputs = fixture()
        value = tree(inputs["capture"])
        for receipt in value["sourceCommands"]:
            if receipt["finishedAtUtc"] == value["producer"]["finishedAtUtc"]:
                receipt["finishedAtUtc"] = "2026-10-12T10:00:01.0000000+00:00"
        value["producer"]["finishedAtUtc"] = "2026-10-12T10:00:01.0000000+00:00"
        value["capturedAtUtc"] = value["producer"]["startedAtUtc"] = "2026-10-12T10:00:00.0000000+00:00"
        self.inspect(inputs, capture=snapshot(value), now=NOW.replace(second=1))

    def test_every_artifact_form_budget_refuses_before_reparse_or_hash(self):
        inputs = fixture()
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            retained = JsonSnapshot(inputs[name].raw)
            object.__setattr__(retained, "raw", b"x" * (ONE_MIB + 1))
            with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.refuse(inputs, **{name: retained}, code="owner-c115-artifact-byte-budget-exceeded")
        for name in ("executed_bytes", "blob_bytes"):
            source = replace(inputs["sources"][0])
            object.__setattr__(source, name, b"x" * (ONE_MIB + 1))
            with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.refuse(inputs, sources=(source,) + inputs["sources"][1:],
                            code="owner-c115-artifact-byte-budget-exceeded")
        for name in ("stdout", "stderr"):
            stream = replace(inputs["streams"][0], **{name: b"x" * (ONE_MIB + 1)})
            with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.refuse(inputs, streams=(stream,) + inputs["streams"][1:],
                            code="owner-c115-artifact-byte-budget-exceeded")

    def test_aggregate_budget_counts_all_repeated_source_forms_before_reparse_or_hash(self):
        inputs = fixture()
        same = b"x" * ONE_MIB
        sources = tuple(replace(source, executed_bytes=same, blob_bytes=same) for source in inputs["sources"])
        with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                patch("hashlib.sha256", side_effect=AssertionError("hashed")):
            self.refuse(inputs, sources=sources, code="owner-c115-aggregate-byte-budget-exceeded")

    def test_aggregate_budget_includes_run_materials_and_each_stream_occurrence(self):
        inputs = fixture(count=11)
        streams = tuple(replace(stream, stderr=b"x" * ONE_MIB) for stream in inputs["streams"])
        with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                patch("hashlib.sha256", side_effect=AssertionError("hashed")):
            self.refuse(inputs, streams=streams, code="owner-c115-aggregate-byte-budget-exceeded")

    @staticmethod
    def byte_count(inputs):
        return (sum(len(inputs[name].raw) for name in
                    ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"))
                + sum(len(source.executed_bytes) + len(source.blob_bytes) for source in inputs["sources"])
                + sum(len(stream.stdout) + len(stream.stderr) for stream in inputs["streams"]))

    def test_exact_one_mib_json_and_stream_artifacts_are_supported(self):
        inputs = fixture()
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            retained = JsonSnapshot(b" " * (ONE_MIB - len(inputs[name].raw)) + inputs[name].raw)
            changed = dict(inputs, **{name: retained})
            if name == "registry":
                changed["approved_registry_sha256"] = retained.sha256
            elif name == "run":
                changed["anchors"] = replace(inputs["anchors"], record_sha256=retained.sha256)
            elif name in ("statement", "command_contract", "review_role_policy"):
                # Rebind exact padded bytes while keeping statement links exact.
                entries = tree(inputs["registry"])
                field = {"statement": "registrationReceipt", "command_contract": "commandContract",
                         "review_role_policy": "reviewRolePolicy"}[name]
                entries[0][field] = ref(name, retained)
                if name != "statement":
                    statement = tree(inputs["statement"])
                    statement[field] = entries[0][field]
                    changed["statement"] = snapshot(statement)
                    entries[0]["registrationReceipt"] = ref("registration", changed["statement"])
                changed["registry"] = snapshot(entries)
                changed["approved_registry_sha256"] = changed["registry"].sha256
            self.inspect(changed)
        value, streams = outputs.changed(tree(inputs["capture"]), inputs["streams"], 0,
            stdout=inputs["streams"][0].stdout + b" " * (ONE_MIB - len(inputs["streams"][0].stdout)),
            stderr=b"x" * ONE_MIB)
        self.inspect(inputs, capture=snapshot(value), streams=streams)

    def test_exact_32_mib_all_occurrence_boundary_and_one_byte_over(self):
        inputs = fixture(count=5)
        # Changed receipt hashes retain fixed-width JSON, so the total is stable.
        remaining = THIRTY_TWO_MIB - self.byte_count(inputs)
        expanded = []
        for stream in inputs["streams"]:
            stderr_size = min(remaining, ONE_MIB)
            remaining -= stderr_size
            padding = min(remaining, ONE_MIB - len(stream.stdout))
            remaining -= padding
            expanded.append(replace(stream, stdout=stream.stdout + b" " * padding, stderr=b"x" * stderr_size))
        self.assertEqual(remaining, 0)
        streams = tuple(expanded)
        inputs.update(capture=snapshot(outputs.bind(tree(inputs["capture"]), streams)), streams=streams)
        self.assertEqual(self.byte_count(inputs), THIRTY_TWO_MIB)
        self.assertEqual(self.inspect(inputs).retained_byte_count, THIRTY_TWO_MIB)
        last = replace(streams[-1], stderr=streams[-1].stderr + b"x")
        self.refuse(inputs, streams=streams[:-1] + (last,), code="owner-c115-aggregate-byte-budget-exceeded")

    def test_exact_one_mib_source_forms_are_supported_with_counted_occurrences(self):
        inputs = fixture()
        same = b"# isolated source\n" + b"x" * (ONE_MIB - len(b"# isolated source\n"))
        sources = (replace(inputs["sources"][0], executed_bytes=same, blob_bytes=same),) + inputs["sources"][1:]
        value = source_receipts(tree(inputs["capture"]), sources)
        result = self.inspect(inputs, sources=sources, capture=snapshot(value))
        self.assertGreater(result.retained_byte_count, 2 * ONE_MIB)

    def test_invalid_raw_json_utf8_and_secret_streams_have_content_free_unchained_refusals(self):
        inputs = fixture()
        for raw in (b"{", b"\xffprivate-test-credential", b'{"a":1,"a":2}'):
            retained = JsonSnapshot(inputs["capture"].raw)
            object.__setattr__(retained, "raw", raw)
            self.refuse(inputs, capture=retained)
        for number in (0, 6, len(inputs["streams"]) - 1):
            for name in ("stdout", "stderr"):
                for raw in (b"\xffprivate-test-credential", b'{"token":"private-test-credential"}'):
                    value, streams = outputs.changed(tree(inputs["capture"]), inputs["streams"], number, **{name: raw})
                    self.refuse(inputs, capture=snapshot(value), streams=streams)

    def test_legacy_fixture_registration_and_unconditional_deployed_refusal_stay_intact(self):
        inputs = fixture()
        entry = bindings.registry_entry()
        command = tree(inputs["command_contract"])
        command["schemaVersion"] = PREFIX + "fixture-command-contract/v2"
        command = snapshot(command)
        role = snapshot({"schemaVersion": PREFIX + "fixture-review-role-policy/v1", "gate": "C1.15",
                         "profileId": "PG-ONPREM-2", "reviewerRole": "independent-security-reviewer",
                         "producerExcluded": True})
        entry.update(commandContract=ref("legacy-command", command), reviewRolePolicy=ref("legacy-role", role))
        statement = snapshot({"schemaVersion": PREFIX + "fixture-registration/v1", "sourceCommit": bindings.COMMIT,
                              **{key: value for key, value in entry.items() if key != "registrationReceipt"}})
        entry["registrationReceipt"] = ref("legacy-registration", statement)
        result = inspect_registration(inspect_registry(snapshot([entry])), inputs["capture"], inputs["sources"],
                                      bindings.COMMIT, statement, command, role)
        self.assertEqual(result.gate, "C1.15")
        with self.assertRaisesRegex(InterchangeFormatError, "^deployed-producer-binding-unavailable$"):
            lookup_deployed_binding("C1.15", "PG-ONPREM-2", result.source.registry)
        with self.assertRaises(InterchangeFormatError):
            inspect_registration(inspect_registry(inputs["registry"]), inputs["capture"], inputs["sources"],
                                 bindings.COMMIT, inputs["statement"], inputs["command_contract"], inputs["review_role_policy"])


if __name__ == "__main__":
    unittest.main()
