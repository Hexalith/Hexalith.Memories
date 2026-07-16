---
change_trigger: "Strengthen story creation and review checks so historical broad slices are not reused as templates"
mode: batch
status: approved-implemented
project: Hexalith.Memories
date: 2026-07-06
scope_classification: moderate
---

# Sprint Change Proposal: Historical Slice Story Guard

Date: 2026-07-06
Project: Hexalith.Memories
Scope: Moderate process correction affecting story creation and review workflows.

## 1. Issue Summary

The planning artifacts already identify Stories 1.2, 1.5, and 1.6 as historical broad technical or bundled infrastructure slices. `epics.md` says they are not valid patterns for future story creation and that reopened or analogous work must be split into independently demonstrable vertical stories with observable evidence.

The active create-story and review workflows do not force that rule at the point where future stories are generated or audited. As a result, the workflow can still treat a previous broad story as useful "previous story intelligence" and accidentally copy its scope shape into a new implementation story.

Evidence:

- `_bmad-output/planning-artifacts/epics.md` contains the binding Epic 1 implementation-readiness amendment and per-story historical scope guards for Stories 1.2, 1.5, and 1.6.
- `.agents/skills/bmad-create-story/SKILL.md` currently asks for previous story learnings and code patterns, but does not classify historical broad slices as anti-templates.
- `.agents/skills/bmad-create-story/checklist.md` reviews previous story intelligence, but does not require a historical-scope exclusion check.
- `.agents/skills/bmad-code-review/steps/step-02-review.md` asks the Acceptance Auditor to check AC/spec violations, but does not explicitly audit whether the story/spec reused a historical broad slice.

## 2. Impact Analysis

Epic impact:

- No product epic needs new scope.
- Epic 1 already has the authoritative historical-scope rule; this change makes the workflows enforce it.
- Future remediation and operational epics benefit because their story files will be forced to stay source-anchored and vertically demonstrable instead of copying old broad infrastructure stories.

Story impact:

- Current implementation stories do not need to be reopened.
- Future create-story runs must classify previous stories as reusable pattern, historical reference only, or anti-template.
- Future reviews must flag story/spec broadening when a story copies historical broad-slice structure or relies on internal-only evidence where observable proof is required.

Artifact conflicts:

- PRD: no change. MVP and post-MVP scope stay intact.
- Epics: no change required. Existing guards are sufficient.
- Architecture: no change. This is workflow/process enforcement.
- UX: no change. No UI behavior is affected.
- Process notes and BMAD skill files need updates.

Technical impact:

- No product code changes.
- No submodule updates.
- No test-suite or CI changes unless a later implementation chooses to add automated linting around story files.

## 3. Recommended Approach

Recommended path: Direct Adjustment with backlog/process coordination.

Rationale:

- The rule already exists in `epics.md`; the issue is enforcement during story generation and review.
- Updating the workflow checks is lower risk than reopening completed broad stories or editing product scope.
- The change should be durable across future runs by adding one process-note lesson and explicit checks in create-story and code-review instructions.

Effort estimate: low to medium, about 0.5 working day.

Risk: low for product code, medium for workflow discipline. The main risk is making the prompts too vague; the implementation must name exact anti-template behavior and exact review findings.

## 4. Detailed Change Proposals

### Proposal A: Add Durable Process Lesson

Artifact: `_bmad-output/process-notes/story-creation-lessons.md`

Section: after `L08 - Party Review vs. Elicitation`

OLD:

```markdown
## L08 - Party Review vs. Elicitation

- Party-mode review is the cross-role critique and triage pass before
  development; it should produce dated trace evidence when completed.
- Advanced elicitation is a separate hardening pass after a completed
  party-mode trace exists; a recommendation to run elicitation is not itself
  completed elicitation evidence.
```

NEW:

```markdown
## L09 - Historical Broad Slices Are Anti-Templates

- Completed broad technical slices may be used only as historical context or
  dependency evidence. They must not be copied as the shape for new stories.
- When epics mark a story as historical completed scope, any reopened,
  reimplemented, or analogous work must be split into newly numbered vertical
  stories with independently observable evidence.
- Story creation must classify previous-story context as reusable pattern,
  historical reference only, or anti-template before carrying lessons forward.
- Review must flag a story or implementation that reuses a historical broad
  slice as a template, hides broad scope behind one story, or accepts internal
  classes/unit tests as sufficient proof where observable API/CLI/contract,
  trace, or integration evidence is required.
```

Rationale: gives future BMAD runs a stable local lesson to load and quote.

### Proposal B: Harden Create-Story Previous Story Analysis

Artifact: `.agents/skills/bmad-create-story/SKILL.md`

Section: Step 2 previous story intelligence.

OLD:

```markdown
**PREVIOUS STORY INTELLIGENCE:** -
Dev notes and learnings from previous story - Review feedback and corrections needed - Files that were created/modified and their
patterns - Testing approaches that worked/didn't work - Problems encountered and solutions found - Code patterns established <action>Extract
all learnings that could impact current story implementation</action>
```

NEW:

```markdown
**PREVIOUS STORY INTELLIGENCE:** -
Dev notes and learnings from previous story - Review feedback and corrections needed - Files that were created/modified and their
patterns - Testing approaches that worked/didn't work - Problems encountered and solutions found - Code patterns established
<action>Classify each previous story or historical reference as one of:
  - reusable pattern: safe to emulate for story shape and evidence
  - historical reference only: useful for dependency/evidence context, but not a template
  - anti-template: broad technical or bundled historical slice that must not be copied
</action>
<action>If epics or process notes mark a previous story as historical completed scope, broad technical scope, bundled infrastructure, or "not valid patterns for future story creation", then:
  - do not copy its task structure, AC density, or file-scope breadth into the new story
  - split analogous work into newly numbered vertical stories before creating implementation-ready context
  - require externally observable API/CLI/contract/trace/integration evidence for each split slice
  - document the exclusion in Dev Notes under Historical Scope Guard
</action>
<action>Extract only the learnings that help the current story implement a narrow, independently demonstrable slice.</action>
```

Rationale: prevents create-story from turning historical context into a new broad template.

### Proposal C: Add Historical Scope Guard to Story Template

Artifact: `.agents/skills/bmad-create-story/template.md`

Section: `## Dev Notes`

OLD:

```markdown
## Dev Notes

- Relevant architecture patterns and constraints
- Source tree components to touch
- Testing standards summary
```

NEW:

```markdown
## Dev Notes

- Relevant architecture patterns and constraints
- Source tree components to touch
- Testing standards summary

### Historical Scope Guard

- Previous or historical stories used as context:
- Reusable patterns:
- Historical reference only:
- Anti-templates explicitly not reused:
- Required split or observable evidence rule:
```

Rationale: makes the classification visible in every generated story, not buried in the create-story debug log.

### Proposal D: Harden Create-Story Validation Checklist

Artifact: `.agents/skills/bmad-create-story/checklist.md`

Section: `2.3 Previous Story Intelligence`

OLD:

```markdown
- If `story_num > 1`, load the previous story file
- Extract **actionable intelligence**:
  - Dev notes and learnings
  - Review feedback and corrections needed
  - Files created/modified and their patterns
  - Testing approaches that worked/didn't work
  - Problems encountered and solutions found
  - Code patterns and conventions established
```

NEW:

```markdown
- If `story_num > 1`, load the previous story file
- Also scan epics and process notes for historical-scope guard language such as
  "historical completed scope", "broad technical", "bundled infrastructure",
  "not valid patterns for future story creation", "must split", or "do not reopen".
- Extract **actionable intelligence**:
  - Dev notes and learnings
  - Review feedback and corrections needed
  - Files created/modified and their patterns
  - Testing approaches that worked/didn't work
  - Problems encountered and solutions found
  - Code patterns and conventions established
- Classify each previous or historical story reference as reusable pattern,
  historical reference only, or anti-template.
- Treat any historical broad slice as an anti-template unless the current epics
  explicitly approve reusing its scope shape.
- If the story copies a historical broad slice, hides several vertical outcomes
  in one task list, or lacks observable evidence per slice, record a Critical
  Miss and revise the story before finalizing.
```

Rationale: makes the validation pass reject the failure mode.

### Proposal E: Harden Code Review Acceptance Auditor

Artifact: `.agents/skills/bmad-code-review/steps/step-02-review.md`

Section: Acceptance Auditor prompt.

OLD:

```markdown
Check for: violations of acceptance criteria, deviations from spec intent, missing implementation of specified behavior, contradictions between spec constraints and actual code.
```

NEW:

```markdown
Check for: violations of acceptance criteria, deviations from spec intent, missing implementation of specified behavior, contradictions between spec constraints and actual code, and story-process guardrail violations. In particular, flag any evidence that the story or implementation reused a historical broad technical slice as a template, widened beyond an independently demonstrable vertical slice, ignored a Historical Scope Guard, or accepted internal-only implementation evidence where the spec requires observable API/CLI/contract/trace/integration proof.
```

Rationale: review catches both implementation drift and malformed story/spec context before acceptance.

### Proposal F: Harden Review Triage Severity

Artifact: `.agents/skills/bmad-code-review/steps/step-03-triage.md`

Section: severity and routing.

OLD:

```markdown
- `high` -- intolerable
```

NEW:

```markdown
- `high` -- intolerable, including story/spec scope drift that reuses a historical broad slice as a template or masks multiple independently demonstrable slices in one story when the project artifacts forbid that pattern
```

Rationale: makes the review outcome block acceptance instead of treating broad-slice reuse as editorial noise.

## 5. Checklist Findings

- [x] 1.1 Triggering story identified: no single new implementation story triggered this; the issue is a process-control gap discovered while reviewing the existing story creation/review workflows against Epic 1 historical-scope guards.
- [x] 1.2 Core problem defined: misunderstanding/process gap. Existing artifacts forbid historical broad-slice reuse, but workflow checks do not enforce the rule.
- [x] 1.3 Evidence gathered: `epics.md` historical guards, create-story previous-story analysis, create-story checklist, code-review auditor prompt.
- [x] 2.1 Current epic still valid: yes. No product epic needs replacement.
- [x] 2.2 Epic-level changes: none required because Epic 1 already contains the rule.
- [x] 2.3 Remaining epics reviewed for impact: current remediation epics 20-26 stay valid; audit-anchor preflight remains complementary.
- [x] 2.4 New epic needed: no.
- [x] 2.5 Priority/order changes: no.
- [x] 3.1 PRD conflicts: none.
- [x] 3.2 Architecture conflicts: none.
- [N/A] 3.3 UI/UX conflicts: no UI scope.
- [x] 3.4 Other artifacts: process notes and BMAD skill files require updates.
- [x] 4.1 Direct adjustment: viable, low effort, low product risk.
- [x] 4.2 Rollback: not viable; there is no bad product implementation to revert.
- [x] 4.3 MVP review: not needed; MVP readiness accounting is unchanged.
- [x] 4.4 Recommended path: direct adjustment with process coordination.
- [x] 5.1-5.5 Proposal components: included above.
- [x] 6.3 User approval: approved by Jerome on 2026-07-06.
- [N/A] 6.4 Sprint-status update: not needed unless a follow-up implementation story is added.
- [x] 6.5 Handoff plan: below.

## 6. Implementation Handoff

Scope classification: Moderate.

Implementation status: approved by Jerome and applied on 2026-07-06.

Routed to: Developer agent for direct workflow/document edits.

Implementation tasks:

1. [x] Apply Proposal A to `_bmad-output/process-notes/story-creation-lessons.md`.
2. [x] Apply Proposal B to `.agents/skills/bmad-create-story/SKILL.md`.
3. [x] Apply Proposal C to `.agents/skills/bmad-create-story/template.md`.
4. [x] Apply Proposal D to `.agents/skills/bmad-create-story/checklist.md`.
5. [x] Apply Proposal E to `.agents/skills/bmad-code-review/steps/step-02-review.md`.
6. [x] Apply Proposal F to `.agents/skills/bmad-code-review/steps/step-03-triage.md`.

Success criteria:

- Future create-story runs must explicitly identify anti-template historical slices before carrying previous-story context forward.
- Generated story files include a `Historical Scope Guard` section when historical or previous stories influence the story.
- Create-story validation rejects story files that reuse Stories 1.2, 1.5, 1.6, or similar historical broad slices as templates.
- Code-review Acceptance Auditor flags broad-slice reuse or missing observable evidence as a spec/process violation.
- No product code, PRD, architecture, UX, sprint-status, or submodule changes are required.

## 7. Approval Record

Approved by Jerome on 2026-07-06 and applied in the same workflow run.

Completion summary:

- Issue addressed: historical broad technical slices could still be reused as future story templates.
- Change scope: moderate process correction.
- Artifacts modified: story-creation lessons, create-story workflow, create-story template, create-story checklist, code-review Acceptance Auditor, code-review severity triage, and this proposal.
- Routed to: Developer agent.

## Supersession Note (2026-07-16)

The policy remains approved, but the direct edits under `.agents/skills/**`
were installation-scoped and were overwritten by a later BMad refresh. The
update-safe enforcement route is superseded by
`sprint-change-proposal-2026-07-16-historical-slice-guard-strengthening.md`:
committed `_bmad/custom/**` overrides plus resolver-level regression coverage.
