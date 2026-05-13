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

    def test_semantic_release_positive_dry_run_output_resolves_next_version(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "[semantic-release] > Run automated release from branch main\n"
                "[semantic-release] > Found 3 commits since last release\n"
                "[semantic-release] > The next release version is 1.2.3\n",
                encoding="utf-8",
            )

            result = self._run_preflight(repo, "-SemanticReleaseDryRunOutputPath", str(output))

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("No stale release tag found for refs/tags/v1.2.3", result.stdout)

    def test_semantic_release_dry_run_with_multiple_distinct_versions_fails_loudly(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "work"
            output = Path(temp) / "dry-run.txt"
            self._init_repo(repo)
            output.write_text(
                "[semantic-release] > The next release version is 1.2.3\n"
                "[semantic-release] > The next release version is 1.3.0\n",
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

    def _run_preflight(self, repo: Path, *args: str) -> subprocess.CompletedProcess[str]:
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
