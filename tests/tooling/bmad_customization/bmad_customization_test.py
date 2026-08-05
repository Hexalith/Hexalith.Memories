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
PROJECT_CONTEXT_CUSTOMIZATION = (
    REPO_ROOT / "_bmad" / "custom" / "bmad-project-context.toml"
)
BMM_CONFIG = REPO_ROOT / "_bmad" / "bmm" / "config.yaml"
POLICY_FACT = "file:{project-root}/_bmad/custom/story-scope-guard.md"
LEDGER_POLICY_FACT = "file:{project-root}/_bmad/custom/story-phase-ledger.md"
LESSONS_FACT = "file:{project-root}/_bmad-output/process-notes/story-creation-lessons.md"
MARKER = "HISTORICAL_SLICE_GUARD:"
LEDGER_MARKER = "STORY_PHASE_LEDGER:"
RUNTIME_CHECKLIST_POLICY = (
    REPO_ROOT / "_bmad" / "custom" / "remediation-runtime-checklist.md"
)
RUNTIME_CHECKLIST_FACT = (
    "file:{project-root}/_bmad/custom/remediation-runtime-checklist.md"
)
RUNTIME_CHECKLIST_MARKER = "REMEDIATION_RUNTIME_CHECKLIST:"
AC_VERIFICATION_POLICY = REPO_ROOT / "_bmad" / "custom" / "epic-ac-verification.md"
AC_VERIFICATION_FACT = "file:{project-root}/_bmad/custom/epic-ac-verification.md"
AC_VERIFICATION_MARKER = "EPIC_AC_VERIFICATION:"
READINESS_GATE_MARKER = "STORY_REVIEW_READINESS_GATE:"
# Only the two phases that can advance a story's status carry the gate. Creation
# cannot: a story has no File List or diff to reconcile at `backlog`.
READINESS_GATE_ROUTES = ("bmad-dev-story", "bmad-code-review")
SPEC_PROJECT_CONTEXT_FACT = "file:{project-root}/project-context.md"
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
EPICS_AC_PREFLIGHT_GUARD = (
    "**Audit-anchor and AC-claim preflight (2026-07-04; broadened 2026-07-28; "
    "bound at authoring and registration 2026-07-28):** "
    "Before any story is authored, registered, selected, created, or "
    "implemented—regardless of epic number, and at any status, including "
    "`backlog`—re-verify against the current repository both the code anchors and "
    "implementation-state assumptions that story cites and every verifiable claim "
    "in the epic, PRD, architecture, or audit text it inherits: quantitative "
    "counts, existence and absence assertions, behavioral descriptions, and file, "
    "symbol, or line locations. Epic acceptance text is planning intent recorded "
    "at a point in time and is advisory until re-derived; where code and planning "
    "text disagree, the code wins. Story files must record the re-verification "
    "date, moved or renamed anchors, how the implementation adapts, and per claim "
    "a re-runnable command with a `confirmed`, `corrected`, or `unverifiable` "
    "verdict, as specified by `_bmad/custom/epic-ac-verification.md`. A corrected "
    "claim must also correct this file or carry a dated correction note here, "
    "because a story that fixes only its own text leaves the planning artifact "
    "wrong for the next reader; a correction that changes scope, epic intent, or "
    "a ratified decision is escalated for a human decision instead of absorbed. "
    'Story 25.3\'s "60 server literals", Story 25.5\'s "no `Client.Rest` '
    'reference", and Story 25.6\'s "double authorization" are the recorded '
    "exemplars of claims that were false against the code. A story created by an "
    "approved sprint change is bound at the moment that change registers it, not "
    "at the moment it is later selected."
)
# Every route that authors or registers a story, or writes an epic acceptance
# claim, must load the epic-AC-verification policy. Mirrors AUTHORING_ROUTES in
# historical_slice_guard_test.py; the guard created on 2026-07-28 covered only
# the first three and was widened by
# sprint-change-proposal-2026-07-28-epic-ac-verification-route-and-binding-coverage.md.
AC_AUTHORING_ROUTES = (
    "bmad-create-story",
    "bmad-dev-story",
    "bmad-code-review",
    "bmad-correct-course",
    "bmad-create-epics-and-stories",
    "bmad-spec",
    "bmad-sprint-planning",
)
# bmad-spec is deliberately excluded from the policy-fact list: the cross-tenant
# project-context delivery contract pins its resolved persistent_facts to exactly
# the bridge fact so the bridge's fail-closed control cannot be diluted. It
# receives the policy through its activation directive instead, which names the
# file to read. See _bmad/custom/bmad-spec.toml.
AC_AUTHORING_ROUTES_WITH_POLICY_FACT = tuple(
    route for route in AC_AUTHORING_ROUTES if route != "bmad-spec"
)
SPRINT_ACTION = (
    'action: "Keep cross-tenant negative validation evidence attached to future '
    'scope-sensitive changes"'
)
SPEC_ACTIVATION_CONTRACT = """1. Resolve customization: `uv run {project-root}/_bmad/scripts/resolve_customization.py --skill {skill-root} --key workflow`. On failure, read `{skill-root}/customize.toml` directly.
2. Run `{workflow.activation_steps_prepend}`. Treat `{workflow.persistent_facts}` as foundational context (`file:` entries are loaded).
3. Resolve config: `uv run {project-root}/_bmad/scripts/resolve_config.py --project-root {project-root}` (merges `_bmad/config.toml`, `_bmad/config.user.toml`, and the `_bmad/custom/` overrides). From the merged JSON resolve `{user_name}`, `{communication_language}`, `{document_output_language}`, `{project_name}`, `{output_folder}` (under `core`), and `{date}`.
4. Detect mode. **Headless** when any of: no TTY, programmatic caller (another skill or non-interactive runner), or the first message pre-supplies all inputs and asks for an artifact path back. **Interactive** otherwise. In interactive mode, greet by `{user_name}` in `{communication_language}`, stay in that language, and mention that `bmad-party-mode` and `bmad-advanced-elicitation` are available for deeper exploration on any field.

Run `{workflow.activation_steps_append}`.

Activation is complete. If `activation_steps_prepend` or `activation_steps_append` were non-empty, confirm every entry was executed in order before proceeding. Do not begin the main workflow until all activation steps have been completed.
"""
PROJECT_CONTEXT_ACTIVATION_CONTRACT = """1. Resolve customization: `uv run {project-root}/_bmad/scripts/resolve_customization.py --skill {skill-root} --key workflow`. On failure, read `{skill-root}/customize.toml` directly and use defaults. Execute `{workflow.activation_steps_prepend}`; treat `{workflow.persistent_facts}` entries as standing context (`file:` = paths/globs to load, others verbatim).
2. Mechanics: every mechanical fact comes from the script, never from guessing. If `{project-root}/_bmad/scripts/context.py` is missing (standalone repo), run `uv run {skill-root}/scripts/context.py bootstrap` once — it installs itself there. All later calls: `uv run {project-root}/_bmad/scripts/context.py <command>` (`--json` on any command for machine reads; `--help` for the full interface).
3. Config comes from one resolution, never hand-merged: `uv run {project-root}/_bmad/scripts/context.py config --json`. It delegates to the installed BMad resolver (`resolve_config.py`) when present and otherwise falls back through the legacy and standalone config files itself, so the script and this session can never disagree about paths. Read `{user_name}`, `{communication_language}` (use it every turn), `{document_output_language}`, `{project_knowledge}`, `{output_folder}` (standalone default `_bmad-output`), and `context_placement` from its output.
4. **First run** (no kernel at `{project_knowledge}/kernel.md`), interactive only: load `references/placement.md` and settle the bundle location and placement there. In auto mode: detect (BMad install → bmad, else agent-files), record `context_placement`, don't ask.
5. Init or resume the memlog at `{project_knowledge}/.memlog.md` (`uv run {project-root}/_bmad/scripts/memlog.py init --path ...` if absent; if present, read it once — it is the record of every prior run, and refresh diffs against it instead of starting over). If `memlog.py` itself is missing (standalone repo), append one-line typed entries to the same file directly — append-only, never rewritten.
6. Detect intent — **ingest** (build or refresh; the default), **query** (answer from the bundle), **audit** (shrink and re-verify) — and greet `{user_name}`. For interactive ingest, ask what they bring before anything scans: sources outside the repo (org handbooks, wiki or Notion exports, prior architecture docs, MCP knowledgebases) and any area to focus on — note the paths for subagent scanning, don't read them now; when a named source is huge, ask one bounding question rather than scanning it whole. Fold `{workflow.external_sources}` entries into the same source list. Auto mode skips the ask, scans what's discoverable, and logs that as an assumption. Execute `{workflow.activation_steps_append}`.
"""
DEPRECATED_GENERATE_FORWARD_PATTERN = re.compile(
    r"(?is)invoke [`']bmad-project-context[`'] with .*?\bingest\b.*? intent"
)
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


def run_resolve_workflow(skill_name: str, surface: str = ".agents") -> subprocess.CompletedProcess:
    skill_dir = REPO_ROOT / surface / "skills" / skill_name
    return subprocess.run(
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


def resolve_workflow(skill_name: str, surface: str = ".agents") -> dict:
    result = run_resolve_workflow(skill_name, surface)
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

        project_context_customization = tomllib.loads(
            PROJECT_CONTEXT_CUSTOMIZATION.read_text(encoding="utf-8")
        )["workflow"]
        self.assertEqual(
            project_context_customization["activation_steps_append"],
            [PROJECT_CONTEXT_WRITER_DIRECTIVE],
        )
        output_folders = re.findall(
            r'(?m)^output_folder: "([^"]+)"[ \t]*$',
            BMM_CONFIG.read_text(encoding="utf-8"),
        )
        self.assertEqual(output_folders, ["{project-root}/_bmad-output"])
        self.assertFalse(
            (
                REPO_ROOT / "_bmad" / "custom" / "bmad-generate-project-context.toml"
            ).exists()
        )

        for surface in (".agents", ".claude"):
            with self.subTest(surface=surface, skill="bmad-project-context"):
                skill_dir = REPO_ROOT / surface / "skills" / "bmad-project-context"
                defaults = tomllib.loads(
                    (skill_dir / "customize.toml").read_text(encoding="utf-8")
                )["workflow"]
                self.assertEqual(defaults["persistent_facts"], [])
                self.assertEqual(defaults["activation_steps_append"], [])

                workflow = resolve_workflow("bmad-project-context", surface)
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
                    normalize_text(PROJECT_CONTEXT_ACTIVATION_CONTRACT),
                )

        for surface in (".agents", ".claude"):
            with self.subTest(surface=surface, skill="bmad-generate-project-context"):
                skill_dir = (
                    REPO_ROOT / surface / "skills" / "bmad-generate-project-context"
                )
                self.assertFalse((skill_dir / "customize.toml").exists())
                skill = (skill_dir / "SKILL.md").read_text(encoding="utf-8")
                self.assertRegex(skill, r"(?m)^#\s+DEPRECATED")
                self.assertRegex(skill, DEPRECATED_GENERATE_FORWARD_PATTERN)
                self.assertEqual(markdown_sections(skill, "## On Activation"), [])

                result = run_resolve_workflow(
                    "bmad-generate-project-context",
                    surface,
                )
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("customize.toml", result.stderr)

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
            # Admitted to the canonical set 2026-07-28 by the Administrator, during Story 31.1's
            # second-pass code review: an approved sprint change landing between creation and
            # development changes the File List and can change counts, and had no row of its own.
            "`correct-course`",
            "`dev-story`",
            "`qa-gap-closure`",
            "`code-review`",
        ):
            self.assertIn(phase_name, policy)
        self.assertIn("`correct-course` was admitted to the canonical set", policy)
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

    def test_runtime_checklist_wired_into_lifecycle_workflows(self):
        # Each skill's directive must carry its own fail-closed gate, not just the
        # marker prefix — otherwise a gutted or cross-wired directive still passes.
        skill_directive_phrases = {
            "bmad-create-story": ("ready-for-dev", "not-applicable note"),
            "bmad-dev-story": ("before setting review", "Re-derive applicability"),
            "bmad-code-review": (
                "fail-closed blocker for done",
                "independently re-derive",
            ),
        }
        for surface in (".agents", ".claude"):
            for skill_name, phrases in skill_directive_phrases.items():
                with self.subTest(surface=surface, skill_name=skill_name):
                    workflow = resolve_workflow(skill_name, surface)
                    self.assertIn(
                        RUNTIME_CHECKLIST_FACT,
                        workflow["persistent_facts"],
                    )
                    checklist_directives = [
                        step
                        for step in workflow["activation_steps_append"]
                        if step.startswith(RUNTIME_CHECKLIST_MARKER)
                    ]
                    self.assertEqual(len(checklist_directives), 1)
                    for phrase in phrases:
                        self.assertIn(phrase, checklist_directives[0])
                    # Injecting the checklist must not clobber the phase-ledger guard.
                    ledger_directives = [
                        step
                        for step in workflow["activation_steps_append"]
                        if step.startswith(LEDGER_MARKER)
                    ]
                    self.assertEqual(len(ledger_directives), 1)

    def test_runtime_checklist_preserves_create_and_review_guards(self):
        create = resolve_workflow("bmad-create-story")
        for fact in (POLICY_FACT, LEDGER_POLICY_FACT, LESSONS_FACT):
            self.assertIn(fact, create["persistent_facts"])
        self.assertEqual(
            len(
                [
                    step
                    for step in create["activation_steps_append"]
                    if step.startswith(MARKER)
                ]
            ),
            1,
        )

        review = resolve_workflow("bmad-code-review")
        self.assertIn(RUNTIME_CHECKLIST_FACT, review["persistent_facts"])
        layer_ids = [layer["id"] for layer in review["review_layers"]]
        for layer_id in (
            "historical-slice-guard",
            "story-phase-ledger",
            "blind-hunter",
            "edge-case-hunter",
            "verification-gap",
            "acceptance-auditor",
        ):
            self.assertEqual(layer_ids.count(layer_id), 1)

    def test_runtime_checklist_policy_defines_categories_and_gates(self):
        policy = normalize_text(
            RUNTIME_CHECKLIST_POLICY.read_text(encoding="utf-8")
        )
        for category in (
            "Dapr workflow activity registration",
            "Observed child workflows",
            "Owner-checked cleanup",
            "Rollback marker and staging-artifact preservation",
            "File List reconciliation",
        ):
            self.assertIn(category, policy)
        # File List reconciliation defers to the phase ledger, not duplicated here.
        self.assertIn("story-phase-ledger.md", policy)
        # Self-scoping applicability with an explicit not-applicable escape.
        self.assertIn(
            "not applicable — no workflow/runtime surface touched",
            policy,
        )
        # Fail-closed creation and review gates.
        self.assertIn("## Creation gate", policy)
        self.assertIn("## Review gate", policy)
        self.assertIn("ready-for-dev", policy)
        self.assertIn("decision_needed", policy)
        self.assertIn("blocks `done`", policy)
        # Obligation sentences must survive, not just headings and keywords.
        self.assertIn("verify ownership", policy)
        self.assertIn("never overwritten", policy)
        self.assertIn("Independently re-derive applicability", policy)
        self.assertIn("not re-specified or separately tested here", policy)

    def test_epic_ac_verification_wired_into_lifecycle_workflows(self):
        # Each skill's directive must carry its own fail-closed gate, not just the
        # marker prefix — otherwise a gutted or cross-wired directive still passes.
        skill_directive_phrases = {
            "bmad-create-story": (
                "before drafting acceptance criteria",
                "ready-for-dev",
            ),
            "bmad-dev-story": (
                "re-derive any verdict the implementation contradicts",
                "before setting review",
            ),
            "bmad-code-review": (
                "Independently re-run the commands",
                "fail-closed blocker for done",
            ),
            "bmad-correct-course": (
                "every claim the proposal itself asserts",
                "before recording an approval while any verifiable claim lacks a "
                "verdict",
            ),
            "bmad-create-epics-and-stories": (
                "where epic acceptance text originates",
                "before registering the generated epic or story list",
            ),
            "bmad-spec": (
                "freezes intent into a machine contract",
                # No persistent fact on this route, so the directive itself must
                # name the file to read or the policy never reaches the agent.
                "{project-root}/_bmad/custom/epic-ac-verification.md",
            ),
            "bmad-sprint-planning": (
                "do not promote or re-status a story",
                "rather than writing it into sprint-status.yaml",
            ),
        }
        # Every authoring route is covered; a route added to the tuple without a
        # pinned phrase pair fails here rather than passing silently.
        self.assertEqual(
            tuple(skill_directive_phrases),
            AC_AUTHORING_ROUTES,
        )
        # Routes carrying each sibling guard, so the non-clobber check below is
        # asserted where the sibling actually applies rather than everywhere.
        ledger_routes = {"bmad-create-story", "bmad-dev-story", "bmad-code-review"}
        slice_routes = set(AC_AUTHORING_ROUTES) - {"bmad-dev-story"}

        for surface in (".agents", ".claude"):
            for skill_name, phrases in skill_directive_phrases.items():
                with self.subTest(surface=surface, skill_name=skill_name):
                    workflow = resolve_workflow(skill_name, surface)
                    if skill_name in AC_AUTHORING_ROUTES_WITH_POLICY_FACT:
                        self.assertIn(
                            AC_VERIFICATION_FACT,
                            workflow["persistent_facts"],
                        )
                    directives = [
                        step
                        for step in workflow["activation_steps_append"]
                        if step.startswith(AC_VERIFICATION_MARKER)
                    ]
                    self.assertEqual(len(directives), 1)
                    for phrase in phrases:
                        self.assertIn(phrase, directives[0])
                    # Adding this guard must not clobber the sibling guards.
                    sibling_markers = []
                    if skill_name in ledger_routes:
                        sibling_markers += [LEDGER_MARKER, RUNTIME_CHECKLIST_MARKER]
                    if skill_name in slice_routes:
                        sibling_markers.append(MARKER)
                    for sibling_marker in sibling_markers:
                        siblings = [
                            step
                            for step in workflow["activation_steps_append"]
                            if step.startswith(sibling_marker)
                        ]
                        self.assertEqual(len(siblings), 1)

    def test_story_review_readiness_gate_wired_into_status_advancing_workflows(self):
        # Assert the directive BODY, not just the marker: a gutted or cross-wired
        # directive still carries the prefix.
        skill_directive_phrases = {
            "bmad-dev-story": (
                "Before setting review, run",
                "tools/check-story-review-readiness.py",
                "--derive-cumulative",
                "Exit 1 is a fail-closed blocker",
                # The vacuous-pass trap the sibling scope gate shipped with.
                "vacuous no-op rather than a pass",
            ),
            "bmad-code-review": (
                "re-run",
                "independently rather than accepting the dev-story citation",
                "decision_needed",
                "fail-closed blocker for `done`",
                "floor, never a ceiling",
            ),
        }
        self.assertEqual(tuple(skill_directive_phrases), READINESS_GATE_ROUTES)

        for surface in (".agents", ".claude"):
            for skill_name, phrases in skill_directive_phrases.items():
                with self.subTest(surface=surface, skill_name=skill_name):
                    workflow = resolve_workflow(skill_name, surface)
                    directives = [
                        step
                        for step in workflow["activation_steps_append"]
                        if step.startswith(READINESS_GATE_MARKER)
                    ]
                    self.assertEqual(len(directives), 1)
                    for phrase in phrases:
                        self.assertIn(phrase, directives[0])
                    # Appending this guard must not clobber any sibling.
                    for sibling_marker in (
                        LEDGER_MARKER,
                        RUNTIME_CHECKLIST_MARKER,
                        AC_VERIFICATION_MARKER,
                    ):
                        siblings = [
                            step
                            for step in workflow["activation_steps_append"]
                            if step.startswith(sibling_marker)
                        ]
                        self.assertEqual(len(siblings), 1, sibling_marker)

    def test_story_review_readiness_gate_absent_from_creation_route(self):
        """Creation has no File List or diff to reconcile, so the gate must not bind there.

        Wiring it into `bmad-create-story` would make every new story fail on an
        empty changed set, which the verifier treats as fail-closed.
        """
        for surface in (".agents", ".claude"):
            with self.subTest(surface=surface):
                workflow = resolve_workflow("bmad-create-story", surface)
                self.assertEqual(
                    [
                        step
                        for step in workflow["activation_steps_append"]
                        if step.startswith(READINESS_GATE_MARKER)
                    ],
                    [],
                )

    def test_phase_ledger_policy_defines_evidence_table_and_executable_gate(self):
        policy = (REPO_ROOT / "_bmad" / "custom" / "story-phase-ledger.md").read_text(
            encoding="utf-8"
        )
        for heading in (
            "## Evidence-Table Status Reconciliation",
            "## Executable Gate",
            "### Declared Exclusions",
        ):
            self.assertIn(heading, policy)
        # The evidence-row rule and its two status triggers.
        self.assertIn("`Review status` or `Review state` column", policy)
        self.assertIn("no row may remain\n`pending` or dateless", policy)
        # The scope-limit paragraph. Without it a green gate becomes the next
        # "reviews stopped looking" failure.
        self.assertIn("A green gate is a floor, never a\nceiling", policy)
        self.assertIn("does NOT verify count arithmetic", policy)
        # The two deliberate limits must stay stated rather than implied.
        self.assertIn("default branch", policy)
        self.assertIn("empty changed set", policy)
        # The withdrawn File List / File Scope check must stay withdrawn, with
        # its reason, so a later reader does not reinstate it.
        self.assertIn("no `File List` / `File Scope` set-agreement check", policy)
        self.assertIn("Scope-Override:", policy)
        # The review gate must name the executable command.
        self.assertIn("tools/check-story-review-readiness.py", policy)

    def test_code_review_ledger_layer_audits_evidence_rows(self):
        for surface in (".agents", ".claude"):
            with self.subTest(surface=surface):
                workflow = resolve_workflow("bmad-code-review", surface)
                layers = {layer["id"]: layer for layer in workflow["review_layers"]}
                self.assertIn("story-phase-ledger", layers)
                instruction = layers["story-phase-ledger"]["instruction"]
                self.assertIn("`Review status` or `Review state` column", instruction)
                self.assertIn("`pending` or dateless", instruction)
                # Extending this layer must not have displaced any sibling layer.
                self.assertEqual(len(workflow["review_layers"]), 6)

    def test_epic_ac_verification_binds_at_authoring_not_only_at_ready_for_dev(self):
        """A claim registered at `backlog` must already carry its verdict.

        The guard was created gating on `ready-for-dev`, which is the same
        backlog exemption DW 27.3-CR16 exploited for story slice scope.
        """
        policy = AC_VERIFICATION_POLICY.read_text(encoding="utf-8")

        canonical = normalize_text(
            policy.split("## Canonical story section", maxsplit=1)[1].split(
                "\n## ", maxsplit=1
            )[0]
        )
        self.assertIn("authored or registered", canonical)
        self.assertIn("at any status, including `backlog`", canonical)
        self.assertIn(
            "`ready-for-dev` is a second, stricter checkpoint, not the first one.",
            canonical,
        )
        self.assertIn(
            "it does not become compliant by not being selected yet.",
            canonical,
        )

        creation = normalize_text(
            policy.split("## Creation gate", maxsplit=1)[1].split("\n## ", maxsplit=1)[
                0
            ]
        )
        # The gate must name itself route-independent, and the fail-closed bullet
        # must block the write itself, not only the ready-for-dev transition.
        self.assertIn(
            "binds every route that authors or registers a story or an epic "
            "acceptance claim",
            creation,
        )
        self.assertIn("The route that writes a claim owns verifying it", creation)
        self.assertIn(
            "do not write a verifiable claim into a story file, `epics.md`, or "
            "`sprint-status.yaml` at any status",
            creation,
        )

    def test_spec_route_receives_ac_policy_without_diluting_context_bridge(self):
        """bmad-spec must get the policy by directive, not by persistent fact.

        The cross-tenant project-context delivery contract pins this route's
        resolved persistent_facts to exactly the bridge fact. The policy is
        delivered through the activation directive, which names the file.
        """
        for surface in (".agents", ".claude"):
            with self.subTest(surface=surface):
                workflow = resolve_workflow("bmad-spec", surface)

                self.assertEqual(
                    workflow["persistent_facts"],
                    [SPEC_PROJECT_CONTEXT_FACT],
                )
                self.assertNotIn(AC_VERIFICATION_FACT, workflow["persistent_facts"])

                directives = [
                    step
                    for step in workflow["activation_steps_append"]
                    if step.startswith(AC_VERIFICATION_MARKER)
                ]
                self.assertEqual(len(directives), 1)
                # Without a persistent fact, the directive is the only delivery
                # path, so it must name the policy file explicitly.
                self.assertIn(
                    "{project-root}/_bmad/custom/epic-ac-verification.md",
                    directives[0],
                )

    def test_ac_authoring_routes_retain_generated_defaults(self):
        """Widening route coverage must not drop any generated default."""
        spec_workflow = resolve_workflow("bmad-spec")
        self.assertEqual(spec_workflow.get("spec_filename"), "SPEC.md")
        self.assertTrue(spec_workflow.get("spec_template"))

        for skill_name in AC_AUTHORING_ROUTES_WITH_POLICY_FACT:
            with self.subTest(skill_name=skill_name):
                workflow = resolve_workflow(skill_name)
                # The project-context bridge fact must survive on every route.
                self.assertTrue(
                    any(
                        "project-context.md" in fact
                        for fact in workflow["persistent_facts"]
                    )
                )

    def test_epic_ac_verification_policy_defines_claims_verdicts_and_gates(self):
        policy = normalize_text(AC_VERIFICATION_POLICY.read_text(encoding="utf-8"))

        # The canonical story section and its table header must survive edits.
        self.assertIn("### Epic AC Verification", policy)
        self.assertIn(
            "| Epic claim | Class | Command / evidence | Observed | Verdict |",
            policy,
        )
        for claim_class in (
            "**Quantitative**",
            "**Existence and absence**",
            "**Behavioral**",
            "**Location**",
        ):
            self.assertIn(claim_class, policy)
        for verdict in ("`confirmed`", "`corrected`", "`unverifiable`"):
            self.assertIn(verdict, policy)
        # The three Epic 25 claims this policy exists to catch.
        for regression in (
            "60 server literals",
            "`Client.Rest`",
            "double authorization",
        ):
            self.assertIn(regression, policy)
        # Explicit escape hatch, mirroring the runtime checklist's not-applicable note.
        self.assertIn(
            "epic AC verification: no verifiable claim in inherited epic text",
            policy,
        )
        # Fail-closed creation, development, and review gates.
        self.assertIn("## Creation gate", policy)
        self.assertIn("## Development gate", policy)
        self.assertIn("## Review gate", policy)
        for gated_status in ("`ready-for-dev`", "`review`", "`done`"):
            self.assertIn(gated_status, policy)
        # Obligation sentences must survive, not just headings and keywords.
        self.assertIn("a command another agent can re-run", policy)
        self.assertIn("not discharged by fixing the story alone", policy)
        self.assertIn("Escalate for a human decision", policy)
        self.assertIn("accept the author's verdict unchallenged", policy)
        self.assertIn("blocks `done`", policy)

        # The planning-side preflight must stay epic-number-independent and point
        # at this policy; a single guard paragraph, not two overlapping ones.
        epics_sections = markdown_sections(
            EPICS.read_text(encoding="utf-8"),
            "## Phase: Post-MVP — Audit Remediation (2026-07-04)",
        )
        self.assertEqual(len(epics_sections), 1)
        preflight_guards = active_lines_starting_with(
            epics_sections[0],
            "**Audit-anchor",
        )
        self.assertEqual(preflight_guards, [EPICS_AC_PREFLIGHT_GUARD])


if __name__ == "__main__":
    unittest.main()
