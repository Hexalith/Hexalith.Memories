# Sprint Change Proposal — Epic 27 Implementation-Readiness Recovery

**Date:** 2026-08-03  
**Mode:** Batch  
**Status:** Approved — implementation handoff active  
**Trigger:** `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-02-rerun.md`  
**Change scope:** Moderate backlog reorganization with Product Owner, Developer, Platform Operations, and independent security-review handoff

The Administrator approved this proposal on 2026-08-03. Approval authorizes only the scoped
planning, producer, story-creation, registration, and reconciliation work defined below. Approval
does not itself register a story, change a sprint state, enable Production lifecycle writes, close
A41, mutate a runtime, or authorize a commit or push.

## 1. Issue Summary

The 2026-08-02 implementation-readiness rerun found the complete portfolio **NOT READY** while
confirming that the active MVP plan, Epics 0-8, remains structurally ready. Three current Epic 27
planning defects cause the portfolio-level failure:

1. C1.1-C1.25 are mandatory Production qualification gates held in a proposal annex, but no
   registered story owns them and no sprint state makes them schedulable.
2. Story 27.3's binding acceptance list still contains AC1-AC5 even though every one says Story
   27.3 cannot discharge it. Its current completion contract is therefore contradictory.
3. Story 27.3 records an accepted dependency on backlog Story 30.3 for four OCI archives, even
   though the checked-in producer already exists and the current Story 27.3-owned CI job invokes
   it locally before deployment verification.

The correction must restore one current ownership truth without weakening any gate. Production
lifecycle writes remain disabled, Story 27.4 remains `backlog`, and
`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains open until all required evidence and approvals
actually pass.

### 1.1 Source-backed verification

Verified 2026-08-03 against worktree HEAD
`3f758f9ab019ca64a793e268470a7e4663cbc1fa`. The worktree is dirty with unrelated user-owned
changes; these observations are read-only and no existing change is absorbed by this proposal.

| Claim | Class | Command / evidence | Observed | Verdict |
| :---- | :---- | :----------------- | :------- | :------ |
| “No Story 27.5 or 27.6 implementation file exists.” | Existence | `find _bmad-output/implementation-artifacts -maxdepth 1 -type f \( -name '27-5-*.md' -o -name '27-6-*.md' \) -print` | No output. | `confirmed` |
| “C1.1-C1.25 are held without a registered owner.” | Existence/count | `sed -n '537,570p' _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01.md \| rg -c '^\| C1\.[0-9]+'` plus the Epic 27 sprint rows | Exactly 25 held rows; the sprint registry contains only Stories 27.1-27.4. | `confirmed` |
| “Story 27.3 has eight binding criteria, five of which it says it cannot discharge.” | Count/behavior | Extract `## Acceptance Criteria` through `## Tasks` from the Story 27.3 file and count numbered rows and the exact `cannot discharge` clause. | Eight numbered criteria; AC1-AC5 each carry the non-discharge clause. | `confirmed` |
| “The four local OCI archives already have a checked-in producer.” | Existence/location | `test -f tools/publish-containers.ps1`; inspect its image table at lines 55-85. | The script exists and names `server.tar.gz`, `mcp.tar.gz`, `access-telemetry.tar.gz`, and `access-telemetry-clock.tar.gz`. | `confirmed` |
| “The current deployment-verification job builds those archives before consuming them.” | Behavior/location | Inspect `.github/workflows/ci.yml` steps `Publish local release OCI archives` and `Verify disposable production rollout`. | The publish step is at line 456 and invokes the checked-in script; the verify step follows at line 460 and consumes all four outputs. | `confirmed` |
| “Story 30.3 is required before Story 27.3 can obtain archives.” | Behavior/dependency | Compare the current CI sequence and checked-in script with Story 30.3's status and activation gate in `epics.md`. | Current source can build the local archives in Story 27.3's job. Story 30.3 is backlog work for the broader four-image publication contract and future hardening. | `corrected` — the accepted forward-dependency statement is stale and must be superseded in its source artifacts |
| “Fresh successor identifiers 27.7-27.31 are unused.” | Existence | Search Story 27 files, Epic 27 headings, and sprint rows for `27.7` through `27.31`. | No story file, epic heading, or sprint row uses those identifiers. | `confirmed` |
| “The correction changes MVP product behavior or UX.” | Applicability | Review the PRD phase boundary, architecture Epic 27 ownership paragraph, UX specification, and the readiness qualified result. | The change repairs operational-readiness ownership and dependency records. Epics 0-8 and user-facing behavior are unchanged. | `confirmed` / no PRD or UX edit applies |

Future C1 producer behavior and Production results are intent, not current facts. No story may cite
this proposal as proof that a producer exists or a gate passed.

## 2. Impact Analysis

### 2.1 Epic impact

- **Epic 27:** cannot complete under the current plan. It needs fresh, independently scoped C1
  successors, a clean Story 27.3 completion contract, and a corrected Story 27.4 predecessor gate.
- **Epic 30:** its backlog status and external activation gate remain unchanged. Its future release
  publication work must preserve the archive interface consumed by Epic 27, but it is not an Epic
  27 predecessor.
- **Epics 0-8:** no scope, sequence, status, or acceptance change. The qualified MVP-readiness
  result remains intact.
- **Other portfolio epics:** the readiness report's non-critical phase, UX/architecture, historical
  numbering, technical-roadmap, and BDD findings remain visible follow-up work. They are not
  silently absorbed into this focused Epic 27 correction.

### 2.2 Story impact

- **Story 27.3:** retain only C0 and C2/C3/C4 as binding scope; preserve transferred C1 text as
  explicitly non-binding history; supersede the Story 30.3 forward-dependency statement.
- **Stories 27.5 and 27.6:** keep withdrawn and reserve their identifiers as historical aliases.
  Reusing them would make earlier citations ambiguous and would reproduce the rejected 11/14-gate
  split.
- **Stories 27.7-27.31:** create one new story per C1 gate. A story is registered only in the same
  bounded change that supplies its real rerunnable producer and passes the creation guards.
- **Story 27.4:** remain `backlog`; replace its dependency on nonexistent Stories 27.5/27.6 with
  the 25 fresh successors and the unchanged all-gates/same-profile requirement.

### 2.3 Artifact conflicts and required reconciliation

| Artifact | Current conflict | Required change after approval |
| :------- | :--------------- | :----------------------------- |
| `epics.md` | Story 27.3 binds AC1-AC5; Story 27.4 names withdrawn successors; C1 definitions are held and unowned. | Remove AC1-AC5 from Story 27.3's binding set, define the successor registration model, register only compliant successors, and replace Story 27.4's predecessor list. |
| Story 27.3 file | Binding criteria and several current-scope statements retain transferred C1 material; five historical statements accept an Epic 30 dependency. | Move transferred material under a non-binding historical heading, retain C0/C2/C3/C4 only, and add a dated source-backed dependency correction. Preserve append-only phase history. |
| `sprint-status.yaml` | Epic 27 order contains only 27.1-27.4 and says all C1 gates are unowned. | Add each compliant successor as `backlog`, order 27.7-27.31 before 27.4, and retain fail-closed wording until every gate passes. |
| `architecture.md` | The access-telemetry paragraph says exact C1 qualification is held without a registered owner. | Replace that sentence with the final one-gate-per-story ownership map after registration; no architecture decision changes. |
| `epic-27-context.md` | Compiled context repeats the unowned-C1 state. | Recompile or patch the context after the planning source is reconciled. |
| `deferred-work.md` | A41 and `DW 27.3-CR40` point to held Story 27.5/27.6 definitions. | Retarget to Stories 27.7-27.31 and keep status open/carried-forward until real evidence passes. |
| Story 30.3 / Epic 30 text | Archive ownership wording can be read as a prerequisite for Story 27.3. | Distinguish the available local-build contract from Story 30.3's future publication hardening and retain a non-regression obligation only. |
| PRD | No conflict with the product or MVP contract. | No change in this correction. |
| UX specification | No affected screen, flow, component, or accessibility behavior. | No change in this correction. |

`epics.md` already contains user-owned modifications. Implementation must re-read and patch only
the approved Epic 27/Epic 30 hunks; wholesale replacement is prohibited.

### 2.4 Technical and operational impact

This proposal changes planning ownership and the implementation sequence. It does not itself
change application code, manifests, CI, dependencies, a live cluster, secrets, or evidence state.
The successor work will add narrowly owned C1 producer modes and immutable evidence packets. The
common runner may be shared, but every mode must emit only its named gate's observation and one
gate's result must never discharge another.

## 3. Recommended Approach

Use **Option 1 — Direct Adjustment** inside Epic 27, classified as a **Moderate** change because it
reorganizes the backlog and requires PO/Developer coordination.

The adjustment has three coordinated parts:

1. clean Story 27.3 so its binding acceptance contract contains only C0/C2/C3/C4;
2. replace the withdrawn bundled successors with 25 narrow, fresh stories, one per C1 gate; and
3. consume the already-available, same-SHA local archive build contract in Story 27.3 while keeping
   Story 30.3 as independent future publication hardening.

### 3.1 Why this path

- Each C1 gate is independently demonstrable. One gate per story directly satisfies the historical
  slice guard and avoids recreating the withdrawn 11/14-gate anti-template.
- Fresh identifiers preserve the meaning of prior Story 27.5/27.6 citations.
- Story 27.3 already owns and executes the deployment-verification job. Binding it to the checked-in
  local archive producer at the same reviewed SHA removes the planning dependency without moving
  runtime code or weakening Epic 30's future obligations.
- The PRD, architecture decisions, UX contract, and MVP boundary remain stable.

### 3.2 Alternatives evaluated

| Option | Verdict | Reason |
| :----- | :------ | :----- |
| Re-register the prior Story 27.5/27.6 split | Rejected | It bundles 11 and 14 independently demonstrable gates, copies an anti-template, and still lacks real producers. |
| Roll back Stories 27.1-27.3 | Rejected | Completed portable implementation and current adapter/deployment-lane work remain useful; rollback does not create C1 owners. |
| Move archive production into a new earlier epic/story | Rejected | The producer and CI invocation already exist. Moving source ownership would create churn without removing a real technical blocker. |
| Resequence Epic 30 before Epic 27 | Rejected | Story 30.3 is externally activation-blocked and includes broader registry publication work not required for Story 27.3's local lane. |
| Review or reduce MVP scope | Rejected | Epics 0-8 are unaffected and already pass the qualified readiness boundary. |

### 3.3 Effort, risk, and timeline

- **Planning/reconciliation effort:** Medium. Six governed records plus 25 story files and sprint
  rows must agree.
- **Implementation/evidence effort:** High. The 25 Production gates require a running target,
  operator-owned fault/load/recovery actions, and two independent approvals.
- **Risk:** Medium after the fail-closed registration rule; High if successor stories are registered
  before their producer modes exist.
- **Timeline:** no MVP impact. Epic 27 remains in progress and Story 27.4 remains backlog until the
  successor sequence completes. The one-gate slices allow independent scheduling and review, but
  the final approval stories remain downstream of all evidence they approve.

## 4. Detailed Change Proposals

### 4.1 Story 27.3 — clean completion contract

**Section:** `## Acceptance Criteria` in both `epics.md` and
`27-3-production-adapter-and-deployment-profile.md`.

**OLD:**

> AC1-AC5 remain in the binding acceptance list and each says, “This criterion defines C1 evidence
> that Story 27.3 cannot discharge.” AC6-AC8 then define the owned C2/C3/C4 behavior.

**NEW:**

- Remove AC1-AC5 from the binding acceptance list in both copies.
- Preserve their exact prior text once under `## Historical C1 Transfer Record (Non-Binding)` in
  the story file, with a dated note that the material is provenance only and has no completion or
  registration authority.
- Retain the stable AC numbers 6, 7, and 8 to avoid breaking existing C2/C3/C4 citations. Do not
  renumber historical review or phase-ledger references.
- Remove `independent of AC1-AC5` from retained criteria. Replace it with the direct statement that
  the criterion advances no C1 gate and enables no Production lifecycle write.
- Keep the current checkpoint table binding only for C0, C2, C3, and C4. Any old C1 table or scope
  narrative remains only inside the explicitly non-binding historical record.

**AC6 archive-boundary edit:**

OLD:

> The kind-based production-deployment-verification lane renders and applies the production
> manifests verbatim to a disposable cluster from the four release OCI archives.

NEW:

> **Given** the reviewed source SHA and the checked-in local archive producer, **when** the
> `production-deployment-verification` job builds the four OCI archives from that same SHA and runs
> the disposable-cluster verification, **then** every render, apply, disposable-context,
> Component-enumeration, substitution, readback, health, evidence-production, validation, and
> upload requirement in the existing AC6 contract must pass. The local build is current Story 27.3
> execution input, not a predecessor completion from Story 30.3. Passing AC6 advances no C1 gate
> and enables no Production lifecycle write.

The remainder of AC6's current fail-closed secret-store substitution and admission-window
disclosure is retained verbatim after this Given/When/Then opening.

**Rationale:** a binding acceptance set must contain only behavior its story can complete. Stable
AC numbering and a non-binding historical annex preserve the extensive existing evidence trail
without presenting transferred work as current completion scope.

### 4.2 Story 27.3 and Epic 30 — remove the forward dependency

**OLD:**

> A forward dependency on Story 30.1/30.3 exists and is accepted because
> `tools/verify-production-deployment.ps1` requires the four archives produced by
> `tools/publish-containers.ps1`.

**NEW:**

> **Dependency correction 2026-08-03.** Story 27.3 consumes the current checked-in
> `tools/publish-containers.ps1` local-build contract through its own
> `production-deployment-verification` job at the same reviewed source SHA. This is a current-source
> input, not a dependency on Story 30.3 reaching any status. Story 30.3 owns future registry
> publication hardening and must not regress the four archive names or their ability to feed the
> Story 27.3 lane. Its backlog status and Hexalith.Builds activation gate remain independent.

Apply this correction to the current Story 27.3 dependency note, the archive-producer Task 2
subtask, Epic 30's scope boundary/downstream obligation, and any current compiled Epic 27 context.
Preserve earlier review decisions as superseded history rather than deleting them.

**Rationale:** current source and configuration refute the planning dependency. Correcting the
source artifacts is required by the epic acceptance-claim verification policy.

### 4.3 C1 successor ownership — one gate per fresh story

Stories 27.5 and 27.6 remain withdrawn. Create fresh Stories 27.7-27.31 using the mapping below.
Each story contains exactly one C1 outcome, one accountable role, one producer mode, one review
state, and one completion state.

The proposed common command contract is:

```powershell
pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.N -ProfileId PG-ONPREM-1 -EvidenceDirectory ./artifacts/access-telemetry-c1/C1.N
```

`C1.N` is replaced by the literal gate identifier in each story. This command is proposed intent,
not current evidence: the runner does not exist at proposal time. A successor may be registered
only in the same bounded change that adds or confirms its literal gate mode, adds a focused fixture
that proves the mode emits the required observation, and makes the literal command rerunnable.
Approval-only modes must validate a named, hash-bound approval artifact; they must not synthesize
approval.

| Story | Gate and single outcome | Accountable role | Required producer observation |
| :---- | :---------------------- | :--------------- | :---------------------------- |
| 27.7 | C1.1 — running-target CRUD round trip | Deployment Adapter Developer | Create/read/update/delete through `state.postgresql/v2`, with immutable request/result identities. |
| 27.8 | C1.2 — post-write strong read | Deployment Adapter Developer | Acknowledged write followed by a strong read returning the acknowledged value. |
| 27.9 | C1.3 — ETag and `FirstWrite` semantics | Deployment Adapter Developer | Match, mismatch, stale rejection, and insertion semantics as separate observations in one gate packet. |
| 27.10 | C1.4 — rollback-atomic transaction | Deployment Adapter Developer | Injected later-operation failure with no partial record or expiry-index commit. |
| 27.11 | C1.5 — effective TTL expiration | Deployment Adapter Developer | Running-target expiry observed across the accepted timing bound. |
| 27.12 | C1.6 — actor reactivation survival | Deployment Adapter Developer | State remains correct after controlled actor deactivation and reactivation. |
| 27.13 | C1.7 — Placement/Scheduler/reminder recovery | Platform Operations | Controlled disruption, reconnection, and the required reminder firing. |
| 27.14 | C1.8 — request-bound enforcement | Deployment Adapter Developer | Running-target size and count limits fail closed at their boundaries. |
| 27.15 | C1.9 — sustained two-writer workload | Platform Operations | 500 events/s for 30 minutes with the ADR latency and loss thresholds. |
| 27.16 | C1.10 — purge backlog catch-up | Platform Operations | 150,000-record backlog and bounded drain/oldest-due result. |
| 27.17 | C1.11 — physical tenant-isolation denial | Security Engineer | Running-profile cross-tenant negative evidence satisfying the exact tenant-evidence rule. |
| 27.18 | C1.12 — transport and at-rest encryption | Security Engineer | Observed TLS `verify-full` plus the approved at-rest posture on the named profile. |
| 27.19 | C1.13 — capacity admission | Platform Operations | Measured 1h/24h/7d operands and checked admission against the exact 70/80/90% byte table. |
| 27.20 | C1.14 — physical reclamation attribution | Platform Operations | Named collector/bound and cohort-attributed reclaimed physical space. |
| 27.21 | C1.15 — runtime/control-plane identity | Deployment Adapter Developer | Dapr runtime, sidecar digest, Scheduler, actor, feature, and alpha-opt-in identities. |
| 27.22 | C1.16 — component/backend identity | Deployment Adapter Developer | Component/API/capability/backend/PostgreSQL identity from the running target. |
| 27.23 | C1.17 — image/manifest/epoch/profile identity | Platform Operations | Image digests, manifest identity, configuration epoch, profile hash, and hash coverage. |
| 27.24 | C1.18 — node/storage/cost record | Platform Operations | Node capacity, storage capacity, host headroom, and operating cost tied to the profile hash. |
| 27.25 | C1.19 — declared-fault durability | Platform Operations | Forced PostgreSQL pod/process replacement with zero acknowledged loss inside the declared boundary. |
| 27.26 | C1.20 — out-of-profile statement | Platform Operations | Published node, volume, control-plane, and site exclusions tied to the profile hash. |
| 27.27 | C1.21 — backup/restore proof | Platform Operations | Named backup destination and successful restore against the same profile. |
| 27.28 | C1.22 — RPO/RTO and no-HA boundary | Platform Operations | Published nonzero RPO/RTO plus an explicit no-HA claim. |
| 27.29 | C1.23 — operations approval | Platform Operations Approver | Separate hash-bound approval of capacity, cost, operation, recovery, rollback, and reclamation evidence. |
| 27.30 | C1.24 — non-HA acknowledgement | Platform Operations Approver | Explicit hash-bound node/disk/control-plane/site non-HA acknowledgement. |
| 27.31 | C1.25 — independent security approval | Independent Security Reviewer | Separate hash-bound approval over identity, secrets, TLS, network, authorization, encryption, privacy, and evidence integrity. |

#### Registration transaction for every successor

Before any story appears in `epics.md` or `sprint-status.yaml`, the same bounded change must contain:

1. a dedicated implementation story file with one gate and no second independently demonstrable
   outcome;
2. `Historical Context Classification` with:
   - the 2026-08-01 Annex A row as `historical-reference-only`, permitted only for the gate's
     requirement/evidence semantics;
   - withdrawn Stories 27.5/27.6 and their bundled definitions as `anti-template`, permitted only
     as split provenance and never for tasks, AC density, or proof shape; and
   - any reused current runner/packet pattern as `current-narrow-pattern`, reverified against
     current source;
3. `Slice Proof` stating one gate, one owner, one literal command/artifact, one reviewer, and one
   completion state;
4. the canonical `### Epic AC Verification` table with every current quantitative, existence,
   behavioral, and location claim quoted and verified;
5. the literal producer mode and focused fixture, both present in the worktree;
6. an exact evidence command with no placeholder cell;
7. `python3 tools/check-story-slice-scope.py --require-record <story-file>` returning success; and
8. a sprint row added as `backlog`, never as `ready-for-dev`, `in-progress`, `review`, or `done`
   merely because registration succeeded.

If any item is missing, the story remains unregistered and its gate remains unowned/not complete.
No held definition, unit-only precondition, shared packet, or other gate's pass may substitute.

### 4.4 Story 27.4 — replace the impossible predecessor gate

**OLD:**

> Actual Story 27.5 and Story 27.6 files exist, are registered, and are both done; Stories 27.3,
> 27.5, 27.6, and 27.4 use the same immutable profile hash.

**NEW:**

> Actual Story 27.7 through Story 27.31 files exist, pass their creation guards, are registered by
> approved changes, and are all `done`. Every C1.1-C1.25 gate is `passed` exactly once on its own
> producer evidence. Story 27.3, all 25 C1 successors, and Story 27.4 use the same immutable
> `PG-ONPREM-1` profile hash. Any missing file, registration, producer, review, pass state, or hash
> match keeps Production lifecycle writes disabled, Story 27.4 `backlog`, and A41 open.

The Epic 27 execution order becomes:

1. Stories 27.1 and 27.2 (`done`);
2. Story 27.3 (C0/C2/C3/C4 only, currently `in-progress`);
3. identity and boundary capture Stories 27.21-27.24;
4. capability/evidence Stories 27.7-27.20 and 27.25-27.28, respecting any producer-level
   prerequisites recorded in their files;
5. approval Stories 27.29-27.31; and
6. Story 27.4 close-out.

Document order may remain numeric; `epic-27.order` is the execution order of record.

### 4.5 Architecture, deferred work, sprint state, and compiled context

**Architecture ownership sentence — OLD:**

> Exact running-target C1 qualification is held without a registered story owner until compliant
> successor files and real per-gate producers exist.

**Architecture ownership sentence — NEW after all registration transactions pass:**

> Exact running-target C1 qualification is owned one gate per story by Stories 27.7-27.31; each
> gate has its own producer, review, completion state, and immutable-profile binding. Story 27.4
> remains blocked until all 25 gates pass against the same profile hash.

Reconcile the A41 deferred entry, the Epic 20 sprint action comment, `DW 27.3-CR40`, Epic 27's
reason/order, and `epic-27-context.md` to that same ownership map. Do not mark A41 resolved, close
the sprint action, or change Story 27.4 from `backlog` during registration.

### 4.6 Non-critical readiness findings

The other readiness findings remain valid inputs but are not required to repair the three Epic 27
portfolio blockers. Route them as separate, independently approved work:

- PRD phase matrix and internal clarity corrections to Product Management;
- Evidence Packet composer, shared trust-state grammar, and latency SLO to Product Management and
  Architecture;
- WCAG/responsive product gate and Epic 17 API dependency to Product/UX/Architecture;
- technical-roadmap separation, historical ordering conventions, broad-story reopen rules, Story
  28.1's heading, and BDD normalization to backlog governance.

This proposal does not claim those findings are resolved. The next readiness rerun must report them
truthfully even if the three critical Epic 27 blockers close.

## 5. Implementation Handoff

### 5.1 Scope classification and recipients

**Moderate — Product Owner / Developer coordination.** Platform Operations and an independent
security reviewer are required evidence owners; an independent code/planning reviewer owns final
verification.

| Recipient | Responsibility |
| :-------- | :------------- |
| Product Owner | Preserve 27.5/27.6 as withdrawn; authorize and register only the fresh one-gate stories whose transaction gates pass; maintain the authoritative execution order. |
| Developer | Clean Story 27.3's binding contract, implement each literal C1 producer mode with focused fixtures, and make scoped artifact edits without absorbing existing user changes. |
| Platform Operations | Supply the approved running target, execute operational/fault/load/backup/recovery gates, own C1.13-C1.14/C1.18/C1.20-C1.24 evidence as allocated, and keep writes fail closed. |
| Security Engineer / Independent Security Reviewer | Own C1.11/C1.12 evidence and independently approve C1.25; do not accept unit-only or self-approved evidence. |
| Reviewer | Re-run every `corrected` claim, every story's Epic AC Verification commands, each producer fixture/command, story-slice validation, and cross-artifact ownership checks. |
| Readiness assessor | Re-run implementation readiness only after all approved reconciliations land; keep non-critical findings separate from the three critical-blocker result. |

### 5.2 Ordered implementation plan

1. Obtain explicit Administrator approval of this complete proposal.
2. Patch Story 27.3, its epic copy, and the Epic 30 archive boundary; verify the C0/C2/C3/C4-only
   contract before any C1 registration.
3. Create and register Stories 27.7-27.31 through the per-story registration transaction. A batch
   may prepare them together, but each story must pass independently.
4. Reconcile Story 27.4, architecture, sprint status, deferred work, and compiled Epic 27 context.
5. Run the narrow governance/tooling checks and `git diff --check`; inspect the final diff against
   the pre-existing dirty worktree.
6. Execute stories in the recorded order. Registration does not pass a gate.
7. Re-run implementation readiness after the ownership and dependency corrections are present.

### 5.3 Success criteria

1. Story 27.3's binding acceptance list contains only AC6-AC8, corresponding to C2/C3/C4, while
   C0 remains its predecessor checkpoint; transferred AC1-AC5 exist only in a visibly non-binding
   historical record.
2. Stories 27.5 and 27.6 remain withdrawn and are not reused.
3. Exactly 25 fresh story files, 27.7-27.31, map C1.1-C1.25 one-to-one with no duplicate or gap.
4. Every successor has one accountable role, a present literal producer mode, a focused fixture,
   a rerunnable evidence command, a review state, and a completion state.
5. Every successor contains compliant Historical Context Classification, Slice Proof, and Epic AC
   Verification sections, and its slice-scope command passes before registration.
6. `epics.md`, `sprint-status.yaml`, `architecture.md`, `deferred-work.md`, Story 27.3, and
   `epic-27-context.md` state the same current ownership and execution order.
7. Story 27.3's local archive build uses the checked-in producer at the reviewed SHA and has no
   predecessor-status dependency on Story 30.3; Story 30.3 retains only its future hardening and
   non-regression obligations.
8. Story 27.4 remains `backlog`, Production lifecycle writes remain disabled, and A41 remains open
   until all 25 gates and terminal close-out evidence actually pass on one profile hash.
9. The readiness rerun no longer reports the three Epic 27 critical blockers. Any remaining finding
   is reported independently and is not hidden by the correction.

## 6. Change Navigation Checklist Result

| Checklist area | Status | Result |
| :------------- | :----- | :----- |
| 1.1-1.3 Trigger/context/evidence | [x] | Story 27.3/27.4 ownership chain and the 2026-08-02 readiness report supply the trigger and concrete evidence. |
| 2.1-2.5 Epic impact/order | [x] | Epic 27 requires modification; Epic 30 stays independent/backlog; no new epic or MVP resequencing is needed. |
| 3.1 PRD | [x] | No semantic or MVP change. Non-critical PRD findings are routed separately. |
| 3.2 Architecture | [x] | One current ownership sentence changes after registration; no architecture decision changes. |
| 3.3 UX | [N/A] | No screen, flow, component, or accessibility behavior changes. |
| 3.4 Other artifacts | [x] | Story, epics, sprint registry, deferred ledger, CI/archive boundary, and compiled context impacts are explicit. |
| 4.1 Direct adjustment | [x] viable | Medium planning effort; high evidence effort; medium controlled risk. |
| 4.2 Rollback | [x] not viable | It discards useful work and does not create owners/producers. |
| 4.3 MVP review | [x] not viable | The active MVP plan is unaffected and structurally ready. |
| 4.4 Selected path | [x] | Moderate direct adjustment within Epic 27. |
| 5.1-5.5 Proposal components | [x] | Issue, impact, recommendation, exact changes, MVP statement, sequence, owners, and handoff are present. |
| 6.1-6.2 Review/accuracy | [x] | Claims are source-backed; corrected claims name their source-artifact edits. |
| 6.3 Explicit approval | [x] | Administrator replied `approve` on 2026-08-03. |
| 6.4 Sprint status update | [!] | Approved but intentionally deferred; each sprint row is added only when its per-story registration transaction passes during implementation. |
| 6.5 Final handoff | [x] | Moderate change routed to Product Owner / Developer, with Platform Operations, security, reviewer, and readiness-assessor responsibilities defined in Section 5. |

## 7. Approval

**Decision:** Approved  
**Approved by:** Administrator  
**Approval date:** 2026-08-03  
**Final scope:** Moderate / Direct Adjustment  
**Routed to:** Product Owner / Developer, with Platform Operations and independent security-review
evidence ownership  
**Implementation input:** Sections 4 and 5, including all per-story registration transactions  
**Implementation status:** Pending

The Administrator replied `approve` after the complete batch proposal was presented. Approval
authorizes only the work and artifact mutations enumerated in this proposal. Every fail-closed
registration, evidence, same-profile, status, A41, runtime, commit, push, and unrelated-worktree
guard remains binding.

## 8. Workflow Execution Log

| Date | Event | Result |
| :--- | :---- | :----- |
| 2026-08-03 | Readiness rerun accepted as the change trigger | Complete |
| 2026-08-03 | PRD, epics, architecture, UX, sprint, deferred-work, Story 27.3, and source archive boundary assessed | Complete |
| 2026-08-03 | Historical-slice and Epic AC verification guards applied | Complete |
| 2026-08-03 | Batch proposal reviewed by Administrator | Continued |
| 2026-08-03 | Sprint Change Proposal explicitly approved by Administrator | Approved |
| 2026-08-03 | Moderate Direct Adjustment routed to Product Owner / Developer and evidence owners | Complete |
