"""Offline guards for intentional release dispatch and shared source proofs."""

import importlib.util
import io
import os
import re
import sys
import tempfile
import unittest
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path
from types import SimpleNamespace
from unittest import mock


ROOT = Path(__file__).resolve().parents[3]
SOURCE_SCRIPT = ROOT / "tools/check-release-source.py"
DESTINATION_SCRIPT = ROOT / "tools/check-release-destinations.py"


def load_script(path: Path, name: str):
    specification = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


class ReleaseDispatchTests(unittest.TestCase):
    def setUp(self):
        self.source = load_script(SOURCE_SCRIPT, "release_source_test")
        self.destination = load_script(DESTINATION_SCRIPT, "release_destination_test")

    def test_workflow_dispatch_proves_source_before_production_job(self):
        workflow = (ROOT / ".github/workflows/release.yml").read_text(encoding="utf-8")
        self.assertIn("workflow_dispatch:", workflow)
        self.assertNotIn("  push:\n", workflow)
        self.assertLess(workflow.index("  release-source:"), workflow.index("  release:\n"))
        self.assertLess(workflow.index("Require production environment approval rule"), workflow.index("  release:\n"))
        self.assertIn("needs: release-source", workflow)
        self.assertIn("environment: production", workflow)
        self.assertIn("Require production environment approval rule", workflow)
        self.assertIn("Recheck production approval rule before publication", workflow)
        self.assertLess(workflow.index("  release:\n"), workflow.index("Recheck production approval rule before publication"))
        self.assertLess(workflow.index("Recheck production approval rule before publication"), workflow.index("NuGet/login@v1"))
        self.assertIn("prepare-domain-release@" + self.source.APPROVED_BUILDS_SHA, workflow)
        self.assertIn("builds-execution-sha: " + self.source.APPROVED_BUILDS_SHA, workflow)
        self.assertIn("expected-package-count: '10'", workflow)
        self.assertIn("NuGet/login@v1", workflow)
        self.assertNotIn("./tools/test-release.ps1", workflow)

    def test_non_main_ref_is_rejected_without_loading_shared_helper(self):
        sha = "a" * 40
        environ = {"GITHUB_EVENT_NAME": "workflow_dispatch", "GITHUB_REF": "refs/heads/other", "GITHUB_SHA": sha}
        with mock.patch.dict(os.environ, environ), mock.patch.object(self.source, "checked_git_sha") as git_sha:
            with self.assertRaisesRegex(ValueError, "refs/heads/main"):
                self.source.prove_source("Hexalith/Hexalith.Memories", sha, Path("unused"), "token")
            git_sha.assert_not_called()

    def test_non_tip_or_missing_exact_ci_is_rejected_by_pinned_proof(self):
        sha = "a" * 40
        with tempfile.TemporaryDirectory() as temporary:
            shared = Path(temporary) / "builds-execution/Github/publish-containers"
            shared.mkdir(parents=True)
            (shared / "oci_registry_validator.py").write_text("", encoding="utf-8")
            (shared / "publication_preflight.py").write_text(
                "def prove_current_green_source(repository, sha, branch, workflow, token):\n"
                "    raise ValueError('source-no-longer-current' if token == 'stale' else 'source-ci-not-successful')\n",
                encoding="utf-8",
            )
            environ = {"GITHUB_EVENT_NAME": "workflow_dispatch", "GITHUB_REF": "refs/heads/main", "GITHUB_SHA": sha}
            with mock.patch.dict(os.environ, environ), mock.patch.object(
                self.source, "checked_git_sha", side_effect=[sha, self.source.APPROVED_BUILDS_SHA] * 2
            ):
                for token, expected in (("stale", "source-no-longer-current"), ("missing", "source-ci-not-successful")):
                    with self.subTest(token=token), self.assertRaisesRegex(ValueError, expected):
                        self.source.prove_source("Hexalith/Hexalith.Memories", sha, shared, token)

    def test_wrong_builds_checkout_is_rejected_before_loading_shared_helper(self):
        sha = "a" * 40
        shared = Path("unused/Github/publish-containers")
        environ = {"GITHUB_EVENT_NAME": "workflow_dispatch", "GITHUB_REF": "refs/heads/main", "GITHUB_SHA": sha}
        with mock.patch.dict(os.environ, environ), mock.patch.object(
            self.source, "checked_git_sha", side_effect=[sha, "c" * 40]
        ):
            with self.assertRaisesRegex(ValueError, "approved Builds revision"):
                self.source.prove_source("Hexalith/Hexalith.Memories", sha, shared, "token")

    def test_valid_exact_source_proof_reaches_protected_job(self):
        sha = "b" * 40
        with tempfile.TemporaryDirectory() as temporary:
            shared = Path(temporary) / "builds-execution/Github/publish-containers"
            shared.mkdir(parents=True)
            (shared / "oci_registry_validator.py").write_text("", encoding="utf-8")
            (shared / "publication_preflight.py").write_text(
                "def prove_current_green_source(repository, sha, branch, workflow, token):\n"
                "    if workflow != 'ci.yml':\n"
                "        raise ValueError('unexpected source CI workflow')\n"
                "    return {'live_sha': sha, 'ci_workflow': workflow, 'ci_run': {'head_sha': sha, 'id': 42}}\n",
                encoding="utf-8",
            )
            environ = {"GITHUB_EVENT_NAME": "workflow_dispatch", "GITHUB_REF": "refs/heads/main", "GITHUB_SHA": sha}
            with mock.patch.dict(os.environ, environ), mock.patch.object(
                self.source, "checked_git_sha", side_effect=[sha, self.source.APPROVED_BUILDS_SHA]
            ):
                proof = self.source.prove_source("Hexalith/Hexalith.Memories", sha, shared, "token")
            self.assertEqual(sha, proof["live_sha"])
            self.assertEqual("ci.yml", proof["ci_workflow"])
            self.assertEqual(42, proof["ci_run"]["id"])

    def test_destination_wrapper_uses_pinned_helper_and_ten_package_count(self):
        self.assertEqual(10, self.destination.EXPECTED_PACKAGE_COUNT)
        self.assertEqual(5, len(self.destination.IMAGE_REPOSITORIES))
        self.assertEqual(self.source.APPROVED_BUILDS_SHA, self.destination.APPROVED_BUILDS_SHA)
        preflight = (ROOT / "tools/release-preflight.ps1").read_text(encoding="utf-8")
        publisher = (ROOT / "tools/publish-nuget.ps1").read_text(encoding="utf-8")
        aggregate = (ROOT / "tools/publish-release.ps1").read_text(encoding="utf-8")
        self.assertIn("check-release-destinations.py", preflight)
        self.assertIn("check-release-destinations.py", publisher)
        self.assertLess(publisher.index("check-release-destinations.py"), publisher.index("dotnet nuget push"))
        self.assertLess(aggregate.index("check-release-source.py"), aggregate.index("'./tools/publish-nuget.ps1'"))

    def test_destination_wrapper_probes_exact_publisher_set_and_refuses_uncertain_results(self):
        package_ids = [f"Package.{index}" for index in range(10)]
        expected_repositories = [
            "registry.test/memories",
            "registry.test/memories-mcp",
            "registry.test/memories-access-telemetry",
            "registry.test/memories-access-telemetry-clock",
            "registry.test/memories-domain-service",
        ]
        publisher = (ROOT / "tools/publish-containers.ps1").read_text(encoding="utf-8")
        declared = re.findall(r"^\s+repository = (.+)$", publisher, re.MULTILINE)
        self.assertEqual(
            ["$RepositoryPrefix", '"$RepositoryPrefix-mcp"', '"$RepositoryPrefix-access-telemetry"',
             '"$RepositoryPrefix-access-telemetry-clock"', '"$RepositoryPrefix-domain-service"'],
            declared,
        )
        for destination_status, expected_exit in ((404, 0), (200, 1), (503, 1)):
            with self.subTest(destination_status=destination_status):
                captured = {}

                def load_identity(manifest, count):
                    captured["manifest"] = manifest
                    captured["count"] = count
                    return {"ids": package_ids}

                def destination_probe(username, api_key):
                    captured["credentials"] = (username, api_key)
                    return lambda kind, identity, version: (
                        destination_status if kind == "container" and identity == expected_repositories[-1] else 404
                    )

                def validate(ids, version, repositories, probe, count):
                    captured["validation"] = (ids, version, repositories, count)
                    for package_id in ids:
                        if probe("nuget", package_id, version) != 404:
                            raise ValueError("NuGet destination refusal")
                    for repository in repositories:
                        status = probe("container", repository, version)
                        if status != 404:
                            raise ValueError("occupied destination" if status == 200 else "ambiguous destination")

                helper = SimpleNamespace(
                    _load_package_identity=load_identity,
                    destination_probe=destination_probe,
                    validate_destination_absence=validate,
                )
                environ = {
                    "HEXALITH_ZOT_REGISTRY": "registry.test",
                    "HEXALITH_ZOT_USERNAME": "test-user",
                    "HEXALITH_ZOT_API_KEY": "test-key",
                }
                with mock.patch.dict(os.environ, environ), mock.patch.object(
                    self.destination, "load_pinned_helper", return_value=helper
                ), mock.patch.object(sys, "argv", ["check-release-destinations.py", "--version", "1.2.3"]), redirect_stdout(
                    io.StringIO()
                ), redirect_stderr(io.StringIO()):
                    exit_code = self.destination.main()
                self.assertEqual(expected_exit, exit_code)
                self.assertEqual(Path("tools/release-packages.json"), captured["manifest"])
                self.assertEqual(10, captured["count"])
                self.assertEqual(("test-user", "test-key"), captured["credentials"])
                self.assertEqual((package_ids, "1.2.3", expected_repositories, 10), captured["validation"])


if __name__ == "__main__":
    unittest.main()
