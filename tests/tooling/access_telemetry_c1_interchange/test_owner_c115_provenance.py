"""Owner inputs and disposable Git checkouts; no target or acceptance authority."""

from __future__ import annotations

from contextlib import ExitStack, contextmanager
from dataclasses import FrozenInstanceError, fields, replace
from datetime import date, datetime, timedelta, timezone
import hashlib
import io
import os
from pathlib import Path
from pkgutil import resolve_name
import shlex
import signal
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))

import access_telemetry_c1_owner_c115_provenance as provenance  # noqa: E402
import access_telemetry_c1_source_provenance as local  # noqa: E402
import test_owner_c115 as owner  # noqa: E402
import test_registration_provenance as legacy  # noqa: E402
from access_telemetry_c1_interchange import InterchangeFormatError, JsonSnapshot  # noqa: E402
from access_telemetry_c1_producer_bindings import lookup_deployed_binding  # noqa: E402


MODULES = (provenance, local, owner.owner_inspector, *owner.COMPOSED_MODULES)
DEPENDENCIES = (*owner.DEPENDENCIES, "os.dup", "os.fstat", "os.read", "os.close", "select.select")


@contextmanager
def denials():
    """Count ambient and prebound local dependencies before the local lane."""
    calls = {}

    def forbidden(name):
        def call(*args, **kwargs):
            calls[name] = calls.get(name, 0) + 1
            raise AssertionError(name + " was called")
        return call

    class ForbiddenEnvironment:
        __getitem__ = forbidden("os.environ")
        __iter__ = forbidden("os.environ")
        __len__ = forbidden("os.environ")
        __contains__ = forbidden("os.environ")
        get = forbidden("os.environ")
        copy = forbidden("os.environ")

    known = [(name, resolve_name(name)) for name in DEPENDENCIES]
    clock_methods = {date.today: "date.today", datetime.now: "datetime.now",
                     datetime.utcnow: "datetime.utcnow", datetime.today: "datetime.today"}
    known.extend((name, dependency) for dependency, name in clock_methods.items())
    environment = os.environ
    previous = sys.getprofile()

    def clock_profile(frame, event, argument):
        if event == "c_call":
            name = clock_methods.get(argument)
            if name is not None:
                forbidden(name)()

    with ExitStack() as stack:
        for module in MODULES:
            for attribute, alias in tuple(vars(module).items()):
                if alias is environment:
                    stack.enter_context(patch.object(module, attribute, ForbiddenEnvironment()))
                    continue
                for name, original in known:
                    if type(alias) is type(original) and (alias is original or alias == original):
                        stack.enter_context(patch.object(module, attribute, side_effect=forbidden(name)))
                        break
        for name in DEPENDENCIES:
            stack.enter_context(patch(name, side_effect=forbidden(name)))
        stack.enter_context(patch("os.environ", ForbiddenEnvironment()))
        for name in ("_git", "_owner_git", "_open_directory", "_owner_metadata", "_assert_safe_config"):
            stack.enter_context(patch.object(local, name, side_effect=forbidden(name)))
        sys.setprofile(clock_profile)
        try:
            yield calls
        finally:
            sys.setprofile(previous)


class OwnerC115ProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="isolated-owner-c115-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        legacy.git(self.root, "init", "-q")
        (self.root / ".gitattributes").write_text("* text eol=crlf\n", encoding="utf-8")
        self.original_sources = owner.bindings.fixture_sources()
        for source in self.original_sources:
            target = self.root / source.path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(source.executed_bytes)
        entry = owner.bindings.registry_entry()
        self.auxiliary = (entry["registeredStory"], entry["verifierPath"])
        for path in self.auxiliary:
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(("# ISOLATED OWNER FIXTURE: " + path + "\r\n").encode())
        self.commit_checkout()

    def commit_checkout(self):
        legacy.git(self.root, "add", ".")
        legacy.git(self.root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                   "commit", "-qm", "test: isolated owner sources")
        self.commit = legacy.git(self.root, "rev-parse", "HEAD")

    def inputs(self, names=("lifecycle-1", "lifecycle-2"), *, count=None, sources=None, entry=None):
        inputs = owner.fixture(names, count=count)
        sources = self.original_sources if sources is None else sources
        sources = tuple(replace(source, source_commit=self.commit) for source in sources)
        capture = owner.source_receipts(owner.tree(inputs["capture"]), sources)
        capture["sourceCommit"] = self.commit
        run = {**owner.tree(inputs["run"]), "sourceCommit": self.commit}
        inputs.update(capture=owner.snapshot(capture), sources=sources, run=owner.snapshot(run))
        inputs["anchors"] = replace(inputs["anchors"], source_commit=self.commit,
                                    record_sha256=inputs["run"].sha256)
        registry, statement, command, role = owner.materials(
            owner.bindings.registry_entry() if entry is None else entry, run)
        inputs.update(registry=registry, statement=statement, command_contract=command,
                      review_role_policy=role, approved_registry_sha256=registry.sha256)
        return inputs

    def verify(self, inputs=None, *, repository_root=None, **changes):
        return provenance.corroborate_owner_c115_sources(
            **{**(self.inputs() if inputs is None else inputs), **changes},
            repository_root=str(self.root) if repository_root is None else repository_root)

    def refuse(self, inputs=None, *, code=None, **changes):
        with self.assertRaises(provenance.OwnerC115LocalSourceCorroborationError) as caught:
            self.verify(inputs, **changes)
        error = caught.exception
        self.assertRegex(str(error), r"\Aowner-c115-[a-z0-9-]+\Z")
        self.assertLess(len(str(error)), 120)
        for sensitive in ("private-test-credential", "ISOLATED", str(self.root)):
            self.assertNotIn(sensitive, str(error))
        self.assertIsNone(error.__cause__)
        self.assertIsNone(error.__context__)
        self.assertTrue(error.__suppress_context__)
        if code is not None:
            self.assertEqual(str(error), code)
        return error

    def pure_refusal(self, inputs, **changes):
        with denials() as calls:
            error = self.refuse(inputs, **changes)
            self.assertEqual(calls, {})
        return error

    @contextmanager
    def local_lane(self, inputs):
        """Only exact bounded Git argv/env is allowed; reinspection remains pure."""
        calls = []
        forbidden_calls = {}
        real_popen = subprocess.Popen
        real_inspector = provenance.inspect_owner_c115
        prefix = ("/usr/bin/python3", "-I", "-S", "-B", "-c", local._OWNER_LAUNCHER,
                  *local._OWNER_GIT_PREFIX)
        environment = {"PATH": os.defpath, "GIT_CONFIG_NOSYSTEM": "1", "GIT_CONFIG_GLOBAL": os.devnull,
                       "GIT_NO_LAZY_FETCH": "1", "GIT_NO_REPLACE_OBJECTS": "1",
                       "GIT_OPTIONAL_LOCKS": "0", "GIT_TERMINAL_PROMPT": "0"}
        revision = [("rev-parse", "--show-toplevel"), ("rev-parse", "--verify", "HEAD^{commit}"),
                    ("ls-files", "-v", "-z"),
                    ("status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=all")]
        paths = tuple(sorted(source.path for source in inputs["sources"]))
        entry = owner.tree(inputs["registry"])[0]
        auxiliary = (entry["registeredStory"], entry["verifierPath"])
        tracked_paths = tuple(legacy.git(self.root, "-c", "core.fsmonitor=false", "ls-files").splitlines())
        safety = [("ls-files", "--stage", "-z"),
                  ("check-attr", "-z", "filter", "--", *tracked_paths)]
        gitlinks = []
        for record in legacy.git(self.root, "-c", "core.fsmonitor=false", "ls-files", "--stage").splitlines():
            if record.startswith("160000 "):
                path = record.split("\t")[1]
                gitlinks.append(("ls-tree", "-z", "HEAD", "--", path))
        safety[1:1] = gitlinks
        expected = [*safety, *revision]

        def index(path):
            return ("ls-files", "-v", "-z", "--", path)

        def attributes(path):
            return ("check-attr", "-z", "text", "eol", "filter", "ident", "working-tree-encoding", "--", path)

        for path in (*paths, *auxiliary):
            oid = legacy.git(self.root, "-c", "core.fsmonitor=false", "rev-parse", f"{self.commit}:{path}")
            tree = ("ls-tree", "-z", self.commit, "--", path)
            expected.extend((index(path), tree) if path in paths else (tree, index(path)))
            expected.extend((("cat-file", "blob", oid), attributes(path)))
        for path in (*paths, *auxiliary):
            expected.extend((index(path), attributes(path)))
        expected.extend((*safety, *revision, *safety))

        def guarded_child(command, **kwargs):
            calls.append(tuple(command[len(prefix):]))
            self.assertEqual(tuple(command[:len(prefix)]), prefix)
            self.assertEqual(calls[-1], expected[len(calls) - 1])
            self.assertEqual(set(kwargs), {"cwd", "pass_fds", "env", "stdin", "stdout", "stderr", "start_new_session"})
            self.assertEqual(kwargs["env"], environment)
            self.assertEqual(kwargs["stdin"], subprocess.DEVNULL)
            self.assertIs(kwargs["start_new_session"], True)
            self.assertEqual(kwargs["stdout"], subprocess.PIPE)
            self.assertEqual(kwargs["stderr"], subprocess.DEVNULL)
            self.assertEqual(len(kwargs["pass_fds"]), 1)
            descriptor = kwargs["pass_fds"][0]
            self.assertIs(type(descriptor), int)
            self.assertEqual(kwargs["cwd"], f"/proc/self/fd/{descriptor}")
            self.assertEqual(os.readlink(kwargs["cwd"]), str(self.root))
            return real_popen(command, **kwargs)

        def reinspect(*args, **kwargs):
            with denials() as pure_calls:
                inspection = real_inspector(*args, **kwargs)
                self.assertEqual(pure_calls, {})
            return inspection

        def forbidden(name):
            def call(*args, **kwargs):
                forbidden_calls[name] = forbidden_calls.get(name, 0) + 1
                raise AssertionError(name + " was called")
            return call

        with ExitStack() as stack:
            stack.enter_context(patch.object(provenance, "inspect_owner_c115", side_effect=reinspect))
            stack.enter_context(patch.object(local.subprocess, "Popen", side_effect=guarded_child))
            for name in ("socket.socket", "socket.create_connection", "socket.getaddrinfo",
                         "urllib.request.urlopen", "urllib.request.OpenerDirector.open", "os.system",
                         "os.popen", "os.getenv", "shutil.which", "subprocess.run", "subprocess.call",
                         "subprocess.check_call", "subprocess.check_output", "time.time", "time.time_ns",
                         "time.perf_counter", "time.perf_counter_ns"):
                stack.enter_context(patch(name, side_effect=forbidden(name)))
            yield calls
        self.assertEqual(calls, expected)
        self.assertEqual(len(calls), 140 + 3 * len(gitlinks))
        self.assertEqual(forbidden_calls, {})

    def test_one_two_three_pods_reinspect_then_corroborate_exact_bounded_local_lane(self):
        for names in (("lifecycle-1",), ("lifecycle-2", "lifecycle-1"),
                      ("lifecycle-3", "lifecycle-2", "lifecycle-1")):
            with self.subTest(pods=names):
                inputs = self.inputs(names)
                with self.local_lane(inputs):
                    result = self.verify(inputs)
                self.assertIs(type(result), provenance.OwnerC115LocalSourceCorroboration)
                self.assertEqual(result.source_commit, self.commit)
                self.assertEqual(result.repository_root, str(self.root))
                self.assertEqual(result.paths, tuple(sorted(owner.bindings.PATHS)))
                self.assertEqual(result.git_blob_oids, tuple(legacy.git(self.root, "rev-parse", f"{self.commit}:{path}")
                                                           for path in result.paths))
                inspection = result.inspection
                self.assertEqual(inspection.commands.pod_names, names)
                self.assertEqual(inspection.retained_streams.pod_names, names)
                self.assertEqual(inspection.retained_byte_count, owner.OwnerC115Tests.byte_count(inputs))
                self.assertEqual((result.owner_principal, result.assurance_mode),
                                 ("github:user:6775094", "sole-owner/self-reviewed"))
                self.assertEqual(inspection.owner_run.requested_action, "observe")
                self.assertIs(inspection.commands.capture, inspection.registration.source.capture)
                self.assertIs(inspection.retained_streams.capture, inspection.commands.capture)
                for name, retained in (("registry", inspection.registration.source.registry.snapshot),
                                       ("capture", inspection.commands.capture), ("run", inspection.owner_run.snapshot),
                                       ("statement", inspection.registration.statement),
                                       ("command_contract", inspection.registration.command_contract),
                                       ("review_role_policy", inspection.registration.review_role_policy)):
                    self.assertIsNot(retained, inputs[name])
                    self.assertEqual(retained.raw, inputs[name].raw)
                    self.assertEqual(retained.sha256, hashlib.sha256(inputs[name].raw).hexdigest())

    def test_results_are_frozen_concealed_and_have_no_gate_or_execution_verdict(self):
        result = self.verify()
        for obj in (result, result.inspection, result.inspection.registration):
            for attribute in fields(obj):
                with self.assertRaises(FrozenInstanceError):
                    setattr(obj, attribute.name, None)
            for name in ("accepted", "passed", "execute", "authority", "execution_handle"):
                self.assertFalse(hasattr(obj, name))
        with self.assertRaises(TypeError):
            result.paths[0] = "foreign"
        for value in ("private-test-credential", "ISOLATED", "kubectl", "fixtures/owner"):
            self.assertNotIn(value, repr(result))
        self.assertIn("sole-owner/self-reviewed", repr(result))

    def test_forged_snapshot_caches_are_discarded_for_all_six_materials(self):
        inputs = self.inputs()
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            retained = JsonSnapshot(inputs[name].raw)
            object.__setattr__(retained, "value", {"forged": "private-test-credential"})
            object.__setattr__(retained, "sha256", "f" * 64)
            result = self.verify(inputs, **{name: retained})
            self.assertEqual(result.inspection.commands.capture.raw, inputs["capture"].raw)
            self.assertEqual(result.inspection.owner_run.snapshot.raw, inputs["run"].raw)
            self.assertEqual(result.inspection.registration.statement.raw, inputs["statement"].raw)

    def test_local_lane_uses_only_detached_inspection_despite_caller_mutation(self):
        inputs = self.inputs()
        real_local = provenance._corroborate_inspected_sources

        def mutate_then_read(source, sources, root, **kwargs):
            self.assertEqual(kwargs, {"owner_safety_preflight": True})
            self.assertIsNot(sources, inputs["sources"])
            for supplied, detached in zip(inputs["sources"], sources):
                self.assertIsNot(supplied, detached)
                self.assertEqual(supplied, detached)
                for attribute in fields(supplied):
                    object.__setattr__(supplied, attribute.name, None)
            for retained in (inputs["capture"], inputs["registry"], inputs["run"], inputs["statement"]):
                object.__setattr__(retained, "raw", b"private-test-credential")
            for stream in inputs["streams"]:
                object.__setattr__(stream, "stdout", b"private-test-credential")
            return real_local(source, sources, root, **kwargs)

        with patch.object(provenance, "_corroborate_inspected_sources", side_effect=mutate_then_read):
            result = self.verify(inputs)
        self.assertEqual(result.source_commit, self.commit)
        self.assertTrue(all(source.executed_bytes for source in result.inspection.sources))
        self.assertNotIn(b"private-test-credential", result.inspection.commands.capture.raw)
        self.assertNotIn(b"private-test-credential", result.inspection.retained_streams.streams[0].stdout)

    def test_prebound_and_ambient_dependency_barriers_detect_attempts(self):
        probes = (("_probe_file", io.FileIO, "io.FileIO", lambda module: module._probe_file("/never-opened", "r")),
                  ("_probe_process", subprocess.Popen, "subprocess.Popen", lambda module: module._probe_process(["never"])),
                  ("_probe_clock", time.time, "time.time", lambda module: module._probe_clock()),
                  ("_probe_now", datetime.now, "datetime.now", lambda module: module._probe_now()),
                  ("_probe_datetime", datetime, "datetime.now", lambda module: module._probe_datetime.now()),
                  ("_probe_date", date, "date.today", lambda module: module._probe_date.today()),
                  ("_probe_environment", os.environ, "os.environ", lambda module: module._probe_environment.get("NEVER")),
                  ("_probe_stat", os.stat, "os.stat", lambda module: module._probe_stat("/never-statted")))
        for module in MODULES:
            for attribute, dependency, name, attempt in probes:
                with self.subTest(module=module.__name__, dependency=name):
                    with patch.object(module, attribute, dependency, create=True), denials() as calls:
                        with self.assertRaisesRegex(AssertionError, "was called"):
                            attempt(module)
                        self.assertEqual(calls, {name: 1})
        with denials() as calls:
            with self.assertRaisesRegex(AssertionError, "was called"):
                Path("/never-opened").read_bytes()
            self.assertEqual(calls, {"pathlib.Path.read_bytes": 1})

    def test_rehashed_cross_namespace_tenant_session_and_commit_refuse_before_local_access(self):
        inputs = self.inputs()
        for name, replacement in (("namespace", "foreign-tenant-namespace"), ("context", "foreign-cluster"),
                                  ("selector", "tenant=foreign"), ("appId", "foreign-tenant-app"),
                                  ("actorType", "ForeignTenantActor")):
            value = owner.tree(inputs["capture"])
            value["target"][name] = replacement
            if name in ("appId", "actorType"):
                for pod in value["observations"]["pods"]:
                    pod["appId" if name == "appId" else "actorTypes"] = replacement if name == "appId" else [replacement]
            owner.semantics.refresh_declarations(value)
            self.pure_refusal(inputs, capture=owner.snapshot(value), code="owner-c115-capture-run-scope-mismatch")
        for field, changed in (("qualificationSessionId", "foreign-tenant-session"), ("sourceCommit", "f" * 40)):
            value = owner.tree(inputs["capture"])
            value[field] = changed
            if field == "qualificationSessionId":
                value["producer"]["arguments"][5] = changed
                value["producer"]["argumentsSha256"] = owner.semantics.fixture_hash(value["producer"]["arguments"])
            self.pure_refusal(inputs, capture=owner.snapshot(value))

    def test_rehashed_later_pod_argv_output_and_final_recheck_drift_refuse_before_local_access(self):
        inputs = self.inputs(("lifecycle-1", "lifecycle-2", "lifecycle-3"))
        for number, position, changed in ((8, 5, "foreign-pod"), (9, 3, "foreign-namespace"),
                                          (10, 5, "lifecycle-1"), (9, 11, "printf harmless"), (8, 4, "delete")):
            value = owner.tree(inputs["capture"])
            value["commands"][number]["arguments"][position] = changed
            owner.commands.rehash(value["commands"][number])
            self.pure_refusal(inputs, capture=owner.snapshot(value), code="owner-c115-c115-command-arguments-mismatch")
        for number, raw in ((8, b"1.18.2\n"), (9, owner.outputs.encoded({"id": "foreign-tenant-app"})),
                            (10, b"true\ntrue\n")):
            value, streams = owner.outputs.changed(owner.tree(inputs["capture"]), inputs["streams"], number, stdout=raw)
            self.pure_refusal(inputs, capture=owner.snapshot(value), streams=streams)
        frames = [owner.outputs.pod_frame(pod) for pod in owner.tree(inputs["capture"])["observations"]["pods"]]
        frames[-1][0]["uid"] = "foreign-pod-uid"
        value, streams = owner.outputs.changed(owner.tree(inputs["capture"]), inputs["streams"], -1,
                                               stdout=b"".join(owner.outputs.encoded(frame) for frame in frames))
        self.pure_refusal(inputs, capture=owner.snapshot(value), streams=streams,
                          code="owner-c115-c115-stream-recheck-mismatch")

    def test_run_withdrawn_missing_expired_unknown_status_and_observe_excluded_refuse_before_local_access(self):
        inputs = self.inputs()
        for changes in ({"run": None}, {"withdrawn": True}, {"withdrawn": None}, {"withdrawn": 0},
                        {"now": owner.owner_runs.COLLECTION}, {"now": owner.owner_runs.EXPIRY},
                        {"now": owner.NOW.replace(tzinfo=None)},
                        {"now": owner.NOW.replace(tzinfo=timezone(timedelta(hours=1)))}):
            self.pure_refusal(inputs, **changes)
        for changes in ({"actions": ["fault"]}, {"status": "draft"}, {"ownerPrincipal": "foreign-owner"},
                        {"assuranceMode": "independently-reviewed"}):
            run = owner.snapshot({**owner.tree(inputs["run"]), **changes})
            self.pure_refusal(inputs, run=run, anchors=replace(inputs["anchors"], record_sha256=run.sha256))

    def test_time_start_and_now_seventh_digit_boundaries(self):
        inputs = self.inputs()
        self.verify(inputs, now=owner.NOW.replace(second=1, microsecond=1))
        self.pure_refusal(inputs, now=owner.NOW.replace(second=1), code="owner-c115-capture-outside-collection")
        value = owner.tree(inputs["capture"])
        value["capturedAtUtc"] = value["producer"]["startedAtUtc"] = "2026-10-12T10:00:00.0000000+00:00"
        self.verify(inputs, capture=owner.snapshot(value))
        value["capturedAtUtc"] = value["producer"]["startedAtUtc"] = "2026-10-12T09:59:59.9999999+00:00"
        self.pure_refusal(inputs, capture=owner.snapshot(value), code="owner-c115-capture-outside-collection")
        self.pure_refusal(inputs, now=owner.owner_runs.START - timedelta(microseconds=1))

    def test_approval_anchors_owner_policy_profile_workload_and_raw_registry_digest_stay_bound(self):
        inputs = self.inputs()
        for name, changed in (("record_sha256", "f" * 64), ("policy_sha256", "f" * 64),
                              ("session_id", "foreign-session"), ("source_commit", "f" * 40),
                              ("target_sha256", "f" * 64), ("custody_root", "/foreign/custody")):
            self.pure_refusal(inputs, anchors=replace(inputs["anchors"], **{name: changed}))
        self.pure_refusal(inputs, approved_registry_sha256="f" * 64)
        self.pure_refusal(inputs, registry=JsonSnapshot(inputs["registry"].raw + b" "))
        for name in ("profileSha256", "workloadSha256"):
            self.pure_refusal(inputs, capture=owner.snapshot({**owner.tree(inputs["capture"]), name: "f" * 64}))
        for material in ("statement", "review_role_policy"):
            for name in owner.OWNER_FIELDS:
                value = {**owner.tree(inputs[material]), name: "f" * 64 if name == "policySha256" else "foreign-owner"}
                self.pure_refusal(owner.rebind_material(inputs, material, value))

    def test_all_refs_and_source_drift_refuse_before_local_access(self):
        inputs = self.inputs()
        for name in ("statement", "command_contract", "review_role_policy"):
            self.pure_refusal(inputs, **{name: JsonSnapshot(inputs[name].raw + b" ")})
        for ref in ("registrationReceipt", "commandContract", "reviewRolePolicy"):
            for field, changed in (("sha256", "f" * 64), ("byteLength", 1)):
                entries = owner.tree(inputs["registry"])
                entries[0][ref][field] = changed
                retained = owner.snapshot(entries)
                self.pure_refusal(inputs, registry=retained, approved_registry_sha256=retained.sha256)
        for field in ("commandContract", "reviewRolePolicy"):
            value = owner.tree(inputs["statement"])
            value[field]["path"] = "fixtures/foreign-owner/material.json"
            self.pure_refusal(owner.rebind_material(inputs, "statement", value))
        source = inputs["sources"][0]
        for changed in (replace(source, source_commit="f" * 40), replace(source, executed_bytes=source.executed_bytes + b"x"),
                        replace(source, blob_bytes=source.blob_bytes + b"x")):
            self.pure_refusal(inputs, sources=(changed,) + inputs["sources"][1:])

    def test_forged_safe_cache_cannot_admit_foreign_or_invalid_raw_bytes(self):
        inputs = self.inputs()
        value = {**owner.tree(inputs["capture"]), "sourceCommit": "f" * 40}
        retained = owner.snapshot(value)
        object.__setattr__(retained, "value", inputs["capture"].value)
        object.__setattr__(retained, "sha256", inputs["capture"].sha256)
        self.pure_refusal(inputs, capture=retained)
        for raw in (b"{", b"\xffprivate-test-credential", b'{"a":1,"a":2}'):
            retained = JsonSnapshot(inputs["capture"].raw)
            object.__setattr__(retained, "raw", raw)
            self.pure_refusal(inputs, capture=retained)

    def test_missing_slots_and_nonclosed_containers_refuse_before_local_access(self):
        inputs = self.inputs()
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            retained = JsonSnapshot(inputs[name].raw)
            object.__delattr__(retained, "raw")
            self.pure_refusal(inputs, **{name: retained}, code="owner-c115-required-field-missing")
        for name in ("sources", "streams"):
            for field in fields(inputs[name][0]):
                changed = replace(inputs[name][0])
                object.__delattr__(changed, field.name)
                self.pure_refusal(inputs, **{name: (changed,) + inputs[name][1:]})
            self.pure_refusal(inputs, **{name: list(inputs[name])})
        for field in fields(inputs["anchors"]):
            anchors = replace(inputs["anchors"])
            object.__delattr__(anchors, field.name)
            self.pure_refusal(inputs, anchors=anchors)

    def test_each_artifact_budget_refuses_before_reparse_hash_or_local_dependencies(self):
        inputs = self.inputs()
        candidates = []
        for name in ("registry", "capture", "run", "statement", "command_contract", "review_role_policy"):
            retained = JsonSnapshot(inputs[name].raw)
            object.__setattr__(retained, "raw", b"x" * (owner.ONE_MIB + 1))
            candidates.append({name: retained})
        for name in ("executed_bytes", "blob_bytes"):
            source = replace(inputs["sources"][0])
            object.__setattr__(source, name, b"x" * (owner.ONE_MIB + 1))
            candidates.append({"sources": (source,) + inputs["sources"][1:]})
        for name in ("stdout", "stderr"):
            stream = replace(inputs["streams"][0], **{name: b"x" * (owner.ONE_MIB + 1)})
            candidates.append({"streams": (stream,) + inputs["streams"][1:]})
        for changes in candidates:
            with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.pure_refusal(inputs, **changes, code="owner-c115-artifact-byte-budget-exceeded")

    def test_repeated_source_forms_and_stream_occurrences_exceed_aggregate_before_dependencies(self):
        inputs = self.inputs(count=11)
        repeated = b"x" * owner.ONE_MIB
        for changes in ({"sources": tuple(replace(source, executed_bytes=repeated, blob_bytes=repeated)
                                           for source in inputs["sources"])},
                        {"streams": tuple(replace(stream, stderr=repeated) for stream in inputs["streams"])}):
            with patch.object(JsonSnapshot, "__post_init__", side_effect=AssertionError("reparsed")), \
                    patch("hashlib.sha256", side_effect=AssertionError("hashed")):
                self.pure_refusal(inputs, **changes, code="owner-c115-aggregate-byte-budget-exceeded")

    def test_exact_one_mib_json_source_and_stream_forms_reach_real_local_corroboration(self):
        inputs = self.inputs()
        padded = JsonSnapshot(b" " * (owner.ONE_MIB - len(inputs["registry"].raw)) + inputs["registry"].raw)
        self.verify(inputs, registry=padded, approved_registry_sha256=padded.sha256)
        value, streams = owner.outputs.changed(owner.tree(inputs["capture"]), inputs["streams"], 0,
            stdout=inputs["streams"][0].stdout + b" " * (owner.ONE_MIB - len(inputs["streams"][0].stdout)),
            stderr=b"x" * owner.ONE_MIB)
        self.verify(inputs, capture=owner.snapshot(value), streams=streams)
        raw = b"# ISOLATED OWNER SOURCE\n" + b"x" * (owner.ONE_MIB - len(b"# ISOLATED OWNER SOURCE\n"))
        source = replace(self.original_sources[0], executed_bytes=raw, blob_bytes=raw)
        (self.root / source.path).write_bytes(raw)
        self.commit_checkout()
        inputs = self.inputs(sources=(source,) + self.original_sources[1:])
        self.assertGreater(self.verify(inputs).inspection.retained_byte_count, 2 * owner.ONE_MIB)

    def test_exact_32_mib_occurrence_total_matches_local_checkout_and_one_byte_over_refuses(self):
        inputs = self.inputs(count=5)
        remaining = owner.THIRTY_TWO_MIB - owner.OwnerC115Tests.byte_count(inputs)
        expanded = []
        for stream in inputs["streams"]:
            stderr_size = min(remaining, owner.ONE_MIB)
            remaining -= stderr_size
            padding = min(remaining, owner.ONE_MIB - len(stream.stdout))
            remaining -= padding
            expanded.append(replace(stream, stdout=stream.stdout + b" " * padding, stderr=b"x" * stderr_size))
        self.assertEqual(remaining, 0)
        streams = tuple(expanded)
        inputs.update(capture=owner.snapshot(owner.outputs.bind(owner.tree(inputs["capture"]), streams)), streams=streams)
        self.assertEqual(owner.OwnerC115Tests.byte_count(inputs), owner.THIRTY_TWO_MIB)
        self.assertEqual(self.verify(inputs).inspection.retained_byte_count, owner.THIRTY_TWO_MIB)
        last = replace(streams[-1], stderr=streams[-1].stderr + b"x")
        self.pure_refusal(inputs, streams=streams[:-1] + (last,), code="owner-c115-aggregate-byte-budget-exceeded")

    def test_relative_noncanonical_and_linked_roots_refuse(self):
        inputs = self.inputs()
        for root in (".", "/", str(self.root) + "/", str(self.root) + "/.", str(self.root) + "/../" + self.root.name,
                     str(self.root) + "\0private-test-credential"):
            self.refuse(inputs, repository_root=root)
        alias = self.root.parent / (self.root.name + "-alias")
        alias.symlink_to(self.root, target_is_directory=True)
        self.addCleanup(alias.unlink)
        self.refuse(inputs, repository_root=str(alias), code="owner-c115-local-source-unavailable")

    def test_foreign_checkout_dirty_tracked_untracked_and_wrong_head_refuse(self):
        inputs = self.inputs()
        with tempfile.TemporaryDirectory(prefix="isolated-foreign-owner-") as foreign:
            legacy.git(foreign, "init", "-q")
            self.refuse(inputs, repository_root=foreign)
        source = self.root / self.original_sources[0].path
        source.write_bytes(b"changed\r\n")
        self.refuse(inputs, code="owner-c115-local-repository-dirty")
        legacy.git(self.root, "checkout", "--", self.original_sources[0].path)
        untracked = self.root / "untracked.txt"
        untracked.write_text("private-test-credential", encoding="utf-8")
        self.refuse(inputs, code="owner-c115-local-repository-dirty")
        untracked.unlink()
        legacy.git(self.root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                   "commit", "--allow-empty", "-qm", "test: newer fixture head")
        self.refuse(inputs, code="owner-c115-local-head-commit-mismatch")

    def test_missing_blob_object_refuses_without_partial_result(self):
        inputs = self.inputs()
        oid = legacy.git(self.root, "rev-parse", f"{self.commit}:{self.original_sources[0].path}")
        (self.root / ".git" / "objects" / oid[:2] / oid[2:]).unlink()
        self.refuse(inputs, code="owner-c115-local-git-unavailable")

    def test_git_directory_files_links_alternates_and_foreign_toplevel_refuse(self):
        inputs = self.inputs()
        gitdir = self.root / ".git"
        moved = self.root / "gitdir"
        gitdir.rename(moved)
        for linked in (False, True):
            if linked:
                gitdir.symlink_to(moved, target_is_directory=True)
            else:
                gitdir.write_text("gitdir: " + str(moved) + "\n", encoding="utf-8")
            self.refuse(inputs, code="owner-c115-local-git-directory-required")
            gitdir.unlink()
        moved.rename(gitdir)
        alternates = gitdir / "objects" / "info" / "alternates"
        alternates.write_text("/never-read\n", encoding="utf-8")
        self.refuse(inputs, code="owner-c115-local-object-alternates-unsupported")
        alternates.unlink()
        real_git = local._owner_git

        def foreign_toplevel(fd, *args):
            return b"/foreign-checkout\n" if args == ("rev-parse", "--show-toplevel") else real_git(fd, *args)

        with patch.object(local, "_owner_git", side_effect=foreign_toplevel):
            self.refuse(inputs, code="owner-c115-foreign-repository-root")

    def test_symlink_hardlink_directory_aliases_and_modes_refuse(self):
        inputs = self.inputs()
        source = self.root / self.original_sources[0].path
        raw = source.read_bytes()
        target = self.root / ".git" / "info" / "source-alias"
        target.write_bytes(raw)
        real_git = local._owner_git

        def clean_status(fd, *args):
            return b"" if args[0] == "status" else real_git(fd, *args)

        source.unlink()
        source.symlink_to(target)
        with patch.object(local, "_owner_git", side_effect=clean_status):
            self.refuse(inputs, code="owner-c115-local-source-unavailable")
        source.unlink()
        source.write_bytes(raw)
        target.unlink()
        os.link(source, target)
        self.refuse(inputs, code="owner-c115-local-git-metadata-unsupported")
        target.unlink()
        source.chmod(0o755)
        with patch.object(local, "_owner_git", side_effect=clean_status):
            self.refuse(inputs, code="owner-c115-local-source-executed-bytes-mismatch")
        source.chmod(0o644)
        parent = source.parent
        moved = parent.with_name(parent.name + "-moved")
        parent.rename(moved)
        parent.symlink_to(moved, target_is_directory=True)
        with patch.object(local, "_owner_git", side_effect=clean_status):
            self.refuse(inputs, code="owner-c115-local-source-unavailable")

    def test_unsupported_attributes_and_index_flags_refuse_even_with_clean_status(self):
        inputs = self.inputs()
        path = self.original_sources[0].path
        attributes = self.root / ".git" / "info" / "attributes"
        for declaration in ("filter=foreign", "ident", "working-tree-encoding=UTF-16", "-text", "eol=foreign"):
            attributes.write_text(path + " " + declaration + "\n", encoding="utf-8")
            self.refuse(inputs)
        attributes.unlink()
        for flag, clear in (("--skip-worktree", "--no-skip-worktree"),
                            ("--assume-unchanged", "--no-assume-unchanged")):
            legacy.git(self.root, "update-index", flag, "--", path)
            self.refuse(inputs, code="owner-c115-local-repository-index-flags-unsupported")
            legacy.git(self.root, "update-index", clear, "--", path)

    def marker_command(self, marker):
        script = ("import pathlib,sys; pathlib.Path(" + repr(str(marker))
                  + ").write_text('clean-filter-executed'); sys.stdout.buffer.write(sys.stdin.buffer.read())")
        return shlex.quote(sys.executable) + " -c " + shlex.quote(script)

    def test_same_size_dirty_source_clean_filter_refuses_before_status_without_execution(self):
        inputs = self.inputs()
        marker = self.root / ".git" / "info" / "filter-marker"
        path = self.original_sources[0].path
        legacy.git(self.root, "config", "filter.marker.clean", self.marker_command(marker))
        legacy.git(self.root, "config", "filter.marker.required", "true")
        (self.root / ".git" / "info" / "attributes").write_text(path + " filter=marker\n", encoding="utf-8")
        source = self.root / path
        raw = source.read_bytes()
        source.write_bytes(b"!" + raw[1:])
        self.assertEqual(len(raw), source.stat().st_size)
        real_git = local._owner_git
        calls = []

        def counted(fd, *args):
            calls.append(args)
            return real_git(fd, *args)

        with patch.object(local, "_owner_git", side_effect=counted):
            self.refuse(inputs, code="owner-c115-local-git-configuration-unsupported")
        self.assertEqual(calls, [])
        self.assertFalse(marker.exists())

    def test_filters_on_unrelated_tracked_paths_refuse_before_status(self):
        unrelated = self.root / "unrelated.txt"
        unrelated.write_bytes(b"isolated unrelated file\r\n")
        self.commit_checkout()
        inputs = self.inputs()
        marker = self.root / ".git" / "info" / "unrelated-filter-marker"
        raw = unrelated.read_bytes()
        unrelated.write_bytes(b"!" + raw[1:])
        attributes = self.root / ".git" / "info" / "attributes"
        attributes.write_text("unrelated.txt filter=marker\n", encoding="utf-8")
        real_git = local._owner_git
        for configured in (False, True):
            if configured:
                legacy.git(self.root, "config", "filter.marker.process", self.marker_command(marker))
            calls = []

            def counted(fd, *args):
                calls.append(args)
                return real_git(fd, *args)

            with patch.object(local, "_owner_git", side_effect=counted):
                self.refuse(inputs, code="owner-c115-local-git-configuration-unsupported" if configured
                            else "owner-c115-local-checkout-filter-unsupported")
            self.assertTrue(all(args[0] != "status" for args in calls))
            self.assertFalse(marker.exists())
            if not configured:
                self.assertEqual(calls[-1][0:4], ("check-attr", "-z", "filter", "--"))
                self.assertIn("unrelated.txt", calls[-1][4:])

    def test_initialized_gitlink_with_nested_clean_filter_refuses_before_status(self):
        nested = self.root / "isolated-nested"
        nested.mkdir()
        legacy.git(nested, "init", "-q")
        source = nested / "nested.txt"
        source.write_bytes(b"isolated nested file\n")
        legacy.git(nested, "add", ".")
        legacy.git(nested, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                   "commit", "-qm", "test: isolated nested sources")
        self.commit_checkout()
        inputs = self.inputs()
        marker = nested / ".git" / "info" / "filter-marker"
        legacy.git(nested, "config", "filter.marker.clean", self.marker_command(marker))
        (nested / ".git" / "info" / "attributes").write_text("nested.txt filter=marker\n", encoding="utf-8")
        raw = source.read_bytes()
        source.write_bytes(b"!" + raw[1:])
        real_git = local._owner_git
        calls = []

        def counted(fd, *args):
            calls.append(args)
            return real_git(fd, *args)

        with patch.object(local, "_owner_git", side_effect=counted):
            self.refuse(inputs, code="owner-c115-local-gitlinks-unsupported")
        self.assertEqual(calls, [("ls-files", "--stage", "-z")])
        self.assertFalse(marker.exists())

    def add_uninitialized_gitlink(self):
        path = "references/isolated-uninitialized"
        slot = self.root / path
        slot.mkdir(parents=True)
        (self.root / ".gitmodules").write_text(
            '[submodule "isolated"]\npath = ' + path + '\nurl = https://example.invalid/never-contacted\n',
            encoding="utf-8")
        legacy.git(self.root, "add", ".gitmodules")
        legacy.git(self.root, "update-index", "--add", "--cacheinfo", "160000," + self.commit + "," + path)
        legacy.git(self.root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                   "commit", "-qm", "test: declare uninitialized fixture dependency")
        self.commit = legacy.git(self.root, "rev-parse", "HEAD")
        return slot

    def test_empty_uninitialized_gitlinks_preserve_clean_owner_corroboration_and_missing_paths_still_require_clean_status(self):
        slot = self.add_uninitialized_gitlink()
        inputs = self.inputs()
        with self.local_lane(inputs):
            result = self.verify(inputs)
        self.assertEqual(result.source_commit, self.commit)
        self.assertEqual(len(result.paths), 19)
        self.assertNotIn(str(slot.relative_to(self.root)), result.paths)
        self.assertFalse((slot / ".git").exists())
        slot.rmdir()
        self.refuse(inputs, code="owner-c115-local-repository-dirty")

    def test_nonempty_linked_or_newly_populated_gitlink_directories_refuse_before_status(self):
        slot = self.add_uninitialized_gitlink()
        inputs = self.inputs()
        child = slot / "uninspected.txt"
        child.write_text("unsupported nested contents", encoding="utf-8")
        self.refuse(inputs, code="owner-c115-local-gitlinks-unsupported")
        child.unlink()
        slot.rmdir()
        slot.symlink_to(self.root, target_is_directory=True)
        self.refuse(inputs, code="owner-c115-local-source-unavailable")
        slot.unlink()
        slot.mkdir()
        real_git = local._owner_git
        statuses = 0

        def populate_after_initial_status(fd, *args):
            nonlocal statuses
            result = real_git(fd, *args)
            if args[0] == "status":
                statuses += 1
                (slot / ".git").write_text("never-inspected", encoding="utf-8")
            return result

        with patch.object(local, "_owner_git", side_effect=populate_after_initial_status):
            self.refuse(inputs, code="owner-c115-local-gitlinks-unsupported")
        self.assertEqual(statuses, 1)

    def test_gitlink_directory_identity_drift_refuses_before_final_status(self):
        slot = self.add_uninitialized_gitlink()
        inputs = self.inputs()
        moved = slot.with_name(slot.name + "-old")
        real_git = local._owner_git
        statuses = 0

        def replace_after_initial_status(fd, *args):
            nonlocal statuses
            result = real_git(fd, *args)
            if args[0] == "status":
                statuses += 1
                slot.rename(moved)
                slot.mkdir()
            return result

        with patch.object(local, "_owner_git", side_effect=replace_after_initial_status):
            self.refuse(inputs, code="owner-c115-local-checkout-safety-changed")
        self.assertEqual(statuses, 1)

    def test_config_includes_helpers_and_extra_worktree_layout_refuse_before_status(self):
        inputs = self.inputs()
        marker = self.root / ".git" / "info" / "filter-marker"
        included = self.root / ".git" / "info" / "included-config"
        included.write_text('[filter "marker"]\nclean = ' + self.marker_command(marker) + "\n", encoding="utf-8")
        real_git = local._owner_git
        for key, value in (("include.path", str(included)), ("includeIf.gitdir:/.path", str(included)),
                           ("credential.helper", self.marker_command(marker)),
                           ("diff.external", self.marker_command(marker)), ("extensions.worktreeConfig", "true")):
            legacy.git(self.root, "config", key, value)
            calls = []

            def counted(fd, *args):
                calls.append(args)
                return real_git(fd, *args)

            with patch.object(local, "_owner_git", side_effect=counted):
                self.refuse(inputs, code="owner-c115-local-git-configuration-unsupported")
            self.assertTrue(all(args[0] != "status" for args in calls))
            self.assertFalse(marker.exists())
            legacy.git(self.root, "config", "--unset", key)
        for name in ("commondir", "config.worktree"):
            path = self.root / ".git" / name
            path.write_text("unsupported", encoding="utf-8")
            with patch.object(local, "_owner_git", side_effect=AssertionError("Git reached")):
                self.refuse(inputs, code="owner-c115-local-git-layout-unsupported")
            path.unlink()

    def test_bounded_regular_config_and_unsupported_tracked_symlinks_refuse_before_status(self):
        inputs = self.inputs()
        config = self.root / ".git" / "config"
        original = config.read_bytes()
        config.write_bytes(b"#" * (owner.ONE_MIB + 1))
        with patch.object(local, "_owner_git", side_effect=AssertionError("Git reached")):
            self.refuse(inputs, code="owner-c115-local-source-not-regular-bounded")
        config.write_bytes(original)
        linked = self.root / ".git" / "info" / "config-alias"
        config.rename(linked)
        config.symlink_to(linked)
        with patch.object(local, "_owner_git", side_effect=AssertionError("Git reached")):
            self.refuse(inputs, code="owner-c115-local-git-metadata-unsupported")
        config.unlink()
        linked.rename(config)
        (self.root / "tracked-symlink").symlink_to(self.original_sources[0].path)
        self.commit_checkout()
        inputs = self.inputs()
        real_git = local._owner_git
        calls = []

        def counted(fd, *args):
            calls.append(args)
            return real_git(fd, *args)

        with patch.object(local, "_owner_git", side_effect=counted):
            self.refuse(inputs, code="owner-c115-local-tracked-paths-unsupported")
        self.assertTrue(all(args[0] != "status" for args in calls))

    def test_config_drift_is_rechecked_before_the_final_status(self):
        inputs = self.inputs()
        config = self.root / ".git" / "config"
        original = config.read_bytes()
        real_read = local._read_source
        config_reads = 0

        def drift(fd, path):
            nonlocal config_reads
            value = real_read(fd, path)
            if path == ".git/config":
                config_reads += 1
                if config_reads == 2:
                    return (value[0] + b"# changed\n", value[1], value[2])
            return value

        with patch.object(local, "_read_source", side_effect=drift):
            self.refuse(inputs, code="owner-c115-local-git-configuration-changed")
        self.assertEqual(config_reads, 2)
        real_git = local._owner_git
        statuses = 0

        def mutate_after_initial_status(fd, *args):
            nonlocal statuses
            result = real_git(fd, *args)
            if args[0] == "status":
                statuses += 1
                config.write_bytes(original + b'[filter "marker"]\nclean = never-executed\n')
            return result

        with patch.object(local, "_owner_git", side_effect=mutate_after_initial_status):
            self.refuse(inputs, code="owner-c115-local-git-configuration-unsupported")
        self.assertEqual(statuses, 1)

    def test_supported_repository_config_and_disabled_fsmonitor_preserve_positive_results(self):
        marker = self.root / ".git" / "info" / "fsmonitor-marker"
        for key, value in (("core.fsmonitor", self.marker_command(marker)), ("core.hooksPath", ".githooks"),
                           ("user.name", "Fixture"), ("user.email", "fixture@example.invalid"),
                           ("remote.origin.url", "https://example.invalid/never-contacted"),
                           ("remote.origin.fetch", "+refs/heads/*:refs/remotes/origin/*"),
                           ("branch.main.remote", "origin"), ("branch.main.merge", "refs/heads/main")):
            legacy.git(self.root, "-c", "core.fsmonitor=false", "config", key, value)
        inputs = self.inputs()
        with self.local_lane(inputs):
            self.assertEqual(self.verify(inputs).source_commit, self.commit)
        self.assertFalse(marker.exists())

    def test_distinct_story_and_verifier_must_be_present_and_match_commit(self):
        for field in ("registeredStory", "verifierPath"):
            entry = owner.bindings.registry_entry()
            entry[field] = "fixtures/missing.py"
            self.refuse(self.inputs(entry=entry), code="owner-c115-local-auxiliary-source-missing")
        entry = owner.bindings.registry_entry()
        entry["verifierPath"] = entry["registeredStory"]
        self.refuse(self.inputs(entry=entry), code="owner-c115-local-auxiliary-source-alias")
        entry = owner.bindings.registry_entry()
        entry["registeredStory"] = self.original_sources[0].path
        self.refuse(self.inputs(entry=entry), code="owner-c115-local-auxiliary-source-alias")
        real_git = local._owner_git

        def clean_status(fd, *args):
            return b"" if args[0] == "status" else real_git(fd, *args)

        for path in self.auxiliary:
            target = self.root / path
            original = target.read_bytes()
            target.write_bytes(b"changed\r\n")
            with patch.object(local, "_owner_git", side_effect=clean_status):
                self.refuse(code="owner-c115-local-auxiliary-source-mismatch")
            target.unlink()
            target.symlink_to(self.root / self.original_sources[0].path)
            with patch.object(local, "_owner_git", side_effect=clean_status):
                self.refuse(code="owner-c115-local-source-unavailable")
            target.unlink()
            target.write_bytes(original)

    def test_final_source_story_verifier_and_clean_head_rereads_refuse_drift(self):
        inputs = self.inputs()
        real_read = local._read_source
        for changed in (self.original_sources[0].path, *self.auxiliary):
            reads = {}

            def drift(fd, path):
                raw, mode, inode = real_read(fd, path)
                reads[path] = reads.get(path, 0) + 1
                return (raw + b"changed", mode, inode) if path == changed and reads[path] == 2 else (raw, mode, inode)

            with patch.object(local, "_read_source", side_effect=drift):
                self.refuse(inputs, code="owner-c115-local-source-changed" if changed not in self.auxiliary
                            else "owner-c115-local-auxiliary-source-changed")
            self.assertEqual(reads[changed], 2)
        real_git = local._owner_git
        revisions = 0

        def head_drift(fd, *args):
            nonlocal revisions
            if args == ("rev-parse", "--verify", "HEAD^{commit}"):
                revisions += 1
                if revisions == 2:
                    return b"f" * 40 + b"\n"
            return real_git(fd, *args)

        with patch.object(local, "_owner_git", side_effect=head_drift):
            self.refuse(inputs, code="owner-c115-local-head-commit-mismatch")
        self.assertEqual(revisions, 2)

    def test_root_identity_drift_refuses_and_closes_open_descriptor(self):
        inputs = self.inputs()
        real_identity = local._assert_root_identity
        real_open = local._open_directory
        descriptors = []
        checks = 0

        def opened(root):
            descriptor = real_open(root)
            descriptors.append(descriptor)
            return descriptor

        def changed(fd, root):
            nonlocal checks
            checks += 1
            if checks == 2:
                raise OSError("private-test-credential")
            return real_identity(fd, root)

        with patch.object(local, "_open_directory", side_effect=opened), \
                patch.object(local, "_assert_root_identity", side_effect=changed):
            self.refuse(inputs, code="owner-c115-local-source-unavailable")
        self.assertEqual(checks, 2)
        with self.assertRaises(OSError):
            os.fstat(descriptors[0])

    def test_local_git_output_bound_and_timeout_reap_children_without_partial_results(self):
        inputs = self.inputs()
        real_popen = subprocess.Popen
        for script, timeout, code in (("import os; os.write(1, b'x' * 2097152)", False,
                                      "owner-c115-local-git-output-budget-exceeded"),
                                     ("import time; time.sleep(30)", True, "owner-c115-local-git-unavailable")):
            children = []

            def child(command, **kwargs):
                process = real_popen([sys.executable, "-c", script], stdin=kwargs["stdin"],
                                     stdout=kwargs["stdout"], stderr=kwargs["stderr"],
                                     start_new_session=kwargs["start_new_session"])
                children.append(process)
                return process

            with ExitStack() as stack:
                stack.enter_context(patch.object(local.subprocess, "Popen", side_effect=child))
                if timeout:
                    stack.enter_context(patch.object(local.select, "select", return_value=([], [], [])))
                self.refuse(inputs, code=code)
            self.assertEqual(len(children), 1)
            self.assertIsNotNone(children[0].poll())
            self.assertTrue(children[0].stdout.closed)

    def test_ignored_unrelated_files_are_outside_point_in_time_evidence(self):
        (self.root / ".git" / "info" / "exclude").write_text("ignored-private.txt\n", encoding="utf-8")
        (self.root / "ignored-private.txt").write_text("private-test-credential", encoding="utf-8")
        self.assertEqual(self.verify().source_commit, self.commit)

    def test_legacy_registration_and_deployed_refusal_remain_compatible(self):
        inputs = self.inputs()
        from test_registration_commands import registration_fixture
        legacy_inputs = registration_fixture(capture=owner.tree(inputs["capture"]), sources=inputs["sources"])
        result = local.corroborate_local_sources(*legacy_inputs, str(self.root))
        self.assertEqual(result.source_commit, self.commit)
        self.assertEqual(result.paths, tuple(sorted(owner.bindings.PATHS)))
        with denials() as calls:
            with self.assertRaisesRegex(InterchangeFormatError, "^deployed-producer-binding-unavailable$"):
                lookup_deployed_binding("C1.15", "PG-ONPREM-2", result.registration.source.registry)
            self.assertEqual(calls, {})

    def test_external_config_include_fifo_refuses_before_every_git_child(self):
        inputs = self.inputs()
        with tempfile.TemporaryDirectory(prefix="isolated-external-include-") as outside:
            fifo = Path(outside) / "never-read-config"
            os.mkfifo(fifo)
            config = self.root / ".git" / "config"
            config.write_bytes(config.read_bytes() + ('[include]\npath = ' + str(fifo) + '\n').encode())
            with patch.object(local, "_owner_git", side_effect=AssertionError("Git reached include")):
                self.refuse(inputs, code="owner-c115-local-git-configuration-unsupported")

    def test_closed_independent_config_grammar_refuses_unsupported_syntax_before_git(self):
        inputs = self.inputs()
        config = self.root / ".git" / "config"
        original = config.read_bytes()
        for addition in (b'[includeIf "gitdir:/"]\npath = /never-read\n',
                         b'[core]\nworktree = /foreign\n', b'[extensions]\nworktreeConfig = true\n',
                         b'[core]\nbare = true\n', b'[core]\nrepositoryformatversion = 1\n',
                         b'[user]\nname = line\\\ncontinued\n', b'[user]\nname = "unclosed\n',
                         b'[remote.origin]\nurl = /foreign\n', b'[user]\nname = x\x00y\n'):
            with self.subTest(addition=addition):
                config.write_bytes(original + addition)
                with patch.object(local, "_owner_git", side_effect=AssertionError("Git parsed config")):
                    self.refuse(inputs, code="owner-c115-local-git-configuration-unsupported")

    def test_metadata_directory_and_file_aliases_refuse_before_repository_git(self):
        inputs = self.inputs()
        attributes = self.root / ".git" / "info" / "attributes"
        attributes.write_bytes(b"# safe attributes\n")
        for name in ("objects", "objects/info", "index", "info/attributes", "HEAD"):
            target = self.root / ".git" / name
            with self.subTest(name=name), tempfile.TemporaryDirectory(prefix="isolated-metadata-alias-") as outside:
                moved = Path(outside) / "metadata"
                target.rename(moved)
                try:
                    target.symlink_to(moved, target_is_directory=moved.is_dir())
                    with patch.object(local, "_owner_git", side_effect=AssertionError("Git read alias")):
                        self.refuse(inputs, code="owner-c115-local-git-metadata-unsupported")
                finally:
                    target.unlink()
                    moved.rename(target)
        with tempfile.TemporaryDirectory(prefix="isolated-hardlink-") as outside:
            os.link(self.root / ".git" / "index", Path(outside) / "index")
            with patch.object(local, "_owner_git", side_effect=AssertionError("Git read hardlink")):
                self.refuse(inputs, code="owner-c115-local-git-metadata-unsupported")

    def test_filter_added_after_preflight_cannot_execute_inside_status(self):
        inputs = self.inputs()
        marker = self.root / ".git" / "info" / "post-preflight-marker"
        path = self.original_sources[0].path
        source = self.root / path
        real_git = local._owner_git
        statuses = 0

        def mutate_before_status(fd, *args):
            nonlocal statuses
            if args[0] == "status":
                statuses += 1
                config = self.root / ".git" / "config"
                config.write_bytes(config.read_bytes() + (
                    '[filter "marker"]\nclean = ' + self.marker_command(marker) + '\nrequired = true\n').encode())
                (self.root / ".git" / "info" / "attributes").write_text(path + " filter=marker\n")
                raw = source.read_bytes()
                source.write_bytes(b"!" + raw[1:])
            return real_git(fd, *args)

        with patch.object(local, "_owner_git", side_effect=mutate_before_status):
            self.refuse(inputs)
        self.assertEqual(statuses, 1)
        self.assertFalse(marker.exists())

    def test_gitlink_initialized_after_preflight_is_never_entered_by_status(self):
        slot = self.add_uninitialized_gitlink()
        inputs = self.inputs()
        marker = slot / ".git" / "info" / "nested-post-preflight-marker"
        real_git = local._owner_git
        statuses = 0

        def initialize_before_status(fd, *args):
            nonlocal statuses
            if args[0] == "status":
                statuses += 1
                legacy.git(slot, "init", "-q")
                source = slot / "nested.txt"
                source.write_bytes(b"isolated nested bytes\n")
                legacy.git(slot, "add", ".")
                legacy.git(slot, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                           "commit", "-qm", "test: isolated nested drift")
                legacy.git(slot, "config", "filter.marker.clean", self.marker_command(marker))
                (slot / ".git" / "info" / "attributes").write_text("nested.txt filter=marker\n")
                raw = source.read_bytes()
                source.write_bytes(b"!" + raw[1:])
            return real_git(fd, *args)

        with patch.object(local, "_owner_git", side_effect=initialize_before_status):
            self.refuse(inputs, code="owner-c115-local-gitlinks-unsupported")
        self.assertEqual(statuses, 1)
        self.assertFalse(marker.exists())

    def test_unrelated_cached_same_size_bytes_and_mode_drift_cannot_weaken_status(self):
        target = self.root / "unrelated.txt"
        target.write_bytes(b"isolated unrelated bytes\r\n")
        self.commit_checkout()
        inputs = self.inputs()
        for key, value in (("core.trustctime", "false"), ("core.checkstat", "minimal"),
                           ("core.filemode", "false"), ("core.ignorestat", "true")):
            # ignorestat itself is outside the closed supported config.
            if key != "core.ignorestat":
                legacy.git(self.root, "config", key, value)
        legacy.git(self.root, "update-index", "--refresh")
        raw, info = target.read_bytes(), target.stat()
        target.write_bytes(b"!" + raw[1:])
        os.utime(target, ns=(info.st_atime_ns, info.st_mtime_ns))
        self.assertEqual(target.stat().st_mtime_ns, info.st_mtime_ns)
        self.refuse(inputs, code="owner-c115-local-repository-dirty")
        target.write_bytes(raw)
        legacy.git(self.root, "update-index", "--refresh")
        target.chmod(0o755)
        self.refuse(inputs, code="owner-c115-local-repository-dirty")

    def test_auxiliary_final_reread_retains_committed_executable_mode(self):
        inputs = self.inputs()
        legacy.git(self.root, "config", "core.filemode", "false")
        real_read = local._read_source
        for target in self.auxiliary:
            calls = 0

            def drift(fd, path):
                nonlocal calls
                raw, mode, identity = real_read(fd, path)
                if path == target:
                    calls += 1
                    if calls == 2:
                        mode ^= 0o100
                return raw, mode, identity

            with patch.object(local, "_read_source", side_effect=drift):
                self.refuse(inputs, code="owner-c115-local-auxiliary-source-changed")
            self.assertEqual(calls, 2)

    def test_supported_config_change_between_preflights_refuses_after_one_status(self):
        inputs = self.inputs()
        real_git = local._owner_git
        statuses = 0

        def change_supported_config(fd, *args):
            nonlocal statuses
            result = real_git(fd, *args)
            if args[0] == "status":
                statuses += 1
                config = self.root / ".git" / "config"
                config.write_bytes(config.read_bytes() + b"[user]\nname = changed supported name\n")
            return result

        with patch.object(local, "_owner_git", side_effect=change_supported_config):
            self.refuse(inputs, code="owner-c115-local-checkout-safety-changed")
        self.assertEqual(statuses, 1)

    def test_metadata_and_attribute_identity_drift_after_preflight_refuses(self):
        inputs = self.inputs()
        real_git = local._owner_git
        for target in (self.root / ".git" / "index", self.root / ".gitattributes"):
            statuses = 0

            def replace_same_bytes(fd, *args):
                nonlocal statuses
                result = real_git(fd, *args)
                if args[0] == "status":
                    statuses += 1
                    raw = target.read_bytes()
                    temporary = target.with_name(target.name + "-replacement")
                    temporary.write_bytes(raw)
                    temporary.replace(target)
                return result

            with self.subTest(path=target.name), patch.object(local, "_owner_git", side_effect=replace_same_bytes):
                self.refuse(inputs, code="owner-c115-local-checkout-safety-changed")
            self.assertEqual(statuses, 1)

    def test_large_real_index_remains_clean_under_native_no_process_creation_filter(self):
        # Git preload_index starts two lstat threads at 1000 entries (500
        # entries per thread). These ordinary tracked files reach that path;
        # owner-only fixed settings keep the native enforced child sequential.
        bulk = self.root / "bulk"
        bulk.mkdir()
        for number in range(1100):
            (bulk / str(number)).write_bytes(b"isolated unrelated bytes\r\n")
        self.commit_checkout()
        inputs = self.inputs()
        count = len(legacy.git(self.root, "ls-files").splitlines())
        self.assertGreaterEqual(count, 1100)
        self.assertLess(len(legacy.git(self.root, "ls-files", "--stage").encode()), owner.ONE_MIB)
        result = self.verify(inputs)
        self.assertEqual(result.source_commit, self.commit)
        self.assertEqual(len(result.paths), 19)
        self.assertEqual(result.inspection.sources, inputs["sources"])

    def test_attribute_aggregate_budget_refuses_before_retaining_the_over_budget_file(self):
        raw = b"#" + b"x" * (owner.ONE_MIB - 2) + b"\n"
        for number in range(33):
            directory = self.root / "attributes" / f"{number:02}"
            directory.mkdir(parents=True)
            (directory / ".gitattributes").write_bytes(raw)
            (directory / "tracked.txt").write_bytes(b"isolated extra tracked file\r\n")
        self.commit_checkout()
        inputs = self.inputs()
        real_git, real_read = local._owner_git, local._read_source
        git_calls, reads, retained_at_refusal = [], [], []
        previous = sys.gettrace()

        def traced(frame, event, argument):
            if (frame.f_code is local._assert_owner_checkout_safe.__code__ and event == "exception"
                    and isinstance(argument[1], InterchangeFormatError)
                    and str(argument[1]) == "local-checkout-attribute-budget-exceeded"):
                retained_at_refusal.extend(frame.f_locals["retained_attributes"])
            return traced

        def counted_git(fd, *args):
            git_calls.append(args)
            return real_git(fd, *args)

        def counted_read(fd, path):
            value = real_read(fd, path)
            reads.append((path, len(value[0])))
            return value

        try:
            sys.settrace(traced)
            with patch.object(local, "_owner_git", side_effect=counted_git), \
                    patch.object(local, "_read_source", side_effect=counted_read):
                self.refuse(inputs, code="owner-c115-local-checkout-attribute-budget-exceeded")
        finally:
            sys.settrace(previous)
        self.assertEqual(git_calls, [("ls-files", "--stage", "-z")])
        big_reads = [path for path, size in reads if size == owner.ONE_MIB]
        self.assertEqual(len(big_reads), 32)
        self.assertTrue(retained_at_refusal)
        retained_names = {path for path, value in retained_at_refusal}
        self.assertNotIn(big_reads[-1], retained_names)
        retained_bytes = sum(len(value[0]) for path, value in retained_at_refusal if value is not None)
        self.assertLessEqual(retained_bytes, owner.THIRTY_TWO_MIB)

    def test_native_seccomp_denies_process_network_file_mutation_and_helper_exec(self):
        # Insert native probes into the trusted test copy after installation,
        # then perform the same permitted Git exec. No production switch exists.
        marker = self.root / ".git" / "info" / "native-marker"
        existing = self.root / ".git" / "info" / "existing-native"
        existing.write_bytes(b"unchanged")
        probes = '''
    def raw_syscall(*arguments):
        result = libc.syscall(*arguments)
        if result == -1:
            raise OSError(ctypes.get_errno(), "blocked")
        return result
    def close_then_reopen_output():
        try:
            os.close(1)
        except PermissionError:
            pass
        reopened = os.open(existing, os.O_RDWR)
        os.write(reopened, b"output-identity-mutation")
    denied = 0
    for call in (
        lambda: os.fork(),
        lambda: socket.socket(),
        lambda: os.open(marker, os.O_WRONLY | os.O_CREAT, 0o600),
        lambda: os.mkdir(marker),
        lambda: os.unlink(existing),
        lambda: os.chmod(existing, 0o777),
        lambda: os.truncate(existing, 0),
        lambda: os.write(existing_fd, b"mutated"),
        lambda: mmap.mmap(existing_fd, 0, flags=mmap.MAP_SHARED, prot=mmap.PROT_READ),
        lambda: os.dup2(existing_fd, 1),
        lambda: os.close(1),
        lambda: os.close(2),
        lambda: raw_syscall(436, 0, 0xffffffff, 0),
        lambda: fcntl.fcntl(1, fcntl.F_SETFD, fcntl.FD_CLOEXEC),
        lambda: fcntl.fcntl(2, fcntl.F_SETFD, fcntl.FD_CLOEXEC),
        close_then_reopen_output,
    ):
        try:
            call()
            os._exit(120)
        except OSError as error:
            if error.errno != 1:
                os._exit(121)
            denied += 1
    alternate = ctypes.create_string_buffer(b"/bin/sh")
    if libc.execve(ctypes.cast(alternate, ctypes.c_char_p), argv, env) != -1 or ctypes.get_errno() != 1:
        os._exit(122)
    os.write(1, ("denied=" + str(denied + 1) + "\\n").encode())
'''
        launcher = local._OWNER_LAUNCHER.replace(
            "import ctypes, os, sys", "import ctypes, os, sys, socket, mmap, fcntl\nmarker=" + repr(str(marker))
            + "\nexisting=" + repr(str(existing)) + "\nexisting_fd=os.open(existing, os.O_RDWR)")
        launcher = launcher.replace("    libc.execve(ctypes.cast(git, ctypes.c_char_p), argv, env)",
                                    probes + "    libc.execve(ctypes.cast(git, ctypes.c_char_p), argv, env)")
        process = subprocess.run(["/usr/bin/python3", "-I", "-S", "-B", "-c", launcher,
                                  *local._OWNER_GIT_PREFIX, "rev-parse", "--show-toplevel"],
                                 cwd=self.root, env={"GIT_CONFIG_NOSYSTEM": "1", "GIT_CONFIG_GLOBAL": os.devnull},
                                 stdin=subprocess.DEVNULL, capture_output=True, timeout=10, start_new_session=True)
        self.assertEqual(process.returncode, 0, process.stderr)
        self.assertEqual(process.stdout, b"denied=17\n" + os.fsencode(self.root) + b"\n")
        self.assertFalse(marker.exists())
        self.assertEqual(existing.read_bytes(), b"unchanged")

    def test_native_enforcement_unavailable_refuses_in_the_owned_child(self):
        inputs = self.inputs()
        for replacement in (('os.uname().machine != "x86_64"', 'True'),
                            ('libc.prctl(38, 1, 0, 0, 0) != 0', 'True')):
            with self.subTest(condition=replacement[0]), patch.object(
                    local, "_OWNER_LAUNCHER", local._OWNER_LAUNCHER.replace(*replacement)):
                self.refuse(inputs, code="owner-c115-local-git-unavailable")

    def test_timeout_cleanup_kills_an_injected_descendant_in_the_owned_group(self):
        inputs = self.inputs()
        pidfile = self.root / ".git" / "info" / "descendant-pid"
        children = []
        real_popen = subprocess.Popen
        script = ("import os,pathlib,time; pid=os.fork(); "
                  "pathlib.Path(" + repr(str(pidfile)) + ").write_text(str(pid)) if pid else None; "
                  "time.sleep(30)")

        def descendant(command, **kwargs):
            process = real_popen([sys.executable, "-c", script], stdin=kwargs["stdin"],
                                 stdout=kwargs["stdout"], stderr=kwargs["stderr"],
                                 start_new_session=kwargs["start_new_session"])
            children.append(process)
            deadline = time.monotonic() + 5
            while not pidfile.exists():
                if time.monotonic() >= deadline:
                    self.fail("fixture descendant did not start")
                time.sleep(0.01)
            return process

        with patch.object(local.subprocess, "Popen", side_effect=descendant), \
                patch.object(local.select, "select", return_value=([], [], [])):
            self.refuse(inputs, code="owner-c115-local-git-unavailable")
        self.assertEqual(len(children), 1)
        self.assertIsNotNone(children[0].poll())
        pid = int(pidfile.read_text())
        deadline = time.monotonic() + 5
        while True:
            try:
                state = Path(f"/proc/{pid}/stat").read_text().split()[2]
            except FileNotFoundError:
                break
            if state == "Z":
                break  # Its ancestor/init owns reaping; the descendant is dead.
            if time.monotonic() >= deadline:
                self.fail("fixture descendant survived process-group cleanup")
            time.sleep(0.01)
        self.assertTrue(children[0].stdout.closed)

    def metadata_records(self):
        descriptor = local._open_directory(str(self.root))
        try:
            return local._owner_metadata(descriptor)
        finally:
            os.close(descriptor)

    @contextmanager
    def budget_observation(self):
        observed = {}
        previous = sys.gettrace()

        def traced(frame, event, argument):
            if event == "exception" and isinstance(argument[1], InterchangeFormatError):
                if frame.f_code is local._owner_metadata.__code__:
                    observed.update(entries=len(frame.f_locals["records"]),
                                    state_bytes=frame.f_locals["byte_count"])
                if frame.f_code is local._assert_owner_checkout_safe.__code__:
                    observed.update(attribute_paths=len(frame.f_locals.get("attribute_paths", ())),
                                    attribute_path_bytes=frame.f_locals.get("attribute_path_bytes", 0),
                                    attributes=tuple(frame.f_locals.get("retained_attributes", ())))
            return traced

        try:
            sys.settrace(traced)
            yield observed
        finally:
            sys.settrace(previous)

    def test_info_attributes_exact_file_limit_and_next_byte_refuse_before_git(self):
        target = self.root / ".git" / "info" / "attributes"
        raw = b"#" + b"x" * (owner.ONE_MIB - 2) + b"\n"
        target.write_bytes(raw)
        self.assertEqual(len(self.verify().paths), 19)
        target.write_bytes(raw + b"x")
        with patch.object(local, "_owner_git", side_effect=AssertionError("Git parsed oversized attributes")):
            self.refuse(code="owner-c115-local-source-not-regular-bounded")

    def test_info_attributes_count_toward_aggregate_before_git_attribute_parsing(self):
        raw = b"#" + b"x" * (owner.ONE_MIB - 2) + b"\n"
        (self.root / ".git" / "info" / "attributes").write_bytes(raw)
        for number in range(31):
            directory = self.root / "attribute-info-budget" / f"{number:02}"
            directory.mkdir(parents=True)
            (directory / ".gitattributes").write_bytes(raw)
            (directory / "t").write_bytes(b"isolated tracked bytes\r\n")
        self.commit_checkout()
        calls = []
        real_git = local._owner_git

        def counted(fd, *args):
            calls.append(args)
            return real_git(fd, *args)

        with self.budget_observation() as observed, patch.object(local, "_owner_git", side_effect=counted):
            self.refuse(code="owner-c115-local-checkout-attribute-budget-exceeded")
        self.assertEqual(calls, [("ls-files", "--stage", "-z")])
        retained = dict(observed["attributes"])
        self.assertEqual(retained[".git/info/attributes"][0], raw)
        self.assertLessEqual(sum(len(value[0]) for value in retained.values() if value is not None),
                             owner.THIRTY_TWO_MIB)
        self.assertNotIn("attribute-info-budget/30/.gitattributes", retained)

    def test_ordinary_quoted_config_subsection_names_preserve_native_corroboration(self):
        for key, value in (("branch.feature+audit.remote", "ordinary name"),
                           ("branch.feature+audit.merge", "refs/heads/feature+audit"),
                           ("remote.ordinary name.url", "https://example.invalid/never-contacted"),
                           ('remote.quoted"name.fetch', "+refs/heads/*:refs/remotes/origin/*")):
            legacy.git(self.root, "config", key, value)
        result = self.verify()
        self.assertEqual(result.source_commit, self.commit)
        self.assertEqual(len(result.git_blob_oids), 19)

    def test_unrelated_native_colon_tilde_and_space_paths_preserve_evidence_ref_validation(self):
        for path in ("unrelated:notes.txt", "~notes.txt", "ordinary notes.txt", "colon:dir/t", "~directory/t"):
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(b"isolated unrelated native bytes\r\n")
        self.commit_checkout()
        inputs = self.inputs()
        self.assertEqual(len(self.verify(inputs).paths), 19)
        for path in ("unrelated:notes.txt", "~notes.txt"):
            source = replace(inputs["sources"][0])
            object.__setattr__(source, "path", path)
            self.pure_refusal(inputs, sources=(source,) + inputs["sources"][1:])

    def test_filter_batches_preserve_850_long_paths_under_native_low_stack_argmax(self):
        for number in range(850):
            name = f"long-{number:03}-" + "x" * 190
            (self.root / name).write_bytes(b"isolated low-stack bytes\r\n")
        self.commit_checkout()
        inputs = self.inputs()
        real_popen = subprocess.Popen
        batches = []
        low_stack = ("import os,resource,sys; resource.setrlimit(resource.RLIMIT_STACK,(524288,524288)); "
                     "assert os.sysconf('SC_ARG_MAX') == 131072; os.execv(sys.argv[1],sys.argv[1:])")

        def constrained(command, **kwargs):
            arguments = tuple(command[6 + len(local._OWNER_GIT_PREFIX):])
            if arguments[:4] == ("check-attr", "-z", "filter", "--"):
                paths = arguments[4:]
                batches.append(paths)
                self.assertLessEqual(sum(len(path.encode()) + 9 for path in paths), 32768)
                self.assertEqual(kwargs["stdin"], subprocess.DEVNULL)
            return real_popen(["/usr/bin/python3", "-I", "-S", "-B", "-c", low_stack, *command], **kwargs)

        with patch.object(local.subprocess, "Popen", side_effect=constrained):
            result = self.verify(inputs)
        tracked = tuple(legacy.git(self.root, "ls-files").splitlines())
        self.assertGreater(len(batches), 3)
        flattened = tuple(path for batch in batches for path in batch)
        self.assertEqual(flattened, tracked * 3)
        self.assertEqual(len(result.paths), 19)

    def test_metadata_entry_limit_accepts_exact_then_refuses_next_before_every_git_child(self):
        directory = self.root / ".git" / "info" / "entry-boundary"
        directory.mkdir()
        initial = len(self.metadata_records())
        for number in range(8192 - initial):
            (directory / str(number)).touch()
        records = self.metadata_records()
        self.assertEqual(len(records), 8192)
        self.assertLess(sum(len(os.fsencode(path)) + 64 for path, state in records), owner.ONE_MIB)
        (directory / "overflow").touch()
        with self.budget_observation() as observed, patch.object(
                local, "_owner_git", side_effect=AssertionError("Git reached entry overflow")):
            self.refuse(code="owner-c115-local-git-metadata-budget-exceeded")
        self.assertEqual(observed["entries"], 8192)
        self.assertLess(observed["state_bytes"], owner.ONE_MIB)

    def test_metadata_state_limit_accepts_exact_then_refuses_next_before_every_git_child(self):
        directory = self.root / ".git" / "info" / "state-boundary"
        directory.mkdir()
        initial = sum(len(os.fsencode(path)) + 64 for path, state in self.metadata_records())
        base = len(os.fsencode(str(directory.relative_to(self.root)) + "/")) + 64
        cost = base + 240
        count, remainder = divmod(owner.ONE_MIB - initial, cost)
        for number in range(count):
            (directory / (f"{number:04}-" + "x" * 235)).touch()
        if 0 < remainder <= base:
            last = directory / (f"{count - 1:04}-" + "x" * 235)
            last.rename(directory / (f"{count - 1:04}-" + "x" * 135))
            remainder += 100
        if remainder:
            (directory / ("y" * (remainder - base))).touch()
        records = self.metadata_records()
        self.assertEqual(sum(len(os.fsencode(path)) + 64 for path, state in records), owner.ONE_MIB)
        self.assertLess(len(records), 8192)
        (directory / "overflow").touch()
        with self.budget_observation() as observed, patch.object(
                local, "_owner_git", side_effect=AssertionError("Git reached state overflow")):
            self.refuse(code="owner-c115-local-git-metadata-budget-exceeded")
        self.assertLess(observed["entries"], 8192)
        self.assertGreater(observed["state_bytes"], owner.ONE_MIB)

    def test_metadata_depth_accepts_64_then_refuses_65_before_every_git_child(self):
        directory = self.root / ".git"
        for number in range(64):
            directory /= "d"
            directory.mkdir()
        self.assertEqual(max(path.count("/") for path, state in self.metadata_records()), 64)
        (directory / "d").mkdir()
        with patch.object(local, "_owner_git", side_effect=AssertionError("Git reached depth overflow")):
            self.refuse(code="owner-c115-local-git-metadata-budget-exceeded")

    def test_attribute_path_entry_and_state_limits_refuse_before_filter_git_child(self):
        # Identical directory trees share Git objects, keeping the metadata
        # limits independent from these whole-index attribute-path limits.
        real_git = local._owner_git
        for label, count, width in (("entries", 8200, 5), ("state", 3800, 200)):
            directory = self.root / ("attribute-" + label)
            for number in range(count):
                child = directory / (f"{number:04}" + "x" * (width - 4))
                child.mkdir(parents=True)
                (child / "t").write_bytes(b"isolated shared subtree\r\n")
            self.commit_checkout()
            calls = []

            def counted(fd, *args):
                calls.append(args)
                return real_git(fd, *args)

            with self.budget_observation() as observed, patch.object(local, "_owner_git", side_effect=counted):
                self.refuse(code="owner-c115-local-checkout-attribute-budget-exceeded")
            self.assertEqual(calls, [("ls-files", "--stage", "-z")])
            if label == "entries":
                self.assertEqual(observed["attribute_paths"], 8192)
                self.assertLess(observed["attribute_path_bytes"], owner.ONE_MIB)
            else:
                self.assertLess(observed["attribute_paths"], 8192)
                self.assertGreater(observed["attribute_path_bytes"], owner.ONE_MIB)
            # Remove only this disposable test fixture's tracked bulk files.
            import shutil
            shutil.rmtree(directory)
            self.commit_checkout()

    def test_actual_seccomp_installation_failure_refuses_in_owned_child_without_git(self):
        launcher = local._OWNER_LAUNCHER.replace(
            "libc.prctl(22, 2, ctypes.byref(program), 0, 0)",
            "libc.prctl(22, 2, ctypes.c_void_p(), 0, 0)")
        children = []
        real_popen = subprocess.Popen

        def observed(command, **kwargs):
            self.assertIs(kwargs["start_new_session"], True)
            process = real_popen(command, **kwargs)
            children.append(process)
            return process

        with patch.object(local, "_OWNER_LAUNCHER", launcher), \
                patch.object(local.subprocess, "Popen", side_effect=observed):
            self.refuse(code="owner-c115-local-git-unavailable")
        self.assertEqual(len(children), 1)
        self.assertEqual(children[0].returncode, 126)
        self.assertTrue(children[0].stdout.closed)

    def test_native_unknown_x32_calls_and_i386_architecture_refuse_in_owned_children(self):
        probe = '''
    for number in (0x7fffffff, 0x40000027):
        if libc.syscall(number) != -1 or ctypes.get_errno() != 1:
            os._exit(120)
    os.write(1, b"unknown-x32-denied\\n")
'''
        launcher = local._OWNER_LAUNCHER.replace(
            "    libc.execve(ctypes.cast(git, ctypes.c_char_p), argv, env)",
            probe + "    libc.execve(ctypes.cast(git, ctypes.c_char_p), argv, env)")
        result = subprocess.run(["/usr/bin/python3", "-I", "-S", "-B", "-c", launcher,
                                 *local._OWNER_GIT_PREFIX, "rev-parse", "--show-toplevel"],
                                cwd=self.root, stdin=subprocess.DEVNULL, capture_output=True,
                                timeout=10, start_new_session=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, b"unknown-x32-denied\n" + os.fsencode(self.root) + b"\n")
        prepare = '''
memory = mmap.mmap(-1, 4096, flags=mmap.MAP_PRIVATE | mmap.MAP_ANONYMOUS,
                   prot=mmap.PROT_READ | mmap.PROT_WRITE | mmap.PROT_EXEC)
memory.write(b"\\xb8\\x14\\x00\\x00\\x00\\xcd\\x80\\xc3")
invoke_i386 = ctypes.CFUNCTYPE(ctypes.c_int)(ctypes.addressof(ctypes.c_char.from_buffer(memory)))
'''
        launcher = local._OWNER_LAUNCHER.replace("import ctypes, os, sys",
                                                "import ctypes, os, sys, mmap\n" + prepare)
        launcher = launcher.replace("    libc.execve(ctypes.cast(git, ctypes.c_char_p), argv, env)",
                                    "    invoke_i386()\n    libc.execve(ctypes.cast(git, ctypes.c_char_p), argv, env)")
        result = subprocess.run(["/usr/bin/python3", "-I", "-S", "-B", "-c", launcher,
                                 *local._OWNER_GIT_PREFIX, "rev-parse", "--show-toplevel"],
                                cwd=self.root, stdin=subprocess.DEVNULL, capture_output=True,
                                timeout=10, start_new_session=True)
        self.assertEqual(result.returncode, -signal.SIGSYS)
        self.assertEqual(result.stdout, b"")

    def test_packed_checkout_corroborates_all_nineteen_native_blob_identities(self):
        expected = tuple(legacy.git(self.root, "rev-parse", f"{self.commit}:{path}")
                         for path in sorted(owner.bindings.PATHS))
        legacy.git(self.root, "repack", "-ad")
        legacy.git(self.root, "prune-packed")
        self.assertTrue(tuple((self.root / ".git" / "objects" / "pack").glob("*.pack")))
        for oid in expected:
            self.assertFalse((self.root / ".git" / "objects" / oid[:2] / oid[2:]).exists())
        result = self.verify()
        self.assertEqual(result.git_blob_oids, expected)
        self.assertEqual(len(result.paths), 19)

    def test_ignorecase_config_cannot_hide_case_colliding_untracked_file(self):
        (self.root / "unrelated.txt").write_bytes(b"isolated tracked lowercase\r\n")
        self.commit_checkout()
        legacy.git(self.root, "config", "core.ignorecase", "true")
        (self.root / "UNRELATED.txt").write_bytes(b"isolated untracked uppercase\r\n")
        self.refuse(code="owner-c115-local-repository-dirty")


if __name__ == "__main__":
    unittest.main()
