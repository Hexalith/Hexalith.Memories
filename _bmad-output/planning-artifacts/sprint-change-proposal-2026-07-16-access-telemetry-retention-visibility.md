---
change_trigger: "Keep 20.5-A41-ACCESS-TELEMETRY-RETENTION visible until a retention/TTL mechanism or explicit accepted-debt disposition is implemented"
mode: batch
status: approved-and-applied
approved_by: Administrator
project: Hexalith.Memories
date: 2026-07-16
scope_classification: minor
---

# Sprint Change Proposal: Access-Telemetry Retention Visibility Gate

Date: 2026-07-16  
Project: Hexalith.Memories  
Scope: Minor planning, documentation, and tracking correction. No runtime retention mechanism or debt acceptance is authorized by this proposal.

## 1. Issue Summary

Architecture audit finding A41 identified three gaps: inbound per-tenant quotas,
incomplete audit emission, and no bounded retention/TTL for access telemetry.
Story 20.5 implemented the first two slices and deliberately carried the
retention slice forward as `20.5-A41-ACCESS-TELEMETRY-RETENTION`.

The implementation evidence is accurate:

- Story 20.5 says A41 retention is not claimed closed.
- The Epic 20 retrospective calls A41 partially closed, records retention/TTL as
  a production-exposure blocker, and creates the action that triggered this
  proposal.
- The Epic 21 retrospective confirms the item remains open and outside Epic 21.
- `deferred-work.md` records it as `carried-forward`.
- `sprint-status.yaml` records the retrospective action as `open`.
- The PRD states that infrastructure access telemetry does not guarantee
  retention compliance.

The remaining problem is durable visibility and consistent closure semantics.
`epics.md` still says Epic 20 and Story 20.5 close A41 without naming the
retention residual; the architecture names a structured-log MVP and a future
dedicated store without stating that neither supplies a repository-owned
retention policy today; and the deferred entry allows a "named retention
exception" without defining what makes that an explicit accepted-debt
disposition. A future planning or documentation pass could therefore infer that
A41 is complete or close the action with evidence weaker than the trigger
requires.

This correction must keep the item visible. It must not implement retention,
accept the debt, reopen completed Epic 20, or create a new product epic.

## 2. Impact Analysis

### Epic and story impact

- Epic 20 and Story 20.5 remain `done`; their delivered rate-limiting and
  audit-emission behavior is unchanged.
- Their A41 wording is reconciled to say they close only the request-limiting
  and audit-emission slices.
- No completed epic is reopened, no story is added retroactively, and no epic is
  renumbered or resequenced.
- No new Epic 27 is required. The work is intentionally unscheduled until an
  operator/security decision selects implementation or accepted debt; the
  deferred register remains its canonical active home.
- Future implementation may be promoted into a normal spec or registered story
  when selected. That later scheduling decision is outside this proposal.

### Artifact impact

| Artifact | Impact | Proposed action after approval |
|---|---|---|
| PRD | No conflict | No edit; FR67 and the compliance limitation already describe the correct product boundary |
| Architecture | Current-state ambiguity | State that the present structured-log route has no repository-owned TTL/purge and link the closure gate |
| UX specification | None | No edit; retention lifecycle has no user interaction in the current scope |
| `epics.md` | Overbroad A41 closure wording | Add a durable residual guard and qualify Epic 20/Story 20.5 closure claims |
| Root project context | Missing persistent close-out rule | Add one concise rule loaded by future implementation agents |
| `deferred-work.md` | Closure semantics too weak | Keep `carried-forward`, name both valid resolution paths, and define accepted-debt evidence |
| `sprint-status.yaml` | Correct status, weak provenance | Keep the action `open` and point its comment to the durable guard |
| `docs/dev/telemetry.md` | Routing documented, lifecycle gap unstated | Add the current retention status and the same two-path closure gate |
| `docs/operations/rate-limiting.md` | Incorrect target for retention work | No content edit; remove it as the deferred entry's target artifact |

### Technical and delivery impact

- No source, configuration, schema, API, persistence, package, deployment, or UI
  behavior changes.
- No claim is made that a log sink, collector, or external platform enforces a
  retention policy for Memories.
- Effort: low, one focused planning/documentation/tracking change.
- Product risk: low; the proposal reduces false-closure risk without touching
  runtime behavior.
- Timeline impact: none now. A later implementation path is expected to be
  medium effort and requires an architecture/operator decision first.
- Production-exposure impact: unchanged; A41 retention/TTL remains an explicit
  blocker until one of the two resolution paths is completed.
- MVP impact: none. The PRD already excludes retention-compliance guarantees
  from the infrastructure telemetry contract.

## 3. Recommended Approach

Use **Direct Adjustment** to create one repository-lifetime visibility and
closure gate across planning, persistent context, deferred work, sprint status,
and telemetry documentation.

The action may leave `open` and the deferred entry may leave `carried-forward`
only after one of these paths is evidenced:

1. **Retention/TTL implementation:** the selected access-telemetry sink or store
   has a bounded, operator-configurable retention policy; expiry/purge behavior
   is implemented; policy ownership and defaults are documented; and focused
   validation proves expiration/purge behavior without breaking required audit
   emission or tenant/privacy boundaries.
2. **Explicit accepted debt:** the deferred entry records the named approver and
   owner, affected storage/scope, rationale, risk and consequence, compensating
   controls, and a time-bounded review/expiry date or measurable reopen trigger.
   Its status is deliberately changed to the project's accepted-debt state.

Until one path is complete, every A41 summary must describe the finding as
partially closed, the sprint action remains `open`, and the deferred entry
remains `carried-forward`.

Rollback is not viable because the completed rate-limiting and audit-emission
work is correct and security-positive. PRD/MVP review is unnecessary because
the product boundary already states that retention compliance is not supplied.
A new epic would overstate an unscheduled operational decision and would
conflict with the repository rule against reopening completed remediation
epics.

## 4. Detailed Change Proposals

### Proposal A: Add a durable A41 residual guard to `epics.md`

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: `Phase: Post-MVP — Audit Remediation (2026-07-04)` preamble

OLD:

No repository-lifetime guard governs the retention residual.

NEW:

```markdown
**A41 access-telemetry retention residual (2026-07-16):** Epic 20 and
Story 20.5 close only A41's request-limiting and audit-emission slices.
`20.5-A41-ACCESS-TELEMETRY-RETENTION` remains `carried-forward`, and its
retrospective action remains `open`, until either bounded retention/TTL is
implemented and validated or an explicit accepted-debt disposition records a
named approver/owner, scope, rationale, risk/consequence, compensating controls,
and a time-bounded review/expiry date or measurable reopen trigger. No artifact
may claim A41 fully closed before that gate is met. This guard does not reopen
completed Epic 20 or schedule implementation by itself.
```

Rationale: makes the visibility and closure rule independent of a single
retrospective or ledger entry.

### Proposal B: Correct Epic 20 and FR67 closure wording

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Sections: FR coverage map, Epic 20 summary, detailed Epic 20 header, Story 20.5

OLD:

```markdown
- FR67: Epic 7 — Search/access telemetry; reinforced by Epic 20 for audit completeness
**Driven by:** Sprint Change Proposal 2026-07-04 (Architecture Audit Remediation) — closes A1, A2, A6, A20, A31, A41
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A1, A2, A6, A20, A31, A41
**Then** each emits an audit event. Closes A41.
```

NEW:

```markdown
- FR67: Epic 7 — Search/access telemetry; reinforced by Epic 20 for audit emission. A41 access-telemetry retention remains governed by `20.5-A41-ACCESS-TELEMETRY-RETENTION`.
**Driven by:** Sprint Change Proposal 2026-07-04 (Architecture Audit Remediation) — closes A1, A2, A6, A20, A31, and A41's request-limiting/audit-emission slices; the retention residual remains carried forward
**Driven by:** Sprint Change Proposal 2026-07-04 — closes A1, A2, A6, A20, A31, and A41's request-limiting/audit-emission slices; the retention residual remains carried forward
**Then** each emits an audit event. This closes A41's request-limiting and audit-emission slices; `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains carried forward.
```

Rationale: reconciles planning text with the already-correct story completion
record and retrospectives without reopening or diminishing completed work.

### Proposal C: Clarify the architecture lifecycle boundary

Artifact: `_bmad-output/planning-artifacts/architecture.md`  
Section: `Growth-phase security`

OLD:

```markdown
- Access telemetry to separate write-only store — MVP: structured log file. Growth: dedicated audit store.
```

NEW:

```markdown
- Access telemetry to separate write-only store — MVP: structured log file. Growth: dedicated audit store. The current repository does not own or enforce bounded retention/TTL or purge for that telemetry route. `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains visible until a selected sink/store implements and validates the lifecycle policy or an explicit accepted-debt disposition satisfies the project's recorded closure gate; neither path changes the PRD limitation that Memories does not guarantee certified retention compliance.
```

Rationale: distinguishes emission/routing from lifecycle enforcement and avoids
implying that a future store name alone resolves retention.

### Proposal D: Add a persistent project-context rule

Artifact: `_bmad-output/project-context.md`  
Section: `Critical Don't-Miss Rules`

OLD:

No persistent rule names the A41 retention residual or its closure gate.

NEW:

```markdown
- **Never hide or over-close the access-telemetry retention residual** — keep `20.5-A41-ACCESS-TELEMETRY-RETENTION` carried forward and its sprint action open until bounded retention/TTL is implemented and validated or an explicit accepted-debt disposition records a named approver/owner, scope, rationale, risk/consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger. Until then, describe A41 as partially closed.
```

Rationale: future planning and implementation agents load project context even
when they do not revisit the Epic 20 retrospective.

### Proposal E: Strengthen the deferred entry without resolving it

Artifact: `_bmad-output/implementation-artifacts/deferred-work.md`  
Entry: `20.5-A41-ACCESS-TELEMETRY-RETENTION`

OLD:

```markdown
  - Target artifact: docs/operations/rate-limiting.md and the future access-telemetry storage/purge implementation
  - Re-open trigger: before A41 is marked fully closed, implement and document bounded access-telemetry retention or approve a named retention exception.
```

NEW:

```markdown
  - Target artifacts: `docs/dev/telemetry.md` plus the selected access-telemetry sink/storage purge implementation and focused lifecycle tests, or this entry updated to an explicit accepted-debt disposition.
  - Resolution gate: Keep this entry `carried-forward` and the matching sprint action `open` until bounded retention/TTL is implemented, documented, and validated, or accepted debt records a named approver and owner, affected storage/scope, rationale, risk and consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger.
  - Re-open/claim trigger: Review before any claim that A41 is fully closed, before any production-retention assurance is made, and at the accepted-debt review/expiry trigger if that path is selected.
```

Rationale: removes an unrelated rate-limiting document as the lifecycle target
and prevents an informal exception from satisfying the action.

### Proposal F: Keep the sprint action open and attach durable provenance

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`  
Action: `Keep 20.5-A41-ACCESS-TELEMETRY-RETENTION visible...`

OLD:

```yaml
status: open
```

NEW:

```yaml
status: open  # 2026-07-16: Durable A41 residual and two-path closure guards proposed in sprint-change-proposal-2026-07-16-access-telemetry-retention-visibility.md; remains open until bounded retention/TTL is implemented and validated or explicit accepted debt satisfies the recorded disposition fields.
```

Rationale: applying the visibility guard is not the same as resolving retention
or accepting debt, so closing the action would contradict its success criteria.

### Proposal G: Document the current retention status beside telemetry routing

Artifact: `docs/dev/telemetry.md`  
Section: after `Audit log routing recipe`

OLD:

The document explains how operators can route the dedicated audit category to a
sink but does not state whether Memories owns expiry or purge.

NEW:

```markdown
### Retention lifecycle status

Routing `AccessTelemetryCategory` to a file, collector, or external sink does
not configure retention by itself. Memories currently emits the structured
events but does not own or enforce a bounded TTL/purge policy for the selected
sink. Track this residual as
`20.5-A41-ACCESS-TELEMETRY-RETENTION`; do not claim A41 fully closed or
retention compliance until bounded lifecycle behavior is implemented and
validated or the deferred entry records the project's explicit accepted-debt
disposition. External sink retention remains an operator responsibility unless
a future implementation makes that policy part of the Memories deployment
contract.
```

Rationale: puts the unresolved lifecycle boundary where operators and
developers configure telemetry routing.

## 5. Verification and Acceptance Criteria

The approved correction is complete only when:

1. `epics.md` identifies A41 as partially closed and contains the durable
   two-path closure gate.
2. Epic 20 and Story 20.5 remain `done`; no epic/story status, number, order, or
   delivery claim other than A41 scope wording changes.
3. Architecture distinguishes access-event emission/routing from bounded
   retention/purge enforcement.
4. Root project context carries the same close-out rule without semantic drift.
5. The deferred entry remains `carried-forward`, points to telemetry lifecycle
   artifacts, and defines all accepted-debt evidence fields.
6. The sprint action remains `open` and links to this correction.
7. Telemetry documentation states that no repository-owned retention/TTL is
   currently implemented.
8. PRD and UX remain unchanged.
9. No code, runtime configuration, deployment artifact, or test behavior is
   changed by this correction.
10. Markdown whitespace checks and sprint-status YAML parsing pass; focused
    textual checks confirm the old unqualified A41 closure claims are gone.

## 6. Alternatives Considered

### Create a new Epic 27 or registered story now

Rejected for this action. The unresolved prerequisite is an operator/security
decision about storage ownership and purge cadence. Registering implementation
before that decision would imply scheduling and scope not requested by the
trigger. The deferred register is already the canonical active home; later
selection can promote it through the normal story/spec process.

### Reopen Epic 20 or Story 20.5

Rejected. The completed implementation is correct, its record already disclaims
retention closure, and the repository explicitly says completed remediation
epics are not reopened.

### Mark the action done after adding documentation

Rejected. Documentation improves visibility but is neither a retention/TTL
mechanism nor an accepted-debt disposition. The trigger explicitly requires the
action to remain visible until one of those outcomes exists.

### Treat an external sink's unspecified defaults as retention

Rejected. Routing does not prove a bounded policy, ownership, purge behavior, or
validation. Any external-sink solution must be made explicit in the deployment
contract and evidenced against the implementation-path acceptance criteria.

## 7. Implementation Handoff

Classification: **Minor**.

Recipients and responsibilities after approval:

- **Product owner / planning agent:** apply the A41 residual wording and preserve
  the no-reopen/no-schedule boundary.
- **Architect / operations owner:** when later selected, decide the telemetry
  storage owner, retention bounds, purge semantics, configuration surface, and
  production assurance boundary.
- **Developer agent:** for an implementation path, implement the selected
  lifecycle behavior and attach focused expiry/purge, emission-continuity, and
  tenant/privacy validation evidence.
- **Test architect / reviewer:** reject A41 close-out that lacks implementation
  evidence or the complete accepted-debt record.
- **Security/operations approver:** for a debt path, explicitly own and approve
  the bounded disposition; an unnamed or undated exception is insufficient.

Implementation sequence after this proposal is approved:

1. Apply only Proposals A-G and validate the artifact consistency gates.
2. Keep the sprint action `open` and deferred entry `carried-forward`.
3. When a later decision selects implementation, promote the work through the
   normal story/spec route before changing runtime behavior.
4. When either valid resolution path completes, reconcile `deferred-work.md`,
   `sprint-status.yaml`, architecture/telemetry documentation, and all A41
   summaries in one reviewed close-out change.

Success definition: the retention residual cannot disappear through epic
completion, documentation churn, or an informal exception, while completed
Epic 20 work remains closed and accurately credited.

## 8. Correct-Course Checklist Status

| Checklist item | Status | Finding |
|---|---|---|
| 1.1 Triggering story identified | Complete | Story 20.5 and Epic 20 retrospective action 1 expose the retained A41 slice |
| 1.2 Core problem defined | Complete | Audit-remediation scope overstatement plus insufficiently explicit durability/closure governance |
| 1.3 Initial evidence assessed | Complete | Audit A41, Story 20.5, Epic 20/21 retrospectives, PRD, architecture, epics, deferred work, sprint status, and telemetry docs agree on the residual |
| 2.1 Current epic impact | Complete | Epic 20 remains complete; only its A41 scope wording changes |
| 2.2 Epic-level changes | Complete | Add a residual guard and qualify two closure claims; no scope reopening |
| 2.3 Remaining epics reviewed | Complete | Epics 21-26 are complete and unrelated; none supplies retention lifecycle behavior |
| 2.4 Epics invalidated/new epic required | N/A | No epic is invalidated; the deferred register remains the active home until scheduling |
| 2.5 Priority/order changes | N/A | No resequencing or priority change |
| 3.1 PRD conflict/impact | Complete | No change; FR67 and the compliance limitation already preserve the correct boundary |
| 3.2 Architecture conflict/impact | Complete | Growth-phase text must distinguish routing/store direction from current lifecycle enforcement |
| 3.3 UX conflict/impact | N/A | No user flow, component, accessibility, or interaction impact |
| 3.4 Other artifact impact | Complete | Epics, project context, deferred ledger, sprint status, and telemetry docs require aligned guard wording |
| 4.1 Direct adjustment viability | Complete | Viable; low effort and low product risk |
| 4.2 Rollback viability | N/A | Correct security-positive implementation must remain |
| 4.3 MVP review viability | N/A | PRD already excludes retention-compliance guarantees |
| 4.4 Recommended path selected | Complete | Direct Adjustment |
| 5.1 Issue summary | Complete | Section 1 |
| 5.2 Epic/artifact impact | Complete | Section 2 |
| 5.3 Recommended path | Complete | Section 3 |
| 5.4 Detailed proposals and MVP impact | Complete | Sections 4 and 5 |
| 5.5 Agent handoff | Complete | Section 7 |
| 6.1 Checklist reviewed | Complete | All applicable analysis items addressed |
| 6.2 Proposal consistency | Complete | The proposal preserves current implementation evidence and strengthens the exact trigger |
| 6.3 Explicit approval | Complete | Approved by Administrator on 2026-07-16 |
| 6.4 Sprint-status implementation | Complete | Provenance comment applied while the action deliberately remains `open` |
| 6.5 Handoff confirmation | Complete | Minor-scope artifact correction applied; later runtime/debt resolution retains its explicit role-based handoff |

## 9. Approval Record

- Decision: Approved and applied on 2026-07-16
- Approver: Administrator
- Approved scope: Minor
- Applied route: planning/documentation/tracking direct adjustment
- Retained boundary: runtime retention or debt acceptance requires a later explicit route

## 10. Workflow Execution Log

- Loaded and reviewed the complete PRD, architecture, UX specification, epics,
  audit A41 evidence, Story 20.5, Epic 20/21 retrospectives, sprint status,
  deferred ledger, root project context, and telemetry/operator documentation.
- Presented the batch proposal and received `c` to continue.
- Received explicit approval from Administrator.
- Applied Proposals A-G without changing runtime behavior or closing the active
  action/deferred entry.
- Validated `sprint-status.yaml` as YAML, confirmed the qualified A41 wording and
  two-path closure guard across all target artifacts, and passed focused
  whitespace checks.
