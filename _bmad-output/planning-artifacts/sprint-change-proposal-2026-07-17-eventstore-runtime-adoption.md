# Sprint Change Proposal: EventStore Runtime Identity Adoption

Date: 2026-07-17  
Approval: Administrator directed application of the detailed blocker/story plan.  
Scope: Moderate backlog addition; Product Owner / Developer handoff.

## 1. Issue Summary

Memories already implements EventStore-backed zero-code ingestion, but it has no open story that owns
future adoption of the exact source and package identities authorized by EventStore Story 1.20.
Completed Epic 9 behavior stories prove integration behavior, not release identity adoption. Current
source and Builds/package pins are different and neither is migration authority.

## 2. Impact Analysis

- Epic impact: add a narrowly scoped Epic 28 rather than reopening completed Epic 9 or mixing adoption
  into the unrelated active Epic 27 retention work.
- Story impact: register Story 28.1 as backlog without creating its implementation file.
- PRD/MVP impact: none. EventStore/DAPR zero-code integration remains unchanged.
- Architecture/UX impact: no UX change and no ingestion/topology redesign; existing additive
  composition-root and DAPR contracts remain controlling.
- Test/release impact: require exact source/package identities, isolated dual-mode builds, focused
  contract tests, and real DAPR persistence/search/dedup proof.

## 3. Recommended Approach

Use a direct backlog addition. Epic 28 isolates dependency adoption from completed behavior history
and from Epic 27. Keep the epic and story backlog until Story 1.20 grants durable authority. Rollback
and PRD/MVP reduction do not address the identity gap.

Effort is medium after activation because package/source graph proof and real sidecar evidence are
required. Pre-activation risk is low because no dependency pointers or runtime code change.

## 4. Detailed Changes

- `epics.md`: add Epic 28 and Story 28.1 with fail-closed activation and complete acceptance criteria.
- `sprint-status.yaml`: register Epic 28 and Story 28.1 as backlog.
- `epic-28-context.md`: record identities, invariants, ownership, and required evidence.
- No Story 28.1 implementation file is created by this proposal.

## 5. Checklist And Handoff

- [x] Trigger and evidence identified: Story 1.20 is non-authorizing and Memories lacks an adoption
  owner.
- [x] Epic, PRD, architecture, UX, testing, release, and submodule impacts assessed.
- [x] Direct addition selected; reopening Epic 9, rollback, and MVP reduction rejected.
- [x] Administrator approval recorded by the `apply` directive.
- [x] Sprint registration updated.
- [!] Developer: create and implement Story 28.1 only after the activation gate passes.
- [!] Product/Release owners: provide durable Story 1.20 authority and approved identities.

Success means the backlog has one bounded adoption owner, existing integration behavior cannot be
changed implicitly, and implementation cannot begin against an unapproved EventStore identity.
