import json
import subprocess
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RESOLVER = REPO_ROOT / "_bmad" / "scripts" / "resolve_customization.py"
POLICY_FACT = "file:{project-root}/_bmad/custom/story-scope-guard.md"
LESSONS_FACT = "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md"
MARKER = "HISTORICAL_SLICE_GUARD:"


def resolve_workflow(skill_name: str) -> dict:
    skill_dir = REPO_ROOT / ".agents" / "skills" / skill_name
    result = subprocess.run(
        [
            sys.executable,
            str(RESOLVER),
            "--skill",
            str(skill_dir),
            "--key",
            "workflow",
        ],
        cwd=REPO_ROOT,
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode != 0:
        raise AssertionError(result.stderr or result.stdout)
    return json.loads(result.stdout)["workflow"]


class BMadCustomizationTests(unittest.TestCase):
    def test_create_story_resolves_update_safe_historical_slice_gate(self):
        workflow = resolve_workflow("bmad-create-story")

        facts = workflow["persistent_facts"]
        self.assertIn("file:{project-root}/**/project-context.md", facts)
        self.assertIn(POLICY_FACT, facts)
        self.assertIn(LESSONS_FACT, facts)

        directives = [
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(MARKER)
        ]
        self.assertEqual(len(directives), 1)
        self.assertIn("Historical Context Classification", directives[0])
        self.assertIn("Slice Proof", directives[0])
        self.assertIn("fail closed", directives[0])
        self.assertIn("ready-for-dev", directives[0])
        self.assertIn("sprint-status", directives[0])

    def test_code_review_resolves_full_historical_slice_layer_and_defaults(self):
        workflow = resolve_workflow("bmad-code-review")

        facts = workflow["persistent_facts"]
        self.assertIn("file:{project-root}/**/project-context.md", facts)
        self.assertIn(POLICY_FACT, facts)
        self.assertIn(LESSONS_FACT, facts)

        directives = [
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(MARKER)
        ]
        self.assertEqual(len(directives), 1)

        layers = workflow["review_layers"]
        layer_ids = [layer["id"] for layer in layers]
        self.assertEqual(layer_ids.count("historical-slice-guard"), 1)
        for default_layer in (
            "blind-hunter",
            "edge-case-hunter",
            "verification-gap",
            "acceptance-auditor",
        ):
            self.assertIn(default_layer, layer_ids)

        guard = next(
            layer for layer in layers if layer["id"] == "historical-slice-guard"
        )
        self.assertEqual(guard["when"], 'Only when {review_mode} = "full".')
        self.assertIn("_bmad/custom/story-scope-guard.md", guard["instruction"])
        self.assertIn("{spec_file}", guard["instruction"])
        self.assertIn("{diff_output}", guard["instruction"])
        self.assertIn("historical anti-template", guard["instruction"])
        self.assertIn("requires a human", guard["instruction"])
        self.assertIn("split decision", guard["instruction"])


if __name__ == "__main__":
    unittest.main()
