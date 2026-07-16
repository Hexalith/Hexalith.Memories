import os
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "release-preflight.ps1"


class ReleasePreflightTests(unittest.TestCase):
    def test_no_existing_tag_passes(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)

    def test_local_tag_collision_fails_exact_ref(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)
            self._git(repo, "tag", "v1.2.3")
            self._git(repo, "tag", "v1.2.30")

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertNotEqual(0, result.returncode)
            self.assertIn("refs/tags/v1.2.3 already exists locally", result.stdout + result.stderr)

    def test_remote_tag_collision_fails_exact_ref(self):
        with tempfile.TemporaryDirectory() as temp:
            remote = Path(temp) / "remote.git"
            seed = Path(temp) / "seed"
            repo = Path(temp) / "work"
            self._git(None, "init", "--bare", str(remote))
            self._init_repo(seed)
            self._git(seed, "remote", "add", "origin", str(remote))
            self._git(seed, "tag", "v1.2.3")
            self._git(seed, "push", "origin", "refs/tags/v1.2.3")

            self._init_repo(repo)
            self._git(repo, "remote", "add", "origin", str(remote))

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertNotEqual(0, result.returncode)
            self.assertIn("refs/tags/v1.2.3 already exists on remote 'origin'", result.stdout + result.stderr)

    def test_matching_local_and_remote_tag_collision_fails_on_exact_ref(self):
        with tempfile.TemporaryDirectory() as temp:
            remote = Path(temp) / "remote.git"
            repo = Path(temp) / "work"
            self._git(None, "init", "--bare", str(remote))
            self._init_repo(repo)
            self._git(repo, "remote", "add", "origin", str(remote))
            self._git(repo, "tag", "v1.2.3")
            self._git(repo, "push", "origin", "refs/tags/v1.2.3")

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertNotEqual(0, result.returncode)
            self.assertIn("refs/tags/v1.2.3 already exists locally", result.stdout + result.stderr)

    def test_similarly_prefixed_remote_tag_does_not_collide(self):
        with tempfile.TemporaryDirectory() as temp:
            remote = Path(temp) / "remote.git"
            seed = Path(temp) / "seed"
            repo = Path(temp) / "work"
            self._git(None, "init", "--bare", str(remote))
            self._init_repo(seed)
            self._git(seed, "remote", "add", "origin", str(remote))
            self._git(seed, "tag", "v1.2.30")
            self._git(seed, "push", "origin", "refs/tags/v1.2.30")

            self._init_repo(repo)
            self._git(repo, "remote", "add", "origin", str(remote))

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)

    def test_semantic_release_no_release_output_exits_successfully(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text("There are no relevant changes, so no new version is released.\n", encoding="utf-8")

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("semantic-release dry-run reported no release", result.stdout)

    def test_semantic_release_ansi_no_release_output_exits_successfully(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "\x1b[90m[4:12:33 PM]\x1b[39m \x1b[90m[semantic-release]\x1b[39m \x1b[90m›\x1b[39m "
                "\x1b[35mℹ \x1b[39m There are no \x1b[32mrelevant changes\x1b[39m, so no new version is released.\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("semantic-release dry-run reported no release", result.stdout)

    def test_semantic_release_stale_checkout_output_exits_successfully_without_remote_probe(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            unreachable_remote = Path(temp) / "must-not-be-probed.git"
            self._init_repo(repo)
            self._git(repo, "remote", "add", "origin", str(unreachable_remote))
            output.write_text(
                "The local branch main is behind the remote one, therefore a new version won't be published.\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("stale checkout", result.stdout)
            self.assertIn("no release or tag check required", result.stdout)
            self.assertNotIn("No stale release tag found", result.stdout)

    def test_semantic_release_logger_prefixed_stale_checkout_exits_successfully(self):
        logger_lines = (
            "[4:12:33 PM] [semantic-release] › ℹ  The local branch main is behind the remote one, therefore a new version won't be published.\n",
            "\x1b[90m[4:12:33 PM]\x1b[39m \x1b[90m[semantic-release]\x1b[39m \x1b[90m›\x1b[39m "
            "\x1b[35mℹ \x1b[39m The local branch main is behind the remote one, therefore a new version won't be published.\n",
        )

        for logger_line in logger_lines:
            with self.subTest(logger_line=logger_line), tempfile.TemporaryDirectory() as temp:
                repo = Path(temp) / "work"
                output = Path(temp) / "dry-run.txt"
                unreachable_remote = Path(temp) / "must-not-be-probed.git"
                self._init_repo(repo)
                self._git(repo, "remote", "add", "origin", str(unreachable_remote))
                output.write_text(logger_line, encoding="utf-8")

                result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                self.assertIn("stale checkout", result.stdout)

    def test_semantic_release_unknown_success_output_fails_loudly(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text("semantic-release completed without a recognized terminal result.\n", encoding="utf-8")

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("terminal outcome could not be parsed", combined)
            self.assertIn("The local branch main is behind the remote one", combined)

    def test_semantic_release_case_variant_or_approximate_stale_output_fails_loudly(self):
        outputs = (
            "The local branch main is Behind the remote one, therefore a new version won't be published.\n",
            "The local branch main is behind its remote one, therefore a new version won't be published.\n",
        )

        for dry_run_output in outputs:
            with self.subTest(dry_run_output=dry_run_output), tempfile.TemporaryDirectory() as temp:
                repo = Path(temp) / "work"
                output = Path(temp) / "dry-run.txt"
                self._init_repo(repo)
                output.write_text(dry_run_output, encoding="utf-8")

                result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

                self.assertNotEqual(0, result.returncode)
                self.assertIn("terminal outcome could not be parsed", result.stdout + result.stderr)

    def test_semantic_release_quoted_or_embedded_stale_output_fails_loudly(self):
        stale_sentence = "The local branch main is behind the remote one, therefore a new version won't be published."
        outputs = (
            f'"{stale_sentence}"\n',
            f'semantic-release said "{stale_sentence}" and stopped.\n',
        )

        for dry_run_output in outputs:
            with self.subTest(dry_run_output=dry_run_output), tempfile.TemporaryDirectory() as temp:
                repo = Path(temp) / "work"
                output = Path(temp) / "dry-run.txt"
                self._init_repo(repo)
                output.write_text(dry_run_output, encoding="utf-8")

                result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

                self.assertNotEqual(0, result.returncode)
                self.assertIn("terminal outcome could not be parsed", result.stdout + result.stderr)

    def test_semantic_release_non_main_stale_output_fails_loudly(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "The local branch release is behind the remote one, therefore a new version won't be published.\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            self.assertIn("terminal outcome could not be parsed", result.stdout + result.stderr)

    def test_semantic_release_stale_checkout_plus_version_fails_as_mixed_outcome(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "The local branch main is behind the remote one, therefore a new version won't be published.\n"
                "The next release version is 1.2.3\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("multiple terminal outcomes", combined)
            self.assertIn("stale checkout", combined)
            self.assertIn("next release version 1.2.3", combined)

    def test_semantic_release_stale_checkout_plus_no_release_fails_as_mixed_outcome(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "The local branch main is behind the remote one, therefore a new version won't be published.\n"
                "There are no relevant changes, so no new version is released.\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("multiple terminal outcomes", combined)
            self.assertIn("stale checkout", combined)
            self.assertIn("no release", combined)

    def test_semantic_release_stale_checkout_plus_malformed_version_fails_loudly(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "The local branch main is behind the remote one, therefore a new version won't be published.\n"
                "The next release version is not-semver\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("malformed next-release marker", combined)
            self.assertIn("stale checkout", combined)

    def test_semantic_release_version_plus_no_release_fails_as_mixed_outcome(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "The next release version is 1.2.3\n"
                "There are no relevant changes, so no new version is released.\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("multiple terminal outcomes", combined)
            self.assertIn("next release version 1.2.3", combined)
            self.assertIn("no release", combined)

    def test_semantic_release_valid_plus_malformed_version_fails_loudly(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "The next release version is 1.2.3\n"
                "The next release version is invalid\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("malformed next-release marker", combined)
            self.assertIn("next release version 1.2.3", combined)

    def test_semantic_release_no_release_plus_malformed_version_fails_loudly(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "There are no relevant changes, so no new version is released.\n"
                "The next release version is invalid\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("malformed next-release marker", combined)
            self.assertIn("no release", combined)

    def test_semantic_release_positive_dry_run_output_resolves_next_version(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "[semantic-release] > Run automated release from branch main\n"
                "[semantic-release] > Found 3 commits since last release\n"
                "The next release version is 1.2.3\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)

    def test_semantic_release_ansi_positive_dry_run_output_resolves_next_version(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "\x1b[90m[4:12:33 PM]\x1b[39m \x1b[90m[semantic-release]\x1b[39m \x1b[90m›\x1b[39m "
                "\x1b[35mℹ \x1b[39m The next release version is \x1b[32m1.2.3\x1b[39m\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)

    def test_semantic_release_payload_terminal_phrases_are_ignored_when_genuine_outcome_exists(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "[4:12:30 PM] [semantic-release] [@semantic-release/release-notes-generator] › ℹ  The next release version is 9.9.9\n"
                "Release notes quote: The local branch main is behind the remote one, therefore a new version won't be published.\n"
                "[4:12:33 PM] [semantic-release] › ℹ  The next release version is 1.2.3\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)
            self.assertNotIn("refs/tags/v9.9.9", result.stdout)

    def test_semantic_release_payload_terminal_phrases_without_genuine_outcome_fail_as_unknown(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "[4:12:30 PM] [semantic-release] [@semantic-release/release-notes-generator] › ℹ  The next release version is 9.9.9\n"
                "Release notes quote: The local branch main is behind the remote one, therefore a new version won't be published.\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            self.assertIn("terminal outcome could not be parsed", result.stdout + result.stderr)

    def test_live_semantic_release_output_ignores_bare_payload_terminal_record(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            fake_bin = Path(temp) / "bin"
            fake_npm = fake_bin / "npm"
            self._init_repo(repo)
            fake_bin.mkdir()
            fake_npm.write_text(
                "#!/bin/sh\n"
                "test \"$HEXALITH_RELEASE_CLASSIFICATION_ONLY\" = 'true' || exit 91\n"
                "printf '%s\\n' \\\n"
                "  'The next release version is 9.9.9' \\\n"
                "  '[4:12:33 PM] [semantic-release] › ℹ  The next release version is 1.2.3'\n",
                encoding="utf-8",
            )
            fake_npm.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{fake_bin}{os.pathsep}{env['PATH']}"

            result = self._run_preflight(repo, env=env)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)
            self.assertNotIn("refs/tags/v9.9.9", result.stdout)

    def test_semantic_release_dry_run_with_multiple_distinct_versions_fails_loudly(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "The next release version is 1.2.3\n"
                "The next release version is 1.3.0\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("multiple distinct next-release versions", combined)
            self.assertIn("1.2.3", combined)
            self.assertIn("1.3.0", combined)

    def test_next_version_with_leading_v_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)

            result = self._run_preflight(repo, "-NextVersion", "v1.2.3")

            self.assertNotEqual(0, result.returncode)
            self.assertIn("does not match the expected semver shape", result.stdout + result.stderr)

    def _run_preflight(
        self,
        repo: Path,
        *args: str,
        env: dict[str, str] | None = None,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "pwsh",
                "-NoLogo",
                "-NoProfile",
                "-File",
                str(SCRIPT),
                "-RepositoryPath",
                str(repo),
                *args,
            ],
            check=False,
            text=True,
            capture_output=True,
            cwd=REPO_ROOT,
            env=env,
        )

    def _init_repo(self, repo: Path) -> None:
        repo.mkdir(parents=True, exist_ok=True)
        self._git(repo, "init")
        self._git(repo, "config", "user.email", "release-preflight@example.invalid")
        self._git(repo, "config", "user.name", "Release Preflight")
        (repo / "README.md").write_text("fixture\n", encoding="utf-8")
        self._git(repo, "add", "README.md")
        self._git(repo, "commit", "-m", "feat: initial fixture")

    def _git(self, cwd: Path | None, *args: str) -> subprocess.CompletedProcess[str]:
        env = os.environ.copy()
        env["GIT_TERMINAL_PROMPT"] = "0"
        result = subprocess.run(
            ["git", *args],
            check=False,
            text=True,
            capture_output=True,
            cwd=cwd,
            env=env,
        )
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        return result


if __name__ == "__main__":
    unittest.main()
