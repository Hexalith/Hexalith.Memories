---
baseline_commit: c35f766247df7fa9b7e9efa14b7da1ae3ccdb243
---

# Story 18.1: AppHost Project-Resolution Guard and Public-Surface Stability Contract

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

| Field | Value |
| :---- | :---- |
| Epic | 18 — Downstream Consumer Integration Contract Hardening |
| Story key | `18-1-apphost-project-resolution-guard-and-public-surface-stability-contract` |
| Origin | MEM-1 (Parties consumer correct-course intake, 2026-05-27; Parties passes 7-7 known-unrelated, 9-3) |
| Lifecycle track | Engineering/Operational Readiness — **not MVP-counted** |
| Release impact | **None.** Test + docs only; no public capability change. Use `test:` / `docs:` / `chore:` commits — **never `feat:`** (see Dev Notes → Commit & release discipline). |
| Parties-side follow-up | Parties adds its own AppHost compile assertion that `Projects.Hexalith_Memories_Server` resolves. |

## Story

As a maintainer of a downstream Aspire AppHost,
I want a compile-time guarantee that `Projects.Hexalith_Memories_Server` and `Projects.Hexalith_Memories_Mcp` resolve and that their public project/type names stay stable,
so that a clean clone with root submodules initialised builds the full `.slnx` without submodule-drift surprises.

## Acceptance Criteria

**AC1 — Buildable project-resolution guard test**
**Given** the AppHost already references `Projects.Hexalith_Memories_Server` and `Projects.Hexalith_Memories_Mcp`,
**When** a dedicated AppHost-resolution test runs,
**Then** it asserts those project symbols resolve at compile time as a buildable test, not an integration/Docker test, and does not depend on a running sidecar.

**AC2 — EventStore wiring surface confirmation (drift was a stale pin)**
**Given** the Parties intake reported `AddHexalithEventStore` redis-parameter drift,
**When** the EventStore wiring surface is reviewed,
**Then** the story confirms the current public wiring is `AddServerEventStoreIntegration(IConfiguration)` → `AddMemoriesEventStoreIntegration(IConfiguration, Action<EventStoreIntegrationBuilder>?)` with no redis parameter, and records that the reported drift was a stale submodule pin rather than a current API.

**AC3 — Public-surface stability contract recorded under `docs/dev`**
**Given** external AppHosts depend on stable project and assembly names,
**When** this story completes,
**Then** the project name, assembly name, and root namespace of `Hexalith.Memories.Server` and `Hexalith.Memories.Mcp` are recorded as a stability contract under `docs/dev`, and any future rename is flagged as requiring a breaking-change note.

> Implementation constraints that are NOT separate ACs but MUST hold (see Dev Notes): the new test runs in the **default (no-Docker) test lane** — it must be a normal `[Fact]`, **not** `[Fact(Skip=...)]`, and must **not** call `DistributedApplicationTestingBuilder` / provision containers; the solution must still build green under `TreatWarningsAsErrors=true`.

## Tasks / Subtasks

- [x] **Task 1 — Add the buildable project-resolution guard test (AC1)**
  - [x] Add a new test class `AppHostProjectResolutionTests.cs` to `tests/Hexalith.Memories.IntegrationTests/Fixtures/` (this project is the only test project that references the AppHost output assembly, so the generated `Projects.*` symbols already resolve here — see Dev Notes → Why IntegrationTests, not a new project).
  - [x] Assert `Projects.Hexalith_Memories_Server` and `Projects.Hexalith_Memories_Mcp` resolve **at compile time** by instantiating them (`new Projects.Hexalith_Memories_Server()` / `new Projects.Hexalith_Memories_Mcp()`) and treating each as `Aspire.Hosting.IProjectMetadata`. The mere reference makes the assembly fail to compile if a symbol stops resolving — that is the guard.
  - [x] Assert each instance's `ProjectPath` is non-null/non-whitespace and ends with the expected csproj filename (`Hexalith.Memories.Server.csproj`, `Hexalith.Memories.Mcp.csproj`). Use Shouldly (`ShouldNotBeNullOrWhiteSpace()`, `ShouldEndWith(...)`).
  - [x] Use a plain `[Fact]` (NOT `[Fact(Skip=...)]`). Do **not** call `DistributedApplicationTestingBuilder.CreateAsync<...>()` — that path requires Docker and would violate AC1. No fixture, no `IAsyncLifetime`, no Testcontainers.
  - [x] Keep the ITANEO MIT copyright header, file-scoped namespace `Hexalith.Memories.IntegrationTests.Fixtures`, and rely on the global `using Xunit;` from `tests/Directory.Build.props` (do not re-add `using Xunit;`).
  - [x] Add a class-level XML `<summary>` that cites Story 18.1 / MEM-1 and states this is a compile-time resolution guard that intentionally avoids Docker.

- [x] **Task 2 — Confirm and record the EventStore wiring surface + drift finding (AC2)**
  - [x] Re-verify (the codebase moves) the current signatures and quote them in the doc: `AddServerEventStoreIntegration(this IServiceCollection, IConfiguration)` (no redis param) and `AddMemoriesEventStoreIntegration(this IServiceCollection, IConfiguration, Action<EventStoreIntegrationBuilder>? configure = null)`.
  - [x] Record in `docs/dev/eventstore-integration.md` (the existing wiring doc) a short "Public wiring surface (stable)" note: the two signatures above, that **no `AddHexalithEventStore` redis parameter exists on the Memories side**, and that the Parties-reported `AddHexalithEventStore` redis-param "drift" was a **stale submodule pin**, not a current API. Cross-link the new stability doc from Task 3.
  - [x] Note that `AddHexalithEventStore` lives only in the `Hexalith.EventStore` submodule (`Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs`) and even there takes no redis parameter (Redis is wired implicitly via DAPR components). Do **not** modify the submodule.

- [x] **Task 3 — Publish the public-surface stability contract (AC3)**
  - [x] Create `docs/dev/public-surface-stability.md` recording the stability contract for the two consumer-facing host projects:
    - `Hexalith.Memories.Server` — project name / assembly name / root namespace all `Hexalith.Memories.Server`; Aspire metadata symbol `Projects.Hexalith_Memories_Server`.
    - `Hexalith.Memories.Mcp` — project name / assembly name / root namespace / `PackageId` all `Hexalith.Memories.Mcp`; Aspire metadata symbol `Projects.Hexalith_Memories_Mcp`.
  - [x] State the guarantee explicitly: these names are a stability contract for downstream AppHosts; **any future rename of the project, assembly name, root namespace, or PackageId is a breaking change requiring a breaking-change note** (and, for `Hexalith.Memories.Mcp`, a semantic-release `BREAKING CHANGE:` since it is the published package).
  - [x] Explain the Aspire symbol derivation rule for downstream maintainers: the `Aspire.AppHost.Sdk` generates a `public class Projects.<SanitizedName>` per `<ProjectReference>` where dots in the project name become underscores (so `Hexalith.Memories.Server` → `Projects.Hexalith_Memories_Server`). Renaming the project therefore silently changes the generated symbol — hence the breaking-change rule.
  - [x] Reference the guard test (Task 1) as the automated enforcement of the symbol-resolution half of this contract.
  - [x] Match `docs/dev` conventions: optional leading review-cadence HTML comment, `#` h1 title, intro paragraph, `##` sections, a reference table, story/ADR identifiers (cite "Story 18.1", "MEM-1"). Cross-link `experimental-apis.md` and `eventstore-integration.md`.

- [x] **Task 4 — Verify build + focused test (AC1–AC3, regression safety)**
  - [x] Build the solution / the IntegrationTests project; it must stay green under `TreatWarningsAsErrors=true` (root `Directory.Build.props`).
  - [x] Run the new test in the default (no-Docker) lane and confirm it passes. See Dev Notes → Running tests in this sandbox for the `dotnet exec` workaround (`dotnet test` fails here with SocketException 13).
  - [x] Record the test count delta in the Change Log (Epic 17 retro action item — track test counts at each phase).
  - [x] Confirm no submodule files were touched and no production code paths changed (this is a test + docs story).

## Dev Notes

### What this story is (and is not)

This is a **drift-guard test + documentation** story. The current `main` already resolves both project symbols and already exposes the no-redis EventStore wiring — a grounded codebase investigation (Sprint Change Proposal 2026-05-27) found the MEM-1 ask was **partly based on a stale assumption**. The residual gap this story closes is exactly two things: (1) a **dedicated compile-resolution guard test** (today only an integration test touches `Projects.Hexalith_Memories_AppHost`; nothing guards Server/Mcp), and (2) a **documented name-stability contract**. Do not add production code, do not change wiring, do not introduce a PublicAPI analyzer.

### Verified current state of every preflight anchor (re-verify before coding — the codebase moves)

**Anchor 1 — AppHost project references** (`src/Hexalith.Memories.AppHost/Program.cs`):
- Line 150–151: `builder.AddProject<Projects.Hexalith_Memories_Server>("memories", launchProfileName: "http")`
- Line 225–226: `builder.AddProject<Projects.Hexalith_Memories_Mcp>("memories-mcp", launchProfileName: "http")`
- Both symbols **resolve today**. They are generated because the AppHost csproj has `<ProjectReference>` to Server and Mcp and uses `Sdk="Aspire.AppHost.Sdk/13.3.3"`.

**Anchor 2 — generated metadata is PUBLIC and flows to referencing assemblies.** Confirmed generated file `src/Hexalith.Memories.AppHost/obj/Debug/net10.0/Aspire/references/Hexalith_Memories_Server.ProjectMetadata.g.cs`:
```csharp
namespace Projects;
public class Hexalith_Memories_Server : global::Aspire.Hosting.IProjectMetadata
{
    public string ProjectPath => """…/src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj""";
    public bool SuppressBuild => true;
}
```
Because these are `public` types in `namespace Projects` compiled into the **AppHost assembly**, any project that references the AppHost output assembly can name them. (A matching `Hexalith_Memories_Mcp.ProjectMetadata.g.cs` exists.)

**Anchor 3 — EventStore wiring signatures (no redis param):**
- `AddServerEventStoreIntegration` — `src/Hexalith.Memories.Server/EventStoreIntegration/ServerEventStoreIntegrationExtensions.cs:24`, `internal static IServiceCollection AddServerEventStoreIntegration(this IServiceCollection services, IConfiguration configuration)`. Single overload, no redis param. Called at `src/Hexalith.Memories.Server/Program.cs:326`: `builder.Services.AddServerEventStoreIntegration(builder.Configuration);`.
- `AddMemoriesEventStoreIntegration` — `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs:33`, `public static IServiceCollection AddMemoriesEventStoreIntegration(this IServiceCollection services, IConfiguration configuration, Action<EventStoreIntegrationBuilder>? configure = null)`. Single overload, no redis param.
- `EventStoreIntegrationBuilder` — `src/Hexalith.Memories.EventStore/EventStoreIntegrationBuilder.cs`, `public sealed class`, 8 fluent `Add*` adapter-replacement methods. No redis param.
- `AddHexalithEventStore` — **does not exist in this repo**; only in the submodule `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs` (2 overloads, neither takes a `redis` parameter; Redis is wired implicitly via DAPR state-store/pub-sub components). This is the evidence that the Parties-reported "redis-parameter drift" was a **stale submodule pin**, not a current API.

**Anchor 4 — project identity (no explicit overrides → names default to the csproj base name):**

| Project | csproj | Assembly name | Root namespace | PackageId | Aspire symbol |
| :------ | :----- | :------------ | :------------- | :-------- | :------------ |
| Server | `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` (`Sdk="Microsoft.NET.Sdk.Web"`, `IsPackable=false`) | `Hexalith.Memories.Server` (default) | `Hexalith.Memories.Server` (default) | — (not packed) | `Projects.Hexalith_Memories_Server` |
| Mcp | `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` (`Sdk="Microsoft.NET.Sdk.Web"`, `IsPackable=true`) | `Hexalith.Memories.Mcp` (default) | `Hexalith.Memories.Mcp` (default) | `Hexalith.Memories.Mcp` (explicit) | `Projects.Hexalith_Memories_Mcp` |

Code confirms the namespaces in practice, e.g. `namespace Hexalith.Memories.Server.Activities.Tenants;` and `namespace Hexalith.Memories.Mcp;`. Neither csproj sets `<AssemblyName>` or `<RootNamespace>`, so the contract is "these defaults must not change." Document the effective values, not the (absent) explicit tags.

### Why IntegrationTests, not a new test project (AC1 placement)

- `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` already references the AppHost (`<ProjectReference Include="..\..\src\Hexalith.Memories.AppHost\…"><IsAspireProjectResource>false</IsAspireProjectResource></ProjectReference>`), so the public `Projects.Hexalith_Memories_Server` / `Projects.Hexalith_Memories_Mcp` types resolve there with **zero new wiring**. It already uses `Projects.Hexalith_Memories_AppHost` in `Fixtures/AppHostComponentFileOrderingTests.cs:29` and `Fixtures/AspireIngestionPipelineFixture.cs:570`. It already pulls `xunit.v3`, `Shouldly`, and `Aspire.Hosting.Testing`, and inherits the global `using Xunit;`.
- A brand-new dedicated test project would have to add the same AppHost reference and packaging anyway, plus a `.slnx` entry and `Directory.Packages.props` churn — more surface for no benefit.
- **Critical distinction:** placing the test in IntegrationTests does **not** make it an integration/Docker test. xUnit only provisions a fixture for tests that consume it. A standalone class with no Testcontainers fixture and no `DistributedApplicationTestingBuilder` runs in the default lane with no Docker. Mirror the *style* of `AppHostComponentFileOrderingTests` (header, namespace, Shouldly) but **not** its `[Fact(Skip=...)]` + `DistributedApplicationTestingBuilder` body — that test is skipped precisely because it needs Docker; yours must not.

### Recommended test shape (illustrative — adapt names to conventions)

```csharp
[Fact]
public void AppHost_Server_And_Mcp_Project_Symbols_Resolve_AtCompileTime()
{
    // Referencing these generated types is itself the compile-time guard: the
    // IntegrationTests assembly fails to build if either symbol stops resolving.
    Aspire.Hosting.IProjectMetadata server = new Projects.Hexalith_Memories_Server();
    Aspire.Hosting.IProjectMetadata mcp = new Projects.Hexalith_Memories_Mcp();

    server.ProjectPath.ShouldNotBeNullOrWhiteSpace();
    server.ProjectPath.ShouldEndWith("Hexalith.Memories.Server.csproj");
    mcp.ProjectPath.ShouldNotBeNullOrWhiteSpace();
    mcp.ProjectPath.ShouldEndWith("Hexalith.Memories.Mcp.csproj");
}
```
No async, no Docker, no sidecar. (`Aspire.Hosting` is already imported by neighbouring files; add the `using` only if your file needs it.)

### Documentation conventions to match (AC2/AC3)

- `docs/dev` files: optional leading `<!-- Review cadence: … Last reviewed: <date> -->` HTML comment, single `#` h1, intro paragraph, `##` sections, reference tables, fenced code blocks, cross-doc links `[file.md](./file.md)`, and story/ADR identifiers for traceability. See `docs/dev/experimental-apis.md` (table-driven stability surface — closest existing analogue) and `docs/dev/cli-output-formats.md` (ADR-7.2-001 breaking-change policy wording you can mirror: "adding a new optional field is non-breaking; renaming/removing/changing semantics is breaking").
- There is **no** existing public-surface/naming stability doc and **no** PublicAPI analyzer in the Memories projects — `public-surface-stability.md` is a new file. Keep it lean and consumer-facing.

### Commit & release discipline

- This story changes **no public capability** → it is **not** a `feat`. Use `test:` for the guard test and `docs:` for the docs (or a single `test:`/`chore:` commit if combined). Per project-context: "Do not label refactors as features"; `feat` triggers a minor release.
- Epic 18 release-timing note: **Story 18.4 is the only semantic-release-sensitive story.** 18.1 has no release impact. Do not bump versions or edit `tools/release-packages.json`.

### Running tests in this sandbox

`dotnet test` fails in this environment with `SocketException (13)` (VSTest TCP-listener limitation). Run the xUnit v3 test assembly directly instead — build, then `DiffEngine_Disabled=true dotnet exec <path-to-Hexalith.Memories.IntegrationTests.dll>` with an xUnit v3 filter for the new test. Set `DiffEngine_Disabled=true` to stop any snapshot tooling from launching a diff tool. (Recorded as the Epic 17 in-process test workaround; see also the project-level memory on running .NET tests.)

### Guardrails / do-NOT list

- Do **not** call `DistributedApplicationTestingBuilder` or use Testcontainers in the new test (would require Docker → fails AC1).
- Do **not** modify the `Hexalith.EventStore` submodule (root-level submodule policy; do not init nested submodules). `AddHexalithEventStore` is theirs, not ours.
- Do **not** add `<AssemblyName>`/`<RootNamespace>` tags or rename projects — the story documents the existing defaults, it does not change them.
- Do **not** add a PublicAPI analyzer or `PublicAPI.*.txt` files — out of scope; the contract here is host project/assembly/namespace names, not member-level API.
- Do **not** add package versions to any `.csproj` (central package management via `Directory.Packages.props`).
- Respect `.editorconfig`: 4-space C#, 2-space for XML/MD structure, CRLF, UTF-8, final newline. Keep the ITANEO MIT header on the new `.cs` file.

### Project Structure Notes

- Source/test layout is `src/` + `tests/` (confirmed). New test file: `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostProjectResolutionTests.cs`. New/edited docs: `docs/dev/public-surface-stability.md` (new), `docs/dev/eventstore-integration.md` (edit — add wiring/drift note).
- No `.slnx` change needed (IntegrationTests is already registered). No `Directory.Packages.props` change needed (all required packages already pinned: `Aspire.Hosting.Testing` 13.4.6, `xunit.v3` 3.2.2, `Shouldly` 4.3.0).
- Aligns with the architecture project layout (architecture.md §Project layout) and Decision D22 (`.slnx`, file-scoped namespaces, warnings-as-errors).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 18.1] — story statement, 3 ACs, Parties-side follow-up, Epic 18 Preflight list, release-timing note.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-27-parties-consumer-integration-contract-hardening.md#MEM-1] — MEM-1 gap analysis: symbols resolve at `AppHost/Program.cs:151`/`:226`; wiring has no redis param; drift = stale submodule pin; residual gap = guard test + name-stability doc.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#MEM-1] — carried-forward entry, re-open trigger ("clean clone … fails to build the full `.slnx`, or the project symbols stop resolving").
- [Source: src/Hexalith.Memories.AppHost/Program.cs:150-151, 225-226] — `AddProject<Projects.Hexalith_Memories_Server>` / `…_Mcp`.
- [Source: src/Hexalith.Memories.AppHost/obj/.../Aspire/references/Hexalith_Memories_Server.ProjectMetadata.g.cs] — generated `public class Projects.Hexalith_Memories_Server : IProjectMetadata`.
- [Source: src/Hexalith.Memories.Server/EventStoreIntegration/ServerEventStoreIntegrationExtensions.cs:24] and [src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs:33] — wiring signatures (no redis param).
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs:71,132] — `AddHexalithEventStore` lives only in the submodule, no redis param.
- [Source: tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj] — AppHost reference (`IsAspireProjectResource=false`), `Aspire.Hosting.Testing`, `xunit.v3`, `Shouldly`.
- [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs:1-30] — header/namespace/Shouldly style; `[Fact(Skip=...)]` Docker pattern to **avoid** copying.
- [Source: tests/Directory.Build.props] — global `using Xunit;`, `IsPackable=false`.
- [Source: docs/dev/experimental-apis.md], [docs/dev/cli-output-formats.md] — stability/breaking-change doc conventions.
- [Source: _bmad-output/project-context.md] — central package management, warnings-as-errors, CRLF, conventional commits, submodule policy, xUnit v3 + Shouldly, additive-contract preference.
- [Source: _bmad-output/planning-artifacts/architecture.md#Decision D9, #Project layout] — concrete-class policy (relevant to sibling Story 18.7), `src/`+`tests/` layout, `.slnx`-only.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- Build (no-Docker lane): `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` → **Build succeeded, 0 Warning(s), 0 Error(s)** under `TreatWarningsAsErrors=true`. The build succeeding is itself the AC1 compile-time guard: the new test names `Projects.Hexalith_Memories_Server` / `Projects.Hexalith_Memories_Mcp`, so the assembly would fail to compile if either symbol stopped resolving.
- Focused test (sandbox workaround — `dotnet test` fails here with SocketException 13): `DiffEngine_Disabled=true dotnet exec <IntegrationTests>.dll -class Hexalith.Memories.IntegrationTests.Fixtures.AppHostProjectResolutionTests` → **Total: 1, Failed: 0, Skipped: 0** (0.066s). Ran in the default lane with no container provisioning.
- Discovery count: IntegrationTests assembly discovers **239** test methods (was 236) → delta **+3**: 1 from `AppHostProjectResolutionTests` (dev-story) and 2 from `PublicSurfaceStabilityTests` (added later in the `bmad-qa-generate-e2e-tests` automation phase — gaps G1/G2 below). The interim dev-story snapshot was 237 (+1) before the QA-automation phase landed the two assembly-identity guards.
- Senior review re-verification (2026-06-24): `dotnet build tests/Hexalith.Memories.IntegrationTests/...csproj` → **Build succeeded, 0 Warning(s), 0 Error(s)** under `TreatWarningsAsErrors`. `DiffEngine_Disabled=true dotnet exec <IntegrationTests>.dll -class …AppHostProjectResolutionTests -class …PublicSurfaceStabilityTests` → **Total: 3, Failed: 0, Skipped: 0** (0.071s). `-list methods` count = **239**. AC2 signatures re-confirmed in source (`internal static … AddServerEventStoreIntegration(IServiceCollection, IConfiguration)` at `ServerEventStoreIntegrationExtensions.cs:24`; `public static … AddMemoriesEventStoreIntegration(IServiceCollection, IConfiguration, Action<EventStoreIntegrationBuilder>?)` at `EventStoreIntegrationServiceCollectionExtensions.cs:33`); no `AddHexalithEventStore` defined in `src/`.

### Completion Notes List

- **AC1 (guard test):** Added `AppHostProjectResolutionTests.cs` as a plain `[Fact]` in the default (no-Docker) lane — no `DistributedApplicationTestingBuilder`, no fixture, no `IAsyncLifetime`. It instantiates both generated `IProjectMetadata` symbols and asserts each `ProjectPath` is non-whitespace and ends with the expected csproj (Shouldly). The compile-time reference is the primary guard; the runtime assertions are the secondary check. **QA-automation phase** additionally strengthened this `[Fact]` to assert the generated Aspire symbol *shape* (`GetType().Namespace == "Projects"`, `GetType().Name == "Hexalith_Memories_Server"`/`…_Mcp`) so a rename cannot quietly emit a different-but-still-resolving symbol.
- **AC2 (wiring surface + drift finding):** Re-verified both signatures against current `main` (`AddServerEventStoreIntegration(IServiceCollection, IConfiguration)` no-redis; `AddMemoriesEventStoreIntegration(IServiceCollection, IConfiguration, Action<EventStoreIntegrationBuilder>?)` no-redis) and recorded them plus the "stale submodule pin, not a current API" finding in `docs/dev/eventstore-integration.md` §1.2.1. Confirmed `AddHexalithEventStore` exists only in the `Hexalith.EventStore` submodule (overloads at lines 71/132) and takes no redis parameter; submodule left untouched.
- **AC3 (stability contract):** Created `docs/dev/public-surface-stability.md` recording project/assembly/namespace/PackageId + Aspire symbol for Server and Mcp, the breaking-change rule (incl. semantic-release `BREAKING CHANGE:` for the published Mcp package), the dots→underscores Aspire symbol-derivation rule, and a reference to the guard test as automated enforcement. Cross-linked `experimental-apis.md` and `eventstore-integration.md`.
- **QA-automation phase (`bmad-qa-generate-e2e-tests`):** Audited the single AC1 guard against the full documented contract and auto-applied three reflectable, no-Docker gaps: **G1** Server assembly name + root namespace and **G2** Mcp assembly name + root namespace → new sibling test `PublicSurfaceStabilityTests.cs` (2 `[Fact]`s reflecting over stable public anchor types `IGraphQueryBuilder` / `MemoriesMcpAuthenticationOptions`); **G3** Aspire symbol shape → folded into the strengthened AC1 test above. The **Mcp `PackageId`** half is a pack-time NuGet property, not reflectable from a built assembly, so it stays review-enforced (the `public-surface-stability.md` "Automated enforcement" section was synced to state this precisely). Net effect: the AC3 contract's assembly-name/root-namespace half is now **test-enforced**, not documentation-only — 5 of 6 contract items automated. Recorded in `tests/test-summary.md`.
- **Scope discipline:** Test + docs only. No production `src/` code changed, no submodule files touched, no `.slnx` / `Directory.Packages.props` / `release-packages.json` edits, no PublicAPI analyzer added. Per release discipline this is `test:` + `docs:`, never `feat:`.

### File List

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostProjectResolutionTests.cs` — **added** (AC1 guard test; QA-automation phase strengthened it with Aspire symbol-shape assertions).
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs` — **added** (QA-automation phase: runtime guard for the AC3 assembly-name / root-namespace half; 2 `[Fact]`s, default no-Docker lane).
- `docs/dev/public-surface-stability.md` — **added** (AC3 stability contract; "Automated enforcement" section documents both guard tests and the review-enforced PackageId half).
- `docs/dev/eventstore-integration.md` — **modified** (AC2 §1.2.1 wiring-surface/drift note).
- `_bmad-output/implementation-artifacts/tests/test-summary.md` — **modified** (QA-automation phase: Story 18.1 test-automation summary, gaps G1–G3, coverage map).
- `_bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md` — **modified** (story tracking: baseline_commit, checkboxes, Dev Agent Record, Change Log, Status, Senior Developer Review).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — **modified** (status tracking: 18-1 → in-progress → review → done; epic-18 → in-progress).

## Change Log

| Date | Phase | Change | Test count |
| :--- | :---- | :----- | :--------- |
| 2026-06-24 | create-story | Story drafted from Epic 18 / MEM-1; all preflight anchors re-verified against current `main` (symbols resolve, no-redis wiring confirmed, project identity captured). | +0 (planned: +1 guard test) |
| 2026-06-24 | dev-story | Implemented all 4 tasks: added compile-time AppHost project-resolution guard test (AC1), recorded stable EventStore wiring surface + stale-pin drift finding in `eventstore-integration.md` §1.2.1 (AC2), published `public-surface-stability.md` contract (AC3). Build green under `TreatWarningsAsErrors`; focused test passes in no-Docker lane. No production/submodule changes. | +1 (IntegrationTests 236 → 237) |
| 2026-06-24 | qa-automation | `bmad-qa-generate-e2e-tests` auto-applied 3 reflectable contract gaps: added `PublicSurfaceStabilityTests.cs` (G1/G2 — Server & Mcp assembly name + root namespace) and strengthened `AppHostProjectResolutionTests` with Aspire symbol-shape assertions (G3). Synced `public-surface-stability.md` "Automated enforcement" + `tests/test-summary.md`. AC3 assembly/namespace half now test-enforced; Mcp `PackageId` remains review-enforced (not reflectable). | +2 (IntegrationTests 237 → 239) |
| 2026-06-24 | senior-review | Adversarial review: implementation sound (build 0/0 under warnings-as-errors; 3 new tests pass; AC2 signatures re-confirmed in source; docs accurate + cross-linked). Fixed 3 MEDIUM story-record drift findings — File List omitted `PublicSurfaceStabilityTests.cs` + `test-summary.md`; Debug Log/Change Log stated stale 237/+1 vs actual 239/+3; Completion Notes understated the now-test-enforced AC3 half. 0 CRITICAL → Status → done. | +0 (239, verified via `-list methods`) |

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-06-24 · **Outcome:** ✅ Approve (after auto-fix of story-record drift)

### Scope verified

Adversarial review of the test + docs surface only (`_bmad/` and `_bmad-output/` excluded from code review per policy). Files reviewed: `AppHostProjectResolutionTests.cs`, `PublicSurfaceStabilityTests.cs`, `docs/dev/public-surface-stability.md`, `docs/dev/eventstore-integration.md` (diff).

### Acceptance criteria

- **AC1 (buildable resolution guard, no Docker)** — ✅ Met. `AppHostProjectResolutionTests` is a plain `[Fact]`, no `DistributedApplicationTestingBuilder`, no fixture; instantiates both `Projects.*` `IProjectMetadata` symbols and asserts `ProjectPath` + symbol shape. Build green is itself the compile-time guard. Runs in 0.071s with no container provisioning.
- **AC2 (EventStore wiring surface + drift finding)** — ✅ Met. Signatures in `eventstore-integration.md` §1.2.1 match source exactly: `AddServerEventStoreIntegration(IServiceCollection, IConfiguration)` (`…Extensions.cs:24`, `internal`), `AddMemoriesEventStoreIntegration(IServiceCollection, IConfiguration, Action<EventStoreIntegrationBuilder>?)` (`…Extensions.cs:33`, `public`). No `AddHexalithEventStore` defined in `src/`; the redis-param "drift" is correctly recorded as a stale submodule pin.
- **AC3 (public-surface stability contract)** — ✅ Met and exceeded. `public-surface-stability.md` records project/assembly/namespace/PackageId + Aspire symbol for Server and Mcp, the breaking-change rule (incl. semantic-release `BREAKING CHANGE:` for Mcp), the dots→underscores derivation, and references both guard tests. The assembly-name/root-namespace half is now test-enforced (was documentation-only).
- **Implementation constraints** — ✅ Default no-Docker lane, plain `[Fact]`s, `TreatWarningsAsErrors` green, no submodule/production/`.slnx`/`Directory.Packages.props` changes.

### Findings (all auto-fixed)

| # | Sev | Finding | Resolution |
| :- | :-- | :------ | :--------- |
| 1 | MEDIUM | `PublicSurfaceStabilityTests.cs` (and `tests/test-summary.md`) present in git but absent from the story File List — incomplete change documentation. | Added both to File List + Completion Notes. |
| 2 | MEDIUM | Debug Log / Change Log claimed 237 methods / delta +1; actual is **239 / +3** after the QA-automation phase added 2 tests. | Corrected Debug Log count and added qa-automation + senior-review Change Log rows. |
| 3 | MEDIUM | Completion Notes / AC3 record stated only the symbol-resolution half was test-enforced; in reality the assembly-name/root-namespace half is now enforced by `PublicSurfaceStabilityTests`. | Updated Completion Notes (AC1 strengthened, new QA bullet) to reflect actual coverage. |

No HIGH or CRITICAL findings: every `[x]` task maps to verified evidence on disk, and all three ACs are genuinely implemented. The discrepancies were stale tracking metadata, not false completion claims.

### Verification commands (re-run during review)

- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` → Build succeeded, 0 Warning(s), 0 Error(s).
- `DiffEngine_Disabled=true dotnet exec <IntegrationTests>.dll -class …AppHostProjectResolutionTests -class …PublicSurfaceStabilityTests` → Total: 3, Failed: 0, Skipped: 0.
- `dotnet exec <IntegrationTests>.dll -list methods` → 239 test methods.
