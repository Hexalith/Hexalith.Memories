# Sprint Change Proposal — 2026-08-01 (Story 31.2 reserved `eventstore` scope and deferred-work retarget)

**Story:** `31-2-runtime-dapr-secret-store-migration`
**Workflow:** `correct-course` (Developer route)
**Decision owner:** Administrator (`jpiquot`)
**Status:** Approved 2026-08-01 by Administrator (`jpiquot`) and implemented 2026-08-01
**Change classification:** Moderate — amends one acceptance criterion and retargets a `done` gate
**Evidence base:** current worktree at HEAD `1d9e9c89ef53d877b4ec09face575c36e5889854`, plus read-only
probes against cluster context `jpiquot@local`, namespace `hexalith-memories`
**Trigger:** Story 31.2's own Open Decisions **D1** and **D2**, settled before development rather than
mid-implementation

> **Second of two proposals dated 2026-08-01 for Epic 31.** The first,
> `sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md`, made Story
> 31.2 developable. This one settles the two scope questions the story correctly routed rather than resolving
> silently. Both are distinct from `sprint-change-proposal-2026-08-01.md` (Story 27.3 registration rollback).

## 1. Issue Summary

Story 31.2 was created 2026-07-28 carrying three routed Open Decisions. **D3** was discharged by the first
2026-08-01 proposal. **D1** and **D2** remained, and both would have blocked development at Task 4 and Task 8
respectively. Re-derivation shows one is worse than recorded and the other is materially out of date.

### D1 — the `eventstore` scope is reserved, not merely unproven

AC1 requires the component to carry the `eventstore` **and** `memories` scopes, and Epic 31's
Implementation-evidence clause requires "both scopes proven by a live scoped read". The story recorded that no
workload *runs* with app-id `eventstore`. Re-derived 2026-08-01, the finding is stronger:

| Probe | Result |
| :---- | :----- |
| `kubectl -n hexalith-memories get pods -o jsonpath='{range .items[*]}{.metadata.name}{"  app-id="}{.metadata.annotations.dapr\.io/app-id}{"\n"}{end}'` | Only `memories` ×2 and `memories-mcp` ×2 carry an app-id. `access-telemetry-postgresql-0`, `falkordb-0` and `redis-stack-0` carry none |
| `grep -rn 'dapr.io/app-id' deploy/kubernetes/` | Four declarations: `memories`, `memories-mcp`, `memories-access-telemetry`, `memories-access-telemetry-clock`. **No manifest declares app-id `eventstore` anywhere** |
| `kubectl -n hexalith-memories get sa,role` | Only `serviceaccount/eventstore` (0 secrets, 12d) and `role.rbac.authorization.k8s.io/eventstore-dapr-secret-reader` |

No workload runs with that app-id **and none is declared**. EventStore is consumed as the
`references/Hexalith.EventStore` submodule and the `src/Hexalith.Memories.EventStore` project — a library
linked into the `memories` app, not a separately deployed Dapr application. The scope, its ServiceAccount and
its Role are declared for a workload that does not exist in this deployment topology.

A live scoped read for `eventstore` is therefore not merely blocked; it is **not meaningful**. Candidate (b)
from the story — deploy or temporarily run an `eventstore`-app-id sidecar — would fabricate a workload the
architecture does not have, mutate the deployed cluster the story puts out of scope, and yield a read that
proves nothing about production.

### D2 — the entry Story 31.2 targets was split one day after the story was written

Story 31.2's Task 8 and checkpoint C5 name `DW 27.3-CR17`. That entry was **split on 2026-07-29 by Story
27.3's chunk-2 code review**, one day after this story was created. The split is recorded in the entry's own
re-open trigger:

> "**Split 2026-07-29 by code review (chunk 2).** This entry now covers ONLY the Story 27.3 / checkpoint C2
> arm… The Story 31.2 arm — 'before Story 31.2 is set `done`' — moved to `DW 27.3-CR29` and remains OPEN."

Story 31.2's actual obligations are now two entries, both `open` and both naming Story 31.2 as owner:

| Entry | Title | Discharge condition |
| :---- | :---- | :------------------ |
| `DW 27.3-CR29` | the OpenBao runtime secret store is still unproven by any executed lane | an executed lane loads `secretstore` **and** `access-telemetry-secrets` at their production `secretstores.hashicorp.vault` type against a **reachable OpenBao** and resolves their consumers' secretKeyRefs |
| `DW 27.3-CR30` | no lane exercises the vault secret-resolution path at runtime | one lane loads a `secretstores.hashicorp.vault` component and resolves a secretKeyRef through it |

Meanwhile D2's original question — how to repair the disposable `kind` lane — **was already decided by the
Administrator on 2026-07-28 and executed inside Story 27.3**, via verification-scoped `secretstores.kubernetes`
substitution with a mandatory `secret-store-substitution.json` disclosure that
`tools/validate-production-deployment-evidence.ps1` fails closed on, mutation-proven in both directions
(commits `564d5d56`, `64434e57`). Story 31.2 must not redo it, and `tools/verify-production-deployment.ps1`
leaves this story's owned-path set entirely.

`CR29`'s rationale states plainly why closing both arms together was wrong: the green run "went green because
`tools/verify-production-deployment.ps1` rewrites both stores to `secretstores.kubernetes` after the verbatim
apply — it routes around the Target artifact rather than repairing it. Closing both arms on that run left
Story 31.2 free to reach `done` with nothing forcing an OpenBao-path verification."

## 2. Decisions

**D1 — Resolved: the `eventstore` scope is reserved. Amend AC1, and prove it structurally plus by denial.**
Story candidates (c) and (a) combined; candidate (b) rejected as fabrication.

**D2 — Resolved: retarget to `CR29` and `CR30`, and discharge `CR29` on `jpiquot@local`.** The live cluster
has a reachable OpenBao with the vault-typed component loaded (`memories-*` pods `2/2 Running`), so it is an
executed lane in `CR29`'s own terms. `CR29` therefore folds into Task 4's scoped read rather than becoming
separate CI work. `CR30` needs one added assertion tying the static and runtime lanes together.

Neither decision weakens a security posture. D1's denial proof is the load-bearing half and is retained in
full; D2 replaces an unreachable CI gate with a stricter, actually-executable one.

## 3. Exact Authorized Planning Changes

Approval authorizes all edits in this section as one atomic correction.

### 3.1 `epics.md` — Story 31.2 AC1 `Then` clause

OLD:

> **Then** `deploy/kubernetes/base/dapr/secretstore.yaml` uses `hashicorp.vault` with the `eventstore` and
> `memories` scopes

NEW:

> **Then** `deploy/kubernetes/base/dapr/secretstore.yaml` uses `hashicorp.vault` with the `eventstore` and
> `memories` scopes, the `memories` scope is proven by a live scoped read and the `eventstore` scope is proven
> structurally — its declared presence plus a demonstrated denial from a non-scoped app-id — because
> `eventstore` is a **reserved** scope with no deployed workload (**amended 2026-08-01** by approved Sprint
> Change Proposal 2026-08-01, resolving Story 31.2 Open Decision D1: no manifest in `deploy/kubernetes/`
> declares app-id `eventstore` and no pod carries it; EventStore is linked as a library, not deployed as a
> Dapr app, so a live read for that app-id would have to fabricate a workload the topology does not have.
> Reopen trigger: an `eventstore`-app-id workload is deployed, at which point the live read becomes both
> possible and required. Owner: Memories Maintainer)

### 3.2 `epics.md` — Story 31.2 Implementation-evidence clause

Replace "the migrated component with both scopes proven by a live scoped read" with "the migrated component
with the `memories` scope proven by a live scoped read and the reserved `eventstore` scope proven structurally
by declared presence plus a demonstrated denial (amended 2026-08-01, see AC1)".

### 3.3 Story 31.2 — AC1, Task 4, Task 8, checkpoints C1 and C5

- **AC1** mirrors §3.1.
- **Task 4** drops "resolve this before claiming AC1" and instead requires the structural-plus-denial proof for
  `eventstore`, keeping every existing denial obligation. The instruction "do not fabricate a read for an
  app-id that does not run" is retained and strengthened.
- **Task 8** is rewritten from "discharge or re-disposition `DW 27.3-CR17`" to "discharge `DW 27.3-CR29` and
  `DW 27.3-CR30`", with the CI-lane repair explicitly marked out of scope and already owned by Story 27.3.
- **C1** drops the "blocked at creation by Open Decision D1" qualifier and states the two proof modes.
- **C5** retargets from `CR17` to `CR29` + `CR30` with their real discharge conditions.

### 3.4 Story 31.2 — Open Decisions D1 and D2 marked resolved

Both keep their analysis for provenance and gain a dated resolution recording the decision, its owner, and its
reopen trigger. Neither is deleted; a routed decision that was answered is evidence, not clutter.

### 3.5 Story 31.2 — scope, ownership and reference corrections

- **Scope Boundary** — `the `DW 27.3-CR17` disposition` becomes `the `DW 27.3-CR29` and `DW 27.3-CR30`
  dispositions`; `tools/verify-production-deployment.ps1` is named **out of scope**, owned by Story 27.3.
- **Expected File Ownership** — the `deferred-work.md` row retargets to `CR29`/`CR30`; the
  `tools/verify-production-deployment.ps1` row moves from "only under D2" to `Preserve` / out of scope.
- **References** — the deferred-work link retargets.
- **Epic AC Verification** — two rows appended for the claims this proposal re-derived.

### 3.6 Story phase ledger

Append a **second** `correct-course` row to Story 31.2's Change Log. `story-phase-ledger.md` requires a
repeated phase to append another row under the same canonical name, never to overwrite the earlier one.

## 4. Epic AC Verification for this proposal

| Claim | Class | Command / evidence | Observed | Verdict |
| :---- | :---- | :----------------- | :------- | :------ |
| No pod in `hexalith-memories` carries app-id `eventstore` | Existence | `kubectl -n hexalith-memories get pods -o jsonpath=…` | Only `memories` ×2, `memories-mcp` ×2 | `confirmed` |
| No tracked manifest declares app-id `eventstore` | Absence | `grep -rn 'dapr.io/app-id' deploy/kubernetes/` | Four declarations, none `eventstore` | `confirmed` — **stronger than the story's "does not run"** |
| An `eventstore` ServiceAccount and Role exist for that absent workload | Existence | `kubectl -n hexalith-memories get sa,role` | `serviceaccount/eventstore` (0 secrets, 12d), `role/eventstore-dapr-secret-reader` | `confirmed` |
| "`DW 27.3-CR17` … names Stories 31.1 and 31.2 as owner, and its reopen trigger fires 'before Story 31.1 or Story 31.2 is set `done`'" (Story 31.2 Task 8) | Behavioral | Read the `CR17` re-open trigger in `deferred-work.md` | Split 2026-07-29; `CR17` now covers **only** the Story 27.3 / C2 arm | **`corrected`** — retargeted to `CR29` + `CR30` by §3.3 |
| `DW 27.3-CR29` is open and owned by Story 31.2 | Existence | `grep -n 'ID: 27.3-CR29' -A6 deferred-work.md` | `Status: open`; `Owner: **Story 31.2**`; trigger "before Story 31.2 is set `done`" | `confirmed` |
| `DW 27.3-CR30` is open and owned by Story 31.2 | Existence | `grep -n 'ID: 27.3-CR30' -A6 deferred-work.md` | `Status: open`; `Owner: Story 31.2, jointly with whoever next revises AC8's static assertions` | `confirmed` |
| The kind-lane repair is already applied and owned by Story 27.3 | Behavioral | `git show --stat 564d5d56`; the `CR17` evidence line | Verification-scoped `secretstores.kubernetes` substitution with validator-enforced disclosure, commits `564d5d56` / `64434e57` | `confirmed` — Story 31.2 must not redo it |
| The live cluster is an executed lane satisfying `CR29` | Behavioral | `kubectl -n hexalith-memories get pods`; Story 31.2 `### Measured Runtime State At Creation` | `memories-*` and `memories-mcp-*` are `2/2 Running`, so `daprd` loaded the `hashicorp.vault` component | `confirmed` — **Task 4 must still execute the secretKeyRef resolution; a loaded component is not a resolved secret** |

## 5. Implementation Handoff

**Scope classification: Moderate.** Planning-artifact and story-file amendment; no code, manifest, cluster,
or pipeline change. `deferred-work.md` is **not** edited by this proposal — `CR29` and `CR30` are dispositioned
by Story 31.2 at discharge time, with re-derived evidence, as its Task 8 requires.

### Success criteria

1. AC1 states the two proof modes and names `eventstore` as a reserved scope, in both `epics.md` and the story.
2. Task 4 requires structural-plus-denial proof for `eventstore` and retains every denial obligation.
3. Task 8 and C5 target `DW 27.3-CR29` and `DW 27.3-CR30`, not `CR17`.
4. `tools/verify-production-deployment.ps1` is out of Story 31.2's scope and ownership.
5. Open Decisions D1 and D2 read resolved, with owner, decision date and reopen trigger.
6. A second `correct-course` ledger row is appended without overwriting the first.
7. A subsequent `dev-story 31.2` reaches Task 1 with no unrouted decision outstanding.
