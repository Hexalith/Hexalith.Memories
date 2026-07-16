import json
import re
import subprocess
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RESOLVER = REPO_ROOT / "_bmad" / "scripts" / "resolve_customization.py"
POLICY = REPO_ROOT / "_bmad" / "custom" / "story-scope-guard.md"
LEDGER_POLICY = REPO_ROOT / "_bmad" / "custom" / "story-phase-ledger.md"
LEDGER_SPEC = (
    REPO_ROOT
    / "_bmad-output"
    / "implementation-artifacts"
    / "spec-track-test-counts-in-story-phase-change-log.md"
)
POLICY_FACT = "file:{project-root}/_bmad/custom/story-scope-guard.md"
LEDGER_POLICY_FACT = "file:{project-root}/_bmad/custom/story-phase-ledger.md"
LESSONS_FACT = "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md"
MARKER = "HISTORICAL_SLICE_GUARD:"
LEDGER_MARKER = "STORY_PHASE_LEDGER:"
LEDGER_EQUATION = re.compile(
    r"create baseline `(?P<baseline>\d+)`.*?"
    r"cumulative story `(?P<cumulative>[+-]\d+)`.*?"
    r"external same-suite `(?P<external>[+-]\d+)`.*?"
    r"observed final `(?P<final>\d+)`"
)


def normalize_text(value: str) -> str:
    lines = (line.strip().removeprefix("> ").strip() for line in value.splitlines())
    return " ".join(" ".join(lines).split())


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
        self.assertIn(LEDGER_POLICY_FACT, facts)
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

        ledger_directive = next(
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(LEDGER_MARKER)
        )
        self.assertIn("create-story", ledger_directive)
        self.assertIn("phase delta +0", ledger_directive)
        self.assertIn("baseline discovery totals", ledger_directive)
        self.assertIn("initial File List", ledger_directive)
        self.assertIn("ready-for-dev", ledger_directive)
        self.assertIn("sprint-status", ledger_directive)

    def test_all_affected_workflows_resolve_exactly_one_phase_ledger_directive(self):
        for skill_name in (
            "bmad-create-story",
            "bmad-dev-story",
            "bmad-qa-generate-e2e-tests",
            "bmad-code-review",
        ):
            with self.subTest(skill_name=skill_name):
                workflow = resolve_workflow(skill_name)
                self.assertIn(LEDGER_POLICY_FACT, workflow["persistent_facts"])
                directives = [
                    step
                    for step in workflow["activation_steps_append"]
                    if step.startswith(LEDGER_MARKER)
                ]
                self.assertEqual(len(directives), 1)

    def test_phase_ledger_policy_defines_schema_arithmetic_and_status_gates(self):
        policy = normalize_text(LEDGER_POLICY.read_text(encoding="utf-8"))

        self.assertIn(
            "| Date | Phase | Change | Test count | File List reconciliation |",
            policy,
        )
        for phase_name in (
            "`create-story`",
            "`dev-story`",
            "`qa-gap-closure`",
            "`code-review`",
        ):
            self.assertIn(phase_name, policy)
        self.assertIn("A repeated phase appends another row", policy)
        self.assertIn("runner, discovery scope, filters", policy)
        self.assertIn("create baseline + cumulative story delta + external delta", policy)
        self.assertIn("`create-story` adoption baseline", policy)
        self.assertIn("Record `matched N/N`", policy)
        self.assertIn("declared comparison baseline", policy)
        self.assertIn("a rename is one entry", policy)
        self.assertIn("restored exactly to its baseline leaves the cumulative set", policy)
        for gated_status in ("`ready-for-dev`", "`review`", "`done`"):
            self.assertIn(gated_status, policy)

    def test_dev_story_requires_discovery_and_reconciliation_before_review(self):
        workflow = resolve_workflow("bmad-dev-story")

        directive = next(
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(LEDGER_MARKER)
        )
        self.assertIn("create-story baseline", directive)
        self.assertIn("immediately before and after development", directive)
        self.assertIn("dev-story", directive)
        self.assertIn("actual phase and cumulative deltas", directive)
        self.assertIn("stale count references", directive)
        self.assertIn("File List", directive)
        self.assertIn("Before setting review", directive)
        self.assertIn("fail closed", directive)

    def test_qa_supports_story_bound_and_standalone_modes_and_halts_unresolved(self):
        workflow = resolve_workflow("bmad-qa-generate-e2e-tests")

        directive = next(
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(LEDGER_MARKER)
        )
        self.assertIn("Story-bound gap-closure", directive)
        self.assertIn("halt before changes when unresolved", directive)
        self.assertIn("create baseline", directive)
        self.assertIn("latest chronological row", directive)
        self.assertIn("qa-gap-closure", directive)
        self.assertIn("phase and cumulative deltas", directive)
        self.assertIn("add every QA-touched", directive)
        self.assertIn("fail closed until File List reconciliation succeeds", directive)
        self.assertIn("Standalone QA", directive)
        self.assertIn("story phase ledger: N/A — standalone QA", directive)

    def test_policy_covers_strengthened_tests_and_blocked_discovery(self):
        policy = normalize_text(LEDGER_POLICY.read_text(encoding="utf-8"))

        self.assertIn("record `phase delta +0`", policy)
        self.assertIn("describe the strengthened behavior", policy)
        self.assertIn("do not claim an added test", policy)
        self.assertIn("do not invent a count", policy)
        for blocked_field in (
            "exact command",
            "blocker",
            "owner",
            "consequence",
            "reopen trigger",
        ):
            self.assertIn(blocked_field, policy)

    def test_synthetic_lifecycle_propagates_qa_counts_and_file_list_to_review(self):
        baseline_total = 40
        cumulative_delta = 0
        cumulative_files = {"story.md"}
        rows = []

        def append_phase(phase, phase_delta, touched_files):
            nonlocal cumulative_delta
            cumulative_delta += phase_delta
            cumulative_files.update(touched_files)
            rows.append(
                {
                    "phase": phase,
                    "phase_delta": phase_delta,
                    "cumulative_delta": cumulative_delta,
                    "total": baseline_total + cumulative_delta,
                    "file_list": frozenset(cumulative_files),
                    "reconciliation": (
                        f"matched {len(cumulative_files)}/{len(cumulative_files)}"
                    ),
                }
            )

        append_phase("create-story", 0, set())
        append_phase("dev-story", 3, {"src/widget.py", "tests/test_widget.py"})
        append_phase(
            "qa-gap-closure",
            2,
            {"tests/test_widget_e2e.py", "tests/test-summary.md"},
        )
        append_phase("dev-story", 1, {"src/widget_retry.py"})
        append_phase("qa-gap-closure", 1, {"tests/test_widget_retry.py"})
        append_phase("code-review", 0, {"story-review.md"})

        self.assertEqual([row["phase_delta"] for row in rows], [0, 3, 2, 1, 1, 0])
        self.assertEqual(
            [row["cumulative_delta"] for row in rows],
            [0, 3, 5, 6, 7, 7],
        )
        self.assertEqual(rows[-1]["total"], 47)
        self.assertIn("tests/test_widget_e2e.py", rows[-1]["file_list"])
        self.assertIn("tests/test-summary.md", rows[-1]["file_list"])
        self.assertIn("tests/test_widget_retry.py", rows[-1]["file_list"])
        self.assertEqual(rows[-1]["reconciliation"], "matched 8/8")

    def test_dogfood_ledger_reconciles_story_and_external_same_suite_deltas(self):
        spec = normalize_text(LEDGER_SPEC.read_text(encoding="utf-8"))
        equations = list(LEDGER_EQUATION.finditer(spec))

        self.assertGreaterEqual(len(equations), 1)
        for equation in equations:
            baseline = int(equation.group("baseline"))
            cumulative = int(equation.group("cumulative"))
            external = int(equation.group("external"))
            final = int(equation.group("final"))
            self.assertEqual(baseline + cumulative + external, final)

    def test_code_review_resolves_full_historical_slice_layer_and_defaults(self):
        workflow = resolve_workflow("bmad-code-review")

        facts = workflow["persistent_facts"]
        self.assertIn("file:{project-root}/**/project-context.md", facts)
        self.assertIn(POLICY_FACT, facts)
        self.assertIn(LEDGER_POLICY_FACT, facts)
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
        self.assertEqual(layer_ids.count("story-phase-ledger"), 1)
        for default_layer in (
            "blind-hunter",
            "edge-case-hunter",
            "verification-gap",
            "acceptance-auditor",
        ):
            self.assertEqual(layer_ids.count(default_layer), 1)

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

        ledger = next(
            layer for layer in layers if layer["id"] == "story-phase-ledger"
        )
        ledger_instruction = normalize_text(ledger["instruction"])
        self.assertEqual(ledger["when"], 'Only when {review_mode} = "full".')
        self.assertIn("_bmad/custom/story-phase-ledger.md", ledger_instruction)
        self.assertIn("{spec_file}", ledger_instruction)
        self.assertIn("{diff_output}", ledger_instruction)
        self.assertIn("same-unit count arithmetic", ledger_instruction)
        self.assertIn("File List", ledger_instruction)
        self.assertIn("do not report that row as missing", ledger_instruction)
        self.assertIn("`patch`", ledger_instruction)
        self.assertIn("`decision_needed`", ledger_instruction)
        self.assertIn("fail-closed blocker for `done`", ledger_instruction)

        ledger_directive = next(
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(LEDGER_MARKER)
        )
        self.assertIn("After review actions and patches", ledger_directive)
        self.assertIn("before status synchronization", ledger_directive)
        self.assertIn("code-review", ledger_directive)
        self.assertIn("every in-scope review chunk is complete", ledger_directive)
        self.assertIn("independently re-run comparable discovery", ledger_directive)
        self.assertIn("cumulative story and external same-lane deltas", ledger_directive)
        self.assertIn("File List reconciliation with baseline/name-status evidence", ledger_directive)
        self.assertIn("count mismatch", ledger_directive)
        self.assertIn("unaccounted in-scope file", ledger_directive)
        self.assertIn("before done", ledger_directive)


if __name__ == "__main__":
    unittest.main()
