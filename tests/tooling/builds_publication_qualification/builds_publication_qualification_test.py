"""Exercise the Story 30.2 pinned Builds publication qualification and its fail-closed branches."""

from __future__ import annotations

import copy
import dataclasses
import hashlib
import importlib.util
import io
import json
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
import urllib.error
import urllib.request
import zipfile
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL = REPO_ROOT / "tools" / "qualify-builds-publication.py"
PROFILE = REPO_ROOT / "tools" / "builds-publication-qualification.json"
BUILDS = REPO_ROOT / "references" / "Hexalith.Builds"
EVIDENCE_ROOT = REPO_ROOT / "_bmad-output" / "implementation-artifacts"
EARLIER_EVIDENCE = EVIDENCE_ROOT / "30-2-qualification-20261101"
COMMITTED_EVIDENCE = EARLIER_EVIDENCE / "run-41304156187-a4788c89"
SELECTED_SOURCE_SHA = "a4788c891958c1f1b6f012ff604c0f9321094751"
SELECTED_ARTIFACT_SHA = "5de2f80e37641b05efb22ba7de65f8f2088d543f31dea618af7575c67b017c3e"
SELECTED_ARTIFACT_BYTES = 29886
HISTORICAL_HASHES = {
    "evidence-upload.json": "434707c03be45d2e703d6c4bf1c38c6b32309f839775be0c623a59f70eae8ba0",
    "set-drift.json": "7fe90ef74f9672c278251fde25d0e17e3d5456a7e9b5b347eb20ff10d6cd730e",
    "partial-publication.json": "353d49332fc852abf066598ca22b33c1d35a479cd4061af075c27f371c49c805",
    "recovery.json": "921b3cad348372dba46c29a3092c8814a6c3c94b38833b41fadb1d40925cf3a3",
    "approval.md": "2befe196aba013ed923f7a277e6605d1812937a6a4dda0947ff9ccf83aa8924c",
    "phase-verification.json": "3226cbb95442b6d6fb0dc45fa08bf4a006b0c0e9e69116cc145c0c9cd143a561",
    "complete-set-dry-run.json": "0dfbb0eb92da1b10a11b711c8f9016f53a1f09ec8363ee44440ad7cc34052cd2",
}
PINNED_SHA = "87d45477c16fef2b3db706d2531e1cf3ff98640f"
HISTORICAL_BUILDS_SHA = "f0feeb234cbc16f88c2886a6ffae7122e05dd64b"
# Epic 30 Story 30.4 names exactly these five mappings under one set identity.
EXPECTED_MAPPINGS = [
    "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj|memories",
    "src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj|memories-mcp",
    "src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj|memories-access-telemetry",
    "src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj|memories-access-telemetry-clock",
    "src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj|memories-domain-service",
]
CHECKPOINTS = (
    "complete-set-dry-run",
    "set-drift",
    "phase-verification",
    "partial-publication",
    "recovery",
    "evidence-upload",
)
PENDING_APPROVAL = (
    "# Story 30.2 Exact-Revision Owner Approval\n\nStatus: pending\n\nQualification result: pass\n\n"
    "## Owner decisions\n\n| Role | Owner | Decision | Date | Notes |\n| --- | --- | --- | --- | --- |\n"
    "| Builds Maintainer | | pending | | |\n| Memories Release Engineer | | pending | | |\n"
)
APPROVED_EXCEPTIONS = {
    "no-dry-run-mode",
    "no-partial-result-document",
    "no-missing-only-republish",
    "no-caller-set-check",
    "warn-upload-without-hash-manifest",
}


def load_tool():
    specification = importlib.util.spec_from_file_location("qualify_builds_publication", TOOL)
    module = importlib.util.module_from_spec(specification)
    sys.modules[specification.name] = module
    specification.loader.exec_module(module)
    return module


Q = load_tool()


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def run_git(directory: Path, *arguments: str) -> str:
    return subprocess.run(
        ["git", "-C", str(directory), "-c", "core.hooksPath=/dev/null", "-c", "commit.gpgsign=false",
         "-c", "user.name=Qualification Tests", "-c", "user.email=qualification-tests@example.invalid", *arguments],
        capture_output=True, text=True, check=True, timeout=60,
    ).stdout


def historical_bytes(repository: Path, revision: str, path: str) -> bytes:
    """Recreate checkout bytes for the immutable capture's original source revision."""
    data = subprocess.run(["git", "-C", str(repository), "show", f"{revision}:{path}"],
                          capture_output=True, check=True, timeout=60).stdout
    # Memories checks out this historical JSON as CRLF, as the captured receipt did.
    return data.replace(b"\n", b"\r\n") if path == "tools/builds-publication-qualification.json" else data


def run_main(*arguments: str) -> tuple:
    stdout, stderr = io.StringIO(), io.StringIO()
    with redirect_stdout(stdout), redirect_stderr(stderr):
        code = Q.main(list(arguments))
    return code, stdout.getvalue(), stderr.getvalue()


def read_result(directory: Path, checkpoint: str) -> dict:
    return json.loads((directory / f"{checkpoint}.json").read_bytes().decode("utf-8"))


def check(document: dict, name: str) -> dict:
    matches = [entry for entry in document["checks"] if entry["name"] == name]
    if len(matches) != 1:
        raise AssertionError(f"expected exactly one check {name!r} in {document['checkpoint']}")
    return matches[0]


class RepositoryQualificationTests(unittest.TestCase):
    """Run the real harness once against this repository's pinned Builds revision."""

    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix="builds-qualification-test-")
        cls.output = Path(cls.temporary.name) / "evidence"
        cls.completed = subprocess.run(
            [sys.executable, str(TOOL), "--output-directory", str(cls.output)],
            cwd=REPO_ROOT, capture_output=True, text=True, timeout=1200, check=False,
        )
        cls.results = {checkpoint: read_result(cls.output, checkpoint) for checkpoint in CHECKPOINTS}

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()






    def test_complete_set_publishes_and_re_verifies_every_declared_member(self):
        document = self.results["complete-set-dry-run"]
        members = document["observations"]["members"]
        self.assertEqual(EXPECTED_MAPPINGS, [f"{member['project']}|{member['repository']}" for member in members])
        self.assertTrue(all(member["state"] == "published" for member in members))
        self.assertEqual(1, len({member["created"] for member in members}))
        self.assertEqual(5, len({member["indexDigest"] for member in members}))
        self.assertEqual(1, document["observations"]["registryLogins"])
        self.assertEqual(5, document["observations"]["sdkPublications"])
        for name in ("preflight-verify-passes", "preflight-publish-passes", "publisher-completes-the-declared-set",
                     "container-preflight-precedes-login-and-every-write", "one-validate-and-smoke-run-per-member"):
            self.assertEqual("pass", check(document, name)["result"], name)

    def test_set_drift_is_refused_caller_side_and_by_builds_between_phases(self):
        expected = {
            "container-dropped": ("container-member-dropped", "accepted", "publication-identity-changed"),
            "container-added": ("container-member-added", "accepted", "publication-identity-changed"),
            "container-renamed": ("container-member-renamed", "accepted", "publication-identity-changed"),
            "container-remapped": ("container-member-remapped", "accepted", "not-detected"),
            "package-count-low": ("package-count-mismatch", "package-inventory-mismatch", "package-inventory-mismatch"),
            "package-count-high": ("package-count-mismatch", "package-inventory-mismatch", "package-inventory-mismatch"),
            "package-renamed": ("package-inventory-drift", "accepted", "publication-identity-changed"),
        }
        variants = {item["variant"]: item for item in self.results["set-drift"]["observations"]["variants"]}
        self.assertEqual(set(expected), set(variants))
        for name, (reason, fresh, between) in expected.items():
            with self.subTest(variant=name):
                self.assertTrue(any(item.startswith(reason) for item in variants[name]["callerRefusal"]))
                self.assertEqual(fresh, variants[name]["freshBuildsVerdict"])
                self.assertEqual(between, variants[name]["betweenPhaseBuildsVerdict"])
        remapped = check(self.results["set-drift"], "container-remapped:between-phase-builds-verdict")["detail"]
        self.assertEqual({"project": "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj", "repository": "memories-mcp"},
                         remapped["firstSdkPublication"])

    def test_partial_publication_classifies_members_and_ambiguity_is_never_success(self):
        document = self.results["partial-publication"]
        self.assertEqual("memories-access-telemetry", document["observations"]["failedMember"])
        variants = document["observations"]["variants"]
        self.assertEqual(["publish-failure", "validation-failure", "smoke-failure"], [item["variant"] for item in variants])
        for item in variants:
            with self.subTest(variant=item["variant"]):
                derived = item["derivedResult"]
                self.assertEqual(["memories", "memories-mcp"], derived["published"])
                self.assertEqual(["memories-access-telemetry"], derived["ambiguous"])
                self.assertEqual(["memories-access-telemetry-clock", "memories-domain-service"], derived["notAttempted"])
                self.assertEqual("incomplete", derived["result"])
                self.assertFalse(derived["success"])
                self.assertNotEqual(0, item["publisher"]["exitCode"])
                self.assertTrue(item["ambiguousProblems"])

    def test_recovery_reads_published_members_and_the_missing_set_is_exact(self):
        document = self.results["recovery"]
        decision = document["observations"]["recoveryDecision"]
        self.assertEqual(["memories", "memories-mcp"], decision["published"])
        self.assertEqual(["memories-access-telemetry", "memories-access-telemetry-clock", "memories-domain-service"],
                         decision["missing"])
        self.assertEqual("30-5-partial-release-recovery", decision["missingMemberPublisher"])
        self.assertEqual([], decision["blocked"])
        self.assertEqual(["published", "published", "missing", "missing", "missing"],
                         [member["disposition"] for member in document["observations"]["members"]])
        negative = document["observations"]["negativeCases"]
        self.assertEqual({"probe-401", "probe-503", "validation-failure", "smoke-failure"}, set(negative))
        for name, (destination, evidence_state) in {
            "probe-401": ("destination-probe-failure", "ambiguous"),
            "probe-503": ("destination-probe-failure", "ambiguous"),
            "validation-failure": ("occupied", "ambiguous"),
            "smoke-failure": ("occupied", "ambiguous"),
        }.items():
            with self.subTest(case=name):
                self.assertEqual(["memories-access-telemetry"], negative[name]["blocked"])
                self.assertEqual((destination, evidence_state, "blocked"),
                                 (negative[name]["member"]["destination"], negative[name]["member"]["evidenceState"],
                                  negative[name]["member"]["disposition"]))
        self.assertEqual(0, negative["smoke-failure"]["member"]["observeCreated"]["exitCode"])
        self.assertEqual("platform-set-mismatch", negative["validation-failure"]["member"]["observeCreated"]["refusal"])
        for name in ("observe-created-re-reads:memories", "observe-created-re-reads:memories-mcp", "missing-set-is-exact",
                     "ambiguous-destination-401-blocks-recovery", "ambiguous-destination-503-blocks-recovery",
                     "present-but-unverifiable-member-blocks-recovery", "present-valid-but-unhealthy-member-blocks-recovery",
                     "rerun-refuses-collisions-before-any-write", "fresh-verify-refuses-the-occupied-version"):
            self.assertEqual("pass", check(document, name)["result"], name)
        rerun = check(document, "rerun-refuses-collisions-before-any-write")["detail"]
        self.assertEqual(("version-collision", 0), (rerun["refusal"], rerun["writes"]))

    def test_evidence_upload_manifest_covers_the_set_and_the_caller_upload_refuses_gaps(self):
        document = self.results["evidence-upload"]
        steps = document["observations"]["buildsUploadSteps"]
        self.assertEqual(2, len(steps))
        for step in steps:
            self.assertEqual(
                (
                    "${{ steps.semantic-release.outputs.oci-release-version != '' && 'error' || 'warn' }}",
                    ".hexalith/release-evidence/**",
                    "true",
                    "30",
                ),
                (step["if-no-files-found"], step["path"], step["include-hidden-files"], step["retention-days"]),
            )
            self.assertEqual("pass", check(document, f"builds-upload-covers-hidden-evidence-root:line-{step['line']}")["result"])
        self.assertEqual("pass", check(document, "no-stray-builds-output-outside-the-expected-evidence")["result"])
        manifest = document["observations"]["callerHashManifest"]
        self.assertEqual(4 + 5 * len(Q.MEMBER_FILES), len(manifest))
        self.assertEqual(sorted(entry["path"] for entry in manifest), [entry["path"] for entry in manifest])
        rendered = "".join(f"{entry['sha256']}  {entry['path']}\n" for entry in manifest)
        self.assertEqual(sha256(rendered.encode("utf-8")), document["observations"]["callerHashManifestSha256"])
        for case in ("missing-file", "tampered-file", "unlisted-file", "empty-evidence"):
            self.assertEqual("pass", check(document, f"caller-upload-refuses-{case}")["result"], case)


    def test_occupied_or_ambiguous_nuget_destinations_stop_before_any_write(self):
        document = self.results["phase-verification"]
        observed = document["observations"]["nugetDestinations"]
        self.assertEqual({
            "package-already-published": ("version-not-newer", "version-not-newer", "preflight-sequence-invalid"),
            "package-probe-503": ("destination-probe-failure", "destination-probe-failure", "preflight-sequence-invalid"),
        }, {case: (item["publish"], item["freshVerify"], item["publisher"]) for case, item in observed.items()})
        for case in observed:
            self.assertEqual("pass", check(document, f"nuget-{case}:refused-before-any-write")["result"], case)




class ProfileTests(unittest.TestCase):
    def valid(self) -> dict:
        return json.loads(PROFILE.read_bytes())

    def test_profile_declares_the_pinned_sha_ten_packages_and_five_exact_mappings(self):
        profile = Q.load_profile(PROFILE.read_bytes())
        self.assertEqual(PINNED_SHA, profile.builds_sha)
        self.assertEqual(10, profile.expected_package_count)
        self.assertEqual(EXPECTED_MAPPINGS, [mapping.line for mapping in profile.mappings])
        self.assertEqual(b"\r\n", PROFILE.read_bytes()[-2:])

    def test_profile_rejects_every_malformed_declaration(self):
        def mutate(change):
            document = self.valid()
            change(document)
            return json.dumps(document).encode("utf-8")

        def mapping(value):
            return mutate(lambda document: document.__setitem__("containerProjects", [value, *document["containerProjects"][1:]]))

        cases = {
            "duplicate-key": PROFILE.read_bytes().replace(b'"expectedPackageCount": 10,', b'"expectedPackageCount": 10, "expectedPackageCount": 10,'),
            "not-json": b"{",
            "missing-key": mutate(lambda document: document.pop("buildsSha")),
            "extra-key": mutate(lambda document: document.__setitem__("helpers", {})),
            "schema": mutate(lambda document: document.__setitem__("schemaVersion", "v0")),
            "upper-sha": mutate(lambda document: document.__setitem__("buildsSha", PINNED_SHA.upper())),
            "short-sha": mutate(lambda document: document.__setitem__("buildsSha", PINNED_SHA[:-1])),
            "zero-count": mutate(lambda document: document.__setitem__("expectedPackageCount", 0)),
            "boolean-count": mutate(lambda document: document.__setitem__("expectedPackageCount", True)),
            "string-count": mutate(lambda document: document.__setitem__("expectedPackageCount", "10")),
            "no-projects": mutate(lambda document: document.__setitem__("containerProjects", [])),
            "no-pipe": mapping("src/A/A.csproj"),
            "two-pipes": mapping("src/A/A.csproj|a|b"),
            "absolute": mapping("/src/A/A.csproj|a"),
            "parent": mapping("src/../A.csproj|a"),
            "backslash": mapping("src\\A\\A.csproj|a"),
            "not-csproj": mapping("src/A/A.fsproj|a"),
            "upper-repository": mapping("src/A/A.csproj|Memories"),
            "padded": mapping(" src/A/A.csproj|a"),
            "duplicate-project": mapping("src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj|other"),
            "duplicate-repository": mapping("src/A/A.csproj|memories-mcp"),
        }
        for name, data in cases.items():
            with self.subTest(case=name), self.assertRaises(Q.QualificationError) as context:
                Q.load_profile(data)
            self.assertEqual("profile-invalid", context.exception.code)


class ReleaseSetTests(unittest.TestCase):
    def declared(self):
        return Q.load_profile(PROFILE.read_bytes()).mappings

    def test_mapping_drift_classifies_drop_add_rename_remap_and_duplicates(self):
        declared = self.declared()
        M = Q.Mapping
        self.assertEqual([], Q.mapping_drift(declared, declared))
        self.assertEqual([], Q.mapping_drift(declared, tuple(reversed(declared))))
        cases = {
            "dropped": (declared[1:], "container-member-dropped:"),
            "added": ((*declared, M("src/X/X.csproj", "x")), "container-member-added:src/X/X.csproj|x"),
            "renamed": ((M(declared[0].project, "other"), *declared[1:]), "container-member-renamed:"),
            "remapped": ((M(declared[0].project, declared[2].repository), *declared[1:]), "container-member-remapped:"),
            "duplicate-project": ((*declared, declared[0]), "container-project-duplicate"),
            "duplicate-repository": ((*declared, M("src/X/X.csproj", declared[0].repository)), "container-repository-duplicate"),
        }
        for name, (candidate, prefix) in cases.items():
            with self.subTest(case=name):
                self.assertTrue(any(reason.startswith(prefix) for reason in Q.mapping_drift(declared, candidate)))

    def test_package_drift_detects_count_duplicates_and_identity_changes(self):
        ids = [f"Package.{index}" for index in range(10)]
        self.assertEqual([], Q.package_drift(10, ids, ids))
        self.assertIn("package-count-mismatch:9!=10", Q.package_drift(10, ids, ids[:-1]))
        self.assertIn("package-count-mismatch:11!=10", Q.package_drift(10, ids, [*ids, "Package.X"]))
        self.assertIn("package-ids-not-unique", Q.package_drift(10, ids, [*ids[:-1], "package.0"]))
        self.assertEqual(["package-inventory-drift"], Q.package_drift(10, ids, [*ids[:-1], "Package.Renamed"]))
        self.assertEqual(["package-inventory-drift"], Q.package_drift(10, ids, list(reversed(ids))))

    def test_repository_discovery_matches_the_declared_profile(self):
        self.assertEqual(sorted(EXPECTED_MAPPINGS), sorted(mapping.line for mapping in Q.discover_container_projects(REPO_ROOT)))

    def test_repository_discovery_fails_closed_on_ambiguous_or_unreadable_projects(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = root / "src" / "A" / "A.csproj"
            project.parent.mkdir(parents=True)
            disabled = root / "src" / "B" / "B.csproj"
            disabled.parent.mkdir(parents=True)
            disabled.write_text("<Project><PropertyGroup><ContainerRepository>b</ContainerRepository></PropertyGroup></Project>")
            for name, content, code in (
                ("no-repository", "<Project><PropertyGroup><EnableContainer>true</EnableContainer></PropertyGroup></Project>",
                 "container-repository-ambiguous"),
                ("two-repositories", "<Project><PropertyGroup><EnableContainer>true</EnableContainer>"
                 "<ContainerRepository>a</ContainerRepository><ContainerRepository>c</ContainerRepository></PropertyGroup></Project>",
                 "container-repository-ambiguous"),
                ("not-xml", "<Project>", "container-project-unreadable"),
            ):
                project.write_text(content)
                with self.subTest(case=name), self.assertRaises(Q.QualificationError) as context:
                    Q.discover_container_projects(root)
                self.assertEqual(code, context.exception.code)
            project.write_text("<Project><PropertyGroup><EnableContainer>True</EnableContainer>"
                               "<ContainerRepository> a </ContainerRepository></PropertyGroup></Project>")
            self.assertEqual([Q.Mapping("src/A/A.csproj", "a")], list(Q.discover_container_projects(root)))

    def test_release_manifest_loading_rejects_ambiguous_inventories(self):
        def manifest(packages):
            return json.dumps({"packages": packages}).encode("utf-8")

        cases = {
            "not-json": b"[",
            "duplicate-key": b'{"packages": [], "packages": []}',
            "no-packages": manifest([]),
            "mixed-shape": manifest([{"packageId": "A", "id": "A"}]),
            "missing-package-id": manifest([{"project": "src/A.csproj"}]),
            "padded": manifest([{"id": " A"}]),
            "case-duplicate": manifest([{"id": "A"}, {"id": "a"}]),
        }
        for name, data in cases.items():
            with self.subTest(case=name), self.assertRaises(Q.QualificationError) as context:
                Q.load_release_manifest(data)
            self.assertEqual("release-manifest-invalid", context.exception.code)

    def test_authoritative_manifest_has_ten_ids_without_legacy_fields(self):
        source = REPO_ROOT / "tools" / "release-packages.json"
        before = source.read_bytes()
        document, ids = Q.load_release_manifest(before)
        context = Q.prepare_context(REPO_ROOT, PROFILE, TOOL)
        self.assertEqual(10, len(ids))
        self.assertEqual(list(ids), [entry["id"] for entry in document["packages"]])
        self.assertTrue(all("packageId" not in entry for entry in document["packages"]))
        self.assertEqual(before, context.release_manifest)
        self.assertEqual(sha256(before), sha256(context.release_manifest))
        self.assertEqual(before, source.read_bytes())








class EvidenceAnalysisTests(unittest.TestCase):
    """Corrupt real single-member Builds evidence and require a non-published classification."""

    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix="builds-member-evidence-")
        cls.context = Q.prepare_context(REPO_ROOT, PROFILE, TOOL)
        if not cls.context.ready:
            raise AssertionError(cls.context.prerequisite_failures)
        cls.mapping = cls.context.profile.mappings[0]
        sandbox = Q.Sandbox(cls.context, Path(cls.temporary.name), "member")
        for phase in ("verify", "publish"):
            outcome = sandbox.preflight(phase, repositories=[cls.mapping.repository])
            if outcome.returncode:
                raise AssertionError(outcome.summary())
        outcome = sandbox.publisher([cls.mapping])
        if outcome.returncode:
            raise AssertionError(outcome.summary())
        cls.sandbox = sandbox
        cls.member = sandbox.evidence_root / cls.mapping.repository

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def classify(self, root: Path, source_sha=None):
        return Q.classify_members(root, [self.mapping], source_sha or self.context.source["sha"])[0]

    def corrupted(self, change) -> Path:
        root = Path(tempfile.mkdtemp(dir=self.temporary.name))
        shutil.copytree(self.member, root / self.mapping.repository)
        change(root / self.mapping.repository)
        return root

    def test_complete_member_evidence_is_published(self):
        member = self.classify(self.sandbox.evidence_root)
        self.assertEqual(("published", []), (member["state"], member["problems"]))
        self.assertEqual(set(Q.PLATFORMS), set(member["children"]))

    def test_absent_member_evidence_is_not_attempted(self):
        self.assertEqual("not-attempted", self.classify(Path(self.temporary.name) / "nothing")["state"])

    def test_every_missing_member_file_is_ambiguous(self):
        for name in Q.MEMBER_FILES:
            with self.subTest(file=name):
                member = self.classify(self.corrupted(lambda directory, name=name: (directory / name).unlink()))
                self.assertEqual("ambiguous", member["state"])
                self.assertTrue(member["problems"])

    def test_tampered_or_contradictory_member_evidence_is_ambiguous(self):
        def rewrite(name, change):
            def apply(directory):
                document = json.loads((directory / name).read_text())
                change(document)
                (directory / name).write_text(json.dumps(document))
            return apply

        def append(name):
            return lambda directory: (directory / name).write_bytes((directory / name).read_bytes() + b" ")

        def duplicate(name):
            # The real files already carry "result": a leading repeat would let the last value silently win.
            return lambda directory: (directory / name).write_text(
                (directory / name).read_text().replace("{", '{"result": "pass", ', 1))

        cases = {
            "validation-failed": (rewrite("oci-validation.json", lambda d: d.__setitem__("result", "fail")), "validation-not-pass"),
            "wrong-image": (rewrite("oci-validation.json", lambda d: d.__setitem__("image", "registry.hexalith.com/x:1.0.0")),
                            "validation-image-mismatch"),
            "one-platform": (rewrite("oci-validation.json", lambda d: d.__setitem__("platforms", ["linux/amd64"])),
                             "validation-platform-mismatch"),
            "index-bytes": (append("index.raw"), "index-raw-hash-mismatch"),
            "manifest-bytes": (append("child-linux-arm64.manifest.raw"), "manifest-raw-hash-mismatch:linux/arm64"),
            "config-bytes": (append("child-linux-amd64.config.raw"), "config-raw-hash-mismatch:linux/amd64"),
            "smoke-failed": (rewrite("smoke-results.json", lambda d: d.__setitem__("result", "liveness-timeout")),
                             "smoke-not-pass:liveness-timeout"),
            "smoke-digest": (rewrite("smoke-results.json", lambda d: d["platforms"][0].__setitem__("digest", "sha256:" + "0" * 64)),
                             "smoke-digest-mismatch:linux/amd64"),
            "smoke-cleanup": (rewrite("smoke-results.json", lambda d: d["platforms"][1].__setitem__("cleanup", "failure")),
                              "smoke-platform-not-pass:linux/arm64"),
            "smoke-log": (append("smoke-linux-amd64.log"), "smoke-hash-mismatch:smoke-linux-amd64.log"),
            "smoke-hash-list": (lambda d: (d / "smoke-sha256.txt").write_text(""), "smoke-hash-list-incomplete"),
            "smoke-one-platform": (rewrite("smoke-results.json", lambda d: d.__setitem__("platforms", d["platforms"][:1])),
                                   "smoke-platform-set-mismatch"),
            "one-child": (rewrite("oci-validation.json", lambda d: d.__setitem__("children", d["children"][:1])),
                          "validation-child-count-mismatch"),
            "created-labels-disagree": (rewrite("oci-validation.json", lambda d: d["children"][0]["provenance_labels"].__setitem__(
                "org.opencontainers.artifact.created", "2000-01-01T00:00:00Z")), "created-label-mismatch:linux/amd64"),
            "created-instants-differ": (rewrite("oci-validation.json", lambda d: d["children"][1]["provenance_labels"].update({
                "org.opencontainers.image.created": "2000-01-01T00:00:00Z",
                "org.opencontainers.artifact.created": "2000-01-01T00:00:00Z"})), "created-instant-not-unique"),
            "validation-duplicate-key": (duplicate("oci-validation.json"), "validation-duplicate-key"),
            "smoke-duplicate-key": (duplicate("smoke-results.json"), "smoke-duplicate-key"),
            "validation-not-json": (lambda d: (d / "oci-validation.json").write_text("{"), "validation-invalid-json"),
            "smoke-not-object": (lambda d: (d / "smoke-results.json").write_text("[]"), "smoke-invalid-json"),
        }
        for name, (change, problem) in cases.items():
            with self.subTest(case=name):
                member = self.classify(self.corrupted(change))
                self.assertEqual("ambiguous", member["state"])
                self.assertTrue(any(item.startswith(problem) for item in member["problems"]), member["problems"])
        member = self.classify(self.sandbox.evidence_root, source_sha="d" * 40)
        self.assertIn("revision-label-mismatch:linux/amd64", member["problems"])
        image = f"{Q.REGISTRY}/{self.mapping.repository}:{Q.QUALIFICATION_VERSION}"
        inspection = Q.inspect_member(self.member, image, self.context.source["sha"], "9.9.9")
        self.assertIn("version-label-mismatch:linux/amd64", inspection.problems)

    def test_checkpoints_return_early_without_their_prerequisite_state(self):
        scratch = Path(tempfile.mkdtemp(dir=self.temporary.name))
        two_members = dataclasses.replace(
            self.context, profile=Q.Profile(self.context.profile.builds_sha, 10, self.context.profile.mappings[:2]))
        for function, context, guard in (
            (Q.run_phase_verification, self.context, "dry-run-evidence-available"),
            (Q.run_partial_publication, two_members, "declared-set-has-a-third-member"),
            (Q.run_recovery, self.context, "partial-evidence-available"),
            (Q.run_evidence_upload, self.context, "dry-run-evidence-available"),
        ):
            with self.subTest(checkpoint=function.__name__):
                result = Q.CheckpointResult(function.__name__)
                function(context, scratch, {}, result)
                self.assertFalse(result.passed)
                self.assertEqual(("fail", guard), (result.checks[-1]["result"], result.checks[-1]["name"]))
                self.assertEqual([], result.exceptions)
        self.assertEqual([], list(scratch.iterdir()))

    def test_container_project_missing_is_a_prerequisite_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            profile = Path(directory) / "profile.json"
            document = json.loads(PROFILE.read_bytes())
            document["containerProjects"][0] = "src/Hexalith.Memories.Missing/Hexalith.Memories.Missing.csproj|memories"
            profile.write_text(json.dumps(document))
            context = Q.prepare_context(REPO_ROOT, profile, TOOL)
        self.assertIn("container-project-missing: src/Hexalith.Memories.Missing/Hexalith.Memories.Missing.csproj",
                      context.prerequisite_failures)
        self.assertFalse(context.ready)

    def test_a_changed_installed_helper_is_refused_before_import(self):
        world = self.sandbox.world
        config = world.config()
        changed = {**config["helperHashes"], "publication_preflight.py": "0" * 64}
        world.write_config({**config, "helperHashes": changed})
        try:
            stderr = io.StringIO()
            with mock.patch.dict("os.environ", {Q.WORLD_VARIABLE: str(world.directory)}), redirect_stderr(stderr):
                self.assertEqual(96, Q.run_seam("preflight", ["--phase", "verify"]))
            self.assertIn("installed-helper-changed", stderr.getvalue())
            self.assertNotIn("publication_preflight", sys.modules)
        finally:
            world.write_config(config)

    def test_derived_set_result_never_treats_ambiguity_as_success(self):
        published = {"repository": "a", "state": "published"}
        self.assertEqual("pass", Q.derive_partial_result([published])["result"])
        for members in ([], [published, {"repository": "b", "state": "ambiguous"}],
                        [published, {"repository": "b", "state": "not-attempted"}]):
            derived = Q.derive_partial_result(members)
            self.assertEqual(("incomplete", False), (derived["result"], derived["success"]))

    def test_caller_hash_manifest_refuses_missing_tampered_unlisted_and_empty_evidence(self):
        root = self.sandbox.evidence_root.parent
        entries = Q.build_hash_manifest(root)
        self.assertEqual([], Q.verify_hash_manifest(root, entries))
        self.assertEqual(["manifest-empty"], Q.verify_hash_manifest(Path(self.temporary.name) / "absent", []))
        copy = Path(tempfile.mkdtemp(dir=self.temporary.name)) / "evidence"
        shutil.copytree(root, copy)
        first = copy / entries[0][0]
        first.write_bytes(first.read_bytes() + b" ")
        (copy / entries[1][0]).unlink()
        (copy / "extra.txt").write_text("x")
        problems = Q.verify_hash_manifest(copy, entries)
        self.assertIn(f"hash-mismatch:{entries[0][0]}", problems)
        self.assertIn(f"missing:{entries[1][0]}", problems)
        self.assertIn("unlisted:extra.txt", problems)

    def test_sandbox_environment_cannot_reach_real_services(self):
        environment = self.sandbox.environment()
        self.assertTrue(environment["PATH"].startswith(str(self.sandbox.world.bin_directory)))
        for variable in ("HTTPS_PROXY", "https_proxy", "HTTP_PROXY", "ALL_PROXY"):
            self.assertEqual(Q.UNREACHABLE_PROXY, environment[variable])
        self.assertNotIn("GITHUB_TOKEN", environment)
        self.assertTrue(environment["DOCKER_HOST"].startswith("unix:///nonexistent/"))
        self.assertEqual("false", environment["HEXALITH_RELEASE_REQUIRE_AUTHORITY"])
        self.assertIn("not-a-secret", environment["HEXALITH_ZOT_API_KEY"])
        installed = {path.name: sha256(path.read_bytes()) for path in self.sandbox.release_directory.iterdir()}
        self.assertEqual(self.context.builds.helpers, installed)




if __name__ == "__main__":
    unittest.main()
