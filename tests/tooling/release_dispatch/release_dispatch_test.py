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

    def test_workflow_dispatch_proves_source_before_production_job(self):
        workflow = (ROOT / ".github/workflows/release.yml").read_text(encoding="utf-8")
        self.assertIn("workflow_dispatch:", workflow)
        self.assertNotIn("  push:\n", workflow)
        self.assertNotIn("    if: github.ref == 'refs/heads/main'", workflow)
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
        self.assertIn("publication-flag: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}", workflow)
        self.assertIn("NuGet/login@v1", workflow)
        self.assertNotIn("./tools/test-release.ps1", workflow)
        self.assertIn("Release version was computed but semantic-release created no tag.", workflow)

    def test_release_pin_is_approved_and_profile_tracks_the_gitlink(self):
        workflow = (ROOT / ".github/workflows/release.yml").read_text(encoding="utf-8")
        approved = "81c88b34d09d34260b92d7f0928dbbc1c7446baa"
        self.assertEqual(approved, self.source.APPROVED_BUILDS_SHA)
        self.assertEqual(approved, self.destination.APPROVED_BUILDS_SHA)
        self.assertEqual(3, workflow.count(approved))
        profile = json.loads((ROOT / "tools/builds-publication-qualification.json").read_text(encoding="utf-8"))
        gitlink = subprocess.run(
            ["git", "rev-parse", "HEAD:references/Hexalith.Builds"], cwd=ROOT,
            check=True, capture_output=True, text=True,
        ).stdout.strip()
        self.assertEqual(gitlink, profile["buildsSha"])

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
        release_config = json.loads((ROOT / ".releaserc.json").read_text(encoding="utf-8"))
        prepare_command = release_config["plugins"][2][1]["prepareCmd"]
        self.assertIn("check-release-destinations.py", preflight)
        self.assertNotIn("check-release-destinations.py", publisher)
        self.assertLess(prepare_command.index("pack-release.ps1"), prepare_command.index("check-release-destinations.py"))
        self.assertLess(prepare_command.index("check-release-destinations.py"), prepare_command.index("check-release-source.py"))
        self.assertIn("--version ${nextRelease.version}", prepare_command)
        self.assertNotIn("check-release-source.py", release_config["plugins"][2][1]["publishCmd"])

    def test_both_production_protection_checks_require_reviewers_no_admin_bypass_and_branch_policy(self):
        workflow = (ROOT / ".github/workflows/release.yml").read_text(encoding="utf-8")
        with tempfile.TemporaryDirectory() as temporary:
            fake_gh = Path(temporary) / "gh"
            fake_gh.write_text(
                "#!/usr/bin/env bash\n"
                "set -euo pipefail\n"
                "query=''\n"
                "while [ \"$#\" -gt 0 ]; do\n"
                "  if [ \"$1\" = '--jq' ]; then shift; query=$1; fi\n"
                "  shift\n"
                "done\n"
                "printf '%s\\n' \"$GH_RESPONSE\" | jq -r \"$query\"\n",
                encoding="utf-8",
            )
            fake_gh.chmod(0o755)
            reviewer = {"type": "User", "id": 1}
            scenarios = (
                ({"protection_rules": []}, False),
                ({"protection_rules": [{"type": "wait_timer", "wait_timer": 5}]}, False),
                ({"protection_rules": [{"type": "required_reviewers", "reviewers": []}]}, False),
                ({"protection_rules": [{"type": "required_reviewers", "reviewers": [reviewer]}],
                  "can_admins_bypass": True, "deployment_branch_policy": {"protected_branches": True}}, False),
                ({"protection_rules": [{"type": "required_reviewers", "reviewers": [reviewer]}],
                  "can_admins_bypass": False, "deployment_branch_policy": None}, False),
                ({"protection_rules": [{"type": "required_reviewers", "reviewers": [reviewer]}],
                  "can_admins_bypass": False, "deployment_branch_policy": {"protected_branches": True}}, True),
            )
            for step_name in ("Require production environment approval rule", "Recheck production approval rule before publication"):
                step = workflow.split("      - name: " + step_name, 1)[1]
                command = re.search(r"        run: \|\n((?:          .*\n)+)", step)
                self.assertIsNotNone(command)
                script = "\n".join(line[10:] for line in command.group(1).splitlines())
                for response, expected_success in scenarios:
                    with self.subTest(step=step_name, response=response):
                        environment = dict(os.environ)
                        environment.update(
                            PATH=f"{temporary}:{os.environ['PATH']}",
                            GITHUB_REPOSITORY="Hexalith/Hexalith.Memories",
                            GH_TOKEN="fixture",
                            GH_RESPONSE=json.dumps(response),
                        )
                        result = subprocess.run(["bash", "-c", script], env=environment, capture_output=True, text=True)
                        self.assertEqual(expected_success, result.returncode == 0, result.stderr)

    def test_prepare_hook_refuses_failed_source_proof_before_tag_phase(self):
        release_config = json.loads((ROOT / ".releaserc.json").read_text(encoding="utf-8"))
        command = release_config["plugins"][2][1]["prepareCmd"].replace("${nextRelease.version}", "1.2.3")
        with tempfile.TemporaryDirectory() as temporary:
            fake_bin = Path(temporary)
            pack = fake_bin / "pwsh"
            pack.write_text("#!/usr/bin/env bash\nexit 0\n", encoding="utf-8")
            pack.chmod(0o755)
            proof = fake_bin / "python3"
            proof.write_text(
                "#!/usr/bin/env bash\n[[ \"$1\" == *check-release-source.py ]] && exit 42\nexit 0\n",
                encoding="utf-8",
            )
            proof.chmod(0o755)
            environment = dict(os.environ)
            environment["PATH"] = f"{temporary}:{os.environ['PATH']}"

            result = subprocess.run(["bash", "-c", command], env=environment, capture_output=True, text=True)

            self.assertEqual(42, result.returncode)

    def test_prepare_hook_refuses_failed_destination_proof_before_tag_phase(self):
        release_config = json.loads((ROOT / ".releaserc.json").read_text(encoding="utf-8"))
        command = release_config["plugins"][2][1]["prepareCmd"].replace("${nextRelease.version}", "1.2.3")
        with tempfile.TemporaryDirectory() as temporary:
            fake_bin = Path(temporary)
            for name, content in (
                ("pwsh", "#!/usr/bin/env bash\nexit 0\n"),
                ("python3", "#!/usr/bin/env bash\n[[ \"$1\" == *check-release-destinations.py ]] && exit 43\nexit 0\n"),
            ):
                path = fake_bin / name
                path.write_text(content, encoding="utf-8")
                path.chmod(0o755)
            environment = dict(os.environ)
            environment["PATH"] = f"{temporary}:{os.environ['PATH']}"
            result = subprocess.run(["bash", "-c", command], env=environment, capture_output=True, text=True)
            self.assertEqual(43, result.returncode)

    def test_prepare_hook_refuses_source_that_advances_during_destination_validation(self):
        release_config = json.loads((ROOT / ".releaserc.json").read_text(encoding="utf-8"))
        command = release_config["plugins"][2][1]["prepareCmd"].replace("${nextRelease.version}", "1.2.3")
        sha = "b" * 40
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            write_controlled_pinned_helper(directory)
            tooling = directory / "tools"
            tooling.mkdir()
            for path in (SOURCE_SCRIPT, DESTINATION_SCRIPT, ROOT / "tools/release-packages.json"):
                shutil.copyfile(path, tooling / path.name)
            fake_bin = directory / "bin"
            fake_bin.mkdir()
            for name, content in (
                ("pwsh", "#!/usr/bin/env bash\nexit 0\n"),
                ("git", "#!/usr/bin/env bash\n"
                 f"if [[ \"$2\" == *builds-execution ]]; then echo {self.source.APPROVED_BUILDS_SHA}; "
                 f"else echo {sha}; fi\n"),
            ):
                path = fake_bin / name
                path.write_text(content, encoding="utf-8")
                path.chmod(0o755)
            environment = {key: value for key, value in os.environ.items() if not key.startswith(("GIT_", "GITHUB_"))}
            environment.update(
                PATH=f"{fake_bin}:{os.environ['PATH']}",
                GITHUB_REPOSITORY="Hexalith/Hexalith.Memories",
                GITHUB_EVENT_NAME="workflow_dispatch",
                GITHUB_REF="refs/heads/main",
                GITHUB_SHA=sha,
                CONTROLLED_DESTINATION_STATUS="404",
                CONTROLLED_SOURCE_STATE=str(directory / "source-advanced"),
            )
            result = subprocess.run(["bash", "-c", command], cwd=directory, env=environment, capture_output=True, text=True)

            self.assertEqual(1, result.returncode, result.stdout + result.stderr)
            self.assertIn("[release-destinations] pass:", result.stdout)
            self.assertIn("[release-source] refused: source-no-longer-current", result.stderr)

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

    def test_workflow_publication_detection_requires_new_tag_and_artifacts(self):
        workflow = (ROOT / ".github/workflows/release.yml").read_text(encoding="utf-8")
        step = workflow.split("      - name: Detect release publication\n", 1)[1].split("      - name:", 1)[0]
        script = textwrap.dedent(step.split("        run: |\n", 1)[1])
        scenarios = (
            ("unchanged-tags", False, False, False, False),
            ("new-tag-without-artifacts", True, False, False, False),
            ("new-tag-without-packages", True, True, False, False),
            ("new-tag-with-artifacts", True, True, True, True),
        )
        for name, new_tag, deployment, package, expected_success in scenarios:
            with self.subTest(name=name), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary)
                environment = {key: value for key, value in os.environ.items() if not key.startswith(("GIT_", "GITHUB_"))}
                environment.update(BEFORE_FINGERPRINT=hashlib.sha256(b"").hexdigest(),
                                   GITHUB_OUTPUT=str(directory / "github-output"))
                for arguments in (
                    ["init", "--initial-branch=main"],
                    ["-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                     "commit", "--allow-empty", "-m", "chore: create publication detection fixture"],
                ):
                    subprocess.run(["git", *arguments], cwd=directory, env=environment, check=True, capture_output=True)
                if new_tag:
                    subprocess.run(["git", "tag", "v1.2.3"], cwd=directory, env=environment, check=True, capture_output=True)
                for present, relative in (
                    (deployment, "artifacts/deployment/hexalith-memories-production.yaml"),
                    (package, "artifacts/packages/release/Package.1.2.3.nupkg"),
                ):
                    if present:
                        path = directory / relative
                        path.parent.mkdir(parents=True, exist_ok=True)
                        path.write_text("fixture", encoding="utf-8")
                output = Path(environment["GITHUB_OUTPUT"])
                output.write_text("", encoding="utf-8")

                result = subprocess.run(["bash", "-e", "-o", "pipefail", "-c", script], cwd=directory,
                                        env=environment, capture_output=True, text=True)

                self.assertEqual(expected_success, result.returncode == 0, result.stdout + result.stderr)
                self.assertEqual("published=true\n" if expected_success else "", output.read_text(encoding="utf-8"))
                if not new_tag:
                    self.assertIn("semantic-release created no tag", result.stderr)

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
