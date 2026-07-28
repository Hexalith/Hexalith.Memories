# Sprint Change Proposal — 2026-07-28

**Status:** approved 2026-07-28 by Administrator
**Author:** Developer (correct-course), for Administrator approval
**Triggering story:** `27-3-production-adapter-and-deployment-profile` (Epic 27)
**Executes:** the two 2026-07-27 Administrator decisions from the eighth-invocation code review of Story 27.3 that a review workflow may not execute — the blocked-C1-gate split (D1) and the C3/C4 ratification (D2)
**Scope classification:** Moderate — backlog reorganization (one new story) plus governed-record amendments to a frozen acceptance-criteria set. No PRD, MVP, UX, or architecture-decision change; no product code, test, manifest, or pipeline change.

---

## 1. Issue Summary

The 2026-07-27 code review of Story 27.3 (eighth-invocation review, range `b073aa57..8368ce1c`)
resolved seven decisions with the Administrator. Two of them state in their own resolution text
that execution requires `correct-course` and is outside a review's mandate, and both hold Story
27.3 at `in-progress`:

| # | Finding location | Administrator resolution (2026-07-27) | Blocking effect |
| :-- | :--------------- | :------------------------------------ | :-------------- |
| D1 | `27-3-…-profile.md:342` | "split the blocked C1 gate set into a newly numbered story" | "Story 27.3 stays `in-progress` until that split lands" |
| D2 | `27-3-…-profile.md:343` | "ratify C3/C4 through correct-course plus the matching `epics.md` amendment" | "C3/C4 remain unratified until it lands" |

This proposal executes both. Neither re-litigates a decided outcome; only execution was deferred.

### 1.1 D1 — the umbrella exemption's own precondition was removed

`story-scope-guard.md:32-34` permits an approved umbrella/checkpoint story to remain one
tracking story **only** when every checkpoint carries its own owner, evidence
command/artifact, review state and completion state. `epics.md:555` states the same rule in
document form and names Story 27.3 as one of its three known instances.

On 2026-07-27, under the Administrator's option-2 decision, thirteen of the twenty-five C1 gate
rows — **C1.1–C1.12 and C1.14** — were marked `Blocked — no executable producer` rather than
being given commands nobody can run:

> no operator-executable producer can exist for a CRUD, strong-read, ETag, transaction-fault,
> TTL, actor/Scheduler, request-bound, throughput, purge-backlog, isolation, encryption or
> reclamation observation while the Production lifecycle environment is disabled.

That judgement is right, and the review recorded it as right. Its consequence is that the
exemption's stated basis no longer holds: thirteen rows now carry no evidence command at all,
so Story 27.3 no longer satisfies the only condition under which it may remain one story.

**Evidence.** The thirteen rows at `27-3-…-profile.md:501-512` and `:514`, each reading
`**Blocked — no executable producer (2026-07-27).** Required observation, via a command not yet
authorable: …`, against the twelve rows that do name a producer (C1.13 names two Python tests;
C1.15–C1.25 name observations in the `adapter-profile` evidence packet).

### 1.2 D2 — two checkpoints created without the instrument their own precedent requires

Checkpoints C3 and C4 were added to Story 27.3 on 2026-07-27 by the code-review session and
marked `complete`. The immediately preceding checkpoint, C2, was added by an approved sprint
change (`sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md`) that
also gave it a declaring acceptance criterion, AC6, in `epics.md`. C3 and C4 have neither — the
review verified their absence from every file in `_bmad-output/planning-artifacts/`.

The substance of both lanes is not in question: both are green, both are executed evidence, and
both were re-cited on 2026-07-27 against the Administrator-amended Checkpoint Execution
Contract. What is missing is the instrument. Story 27.3's own precedent — and the standing rule
that a checkpoint proves an acceptance criterion — is that adding a checkpoint requires an
approved course correction plus a matching `epics.md` amendment.

The story file currently asserts the opposite at `27-3-…-profile.md:445`
("No `epics.md` amendment is required, per the same decision"). The 2026-07-27 D2 resolution
supersedes that sentence; this proposal removes it.

---

## 2. Impact Analysis

### 2.1 Epic impact

| Epic | Impact | Can it still complete as planned? |
| :--- | :----- | :-------------------------------- |
| Epic 27 | Gains Story 27.5, which owns the thirteen environment-blocked capability gates. Story 27.3's AC2 narrows to capacity admission; AC5 extends across both stories; AC7 and AC8 are added. Story 27.4's predecessor gate extends to Story 27.5. | Yes — same scope, split across two provable slices. No gate is dropped or weakened. |
| Epic 20 | None to state. The `20.5-A41-ACCESS-TELEMETRY-RETENTION` backlog-home *reference* updates from `Stories 27.1-27.4` to `27.1-27.5`; its `carried-forward` status, resolution gate, and reopen trigger are untouched. | Yes |
| Epics 28, 29, 30, 31 | None. No file, tool, workflow, or checkpoint moves between epics. | Yes |

No epic is added, removed, or resequenced. Epic execution order is unchanged.

### 2.2 Story impact

**Story 27.3** — narrowed and clarified; stays `in-progress`.

| Element | Before | After |
| :------ | :----- | :---- |
| C1 gate rows | 25 (13 blocked, 12 with producers) | 12, every one naming a producer |
| AC2 | 14 capability/capacity items | capacity admission only (C1.13) |
| AC5 | fail-closed over 27.3/27.4/A41 | same, extended to bind the gates now owned by 27.5 |
| AC6 / C2 | unchanged | unchanged |
| C3, C4 | complete, undeclared by any AC | complete, declared by AC7 and AC8 |
| Umbrella exemption | precondition not met | met — every retained checkpoint names owner, evidence command/artifact, review state, completion state |

**Story 27.5 (new)** — `backlog`. Owns C1.1–C1.12 and C1.14, **keeping their existing gate
identifiers** so every prior citation in the story ledger, the deferred register, and the
evidence packets stays resolvable. It also owns the work of authoring each row's
operator-executed command against the running target — the piece that has no owner today.

**Story 27.4** — stays `backlog`; its predecessor gate now names Story 27.5 as well as 27.3.

### 2.3 What does *not* move

- **No repository file transfers.** The thirteen gates have no producer today, so no path
  currently implements them. `tools/verify_access_telemetry_lifecycle.py` states in its own
  docstring that it is not a behavioural prober; it stays with Story 27.3 for C1.13 and
  C1.15–C1.25. Story 27.5 declares its own `## File Scope` when `create-story` authors it.
- **No evidence packet changes.** `_bmad-output/implementation-artifacts/tests/27-3-adapter-profile-evidence.md`
  and the per-run packets stay with Story 27.3.
- **No completion state changes anywhere.** Every C1 row stays `pending` / `not complete`;
  C1 stays rejected; Production lifecycle writes stay disabled; A41 stays open.

### 2.4 Artifact conflicts

| Artifact | Conflict | Action |
| :------- | :------- | :----- |
| PRD (`prd.md`) | None — no reference to Story 27.3's C1 gate set | No change |
| Architecture (`architecture.md`) | None. `:227` cites ADR 27.1-001, not story-level gates; no Decision D1–D31 is affected | No change |
| UX specifications | Not applicable — no user-facing surface | N/A |
| `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` | None. The profile, its component block, and the approved `profile_sha256` are unchanged — this proposal moves *who proves which gate*, not what the profile is | No change |
| `epics.md` | Epic 27 preamble, Story 27.3 AC2/AC5 + new AC7/AC8, Story 27.4 predecessor gate, new Story 27.5 | Updated by this proposal |
| `27-3-production-adapter-and-deployment-profile.md` | Acceptance criteria, Task 1 subtask ownership, checkpoint preamble, C1 gate table, Dev Notes exemption basis | Updated by this proposal |
| `sprint-status.yaml` | No row for Story 27.5; no `story_execution_order.epic-27` | Updated by this proposal |
| `deferred-work.md` | A41 backlog-home reference reads `Stories 27.1-27.4` | Reference updated; no status mutated |
| Approved dated proposals (07-19, 07-20, 07-26, 07-27) | Describe a 25-row single-story C1 set | **Not edited.** Append-only; superseded by reference |
| CI/CD (`ci.yml`), deployment manifests, product code, tests | None | No change |

### 2.5 Technical impact

None. No product code, test, deployment manifest, tool, or pipeline changes. The approved
`PG-ONPREM-1` profile hash is unchanged:
`profile_sha256 dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`.

---

## 3. Recommended Approach

**Selected path: Option 1 — Direct Adjustment.**

| Option | Verdict | Reasoning |
| :----- | :------ | :-------- |
| 1. Direct Adjustment | **Viable, selected** | Effort Low, risk Low. Both outcomes are already decided; a new story inside the existing epic plus two declaring criteria is the minimum instrument that satisfies both. |
| 2. Potential Rollback | Not viable | Nothing needs reverting. Marking thirteen rows blocked was the correct call; C3 and C4 are green on executed evidence. Reopening C3/C4 to `pending` (D2 option c) would discard real proof to fix a bookkeeping gap. |
| 3. PRD MVP Review | Not viable, not needed | Epic 27 is `mvpReadiness: excluded` operational readiness. The MVP is untouched. |

**Rationale.** The two decisions pull in the same direction: Story 27.3 should assert only what
it can prove. The split removes thirteen unprovable gates from a story that would otherwise
carry them to `done` unproven; the ratification gives two lanes that *are* proven the
acceptance criteria they have been shipping without.

**Why a new story rather than a new epic:** the thirteen gates qualify the same adapter, on the
same profile hash, for the same epic outcome. Splitting them into Epic 32 would spread one
epic's stated outcome across two epics and add an epic to the execution order for no gain.

**Why the gate identifiers are preserved:** C1.1–C1.12 and C1.14 are cited across the Story
27.3 ledger, the review findings, the deferred register, and seven committed evidence packets.
Renumbering would break every citation for no benefit.

**Timeline impact:** none negative. Story 27.3's path to `done` shortens to work that is either
already proven (C3, C4), authorable today (C1.13), or blocked only on inputs it already
declares (C1.15–C1.25, C2). The thirteen genuinely blocked gates keep their reopen trigger,
now attached to a story that can be scheduled when the environment exists.

---

## 4. Detailed Change Proposals

### 4.1 D1 — `epics.md`, Epic 27 preamble

**Artifact:** `_bmad-output/planning-artifacts/epics.md`, Epic 27 header block.

```
OLD (Driven by):
**Driven by:** … and the approved Sprint Change Proposal 2026-07-20 (Story 27.3 On-Premises
PostgreSQL 18.4 Profile).

NEW:
**Driven by:** … the approved Sprint Change Proposal 2026-07-20 (Story 27.3 On-Premises
PostgreSQL 18.4 Profile), and the approved Sprint Change Proposal 2026-07-28 (blocked C1 gate
split to Story 27.5 and C3/C4 ratification).
```

**Appended to the "Qualification and close-out split" paragraph:**

> **Amended 2026-07-28 by approved Sprint Change Proposal 2026-07-28.** Story 27.5 owns the
> thirteen C1 capability gates (C1.1-C1.12 and C1.14) for which no operator-executable producer
> can exist while the `PG-ONPREM-1` lifecycle environment is disabled, and owns authoring each
> gate's producer against the running target. Story 27.3 retains profile identity capture,
> capacity admission, declared-fault durability, backup/restore, both separated approvals, and
> the three non-C1 lanes (C2, C3, C4). Production lifecycle writes remain disabled until the
> gates of both stories pass: Story 27.3 reaching `done` does not enable them. Story 27.4's
> predecessor gate is met only when Stories 27.3 and 27.5 are both `done` at the same profile
> hash.

### 4.2 D1 — `epics.md`, Story 27.3 AC2 and AC5

```
OLD (AC2):
2. CRUD, strong reads, ETags, rollback-atomic multi-key transactions, TTL, actor reactivation,
   Placement/Scheduler/reminder recovery, request bounds, two-writer 500 events/s throughput,
   150,000-record purge catch-up, isolation, encryption, capacity, and cohort-attributable
   physical reclamation all pass without skip.

NEW (AC2):
2. Capacity is proven for the 1-hour, configured 24-hour, and 7-day horizons: every operand is
   normalized to integer bytes/counts, the arithmetic is checked, and the computed result is
   admitted against the approved 70/80/90% threshold table without skip. *Narrowed 2026-07-28
   by approved Sprint Change Proposal 2026-07-28: CRUD, strong reads, ETags, rollback-atomic
   multi-key transactions, TTL, actor reactivation, Placement/Scheduler/reminder recovery,
   request bounds, two-writer 500 events/s throughput, 150,000-record purge catch-up,
   isolation, encryption, and cohort-attributable physical reclamation transfer to Story 27.5
   as gates C1.1-C1.12 and C1.14, keeping their gate identifiers. No gate is dropped, weakened,
   or made discharge-able by a unit lane.*
```

```
OLD (AC5):
5. Any missing digest, placeholder, profile drift, failed probe, missing approval, or
   unreserved capacity keeps Production writes disabled, Story 27.3 `in-progress`, Story 27.4
   `backlog`, and A41 open.

NEW (AC5):
5. Any missing digest, placeholder, profile drift, failed probe, missing approval, or
   unreserved capacity keeps Production writes disabled, Story 27.3 `in-progress`, Story 27.4
   `backlog`, and A41 open. *Extended 2026-07-28 by approved Sprint Change Proposal 2026-07-28:
   the same fail-closed rule binds the thirteen gates transferred to Story 27.5. Production
   writes remain disabled while any of them is unproven, and Story 27.3 reaching `done` neither
   enables them nor advances Story 27.4.*
```

**Rationale:** AC2 was the declaring criterion for all fourteen capability/capacity gates. After
the split it must declare only what Story 27.3 can prove; AC5's fail-closed rule must keep
binding the transferred gates, or the split would create an enable-on-`done` gap that does not
exist today.

### 4.3 D2 — `epics.md`, Story 27.3 AC7 and AC8 (new)

```
NEW (AC7):
7. The `Hexalith.Memories.AccessTelemetry.Tests` unit lane proves the `state.postgresql/v2`
   access-telemetry adapter contract — transactional record-plus-expiry-index write and delete,
   ETag and `FirstWrite` semantics, `Conflict`/`StaleIndex`/`VerificationFailed`/`AlreadyAbsent`
   classification, ordering parity, and bucket-identity matching — against in-process fakes,
   executed from a fresh Release build under the story's Checkpoint Execution Contract. Added
   2026-07-28 by approved Sprint Change Proposal 2026-07-28, ratifying checkpoint C3. This
   criterion is independent of AC1-AC5: it proves the adapter code contract, never the running
   target; passing it advances no C1 gate, enables no Production lifecycle write, and does not
   satisfy C1.11, whose cross-tenant denial must be observed against the running profile.

NEW (AC8):
8. The `ProductionDeploymentArtifactsTests` lane statically binds the reviewed `PG-ONPREM-1`
   manifests — `connectionString` resolving only through `secretKeyRef`, `actorStateStore:
   "true"`, `skipVerify`/`tlsServerName`, the ordered first-match `pg_hba` rules, least-privilege
   init-SQL grants, the RBAC secret-reader Roles, the deny-default lifecycle ACL, and the
   connection-pool arithmetic against `max_connections` including `maxSurge`/`maxUnavailable` —
   executed under its own class selector from a fresh Release build under the story's Checkpoint
   Execution Contract. Added 2026-07-28 by approved Sprint Change Proposal 2026-07-28, ratifying
   checkpoint C4. This criterion is independent of AC1-AC5: it proves the manifests say what
   they must, never that the running deployment behaves as they say; passing it advances no C1
   gate and enables no Production lifecycle write.
```

**Rationale:** exactly the C2/AC6 precedent — one declaring acceptance criterion per checkpoint,
each stated as independent of AC1–AC5. Both lanes already ship, pass, and close findings; this
makes them accountable to a criterion instead of to nothing.

### 4.4 D1 — `epics.md`, Story 27.4 predecessor gate

```
NEW bullet, appended to Story 27.4's **Predecessor Gate**:
- Story 27.5 is `done`: all thirteen transferred capability gates (C1.1-C1.12 and C1.14) are
  `passed` on operator-executed evidence from the running `PG-ONPREM-1` target at the same
  profile hash. Added 2026-07-28 by approved Sprint Change Proposal 2026-07-28. A hash mismatch
  between Stories 27.3, 27.5 and 27.4 returns ownership to Story 27.3 and keeps writes disabled.
```

### 4.5 D1 — `epics.md`, new Story 27.5

**Artifact:** `_bmad-output/planning-artifacts/epics.md`, appended to Epic 27 after Story 27.4.
Document order is numeric; the execution order of record is
`27-1 → 27-2 → 27-3 → 27-5 → 27-4` in `sprint-status.yaml`.

```
NEW:
### Story 27.5: Running PG-ONPREM-1 Capability Qualification

As a Platform Operations and security review pair,
I want the thirteen capability gates that can only be observed against the running
`PG-ONPREM-1` target proven on operator-executed evidence,
So that the Production adapter's behavior is qualified rather than asserted.

**Origin:** split out of Story 27.3 on 2026-07-28 by approved Sprint Change Proposal 2026-07-28,
executing the Administrator decision of 2026-07-27 (Story 27.3 code review, eighth-invocation
review). The thirteen gates keep their existing identifiers — C1.1-C1.12 and C1.14 — so every
prior citation in the Story 27.3 ledger, the deferred register, and the committed evidence
packets stays resolvable.

**Activation gate:** this story must not be set `ready-for-dev` until the `PG-ONPREM-1`
lifecycle Deployments are scaled above zero with the production flag and profile hash set and
the clock authorities pointed at real endpoints. Until then no operator-executable producer can
exist for any of its gates and the story stays `backlog`. This is the reopen trigger recorded
in Story 27.3's C1 gate table on 2026-07-27.

**Predecessor gate:** Story 27.3 has captured and pinned the immutable `PG-ONPREM-1` profile
identity (C1.15-C1.18) at the approved
`profile_sha256 dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`. Story 27.5
qualifies that exact profile; a hash mismatch at start returns ownership to Story 27.3.

**Acceptance Criteria:**

1. **Given** thirteen gates that no artifact in the repository can currently produce,
   **When** the running target becomes available,
   **Then** each gate's own operator-executed command is authored and recorded in its checkpoint
   row before any completion state changes, and no row is discharged by a shared or unrelated
   command.

2. **Given** the running `PG-ONPREM-1` target at the approved profile hash,
   **When** the capability probe runs,
   **Then** CRUD, strong reads, ETags, rollback-atomic multi-key transactions with a fault
   injected on a later operation and no partial record or expiry-index commit, effective TTL,
   actor reactivation, and Placement/Scheduler/reminder recovery after control-plane disruption
   all pass without skip.

3. **Given** the ADR two-writer workload at 500 events/s during purge,
   **When** the 30-minute steady-state window and the 10-minute 150,000-due-record purge backlog
   run,
   **Then** request bounds hold, acknowledged loss is zero, p99 transaction latency stays below
   the configured 3-second Dapr client timeout, p95 regression against the same-profile no-purge
   baseline stays at or below 10%, and the backlog drains within five minutes with oldest-due age
   below 15 minutes.

4. **Given** two authorized tenant contexts against the running profile,
   **When** isolation and encryption are observed,
   **Then** physical cross-tenant denial is proven with focused negative evidence naming the
   affected surfaces, per the project-context tenant-isolation rule, and TLS `verify-full` plus
   the at-rest encryption posture are recorded. The existing
   `DeleteAndVerifyAsync_EntryCarryingAnotherTenantMarker_IsDeniedAndLeavesTheRecordPurgeable`
   unit test does not satisfy this criterion: both its records resolve to one state key, so it
   exercises envelope-hash mismatch rather than tenant isolation.

5. **Given** a purge cohort,
   **When** reclamation is measured,
   **Then** the collector and its bound are named and physical space reclamation is attributed to
   that cohort.

6. **Given** any gate that is unproven, skipped, stale, or discharged by a non-running-target
   artifact,
   **When** the story is evaluated,
   **Then** Production lifecycle writes remain disabled, Story 27.4 remains `backlog`, A41
   remains open, and rejection of `PG-ONPREM-1` routes a new correct-course decision rather than
   a weakened gate or a substituted profile.

**Implementation evidence:** one checkpoint row per gate, each carrying its accountable owner,
its own evidence command or artifact, a review state, and a completion state — the
`story-scope-guard.md:32-34` condition. Owners as recorded on 2026-07-27: Deployment adapter
owner for C1.1-C1.8; plus Hexalith Platform Operations for C1.9, C1.10 and C1.14; plus the
independent security reviewer for C1.11 and C1.12.

**Boundary:** Story 27.5 does not own profile identity capture, capacity admission, declared-fault
durability, backup/restore, the two separated approvals, or the C2/C3/C4 lanes — all retained by
Story 27.3. No repository path transfers to it at this correction, because no producer exists
today; it declares its own File Scope when `create-story` authors it. It never mutates
`20.5-A41-ACCESS-TELEMETRY-RETENTION`.
```

### 4.6 D1 + D2 — Story 27.3 story file

**Artifact:** `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md`

| # | Section | Change |
| :-- | :------ | :----- |
| 1 | `Execution gate` header paragraph | Add: the 2026-07-28 correction transfers the thirteen environment-blocked C1 gates to Story 27.5; C1 completion for this story now means the retained twelve rows, and Production writes stay disabled until both stories' gates pass. |
| 2 | `## Acceptance Criteria` | AC2 narrowed, AC5 extended, AC7 and AC8 added — the same texts as §4.2 and §4.3, in the story's `Given/When/Then` form. |
| 3 | `Task 1` | Add a transfer note under the task heading: subtask 4 (the complete ADR behavioral probe), subtask 5 (multi-key transaction fault injection), and the two-writer-workload/purge-backlog half of subtask 8 move to Story 27.5. Subtasks 1, 2, 3, 6, 7, 9, 10 and 11 stay, and the threshold-table half of subtask 8 stays with C1.13. No subtask text is rewritten. |
| 4 | New `### Scope Transferred to Story 27.5 (2026-07-28)` | Records the thirteen gates, the retained twelve, the preserved identifiers, that no file transfers, and that neither transferred scope may be claimed as Story 27.3 evidence. Placed alongside the existing 27.4 and 30.1/31.1 transfer sections. |
| 5 | `## Implementation Checkpoints` preamble | Replace "No `epics.md` amendment is required, per the same decision" with the ratification record: C3 and C4 are ratified by this proposal and declared by AC7 and AC8; their completion evidence and re-citation under the amended Checkpoint Execution Contract are unchanged. |
| 6 | `#### C1 Gate Evidence Table` | Remove rows C1.1-C1.12 and C1.14; retain C1.13 and C1.15-C1.25 unrenumbered. Add a transfer note above the table naming the thirteen moved gates, their new owner story, and the fact that identifiers are preserved. |
| 7 | The 2026-07-27 "Producer correction … (option 2)" paragraph | Append a superseding note: the thirteen rows it marked blocked are now Story 27.5's, with the reopen trigger it recorded becoming Story 27.5's activation gate. The paragraph itself is append-only history and is not rewritten. |
| 8 | `### Scope and Authority` and `### Slice Proof` | Restate the exemption basis: after the transfer, every retained checkpoint (C0, C1's twelve rows, C2, C3, C4) names an owner, an evidence command or artifact, a review state, and a completion state, so `story-scope-guard.md:32-34` is satisfied by observation rather than by assertion. |
| 9 | `### Course Correction Record` | Add the 2026-07-28 approval, its active effect, scope classification, and artifacts changed. |
| 10 | `## File Scope` and `### File List` | Add this proposal's path. The two sets stay identical. |
| 11 | `## Change Log` | One `correct-course` row for this correction. |

**Explicit non-changes:** AC1, AC3, AC4 and AC6 are untouched. C0, C1.13, C1.15-C1.25, C2, C3
and C4 keep their exact review states, completion states and dates. No completion state
advances anywhere. The evidence packets, the approved profile, and the profile hash are
unchanged.

### 4.7 Sprint status

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

- `development_status`: add `27-5-running-pg-onprem-1-capability-qualification: backlog`.
- Add `story_execution_order.epic-27` with order
  `27-1 → 27-2 → 27-3 → 27-5 → 27-4` and the reason: Story 27.5 qualifies the running target
  against the profile Story 27.3 pins, and Story 27.4's deployment-shaped verification consumes
  a fully qualified adapter, so it runs last. Document order in `epics.md` is numeric; this list
  is the execution order of record.
- `# last_updated` → `2026-07-28`.

No status advances: `epic-27` stays `in-progress`, `27-3` stays `in-progress`, `27-4` stays
`backlog`, `20.5-A41-ACCESS-TELEMETRY-RETENTION` stays open.

### 4.8 Deferred work

**Artifact:** `_bmad-output/implementation-artifacts/deferred-work.md`

```
OLD (20.5-A41-ACCESS-TELEMETRY-RETENTION):
  - Backlog home: Epic 27, Stories 27.1-27.4. Story 27.3 qualifies the exact Production adapter;
    Story 27.4 owns deployment-shaped verification and close-out. Scheduling does not satisfy the
    resolution gate.

NEW:
  - Backlog home: Epic 27, Stories 27.1-27.5. Story 27.3 pins and approves the exact Production
    adapter profile; Story 27.5 qualifies the thirteen capability gates that require the running
    target (added 2026-07-28 by approved Sprint Change Proposal 2026-07-28); Story 27.4 owns
    deployment-shaped verification and close-out. Scheduling does not satisfy the resolution gate.
```

This is a backlog-home *reference* edit only, of the same class as the one Story 27.3 made under
the 2026-07-20 correction. The entry's `carried-forward` status, resolution gate, reopen
trigger, and rationale are untouched, and no A41 summary, sprint action, or Epic 20/Story 20.5
history changes.

**No deferred entry changes status.** `DW 27.3-CR17`, `CR24`, `CR25` and `CR26` stay `open` with
their existing owners and reopen triggers; D1 and D2 were recorded as review findings, not as
register entries, so none is being discharged here.

---

## 5. Implementation Handoff

**Scope classification: Moderate** — backlog reorganization plus amendments to a frozen
acceptance-criteria set.

| Recipient | Responsibility |
| :-------- | :------------- |
| Developer (this correct-course session) | Apply every edit in Section 4 on approval: `epics.md`, the Story 27.3 file, `sprint-status.yaml`, `deferred-work.md`. Run the story-scope, deferred-register, line-ending, and `access_telemetry_lifecycle` lanes to prove nothing regressed. |
| Product Owner | Confirm the Story 27.5 boundary before it is set `ready-for-dev`. Still outstanding and **not** addressed here: nominate the independent AC4 security approver for Story 27.3 (C1.25's accepted blocker). |
| `create-story` | Author the Story 27.5 file when the activation gate opens. This proposal registers the story; it does not author its story file, its checkpoint table, or its File Scope. |
| Hexalith Platform Operations | Owns the activation gate: scaling the `PG-ONPREM-1` lifecycle Deployments above zero with the production flag, profile hash, and real clock endpoints. Until then Story 27.5 cannot start. |
| `dev-story` (Story 27.3) | Continue on the retained scope only: C1.13 capacity admission, C1.15-C1.25 packet observations, and C2, whose blocking owner remains `DW 27.3-CR17`. |

**Success criteria for this correction:**

1. `epics.md` Epic 27 contains five stories; Story 27.5 carries one independently demonstrable
   outcome, an activation gate, a predecessor gate, and a one-row-per-gate checkpoint
   requirement.
2. Story 27.3's C1 gate table contains twelve rows, every one naming a producer, and no row is
   renumbered.
3. Story 27.3 declares AC7 and AC8; checkpoints C3 and C4 cite them; the "no `epics.md`
   amendment is required" sentence is gone.
4. AC5's fail-closed rule demonstrably binds the transferred gates.
5. `sprint-status.yaml` registers `27-5-…` as `backlog` with `story_execution_order.epic-27`
   recorded, and no status advances.
6. The story-scope validator, the deferred-register guard, the line-ending lane, and the
   `access_telemetry_lifecycle` lane all pass after the edits.

**Explicitly out of scope:** enabling Production lifecycle writes; advancing any C1 gate,
checkpoint, or story status; mutating `20.5-A41-ACCESS-TELEMETRY-RETENTION` state; editing any
approved dated sprint change proposal; authoring the Story 27.5 story file; changing any product
code, test, manifest, tool, or pipeline.

---

## 6. Approval

- [x] Administrator approved this Sprint Change Proposal on 2026-07-28. All edits in Section 4 were applied in the same session.

Recorded decision inputs: both executed items were resolved by the Administrator on 2026-07-27
during the eighth-invocation code review of Story 27.3 and are quoted verbatim in §1. The story
number and epic home for the split (Story 27.5 in Epic 27), and the two-criteria shape of the
C3/C4 ratification (AC7 + AC8), were selected by the Administrator on 2026-07-28.
