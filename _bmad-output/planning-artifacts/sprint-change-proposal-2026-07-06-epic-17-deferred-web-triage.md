# Sprint Change Proposal - Epic 17 Deferred Web Triage

Date: 2026-07-06
Project: Hexalith.Memories
Mode: Batch
Approval: Approved by Jerome on 2026-07-06

## 1. Issue Summary

Epic 17 retrospective action item 3 remained open: `17.1-CR1` through `17.1-CR18` and the deferred web architecture decisions for command-palette scope, refresh persistence, mobile grid strategy, and role authorization model needed final triage into bounded follow-up stories or explicit acceptance rationale in `_bmad-output/implementation-artifacts/deferred-work.md`.

The evidence came from:

- `_bmad-output/implementation-artifacts/deferred-work.md` Story 17.1 review section.
- `_bmad-output/implementation-artifacts/epic-17-retro-2026-06-24.md` action item 3 and deferred architecture decision list.
- `_bmad-output/planning-artifacts/epics.md` Story 17.7 and Story 25.7 backlog homes.
- `docs/dev/adr-10.2-004-auth-granularity.md` for the accepted tenant-claim-only MVP authorization model.

## 2. Impact Analysis

Epic impact: Epic 17 stays reopened only for the already-added Story 17.7 browser/AT validation gap closure. Completed Story 17.1 through Story 17.6 history is not reopened.

Story impact:

- Existing Story 17.7 receives the browser/AT/mobile validation bucket.
- Existing Story 25.7 receives the Evidence Cockpit conformance/localization/Fluent primitive bucket.
- Future web story homes are named for Evidence Metadata and Trust Semantics Surfacing, Omitted Details Expansion UX, Command Surface Scope and Redaction Safety, and Tenant-Scoped Refresh Persistence Policy.
- Defensive-only or already-covered items are accepted or resolved in the ledger.

Artifact impact:

- Updated `_bmad-output/implementation-artifacts/deferred-work.md`.
- Updated `_bmad-output/implementation-artifacts/sprint-status.yaml` to mark the retrospective action item done.

Technical impact: Documentation/planning only. No production code, API, storage, MCP, CLI, or tenant-isolation behavior changed.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The issue is backlog hygiene, not a product pivot. Current planning already contains two concrete homes, Story 17.7 and Story 25.7. The remaining items are either bounded future web decisions or accepted/resolved risks. A rollback or PRD MVP review would add no value because Epic 17 is explicitly future web UI work outside MVP readiness unless pulled forward.

Effort: Low.
Risk: Low.
Timeline impact: None unless a future sprint selects one of the named web follow-up stories.

## 4. Detailed Change Proposals

### Deferred Work

Add a new section below the legacy Story 17.1 review bullets:

- `17.1-CR1`, `17.1-CR16`, `17.1-CR18` -> Story 25.7 Evidence Cockpit UX Conformance.
- `17.1-CR2`, `17.1-CR4`, `17.1-CR5`, `17.1-CR6`, `17.1-CR17` -> future Evidence Metadata and Trust Semantics Surfacing story.
- `17.1-CR3` -> future Omitted Details Expansion UX story.
- `17.1-CR8`, `17.1-CR9`, `17-WEB-AD1-COMMAND-PALETTE-SCOPE` -> future Command Surface Scope and Redaction Safety story.
- `17.1-CR7`, `17.1-CR10`, `17.1-CR11`, `17-WEB-AD3-MOBILE-GRID-STRATEGY` -> Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure.
- `17-WEB-AD2-REFRESH-PERSISTENCE` -> future Tenant-Scoped Refresh Persistence Policy story.
- `17.1-CR12`, `17.1-CR13`, `17.1-CR14`, `17-WEB-AD4-ROLE-AUTHORIZATION-MODEL` -> accepted with rationale and re-open triggers.
- `17.1-CR15` -> resolved with stale-state fixture and test evidence.

### Sprint Status

Change the Epic 17 action item status from `open` to `done` with a 2026-07-06 note referencing the structured deferred-work dispositions.

## 5. Implementation Handoff

Scope classification: Minor.
Approval status: Approved.

Handoff:

- Developer/maintainer: use the structured ledger entries when creating future web stories.
- UX designer: own placement decisions for permissions context, omitted details, source trust marks, command scope, and mobile transformation strategy when those stories are selected.
- Test architect: ensure Story 17.7 and future web stories include browser/AT, redaction, stale-context, and tenant-scope validation where their entries require it.

Success criteria:

- `deferred-work.md` contains a structured disposition for every `17.1-CR1` through `17.1-CR18` item and the four named architecture decisions.
- The Epic 17 retrospective action item is marked `done`.
- No completed Epic 17 story history is rewritten.

## 6. Checklist Summary

| Checklist Area | Status | Notes |
|---|---|---|
| Trigger and context | Done | Epic 17 retrospective action item 3 supplied the trigger. |
| Epic impact | Done | No completed story reopened; Story 17.7 and Story 25.7 used where applicable. |
| Artifact conflicts | Done | Planning/deferred-work only; no PRD or architecture rewrite required. |
| Path forward | Done | Direct adjustment selected. |
| Proposal and handoff | Done | This proposal and ledger updates provide the handoff. |
