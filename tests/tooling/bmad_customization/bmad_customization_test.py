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
LEDGER_POLICY = REPO_ROOT / "_bmad" / "custom" / "story-phase-ledger.md"
LEDGER_SPEC = (
    REPO_ROOT
    / "_bmad-output"
    / "implementation-artifacts"
    / "spec-track-test-counts-in-story-phase-change-log.md"
)
ROOT_CONTEXT_BRIDGE = REPO_ROOT / "project-context.md"
CANONICAL_PROJECT_CONTEXT = REPO_ROOT / "_bmad-output" / "project-context.md"
EPICS = REPO_ROOT / "_bmad-output" / "planning-artifacts" / "epics.md"
SPRINT_STATUS = (
    REPO_ROOT / "_bmad-output" / "implementation-artifacts" / "sprint-status.yaml"
)
GENERATOR_CUSTOMIZATION = (
    REPO_ROOT / "_bmad" / "custom" / "bmad-generate-project-context.toml"
)
BMM_CONFIG = REPO_ROOT / "_bmad" / "bmm" / "config.yaml"
POLICY_FACT = "file:{project-root}/_bmad/custom/story-scope-guard.md"
LEDGER_POLICY_FACT = "file:{project-root}/_bmad/custom/story-phase-ledger.md"
LESSONS_FACT = "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md"
MARKER = "HISTORICAL_SLICE_GUARD:"
LEDGER_MARKER = "STORY_PHASE_LEDGER:"
SPEC_PROJECT_CONTEXT_FACT = "file:{project-root}/project-context.md"
GENERATOR_PROJECT_CONTEXT_FACT = "file:{project-root}/**/project-context.md"
PROJECT_CONTEXT_BRIDGE = """# Project Context Bridge

Resolve paths from the repository root. You MUST fully load
`_bmad-output/project-context.md` as foundational context before proceeding.
That canonical file is this repository's only project-context policy source.
This bridge contains forwarding and fail-closed controls, but no implementation
policy; it MUST NOT be expanded with implementation policy.

If `_bmad-output/project-context.md` is missing, unreadable, empty, or does not
contain the exact active `Tenant isolation requires attached negative evidence`
rule under `### Testing Rules`, HALT and report the failure. Do not proceed
without valid canonical context.

Project-context generators MUST NEVER update this bridge. They may read it, but
MUST update only `_bmad-output/project-context.md`.
"""
PROJECT_CONTEXT_WRITER_DIRECTIVE = (
    "PROJECT_CONTEXT_BRIDGE: Project-context discovery may read matched context "
    "files. Treat {project-root}/_bmad-output/project-context.md as the only "
    "project-context update/write target; all other discovered project-context "
    "files are read-only. Never update or rewrite "
    "{project-root}/project-context.md; it is the forwarding/control bridge."
)
CANONICAL_ATTACHED_EVIDENCE_RULE = (
    "- **Tenant isolation requires attached negative evidence** - any change to "
    "tenant/case routing, endpoint filters or auth claims, tenant status, "
    "index/key/graph selection, actor IDs, storage/query selectors, MCP "
    "authorization/execution, evidence scope display, verifier markers, "
    "attribution, or tenant-scoped data movement must name the affected surfaces "
    "and attach focused cross-tenant denial or fail-closed test names, command, "
    "and result to its story/spec plus completion or review record. Cite Story "
    "20.2 denial-before-dependency and Story 24.3 verifier/tenant-marker evidence "
    "when applicable, or link the newer canonical replacement. If proof cannot "
    "run, record an accepted blocker with owner, consequence, and reopen trigger. "
    "Do not close on happy-path, broad-suite, build-only, or refactor-green "
    "evidence alone."
)
EPICS_CARRY_FORWARD_GUARD = (
    "**Cross-tenant negative-evidence carry-forward (2026-07-06; broadened "
    "2026-07-16):** Any future scope-sensitive story, spec, refactor, fix, review "
    "patch, sprint correction, or implementation change—regardless of epic "
    "number—must keep cross-tenant negative validation evidence attached to the "
    "change instead of treating it as historical proof. Scope-sensitive includes "
    "tenant/case route grouping or versioning; endpoint filters or middleware; "
    "auth or claim normalization; tenant status guards; MCP tool executors or "
    "client calls; evidence-packet scope metadata or restrictive web rendering; "
    "tenant verifier logic or tenant markers; key/index/graph routing, actor IDs, "
    "storage selectors, or query builders; search/graph/case attribution; "
    "export/import or backup/restore; and any refactor that moves those paths. The "
    "story/spec and completion or review record must list the impacted surfaces, "
    "cite Story 20.2 denial-before-dependency and Story 24.3 verifier "
    "fail-closed/tenant-marker evidence when applicable (or link a newer canonical "
    "replacement), and record focused negative test names, command, and result. "
    "If proof cannot run, record an explicit accepted blocker with owner, "
    "consequence, and reopen trigger. A scope-sensitive change cannot close on "
    "happy-path, broad-suite, build-only, or refactor-green evidence alone."
)
SPRINT_ACTION = (
    'action: "Keep cross-tenant negative validation evidence attached to future '
    'scope-sensitive changes"'
)
SPEC_ACTIVATION_CONTRACT = """1. Resolve customization: `uv run {project-root}/_bmad/scripts/resolve_customization.py --skill {skill-root} --key workflow`. On failure, read `{skill-root}/customize.toml` directly.
2. Run `{workflow.activation_steps_prepend}`. Treat `{workflow.persistent_facts}` as foundational context (`file:` entries are loaded).
3. Load `{project-root}/_bmad/core/config.yaml` (and `config.user.yaml` if present), root level and `bmm` section. Resolve `{user_name}`, `{communication_language}`, `{document_output_language}`, `{planning_artifacts}`, `{project_name}`, `{date}`.
4. Detect mode. **Headless** when any of: no TTY, programmatic caller (another skill or non-interactive runner), or the first message pre-supplies all inputs and asks for an artifact path back. **Interactive** otherwise. In interactive mode, greet by `{user_name}` in `{communication_language}`, stay in that language, and mention that `bmad-party-mode` and `bmad-advanced-elicitation` are available for deeper exploration on any field.

Run `{workflow.activation_steps_append}`.

Activation is complete. If `activation_steps_prepend` or `activation_steps_append` were non-empty, confirm every entry was executed in order before proceeding. Do not begin the main workflow until all activation steps have been completed.
"""
GENERATOR_ACTIVATION_CONTRACT = """### Step 1: Resolve the Workflow Block

Run: `python3 {project-root}/_bmad/scripts/resolve_customization.py --skill {skill-root} --key workflow`

**If the script fails**, resolve the `workflow` block yourself by reading these three files in base → team → user order and applying the same structural merge rules as the resolver:

1. `{skill-root}/customize.toml` — defaults
2. `{project-root}/_bmad/custom/{skill-name}.toml` — team overrides
3. `{project-root}/_bmad/custom/{skill-name}.user.toml` — personal overrides

Any missing file is skipped. Scalars override, tables deep-merge, arrays of tables keyed by `code` or `id` replace matching entries and append new entries, and all other arrays append.

### Step 2: Execute Prepend Steps

Execute each entry in `{workflow.activation_steps_prepend}` in order before proceeding.

### Step 3: Load Persistent Facts

Treat every entry in `{workflow.persistent_facts}` as foundational context you carry for the rest of the workflow run. Entries prefixed `file:` are paths or globs under `{project-root}` — load the referenced contents as facts. All other entries are facts verbatim.

### Step 4: Load Config

Load config from `{project-root}/_bmad/bmm/config.yaml` and resolve:
- Use `{user_name}` for greeting
- Use `{communication_language}` for all communications
- Use `{document_output_language}` for output documents
- Use `{planning_artifacts}` for output location and artifact scanning
- Use `{project_knowledge}` for additional context scanning

### Step 5: Greet the User

Greet `{user_name}`, speaking in `{communication_language}`.

### Step 6: Execute Append Steps

Execute each entry in `{workflow.activation_steps_append}` in order.

Activation is complete. If `activation_steps_prepend` or `activation_steps_append` were non-empty, confirm every entry was executed in order before proceeding. Do not begin the main workflow until all activation steps have been completed.
"""
GENERATOR_CONTEXT_REFERENCES = {
    "SKILL.md": [
        "description: 'Create project-context.md with AI rules. Use when the user says \"generate project context\" or \"create project context\"'",
        "**Goal:** Create a concise, optimized `project-context.md` file containing critical rules, patterns, and guidelines that AI agents must follow when implementing code. This file focuses on unobvious details that LLMs need to be reminded of.",
        "- `output_file` = `{output_folder}/project-context.md`",
    ],
    "customize.toml": [
        '"file:{project-root}/**/project-context.md",',
        "# after the project-context.md file is optimized and saved. Override wins.",
    ],
    "steps/step-01-discover.md": [
        "- Look for file at `{project_knowledge}/project-context.md or {project-root}/**/project-context.md`",
        "Copy template from `../project-context-template.md` to `{output_folder}/project-context.md`",
    ],
    "steps/step-02-generate.md": [
        "When user selects 'C' for a category, append the content directly to `{output_folder}/project-context.md` using the structure from step 8.",
    ],
    "steps/step-03-complete.md": [
        "File saved to: `{output_folder}/project-context.md`",
        "`{output_folder}/project-context.md`",
    ],
}
LEDGER_EQUATION = re.compile(
    r"create baseline `(?P<baseline>\d+)`.*?"
    r"cumulative story `(?P<cumulative>[+-]\d+)`.*?"
    r"external same-suite `(?P<external>[+-]\d+)`.*?"
    r"observed final `(?P<final>\d+)`"
)


def normalize_text(value: str) -> str:
    lines = (line.strip().removeprefix("> ").strip() for line in value.splitlines())
    return " ".join(" ".join(lines).split())


def markdown_sections(value: str, heading: str) -> list[str]:
    level = len(heading) - len(heading.lstrip("#"))
    return re.findall(
        rf"(?ms)^{re.escape(heading)}[ \t]*\n(.*?)(?=^#{{1,{level}}} |\Z)",
        value,
    )


def active_lines_starting_with(value: str, prefix: str) -> list[str]:
    matches = []
    fence = None
    in_comment = False
    for raw_line in value.splitlines():
        line = raw_line
        if in_comment:
            if "-->" not in line:
                continue
            line = line.split("-->", 1)[1]
            in_comment = False
        if "<!--" in line:
            visible, comment = line.split("<!--", 1)
            line = visible
            if "-->" not in comment:
                in_comment = True

        fence_match = re.match(r"^ {0,3}(`{3,}|~{3,})", line)
        if fence is not None:
            if (
                fence_match
                and fence_match.group(1)[0] == fence[0]
                and len(fence_match.group(1)) >= len(fence)
            ):
                fence = None
            continue
        if fence_match:
            fence = fence_match.group(1)
            continue
        if line.startswith(("    ", "\t")):
            continue
        if line.startswith(prefix):
            matches.append(normalize_text(line))
    return matches


def resolve_workflow(skill_name: str, surface: str = ".agents") -> dict:
    skill_dir = REPO_ROOT / surface / "skills" / skill_name
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
    def test_cross_tenant_project_context_delivery_contract(self):
        self.assertEqual(
            ROOT_CONTEXT_BRIDGE.read_text(encoding="utf-8"),
            PROJECT_CONTEXT_BRIDGE,
        )

        canonical_sections = markdown_sections(
            CANONICAL_PROJECT_CONTEXT.read_text(encoding="utf-8"),
            "### Testing Rules",
        )
        self.assertEqual(len(canonical_sections), 1)
        canonical_rules = active_lines_starting_with(
            canonical_sections[0],
            "- **Tenant isolation requires attached negative evidence**",
        )
        self.assertEqual(canonical_rules, [CANONICAL_ATTACHED_EVIDENCE_RULE])

        epics_sections = markdown_sections(
            EPICS.read_text(encoding="utf-8"),
            "## Phase: Post-MVP — Audit Remediation (2026-07-04)",
        )
        self.assertEqual(len(epics_sections), 1)
        epics_guards = active_lines_starting_with(
            epics_sections[0],
            "**Cross-tenant negative-evidence carry-forward",
        )
        self.assertEqual(epics_guards, [EPICS_CARRY_FORWARD_GUARD])

        sprint_status = SPRINT_STATUS.read_text(encoding="utf-8")
        self.assertEqual(sprint_status.count("\naction_items:\n"), 1)
        action_section = sprint_status.split("\naction_items:\n", 1)[1]
        action_blocks = re.findall(
            r"(?ms)^  - epic: .*?(?=^  - epic: |\Z)",
            action_section,
        )
        matching_actions = [
            block
            for block in action_blocks
            if SPRINT_ACTION in [normalize_text(line) for line in block.splitlines()]
        ]
        self.assertEqual(len(matching_actions), 1)
        action_lines = [
            normalize_text(line) for line in matching_actions[0].splitlines()
        ]
        self.assertEqual(action_lines[0], "- epic: 0")
        statuses = [line for line in action_lines if line.startswith("status:")]
        self.assertEqual(len(statuses), 1)
        self.assertEqual(statuses[0].split("#", 1)[0].strip(), "status: in-progress")

        for surface in (".agents", ".claude"):
            with self.subTest(surface=surface, skill="bmad-spec"):
                skill_dir = REPO_ROOT / surface / "skills" / "bmad-spec"
                defaults = tomllib.loads(
                    (skill_dir / "customize.toml").read_text(encoding="utf-8")
                )["workflow"]
                self.assertEqual(
                    defaults["persistent_facts"],
                    [SPEC_PROJECT_CONTEXT_FACT],
                )
                self.assertEqual(
                    resolve_workflow("bmad-spec", surface)["persistent_facts"],
                    [SPEC_PROJECT_CONTEXT_FACT],
                )

                skill = (skill_dir / "SKILL.md").read_text(encoding="utf-8")
                activation_sections = markdown_sections(skill, "## On Activation")
                self.assertEqual(len(activation_sections), 1)
                self.assertEqual(
                    normalize_text(activation_sections[0]),
                    normalize_text(SPEC_ACTIVATION_CONTRACT),
                )

        generator_customization = tomllib.loads(
            GENERATOR_CUSTOMIZATION.read_text(encoding="utf-8")
        )["workflow"]
        self.assertEqual(
            generator_customization["activation_steps_append"],
            [PROJECT_CONTEXT_WRITER_DIRECTIVE],
        )
        output_folders = re.findall(
            r'(?m)^output_folder: "([^"]+)"[ \t]*$',
            BMM_CONFIG.read_text(encoding="utf-8"),
        )
        self.assertEqual(output_folders, ["{project-root}/_bmad-output"])

        for surface in (".agents", ".claude"):
            with self.subTest(surface=surface, skill="bmad-generate-project-context"):
                skill_dir = (
                    REPO_ROOT / surface / "skills" / "bmad-generate-project-context"
                )
                defaults = tomllib.loads(
                    (skill_dir / "customize.toml").read_text(encoding="utf-8")
                )["workflow"]
                self.assertEqual(
                    defaults["persistent_facts"],
                    [GENERATOR_PROJECT_CONTEXT_FACT],
                )
                self.assertEqual(defaults["activation_steps_append"], [])

                workflow = resolve_workflow(
                    "bmad-generate-project-context",
                    surface,
                )
                self.assertEqual(
                    workflow["activation_steps_append"],
                    [PROJECT_CONTEXT_WRITER_DIRECTIVE],
                )
                self.assertNotIn(
                    "only project-context read and update target",
                    workflow["activation_steps_append"][0],
                )

                skill = (skill_dir / "SKILL.md").read_text(encoding="utf-8")
                activation_sections = markdown_sections(skill, "## On Activation")
                self.assertEqual(len(activation_sections), 1)
                self.assertEqual(
                    normalize_text(activation_sections[0]),
                    normalize_text(GENERATOR_ACTIVATION_CONTRACT),
                )

                references = {}
                for path in sorted(skill_dir.rglob("*")):
                    if not path.is_file() or path.suffix not in {".md", ".toml"}:
                        continue
                    lines = [
                        line.strip()
                        for line in path.read_text(encoding="utf-8").splitlines()
                        if "project-context.md" in line
                    ]
                    if lines:
                        references[path.relative_to(skill_dir).as_posix()] = lines
                self.assertEqual(references, GENERATOR_CONTEXT_REFERENCES)

    def test_policy_contract_extractor_rejects_inactive_markdown(self):
        rule = CANONICAL_ATTACHED_EVIDENCE_RULE
        for inactive in (
            f"```markdown\n{rule}\n```",
            f"~~~markdown\n{rule}\n~~~",
            f"<!--\n{rule}\n-->",
            f"    {rule}",
            f"\t{rule}",
        ):
            with self.subTest(inactive=inactive.splitlines()[0]):
                self.assertEqual(
                    active_lines_starting_with(
                        inactive,
                        "- **Tenant isolation requires attached negative evidence**",
                    ),
                    [],
                )

        retired_document = (
            "### Retired Rules\n"
            f"{rule}\n\n"
            "### Testing Rules\n"
            "- **Another rule** - remains active.\n"
        )
        testing_sections = markdown_sections(retired_document, "### Testing Rules")
        self.assertEqual(len(testing_sections), 1)
        self.assertEqual(
            active_lines_starting_with(
                testing_sections[0],
                "- **Tenant isolation requires attached negative evidence**",
            ),
            [],
        )

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
