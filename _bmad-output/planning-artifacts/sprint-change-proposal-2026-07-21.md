---
project: memories
date: 2026-07-21
status: approved
change_scope: minor
mode: batch
trigger: Epic 18 retrospective action item 2
prior_proposal: sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md
approved_by: Administrator
approved_on: 2026-07-21
---

# Sprint Change Proposal — Revalidate architecture anchors against live code

## 1. Issue Summary

Epic 18 retrospective action item 2 identified four stale implementation anchors in
`architecture.md`: the Memory Unit identifier was described as a ULID, the test
framework was pinned to xUnit 2.9.3, the product REST surface was shown as MVC
controllers, and the Memories Server Dapr app ID was shown as `memories-server`.

The same trigger was investigated and approved on 2026-07-16 in
`sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md`. This
run revalidates that conclusion against current `main` at `4cbef886`. The requested
architecture reconciliation remains complete: commit `edcdade8` applied the four
corrections on 2026-06-25, later architecture changes preserved them, and the current
file is byte-identical to its `HEAD` blob.

Current evidence:

- `IngestionWorkflow.ResolveMemoryUnitId` returns a suitable workflow `InstanceId`
  or `context.NewGuid().ToString()`. The public contract is an opaque string, with no
  ULID syntax or time-ordering guarantee.
- `references/Hexalith.Builds/Props/Directory.Packages.props` pins `xunit.v3`,
  `xunit.v3.assert`, and `xunit.v3.extensibility.core` to 3.2.2.
- `Server/Program.cs` composes the product REST surface through
  `app.MapIngestionEndpoints()`, `app.MapTenantLifecycleEndpoints()`, and the other
  `app.MapX` endpoint groups under `Server/Endpoints/`. `app.MapControllers()` is
  intentionally retained for the EventStore Dapr pub/sub adapter
  `EventIngestionController`; it does not make the product REST surface
  controller-based.
- `AddHexalithMemoriesSearchIndexServer` defaults `serverName` to `memories` and
  assigns it to `DaprSidecarOptions.AppId`. The repository AppHost independently
  defaults `MEMORIES_DAPR_APP_ID` to `memories`.
- A focused stale-form scan finds no `string (ULID)`, `xUnit 2.9.3`,
  `Controllers/*Controller.cs`, `Server/Controllers`, or `memories-server` in the
  current `architecture.md`.

## 2. Impact Analysis

### Epic and story impact

- Epic 18 remains complete. Stories 18.3 and 18.6 remain aligned with the current
  minimal-API route surface and opaque `MemoryUnitId` contract.
- Epic 18 retrospective action item 2 remains resolved by commit `edcdade8` and the
  approved 2026-07-16 proposal.
- The complete Epic 0–29 outline was reviewed. No current or future epic requires a
  scope change, new story, removal, resequencing, or priority adjustment.
- Completed Story 15.6 references to the historical `memories-server` resource are
  retained as implementation-history evidence; they are not current architecture
  guidance.
- `sprint-status.yaml` requires no change because no epic or story status changes.

### Artifact conflicts

- **PRD:** No functional requirement, NFR, MVP gate, or scope change results from
  this architecture correction. The PRD still contains generic `REST controllers`
  terminology in its implementation-consideration prose. That adjacent editorial
  drift is recorded as a separate follow-up and is not silently added to this
  architecture-only action.
- **Architecture:** All four historical conflicts are already resolved and remain
  accurate against current source and package configuration.
- **UX:** No impact on screens, flows, components, interactions, responsive behavior,
  or accessibility requirements.
- **Specs and project knowledge:** No standalone `*spec-*.md` planning artifact or
  `docs/index.md` exists. The canonical project context already identifies xUnit v3
  3.2.2 and the minimal-API composition pattern.
- **Operations and deployment:** No infrastructure or configuration change is
  required. This proposal validates the existing app-ID default rather than changing
  it.
- **Historical evidence:** Retrospectives, completed stories, and earlier proposals
  may preserve wording that was accurate at their authoring point. They should not be
  bulk-rewritten as current guidance.

### Technical impact

The requested reconciliation is documentation-only and behavior-neutral. It changes
no runtime path, public API, serialized contract, package dependency, deployment
topology, authentication boundary, or test execution. No architecture edit or code
change remains.

## 3. Recommended Approach

Select **Direct Adjustment — already applied**.

- **Remaining effort:** None for the requested action.
- **Risk:** Low; the only meaningful risk is reintroducing stale wording.
- **Timeline impact:** None.
- **MVP and release impact:** None.
- **Rollback:** Not viable. Reverting commit `edcdade8` would make the architecture
  contradict the implementation.
- **PRD/MVP review:** Not viable or necessary; no product goal or scope assumption
  changed.

Close this trigger as a verified no-op. Handle the adjacent PRD route terminology
through a separately authorized documentation edit so the scope and evidence remain
clear.

## 4. Detailed Change Proposals

### 4.1 Memory Unit identifier contract

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
| `Id` | string (ULID) | Yes | Generated | Globally unique, time-sortable |
```

**Required state, already present:**

```markdown
| `Id` | string (opaque) | Yes | Generated | Workflow `InstanceId` (a GUID) or a fresh GUID via `ResolveMemoryUnitId`; opaque, **not** a ULID and **not** time-sortable. Stability semantics: `docs/dev/memory-unit-id-stability.md` |
```

**Rationale:** The implementation preserves a suitable workflow instance ID or
derives a GUID string. Consumers must not infer syntax, chronology, or source identity
from the value.

### 4.2 Test framework version

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
**Framework:** xUnit 2.9.3, Shouldly 4.3.0, NSubstitute 5.3.0, coverlet.collector
```

**Required state, already present:**

```markdown
**Framework:** xUnit v3 (`xunit.v3` 3.2.2), Shouldly 4.3.0, NSubstitute 5.3.0, coverlet.collector
```

**Rationale:** Central package management pins the xUnit v3 package family to 3.2.2.

### 4.3 Server route organization

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
| REST (external) | `Controllers/*Controller.cs` | `IngestionValidator` + FluentValidation | Direct tenant ID | `TenantAuthorizationMiddleware` |
```

The historical project tree also listed `Server/Controllers/` and five product
controller classes.

**Required state, already present:**

```markdown
| REST (external) | minimal-API endpoints in `Program.cs` (`app.MapGet/MapPost/...`) | `IngestionValidator` + FluentValidation | JWT bearer fallback policy plus tenant authorization | `TenantAuthorizationMiddleware` and endpoint filters |
```

The current project map places the product REST surface under `Server/Endpoints/`
and `Server/Program.cs`. It separately names
`EventStore/EventIngestionController.cs` as the intentional Dapr event adapter.

**Rationale:** `Program.cs` composes product endpoint groups with `app.MapX` extension
methods, whose endpoint files map the concrete HTTP verbs.

### 4.4 Memories Server Dapr app ID

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
| Memories Server | C# (.NET 10) | `memories-server` | Core domain: ingestion, search, tenants, fusion | Controllers, CLI, MCP |
```

**Required state, already present:**

```markdown
| Memories Server | C# (.NET 10) | `memories` | Core domain: ingestion, search, tenants, fusion | CLI, MCP, REST / Dapr callers |
```

The current AppHost example also uses `memories` for both the resource name and
sidecar `AppId`.

**Rationale:** Both the reusable Aspire extension and the repository AppHost default
the server's Dapr app ID to `memories`.

No additional architecture edit is proposed because every required state is already
committed and freshly verified.

## 5. Implementation Handoff

**Classification:** Minor — already resolved.

**Recipient:** Developer agent, with Technical Writer ownership of future drift.

**Responsibilities after approval:**

- Apply no runtime or architecture change for this trigger.
- Preserve the four corrected architecture anchors.
- Treat commit `edcdade8`, the approved 2026-07-16 proposal, and this current-HEAD
  revalidation as closure evidence for Epic 18 retrospective action item 2.
- Do not modify completed-story evidence or `sprint-status.yaml` for this no-op
  closure.
- Route the separate PRD `REST controllers` terminology residual through a new,
  explicitly authorized documentation task if desired.

**Success criteria:**

- `architecture.md` describes `MemoryUnitId` as opaque and GUID-derived without a
  ULID or time-ordering promise — met.
- `architecture.md` identifies xUnit v3 package version 3.2.2 — met.
- `architecture.md` describes product routes as minimal APIs composed through
  `app.MapX`, while preserving the EventStore Dapr-controller nuance — met.
- `architecture.md` records `memories` as the default Memories Server Dapr app ID —
  met.
- The architecture working-tree file matches `HEAD` and contains no stale forms —
  met.
- PRD, epics, UX, sprint status, runtime code, dependencies, and historical evidence
  remain unchanged by this proposal — met.

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-21 | Trigger confirmed from Epic 18 retrospective action item 2 | Complete |
| 2026-07-21 | PRD, epics, architecture, UX, project context, live code, package pins, sprint status, and Git history reviewed | Complete |
| 2026-07-21 | Current architecture checked for all four required states and stale forms | Correct; no stale forms |
| 2026-07-21 | Direct Adjustment, rollback, and MVP-review paths evaluated | Direct Adjustment already applied |
| 2026-07-21 | Batch Sprint Change Proposal written | Complete |
| 2026-07-21 | Proposal explicitly approved by Administrator | Approved |
| 2026-07-21 | Minor-scope handoff to Developer agent and Technical Writer | Complete; no implementation remaining |

## Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified as Epic 18 retrospective action item 2, informed by
  Stories 18.3 and 18.6 rather than a failing implementation story.
- [x] 1.2 Core problem categorized as stale documentation anchors discovered during
  implementation and retrospective review.
- [x] 1.3 Evidence collected from source, central package management, architecture,
  project context, sprint artifacts, and Git history.

### 2. Epic impact assessment

- [x] 2.1 Epic 18 remains complete and valid.
- [N/A] 2.2 No epic-level modification is required.
- [x] 2.3 The complete Epic 0–29 outline and dependencies were reviewed; no remaining
  epic is affected.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No priority or sequencing change is needed.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no requirement or MVP change is required. Adjacent generic
  controller terminology is recorded as an out-of-scope editorial residual.
- [x] 3.2 Architecture reviewed; all four historical conflicts remain resolved.
- [N/A] 3.3 UX is unaffected.
- [x] 3.4 Live code, packages, deployment composition, project context, test strategy,
  sprint tracking, and historical evidence were reviewed; no secondary change is
  required for this trigger.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable and already applied; effort none, risk low.
- [N/A] 4.2 Rollback is not viable because it would restore false guidance.
- [N/A] 4.3 PRD/MVP review is unnecessary because product scope is unaffected.
- [x] 4.4 Direct Adjustment selected and verified against current `main`.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact and no-op action plan documented.
- [x] 5.5 Minor-scope Developer/Technical Writer handoff documented.

### 6. Final review and handoff

- [x] 6.1 All currently applicable checklist items are addressed.
- [x] 6.2 Proposal checked against current repository evidence.
- [x] 6.3 Explicit approval received from Administrator on 2026-07-21.
- [N/A] 6.4 `sprint-status.yaml` requires no update because no epic or story changes.
- [x] 6.5 No-op Developer/Technical Writer handoff and success criteria confirmed.
