import os
import shutil
import subprocess
import tempfile
import time
import unittest
from contextlib import contextmanager
from pathlib import Path
from typing import Optional


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "release-preflight.ps1"
PWSH = shutil.which("pwsh")


@contextmanager
def temporary_directory():
    directory = tempfile.TemporaryDirectory()
    try:
        yield directory.name
    finally:
        # Git or antivirus can briefly retain Windows file handles. Removal is
        # idempotent; retry permission failures only, and surface a lasting lock.
        for attempt in range(3):
            try:
                directory.cleanup()
                break
            except PermissionError:
                if attempt == 2:
                    raise
                time.sleep(0.05 * (attempt + 1))


@unittest.skipUnless(PWSH, "PowerShell 7 (pwsh) is required for release preflight execution tests")
class ReleasePreflightTests(unittest.TestCase):
    def test_no_existing_tag_passes(self):
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)

    def test_local_tag_collision_fails_exact_ref(self):
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)
            self._git(repo, "tag", "v1.2.3")
            self._git(repo, "tag", "v1.2.30")

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertNotEqual(0, result.returncode)
            self.assertIn("refs/tags/v1.2.3 already exists locally", result.stdout + result.stderr)

    def test_remote_tag_collision_fails_exact_ref(self):
        with temporary_directory() as temp:
            remote = Path(temp) / "remote.git"
            seed = Path(temp) / "seed"
            repo = Path(temp) / "work"
            self._init_bare_repo(remote)
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
        with temporary_directory() as temp:
            remote = Path(temp) / "remote.git"
            repo = Path(temp) / "work"
            self._init_bare_repo(remote)
            self._init_repo(repo)
            self._git(repo, "remote", "add", "origin", str(remote))
            self._git(repo, "tag", "v1.2.3")
            self._git(repo, "push", "origin", "refs/tags/v1.2.3")

            result = self._run_preflight(repo, "-NextVersion", "1.2.3")

            self.assertNotEqual(0, result.returncode)
            self.assertIn("refs/tags/v1.2.3 already exists locally", result.stdout + result.stderr)

    def test_similarly_prefixed_remote_tag_does_not_collide(self):
        with temporary_directory() as temp:
            remote = Path(temp) / "remote.git"
            seed = Path(temp) / "seed"
            repo = Path(temp) / "work"
            self._init_bare_repo(remote)
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
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text("There are no relevant changes, so no new version is released.\n", encoding="utf-8")

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("semantic-release dry-run reported no release", result.stdout)

    def test_semantic_release_ansi_no_release_output_exits_successfully(self):
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
            with self.subTest(logger_line=logger_line), temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
            with self.subTest(dry_run_output=dry_run_output), temporary_directory() as temp:
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
            with self.subTest(dry_run_output=dry_run_output), temporary_directory() as temp:
                repo = Path(temp) / "work"
                output = Path(temp) / "dry-run.txt"
                self._init_repo(repo)
                output.write_text(dry_run_output, encoding="utf-8")

                result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

                self.assertNotEqual(0, result.returncode)
                self.assertIn("terminal outcome could not be parsed", result.stdout + result.stderr)

    def test_semantic_release_non_main_stale_output_fails_loudly(self):
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
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
        markers = (
            "The next release version is",
            "There is no previous release, the next release version is",
        )
        for marker in markers:
            for payload_marker in markers:
                with self.subTest(marker=marker, payload_marker=payload_marker), temporary_directory() as temp:
                    repo = Path(temp) / "work"
                    self._init_repo(repo)

                    result = self._run_npm_fixture(repo, (
                        f"{payload_marker} 9.9.9",
                        f"[4:12:33 PM] [semantic-release] › ℹ  {marker} 1.2.3",
                    ))

                    self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                    self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)
                    self.assertNotIn("refs/tags/v9.9.9", result.stdout)

    def test_live_first_release_payload_without_core_logger_outcome_fails(self):
        for record in (
            "There is no previous release, the next release version is 9.9.9",
            "[4:12:30 PM] [semantic-release] [plugin] › ℹ  There is no previous release, the next release version is 9.9.9",
        ):
            with self.subTest(record=record), temporary_directory() as temp:
                repo = Path(temp) / "work"
                self._init_repo(repo)
                self._git(repo, "tag", "v9.9.9")

                result = self._run_npm_fixture(repo, (record,))

                self.assertEqual(1, result.returncode)
                self.assertIn("terminal outcome could not be parsed", result.stderr)
                self.assertNotIn("already exists locally", result.stderr)
                self.assertNotIn("No stale release tag found", result.stdout)

    def test_semantic_release_dry_run_with_multiple_distinct_versions_fails_loudly(self):
        with temporary_directory() as temp:
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
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)

            result = self._run_preflight(repo, "-NextVersion", "v1.2.3")

            self.assertNotEqual(0, result.returncode)
            self.assertIn("does not match the expected semver shape", result.stdout + result.stderr)

    def test_first_release_marker_resolves_version(self):
        marker = "There is no previous release, the next release version is 1.0.0-beta.1"
        for record in (
            marker,
            f"[4:12:33 PM] [semantic-release] › ℹ  {marker}",
            f"\x1b[90m[4:12:33 PM]\x1b[39m [semantic-release] › ℹ  \x1b[32m{marker}\x1b[39m",
        ):
            with self.subTest(record=record), temporary_directory() as temp:
                repo = Path(temp) / "work"
                output = Path(temp) / "dry-run.txt"
                self._init_repo(repo)
                output.write_text(record + "\n", encoding="utf-8")

                result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                self.assertIn("No stale release tag found for refs/tags/v1.0.0-beta.1", result.stdout)

    def test_first_release_marker_still_checks_tag_collision(self):
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            self._git(repo, "tag", "v1.0.0")
            output.write_text(
                "There is no previous release, the next release version is 1.0.0\n", encoding="utf-8"
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(1, result.returncode)
            self.assertIn("refs/tags/v1.0.0 already exists locally", result.stderr)

    def test_first_release_malformed_marker_is_rejected_even_with_valid_outcome(self):
        for version in ("invalid", "v1.0.0", "1.0.0 trailing text", ""):
            with self.subTest(version=version), temporary_directory() as temp:
                repo = Path(temp) / "work"
                output = Path(temp) / "dry-run.txt"
                self._init_repo(repo)
                output.write_text(
                    "The next release version is 1.2.3\n"
                    f"There is no previous release, the next release version is {version}\n",
                    encoding="utf-8",
                )

                result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

                self.assertEqual(1, result.returncode)
                self.assertIn("malformed next-release marker", result.stderr)
                self.assertIn("Expected 'The next release version is <version>'", result.stderr)
                self.assertIn("or 'There is no previous release, the next release version is <version>'", result.stderr)

    def test_first_release_conflicting_outcome_is_rejected(self):
        for other_record, expected in (
            ("The next release version is 1.2.3", "multiple distinct next-release versions"),
            ("There are no relevant changes, so no new version is released.", "multiple terminal outcomes"),
            ("The local branch main is behind the remote one, therefore a new version won't be published.",
             "multiple terminal outcomes"),
        ):
            with self.subTest(other_record=other_record), temporary_directory() as temp:
                repo = Path(temp) / "work"
                output = Path(temp) / "dry-run.txt"
                self._init_repo(repo)
                output.write_text(
                    f"{other_record}\nThere is no previous release, the next release version is 1.0.0\n",
                    encoding="utf-8",
                )

                result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

                self.assertEqual(1, result.returncode)
                self.assertIn(expected, result.stderr)

    def test_first_release_payload_records_cannot_override_logger_outcome(self):
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "Release notes quote: There is no previous release, the next release version is 9.9.9\n"
                "[4:12:30 PM] [semantic-release] [plugin] › ℹ  There is no previous release, the next release version is 9.9.9\n"
                "[4:12:33 PM] [semantic-release] › ℹ  There is no previous release, the next release version is 1.0.0\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.0.0", result.stdout)
            self.assertNotIn("refs/tags/v9.9.9", result.stdout)

    def test_invalid_repository_path_has_actionable_diagnostic(self):
        with temporary_directory() as temp:
            file_path = Path(temp) / "a-file"
            file_path.write_text("not a directory\n", encoding="utf-8")
            non_git = Path(temp) / "not-a-repo"
            non_git.mkdir()
            for repo in (Path(temp) / "missing", file_path, non_git):
                with self.subTest(repo=repo):
                    result = self._run_preflight(repo, "-NextVersion", "1.2.3")

                    self.assertEqual(1, result.returncode)
                    self.assertIn("RepositoryPath", result.stderr)
                    self.assertIn(str(repo), result.stderr)
                    self.assertIn("existing Git working directory", result.stderr)

    def test_local_ref_probe_failure_keeps_original_details_and_stops_before_remote(self):
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)
            self._git(repo, "remote", "add", "origin", str(Path(temp) / "must-not-be-probed.git"))

            result = self._run_git_fixture(repo, "ref-error")

            self.assertEqual(1, result.returncode)
            self.assertIn("Local tag-ref probe failed for refs/tags/v1.2.3", result.stderr)
            self.assertIn("Inspect", result.stderr)
            self.assertIn("exit code 128", result.stderr)
            self.assertIn("fixture ref database unreadable", result.stderr)
            self.assertNotIn("No stale release tag found", result.stdout)
            self.assertFalse((repo / "remote-probed").exists())

    def test_failure_reports_inner_exception_and_script_stack(self):
        with temporary_directory() as temp:
            repo = Path(temp) / "work"
            self._init_repo(repo)
            self._git(repo, "remote", "add", "origin", str(Path(temp) / "must-not-be-probed.git"))

            result = self._run_git_fixture(repo, "nested-error")

            self.assertEqual(1, result.returncode)
            self.assertIn("outer fixture failure", result.stderr)
            self.assertIn("inner fixture detail", result.stderr)
            self.assertIn("Invoke-GitCommand", result.stderr)
            self.assertIn("release-preflight.ps1", result.stderr)
            self.assertFalse((repo / "remote-probed").exists())

    def test_peeled_only_remote_ref_uses_exact_tag_matching(self):
        for scenario, expected_exit in (("peeled-exact", 1), ("peeled-prefix", 0)):
            with self.subTest(scenario=scenario), temporary_directory() as temp:
                repo = Path(temp) / "work"
                self._init_repo(repo)
                self._git(repo, "remote", "add", "origin", str(Path(temp) / "unused.git"))

                result = self._run_git_fixture(repo, scenario)

                self.assertEqual(expected_exit, result.returncode, result.stdout + result.stderr)
                self.assertTrue((repo / "remote-probed").exists())
                if expected_exit:
                    self.assertIn("refs/tags/v1.2.3 already exists on remote 'origin'", result.stderr)
                else:
                    self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)

    def _run_npm_fixture(self, repo: Path, records: tuple[str, ...]) -> subprocess.CompletedProcess[str]:
        fake_bin = repo / "fake-bin"
        fake_bin.mkdir()
        fake_npm = fake_bin / "npm"
        fake_npm.write_text(
            "#!/bin/sh\n"
            "test \"$HEXALITH_RELEASE_CLASSIFICATION_ONLY\" = 'true' || exit 91\n"
            "printf '%s\\n' \"$PREFLIGHT_FIXTURE_OUTPUT\"\n",
            encoding="utf-8",
        )
        fake_npm.chmod(0o755)
        env = self._environment()
        env["PATH"] = f"{fake_bin}{os.pathsep}{env['PATH']}"
        env["PREFLIGHT_FIXTURE_OUTPUT"] = "\n".join(records)
        return self._run_preflight(repo, env=env)

    def _run_git_fixture(self, repo: Path, scenario: str) -> subprocess.CompletedProcess[str]:
        wrapper = repo / "preflight-fixture.ps1"
        wrapper.write_text(
            "param([string] $ScriptPath, [string] $RepositoryPath, [string] $Scenario)\n"
            "$global:PreflightFixtureScenario = $Scenario\n"
            "$global:PreflightFixtureRepository = $RepositoryPath\n"
            "$global:PreflightFixtureRealGit = @(Get-Command git -CommandType Application)[0].Source\n"
            "function global:git {\n"
            "    if ($args -contains 'show-ref') {\n"
            "        if ($global:PreflightFixtureScenario -eq 'ref-error') {\n"
            "            Write-Error 'fixture ref database unreadable' -ErrorAction Continue\n"
            "            $global:LASTEXITCODE = 128\n"
            "            return\n"
            "        }\n"
            "        if ($global:PreflightFixtureScenario -eq 'nested-error') {\n"
            "            throw [System.InvalidOperationException]::new('outer fixture failure',\n"
            "                [System.IO.IOException]::new('inner fixture detail'))\n"
            "        }\n"
            "    }\n"
            "    if ($args -contains 'ls-remote') {\n"
            "        Set-Content -LiteralPath (Join-Path $global:PreflightFixtureRepository 'remote-probed') -Value 'yes'\n"
            "        $global:LASTEXITCODE = 0\n"
            "        if ($global:PreflightFixtureScenario -eq 'peeled-exact') {\n"
            "            Write-Output ('a' * 40 + \"`trefs/tags/v1.2.3^{}\")\n"
            "            return\n"
            "        }\n"
            "        if ($global:PreflightFixtureScenario -eq 'peeled-prefix') {\n"
            "            Write-Output ('a' * 40 + \"`trefs/tags/v1.2.30^{}\")\n"
            "            return\n"
            "        }\n"
            "        throw 'unexpected remote probe'\n"
            "    }\n"
            "    & $global:PreflightFixtureRealGit @args\n"
            "}\n"
            "& $ScriptPath -RepositoryPath $RepositoryPath -NextVersion '1.2.3'\n"
            "exit $LASTEXITCODE\n",
            encoding="utf-8",
        )
        return subprocess.run(
            [PWSH, "-NoLogo", "-NoProfile", "-File", str(wrapper), str(SCRIPT), str(repo), scenario],
            check=False, text=True, capture_output=True, cwd=REPO_ROOT,
            env=self._environment(), encoding="utf-8", errors="replace",
        )

    @staticmethod
    def _environment(source=None):
        # Git hooks export routing variables that must not redirect disposable fixtures.
        return {key: value for key, value in (os.environ if source is None else source).items()
                if not key.startswith("GIT_")}

    def _run_preflight(
        self,
        repo: Path,
        *args: str,
        env: Optional[dict[str, str]] = None,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                PWSH,
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
            env=self._environment(env),
            encoding="utf-8",
            errors="replace",
        )

    def _init_bare_repo(self, repo: Path) -> None:
        self._git(None, "init", "--bare", "--initial-branch=main", str(repo))

    def _init_repo(self, repo: Path) -> None:
        repo.mkdir(parents=True, exist_ok=True)
        self._git(repo, "init", "--initial-branch=main")
        self._git(repo, "config", "user.email", "release-preflight@example.invalid")
        self._git(repo, "config", "user.name", "Release Preflight")
        (repo / "README.md").write_text("fixture\n", encoding="utf-8")
        self._git(repo, "add", "README.md")
        self._git(repo, "commit", "-m", "feat: initial fixture")

    def _git(self, cwd: Optional[Path], *args: str) -> subprocess.CompletedProcess[str]:
        env = self._environment()
        env["GIT_TERMINAL_PROMPT"] = "0"
        result = subprocess.run(
            ["git", *args],
            check=False,
            text=True,
            capture_output=True,
            cwd=cwd,
            env=env,
            encoding="utf-8",
            errors="replace",
        )
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        return result


if __name__ == "__main__":
    unittest.main()
