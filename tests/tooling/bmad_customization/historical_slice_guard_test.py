import json
import re
import subprocess
import sys
import tomllib
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RESOLVER = REPO_ROOT / "_bmad" / "scripts" / "resolve_customization.py"
POLICY = REPO_ROOT / "_bmad" / "custom" / "story-scope-guard.md"
POLICY_FACT = "file:{project-root}/_bmad/custom/story-scope-guard.md"
LESSONS_FACT = "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md"
MARKER = "HISTORICAL_SLICE_GUARD:"
# Routes that author or register stories and must therefore load the policy.
# Added 2026-07-28 alongside the four `_bmad/custom/*.toml` overrides.
AUTHORING_ROUTES = (
    "bmad-correct-course",
    "bmad-create-epics-and-stories",
    "bmad-spec",
    "bmad-sprint-planning",
)
# bmad-spec is deliberately excluded: the cross-tenant project-context delivery
# contract in bmad_customization_test.py pins its resolved persistent_facts to
# exactly the bridge fact, so the policy reaches that route through its
# activation directive instead. See _bmad/custom/bmad-spec.toml.
AUTHORING_ROUTES_WITH_POLICY_FACT = (
    "bmad-correct-course",
    "bmad-create-epics-and-stories",
    "bmad-sprint-planning",
)


def normalize_text(value: str) -> str:
    lines = (line.strip().removeprefix("> ").strip() for line in value.splitlines())
    return " ".join(" ".join(lines).split())


def markdown_list_items(value: str, heading: str) -> set[str]:
    marker = f"## {heading}"
    section = value.split(marker, maxsplit=1)[1].split("\n## ", maxsplit=1)[0]
    items = []
    current = []
    for raw_line in section.splitlines():
        line = raw_line.strip()
        match = re.match(r"^(?:-|\d+\.)\s+(.*)$", line)
        if match:
            if current:
                items.append(" ".join(current))
            current = [match.group(1)]
        elif current and line:
            current.append(line)
        elif current:
            items.append(" ".join(current))
            current = []
    if current:
        items.append(" ".join(current))
    return {normalize_text(item) for item in items}


def load_workflow_config(path: Path) -> dict:
    with path.open("rb") as stream:
        return tomllib.load(stream)["workflow"]


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


class HistoricalSliceGuardTests(unittest.TestCase):
    def test_every_story_authoring_route_resolves_exactly_one_guard(self):
        """Route coverage is refresh-safe on all four routes added 2026-07-28.

        DW 27.3-CR16 recorded that the 2026-07-26 correct-course split produced two
        anti-template stories because the splitting route never loaded the policy.
        These four routes author or register stories; a refresh must not silently
        drop the guard from any of them.
        """
        for skill_name in AUTHORING_ROUTES:
            with self.subTest(skill=skill_name):
                workflow = resolve_workflow(skill_name)

                directives = [
                    step
                    for step in workflow["activation_steps_append"]
                    if step.startswith(MARKER)
                ]
                self.assertEqual(len(directives), 1)
                directive = directives[0]
                self.assertIn("story-scope policy", directive)
                # Every route must state that backlog grants no exemption -- the
                # exact loophole DW 27.3-CR16 relied on.
                self.assertIn("backlog", directive)

        for skill_name in AUTHORING_ROUTES_WITH_POLICY_FACT:
            with self.subTest(skill=skill_name, delivery="persistent_fact"):
                facts = resolve_workflow(skill_name)["persistent_facts"]
                self.assertEqual(facts.count(POLICY_FACT), 1)
                self.assertEqual(facts.count(LESSONS_FACT), 1)

    def test_spec_route_receives_policy_without_diluting_context_bridge(self):
        """bmad-spec must get the policy by directive, not by persistent fact.

        The cross-tenant project-context delivery contract pins this route's
        resolved persistent_facts to exactly the bridge fact. That gate is not
        weakened to accommodate the guard, so the directive must name the policy
        files it requires the route to read.
        """
        workflow = resolve_workflow("bmad-spec")

        self.assertEqual(
            workflow["persistent_facts"],
            ["file:{project-root}/project-context.md"],
        )

        directive = next(
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(MARKER)
        )
        self.assertIn("_bmad/custom/story-scope-guard.md", directive)
        self.assertIn("story-creation-lessons.md", directive)

    def test_authoring_routes_retain_generated_defaults(self):
        """The team override must merge over defaults, never replace them."""
        spec_workflow = resolve_workflow("bmad-spec")
        self.assertIn("spec_template", spec_workflow)
        self.assertEqual(spec_workflow["spec_filename"], "SPEC.md")
        self.assertIn(
            "file:{project-root}/project-context.md",
            spec_workflow["persistent_facts"],
        )

        for skill_name in ("bmad-correct-course", "bmad-sprint-planning"):
            with self.subTest(skill=skill_name):
                workflow = resolve_workflow(skill_name)
                self.assertIn(
                    "file:{project-root}/**/project-context.md",
                    workflow["persistent_facts"],
                )

    def test_policy_binds_at_authoring_not_only_at_ready_for_dev(self):
        """The backlog exemption DW 27.3-CR16 relied on must be closed."""
        policy = POLICY.read_text(encoding="utf-8")
        creation_section = normalize_text(
            policy.split("## Creation gate", maxsplit=1)[1].split("\n## ", maxsplit=1)[
                0
            ]
        )

        self.assertIn("authored or registered", creation_section)
        self.assertIn("at any status, including `backlog`", creation_section)
        self.assertIn(
            "`ready-for-dev` is a second, stricter checkpoint, not the first one.",
            creation_section,
        )
        self.assertIn(
            "it does not become compliant by not being selected yet.",
            creation_section,
        )

    def test_policy_scopes_the_executable_subset_without_overclaiming(self):
        """The gate checks the record exists; it must not claim to judge correctness."""
        policy = POLICY.read_text(encoding="utf-8")
        subset = normalize_text(
            policy.split("## Executable subset", maxsplit=1)[1].split(
                "\n## ", maxsplit=1
            )[0]
        )

        self.assertIn("tools/check-story-slice-scope.py", subset)
        self.assertIn("does not judge whether a label is **correct**", subset)
        self.assertIn(
            "A green gate is evidence the record exists, never evidence the record "
            "is right.",
            subset,
        )

    def test_create_story_resolves_exactly_one_fail_closed_guard(self):
        workflow = resolve_workflow("bmad-create-story")

        facts = workflow["persistent_facts"]
        self.assertEqual(facts.count(POLICY_FACT), 1)
        self.assertEqual(facts.count(LESSONS_FACT), 1)

        directives = [
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(MARKER)
        ]
        self.assertEqual(len(directives), 1)
        directive = directives[0]
        self.assertIn("Classify every previous/historical story", directive)
        self.assertIn("ignore numeric adjacency", directive)
        self.assertIn("Historical Context Classification", directive)
        self.assertIn("Slice Proof", directive)
        self.assertIn("fail closed", directive)
        self.assertIn("ready-for-dev", directive)
        self.assertIn("sprint-status", directive)

    def test_policy_defines_all_classifications_and_narrow_reuse_proof(self):
        policy = POLICY.read_text(encoding="utf-8")
        classifications = markdown_list_items(policy, "Mandatory classification")
        creation_rules = markdown_list_items(policy, "Creation gate")

        self.assertIn(
            "`current-narrow-pattern` — only a focused implementation/test pattern "
            "that has been re-verified against current source; whole-story shape is "
            "not reused.",
            classifications,
        )
        self.assertIn(
            "`historical-reference-only` — dependency, decision, or evidence context.",
            classifications,
        )
        self.assertIn(
            "Select work from current epic intent and current code evidence, not "
            "numeric story adjacency.",
            creation_rules,
        )
        self.assertIn(
            "Add `Historical Context Classification` and `Slice Proof` sections to "
            "the generated story whenever any prior story influences it.",
            creation_rules,
        )

    def test_policy_blocks_anti_template_breadth_and_routes_split_ambiguity(self):
        policy = POLICY.read_text(encoding="utf-8")
        classifications = markdown_list_items(policy, "Mandatory classification")
        creation_rules = markdown_list_items(policy, "Creation gate")
        review_rules = markdown_list_items(policy, "Review gate")

        self.assertIn(
            "`anti-template` — broad, bundled, umbrella, checkpoint-heavy, "
            "superseded, alias-only, reserved, or explicitly guarded scope that must "
            "not shape a new story.",
            classifications,
        )
        self.assertIn(
            "Do not copy an anti-template's tasks, AC density, file list, or proof "
            "shape.",
            creation_rules,
        )
        self.assertIn(
            "Split multiple independently demonstrable outcomes into newly numbered "
            "stories before the story is registered at any status.",
            creation_rules,
        )
        self.assertIn(
            "A correction, split, or replan that creates stories must satisfy this "
            "policy for every story it creates. A split must not reproduce the shape "
            "it was executed to cure.",
            creation_rules,
        )
        self.assertIn(
            "Treat an unresolved violation as a Critical Miss: do not set "
            "`ready-for-dev` and do not update sprint status.",
            creation_rules,
        )
        self.assertIn(
            "Rate confirmed anti-template reuse or hidden multi-slice scope as "
            "`high`.",
            review_rules,
        )
        self.assertIn(
            "Route to `decision_needed` when the correct split requires a human scope "
            "choice; otherwise route an unambiguous correction to `patch`.",
            review_rules,
        )

    def test_policy_requires_independent_checkpoint_and_observable_proof(self):
        policy = POLICY.read_text(encoding="utf-8")
        classification_section = normalize_text(
            policy.split("## Mandatory classification", maxsplit=1)[1].split(
                "\n## ", maxsplit=1
            )[0]
        )
        creation_rules = markdown_list_items(policy, "Creation gate")
        review_rules = markdown_list_items(policy, "Review gate")

        self.assertIn(
            "unless current epics explicitly approve a narrower use.",
            classification_section,
        )
        self.assertIn(
            "An explicitly approved umbrella/checkpoint story may remain one tracking "
            "story only when every checkpoint has its own owner, evidence "
            "command/artifact, review state, and completion state.",
            creation_rules,
        )
        self.assertIn(
            "Confirm the implementation stays within one approved slice or "
            "independently proves every explicitly approved checkpoint.",
            review_rules,
        )
        self.assertIn(
            "Confirm externally observable proof is present wherever current artifacts "
            "require API, CLI, contract, trace, integration, or downstream-consumer "
            "proof.",
            review_rules,
        )

    def test_code_review_preserves_all_default_and_team_layers(self):
        workflow = resolve_workflow("bmad-code-review")

        facts = workflow["persistent_facts"]
        self.assertEqual(facts.count(POLICY_FACT), 1)
        self.assertEqual(facts.count(LESSONS_FACT), 1)

        directives = [
            step
            for step in workflow["activation_steps_append"]
            if step.startswith(MARKER)
        ]
        self.assertEqual(len(directives), 1)

        layers = workflow["review_layers"]
        layer_ids = [layer["id"] for layer in layers]
        skill_dir = REPO_ROOT / ".agents" / "skills" / "bmad-code-review"
        default_layers = load_workflow_config(skill_dir / "customize.toml")[
            "review_layers"
        ]
        team_layers = load_workflow_config(
            REPO_ROOT / "_bmad" / "custom" / "bmad-code-review.toml"
        )["review_layers"]
        for layer in [*default_layers, *team_layers]:
            self.assertEqual(layer_ids.count(layer["id"]), 1)

        guard = next(
            layer for layer in layers if layer["id"] == "historical-slice-guard"
        )
        instruction = normalize_text(guard["instruction"])
        self.assertEqual(guard["when"], 'Only when {review_mode} = "full".')
        for required_text in (
            "_bmad/custom/story-scope-guard.md",
            "{spec_file}",
            "{diff_output}",
            "historical anti-template",
            "Output only actionable Markdown findings",
            "violated policy rule",
            "spec location",
            "diff evidence",
            "requires a human",
            "split decision",
        ):
            self.assertIn(required_text, instruction)


if __name__ == "__main__":
    unittest.main()
