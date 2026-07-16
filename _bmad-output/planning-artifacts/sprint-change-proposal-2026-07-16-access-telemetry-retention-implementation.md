---
change_trigger: "Implement and validate bounded retention/TTL for 20.5-A41-ACCESS-TELEMETRY-RETENTION"
mode: batch
status: approved-and-applied
requested_by: Administrator
approved_by: Administrator
project: Hexalith.Memories
date: 2026-07-16
scope_classification: moderate
supersedes: null
follows:
  - sprint-change-proposal-2026-07-16-access-telemetry-retention-visibility.md
---

# Sprint Change Proposal: Access-Telemetry Retention Implementation

Date: 2026-07-16  
Project: Hexalith.Memories  
Scope: Moderate backlog, architecture, implementation, deployment, testing, and
documentation change. This proposal does not itself close A41 or authorize an
accepted-debt disposition.

## 1. Issue Summary

Architecture audit finding A41 identified three gaps in access telemetry:
inbound per-tenant quotas, incomplete audit emission, and no bounded
retention/TTL. Story 20.5 completed the first two slices and correctly carried
the retention slice forward as
`20.5-A41-ACCESS-TELEMETRY-RETENTION`.

The approved visibility correction in
`sprint-change-proposal-2026-07-16-access-telemetry-retention-visibility.md`
prevents that residual from disappearing. It intentionally did not implement a
retention mechanism, accept debt, reopen Epic 20, or schedule new product work.
The new trigger is Administrator's request to fix the remaining residual by
selecting the implementation path rather than the accepted-debt path.

Current repository evidence shows why implementation needs an explicit
architecture decision before code begins:

- `AccessTelemetryLog` emits structured `AccessTelemetryEvent` records through
  `ILogger<AccessTelemetryCategory>`.
- Service defaults always add JSON console logging and add OpenTelemetry log
  export only when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
- The production deployment runs two Server replicas, uses a read-only root
  filesystem, and does not commit an access-telemetry sink, durable audit
  volume, TTL, rotation, or purge policy.
- Routing logs to console or an unspecified OTLP receiver does not prove
  bounded retention, ownership, purge behavior, or restart/multi-replica
  correctness.
- The PRD deliberately describes access events as infrastructure telemetry,
  not a tamper-evident or certified-retention audit trail. Implementing bounded
  lifecycle behavior must preserve that product boundary.

The core problem is therefore not only "add a timer." The project must first
choose and own a concrete write path whose retention semantics can be
configured, observed, and tested under the real two-replica deployment shape.

## 2. Impact Analysis

### Epic and story impact

- Epic 20 and Story 20.5 remain `done`. Their rate-limiting and audit-emission
  delivery remains correct and is not rolled back or reopened.
- Epics 21-26 are also complete and do not provide an appropriate active home
  for this security/operations residual.
- Add **Epic 27: Access Telemetry Lifecycle Hardening** as the first new backlog
  home after the completed audit-remediation sequence.
- Use a decision-first story before implementation because the repository has
  no selected sink/store owner and local files are incompatible with the
  current multi-replica, read-only-root deployment unless separately designed
  with shared durable storage and safe concurrent rotation.
- Keep the deferred entry `carried-forward` and the sprint action `open` while
  Epic 27 is in backlog or in progress. Scheduling the work is not proof that
  retention works.

### Artifact impact

| Artifact | Impact | Action after approval |
|---|---|---|
| PRD | No requirement conflict | No edit; FR67 and the compliance boundary remain authoritative |
| `epics.md` | No registered implementation home | Add Epic 27 and Stories 27.1-27.3; retain completed Epic 20 wording |
| `sprint-status.yaml` | No rows for the new work | Add Epic 27/story rows as `backlog`; keep the existing action `open` |
| Architecture | Current state records only an unresolved future store | Add the decision gate and later replace it with the ratified sink/store and lifecycle contract |
| UX specification | No user-facing flow | No edit |
| `deferred-work.md` | Active residual now has a planned home | Keep `carried-forward`; point implementation ownership to Epic 27 without weakening the two-path gate |
| Root project context | Existing close-out guard is correct | No immediate semantic change; reconcile it only in the final evidence-backed close-out |
| `docs/dev/telemetry.md` | Correctly states the current gap | Add the Epic 27 implementation home after approval; document the selected policy only after Story 27.1 |
| Deployment artifacts | No bounded sink/store contract | Implement the ratified topology/configuration in Story 27.2 |
| Operations documentation | No lifecycle runbook | Add configuration, monitoring, capacity, incident, and purge-verification guidance in Story 27.3 |
| Tests and verification | No expiry/purge evidence | Add deterministic lifecycle tests plus a real deployment-shaped validation lane |

### Technical impact

The decision and implementation touch the Server logging boundary,
configuration validation, deployment topology, and operational evidence. The
selected design must:

- preserve existing console and OTLP emission unless the ratified decision
  explicitly replaces a path with compatible evidence;
- identify one lifecycle owner for the selected sink/store and define its
  write, expiry, purge, restart, and failure semantics;
- work with at least two Server writers and the production read-only-root
  security posture;
- use an operator-configurable bounded duration with documented defaults,
  allowed range, and clock semantics;
- fail closed on invalid production lifecycle configuration without turning a
  temporary telemetry-backend outage into an undocumented total-service
  failure;
- expose low-cardinality health/metrics for accepted, rejected/dropped,
  failed, expired, and purged events without logging secrets or content;
- preserve tenant and privacy boundaries and attach focused cross-tenant
  negative evidence as required by project context;
- keep the PRD limitation: this is bounded infrastructure telemetry, not a
  claim of immutability, legal compliance, or certified audit retention.

### Delivery impact

- Effort: medium, three focused stories.
- Product risk: medium. The runtime request path, logging pipeline, and
  production topology are involved, but the work is isolated from domain data.
- Timeline: adds one post-remediation epic; no completed epic or release claim
  is rewritten.
- Production exposure: the existing A41 blocker remains until Story 27.3
  records executable lifecycle evidence and performs the coordinated close-out.
- MVP: no scope reduction or product-goal change.

## 3. Recommended Approach

Use **Direct Adjustment with a decision-first implementation epic**.

Create Epic 27 with three ordered stories:

1. **27.1 Access-Telemetry Retention Ownership Decision (Decision-First)**
   chooses the sink/store and records the complete lifecycle contract.
2. **27.2 Bounded Retention/TTL and Purge Implementation** implements that
   decision across code, configuration, and deployment artifacts.
3. **27.3 Retention Verification, Operations Runbook, and A41 Close-Out** proves
   expiry/purge under the production-shaped topology and reconciles every
   guarded artifact in one reviewed change.

Story 27.1 must compare at least these viable families against current
constraints:

- a deployment-owned external telemetry backend reached through the existing
  OTLP seam, with repository-enforced configuration and validation of its
  bounded policy;
- a dedicated repository-owned write-only telemetry store with explicit TTL or
  purge behavior and no dependency on the primary domain-data store;
- a file/volume design only if it proves safe concurrent writes, rotation,
  durable storage, pod rescheduling, and purge for the current two-replica
  deployment.

The decision must select one family. "Use whatever the operator already has,"
console-log rotation, an unspecified collector default, or documentation alone
cannot satisfy the implementation path.

Rollback is not viable because Story 20.5 delivered correct security-positive
behavior. PRD/MVP review is unnecessary because the PRD already distinguishes
infrastructure telemetry from certified audit retention. Accepted debt remains
a valid closure path under the earlier proposal, but it is not the recommended
response to the present request to fix the residual.

## 4. Detailed Change Proposals

### Proposal A: Register Epic 27 in the epic list

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: Epic List, after Epic 26

OLD:

```markdown
### Epic 26: Test, Deployment & Operational Readiness
...
```

NEW:

```markdown
### Epic 27: Access Telemetry Lifecycle Hardening
Operators can configure and verify a bounded lifecycle for access telemetry
through one explicitly owned write-only sink/store without weakening audit
emission, tenant/privacy boundaries, or the PRD compliance boundary.
**Lifecycle label:** Operational Readiness / Security and Observability Hardening
**Driven by:** Sprint Change Proposal 2026-07-16 (Access-Telemetry Retention Implementation)
**FRs reinforced:** FR67
```

Rationale: every existing epic is complete, and reopening Epic 20 would rewrite
completed history. Epic 27 provides a normal, status-tracked implementation
home.

### Proposal B: Add the detailed Epic 27 stories

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: after the detailed Epic 26 stories

OLD:

No registered story owns the A41 retention implementation.

NEW:

```markdown
## Epic 27: Access Telemetry Lifecycle Hardening

Operators can configure and verify a bounded lifecycle for access telemetry
through one explicitly owned write-only sink/store without weakening audit
emission, tenant/privacy boundaries, or the PRD compliance boundary.

**Lifecycle label:** Operational Readiness / Security and Observability Hardening.

**Driven by:** Sprint Change Proposal 2026-07-16 (Access-Telemetry Retention Implementation).

**Sequencing gate:** Story 27.1 is decision-first. Stories 27.2 and 27.3 must
not implement or claim a sink/store before its ownership, topology, failure,
retention, purge, and validation contract is ratified.

### Story 27.1: Access-Telemetry Retention Ownership Decision (Decision-First)

As an architect and operator,
I want one ratified access-telemetry lifecycle contract,
So that implementation has an owned, deployable, and testable target.

**Acceptance Criteria:**

**Given** access telemetry currently reaches JSON console and optional OTLP
export without a repository-owned bounded lifecycle,
**When** the decision evaluates external OTLP storage, a dedicated write-only
store, and any file/volume alternative,
**Then** it selects one design and records ownership, topology, multi-replica
write behavior, durability boundary, retention default/range, expiry/purge
semantics, clock source, failure/backpressure policy, recovery, observability,
privacy/tenant boundary, capacity assumptions, and rollback.

**Given** the production Server has two replicas and a read-only root
filesystem,
**When** the decision is ratified,
**Then** no local-file approach is accepted without durable shared or
per-replica storage, concurrency-safe rotation, pod-rescheduling behavior, and
executable purge evidence; no unspecified external default is treated as a
policy.

**Given** the PRD calls this infrastructure telemetry,
**When** the contract states its assurance boundary,
**Then** it does not claim tamper evidence, append-only integrity, legal
compliance, or certified audit retention.

### Story 27.2: Bounded Retention/TTL and Purge Implementation

As an operator,
I want the ratified access-telemetry sink/store to enforce a bounded lifecycle,
So that emitted access records do not grow without limit.

**Acceptance Criteria:**

**Given** Story 27.1's ratified contract,
**When** Server and deployment configuration are applied,
**Then** access events enter the selected write-only sink/store with the
documented bounded duration and expiry/purge behavior, while existing required
audit emission remains continuous.

**Given** valid, invalid, missing, minimum, and maximum lifecycle settings,
**When** the host starts in Development and Production,
**Then** configuration validation follows the ratified fail-closed/degraded
policy and never silently falls back to unbounded retention.

**Given** two Server writers, restart/rescheduling, backpressure, and temporary
sink/store failure,
**When** access events are emitted,
**Then** behavior matches the ratified delivery and recovery contract and
low-cardinality health/metrics expose loss or degradation without secrets,
raw content, or unbounded tenant labels.

**Given** two authorized tenant contexts and rejected/unknown scope,
**When** records are written, expired, purged, and inspected through any
supported operational seam,
**Then** tenant/privacy boundaries fail closed and focused cross-tenant
negative tests name the affected storage, routing, and evidence surfaces.

### Story 27.3: Retention Verification, Operations Runbook, and A41 Close-Out

As a security reviewer,
I want executable lifecycle evidence and one coordinated close-out,
So that A41 closes only after the policy works in the deployment shape.

**Acceptance Criteria:**

**Given** a short test retention window and a production-shaped deployment,
**When** old and new access events cross the expiry boundary across at least
two Server writers and a controlled restart,
**Then** focused evidence proves expired records are unavailable/purged,
newer records remain, required audit emission continues, and tenant/privacy
negative checks pass.

**Given** the ratified production duration,
**When** operators deploy, monitor, change, or roll back the policy,
**Then** telemetry, deployment-configuration, capacity, monitoring, incident,
and recovery documentation identifies the owner, configuration, defaults,
storage impact, purge verification, alarms, rollback, and assurance limits.

**Given** all implementation and documentation evidence passes,
**When** A41 is closed,
**Then** `20.5-A41-ACCESS-TELEMETRY-RETENTION` is reconciled from
`carried-forward`, the matching sprint action is closed, architecture and all
A41 summaries cite the evidence, and Epic 20/Story 20.5 remain historical
`done` records rather than being reopened.
```

Rationale: separates the irreversible architecture/topology choice from
implementation and requires deployment-shaped evidence before close-out.

### Proposal C: Register backlog status without closing the residual

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`  
Section: `development_status`, after Epic 26

OLD:

No Epic 27 rows exist. The A41 action is `open`.

NEW:

```yaml
  epic-27: backlog
  27-1-access-telemetry-retention-ownership-decision: backlog
  27-2-bounded-retention-ttl-and-purge-implementation: backlog
  27-3-retention-verification-operations-runbook-and-a41-close-out: backlog
  epic-27-retrospective: optional
```

The existing action remains:

```yaml
status: open
```

Its provenance comment may point to this approved proposal and Epic 27, but
must continue to state that scheduling is not closure.

Rationale: status tracking begins at backlog registration; no implementation
or close-out evidence exists yet.

### Proposal D: Attach the active deferred entry to Epic 27

Artifact: `_bmad-output/implementation-artifacts/deferred-work.md`  
Entry: `20.5-A41-ACCESS-TELEMETRY-RETENTION`

OLD:

```markdown
- Target artifacts: `docs/dev/telemetry.md` plus the selected
  access-telemetry sink/storage purge implementation and focused lifecycle
  tests, or this entry updated to an explicit accepted-debt disposition.
```

NEW:

```markdown
- Backlog home: Epic 27, Stories 27.1-27.3. Scheduling does not satisfy the
  resolution gate.
- Target artifacts: `docs/dev/telemetry.md`, the Story 27.1 architecture
  decision, the selected access-telemetry sink/storage deployment and purge
  implementation, and focused lifecycle/tenant-privacy tests, or this entry
  updated to a complete explicit accepted-debt disposition.
```

Keep `Status: carried-forward`, the complete resolution gate, the claim/reopen
trigger, and the named owner unchanged until Story 27.3 passes.

Rationale: creates traceability without treating a backlog row as evidence.

### Proposal E: Record the decision gate in architecture

Artifact: `_bmad-output/planning-artifacts/architecture.md`  
Section: `Growth-phase security`, access-telemetry bullet

OLD:

```markdown
The current repository does not own or enforce bounded retention/TTL or purge
for that telemetry route. `20.5-A41-ACCESS-TELEMETRY-RETENTION` remains visible
until a selected sink/store implements and validates the lifecycle policy or
an explicit accepted-debt disposition satisfies the project's recorded
closure gate.
```

NEW after approval, before Story 27.1:

```markdown
The current repository does not own or enforce bounded retention/TTL or purge
for that telemetry route. Epic 27 owns the decision-first implementation path:
Story 27.1 must ratify the sink/store and lifecycle contract before Stories
27.2-27.3 implement and validate it. `20.5-A41-ACCESS-TELEMETRY-RETENTION`
remains visible until that evidence exists or an explicit accepted-debt
disposition satisfies the project's recorded closure gate.
```

After Story 27.1, replace the temporary decision-gate sentence with the
ratified architecture and its assurance boundary. After Story 27.3, reconcile
the residual language to cite close-out evidence rather than deleting history.

Rationale: prevents implementation before the storage/topology decision and
keeps current-state text accurate.

### Proposal F: Preserve PRD, UX, and completed-history boundaries

Artifacts:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md`
- Epic 20 and Story 20.5 status/history

Change: none.

Rationale: FR67 already requires per-tenant access-event logging, and the PRD
already excludes tamper evidence and retention compliance. There is no current
user flow. Implementing lifecycle behavior does not reopen completed work.

## 5. Verification and Acceptance Criteria

The approved planning correction is complete when:

1. Epic 27 and Stories 27.1-27.3 are registered once in `epics.md` and
   `sprint-status.yaml` with backlog/optional statuses.
2. Epic 20 and Story 20.5 remain `done`; no completed epic is reopened or
   renumbered.
3. The deferred entry remains `carried-forward` and the sprint action remains
   `open`, both linked to Epic 27 without weakening the two-path closure gate.
4. Architecture identifies Story 27.1 as decision-first and still states that
   no bounded lifecycle is currently implemented.
5. PRD and UX remain unchanged.
6. YAML parsing, duplicate-key checks, epic/story parity checks, Markdown
   whitespace checks, and wording invariants pass.

Epic 27 may close only when:

1. Story 27.1 ratifies a concrete sink/store and complete lifecycle contract.
2. Story 27.2 implements bounded retention and expiry/purge with explicit
   production configuration and no silent unbounded fallback.
3. Story 27.3 records deployment-shaped proof for age-bound expiry/purge,
   newer-record preservation, emission continuity, multi-writer/restart
   behavior, and focused cross-tenant/privacy denial.
4. Operations documentation names ownership, defaults, range, monitoring,
   capacity, incident response, policy change, rollback, purge verification,
   and the non-certified assurance boundary.
5. The deferred entry, sprint action, architecture, telemetry documentation,
   audit finding summaries, and Epic 27 evidence are reconciled together.

## 6. Alternatives Considered

### Add Story 20.7 or reopen Story 20.5

Rejected. Epic 20 and its retrospective are complete. Reopening or appending to
completed remediation history would contradict the established no-reopen
boundary and obscure when the lifecycle work was actually selected.

### Implement a local rolling file immediately

Rejected as a pre-decision shortcut. The production Server has two replicas, a
read-only root filesystem, and no committed durable audit volume. A valid file
design may still be selected by Story 27.1, but only with explicit concurrency,
storage, rotation, rescheduling, and purge evidence.

### Treat console/container rotation as sufficient

Rejected. The repository does not own cluster kubelet/container-runtime
rotation settings, and bounded file count alone does not define the requested
retention duration or prove purge behavior across nodes and rescheduling.

### Treat any configured OTLP endpoint as sufficient

Rejected. Export proves routing, not a bounded backend policy. The selected
backend, policy owner, duration, purge semantics, and validation must be part of
the deployment contract.

### Accept the debt

Not selected. It remains a legitimate alternative only if a named approver and
owner explicitly record every disposition field required by the existing
two-path gate. Administrator's present request is interpreted as selecting the
implementation path.

## 7. Implementation Handoff

Classification: **Moderate**.

- **Product owner / planning agent:** register Epic 27 and its status rows,
  preserve completed history, and keep the residual open until evidence exists.
- **Solution architect / operations owner:** lead Story 27.1, select the
  sink/store, and own topology, lifecycle, capacity, failure, and assurance
  decisions.
- **Developer agent:** implement Story 27.2 exactly against the ratified
  contract; do not choose an undeclared sink while coding.
- **Test architect / security reviewer:** define deterministic time-bound tests
  and the real multi-writer/restart/tenant-privacy validation lane; reject
  documentation-only or routing-only close-out.
- **Technical writer / operator:** produce the Story 27.3 lifecycle runbook and
  reconcile every guarded artifact after evidence passes.

Handoff order: planning registration -> Story 27.1 decision -> Story 27.2
implementation -> Story 27.3 evidence/runbook -> coordinated A41 close-out ->
Epic 27 retrospective.

## 8. Correct-Course Checklist Status

| Checklist item | Status | Finding |
|---|---|---|
| 1.1 Triggering story identified | Complete | Story 20.5 and residual `20.5-A41-ACCESS-TELEMETRY-RETENTION` |
| 1.2 Core problem defined | Complete | Missing selected, owned, bounded access-telemetry lifecycle implementation |
| 1.3 Evidence assessed | Complete | Audit A41, Story 20.5, retrospectives, logging code, deployment topology, prior visibility proposal, deferred ledger, and sprint action |
| 2.1 Current epic impact | Complete | Epic 20 remains complete and unchanged |
| 2.2 Required epic changes | Complete | Add new decision-first Epic 27 |
| 2.3 Remaining epics reviewed | Complete | Epics 21-26 are complete and do not own this residual |
| 2.4 New epic required | Complete | Yes; normal backlog home is required before implementation |
| 2.5 Priority/order | Complete | Epic 27 follows the completed remediation sequence and remains a production-exposure blocker |
| 3.1 PRD impact | Complete | No edit; FR67/compliance boundary already fit |
| 3.2 Architecture impact | Complete | Sink/store ownership and lifecycle contract must be decided and recorded |
| 3.3 UX impact | N/A | No user interface or interaction change |
| 3.4 Other artifacts | Complete | Sprint status, deferred ledger, deployment, tests, telemetry and operations docs |
| 4.1 Direct adjustment | Viable | Medium effort and risk through a three-story epic |
| 4.2 Rollback | Not viable | Completed security behavior is correct |
| 4.3 MVP review | Not viable | No product-scope conflict |
| 4.4 Recommended path | Complete | Direct Adjustment with a decision-first implementation epic |
| 5.1 Issue summary | Complete | Section 1 |
| 5.2 Epic/artifact impact | Complete | Section 2 |
| 5.3 Recommended path | Complete | Section 3 |
| 5.4 Action plan and MVP impact | Complete | Sections 3-5 |
| 5.5 Handoff | Complete | Section 7 |
| 6.1 Checklist review | Complete | All applicable items addressed |
| 6.2 Proposal accuracy | Complete | Current-state and no-false-closure guards preserved |
| 6.3 Explicit approval | Complete | Approved by Administrator on 2026-07-16 |
| 6.4 Sprint-status update | Complete | Epic 27 and Stories 27.1-27.3 registered as backlog; action remains open |
| 6.5 Next steps | Complete | Role and sequencing handoff defined |

## 9. Approval Record

- Decision: Approved and applied on 2026-07-16
- Approver: Administrator
- Approved scope: Moderate
- Applied route: decision-first Epic 27 planning and backlog registration
- Retained boundary: A41 remains partially closed until executable lifecycle
  evidence or a complete explicit accepted-debt disposition exists

## 10. Workflow Execution Log

- Loaded the configured project context and the complete Correct Course
  checklist.
- Verified the prior access-telemetry visibility proposal and its applied
  invariants.
- Reviewed PRD FR67/compliance boundaries, Epic 20/Story 20.5 history,
  architecture A41 evidence, completed epic/status state, the access-telemetry
  logging path, ServiceDefaults export behavior, and the production deployment
  topology.
- Selected batch mode from the concrete request and existing approved context.
- Drafted this moderate-scope proposal without modifying source, runtime
  configuration, PRD, UX, completed history, deferred status, or sprint action.
- Received explicit approval from Administrator.
- Registered Epic 27 and Stories 27.1-27.3, attached the deferred entry and
  architecture/telemetry documentation to that backlog home, and kept the
  sprint action `open` and deferred status `carried-forward`.
- Made no source, runtime configuration, deployment, PRD, UX, or completed
  Epic 20/Story 20.5 status change. Runtime implementation remains the Epic 27
  handoff.
- Validated sprint-status YAML parsing, Epic 27 epic/story/status parity,
  A41 `open`/`carried-forward` invariants, approval metadata, focused wording,
  and Markdown whitespace.
