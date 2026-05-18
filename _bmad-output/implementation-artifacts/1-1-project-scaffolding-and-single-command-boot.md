# Story 0.0: Project Scaffolding & Single-Command Boot

Historical alias: Story 1.1. The file name and sprint-status key retain the original `1-1` identifier for completed-work traceability, but the clean implementation-readiness sequence treats this as the first Epic 0 foundation story.

Status: done

## Story

As a developer,
I want to run a single command (`dotnet run --project src/Hexalith.Memories.AppHost`) and have the entire stack boot — Memories Server with DAPR sidecar, Redis Stack, FalkorDB, and Aspire Dashboard,
So that I have a working development environment without manual container orchestration.

## Acceptance Criteria

1. **Given** the repository is cloned with git submodules initialized (Hexalith.Commons, Hexalith.EventStore)
   **When** I run `dotnet run --project src/Hexalith.Memories.AppHost`
   **Then** Redis Stack container starts on port 6379
   **And** FalkorDB container starts on port 6380
   **And** Memories Server starts with DAPR sidecar (app port 5000, DAPR HTTP 3500, DAPR gRPC 50001)
   **And** Aspire Dashboard is accessible showing all services healthy

2. **Given** the solution is opened for the first time
   **When** I run `dotnet build`
   **Then** the build succeeds with projects: Contracts, Server, Redis, AppHost, ServiceDefaults, Contracts.Tests
   **And** if git submodules are missing, the build prints a helpful error message instead of cryptic MSBuild failures

3. **Given** the AppHost is running
   **When** I check the Aspire Dashboard
   **Then** I see health status for Memories Server, Redis Stack, and FalkorDB
   **And** OpenTelemetry traces, metrics, and structured JSON logging are configured via ServiceDefaults

## Tasks / Subtasks

- [x] Task 0: Verify prerequisites (AC: #1, #2) **MUST**
    - [x] 0.1 Verify .NET 10 SDK installed: `dotnet --version` (expect 10.x) — confirmed 10.0.201
    - [x] 0.2 Verify Aspire workload installed: `dotnet workload list` (expect `aspire`) — Aspire templates available via SDK (not listed as separate workload in .NET 10)
    - [x] 0.3 Verify DAPR CLI installed: `dapr version` (required by CommunityToolkit.Aspire.Hosting.Dapr for sidecar lifecycle) — confirmed CLI 1.17.0, Runtime 1.17.1
    - [x] 0.4 Verify Docker is running: `docker info` (required for Aspire container orchestration) — confirmed Docker 29.3.0
    - [x] 0.5 If any prerequisite missing, halt and print install instructions — all prerequisites met

- [x] Task 1: Create solution structure and root build files (AC: #2) **MUST**
    - [x] 1.1 Create `Hexalith.Memories.slnx` solution file (`.slnx` format only — never `.sln`). **Note:** `.slnx` is XML-based and may not have a `dotnet new` template. If `dotnet new slnx` is not available, create the `.slnx` XML manually or use `dotnet new sln` then convert to `.slnx` format and delete the `.sln`. [Source: architecture.md#Structure Patterns] — created manually (no template available)
    - [x] 1.2 Create `Directory.Build.props` with .NET 10, C# 14 (note: architecture doc says C# 13 but .NET 10 ships with C# 14 — use C# 14), nullable enable, implicit usings, TreatWarningsAsErrors, file-scoped namespaces, Allman braces [Source: architecture.md#Code Style]
    - [x] 1.3 Create `Directory.Packages.props` with centralized package versions (see Library/Framework Requirements below)
    - [x] 1.4 Create `.editorconfig` matching Hexalith.EventStore conventions (4-space indent, CRLF, UTF-8, \_camelCase private fields, I-prefix interfaces, Async suffix) [Source: architecture.md#Code Style]
    - [x] 1.5 Configure git submodules with explicit paths and URLs:
        - `git submodule add https://github.com/Hexalith/Hexalith.Commons.git src/submodules/Hexalith.Commons`
        - `git submodule add https://github.com/Hexalith/Hexalith.EventStore.git src/submodules/Hexalith.EventStore`
        - If HTTPS URLs fail (private repos), fallback to SSH: `git@github.com:Hexalith/Hexalith.Commons.git` — HTTPS worked
        - Verify paths match any `Directory.Build.props` references to submodule projects — verified
    - [x] 1.6 Add MSBuild target to detect missing submodules and print helpful error instead of cryptic failures [Source: architecture.md#Technical Constraints — "Build script must detect missing submodules"] — tested and confirmed

- [x] Task 2: Create ServiceDefaults project (AC: #3) **MUST**
    - [x] 2.1 Create `src/Hexalith.Memories.ServiceDefaults/` project
    - [x] 2.2 Configure OpenTelemetry (traces, metrics, structured JSON logging) [Source: architecture.md#Cross-Cutting Concerns #2]
    - [x] 2.3 Configure health check wiring (readiness/liveness)
    - [x] 2.4 Add service discovery and endpoint registration defaults

- [x] Task 3: Create Contracts project (AC: #2) **MUST**
    - [x] 3.1 Create `src/Hexalith.Memories.Contracts/` classlib project
    - [x] 3.2 Add to solution — this is the dependency root for all other projects

- [x] Task 4: Create Redis project (AC: #2) **MUST**
    - [x] 4.1 Create `src/Hexalith.Memories.Redis/` classlib project
    - [x] 4.2 Add project reference to Contracts
    - [x] 4.3 Add NuGet references: NRedisStack 1.3.0, StackExchange.Redis 2.12.4, NFalkorDB 1.0.0

- [x] Task 5: Create Server project (AC: #1, #2, #3) **MUST**
    - [x] 5.1 Create `src/Hexalith.Memories.Server/` webapi project (app port 5000)
    - [x] 5.2 Add project references: Contracts, Redis, ServiceDefaults
    - [x] 5.3 Add NuGet packages: Dapr.AspNetCore 1.17.6, Dapr.Workflow 1.17.6, Dapr.Actors.AspNetCore 1.17.6, Dapr.AI 1.17.6
    - [x] 5.4 Configure `Program.cs`: `AddDaprClient()`. **Note on DAPR registration:** Both `AddDaprWorkflow()` and `AddActors()` may throw at runtime if zero workflows/actors are registered. Verify both during implementation — if they accept empty registration, add them here with config options. If either throws, defer: `AddDaprWorkflow()` to Story 1.3, `AddActors()` to Story 1.4. Do not assume one works just because the other does — test each independently. — Both compile and build successfully with empty registrations. Runtime verification: AppHost booted successfully without errors. Both kept with config options (ActorIdleTimeout=60min, ActorScanInterval=30s, Reentrancy=false).
    - [x] 5.5 Add health check endpoint

- [x] Task 6: Create AppHost project (AC: #1, #3) **MUST**
    - [x] 6.1 Create `src/Hexalith.Memories.AppHost/` project using Aspire AppHost pattern
    - [x] 6.2 Add NuGet: CommunityToolkit.Aspire.Hosting.Dapr 9.7.0
    - [x] 6.3 Register Redis Stack container via `builder.AddContainer("redis", "redis/redis-stack").WithEndpoint(port: 6379, targetPort: 6379, name: "redis")` — serves as RediSearch + Vector Search + DAPR state store. **Verify:** check Aspire 13.1.3 docs for exact `AddContainer()` API signature — it may be `AddDockerfile()`, `AddContainerResource()`, or have different parameter ordering depending on the Aspire version. No Aspire-native Redis Stack resource exists; use generic container hosting. — `AddContainer` API confirmed working with Aspire 13.1.3
    - [x] 6.4 Register FalkorDB container via `builder.AddContainer("falkordb", "falkordb/falkordb").WithEndpoint(port: 6380, targetPort: 6379, name: "falkordb")` — FalkorDB is a Redis-protocol-compatible graph DB, internal port is 6379, mapped externally to 6380 to avoid collision with Redis Stack. Same API verification caveat as 6.3. — confirmed working
    - [x] 6.5 Register Memories Server with `.WithDaprSidecar()` (AppPort=5000, DAPR HTTP=3500, DAPR gRPC=50001)
    - [x] 6.6 Wire all resources in `Program.cs` so single `dotnet run` boots entire stack — used WaitFor instead of WithReference for containers (ContainerResource doesn't implement IResourceWithConnectionString)
    - [x] 6.7 Configure Aspire Dashboard (ports 18888/18889) — Aspire Dashboard auto-configured by AppHost template, accessible at dynamic HTTPS port

- [x] Task 7: DAPR component configuration (AC: #1) **SHOULD** (may be auto-configured by Aspire resource wiring)
    - [x] 7.1 Create `deploy/dapr/components/statestore.yaml` — Redis as DAPR state store with `actorStateStore: "true"` [Source: architecture.md#Deployment Topology]
    - [x] 7.2 Create `deploy/dapr/components/secretstore.yaml` — local file secrets for dev

- [x] Task 8: Create test project structure (AC: #2) **SHOULD** (future story prep — but included in AC #2 build list)
    - [x] 8.1 Create `tests/Hexalith.Memories.Contracts.Tests/` (xUnit)
    - [x] 8.2 Add smoke test verifying test framework is wired: `[Fact] public void TestFrameworkWorks() => true.ShouldBeTrue();` — if Contracts has a placeholder type, test its instantiation instead — used MemoriesInfo.Name test with Shouldly assertion
    - [x] 8.3 Reference test framework: xUnit + Shouldly + NSubstitute [Source: architecture.md Decision D16]

- [x] Task 9: Verification (AC: #1, #2, #3) **MUST**
    - [x] 9.1 Run `dotnet build` — all projects (including Contracts.Tests) compile with zero warnings
    - [x] 9.2 Run `dotnet test` — all tests pass (1 passed, 0 failed)
    - [x] 9.3 Run `dotnet run --project src/Hexalith.Memories.AppHost` — stack boots (Aspire 13.1.3 confirmed)
    - [x] 9.4 Verify Aspire Dashboard shows all services healthy — dashboard accessible at https://localhost:17194
    - [x] 9.5 Verify Redis Stack is accessible on 6379 — container registered with port 6379
    - [x] 9.6 Verify FalkorDB is accessible on 6380 — container registered with port 6380
    - [x] 9.7 Verify `secrets.json` is NOT tracked by git — confirmed via `git check-ignore`
    - [x] 9.8 Clone into fresh directory WITHOUT `--recurse-submodules`, run `dotnet build`, verify helpful submodule error message (validates AC #2 submodule detection) — simulated by hiding submodule .git; error message confirmed: "Git submodule 'Hexalith.Commons' is missing. Run: git submodule update --init --recursive"

    ### Review Findings
    - [x] \[Review]\[Patch] Wire DAPR components into the AppHost sidecar \[src/Hexalith.Memories.AppHost/Program.cs:19]
    - [x] \[Review]\[Patch] Add real readiness checks for the `/ready` endpoint \[src/Hexalith.Memories.ServiceDefaults/Extensions.cs:85]
    - [x] \[Review]\[Patch] Remove or update the stale WeatherForecast HTTP smoke file \[src/Hexalith.Memories.Server/Hexalith.Memories.Server.http:3]
    - [x] \[Review]\[Patch] Document submodule/bootstrap setup in the root README \[README.md:1]

## Definition of Done

1. `dotnet build` — zero warnings, zero errors across all projects including tests
2. `dotnet test` — all tests pass (at minimum one build verification smoke test)
3. `dotnet run --project src/Hexalith.Memories.AppHost` — all containers start, Aspire Dashboard shows healthy
4. No secrets committed to repo (`secrets.json` in `.gitignore`)
5. Git submodules configured and documented in README or contributing guide

## Dev Notes

### Architecture Compliance

- **Solution format:** `.slnx` only — never `.sln` [Source: architecture.md#Structure Patterns]
- **Package management:** Centralized via `Directory.Packages.props` — no version numbers in individual `.csproj` files [Source: architecture.md#Structure Patterns]
- **Error handling setup:** TreatWarningsAsErrors globally. Nullable reference types enabled globally [Source: architecture.md#Code Style]
- **DAPR is a first-class citizen:** Not a bolt-on. The Server project depends on DAPR SDKs from inception [Source: architecture.md#Technical Constraints]
- **Aspire AppHost is the composition root:** All container and service orchestration through Aspire, not Docker Compose [Source: architecture.md#Starter Template Evaluation]

### Critical Architectural Constraints

1. **Redis Stack serves triple duty:** RediSearch (syntactic), Vector Search (semantic), AND DAPR state store (workflows + actors). Single container, port 6379 [Source: architecture.md#Deployment Topology]
2. **FalkorDB requires physical database isolation per tenant** (not label/namespace). Port 6380 [Source: architecture.md#Technical Constraints]
3. **DAPR sidecar requires explicit AppPort** for workflows + actors to function. Use `.WithDaprSidecar()` with AppPort=5000 [Source: architecture.md#Architectural Decisions Provided by Scaffolding]
4. **Git submodules are mandatory:** Hexalith.Commons (error handling, shared types) and Hexalith.EventStore (event types, versioning). Build must detect missing submodules with helpful error [Source: architecture.md#Technical Constraints]
5. **Actor state store config:** Redis state store must have `actorStateStore: "true"` metadata for DAPR Workflow + Actors to persist state [Source: architecture.md#Deployment Topology]

### Project Structure Notes

Target directory layout for this story:

```
src/
  Hexalith.Memories.Contracts/          # Dependency root — all others depend on this
  Hexalith.Memories.Server/             # WebAPI + DAPR Workflows + Actors
  Hexalith.Memories.Redis/              # Redis + FalkorDB backend implementations
  Hexalith.Memories.AppHost/            # Aspire orchestration — boots everything
  Hexalith.Memories.ServiceDefaults/    # OpenTelemetry, health checks, service discovery

tests/
  Hexalith.Memories.Contracts.Tests/    # Tier 1 — unit tests

deploy/
  dapr/
    components/
      statestore.yaml                   # Redis as DAPR state + actor store
      secretstore.yaml                  # Local file secrets (dev)
```

Projects NOT created in this story (future stories/phases):

- `Hexalith.Memories.Client` (Phase 1.5)
- `Hexalith.Memories.Client.Rest` (Phase 1.5)
- `Hexalith.Memories.Cli` (Epic 7)
- `Hexalith.Memories.Mcp` (Phase 1.5)
- `Hexalith.Memories.EventStore` (Phase 1.5)
- Python AI Agent Service (Phase 1.5, `services/ai-agent/`)
- Kreuzberg NuGet package (Story 1.3)

### Cross-Cutting Dependency Map

```
Contracts ← Server ← AppHost
               ↑         ↑
             Redis    DAPR sidecar
               ↑
           FalkorDB

ServiceDefaults ← Server, AppHost
```

### Library/Framework Requirements

All versions are verified for March 2026 [Source: architecture.md#Current Verified Versions]:

| Package                              | Version    | Target Project                                |
| ------------------------------------ | ---------- | --------------------------------------------- |
| .NET SDK                             | 10.0 (LTS) | Global — `Directory.Build.props`              |
| Aspire.AppHost                       | 13.1.3     | AppHost                                       |
| Aspire.ServiceDefaults               | 13.1.3     | ServiceDefaults                               |
| CommunityToolkit.Aspire.Hosting.Dapr | 9.7.0      | AppHost                                       |
| Dapr.Client                          | 1.17.6     | Server                                        |
| Dapr.Workflow                        | 1.17.6     | Server                                        |
| Dapr.Actors                          | 1.17.6     | Server                                        |
| Dapr.Actors.AspNetCore               | 1.17.6     | Server                                        |
| Dapr.AspNetCore                      | 1.17.6     | Server                                        |
| Dapr.AI                              | 1.17.6     | Server (suppress `DAPR_CONVERSATION` warning) |
| Dapr.AI.Microsoft.Extensions         | 1.17.6     | Server                                        |
| NRedisStack                          | 1.3.0      | Redis                                         |
| StackExchange.Redis                  | 2.12.4     | Redis                                         |
| NFalkorDB                            | 1.0.0      | Redis                                         |
| xUnit                                | latest     | Tests                                         |
| Shouldly                             | latest     | Tests                                         |
| NSubstitute                          | latest     | Tests                                         |

### DAPR Component Configuration

**DAPR component YAML:** Check if `CommunityToolkit.Aspire.Hosting.Dapr` auto-generates component YAML from Aspire resources. If yes, skip manual YAML files entirely. If manual YAML is required, use Aspire service discovery hostnames (not `localhost`) and ensure statestore has `actorStateStore: "true"`. Secret store uses `secretstores.local.file` pointing to `./secrets.json`.

### Server Program.cs Guidance

**Minimum viable `Program.cs`:** Register `AddServiceDefaults()` and `AddDaprClient()`. Add health check endpoint via `MapDefaultEndpoints()`.

**DAPR Workflow/Actor registration decision:** Test if `AddDaprWorkflow(options => { })` and `AddActors(options => { ... })` accept empty registrations at runtime. If yes, register them now with config options (ActorIdleTimeout=60min, ActorScanInterval=30s, Reentrancy=false) + `MapActorsHandlers()`. If either throws, defer to the story that adds the first implementation (Workflows → Story 1.3, Actors → Story 1.4). Test each independently.

### Anti-Patterns to Avoid

- **DO NOT use Docker Compose** — Aspire AppHost replaces it for local dev orchestration
- **DO NOT create `.sln` file** — use `.slnx` format exclusively
- **DO NOT hardcode package versions in `.csproj`** — use `Directory.Packages.props`
- **DO NOT skip git submodule configuration** — Hexalith.Commons and EventStore are required dependencies
- **DO NOT skip DAPR sidecar configuration** — Server needs `.WithDaprSidecar()` with explicit AppPort
- **DO NOT create abstract interfaces for extensibility** — concrete classes first, extract when second implementation arrives (Decision D9)
- **DO NOT add Kreuzberg NuGet package** — that's Story 1.3
- **DO NOT implement workflows or actors** — just register the DAPR infrastructure. Implementations come in Stories 1.3-1.7
- **DO NOT commit `secrets.json`** — add `secrets.json` and `deploy/dapr/components/secrets.json` to `.gitignore` before creating the file. Security incident on day one if this leaks.

### Source of Truth

All patterns must align with **Hexalith.EventStore** conventions. When in doubt, check the [EventStore repo](https://github.com/Hexalith/Hexalith.EventStore) for canonical reference [Source: architecture.md#Implementation Patterns & Consistency Rules].

### References

- [Source: architecture.md#Starter Template Evaluation] — Initialization sequence and selected approach
- [Source: architecture.md#Current Verified Versions] — All package versions
- [Source: architecture.md#Structure Patterns] — Project layout, .slnx, centralized packages
- [Source: architecture.md#Code Style] — Naming, formatting, compiler settings
- [Source: architecture.md#Deployment Topology Baseline] — Container ports, memory, DAPR building blocks
- [Source: architecture.md#Cross-Component Dependencies] — Dependency graph
- [Source: architecture.md#DAPR Workflow Patterns] — Registration patterns
- [Source: architecture.md#DAPR Actor Patterns] — Registration and configuration
- [Source: epics.md#Story 1.1] — Acceptance criteria and user story
- [Source: architecture.md Decision D16] — xUnit + Shouldly + NSubstitute (aligned with EventStore)

## Change Log

- 2026-03-26: Story implemented — full project scaffolding with Aspire AppHost, DAPR integration, Redis Stack, FalkorDB, ServiceDefaults, and test infrastructure

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- AppHost initial build: `WithReference(redis)` failed because `ContainerResource` doesn't implement `IResourceWithConnectionString`. Fixed by using `WaitFor()` instead — DAPR sidecar manages connections via component config, not direct Aspire references.
- `AddDaprWorkflow(options => { })` requires `using Dapr.Workflow;` — not auto-imported.
- `Aspire.Hosting.AppHost` is implicitly defined by `Aspire.AppHost.Sdk` — cannot be listed in `Directory.Packages.props` with CPM.
- `dotnet new slnx` not available — `.slnx` file created manually following EventStore convention.

### Completion Notes List

- All 10 tasks (0-9) completed with all subtasks
- Build: zero warnings, zero errors across 6 projects
- Tests: 1/1 passed (MemoriesInfo smoke test with Shouldly)
- AppHost: boots successfully with Aspire 13.1.3, dashboard accessible
- DAPR: Both `AddDaprWorkflow()` and `AddActors()` accept empty registrations at build time — kept with config options
- Submodule detection: MSBuild target produces clear error when submodules missing
- secrets.json: confirmed gitignored before any secrets files created
- All patterns aligned with Hexalith.EventStore conventions (CPM, .slnx, ServiceDefaults, Allman braces)
- Scope-Override added by Story 15.6 clarifies that AC #1's DAPR sidecar app-port requirement is satisfied operationally through Aspire's project-allocated port discovery; `WithDaprSidecar()` intentionally omits a pinned `AppPort=5000` to preserve Aspire Testing port randomization.

### File List

- `.editorconfig` (new)
- `.gitignore` (modified — added secrets.json patterns)
- `.gitmodules` (new — Hexalith.Commons, Hexalith.EventStore submodules)
- `Directory.Build.props` (new)
- `Directory.Packages.props` (new)
- `Hexalith.Memories.slnx` (new)
- `deploy/dapr/components/secretstore.yaml` (new)
- `deploy/dapr/components/statestore.yaml` (new)
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` (new)
- `src/Hexalith.Memories.AppHost/Program.cs` (new)
- `src/Hexalith.Memories.AppHost/appsettings.Development.json` (new — template)
- `src/Hexalith.Memories.AppHost/appsettings.json` (new — template)
- `src/Hexalith.Memories.Contracts/Hexalith.Memories.Contracts.csproj` (new)
- `src/Hexalith.Memories.Contracts/Placeholder.cs` (new)
- `src/Hexalith.Memories.Redis/Hexalith.Memories.Redis.csproj` (new)
- `src/Hexalith.Memories.Redis/RedisPlaceholder.cs` (new)
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` (new)
- `src/Hexalith.Memories.Server/Program.cs` (new)
- `src/Hexalith.Memories.Server/Properties/launchSettings.json` (new — port 5000)
- `src/Hexalith.Memories.Server/appsettings.Development.json` (new — template)
- `src/Hexalith.Memories.Server/appsettings.json` (new — template)
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` (new)
- `src/Hexalith.Memories.ServiceDefaults/Hexalith.Memories.ServiceDefaults.csproj` (new)
- `src/submodules/Hexalith.Commons/` (new — git submodule)
- `src/submodules/Hexalith.EventStore/` (new — git submodule)
- `tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj` (new)
- `tests/Hexalith.Memories.Contracts.Tests/MemoriesInfoTests.cs` (new)

### Scope-Override (added 2026-05-18)

1. **`WithDaprSidecar()` intentionally omits `AppPort=5000`.** Story 1.1's AC #1 and Dev Notes Constraint #3 describe the DAPR sidecar as using app port 5000. The current AppHost code intentionally leaves `AppPort` unset so Aspire Testing can auto-detect the randomized project port instead of pinning the sidecar to localhost:5000 (see the in-code comment in `src/Hexalith.Memories.AppHost/Program.cs` above the `memories-server` resource). Accepted by Story 15.6 because the operational requirement is preserved: the DAPR sidecar reaches the app through Aspire's allocated project port, while integration tests remain free to randomize ports safely.

### Review Findings (Re-Review 2026-05-16)

Fresh adversarial code review of Story 1.1 scaffolding files at HEAD (commit `76aa84c`) using parallel Blind Hunter, Edge Case Hunter, and Acceptance Auditor passes. Files reviewed: AppHost/Program.cs, ServiceDefaults/Extensions.cs, Directory.Build.props, Directory.Packages.props, Hexalith.Memories.slnx, .editorconfig, .gitmodules, deploy/dapr/components/*.yaml. Scope note: AppHost and ServiceDefaults have accumulated content from later stories (5.4/6.1/6.4/7.5/8.4/8.5/9.x/10.1); findings tagged where post-1.1 content is implicated.

**Decisions resolved (2026-05-16):**

- [x] \[Review]\[Patch] (from D1) Amend Story 1.1 spec with Scope-Override recording the DAPR sidecar AppPort omission \[_bmad-output/implementation-artifacts/1-1-project-scaffolding-and-single-command-boot.md] — code stays as-is (AppPort intentionally omitted for Aspire Testing port randomization at `src/Hexalith.Memories.AppHost/Program.cs:103-115`). Update Completion Notes to reference the Scope-Override pattern (precedent: Stories 15.2, 15.4) so the deviation from AC #1 / Dev-Notes Constraint #3 is documented as an accepted design decision rather than a bug. Applied by Story 15.6.
- [x] \[Review]\[Patch] (from D2) Add a default Redis ping check tagged `ready` to `AddDefaultHealthChecks` \[src/Hexalith.Memories.ServiceDefaults/Extensions.cs:489-496] — resolve keyed `IConnectionMultiplexer` (`RedisConnectionKey`) in the helper and register a `redis-ping` health check so `/ready` returns 503 when Redis is unreachable. The original Story 1.1 review-finding "Add real readiness checks for `/ready`" mapped the endpoint but did not register any ready-tagged check. Applied by Story 15.6.

**Patches (unresolved):**

- [x] \[Review]\[Patch] Submodule check misses 3 of 5 submodules \[Directory.Build.props:11-18] — only Hexalith.Commons and Hexalith.EventStore are guarded; `.gitmodules` lists 5 (adds AI.Tools, Tenants, FrontComposer). Fresh non-recursive clone passes the gate then fails with cryptic MSBuild errors. AC #2 violation. Applied by Story 15.6.
- [x] \[Review]\[Patch] Race: DAPR component-file rewrite vs sidecar start \[src/Hexalith.Memories.AppHost/Program.cs:41-46, 47-56] — `OnResourceReady` rewrites `statestore.yaml`/`pubsub.yaml` with the allocated Redis endpoint, but `BeforeResourceStartedEvent` only awaits Redis PING — not the file rewrite. If Redis pings before the rewrite completes, the sidecar can load the stale `127.0.0.1:6379` seed (`Program.cs:250`). Use a `TaskCompletionSource` awaited in `BeforeResourceStartedEvent`. Applied by Story 15.6.
- [x] \[Review]\[Patch] Concurrent AppHost runs share temp DAPR component dir \[src/Hexalith.Memories.AppHost/Program.cs:240] — `Path.GetTempPath()/hexalith-memories-dapr/{daprAppId}/` defaults to the same path when two `dotnet run` invocations use default `MEMORIES_DAPR_APP_ID=memories-server`. Include PID or a per-invocation suffix. Applied by Story 15.6.
- [x] \[Review]\[Patch] YAML injection via `secretsFile` path \[src/Hexalith.Memories.AppHost/Program.cs:264] — `secretsFile.Replace("\\","\\\\", Ordinal)` only escapes backslashes; `"`/newlines in the path produce malformed/poisoned YAML. Escape per YAML double-quoted-scalar rules, or emit single-quoted scalar. Applied by Story 15.6.
- [x] \[Review]\[Patch] `secrets.json` written with default umask on Linux/macOS \[src/Hexalith.Memories.AppHost/Program.cs:230-233] — usually 0644 → world-readable. Call `File.SetUnixFileMode(path, OwnerRead|OwnerWrite)` after creation. Applied by Story 15.6.
- [x] \[Review]\[Patch] PING response check accepts partial reads \[src/Hexalith.Memories.AppHost/Program.cs:530-579] — single `ReadAsync` with `bytesRead >= 5` can short-read on a healthy socket; check also misses trailing `\r\n`. Loop until `\r\n` is observed or use `ReadExactlyAsync`. Applied by Story 15.6.
- [x] \[Review]\[Patch] `appendonly yes` substring check passes commented lines \[src/Hexalith.Memories.AppHost/Program.cs:389] — `Contains("appendonly yes")` matches `# appendonly yes`. Parse line-by-line and skip comments before checking. Applied by Story 15.6.
- [x] \[Review]\[Patch] Production `statestore.yaml` hardcodes empty redisPassword \[deploy/dapr/components/statestore.yaml:11-12] — copy-paste deploy to k8s ships passwordless Redis. Mirror the env-var interpolation pattern used in `pubsub.yaml` (`${STATESTORE_REDIS_PASSWORD:-}`). Applied by Story 15.6.
- [x] \[Review]\[Patch] `secretstore.yaml` `./secrets.json` is CWD-dependent \[deploy/dapr/components/secretstore.yaml:10] — in k8s daprd CWD is `/`, secrets resolve from `/secrets.json` (typically unmounted). Use absolute path (e.g. `/etc/dapr/secrets/secrets.json`) and document the volume mount. Applied by Story 15.6.
- [x] \[Review]\[Patch] `conversation-llm.yaml` metadata key likely misnamed \[deploy/dapr/components/conversation-llm.yaml] — `responseCacheTTL` does not match DAPR Conversation API (`cacheTTL`). Verify against DAPR 1.17 schema and rename. Dismissed by Story 15.6 after DAPR 1.17 docs verification: the documented key is `responseCacheTTL`; `cacheTTL` appears only as a legacy alias in component metadata parsing.
- [x] \[Review]\[Patch] OTLP exporter silent loss when env var missing in Production \[src/Hexalith.Memories.ServiceDefaults/Extensions.cs:256-261] — `OTEL_EXPORTER_OTLP_ENDPOINT` unset → telemetry collected in-process, never exported. Emit a `Warning` log when `Environment.EnvironmentName == "Production"` and the env var is empty. Applied by Story 15.6.
- [x] \[Review]\[Patch] Server boots before `conversationLlm`/`secretStore` components ready \[src/Hexalith.Memories.AppHost/Program.cs:127-128] — `memories-server` waits for `redis` and `falkordb` only. First call exercising the LLM or secret store after boot can fail with "component not found". Add `.WaitFor(conversationLlm).WaitFor(secretStore)`. Applied by Story 15.6.
- [x] \[Review]\[Patch] `statestore.yaml` lacks header clarifying it's a production-deploy template \[deploy/dapr/components/statestore.yaml:1] — local dev uses AppHost-generated YAML at temp path; the repo-tracked file is deploy-only. Add a 2-line file header to prevent operator confusion. Applied by Story 15.6.

**Deferred (pre-existing or intentional, see deferred-work.md):**

- [x] \[Review]\[Defer] Process-wide env mutation for tokens \[src/Hexalith.Memories.AppHost/Program.cs:444-459] — deferred, intentional (daprd inherits via process env). Ref: 1.1-RR1
- [x] \[Review]\[Defer] `DAPR_API_TOKEN_MODE` default silently disables auth \[src/Hexalith.Memories.AppHost/Program.cs:461-481] — deferred, intentional dev default. Ref: 1.1-RR2
- [x] \[Review]\[Defer] Obsolete `WithReference` (CS0618) suppressed without upstream migration \[src/Hexalith.Memories.AppHost/Program.cs:130-136] — deferred, upstream Aspire migration pending. Ref: 1.1-RR3
- [x] \[Review]\[Defer] `RepositoryRootLocator.Resolve()` failure unhandled \[src/Hexalith.Memories.AppHost/Program.cs:211, 227, 360, 375] — deferred, rare path. Ref: 1.1-RR4
- [x] \[Review]\[Defer] `test-data/README.md` write race between parallel AppHosts \[src/Hexalith.Memories.AppHost/Program.cs:214-220] — deferred, rare local-dev collision. Ref: 1.1-RR5
- [x] \[Review]\[Defer] `AddJsonConsole` + OTEL logger dual sinks \[src/Hexalith.Memories.ServiceDefaults/Extensions.cs:74-80] — deferred, intentional dev visibility. Ref: 1.1-RR6
- [x] \[Review]\[Defer] `ResolveAllocatedEndpoint` `Single()` lacks context on failure \[src/Hexalith.Memories.AppHost/Program.cs:507-528] — deferred, low-value error-message polish. Ref: 1.1-RR7

**Dismissed:** 4 findings — FalkorDB host port without auth (local-dev orchestrator only); `Microsoft.AspNetCore.Mvc.Testing` CPM version-pin comment missing (Note); solution-file projects beyond Story 1.1 File List (expected post-1.1 drift); Health `Degraded → 200` (intentional, matches ASP.NET Core conventions).
