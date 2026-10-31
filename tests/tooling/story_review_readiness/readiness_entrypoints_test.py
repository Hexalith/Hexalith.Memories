"""Execute the configured hook and CI readiness commands in disposable repositories."""

from __future__ import annotations

import os
import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

from story_review_readiness_test import REPO_ROOT, VERIFIER, story


def workflow_step(name: str) -> str:
    """Find one literal named CI step, including its enforcement configuration."""
    workflow = (REPO_ROOT / ".github/workflows/ci.yml").read_text(encoding="utf-8")
    steps = re.split(r"^      - name: ", workflow, flags=re.MULTILINE)[1:]
    matches = [step for step in steps if step.splitlines()[0] == name]
    if len(matches) != 1:
        raise AssertionError(f"Expected exactly one CI step named {name!r}")
    return matches[0]


def workflow_job(name: str, workflow: str) -> str:
    """Stop at the next job regardless of its name or conditions."""
    match = re.search(
        rf"(?ms)^  {re.escape(name)}:\n.*?(?=^  [A-Za-z0-9_-]+:\n|\Z)", workflow,
    )
    if match is None:
        raise AssertionError(f"Expected CI job {name!r}")
    return match.group(0)


def workflow_command(name: str) -> str:
    """Read one named CI step's literal shell command, without copying its behavior."""
    lines = workflow_step(name).splitlines()
    for index, line in enumerate(lines):
        if line.startswith("        run: "):
            value = line[len("        run: "):]
            if value != "|":
                return value
            command = []
            for following in lines[index + 1:]:
                if following and not following.startswith("          "):
                    break
                command.append(following[10:])
            return "\n".join(command)
    raise AssertionError(f"CI step {name!r} has no executable run command")


class ReadinessEntryPointTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="readiness entrypoints ")
        self.addCleanup(temporary.cleanup)
        self.repo = Path(temporary.name)
        self.git("init", "--initial-branch=main")
        self.git("config", "diff.renames", "true")
        (self.repo / "docs").mkdir()
        (self.repo / "docs/old é.md").write_text("fixture\n", encoding="utf-8")
        self.git("add", "docs")
        self.git("commit", "-m", "test: create entrypoint baseline fixture")
        self.baseline = self.git("rev-parse", "HEAD").strip()
        tools = self.repo / "tools"
        tools.mkdir()
        shutil.copy2(VERIFIER, tools / VERIFIER.name)
        for name in ("check-story-file-scope.py", "check-tenant-isolation-evidence.py"):
            (tools / name).write_text("raise SystemExit(0)\n", encoding="utf-8")
        commitlint = self.repo / "node_modules/.bin/commitlint"
        commitlint.parent.mkdir(parents=True)
        commitlint.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
        commitlint.chmod(0o755)
        self.artifacts = self.repo / "_bmad-output/implementation-artifacts"
        self.artifacts.mkdir(parents=True)
        (self.artifacts / "sprint-status.yaml").write_text(
            "development_status:\n  99-9-fixture: in-progress\n", encoding="utf-8",
        )
        self.message = self.repo / "message.txt"
        self.message.write_text("test: verify readiness entrypoints\n\nStory: 99-9-fixture\n", encoding="utf-8")

    def git(self, *args):
        result = subprocess.run(
            ["git", "-c", "user.name=Readiness Fixture", "-c",
             "user.email=readiness@example.invalid", "-c", "commit.gpgsign=false", *args],
            cwd=self.repo, text=True, capture_output=True, check=True,
        )
        return result.stdout

    def write_story(self, paths, *, baseline=None):
        (self.artifacts / "99-9-fixture.md").write_text(
            story(file_list=paths, frontmatter=f"---\nbaseline_commit: {baseline or self.baseline}\n---\n"),
            encoding="utf-8",
        )

    def execute(self, command, **environment):
        return subprocess.run(
            command, cwd=self.repo, env={**os.environ, **environment},
            text=True, capture_output=True, check=False, timeout=20,
        )

    def test_hook_derives_cumulative_paths_and_propagates_refusal(self):
        self.git("switch", "-c", "fix/99-9-fixture")
        self.git("mv", "docs/old é.md", "docs/committed 日本語.md")
        self.git("commit", "-m", "test: rename entrypoint fixture")
        self.git("mv", "docs/committed 日本語.md", "docs/staged é.md")
        paths = ["docs/old é.md", "docs/committed 日本語.md", "docs/staged é.md"]
        for omitted in (None, *paths):
            with self.subTest(omitted=omitted):
                self.write_story([path for path in paths if path != omitted])
                result = self.execute(["sh", str(REPO_ROOT / ".githooks/commit-msg"), str(self.message)])
                self.assertEqual(result.returncode, 0 if omitted is None else 1, result.stdout + result.stderr)
                self.assertIn("Selected story key: 99-9-fixture", result.stdout)
                if omitted:
                    self.assertIn(f"'{omitted}' changed but is not in the File List", result.stdout)
                self.assertEqual(list((self.repo / ".git").glob("story-scope-changed-*.txt")), [])

    def test_hook_prepared_paths_preserve_staged_rename_source(self):
        # Main deliberately skips cumulative derivation. Capture the actual
        # shared input in the unrelated scope gate, before readiness consumes it.
        self.git("mv", "docs/old é.md", "docs/new 日本語.md")
        scope = self.repo / "tools/check-story-file-scope.py"
        scope.write_text(
            "import pathlib, sys\n"
            "source = pathlib.Path(sys.argv[sys.argv.index('--changed-files-file') + 1])\n"
            "pathlib.Path('captured-paths.txt').write_bytes(source.read_bytes())\n",
            encoding="utf-8",
        )
        self.write_story(["docs/old é.md", "docs/new 日本語.md"])
        result = self.execute(["sh", str(REPO_ROOT / ".githooks/commit-msg"), str(self.message)])
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(set((self.repo / "captured-paths.txt").read_text().splitlines()),
                         {"docs/old é.md", "docs/new 日本語.md"})

    def test_ci_uses_prepared_range_and_propagates_refusal(self):
        changed = self.repo / "prepared paths.txt"
        changed.write_text("docs/old name.md\ndocs/new name.md\n", encoding="utf-8")
        command = workflow_command("Validate story review readiness")
        for omitted in (None, "docs/old name.md", "docs/new name.md"):
            with self.subTest(omitted=omitted):
                self.write_story([path for path in changed.read_text().splitlines() if path != omitted],
                                 baseline="deliberately-unresolvable")
                result = self.execute(
                    ["bash", "-e", "-c", command], BRANCH_NAME="fix/99-9-fixture",
                    COMMIT_MESSAGE_FILE=str(self.message), CHANGED_FILES_FILE=str(changed),
                )
                self.assertEqual(result.returncode, 0 if omitted is None else 1, result.stdout + result.stderr)
                self.assertIn("Selected story key: 99-9-fixture", result.stdout)
                self.assertNotIn("C1 SKIPPED", result.stdout)
                if omitted:
                    self.assertIn(f"'{omitted}' changed but is not in the File List", result.stdout)

    def test_ci_prepares_both_rename_paths_for_pr_and_push_ranges(self):
        self.git("switch", "-c", "fix/99-9-fixture")
        self.git("mv", "docs/old é.md", "docs/new 日本語.md")
        self.git("commit", "-m", "test: rename CI range fixture")
        head = self.git("rev-parse", "HEAD").strip()
        command = workflow_command("Collect story scope inputs")
        for event in ("pull_request", "push"):
            with self.subTest(event=event):
                environment = self.repo / "github-env.txt"
                environment.write_text("", encoding="utf-8")
                result = self.execute(
                    ["bash", "-e", "-c", command], EVENT_NAME=event,
                    PR_HEAD_REF="fix/99-9-fixture", PR_HEAD_SHA=head, PR_BASE_SHA=self.baseline,
                    PUSH_BEFORE_SHA=self.baseline, GITHUB_SHA=head, GITHUB_REF_NAME="fix/99-9-fixture",
                    GITHUB_ENV=str(environment),
                )
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual(set((self.repo / ".story-scope/changed-files.txt").read_text().splitlines()),
                                 {"docs/old é.md", "docs/new 日本語.md"})
                self.assertIn("CHANGED_FILES_FILE=.story-scope/changed-files.txt", environment.read_text())

    def test_hook_and_ci_forward_independent_metadata_sources(self):
        (self.repo / "docs/old é.md").write_text("changed fixture\n", encoding="utf-8")
        self.git("add", "docs")
        self.write_story(["docs/old é.md"])
        changed = self.repo / "prepared paths.txt"
        changed.write_text("docs/old é.md\n", encoding="utf-8")
        cases = (
            ("branch", "fix/99-9-fixture", "test: branch metadata fixture\n", 0, "Story source: branch"),
            ("trailer", "main", "test: trailer metadata fixture\n\nStory: 99-9-fixture\n", 0, "Story source: trailer"),
            ("conflict", "fix/99-8-other", "test: conflict metadata fixture\n\nStory: 99-9-fixture\n", 1, "Conflicting story keys"),
            ("bypass", "fix/99-9-fixture", "test: bypass metadata fixture\n\nStory: 99-9-fixture\nStory-Review-Readiness-Bypass: fixture outage\n", 0, "bypassed by trailer: fixture outage"),
        )
        for entrypoint in ("hook", "ci"):
            for label, branch, message, code, diagnostic in cases:
                with self.subTest(entrypoint=entrypoint, case=label):
                    self.message.write_text(message, encoding="utf-8")
                    if entrypoint == "hook":
                        if branch != "main":
                            self.git("branch", "-f", branch)
                        self.git("switch", branch)
                        command = ["sh", str(REPO_ROOT / ".githooks/commit-msg"), str(self.message)]
                    else:
                        command = ["bash", "-e", "-c", workflow_command("Validate story review readiness")]
                    result = self.execute(command, BRANCH_NAME=branch,
                                          COMMIT_MESSAGE_FILE=str(self.message), CHANGED_FILES_FILE=str(changed))
                    self.assertEqual(result.returncode, code, result.stdout + result.stderr)
                    self.assertIn(diagnostic, result.stdout)

    def test_ci_step_conditions_and_failure_policy_preserve_enforcement(self):
        step = workflow_step("Validate story review readiness")
        self.assertNotRegex(step, r"(?m)^        (?:if|continue-on-error):")
        workflow = (REPO_ROOT / ".github/workflows/ci.yml").read_text(encoding="utf-8")
        job = workflow_job("story-file-scope", workflow)
        self.assertNotRegex(job, r"(?m)^    continue-on-error:")
        self.assertEqual(re.findall(r"(?m)^    if: (.+)$", job),
                         ["${{ github.event_name != 'push' || github.ref_name != 'main' }}"])

    def test_ci_scope_job_extraction_excludes_a_later_jobs_unrelated_condition(self):
        workflow = (REPO_ROOT / ".github/workflows/ci.yml").read_text(encoding="utf-8")
        workflow = workflow.replace("  shared-ci:\n", "  renamed-shared-ci:\n    if: false\n", 1)
        job = workflow_job("story-file-scope", workflow)
        self.assertEqual(re.findall(r"(?m)^    if: (.+)$", job),
                         ["${{ github.event_name != 'push' || github.ref_name != 'main' }}"])
        self.assertNotIn("renamed-shared-ci", job)


if __name__ == "__main__":
    unittest.main()
