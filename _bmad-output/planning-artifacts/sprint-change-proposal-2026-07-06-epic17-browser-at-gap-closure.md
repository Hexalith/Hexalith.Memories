# Sprint Change Proposal: Epic 17 Browser and Assistive-Technology Gap Closure

Date: 2026-07-06
Project: memories
Owner: Jerome
Workflow: bmad-correct-course
Mode: Batch

## 1. Issue Summary

Story 17.5 and Story 17.6 completed Epic 17 validation only at host-less RCL/component-specimen level. The machine-checked register `Epic17ValidationInventory.Gaps` still records fail-closed browser and assistive-technology gaps for Playwright/axe, automated contrast, forced-colors, reduced-motion, zoom/reflow, touch-target sizing, screen-reader validation, and live keyboard focus-trap/focus-return behavior.

The trigger is the Epic 17 retrospective/action-item requirement to define and schedule a runnable web host or specimen app plus at least one manual screen-reader pass. Without that scheduled work, the project could accidentally treat bUnit/component-specimen evidence as full browser or product-surface accessibility evidence.

Evidence:

- `_bmad-output/implementation-artifacts/17-5-responsive-and-accessible-web-validation.md` states that browser/axe/forced-colors/reduced-motion/zoom/screen-reader dimensions remain deferred fail-closed gaps because `Hexalith.Memories.Web` is a host-less Razor Class Library.
- `_bmad-output/implementation-artifacts/tests/test-summary-17-5-responsive-accessible.md` records the same deferred register and says a runnable host/specimen plus manual screen-reader pass is required before a full product-surface claim.
- `_bmad-output/implementation-artifacts/17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening.md` confirms Story 17.6 did not close the browser/AT gaps.
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs` keeps the gaps fail-closed with owners, severity, waiver state, and release disposition.

## 2. Impact Analysis

Epic impact: Epic 17 is reopened from `done` to `in-progress` because a new scheduled backlog story is required to close the remaining browser/AT validation evidence gap.

Story impact:

- New Story 17.7 is added: `Runnable Web Specimen and Browser/AT Accessibility Gap Closure`.
- Story 17.6 remains the preflight conformance gate and must stay first in `story_execution_order.epic-17`.
- Stories 17.1 through 17.5 remain done; their evidence is not reclassified as browser or product-route evidence.

Artifact conflicts:

- PRD: no MVP scope change. Epic 17 remains excluded from MVP unless explicitly sprint-selected.
- Architecture: no production web host commitment is added. The new story is scoped to a test/specimen host unless implementation later proposes a product route.
- UX: aligned with the UX testing strategy requiring automated checks plus human interaction passes, including screen-reader readability.
- Sprint status: requires a new Story 17.7 backlog row, updated Epic 17 execution order, and closure of the existing "define and schedule" action item.

Technical impact:

- Later implementation will likely add a test-only Blazor specimen host and Playwright project.
- Later implementation must update `Epic17ValidationInventory.Gaps` only with evidence-backed resolved rows or still-fail-closed dispositions.
- Later implementation must preserve existing FrontComposer/Fluent UI V5 conformance, fixture semantics, redaction rules, and no-new-product-workflow boundary.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: the issue is an uncovered validation lane, not a product pivot. The cleanest correction is to add one bounded Epic 17 story that creates a runnable specimen and gathers automated browser plus manual assistive-technology evidence. This preserves all existing Story 17.5/17.6 evidence and prevents accidental over-claiming.

Effort: Medium.
Risk: Medium, because Playwright infrastructure and manual AT evidence require environment coordination.
Timeline impact: one additional Epic 17 story before any full browser/product-surface accessibility claim.
MVP impact: none, because Epic 17 remains future web UI and excluded unless sprint-selected.

## 4. Detailed Change Proposals

### 4.1 Epics: Epic 17 Readiness Note

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Epic 17: Future Web UX Composition & Accessibility`

OLD:

```markdown
**Readiness note:** Story 17.6 is the current conformance gate for the host-less web RCL. It does not close product-route browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, or manual screen-reader validation gaps; those remain future web-host validation work and must be sprint-selected separately.
```

NEW:

```markdown
**Readiness note:** Story 17.6 is the current conformance gate for the host-less web RCL. It does not close product-route browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, or manual screen-reader validation gaps. Story 17.7 is the scheduled browser/assistive-technology gap-closure story for those fail-closed dimensions.
```

Rationale: makes the scheduled closure story visible where the original gap note lived.

### 4.2 Epics: New Story 17.7

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: after Story 17.6.

NEW:

```markdown
### Story 17.7: Runnable Web Specimen and Browser/AT Accessibility Gap Closure

As a user of the future web surface,
I want Epic 17 trust workflows to run in a browser-backed specimen with automated and manual accessibility evidence,
So that the fail-closed browser and assistive-technology gaps in `Epic17ValidationInventory.Gaps` are closed by evidence rather than waived by component-specimen coverage.
```

The new story requires:

- a minimal runnable Memories web specimen over existing RCL components and contract fixtures,
- Playwright smoke and `@axe-core/playwright` scans,
- forced-colors, reduced-motion, zoom/reflow, and 44x44px touch-target evidence,
- at least one manual screen-reader pass, preferably NVDA with Edge or Chrome on Windows,
- sanitized bounded artifact evidence,
- explicit `Epic17ValidationInventory.Gaps` resolution or fail-closed carry-forward.

Rationale: turns an open retrospective action into implementable backlog.

### 4.3 Sprint Status: Epic 17 Execution Order

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
story_execution_order:
  epic-17:
    reason: "Story 17.6 is the conformance gate for future Epic 17 web work."
    order:
      - 17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening
      - 17-2-recovery-and-feedback-state-grammar
      - 17-3-contract-aware-web-interaction-patterns
      - 17-4-role-specific-web-inspection-lenses
      - 17-5-responsive-and-accessible-web-validation
```

NEW:

```yaml
story_execution_order:
  epic-17:
    reason: "Story 17.6 is the conformance gate for future Epic 17 web work; Story 17.7 closes the browser/AT validation gaps after that gate."
    order:
      - 17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening
      - 17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure
      - 17-2-recovery-and-feedback-state-grammar
      - 17-3-contract-aware-web-interaction-patterns
      - 17-4-role-specific-web-inspection-lenses
      - 17-5-responsive-and-accessible-web-validation
```

Rationale: preserves the 17.6 preflight while scheduling the browser/AT closure before any future reopened web stories.

### 4.4 Sprint Status: Development Rows

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  # Epic 17: Future Web UX Composition & Accessibility
  epic-17: done
  17-1-evidence-cockpit-and-trust-components: done
  17-2-recovery-and-feedback-state-grammar: done
  17-3-contract-aware-web-interaction-patterns: done
  17-4-role-specific-web-inspection-lenses: done
  17-5-responsive-and-accessible-web-validation: done
  17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening: done
  epic-17-retrospective: done
```

NEW:

```yaml
  # Epic 17: Future Web UX Composition & Accessibility
  epic-17: in-progress
  17-1-evidence-cockpit-and-trust-components: done
  17-2-recovery-and-feedback-state-grammar: done
  17-3-contract-aware-web-interaction-patterns: done
  17-4-role-specific-web-inspection-lenses: done
  17-5-responsive-and-accessible-web-validation: done
  17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening: done
  17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure: backlog
  epic-17-retrospective: done
```

Rationale: keeps sprint accounting honest after adding an incomplete Story 17.7.

### 4.5 Sprint Status: Retrospective Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  - epic: 17
    action: "Define and schedule a runnable web host / specimen app and at least one manual screen-reader pass to close the fail-closed browser/AT gaps in Epic17ValidationInventory.Gaps (Playwright/axe, forced-colors, reduced-motion, zoom/reflow, touch-target)"
    owner: "Sally, Murat"
    status: open
```

NEW:

```yaml
  - epic: 17
    action: "Define and schedule a runnable web host / specimen app and at least one manual screen-reader pass to close the fail-closed browser/AT gaps in Epic17ValidationInventory.Gaps (Playwright/axe, forced-colors, reduced-motion, zoom/reflow, touch-target)"
    owner: "Sally, Murat"
    status: done  # 2026-07-06: Story 17.7 added to epics.md and sprint-status.yaml; implementation remains tracked by the new backlog story.
```

Rationale: the definition/scheduling action is complete; implementation remains tracked by Story 17.7.

## 5. Implementation Handoff

Scope classification: Moderate.

Route to: Product Owner / Developer / QA / UX.

Responsibilities:

- Developer agent: create Story 17.7 when selected; implement the test-only specimen host, Playwright lane, source/fixture route anchors, inventory updates, and evidence summary.
- QA/Test Architect: define the automated browser/axe checks, media emulation checks, zero-node guards, artifact redaction scan, and pass/fail thresholds.
- UX/accessibility owner: run or coordinate the manual screen-reader pass and review forced-colors, reduced-motion, zoom/reflow, and touch-target evidence.
- Product owner: decide whether any remaining unsupported browser/AT dimensions are release blockers, accepted debt, or future follow-up.

Success criteria:

- Story 17.7 exists in Epic 17 and sprint status as backlog work.
- Epic 17 execution order requires Story 17.6 before Story 17.7.
- The former open action item is closed as scheduled work, not silently ignored.
- Future implementation cannot mark `Epic17ValidationInventory.Gaps` closed without browser/manual evidence or an explicit fail-closed carry-forward.

## 6. Approval

Jerome approved this Sprint Change Proposal for implementation on 2026-07-06.

Approved scope classification: Moderate.

Approved route: Product Owner / Developer / QA / UX, with Story 17.7 as the implementation backlog home.

## 7. Checklist Results

| Checklist Item | Status | Notes |
|---|---|---|
| 1.1 Triggering story identified | [x] | Story 17.5/17.6 evidence and Epic 17 action item revealed the browser/AT gap. |
| 1.2 Core problem defined | [x] | Failed approach requiring a separate validation lane: component specimens cannot prove browser/AT behavior. |
| 1.3 Evidence gathered | [x] | Story 17.5, Story 17.6, test summary, and `Epic17ValidationInventory.Gaps`. |
| 2.1 Current epic assessed | [x] | Epic 17 remains valid but must reopen for Story 17.7. |
| 2.2 Epic-level changes | [x] | Add Story 17.7 and update readiness note. |
| 2.3 Remaining epics reviewed | [x] | No direct impact on Epics 18-26. |
| 2.4 New epic needed | [N/A] | Existing Epic 17 is the correct home. |
| 2.5 Priority/order | [x] | Story 17.7 follows Story 17.6 in execution order. |
| 3.1 PRD conflict | [x] | No MVP change; future web UI remains excluded unless sprint-selected. |
| 3.2 Architecture conflict | [x] | No production host commitment; test/specimen host only. |
| 3.3 UI/UX conflict | [x] | Aligns with UX accessibility testing strategy. |
| 3.4 Secondary artifacts | [x] | Sprint status and future evidence summaries affected. |
| 4.1 Direct adjustment | [x] | Recommended. |
| 4.2 Rollback | [N/A] | No completed implementation needs rollback. |
| 4.3 MVP review | [N/A] | MVP scope unchanged. |
| 4.4 Path selected | [x] | Direct adjustment with a new scheduled story. |
| 5.1 Issue summary | [x] | Included. |
| 5.2 Impact documented | [x] | Included. |
| 5.3 Recommendation | [x] | Included. |
| 5.4 MVP/action plan | [x] | No MVP change; action plan is Story 17.7. |
| 5.5 Handoff | [x] | Included. |
| 6.1 Completion review | [x] | Applicable sections addressed. |
| 6.2 Proposal accuracy | [x] | Grounded in current artifacts and register. |
| 6.3 User approval | [x] | Jerome explicitly approved this proposal on 2026-07-06. |
| 6.4 Sprint status update | [x] | Updated Epic 17 status, Story 17.7 row, execution order, and action item. |
| 6.5 Next steps | [x] | Create/implement Story 17.7 when selected. |
