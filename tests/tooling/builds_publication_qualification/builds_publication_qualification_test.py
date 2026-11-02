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
PINNED_SHA = "6e7011fc754e74ac2a8864da1ce0b6d8f7178a48"
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

    def test_harness_exits_zero_with_six_passing_results(self):
        self.assertEqual(0, self.completed.returncode, self.completed.stdout + self.completed.stderr)
        self.assertEqual(sorted([*(f"{name}.json" for name in CHECKPOINTS), "approval.md"]),
                         sorted(path.name for path in self.output.iterdir()))
        for checkpoint, document in self.results.items():
            with self.subTest(checkpoint=checkpoint):
                self.assertEqual(Q.SCHEMA_VERSION, document["schemaVersion"])
                self.assertEqual(checkpoint, document["checkpoint"])
                self.assertEqual(("pass", "pass", "pass"),
                                 (document["result"], document["checksResult"], document["overallResult"]))
                self.assertTrue(document["synthetic"])
                self.assertEqual([], document["reasons"])
                self.assertTrue(document["checks"])

    def test_every_result_binds_identical_builds_source_harness_and_profile_identities(self):
        bindings = [document["bindings"] for document in self.results.values()]
        self.assertTrue(all(binding == bindings[0] for binding in bindings))
        binding = bindings[0]
        self.assertEqual(PINNED_SHA, binding["builds"]["sha"])
        self.assertEqual(PINNED_SHA, binding["builds"]["superprojectGitlink"])
        self.assertEqual(PINNED_SHA, binding["builds"]["checkout"])
        self.assertTrue(binding["builds"]["verified"])
        self.assertEqual(set(Q.EXECUTED_HELPERS), set(binding["builds"]["executedHelpers"]))
        for name, digest in binding["builds"]["executedHelpers"].items():
            pinned = subprocess.run(["git", "-C", str(BUILDS), "cat-file", "blob", f"{PINNED_SHA}:Github/publish-containers/{name}"],
                                    capture_output=True, check=True).stdout
            self.assertEqual(sha256(pinned), digest, name)
        self.assertEqual(run_git(REPO_ROOT, "rev-parse", "HEAD").strip(), binding["source"]["sha"])
        self.assertIsInstance(binding["source"]["dirty"], bool)
        self.assertEqual({"path": "tools/qualify-builds-publication.py", "sha256": sha256(TOOL.read_bytes())}, binding["harness"])
        self.assertEqual({"path": "tools/builds-publication-qualification.json", "sha256": sha256(PROFILE.read_bytes())},
                         binding["profile"])

    def test_frozen_identity_binds_the_executed_helpers_and_the_authoritative_id_manifest(self):
        document = self.results["phase-verification"]
        for name in ("builds-workflow-and-action-sha", "builds-executed-helper-hashes", "memories-source",
                     "container-repository-set", "package-inventory", "release-identity"):
            self.assertEqual("pass", check(document, f"frozen-identity-binds:{name}")["result"], name)
        for phase in ("verify", "publish", "container"):
            self.assertEqual("pass", check(document, f"{phase}-record-binds-the-frozen-identity")["result"])
        self.assertEqual("pass", check(document, "repeated-verification-reproduces-the-identity")["result"])
        self.assertEqual("pass", check(document, "per-image-verification-never-collides")["result"])

    def test_approval_stays_pending_and_lists_identities_and_evidence_hashes(self):
        data = (self.output / "approval.md").read_bytes()
        text = data.decode("utf-8")
        self.assertIn("\r\nStatus: pending\r\n", text)
        self.assertNotRegex(text.lower(), r"\|\s*(approved|rejected|accepted)\s*\|")
        self.assertIn("| Builds Maintainer | | pending | | |", text)
        self.assertIn("| Memories Release Engineer | | pending | | |", text)
        self.assertIn("## Upstream candidate fixtures", text)
        self.assertIn(f"\r\n{Q.UPSTREAM_FIXTURE_COMMAND}\r\n", text)
        self.assertEqual(self.results["recovery"]["bindings"]["source"]["dirty"],
                         "**Not approvable:** this evidence came from a dirty working tree." in text)
        binding = self.results["recovery"]["bindings"]
        for value in (PINNED_SHA, binding["source"]["sha"], binding["harness"]["sha256"], binding["profile"]["sha256"],
                      *binding["builds"]["executedHelpers"].values()):
            self.assertIn(f"`{value}`", text)
        for checkpoint in CHECKPOINTS:
            digest = sha256((self.output / f"{checkpoint}.json").read_bytes())
            self.assertIn(f"| {checkpoint} | `{checkpoint}.json` | pass | `{digest}` |", text)

    def test_evidence_is_written_with_crlf_so_recorded_hashes_survive_checkout(self):
        for path in self.output.iterdir():
            with self.subTest(path=path.name):
                data = path.read_bytes()
                self.assertIn(b"\r\n", data)
                self.assertEqual(data.count(b"\n"), data.count(b"\r\n"))

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
            self.assertEqual(("warn", ".hexalith/release-evidence/**", "true", "30"),
                             (step["if-no-files-found"], step["path"], step["include-hidden-files"], step["retention-days"]))
            self.assertEqual("pass", check(document, f"builds-upload-covers-hidden-evidence-root:line-{step['line']}")["result"])
        self.assertEqual("pass", check(document, "no-stray-builds-output-outside-the-expected-evidence")["result"])
        manifest = document["observations"]["callerHashManifest"]
        self.assertEqual(4 + 5 * len(Q.MEMBER_FILES), len(manifest))
        self.assertEqual(sorted(entry["path"] for entry in manifest), [entry["path"] for entry in manifest])
        rendered = "".join(f"{entry['sha256']}  {entry['path']}\n" for entry in manifest)
        self.assertEqual(sha256(rendered.encode("utf-8")), document["observations"]["callerHashManifestSha256"])
        for case in ("missing-file", "tampered-file", "unlisted-file", "empty-evidence"):
            self.assertEqual("pass", check(document, f"caller-upload-refuses-{case}")["result"], case)

    def test_exceptions_and_findings_are_named_and_handed_off(self):
        exceptions = [item for document in self.results.values() for item in document["exceptions"]]
        self.assertEqual(APPROVED_EXCEPTIONS, {item["code"] for item in exceptions})
        self.assertEqual(len(APPROVED_EXCEPTIONS), len(exceptions))
        self.assertTrue(all(item["premiseObserved"] for item in exceptions))
        findings = {item["code"]: item for document in self.results.values() for item in document["findings"]}
        self.assertIn("30-3", findings["prepare-domain-release-cannot-publish-containers"]["owner"])
        self.assertIn("30-4", findings["prepare-domain-release-cannot-publish-containers"]["owner"])
        self.assertIn("recovery-workflow-superseded-set", findings)
        self.assertEqual("30-5-partial-release-recovery", findings["recovery-workflow-superseded-set"]["owner"])
        self.assertNotIn("package-manifest-id-shape", findings)
        self.assertEqual("30-4-five-image-publication-contract", findings["builds-identity-omits-project-mapping"]["owner"])
        provenance = findings["provenance-labels-not-emitted"]
        self.assertEqual("30-4-five-image-publication-contract", provenance["owner"])
        self.assertIn("org.opencontainers.image.revision", provenance["evidence"]["missingLabels"])
        retention = findings["evidence-retention-limited"]
        self.assertEqual("30-4-five-image-publication-contract; 30-5-partial-release-recovery", retention["owner"])
        self.assertEqual(["30", "30"], [step["retentionDays"] for step in retention["evidence"]["steps"]])

    def test_occupied_or_ambiguous_nuget_destinations_stop_before_any_write(self):
        document = self.results["phase-verification"]
        observed = document["observations"]["nugetDestinations"]
        self.assertEqual({
            "package-already-published": ("version-not-newer", "version-not-newer", "preflight-sequence-invalid"),
            "package-probe-503": ("destination-probe-failure", "destination-probe-failure", "preflight-sequence-invalid"),
        }, {case: (item["publish"], item["freshVerify"], item["publisher"]) for case, item in observed.items()})
        for case in observed:
            self.assertEqual("pass", check(document, f"nuget-{case}:refused-before-any-write")["result"], case)


class CommittedEvidenceTests(unittest.TestCase):
    """The committed capture stays bound to its original source and Builds revisions."""

    @classmethod
    def setUpClass(cls):
        cls.results = {checkpoint: read_result(COMMITTED_EVIDENCE, checkpoint) for checkpoint in CHECKPOINTS}
        cls.approval = (COMMITTED_EVIDENCE / "approval.md").read_text(encoding="utf-8")

    def assert_successful_documents(self, results):
        self.assertEqual(set(CHECKPOINTS), set(results))
        for checkpoint, document in results.items():
            self.assertEqual(Q.SCHEMA_VERSION, document["schemaVersion"], checkpoint)
            self.assertEqual(checkpoint, document["checkpoint"])
            self.assertIs(document["synthetic"], True, "These results qualify offline contracts only.")
            for field in ("result", "checksResult", "overallResult"):
                self.assertEqual("pass", document[field], f"{checkpoint}: {field}")
            self.assertEqual([], document["reasons"], checkpoint)
            self.assertTrue(document["checks"], checkpoint)
            for entry in document["checks"]:
                self.assertEqual("pass", entry["result"], f"{checkpoint}: {entry['name']}")
            self.assertIs(document["bindings"]["source"]["dirty"], False, checkpoint)

    def assert_archive_members(self, archive, receipt, directory, results):
        expected = {"dotnet-version.txt", "source-status.txt", "upstream-fixtures.log", "qualification.log",
                    "results/approval.md", *(f"results/{name}.json" for name in CHECKPOINTS)}
        names = archive.namelist()
        self.assertEqual(len(names), len(set(names)), "ZIP member names must be unique.")
        self.assertEqual(expected, set(names))
        self.assertEqual(expected, set(receipt["members"]))
        for name in names:
            data = archive.read(name)
            self.assertEqual({"bytes": len(data), "sha256": sha256(data)}, receipt["members"][name], name)
            if name.startswith("results/"):
                self.assertEqual(data, (directory / Path(name).name).read_bytes(), name)
        self.assertEqual(b"", archive.read("source-status.txt"))
        version = archive.read("dotnet-version.txt").decode("utf-8").strip()
        self.assertRegex(version, r"^10\.0\.[0-9]+$")
        self.assertEqual(version, receipt["dotnetVersion"])
        sdk = json.loads(run_git(REPO_ROOT, "show", f"{SELECTED_SOURCE_SHA}:global.json"))["sdk"]
        self.assertEqual("latestFeature", sdk["rollForward"])
        self.assertEqual(sdk, receipt["sdkPolicy"])
        self.assert_upstream_suite(archive.read("upstream-fixtures.log").decode("utf-8"), receipt["upstreamTests"])
        self.assertEqual(166, receipt["upstreamTests"])
        self.assertEqual({name: len(document["checks"]) for name, document in results.items()}, receipt["checkpoints"])
        self.assertEqual(sum(receipt["checkpoints"].values()), receipt["checkpointAssertions"])

    def assert_upstream_suite(self, log, count):
        self.assertRegex(log, rf"(?m)^Ran {count} tests in [0-9.]+s\r?\n(?:\r?\n)*OK\r?$")

    def test_retained_upstream_suite_refuses_skipped_tests(self):
        with zipfile.ZipFile(COMMITTED_EVIDENCE / "capture.zip") as archive:
            log = archive.read("upstream-fixtures.log").decode("utf-8")
        self.assert_upstream_suite(log, 166)
        for skipped in (1, 166):
            changed = re.sub(r"(?m)^OK(?=\r?$)", f"OK (skipped={skipped})", log)
            self.assertNotEqual(log, changed)
            with self.subTest(skipped=skipped), self.assertRaises(AssertionError):
                self.assert_upstream_suite(changed, 166)

    def assert_capture(self, directory, results):
        receipt = Q.load_strict_json((directory / "artifact-receipt.json").read_bytes(),
                                     "receipt-invalid", "selected CI receipt")
        for key, value in {
            "repository": "Hexalith/Hexalith.Memories", "source": SELECTED_SOURCE_SHA,
            "sourceDirty": False, "builds": HISTORICAL_BUILDS_SHA, "run": 41304156187, "attempt": 1,
            "artifact": 13082461185, "job": 124841016777, "runConclusion": "success",
            "artifactName": "builds-publication-qualification-41304156187-1",
            "workflow": ".github/workflows/qualify-builds-publication.yml", "livePublication": False,
            "artifactSha256": SELECTED_ARTIFACT_SHA, "artifactBytes": SELECTED_ARTIFACT_BYTES,
        }.items():
            self.assertEqual(value, receipt[key], key)
        self.assertIs(receipt["sourceDirty"], False)
        binding = results["complete-set-dry-run"]["bindings"]
        self.assertEqual(binding, receipt["bindings"])
        self.assertEqual({"repository": receipt["repository"], "sha": receipt["source"], "dirty": False}, binding["source"])
        self.assertIs(binding["builds"]["verified"], True)
        for field in ("sha", "superprojectGitlink", "checkout"):
            self.assertEqual(HISTORICAL_BUILDS_SHA, binding["builds"][field])
        self.assertEqual({relative: sha256(historical_bytes(BUILDS, HISTORICAL_BUILDS_SHA, relative)) for relative in Q.REFERENCE_FILES},
                         binding["builds"]["readReferences"])
        self.assertEqual({"path": "tools/qualify-builds-publication.py", "sha256": sha256(historical_bytes(REPO_ROOT, SELECTED_SOURCE_SHA, "tools/qualify-builds-publication.py"))}, binding["harness"])
        self.assertEqual({"path": "tools/builds-publication-qualification.json", "sha256": sha256(historical_bytes(REPO_ROOT, SELECTED_SOURCE_SHA, "tools/builds-publication-qualification.json"))}, binding["profile"])
        self.assertEqual({name: sha256(historical_bytes(BUILDS, HISTORICAL_BUILDS_SHA, "Github/publish-containers/" + name)) for name in Q.EXECUTED_HELPERS},
                         binding["builds"]["executedHelpers"])
        data = (directory / "capture.zip").read_bytes()
        self.assertEqual(SELECTED_ARTIFACT_BYTES, len(data))
        self.assertEqual(SELECTED_ARTIFACT_SHA, sha256(data))
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            self.assert_archive_members(archive, receipt, directory, results)

    def test_all_adopted_checkpoints_require_successful_checks_and_clean_source(self):
        self.assert_successful_documents(self.results)

    def test_failed_incomplete_or_dirty_adopted_checkpoints_are_rejected(self):
        for checkpoint in CHECKPOINTS:
            mutations = [
                *(lambda d, field=field: d.__setitem__(field, "fail") for field in ("result", "checksResult", "overallResult")),
                lambda d: d.__setitem__("reasons", ["qualification-incomplete:recovery"]),
                lambda d: d.__setitem__("checks", []),
                lambda d: d["checks"][0].__setitem__("result", "fail"),
                *(lambda d, value=value: d["bindings"]["source"].__setitem__("dirty", value) for value in (True, None, "false", 0)),
            ]
            for index, mutate in enumerate(mutations):
                results = copy.deepcopy(self.results)
                mutate(results[checkpoint])
                with self.subTest(checkpoint=checkpoint, mutation=index), self.assertRaises(AssertionError):
                    self.assert_successful_documents(results)

    def test_selected_capture_and_receipt_bind_the_exact_retained_results(self):
        self.assert_capture(COMMITTED_EVIDENCE, self.results)

    def test_missing_corrupt_or_changed_carriers_are_rejected(self):
        for filename in ("capture.zip", "artifact-receipt.json"):
            for change in ("missing", "corrupt", "changed"):
                with self.subTest(filename=filename, change=change), tempfile.TemporaryDirectory() as temporary:
                    directory = Path(temporary) / "capture"
                    shutil.copytree(COMMITTED_EVIDENCE, directory)
                    target = directory / filename
                    if change == "missing":
                        target.unlink()
                    elif change == "corrupt":
                        target.write_bytes(b"{")
                    else:
                        if filename == "artifact-receipt.json":
                            receipt = json.loads(target.read_bytes())
                            receipt["source"] = "wrong"
                            target.write_text(json.dumps(receipt))
                        else:
                            target.write_bytes(target.read_bytes() + b" ")
                    with self.assertRaises((AssertionError, OSError, Q.QualificationError)):
                        self.assert_capture(directory, self.results)

    def test_receipt_source_helper_and_artifact_mismatches_are_rejected(self):
        mutations = [
            *(lambda r, key=key: r.__setitem__(key, "wrong") for key in
              ("source", "builds", "run", "attempt", "job", "artifact", "artifactName", "artifactSha256", "artifactBytes", "runConclusion", "workflow", "dotnetVersion")),
            lambda r: r.__setitem__("sourceDirty", True),
            lambda r: r["bindings"]["source"].__setitem__("dirty", True),
            lambda r: r["bindings"]["harness"].__setitem__("sha256", "0" * 64),
            lambda r: r["bindings"]["profile"].__setitem__("sha256", "0" * 64),
            lambda r: r["bindings"]["builds"]["executedHelpers"].__setitem__("publish-containers.sh", "0" * 64),
            lambda r: r["bindings"]["builds"]["readReferences"].__setitem__("Github/publish-containers/action.yml", "0" * 64),
            lambda r: r["members"].pop("qualification.log"),
            lambda r: r["members"]["qualification.log"].__setitem__("sha256", "0" * 64),
            lambda r: r["members"]["qualification.log"].__setitem__("bytes", 0),
        ]
        for index, mutate in enumerate(mutations):
            with self.subTest(mutation=index), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary) / "capture"
                shutil.copytree(COMMITTED_EVIDENCE, directory)
                receipt = json.loads((directory / "artifact-receipt.json").read_bytes())
                mutate(receipt)
                (directory / "artifact-receipt.json").write_text(json.dumps(receipt))
                with self.assertRaises(AssertionError):
                    self.assert_capture(directory, self.results)

    def test_archive_duplicates_inventory_and_committed_result_drift_are_rejected(self):
        receipt = json.loads((COMMITTED_EVIDENCE / "artifact-receipt.json").read_bytes())
        original = zipfile.ZipFile(COMMITTED_EVIDENCE / "capture.zip")
        self.addCleanup(original.close)
        for change in ("duplicate", "missing", "extra", "changed", "result-bytes"):
            with self.subTest(change=change), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary) / "capture"
                shutil.copytree(COMMITTED_EVIDENCE, directory)
                data = io.BytesIO()
                with zipfile.ZipFile(data, "w") as changed:
                    for name in original.namelist():
                        if change == "missing" and name == "qualification.log":
                            continue
                        content = original.read(name)
                        if change == "changed" and name == "qualification.log":
                            content += b"tampered"
                        changed.writestr(name, content)
                    if change == "extra":
                        changed.writestr("extra.txt", b"extra")
                    if change == "duplicate":
                        with self.assertWarns(UserWarning):
                            changed.writestr("qualification.log", b"duplicate")
                if change == "result-bytes":
                    (directory / "recovery.json").write_bytes(b"{}")
                with zipfile.ZipFile(data) as changed, self.assertRaises(AssertionError):
                    self.assert_archive_members(changed, receipt, directory, self.results)

    def test_the_seven_original_approved_files_remain_unchanged(self):
        directory = EVIDENCE_ROOT / "30-2-qualification"
        for name, digest in HISTORICAL_HASHES.items():
            path = directory / name
            with self.subTest(file=name):
                # .gitattributes materializes these text files as CRLF on every checkout.
                self.assertIn(": eol: crlf", run_git(REPO_ROOT, "check-attr", "eol", "--", str(path)))
                self.assertEqual(digest, sha256(path.read_bytes()))
        earlier_receipt = (EARLIER_EVIDENCE / "artifact-receipt.json").read_bytes()
        self.assertEqual("5ee1003b9545dea92ce942a2887565d3f13a349dee7e5ad72567a0c4f80d3f72", sha256(earlier_receipt))
        self.assertEqual(HISTORICAL_HASHES, json.loads(earlier_receipt)["historicalFilesPreserved"])
        earlier_capture = (EARLIER_EVIDENCE / "capture.zip").read_bytes()
        self.assertEqual("db2080f1cb9d3ef834210d685a984b8969808710cdea89ca10a065fd8eb7f081", sha256(earlier_capture))
        with zipfile.ZipFile(io.BytesIO(earlier_capture)) as archive:
            for name in HISTORICAL_HASHES:
                self.assertEqual(archive.read(f"results/{name}"), (EARLIER_EVIDENCE / name).read_bytes(), name)

    def test_all_six_bindings_blocks_are_identical(self):
        bindings = [document["bindings"] for document in self.results.values()]
        self.assertTrue(all(binding == bindings[0] for binding in bindings))

    def test_bound_harness_profile_and_helper_hashes_equal_the_archived_source_bytes(self):
        binding = self.results["complete-set-dry-run"]["bindings"]
        self.assertEqual({"path": "tools/qualify-builds-publication.py", "sha256": sha256(historical_bytes(REPO_ROOT, SELECTED_SOURCE_SHA, "tools/qualify-builds-publication.py"))},
                         binding["harness"], "The capture must bind its original harness bytes.")
        self.assertEqual({"path": "tools/builds-publication-qualification.json", "sha256": sha256(historical_bytes(REPO_ROOT, SELECTED_SOURCE_SHA, "tools/builds-publication-qualification.json"))},
                         binding["profile"], "The capture must bind its original profile bytes.")
        self.assertEqual(HISTORICAL_BUILDS_SHA, binding["builds"]["sha"])
        self.assertEqual(
            {name: sha256(historical_bytes(BUILDS, HISTORICAL_BUILDS_SHA, "Github/publish-containers/" + name)) for name in Q.EXECUTED_HELPERS},
            binding["builds"]["executedHelpers"],
        )
        for value in (binding["harness"]["sha256"], binding["profile"]["sha256"], *binding["builds"]["executedHelpers"].values()):
            self.assertIn(f"`{value}`", self.approval)

    def test_each_listed_evidence_hash_equals_its_committed_file(self):
        rows = {
            match.group("checkpoint"): match
            for match in re.finditer(
                r"^\| (?P<checkpoint>[a-z-]+) \| `(?P<file>[^`]+)` \| (?P<result>pass|fail) \| `(?P<sha>[0-9a-f]{64})` \|$",
                self.approval, re.MULTILINE)
        }
        self.assertEqual(set(CHECKPOINTS), set(rows))
        for checkpoint, row in rows.items():
            with self.subTest(checkpoint=checkpoint):
                self.assertEqual(f"{checkpoint}.json", row.group("file"))
                self.assertEqual(sha256((COMMITTED_EVIDENCE / row.group("file")).read_bytes()), row.group("sha"))
                self.assertEqual(self.results[checkpoint]["result"], row.group("result"))
        self.assertIn(self.approval_status(), ("pending", "approved"))

    def approval_status(self) -> str:
        statuses = re.findall(r"^Status: (\S+)$", self.approval, re.MULTILINE)
        self.assertEqual(1, len(statuses), "approval.md must carry exactly one Status line")
        return statuses[0]

    def test_committed_approval_is_untouched_pending_or_fully_decided(self):
        rows = {match.group("role"): match for match in re.finditer(
            r"^\| (?P<role>Builds Maintainer|Memories Release Engineer) \|(?P<owner>[^|]*)\|(?P<decision>[^|]*)\|"
            r"(?P<date>[^|]*)\|(?P<notes>[^|]*)\|$", self.approval, re.MULTILINE)}
        self.assertEqual({"Builds Maintainer", "Memories Release Engineer"}, set(rows))
        if self.approval_status() == "pending":
            self.assertIsNone(Q.approval_guard(COMMITTED_EVIDENCE))
            return
        # A decided record names an owner, an approved decision and a date for every role,
        # and the harness must refuse to overwrite it.
        for role, row in rows.items():
            with self.subTest(role=role):
                self.assertTrue(row.group("owner").strip())
                self.assertEqual("approved", row.group("decision").strip())
                self.assertRegex(row.group("date").strip(), r"^\d{4}-\d{2}-\d{2}$")
                self.assertTrue(row.group("notes").strip())
        self.assertIsNotNone(Q.approval_guard(COMMITTED_EVIDENCE))


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


class PinVerificationTests(unittest.TestCase):
    """Prove every pin, gitlink and helper-byte mismatch fails closed."""

    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="builds-pin-test-")
        self.root = Path(self.temporary.name) / "super"
        self.builds = self.root / "references" / "Hexalith.Builds"
        self.builds.mkdir(parents=True)
        run_git(self.builds, "init", "--quiet")
        for relative in [*(f"Github/publish-containers/{name}" for name in Q.EXECUTED_HELPERS), *Q.REFERENCE_FILES]:
            target = self.builds / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(BUILDS / relative, target)
        run_git(self.builds, "add", "-A")
        run_git(self.builds, "commit", "--quiet", "-m", "pinned")
        self.sha = run_git(self.builds, "rev-parse", "HEAD").strip()
        run_git(self.root, "init", "--quiet")
        run_git(self.root, "update-index", "--add", "--cacheinfo", f"160000,{self.sha},references/Hexalith.Builds")
        run_git(self.root, "commit", "--quiet", "-m", "superproject")

    def tearDown(self):
        self.temporary.cleanup()

    def assert_code(self, code, declared=None):
        with self.assertRaises(Q.QualificationError) as context:
            Q.verify_builds_pin(self.root, declared or self.sha)
        self.assertEqual(code, context.exception.code)

    def test_matching_pin_binds_the_executed_helper_and_reference_hashes(self):
        binding = Q.verify_builds_pin(self.root, self.sha)
        self.assertEqual((self.sha, self.sha, self.sha), (binding.sha, binding.gitlink, binding.checkout))
        for name in Q.EXECUTED_HELPERS:
            self.assertEqual(sha256((BUILDS / "Github/publish-containers" / name).read_bytes()), binding.helpers[name])
        self.assertEqual(set(Q.REFERENCE_FILES), set(binding.references))

    def test_wrong_declared_sha_fails(self):
        self.assert_code("builds-gitlink-mismatch", "0" * 40)

    def test_staged_gitlink_drift_fails(self):
        run_git(self.root, "update-index", "--cacheinfo", f"160000,{'1' * 40},references/Hexalith.Builds")
        self.assert_code("builds-gitlink-mismatch")

    def test_checkout_that_left_the_pin_fails(self):
        (self.builds / "README.md").write_text("moved\n")
        run_git(self.builds, "add", "README.md")
        run_git(self.builds, "commit", "--quiet", "-m", "moved")
        self.assert_code("builds-checkout-mismatch")

    def test_missing_checkout_fails(self):
        shutil.rmtree(self.builds / ".git")
        self.assert_code("builds-checkout-missing")

    def test_modified_or_missing_helper_bytes_fail(self):
        helper = self.builds / "Github/publish-containers/publication_preflight.py"
        original = helper.read_bytes()
        helper.write_bytes(original + b"\n")
        self.assert_code("builds-helper-hash-mismatch")
        helper.unlink()
        self.assert_code("builds-helper-missing")

    def test_modified_reference_bytes_fail(self):
        reference = self.builds / Q.DOMAIN_RELEASE_WORKFLOW
        reference.write_bytes(reference.read_bytes().replace(b"if-no-files-found: warn", b"if-no-files-found: error"))
        self.assert_code("builds-reference-hash-mismatch")

    def test_missing_reference_fails(self):
        (self.builds / Q.PREPARE_DOMAIN_RELEASE_ACTION).unlink()
        self.assert_code("builds-reference-missing")

    def test_missing_release_manifest_is_a_prerequisite_failure(self):
        profile = Path(self.temporary.name) / "profile.json"
        document = json.loads(PROFILE.read_bytes())
        document["buildsSha"] = self.sha
        profile.write_text(json.dumps(document))
        context = Q.prepare_context(self.root, profile, TOOL)
        self.assertIsNotNone(context.builds)
        self.assertIn("release-manifest-missing: tools/release-packages.json is unreadable.", context.prerequisite_failures)
        self.assertFalse(context.ready)

    def test_missing_gitlink_fails(self):
        run_git(self.root, "rm", "--cached", "--quiet", "references/Hexalith.Builds")
        run_git(self.root, "commit", "--quiet", "--allow-empty", "-m", "dropped")
        self.assert_code("builds-gitlink-missing")


class FailClosedRunTests(unittest.TestCase):
    """Bad pins, profiles and decided approvals stop the harness without a partial pass."""

    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="builds-qualification-fail-")
        self.directory = Path(self.temporary.name)

    def tearDown(self):
        self.temporary.cleanup()

    def assert_all_fail(self, output: Path, reason: str):
        for checkpoint in CHECKPOINTS:
            document = read_result(output, checkpoint)
            self.assertEqual(("fail", "fail"), (document["result"], document["overallResult"]), checkpoint)
            self.assertTrue(any(reason in item for item in document["reasons"]), document["reasons"])
        approval = (output / "approval.md").read_text(encoding="utf-8")
        self.assertIn("Status: pending", approval)
        self.assertIn("Qualification result: fail", approval)

    def test_wrong_builds_pin_fails_every_result_and_exits_non_zero(self):
        profile = self.directory / "profile.json"
        document = json.loads(PROFILE.read_bytes())
        document["buildsSha"] = "0" * 40
        profile.write_text(json.dumps(document))
        output = self.directory / "evidence"
        code, _, stderr = run_main("--output-directory", str(output), "--profile", str(profile))
        self.assertEqual(1, code)
        self.assertIn("builds-gitlink-mismatch", stderr)
        self.assert_all_fail(output, "builds-gitlink-mismatch")
        binding = read_result(output, "recovery")["bindings"]["profile"]
        self.assertEqual({"path": str(profile.resolve()), "sha256": sha256(profile.read_bytes())}, binding)
        self.assertIn(f"| Profile SHA-256 (`{profile.resolve()}`) |", (output / "approval.md").read_text(encoding="utf-8"))

    def test_profile_binding_is_repository_relative_under_the_root(self):
        self.assertEqual("tools/builds-publication-qualification.json", Q.profile_label(REPO_ROOT, PROFILE))
        self.assertEqual("custom/profile.json", Q.profile_label(REPO_ROOT, REPO_ROOT / "custom" / "profile.json"))
        self.assertEqual(str(self.directory.resolve() / "p.json"), Q.profile_label(REPO_ROOT, self.directory / "p.json"))

    def test_missing_or_drifted_profile_fails_every_result(self):
        for name, content, reason in (
            ("missing", None, "profile-missing"),
            ("drifted", lambda document: document["containerProjects"].pop(), "repository-set-drift"),
            ("miscounted", lambda document: document.__setitem__("expectedPackageCount", 9), "package-count-mismatch:10!=9"),
        ):
            with self.subTest(case=name):
                profile = self.directory / f"{name}.json"
                if content is not None:
                    document = json.loads(PROFILE.read_bytes())
                    content(document)
                    profile.write_text(json.dumps(document))
                output = self.directory / f"evidence-{name}"
                code, _, _ = run_main("--output-directory", str(output), "--profile", str(profile))
                self.assertEqual(1, code)
                self.assert_all_fail(output, reason)

    def test_failed_rerun_overwrites_stale_passing_evidence(self):
        output = self.directory / "evidence"
        output.mkdir()
        for checkpoint in CHECKPOINTS:
            (output / f"{checkpoint}.json").write_text(json.dumps({"result": "pass"}))
        code, _, _ = run_main("--output-directory", str(output), "--profile", str(self.directory / "absent.json"))
        self.assertEqual(1, code)
        self.assert_all_fail(output, "profile-missing")

    def test_decided_approval_is_never_overwritten(self):
        builds_row = "| Builds Maintainer | | pending | | |"
        release_row = "| Memories Release Engineer | | pending | | |"
        cases = {
            "approve": (builds_row, "| Builds Maintainer | | approve | | |"),
            "declined": (builds_row, "| Builds Maintainer | | declined | | |"),
            "approved-with-conditions": (builds_row, "| Builds Maintainer | | Approved with conditions | | |"),
            "rejected": (release_row, "| Memories Release Engineer | | rejected | | |"),
            "accepted": (release_row, "| Memories Release Engineer | | accepted | | |"),
            "approved": (builds_row, "| Builds Maintainer | jpiquot | approved | 2026-11-01 | |"),
            "owner-filled": (builds_row, "| Builds Maintainer | jpiquot | pending | | |"),
            "date-filled": (release_row, "| Memories Release Engineer | | pending | 2026-11-01 | |"),
            "row-removed": (release_row + "\n", ""),
            "row-duplicated": (release_row, release_row + "\n" + release_row),
            "status-approved": ("Status: pending", "Status: approved"),
            "second-status": ("Status: pending", "Status: pending\nStatus: approved"),
            "status-removed": ("Status: pending\n", ""),
            "heading-removed": ("## Owner decisions\n", ""),
        }
        for name, (old, new) in cases.items():
            with self.subTest(case=name):
                self.assertIn(old, PENDING_APPROVAL)
                text = PENDING_APPROVAL.replace(old, new, 1)
                output = self.directory / name
                output.mkdir()
                (output / "approval.md").write_text(text)
                code, _, stderr = run_main("--output-directory", str(output))
                self.assertEqual(1, code)
                self.assertIn("approval-record-not-pending", stderr)
                self.assertEqual(["approval.md"], [path.name for path in output.iterdir()])
                self.assertEqual(text, (output / "approval.md").read_text())
        for name, text in (("pending", PENDING_APPROVAL), ("pending-crlf", PENDING_APPROVAL.replace("\n", "\r\n"))):
            with self.subTest(case=name):
                pending = self.directory / name
                pending.mkdir()
                (pending / "approval.md").write_bytes(text.encode("utf-8"))
                self.assertIsNone(Q.approval_guard(pending))

    def test_a_rerun_may_replace_its_own_pending_record(self):
        output = self.directory / "evidence"
        self.assertEqual(1, run_main("--output-directory", str(output), "--profile", str(self.directory / "absent.json"))[0])
        self.assertIsNone(Q.approval_guard(output))


class DocumentTests(unittest.TestCase):
    def context(self):
        context = Q.Context(REPO_ROOT, "a" * 64, "b" * 64, profile=Q.load_profile(PROFILE.read_bytes()),
                            source={"repository": Q.RELEASE_REPOSITORY, "sha": "c" * 40, "dirty": False})
        context.package_ids = ("A",)
        return context

    def passing(self):
        results = {}
        for checkpoint in CHECKPOINTS:
            result = Q.CheckpointResult(checkpoint)
            result.check("ran", True)
            results[checkpoint] = result
        return results

    def test_every_checkpoint_passes_only_when_all_pass(self):
        overall, documents = Q.build_documents(self.context(), self.passing())
        self.assertTrue(overall)
        self.assertTrue(all(document["result"] == "pass" for document in documents.values()))

    def test_one_failing_checkpoint_fails_every_result(self):
        results = self.passing()
        results["recovery"].check("missing-set-is-exact", False)
        overall, documents = Q.build_documents(self.context(), results)
        self.assertFalse(overall)
        for checkpoint, document in documents.items():
            with self.subTest(checkpoint=checkpoint):
                self.assertEqual("fail", document["result"])
                expected = "check-failed:missing-set-is-exact" if checkpoint == "recovery" else "qualification-incomplete:recovery"
                self.assertIn(expected, document["reasons"])
        self.assertEqual("pass", documents["set-drift"]["checksResult"])

    def test_a_checkpoint_without_checks_never_passes(self):
        results = self.passing()
        results["evidence-upload"] = Q.CheckpointResult("evidence-upload")
        self.assertFalse(Q.build_documents(self.context(), results)[0])

    def test_an_unexpected_checkpoint_error_fails_closed(self):
        def explode(*_arguments):
            raise RuntimeError("boom")

        with mock.patch.object(Q, "GROUPS", (("recovery", explode),)):
            results = Q.run_groups(self.context(), Path(tempfile.gettempdir()))
        self.assertFalse(results["recovery"].passed)
        self.assertEqual("checkpoint-completed", results["recovery"].checks[0]["name"])

    def test_unrecorded_or_unexpected_refusals_fail(self):
        for outcome, code in (
            (Q.Outcome(0, "", ""), "preflight-phase-collision"),
            (Q.Outcome(1, "", "[publication-preflight] version-collision: taken"), "preflight-phase-collision"),
            (Q.Outcome(2, "", "error: argument --phase: invalid choice"), "preflight-phase-collision"),
        ):
            result = Q.CheckpointResult("phase-verification")
            Q._expect_refusal(result, "replay", outcome, code)
            self.assertFalse(result.passed)
        result = Q.CheckpointResult("phase-verification")
        Q._expect_refusal(result, "replay", Q.Outcome(1, "", "[publication-preflight] preflight-phase-collision: x"),
                          "preflight-phase-collision")
        self.assertTrue(result.passed)

    def test_self_check_detects_changed_or_divergent_evidence(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory)
            _, documents = Q.build_documents(self.context(), self.passing())
            for checkpoint, document in documents.items():
                Q.write_crlf(output / f"{checkpoint}.json", json.dumps(document))
            self.assertEqual([], Q.verify_written(output, documents))
            (output / "recovery.json").write_text("{}")
            self.assertIn("evidence-unreadable-or-changed:recovery", Q.verify_written(output, documents))
            (output / "recovery.json").unlink()
            self.assertIn("evidence-unreadable-or-changed:recovery", Q.verify_written(output, documents))
            documents["set-drift"]["bindings"] = {"builds": "other"}
            Q.write_crlf(output / "set-drift.json", json.dumps(documents["set-drift"]))
            self.assertIn("evidence-bindings-differ", Q.verify_written(output, {key: documents[key] for key in ("set-drift", "phase-verification")}))

    def test_a_failed_self_check_forces_every_result_to_fail(self):
        with tempfile.TemporaryDirectory() as directory, \
                mock.patch.object(Q, "verify_written", return_value=["evidence-unreadable-or-changed:recovery"]):
            output = Path(directory)
            overall, _, documents = Q.write_outputs(output, self.context(), self.passing())
            self.assertFalse(overall)
            for checkpoint in CHECKPOINTS:
                with self.subTest(checkpoint=checkpoint):
                    written = read_result(output, checkpoint)
                    self.assertEqual(written, documents[checkpoint])
                    self.assertEqual(("fail", "pass"), (written["result"], written["checksResult"]))
                    self.assertIn("evidence-self-check-failed:evidence-unreadable-or-changed:recovery", written["reasons"])
            self.assertIn("Qualification result: fail", (output / "approval.md").read_text(encoding="utf-8"))

    def test_summary_keeps_the_refusal_line_behind_long_stderr(self):
        noise = "\n".join(f"diagnostic line {index} " + "x" * 40 for index in range(40))
        outcome = Q.Outcome(2, "", f"{noise}\nerror: argument --phase: invalid choice: 'recover'\n")
        summary = outcome.summary()
        self.assertNotIn("invalid choice", summary["stderr"])
        self.assertTrue(summary["stderrTail"].endswith("error: argument --phase: invalid choice: 'recover'"))
        refused = Q.Outcome(1, "", f"{noise}\n[publication-preflight] version-collision: A proposed tag exists.\n")
        self.assertEqual("[publication-preflight] version-collision: A proposed tag exists.", refused.summary()["refusalLine"])
        self.assertEqual("version-collision", refused.summary()["refusal"])


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


class SeamTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="builds-seams-")
        self.world = Q.World(Path(self.temporary.name))
        self.world.write_config({"releaseDirectory": self.temporary.name, "helperHashes": {}, "publishFailures": {},
                                 "probeOverrides": {}, "nugetExisting": []})
        self.project = Path(self.temporary.name) / "A.csproj"
        self.project.write_text("<Project />")

    def tearDown(self):
        self.temporary.cleanup()

    def arguments(self, **overrides):
        properties = {
            "RuntimeIdentifiers": Q.RUNTIME_IDENTIFIERS,
            "ContainerRuntimeIdentifiers": Q.RUNTIME_IDENTIFIERS,
            "ContainerImageFormat": "OCI",
            "UseHexalithProjectReferences": "false",
            "ContainerRegistry": Q.REGISTRY,
            "ContainerRepository": "memories",
            "ContainerImageTag": Q.QUALIFICATION_VERSION,
            "Version": Q.QUALIFICATION_VERSION,
            "ContainerProvenanceSourceSha": "a" * 40,
            "ContainerProvenanceReleaseVersion": Q.QUALIFICATION_VERSION,
            "ContainerProvenanceCreated": "2026-11-01T00:00:00Z",
        }
        properties.update(overrides)
        return ["publish", str(self.project), "--configuration", "Release", "/t:PublishContainer",
                *(f"-p:{name}={value}" for name, value in properties.items() if value is not None)]

    def test_fake_dotnet_accepts_only_the_pinned_publication_contract(self):
        self.assertEqual("memories", Q._parse_dotnet_publish(self.arguments())["repository"])
        cases = {
            "single-rid": self.arguments(RuntimeIdentifiers="linux-musl-x64"),
            "missing-created": self.arguments(ContainerProvenanceCreated=None),
            "tag-mismatch": self.arguments(ContainerImageTag="0.0.1"),
            "other-registry": self.arguments(ContainerRegistry="docker.io"),
            "project-references": self.arguments(UseHexalithProjectReferences="true"),
            "extra-property": [*self.arguments(), "-p:Extra=1"],
            "extra-argument": [*self.arguments(), "--no-build"],
            "debug": [value if value != "Release" else "Debug" for value in self.arguments()],
            "not-publish": ["build", str(self.project)],
            "missing-project": ["publish", str(self.project) + ".missing", *self.arguments()[2:]],
        }
        for name, arguments in cases.items():
            with self.subTest(case=name), self.assertRaises(Q.QualificationError):
                Q._parse_dotnet_publish(arguments)

    def test_fake_registry_serves_only_authenticated_qualification_requests(self):
        opener = Q._fake_registry_open(self.world)
        request = urllib.request.Request(f"https://{Q.REGISTRY}/v2/memories/manifests/{Q.QUALIFICATION_VERSION}",
                                         headers={"Authorization": "Basic eDp5"})
        with self.assertRaises(urllib.error.HTTPError) as missing:
            opener(request)
        self.assertEqual(404, missing.exception.code)
        missing.exception.close()
        digest = self.world.put_object("memories", Q.OCI_INDEX_MEDIA_TYPE, b"{}")
        self.world.put_tag("memories", Q.QUALIFICATION_VERSION, digest)
        with opener(request) as response:
            self.assertEqual((200, b"{}", digest), (response.status, response.read(), response.headers.get("Docker-Content-Digest")))
        with self.assertRaises(urllib.error.HTTPError) as unauthenticated:
            opener(urllib.request.Request(request.full_url))
        self.assertEqual(401, unauthenticated.exception.code)
        unauthenticated.exception.close()
        with self.assertRaises(urllib.error.URLError):
            opener(urllib.request.Request("https://registry.example.test/v2/memories/manifests/1.0.0",
                                          headers={"Authorization": "Basic eDp5"}))

    def test_disabled_network_and_unknown_seams_fail_closed(self):
        with self.assertRaises(urllib.error.URLError):
            Q._network_disabled(urllib.request.Request("https://api.github.com"))
        stderr = io.StringIO()
        with redirect_stderr(stderr):
            self.assertEqual(97, Q.run_seam("bogus", []))
        with mock.patch.dict("os.environ", {Q.WORLD_VARIABLE: ""}), redirect_stderr(io.StringIO()):
            self.assertEqual(96, Q.run_seam("docker", ["version"]))

    def test_fake_probe_reports_occupied_absent_and_overridden_destinations(self):
        class Module:
            class PreflightError(Exception):
                def __init__(self, code, message):
                    super().__init__(message)
                    self.code = code

        probe = Q._fake_probe_factory(self.world, Module, {f"container:{Q.REGISTRY}/memories-mcp": 503})("user", "key")
        self.assertEqual(404, probe("container", f"{Q.REGISTRY}/memories", Q.QUALIFICATION_VERSION))
        self.assertEqual(404, probe("nuget", "Hexalith.Memories.Contracts", Q.QUALIFICATION_VERSION))
        self.assertEqual(503, probe("container", f"{Q.REGISTRY}/memories-mcp", Q.QUALIFICATION_VERSION))
        self.assertEqual(502, probe("container", "registry.example.test/memories", Q.QUALIFICATION_VERSION))
        self.world.put_tag("memories", Q.QUALIFICATION_VERSION, "sha256:" + "0" * 64)
        self.assertEqual(200, probe("container", f"{Q.REGISTRY}/memories", Q.QUALIFICATION_VERSION))
        with self.assertRaises(Module.PreflightError):
            Q._fake_probe_factory(self.world, Module)("", "")("container", f"{Q.REGISTRY}/memories", Q.QUALIFICATION_VERSION)


if __name__ == "__main__":
    unittest.main()
