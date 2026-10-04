---
title: 'Story 27.8: post-write strong read'
status: draft
registration: held
created: '2026-10-04'
story_key: '27-8-post-write-strong-read'
gate_id: 'C1.2'
accountable_role: 'Deployment Adapter Developer'
profile_decision: 'PG-ONPREM-2 direction approved; exact bytes pending'
---

# Story 27.8: post-write strong read

This is an unregistered planning draft. The gate mode, fixture, revised profile,
operator authorization and reviewer assignment are prerequisites to finalization.
The command below is future intent and is not runnable with the current runner.
No backlog row, gate pass, live mutation or Production permission is created here.

## Story

As a Deployment Adapter Developer,
I want one independently attributable C1.2 observation,
So that an independent reviewer can judge post-write strong read on the approved profile.

## Acceptance Criteria

1. **Given** an explicitly authorized, eligible non-Production target on the approved
   immutable profile, **when** this literal gate producer executes, **then** it emits
   one new immutable, secret-safe packet proving this single observation:
   Acknowledged write followed by a strong read returning the acknowledged value.
2. **Given** the controlled scenario, **when** its observation is collected, **then**:
   Record an acknowledged write, then a strong-consistency read of that exact key/value/version from the running component. Preserve acknowledgement and read timestamps and identities.
3. **Given** invalid, missing, stale, mismatched or denied inputs, **when** collection
   or validation runs, **then**:
   Reject a stale or wrong-key value, a missing acknowledgement, a missing consistency setting, and a read from another component/profile.
4. **Given** a successful producer or offline fixture, **when** its result is recorded,
   **then** capture and independent acceptance remain separate; producer success
   grants no automatic gate pass, Production activation or A41 closure. Any permitted
   qualification mutation retains initial/final disabled gate, released Lease and
   zero lifecycle/clock replica receipts and removes only owned synthetic data.

## Tasks

- [ ] Finalize exact target/profile/authority, affected surfaces, independent reviewer
  and declared cleanup contract before implementation or target contact.
- [ ] Add only literal C1.2 to the existing runner dispatch and reuse its
  bounded, redacted immutable packet/source/command patterns after source inspection.
- [ ] Add `tests/tooling/access_telemetry_c1/gate_c1_2_test.py` with complete, denied, missing, malformed,
  drift, secret and timeout cases; prove no target/dependency call before denial.
- [ ] Implement the observed scenario and its verifier-visible end-state evidence;
  approval modes validate genuine input artifacts rather than generating consent.
- [ ] Reconcile this gate's observation/disposition into the reviewed canonical
  predecessor interchange; reject unique-path/hash/source/profile mismatches.
- [ ] Before registration, run the focused fixtures and required slice guard;
  add the finalized story, one epic definition and one `backlog` sprint row in the
  same bounded change. Append only this gate's owner mapping to the compiled context.
- [ ] After explicit live authorization, retain actual capture, cleanup, independent
  disposition and durable archive receipts. Mark incomplete operator work honestly.

## Checkpoint

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| :--------- | :---- | :--------------------------- | :----------- | :--------------- |
| C1.2 | Deployment Adapter Developer (planned role) | `pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.2 -ProfileId PG-ONPREM-2 -EvidenceDirectory /approved-evidence/access-telemetry-c1/C1.2` — proposed only; mode and profile unavailable | unreviewed | not-started |

The evidence root in the proposed command is a proposed operator input, not an
existing authorized directory. A later approved profile decision replaces the
proposed literal profile identity before any callable command is finalized.

## Dev Notes

### Dependencies

CRUD producer and approved component identity.

### Code Map and Proposed File Scope

- `tools/verify-access-telemetry-c1.ps1`: existing capture-only runner; currently
  accepts C1.15 and explicit historical-opt-in C1.16 on PG-ONPREM-1.
  PG-ONPREM-2 remains rejected. Retain the established capture behavior.
- `tests/tooling/access_telemetry_c1/gate_c1_2_test.py`: proposed new focused fixture, absent today.
- `_bmad-output/implementation-artifacts/27-8-post-write-strong-read.md`: proposed final story path.
- `_bmad-output/planning-artifacts/epics.md`,
  `_bmad-output/implementation-artifacts/sprint-status.yaml` and
  `_bmad-output/implementation-artifacts/epic-27-context.md`: bounded future
  registration surfaces; unchanged by this draft.
- Inspect existing Dapr qualification and adapter seams before finalizing runtime
  edits. Add any indispensable platform seam through its owning technical boundary;
  do not duplicate platform hosting or create a proprietary domain MCP/CLI.

### Historical Context Classification

| Influence | Classification | Permitted use |
| :-------- | :------------- | :------------ |
| Approved 2026-08-03 one-gate allocation and 2026-08-01 Annex A | historical-reference-only | Gate identifier, accountable role and required observation only. |
| Withdrawn Stories 27.5 and 27.6; broad Story 27.3 implementation | anti-template | Split provenance only; never copy bundled tasks, criteria or completion shape. |
| Story 27.21 runner parameter and neutral immutable packet contract | current-narrow-pattern | Source-confirmed literal dispatch and capture envelope only; no reuse of its whole story or historical gate credit. |

### Slice Proof

One gate, one outcome, one accountable role, one future literal command and one
separately recorded review/completion pair. Other gate results may be authenticated
inputs; they cannot be discharged by this story or share this gate's artifact.
Registration remains held until a genuine supported producer and focused fixtures
exist and the creation checks pass.

### Epic AC Verification

Verified 2026-10-04 against `47027d35a1f4a2c6986bec85b5cae601ce1b201e` and the
observed planning worktree; rechecked after the approved handoff against
`5b43fe2f8a0f04dc021921a077dff1a573c2ce5e` and its changed runner. Future
behavior above is desired intent, not a current fact.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "C1.2 — post-write strong read" | Location | `rg -n -F 'C1.2 — post-write strong read' _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md` | Exact approved allocation row; one matching gate. | confirmed |
| "The runner accepts C1.15 and historical-opt-in C1.16 on PG-ONPREM-1" | Behavioral | `rg -n -e "ValidateSet" -e "AllowHistoricalProfileCapture" tools/verify-access-telemetry-c1.ps1`; full and affected fixture results in the preparation spec | Updated after approved handoff: C1.16 preparation exists, but this proposed PG-ONPREM-2 command remains invalid. | confirmed |
| "The remaining twenty-four C1 gates stay held without a registered owner." | Quantitative/existence | Approved-map/story-file audit in `../verification.md`; `rg -n -F 'The remaining twenty-four C1 gates stay held without a registered owner.' _bmad-output/implementation-artifacts/epic-27-context.md` | The 24 allocation rows other than the registered identity capture have no implementation story files. | confirmed |

### Tenant and Privacy Evidence

Name all changed target selectors, tenant markers, storage/query selectors,
authorities and evidence projections when finalizing this story. Attach focused
cross-tenant denial or fail-closed test names, exact command and result wherever
those surfaces change. For physical isolation, live principal-driven denial is
mandatory. A blocked proof records owner, consequence and measurable reopen trigger;
a happy-path run, source build or retained unrelated gate cannot substitute.

## Proposed Verification

These are future registration/implementation checks, not results of this plan:

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'gate_c1_2_test.py' -v` — require nonzero tests, zero failures/errors/skips.
- `python3 tools/check-story-slice-scope.py --require-record --story-key 27-8-post-write-strong-read` — require exactly this finalized story to be checked.
- The literal producer command from the checkpoint, only after approved profile,
  authorized target, external evidence root and protected credential access exist.

## Open Prerequisites

| Owner | Outstanding input | Consequence | Reopen trigger |
| :---- | :---------------- | :---------- | :------------- |
| Developer | Literal producer and focused fixture are absent | Registration and live evidence are held | Both exist and required offline checks pass. |
| Architecture owner / Platform Operations | Revised immutable profile approval | Proposed PG-ONPREM-2 command is invalid today | Approved exact image/chart/manifest identities and new canonical profile hash. |
| Deployment Adapter Developer | Authorized target, external archive and named independent reviewer | No live execution or acceptance | Explicit scoped authorization, configured credential access and reviewed evidence retention. |
