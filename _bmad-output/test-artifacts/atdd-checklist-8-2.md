---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-generation-mode', 'step-03-test-strategy', 'step-04-generate-tests', 'step-05-validate-and-complete']
lastStep: 'step-05-validate-and-complete'
lastSaved: '2026-04-19'
story_id: '8-2'
story_title: 'Consistency Verification & Repair'
detected_stack: 'backend'
mode: 'C'
inputDocuments:
  - _bmad-output/implementation-artifacts/8-2-consistency-verification-and-repair.md
  - _bmad/tea/config.yaml
  - _bmad/core/config.yaml
  - _bmad/tea/testarch/knowledge/test-levels-framework.md
  - _bmad/tea/testarch/knowledge/test-priorities-matrix.md
  - _bmad/tea/testarch/knowledge/test-quality.md
  - tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs
  - tests/Hexalith.Memories.Server.Tests/Activities/Indexing/VerifyConsistencyActivityTests.cs
  - tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs
  - tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs
  - src/Hexalith.Memories.Server/Activities/Indexing/VerifyConsistencyActivity.cs
  - src/Hexalith.Memories.Server/Activities/Indexing/ConsistencyResult.cs
---

# ATDD Checklist — Story 8.2: Consistency Verification & Repair

Generate failing acceptance tests (xUnit + NSubstitute + Shouldly) ahead of implementation, covering the 9 ACs of story 8.2.

## Step 1 — Preflight & Context

### Stack detection
- `test_stack_type: auto` → **backend** (multiple `.csproj`, `Hexalith.Memories.slnx`, no root-level `package.json`/Playwright config). Submodules contain `package.json` but are not in the active test surface.
- TEA config flags: `tea_use_playwright_utils=true`, `tea_use_pactjs_utils=true`, `tea_pact_mcp=mcp`, `tea_browser_automation=auto`. For this backend story **Playwright Utils and Pact.js fragments are NOT applicable** (no UI/browser; not a microservice contract story). `contract-testing.md` also skipped — consistency is intra-service.

### Prerequisites — all green
- Story `8-2-consistency-verification-and-repair.md` present (`ready-for-dev`, 9 ACs, test inventory in AC #9).
- Test framework configured: xUnit + NSubstitute + Shouldly across `Hexalith.Memories.Server.Tests`, `Hexalith.Memories.Cli.Tests`, `Hexalith.Memories.IntegrationTests`.
- .NET dev environment available (Windows 11, bash shell, `dotnet` CLI assumed; workflow build verification deferred to Step 5).

### Story surface (for test-generation scope)
- **Workflows (2):** `ConsistencyVerificationWorkflow`, `ConsistencyRepairWorkflow`.
- **Activities (2 new):** `EnumerateMemoryUnitIdsActivity`, `RepairUnitActivity`.
- **Services (3 new):** `ConsistencyInspectionService`, `SemanticIndexer`, `GraphNodeMerger`, plus static `RepairPlanCalculator`.
- **REST endpoints (5):** POST/GET verify, GET inspect, POST/GET repair on `/api/tenants/{tenantId}/consistency/*`.
- **Client (5 methods):** `MemoriesClient.{StartConsistencyVerificationAsync,GetConsistencyVerificationStatusAsync,InspectConsistencyAsync,StartConsistencyRepairAsync,GetConsistencyRepairStatusAsync}`.
- **CLI (3 commands):** `memories consistency verify|inspect|repair`.
- **Contracts (11 new records/enums) + `MemoriesJsonContext` registration.**
- **Docs:** `docs/dev/consistency.md`.

### Existing test patterns to mirror
- Workflow tests → `TenantDeletionWorkflowTests.cs` (NSubstitute on `WorkflowContext`, `Received.InOrder` for ordering, `context.CallActivityAsync<T>` mocking, `TestLogger` helper).
- Activity tests → `VerifyConsistencyActivityTests.cs` (NSubstitute on `IConnectionMultiplexer`/`IDatabase`/`IGraphQueryBuilder`, fake `RedisResult` payloads for FalkorDB).
- Endpoint tests → `TenantConfigurationEndpointTests.cs` (unit-level composable pieces: guards, JSON round-trips, `ToHttpResult`). **Note:** AC #9 explicitly calls for `WebApplicationFactory<Program>` on `ConsistencyEndpointTests` — existing endpoint tests don't use that factory today, so that file will need to introduce the pattern (blocker risk if the pattern doesn't fit the existing `Program.cs` — will flag in Step 3).
- Client tests → `MemoriesClientTests.cs` under `Hexalith.Memories.Cli.Tests/ClientRest/` (mocked `HttpMessageHandler`, `MemoriesJsonContext.Options` for serialization). AC #9 names a separate `Hexalith.Memories.Client.Rest.Tests` project that **does not exist today**; tests will be placed in the existing `Hexalith.Memories.Cli.Tests/ClientRest/` folder to match the project's current convention (flag in Step 3 for user decision).

### Risks surfaced during preflight
1. **No `Hexalith.Memories.Client.Rest.Tests` project exists** — story references it but the existing client tests live in `Hexalith.Memories.Cli.Tests/ClientRest/`. Will propose placing new client tests in the existing folder unless user prefers a new project.
2. **`ConsistencyEndpointTests` calls for `WebApplicationFactory<Program>`** — no existing endpoint test uses that pattern. Server program may need `InternalsVisibleTo`/`[assembly: InternalsVisibleTo]` or `Program.cs` may need to expose a `public partial class Program` shim. Will generate the failing tests and document the blocker.
3. **10K discrepancies truncation (AC #8, Risk #7)** — needs seedable workflow path; failing test will assert the contract even before implementation exists.
4. **DAPR Workflow harness** — existing `TenantDeletionWorkflowTests` uses NSubstitute `WorkflowContext` — pattern is proven.

→ Proceed to Step 2 (generation mode selection).

## Step 2 — Generation Mode

**Mode: AI Generation** (no browser recording).

Rationale: `{detected_stack} = backend`. Per workflow §2, backend projects always use AI generation from source code / contracts / ACs. Story 8.2 ACs are concrete (presence matrix, HTTP shapes, workflow transitions); no UI interactions to record.

Inputs to generation:
- Story ACs + Risk→Guard-test table (sections already extracted in Step 1).
- Existing NSubstitute patterns for `WorkflowContext`, `IConnectionMultiplexer`, `IDatabase`, `IGraphQueryBuilder` (from `TenantDeletionWorkflowTests` + `VerifyConsistencyActivityTests`).
- `MemoriesJsonContext.Options` + `ErrorResponse` / `MemoriesRemoteException` patterns from existing client tests.

→ Proceed to Step 3 (test strategy).

## Step 3 — Test Strategy

### Scope of this ATDD pass

ATDD seeds the **RED phase**: a tight seminal suite of failing tests that drives the core business logic of 8.2 through TDD red→green→refactor. AC #9 remains the authoritative inventory for the *complete* test set (66+ tests across 11 classes) — the balance lands naturally during the dev-story phase after red→green converges on the seminal set.

Deliberate scope trims vs. AC #9:

| Surface | Seminal RED tests | Full AC #9 inventory | Rationale |
|---|---:|---:|---|
| `RepairPlanCalculator` | 1 `[Theory]`×8 | 1 `[Theory]`×8 | Pure function; one test block covers all risks. |
| `ConsistencyInspectionService` | 3 | 6 | Core probes + guard for Risk #4; full cancellation/edge coverage in dev-story. |
| `EnumerateMemoryUnitIdsActivity` | 2 | 5 | Pin Risk #3 + Risk #6; remaining 3 land in dev-story. |
| `RepairUnitActivity` | 3 | 8 | Pin Risk #1 + AC #5 + AC #7; 5 more land in dev-story. |
| `ConsistencyVerificationWorkflow` | 2 | 7 | Pin AC #2 invariant + Risk #7 truncation; 5 more in dev-story. |
| `ConsistencyRepairWorkflow` | 2 | 6 | Pin Risk #1 + Risk #5; 4 more in dev-story. |
| Contract serialization | 1 `[Theory]`×11 | embedded in AC #9 classes | Round-trip every new V1 record through `MemoriesJsonContext`. |
| **Endpoint / Client / CLI** | **0** | 25 | **Deferred to dev-story**: HTTP / transport tests arrive after the core logic is green. ATDD red-phase focuses on replay-safe workflow/activity/service logic. |
| **Integration** | **0** | 3 | **Deferred**: integration skip/un-skip decision belongs to dev-story (per AC #9 + story Task 8.1). |

**Total RED-phase seminal tests: 14 methods across 7 test files.** Every one of the 8 risks in the story's Risk→Guard-test table is pinned; every AC with testable logic (1–8) has at least one anchor.

### AC → Level → Priority → Test mapping

All RED-phase tests are **unit** level (no DB/network; NSubstitute for external dependencies). Naming follows existing project convention (`MethodName_Scenario_Expected`). Test IDs follow `8.2-UNIT-NNN`.

| ID | Test | Class | AC | Risk | Priority | Why |
|---|---|---|---|---|---|---|
| 8.2-UNIT-001 | `Calculate_EveryPresenceCombination_MapsToExpectedRecommendation` `[Theory]`×8 | `RepairPlanCalculatorTests` | #2, #7 | #8 | **P0** | Pure-function source-of-truth for repair decisions; 8 rows cover every presence combination. |
| 8.2-UNIT-002 | `InspectAsync_AllBackendsPresent_ReturnsInspectionResultWithNoOp` | `ConsistencyInspectionServiceTests` | #3 | — | **P0** | Happy path of the synchronous inspection endpoint. |
| 8.2-UNIT-003 | `InspectAsync_MalformedMemoryUnitId_ThrowsArgumentException` | `ConsistencyInspectionServiceTests` | #3 | **#4** | **P0** | Cypher-injection guard before query builder. |
| 8.2-UNIT-004 | `InspectAsync_AllBackendsMissing_ThrowsKeyNotFoundException` | `ConsistencyInspectionServiceTests` | #3 | — | **P0** | 404 mapping for unknown ID. |
| 8.2-UNIT-005 | `RunAsync_OrphanInVectorOnly_IsReturnedInUnion` | `EnumerateMemoryUnitIdsActivityTests` | #1, #5 | **#3** | **P0** | Union must include vector-only orphans; missing → repair misses them. |
| 8.2-UNIT-006 | `RunAsync_UsesCursorScan_NotKeysCommand` | `EnumerateMemoryUnitIdsActivityTests` | #1 | **#6** | **P0** | SCAN-not-KEYS regression guard. |
| 8.2-UNIT-007 | `RunAsync_ReVerifyReturnsConsistent_SkipsAction` | `RepairUnitActivityTests` | #4 | **#1** | **P0** | Re-verify-before-act safety. Load-bearing. |
| 8.2-UNIT-008 | `RunAsync_RemoveOrphanedSemantic_DeletesVectorKey` | `RepairUnitActivityTests` | #5 | — | **P0** | Orphan semantic removal. |
| 8.2-UNIT-009 | `RunAsync_Unrepairable_ReturnsSucceededFalseWithReason` | `RepairUnitActivityTests` | #7 | — | **P1** | Unrepairable record contract. |
| 8.2-UNIT-010 | `RunAsync_AggregateCounts_InvariantHolds` | `ConsistencyVerificationWorkflowTests` | #2 | — | **P0** | `consistentCount + inconsistentCount = totalUnits`. |
| 8.2-UNIT-011 | `RunAsync_TenThousandDiscrepancies_ResultTruncated` | `ConsistencyVerificationWorkflowTests` | #8 | **#7** | **P0** | Truncation cap + `TotalDiscrepancyCount` preserved. |
| 8.2-UNIT-012 | `RunAsync_ReVerifyDiffers_NoMutation` | `ConsistencyRepairWorkflowTests` | #4 | **#1** | **P0** | Stale verify snapshot → repair must not act. |
| 8.2-UNIT-013 | `RunAsync_ThreePassesFail_RemainingMarkedUnrepairable` | `ConsistencyRepairWorkflowTests` | #7 | **#5** | **P1** | Convergence ceiling. |
| 8.2-UNIT-014 | `MemoriesJsonContext_NewV1Contracts_RoundTrip` `[Theory]`×11 | `ConsistencyContractSerializationTests` | #1–#7 | — | **P1** | Source-gen registration for every new V1 record; catches missing `[JsonSerializable]` attributes. |

### File layout (RED-phase)

```
tests/Hexalith.Memories.Server.Tests/
├── Consistency/
│   ├── RepairPlanCalculatorTests.cs                 (NEW)
│   └── ConsistencyInspectionServiceTests.cs         (NEW)
├── Activities/Indexing/
│   ├── EnumerateMemoryUnitIdsActivityTests.cs       (NEW)
│   └── RepairUnitActivityTests.cs                   (NEW)
└── Workflows/
    ├── ConsistencyVerificationWorkflowTests.cs      (NEW)
    └── ConsistencyRepairWorkflowTests.cs            (NEW)

tests/Hexalith.Memories.Contracts.Tests/
└── V1/
    └── ConsistencyContractSerializationTests.cs     (NEW)
```

### Red-phase failure mechanism (expected)

Every test file will fail to compile until implementation lands, because every test references types that do not yet exist:

- `Hexalith.Memories.Contracts.V1.{ConsistencyRepairRecommendation, ConsistencyVerificationRequest, …}`
- `Hexalith.Memories.Server.Consistency.{RepairPlanCalculator, ConsistencyInspectionService, SemanticIndexer, GraphNodeMerger}`
- `Hexalith.Memories.Server.Activities.Indexing.{EnumerateMemoryUnitIdsActivity, RepairUnitActivity, EnumerateMemoryUnitIdsInput, EnumerateMemoryUnitIdsResult, RepairUnitInput, RepairUnitResult}`
- `Hexalith.Memories.Server.Workflows.{ConsistencyVerificationWorkflow, ConsistencyRepairWorkflow, ConsistencyVerificationInput, ConsistencyRepairInput}`

This is the **RED signal** TDD requires: the compiler itself reports the missing contract. The dev-story phase replaces each compile error with an implementation, and once the seminal 14 tests compile + pass, the remaining AC #9 inventory is expanded.

### Non-goals for this ATDD pass

1. Endpoint integration tests (`ConsistencyEndpointTests`) — deferred; `WebApplicationFactory<Program>` pattern is not yet established in Server.Tests.
2. Client tests (`MemoriesClientConsistencyTests`) — deferred; placement decision (new project vs. existing `Cli.Tests/ClientRest/`) left to dev-story.
3. CLI tests (`ConsistencyVerify/Inspect/RepairCommandTests`) — deferred; depend on client + formatter wiring.
4. Integration tests (`ConsistencyWorkflowIntegrationTests`) — deferred pending Aspire CS0311 status at dev-start.

These deferrals are tracked in the Completion Notes section of the story itself (Task 9 sprint-status) so the dev-story workflow picks them up.

→ Proceed to Step 4 (generate failing tests).

## Step 4 — Generate Failing Tests (RED phase)

Executed in **sequential mode** (backend/.NET stack doesn't fit the Playwright-focused parallel subagents; the skill's `step-04a` / `step-04b` orchestration is for TypeScript/browser tests). Tests authored directly, using the xUnit idiom `[Fact(Skip = "ATDD RED — …")]` with body-preserving safety net (`Assert.Fail(…)` so the test fails loudly if `Skip` is removed prematurely) and a full XML-doc / in-line blueprint block.

### Files created

| File | Tests | ATDD IDs | Status |
|---|---:|---|---|
| `tests/Hexalith.Memories.Server.Tests/Consistency/RepairPlanCalculatorTests.cs` | 1 theory × 8 rows | 8.2-UNIT-001 | SKIPPED |
| `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs` | 3 | 8.2-UNIT-002 · 003 · 004 | SKIPPED |
| `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs` | 2 | 8.2-UNIT-005 · 006 | SKIPPED |
| `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs` | 3 | 8.2-UNIT-007 · 008 · 009 | SKIPPED |
| `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs` | 2 | 8.2-UNIT-010 · 011 | SKIPPED |
| `tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs` | 2 | 8.2-UNIT-012 · 013 | SKIPPED |
| `tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs` | 1 theory × 11 types | 8.2-UNIT-014 | SKIPPED |

**Totals:** 7 new files, 14 test methods (some theory-backed, rows collapse to 1 when Skip is applied at the attribute level → 13 discoverable cases in Server.Tests + 1 in Contracts.Tests).

### RED-phase encoding choice

Two options were considered for expressing RED in a statically-typed C# project:

1. **Compile-fail RED** — reference target types directly; the compiler itself reports the missing contract. This is the canonical TDD RED signal but breaks the entire test assembly until scaffolding lands, preventing any other test from running in the interim.
2. **Skip-gated RED** (**chosen**) — body compiles today (no reference to unimplemented types); `[Fact(Skip = …)]` keeps the suite green; rich XML-doc + commented-blueprint blocks contain the precise arrange/act/assert the dev will uncomment; `Assert.Fail(…)` ensures that removing `Skip` before implementation fails loudly with the contract text.

Option 2 wins because it (a) preserves the ability to run the existing 500+ green tests during the dev-story iteration, (b) keeps CI green, and (c) still carries the full contract blueprint to the dev — each skipped test is an executable spec.

### Build + skip verification

- `dotnet build tests/Hexalith.Memories.Server.Tests/...` → **Build succeeded. 0 Warning(s). 0 Error(s).**
- `dotnet build tests/Hexalith.Memories.Contracts.Tests/...` → **Build succeeded. 0 Warning(s). 0 Error(s).**
- `dotnet test ... --filter` on the 6 Server test classes → `Skipped! - Failed: 0, Passed: 0, Skipped: 13`.
- `dotnet test ... --filter` on the Contracts serialization class → `Skipped! - Failed: 0, Passed: 0, Skipped: 1`.

### Red-phase activation protocol (for the dev-story agent)

When Amelia (dev agent) opens Story 8.2 to implement:

1. **Scaffold types first** (Tasks 1–3 of the story). Each target type referenced in an ATDD blueprint must exist before the test is activated.
2. **Per-test activation** — remove `Skip = …`, uncomment the blueprint `using` directives + body, and delete the `Assert.Fail(…)` safety net. The test should now FAIL (red) against the empty implementation.
3. **Implement against the failing test** until green.
4. **Expand to full AC #9 inventory** — the seminal 14 tests anchor the core logic; the remaining ~52 tests (endpoint, client, CLI, integration) land during the rest of the dev-story workflow.

### Risk coverage

| # | Story risk | Pinned by |
|---|---|---|
| 1 | Repair destroys live data (stale verify) | 8.2-UNIT-007 · 8.2-UNIT-012 |
| 2 | Fan-out overwhelms backend | *deferred (bounded-concurrency test belongs to dev-story full fan-out suite)* |
| 3 | Orphan in graph/vector missed | 8.2-UNIT-005 |
| 4 | Cypher injection via inspection URL | 8.2-UNIT-003 |
| 5 | Repair loop diverges | 8.2-UNIT-013 |
| 6 | SCAN-vs-KEYS regression | 8.2-UNIT-006 |
| 7 | Workflow state size limit | 8.2-UNIT-011 |
| 8 | Authoritative-source ambiguity | 8.2-UNIT-001 (rows with `F` syntactic) |

7 of 8 risks pinned in the RED-phase seminal set. Risk #2 (fan-out) is deferred because a meaningful test requires the fan-out loop to exist first; it's tracked in the story at `ConsistencyVerificationWorkflowTests.BatchedFanOut_DoesNotExceedBatchSize`.

→ Proceed to Step 5 (validate & complete).

## Step 5 — Validate & Complete

### Checklist validation

- [x] **Prerequisites satisfied** — Story 8.2 ready-for-dev with 9 ACs; xUnit + NSubstitute + Shouldly configured; `.NET SDK 10` preview in place.
- [x] **Test files created correctly** — 7 files, 14 test methods, all compile, all properly Skip-gated. See table above.
- [x] **Checklist matches acceptance criteria** — ACs #1, #2, #3, #4, #5, #7, #8 each have at least one anchor test; AC #6 (re-index from syntactic) covered by the blueprint comments in `RepairUnitActivityTests` (seminal tests focused on the load-bearing AC #4/#5/#7 paths; re-index variants will be added during dev-story expansion). AC #9 is the inventory AC (meta); the ATDD suite itself is the beginning of satisfying it. AC #10 is a docs AC (non-code).
- [x] **Tests are designed to fail before implementation** — every seminal test asserts expected behavior that cannot be provided by any existing code, and every test has an `Assert.Fail(…)` safety net that triggers if `Skip` is removed prematurely.
- [x] **No orphan browser sessions** — backend stack; no browser automation used.
- [x] **Temp artifacts in `{test_artifacts}`** — `atdd-checklist-8-2.md` created at `_bmad-output/test-artifacts/atdd-checklist-8-2.md`; no artifacts outside that directory.

### Key assumptions / risks

1. **`Hexalith.Memories.Client.Rest.Tests` project does not exist.** Story AC #9 references it. The existing client tests live in `Hexalith.Memories.Cli.Tests/ClientRest/`. Flag for the dev agent: choose whether to (a) create the new test project or (b) colocate the 5 new client tests with the existing ones. **Decision deferred** — no seminal client tests included in this ATDD pass.
2. **`WebApplicationFactory<Program>` unused today.** `Microsoft.AspNetCore.Mvc.Testing` is referenced in the Server.Tests project (line 24 of the csproj, added by Story 7.5) but **no existing endpoint test instantiates the full host**. AC #9's `ConsistencyEndpointTests.cs` is the first. Dev should validate that `Program.cs` is accessible (may need `public partial class Program { }` at the bottom of the file or `[assembly: InternalsVisibleTo]`).
3. **Aspire CS0311 status at dev-start is unknown.** Story Task 8.1 allows `[Fact(Skip)]` inheritance from Story 5.6 / 8.1. This ATDD pass does **not** author integration tests — dev-story decides skip/un-skip at execution time.
4. **xUnit 2.9.3 skip-attribute behavior on `[Theory]`.** Confirmed: `[Theory(Skip = "…")]` collapses all InlineData rows into a single skipped test case in the report. This is fine for RED-phase reporting but means the dev should expect `[Theory]` to expand to 8 (PlanCalculator) or 11 (Serialization) individual test cases once `Skip` is removed.

### Next recommended workflow

**`bmad-dev-story`** with story-id `8-2`. The 14 RED-phase tests are the implementation blueprint:
- **Task 3.4** (RepairPlanCalculator) gates 8.2-UNIT-001.
- **Task 3.3** (ConsistencyInspectionService) gates 8.2-UNIT-002 / 003 / 004.
- **Task 1.1** (EnumerateMemoryUnitIdsActivity) gates 8.2-UNIT-005 / 006.
- **Task 1.3** (RepairUnitActivity) gates 8.2-UNIT-007 / 008 / 009.
- **Task 2.1** (ConsistencyVerificationWorkflow) gates 8.2-UNIT-010 / 011.
- **Task 2.3** (ConsistencyRepairWorkflow) gates 8.2-UNIT-012 / 013.
- **Tasks 3.1 + 3.2** (V1 contracts + JsonContext) gates 8.2-UNIT-014.

After dev-story converges RED → GREEN on the seminal 14, the dev agent expands to the full AC #9 inventory (endpoint / client / CLI / integration tests).

### Completion summary

- **Test files created:** 7 (6 in Server.Tests, 1 in Contracts.Tests). All compile, all Skip-gated.
- **Checklist output:** `D:\Hexalith.Memories\_bmad-output\test-artifacts\atdd-checklist-8-2.md` (this file).
- **Tests verified via `dotnet test`:** 14 Skipped · 0 Failed · 0 Passed on the ATDD filter. The rest of the suite remains unaffected.
- **Sprint-status transition:** NOT modified by ATDD — Story 8.2 stays `ready-for-dev`; the dev-story workflow is responsible for flipping it to `in-progress` / `review` / `done`.

---

## Addendum — Full AC #9 Inventory Expansion

The initial 14-test seminal set was expanded to match the full **AC #9 test inventory (~66 tests)** across **11 files and 4 test projects** — no surface deferred. All new tests remain `[Fact(Skip = "ATDD RED — …")]`-gated so the repo stays green.

### Files (11 total)

**Server.Tests (7 files, 41 tests after expansion):**

| File | Tests | Status |
|---|---:|---|
| `Consistency/RepairPlanCalculatorTests.cs` | 1 Theory × 8 rows | SKIPPED |
| `Consistency/ConsistencyInspectionServiceTests.cs` | 3 → **6** | SKIPPED |
| `Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs` | 2 → **5** | SKIPPED |
| `Activities/Indexing/RepairUnitActivityTests.cs` | 3 → **8** | SKIPPED |
| `Workflows/ConsistencyVerificationWorkflowTests.cs` | 2 → **7** | SKIPPED |
| `Workflows/ConsistencyRepairWorkflowTests.cs` | 2 → **6** | SKIPPED |
| `Endpoints/ConsistencyEndpointTests.cs` *(new)* | **8** | SKIPPED |

**Contracts.Tests (1 file):**

| File | Tests | Status |
|---|---:|---|
| `V1/ConsistencyContractSerializationTests.cs` | 1 Theory × 11 types | SKIPPED |

**Cli.Tests (4 files, 16 tests):**

| File | Tests | Status |
|---|---:|---|
| `ClientRest/MemoriesClientConsistencyTests.cs` *(new)* | **5** | SKIPPED |
| `Cli/ConsistencyVerifyCommandTests.cs` *(new)* | **4** | SKIPPED |
| `Cli/ConsistencyInspectCommandTests.cs` *(new)* | **3** | SKIPPED |
| `Cli/ConsistencyRepairCommandTests.cs` *(new)* | **4** | SKIPPED |

**IntegrationTests (1 file):**

| File | Tests | Status |
|---|---:|---|
| `Consistency/ConsistencyWorkflowIntegrationTests.cs` *(new)* | **3** | SKIPPED (Aspire CS0311 reason string) |

### Count totals

- **Test methods skipped (post-expansion):** **61** (theory tests collapse to 1 each when Skip is applied at the attribute level; expands to 79 individual cases when Skip is removed — 8 rows for `RepairPlanCalculator` + 11 rows for `ConsistencyContract`, plus 58 Facts).
- **Files created or rewritten:** 11 (6 new, 5 rewrites expanding existing seminal files).
- **Test projects touched:** 4 (Server.Tests, Contracts.Tests, Cli.Tests, IntegrationTests).

### Build + run verification (second pass)

| Project | Build | Filtered run |
|---|---|---|
| `Hexalith.Memories.Server.Tests` | 0 errors, 0 warnings | 41 Skipped (13 pre-existing `VerifyConsistencyActivityTests` also matched by filter and passed; **0 Failed**) |
| `Hexalith.Memories.Contracts.Tests` | 0 errors, 0 warnings | 1 Skipped, 0 Failed, 0 Passed |
| `Hexalith.Memories.Cli.Tests` | 0 errors, 0 warnings | 16 Skipped, 0 Failed, 0 Passed |
| `Hexalith.Memories.IntegrationTests` | 0 errors, 0 warnings | 3 Skipped, 0 Failed, 0 Passed |

### Expanded AC → Test coverage

| AC | Tests anchoring the AC |
|---|---|
| #1 (verify workflow + three-backend enumeration) | 8.2-UNIT-010a · 010b · 010c · 010d · 010e · 005 · 005a · 005b · 006 · 006b · ENDPOINT-001 · 002 · CLIENT-001 · 002 · CLI-001 · 002 · 003 · 004 · INT-001 · INT-002 |
| #2 (discrepancy shape + invariant) | 8.2-UNIT-001 · 010 · 010c |
| #3 (per-unit inspection) | 8.2-UNIT-002 · 002b · 002c · 002d · 003 · 004 · ENDPOINT-003 · 004 · 005 · CLIENT-003 · CLI-005 · 006 · 007 |
| #4 (repair re-verifies before acting) | 8.2-UNIT-007 · 012 · 013b · ENDPOINT-006 · 007 · CLIENT-004 · 005 · CLI-008 · 009 · 010 |
| #5 (orphan removal) | 8.2-UNIT-008 · 008b · 008c · INT-002 · INT-003 |
| #6 (re-index from syntactic) | 8.2-UNIT-010a · 010b · 010c · 013a |
| #7 (unrepairable flagging) | 8.2-UNIT-001 (rows with `F` syntactic) · 009 · 013 · CLI-011 |
| #8 (batched + progress visibility) | 8.2-UNIT-010d · 011 · CLI-002 · 009 |
| #9 (test inventory) | this file ✓ |
| #10 (docs) | not-code (docs/dev/consistency.md authored in dev-story) |

### Risk coverage (complete)

| # | Risk | Pinned by |
|---|---|---|
| 1 | Repair destroys live data from stale verify | 8.2-UNIT-007 · 012 |
| 2 | Unbounded fan-out overwhelms backend | 8.2-UNIT-010d |
| 3 | Orphan in graph / vector missed | 8.2-UNIT-005 · 005b |
| 4 | Cypher injection via inspection URL | 8.2-UNIT-003 · ENDPOINT-005 · CLI-007 |
| 5 | Repair loop diverges | 8.2-UNIT-013 |
| 6 | SCAN-vs-KEYS regression | 8.2-UNIT-006 |
| 7 | Workflow state size limit | 8.2-UNIT-011 |
| 8 | Authoritative source ambiguity | 8.2-UNIT-001 (theory rows with `F` syntactic) |

**8 of 8 risks pinned.** No deferrals.

### Dev-story activation protocol (unchanged)

1. Scaffold target types for a Task (e.g., Task 3.4 → `RepairPlanCalculator`).
2. Open the corresponding ATDD test file, remove `Skip`, uncomment the blueprint `using` directives + body, delete `Assert.Fail(…)`.
3. Run: test FAILS (RED — no implementation).
4. Implement until GREEN.
5. Move to next Task.

### Flagged decisions for dev-story

1. **`Hexalith.Memories.Client.Rest.Tests` project doesn't exist.** New client tests colocated in `Hexalith.Memories.Cli.Tests/ClientRest/` matching the existing `MemoriesClientTests.cs` convention. Dev may choose to split into a new project.
2. **`WebApplicationFactory<Program>` pattern.** `Microsoft.AspNetCore.Mvc.Testing` already referenced (Story 7.5 line 24 of csproj); `Program.cs` may need `public partial class Program { }` shim for `WebApplicationFactory<Program>` to resolve.
3. **`ConsistencyWorkflowIntegrationTests` skip reason** set to `"Aspire fixture build failure tracked in 5.6 Dev Notes — Story 8.2 Task 8.1"`. Dev un-skips if the CS0311 build error is resolved at dev-start (Pre-flight step 7 of story Dev Notes).
4. **Formatter registration (Task 6.5)** — `CommandPayloadRegistry` entries for `ConsistencyVerificationResult`, `ConsistencyInspectionResult`, `ConsistencyRepairResult` gate `8.2-CLI-001` through `8.2-CLI-011`.
5. **Error code registration (Task 6.6)** — `ErrorMessageCatalog.cs` entries `MEMORY_UNIT_NOT_FOUND`, `INVALID_MEMORY_UNIT_ID`, `CONSISTENCY_WORKFLOW_TIMEOUT`, `CONSISTENCY_VERIFY_NOT_FOUND`, `CONSISTENCY_REPAIR_NOT_FOUND` gate `8.2-CLI-006` / `007`.

### Final completion summary (post-expansion)

- **Test files touched:** 11 (6 new, 5 rewrites) across 4 test projects.
- **Tests authored:** 61 Skip-gated test methods (expands to 79 individual cases when theories unskipped).
- **Build status:** all 4 test projects compile clean (0 warnings, 0 errors).
- **Test status:** all 61 ATDD tests correctly Skipped; 0 Failed; existing suite unaffected.
- **AC coverage:** ACs #1–#8 anchored; AC #9 fulfilled by this ATDD pass; AC #10 is docs (dev-story).
- **Risk coverage:** 8 of 8 risks from the story's Risk→Guard-test table pinned.
- **Sprint-status transition:** still NOT modified by ATDD — Story 8.2 remains `ready-for-dev`; dev-story flips it.

