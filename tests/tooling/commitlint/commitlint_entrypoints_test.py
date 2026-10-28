"""Exercise the real local commit gate and its configured CI caller contract."""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest import mock

from commitlint_config_test import IGNORED_MESSAGE_SHAPES, REPO_ROOT, stdio


WORKFLOW = REPO_ROOT / ".github/workflows/commitlint.yml"
PUSH_GROUP = "commitlint-${{ github.event_name == 'push' && github.run_id || github.ref }}"
PR_CANCELLATION = "${{ github.event_name == 'pull_request' }}"


def read_workflow() -> dict:
    # js-yaml is already pinned in the root Node graph used by commitlint.
    result = subprocess.run(
        ["node", "--input-type=module", "-e",
         "import fs from 'node:fs'; import yaml from 'js-yaml'; "
         "console.log(JSON.stringify(yaml.load(fs.readFileSync(process.argv[1], 'utf8'))));",
         str(WORKFLOW)],
        cwd=REPO_ROOT, text=True, capture_output=True, check=True, timeout=20,
    )
    return json.loads(result.stdout)


class CommitMessageHookTests(unittest.TestCase):
    def setUp(self) -> None:
        temporary = tempfile.TemporaryDirectory(prefix="commit gate ")
        self.addCleanup(temporary.cleanup)
        self.repo = Path(temporary.name)
        # Hooks and Git-aware runners export routing, index and config variables.
        # None may redirect these disposable commands into the launching repo.
        self.environment = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
        self.environment.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
        self.git("init", "--initial-branch=main")
        self.git("config", "user.name", "Commit Gate Fixture")
        self.git("config", "user.email", "commit-gate@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.hooksPath", ".githooks")
        hooks = self.repo / ".githooks"
        hooks.mkdir()
        shutil.copy2(REPO_ROOT / ".githooks/commit-msg", hooks / "commit-msg")
        shutil.copy2(REPO_ROOT / "commitlint.config.mjs", self.repo / "commitlint.config.mjs")
        tools = self.repo / "tools"
        tools.mkdir()
        # Only the downstream story/evidence gates are substitutes. The hook,
        # message file, Git commit path, CLI and policy are the real ones.
        for name in (
            "check-story-file-scope.py", "check-tenant-isolation-evidence.py",
            "check-story-review-readiness.py",
        ):
            (tools / name).write_text(
                "from pathlib import Path\n"
                "with Path('downstream.txt').open('a') as out: out.write(__file__ + '\\n')\n",
                encoding="utf-8",
            )
        (self.repo / "change.txt").write_text("fixture\n", encoding="utf-8")
        self.git("add", "change.txt")

    def git(self, *arguments: str, check: bool = True) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["git", *arguments], cwd=self.repo, env=self.environment,
            text=True, capture_output=True, check=check, timeout=30,
        )

    def install_commitlint(self) -> None:
        (self.repo / "node_modules").symlink_to(REPO_ROOT / "node_modules", target_is_directory=True)

    def commit(self, message: str) -> subprocess.CompletedProcess[str]:
        path = self.repo / "proposed message.txt"
        path.write_text(message + "\n", encoding="utf-8")
        return self.git("commit", "--file", str(path), check=False)

    def assert_refused_before_other_gates(self, result: subprocess.CompletedProcess[str]) -> None:
        self.assertNotEqual(0, result.returncode, stdio(result))
        self.assertNotEqual(0, self.git("rev-parse", "--verify", "HEAD", check=False).returncode)
        self.assertFalse((self.repo / "downstream.txt").exists(), stdio(result))
        self.assertEqual([], list((self.repo / ".git").glob("story-scope-changed-*.txt")))

    def test_missing_commitlint_blocks_commit_with_recovery_guidance(self) -> None:
        result = self.commit("fix(tooling): Correct ordering")
        self.assert_refused_before_other_gates(result)
        self.assertIn("npm ci", stdio(result))
        self.assertIn("commitlint", stdio(result))

    def test_nonexecutable_commitlint_blocks_commit(self) -> None:
        binary = self.repo / "node_modules/.bin/commitlint"
        binary.parent.mkdir(parents=True)
        binary.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
        binary.chmod(0o644)
        result = self.commit("fix(tooling): Correct ordering")
        self.assert_refused_before_other_gates(result)
        self.assertIn("npm ci", stdio(result))

    def test_invalid_messages_block_real_commits(self) -> None:
        self.install_commitlint()
        for message in ("Update dependencies", "unknown: Correct ordering", *IGNORED_MESSAGE_SHAPES):
            with self.subTest(message=message):
                result = self.commit(message)
                self.assert_refused_before_other_gates(result)
                self.assertIn("message failed commitlint", stdio(result))

    def assert_commit_passes(self, message: str) -> None:
        self.install_commitlint()
        result = self.commit(message)
        self.assertEqual(0, result.returncode, stdio(result))
        self.assertEqual(message, self.git("log", "-1", "--format=%B").stdout.strip())
        invoked = [Path(line).name for line in (self.repo / "downstream.txt").read_text().splitlines()]
        self.assertEqual([
            "check-story-file-scope.py", "check-tenant-isolation-evidence.py",
            "check-story-review-readiness.py",
        ], invoked)
        self.assertEqual([], list((self.repo / ".git").glob("story-scope-changed-*.txt")))

    def test_valid_message_reaches_all_downstream_gates_and_creates_commit(self) -> None:
        self.assert_commit_passes("fix(tooling): Correct ordering\n\nStory-Key: spec-fixture")

    def test_conventional_revert_with_body_reaches_all_gates_and_creates_commit(self) -> None:
        self.assert_commit_passes(
            "revert(tooling): Restore the previous ordering\n\n"
            "This reverts commit 0123456789012345678901234567890123456789.\n\n"
            "Story-Key: spec-fixture"
        )


class CommitlintWorkflowTests(unittest.TestCase):
    def test_main_pushes_have_distinct_groups_and_do_not_cancel(self) -> None:
        concurrency = read_workflow()["concurrency"]
        # Pin the reviewed GitHub expressions: run_id separates both running
        # and pending pushes; cancel-in-progress=false alone loses queued runs.
        self.assertEqual(PUSH_GROUP, concurrency["group"])
        self.assertEqual(PR_CANCELLATION, concurrency["cancel-in-progress"])

    def test_pr_title_edits_call_the_shared_validator_with_the_exact_title(self) -> None:
        workflow = read_workflow()
        triggers = workflow["on"]
        self.assertEqual(["main"], triggers["push"]["branches"])
        self.assertEqual(["main"], triggers["pull_request"]["branches"])
        self.assertEqual({"opened", "synchronize", "reopened", "edited"},
                         set(triggers["pull_request"]["types"]))
        job = workflow["jobs"]["commitlint"]
        self.assertEqual("Hexalith/Hexalith.Builds/.github/workflows/commitlint.yml@main", job["uses"])
        self.assertEqual("${{ github.event.pull_request.title }}", job["with"]["pull-request-title"])
        self.assertNotIn("if", job)
        self.assertNotIn("continue-on-error", job)


class FixtureIsolationTests(unittest.TestCase):
    def test_inherited_git_environment_cannot_route_fixture_commands_outside_its_repo(self) -> None:
        with tempfile.TemporaryDirectory(prefix="outer git fixture ") as directory:
            outer = Path(directory)
            environment = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
            environment.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
            subprocess.run(
                ["git", "init", "--initial-branch=outer"], cwd=outer,
                env=environment, capture_output=True, check=True, timeout=20,
            )
            (outer / "sentinel.txt").write_text("preserve staged content\n", encoding="utf-8")
            subprocess.run(
                ["git", "add", "sentinel.txt"], cwd=outer,
                env=environment, capture_output=True, check=True, timeout=20,
            )
            config = (outer / ".git/config").read_bytes()
            index = (outer / ".git/index").read_bytes()
            injected = {
                "GIT_DIR": str(outer / ".git"), "GIT_WORK_TREE": str(outer),
                "GIT_INDEX_FILE": str(outer / ".git/index"),
                "GIT_COMMON_DIR": str(outer / ".git"),
                "GIT_OBJECT_DIRECTORY": str(outer / ".git/objects"),
                "GIT_CONFIG_COUNT": "1", "GIT_CONFIG_KEY_0": "core.hooksPath",
                "GIT_CONFIG_VALUE_0": str(outer / "injected hooks"),
            }
            fixture = CommitMessageHookTests("test_missing_commitlint_blocks_commit_with_recovery_guidance")
            try:
                with mock.patch.dict(os.environ, injected):
                    fixture.setUp()
                    self.assertEqual(fixture.repo.resolve(),
                                     Path(fixture.git("rev-parse", "--show-toplevel").stdout.strip()).resolve())
                    fixture.test_missing_commitlint_blocks_commit_with_recovery_guidance()
                self.assertEqual(config, (outer / ".git/config").read_bytes())
                self.assertEqual(index, (outer / ".git/index").read_bytes())
                self.assertFalse((outer / "change.txt").exists())
            finally:
                fixture.doCleanups()


if __name__ == "__main__":
    unittest.main()
