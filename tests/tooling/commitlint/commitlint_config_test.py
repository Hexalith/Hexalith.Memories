import subprocess
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
COMMITLINT = REPO_ROOT / "node_modules" / ".bin" / "commitlint"
CONFIG = REPO_ROOT / "commitlint.config.mjs"
IGNORED_MESSAGE_SHAPES = (
    "Merge branch 'topic'",
    "Merge pull request #42 from Hexalith/topic",
    "Merge remote-tracking branch 'origin/main'",
    "Merge tag 'v1.2.3'",
    "v1.2.3",
    "1.2.3",
    'Revert "fix: Correct ordering"',
    'reapply "fix: Correct ordering"',
    "fixup! fix: Correct ordering",
    "squash! fix: Correct ordering",
)


def run_commitlint(message: str) -> "subprocess.CompletedProcess[str]":
    if not COMMITLINT.is_file():
        raise AssertionError(
            f"Repository-pinned commitlint CLI is missing at {COMMITLINT}; run npm ci first."
        )

    return subprocess.run(
        [str(COMMITLINT), "--config", str(CONFIG), "--verbose"],
        cwd=REPO_ROOT,
        input=f"{message}\n",
        text=True,
        capture_output=True,
        check=False,
        timeout=30,
    )


def stdio(result: "subprocess.CompletedProcess[str]") -> str:
    return (result.stdout or "") + (result.stderr or "")


class CommitlintConfigTests(unittest.TestCase):
    def test_default_ignored_message_shapes_are_validated(self) -> None:
        for message in IGNORED_MESSAGE_SHAPES:
            with self.subTest(message=message):
                result = run_commitlint(message)
                self.assertNotEqual(0, result.returncode, stdio(result))
                self.assertIn("type-empty", stdio(result))

    def test_conventional_commit_types_remain_accepted(self) -> None:
        for commit_type in (
            "build", "chore", "ci", "docs", "feat", "fix", "perf",
            "refactor", "revert", "style", "test",
        ):
            with self.subTest(commit_type=commit_type):
                result = run_commitlint(f"{commit_type}(tooling): Correct ordering")
                self.assertEqual(0, result.returncode, stdio(result))

    def test_chore_maintenance_subject_is_accepted(self) -> None:
        result = run_commitlint(
            "chore: update subproject references for Hexalith.FrontComposer and Hexalith.Tenants"
        )

        self.assertEqual(0, result.returncode, stdio(result))

    def test_unsupported_type_is_rejected_by_type_enum(self) -> None:
        result = run_commitlint("unknown: update dependencies")

        self.assertNotEqual(0, result.returncode, stdio(result))
        self.assertIn("type-enum", stdio(result))

    def test_malformed_header_is_rejected(self) -> None:
        result = run_commitlint("Update dependencies")

        self.assertNotEqual(0, result.returncode, stdio(result))
        self.assertIn("type-empty", stdio(result))
        self.assertIn("subject-empty", stdio(result))
