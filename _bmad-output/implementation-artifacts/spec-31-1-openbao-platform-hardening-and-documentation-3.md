---
title: '31.1: Refresh measured OpenBao platform documentation'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c64e292a46858058f6764095c8e01c2faa0ff3e0'
story_key: '31-1-openbao-platform-hardening-and-documentation'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 31.1 remains open. Fresh read-only probes on 2026-11-16 found OpenBao 2.6.2/chart label 0.29.5, ten Helm revisions and additional ingress policies. Published observations still describe 2.6.0; approved repository inputs select 2.6.4/chart 0.29.6. Evidence records C7 reopened after its October 26 waiver expiry, but other current summaries still call it waived.

**Approach:** Document the measured deployment separately from historical evidence and approved inputs, rerun the CA-only smoke Job, and bind current security checkpoint states across the records. Keep the outstanding independent evaluation visible.

## Boundaries & Constraints

**Always:** Retain three healthy Raft voters on one node as the measured availability profile. Keep exactly two accepted limitations with substantive owner, consequence, controls and reopen trigger. Preserve dated historical evidence. C4b/C5b/C7 remain pending/not complete without independent signed evidence; reviewer unassigned, Administrator accountable for scheduling. Story status stays in-progress. Helm reproduction remains Platform Operations work; Story 31.2 activation criteria are unchanged. Follow CRLF and secret-safe evidence rules.

**Never:** Deploy or downgrade the approved 2.6.4 inputs, change live ingress/RBAC, read Secret contents, invent approval, extend a waiver, migrate runtime stores, alter Production qualification or tenant routing. Live writes are limited to the named disposable smoke Job and removal of a previous completed instance if required.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Version drift | Historical 2.6.0, live 2.6.2, approved 2.6.4 | Three explicitly dated identities; owned divergence | Never infer chart digest from labels |
| Expired waiver | Unsigned evidence and expired C7 waiver | Current story, evidence and runbook agree on pending/not complete | Reject restored current waiver closure |
| Smoke execution | CA-only client against measured server | Exact commands, UTC context and observed result | Record actual failure/blocker; retain historical success |

</frozen-after-approval>

## Code Map

- `docs/operations/openbao.md`: historical profile, repository adoption, owned manifest tables, divergences and accepted limitations. Reuse these sections; preserve the jointly owned runtime Secret inventory.
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`: reuse MarkdownContractDocument, exact row/cell bindings, secret-shape scans and state-dependent waiver validation. Existing reviewer guard checks evidence but does not bind story C7 or the runbook divergence.
- `deploy/openbao/smoke-test.yaml`: pinned 2.6.0 CA-only status client; preserve its digest and one-key projection. All four owned manifests remain unchanged.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` — re-probe at implementation time and append projected read-only measurements with UTC timestamps and fresh smoke commands/results; distinguish historical waiver from current independent-review ownership.
- [x] `docs/operations/openbao.md` — add dated current deployment configuration, reconcile current operational prose and divergence ownership, and document additional policy sources without adding an accepted limitation. Preserve approved values and historical smoke results.
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` — bind current profile and smoke fields separately from historical rows; compare current C4b/C5b/C7 and reviewer-independence divergence with evidence. Prove refusal of stale version/waiver mutations in disposable copies.
- [x] `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` — reconcile checkpoints and append phase evidence, runner-derived discovery delta and matched File List.
- [x] `_bmad-output/planning-artifacts/epics.md` — correct only Story 31.1's obsolete review/Helm-gate summary to the approved checkpoint split and current status.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` — correct expired-trigger commentary if needed; preserve status values and activation criteria.

**Acceptance Criteria:**

- Given the deployed platform, when current records are read, then settings declared by all four owned manifests have measured values or explicitly owned divergences, with no secret values.
- Given multiple dated profiles, when guards run, then approved inputs and historical results cannot substitute for fresh observations or the new smoke outcome.
- Given no independent signature, when security state is checked across all current records, then C4b/C5b/C7 remain open with accountable escalation and no completed date; no automatic approval is inferred from passing tests.

## Implementation Notes

- 2026-11-16: All six execution tasks complete. Live CA-only smoke succeeded at 15:46:10Z against 2.6.2; deployment inputs remain approved 2.6.4. Final Debug/source build has zero warnings/errors; documentation 16/16 and pinned regression 1/1 passed. Six stale-record mutations refused; restored copy 16/16 passed. Matrix rows map respectively to CurrentDeploymentProfile, CurrentSecurityCheckpoints and CurrentSmokeResult facts, all executed.
- Parent diff audit verified eight owned paths, unchanged manifests and sprint status values, and unchanged frozen intent SHA-256 cb0d8eebf06f6006daade5d8cbb21f8561de84b411c5dcd39d64b6f02f80456c. The generated epic context belongs to this workflow invocation. C4b/C5b/C7 remain externally blocked; the parent story stays in-progress. Evidence: platform record section 9.5; local logs under artifacts/story-31-1/.

- Final review: all eleven layer findings (one duplicate) and parent fixture-lifecycle finding fixed; none deferred. Parent verification ran 18/18 documentation cases and 1/1 pinned regression, Debug/source build 0 warnings/errors. Final method discovery 2454→2459 (+5). Evidence §9.6 and local parent-verification.json retain exact commands and hashes.
- The generic workflow review-status synchronization is superseded by this approved frozen intent: parent Story 31.1 and all sprint status values stay in-progress/unchanged because independent security evidence remains outstanding. Only this bounded dispatch spec is done.

## Spec Change Log

## Review Triage Log

- **medium BH1 — patch:** Current voter wording infers voting membership from three Ready replicas/status, with no peer-membership observation. Qualify the three observed servers and explicitly own unmeasured voter membership; do not read credentials for this proof.
- **medium BH2 — patch:** Detailed security/probe/mount observations came from StatefulSet templates, while actual pod projection lacks those fields; OnDelete permits divergence. Add projected admitted-Pod metadata or qualify template-only claims.
- **medium BH3 — patch:** ConfigMap HCL is declared configuration, not proof of each process's loaded configuration. Label its provenance and own runtime reconciliation without changing approved inputs.
- **medium BH4 — patch:** Smoke-Pod projection captures Secret items but omits automount and mounts. Narrow both current tables/prose to Job-confirmed mount/automount and Pod-confirmed CA projection.
- **medium BH5 — patch:** ReadMatchingDatedTables only requires matching nonblank cells, so matching TODO/disabled TLS can pass. Reuse substantive-cell validation and independently pin measured security values.
- **medium BH6 — patch:** Required divergence/inventory key lists omit newly added owned gaps; deleting them bypasses disposition. Add exact key/owner/trigger requirements for these current records.
- **medium BH7 — patch:** CurrentSmokeResult never reads C3, so restoring historical checkpoint content leaves guards green. Bind C3 evidence reference, current command/outcome and completion date to the current smoke record.
- **medium BH8 — patch:** The new state guard immediately requires Not signed/unassigned/pending, even though the existing record validator has a reachable signed-evaluation branch. Reuse state-dependent validation and require consistent independently signed closure instead of permanently unsigned state.
- **low BH9 — patch:** Evidence expressly leaves Traefik custom routes unprobed; runner grants include those objects but no explicit unmeasured-route disposition exists. Add a direct owned-gap record with a probe/reconciliation trigger.
- **medium BH10 — patch:** New evidence section 9.4 is a current summary but is unread by the new cross-record guard. Bind its current states and ownership to section 4 and story checkpoints.
- **medium EC1 — patch:** Same demonstrated literal Not signed assertion as BH8 prevents a legitimate signed-evaluation transition supported by the pre-existing validator; grouped with BH8 for one state-dependent fix.
- **medium P1 — patch:** The synthetic signed-control builder still searches live pending-only prose and comments, so changing real records to signed makes the control test fail before validation. Separate fixed synthetic fixtures from actual-record inputs and exercise control generation with already signed input. Actual-record validation stays active.
- Resolution: all BH1–BH10, EC1 and P1 patches verified in the final 18-case class; observation gaps carry explicit owners/triggers, C3/current summaries are bound, and state controls pass for both pending and signed fixtures. No finding was deferred.
- Verification-gap layer: no findings. Each reviewer finding above was verified against the changed guards, recorded projection fields and surrounding state-dependent validator before grouping. All fixes are local record qualifications or extensions of existing guards; no new public surface, deployment, waiver or independent authority is introduced.

## Verification

Use `run-with-temp` for builds, mutation copies and scratch outputs; retain useful logs under `artifacts/story-31-1/`. Build Server.Tests in Debug with source references, then invoke its net11.0 assembly directly with `-class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo`. Run the existing `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal` regression and record comparable method discovery. Follow the baseline fallback ladder if source builds are blocked; record exact failures without weakening gates.

Live smoke: `kubectl --context=jpiquot@local apply -f deploy/openbao/smoke-test.yaml`; `kubectl --context=jpiquot@local -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m`; capture Job logs and admitted CA-only projection before TTL cleanup. Record failures truthfully. End with `git diff --check` and exact owned-path reconciliation.

## File Scope

Allowed to modify:

- `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md`
- `_bmad-output/implementation-artifacts/epic-31-context.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md`
- `_bmad-output/planning-artifacts/epics.md`
- `docs/operations/openbao.md`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`
- `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-3.md`

## Cross-Tenant Negative Evidence

**Not triggered:** This documentation/evidence/test-guard refresh changes no tenant or case routing, storage/query selectors, authorization, evidence attribution, or persisted data. Production qualification and runtime stores are unchanged.
