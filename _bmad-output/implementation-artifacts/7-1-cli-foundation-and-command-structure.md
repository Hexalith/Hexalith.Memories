# Story 7.1: CLI Foundation & Command Structure

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** the first Epic 7 story — a .NET global tool named `memories` that installs with `dotnet tool install -g Hexalith.Memories.Cli`, advertises the full top-level command surface (`ingest`, `search`, `traverse`, `case`, `tenant`, `status`, `explore`, `handlers`, `quickstart`), resolves a server endpoint through a documented precedence chain, and talks to the existing Minimal-API REST surface in `Hexalith.Memories.Server` via a small REST client. Subsequent stories (7.2–7.5) fill in output formatting, explain display, error messages, quickstart flow, and telemetry — **do not implement those here**.

In practice this story adds four things to the repo:

**Why `Client.Rest` as a separate project rather than inlined HTTP in the CLI?** Two agents (John + Barry) pushed back on this during review. The kept decision rests on a concrete cost/benefit: the library is thin (~50 LOC + options + auth handler), the one-way dependency graph (`Cli → Client.Rest → Contracts`) matches Architecture Build Order #8, and `Contracts.V1.TenantSummary` is already the canonical response type — there is no duplication tax. The **keep-cost** is one extra csproj entry. The **collapse-cost** (if MCP or a second consumer arrives in Phase 1.5 and wants to reuse it) is: rename namespaces, rewrite tests, update references, re-NuGet-publish with a new package identity. Keep-cost < collapse-cost on a 3-week horizon. If MCP slips past that horizon, collapsing is a small refactor.

1. **Two new projects, packaged for NuGet:**
    - `src/Hexalith.Memories.Client.Rest/` — a thin, typed `MemoriesClient` (and interface) that wraps `HttpClient` calls to the Server endpoints this story exercises: tenant listing, case listing, and health/version probe. No controller-shaped DTOs — reuse `Hexalith.Memories.Contracts.V1` types. This is the project Epic 7/10/11 and Phase 2 consumers will extend; keep the surface **small and honest** for 7.1.
    - `src/Hexalith.Memories.Cli/` — console project with `<PackAsTool>true</PackAsTool>`, `<ToolCommandName>memories</ToolCommandName>`, wired top-level command groups that each print a brief description and advertise the full group list. Commands that require Server traffic (like `tenant list` and a connectivity probe) are implemented here; the rest of the groups are stubs that print "Not yet implemented — see Story 7.X" and exit non-zero. Do **not** pre-implement Story 7.2–7.5 behavior.

2. **A documented endpoint resolution chain — 4 tiers, not 7.** AC3 specifies the precedence explicitly: CLI flag → env vars (`HEXALITH_MEMORIES_*`) → user config file `~/.hexalith/memories.json` → built-in default. The original plan listed seven tiers including DAPR Secrets, .NET User Secrets, and a DAPR configuration component — those are **cut from 7.1** because no caller in Epics 7-11 exercises them and dead configuration tiers rot fast. Implement as an `IConfigurationSource` pipeline (one source per tier) so Phase 1.5 / Phase 2 can add new tiers additively without rewriting the resolver. The single returned triple is `(Uri endpoint, string? apiToken, string resolvedBy)`.

3. **Three environment connection modes tested:** `localhost` default (`http://127.0.0.1:5000` or whatever the running Aspire host exposes), docker service name (`http://memories-server:5000`) when run inside a container network, and an arbitrary ingress URL (`https://memories.example.com`). 7.1 demonstrates connectivity against the existing `/health` endpoint and a low-risk read endpoint (`GET /api/tenants` returning the tenant list). Do not introduce new Server endpoints.

4. **Auth plumbing — not auth enforcement.** The Server already has `Story 5.4 AC3` DAPR API token propagation behind `DAPR_API_TOKEN_MODE=enabled`. The CLI MUST support two cases without regressing the existing integration test fixture: (a) token-free local development (default — no `Authorization` or `dapr-api-token` header sent), and (b) token injection when `HEXALITH_MEMORIES_API_TOKEN` or CLI flag is set — in which case the CLI attaches the `dapr-api-token` header on sidecar-routed requests, and `Authorization: Bearer <token>` on ingress-routed requests. Do not invent a new cross-cutting `TenantAuthorizationMiddleware`; that is Phase 1.5 (D8).

**What does NOT ship:**

- output formatting polish (`--format json|table|yaml` — **Story 7.2**);
- `--explain` display (**Story 7.2**);
- actionable error messages with recovery suggestions or empty-state nudges (**Story 7.3**);
- `memories quickstart` guided flow (**Story 7.4**);
- search/access telemetry (**Story 7.5**);
- full implementations of `ingest`, `search`, `traverse`, `case`, `status`, `explore`, `handlers` (those are covered in 7.2+ and by existing Server endpoints — the CLI side will grow in 7.2–7.5 and Phase 1.5 MCP);
- new Server endpoints — the REST surface is already extensive and stable after Stories 1–6;
- `TenantAuthorizationMiddleware` (Phase 1.5, architecture D8);
- DAPR Secrets API, .NET User Secrets, and DAPR configuration component as endpoint-resolution tiers — **cut from 7.1**. The `IConfigurationSource` interface leaves the door open; add them in Phase 1.5 when a real caller needs them;
- a BOM-heavy or emoji-heavy UX layer — keep output plain text and ASCII-safe for 7.1 (color is fine, emojis are not — repo convention).

**Primary risks:**

1. **Over-scoping into 7.2–7.5.** The single biggest failure mode is a dev agent "helpfully" implementing format flags, rich error messages, or the quickstart wizard in this story. Those ACs don't belong here.
2. **Inventing a parallel DTO layer.** The existing `Hexalith.Memories.Contracts.V1` types (`TenantSummary`, `TenantInfo`, `Case`, `ErrorResponse`, `TenantConfiguration`, etc.) are the canonical contract — consume them. Do not create `CliTenantDto` or similar. Note: `TenantRegistryEntry` lives in `Server/Tenants/` and is server-internal — it is **not** a contract type.
3. **Picking the wrong CLI framework.** Architecture does **not** mandate Spectre.Console vs System.CommandLine. Story 7.1 picks one; 7.2+ inherits it. Default recommendation: **System.CommandLine (Microsoft)** for subcommand composition and global-tool ergonomics, **or** `Spectre.Console.Cli` if the dev agent prefers attribute-based registration. **Verify the current GA/beta status via Microsoft Docs MCP before pinning a version** (this is a gating decision for Tasks 2.3, 2.4, and 4). Record the choice as a one-line comment in the CLI csproj — no separate `cli-framework.md`. Do not mix both.
4. **Breaking the existing integration test fixture.** `AspireIngestionPipelineFixture` and friends boot the full topology. The CLI project must not introduce `AddDaprClient()` or other hosting bootstrap that conflicts. It is a **client**, not a host.
5. **Global-tool packaging drift.** `PackAsTool` requires `OutputType=Exe`, a tool manifest entry, correct `ToolCommandName`, and `IsPackable=true`. Miss any of these and `dotnet tool install -g` fails in a non-obvious way. AC validation must cover `dotnet pack` + local install flow.
6. **Ugly first-impression error on connection failure.** The CLI is a Gate 3 developer-experience surface. If `memories tenant list` prints a raw `HttpRequestException` + stack trace when the AppHost isn't running, that is the developer's first experience of the product. AC #11 adds a one-line bridge message so the interim between Story 7.1 landing and Story 7.3 (rich actionable errors) is not a disaster. **Do not delete the AC #11 bridge when Story 7.3 lands — that story owns _enriching_ the message with a recovery suggestion, not replacing it.**

## Story

As a developer,
I want to install a CLI tool and interact with all retrieval, ingestion, and management capabilities through a consistent command structure,
so that I have a single tool for all Memories operations across any environment.

## Acceptance Criteria

1. **The CLI ships as a .NET global tool named `memories`.**
   **Given** the repo has been built and packaged,
   **When** I run `dotnet pack src/Hexalith.Memories.Cli -c Release` and then `dotnet tool install -g --add-source ./artifacts Hexalith.Memories.Cli`,
   **Then** the `memories` command is available on `PATH`
   **And** `memories --version` prints the assembly informational version
   **And** the package has `PackAsTool=true`, `ToolCommandName=memories`, and `IsPackable=true`.

2. **Running `memories` with no args advertises the full top-level command surface.**
   **Given** the CLI is installed,
   **When** I run `memories`,
   **Then** the output lists the command groups: `ingest`, `search`, `traverse`, `case`, `tenant`, `status`, `explore`, `handlers`, `quickstart` (FR53)
   **And** each group shows a one-line description
   **And** the exit code is zero (help invocation is not an error).

3a. **Endpoint precedence order is exactly these four tiers, highest to lowest.**
**Given** the CLI reads endpoint configuration,
**When** multiple sources define an endpoint,
**Then** the resolver uses the first non-empty value from this list: 1. command-line flag (`--endpoint <url>`), 2. environment variable `HEXALITH_MEMORIES_ENDPOINT`, 3. user config file `$HOME/.hexalith/memories.json` (Windows: `%USERPROFILE%\.hexalith\memories.json`), 4. built-in default `http://127.0.0.1:5000`
**And** token resolution (`HEXALITH_MEMORIES_API_TOKEN` / `--token` / config file `apiToken`) follows the same precedence independently
**And** project-local `./.hexalith/memories.json` is **not** a tier in 7.1 (deferred — see Task 3.2).

3b. **New tiers extend — they do not rewrite.**
**Given** the resolver is implemented as a chain of `IConfigurationSource` implementations,
**When** Phase 1.5 or Phase 2 needs to add DAPR Secrets, .NET User Secrets, or a DAPR configuration component as a source,
**Then** the new tier is registered as an additional `IConfigurationSource` at the appropriate priority position
**And** no existing tier's code changes.

3c. **A diagnostic surface makes the resolution visible.**
**Given** the CLI is installed,
**When** I run `memories config show`,
**Then** it prints exactly the following format to stdout — one key per line, `key=value`, no ANSI, no trailing blank line:
`         endpoint=<resolved URI>
        resolvedBy=<source class short name>
        tokenConfigured=<true|false>
        `
**And** the token value is **never** printed, not even partially masked
**And** exit code is zero (diagnostic is not an error) (NFR23)
**And** a JSON variant of this output arrives with `--format json` in Story 7.2 — the key=value form is the 7.1 contract and must remain stable even after 7.2 ships (so scripts relying on it don't break).

4. **The CLI connects against three environment shapes and proves connectivity against the existing Server.**
   **Given** the Memories Server is running (via AppHost locally, as a docker container by service name, or via an ingress URL),
   **When** I run `memories tenant list --endpoint <url>`,
   **Then** the CLI performs `GET /api/tenants` and prints the tenant IDs and display names
   **And** it works against `http://127.0.0.1:5000` (local AppHost),
   **And** it works against `http://memories-server:5000` (in-network docker service name),
   **And** it works against an arbitrary HTTPS ingress URL
   **And** SSL certificate validation is **not** disabled by default (must be respected).

5. **Transport is HTTP to the Server's existing Minimal-API REST endpoints via `Hexalith.Memories.Client.Rest`.**
   **Given** the CLI needs server state,
   **When** it issues any request,
   **Then** it goes through a typed `MemoriesClient` in `src/Hexalith.Memories.Client.Rest/`
   **And** the client uses `HttpClient` (via `IHttpClientFactory`) against the published routes in `src/Hexalith.Memories.Server/Program.cs`
   **And** DTOs are reused from `Hexalith.Memories.Contracts.V1` — **no CLI-local duplicates**
   **And** `ErrorResponse` parsing is centralized (so 7.3 can reuse it for actionable messages).

6. **Authentication supports opt-in token injection without breaking token-free local dev.**
   **Given** Story 5.4's DAPR API token mode is the only auth layer in MVP,
   **When** no token is configured,
   **Then** the CLI sends no `dapr-api-token` or `Authorization` header
   **And** all existing integration tests still pass,
   **And** when `HEXALITH_MEMORIES_API_TOKEN` or `memories --token <value>` is provided,
   **Then** the CLI attaches `dapr-api-token: <value>` on sidecar-routed requests
   **And** `Authorization: Bearer <value>` on ingress-routed requests (detection is host-based or explicit flag).

7. **`--help` works at every level and lists at least one usage example on top-level groups.**
   **Given** the CLI is installed,
   **When** I run `memories --help`, `memories tenant --help`, `memories tenant list --help`,
   **Then** each prints a description, flags, and at least one example for groups covered by this story (at minimum: `memories tenant list`)
   **And** other groups print "Not yet implemented — tracked in Story 7.X"
   **And** NFR30 is **partially** satisfied in 7.1; full per-command `--help` example coverage with CI verification lands in **Story 7.4** (see Story 7.4 acceptance criteria for the audit).

8. **Package wiring preserves the repo's test strategy.**
   **Given** the solution includes the new projects,
   **When** I run `dotnet build Hexalith.Memories.slnx`,
   **Then** the build succeeds with `TreatWarningsAsErrors=true` (Directory.Build.props)
   **And** new unit tests use xUnit + Shouldly + NSubstitute (existing convention) in a **single** consolidated `Hexalith.Memories.Cli.Tests` project covering both `Client.Rest` and `Cli` logic
   **And** the CLI is exercised via in-process library calls (`MemoriesClient`, `ResolvedConfigPipeline`, `CliCommandExecutor`) — **no tests that spawn the `memories` process in CI unit suites**.

9. **CLI startup is cheap — no implicit DAPR or Aspire hosting boot.**
   **Given** the CLI is a plain .NET 10 console global tool,
   **When** it starts,
   **Then** it does not call `WebApplication.CreateBuilder()`, `AddDaprClient()` in a way that forces a sidecar connection at startup, or any activity that requires Aspire to be running
   **And** a cold `memories --version` returns in under 1 second on a warm machine (advisory; do not assert in CI).

10. **Adding these two projects to the solution does not regress existing packages.**
    **Given** the solution is `Hexalith.Memories.slnx`,
    **When** the CLI and Client.Rest projects are added,
    **Then** they appear under the `/src/` solution folder
    **And** `Hexalith.Memories.Contracts` remains a dependency of `Client.Rest` and `Cli`
    **And** `Hexalith.Memories.Server` does **not** reference `Cli` or `Client.Rest` (the dependency direction is one-way).

11. **Connection failures do not look broken — minimal UX bridge until Story 7.3 lands rich error messages.**
    **Given** the Memories Server is not reachable at the resolved endpoint,
    **When** any command that touches the Server runs (e.g., `memories tenant list`),
    **Then** the CLI prints a single-line message: `Cannot reach Memories Server at <endpoint>. Check that the service is running.`
    **And** exit code is **2** (plumbing error per the exit-code table in Implementation Contracts)
    **And** no raw `HttpRequestException` or stack trace is printed by default
    **And** `--verbose` (or equivalent existing flag) may still show the full exception for debugging
    **And** no recovery suggestion is added here — Story 7.3 owns the rich error surface (FR56). This AC exists **only** so that the interim between 7.1 and 7.3 does not ship with a stack-trace-on-first-run developer experience.
    **And** when Story 7.3 lands, it **replaces** the one-line message with its full actionable-error output — the bridge is **not** preserved alongside the richer surface (single owner of the connection-failure path). The story 7.3 author must delete or rewrite the bridge in the same PR.

## Tasks / Subtasks

- [x] Task 1: Scaffold `Hexalith.Memories.Client.Rest` (AC: #5, #8, #10)
    - [x] 1.1 Create `src/Hexalith.Memories.Client.Rest/Hexalith.Memories.Client.Rest.csproj` with:
        - [x] `Sdk="Microsoft.NET.Sdk"`,
        - [x] `<IsPackable>true</IsPackable>`,
        - [x] `<ProjectReference Include="..\Hexalith.Memories.Contracts\Hexalith.Memories.Contracts.csproj" />`,
        - [x] no Aspire / DAPR hosting packages,
        - [x] `Microsoft.Extensions.Http` and `Microsoft.Extensions.Logging.Abstractions` (prefer framework-level packages over extra transitive bloat).
    - [x] 1.2 Add an `MemoriesClient` concrete class (no interface — Architecture D9: extensibility points are concrete classes, not interfaces; only safety-critical interfaces like `IGraphQueryBuilder` earn an abstraction) that wraps `HttpClient`:
        - [x] `Task<IReadOnlyList<TenantSummary>> ListTenantsAsync(CancellationToken ct)` → `GET /api/tenants` returns `TenantSummary[]` from `Hexalith.Memories.Contracts.V1.TenantSummary` (confirmed at `src/Hexalith.Memories.Server/Program.cs:657-669` — `TenantRegistryEntry` is server-internal and must NOT be used here).
        - [x] `Task<IReadOnlyList<Case>> ListCasesAsync(string tenantId, CancellationToken ct)` → `GET /api/tenants/{tenantId}/cases` (used for smoke test).
        - [x] `Task<bool> ProbeHealthAsync(CancellationToken ct)` → `GET /health` — returns `true` iff HTTP 2xx. No typed payload needed in 7.1; if Story 8.1 later needs structured health details, introduce the type then.
    - [x] 1.3 Create a `MemoriesClientOptions` type with two fields only: `Uri Endpoint`, `string? ApiToken`. Timeout is configured once on the `HttpClient` inside `AddMemoriesClient` (default `TimeSpan.FromSeconds(30)`) — NOT exposed as a global CLI flag in 7.1.
    - [x] 1.4 Add a `MemoriesClientServiceCollectionExtensions.AddMemoriesClient(...)` extension that registers `IHttpClientFactory` with resilience if available, applies `Options`, attaches the auth delegating handler (defined in **Task 4**), and returns a typed `MemoriesClient`. Constructor signature for `MemoriesClient`: `(HttpClient httpClient, IOptions<MemoriesClientOptions> options, ILogger<MemoriesClient> logger)` — matches existing repo convention (e.g., `EmbeddingClient` at `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`).
    - [x] 1.5 Route error bodies through a single `ErrorResponseDecoder` so non-2xx responses surface as a typed `MemoriesRemoteException(ErrorResponse, int statusCode)` using the existing `ErrorResponse` from `Contracts.V1`. Do **not** define a new error format.

- [x] Task 2: Scaffold `Hexalith.Memories.Cli` as a global tool (AC: #1, #2, #9, #10)
    - [x] 2.1 Create `src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj`:
        - [x] `Sdk="Microsoft.NET.Sdk"`,
        - [x] `<OutputType>Exe</OutputType>`,
        - [x] `<PackAsTool>true</PackAsTool>`,
        - [x] `<ToolCommandName>memories</ToolCommandName>`,
        - [x] `<IsPackable>true</IsPackable>`,
        - [x] `<PackageId>Hexalith.Memories.Cli</PackageId>`,
        - [x] `ProjectReference` to `Hexalith.Memories.Client.Rest` and `Hexalith.Memories.Contracts`.
    - [x] 2.2 Pick the CLI framework using this **decision tree** — keep it to two branches:
        1. Run `microsoft_docs_search` for `System.CommandLine` to check current package status (GA vs prerelease) as of April 2026.
        2. **If GA** (stable, non-preview) → pick `System.CommandLine`. Root-level global options inherited by subcommands is its documented pattern; Task 2.4 maps cleanly.
        3. **If still prerelease** → pick `Spectre.Console.Cli` **unless** you verify it cleanly supports root-level global options inherited by subcommands (Task 2.4 depends on this). If Spectre requires per-command option declaration instead, document the workaround in ADR-7.1-008 **before** starting Task 2.4.
        4. Record the choice in `Directory.Packages.props` under central package management. Record the pinned version and the rationale in ADR-7.1-008 (update in-place). Do not create `docs/dev/cli-framework.md` — the csproj comment + ADR entry is sufficient.
    - [x] 2.3 Implement `Program.cs` root command and subcommand groups (empty handlers where not in scope):
        - [x] `ingest`, `search`, `traverse`, `case`, `tenant`, `status`, `explore`, `handlers`, `quickstart`
        - [x] only `tenant list` is fully wired (delegates to `MemoriesClient.ListTenantsAsync`)
        - [x] every other group returns exit code 2 with message: `"Not yet implemented — tracked in Story 7.X"`.
    - [x] 2.4 Wire exactly three global options on the root command so subcommands inherit them: `--endpoint <url>`, `--token <value>`, and `--verbose` (boolean flag, drives Task 10.4 output). Do **not** add `--timeout` — no 7.1 AC exercises it, the default 30s on `HttpClient` is sufficient, and extra options widen the scope for `--help` test assertions in Story 7.4.
    - [x] 2.5 Ensure no code path in `Program.cs` invokes `WebApplication.CreateBuilder()` or attaches a DAPR hosting sidecar at startup.
    - [x] 2.6 Default output is plain text with minimal ANSI (e.g., bold for headings, red for errors). Do **not** use emojis (repo convention). Full format polish (JSON/table) is 7.2.
    - [x] 2.7 **Every subcommand handler that touches the Memories Server** (in 7.1: `tenant list` + `config show --check` if implemented) is registered through `CliCommandExecutor` (Task 10). Do **not** wire commands directly to `MemoriesClient`. This is a registration-time guarantee: if a future story adds a new network-touching command, the wrapper protects it automatically. Reinforced by anti-pattern #18.
    - [x] 2.8 **Ctrl-C (SIGINT) handling:** wire the CLI framework's cancellation pipeline so `Console.CancelKeyPress` triggers a linked `CancellationTokenSource`. Every command handler signature accepts `CancellationToken ct`, passed to `CliCommandExecutor.ExecuteAsync`, which passes it to `HttpClient` calls. On cancellation: print `"Cancelled."` to stderr (no color), exit with code **130** (standard Unix SIGINT — distinct from 0/1/2). Do not print stack traces. Do not require `--verbose` for this path — it's the expected cancellation UX.

- [x] Task 3: Endpoint resolution chain — 4 tiers (AC: #3a, #3b, #3c, #6)
    - [x] 3.1 In `Hexalith.Memories.Cli/Configuration/`, define:
        - [x] `public sealed record ResolvedConfig(Uri Endpoint, string? ApiToken, string ResolvedBy)` — `ResolvedBy` is the source class's short name (e.g., `"FlagConfigurationSource"`) for the `memories config show` diagnostic (AC #3c).
        - [x] `public interface IConfigurationSource { bool TryResolve(out ResolvedConfig? config); }` — synchronous; no I/O should take longer than a file read. If a source cannot contribute (env unset, file missing, etc.), return `false` and set `config` to `null`.
        - [x] A `ResolvedConfigPipeline` that iterates registered sources in DI registration order and returns the first successful resolution. **Order is established by DI registration**, not by if/else branches inside a god-method.
    - [x] 3.2 Implement these sources, each as its own class, and register them in the priority order below:
        - [x] `FlagConfigurationSource` (reads `--endpoint` / `--token` parsed from the root command),
        - [x] `EnvironmentVariableConfigurationSource` (reads `HEXALITH_MEMORIES_ENDPOINT`, `HEXALITH_MEMORIES_API_TOKEN`),
        - [x] `FileConfigurationSource` — single probe at user config only: `$HOME/.hexalith/memories.json` (Windows `%USERPROFILE%\.hexalith\memories.json`). Project-local discovery is **deliberately cut**; add it back in a future story when a multi-contributor workflow needs per-repo endpoints. Simpler = fewer edge cases (walk-up boundary, CI runners without a git root, etc.).
        - [x] `DefaultConfigurationSource` (`http://127.0.0.1:5000`, no token).
    - [x] 3.3 **Cut from 7.1 scope:** DAPR Secrets, .NET User Secrets, DAPR configuration component. These were originally planned as tiers 5-7; they are removed because no Epic 7-11 caller needs them. The `IConfigurationSource` interface leaves the door open — add a new source in Phase 1.5 when a real consumer appears. **Do not** pre-implement them.
    - [x] 3.4 Expose a diagnostic subcommand `memories config show` that prints the resolved endpoint URI, the `resolvedBy` source class name, and `"tokenConfigured": true|false` — **never** the token value, not even partially redacted. Include a negative test (Task 6.5).
    - [x] 3.5 Config file schema (`memories.json`): `{ "endpoint": "https://...", "apiToken": "...", "timeoutSeconds": 30 }`. Document the schema in `docs/dev/cli-config.md` and reference it from `--help`. Include a Mermaid flowchart of the 4-tier resolution order in the same doc (Paige's recommendation).

- [x] Task 4: Auth forwarding (AC: #6)
    - [x] 4.1 Add a delegating handler `MemoriesAuthHandler` in `Hexalith.Memories.Client.Rest`.
    - [x] 4.2 If `Options.ApiToken` is null/empty → no headers added.
    - [x] 4.3 Else, detect "ingress-style" vs "sidecar-style" endpoint:
        - [x] Ingress-style: scheme is `https` OR host is not `127.0.0.1|localhost` → attach `Authorization: Bearer <token>`.
        - [x] Sidecar-style: localhost http or explicit `--sidecar` flag (optional, but keep the helper) → attach `dapr-api-token: <token>` instead.
    - [x] 4.4 Never attach both headers; never log the token value.
    - [x] 4.5 Document in a code comment on `MemoriesClientOptions.ApiToken` AND in `docs/dev/cli-config.md`: **"Prefer `HEXALITH_MEMORIES_API_TOKEN` environment variable over `--token` CLI flag — argv is visible in shell history, `/proc/<pid>/cmdline` on Linux, and process listings on Windows."** Do not remove the `--token` flag (operators sometimes need it for one-off commands), but steer users via docs.

- [x] Task 5: Unit tests — Client.Rest (AC: #5, #8)
    - [x] 5.1 Create a **single consolidated test project** `tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj` (xUnit + Shouldly + NSubstitute, following existing `.Server.Tests` conventions). This project tests both `Client.Rest` and `Cli` — organize by folder: `tests/Hexalith.Memories.Cli.Tests/ClientRest/` for Task 5 tests and `tests/Hexalith.Memories.Cli.Tests/Cli/` for Task 6/10 tests. Two csprojs would duplicate test runner infrastructure with no isolation benefit (both test against in-memory fakes; neither needs a live Server). Project references both `Hexalith.Memories.Client.Rest` and `Hexalith.Memories.Cli`.
    - [x] 5.2 Test `MemoriesClient.ListTenantsAsync`:
        - [x] 200 returns correctly deserialized `TenantSummary` list.
        - [x] 401/403 surfaces as `MemoriesRemoteException` with original status code.
        - [x] 500 with `ErrorResponse` body surfaces as `MemoriesRemoteException` with parsed `ErrorResponse`.
        - [x] Malformed JSON → surfaces as a client error (but not masked as success).
    - [x] 5.3 Test `MemoriesAuthHandler` using a single parameterized `[Theory]` with `[InlineData]` covering four rows (the docker-service-name case collapses into row 4 — same rule, no information gain):
        - [x] (token=null, endpoint=`https://ingress.example/`) → 0 auth headers.
        - [x] (token=null, endpoint=`http://127.0.0.1:5000/`) → 0 auth headers.
        - [x] (token="t", endpoint=`https://ingress.example/`) → exactly 1 header: `Authorization: Bearer t`.
        - [x] (token="t", endpoint=`http://127.0.0.1:5000/` OR non-HTTPS host like `http://memories-server:5000/`) → exactly 1 header: `dapr-api-token: t`.
        - [x] **Assert `both headers present == false` on every row** — that is the critical negative invariant.
    - [x] 5.4 Use `IHttpClientFactory` substitution following the existing pattern in `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` (already in repo) — do not invent a new mocking style.

- [x] Task 6: Unit tests — endpoint resolver (AC: #3)
    - [x] 6.1 Place CLI-resolver tests in the consolidated test project from Task 5.1 under `tests/Hexalith.Memories.Cli.Tests/Cli/` — do **not** create a second test project.
    - [x] 6.2 Test each precedence tier in isolation by injecting fakes/stubs (no real filesystem access in unit tests — use `IFileSystem` abstraction if needed, or a test-specific `IConfigFileProvider`). For `FileConfigurationSource`, cover at minimum these edge cases:
        - [x] file does not exist → source returns empty, no throw.
        - [x] file is 0-byte / empty → source returns empty, no throw.
        - [x] file contains malformed JSON → source throws typed `InvalidConfigurationException` with the file path embedded in the exception message.
        - [x] file contains valid JSON with unknown properties → source ignores unknowns (forward-compat with future schema additions).
        - [x] file contains valid JSON with `"endpoint"` as an empty string → treated as unset, fall-through to next tier (do NOT construct `new Uri("")`).
        - [x] Windows path with `~` literal (e.g., if env provides `~/...` unexpanded) → source must expand or reject cleanly; document which and test it.
    - [x] 6.3 Test flag-wins-over-env, env-wins-over-file, file-wins-over-default, and final fall-through to default. Cover the empty-string trap explicitly: `HEXALITH_MEMORIES_ENDPOINT=""` must be treated as unset (fall-through), **not** as an endpoint value (which would throw on `Uri.TryCreate`). Same rule for `HEXALITH_MEMORIES_API_TOKEN=""`.
    - [x] 6.4 Test that the resolver returns `DefaultConfigurationSource` when all prior tiers are empty / misconfigured. (DAPR-tier fall-through tests are obsolete — Task 3.3 cut those tiers. Do not re-add tests for code that doesn't exist.)
    - [x] 6.5 Token-redaction assertion is **full-output**, not field-scoped: assert that a token value chosen as an obviously-distinct sentinel (e.g., `"UNIQUE-TOKEN-SENTINEL-DO-NOT-LEAK"`) **does not appear anywhere** in the combined stdout + stderr captured during `memories config show`, `memories tenant list`, and `memories --help` runs. Use string containment, not regex. This protects against future telemetry/verbose-mode additions accidentally echoing the token through a different field. (Murat's broader invariant.)

- [x] Task 7: Integration test — one golden path against Aspire (AC: #4, #5)
    - [x] 7.1 In `tests/Hexalith.Memories.IntegrationTests/Cli/` (new folder), add `CliTenantListIntegrationTests.cs`.
    - [x] 7.2 Reuse `AspireIngestionPipelineFixture` to start the full topology, then:
        - [x] create a tenant via the existing `/api/tenants` endpoint,
        - [x] instantiate `MemoriesClient` against the fixture's `MemoriesClient.BaseAddress`,
        - [x] call `ListTenantsAsync`,
        - [x] assert the created tenant appears with the expected ID and display name.
    - [x] 7.3 Do NOT spawn the `memories` process in CI — that adds packaging concerns to the unit suite. Package-install validation lives in Task 8 and in local manual verification.
    - [x] 7.4 If the fixture pattern requires a new collection, declare it with `[CollectionDefinition(nameof(AspireIngestionPipelineFixture), DisableParallelization = true)]` consistent with existing integration tests.

- [x] Task 8: Packaging validation (AC: #1, #8, #10)
    - [x] 8.1 Add a dev-only script (`tools/verify-cli-pack.ps1` or equivalent bash) that runs:
        - [x] `dotnet pack src/Hexalith.Memories.Cli -c Release -o ./artifacts`,
        - [x] `dotnet tool install --global --add-source ./artifacts Hexalith.Memories.Cli`,
        - [x] `memories --version` (if this fails with "command not found," the script must check whether `~/.dotnet/tools` on Unix or `%USERPROFILE%\.dotnet\tools` on Windows is on `PATH` — print a clear remediation message pointing to `docs/dev/cli-config.md`'s PATH section before failing),
        - [x] `dotnet tool uninstall --global Hexalith.Memories.Cli`,
        - [x] report any failure with a clear log.
    - [x] 8.2 Add the two new projects to `Hexalith.Memories.slnx` under `/src/` (`Hexalith.Memories.Client.Rest`, `Hexalith.Memories.Cli`) and the **single** consolidated test project under `/tests/` (`Hexalith.Memories.Cli.Tests`).
    - [x] 8.3 If `Directory.Packages.props` needs new `PackageVersion` entries for the chosen CLI framework, add them under a clearly labeled "Cli" group and match the verified current version (use `mcp__plugin_microsoft-docs_microsoft-learn__microsoft_docs_search` to confirm). Do **not** downgrade any existing package versions.

- [x] Task 9: Minimal docs (supporting, AC: #7)
    - [x] 9.1 Add `docs/dev/cli-config.md` describing:
        - [x] config file schema (with a small JSON example),
        - [x] the 4-tier resolution chain (AC #3a) as a **Mermaid flowchart** (`flowchart TD`) — 4 boxes, clear arrows, end node showing `(endpoint, apiToken, resolvedBy)`,
        - [x] env var names (`HEXALITH_MEMORIES_*`),
        - [x] how to point the CLI at localhost / docker service / ingress (three concrete command examples),
        - [x] token handling and the no-redaction-ever rule from AC #3c,
        - [x] **PATH troubleshooting section**: on some locked-down corp Windows machines or minimal Linux containers, `~/.dotnet/tools` / `%USERPROFILE%\.dotnet\tools` is not automatically on `PATH` after `dotnet tool install -g`. Document the one-line remediation per shell (bash/zsh/PowerShell/cmd).
    - [x] 9.2 Record the CLI framework choice as a one-line comment at the top of `src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj` (e.g., `<!-- CLI framework: System.CommandLine <version> — chosen over Spectre.Console.Cli because <reason>. -->`). Do **not** create `docs/dev/cli-framework.md` — a 3-line csproj comment is sufficient ceremony for a solo-architect repo (Barry's point).
    - [x] 9.3 Update `README.md` with a one-paragraph "CLI (preview)" section pointing at `docs/dev/cli-config.md` and clarifying 7.1 scope ("foundation only — output formatting, rich errors, quickstart, and telemetry land in Stories 7.2-7.5").

- [x] Task 10: Connection-failure UX bridge (AC: #11)
    - [x] 10.1 Centralize the CLI's HTTP-call invocation path so every subcommand that touches `MemoriesClient` goes through a single wrapper (e.g., `CliCommandExecutor`). This keeps the error surface in one place rather than duplicated across nine command groups. The executor has an **outermost `catch (Exception ex)`** that maps any unhandled exception to exit code **2** (plumbing), prints a minimal one-liner, and never propagates to the .NET CLI default (which is exit 1 — reserved for Story 7.3's domain errors per the exit-code table). Rationale: in 7.1, if anything unexpected escapes the specific catches in Task 10.2, the safe-by-default landing zone is "plumbing error," not "domain error."
    - [x] 10.2 In that wrapper, catch these exceptions and convert each to a single-line output (distinct messages, same exit code 2):
        - [x] `HttpRequestException` + `SocketException` → `Cannot reach Memories Server at <endpoint>. Check that the service is running.`
        - [x] `TaskCanceledException` (timeout, not user-cancellation — detect via `ex.InnerException is TimeoutException` or `cts.IsCancellationRequested == false`) → `Request to Memories Server at <endpoint> timed out after 30s.`
        - [x] `AuthenticationException` (SSL/TLS cert failure) → `SSL certificate validation failed for <endpoint>. Check the certificate or the endpoint URL.`
        - [x] `UriFormatException` on resolved endpoint → `Configured endpoint '<value>' is not a valid URI. Check the --endpoint flag, HEXALITH_MEMORIES_ENDPOINT, or config file.`
        - [x] The Anti-pattern #17 guard (http + token + non-localhost) → print its specific message and exit 2 **before** attempting the HTTP call.
    - [x] 10.3 Exit code is non-zero (use `2` — distinct from `0=success`, `1=domain error`, reserving `2=network/plumbing`). Do NOT re-throw; do NOT print stack trace on the default path.
    - [x] 10.4 When `--verbose` (add this as a root global option — same scope as `--endpoint`, `--token`) is set, print the underlying exception type and message under the one-liner. No stack trace even in verbose — that is a 7.3/7.5 concern. **Before printing the exception message, scrub any configured token substring** via simple `.Replace(token, "<redacted>")` on the message string. This protects against tokens embedded in URLs (e.g., `https://user:token@host/...` surfaced via `UriFormatException`) or echoed by inner handler exceptions. Add a unit test that feeds a deliberately-formed exception whose message contains the token — assert it does not appear in output.
    - [x] 10.5 Do NOT add per-command recovery suggestions, empty-state nudges, or error code mapping. Those are Story 7.3 (FR56).
    - [x] 10.6 Add a unit test: given a stubbed `HttpMessageHandler` that throws `HttpRequestException`, invoke the wrapper, assert (a) stdout contains the bridge message exactly once, (b) stderr does not contain "at " (stack-trace marker), (c) exit code is 2.

## Dev Notes

### Current repo state that matters

- The Memories Server is **Minimal API**, not MVC controllers. Endpoints are declared in `src/Hexalith.Memories.Server/Program.cs` via `app.MapGet` / `app.MapPost` / `app.MapPut` / `app.MapDelete` / `app.MapPatch`. The CLI must target those exact routes. [Source: `src/Hexalith.Memories.Server/Program.cs`]
- Representative routes the CLI will lean on (subset exercised by 7.1):
    - `GET /api/tenants` → list tenants (used by AC4, AC7).
    - `GET /api/tenants/{tenantId}` → tenant detail.
    - `GET /api/tenants/{tenantId}/cases` → list cases.
    - `GET /health` → liveness/readiness (from `ServiceDefaults`, exposed in Development). Use it for `memories config show --check`.
- The contract types live in `src/Hexalith.Memories.Contracts/V1/`: `TenantSummary`, `TenantInfo`, `Case`, `CaseStatusDetail`, `ErrorResponse`, `TenantConfiguration`, etc. Reuse these — do not duplicate. `TenantRegistryEntry` is **server-internal** (`src/Hexalith.Memories.Server/Tenants/TenantRegistryEntry.cs`) and must not be referenced from `Client.Rest`. [Source: `src/Hexalith.Memories.Contracts/V1/`]
- Solution file is `Hexalith.Memories.slnx` (new solution format). Add projects by editing the XML directly (not `dotnet sln add`, which does not yet understand `.slnx` in all tooling paths — verify before relying on it). Current contents have `/src/` and `/tests/` folders. [Source: `Hexalith.Memories.slnx`]
- `Directory.Build.props` at the repo root enforces `TargetFramework=net10.0`, `LangVersion=14`, `Nullable=enable`, `ImplicitUsings=enable`, and `TreatWarningsAsErrors=true`. The CLI and Client.Rest projects inherit these automatically. No per-project overrides. [Source: `Directory.Build.props`]
- `Directory.Packages.props` uses central package management (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`). All new package references must go through it. [Source: `Directory.Packages.props`]
- Existing HTTP-client test pattern: `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` demonstrates the canonical approach — substitute `IHttpClientFactory`, inject a scripted `DelegatingHandler`, assert on requests and responses. Follow that pattern in `Hexalith.Memories.Client.Rest.Tests`. [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`]
- Auth is already handled in the Server by Story 5.4 behind `DAPR_API_TOKEN_MODE=enabled`. When the env is not enabled, requests go through with no token. The CLI must preserve that default. [Source: `src/Hexalith.Memories.AppHost/Program.cs` lines 10-22 and lines 86-101]
- `Hexalith.Memories.Client` is listed in the architecture as Phase 1.5 too (Build Order #7). Story 7.1 implements `Hexalith.Memories.Client.Rest` (the REST variant, Build Order #8) because that is what Gate 3 CLI needs. Do not create `Hexalith.Memories.Client` speculatively in this story. [Source: `_bmad-output/planning-artifacts/architecture.md`]
- **NFR31 (<30-minute onboarding) is not measured in Story 7.1.** The clock starts in **Story 7.4's** `memories quickstart` flow. 7.1 ships the substrate (installable tool, resolver, first working command) that 7.4's measurement assumes. If 7.1 scope creeps into polishing that doesn't move 7.4's timer, it is wasted effort. [Source: `prd.md` NFR31, Epic 7 overview]

### Architectural Decisions (locked in Story 7.1)

The following ADRs formalize Story 7.1's commitments. They exist to make later stories' choices defensible and to prevent re-litigating these in code review.

**ADR-7.1-001 — Separate `Hexalith.Memories.Client.Rest` project**

- **Decision:** Ship `Client.Rest` as its own csproj rather than inlining HTTP in the CLI.
- **Rationale:** Keep-cost (1 csproj + ~50 LOC) < collapse-cost (rename, rewrite tests, re-publish) on a 3-week horizon.
- **Future consumers clarified:** MCP Server (Phase 1.5 Story 10.1) does **NOT** consume `Client.Rest` — per architecture, MCP → Memories Server uses DAPR service invocation, not REST. The real future consumers are (a) external CLI users installing the global tool, (b) Phase 2 REST application clients, (c) the pending `Hexalith.Memories.Client` abstraction (Build Order #7) which this REST flavor supplies, (d) **`Hexalith.Memories.Benchmarks` (Story 2.7)** — a typed `MemoriesClient` is a cleaner way to drive benchmark queries than hand-rolled `HttpClient` calls. Treat "MCP reuse" as NOT a justification for keeping the project.
- **Reconsider at:** Start of Story 7.5 or Phase 1.5. If no second consumer outside the CLI is in sight (external .NET clients, Phase 2 REST apps), collapse into the CLI — it is the lowest-scoring "keep" decision in this story (13/20 on the keep-vs-cut matrix run during elicitation).

**ADR-7.1-002 — `MemoriesClient` is a concrete class; no `IMemoriesClient` interface**

- **Decision:** No interface abstraction.
- **Rationale:** Architecture D9 — safety-critical interfaces (e.g., `IGraphQueryBuilder`) earn an abstraction; extensibility points are concrete classes. Mocking happens at the `HttpClient` / `IHttpClientFactory` boundary, not the client class.
- **Reconsider at:** First second-implementation requirement (in-memory fake, alternate transport). Extract then.

**ADR-7.1-003 — 4-tier endpoint resolver via `IConfigurationSource`**

- **Decision:** Chain is `flag → env → user config file → default`. DAPR Secrets, .NET User Secrets, and DAPR configuration component tiers are deferred.
- **Rationale:** No Epic 7-11 caller exercises the deferred tiers; dead tiers accumulate bugs. The `IConfigurationSource` pattern makes them additively extensible without rewriting the resolver.
- **Reconsider at:** First production/K8s deployment (Phase 2) — operators will want DAPR Secrets.

**ADR-7.1-004 — User-only config file (`$HOME/.hexalith/memories.json`)**

- **Decision:** Single probe at user config. No project-local walk-up.
- **Rationale:** Walk-up boundary conditions (CI runners without a git root, drive-root escape, symlink loops) add maintenance tax without a concrete multi-contributor case in 7.1.
- **Reconsider at:** First multi-contributor workflow that needs per-repo endpoints.

**ADR-7.1-005 — Global options: `--endpoint`, `--token`, `--verbose`. No `--timeout`.**

- **Decision:** Three global flags. Timeout is fixed at 30s on `HttpClient`.
- **Rationale:** `--verbose` is required by AC #11's bridge-message expansion. `--timeout` has no AC; every extra global option widens the Story 7.4 `--help` assertion surface.
- **Reconsider at:** Story 7.2 when real search responses may exceed 30s.

**ADR-7.1-006 — Client consumes `TenantSummary` from `Contracts.V1`, not `TenantRegistryEntry`**

- **Decision:** `ListTenantsAsync` returns `IReadOnlyList<TenantSummary>`.
- **Rationale:** `TenantRegistryEntry` is server-internal (wraps `TenantInfo` + workflow instance ID). `TenantSummary` is the canonical contract. `GET /api/tenants` returns `TenantSummary[]` — confirmed at `src/Hexalith.Memories.Server/Program.cs:657-669`.
- **Reconsider at:** Never (this is the correct type; ADR exists to document a draft-revision correction).

**ADR-7.1-007 — Connection-failure bridge has a single owner**

- **Decision:** Story 7.1 ships the one-line bridge message (AC #11). Story 7.3 **replaces** it; they do not coexist.
- **Rationale:** Two error surfaces on the same failure path drift apart.
- **Reconsider at:** Never — the handoff is the decision.

**ADR-7.1-008 — CLI framework choice (pending)**

- **Status:** To be locked during Task 2.2 using the decision tree in that task.
- **Decision:** _Fill in at implementation time._ Options: `System.CommandLine` (Microsoft, verify GA status), `Spectre.Console.Cli` (attribute-based registration).
- **Gotcha to verify before committing:** Task 2.4 requires three global options (`--endpoint`, `--token`, `--verbose`) declared at the root command and **inherited** by every subcommand. `System.CommandLine` supports this via `Command.Add(new Option<T>(...)) { IsGlobal = true }`. `Spectre.Console.Cli` does not inherit root-command options the same way — it typically requires declaring them on a base `Settings` type that all command settings derive from. If `Spectre` is chosen, document the `Settings` hierarchy pattern **before** starting Task 2.4 or the dev agent will rewrite Task 2.4 mid-implementation.
- **Rationale section to complete:** Version verified via `mcp__plugin_microsoft-docs_microsoft-learn__microsoft_docs_search`, reason for rejecting the other option, one-line note in the CLI csproj referencing this ADR.
- **Reconsider at:** Never during 7.1-7.5; a framework swap inside an epic is a cross-cutting refactor that belongs to its own story.

### Implementation contracts (answers common dev-agent questions)

**Exit codes** (Story 7.1 defines four; others reserved for later stories):

| Code | Meaning                                                                                  | Owner                                             |
| ---- | ---------------------------------------------------------------------------------------- | ------------------------------------------------- |
| 0    | Success (includes `--help`, `--version`, successful commands)                            | 7.1                                               |
| 1    | Domain/business error (e.g., `CASE_NOT_FOUND`, `TENANT_MISMATCH`)                        | **Reserved for Story 7.3** — do not emit from 7.1 |
| 2    | Plumbing/config error (connection failure, bad URI, cert failure, token-over-http guard) | 7.1 (Task 10)                                     |
| 130  | User cancellation (Ctrl-C / SIGINT)                                                      | 7.1 (Task 2.8)                                    |

**Health probe timeout:** `ProbeHealthAsync` uses a **5-second** timeout on its `HttpRequestMessage` — not the client's default 30s. Health checks should be fast; a hung probe blocks `memories config show --check`. Implement via `request.Headers.Add` + linked CTS on the per-call basis, not by mutating `HttpClient.Timeout` (which is shared).

**DI pattern in the CLI project:** Compose a `ServiceCollection` in `Program.cs`, call `AddMemoriesClient(...)`, register `IConfigurationSource` implementations (Task 3.2) and `CliCommandExecutor` (Task 10), then `BuildServiceProvider()`. If Task 2.2 picks `System.CommandLine` + `Microsoft.Extensions.Hosting` integration (if published), use that — it wires DI to command handlers automatically. If Task 2.2 picks `Spectre.Console.Cli`, use its `ITypeRegistrar` bridge to the `ServiceCollection`. Either way, **only one DI container**, scoped to `Program.Main`.

**Endpoint resolution ownership:** `CliCommandExecutor` (Task 10.1) **owns** calling the `ResolvedConfigPipeline` once per invocation and passes the resolved `Uri endpoint` to the command handler. Handlers do **NOT** call the resolver directly. Executor signature:

```csharp
public Task<int> ExecuteAsync(
    Func<HttpClient, Uri endpoint, CancellationToken, Task<int>> handler,
    CancellationToken ct);
```

This guarantees: (a) error messages always have the endpoint to cite, (b) the http+token+non-localhost guard (anti-pattern #17) runs in one place, (c) future commands can't forget the wrapper.

**JSON serialization:** Client.Rest uses `System.Text.Json` defaults matching the server's Minimal API default serializer. No custom `JsonSerializerOptions` for 7.1. `Contracts.V1` types already carry necessary `[JsonConverter]` attributes (e.g., `CamelCaseStringEnumConverter.cs`).

**Missing `$HOME`/`%USERPROFILE%`:** `FileConfigurationSource` treats this as "no user config exists" — returns empty, no throw, no warning. Fall-through to default tier. Rare on dev machines, common on some CI runners; do not surface an error.

**CLI logging policy:** Use `ILogger<T>` sparingly — this is a CLI, not a server. Default: log `Warning` and `Error` to stderr (never stdout — stdout belongs to command output); log `Information` and `Debug` **only when `--verbose` is set**. Never log token values or full request bodies. Never log at any level that produces output during a successful default-path `tenant list` run (keeps pipe-friendly behavior intact). Stories 7.2-7.5 inherit this policy.

### Architecture guardrails

- **Capability alignment, not feature parity.** The CLI is the reference implementation — superset of capabilities. MCP, REST ingress, and DAPR service invocation are other interfaces with narrower purposes. [Source: `_bmad-output/planning-artifacts/architecture.md` Interface Philosophy]
- **MVP REST is CLI routing only.** Do not treat `Hexalith.Memories.Server` as a public API in this story — full REST API (pagination, facets) is Phase 2 (D5). [Source: `architecture.md` Architectural Decision D5]
- **Error format is `{code, message, suggestion}`** — simpler than the full Hexalith.Commons envelope (D6). The full envelope is Phase 1.5 work driven by MCP. The CLI in 7.3 uses the three-field format too; 7.1 just needs to not block that direction. [Source: `architecture.md` D6]
- **DAPR is a first-class citizen for the Server**, but **the CLI is not a DAPR app**. It's a plain console tool. Do not add `.WithDaprSidecar()`, `AddDaprClient()`, or any other hosting integration that expects a sidecar at CLI startup. The DAPR Secrets / configuration tiers in the endpoint resolver are **optional lookups**, not a runtime dependency. [Source: architecture DAPR as first-class citizen + D8 deferral of TenantAuthorizationMiddleware]
- **One-way dependency direction.** `Cli → Client.Rest → Contracts`. The Server depends on `Contracts` and `Redis`. Never introduce a `Server → Client.Rest` or `Server → Cli` reference. [Source: architecture Build Order]
- **Phase Compatibility Requirement.** The CLI must accommodate Phase 1.5 additions (MCP, EventStore) as additive, not transformative. Don't hard-code assumptions about which capabilities exist — group registration should be data-driven enough that Phase 1.5 stories can add new groups without restructuring the root command. [Source: architecture.md Phase Compatibility]

### Test strategy

- **Unit tests (Tier 1)** — All logic in `Client.Rest` and the endpoint resolver is exercised here. No real network, no real filesystem (use abstractions), no real DAPR. xUnit + Shouldly + NSubstitute, matching existing conventions.
- **Integration test (Tier 3)** — One golden-path test that:
    1. Boots the Aspire topology via `AspireIngestionPipelineFixture`,
    2. Creates a tenant through the existing Server endpoints,
    3. Uses `MemoriesClient` (the new library) against the fixture's HTTP endpoint,
    4. Asserts the tenant appears in `ListTenantsAsync`.
       Do **not** package+install+invoke `memories` as a subprocess in CI — that slows everything down and couples tests to `dotnet tool` behavior. Keep packaging validation in the dev-only script.
- **No test relies on real DAPR Secrets / DAPR Configuration.** Those tiers are cut from 7.1 scope (see Task 3.3) — if a PR reintroduces them, the review must reject it as scope creep.
- **Determinism** — token redaction (AC #3c / AC #6) is tested with a **full-output containment assertion** (Task 6.5), not a field-specific check. The assertion runs against the combined stdout + stderr of `memories config show`, `memories tenant list`, and `memories --help`. Extend this assertion list in 7.5 when telemetry adds new output paths.
- **Connection-failure UX (AC #11)** — separate unit test targeting whichever class wraps the top-level `MemoriesClient` call (most likely a command handler). Assert: given an `HttpRequestException` from a stubbed client, stdout shows exactly the one-line message, stderr does not print a stack trace, and exit code is non-zero.

### Previous story intelligence

Story 6.4 (the most recently created story, `6-4-pipeline-state-persistence-and-zero-data-loss.md`) set a precedent the dev agent must follow here:

- **Keep story scope narrow.** Story 6.4 explicitly calls out anti-patterns like "over-scoping into workflow-history purge/retention". The same discipline applies here — resist scope creep into 7.2-7.5.
- **Prefer reusing existing surfaces over creating new ones.** 6.4 reused Story 6.3's failed-unit endpoints. 7.1 reuses the existing Server endpoints and `Contracts.V1` types.
- **Integration tests live in `Hexalith.Memories.IntegrationTests` under per-feature folders.** 6.4 used `tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs`; 7.1 uses `tests/Hexalith.Memories.IntegrationTests/Cli/CliTenantListIntegrationTests.cs`.
- **Do not hardcode `DaprSidecarOptions.AppPort`.** This is a repo memory rule (`/memories/repo/aspire-dapr-port.md`) and it survives into this story because the integration test will boot the same AppHost.
- **Record the choice of libraries inline.** 6.4 documented Dapr Workflow retention tradeoffs in Dev Notes. 7.1 records the CLI framework choice as a one-line comment in the CLI csproj — no separate doc.
- **Xunit + Shouldly + NSubstitute remain the standard.** Do not introduce new test libraries.

### Git intelligence

Recent commit pattern (top of current branch, from `git log`):

- `369bdb3` — Add unit tests for ingestion activities and related components
- `d079974` — Implement per-tenant rate limiting and concurrency control
- `a4f32f8` — Add unit tests for ingestion activities and services
- `948b8a5` — feat: Add search endpoint degradation logging and response handling
- `30f86c2` — Add TenantEndpointHandlers for tenant configuration and listing endpoints

Pattern: **small focused additions, co-located tests, no broad refactors**. Story 7.1 should ship as: (a) two new project scaffolds, (b) their unit tests, (c) one integration test, (d) solution wiring, (e) minimal docs. Any change that touches more than that is a red flag.

### Latest platform notes (verify before pinning versions)

- .NET SDK is at **10.0 (LTS)** in `Directory.Build.props`. C# 14 is the language version.
- `System.CommandLine` has historically shipped as beta packages; verify the current GA/prerelease status and package identity using the Microsoft Docs MCP (`microsoft_docs_search "System.CommandLine"`) before pinning a version in `Directory.Packages.props`. Do not assume a version from training data. [External check required at implementation time]
- If `System.CommandLine` is still prerelease in April 2026 and that is unacceptable, `Spectre.Console.Cli` is a well-known alternative — but the choice must be documented.
- Global-tool packaging reference: `PackAsTool` requires `OutputType=Exe` and `IsPackable=true`; `ToolCommandName` sets the invocation name. [External: Microsoft .NET global tool authoring docs]
- DAPR Secrets SDK call (if used) should be short-timeout and failure-tolerant. `DaprClient.GetSecretAsync` without a running sidecar throws — wrap it. [External: Dapr SDK docs]

### Anti-patterns to avoid

**Top-5 that will most likely bite you** (read these even if you skim the rest):

1. **#1** — Don't implement `--format`, `--explain`, rich errors, or `memories quickstart` in 7.1. Those are Stories 7.2-7.5.
2. **#8** — Don't spawn the `memories` binary in CI tests. Use in-process library calls.
3. **#12** — Never log the token value. Task 6.5 asserts full-output containment — don't silently break it with verbose-mode additions.
4. **#17** — Refuse `http://` + token to a non-localhost host. Fail fast at resolver completion, before any HTTP call.
5. **#18** — Every network-touching handler routes through `CliCommandExecutor`. No direct handler-to-`MemoriesClient` wiring.

**Full list:**

1. **Do not** implement `--format json|table|yaml` in this story. That is Story 7.2.
2. **Do not** implement `--explain` display, per-axis score formatting, or metadata origin rendering. Story 7.2.
3. **Do not** implement actionable error messages ("Is the service running? Try: `dotnet run ...`"). Story 7.3.
4. **Do not** implement `memories quickstart` logic. Story 7.4.
5. **Do not** implement telemetry / structured audit logging. Story 7.5.
6. **Do not** create `Hexalith.Memories.Client` (the non-REST Client variant) in this story — only `Client.Rest`.
7. **Do not** add controller-shaped DTOs in `Client.Rest` — consume `Contracts.V1` types directly.
8. **Do not** spawn the `memories` binary in CI unit or integration tests. Packaging is validated by a dev-only script.
9. **Do not** re-introduce DAPR Secrets, .NET User Secrets, or DAPR configuration tiers in the resolver. They were consciously cut from 7.1. If a future story needs them, add them as new `IConfigurationSource` implementations — do not retrofit them here.
10. **Do not** hardcode `DaprSidecarOptions.AppPort` in any code path this story touches. (Repo memory rule.)
11. **Do not** disable SSL validation when talking to HTTPS ingress. Respect the OS trust store.
12. **Do not** log or print the API token value under any mode (including verbose/diagnostic). The redaction test (Task 6.5) is full-output, not field-scoped.
13. **Do not** change `TreatWarningsAsErrors` at the repo level. New code compiles warning-free. A **targeted `<NoWarn>` for specific analyzer IDs from a prerelease framework package** (e.g., `System.CommandLine` beta diagnostics like `SYSLIB*` or `NU1603`) is permitted **inside the CLI csproj only**, but each silenced ID must be accompanied by a one-line comment explaining why and under what condition it should be removed. Do not silence analyzer categories wholesale (e.g., do not `<NoWarn>CS</NoWarn>`).
14. **Do not** introduce emoji into CLI output strings. Repo convention is plain ASCII + minimal ANSI color.
15. **Do not** set `TargetFramework` or `LangVersion` per project — the root `Directory.Build.props` is authoritative.
16. **Do not** expand AC #11 beyond the one-line bridge message. Recovery suggestions, empty-state nudges, and rich error formatting are Story 7.3's scope. This AC exists only to prevent stack-trace-on-first-run until 7.3 lands.
17. **Do not** send a configured token over `http://` to a non-localhost host. On resolver completion, if `(endpoint.Scheme == "http")` AND `(host != "127.0.0.1" && host != "localhost" && host != "::1")` AND `(apiToken != null)`, fail fast with a clear error: `"Refusing to send API token over http:// to non-localhost host '<host>'. Use https:// or unset the token."` Exit code 2 (plumbing error). This protects against a plaintext token exfiltration via a maliciously-advertised ingress URL.
18. **Do not** wire subcommand handlers directly to `MemoriesClient`. All network-touching commands go through `CliCommandExecutor` (Task 10.1) — the executor owns endpoint resolution, exception mapping, and token scrubbing. Direct handler-to-client calls bypass AC #11, anti-pattern #17, and the token-redaction guarantee.

### Definition of Done

1. `src/Hexalith.Memories.Client.Rest/` exists with a concrete `MemoriesClient` (no interface — see Architecture D9), `MemoriesClientOptions`, `MemoriesAuthHandler`, `MemoriesRemoteException`, and a DI extension. Consumes `Contracts.V1` types — no duplicates.
2. `src/Hexalith.Memories.Cli/` exists, packages as `Hexalith.Memories.Cli` global tool with command name `memories`, and advertises all nine top-level command groups on the root help output.
3. `tenant list` works against the existing Server REST surface with the **4-tier** endpoint resolution chain fully implemented (flag → env → config file → default) via the `IConfigurationSource` pattern. No DAPR Secrets, User Secrets, or DAPR configuration tiers in 7.1.
4. Auth is opt-in via env/flag, attaches the correct header per transport shape (never both), and never regresses the token-free local-dev path.
5. Connection failures print the one-line bridge message (AC #11) — no raw stack traces on the default path.
6. All new projects are in `Hexalith.Memories.slnx`: two under `/src/` (`Client.Rest`, `Cli`) and **one** consolidated `Cli.Tests` under `/tests/`.
7. Unit tests cover `MemoriesClient`, `MemoriesAuthHandler` (parameterized four-row auth matrix), the endpoint resolver, the connection-failure UX wrapper (Task 10.6), and the full-output token-redaction assertion — xUnit + Shouldly + NSubstitute.
8. One integration test wires the new client against `AspireIngestionPipelineFixture` and proves live tenant listing.
9. A dev-only script validates `dotnet pack` + `dotnet tool install -g` + `memories --version` + uninstall.
10. `docs/dev/cli-config.md` exists and includes a Mermaid flowchart of the 4-tier resolution order; the CLI framework choice is recorded as a csproj comment (no separate `cli-framework.md`); `README.md` has a "CLI (preview)" note.
11. `dotnet build Hexalith.Memories.slnx` succeeds with `TreatWarningsAsErrors=true`. The fast non-integration suite stays green.

### References

- Epic 7 overview and Story 7.1 acceptance criteria: [Source: `_bmad-output/planning-artifacts/epics.md#Epic-7`], [Source: `_bmad-output/planning-artifacts/epics.md#Story-7.1`]
- FR53 (CLI for all capabilities), FR55 (output formats — 7.2), FR56 (actionable errors — 7.3), FR57 (discoverable actions — 7.3/7.4), NFR23 (configurable endpoint), NFR30 (help examples): [Source: `_bmad-output/planning-artifacts/prd.md`]
- Architecture D5 (MVP REST minimal), D6 (error format), D7 (capability alignment), D8 (TenantAuthorizationMiddleware deferred), Build Order #6 and #8, Interface Philosophy, Phase Compatibility: [Source: `_bmad-output/planning-artifacts/architecture.md`]
- Server endpoint catalogue: [Source: `src/Hexalith.Memories.Server/Program.cs`]
- Contract types: [Source: `src/Hexalith.Memories.Contracts/V1/`]
- Existing integration fixture (to reuse in Task 7): [Source: `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`]
- HTTP-client test style reference: [Source: `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`]
- Story 5.4 token plumbing (reference for auth direction): [Source: `src/Hexalith.Memories.AppHost/Program.cs` lines 10-22, 86-101]
- Solution file: [Source: `Hexalith.Memories.slnx`]
- Central package management: [Source: `Directory.Packages.props`]
- Repo build conventions: [Source: `Directory.Build.props`]
- Repo memory rule about AppPort under Aspire Testing: [Source: `/memories/repo/aspire-dapr-port.md`]
- Previous story precedent for scope discipline: [Source: `_bmad-output/implementation-artifacts/6-4-pipeline-state-persistence-and-zero-data-loss.md`]
- Official Microsoft global-tool authoring docs: verify via `mcp__plugin_microsoft-docs_microsoft-learn__microsoft_docs_search` at implementation time.
- Official `System.CommandLine` docs: verify current package status via `mcp__plugin_microsoft-docs_microsoft-learn__microsoft_docs_search` before pinning a version.

### Story Template for Stories 7.2-7.5

7.1 is the **foundation story** for Epic 7. Stories 7.2-7.5 are derivative — they extend the substrate this story defines. To avoid bloat, they should inherit from 7.1 rather than re-deriving.

**Inherit by reference:**

- All 8 ADRs in this story's "Architectural Decisions" section — do not re-state, cite by ID (e.g., "per ADR-7.1-005, no `--timeout` global option").
- The 18-item anti-pattern list — cite the specific numbers that apply, do not duplicate the text.
- The Implementation Contracts section (exit codes, logging policy, DI pattern, executor ownership, JSON serialization).
- The CLI framework choice (ADR-7.1-008 once locked) — do not re-litigate.

**Do differently for derivative stories:**

- Skip party-mode and ADR rounds of advanced elicitation — this is already done here.
- Run a **pre-mortem + planning** pass only (2 rounds instead of 6).
- Target **250-350 lines**, not 520. If the story grows beyond that, scope is too wide — split.
- Add new ADRs only when the derivative story makes a new locked decision (e.g., 7.2 will lock the `--format` output schema).

**Story-specific scope for 7.2-7.5:**

- **7.2 (Output formats & explain display):** `--format human|json|table`, `--explain` score breakdown rendering, metadata origin display (FR64).
- **7.3 (Actionable errors & discoverable actions):** rich error messages with recovery suggestions (FR56), empty-state nudges (FR57), error-code→suggestion mapping. **Replaces** 7.1's AC #11 bridge message.
- **7.4 (Quickstart & docs):** `memories quickstart` guided setup, README quickstart completes in <30min (NFR31 — the timer starts here). Full per-command `--help` examples (NFR30 full).
- **7.5 (Search & access telemetry):** structured JSON logging with OTel correlation IDs (NFR27), trace propagation (NFR28), custom metrics (NFR29), per-tenant audit events (FR67).

## Dev Agent Record

### Agent Model Used

claude-opus-4-6[1m]

### Debug Log References

- Story created from current repo state on 2026-04-16.
- Target story selected explicitly from user input `/bmad-create-story 7-1`.
- Epic 7 status transitioned from `backlog` → `in-progress` as this is the first story.

### Completion Notes List

- Story file created with repo-grounded implementation guidance and strict scope boundaries against Stories 7.2-7.5.
- Sprint status updated: `epic-7: backlog → in-progress`, `7-1-...: backlog → ready-for-dev`.
- **Implementation (2026-04-16, `claude-opus-4-6[1m]`):** all 10 Tasks shipped.
    - Scaffolded `src/Hexalith.Memories.Client.Rest/` with `MemoriesClient`, `MemoriesClientOptions`, `MemoriesAuthHandler`, `MemoriesRemoteException`, `ErrorResponseDecoder`, and the `AddMemoriesClient` DI extension. No interface per ADR-7.1-002; consumes `Contracts.V1.TenantSummary` per ADR-7.1-006.
    - Scaffolded `src/Hexalith.Memories.Cli/` as a `PackAsTool=true` global tool with command name `memories`. Framework choice locked at **System.CommandLine 2.0.0-beta5.25306.1** (per ADR-7.1-008; still prerelease, chosen because `Option.Recursive=true` gives root-level global options out of the box and async handlers receive `CancellationToken` natively for Ctrl-C). Rationale recorded as a csproj comment; no separate `docs/dev/cli-framework.md`. `NoWarn=NU5104;NU1603` added for the prerelease dependency warnings only.
    - Root help advertises the full AC #2 surface (`ingest`, `search`, `traverse`, `case`, `tenant`, `status`, `explore`, `handlers`, `quickstart`) plus `config` as a diagnostic group. Only `tenant list` and `config show` are wired; every other group is a `NotImplementedCommand` stub that prints `"Not yet implemented — tracked in Story 7.X"` and exits with code 2.
    - Endpoint resolver: `IConfigurationSource` pipeline with four sources registered in priority order (`FlagConfigurationSource` → `EnvironmentVariableConfigurationSource` → `FileConfigurationSource` → `DefaultConfigurationSource`). `ResolvedConfig` record returns `(Uri endpoint, string? apiToken, string resolvedBy)`. `FileConfigurationSource` reads user-scoped `~/.hexalith/memories.json` only per ADR-7.1-004 (no project-local walk-up). `InvalidConfigurationException` surfaces malformed-JSON / bad-URI cases with the file path embedded. Empty-string env vars are treated as unset (fall-through trap covered in tests).
    - Auth: `MemoriesAuthHandler` attaches `Authorization: Bearer {token}` on HTTPS and `dapr-api-token: {token}` on HTTP (which collapses the docker-service-name case per Task 5.3 row 4). Never both; never logs the token.
    - Anti-pattern #17 guard: `InsecureTokenTransportException.ShouldRefuse` blocks token-over-http to non-localhost at executor entry, before any HTTP call.
    - `CliCommandExecutor` owns every network-touching command (Task 2.7 / anti-pattern #18). Outermost `catch (Exception)` maps unhandled exceptions to exit code 2 (plumbing), not .NET's default exit 1 (reserved for Story 7.3 domain errors). Per-type catches: `HttpRequestException`/`SocketException` → bridge message; `TaskCanceledException` (timeout) → timeout message; `AuthenticationException` → TLS failure message; `UriFormatException` → URI-format message; user-cancellation (`OperationCanceledException`) → `"Cancelled."` + exit 130. Verbose mode scrubs the configured token substring from exception messages before printing.
    - `CliConsole` abstraction so tests can capture stdout/stderr. Default `CliConsole` wires to `Console.Out`/`Console.Error`; `TokenRedactionTests` swaps in `StringWriter` instances.
    - `Program.cs` is plain `Main` — no `WebApplication.CreateBuilder`, no DAPR sidecar at startup (AC #9). Ctrl-C linked to a `CancellationTokenSource` that flows into every handler via `parseResult.InvokeAsync(cts.Token)`.
    - Tests: single consolidated `tests/Hexalith.Memories.Cli.Tests/` project (per Revision 6), xUnit + Shouldly + NSubstitute, 51 tests passing. Covers: `MemoriesClient` (200/401/403/500/malformed-JSON cases), `MemoriesAuthHandler` parameterized theory including the critical "never both headers" negative invariant, endpoint-resolver tiers (flag-wins-over-env-over-file-over-default, token independence from endpoint, empty-string trap, config-file edge cases: missing/empty/malformed-JSON/unknown-props/empty-endpoint-string/invalid-URI), anti-pattern #17 guard matrix, `CliCommandExecutor` exception mapping with exit codes, verbose-mode token scrubbing (full-output containment against `UNIQUE-TOKEN-SENTINEL-DO-NOT-LEAK`).
    - Integration test: `tests/Hexalith.Memories.IntegrationTests/Cli/CliTenantListIntegrationTests.cs` reuses `AspireIngestionPipelineFixture`, provisions a tenant via the existing `POST /api/tenants`, then calls `MemoriesClient.ListTenantsAsync` against the fixture HTTP client and asserts the created tenant appears. No subprocess spawn (Task 7.3 / anti-pattern #8).
    - Solution wiring: `Hexalith.Memories.slnx` now lists the two new `src/` projects and the single new `tests/` project. Central package management updated in `Directory.Packages.props` with `Microsoft.Extensions.Http`, `.Options`, `.DependencyInjection`, `.Logging.Abstractions`, and `System.CommandLine`.
    - Packaging validated end-to-end: `dotnet pack` → `dotnet tool install -g` → `memories --version` (prints `1.0.0+<commit sha>`) → `memories config show` → `dotnet tool uninstall -g`. Dev-only scripts added at `tools/verify-cli-pack.ps1` and `.sh` with PATH troubleshooting remediation on failure.
    - Docs: `docs/dev/cli-config.md` includes the 4-tier resolution Mermaid flowchart, env var table, config schema, three environment-shape examples, and per-shell PATH troubleshooting. `README.md` now has a "CLI (preview)" section pointing at the doc.
    - Full non-integration test suite green: Cli.Tests 51 / Server.Tests 1025 / Contracts.Tests 288 — no regressions. Integration tests compile (Aspire fixture requires Docker + DAPR at test-run time and was not invoked in this session).
    - Sprint status updated: `7-1-cli-foundation-and-command-structure: ready-for-dev → review`.
- **Revision 2 (2026-04-16, post-party-mode review):** endpoint resolver simplified from 7 tiers to 4 (flag → env → config file → default) via `IConfigurationSource` pattern; DAPR Secrets / User Secrets / DAPR configuration tiers deferred to Phase 1.5. AC #3 split into 3a/3b/3c for scannability. AC #11 added for minimal connection-failure UX bridge until Story 7.3 lands rich errors. Auth tests consolidated into a parameterized five-row `[Theory]`. Token-redaction assertion broadened to full stdout+stderr containment. `docs/dev/cli-framework.md` removed (framework choice lives as a csproj comment). Mermaid flowchart added to `docs/dev/cli-config.md` spec.
- **Revision 3 (2026-04-16, post-advanced-elicitation pre-mortem/first-principles/Occam):** Dropped `IMemoriesClient` interface per Architecture D9 (extensibility points are concrete classes — only `MemoriesClient` the class ships). Dropped `--timeout` global option (no AC exercises it). Collapsed `HealthResult` record to `Task<bool> ProbeHealthAsync` (typed payload isn't needed until Story 8.1). Collapsed auth test matrix from 5 rows to 4 (docker-service-name case folds into the non-HTTPS rule). Cut config file discovery to **user-only** `~/.hexalith/memories.json`; deferred project-local walk-up until a real multi-contributor need appears. Pinned `GET /api/tenants` response shape to `TenantSummary[]` (from `Contracts.V1`) with explicit source-line citation (`Program.cs:657-669`) — prevents the "empty list because wrong DTO" pre-mortem failure. Added targeted `<NoWarn>` allowance for known framework-analyzer diagnostics in the CLI csproj. Added **Task 10** dedicated to AC #11 implementation (the bridge had no owning task) and its unit test (Task 10.6). AC #11 now explicitly says Story 7.3 **replaces** the bridge (single-owner contract). TL;DR now documents the `Client.Rest` keep-vs-collapse reasoning explicitly to settle the John/Barry challenge on merit, not authority.
- **Revision 4 (2026-04-16, post-advanced-elicitation ADR/security/FMA/matrix/hindsight):** Added a dedicated **Architectural Decisions** section with 8 ADRs (7.1-001 through 7.1-008) formalizing every committed design choice with rationale and reconsider-at conditions — makes later stories' choices defensible and prevents re-litigation. Added Task 4.5: document env-var-over-`--token` preference (argv exposure guidance). Added Anti-pattern #17: refuse `http://` + token to non-localhost host with a fail-fast error — closes a plaintext-exfiltration vector via malicious ingress URL. Task 10.4 extended with **token substring scrubbing in exception messages** before verbose print — closes the "token embedded in URL surfaces via `UriFormatException`" leak. Task 10.2 catch list expanded from 3 exceptions to 5 (`AuthenticationException`, `UriFormatException`, plus pre-emptive Anti-pattern #17 guard) with distinct messages per class. Task 6.2 extended with six config-file edge cases (missing, empty, malformed JSON, unknown props, empty endpoint string, Windows `~`). Task 6.3 extended to cover empty-string env var trap. Dev Notes now states NFR31's <30min timer starts in Story 7.4, not 7.1 — prevents scope drift into polish that doesn't move the gate. ADR-7.1-001 flagged as lowest-scoring keep (13/20 on the keep-vs-cut matrix) with explicit reconsider-at pointer at Story 7.5 / Phase 1.5 start.
- **Revision 5 (2026-04-16, post-advanced-elicitation planning/5-whys/what-if/active-recall/rubber-duck):** Fixed Task 1.4 cross-reference bug ("Task 3" → "Task 4" for auth handler). Pinned `MemoriesClient` constructor signature matching repo convention (`EmbeddingClient`-style). Defined `ResolvedConfig` record shape and `IConfigurationSource` interface contract in Task 3.1 — was a named-but-undefined type. Added **Task 2.7**: all network-touching handlers route through `CliCommandExecutor` at registration time (Planning method found the bypass risk). Added Task 2.2 **decision tree** for CLI framework selection (5-Whys root cause: story didn't give deterministic rule). Added a new **Implementation contracts** section in Dev Notes answering the top 6 dev-agent cold-context questions (exit codes table, health probe 5s timeout, DI pattern, executor endpoint-ownership, JSON serializer match, missing-home-dir handling). ADR-7.1-008 gained a gotcha note about Spectre's non-inherited-root-options pattern — prevents mid-Task-2.4 rewrite. ADR-7.1-001 corrected: MCP does NOT consume Client.Rest (per architecture, MCP uses DAPR service invocation, not REST) — true future consumers are external CLI users and Phase 2 REST apps, not MCP reuse. Added Anti-pattern #18 reinforcing Task 2.7 (no direct handler-to-client calls, executor always in the path).
- **Revision 6 (2026-04-16, post-advanced-elicitation CRG/SCAMPER/M-and-A/reasoning/random):** Pinned `memories config show` output format to key=value lines on stdout (AC #3c) — prevents format drift and locks a stable contract scripts can rely on before JSON output arrives in 7.2. **Consolidated test projects:** a single `Hexalith.Memories.Cli.Tests` covers both `Client.Rest` and `Cli` logic — dropped from 2 test projects to 1 (same CI behavior, less csproj churn). Added **Task 2.8 Ctrl-C handling:** SIGINT triggers linked CTS, all handlers accept `CancellationToken`, cancellation prints `"Cancelled."` to stderr and exits 130. Exit-code table extended with code 130. ADR-7.1-001 future-consumer list extended with `Hexalith.Memories.Benchmarks` (Story 2.7) — a typed `MemoriesClient` is cleaner for benchmark queries than hand-rolled `HttpClient`, strengthens the "keep Client.Rest" rationale. AC #8 and DoD #6 updated to reflect the single test project.
- **Revision 7 (2026-04-16, post-advanced-elicitation meta/time-traveler/socratic/chaos/literature):** Pinned AC #11 exit code to **2** directly (was "non-zero" — now cites the exit-code table). Task 10.1 extended: outermost `catch` in `CliCommandExecutor` maps unhandled exceptions to exit code 2 instead of .NET's default exit 1 (which is reserved for Story 7.3 domain errors) — prevents a real behavior bug. Task 8.1 packaging script now verifies `dotnet tool install -g` target directory is on PATH and points to `docs/dev/cli-config.md`'s PATH section on failure (Chaos Monkey found this corp-Windows edge case). Task 9.1 adds a PATH troubleshooting section to `cli-config.md` covering bash/zsh/PowerShell/cmd. Implementation Contracts gained a **CLI logging policy** subsection (stderr only, `Information` gated on `--verbose`, no token leakage, pipe-friendly stdout on the default path). Anti-pattern section now leads with a **Top-5 highest-risk** callout (#1, #8, #12, #17, #18) for scannability — the 18-item full list follows. Added a **Story Template for Stories 7.2-7.5** note at the end of Dev Notes codifying the process lessons: inherit ADRs + anti-patterns + implementation contracts by reference; run only pre-mortem + planning elicitation; target 250-350 lines per derivative story, not 520+.

### File List

- `_bmad-output/implementation-artifacts/7-1-cli-foundation-and-command-structure.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `Directory.Packages.props`
- `Hexalith.Memories.slnx`
- `README.md`
- `docs/dev/cli-config.md`
- `src/Hexalith.Memories.Client.Rest/Hexalith.Memories.Client.Rest.csproj`
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `src/Hexalith.Memories.Client.Rest/MemoriesClientOptions.cs`
- `src/Hexalith.Memories.Client.Rest/MemoriesAuthHandler.cs`
- `src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Client.Rest/MemoriesRemoteException.cs`
- `src/Hexalith.Memories.Client.Rest/ErrorResponseDecoder.cs`
- `src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj`
- `src/Hexalith.Memories.Cli/Program.cs`
- `src/Hexalith.Memories.Cli/CliServices.cs`
- `src/Hexalith.Memories.Cli/LiveOptionsMonitor.cs`
- `src/Hexalith.Memories.Cli/Commands/CliGlobalOptions.cs`
- `src/Hexalith.Memories.Cli/Commands/ConfigShowCommand.cs`
- `src/Hexalith.Memories.Cli/Commands/NotImplementedCommand.cs`
- `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`
- `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs`
- `src/Hexalith.Memories.Cli/Configuration/DefaultConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Configuration/EnvironmentVariableConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Configuration/FileConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Configuration/FlagConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Configuration/IConfigurationSource.cs`
- `src/Hexalith.Memories.Cli/Configuration/InsecureTokenTransportException.cs`
- `src/Hexalith.Memories.Cli/Configuration/InvalidConfigurationException.cs`
- `src/Hexalith.Memories.Cli/Configuration/ResolvedConfig.cs`
- `src/Hexalith.Memories.Cli/Configuration/ResolvedConfigPipeline.cs`
- `src/Hexalith.Memories.Cli/Execution/CliCommandExecutor.cs`
- `src/Hexalith.Memories.Cli/Execution/CliConsole.cs`
- `src/Hexalith.Memories.Cli/Execution/CliExitCodes.cs`
- `src/Hexalith.Memories.Cli/Execution/MemoriesClientOptionsMutator.cs`
- `tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj`
- `tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesAuthHandlerTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/CliCommandExecutorTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/EnvironmentVariableConfigurationSourceTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/FileConfigurationSourceTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/InsecureTokenTransportExceptionTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/ResolvedConfigPipelineTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/TokenRedactionTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj`
- `tests/Hexalith.Memories.IntegrationTests/Cli/CliTenantListIntegrationTests.cs`
- `tools/verify-cli-pack.ps1`
- `tools/verify-cli-pack.sh`

### Change Log

| Date       | Version | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| :--------- | :------ | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-04-16 | 1.0     | Implementation landed. Two new `src/` projects (`Client.Rest`, `Cli`) and one consolidated `tests/` project (`Cli.Tests`). CLI framework locked to System.CommandLine 2.0.0-beta5 per ADR-7.1-008. All 10 Tasks checked off, 51 new Cli.Tests passing, no regressions in Server (1025) or Contracts (288) test suites. Packaging verified end-to-end via `dotnet pack` + `dotnet tool install -g` + `memories --version` + uninstall. Story status: ready-for-dev → review. |
| 2026-04-16 | 1.1     | Review fixes applied. Resolved the docker-service token conflict in favor of the strict plaintext-token guard, sanitized endpoint diagnostics, made no-arg root help succeed, added required help examples, rejected malformed env endpoints instead of silently falling through, broadened health probes to any 2xx, and raised the CLI test suite to 55 passing tests. Story status: review → done.                                                                       |

### Review Findings

- [x] \[Review]\[Decision] Docker service-name token handling conflicts with the plaintext-token guard — resolved in favor of the strict plaintext-token guard: non-localhost `http://` endpoints remain unsupported when a token is configured, and `MemoriesAuthHandler`, its tests, and the CLI docs were aligned to reject that path explicitly. Evidence: `src/Hexalith.Memories.Client.Rest/MemoriesAuthHandler.cs`, `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesAuthHandlerTests.cs`, `docs/dev/cli-config.md`.

- [x] \[Review]\[Patch] No-arg `memories` exits with code 1 instead of showing help successfully [src/Hexalith.Memories.Cli/Program.cs:21] — fixed by translating an empty argument list to `--help` and returning success.
- [x] \[Review]\[Patch] Help output is missing the required usage examples for `memories tenant list` [src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs:43] — fixed by embedding examples in the root, `tenant`, and `tenant list` descriptions and validating them with CLI tests.
- [x] \[Review]\[Patch] `config show` and verbose diagnostics leak credentials embedded in endpoint URIs [src/Hexalith.Memories.Cli/Commands/ConfigShowCommand.cs:36] — fixed by sanitizing endpoint display output to strip userinfo, query, and fragment material.
- [x] \[Review]\[Patch] Invalid `HEXALITH_MEMORIES_ENDPOINT` values silently fall through to the default endpoint while preserving the env token [src/Hexalith.Memories.Cli/Configuration/EnvironmentVariableConfigurationSource.cs:39] — fixed by failing fast with `InvalidConfigurationException` when the env endpoint is malformed.
- [x] \[Review]\[Patch] `ProbeHealthAsync` only accepts 200/204 instead of treating every 2xx as healthy [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:108] — fixed by accepting any successful HTTP status from `/health`.
