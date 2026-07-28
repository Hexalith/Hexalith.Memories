# Sprint Change Proposal 2026-07-28 — Story 31.1 Deployed-Profile AC2 Ratification

**Date:** 2026-07-28
**Author:** `correct-course` (Developer)
**Approver:** Administrator
**Trigger story:** Story 31.1 — OpenBao Platform Hardening and Documentation (`ready-for-dev`, created 2026-07-28)
**Scope classification:** Minor — one acceptance-criterion wording amendment plus its story-file alignment. No story is added, removed, renumbered, split, or advanced.

---

## 1. Issue Summary

Story 31.1's AC2 opens with **"Given the single-node deployment profile"**. Story creation measured the running platform read-only and found that premise partly false: the OpenBao `hexalith-keys` deployment is a **three-voter Raft cluster with `ha_enabled: true`**, not the single Raft node its tracked `values.yaml` declares.

Writing the documentation to satisfy the AC's literal wording would put a false single-node claim into `docs/operations/openbao.md` — the precise failure AC2 exists to prevent. Leaving the wording unamended would leave the story unsatisfiable in good faith.

**A second measurement resolves it favourably.** The Kubernetes cluster has exactly **one node** (`node1`, control-plane+worker), and all three OpenBao voters are co-located on it. So:

- "single-node" is **wrong** about the Raft topology — there are three voters, with leader election and `-active`/`-standby` Services.
- "single-node" is **right** about the hosting and the failure domain — one node, one kernel, one disk holds the entire quorum. Losing `node1` loses everything.

AC2's intent — record the limitations honestly, claim no HA — is therefore fully intact and, on the evidence, strengthened. Only the phrase's referent needs to be made explicit.

### Evidence

Read-only probes, context `jpiquot@local`, 2026-07-28. No Secret contents were read.

| Observation | Command | Result |
| :---------- | :------ | :----- |
| Node count | `kubectl get nodes -o wide` | one node, `node1`, `control-plane,worker`, `v1.34.9` |
| Voter placement | `kubectl -n openbao get pods -o custom-columns=NAME:.metadata.name,NODE:.spec.nodeName` | `hexalith-keys-0/1/2` all on `node1` |
| Replica count | `kubectl -n openbao get statefulset hexalith-keys -o jsonpath='{.spec.replicas}'` | `3` |
| HA mode | `kubectl -n openbao exec hexalith-keys-0 -- env BAO_CACERT=… bao status -format=json` | `ha_enabled: true`, `initialized: true`, `sealed: false`, `storage_type: raft`, `version: 2.6.0` |
| Seal | `kubectl -n openbao get cm hexalith-keys-config -o jsonpath='{.data}'` | `seal "static"`, `current_key = file:///openbao/userconfig/openbao-seal/current.key` |
| Ingress | `kubectl -n openbao get networkpolicy hexalith-keys -o jsonpath='{.spec}'` | namespace-wide 8200 from `hexalith-memories` **and** `cert-manager` |

**Both limitations AC2 names are verified and unchanged in substance.** The static file-based seal keeps its key in Secret `openbao-seal` in the same namespace as the data PVCs. The NetworkPolicy admits every pod in `hexalith-memories` (7 at measurement) plus every pod in `cert-manager` on port 8200.

---

## 2. Impact Analysis

### Epic impact

**Epic 31 completes as planned.** No epic is added, removed, deferred, or redefined. Story 31.1's outcome — a documented platform with its accepted limitations on record — is unchanged. Story 31.2's activation gate (31.1 `done` first) is unchanged, and its own acceptance criteria contain no single-node premise.

No other epic is affected. No epic order or priority changes.

### Story impact

Story 31.1 only. Its AC1 and AC3 are untouched. Its checkpoint rows C4 and C5 — the two accepted limitations — stay in scope with their owners and evidence unchanged; only the label describing the profile they belong to is made accurate.

### Artifact conflicts

| Artifact | Impact |
| :------- | :----- |
| `_bmad-output/planning-artifacts/epics.md` | **Amend.** Story 31.1 AC2 premise, its "accepted single-node limitations" clause, and the matching Implementation-evidence clause |
| `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` | **Amend.** Restate AC2 to match, convert the open-decision section into a resolved ratification record, and record the one-node measurement |
| `_bmad-output/planning-artifacts/prd.md` | **No conflict.** NFR9 governs the secret boundary and makes no availability claim |
| `_bmad-output/planning-artifacts/architecture.md` | **No conflict.** D31 defines the secret-provider invariant, not the platform's availability. Its line 227 "single-node on-premises cluster" refers to the Kubernetes cluster and is **confirmed correct** by this measurement |
| UX specifications | **N/A.** No user-facing surface |
| `docs/operations/openbao.md` | **No amendment here.** Its stale claims are already Story 31.1 Task 3's deliverable |
| `deploy/openbao/**` | **No amendment here.** Reconciliation is already Story 31.1 Task 2's deliverable |
| `_bmad-output/implementation-artifacts/deferred-work.md` | **No amendment.** `DW 27.3-CR6` is `resolved` and records what was approved on 2026-07-21. Dated approval records are not rewritten |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | **No change.** No epic or story is added, removed, renumbered, or advanced |
| `deploy/kubernetes/base/access-telemetry-postgresql.yaml` | **No conflict.** Its `hexalith.io/availability: single-node-non-ha` annotation describes the PostgreSQL adapter, not OpenBao |
| CI/CD, tests, monitoring | **No impact.** The documentation guard is new work already specified in Story 31.1 Task 5 |

### Technical impact

None. This proposal changes no code, manifest, or deployed resource. The live platform is not touched.

---

## 3. Recommended Approach

**Option 1 — Direct Adjustment. Selected.** Effort: Low. Risk: Low.

Amend AC2's wording to name the profile as measured, keeping both limitations and the no-HA-claim clause exactly as they are.

**Option 2 — Rollback** (revert the platform to the single-voter profile `values.yaml` declares): **not viable as a response to this issue.** It is a live change to a running secrets platform, needs Platform Operations approval on its own merits, and would discard a working snapshot CronJob and leader-election setup to satisfy a document. If single-voter Raft is later judged preferable, that is its own change with its own evidence — not a wording fix.

**Option 3 — PRD MVP Review:** **not applicable.** Epic 31 is Operational Readiness track and is explicitly not counted toward MVP product readiness. The MVP is unaffected.

### Rationale

The amendment preserves every guarantee AC2 was written to obtain. Both named limitations survive verbatim in substance. The "neither limitation is described as hardened or production-HA" clause is kept **and becomes sharper**: with three voters on one node, "not production-HA" is now a claim that must be stated precisely rather than inferred from a voter count. The change is confined to making a premise match measured reality, which is the same obligation AC1 already places on the documentation.

---

## 4. Detailed Change Proposals

### 4.1 `_bmad-output/planning-artifacts/epics.md` — Story 31.1 AC2

**Section:** Epic 31 → Story 31.1 → Acceptance Criteria, second GWT block.

**OLD**

```markdown
**Given** the single-node deployment profile,
**When** the security reviewer evaluates it,
**Then** the static file-based OpenBao seal - the unseal key held in a Kubernetes Secret beside the data - and the namespace-wide port 8200 ingress are surfaced explicitly as accepted single-node limitations with owner, consequence, compensating controls, and a reopen trigger
**And** neither limitation is described as hardened or production-HA.
```

**NEW**

```markdown
**Given** the deployed availability profile as measured - OpenBao Raft voters co-located on a single Kubernetes node, so the node is the whole failure domain regardless of voter count,
**When** the security reviewer evaluates it,
**Then** the static file-based OpenBao seal - the unseal key held in a Kubernetes Secret beside the data - and the namespace-wide port 8200 ingress are surfaced explicitly as accepted limitations of that single-node-hosted profile, each with owner, consequence, compensating controls, and a reopen trigger
**And** neither limitation is described as hardened or production-HA
**And** the documented voter count and HA mode match the running platform rather than the tracked manifest.
```

**Rationale:** The premise now names what was measured instead of asserting a Raft topology that is false. Both limitations are unchanged. The added `**And**` closes the gap this issue exposed — an unamended AC2 would have let a documented voter count drift from the running one exactly as it already had.

### 4.2 `_bmad-output/planning-artifacts/epics.md` — Story 31.1 Implementation evidence

**OLD**

```markdown
Required rows: the four documented topology files at their deployed configuration; the executed smoke test with its command and result; each accepted single-node limitation with owner, consequence, compensating controls and reopen trigger; and the security reviewer's recorded evaluation.
```

**NEW**

```markdown
Required rows: the four documented topology files at their deployed configuration; the executed smoke test with its command and result; each accepted limitation of the single-node-hosted profile with owner, consequence, compensating controls and reopen trigger; and the security reviewer's recorded evaluation.
```

**Rationale:** Keeps the Implementation-evidence clause in lock-step with the amended AC2. The required-row set is unchanged.

### 4.3 Story file `31-1-openbao-platform-hardening-and-documentation.md`

Four aligned edits, no scope change:

1. **AC2** restated to match §4.1 verbatim.
2. **The `> AC2 premise conflict` callout** removed and replaced with a pointer to this ratification.
3. **`### Open Decision — AC2 "single-node" qualifier`** replaced by **`### Ratified — AC2 deployed-profile qualifier (2026-07-28)`**, recording the decision, this proposal, and the one-node measurement that resolved it.
4. **`### Deployed-State Drift Measured At Creation`** gains the node-count row and the corrected availability reading, so the developer re-measures node placement in Task 1 rather than only voter count.

Checkpoint rows C4 and C5 keep their owners, evidence, and `pending` / `not complete` states. Task 3's requirement to document both limitations is unchanged. Task 5's guard assertions are unchanged, and the ban on `hardened` / `production-HA` / `highly available` / `production-ready` wording still applies.

**Rationale:** The story file is the developer's only context. An amended AC in `epics.md` that the story file contradicts is exactly the drift this correction is fixing.

---

## 5. Implementation Handoff

**Scope classification: Minor** — one AC wording amendment and its story-file alignment.

| Recipient | Responsibility |
| :-------- | :------------- |
| Developer (this `correct-course` session) | Apply §4.1, §4.2, and §4.3 on approval |
| `dev-story` (Story 31.1) | Implement the story against the amended AC2. Re-measure node count and voter placement in Task 1; document the availability profile as measured; keep both accepted limitations |
| Security reviewer (`murat-tea-for-jpiquot`) | Evaluate checkpoint C7 against the platform as measured — three voters, one node, static seal, namespace-wide 8200 ingress |
| Hexalith Platform Operations (`jpiquot`) | Owns C2 drift reconciliation, C3 smoke-test execution, and the C4/C5 limitation records. A revert to single-voter Raft, if ever wanted, is a separate change with its own approval |

### Success criteria

1. `epics.md` Story 31.1 AC2 names the measured availability profile, retains both limitations, retains the no-hardened/no-production-HA clause, and adds the voter-count/HA-mode accuracy clause.
2. The Implementation-evidence clause matches the amended AC2 and its required-row set is unchanged.
3. The story file's AC2 matches `epics.md` exactly, the open-decision section is a resolved ratification record, and the drift table carries the node-count measurement.
4. No story status advances; no story is added, removed, renumbered, or split; `sprint-status.yaml` is unchanged.
5. Both accepted limitations remain in scope with checkpoint rows C4 and C5 `pending` / `not complete`.

**Explicitly out of scope:** any change to the running OpenBao platform; any edit to `docs/operations/openbao.md` or `deploy/openbao/**` (Story 31.1's own deliverables); any Story 31.2 scope; any edit to a previously approved dated sprint change proposal or to the `resolved` `DW 27.3-CR6` record; advancing any story status.

---

## 6. Approval

**Path A selected by the Administrator on 2026-07-28**, choosing amendment of the AC text over reverting the deployed platform. The one-node measurement was captured after that selection and narrows the amendment rather than changing its direction: "single-node" is retained where it is accurate (the hosting node and failure domain) and dropped where it is not (the Raft voter count).

**Approved by the Administrator on 2026-07-28**, selecting the variant that retains the voter-count/HA-mode accuracy clause (§4.1, final `**And**`) after the redundancy argument against it was put to them explicitly: AC1 already requires the four files to be documented at their exact deployed configuration, and the voter count lives in one of those files. The clause was retained because that implicit coverage is precisely what failed silently across nine Helm revisions.

**Applied 2026-07-28.** §4.1, §4.2, and §4.3 executed in full. `sprint-status.yaml` unchanged, as specified. Story 31.1 remains `ready-for-dev`; `epic-31` remains `in-progress`.
