# Sprint Change Proposal: Cross-Tenant Negative Evidence Carry-Forward

**Date:** 2026-07-06
**Project:** Hexalith.Memories
**Trigger:** Epic 0 retrospective action item: keep cross-tenant negative validation evidence attached to future scope-sensitive changes.
**Status:** Approved by Jerome on 2026-07-06 and applied
**Mode:** Batch
**Change Scope:** Minor - planning and sprint-status guard only

## 1. Issue Summary

Epic 0 closed the tenant and case safety foundation, and later Epics 20 and 24 produced stronger tenant-boundary evidence: Story 20.2 proves cross-tenant requests are denied before tenant state, search, graph, actor, workflow, or registry dependencies are invoked; Story 24.3 proves verifier checks fail closed against target-prefix tenant-marker evidence and removes misleading runtime self-test evidence.

The open risk is not a missing implementation today. The risk is evidence drift: future scope-sensitive refactors in route grouping, endpoint filters, MCP execution, evidence rendering, route versioning, tenant verification, key routing, or case attribution could preserve happy paths while silently dropping the negative cross-tenant proof that makes the tenant boundary credible.

## 2. Impact Analysis

**Epic Impact:** Epic 0 remains done. Epic 20 and Epic 24 remain done. Epic 25 and any remaining Epic 20-26 scope-sensitive work inherit a new evidence carry-forward guard.

**Story Impact:** No new story is created. Future scope-sensitive stories must cite and preserve the applicable cross-tenant negative evidence. The most obvious affected future stories are 25.2, 25.3, 25.4, 25.6, and 25.7 because they can move endpoint filters, route constants, MCP authorization, or evidence-scope rendering.

**Artifact Conflicts:** No PRD, architecture, or UX contradiction is introduced. The guard reinforces existing PRD NFR8, FR44, FR67, the architecture physical-isolation target, and the UX requirement that wrong-scope evidence is trust-blocking.

**Technical Impact:** None immediately. The change affects story creation, Dev Agent Record content, review checklists, and validation expectations for future scope-sensitive changes.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale: this is an evidence-retention and planning hygiene issue. A new epic would overstate the scope, and rollback is not relevant because the existing negative tests are valuable. The right fix is to attach a durable preflight guard to Epic 20-26 planning and mark the retrospective action as actively carried forward.

Effort estimate: Low.
Risk level: Low.
Timeline impact: None for current implementation; future scope-sensitive stories must budget focused negative validation.

## 4. Detailed Change Proposals

### Proposal A: Add Epic 20-26 Evidence Carry-Forward Guard

Artifact: `_bmad-output/planning-artifacts/epics.md`

OLD:

```text
Audit-anchor preflight requires current code anchors and implementation-state assumptions to be re-verified, but it does not explicitly require future scope-sensitive stories to preserve the cross-tenant negative validation evidence produced by Story 20.2 and Story 24.3.
```

NEW:

```text
Add a 2026-07-06 cross-tenant negative-evidence carry-forward guard after the audit-anchor preflight. The guard requires future scope-sensitive Epic 20-26 stories to cite prior evidence, name impacted surfaces, and include focused negative tests or an explicit accepted blocker.
```

Rationale: future route/auth/MCP/evidence/verifier refactors should not close on happy-path or refactor-green tests alone when they touch tenant or case scope.

### Proposal B: Advance The Retrospective Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
status: open
```

NEW:

```yaml
status: in-progress  # 2026-07-06: Cross-tenant negative-evidence carry-forward guard added to epics.md and captured in sprint-change-proposal-2026-07-06-cross-tenant-negative-evidence.md; remains active for future scope-sensitive stories.
```

Rationale: the guard is now attached to planning, but the obligation remains active for future stories.

## 5. Checklist Results

- 1.1 Triggering story: N/A. Trigger is Epic 0 retrospective action item 4.
- 1.2 Core problem: Done. Evidence-retention risk during future scope-sensitive changes.
- 1.3 Supporting evidence: Done. Story 20.2 and Story 24.3 contain the relevant negative validation evidence.
- 2.1 Current epic impact: Done. Epic 0, 20, and 24 remain complete.
- 2.2 Required epic changes: Done. Add a planning guard to the Epic 20-26 audit-remediation preflight.
- 2.3 Remaining planned epics: Done. Epic 25 is the primary future risk because it moves routes, errors, telemetry, contracts, MCP execution, and evidence rendering.
- 2.4 Invalidated epics/new epics: N/A. No new epic is needed.
- 2.5 Order/priority: N/A. No sequencing change.
- 3.1 PRD conflicts: Done. No conflict; reinforces FR44, FR67, and NFR8.
- 3.2 Architecture conflicts: Done. No conflict; reinforces the physical-isolation direction and D8/D29 boundary.
- 3.3 UX conflicts: Done. No conflict; reinforces wrong-scope evidence as a trust-blocking state.
- 3.4 Other artifacts: Done. Sprint status action item updated.
- 4.1 Direct adjustment: Viable. Low effort, low risk.
- 4.2 Rollback: Not viable. Existing evidence should be preserved, not reverted.
- 4.3 MVP review: Not required. MVP scope unchanged.
- 4.4 Recommended path: Done. Direct adjustment.
- 5.1-5.5 Proposal components: Done.
- 6.1 Proposal review: Done by repository evidence inspection.
- 6.2 Accuracy check: Done. Guard references existing Story 20.2 and Story 24.3 evidence.
- 6.3 User approval: Approved by Jerome on 2026-07-06.
- 6.4 Sprint status update: Done.
- 6.5 Handoff plan: Done.

## 6. Implementation Handoff

Scope classification: **Minor**.

Handoff recipients:

- Developer agent: when creating or implementing future scope-sensitive stories, cite the preserved negative evidence and add focused cross-tenant denial or fail-closed validation.
- Test architect: reject scope-sensitive close-out that lacks negative cross-tenant proof or a documented accepted blocker.
- Architect/product owner: keep this guard attached during Epic 25 story creation, especially route, contract, MCP, and evidence-scope changes.

Success criteria:

- Epic 20-26 planning contains a visible guard for cross-tenant negative evidence.
- The Epic 0 retrospective action item is no longer open; it remains active as an in-progress carry-forward guard.
- Future scope-sensitive story files and Dev Agent Records cannot close with happy-path or refactor-only validation.

## 7. Approval

- [x] Approved by Jerome - 2026-07-06
- [x] `epics.md` updated with the cross-tenant negative-evidence carry-forward guard
- [x] `sprint-status.yaml` updated to move the retrospective action item to `in-progress`
