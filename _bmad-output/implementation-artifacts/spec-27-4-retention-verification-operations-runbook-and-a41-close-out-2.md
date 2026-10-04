---
title: 'Story 27.4 operator qualification and A41 close-out'
type: 'feature'
created: '2026-09-10'
status: 'awaiting-operator'
baseline_commit: '47027d35a1f4a2c6986bec85b5cae601ce1b201e'
route: 'dispatch'
review_loop_iteration: 0
operator_actions:
  - 'Use a separately approved correct-course effort to register and complete the remaining twenty-four C1 successor stories; retain one unique passing artifact for every C1.1-C1.25 gate on PG-ONPREM-1 and obtain independent profile-bound C1 approvals.'
  - 'Upgrade and requalify OpenBao to at least 2.6.2, assess the governed Helm chart requirement, and retain the reviewed security/profile evidence; no security exception is authorized.'
  - 'Obtain explicit authorization for one non-Production qualification target, an external evidence root, and the required credentials; run C0 and the reviewed C2-C4 producers only after all prerequisites pass, retaining immutable packets and disabled-gate, released-Lease, zero-replica cleanup proof.'
  - 'Obtain fresh independent C5/C6 approvals of the actual C0-C4 evidence, pass terminal validation, then perform the exact four-path A41 preflight/postflight and approved commit/publication/remote verification chain.'
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - 'docs/dev/adr-27.1-001-access-telemetry-lifecycle.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 27.4's repository work is complete, but C0-C6 evidence, approvals, and A41 close-out remain operator-pending. The C1 predecessor does not exist: only Story 27.21 is registered, C1.15 is pending, and 24 gates remain unowned.

**Approach:** Resume only after governance, security, authorization, and credential prerequisites pass. Execute the reviewed non-Production producers, retain same-profile evidence, obtain independent approvals, and apply exact A41 close-out only after terminal and publication verification.

## Decisions

- This invocation stops at verified `awaiting-operator`; it authorizes no live target or A41 action.
- Stories 27.7-27.31 and their C1 gates remain external blockers. Any work to create them requires a separate correct-course effort.
- Qualification requires OpenBao 2.6.2+ requalification and Helm chart 0.28.6 assessment; no security exception is approved by this spec.

## Boundaries & Constraints

**Always:** Require all 25 distinct C1 gates on `PG-ONPREM-1` before C2-C4; use one authorized non-Production target and external evidence root; obtain independent C1 and fresh C5/C6 approvals; restore the gate disabled, Lease released, and lifecycle/clock replicas at zero; keep Production disabled until the governed chain passes.

**Never:** Fabricate/share-discharge C1; contact a target without authorization; expose credentials or tenant content; weaken the profile, Dapr-only authority, zero overlays, or assurance claims; change `sprint-status.yaml`, `epics.md`, or historical Epic 20/Story 20.5; close A41 before postflight and publication proof.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Missing predecessor | Any C1 gate absent, reused, unowned, or not independently approved | No live C2-C4 execution and no A41 mutation | Report exact missing gate/owner and remain operator-pending |
| Authorized qualification | Complete C1 and required operator inputs | C0/C2-C4 emit immutable same-profile packets and finish disabled/zero | Emit a rejection packet, clean up, and exit nonzero |
| Retention proof | Valid journal and 1h/24h/168h cohorts | Prove purge, survivor preservation, and allocator reclamation | Reject drift, mutation, ambiguity, or skipped observations |
| A41 close-out | Passing C0-C6 and terminal packet | Change only four registered paths and prove publication | Reject dirt, races, drift, or protected changes |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md` -- completed contract and operator actions; do not reopen its implementation.
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` -- canonical matrix; keep live rows operator-pending until packets pass.
- `tools/verify-access-telemetry-lifecycle.py` -- CLI for C0, C2-C4, inventory, preflight, postflight, and publication.
- `tools/verify_access_telemetry_lifecycle.py` -- schemas, validation, terminal chain, and exact A41 registry.
- `tools/access_telemetry_producer_common.py` -- fixed orchestration, cleanup, C3 journal, and C2-C4 producers; never substitute operator commands.
- `docs/operations/access-telemetry-lifecycle.md` -- canonical procedure and recovery.
- `_bmad-output/planning-artifacts/epics.md` and `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` -- current C1 blockers; read only.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/27-{7..31}-*.md`, and external C1 packets -- verify 25 owned, complete, unique same-profile gates and independent approvals before target contact.
- [ ] `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` -- verify the OpenBao upgrade/requalification or approved exception.
- [ ] `tools/verify-access-telemetry-lifecycle.py` and `docs/operations/access-telemetry-lifecycle.md` -- with authorization and inputs, run C0/C2-C4 and retain cleanup proof and packets.
- [ ] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` -- obtain fresh independent C5/C6 approvals and validate exact C0-C6 with no skip/failure.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, and `docs/dev/telemetry.md` -- after preflight, apply approved semantics and complete postflight, commit, publish, and remote verification.

**Acceptance Criteria:**
- Given any prerequisite is missing, when 27.4 is evaluated, then no live mutation occurs, Production stays disabled, A41 stays open, and the blocker is named.
- Given an authorized run, when a producer ends, then immutable evidence captures the result and proves disabled gate, released Lease, and zero replicas.
- Given passing C0-C6 and terminal evidence, when close-out runs, then only four registered paths change, protected bytes remain identical, and fetched-remote containment proves publication.

## Implementation Notes

### 2026-10-04 verified operator handoff

This invocation ends at `awaiting-operator`. No code delta remains in the completed
27.4 machinery. All three frontmatter context files and the required Hexalith
baseline were loaded before verification. The frozen intent and all five live
execution checkboxes remain unchanged: none of those operator tasks has passed.

The Story 27.21 record and compiled Epic 27 context now record `done` and an
independently accepted/complete C1.15 identity capture. The frozen problem's
pending-C1.15 statement and the older `epics.md` registration wording are retained
as historical inputs; they supply no current gate credit. This invocation verified
the existing local evidence without querying its target:

- [Observed C1.15 packet](../../artifacts/access-telemetry-c1/C1.15/c1.15-runtime-control-plane-identity-20261004T104156676Z-af39ddb5939044f38e438e6e584c3268.json): SHA-256 `17d7f350c3193ce6663364b0b4e6ef52d8f319f4ca4c0a25e9886a006ff0ed87`, 4,429 bytes, mode `0444`.
- [Independent disposition](../../artifacts/access-telemetry-c1/C1.15/c1.15-independent-framed-packet-review-20261004T104839838701Z-1b6be26b1d604a50bc53eefcc2203278.json): SHA-256 `8a70077297e68962f12da991d76c37a58a2faa86697ea0e2212f590941e62c73`, 32,097 bytes, mode `0444`; reviewer `/root/review_c1_packet`.

Both file hashes and immutable modes were recomputed locally. The packet remains
`producerStatus: observed`, `gateStatus: not-evaluated`, and
`productionGatePassed: false`. The disposition accepts only C1.15 identity capture;
it grants no Production acceptance or write authorization, other C1 discharge,
C1.25 approval, human sign-off, Story 27.4 advancement, or A41 closure. These ignored
local artifacts have not been archived to an external evidence root by this
invocation, and the existing external-retention follow-up remains open.

The current canonical profile SHA-256 is
`dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14` and workload
SHA-256 is `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.
Recomputing those repository identities supplies no running-profile proof.

### Exact prerequisite audit

The registration audit found only `27-21-runtime-and-control-plane-identity.md`
among the 25 candidate Story 27.7-27.31 files. Candidate story numbers below follow
the approved 2026-08-03 allocation; listing them does not register a story, assign
an owner, or authorize its implementation.

| Gate | Candidate story | Registered owner | Current evidence / blocker |
| :--- | :-------------- | :--------------- | :------------------------- |
| C1.1 | 27.7 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.2 | 27.8 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.3 | 27.9 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.4 | 27.10 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.5 | 27.11 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.6 | 27.12 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.7 | 27.13 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.8 | 27.14 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.9 | 27.15 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.10 | 27.16 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.11 | 27.17 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.12 | 27.18 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.13 | 27.19 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.14 | 27.20 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.15 | 27.21 | Deployment Adapter Developer | Story `done`; independently accepted/complete runtime/control-plane identity capture only; Production gate remains `not-evaluated`. |
| C1.16 | 27.22 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.17 | 27.23 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.18 | 27.24 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.19 | 27.25 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.20 | 27.26 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.21 | 27.27 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.22 | 27.28 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.23 | 27.29 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.24 | 27.30 | No registered owner | Story file absent; gate held and no passing successor packet established. |
| C1.25 | 27.31 | No registered owner | Story file absent; gate held and no passing successor packet established. |

A complete canonical C1 predecessor with 25 distinct passing gate artifacts and
independent Operations/Security approvals has not been established. The C1.15
capture and its reviewer cannot discharge any missing row or substitute for the
canonical predecessor. The existing verifier rejects missing, reused, unpassed,
profile-mismatched, or unapproved gate inputs before live C2-C4 producers run;
the passing offline fixtures exercise these refusals.

Security qualification also remains unsatisfied. `deploy/openbao/values.yaml`
still pins OpenBao `2.6.0` and chart `0.28.5`; the loaded Architecture Spine requires
OpenBao `2.6.2` or later with requalification and assessment of the current
`0.29.x` chart line, beyond the frozen spec's historical `0.28.6` assessment.
That governance discrepancy is recorded for the separate prerequisite effort;
this invocation changes no dependency/profile pin and approves no exception.
The same spine retains the PostgreSQL `18.4` security-upgrade/requalification
blocker, which likewise supplies no permission to alter `PG-ONPREM-1` here.

This invocation authorizes no live target. No authorized non-Production scenario,
external evidence-root input, credential input, passing C0-C6 chain, fresh C5/C6
approvals, or terminal/publication proof has been established for execution.
Consequently C0/C2-C4 producers, A41 inventory/preflight/postflight, commit/push,
and publish verification were not invoked. The canonical C0-C6 matrix remains
unchanged, with live rows `operator-pending`; A41 DW-17 stays `open` and
`carried-forward`.

Repository checks confirm `ACCESS_TELEMETRY_ENABLED=false` in the Production
configuration, both Production lifecycle/clock replicas at zero, and the
qualification gate disabled with an empty Lease. This is static repository
verification, not a current-cluster observation or cleanup receipt. The prior
C1.15 capture's separately authorized live replica is not asserted to have been
zeroed. A future authorized producer must independently prove its initial state
and retain actual gate-disabled, released-Lease, lifecycle-zero, and clock-zero
cleanup evidence.

### Local verification results

| Command | Result |
| :------ | :----- |
| `python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` | Exit 0; 69/69 passed in 18.079 seconds; zero failures, errors, or skips. |
| `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release` | Exit 0; zero warnings/errors in 17.82 seconds. |
| `dotnet build tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj -c Release` | Exit 0; zero warnings/errors in 5.72 seconds. |
| `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -parallel none -noLogo` | Exit 0; 11/11 passed, zero errors/failures/skips/not-run. Runner reported its existing `-parallel` deprecation notice. |
| `git diff --check` | Exit 0; no whitespace errors. |

The frozen block SHA-256 remains
`dc226668697ca9c6eddb1c55f94656d40da2dc898a9e0adff12720efcc83a725`.
A before/after byte audit preserves the canonical matrix, predecessor spec,
A41 four-path close-out documents, `sprint-status.yaml`, `epics.md`, historical
Story 20.5, Production manifests, qualification gate/Lease, and C1.15 collector.
Only this spec's operational metadata and implementation notes changed; no live
mutation, staging, commit, push, or dependency update occurred.

## Spec Change Log

- 2026-10-04: Recorded verified `awaiting-operator` status, imperative operator
  actions, current accepted C1.15 capture limits, exact missing gate/owner audit,
  unresolved security prerequisites, and passing local checks. Frozen intent,
  live execution checkboxes, canonical matrix, and protected artifacts are unchanged.

## Review Triage Log

The three context-free workflow reviewers completed against the single-file diff:
`/root/review_27_4_edges` returned no findings and
`/root/review_27_4_verification` found no verification gaps. Blind Hunter returned
four documentary improvements (finding floor 4). Each was independently checked
before triage. No implementation patch, loopback, or deferred-ledger entry resulted:
step-04 expressly rejects findings whose fix edits this build's spec. The following
observations preserve the future prerequisite guidance without changing approved
intent or granting any live gate credit.

| Finding | Verdict | Evidence and disposition |
| :------ | :------ | :----------------------- |
| B1: Include PostgreSQL security requalification in the frontmatter action list. | `low` | The Architecture Spine's AD-19 PostgreSQL row requires upgrading and requalifying the 18.4 store, owned by Jérôme Piquot with review 2026-10-31. This prerequisite is already explicitly recorded above; the shorter action list omits its name. Reject the suggested spec edit under step-04; the blocker remains unsatisfied. |
| B2: Explain approved immutable-profile revision after the security upgrade. | `low` | `_validate_predecessor` requires the canonical `STORY_27_4_PROFILE_SHA256`; changing PostgreSQL/profile bytes cannot pass against the old hash. The handoff already forbids altering PG-ONPREM-1 here. A separately governed prerequisite effort must approve the revised profile, reconcile its verifier identity, and requalify matching evidence; upgrade evidence alone is insufficient. Reject the proposed spec clarification under step-04. |
| B3: Explicitly require fresh qualification-session C1.15 capture. | `low` | `run_story_27_4_producer` invokes `_validate_predecessor(..., require_authorization_freshness=True)`, which checks every gate; the passing `test_c1_freshness_is_authorization_only_and_retained_evidence_stays_valid` verifies rejection of stale session inputs. Historical accepted capture stays valid only as retained evidence and cannot authorize a later run: recapture/review is required when freshness expires. Existing rejection and capture-only warnings prevent false gate credit; reject the suggested spec edit under step-04. |
| B4: Name the existing evidence-archive follow-up and its owner. | `low` | Open `27.21-C1.15-EVIDENCE-RETENTION` is owned by the Deployment Adapter Developer in `deferred-work.md`: before external consumption, assign retention ownership and an approved retrievable archive, preserve packet/review and all bound collector source bytes, and independently verify hashes and retrieval. The handoff already discloses local-only artifacts and the open follow-up. Reject the suggested spec-link edit under step-04; no archive publication or closure is claimed. |

The matrix audit is satisfied by existing tests executed in the passing 69-test
lifecycle tooling run: missing predecessors by
`test_c1_requires_unique_25_gate_artifacts_disabled_production_and_authorization`;
qualification packets/cleanup by `test_complete_c2_c3_and_c4_packets_validate`
and `test_producer_restores_disabled_state_when_body_fails_after_enable`;
retention/journal validation by
`test_c3_horizons_use_emission_time_and_bind_the_final_newer_control` and
`test_c3_journal_is_exclusive_append_only_and_tamper_evident`;
exact close-out/publication by
`test_mutation_manifest_is_exact_and_verifier_owned` and
`test_registered_producers_and_complete_close_out_chain`.
These are offline fixture contracts, not executed live acceptance evidence.
All five operator tasks remain incomplete. Final status follows the frozen decision
"This invocation stops at verified `awaiting-operator`"; it does not use the generic
step-05 `done`, sprint-sync, or commit route to bypass the missing predecessors.

## Design Notes

No code delta remains: focused checks pass and the prior spec is `awaiting-operator`. C2-C4 disrupt the cluster, C3 purge/reclamation is irreversible, evidence writes are exclusive, and commit/push changes external state.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` -- expected: 69 pass without target access.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release` -- expected: no warnings/errors.
- `dotnet build tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj -c Release` -- expected: no warnings/errors.
- `git diff --check` -- expected: no whitespace errors and no protected-path mutation.
