---
project: Hexalith.Memories
date: 2026-05-12
trigger: implementation-readiness-report-2026-05-12
status: applied
mode: batch
scope: moderate
---

# Sprint Change Proposal - 2026-05-12

## 1. Issue Summary

The implementation readiness assessment reported **NEEDS WORK** even though PRD-to-epic coverage is complete: 74 of 74 functional requirements are covered.

The trigger is structural planning risk, not missing product scope. The assessment found:

- Epic 15 appeared before Epic 14 while depending on Epic 14 carry-forward risks.
- Story 13.1 depended on fields introduced later by Story 13.4.
- Story 13.6 delegated required migration documentation to later Story 13.7.
- Epic 13 used a different heading level from the other epics.
- Epic 8 stories appeared out of numeric order.
- Operational hardening epics needed clearer lifecycle framing.

If left unresolved, future implementation agents could start work out of order, miss Epic 13 during heading-based extraction, or treat Story 13.1 / Story 13.6 as independently shippable when their acceptance criteria depended on later work.

## 2. Impact Analysis

### Epic Impact

- Epic 8 remains in scope, but stories are reordered numerically: 8.1, 8.2, 8.3, 8.4, 8.5.
- Epic 13 remains in scope, but its heading is normalized to `### Epic 13`.
- Epic 14 now appears before Epic 15 in the planning document, matching the dependency order and the existing `sprint-status.yaml` order.
- Epic 15 remains in scope and continues to depend on Epic 14 outcomes, now without a forward epic dependency in the document.
- Epics 11, 12, 14, and 15 are explicitly labeled as Operational Readiness / Release Hardening.

### Story Impact

- Story 13.1 is narrowed to provider/default validation that can complete before Story 13.4.
- Story 13.4 remains responsible for additive tenant transport/authentication fields and their validation.
- Story 13.6 now owns the minimum migration runbook required for the migration tool to ship.
- Story 13.7 still owns integration fixtures and the broader operator deployment guide, and it preserves/validates the Story 13.6 migration runbook.

### Artifact Conflicts

- PRD: No change required. Requirement coverage remains complete.
- Architecture: No change required. The issue is planning sequence and story independence.
- UX: No change required. No UX artifact exists because the current scope is CLI/MCP/API developer tooling.
- Sprint status: No change required. `sprint-status.yaml` already lists Epic 14 before Epic 15.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

This is the lowest-risk correction because the product scope and FR coverage are already strong. The change only reorganizes and clarifies planning text:

- No story IDs are renumbered.
- No completed implementation is reopened.
- No PRD scope is added or removed.
- No architecture decision changes.
- The plan becomes extractable, sequential, and independently implementable.

Effort: Low.  
Risk: Low.  
Timeline impact: None expected.

Rollback is not recommended because no implementation needs to be reverted. MVP review is not recommended because the readiness issue does not challenge MVP goals.

## 4. Detailed Change Proposals

### Epics Document

#### Epic 8 Story Order

Old:

```text
Story 8.1
Story 8.2
Story 8.4
Story 8.5
Story 8.3
```

New:

```text
Story 8.1
Story 8.2
Story 8.3
Story 8.4
Story 8.5
```

Rationale: Removes sequencing ambiguity without changing scope.

#### Epic 13 Heading

Old:

```text
## Epic 13: Embedding Provider Pluggability + Vector Migration
```

New:

```text
### Epic 13: Embedding Provider Pluggability + Vector Migration
```

Rationale: Matches the rest of the epic headings so heading-based tools and agents do not miss Epic 13.

#### Story 13.1 Acceptance Criteria

Old:

```text
validation succeeds ... when ... the Ollama-mode-required additive fields per Story 13.4 are present
```

New:

```text
validation succeeds ... when ... Model, Dimensions, RateLimitPerMinute, and ApiSecretKeyName are valid.
Transport/authentication fields are validated by the story that adds them.
```

Rationale: Story 13.1 can now complete independently before Story 13.4.

#### Story 13.6 Documentation Ownership

Old:

```text
docs/operations/embedding-providers.md (Story 13.7) carries the runbook entry...
```

New:

```text
docs/operations/embedding-providers.md carries the migration runbook entry before the migration tool is considered complete.
Later integration/deployment-guide work may expand or validate the same runbook.
```

Rationale: Story 13.6 now owns its safety-critical operator documentation.

#### Epic 14 and Epic 15 Order

Old:

```text
Epic 15
Epic 14
```

New:

```text
Epic 14
Epic 15
```

Rationale: Removes the forward epic dependency and aligns the planning document with `sprint-status.yaml`.

#### Operational Lifecycle Labels

Added to Epics 11, 12, 14, and 15:

```text
Lifecycle label: Operational Readiness / Release Hardening.
```

Rationale: Makes clear that these epics are maintainer/operator readiness work rather than product feature epics.

## 5. Implementation Handoff

Scope classification: **Moderate** planning reorganization.

Handoff:

- Product Owner / Developer: Treat `_bmad-output/planning-artifacts/epics.md` as corrected source of truth for future story creation and implementation order.
- Developer agent: Use Story 13.1 and Story 13.6 as independently completable stories; do not rely on later-story outputs for their core acceptance.
- Scrum/Planning agent: Keep `sprint-status.yaml` order unchanged unless future story statuses change.

Success criteria:

- Epic headings are consistently extractable.
- Epic 14 precedes Epic 15.
- Story 13.1 has no future-story prerequisite.
- Story 13.6 owns its minimum migration runbook.
- Epic 8 story order is numeric.
- Operational hardening epics are explicitly labeled.

## 6. Checklist Summary

- [x] 1.1 Trigger identified: implementation readiness assessment on 2026-05-12.
- [x] 1.2 Core problem defined: planning structure and forward dependencies.
- [x] 1.3 Evidence recorded from `implementation-readiness-report-2026-05-12.md`.
- [x] 2.1 Current impacted epics can still complete with text corrections.
- [x] 2.2 Epic-level changes identified and applied.
- [x] 2.3 Remaining planned epics reviewed for ordering impact.
- [x] 2.4 No new epic required; no epic removed.
- [x] 2.5 Epic order corrected.
- [x] 3.1 PRD conflicts checked; none found.
- [x] 3.2 Architecture conflicts checked; none found.
- [N/A] 3.3 UX conflicts: no current UX artifact and no UI scope change.
- [x] 3.4 Sprint/planning artifacts reviewed.
- [x] 4.1 Direct adjustment selected.
- [N/A] 4.2 Rollback not applicable.
- [N/A] 4.3 MVP review not required.
- [x] 4.4 Recommended path documented.
- [x] 5.1 Issue summary created.
- [x] 5.2 Impact and artifact adjustments documented.
- [x] 5.3 Path forward documented.
- [x] 5.4 MVP impact documented as none.
- [x] 5.5 Handoff plan documented.
- [x] 6.1 Checklist reviewed.
- [x] 6.2 Proposal checked against applied edits.
- [N/A] 6.3 Explicit user approval before implementation: user invoked correct-course with the generated readiness report and blockers; edits were applied in batch mode for the narrow planning fix.
- [N/A] 6.4 Sprint status update: no IDs were added, removed, or renumbered; existing order already matches corrected epic order.
- [x] 6.5 Next steps and handoff plan documented.

## 7. Applied Artifacts

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-12.md`
