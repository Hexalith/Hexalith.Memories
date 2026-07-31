# Sprint Change Proposal — 2026-07-30

> **Citation note, added 2026-07-30 by code review.** A concurrent session created `sprint-change-proposal-2026-07-30-story-gate-hook-sequencing.md` on the same date, so the bare phrase "approved Sprint Change Proposal 2026-07-30" no longer resolves uniquely by date alone. Every such citation in `epics.md`, the Story 27.3 artifact, and `sprint-status.yaml` refers to THIS file - `sprint-change-proposal-2026-07-30.md` - which concerns the Story 27.3 C1 umbrella closure. Citations of the hook-sequencing proposal name it explicitly.

**Status:** approved for implementation — Administrator approval recorded 2026-07-30
**Author:** Developer (`correct-course`); approved by Administrator
**Triggering story:** `27-3-production-adapter-and-deployment-profile` (Epic 27)
**Executes:** the two 2026-07-29 Administrator decisions recorded as open
`[Review][Action]` items by Story 27.3's code review, chunk 2 of 3
**Scope classification:** Moderate — one acceptance-criterion ratification and backlog scope
reorganization between two existing Epic 27 stories. No PRD, architecture, UX, product code,
test, manifest, tool, pipeline, dependency, deferred-entry status, A41 state, or story status
change.

---

## 1. Issue Summary

Story 27.3's 2026-07-29 code review, chunk 2 of 3, records two Administrator decisions that a
code-review workflow could not execute:

1. **D1 — ratify the runtime secret-store substitution in AC6.** The kind lane applies the
   rendered production manifests verbatim, then changes the two vault-typed Dapr Components'
   live `spec.type` from `secretstores.hashicorp.vault` to `secretstores.kubernetes` before its
   health stages. The successful lane therefore proves the disclosed disposable-cluster
   topology, not the production OpenBao secret-resolution path. AC6 currently omits that
   deviation.
2. **D2 — transfer the remaining twelve C1 child gates to Story 27.5.** Story 27.3 still owns
   C1.13 and C1.15-C1.25. All twelve are dateless `pending | not complete | —` rows, and moving
   them leaves Story 27.3 with no C1 child gate. The Administrator resolved the non-mechanical
   disposition on 2026-07-30: retain and close Story 27.3's C1 umbrella as a scope-transfer
   record; transfer only the twelve child rows.

The decisions are coupled. D1 must make AC6 honest before C2 can be reconsidered. D2 must not
turn Story 27.3 completion into Production enablement or a Story 27.4 predecessor signal.

### 1.1 Verified claim register

Verified 2026-07-30 against worktree HEAD
`4a6f0d33689fde8335b5c7a8d429d885fa82040a` plus the preserved uncommitted code-review
patches.

| Claim quoted from the decision input | Class | Re-runnable command / evidence | Observed | Verdict |
| :----------------------------------- | :---- | :---------------------------- | :------- | :------ |
| "The two action items are the `- [ ] [Review][Action]` bullets" | Quantitative / existence | `sed -n '/2026-07-29 code review, chunk 2 of 3/,/## File Scope/p' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md \| grep -c '^- \[ \] \[Review\]\[Action\]'` | `2` | confirmed |
| "AC6 reads 'the production manifests are rendered and applied'" | Existence / location | `sed -n '50,57p' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md; sed -n '4912,4916p' _bmad-output/planning-artifacts/epics.md` | Both AC6 copies say the manifests are rendered/applied and omit the live type rewrite. | confirmed |
| "rewrites two applied Dapr Components' `spec.type`" | Behavioral / quantitative | `rg -l 'type:[[:space:]]*secretstores\.hashicorp\.vault' deploy/kubernetes/base/dapr \| sort; rg -n "kubectl @\('apply'\|secretstores\.hashicorp\.vault\|secretstores\.kubernetes\|'--replicas=0'" tools/verify-production-deployment.ps1` | Exactly `secretstore.yaml` and `access-telemetry-secrets.yaml` declare the vault type; the verifier applies at lines 823-824, discovers vault-typed live Components, patches to Kubernetes at 867-871, discloses at 886-892, then scales down at 899 before health execution. | confirmed |
| "does NOT prove the production secret-resolution path" | Behavioral | `sed -n '823,910p' tools/verify-production-deployment.ps1; sed -n '/^### DW 27.3-CR29 /,/^### DW 27.3-CR30 /p' _bmad-output/implementation-artifacts/deferred-work.md` | The verifier says the OpenBao path is not exercised; CR29 records the production path unproven. | confirmed |
| "do not close either" CR29 / CR30 | Existence / state | `for id in 29 30; do sed -n "/^### DW 27.3-CR${id} /,/^### DW /p" _bmad-output/implementation-artifacts/deferred-work.md \| sed -n '1,8p'; done` | Both entries exist with `Status: open`. | confirmed |
| "Verified count ... = 13" | Quantitative | `grep -n '^\| C1' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md \| grep -c pending` | `13`: one C1 umbrella plus C1.13 and C1.15-C1.25. | confirmed |
| "only C1.25 names an accepted blocker" | Quantitative / existence | `grep -n '^\| C1' _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md \| grep pending \| grep -c 'Accepted blocker'` | `1`, C1.25. | confirmed |
| "all thirteen block the transition to `review`" | Behavioral | `python3 tools/check-story-review-readiness.py --story-key 27-3-production-adapter-and-deployment-profile; echo $?` | Exit `1`; the story remains `in-progress`. The command's present default-branch violation is `C1: the changed set is empty for a governed story`; independently, `story-phase-ledger.md` forbids `review` while evidence rows remain pending. | confirmed |
| "Story 27.5" is the transfer target | Existence / state | `sed -n '4946,4987p' _bmad-output/planning-artifacts/epics.md; rg -n '27-5-running-pg-onprem-1-capability-qualification:' _bmad-output/implementation-artifacts/sprint-status.yaml` | Story 27.5 is registered in Epic 27 and is `backlog`. | confirmed |
| "every story it creates or amends needs its `Historical Context Classification` and `Slice Proof` records" | Existence | `sed -n '4946,4987p' _bmad-output/planning-artifacts/epics.md \| grep -Ec 'Historical Context Classification\|Slice Proof'` | `0`; this proposal corrects both omissions before recording approval. | corrected |
| "Production lifecycle writes stay disabled ... Story 27.3 reaching `done` neither enables them nor advances Story 27.4" | Existence / state | `sed -n '4908,4930p' _bmad-output/planning-artifacts/epics.md; sed -n '445,460p' _bmad-output/implementation-artifacts/sprint-status.yaml` | AC5 carries the fail-closed rule; Story 27.4 and Story 27.5 are `backlog`. | confirmed |
| "A41 remains open" | Existence / state | `sed -n '586,598p' _bmad-output/implementation-artifacts/sprint-status.yaml` | The Epic 20 A41 action is `status: open`. | confirmed |
| "13 `[Review][Patch]` items ... chunk 3 ... not started" | Quantitative / state | `sed -n '/code review of the chunk-1 remediation delta/,/2026-07-29 code review, chunk 2 of 3/p' <story> \| grep -c '^- \[ \] \[Review\]\[Patch\]'`; `sed -n '/2026-07-29 code review, chunk 2 of 3/,/## File Scope/p' <story> \| sed -n '1,8p'` | `13`; chunk-2 preamble says chunk 3 has not started. | confirmed |
| "The kind-based AC6 lane has NOT been re-run since the chunk-2 patches" | Behavioral / change | `git diff --name-status 64434e57 -- tools/verify-production-deployment.ps1 tools/validate-production-deployment-evidence.ps1 tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py; git status --short -- <same paths>` | All three paths differ from the last cited qualifying-run commit and are currently modified. The cited run cannot qualify the patched worktree. | confirmed |

`<story>` above expands to
`_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`.

No inherited PRD, architecture, or UX claim is corrected by this proposal. Their application
runtime-secret invariant remains unchanged; D1 discloses that AC6 does not prove it.

## 2. Impact Analysis

### 2.1 Epic impact

- **Epic 27 only.** Its qualification split changes from Story 27.3 owning twelve retained C1
  gates and Story 27.5 owning thirteen blocked gates to Story 27.5 owning all twenty-five C1
  child gates. Story 27.3 owns C0 and the independent C2/C3/C4 lanes.
- **Story 27.4 remains last.** Its predecessor gate requires Story 27.5 to pass all C1.1-C1.25
  at the approved profile and Story 27.3 to complete its independent non-C1 lanes.
- No new epic or story is created, no story is renumbered, and execution order remains
  `27.1 -> 27.2 -> 27.3 -> 27.5 -> 27.4`.

### 2.2 Story impact

**Story 27.3**

- AC6 is ratified as a disclosed verification-only deviation. The manifests remain the desired
  production end state; the criterion is not weakened to pretend the live substitution is that
  end state.
- C2 remains `pending | not complete | —` after this correction. Ratification becomes a
  necessary condition for later completion, not sufficient evidence. A fresh qualifying kind
  run against the chunk-2-patched worktree is still required.
- C1.13 and C1.15-C1.25 leave the C1 child-gate table. The C1 umbrella remains in Story 27.3 and
  closes administratively as `scope transferred`, explicitly not as a `passed` gate result.
- AC1-AC5 remain unchanged in place as the shared, fail-closed Production-profile contract and
  traceability record. Their proof and completion ownership is Story 27.5; Story 27.3 completion
  claims none of them passed.
- Task 1 and the stale "retained twelve rows" / 2026-07-28 narrowing statements receive
  superseding ownership notes. Historical text is preserved rather than silently rewritten.

**Story 27.5**

- Its single outcome becomes qualification of the exact running `PG-ONPREM-1` profile across
  all C1.1-C1.25 child gates.
- The existing thirteen rows and the twelve transferred rows retain identifiers and evidence
  definitions. No gate is dropped, merged, or weakened.
- The story receives the missing `Historical Context Classification`, `Slice Proof`, and a
  one-row-per-gate checkpoint table. This is the approved-checkpoint exception to the normal
  split rule; the original Story 27.3 whole-story shape remains an anti-template.
  *Corrected 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR33). As
  executed, these records reached no registration surface — they were moved to Annex A of this
  document, marked "held, not registered". The 2026-07-31 proposal registers them into
  `epics.md`: Story 27.5 (C1.15-C1.25) and new Story 27.6 (C1.1-C1.14).*
- Story 27.5 stays `backlog`. This proposal does not author its implementation story file or set
  it `ready-for-dev`.

### 2.3 Artifact conflicts

| Artifact | Impact |
| :------- | :----- |
| PRD | No change. NFR9 still requires Dapr Secrets API backed by OpenBao in deployed environments. |
| Architecture | No change. D31 and the Story 31.2 boundary still own the real OpenBao runtime path. |
| UX | Not applicable; no interface or journey changes. |
| `epics.md` | Ratify AC6; register the all-C1 ownership transfer; update Epic 27 and Stories 27.3-27.5; add Story 27.5 guard records and checkpoint table. **Corrected 2026-07-31 (DW 27.3-CR33): the diff removed these from `epics.md` rather than adding them; they are registered by the 2026-07-31 proposal across Stories 27.5 and 27.6.** |
| Story 27.3 | Mirror AC6; close the C1 umbrella as transferred; remove twelve child rows; restate completion, task ownership, classifications, slice proof, course-correction record, File Scope/List, Change Log, and the two review actions. |
| `sprint-status.yaml` | Register the amended Story 27.5 ownership and unchanged fail-closed execution order; no status value advances. |
| `deferred-work.md` | **Amended 2026-07-30 by code review, ratified by the Administrator.** The correction DID edit this file - the A41 `Backlog home` line was rewritten to cite this proposal - so the original "No edit" entry was false as executed. The edit is ratified as in-scope, matching the 2026-07-28 precedent which explicitly declared the same class of A41 reference edit in-scope. It remains status-neutral: CR29 and CR30 stay `open`, and no deferred entry's status changed. The changed-path count for this correction is therefore FIVE, not four. |

### 2.4 Technical and delivery impact

No runtime behavior changes. The lane already substitutes the Components; D1 makes the
acceptance record say so. D2 moves evidence ownership only. The implementation effort is low;
governance risk is medium because an imprecise edit could falsely imply C1 passed, C2 is current,
or Production writes may start.

## 3. Recommended Approach

Use **Direct Adjustment** within Epic 27.

- Ratify the deliberate AC6 deviation without changing the desired production end state.
- Consolidate all C1 child gates in the already-registered running-target Story 27.5.
- Close Story 27.3's C1 umbrella only as an administrative transfer record.
- Preserve AC5, the Production-write disablement, Story 27.4's backlog state, and A41's open
  state.
- Keep C2 pending until a fresh qualifying kind run covers the patched verifier.

Rollback is not useful: it would restore a misleading criterion or strand pending gates in a
story whose non-C1 lanes are independently complete-able. MVP review is not applicable; Epic 27
is post-MVP operational hardening.

**Effort:** low. **Risk:** medium if wording overclaims evidence; low after the explicit
fail-closed and guard records below. **Timeline impact:** record-only; Story 27.5 remains gated by
the existing environment activation condition.

## 4. Detailed Change Proposals

### 4.1 D1 — amend AC6 in `epics.md`

**OLD**

> 6. The kind-based production-deployment-verification lane renders and applies the production
> manifests to a disposable cluster from the four release OCI archives, produces verification
> evidence, and validates that evidence, with any failed render, apply, health, or
> evidence-validation step failing the lane. Added 2026-07-27 by approved Sprint Change Proposal
> 2026-07-27 (DW 27.3-CR15). This criterion is independent of AC1-AC5: it neither requires nor
> unblocks C1, and passing it advances no C1 gate.

**NEW**

> 6. The kind-based production-deployment-verification lane renders and applies the production
> manifests verbatim to a disposable cluster from the four release OCI archives, then — because
> that cluster has no OpenBao — discovers the two vault-typed Dapr secret-store Components in the
> rendered profile (`secretstore` and `access-telemetry-secrets`) and substitutes their live
> `spec.type` from `secretstores.hashicorp.vault` to `secretstores.kubernetes` before the health
> stages. The lane records and validates that deliberate verification-only deviation in
> `secret-store-substitution.json`; any failed render, apply, substitution, health, evidence
> production, or evidence-validation step fails the lane. The resulting cluster differs from the
> rendered production manifests; every health-stage observation occurs after the live substitution,
> so no health stage exercises either vault-typed store and the lane does **not** prove the
> production OpenBao secret-resolution path. Added 2026-07-27 by approved Sprint Change Proposal
> 2026-07-27 (DW 27.3-CR15); amended 2026-07-30 by approved Sprint Change Proposal 2026-07-30.
> This criterion is independent of AC1-AC5: it neither requires nor unblocks C1; passing it
> advances no C1 gate, enables no Production lifecycle write, and leaves AC5 unchanged.

**Rationale:** the substitution is a deliberate deviation to disclose. The desired production
manifest and OpenBao path are not weakened or declared unnecessary.

### 4.2 D1 — mirror AC6 in Story 27.3

Replace the story's Given/When/Then AC6 with the same contract:

> 6. **Given** the kind-based production-deployment-verification lane,
>    **When** `.github/workflows/ci.yml` renders and applies the production manifests verbatim to
>    its disposable cluster,
>    **Then** after the apply — because that cluster has no OpenBao — the lane discovers the two
>    vault-typed Dapr secret-store Components in the rendered profile (`secretstore` and
>    `access-telemetry-secrets`), substitutes their live `spec.type` from
>    `secretstores.hashicorp.vault` to `secretstores.kubernetes` before health execution, records
>    and validates `secret-store-substitution.json`, produces and uploads the remaining evidence,
>    and fails on any render, apply, substitution, health, evidence-production, or
>    evidence-validation error rather than skipping it. *The resulting cluster differs from the
>    rendered production manifests; every health-stage observation occurs after the live
>    substitution, so no health stage exercises either vault-typed store and this lane does not
>    prove the production OpenBao secret-resolution path. Added 2026-07-27 by approved Sprint Change
>    Proposal 2026-07-27 (DW 27.3-CR15); amended 2026-07-30 by approved Sprint Change Proposal
>    2026-07-30. This criterion is independent of AC1-AC5: it neither requires nor unblocks C1;
>    passing it advances no C1 gate, enables no Production lifecycle write, and leaves AC5
>    unchanged.*

C2 remains unchanged at `pending | not complete | —`. Its evidence cell gains:

> AC6 is ratified, but the successful run cited below predates the current chunk-2 verifier,
> validator, and tooling-test patches. C2 may leave `not complete` only after a fresh qualifying
> kind run covers those patches and the owning review phase confirms the amended AC6.

### 4.3 D2 — Epic 27 ownership amendment

Append the 2026-07-30 proposal to Epic 27's `Driven by` list and supersede the 2026-07-28
thirteen-gate split paragraph with an append-only amendment:

> **Amended 2026-07-30 by approved Sprint Change Proposal 2026-07-30.** Story 27.5 now owns all
> twenty-five C1 child gates, C1.1-C1.25. The twelve gates Story 27.3 retained on 2026-07-28 —
> C1.13 and C1.15-C1.25 — transfer with their identifiers and evidence definitions unchanged.
> Story 27.3 retains C0 and the independent C2/C3/C4 lanes; its C1 umbrella closes only as an
> administrative scope-transfer record and is not a `passed` C1 result. AC1-AC5 remain unchanged
> as the shared fail-closed Production-profile contract, but their proof and completion ownership
> belongs to Story 27.5. Production lifecycle writes remain disabled while any Story 27.5 C1 gate
> is unproven. Story 27.3 reaching `done` neither enables writes nor advances Story 27.4, and A41
> remains open and outside Story 27.3's mutation authority.

### 4.4 D2 — Story 27.3 scope disposition

Apply the following coordinated edits in both `epics.md` and the Story 27.3 artifact where the
text exists:

1. Supersede "C1 completion for this story means the retained twelve rows" with:

   > Story 27.3 owns no C1 child gate after the approved 2026-07-30 transfer. Its C1 umbrella is
   > closed as a scope-transfer record only; Story 27.3 does not claim that C1, AC1-AC5, or any
   > transferred gate passed. Completion of Story 27.3 is limited to C0 and C2-C4 plus its
   > otherwise-governed remediation and ledger obligations. It enables no Production lifecycle
   > write and cannot advance Story 27.4.

2. Add a superseding note to the 2026-07-28 Task 1 narrowing paragraph: all remaining Task 1/C1
   proof ownership now transfers to Story 27.5. Preserve the historical subtask text; do not mark
   the work executed under Story 27.3.
3. Update `Scope Transferred to Story 27.5` to enumerate C1.1-C1.25 and say that no repository
   path changes owner in this record-only correction. Existing tools and packets are read/verify
   inputs; Story 27.5 declares future producer paths when its story file is authored.
4. Remove C1.13 and C1.15-C1.25 from Story 27.3's C1 child-gate table without renumbering. Keep an
   explicit transfer note resolving every identifier to Story 27.5.
5. Retain the C1 umbrella row and change only its governance state:

   | Checkpoint | Review status | Completion state | Completion date |
   | :--------- | :------------ | :--------------- | :-------------- |
   | C1 | `reviewed 2026-07-30 (correct-course scope transfer)` | `complete — administrative scope transfer only; C1.1-C1.25 remain pending/not complete under Story 27.5, no gate passed, and Production writes remain disabled` | `2026-07-30` |

6. Amend the transition rule to state that `passed` remains the only admissible **child-gate**
   completion state; the umbrella's administrative closure is not evidence and cannot discharge a
   child gate.
7. Mark both chunk-2 `[Review][Action]` items complete only after all approved edits and checks
   land. Leave the thirteen chunk-1 `[Review][Patch]` items, chunk 3, CR28, and all unrelated
   findings untouched.

### 4.5 D2 — Story 27.5 complete gate ownership

Update Story 27.5's story statement from "thirteen capability gates" to "all twenty-five C1
capability gates" and add this origin amendment:

> **Amended 2026-07-30 by approved Sprint Change Proposal 2026-07-30.** C1.13 and C1.15-C1.25
> transfer from Story 27.3 with their identifiers, owners, and evidence definitions unchanged.
> Together with the 2026-07-28 transfer of C1.1-C1.12 and C1.14, Story 27.5 owns all C1.1-C1.25.
> Story 27.3's closed umbrella is a scope-transfer record, not qualification evidence.

Update the predecessor wording so the immutable profile identity comes from the approved
`PG-ONPREM-1` planning/ADR/profile-hash record and Story 27.3's C4 static manifest guard; it no
longer claims Story 27.3 owns C1.15-C1.18.

Update the acceptance and boundary text to cover all twenty-five rows. The existing six
acceptance criteria remain load-bearing; the transferred rows add profile identity, capacity,
declared-fault durability, backup/restore, explicit non-HA bounds, and both separated approvals
to the same exact-profile outcome. No criterion permits shared evidence to discharge multiple
rows.

#### Historical Context Classification — Story 27.5

| Reference | Classification | Permitted influence on Story 27.5 |
| :-------- | :------------- | :-------------------------------- |
| Story 27.3 whole-story shape, including its original 25-gate C1 bundle | `anti-template` | Transfer only the exact ratified C1 gate identifiers, owners, evidence definitions, and fail-closed rules. Do not copy Story 27.3's tasks, non-C1 checkpoints, File List, ledger breadth, review history, or status shape. |
| Approved 2026-07-28 C1 split proposal | `historical-reference-only` | Authority and provenance for C1.1-C1.12/C1.14 and the Story 27.5 activation gate; not a template for another partial split. |
| Approved 2026-07-30 proposal | `historical-reference-only` | Authority and provenance for all-C1 ownership, the closed Story 27.3 umbrella, and preserved fail-closed consequences. |
| Current ADR 27.1 `PG-ONPREM-1` profile and current C1 evidence definitions | `current-narrow-pattern` | Re-verified exact profile identity, thresholds, faults, and one-row-per-gate evidence contract only; whole-story shapes remain excluded. |

#### Slice Proof — Story 27.5

Story 27.5 has one outcome: accept or reject the single exact running `PG-ONPREM-1` profile.
It remains one explicitly approved checkpoint story because all twenty-five independently
verifiable C1 gates appear in one table, each with an accountable owner, its own evidence command
or artifact/accepted activation blocker, review state, completion state, consequence, and reopen
trigger. No shared umbrella state discharges a gate. Story 27.5 owns no C0, C2, C3, C4, runbook,
A41 mutation, product-code, or manifest-authoring outcome. This bounded checkpoint form satisfies
the story-scope guard without reproducing Story 27.3's former mixed C1/non-C1 umbrella shape.

#### Story 27.5 checkpoint table

Move the twelve current rows verbatim and restore the thirteen already-transferred identifiers
from their approved record. Normalize the destination to this fail-closed shape:

| Gate set | Rows | Destination state at registration |
| :------- | :--- | :-------------------------------- |
| Original 2026-07-28 transfer | C1.1-C1.12, C1.14 | `pending | not complete | —`; accepted activation blocker: no operator-executable producer can exist until the existing activation gate opens; the in-row owner, consequence, and reopen trigger remain explicit. |
| 2026-07-30 transfer | C1.13, C1.15-C1.25 | `pending | not complete | —`; retain each current owner and evidence definition. C1.25 retains its independent-security-approver blocker. No row is completed by transfer. |

The implementation edit expands this summary into twenty-five rows in `epics.md`; it does not
collapse gate identifiers or use one status cell for multiple gates.

*Corrected 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR33). No rows
were written to `epics.md`; the twenty-five rows went to Annex A of this document. They are
registered as 11 + 14 across Stories 27.5 and 27.6 by the 2026-07-31 proposal.*

### 4.6 `sprint-status.yaml`

- Keep `epic-27: in-progress`, `27-3: in-progress`, `27-4: backlog`, and `27-5: backlog`.
- Update the Story 27.5 registration comment and `story_execution_order.epic-27.reason` to say it
  owns C1.1-C1.25 and Story 27.3's completion does not advance Story 27.4.
- Preserve the order `27-1 -> 27-2 -> 27-3 -> 27-5 -> 27-4`.
- Set `# last_updated` to `2026-07-30` if the file carries that field.
- Leave the Epic 20 A41 action `open`.

### 4.7 Story 27.3 guard and ledger records

Append amendment-specific rows to `Historical Context Classification` without closing the open
chunk-1 re-derivation finding:

| Reference | Classification | Permitted influence on Story 27.3 |
| :-------- | :------------- | :-------------------------------- |
| Approved 2026-07-28 partial C1 split | `historical-reference-only` | Prior authority and the exact thirteen-gate transfer record only; its "retained twelve" sentence is superseded. |
| Approved 2026-07-30 all-C1 transfer | `historical-reference-only` | Authority for closing the C1 umbrella as transferred and retaining only C0/C2/C3/C4; never evidence that a C1 gate passed. |

Amend `Slice Proof` to say Story 27.3's checkpoint exception now covers C0/C2/C3/C4 only, each
with its own owner, evidence, review state, and completion state. C1 is a closed transfer record,
not an outcome. The open review demand to re-derive the broader historical table remains open.

Append a `correct-course` Change Log row with exact runner evidence rather than current totals
alone:

- `production_deployment_evidence`: Python unittest cases, phase delta `+0`, before/after
  `40 -> 40`; exact command
  `PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py' -v`.
- `access_telemetry_lifecycle` adapter-profile lane: Python unittest cases, phase delta `+0`,
  before/after `32 -> 32`; exact command
  `PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_adapter_profile.py' -v`.
- State explicitly that this phase changes governed records only and adds no test.
- Reconcile the cumulative File Scope/List with a literal `matched N/N`, the declared baseline,
  and the exact existing extraction/diff command. Add this proposal path to both sets.

## 5. Implementation Handoff

**Scope classification:** Moderate — governed acceptance text plus backlog reorganization between
existing stories.

| Recipient | Responsibility |
| :-------- | :------------- |
| Administrator | Approve, reject, or revise this complete proposal. Approval authorizes only the artifact edits enumerated here. |
| Developer (`correct-course`) | After approval, apply the edits to this proposal, `epics.md`, Story 27.3, and `sprint-status.yaml`; preserve unrelated dirty-worktree changes; run the validation plan; do not advance status. |
| `create-story` for Story 27.5 | When the existing activation gate opens, author the implementation story file from the approved 25-row checkpoint contract and declare any future producer paths. |
| Hexalith Platform Operations + independent security reviewer | Produce/review the running-target C1 evidence under Story 27.5. No gate is discharged by this proposal. |
| Story 27.3 owner / final code review | Obtain a fresh qualifying kind run before C2 completion; finish chunk 3 and the open chunk-1 remediation separately. |
| Story 31.2 owner | Close CR29/CR30 only with executed vault-path evidence; this proposal grants no closure. |

### Success criteria

1. AC6 in both governed artifacts discloses the live secret-store type rewrite and explicitly
   excludes the production secret-resolution path.
2. C2 remains `pending | not complete | —`; the record says a fresh qualifying kind run is owed.
3. Story 27.3's C1 umbrella is administratively complete as transferred, while no C1 child gate
   is marked passed and no C1 child row remains in Story 27.3.
4. Story 27.5 owns one explicit row for every C1.1-C1.25 gate, with identifiers preserved and no
   completion advance.
5. Story 27.5 contains valid Historical Context Classification and Slice Proof records and does
   not inherit Story 27.3's mixed non-C1 shape.

*Criteria 4 and 5 were not met as executed on 2026-07-30; corrected 2026-07-31 by approved
Sprint Change Proposal 2026-07-31 (DW 27.3-CR33). Neither was true — Story 27.5 had no story
file and no gate record on any binding surface. Both are satisfied by the 2026-07-31 proposal,
with the gate set split 11/14 across Stories 27.5 and 27.6 per DW 27.3-CR31 and CR34.*
6. Production writes stay disabled; Story 27.4 and Story 27.5 stay `backlog`; Story 27.3 stays
   `in-progress`; A41 stays open.
7. CR29, CR30, CR28, thirteen chunk-1 patches, and chunk 3 remain open/untouched.
8. The correct-course ledger row records runner-derived `+0` deltas and exact commands.

### Validation plan after approval

```text
PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py' -v
PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_adapter_profile.py' -v
python3 tools/check-story-slice-scope.py --story-key 27-3-production-adapter-and-deployment-profile
# SCOPE LIMIT (added 2026-07-31 by code review): this invocation validates the Story 27.3
# artifact ONLY. tools/check-story-slice-scope.py:499-517 examines
# _bmad-output/implementation-artifacts/<key>.md paths; epics.md and sprint-status.yaml merely set
# the `registered` flag, and Story 27.5 has no story file, so no code path in the gate ever
# examines it. A green result here is NOT evidence of Story 27.5 scope-guard compliance and must
# not be cited as such. Registering Story 27.5's record is DW 27.3-CR31.
python3 tools/check-story-review-readiness.py --story-key 27-3-production-adapter-and-deployment-profile
python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -v
git diff --check
```

The review-readiness command is expected to remain nonzero for the documented independent owner;
this proposal neither bypasses nor converts it to green evidence. The implementation report must
record its exact result.

## 6. Checklist Completion

| Checklist section | Status | Finding |
| :---------------- | :----- | :------ |
| 1. Trigger and context | [x] Done | Story 27.3 chunk-2 review; two verified Administrator decisions. |
| 2. Epic impact | [x] Done | Direct Epic 27 adjustment; no new epic/story. |
| 3. Artifact conflict | [x] Done | PRD/architecture/UX unchanged; four governed artifacts in implementation scope including this proposal. |
| 4. Path forward | [x] Done | Direct Adjustment selected; rollback/MVP review not viable. |
| 5. Proposal components | [x] Done | Before/after edits, handoff, success criteria, guards, and validation defined. |
| 6. Final review/handoff | [x] Done | Administrator approved implementation on 2026-07-30. |

## 7. Approval

- [x] Administrator approves this Sprint Change Proposal for implementation.

**Approval recorded:** Administrator replied `yes` on 2026-07-30 after the complete proposal was
presented for approval. Status: **approved for implementation**.

**Second approval recorded 2026-07-31 (DW 27.3-CR33).** Approximately 52 lines of normative
content (Annex A) and the rewritten authorization row at `:118` were added to this document
after the 2026-07-30 approval. That approval, at `:351`, authorizes "only the artifact edits
enumerated here" and, above, records approval of "the complete proposal" as presented on
2026-07-30 — so it does not cover the post-approval content. The Administrator re-approved this
document as amended on 2026-07-31, together with the four dated correction notes above, when
approving Sprint Change Proposal 2026-07-31. Status: **approved as amended**.

## Annex A — Story 27.5 checkpoint contract (superseded 2026-07-31; provenance record only)

**Superseded 2026-07-31 by approved Sprint Change Proposal 2026-07-31 (DW 27.3-CR31, CR33,
CR34).** These rows are now registered in `epics.md` — C1.15-C1.25 under Story 27.5 and
C1.1-C1.14 under Story 27.6 — and `epics.md` is authoritative wherever the two differ. C1.13's
row is rewritten there per DW 27.3-CR34: its running-target binding is retained and the passing
unit lane is a precondition, not discharge. Annex A is retained as the provenance record of the
2026-07-30 transfer and is no longer a live contract.

Moved here verbatim from `epics.md` on 2026-07-30 by code review, under the Administrator's
decision to hold the checkpoint contract in this proposal rather than register it as a live
checkpoint record. `create-story` authors it into a Story 27.5 artifact when the activation gate
opens. Holding it here changes no gate, owner, evidence definition, consequence, or reopen
trigger; the rows are reproduced unchanged so nothing is lost by the move.

#### Historical Context Classification — Story 27.5

| Reference | Classification | Permitted influence on Story 27.5 |
| :-------- | :------------- | :-------------------------------- |
| Story 27.3 whole-story shape, including its original 25-gate C1 bundle | `anti-template` | Transfer only the exact ratified C1 gate identifiers, owners, evidence definitions, and fail-closed rules. Do not copy Story 27.3's tasks, non-C1 checkpoints, File List, ledger breadth, review history, or status shape. |
| Approved 2026-07-28 C1 split proposal | `historical-reference-only` | Authority and provenance for C1.1-C1.12/C1.14 and the Story 27.5 activation gate; not a template for another partial split. |
| Approved 2026-07-30 proposal | `historical-reference-only` | Authority and provenance for all-C1 ownership, the closed Story 27.3 umbrella, and preserved fail-closed consequences. |
| Current ADR 27.1 `PG-ONPREM-1` profile and current C1 evidence definitions | `current-narrow-pattern` | Re-verified exact profile identity, thresholds, faults, and one-row-per-gate evidence contract only; whole-story shapes remain excluded. |

#### Slice Proof — Story 27.5

Story 27.5 has one outcome: accept or reject the single exact running `PG-ONPREM-1` profile. It remains one explicitly approved checkpoint story because all twenty-five independently verifiable C1 gates appear in one table, each with an accountable owner, its own evidence command or artifact/accepted activation blocker, review state, completion state, consequence, and reopen trigger. No shared umbrella state discharges a gate. Story 27.5 owns no C0, C2, C3, C4, runbook, A41 mutation, product-code, or manifest-authoring outcome. This bounded checkpoint form satisfies the story-scope guard without reproducing Story 27.3's former mixed C1/non-C1 umbrella shape.

#### C1 Checkpoint Table — Story 27.5

Every row remains `pending | not complete | —` at registration. For C1.1-C1.12 and C1.14, the accepted activation blocker is that no operator-executable producer can exist until the activation gate above opens; the required observation is preserved below and its own command must then be authored. For C1.13 and C1.15-C1.25, the transferred evidence definition remains unchanged. Transfer completes no gate.

| Gate | AC | Accountable owner | Required evidence observation, command, artifact, or accepted blocker | Consequence and reopen trigger | Review state | Completion state | Completion date |
| :--- | :-- | :---------------- | :--------------------------------------------------------------- | :----------------------------- | :----------- | :--------------- | :-------------- |
| C1.1 CRUD | AC2 | Deployment adapter owner | **Accepted activation blocker:** required running-target create/read/update/delete round trip on `state.postgresql/v2`; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.2 Strong reads | AC2 | Deployment adapter owner | **Accepted activation blocker:** required post-write strong-consistency read observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.3 ETags | AC2 | Deployment adapter owner | **Accepted activation blocker:** required ETag match/mismatch and `FirstWrite` insert-semantics observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.4 Rollback-atomic multi-key transactions | AC2 | Deployment adapter owner | **Accepted activation blocker:** required later-operation fault injection with no partial record or expiry-index commit; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.5 TTL | AC2 | Deployment adapter owner | **Accepted activation blocker:** required effective TTL-expiry observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.6 Actor reactivation | AC2 | Deployment adapter owner | **Accepted activation blocker:** required actor-state survival across deactivation/reactivation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.7 Placement / Scheduler / reminder recovery | AC2 | Deployment adapter owner | **Accepted activation blocker:** required Placement and Scheduler reconnection and reminder firing after control-plane disruption; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.8 Request bounds | AC2 | Deployment adapter owner | **Accepted activation blocker:** required request size/count bound enforcement; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.9 Two-writer 500 events/s throughput | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required `--workload-profile adr-27.1-two-writer-500eps --steady-state-minutes 30` observation: ADR mix, zero acknowledged loss, p99 below 3s, and no more than 10% p95 regression; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.10 150,000-record purge catch-up | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required `--purge-backlog-records 150000` observation: ten-minute backlog drains within five minutes and oldest-due age stays below fifteen minutes; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.11 Isolation | AC2 | Deployment adapter owner + independent security reviewer | **Accepted activation blocker:** required physical cross-tenant denial on the running profile; the one-key envelope-hash unit test is insufficient and the command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.12 Encryption | AC2 | Deployment adapter owner + independent security reviewer | **Accepted activation blocker:** required TLS `verify-full` and at-rest-encryption posture observation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.13 Capacity | AC2 | Deployment adapter owner + Hexalith Platform Operations | `AdapterProfileTests.test_capacity_inputs_fail_closed` and `AdapterProfileTests.test_capacity_result_is_admitted_against_profile_thresholds`, plus the checked-arithmetic capacity result against the approved 70/80/90% table at 1h / 24h / 7d. *(Anchor restored 2026-07-31 by code review: the governing threshold table - its byte values and the rule that exactly 80% is critical, not an admissible peak - lives in `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` under `### Production-Shaped Execution Contract`; the transfer left this row citing "the approved 70/80/90% table" with no path. Per `DW 27.3-CR34` this row's evidence stays bound to the running target and the named unit tests are a precondition, not discharge.)* | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact result is recorded, the owner transitions the row, and reviewer confirmation exists. | pending | not complete | — |
| C1.14 Cohort-attributable physical reclamation | AC2 | Deployment adapter owner + Hexalith Platform Operations | **Accepted activation blocker:** required named collector/bound and cohort-attributable physical-space reclamation; command is not authorable until activation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the activation gate opens and this row's command is authored. | pending | not complete | — |
| C1.15 Runtime and control-plane identity | AC1 | Deployment adapter owner | Packet observation: Dapr runtime version, sidecar image digest, Scheduler connections, actor types, enabled features and alpha opt-in, captured from the running deployment rather than package pins. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.16 Component and backend identity | AC1 | Deployment adapter owner | Packet observation: component type, API version, capabilities, backend identity, and PostgreSQL 18.4 version. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.17 Image, manifest and epoch identity | AC1 | Deployment adapter owner | Packet observation: application image digests, component/config manifest identity, configuration epoch, and component manifest/profile hash with its coverage statement. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.18 Node/storage capacity and operating cost | AC1 | Hexalith Platform Operations | Packet observation: node/storage capacity, host-filesystem headroom for the non-reserving local PVC, and operating cost. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.19 Declared-fault zero acknowledged-record loss | AC3 | Deployment adapter owner | Packet observation: PostgreSQL pod/process forcibly lost and its StatefulSet pod replaced while node and retained local volume remain healthy; every Dapr-acknowledged record remains present. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.20 Out-of-profile statement | AC3 | Hexalith Platform Operations | Packet observation: node, local-volume, control-plane, and site loss explicitly published as outside profile. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.21 Backup destination and successful restore | AC3 | Hexalith Platform Operations | Packet observation: named backup destination and successful restore result. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.22 Published RPO/RTO without HA claim | AC3 | Hexalith Platform Operations | Packet observation: resulting nonzero RPO and RTO for out-of-profile failures, with no node/disk/site HA claim. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact packet observation, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.23 Platform Operations approval | AC4 | Hexalith Platform Operations | Packet observation: separate approval of node/storage capacity, operating cost, operation, bounded fault, backup/restore, upgrade, rollback, and reclamation. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact approval, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.24 Non-HA acknowledgement | AC4 | Hexalith Platform Operations | Packet observation: explicit acknowledgement of absent node, disk, and site HA. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when the exact acknowledgement, owner transition, and reviewer confirmation exist. | pending | not complete | — |
| C1.25 Security reviewer approval | AC4 | Independent security reviewer | Packet observation: separate approval of identity, secrets, TLS, network, authorization, encryption, privacy, and evidence integrity. **Accepted blocker:** no independent security approver is currently assigned. | Writes disabled; Story 27.4 backlog; A41 open. Reopen when an independent approver is assigned and the exact approval, transition, and reviewer confirmation exist. | pending | not complete | — |
