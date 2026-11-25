"""Offline guards for intentional release dispatch and shared source proofs."""

import hashlib
import importlib.util
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import textwrap
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


def write_controlled_pinned_helper(directory: Path) -> Path:
    shared = directory / ".hexalith/builds-execution/Github/publish-containers"
    shared.mkdir(parents=True)
    (shared / "oci_registry_validator.py").write_text("", encoding="utf-8")
    (shared / "publication_preflight.py").write_text(
        textwrap.dedent("""\
            import json
            import os
            from pathlib import Path

            def _load_package_identity(manifest, count):
                return {"ids": [entry["id"] for entry in json.loads(manifest.read_text())["packages"]]}

            def destination_probe(username, api_key):
                return lambda *arguments: int(os.environ.get("CONTROLLED_DESTINATION_STATUS", "404"))

            def validate_destination_absence(ids, version, repositories, probe, count):
                if probe("nuget", ids[0], version) != 404:
                    raise ValueError("occupied controlled destination")
                if os.environ.get("CONTROLLED_SOURCE_STATE"):
                    Path(os.environ["CONTROLLED_SOURCE_STATE"]).touch()

            def prove_current_green_source(repository, sha, branch, workflow, token):
                if Path(os.environ["CONTROLLED_SOURCE_STATE"]).exists():
                    raise ValueError("source-no-longer-current")
                return {"live_sha": sha, "ci_run": {"head_sha": sha, "id": 42}}
            """),
        encoding="utf-8",
    )
    return shared


class ReleaseDispatchTests(unittest.TestCase):
    def setUp(self):
        self.source = load_script(SOURCE_SCRIPT, "release_source_test")
        self.destination = load_script(DESTINATION_SCRIPT, "release_destination_test")


    def test_release_pin_is_approved_and_profile_tracks_the_gitlink(self):
        approved = "62876129eaa035e78bd0324419231f07d43a07b8"
        self.assertEqual(approved, self.source.APPROVED_BUILDS_SHA)
        self.assertEqual(approved, self.destination.APPROVED_BUILDS_SHA)
        profile = json.loads((ROOT / "tools/builds-publication-qualification.json").read_text(encoding="utf-8"))
        gitlink = subprocess.run(
            ["git", "rev-parse", "HEAD:references/Hexalith.Builds"], cwd=ROOT,
            check=True, capture_output=True, text=True,
        ).stdout.strip()
        self.assertEqual(gitlink, profile["buildsSha"])
        self.assertEqual(gitlink, approved)

    def test_five_image_release_retains_diagnostic_and_durable_success_evidence(self):
        publisher = (ROOT / "tools/publish-release.ps1").read_text(encoding="utf-8")
        self.assertLess(publisher.index("--phase authorize"), publisher.index("-Script './tools/publish-nuget.ps1'"))
        self.assertIn("-PublicationContract", publisher)
        self.assertIn("archive-release-evidence.py", publisher)

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

    def test_wrappers_exit_nonzero_as_processes_when_refusing(self):
        # Every gate reads only the wrapper's process exit status, so prove it for both scripts.
        environ = {key: value for key, value in os.environ.items() if not key.startswith("GITHUB_")}
        environ.update(
            {
                "GITHUB_REPOSITORY": "Hexalith/Hexalith.Memories",
                "GITHUB_EVENT_NAME": "workflow_dispatch",
                "GITHUB_REF": "refs/heads/other",
                "GITHUB_SHA": "a" * 40,
            }
        )
        cases = (
            ([str(SOURCE_SCRIPT)], "[release-source] refused: Release dispatch must select refs/heads/main."),
            (
                [str(DESTINATION_SCRIPT), "--version", "invalid"],
                "[release-destinations] refused: Release version must be an image-taggable semantic version.",
            ),
        )
        with tempfile.TemporaryDirectory() as temporary:
            for arguments, refusal in cases:
                with self.subTest(script=Path(arguments[0]).name):
                    result = subprocess.run(
                        [sys.executable, *arguments],
                        check=False,
                        text=True,
                        capture_output=True,
                        cwd=temporary,
                        env=environ,
                    )
                    self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                    self.assertIn(refusal, result.stderr)
                    self.assertNotIn("pass:", result.stdout)

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
        self.assertIn("check-release-destinations.py", preflight)
        self.assertNotIn("check-release-destinations.py", publisher)

    def test_destination_wrapper_dynamically_loads_valid_helper_for_absent_and_occupied_destinations(self):
        with tempfile.TemporaryDirectory() as temporary:
            shared = write_controlled_pinned_helper(Path(temporary))
            arguments = ["check-release-destinations.py", "--version", "1.2.3", "--manifest",
                         str(ROOT / "tools/release-packages.json"), "--shared-directory", str(shared)]
            for status, expected_exit in ((404, 0), (200, 1)):
                with self.subTest(status=status):
                    stdout, stderr = io.StringIO(), io.StringIO()
                    with mock.patch.dict(os.environ, {"CONTROLLED_DESTINATION_STATUS": str(status),
                                                      "CONTROLLED_SOURCE_STATE": ""}), mock.patch.object(
                        self.destination.subprocess, "run",
                        return_value=SimpleNamespace(stdout=self.destination.APPROVED_BUILDS_SHA + "\n"),
                    ), mock.patch.object(sys, "argv", arguments), redirect_stdout(stdout), redirect_stderr(stderr):
                        exit_code = self.destination.main()
                    self.assertEqual(expected_exit, exit_code, stdout.getvalue() + stderr.getvalue())
                    if status == 404:
                        self.assertIn("[release-destinations] pass:", stdout.getvalue())
                    else:
                        self.assertIn("occupied controlled destination", stderr.getvalue())


    def test_destination_wrapper_refuses_wrong_checkout_missing_helper_and_version(self):
        with tempfile.TemporaryDirectory() as temporary:
            shared = Path(temporary) / "builds-execution/Github/publish-containers"
            shared.mkdir(parents=True)
            wrong_sha = SimpleNamespace(stdout="a" * 40 + "\n")
            with mock.patch.object(self.destination.subprocess, "run", return_value=wrong_sha):
                with self.assertRaisesRegex(ValueError, "approved Builds revision"):
                    self.destination.load_pinned_helper(shared)
            approved_sha = SimpleNamespace(stdout=self.destination.APPROVED_BUILDS_SHA + "\n")
            with mock.patch.object(self.destination.subprocess, "run", return_value=approved_sha):
                with self.assertRaisesRegex(ValueError, "helper is unavailable"):
                    self.destination.load_pinned_helper(shared)
            with mock.patch.object(self.destination, "load_pinned_helper") as loader, mock.patch.object(
                sys, "argv", ["check-release-destinations.py", "--version", "invalid"]
            ), redirect_stderr(io.StringIO()):
                self.assertEqual(1, self.destination.main())
                loader.assert_not_called()

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
