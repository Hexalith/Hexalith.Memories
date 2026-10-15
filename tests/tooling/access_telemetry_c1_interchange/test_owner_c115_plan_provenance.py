"""Held current-plan composition with disposable clean local Git checkouts."""

from contextlib import ExitStack, contextmanager
from dataclasses import FrozenInstanceError, fields, replace
from datetime import date, datetime, timedelta, timezone
import inspect
import os
from pkgutil import resolve_name
import subprocess
import sys
import unittest
from unittest.mock import patch

import test_owner_c115 as owner
import test_owner_c115_provenance as checkout
import test_owner_custody as custody
import access_telemetry_c1_owner_c115_plan_files as plan_files
import access_telemetry_c1_owner_c115_provenance as provenance
import access_telemetry_c1_owner_custody as held_files
import access_telemetry_c1_owner_dispatch as dispatch_files
import access_telemetry_c1_source_provenance as local
from access_telemetry_c1_interchange import JsonSnapshot


MATERIALS = ("registry", "capture", "statement", "command_contract", "review_role_policy")
FORBIDDEN = tuple(name for name in owner.DEPENDENCIES if name.startswith((
    "socket.", "urllib.", "shutil.", "subprocess.", "time.",
)) or name in ("os.system", "os.popen", "os.getenv"))
MODULES = (provenance, plan_files, held_files, dispatch_files, local,
           owner.owner_inspector, *owner.COMPOSED_MODULES)


class OwnerC115PlanProvenanceTests(unittest.TestCase):
    def setUp(self):
        # Keep fixture TestCase classes behind their modules to avoid discovery
        # importing their tests as additional cases in this new discovery unit.
        self.files = custody.OwnerRunCustodyTests(methodName="runTest")
        self.files.setUp()
        self.addCleanup(self.files.doCleanups)
        self.checkout = checkout.OwnerC115ProvenanceTests(methodName="runTest")
        self.checkout.setUp()
        self.addCleanup(self.checkout.doCleanups)
        self.root = self.checkout.root
        self.inputs = self.checkout.inputs()
        self.prepare()

    def prepare(self):
        value = {**owner.tree(self.inputs["run"]), "custodyRoot": str(self.files.custody)}
        self.inputs["run"] = owner.snapshot(value)
        self.files.value = value
        self.files.plan.update(
            policySha256=value["policySha256"], sessionId=value["sessionId"],
            sourceCommit=value["sourceCommit"], targetSha256=value["targetSha256"],
            registrySha256=self.inputs["registry"].sha256,
        )
        self.files.prepare(record_raw=self.inputs["run"].raw)

    def verify(self, **changes):
        return provenance.corroborate_owner_c115_sources_from_plan_files(**{
            "plan_path": str(self.files.plan_path), "approved_plan_sha256": self.files.approved,
            "custody_root": str(self.files.custody), "repository_root": str(self.root),
            **{name: self.inputs[name] for name in (*MATERIALS, "sources", "streams", "now")},
            **changes,
        })

    def refuse(self, *, code=None, **changes):
        with self.assertRaises(provenance.OwnerC115PlanSourceCorroborationError) as caught:
            self.verify(**changes)
        error = caught.exception
        self.assertRegex(str(error), r"\Aowner-c115-plan-source-[a-z0-9-]+\Z")
        self.assertLess(len(str(error)), 160)
        self.assertIsNone(error.__cause__)
        self.assertIsNone(error.__context__)
        self.assertTrue(error.__suppress_context__)
        self.assertFalse(hasattr(error, "inspection"))
        for sensitive in ("private-canary", "private-test-credential", str(self.root), str(self.files.custody)):
            self.assertNotIn(sensitive, repr(error))
        if code is not None:
            self.assertEqual("owner-c115-plan-source-" + code, str(error))
        return error

    @contextmanager
    def forbidden_calls(self, *, allow_git=False):
        """Count forbidden ambient calls and aliases; only Git deadlines remain."""
        names = tuple(name for name in FORBIDDEN if not allow_git or name not in (
            "subprocess.Popen", "time.monotonic", "time.sleep",
        ))
        known = [(name, resolve_name(name)) for name in names]
        calls = {}

        def forbidden(name):
            def call(*args, **kwargs):
                calls[name] = calls.get(name, 0) + 1
                raise AssertionError(name + " was called")
            return call

        clocks = {date.today: "date.today", datetime.now: "datetime.now",
                  datetime.utcnow: "datetime.utcnow", datetime.today: "datetime.today"}
        known.extend((name, method) for method, name in clocks.items())

        def profile(frame, event, argument):
            if event == "c_call" and argument in clocks:
                forbidden(clocks[argument])()

        previous = sys.getprofile()
        try:
            with ExitStack() as stack:
                for module in MODULES:
                    for attribute, alias in tuple(vars(module).items()):
                        for name, original in known:
                            if type(alias) is type(original) and (alias is original or alias == original):
                                stack.enter_context(patch.object(module, attribute, side_effect=forbidden(name)))
                                break
                for name in names:
                    stack.enter_context(patch(name, side_effect=forbidden(name)))
                sys.setprofile(profile)
                yield calls
        finally:
            sys.setprofile(previous)
        self.assertEqual(calls, {})

    def before_git_refusal(self, *, inspected=None, **changes):
        with self.forbidden_calls(), \
                patch.object(local, "_open_directory") as opened, \
                patch.object(local, "_owner_git") as git, \
                patch.object(plan_files, "inspect_owner_c115", wraps=owner.inspect_owner_c115) as inspection:
            result = self.refuse(**changes)
        opened.assert_not_called()
        git.assert_not_called()
        if inspected is not None:
            self.assertEqual(int(inspected), inspection.call_count)
        return result

    @contextmanager
    def tracked_custody(self):
        held, closed = [], []
        real_open, real_close = held_files._open_held_file, os.close

        def open_file(*args, **kwargs):
            result = real_open(*args, **kwargs)
            held.extend(descriptor for descriptor, before in result.ancestors)
            held.append(result.descriptor)
            return result

        def close(descriptor):
            if descriptor in held:
                closed.append(descriptor)
            return real_close(descriptor)

        with patch.object(held_files, "_open_held_file", side_effect=open_file), \
                patch.object(held_files.os, "close", side_effect=close):
            yield held, closed
        self.assertEqual(held[::-1], closed)
        for descriptor in held:
            with self.assertRaises(OSError):
                os.fstat(descriptor)

    def test_one_two_three_pods_have_one_full_inspection_then_only_bounded_local_git(self):
        parameters = inspect.signature(provenance.corroborate_owner_c115_sources_from_plan_files).parameters
        self.assertEqual(set(parameters), {
            "plan_path", "approved_plan_sha256", "custody_root", "repository_root", "now",
            *MATERIALS, "sources", "streams",
        })
        for names in (("lifecycle-1",), ("lifecycle-2", "lifecycle-1"),
                      ("lifecycle-3", "lifecycle-1", "lifecycle-2")):
            self.inputs = self.checkout.inputs(names)
            self.prepare()
            with self.subTest(pods=names), self.checkout.local_lane(self.inputs), \
                    self.forbidden_calls(allow_git=True), \
                    patch.object(plan_files, "inspect_owner_c115", wraps=provenance.inspect_owner_c115) as inspection:
                result = self.verify()
            self.assertEqual(inspection.call_count, 1)
            self.assertIs(type(result), provenance.OwnerC115LocalSourceCorroboration)
            self.assertEqual(result.source_commit, self.checkout.commit)
            self.assertEqual(result.repository_root, str(self.root))
            self.assertEqual(result.paths, tuple(sorted(owner.bindings.PATHS)))
            self.assertEqual(result.git_blob_oids, tuple(
                checkout.legacy.git(self.root, "rev-parse", f"{self.checkout.commit}:{path}")
                for path in result.paths))
            self.assertEqual(result.inspection.commands.pod_names, names)
            self.assertEqual(result.inspection.retained_streams.pod_names, names)
            self.assertEqual(result.inspection.retained_byte_count, owner.OwnerC115Tests.byte_count(self.inputs))
            self.assertEqual((result.owner_principal, result.assurance_mode),
                             ("github:user:6775094", "sole-owner/self-reviewed"))
            self.assertEqual(result.inspection.owner_run.requested_action, "observe")
            for attribute in fields(result):
                with self.assertRaises(FrozenInstanceError):
                    setattr(result, attribute.name, None)
            for name in ("execute", "accepted", "authorized", "passed", "a41_closed"):
                self.assertFalse(hasattr(result, name))

    def test_all_plan_run_status_and_ancestor_descriptors_survive_git_and_status_last_readback(self):
        real_read, real_popen = os.read, subprocess.Popen
        identities = {path.stat().st_ino: name for name, path in (
            ("plan", self.files.plan_path), ("run", self.files.record_path), ("status", self.files.status_path),
        )}
        for call in range(2):
            reads, children = [], []

            def read(descriptor, count):
                name = identities.get(os.fstat(descriptor).st_ino)
                if name is not None:
                    reads.append(name)
                return real_read(descriptor, count)

            def child(*args, **kwargs):
                self.assertEqual(reads, [name for name in ("plan", "run", "status") for _ in range(2)])
                self.assertEqual(closed, [])
                for descriptor in held:
                    os.fstat(descriptor)
                children.append(real_popen(*args, **kwargs))
                return children[-1]

            with self.subTest(call=call), self.tracked_custody() as (held, closed), \
                    patch.object(held_files.os, "read", side_effect=read), \
                    patch.object(local.subprocess, "Popen", side_effect=child):
                self.verify()
            self.assertEqual(reads, [name for name in ("plan", "run", "status", "plan", "run", "status")
                                    for _ in range(2)])
            self.assertEqual(len(children), 140)
            self.assertTrue(all(process.poll() is not None and process.stdout.closed for process in children))

    def test_replacement_approved_plan_run_registry_and_status_are_fresh_each_call(self):
        first = self.verify()
        old_approval = self.files.approved
        run = {**owner.tree(self.inputs["run"]), "sessionId": "replacement-approved-session", "policySha256": "e" * 64}
        capture = owner.tree(self.inputs["capture"])
        capture["qualificationSessionId"] = run["sessionId"]
        capture["producer"]["arguments"][5] = run["sessionId"]
        capture["producer"]["argumentsSha256"] = owner.semantics.fixture_hash(capture["producer"]["arguments"])
        registry, statement, command, role = owner.materials(owner.bindings.registry_entry(), run)
        self.inputs.update(run=owner.snapshot(run), capture=owner.snapshot(capture), registry=registry,
                           statement=statement, command_contract=command, review_role_policy=role)
        self.prepare()
        for path in (self.files.plan_path, self.files.record_path, self.files.status_path):
            old_inode = path.stat().st_ino
            replacement = self.files.custody / "replacement.json"
            self.files.write_private(replacement, path.read_bytes())
            os.replace(replacement, path)
            self.assertNotEqual(old_inode, path.stat().st_ino)
        self.before_git_refusal(approved_plan_sha256=old_approval, code="plan-digest-mismatch", inspected=False)
        with patch.object(plan_files, "inspect_owner_c115", wraps=owner.inspect_owner_c115) as inspection:
            second = self.verify()
        self.assertEqual(inspection.call_count, 1)
        anchors = inspection.call_args.args[5]
        self.assertEqual(tuple(getattr(anchors, field.name) for field in fields(anchors)), tuple(
            self.files.plan[name] for name in
            ("recordSha256", "policySha256", "sessionId", "sourceCommit", "targetSha256", "custodyRoot")))
        self.assertEqual(inspection.call_args.args[9], self.files.plan["registrySha256"])
        self.assertNotEqual(first.inspection.owner_run.snapshot.raw, second.inspection.owner_run.snapshot.raw)
        self.files.write_private(self.files.status_path, custody.wire({**self.files.status, "withdrawn": True}))
        self.before_git_refusal(code="withdrawn", inspected=False)

    def test_invalid_closed_containers_missing_slots_and_times_precede_every_file_read(self):
        candidates = [{"sources": list(self.inputs["sources"])}, {"streams": list(self.inputs["streams"])},
                      {"sources": self.inputs["sources"][:-1]}, {"registry": None},
                      {"now": owner.NOW.replace(tzinfo=None)},
                      {"now": owner.NOW.replace(tzinfo=timezone(timedelta(hours=1)))}]
        for name in MATERIALS:
            material = JsonSnapshot(self.inputs[name].raw)
            object.__delattr__(material, "raw")
            candidates.append({name: material})
        for name in ("sources", "streams"):
            for attribute in fields(self.inputs[name][0]):
                record = replace(self.inputs[name][0])
                object.__delattr__(record, attribute.name)
                candidates.append({name: (record,) + self.inputs[name][1:]})
        for changes in candidates:
            with self.subTest(changes=tuple(changes)), checkout.denials() as calls:
                self.refuse(**changes)
                self.assertEqual(calls, {})

    def test_each_byte_form_and_repeated_occurrence_budgets_precede_read_parse_and_hash(self):
        candidates = []
        for name in MATERIALS:
            item = JsonSnapshot(self.inputs[name].raw)
            object.__setattr__(item, "raw", b"x" * (owner.ONE_MIB + 1))
            candidates.append({name: item})
        for name, byte_fields in (("sources", ("executed_bytes", "blob_bytes")), ("streams", ("stdout", "stderr"))):
            for attribute in byte_fields:
                record = replace(self.inputs[name][0])
                object.__setattr__(record, attribute, b"x" * (owner.ONE_MIB + 1))
                candidates.append({name: (record,) + self.inputs[name][1:]})
        repeated = b"x" * owner.ONE_MIB
        candidates.append({"sources": tuple(replace(source, executed_bytes=repeated, blob_bytes=repeated)
                                             for source in self.inputs["sources"])})
        candidates.append({"streams": (self.inputs["streams"][0],) * (owner.ONE_MIB + 1)})
        for changes in candidates:
            with checkout.denials() as calls, \
                    patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.refuse(**changes)
                self.assertEqual(calls, {})

    def test_detachment_before_file_callbacks_and_checked_sources_before_git(self):
        raw_materials = {name: self.inputs[name].raw for name in MATERIALS}
        sources, streams = self.inputs["sources"], self.inputs["streams"]
        real_compose = plan_files._compose_owner_run_files
        real_local = provenance._corroborate_inspected_sources

        def mutate_then_open(*args, **kwargs):
            for name in MATERIALS:
                object.__setattr__(self.inputs[name], "raw", b"private-test-credential")
            for record in (*sources, *streams):
                for attribute in fields(record):
                    object.__setattr__(record, attribute.name, None)
            return real_compose(*args, **kwargs)

        def checked(source, retained, root, **kwargs):
            self.assertEqual(kwargs, {"owner_safety_preflight": True})
            self.assertIsNot(retained, sources)
            for supplied, detached in zip(sources, retained):
                self.assertIsNot(supplied, detached)
                self.assertIsNotNone(detached.executed_bytes)
            return real_local(source, retained, root, **kwargs)

        for name in MATERIALS:
            object.__setattr__(self.inputs[name], "value", {"forged": "private-test-credential"})
            object.__setattr__(self.inputs[name], "sha256", "f" * 64)
        with patch.object(plan_files, "_compose_owner_run_files", side_effect=mutate_then_open), \
                patch.object(provenance, "_corroborate_inspected_sources", side_effect=checked):
            result = self.verify()
        inspection = result.inspection
        retained = (inspection.registration.source.registry.snapshot, inspection.commands.capture,
                    inspection.registration.statement, inspection.registration.command_contract,
                    inspection.registration.review_role_policy)
        for name, material in zip(MATERIALS, retained):
            self.assertEqual(material.raw, raw_materials[name])
        self.assertNotIn(b"private-test-credential", inspection.retained_streams.streams[0].stdout)

    def test_plan_digest_and_every_outside_plan_binding_refuse_before_git(self):
        self.before_git_refusal(approved_plan_sha256="f" * 64, inspected=False, code="plan-digest-mismatch")
        for name, changed in (("policySha256", "f" * 64), ("sessionId", "foreign-session"),
                              ("sourceCommit", "f" * 40), ("targetSha256", "f" * 64)):
            original = self.files.plan[name]
            self.files.plan[name] = changed
            self.files.prepare(record_raw=self.inputs["run"].raw)
            self.before_git_refusal(inspected=False, code="anchor-mismatch")
            self.files.plan[name] = original
        self.prepare()
        self.files.plan["custodyRoot"] = str(self.files.custody.parent)
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.before_git_refusal(inspected=False, code="root-mismatch")

    def test_required_registry_binding_is_exact_raw_digest_and_precedes_git(self):
        self.files.plan.pop("registrySha256")
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.before_git_refusal(inspected=False, code="registry-binding-required")
        for value in (None, True, "A" * 64, "f" * 63, "f" * 64 + "\n"):
            self.files.plan["registrySha256"] = value
            self.files.prepare(record_raw=self.inputs["run"].raw)
            self.before_git_refusal(inspected=False)
        self.prepare()
        self.before_git_refusal(registry=JsonSnapshot(self.inputs["registry"].raw + b" "),
                                inspected=True, code="approved-registry-digest-mismatch")

    def test_invalid_raw_bytes_and_forged_safe_caches_refuse_before_checkout_access(self):
        for name in MATERIALS:
            for raw in (b"{", b"\xffprivate-test-credential", b'{"a":1,"a":2}'):
                candidate = JsonSnapshot(self.inputs[name].raw)
                object.__setattr__(candidate, "raw", raw)
                with checkout.denials() as calls:
                    self.refuse(**{name: candidate})
                    self.assertEqual(calls, {})
        candidate = owner.snapshot({**owner.tree(self.inputs["capture"]), "sourceCommit": "f" * 40})
        object.__setattr__(candidate, "value", self.inputs["capture"].value)
        object.__setattr__(candidate, "sha256", self.inputs["capture"].sha256)
        self.before_git_refusal(capture=candidate, inspected=True, code="capture-run-scope-mismatch")

    def test_current_record_status_withdrawal_expiry_and_observe_refuse_before_git(self):
        self.files.write_private(self.files.record_path, self.inputs["run"].raw + b" ")
        self.before_git_refusal(inspected=False, code="record-digest-mismatch")
        self.prepare()
        for status in ({**self.files.status, "withdrawn": True}, {**self.files.status, "withdrawn": 0},
                       {**self.files.status, "sessionId": "foreign-session"},
                       {**self.files.status, "recordSha256": "f" * 64},
                       {**self.files.status, "planSha256": "f" * 64}, {**self.files.status, "extra": False}):
            self.files.write_private(self.files.status_path, custody.wire(status))
            self.before_git_refusal(inspected=False)
        self.prepare()
        for now in (owner.owner_runs.COLLECTION, owner.owner_runs.REVIEW, owner.owner_runs.EXPIRY):
            self.before_git_refusal(now=now, inspected=False)
        self.inputs["run"] = owner.snapshot({**owner.tree(self.inputs["run"]), "actions": ["purge"]})
        self.prepare()
        self.before_git_refusal(inspected=False)

    def test_owner_policy_self_review_and_pg2_pins_refuse_before_git(self):
        original = self.inputs["run"]
        for field, changed in (("ownerPrincipal", "foreign-owner"), ("policyId", "foreign-policy"),
                               ("assuranceMode", "independently-reviewed"), ("status", "draft"),
                               ("profileId", "PG-ONPREM-1"), ("profileSha256", "f" * 64),
                               ("workloadSha256", "f" * 64), ("environment", "Production")):
            self.inputs["run"] = owner.snapshot({**owner.tree(original), field: changed})
            self.prepare()
            self.before_git_refusal(inspected=False)
        self.inputs["run"] = original
        self.prepare()
        for material in ("statement", "review_role_policy"):
            for name in owner.OWNER_FIELDS:
                original_inputs = self.inputs
                value = {**owner.tree(self.inputs[material]), name: "f" * 64 if name == "policySha256" else "foreign-owner"}
                self.inputs = owner.rebind_material(self.inputs, material, value)
                self.prepare()
                self.before_git_refusal(inspected=True)
                self.inputs = original_inputs

    def test_rehashed_foreign_tenant_and_namespace_refuse_before_git(self):
        for name, changed in (("namespace", "foreign-tenant-namespace"), ("context", "foreign-cluster"),
                              ("selector", "tenant=foreign"), ("appId", "foreign-tenant-app"),
                              ("actorType", "ForeignTenantActor")):
            value = owner.tree(self.inputs["capture"])
            value["target"][name] = changed
            if name in ("appId", "actorType"):
                for pod in value["observations"]["pods"]:
                    pod["appId" if name == "appId" else "actorTypes"] = changed if name == "appId" else [changed]
            owner.semantics.refresh_declarations(value)
            self.before_git_refusal(capture=owner.snapshot(value), inspected=True, code="capture-run-scope-mismatch")

    def test_rehashed_foreign_session_and_commit_refuse_before_git(self):
        for name, changed in (("qualificationSessionId", "foreign-tenant-session"), ("sourceCommit", "f" * 40)):
            value = owner.tree(self.inputs["capture"])
            value[name] = changed
            if name == "qualificationSessionId":
                value["producer"]["arguments"][5] = changed
                value["producer"]["argumentsSha256"] = owner.semantics.fixture_hash(value["producer"]["arguments"])
            self.before_git_refusal(capture=owner.snapshot(value), inspected=True, code="capture-run-scope-mismatch")

    def test_rehashed_later_pod_foreign_argv_outputs_and_recheck_refuse_before_git(self):
        self.inputs = self.checkout.inputs(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        self.prepare()
        for number, position, changed in ((8, 5, "foreign-pod"), (9, 3, "foreign-namespace"),
                                          (10, 5, "lifecycle-1"), (9, 11, "printf harmless"), (8, 4, "delete")):
            value = owner.tree(self.inputs["capture"])
            value["commands"][number]["arguments"][position] = changed
            owner.commands.rehash(value["commands"][number])
            self.before_git_refusal(capture=owner.snapshot(value), inspected=True)
        for number, raw in ((8, b"1.18.2\n"), (9, owner.outputs.encoded({"id": "foreign-tenant-app"})),
                            (10, b"true\ntrue\n")):
            value, streams = owner.outputs.changed(owner.tree(self.inputs["capture"]), self.inputs["streams"], number, stdout=raw)
            self.before_git_refusal(capture=owner.snapshot(value), streams=streams, inspected=True)
        frames = [owner.outputs.pod_frame(pod) for pod in owner.tree(self.inputs["capture"])["observations"]["pods"]]
        frames[-1][0]["uid"] = "foreign-tenant-uid"
        value, streams = owner.outputs.changed(owner.tree(self.inputs["capture"]), self.inputs["streams"], -1,
            stdout=b"".join(owner.outputs.encoded(frame) for frame in frames))
        self.before_git_refusal(capture=owner.snapshot(value), streams=streams, inspected=True)

    def test_foreign_sources_refs_and_raw_stream_inconsistency_refuse_before_git(self):
        sources = self.inputs["sources"]
        for changed in (replace(sources[0], source_commit="f" * 40),
                        replace(sources[0], executed_bytes=sources[0].executed_bytes + b"x"),
                        replace(sources[0], blob_bytes=sources[0].blob_bytes + b"x")):
            self.before_git_refusal(sources=(changed,) + sources[1:], inspected=True)
        for name in ("statement", "command_contract", "review_role_policy"):
            self.before_git_refusal(**{name: JsonSnapshot(self.inputs[name].raw + b" ")}, inspected=True)
        streams = self.inputs["streams"]
        self.before_git_refusal(streams=(replace(streams[0], stdout=streams[0].stdout + b"x"),) + streams[1:], inspected=True)

    def test_unsafe_missing_linked_and_nonprivate_current_files_refuse_before_git(self):
        for path in (self.files.plan_path, self.files.record_path, self.files.status_path):
            original = path.read_bytes()
            for unsafe in ("missing", "oversized", "nonprivate", "hardlink", "symlink"):
                with self.subTest(path=path.name, unsafe=unsafe):
                    alias = self.files.custody / "alias"
                    if unsafe == "missing":
                        path.unlink()
                    elif unsafe == "oversized":
                        self.files.write_private(path, b"x" * (owner.ONE_MIB + 1))
                    elif unsafe == "nonprivate":
                        path.chmod(0o644)
                    elif unsafe == "hardlink":
                        os.link(path, alias)
                    else:
                        path.rename(alias)
                        path.symlink_to(alias)
                    self.before_git_refusal(inspected=False)
                    if path.is_symlink():
                        path.unlink()
                    if alias.exists():
                        alias.unlink()
                    self.files.write_private(path, original)

    def test_acquired_run_and_registry_exact_one_mib_and_full_occurrence_boundary(self):
        raw = self.inputs["run"].raw
        self.inputs["run"] = JsonSnapshot(b" " * (owner.ONE_MIB - len(raw)) + raw)
        raw = self.inputs["registry"].raw
        self.inputs["registry"] = JsonSnapshot(b" " * (owner.ONE_MIB - len(raw)) + raw)
        self.files.plan["registrySha256"] = self.inputs["registry"].sha256
        self.files.prepare(record_raw=self.inputs["run"].raw)
        result = self.verify()
        self.assertEqual(len(result.inspection.owner_run.snapshot.raw), owner.ONE_MIB)
        self.assertEqual(len(result.inspection.registration.source.registry.snapshot.raw), owner.ONE_MIB)
        self.inputs = self.checkout.inputs(count=5)
        self.prepare()
        remaining = owner.THIRTY_TWO_MIB - owner.OwnerC115Tests.byte_count(self.inputs)
        expanded = []
        for stream in self.inputs["streams"]:
            stderr_size = min(remaining, owner.ONE_MIB)
            remaining -= stderr_size
            padding = min(remaining, owner.ONE_MIB - len(stream.stdout))
            remaining -= padding
            expanded.append(replace(stream, stdout=stream.stdout + b" " * padding, stderr=b"x" * stderr_size))
        self.assertEqual(remaining, 0)
        streams = tuple(expanded)
        self.inputs.update(streams=streams, capture=owner.snapshot(owner.outputs.bind(owner.tree(self.inputs["capture"]), streams)))
        self.assertEqual(self.verify().inspection.retained_byte_count, owner.THIRTY_TWO_MIB)
        self.before_git_refusal(streams=streams[:-1] + (replace(streams[-1], stderr=streams[-1].stderr + b"x"),),
                                inspected=True, code="aggregate-byte-budget-exceeded")

    def test_mid_git_withdrawal_discards_result_after_cleaning_held_files_and_children(self):
        real_popen = subprocess.Popen
        children = []

        def child(*args, **kwargs):
            process = real_popen(*args, **kwargs)
            children.append(process)
            if len(children) == 1:
                self.files.write_private(self.files.status_path, custody.wire({**self.files.status, "withdrawn": True}))
            return process

        with self.tracked_custody(), patch.object(local.subprocess, "Popen", side_effect=child):
            self.refuse(code="current-status-drifted")
        self.assertEqual(len(children), 140)
        self.assertTrue(all(process.poll() is not None and process.stdout.closed for process in children))

    def test_final_readback_withdrawal_after_successful_git_discards_computed_result(self):
        real_read = os.read
        plan_inode = self.files.plan_path.stat().st_ino
        plan_reads, computed = [], []
        real_corroborate = provenance._corroborate_owner_inspection

        def corroborate(*args, **kwargs):
            computed.append(real_corroborate(*args, **kwargs))
            return computed[-1]

        def read(descriptor, count):
            if os.fstat(descriptor).st_ino == plan_inode:
                plan_reads.append(descriptor)
                if len(plan_reads) == 3:
                    self.assertEqual(len(computed), 1)
                    self.files.write_private(self.files.status_path, custody.wire({**self.files.status, "withdrawn": True}))
            return real_read(descriptor, count)

        with self.tracked_custody(), patch.object(held_files.os, "read", side_effect=read), \
                patch.object(provenance, "_corroborate_owner_inspection", side_effect=corroborate):
            self.refuse(code="current-status-drifted")
        self.assertEqual(len(computed), 1)

    def test_same_size_coarse_timestamp_plan_run_and_status_drift_discard_local_result(self):
        real_fstat, real_stat = os.fstat, os.stat
        real_corroborate = provenance._corroborate_owner_inspection
        for path, original in ((self.files.plan_path, self.files.plan_bytes),
                               (self.files.record_path, self.inputs["run"].raw),
                               (self.files.status_path, custody.wire(self.files.status))):
            metadata = real_stat(path)
            inode = metadata.st_ino

            def coarse(value):
                if value.st_ino == inode:
                    return custody.metadata_copy(value, st_mtime_ns=metadata.st_mtime_ns, st_ctime_ns=metadata.st_ctime_ns)
                return value

            def corroborate(*args, **kwargs):
                result = real_corroborate(*args, **kwargs)
                self.files.write_private(path, original.replace(b"a", b"e", 1))
                return result

            with self.subTest(path=path.name), self.tracked_custody(), \
                    patch.object(held_files.os, "fstat", side_effect=lambda fd: coarse(real_fstat(fd))), \
                    patch.object(held_files.os, "stat", side_effect=lambda *a, **k: coarse(real_stat(*a, **k))), \
                    patch.object(provenance, "_corroborate_owner_inspection", side_effect=corroborate):
                self.refuse(code="artifact-bytes-drifted")
            self.files.write_private(path, original)

    def test_ancestor_movement_after_local_result_refuses_and_closes_all_descriptors(self):
        moved = self.files.custody.with_name("moved-custody")
        real_corroborate = provenance._corroborate_owner_inspection

        def corroborate(*args, **kwargs):
            result = real_corroborate(*args, **kwargs)
            self.files.custody.rename(moved)
            return result

        try:
            with self.tracked_custody(), patch.object(provenance, "_corroborate_owner_inspection", side_effect=corroborate):
                self.refuse()
        finally:
            moved.rename(self.files.custody)

    def test_dirty_wrong_commit_and_unsafe_standalone_checkout_refuse(self):
        source = self.root / self.checkout.original_sources[0].path
        original = source.read_bytes()
        source.write_bytes(original.replace(b"I", b"i", 1))
        with self.tracked_custody():
            self.refuse(code="local-repository-dirty")
        source.write_bytes(original)
        checkout.legacy.git(self.root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                            "commit", "--allow-empty", "-qm", "test: isolated newer commit")
        self.refuse(code="local-head-commit-mismatch")
        gitdir = self.root / ".git"
        moved = self.root / "gitdir"
        gitdir.rename(moved)
        gitdir.write_text("gitdir: " + str(moved) + "\n", encoding="utf-8")
        with patch.object(local, "_owner_git") as git:
            self.refuse(code="local-git-directory-required")
        git.assert_not_called()

    def test_local_git_deadline_failure_cleans_child_and_all_held_descriptors(self):
        children = []
        real_popen = subprocess.Popen

        def child(*args, **kwargs):
            process = real_popen(*args, **kwargs)
            children.append(process)
            return process

        with self.tracked_custody(), patch.object(local.subprocess, "Popen", side_effect=child), \
                patch.object(local.select, "select", return_value=([], [], [])):
            self.refuse(code="local-git-unavailable")
        self.assertEqual(len(children), 1)
        self.assertIsNotNone(children[0].poll())
        self.assertTrue(children[0].stdout.closed)

    def test_child_output_cleanup_fault_refuses_after_closing_files_and_child(self):
        real_popen = subprocess.Popen
        children, faults = [], []
        with ExitStack() as patches:
            def child(*args, **kwargs):
                process = real_popen(*args, **kwargs)
                children.append(process)
                real_close = process.stdout.close

                def close():
                    real_close()
                    try:
                        raise ValueError("private-canary-child-context")
                    except ValueError:
                        error = OSError("private-canary-child-cleanup")
                        error.__cause__ = RuntimeError("private-canary-child-cause")
                        faults.append(error)
                        raise error

                patches.enter_context(patch.object(process.stdout, "close", side_effect=close))
                return process

            with self.tracked_custody(), patch.object(local.subprocess, "Popen", side_effect=child):
                self.refuse(code="local-source-unavailable")
        self.assertEqual(len(children), 1)
        self.assertEqual(len(faults), 1)
        self.assertIsNotNone(faults[0].__cause__)
        self.assertIsNotNone(faults[0].__context__)
        self.assertIsNotNone(children[0].poll())
        self.assertTrue(children[0].stdout.closed)

    def test_acquisition_seek_and_cleanup_faults_are_content_free_after_complete_cleanup(self):
        real = {name: getattr(os, name) for name in ("open", "read", "lseek", "close")}
        status_inode = self.files.status_path.stat().st_ino
        for operation in real:
            opened, closed, faults = [], [], []

            def fail():
                try:
                    raise ValueError("private-canary-context")
                except ValueError:
                    error = OSError("private-canary-filesystem")
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
                if operation == "read" and os.fstat(descriptor).st_ino == status_inode:
                    fail()
                return real["read"](descriptor, count)

            def seek(descriptor, offset, whence):
                if operation == "lseek" and os.fstat(descriptor).st_ino == status_inode:
                    fail()
                return real["lseek"](descriptor, offset, whence)

            def close(descriptor):
                closed.append(descriptor)
                real["close"](descriptor)
                if operation == "close" and not faults:
                    fail()

            # Keep these OS fault probes on the held stack; native child faults
            # and real successful-child cleanup have their own dedicated cases.
            with self.subTest(operation=operation), \
                    patch.object(held_files.os, "open", side_effect=open_file), \
                    patch.object(held_files.os, "read", side_effect=read), \
                    patch.object(held_files.os, "lseek", side_effect=seek), \
                    patch.object(held_files.os, "close", side_effect=close), \
                    patch.object(provenance, "_corroborate_inspected_sources", return_value=("a" * 40,) * 19):
                self.refuse(code="filesystem-unavailable")
            self.assertEqual(len(faults), 1)
            self.assertIsNotNone(faults[0].__cause__)
            self.assertIsNotNone(faults[0].__context__)
            self.assertEqual(opened[::-1], closed)
            for descriptor in opened:
                with self.assertRaises(OSError):
                    os.fstat(descriptor)

    def test_prebound_process_network_and_native_clock_barriers_count_attempts(self):
        for module in (provenance, plan_files, held_files, dispatch_files):
            for attribute, dependency, attempt in (
                ("_probe_process", subprocess.run, lambda: module._probe_process(["never-executed"])),
                ("_probe_clock", datetime.now, lambda: module._probe_clock()),
                ("_probe_network", resolve_name("socket.socket"), lambda: module._probe_network()),
            ):
                with self.subTest(module=module.__name__, attribute=attribute), \
                        patch.object(module, attribute, dependency, create=True):
                    with self.assertRaisesRegex(AssertionError, "was called"):
                        with self.forbidden_calls() as calls:
                            attempt()
                    self.assertEqual(sum(calls.values()), 1)

    def test_deployed_binding_refusal_is_unchanged(self):
        with checkout.denials() as calls, self.assertRaisesRegex(ValueError, "deployed-producer-binding-unavailable"):
            owner.lookup_deployed_binding("C1.15", "PG-ONPREM-2")
        self.assertEqual(calls, {})


if __name__ == "__main__":
    unittest.main()
