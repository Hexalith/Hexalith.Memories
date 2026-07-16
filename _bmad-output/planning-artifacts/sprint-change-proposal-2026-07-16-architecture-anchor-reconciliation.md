---
project: memories
date: 2026-07-16
status: approved
change_scope: minor
approved_by: Administrator
---

# Sprint Change Proposal — Reconcile architecture anchors with live code

## 1. Issue Summary

Epic 18's retrospective action item 2 identified four stale implementation anchors in `architecture.md`: the Memory Unit identifier was described as a ULID, the test framework was pinned to xUnit 2.9.3, the REST surface was shown as MVC controllers, and the Memories Server Dapr app ID was shown as `memories-server`.

Repository inspection established that the requested reconciliation is already complete. Commit `edcdade8` applied all four corrections on 2026-06-25, the current architecture contains the corrected anchors, and `architecture.md` has no working-tree diff.

Evidence:

- `IngestionWorkflow.ResolveMemoryUnitId` returns the workflow `InstanceId` when it is suitable and otherwise returns `context.NewGuid().ToString()`; callers receive an opaque identifier, not a ULID or a time-sortable contract.
- central package management pins `xunit.v3` and `xunit.v3.assert` to 3.2.2.
- `Server/Program.cs` registers the product REST surface through `app.MapIngestionEndpoints()`, `app.MapTenantLifecycleEndpoints()`, `app.MapSearchEndpoints()`, and the other `app.MapX` endpoint groups. `app.MapControllers()` remains intentionally present for the EventStore Dapr event-ingestion controller and does not make the product REST surface controller-based.
- `AddHexalithMemoriesSearchIndexServer` defaults `serverName` to `memories` and assigns that value to the Dapr sidecar `AppId`.
- Git history shows the four stale forms replaced by commit `edcdade8`.

## 2. Impact Analysis

### Epic and story impact

- Epic 18 remains complete and unchanged.
- Stories 18.3 and 18.6 remain complete; their routing and Memory Unit identity contracts already align with the live implementation.
- Epic 18 retrospective action item 2 can be treated as resolved by commit `edcdade8` and this evidence review.
- No current or future epic requires scope adjustment, resequencing, or a new story.
- `sprint-status.yaml` requires no change because no epic or story status changed.

### Artifact conflicts

- **PRD:** No conflict. Functional requirements, non-functional requirements, MVP scope, and success measures remain unchanged.
- **Architecture:** The historical conflict is already resolved. The current file accurately records all four requested anchors.
- **UX:** Not applicable; the reconciliation does not change a user flow, interaction, or visual contract.
- **Other artifacts:** Historical story, test, and retrospective records may retain version or wording that was true when they were authored. They are evidence records and must not be bulk-rewritten as if they were current architecture guidance.
- **Operations and deployment:** No infrastructure or configuration change is required; this proposal validates the existing server app-ID default rather than changing it.

### Technical impact

The reconciliation is documentation-only and behavior-neutral. It changes no runtime path, public API, serialized contract, package dependency, deployment topology, or test execution. No code or test modification remains.

## 3. Recommended Approach

Use **Direct Adjustment**, recognized as already applied.

- **Effort:** None remaining
- **Risk:** Low
- **Timeline impact:** None
- **MVP impact:** None
- **Release impact:** None
- **Rollback:** Not justified; restoring any stale anchor would make the architecture disagree with live code.
- **PRD/MVP review:** Not applicable; product scope and readiness are unaffected.

## 4. Detailed Change Proposal

### 4.1 Memory Unit identifier contract

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
| `Id` | string (ULID) | Yes | Generated | Globally unique, time-sortable |
```

**Proposed state, already present:**

```markdown
| `Id` | string (opaque) | Yes | Generated | Workflow `InstanceId` (a GUID) or a fresh GUID via `ResolveMemoryUnitId`; opaque, **not** a ULID and **not** time-sortable. Stability semantics: `docs/dev/memory-unit-id-stability.md` |
```

**Rationale:** The implementation preserves a suitable workflow instance ID or derives a new GUID string. Neither path establishes ULID syntax or time-sort ordering as a caller-visible contract.

### 4.2 Test framework version

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
**Framework:** xUnit 2.9.3, Shouldly 4.3.0, NSubstitute 5.3.0, coverlet.collector
```

**Proposed state, already present:**

```markdown
**Framework:** xUnit v3 (`xunit.v3` 3.2.2), Shouldly 4.3.0, NSubstitute 5.3.0, coverlet.collector
```

**Rationale:** `references/Hexalith.Builds/Props/Directory.Packages.props` centrally pins `xunit.v3` 3.2.2. The architecture should identify both the generation and the package/version used by the test projects.

### 4.3 Server route organization

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
| REST (external) | `Controllers/*Controller.cs` | `IngestionValidator` + FluentValidation | Direct tenant ID | `TenantAuthorizationMiddleware` |
```

The historical project tree also listed `Server/Controllers/` and five product controller classes.

**Proposed state, already present:**

```markdown
| REST (external) | minimal-API endpoints in `Program.cs` (`app.MapGet/MapPost/...`) | `IngestionValidator` + FluentValidation | JWT bearer fallback policy plus tenant authorization | `TenantAuthorizationMiddleware` and endpoint filters |
```

The current project map places the REST surface under `Server/Endpoints/` and `Server/Program.cs`. The EventStore integration project's `EventIngestionController` is an intentional Dapr pub/sub adapter and is not evidence of a controller-based product REST surface.

**Rationale:** `Program.cs` composes the product endpoints with `app.MapX` extension methods, and the endpoint files map verbs through `MapGet`, `MapPost`, `MapPut`, `MapPatch`, and `MapDelete`.

### 4.4 Memories Server Dapr app ID

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Historical old text:**

```markdown
| Memories Server | C# (.NET 10) | `memories-server` | Core domain: ingestion, search, tenants, fusion | Controllers, CLI, MCP |
```

The historical AppHost example also used `memories-server` for the resource name and sidecar `AppId`.

**Proposed state, already present:**

```markdown
| Memories Server | C# (.NET 10) | `memories` | Core domain: ingestion, search, tenants, fusion | CLI, MCP, REST / Dapr callers |
```

The AppHost example now uses `memories` for both the resource name and sidecar `AppId`.

**Rationale:** `AddHexalithMemoriesSearchIndexServer` defaults `serverName` to `memories` and passes it to `DaprSidecarOptions.AppId`.

No additional architecture edit will be applied because the proposed state is already committed and verified.

## 5. Implementation Handoff

**Classification:** Minor — already resolved.

**Recipient:** Developer agent.

**Responsibilities after approval:**

- No implementation work remains.
- Preserve the four corrected architecture anchors.
- Treat commit `edcdade8`, the live-code anchors, and this proposal as closure evidence for Epic 18 retrospective action item 2.
- Do not modify historical evidence records, unrelated working-tree changes, or sprint status for this no-op closure.

**Success criteria:**

- `architecture.md` describes Memory Unit IDs as opaque and GUID-derived, without promising ULID syntax or time ordering — met.
- `architecture.md` identifies xUnit v3 package version 3.2.2 — met.
- `architecture.md` maps the product REST surface to minimal APIs composed through `app.MapX`, while retaining the EventStore Dapr-controller nuance — met.
- `architecture.md` records `memories` as the default Memories Server Dapr app ID — met.
- `architecture.md` has no working-tree diff and no additional edit is needed — met.
- PRD, epics, UX, sprint status, runtime code, and historical evidence remain unchanged — met.

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-16 | Trigger confirmed from Epic 18 retrospective action item 2 | Complete |
| 2026-07-16 | PRD, epics, architecture, UX, live code, and Git history reviewed | Complete |
| 2026-07-16 | Direct Adjustment evaluated | Already applied by `edcdade8` |
| 2026-07-16 | Current architecture checked for a working-tree diff | Clean |
| 2026-07-16 | Proposal approved by Administrator | Approved |
| 2026-07-16 | Minor-scope handoff to Developer agent | Complete; no implementation remaining |

## Checklist Record

### 1. Understand the trigger and context

- [N/A] 1.1 No implementation story is failing; the trigger is Epic 18 retrospective action item 2, informed by Stories 18.3 and 18.6.
- [x] 1.2 Core problem defined: four architecture anchors historically disagreed with live implementation.
- [x] 1.3 Evidence collected from the retrospective, architecture, source, central package management, and Git history.

### 2. Epic impact assessment

- [x] 2.1 Epic 18 and the current sprint plan remain valid.
- [N/A] 2.2 No epic-level modification is required.
- [x] 2.3 Remaining epics reviewed; no dependency impact found.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No priority or sequencing change is needed.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no conflict or modification required.
- [x] 3.2 Architecture reviewed; all four historical conflicts are already resolved.
- [N/A] 3.3 UX is unaffected.
- [x] 3.4 Live code, tests/packages, deployment composition, historical evidence, and operational impact reviewed; no further artifact edit is required.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable and already applied; effort none, risk low.
- [N/A] 4.2 Rollback is unnecessary and would restore stale guidance.
- [N/A] 4.3 PRD/MVP review is unnecessary.
- [x] 4.4 Direct Adjustment selected and verified.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact and action plan documented.
- [x] 5.5 Minor-scope Developer handoff documented.

### 6. Final review and handoff

- [x] 6.1 Applicable checklist items completed.
- [x] 6.2 Proposal checked against repository evidence.
- [x] 6.3 Explicit approval received from Administrator on 2026-07-16.
- [N/A] 6.4 `sprint-status.yaml` requires no update.
- [x] 6.5 Developer handoff, success criteria, and closure evidence confirmed; no implementation remains.
