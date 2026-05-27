---
project: Hexalith.Memories
date: 2026-05-13
trigger: implementation-readiness-follow-up
status: applied
mode: incremental
scope: minor
---

# Sprint Change Proposal - 2026-05-13

## 1. Issue Summary

The May 12 implementation-readiness assessment identified several planning-artifact readiness blockers in `epics.md`. Follow-up analysis on May 13 found that the substantive issues have already been corrected in the detailed epic body and implementation tracking:

- Epic 13 uses the same `### Epic N` heading level as the other epics.
- Story 13.1 no longer depends on fields introduced by Story 13.4.
- Story 13.6 owns the minimum migration runbook required for the migration tool to ship.
- Story 8 stories are numerically ordered.
- Epic 14 appears before Epic 15.
- Operational hardening epics include lifecycle labels.

The remaining issue is narrower: the high-level `## Epic List` summary in `epics.md` still stops at Epic 12, while the detailed body and `sprint-status.yaml` include Epics 13, 14, and 15.

## 2. Impact Analysis

### Epic Impact

- Epics 1-12 are unchanged.
- Epic 13, Epic 14, and Epic 15 are already present in the detailed body and tracked in `sprint-status.yaml`.
- The `## Epic List` summary needs to be synchronized so agents and humans scanning the top of the file see the current plan.

### Story Impact

- No story IDs, story text, or acceptance criteria need to change.
- No completed story is reopened.
- No story status changes are required.

### Artifact Conflicts

- PRD: No change required.
- Architecture: No change required.
- UX: Not applicable; no UX artifact exists for this CLI/MCP/API-focused scope.
- Sprint status: No change required. `sprint-status.yaml` already tracks Epic 13 as done, Epic 14 as done, and Epic 15 as in progress.
- Epics: `epics.md` summary requires one update.

### Technical Impact

No code, infrastructure, deployment, or test impact. This is a planning-document synchronization change only.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The detailed epic body is already correct.
- The implementation tracking is already correct.
- The remaining defect is isolated to a stale summary.
- Fixing the summary prevents future extraction or orientation errors without reopening completed work.

Effort: Low.  
Risk: Low.  
Timeline impact: None expected.

Rollback is not recommended because no implementation needs to be reverted. MVP review is not recommended because the issue does not change product scope, requirements, or gates.

## 4. Detailed Change Proposals

### Epics Document

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: `## Epic List`

#### Proposal: Add Epics 13-15 to the top epic summary

Old:

```markdown
### Phase: Post-MVP — Operations & First Release

### Epic 12: First Release & Operations Foundation
Cut the first real release of Hexalith.Memories to nuget.org, apply branch protection on `main`, operationalize the Epic 11 retrospective action items, and prove the release path end-to-end before any further feature investment. Closes the gap between "CI infrastructure built" and "release path proven against a real publish event."
**Driven by:** Epic 11 retrospective + Sprint Change Proposal 2026-04-26 (Hybrid path = Operations Epic 12 first, then Phase 2 decision)
```

New:

```markdown
### Phase: Post-MVP — Operations & First Release

### Epic 12: First Release & Operations Foundation
Cut the first real release of Hexalith.Memories to nuget.org, apply branch protection on `main`, operationalize the Epic 11 retrospective action items, and prove the release path end-to-end before any further feature investment. Closes the gap between "CI infrastructure built" and "release path proven against a real publish event."
**Driven by:** Epic 11 retrospective + Sprint Change Proposal 2026-04-26 (Hybrid path = Operations Epic 12 first, then Phase 2 decision)

### Epic 13: Embedding Provider Pluggability + Vector Migration
Operator can migrate the embedding pipeline from Google to a self-hosted Ollama gateway protected by Keycloak OIDC, while preserving Google as an opt-in provider and providing a Path A vector migration tool.
**Driven by:** Sprint Change Proposal 2026-04-29

### Epic 14: Deferred Work Hardening and Operational Readiness
Maintainers and operators can close high-value deferred review findings across CI correctness, release integrity, OIDC/embedding security, migration reliability, and deferred-work governance.
**Lifecycle label:** Operational Readiness / Release Hardening

### Epic 15: Carry-Forward Operational Risk Closure
Maintainers and operators can convert remaining carry-forward risks from Epic 14 into planned implementation, acceptance, or refreshed deferral decisions.
**Lifecycle label:** Operational Readiness / Release Hardening
```

Justification: The detailed `epics.md` body and `sprint-status.yaml` already include these epics. The top summary should match the current planning state.

## 5. Implementation Handoff

Scope classification: **Minor**.

Route to: Developer agent for direct planning-artifact edit.

Responsibilities:

- Update `epics.md` top `## Epic List` summary with Epics 13-15.
- Preserve existing detailed epic/story content.
- No PRD, architecture, UX, sprint status, or code changes.

Success criteria:

- `epics.md` top summary includes Epics 13, 14, and 15.
- Detailed Epic 13-15 sections remain unchanged.
- `sprint-status.yaml` remains unchanged.
- A follow-up readiness check no longer reports the stale Epic List summary as a gap.
