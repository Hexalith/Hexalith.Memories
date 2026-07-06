# Sprint Change Proposal: Epic 0 Evidence Map

**Date:** 2026-07-06
**Project:** Hexalith.Memories
**Trigger:** Epic 0 retrospective action item: maintain a compact evidence map linking each Epic 0 story row to its canonical proof artifact.
**Status:** Approved by Jerome on 2026-07-06 and applied
**Change Scope:** Minor - documentation and sprint-status hygiene only

## 1. Issue Summary

Epic 0 is complete, but its completion evidence is intentionally fragmented because several rows were reclassified or imported from historical work. Story 0.0 points to historical Story 1.1, Story 0.2 points to deep Story 3.1 proof, and Story 0.4 imports the minimum CI preflight proof from Story 11.1.

The retrospective identified this as a maintenance risk: future readiness checks should not need to chase aliases manually before they can verify the Epic 0 foundation gate.

## 2. Impact Analysis

**Epic Impact:** Epic 0 remains done. No story scope, acceptance criteria, or sequencing changes.

**Story Impact:** No story status changes except closing the retrospective action item that requested the map. The five Epic 0 story rows remain `done`.

**Artifact Conflicts:** None. The new map centralizes existing proof links and does not supersede the underlying story files.

**Technical Impact:** None. No code, infrastructure, tests, or deployment behavior changes.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale: the issue is documentation discoverability, not product scope or implementation behavior. A single compact map closes the retrospective action without reopening historical story files or altering completed evidence.

Effort estimate: Low.
Risk level: Low.
Timeline impact: None.

## 4. Detailed Change Proposals

### Proposal A: Add Epic 0 Evidence Map

Artifact: `_bmad-output/implementation-artifacts/epic-0-evidence-map.md`

OLD:

```text
No compact Epic 0 row-to-proof map exists.
```

NEW:

```text
Add a table mapping each Epic 0 sprint-status story row to its canonical proof artifact, proof summary, and supporting historical/imported-evidence context.
```

Rationale: future readiness checks can verify Story 0.0, 0.2, and 0.4 evidence without chasing historical aliases manually.

### Proposal B: Close The Retrospective Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
status: open
```

NEW:

```yaml
status: done  # 2026-07-06: Epic 0 evidence map added at _bmad-output/implementation-artifacts/epic-0-evidence-map.md.
```

Rationale: the requested map now exists and links all five Epic 0 story rows to proof.

## 5. Checklist Results

- 1.1 Triggering story: N/A. Trigger is Epic 0 retrospective action item 1.
- 1.2 Core problem: Done. Evidence discoverability issue caused by reclassification/imported proof.
- 1.3 Supporting evidence: Done. Retrospective names the fragmented evidence and success criteria.
- 2.1 Current epic impact: Done. Epic 0 remains complete.
- 2.2 Required epic changes: N/A. No epic scope or acceptance change.
- 2.3 Remaining planned epics: N/A. No downstream epic impact.
- 2.4 Invalidated epics/new epics: N/A.
- 2.5 Order/priority: N/A.
- 3.1 PRD conflicts: N/A. No product requirement change.
- 3.2 Architecture conflicts: N/A. No architecture change.
- 3.3 UX conflicts: N/A. No UI/UX change.
- 3.4 Other artifacts: Done. Sprint status action item updated.
- 4.1 Direct adjustment: Viable. Low effort, low risk.
- 4.2 Rollback: Not viable. No implementation rollback involved.
- 4.3 MVP review: Not required. MVP scope unchanged.
- 4.4 Recommended path: Done. Direct adjustment.
- 5.1-5.5 Proposal components: Done.
- 6.1 Proposal review: Done by repository evidence inspection.
- 6.2 Accuracy check: Done. Map rows match sprint-status Epic 0 rows.
- 6.3 User approval: Applied by explicit user request on 2026-07-06.
- 6.4 Sprint status update: Done.
- 6.5 Handoff plan: Done.

## 6. Implementation Handoff

Scope classification: **Minor**.

Handoff recipients:

- Technical writer / developer: maintain the map when Epic 0 rows, aliases, or proof artifacts change.
- Future readiness-check agents: use `_bmad-output/implementation-artifacts/epic-0-evidence-map.md` as the first Epic 0 proof index.

Success criteria:

- Every Epic 0 story row in sprint status appears in the map.
- Each row points to the canonical proof artifact and supporting historical/imported-evidence context.
- The Epic 0 retrospective action item is marked done without closing unrelated action items.

## 7. Approval Record

Jerome approved this Sprint Change Proposal for implementation on 2026-07-06.
