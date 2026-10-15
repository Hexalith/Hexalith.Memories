"""Approved retained evidence: bounded acquisition, denial order and custody."""

from contextlib import contextmanager
from copy import deepcopy
from dataclasses import fields, replace
from datetime import timedelta, timezone
import hashlib
import inspect
import os
from pathlib import Path
import subprocess
import unittest
from unittest.mock import patch

import test_owner_c115 as owner
import test_owner_c115_plan_provenance as fixture
import access_telemetry_c1_owner_c115_retained as retained
import access_telemetry_c1_retained_files as streams_files


JSON_FIELDS = {"registry": "registry", "capture": "capture", "statement": "statement",
               "commandContract": "command_contract", "reviewRolePolicy": "review_role_policy"}


class OwnerC115RetainedTests(unittest.TestCase):
    def setUp(self):
        # Module-qualified fixtures keep their TestCases out of this discovery.
        self.fixture = fixture.OwnerC115PlanProvenanceTests(methodName="runTest")
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.files = self.fixture.files
        self.root = self.fixture.root
        self.inputs = self.fixture.inputs
        self.directory = self.files.custody / "evidence"
        self.directory.mkdir(mode=0o700)
        self.manifest_path = self.directory / "evidence.json"
        self.packet = self.directory / "capture.json"
        self.sidecar = self.directory / "capture.json.streams"
        self.sidecar.mkdir(mode=0o700)
        self.prepare()

    def write(self, path, raw):
        self.files.write_private(path, raw)

    def reference(self, path, raw):
        self.write(path, raw)
        return {"path": str(path), "sha256": hashlib.sha256(raw).hexdigest()}

    def approve_manifest(self, raw=None):
        raw = fixture.custody.wire(self.manifest) if raw is None else raw
        self.write(self.manifest_path, raw)
        self.files.plan.update(evidenceManifestPath=str(self.manifest_path),
                               evidenceManifestSha256=hashlib.sha256(raw).hexdigest())
        self.files.prepare(record_raw=self.inputs["run"].raw)

    def prepare(self):
        self.fixture.inputs = self.inputs
        self.fixture.prepare()
        self.inputs = self.fixture.inputs
        self.manifest = {"schemaVersion": retained.EVIDENCE_MANIFEST_SCHEMA}
        for field, name in JSON_FIELDS.items():
            self.manifest[field] = self.reference(self.directory / (name + ".json"), self.inputs[name].raw)
        sources = []
        for index, source in enumerate(self.inputs["sources"]):
            sources.append({
                "path": source.path, "sourceCommit": source.source_commit,
                "executedBytes": self.reference(self.directory / f"source-{index:02}.executed", source.executed_bytes),
                "blobBytes": self.reference(self.directory / f"source-{index:02}.blob", source.blob_bytes),
                "normalization": source.normalization, "gitMode": source.git_mode,
                "cleanFilter": source.clean_filter, "workingTreeEncoding": source.working_tree_encoding,
                "ident": source.ident,
            })
        self.manifest["sources"] = sources
        for path in self.sidecar.iterdir():
            path.unlink()
        capture = owner.tree(self.inputs["capture"])
        self.stream_manifest = {
            "schemaVersion": streams_files.STREAM_MANIFEST_SCHEMA,
            "captureSha256": self.inputs["capture"].sha256,
            "receipts": [{"index": index, "purpose": command["purpose"],
                          "stdoutSha256": command["stdoutSha256"], "stderrSha256": command["stderrSha256"]}
                         for index, command in enumerate(capture["commands"])],
        }
        self.write(self.sidecar / "manifest.json", fixture.custody.wire(self.stream_manifest))
        for index, stream in enumerate(self.inputs["streams"]):
            self.write(self.sidecar / f"{index:04d}.stdout", stream.stdout)
            self.write(self.sidecar / f"{index:04d}.stderr", stream.stderr)
        self.approve_manifest()

    def verify(self, **changes):
        return retained.corroborate_owner_c115_retained_files(**{
            "plan_path": str(self.files.plan_path), "approved_plan_sha256": self.files.approved,
            "custody_root": str(self.files.custody), "repository_root": str(self.root),
            "now": self.inputs["now"], **changes,
        })

    def refuse(self, code=None, **changes):
        with self.assertRaises(retained.OwnerC115RetainedFilesError) as caught:
            self.verify(**changes)
        error = caught.exception
        self.assertRegex(str(error), r"\Aowner-c115-retained-[a-z0-9-]+\Z")
        self.assertLess(len(str(error)), 160)
        self.assertIsNone(error.__cause__)
        self.assertIsNone(error.__context__)
        self.assertTrue(error.__suppress_context__)
        self.assertFalse(hasattr(error, "inspection"))
        for secret in ("private-canary", str(self.root), str(self.files.custody)):
            self.assertNotIn(secret, repr(error))
        if code is not None:
            self.assertEqual("owner-c115-retained-" + code, str(error))
        return error

    @contextmanager
    def barriers(self, *, allow_git=False):
        with patch.object(fixture, "MODULES", (*fixture.MODULES, retained, streams_files)), \
                self.fixture.forbidden_calls(allow_git=allow_git) as calls:
            yield calls

    def before_git_refusal(self, code=None, *, inspected=None, **changes):
        with self.barriers(), patch.object(fixture.local, "_open_directory") as opened, \
                patch.object(fixture.local, "_owner_git") as git, \
                patch.object(retained, "inspect_owner_c115", wraps=owner.inspect_owner_c115) as inspection:
            self.refuse(code, **changes)
        opened.assert_not_called()
        git.assert_not_called()
        if inspected is not None:
            self.assertEqual(int(inspected), inspection.call_count)

    @contextmanager
    def tracked(self):
        held, closed = [], []
        real_open, real_close = retained._open_held_file, os.close

        def opened(*args, **kwargs):
            result = real_open(*args, **kwargs)
            held.extend(fd for fd, before in result.ancestors)
            held.append(result.descriptor)
            return result

        def close(fd):
            if fd in held:
                closed.append(fd)
            return real_close(fd)

        with patch.object(retained, "_open_held_file", side_effect=opened), \
                patch.object(fixture.held_files, "_open_held_file", side_effect=opened), \
                patch.object(retained.os, "close", side_effect=close):
            yield held, closed
        self.assertEqual(held[::-1], closed)
        for fd in held:
            with self.assertRaises(OSError):
                os.fstat(fd)

    def test_one_two_three_pods_inspect_once_then_only_existing_bounded_git(self):
        self.assertEqual(set(inspect.signature(retained.corroborate_owner_c115_retained_files).parameters),
                         {"plan_path", "approved_plan_sha256", "custody_root", "repository_root", "now"})
        for names in (("lifecycle-1",), ("lifecycle-2", "lifecycle-1"),
                      ("lifecycle-3", "lifecycle-1", "lifecycle-2")):
            self.inputs = self.fixture.checkout.inputs(names)
            self.prepare()
            with self.subTest(pods=names), self.fixture.checkout.local_lane(self.inputs), \
                    self.barriers(allow_git=True), \
                    patch.object(retained, "inspect_owner_c115", wraps=owner.inspect_owner_c115) as inspection:
                result = self.verify()
            self.assertEqual(inspection.call_count, 1)
            self.assertEqual(result.paths, tuple(sorted(owner.bindings.PATHS)))
            self.assertEqual(result.source_commit, self.fixture.checkout.commit)
            self.assertEqual(result.inspection.commands.pod_names, names)
            self.assertEqual(result.inspection.retained_streams.streams, self.inputs["streams"])
            self.assertEqual(result.inspection.retained_byte_count, owner.OwnerC115Tests.byte_count(self.inputs))
            self.assertEqual((result.owner_principal, result.assurance_mode),
                             ("github:user:6775094", "sole-owner/self-reviewed"))
            self.assertEqual(result.inspection.owner_run.requested_action, "observe")
            self.assertNotIn("ISOLATED", repr(result))
            for name in ("execute", "accepted", "passed", "a41_closed"):
                self.assertFalse(hasattr(result, name))

    def test_committed_executable_and_identical_byte_forms_preserve_metadata_and_occurrences(self):
        checkout = self.fixture.checkout
        raw = b"# ISOLATED EXECUTABLE SOURCE; IDENTICAL RETAINED BYTE FORMS\n"
        source = replace(checkout.original_sources[0], executed_bytes=raw, blob_bytes=raw, git_mode="100755")
        target = self.root / source.path
        target.write_bytes(raw)
        target.chmod(0o755)
        checkout.commit_checkout()
        self.assertEqual(fixture.checkout.legacy.git(self.root, "ls-tree", "HEAD", "--", source.path).split()[0],
                         "100755")
        self.inputs = checkout.inputs(sources=(source,) + checkout.original_sources[1:])
        self.prepare()
        reference = self.manifest["sources"][0]
        executed = Path(reference["executedBytes"]["path"])
        blob = Path(reference["blobBytes"]["path"])
        self.assertNotEqual(executed, blob)
        self.assertNotEqual((executed.stat().st_dev, executed.stat().st_ino),
                            (blob.stat().st_dev, blob.stat().st_ino))
        self.assertEqual((executed.stat().st_nlink, blob.stat().st_nlink), (1, 1))
        self.assertEqual(executed.read_bytes(), raw)
        self.assertEqual(blob.read_bytes(), raw)
        self.assertEqual(reference["executedBytes"]["sha256"], reference["blobBytes"]["sha256"])
        other_bytes = (
            sum(len(self.inputs[name].raw) for name in (*JSON_FIELDS.values(), "run"))
            + sum(len(item.executed_bytes) + len(item.blob_bytes) for item in self.inputs["sources"][1:])
            + sum(len(stream.stdout) + len(stream.stderr) for stream in self.inputs["streams"])
        )
        with checkout.local_lane(self.inputs), self.barriers(allow_git=True):
            result = self.verify()
        acquired = result.inspection.sources[0]
        self.assertEqual(acquired, replace(source, source_commit=checkout.commit))
        self.assertEqual(acquired.git_mode, "100755")
        self.assertEqual(acquired.executed_bytes, acquired.blob_bytes)
        self.assertEqual(result.inspection.retained_byte_count, other_bytes + 2 * len(raw))

    def test_all_evidence_descriptors_survive_git_and_readback_with_status_last(self):
        real_popen, real_read = subprocess.Popen, os.read
        real_corroborate = retained._corroborate_owner_inspection
        reads, children, computed = [], [], []
        expected_paths = {str(path) for path in self.files.custody.rglob("*") if path.is_file()}
        control_ids = {path.stat().st_ino: name for name, path in (
            ("plan", self.files.plan_path), ("run", self.files.record_path), ("status", self.files.status_path))}

        def read(fd, count):
            name = control_ids.get(os.fstat(fd).st_ino, "evidence")
            reads.append(name)
            if computed and name == "evidence":
                self.assertEqual(closed, [])
                for descriptor in held:
                    os.fstat(descriptor)
            return real_read(fd, count)

        def corroborate(*args, **kwargs):
            computed.append(real_corroborate(*args, **kwargs))
            return computed[-1]

        def child(*args, **kwargs):
            self.assertEqual(closed, [])
            self.assertTrue(expected_paths <= {os.readlink(f"/proc/self/fd/{fd}") for fd in held})
            for fd in held:
                os.fstat(fd)
            process = real_popen(*args, **kwargs)
            children.append(process)
            return process

        with self.tracked() as (held, closed), patch.object(retained.os, "read", side_effect=read), \
                patch.object(retained, "_corroborate_owner_inspection", side_effect=corroborate), \
                patch.object(fixture.local.subprocess, "Popen", side_effect=child):
            self.verify()
        self.assertEqual(reads[:6], ["plan", "plan", "run", "run", "status", "status"])
        self.assertEqual(reads[-6:], ["plan", "plan", "run", "run", "status", "status"])
        self.assertEqual(len(children), 140)
        self.assertTrue(all(child.poll() is not None and child.stdout.closed for child in children))

    def test_invalid_time_precedes_all_acquisition(self):
        for now in (None, self.inputs["now"].replace(tzinfo=None),
                    self.inputs["now"].replace(tzinfo=timezone(timedelta(hours=1)))):
            with patch.object(fixture.held_files, "_open_held_file") as opened:
                self.before_git_refusal(now=now, inspected=False)
            opened.assert_not_called()

    def test_bad_approval_and_owner_authority_precede_evidence_acquisition(self):
        denied = self.files.forbid_artifact_reads(self.manifest_path)
        with patch.object(retained.os, "read", side_effect=denied):
            self.before_git_refusal("plan-digest-mismatch", approved_plan_sha256="f" * 64, inspected=False)
            for now in (owner.owner_runs.COLLECTION, owner.owner_runs.REVIEW, owner.owner_runs.EXPIRY):
                self.before_git_refusal(now=now, inspected=False)
            for field, value in (("policySha256", "f" * 64), ("sessionId", "foreign-session"),
                                 ("sourceCommit", "f" * 40), ("targetSha256", "f" * 64)):
                original = self.files.plan[field]
                self.files.plan[field] = value
                self.files.prepare(record_raw=self.inputs["run"].raw)
                self.before_git_refusal("anchor-mismatch", inspected=False)
                self.files.plan[field] = original
            self.prepare()
            self.write(self.files.status_path, fixture.custody.wire({**self.files.status, "withdrawn": True}))
            self.before_git_refusal("withdrawn", inspected=False)

    def test_owner_pg2_policy_and_self_review_checks_precede_evidence(self):
        original = self.inputs["run"]
        for field, value in (("ownerPrincipal", "foreign-owner"), ("policyId", "foreign-policy"),
                             ("assuranceMode", "independent"), ("environment", "Production"),
                             ("profileId", "PG-ONPREM-1"), ("status", "draft"), ("actions", ["purge"])):
            self.inputs["run"] = owner.snapshot({**owner.tree(original), field: value})
            self.prepare()
            with patch.object(retained.os, "read", side_effect=self.files.forbid_artifact_reads(self.manifest_path)):
                self.before_git_refusal(inspected=False)

    def test_manifest_binding_and_digest_precede_every_inventory_reference(self):
        for name in ("evidenceManifestPath", "evidenceManifestSha256", "registrySha256"):
            self.prepare()
            self.files.plan.pop(name)
            self.files.prepare(record_raw=self.inputs["run"].raw)
            self.before_git_refusal(inspected=False)
        self.prepare()
        self.write(self.manifest_path, b"private-canary" + self.manifest_path.read_bytes())
        refs = [Path(self.manifest[name]["path"]) for name in JSON_FIELDS]
        with patch.object(retained.os, "read", side_effect=self.files.forbid_artifact_reads(*refs)):
            self.before_git_refusal("manifest-digest-mismatch", inspected=False)

    def test_complete_closed_manifest_inventory_precedes_reference_reads(self):
        original = deepcopy(self.manifest)
        mutations = (
            lambda m: m.update(extra=False), lambda m: m.pop("registry"),
            lambda m: m.update(schemaVersion="unknown/v1"), lambda m: m.update(sources={}),
            lambda m: m["sources"].pop(), lambda m: m["sources"].append(m["sources"][0]),
            lambda m: m["sources"][-1].pop("ident"), lambda m: m["sources"][-1].update(extra=False),
            lambda m: m["sources"][-1].update(normalization="arbitrary"),
            lambda m: m["sources"][-1].update(gitMode="120000"),
            lambda m: m["sources"][-1].update(cleanFilter="execute"),
            lambda m: m["sources"][-1].update(workingTreeEncoding="utf-16"),
            lambda m: m["sources"][-1].update(ident=True),
            lambda m: m["sources"][-1].update(path=m["sources"][0]["path"]),
            lambda m: m["sources"][-1]["blobBytes"].update(sha256="A" * 64),
            lambda m: m["sources"][-1]["blobBytes"].update(extra=False),
            lambda m: m["sources"][-1]["blobBytes"].update(path="relative/private-canary"),
        )
        for mutate in mutations:
            self.manifest = deepcopy(original)
            mutate(self.manifest)
            self.approve_manifest()
            with self.subTest(mutate=mutate), patch.object(retained.os, "read", side_effect=
                    self.files.forbid_artifact_reads(Path(original["registry"]["path"]))):
                self.before_git_refusal(inspected=False)

    def test_invalid_manifest_json_and_control_manifest_limits(self):
        for raw in (b"\xffprivate-canary", b'{"a":1,"a":2}', b'{"private-canary":', b"x" * (owner.ONE_MIB + 1)):
            self.approve_manifest(raw)
            self.before_git_refusal(inspected=False)
        self.prepare()
        for path in (self.manifest_path, self.sidecar / "manifest.json"):
            raw = path.read_bytes()
            self.write(path, raw + b" " * (owner.ONE_MIB - len(raw)))
            if path == self.manifest_path:
                self.approve_manifest(path.read_bytes())
        self.verify()

    def test_all_json_source_and_stream_hashes_bind_exact_bytes(self):
        paths = [Path(self.manifest[name]["path"]) for name in JSON_FIELDS]
        paths.extend(Path(self.manifest["sources"][0][name]["path"]) for name in ("executedBytes", "blobBytes"))
        paths.extend((self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"))
        for path in paths:
            original = path.read_bytes()
            self.write(path, original + b" ")
            with self.subTest(path=path.name):
                self.before_git_refusal()
            self.write(path, original)

    def test_all_artifacts_refuse_oversize_before_read(self):
        paths = [Path(self.manifest[name]["path"]) for name in JSON_FIELDS]
        paths.extend(Path(self.manifest["sources"][0][name]["path"]) for name in ("executedBytes", "blobBytes"))
        paths.extend((self.manifest_path, self.sidecar / "manifest.json",
                      self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"))
        for path in paths:
            original = path.read_bytes()
            self.write(path, b"x" * (owner.ONE_MIB + 1))
            with self.subTest(path=path.name), patch.object(retained.os, "read", side_effect=
                    self.files.forbid_artifact_reads(path)):
                self.before_git_refusal("artifact-byte-budget-exceeded", inspected=False)
            self.write(path, original)

    def test_distinct_paths_include_every_control_manifest_source_and_stream(self):
        original = deepcopy(self.manifest)
        aliases = [str(self.files.plan_path), str(self.files.record_path), str(self.files.status_path),
                   str(self.manifest_path), original["capture"]["path"], original["registry"]["path"],
                   str(self.sidecar / "manifest.json"), str(self.sidecar / "0000.stdout")]
        for path in aliases:
            self.manifest = deepcopy(original)
            self.manifest["sources"][-1]["blobBytes"]["path"] = path
            self.approve_manifest()
            self.before_git_refusal("distinct-paths-required", inspected=False)

    def test_alias_inodes_across_controls_and_evidence_refuse_even_without_links(self):
        path = Path(self.manifest["sources"][-1]["blobBytes"]["path"])
        identity = self.files.plan_path.stat()
        real_open = retained._open_held_file

        def open_file(name, *args, **kwargs):
            held = real_open(name, *args, **kwargs)
            if name == str(path):
                held.before = fixture.custody.metadata_copy(held.before, st_dev=identity.st_dev, st_ino=identity.st_ino)
            return held

        with patch.object(retained, "_open_held_file", side_effect=open_file):
            self.before_git_refusal("distinct-files-required", inspected=False)

    def test_noncanonical_outside_and_linked_ancestors_refuse(self):
        original = deepcopy(self.manifest)
        for path in ("relative/path", str(self.directory) + "//file", str(self.directory) + "/../file",
                     str(self.directory) + "/./file", str(self.files.custody.parent / "outside")):
            self.manifest = deepcopy(original)
            self.manifest["registry"]["path"] = path
            self.approve_manifest()
            self.before_git_refusal(inspected=False)
        self.manifest = original
        self.approve_manifest()
        moved = self.directory.with_name("moved")
        self.directory.rename(moved)
        self.directory.symlink_to(moved, target_is_directory=True)
        try:
            self.before_git_refusal(inspected=False)
        finally:
            self.directory.unlink()
            moved.rename(self.directory)

    def test_private_single_link_regular_files_and_directories_required(self):
        paths = [self.manifest_path, Path(self.manifest["registry"]["path"]), self.packet,
                 Path(self.manifest["sources"][0]["executedBytes"]["path"]),
                 self.sidecar / "manifest.json", self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"]
        for path in paths:
            raw = path.read_bytes()
            for kind in ("missing", "public", "symlink", "hardlink", "fifo", "directory"):
                backup = self.directory / "temporary-alias"
                if kind == "public":
                    path.chmod(0o644)
                else:
                    path.rename(backup)
                    if kind == "symlink":
                        path.symlink_to(backup)
                    elif kind == "hardlink":
                        os.link(backup, path)
                    elif kind == "fifo":
                        os.mkfifo(path, mode=0o600)
                    elif kind == "directory":
                        path.mkdir(mode=0o700)
                with self.subTest(path=path.name, kind=kind):
                    self.before_git_refusal(inspected=False)
                if kind != "public":
                    if kind == "directory":
                        path.rmdir()
                    elif path.exists() or path.is_symlink():
                        path.unlink()
                    backup.rename(path)
                self.write(path, raw)
        for path in (self.directory, self.sidecar):
            path.chmod(0o750)
            self.before_git_refusal(inspected=False)
            path.chmod(0o700)

    def test_sidecar_schema_inventory_order_and_semantics_stay_required(self):
        for kind in ("missing", "surplus", "reordered", "schema", "hash"):
            self.prepare()
            if kind == "missing":
                (self.sidecar / "0000.stderr").unlink()
            elif kind == "surplus":
                self.write(self.sidecar / "extra", b"")
            else:
                if kind == "reordered":
                    self.stream_manifest["receipts"].reverse()
                elif kind == "schema":
                    self.stream_manifest["schemaVersion"] = "other/v1"
                else:
                    self.stream_manifest["captureSha256"] = "f" * 64
                self.write(self.sidecar / "manifest.json", fixture.custody.wire(self.stream_manifest))
            self.before_git_refusal(inspected=False)

    def test_foreign_tenant_namespace_session_and_commit_deny_before_git(self):
        original = self.inputs["capture"]
        for name, value in (("namespace", "foreign-tenant-namespace"), ("context", "foreign-cluster"),
                            ("selector", "tenant=foreign"), ("appId", "foreign-tenant-app"),
                            ("actorType", "ForeignTenantActor")):
            capture = owner.tree(original)
            capture["target"][name] = value
            if name in ("appId", "actorType"):
                for pod in capture["observations"]["pods"]:
                    pod["appId" if name == "appId" else "actorTypes"] = value if name == "appId" else [value]
            owner.semantics.refresh_declarations(capture)
            self.inputs["capture"] = owner.snapshot(capture)
            self.prepare()
            self.before_git_refusal()
        for name, value in (("qualificationSessionId", "foreign-tenant-session"), ("sourceCommit", "f" * 40)):
            capture = owner.tree(original)
            capture[name] = value
            if name == "qualificationSessionId":
                capture["producer"]["arguments"][5] = value
                capture["producer"]["argumentsSha256"] = owner.semantics.fixture_hash(capture["producer"]["arguments"])
            self.inputs["capture"] = owner.snapshot(capture)
            self.prepare()
            self.before_git_refusal(inspected=True)

    def test_foreign_source_commit_registry_and_owner_materials_deny_before_git(self):
        source = self.manifest["sources"][0]
        source["sourceCommit"] = "f" * 40
        self.approve_manifest()
        self.before_git_refusal(inspected=True)
        self.prepare()
        self.files.plan["registrySha256"] = "f" * 64
        self.files.prepare(record_raw=self.inputs["run"].raw)
        self.before_git_refusal("approved-registry-digest-mismatch", inspected=True)
        for material in ("statement", "review_role_policy"):
            self.inputs = owner.rebind_material(self.inputs, material,
                {**owner.tree(self.inputs[material]), "ownerPrincipal": "foreign-owner"})
            self.prepare()
            self.before_git_refusal(inspected=True)

    def test_exact_32_mib_limit_includes_run_and_both_source_forms(self):
        self.inputs = self.fixture.checkout.inputs(count=5)
        remaining = owner.THIRTY_TWO_MIB - owner.OwnerC115Tests.byte_count(self.inputs)
        # Account for the run's changed custodyRoot length in this fixture.
        self.prepare()
        remaining = owner.THIRTY_TWO_MIB - owner.OwnerC115Tests.byte_count(self.inputs)
        expanded = []
        for stream in self.inputs["streams"]:
            stderr_size = min(remaining, owner.ONE_MIB)
            remaining -= stderr_size
            stdout_size = min(remaining, owner.ONE_MIB - len(stream.stdout))
            remaining -= stdout_size
            expanded.append(replace(stream, stdout=stream.stdout + b" " * stdout_size, stderr=b"x" * stderr_size))
        self.assertEqual(remaining, 0)
        self.inputs.update(streams=tuple(expanded), capture=owner.snapshot(
            owner.outputs.bind(owner.tree(self.inputs["capture"]), tuple(expanded))))
        self.prepare()
        self.assertEqual(self.verify().inspection.retained_byte_count, owner.THIRTY_TWO_MIB)
        stream = self.inputs["streams"][-1]
        self.inputs["streams"] = self.inputs["streams"][:-1] + (replace(stream, stderr=stream.stderr + b"x"),)
        self.inputs["capture"] = owner.snapshot(owner.outputs.bind(owner.tree(self.inputs["capture"]), self.inputs["streams"]))
        self.prepare()
        overflow = self.sidecar / f"{len(self.inputs['streams']) - 1:04d}.stderr"
        with patch.object(retained.os, "read", side_effect=self.files.forbid_artifact_reads(overflow)):
            self.before_git_refusal("aggregate-byte-budget-exceeded", inspected=False)

    def test_repeated_source_byte_occurrences_charge_incrementally_before_next_read(self):
        for source in self.manifest["sources"]:
            for name in ("executedBytes", "blobBytes"):
                reference = source[name]
                reference.update(self.reference(Path(reference["path"]), b"x" * owner.ONE_MIB))
        self.approve_manifest()
        denied = Path(self.manifest["sources"][16]["executedBytes"]["path"])
        with patch.object(retained.os, "read", side_effect=self.files.forbid_artifact_reads(denied)):
            self.before_git_refusal("aggregate-byte-budget-exceeded", inspected=False)

    def test_same_size_evidence_drift_discards_computed_result_with_coarse_metadata(self):
        real_corroborate = retained._corroborate_owner_inspection
        paths = [self.manifest_path, Path(self.manifest["registry"]["path"]), self.packet,
                 Path(self.manifest["sources"][0]["executedBytes"]["path"]),
                 self.sidecar / "manifest.json", self.sidecar / "0000.stdout"]
        for path in paths:
            original = path.read_bytes()
            replacement = (original[:-1] + b" " if original else b"")
            if not original:
                continue

            def corroborate(*args, **kwargs):
                result = real_corroborate(*args, **kwargs)
                self.write(path, replacement)
                return result

            with self.subTest(path=path.name), self.tracked(), \
                    patch.object(fixture.dispatch_files, "_file_metadata", side_effect=lambda m:
                        (m.st_dev, m.st_ino, m.st_mode, m.st_nlink, m.st_uid, m.st_gid, m.st_size)), \
                    patch.object(retained, "_corroborate_owner_inspection", side_effect=corroborate):
                self.refuse("artifact-bytes-drifted")
            self.write(path, original)

    def test_mid_git_and_final_withdrawal_discard_result_and_close_children(self):
        real_popen, real_read = subprocess.Popen, os.read
        for phase in ("git", "final"):
            self.prepare()
            children, plan_reads = [], []
            plan_inode = self.files.plan_path.stat().st_ino

            def withdraw():
                self.write(self.files.status_path, fixture.custody.wire({**self.files.status, "withdrawn": True}))

            def child(*args, **kwargs):
                process = real_popen(*args, **kwargs)
                children.append(process)
                if phase == "git" and len(children) == 1:
                    withdraw()
                return process

            def read(fd, count):
                if os.fstat(fd).st_ino == plan_inode:
                    plan_reads.append(fd)
                    if phase == "final" and len(plan_reads) == 3:
                        self.assertEqual(len(children), 140)
                        withdraw()
                return real_read(fd, count)

            with self.subTest(phase=phase), self.tracked(), \
                    patch.object(retained.os, "read", side_effect=read), \
                    patch.object(fixture.local.subprocess, "Popen", side_effect=child):
                self.refuse("current-status-drifted")
            self.assertEqual(len(children), 140)
            self.assertTrue(all(child.poll() is not None and child.stdout.closed for child in children))

    def test_evidence_ancestor_move_after_git_discards_result(self):
        real_corroborate = retained._corroborate_owner_inspection
        moved = self.directory.with_name("moved")

        def corroborate(*args, **kwargs):
            result = real_corroborate(*args, **kwargs)
            self.directory.rename(moved)
            return result

        try:
            with self.tracked(), patch.object(retained, "_corroborate_owner_inspection", side_effect=corroborate):
                self.refuse()
        finally:
            moved.rename(self.directory)

    def test_cleanup_faults_normalize_after_closing_every_descriptor(self):
        with self.tracked() as (held, closed):
            real_close = os.close

            def close(fd):
                real_close(fd)
                if fd in held:
                    raise OSError("private-canary cleanup")

            with patch.object(retained.os, "close", side_effect=close):
                self.refuse()

    def test_filesystem_faults_and_replaced_files_never_leak_exception_context(self):
        for operation in ("open", "read", "stat", "fstat", "scandir", "lseek"):
            with self.subTest(operation=operation), \
                    patch.object(retained.os, operation, side_effect=OSError("private-canary")):
                self.refuse()

    def test_initial_evidence_substitution_refuses_before_git_and_cleans_files(self):
        target = Path(self.manifest["registry"]["path"])
        identity = target.stat().st_ino
        replacement = self.directory / "replacement"
        self.write(replacement, target.read_bytes())
        real_read = os.read
        replaced = []

        def read(fd, count):
            raw = real_read(fd, count)
            if os.fstat(fd).st_ino == identity and not replaced:
                replaced.append(True)
                os.replace(replacement, target)
            return raw

        with self.tracked(), patch.object(retained.os, "read", side_effect=read):
            self.before_git_refusal("current-status-drifted", inspected=False)
        self.assertEqual(replaced, [True])

    def test_git_deadline_and_child_cleanup_faults_close_all_evidence_and_child(self):
        real_popen = subprocess.Popen
        for fault in ("deadline", "cleanup"):
            children = []

            def child(*args, **kwargs):
                process = real_popen(*args, **kwargs)
                children.append(process)
                if fault == "cleanup":
                    original = process.stdout.close

                    def close():
                        original()
                        try:
                            raise ValueError("private-canary context")
                        except ValueError:
                            raise OSError("private-canary child cleanup") from RuntimeError("private-canary cause")

                    patches.enter_context(patch.object(process.stdout, "close", side_effect=close))
                return process

            with self.subTest(fault=fault), self.tracked(), fixture.ExitStack() as patches:
                patches.enter_context(patch.object(fixture.local.subprocess, "Popen", side_effect=child))
                if fault == "deadline":
                    patches.enter_context(patch.object(fixture.local.select, "select", return_value=([], [], [])))
                self.refuse("local-git-unavailable" if fault == "deadline" else "local-source-unavailable")
            self.assertEqual(len(children), 1)
            self.assertIsNotNone(children[0].poll())
            self.assertTrue(children[0].stdout.closed)

    def test_effective_user_drift_after_git_discards_retained_result(self):
        real_corroborate, effective_uid = retained._corroborate_owner_inspection, os.geteuid()
        computed = []

        def corroborate(*args, **kwargs):
            computed.append(real_corroborate(*args, **kwargs))
            return computed[-1]

        with self.tracked(), patch.object(retained, "_corroborate_owner_inspection", side_effect=corroborate), \
                patch.object(retained.os, "geteuid", side_effect=lambda: effective_uid + bool(computed)):
            self.refuse("effective-user-drifted")

    def test_fresh_files_and_withdrawal_are_reacquired_on_each_invocation(self):
        self.verify()
        self.write(Path(self.manifest["registry"]["path"]), self.inputs["registry"].raw + b" ")
        self.before_git_refusal("artifact-digest-mismatch", inspected=False)
        self.prepare()
        self.verify()
        self.write(self.files.status_path, fixture.custody.wire({**self.files.status, "withdrawn": True}))
        self.before_git_refusal("withdrawn", inspected=False)


if __name__ == "__main__":
    unittest.main()
