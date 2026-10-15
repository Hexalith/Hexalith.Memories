"""Current private-plan C1.15 fixtures; successful inspection grants no authority."""

from dataclasses import fields, replace
from datetime import date, datetime, timedelta, timezone
import itertools
import os
from pkgutil import resolve_name
import sys
import unittest
from unittest.mock import patch

import test_owner_c115 as evidence
import test_owner_custody as custody
import access_telemetry_c1_owner_c115_plan_files as plan_files
import access_telemetry_c1_owner_custody as held_files
import access_telemetry_c1_owner_dispatch as dispatch_files
from access_telemetry_c1_interchange import JsonSnapshot
from access_telemetry_c1_owner_c115 import OwnerC115Inspection, inspect_owner_c115
from access_telemetry_c1_owner_c115_plan_files import OwnerC115PlanFilesError


FORBIDDEN = tuple(name for name in evidence.DEPENDENCIES if name.startswith((
    "subprocess.", "socket.", "urllib.", "time.", "shutil.",
)) or name in ("os.system", "os.popen", "os.getenv"))


class OwnerC115PlanFilesTests(unittest.TestCase):
    def setUp(self):
        # Import fixture modules, never their TestCase classes into discovery.
        self.files = custody.OwnerRunCustodyTests(methodName="runTest")
        self.files.setUp()
        self.addCleanup(self.files.doCleanups)
        self.inputs = evidence.fixture()
        self.prepare()
        self.dependency_calls = {}
        known = [(name, resolve_name(name)) for name in FORBIDDEN]

        def forbidden(name):
            def call(*args, **kwargs):
                self.dependency_calls[name] = self.dependency_calls.get(name, 0) + 1
                raise AssertionError(name + " was called")
            return call

        self.forbidden = forbidden
        # Bind these before global patching so the module scan must deny aliases
        # that would otherwise retain the original process/clock dependencies.
        for name, dependency in (("_denial_probe_process", resolve_name("subprocess.run")),
                                 ("_denial_probe_clock", resolve_name("time.time")),
                                 ("_denial_probe_now", datetime.now)):
            alias = patch.object(dispatch_files, name, dependency, create=True)
            alias.start()
            self.addCleanup(alias.stop)
        for name, original in known:
            barrier = patch(name, side_effect=forbidden(name))
            barrier.start()
            self.addCleanup(barrier.stop)
        for module in (plan_files, held_files, dispatch_files, evidence.owner_inspector, *evidence.COMPOSED_MODULES):
            for attribute, alias in tuple(vars(module).items()):
                for name, original in known:
                    if type(alias) is type(original) and alias == original:
                        barrier = patch.object(module, attribute, side_effect=forbidden(name))
                        barrier.start()
                        self.addCleanup(barrier.stop)
                        break
        clocks = {date.today: "date.today", datetime.now: "datetime.now",
                  datetime.utcnow: "datetime.utcnow", datetime.today: "datetime.today"}

        def profile(frame, event, argument):
            if event == "c_call" and argument in clocks:
                forbidden(clocks[argument])()

        self.profile = profile
        previous = sys.getprofile()
        self.addCleanup(sys.setprofile, previous)
        sys.setprofile(profile)

    def tearDown(self):
        self.assertEqual(self.dependency_calls, {})

    def prepare(self):
        value = evidence.tree(self.inputs["run"])
        value["custodyRoot"] = str(self.files.custody)
        self.inputs["run"] = evidence.snapshot(value)
        self.files.value = value
        self.files.plan.update(
            policySha256=value["policySha256"], sessionId=value["sessionId"],
            sourceCommit=value["sourceCommit"], targetSha256=value["targetSha256"],
            registrySha256=self.inputs["registry"].sha256,
        )
        self.files.prepare(record_raw=self.inputs["run"].raw)

    def inspect(self, **changes):
        supplied = {name: self.inputs[name] for name in (
            "registry", "capture", "sources", "streams", "statement", "command_contract",
            "review_role_policy", "now",
        )}
        return plan_files.inspect_owner_c115_from_plan_files(
            **{**supplied, "plan_path": str(self.files.plan_path),
               "approved_plan_sha256": self.files.approved,
               "custody_root": str(self.files.custody), **changes},
        )

    def refuses(self, *, code=None, inspected=False, **changes):
        with patch.object(plan_files, "inspect_owner_c115", wraps=inspect_owner_c115) as inspect, \
                self.assertRaises(OwnerC115PlanFilesError) as caught:
            self.inspect(**changes)
        error = caught.exception
        self.assertRegex(str(error), r"\Aowner-c115-plan-files-[a-z0-9-]+\Z")
        self.assertLess(len(str(error)), 160)
        self.assertIsNone(error.__cause__)
        self.assertIsNone(error.__context__)
        self.assertTrue(error.__suppress_context__)
        self.assertNotIn("private-canary", repr(error))
        self.assertFalse(hasattr(error, "inspection"))
        self.assertEqual(int(inspected), inspect.call_count)
        if code is not None:
            self.assertEqual("owner-c115-plan-files-" + code, str(error))
        return error

    def test_one_two_three_pods_and_orders_return_existing_frozen_detached_inspection(self):
        for count in (1, 2, 3):
            for names in itertools.permutations(("lifecycle-1", "lifecycle-2", "lifecycle-3")[:count]):
                self.inputs = evidence.fixture(names)
                self.prepare()
                with self.subTest(names=names):
                    result = self.inspect()
                    self.assertIs(type(result), OwnerC115Inspection)
                    self.assertEqual(names, result.commands.pod_names)
                    self.assertEqual(names, result.retained_streams.pod_names)
                    self.assertEqual("observe", result.owner_run.requested_action)
                    self.assertEqual(self.inputs["run"].raw, result.owner_run.snapshot.raw)
                    self.assertEqual("github:user:6775094", result.owner_principal)
                    self.assertEqual("sole-owner/self-reviewed", result.assurance_mode)
                    self.assertEqual(evidence.OwnerC115Tests.byte_count(self.inputs), result.retained_byte_count)
                    for supplied, retained in zip(self.inputs["sources"], result.sources):
                        self.assertIsNot(supplied, retained)
                    for supplied, retained in zip(self.inputs["streams"], result.retained_streams.streams):
                        self.assertIsNot(supplied, retained)
                    for name in ("execute", "accepted", "authorized", "passed", "a41_closed"):
                        self.assertFalse(hasattr(result, name))

    def test_every_call_freshly_reads_three_files_with_status_last(self):
        identities = {path.stat().st_ino: name for name, path in (
            ("plan", self.files.plan_path), ("run", self.files.record_path),
            ("status", self.files.status_path),
        )}
        real_read = os.read
        for call in range(2):
            reads = []

            def read(descriptor, count):
                reads.append(identities[os.fstat(descriptor).st_ino])
                return real_read(descriptor, count)

            with self.subTest(call=call), patch.object(held_files.os, "read", side_effect=read):
                self.inspect()
            self.assertEqual([name for name in ("plan", "run", "status", "plan", "run", "status")
                              for _ in range(2)], reads)

    def test_between_call_approved_plan_registry_and_run_replacements_use_new_bindings(self):
        first = self.inspect()
        previous_approval = self.files.approved
        previous_inodes = {path: path.stat().st_ino for path in (
            self.files.plan_path, self.files.record_path, self.files.status_path,
        )}
        capture = evidence.tree(self.inputs["capture"])
        capture.update(qualificationSessionId="replacement-approved-session", sourceCommit="d" * 40)
        capture["producer"]["arguments"][5] = capture["qualificationSessionId"]
        capture["producer"]["argumentsSha256"] = evidence.semantics.fixture_hash(capture["producer"]["arguments"])
        run = evidence.tree(self.inputs["run"])
        run.update(sessionId=capture["qualificationSessionId"], sourceCommit=capture["sourceCommit"],
                   policySha256="e" * 64)
        registry, statement, command, role = evidence.materials(evidence.bindings.registry_entry(), run)
        self.inputs.update(
            capture=evidence.snapshot(capture), run=evidence.snapshot(run),
            registry=registry, statement=statement, command_contract=command, review_role_policy=role,
            sources=tuple(replace(source, source_commit=run["sourceCommit"]) for source in self.inputs["sources"]),
        )
        self.prepare()
        for path in previous_inodes:
            replacement = self.files.custody / "replacement.json"
            self.files.write_private(replacement, path.read_bytes())
            os.replace(replacement, path)
            self.assertNotEqual(previous_inodes[path], path.stat().st_ino)
        self.assertNotEqual(previous_approval, self.files.approved)
        self.refuses(approved_plan_sha256=previous_approval, code="plan-digest-mismatch")
        with patch.object(plan_files, "inspect_owner_c115", wraps=inspect_owner_c115) as inspect:
            second = self.inspect()
        args = inspect.call_args.args
        anchors = evidence.OwnerRunAnchors(
            self.files.plan["recordSha256"], self.files.plan["policySha256"], self.files.plan["sessionId"],
            self.files.plan["sourceCommit"], self.files.plan["targetSha256"], self.files.plan["custodyRoot"],
        )
        self.assertEqual(anchors, args[5])
        self.assertEqual(self.files.plan["registrySha256"], args[9])
        self.assertEqual(self.inputs["registry"].raw, args[0].raw)
        self.assertEqual(self.inputs["run"].raw, args[4].raw)
        self.assertEqual(self.inputs["registry"].raw, second.registration.source.registry.snapshot.raw)
        self.assertEqual(self.inputs["run"].raw, second.owner_run.snapshot.raw)
        self.assertNotEqual(first.registration.source.registry.snapshot.raw,
                            second.registration.source.registry.snapshot.raw)
        self.assertNotEqual(first.owner_run.snapshot.raw, second.owner_run.snapshot.raw)
        replacement = self.files.custody / "withdrawal.json"
        self.files.write_private(replacement, custody.wire({**self.files.status, "withdrawn": True}))
        os.replace(replacement, self.files.status_path)
        self.refuses(code="withdrawn")

    def test_all_artifact_and_ancestor_descriptors_survive_complete_c115_then_close(self):
        real_open, real_close = os.open, os.close
        opened, closed = [], []

        def open_file(*args, **kwargs):
            descriptor = real_open(*args, **kwargs)
            opened.append(descriptor)
            return descriptor

        def close_file(descriptor):
            closed.append(descriptor)
            return real_close(descriptor)

        def inspect(*args, **kwargs):
            self.assertEqual([], closed)
            for descriptor in opened:
                os.fstat(descriptor)
            return inspect_owner_c115(*args, **kwargs)

        with patch.object(held_files.os, "open", side_effect=open_file), \
                patch.object(held_files.os, "close", side_effect=close_file), \
                patch.object(plan_files, "inspect_owner_c115", side_effect=inspect):
            self.inspect()
        self.assertEqual(opened[::-1], closed)

    def test_plan_is_outside_digest_approved_and_every_anchor_is_plan_derived(self):
        self.refuses(approved_plan_sha256="f" * 64, code="plan-digest-mismatch")
        for name, value in (("policySha256", "f" * 64), ("sessionId", "foreign-session"),
                            ("sourceCommit", "f" * 40), ("targetSha256", "f" * 64)):
            original = self.files.plan[name]
            self.files.plan[name] = value
            self.files.prepare(record_raw=self.inputs["run"].raw)
            with self.subTest(name=name):
                self.refuses(code="anchor-mismatch")
            self.files.plan[name] = original
        self.prepare()
        self.files.plan["custodyRoot"] = str(self.files.custody.parent)
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.refuses(code="root-mismatch")

    def test_registry_binding_is_additional_exact_raw_digest_only_for_new_api(self):
        self.files.plan.pop("registrySha256")
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.assertEqual("observe", self.files.dispatch(now=evidence.NOW).requested_action)
        self.refuses(code="registry-binding-required")
        for value in (None, True, "A" * 64, "f" * 63, "f" * 64 + "\n"):
            self.files.plan["registrySha256"] = value
            self.files.prepare(record_raw=self.inputs["run"].raw)
            with self.subTest(value=value):
                self.refuses()
        self.prepare()
        self.refuses(registry=JsonSnapshot(self.inputs["registry"].raw + b" "),
                     code="approved-registry-digest-mismatch", inspected=True)
        self.files.plan["registrySha256"] = "f" * 64
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.refuses(code="approved-registry-digest-mismatch", inspected=True)

    def test_changed_record_status_withdrawal_expiry_and_observe_reject_before_c115(self):
        self.inspect()
        self.files.write_private(self.files.record_path, self.inputs["run"].raw + b" ")
        self.refuses(code="record-digest-mismatch")
        self.prepare()
        for status in ({**self.files.status, "withdrawn": True},
                       {**self.files.status, "withdrawn": 0},
                       {**self.files.status, "sessionId": "foreign-session"},
                       {**self.files.status, "recordSha256": "f" * 64},
                       {**self.files.status, "planSha256": "f" * 64},
                       {**self.files.status, "extra": False}):
            self.files.write_private(self.files.status_path, custody.wire(status))
            self.refuses()
        self.prepare()
        self.refuses(now=evidence.owner_runs.COLLECTION, code="action-outside-phase")
        self.refuses(now=evidence.owner_runs.REVIEW, code="outside-use-window")
        self.refuses(now=evidence.owner_runs.EXPIRY, code="outside-use-window")
        self.files.value["actions"] = ["fault"]
        self.files.prepare()
        self.refuses(code="action-not-included")

    def test_invalid_supplied_closed_types_precede_filesystem_reads(self):
        class TupleSubclass(tuple):
            pass

        class SnapshotSubclass(JsonSnapshot):
            pass

        cases = [{name: value} for name in (
            "registry", "capture", "statement", "command_contract", "review_role_policy",
        ) for value in (None, SnapshotSubclass(self.inputs[name].raw))]
        cases += [{"sources": value} for value in (
            None, list(self.inputs["sources"]), iter(self.inputs["sources"]),
            self.inputs["sources"][:-1], TupleSubclass(self.inputs["sources"]),
            (object(),) + self.inputs["sources"][1:],
        )]
        cases += [{"streams": value} for value in (
            None, list(self.inputs["streams"]), iter(self.inputs["streams"]),
            TupleSubclass(self.inputs["streams"]), (object(),) + self.inputs["streams"][1:],
        )]
        for changes in cases:
            with self.subTest(changes=changes), patch.object(held_files.os, "open") as opened:
                self.refuses(**changes)
                opened.assert_not_called()

    def test_owner_policy_principal_assurance_and_pg2_pins_refuse_before_c115(self):
        original = dict(self.files.value)
        for field, changed in (("policyId", "foreign-policy"), ("ownerPrincipal", "github:user:1"),
                               ("assuranceMode", "independent"), ("profileId", "PG-ONPREM-1"),
                               ("profileSha256", "f" * 64), ("workloadSha256", "f" * 64),
                               ("environment", "production"), ("status", "withdrawn")):
            self.files.value = {**original, field: changed}
            self.files.prepare()
            with self.subTest(field=field):
                self.refuses()

    def test_forged_stream_purpose_cannot_run_equality_before_io(self):
        callbacks = []

        class CallbackString(str):
            def __eq__(self, other):
                callbacks.append(other)
                os.system("never-executed")
                return True

        stream = replace(self.inputs["streams"][0])
        object.__setattr__(stream, "purpose", CallbackString("current-context"))
        with patch.object(held_files.os, "open") as opened:
            self.refuses(streams=(stream,) + self.inputs["streams"][1:], code="stream-purpose-required")
            opened.assert_not_called()
        self.assertEqual([], callbacks)

    def test_non_utc_now_and_invalid_path_or_approval_refuse_without_file_reads(self):
        for now in (None, 0, evidence.NOW.replace(tzinfo=None),
                    evidence.NOW.replace(tzinfo=timezone(timedelta(hours=1)))):
            with patch.object(held_files.os, "open") as opened:
                self.refuses(now=now, code="aware-utc-required")
                opened.assert_not_called()
        for changes in ({"plan_path": "relative"}, {"custody_root": "/"},
                        {"approved_plan_sha256": "A" * 64}):
            with patch.object(held_files.os, "open") as opened:
                self.refuses(**changes)
                opened.assert_not_called()

    def test_missing_fields_and_malformed_raw_precede_filesystem_reads(self):
        for name in ("registry", "capture", "statement", "command_contract", "review_role_policy"):
            for raw in (None, b"{", b"\xffprivate-canary"):
                supplied = JsonSnapshot(self.inputs[name].raw)
                if raw is None:
                    object.__delattr__(supplied, "raw")
                else:
                    object.__setattr__(supplied, "raw", raw)
                with patch.object(held_files.os, "open") as opened:
                    self.refuses(**{name: supplied})
                    opened.assert_not_called()
        for name, byte_field in (("sources", "blob_bytes"), ("streams", "stdout")):
            supplied = replace(self.inputs[name][0])
            object.__delattr__(supplied, byte_field)
            with patch.object(held_files.os, "open") as opened:
                self.refuses(**{name: (supplied,) + self.inputs[name][1:]}, code="required-field-missing")
                opened.assert_not_called()

    def test_forged_snapshot_caches_are_discarded_before_file_acquisition(self):
        for name in ("registry", "capture", "statement", "command_contract", "review_role_policy"):
            supplied = JsonSnapshot(self.inputs[name].raw)
            object.__setattr__(supplied, "value", {"private-canary": "forged"})
            object.__setattr__(supplied, "sha256", "f" * 64)
            result = self.inspect(**{name: supplied})
            reparsed = {"registry": result.registration.source.registry.snapshot,
                        "capture": result.commands.capture, "statement": result.registration.statement,
                        "command_contract": result.registration.command_contract,
                        "review_role_policy": result.registration.review_role_policy}[name]
            self.assertEqual(self.inputs[name].raw, reparsed.raw)
            self.assertEqual(self.inputs[name].sha256, reparsed.sha256)

    def test_filesystem_callback_caller_mutations_cannot_replace_detached_evidence(self):
        expected_raw = self.inputs["capture"].raw
        expected_sources = self.inputs["sources"]
        expected_streams = self.inputs["streams"]
        source_values = tuple(tuple(getattr(source, field.name) for field in fields(source))
                              for source in expected_sources)
        stream_values = tuple(tuple(getattr(stream, field.name) for field in fields(stream))
                              for stream in expected_streams)
        real_open = os.open
        mutated = False

        def open_file(*args, **kwargs):
            nonlocal mutated
            if not mutated:
                mutated = True
                for name in ("registry", "capture", "statement", "command_contract", "review_role_policy"):
                    for attribute in ("raw", "value", "sha256"):
                        object.__setattr__(self.inputs[name], attribute, None)
                for item in (*expected_sources, *expected_streams):
                    for field in fields(item):
                        object.__setattr__(item, field.name, None)
            return real_open(*args, **kwargs)

        with patch.object(held_files.os, "open", side_effect=open_file):
            result = self.inspect()
        self.assertTrue(mutated)
        self.assertEqual(expected_raw, result.commands.capture.raw)
        self.assertEqual(source_values, tuple(tuple(getattr(source, field.name) for field in fields(source))
                                             for source in result.sources))
        self.assertEqual(stream_values, tuple(tuple(getattr(stream, field.name) for field in fields(stream))
                                             for stream in result.retained_streams.streams))

    def test_rehashed_foreign_namespace_tenant_session_commit_refuse(self):
        for field, changed in (("namespace", "foreign-tenant-namespace"), ("selector", "tenant=foreign"),
                               ("context", "foreign-context"), ("appId", "foreign-app"),
                               ("actorType", "ForeignTenantActor")):
            value = evidence.tree(self.inputs["capture"])
            value["target"][field] = changed
            if field in ("appId", "actorType"):
                for pod in value["observations"]["pods"]:
                    pod["appId" if field == "appId" else "actorTypes"] = changed if field == "appId" else [changed]
            evidence.semantics.refresh_declarations(value)
            with self.subTest(field=field):
                self.refuses(capture=evidence.snapshot(value), code="capture-run-scope-mismatch", inspected=True)
        for field, changed in (("qualificationSessionId", "foreign-session"), ("sourceCommit", "d" * 40)):
            value = evidence.tree(self.inputs["capture"])
            value[field] = changed
            if field == "qualificationSessionId":
                value["producer"]["arguments"][5] = changed
                value["producer"]["argumentsSha256"] = evidence.semantics.fixture_hash(value["producer"]["arguments"])
            self.refuses(capture=evidence.snapshot(value), code="capture-run-scope-mismatch", inspected=True)

    def test_rehashed_foreign_later_pod_arguments_and_outputs_refuse(self):
        self.inputs = evidence.fixture(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        self.prepare()
        for number, position, changed in ((8, 5, "lifecycle-1"), (9, 5, "foreign-pod"),
                                          (10, 3, "foreign-namespace"), (9, 11, "printf harmless")):
            value = evidence.tree(self.inputs["capture"])
            value["commands"][number]["arguments"][position] = changed
            evidence.commands.rehash(value["commands"][number])
            self.refuses(capture=evidence.snapshot(value), inspected=True)
        for number, raw in ((8, b"1.18.2\n"), (9, evidence.outputs.encoded({"id": "foreign-app"})),
                            (10, b"true\ntrue\n")):
            value, streams = evidence.outputs.changed(evidence.tree(self.inputs["capture"]),
                                                     self.inputs["streams"], number, stdout=raw)
            self.refuses(capture=evidence.snapshot(value), streams=streams, inspected=True)
        frames = [evidence.outputs.pod_frame(pod) for pod in evidence.tree(self.inputs["capture"])["observations"]["pods"]]
        frames[-1][0]["uid"] = "foreign-tenant-uid"
        value, streams = evidence.outputs.changed(evidence.tree(self.inputs["capture"]), self.inputs["streams"], -1,
                                                 stdout=b"".join(evidence.outputs.encoded(frame) for frame in frames))
        self.refuses(capture=evidence.snapshot(value), streams=streams, inspected=True)

    def test_foreign_source_ref_and_raw_output_consistency_refuse(self):
        sources = self.inputs["sources"]
        for changed in (replace(sources[0], source_commit="f" * 40),
                        replace(sources[0], executed_bytes=sources[0].executed_bytes + b"x")):
            self.refuses(sources=(changed,) + sources[1:], inspected=True)
        for name in ("statement", "command_contract", "review_role_policy"):
            self.refuses(**{name: JsonSnapshot(self.inputs[name].raw + b" ")}, inspected=True)
        streams = self.inputs["streams"]
        self.refuses(streams=(replace(streams[0], stdout=streams[0].stdout + b"x"),) + streams[1:], inspected=True)

    def test_excessive_occurrences_and_every_artifact_byte_form_refuse_before_io(self):
        for name in ("registry", "capture", "statement", "command_contract", "review_role_policy"):
            supplied = JsonSnapshot(self.inputs[name].raw)
            object.__setattr__(supplied, "raw", b"x" * (evidence.ONE_MIB + 1))
            with patch.object(held_files.os, "open") as opened, \
                    patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")):
                self.refuses(**{name: supplied}, code="artifact-byte-budget-exceeded")
                opened.assert_not_called()
        for name, byte_fields in (("sources", ("executed_bytes", "blob_bytes")),
                                 ("streams", ("stdout", "stderr"))):
            for field in byte_fields:
                supplied = replace(self.inputs[name][0])
                object.__setattr__(supplied, field, b"x" * (evidence.ONE_MIB + 1))
                with patch.object(held_files.os, "open") as opened:
                    self.refuses(**{name: (supplied,) + self.inputs[name][1:]}, code="artifact-byte-budget-exceeded")
                    opened.assert_not_called()
        repeated = b"x" * evidence.ONE_MIB
        sources = tuple(replace(source, executed_bytes=repeated, blob_bytes=repeated) for source in self.inputs["sources"])
        with patch.object(held_files.os, "open") as opened:
            self.refuses(sources=sources, code="aggregate-byte-budget-exceeded")
            opened.assert_not_called()
        stream = self.inputs["streams"][0]
        with patch.object(held_files.os, "open") as opened:
            self.refuses(streams=(stream,) * (evidence.ONE_MIB + 1), code="stream-count-exceeded")
            opened.assert_not_called()

    def test_exact_one_mib_acquired_run_and_supplied_registry_are_supported(self):
        run_raw = self.inputs["run"].raw
        self.inputs["run"] = JsonSnapshot(b" " * (evidence.ONE_MIB - len(run_raw)) + run_raw)
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.assertEqual(evidence.ONE_MIB, len(self.inspect().owner_run.snapshot.raw))
        registry = self.inputs["registry"].raw
        self.inputs["registry"] = JsonSnapshot(b" " * (evidence.ONE_MIB - len(registry)) + registry)
        self.files.plan["registrySha256"] = self.inputs["registry"].sha256
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.assertEqual(evidence.ONE_MIB, len(self.inspect().registration.source.registry.snapshot.raw))

    def test_32_mib_boundary_charges_acquired_run_and_one_extra_byte_refuses(self):
        self.inputs = evidence.fixture(count=5)
        self.prepare()
        remaining = evidence.THIRTY_TWO_MIB - evidence.OwnerC115Tests.byte_count(self.inputs)
        expanded = []
        for stream in self.inputs["streams"]:
            stderr_size = min(remaining, evidence.ONE_MIB)
            remaining -= stderr_size
            padding = min(remaining, evidence.ONE_MIB - len(stream.stdout))
            remaining -= padding
            expanded.append(replace(stream, stdout=stream.stdout + b" " * padding, stderr=b"x" * stderr_size))
        self.assertEqual(0, remaining)
        self.inputs["streams"] = tuple(expanded)
        self.inputs["capture"] = evidence.snapshot(evidence.outputs.bind(evidence.tree(self.inputs["capture"]),
                                                                        self.inputs["streams"]))
        self.assertEqual(evidence.THIRTY_TWO_MIB, self.inspect().retained_byte_count)
        streams = self.inputs["streams"]
        self.refuses(streams=streams[:-1] + (replace(streams[-1], stderr=streams[-1].stderr + b"x"),),
                     code="aggregate-byte-budget-exceeded", inspected=True)

    def test_withdrawal_during_inspection_discards_computed_valid_result_and_closes(self):
        real_open, real_close = os.open, os.close
        opened, closed, computed = [], [], []

        def open_file(*args, **kwargs):
            descriptor = real_open(*args, **kwargs)
            opened.append(descriptor)
            return descriptor

        def close_file(descriptor):
            closed.append(descriptor)
            real_close(descriptor)

        def inspect(*args, **kwargs):
            computed.append(inspect_owner_c115(*args, **kwargs))
            self.files.write_private(self.files.status_path, custody.wire({**self.files.status, "withdrawn": True}))
            return computed[-1]

        with patch.object(held_files.os, "open", side_effect=open_file), \
                patch.object(held_files.os, "close", side_effect=close_file), \
                patch.object(plan_files, "inspect_owner_c115", side_effect=inspect), \
                self.assertRaises(OwnerC115PlanFilesError) as caught:
            self.inspect()
        self.assertEqual(1, len(computed))
        self.assertEqual(opened[::-1], closed)
        self.assertIsNone(caught.exception.__context__)

    def test_mid_source_inspection_withdrawal_prevents_complete_valid_inspection_escaping(self):
        inspected = []
        real_sources = evidence.owner_inspector.inspect_sources

        def inspect_sources(*args, **kwargs):
            result = real_sources(*args, **kwargs)
            inspected.append(result)
            self.files.write_private(self.files.status_path, custody.wire({**self.files.status, "withdrawn": True}))
            return result

        with patch.object(evidence.owner_inspector, "inspect_sources", side_effect=inspect_sources):
            self.refuses(inspected=True)
        self.assertEqual(1, len(inspected))

    def test_final_readback_withdrawal_is_checked_after_plan_and_run(self):
        real_read = os.read
        plan_inode = self.files.plan_path.stat().st_ino
        status_inode = self.files.status_path.stat().st_ino
        plan_reads = 0
        status_checks = 0

        def read(descriptor, count):
            nonlocal plan_reads, status_checks
            inode = os.fstat(descriptor).st_ino
            if inode == plan_inode:
                plan_reads += 1
                if plan_reads == 3:
                    self.files.write_private(self.files.status_path, custody.wire({**self.files.status, "withdrawn": True}))
            if inode == status_inode:
                status_checks += 1
            return real_read(descriptor, count)

        with patch.object(held_files.os, "read", side_effect=read):
            self.refuses(inspected=True)
        self.assertEqual(2, status_checks)  # Final status metadata refuses before another byte read.

    def test_coarse_timestamp_same_size_rewrites_of_each_artifact_refuse_exact_bytes(self):
        real_fstat, real_stat = os.fstat, os.stat
        for path, original in ((self.files.plan_path, self.files.plan_bytes),
                               (self.files.record_path, self.inputs["run"].raw),
                               (self.files.status_path, custody.wire(self.files.status))):
            metadata = real_stat(path)
            inode = metadata.st_ino

            def coarse(value):
                if value.st_ino == inode:
                    return custody.metadata_copy(value, st_mtime_ns=metadata.st_mtime_ns,
                                                 st_ctime_ns=metadata.st_ctime_ns)
                return value

            def inspect(*args, **kwargs):
                result = inspect_owner_c115(*args, **kwargs)
                self.files.write_private(path, original.replace(b"a", b"e", 1))
                return result

            with self.subTest(path=path.name), \
                    patch.object(held_files.os, "fstat", side_effect=lambda fd: coarse(real_fstat(fd))), \
                    patch.object(held_files.os, "stat", side_effect=lambda *a, **k: coarse(real_stat(*a, **k))), \
                    patch.object(plan_files, "inspect_owner_c115", side_effect=inspect), \
                    self.assertRaisesRegex(OwnerC115PlanFilesError, "artifact-bytes-drifted"):
                self.inspect()
            self.files.write_private(path, original)

    def test_missing_oversized_nonprivate_linked_and_moved_files_refuse_before_c115(self):
        for path in (self.files.plan_path, self.files.record_path, self.files.status_path):
            original = path.read_bytes()
            for unsafe in ("missing", "oversized", "nonprivate", "hardlink", "symlink"):
                with self.subTest(path=path.name, unsafe=unsafe):
                    alias = self.files.custody / "alias"
                    if unsafe == "missing":
                        path.unlink()
                    elif unsafe == "oversized":
                        self.files.write_private(path, b"x" * (evidence.ONE_MIB + 1))
                    elif unsafe == "nonprivate":
                        path.chmod(0o644)
                    elif unsafe == "hardlink":
                        os.link(path, alias)
                    else:
                        path.rename(alias)
                        path.symlink_to(alias)
                    self.refuses()
                    if path.is_symlink():
                        path.unlink()
                    if alias.exists():
                        alias.unlink()
                    self.files.write_private(path, original)
        renamed = self.files.custody.with_name("moved-custody")

        def inspect(*args, **kwargs):
            result = inspect_owner_c115(*args, **kwargs)
            self.files.custody.rename(renamed)
            return result

        try:
            with patch.object(plan_files, "inspect_owner_c115", side_effect=inspect), \
                    self.assertRaises(OwnerC115PlanFilesError):
                self.inspect()
        finally:
            renamed.rename(self.files.custody)

    def test_failed_c115_also_closes_every_acquired_descriptor_without_partial_result(self):
        real_open, real_close = os.open, os.close
        opened, closed = [], []

        def open_file(*args, **kwargs):
            descriptor = real_open(*args, **kwargs)
            opened.append(descriptor)
            return descriptor

        def close_file(descriptor):
            closed.append(descriptor)
            real_close(descriptor)

        with patch.object(held_files.os, "open", side_effect=open_file), \
                patch.object(held_files.os, "close", side_effect=close_file):
            self.refuses(statement=JsonSnapshot(self.inputs["statement"].raw + b" "), inspected=True)
        self.assertEqual(opened[::-1], closed)

    def test_early_plan_run_and_status_refusals_close_all_partial_descriptor_stacks(self):
        real_open, real_close = os.open, os.close
        for failure in ("plan-digest", "run-missing", "status-missing", "status-withdrawn"):
            opened, closed = [], []
            self.prepare()
            changes = {}
            if failure == "plan-digest":
                changes["approved_plan_sha256"] = "f" * 64
            elif failure == "run-missing":
                self.files.record_path.unlink()
            elif failure == "status-missing":
                self.files.status_path.unlink()
            else:
                self.files.write_private(self.files.status_path, custody.wire({**self.files.status, "withdrawn": True}))

            def open_file(*args, **kwargs):
                descriptor = real_open(*args, **kwargs)
                opened.append(descriptor)
                return descriptor

            def close_file(descriptor):
                closed.append(descriptor)
                real_close(descriptor)

            with self.subTest(failure=failure), \
                    patch.object(held_files.os, "open", side_effect=open_file), \
                    patch.object(held_files.os, "close", side_effect=close_file):
                self.refuses(**changes)
            self.assertEqual(opened[::-1], closed)

    def test_acquisition_seek_and_cleanup_canary_faults_normalize_after_descriptor_cleanup(self):
        real = {name: getattr(os, name) for name in ("open", "read", "fstat", "stat", "lseek", "close")}
        status_inode = self.files.status_path.stat().st_ino
        for operation in real:
            opened, closed, faults, computed = [], [], [], []

            def fail():
                try:
                    raise ValueError("private-canary-context")
                except ValueError:
                    error = OSError("private-canary-filesystem-failure")
                    error.__cause__ = RuntimeError("private-canary-cause")
                    faults.append(error)
                    raise error

            def open_file(path, *args, **kwargs):
                if operation == "open" and path == self.files.status_path.name:
                    fail()
                descriptor = real["open"](path, *args, **kwargs)
                opened.append(descriptor)
                return descriptor

            def read(descriptor, count):
                if operation == "read" and real["fstat"](descriptor).st_ino == status_inode:
                    fail()
                return real["read"](descriptor, count)

            def fstat(descriptor):
                metadata = real["fstat"](descriptor)
                if operation == "fstat" and metadata.st_ino == status_inode:
                    fail()
                return metadata

            def stat(*args, **kwargs):
                metadata = real["stat"](*args, **kwargs)
                if operation == "stat" and metadata.st_ino == status_inode:
                    fail()
                return metadata

            def seek(descriptor, offset, whence):
                if operation == "lseek" and real["fstat"](descriptor).st_ino == status_inode:
                    fail()
                return real["lseek"](descriptor, offset, whence)

            def close(descriptor):
                closed.append(descriptor)
                real["close"](descriptor)
                if operation == "close" and not faults:
                    fail()

            def inspect(*args, **kwargs):
                computed.append(inspect_owner_c115(*args, **kwargs))
                return computed[-1]

            with self.subTest(operation=operation), \
                    patch.object(held_files.os, "open", side_effect=open_file), \
                    patch.object(held_files.os, "read", side_effect=read), \
                    patch.object(held_files.os, "fstat", side_effect=fstat), \
                    patch.object(held_files.os, "stat", side_effect=stat), \
                    patch.object(held_files.os, "lseek", side_effect=seek), \
                    patch.object(held_files.os, "close", side_effect=close), \
                    patch.object(plan_files, "inspect_owner_c115", side_effect=inspect), \
                    self.assertRaises(OwnerC115PlanFilesError) as caught:
                self.inspect()
            error = caught.exception
            self.assertEqual("owner-c115-plan-files-filesystem-unavailable", str(error))
            self.assertLess(len(str(error)), 160)
            self.assertNotIn("private-canary", repr(error))
            self.assertIsNone(error.__cause__)
            self.assertIsNone(error.__context__)
            self.assertTrue(error.__suppress_context__)
            self.assertFalse(hasattr(error, "inspection"))
            self.assertEqual(1, len(faults))
            self.assertIsNotNone(faults[0].__cause__)
            self.assertIsNotNone(faults[0].__context__)
            self.assertEqual(opened[::-1], closed)
            for descriptor in opened:
                with self.assertRaises(OSError):
                    real["fstat"](descriptor)
            self.assertEqual(int(operation in ("lseek", "close")), len(computed))
            if computed:
                self.assertIs(type(computed[0]), OwnerC115Inspection)

    def test_dispatch_prebound_process_and_clock_aliases_are_counted_and_denied(self):
        for name, attempt in (
            ("subprocess.run", lambda: dispatch_files._denial_probe_process(["git", "status"])),
            ("time.time", lambda: dispatch_files._denial_probe_clock()),
            ("datetime.now", lambda: dispatch_files._denial_probe_now()),
        ):
            sys.setprofile(self.profile)
            with self.assertRaisesRegex(AssertionError, "was called"):
                attempt()
            self.assertEqual({name: 1}, self.dependency_calls)
            self.dependency_calls.clear()
        sys.setprofile(self.profile)
        self.inspect()

    def test_counted_process_network_git_and_native_clock_barriers_detect_attempts(self):
        for name, attempt in (("subprocess.run", lambda: resolve_name("subprocess.run")(["git", "status"])),
                              ("socket.socket", lambda: resolve_name("socket.socket")()),
                              ("time.time", lambda: resolve_name("time.time")()),
                              ("datetime.now", lambda: datetime.now()),
                              ("date.today", lambda: date.today())):
            sys.setprofile(self.profile)
            with self.assertRaisesRegex(AssertionError, "was called"):
                attempt()
            self.assertEqual({name: 1}, self.dependency_calls)
            self.dependency_calls.clear()
        sys.setprofile(self.profile)
        self.inspect()


if __name__ == "__main__":
    unittest.main()
