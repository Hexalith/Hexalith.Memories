# Story 7.4: Quickstart & Documentation

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** the **`memories quickstart` interactive wizard** and the **`--help` completeness guarantee** that closes NFR30 + NFR31 — i.e., the Epic 7 Gate 3 "polished CLI, <30-min onboarding" gate. Replaces 7.3's `NotImplementedCommand` stub for `quickstart` (registered at `RootCommandFactory.cs:42` as `("quickstart", "Guided onboarding flow (quickstart wizard).", "7.4")`) with a real command that walks a developer through: (a) **prerequisite verification** — Docker daemon reachable, .NET 9 SDK present, port availability (5000, 6379, 6380, 3500, 50001), optional DAPR CLI, (b) **stack boot instructions** — prints the exact `dotnet run --project src/Hexalith.Memories.AppHost` command (never spawns it — see ADR-7.4-001), (c) **server-reachability probe** — polls `GET /health` against the resolved endpoint until responsive or a bounded timeout fires, (d) **tenant provisioning** — creates a sample tenant via `POST /api/tenants` using the existing `MemoriesClient` (tenant-create-via-client is already wired for the server REST surface — see References for the endpoint path), (e) **sample-document ingestion** — publishes one in-memory sample `MemoryUnit` via `POST /api/ingest` (no file dependency — the quickstart embeds the sample text), (f) **validation search** — runs one hybrid search and confirms the sample's returned. Also ships a **README quickstart section** timed to complete in <30 minutes (NFR31 — measured by a CI walkthrough test), a **`--help` audit** that asserts every `memories ...` subcommand has at least one usage example embedded in its `Description` (NFR30 — enforced via `CliHelpCompletenessTests`), and extended **`docs/dev/` content** covering the quickstart command, the 30-minute walkthrough trace, and a decision tree of "what to do if the quickstart fails at step X."

In practice this story adds eight things to the repo:

1. **`QuickstartCommand` production code** in `src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs`:
    - Public `const string CommandName = "quickstart"` following the 7.3 command-name plumbing convention (see `TenantListCommand.cs:42`, `SearchQueryCommand.cs:26`, `ConfigShowCommand.cs:26`).
    - Command registered under `RootCommandFactory.Build` as its own top-level subcommand — **replaces** the current `NotImplementedCommand` stub entry in `RootCommandFactory.CommandGroups` (`RootCommandFactory.cs:42`). Register by removing the `("quickstart", ...)` tuple from `CommandGroups` and adding an explicit `root.Subcommands.Add(QuickstartCommand.Build(services));` block alongside `tenant` / `config` / `search` at `RootCommandFactory.cs:60-77`. The ordering inside `Build` matches the other wired groups (create command, add subcommands if any, set default help action, add to root).
    - No subcommands in 7.4 — `memories quickstart` is a single command. If Phase 1.5 adds `quickstart cleanup` (tear-down of the sample tenant) it lands as an additive subcommand then; 7.4 keeps the surface minimal.
    - Options (all optional, sane defaults):
      - `--tenant <id>` — id of the sample tenant to create (default: `quickstart-YYYYMMDD` using invariant date format in UTC, so reruns on the same day collide intentionally — see ADR-7.4-004 for idempotency semantics).
      - `--skip-boot-check` — skip the `GET /health` readiness probe (step c). Useful when running the wizard inside a fixture that already bootstrapped the stack.
      - `--skip-prereq-check` — skip the Docker/.NET/port prerequisite block (step a). Useful in CI where the stack is container-based and Docker-on-Docker is either unavailable or intentional.
      - `--dry-run` — print every step and the exact command/API call it would perform, but do not mutate any state (no REST calls, no file writes). Exits `0`. Required for the NFR31 CI walkthrough test so we can assert the script without standing up a real server.
    - Execution flow is a straight sequence with **no interactive prompts** (ADR-7.4-002 — wizard is non-interactive by default, prints-and-continues). Every step prints a labeled status line (`[1/6] Verifying prerequisites...`), runs the work, prints the outcome (`[1/6] OK: Docker reachable (docker ps succeeded in 240ms)` or `[1/6] FAIL: ...` with the FR56 recovery-suggestion shape from Story 7.3's `ErrorMessageCatalog`).
    - Respects `--format json` — emits a single envelope `{ "schemaVersion": 1, "command": "quickstart", "data": { "steps": [ { "id": 1, "title": "Verify prerequisites", "status": "ok"|"fail"|"skip", "durationMs": n, "message": "...", "suggestion": "..." }, ... ], "overallStatus": "ok"|"fail", "elapsedMs": n } }`. Per-step `message`/`suggestion` match the human-format strings.
    - Respects `--format table` — prints a two-column table: `STEP | STATUS`. Each row's detail-line (e.g., "OK: Docker reachable") goes to stderr as a supplementary nudge so the table stays interactively readable AND pipe-friendly.
    - Exit codes: `0` if all steps succeeded or all skipped; `1` if a domain-shaped failure blocked the wizard (e.g., tenant provisioning returned `TENANT_FAILED`); `2` if infrastructure/plumbing failed (Docker not reachable, server unreachable after timeout, port conflict). Matches 7.3's split precisely — use `ErrorMessageCatalog.Resolve` to map any `MemoriesRemoteException` to `(message, suggestion, exitCode)`.
    - **Cancellation** — honors `CancellationToken` at every await boundary. `Ctrl-C` exits `130` unchanged from 7.1.
    - **Internal structure** — `QuickstartCommand.ExecuteAsync` delegates to five private `RunStepXAsync` methods, one per wizard step. Each returns a `QuickstartStepResult` record `(int Id, string Title, QuickstartStepStatus Status, TimeSpan Duration, string Message, string? Suggestion)`. The main loop composes results into the envelope. **Why split:** keeps each step unit-testable in isolation without spinning up the whole command; each private method can be an `internal static` with `InternalsVisibleTo` so tests can call them directly (pattern inherited from 7.3's `JsonErrorEnvelopeWriter` visibility).

2. **Prerequisite-check substrate** in `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteCheck.cs`:
    - `public sealed record PrerequisiteCheckResult(bool Passed, string Diagnostic, string? RecoverySuggestion);`
    - `public static class PrerequisiteChecks` exposing five check methods — `CheckDockerAsync`, `CheckDotnetSdkAsync`, `CheckPortAvailabilityAsync`, `CheckOsPlatform`, `CheckDaprCliAsync` (optional — passes with a `(true, "DAPR CLI not found (optional).", null)` when missing).
    - Docker check: shell out to `docker ps` with a 5-second timeout; success if exit code 0. Recovery suggestion on failure: `"Docker daemon not reachable. Install Docker Desktop (https://docs.docker.com/desktop/) or start an existing daemon, then retry. See docs/dev/quickstart.md for OS-specific setup."`
    - .NET SDK check: `dotnet --list-sdks` with timeout; parse the version strings and assert at least one 9.x SDK is present (the `global.json` if any pins the minor, but the wizard should fail fast if there's no 9.x at all — the SDK-pin error from `dotnet build` is a confusing surface).
    - Port-availability check: use `TcpListener.Start` on `127.0.0.1:<port>` for each of [5000, 6379, 6380, 3500, 50001] — if bind fails, report the port as in-use with suggestion "Port <port> appears to be in use by another process. Find the owner with `lsof -i :<port>` (macOS/Linux) or `netstat -ano | findstr :<port>` (Windows), then stop it or set a conflicting service to a different port."
    - OS platform check: `RuntimeInformation.IsOSPlatform(OSPlatform.Windows/Linux/OSX)` — prints the detected platform; purely informational (no failure mode), but flagged in output so a bug-reporter can include the platform in issue triage.
    - DAPR CLI check: `dapr --version` with timeout; soft-fail (check returns `Passed = true` with a diagnostic like "DAPR CLI not installed (optional for local dev; Aspire manages sidecar)").
    - **Platform-specific port check caveat:** on Windows, some privileged services (Hyper-V, Docker Desktop, IIS) reserve port ranges without holding a live listener — `TcpListener.Start` succeeds then the real service lands on the same port later. Mitigation: the check is **best-effort**; a failure is load-bearing ("port unavailable — boot will fail") but a pass is advisory ("port appeared free at the time of check"). Document this in the diagnostic text so users who see a later boot failure don't feel misled.

3. **Server-reachability probe** in `src/Hexalith.Memories.Cli/Quickstart/HealthProbe.cs`:
    - `public static async Task<HealthProbeResult> WaitForReadyAsync(MemoriesClient client, TimeSpan totalTimeout, TimeSpan pollInterval, CancellationToken ct)`.
    - Polls the server's existing `/health` endpoint (see `README.md` — `http://localhost:5000/health`) at `pollInterval` (default 1s) until one of: `200 OK` → success, `totalTimeout` elapsed → failure, `ct.IsCancellationRequested` → canceled.
    - **Client method pinned (Revision 0.2 — Amelia finding):** `MemoriesClient.ProbeHealthAsync(CancellationToken) : Task<bool>` **already exists** at `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:257`. The wizard uses it directly — no new client method needed. Swallows transport exceptions and returns `false` for "not yet ready" already, matching the probe loop's expected semantics.
    - Returns `HealthProbeResult(bool Ready, TimeSpan Elapsed, string? LastError)`. The `LastError` is the last exception's message (for diagnostic display when the wizard fails — `"Server did not become ready within 60s. Last probe error: Connection refused to http://127.0.0.1:5000"`).
    - **Timeout default 60 seconds.** Rationale: on a cold-start where Docker images are pulled for the first time, 60s is tight but achievable for the already-cached typical dev loop. CI-cold runs (see NFR31 walkthrough test) may need `--timeout <seconds>` in a later polish story; 7.4 hardcodes 60s and documents the limit. If the CI walkthrough test observes >60s in practice, extend default to 120s — but do NOT make this configurable until a real user complaint arrives (YAGNI).

4. **Tenant provisioning helper** in `src/Hexalith.Memories.Cli/Quickstart/QuickstartTenantProvisioner.cs`:
    - `public static async Task<QuickstartTenantResult> EnsureSampleTenantAsync(MemoriesClient client, string tenantId, CancellationToken ct)`.
    - **Client method pinned (Revision 0.2 — Amelia finding):** `MemoriesClient` has `ListTenantsAsync`, `ListCasesAsync`, `HybridSearchAsync`, `SearchAsync`, `GetMemoryUnitAsync`, `ProbeHealthAsync` — but **no tenant-create method** as of 7.3. The server endpoint exists (`src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs`). 7.4 takes on scope to add `CreateTenantAsync(string tenantId, string displayName, CancellationToken ct) : Task<TenantSummary>` to `MemoriesClient` — a minimal wrapper around the existing server `POST /api/tenants` (or equivalent path; confirm by reading `TenantEndpointHandlers.cs` route attributes at implementation time). **Rationale for scope expansion:** without this, the "wizard" is a cheat-sheet that prints curl commands — fails the FR57 "no dead-end states" spirit. One new client method (~20 LOC) is materially cheaper than shipping a degraded wizard and calling it "7.4 done." The method stays internal to 7.4 scope; `memories tenant create` CLI subcommand wiring is still a separate Phase 1.5 story.
    - **EXPERIMENTAL marker (Revision 0.3 — Debate Club synthesis):** `CreateTenantAsync` and `IngestAsync` ship as `public virtual` (matching existing `MemoriesClient` convention — all public methods are `virtual` for mock-friendly unit testing). To manage the "half-orphaned" concern (no CLI subcommand uses them until Phase 1.5), annotate each with XML doc `<remarks>EXPERIMENTAL (Story 7.4): Added to unblock the quickstart wizard. Signature may change when 'memories tenant create' / 'memories ingest' CLI subcommands are wired in Phase 1.5 — external consumers (e.g., test harnesses, third-party tooling) using these methods directly should expect refactoring.</remarks>` This keeps the API surface consistent with the rest of the client while setting expectation that the methods are subject to change without a major-version bump until the CLI subcommands stabilize them.
    - Idempotency: if the tenant already exists (`TENANT_ALREADY_EXISTS` or equivalent server code), report as `Passed = true` with message "Sample tenant already exists — continuing with existing tenant." This makes `memories quickstart` safe to rerun without state cleanup (ADR-7.4-004). If the server's "already exists" code is a 409 → parsed as `MemoriesRemoteException` with a specific `Code`, use `ErrorMessageCatalog.Resolve` for the message/suggestion but convert the step status to `skip` rather than `fail` when the catalog entry's exit code is `1` AND the server code is an "already exists" signal. **Grep the server source for the exact code literal at implementation time** — server emits "already-exists" signals via `TenantStatusGuard` / `TenantEndpointHandlers`; common candidates are `TENANT_ALREADY_EXISTS`, `DUPLICATE_TENANT`, `CONFLICT`. Pin the literal in the catalog's "known-safe rerun" allow-list. If no dedicated code exists (server silently treats duplicate as success), skip the branch — treat all 2xx responses as ok.

5. **Sample ingestion + validation search** in `src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs`:
    - Constant sample payload: a small embedded English text string (~200 words) describing a **generic memory system** — purpose-written fresh prose for this story (Revision 0.2 — Paige finding — avoid embedding the product-brief, which is an internal BMAD artifact). The text must contain the deterministic validation keywords `hybrid`, `search`, `memory`, `tenant`, `case` used by Task 5.4's validation query. Sample text lives in `QuickstartSampleFlow.cs` as a `private const string SampleDocumentText`; target length ~200 words; tone neutral-descriptive so a reader glancing at the ingested unit understands it's a demo document, not a real memory. **Do not** read from disk at runtime — zero file-IO dependencies.
    - `IngestSampleAsync(MemoriesClient client, string tenantId, CancellationToken ct)` → **Client method pinned (Revision 0.2 — Amelia finding):** `MemoriesClient` has **no ingest method** as of 7.3. 7.4 takes on scope to add `IngestAsync(string tenantId, string caseId, string content, IReadOnlyDictionary<string, string> metadata, CancellationToken ct) : Task<MemoryUnit>` wrapping the existing server `POST /api/ingest` (or equivalent; confirm endpoint path at implementation time from `Program.cs` route registration). Same rationale as the tenant-create scope expansion above. Pass `metadata = {"origin": "quickstart", "wizardVersion": "7.4"}` so the unit is discoverable as a wizard-created artifact.
    - `ValidateSearchAsync(MemoriesClient client, string tenantId, string memoryUnitId, CancellationToken ct)` → runs one hybrid search with a deterministic query (`"hybrid search"` — the sample text embeds this term exactly once). Returns `Passed = true` if `results.Any(r => r.MemoryUnitId == memoryUnitId)`. Thresholded timeout exists because the vector index may take a few seconds to become queryable after ingestion (async-write pipeline). If zero/unmatching results, retry up to 3 times with 2-second backoff before declaring failure.
    - On failure: do NOT delete the sample tenant — leaving it allows the developer to inspect state with `memories tenant list` / `memories search query` to debug. ADR-7.4-005 pins this "no cleanup on failure" rule.

6. **README Quick start rewrite** in `README.md`:
    - Replace the existing two-line "Quick start" section (`README.md:5-14`) with a full NFR31-compliant walkthrough: prerequisite list, clone + submodule init, `dotnet run --project src/Hexalith.Memories.AppHost` (explicit, not `dotnet run` bare — Aspire needs the specific project), wait for Aspire dashboard, in a second terminal install the CLI (`dotnet pack ... && dotnet tool install -g ...`), run `memories quickstart`, expected outputs per step, where to look if step N fails.
    - Timing annotation in parentheses after each step: `(~2 min first time, ~30s subsequent)` — measured by the CI walkthrough test, not guessed.
    - Keep the existing "Local development stack" section (`README.md:16-30`) as a reference. The Quick start points to it for deep-dive.
    - **Preserve** the existing "CLI (preview)" section's 7.2/7.3 content — 7.4 adds the line `"Story 7.4 wires the 'memories quickstart' guided wizard that verifies prerequisites, boots the stack, and runs a sample search — completing in <30 minutes on a clean machine (NFR31)."` alongside the 7.2/7.3 sentences. Do NOT rewrite the whole section; this is an additive edit.
    - Do NOT remove the `git submodule update --init --recursive` step — it's load-bearing (see `README.md:8`). The quickstart wizard assumes submodules are present; document this dependency.

7. **`--help` completeness guarantee** in `tests/Hexalith.Memories.Cli.Tests/Cli/CliHelpCompletenessTests.cs`:
    - Single `[Fact]` that walks the command tree built by `RootCommandFactory.Build(services, globalOptions)` (use the same service-provider setup as other `Cli.Tests` suites — `ServiceCollection` + `BuildServiceProvider`).
    - For every `Command` in the tree (root + descendants), assert: (a) `Description` is non-null and non-empty, (b) `Description` contains the literal substring `"Example"` (case-insensitive), (c) at least one line of the description starts with `"    memories "` (four-space-indented `memories` invocation — the established pattern from `TenantListCommand.cs:23-27`). This is the concrete "at least one usage example" contract for NFR30.
    - Tree-walk helper: recursive descent over `command.Subcommands`. Flatten to a list, filter out `HelpCommand` and `VersionCommand` (System.CommandLine built-ins have no examples and should be excluded from the audit).
    - Failure message must name the exact command path that's missing an example (e.g., `"Command 'memories search inspect' has no usage example in its description. See TenantListCommand.cs:23-27 for the expected pattern."`) — makes the fix obvious without the developer having to grep.
    - **Scope:** audits WIRED commands (not `NotImplementedCommand` stubs). The stubs' descriptions come from `CommandGroups` tuples (`RootCommandFactory.cs:36-43`) which are terse one-liners — holding them to the example rule now would block 7.4 on 5 other stories (7.2 `ingest`, `traverse`, `case`, `status`, `explore`, `handlers`). Filter: skip commands whose action is the `NotImplementedCommand` stub (detect by checking if the command's action prints the "Not yet implemented" string — a tag via `Tags` or a marker property is cleaner than string-matching; add a tag via `command.Tags.Add("stub")` in `NotImplementedCommand.Create` and filter on `command.Tags.Contains("stub")` in the test).
    - **Concrete in-scope set as of 7.4:** `memories tenant list` ✓, `memories config show` ✓, `memories search query` ✓, `memories search inspect` ✓, `memories quickstart` (added in this story, must pass). The root command and container commands (`tenant`, `config`, `search`) are also in-scope and must have a description with an example — verify `"tenant"` and `"search"` already have descriptions in `RootCommandFactory.Build` and amend if needed.

8. **Quickstart-specific docs** in `docs/dev/quickstart.md` (new file):
    - 30-min walkthrough with timing annotations per section (mirrors the README but deeper).
    - Per-step failure decision tree: what to check if step 1 fails, step 2 fails, etc. Each failure node ends with either a remediation action or a "file an issue with this info" link.
    - OS-specific notes: Windows (Docker Desktop WSL2 requirement, port reservation edge case), macOS (Docker Desktop resource allocation, Rosetta on Apple Silicon if needed), Linux (native Docker daemon vs. rootless — impacts `docker ps` check).
    - `--dry-run` mode explanation and an example dry-run transcript.
    - `--format json` envelope structure and a worked example (per-step status enumeration).
    - Cross-references to `docs/dev/cli-config.md` (endpoint resolution), `docs/dev/cli-output-formats.md` (envelope + exit-code table).

**What does NOT ship:**

- **Interactive prompts in the wizard** (`Press Enter to continue`, Q&A dialogs). Quickstart is non-interactive end-to-end (ADR-7.4-002) — every step runs unattended and prints outcomes. Interactivity inflates test surface (TTY-dependent paths) and makes the wizard unscriptable. A future story can add `--interactive` opt-in if real user feedback justifies it.
- **Quickstart teardown subcommand** (`memories quickstart cleanup`). The wizard intentionally leaves the sample tenant + sample memory unit in place so the developer can poke around. Cleanup lands in Phase 1.5 or when the samples/ folder flow (PRD line 784 — `samples/01-quickstart/`) is wired end-to-end.
- **samples/ folder wiring** (PRD line 784: `samples/01-quickstart/`, `samples/02-eventstore-integration/`, `samples/03-mcp-agent/`). The PRD references these but they are **NOT** in the epics-file scope for Story 7.4 — 7.4 owns the `memories quickstart` CLI command and README quickstart, not the sample projects. Creating the samples/ folder in this story would expand scope by ~3 additional projects. Noted in References as "PRD mentions samples/ — out of 7.4 scope; file follow-up story."
- **Programmatic AppHost boot from within the wizard.** The wizard never runs `dotnet run --project src/Hexalith.Memories.AppHost` on the developer's behalf — it prints the command and polls for readiness (ADR-7.4-001). Spawning subprocesses from a `dotnet tool global` binary couples CLI to build-tool state, and a failed `dotnet run` leaves the wizard in an ambiguous "is the subprocess my problem?" state. Print-then-poll is strictly better.
- **`memories tenant create` command wiring.** Story 7.3's `TenantListCommand.WriteEmptyTenantsNudge` (at `TenantListCommand.cs:70-73`) already references `memories tenant create` as "coming soon" and falls back to REST-API instructions. The quickstart wizard **uses** the existing server endpoint via `MemoriesClient` but does NOT wire the `tenant create` CLI subcommand — that's a separate deferred item owned by a Phase 1.5 follow-up. If the wizard uses client API directly for tenant-create, 7.4 ships with the CLI `tenant create` still absent; 7.3's "coming soon" wording stays intact.
- **`memories ingest` command wiring.** Same pattern: the wizard calls `client.IngestAsync(...)` directly; the `ingest` CLI subcommand (`RootCommandFactory.cs:36` stub for story 7.2 — actually it's marked "7.2" which is stale — ingest is a separate story territory) stays a stub.
- **Search/access audit telemetry.** Owned by Story 7.5 (FR67). The wizard logs to stdout/stderr for human observability but does NOT emit structured audit events.
- **New server endpoints.** 7.4 consumes the existing `/health`, `POST /api/tenants`, `POST /api/ingest`, search endpoints as-is. Zero server diff.
- **Schema-version bump.** The quickstart envelope `{ schemaVersion: 1, command: "quickstart", data: { steps: [...], overallStatus, elapsedMs } }` is a NEW `command`-level payload shape (matching ADR-7.2-001's additive rule); `schemaVersion` stays at `1`. Register `CliOutputEnvelope<QuickstartEnvelopeData>` in `CliJsonContext` (source-gen) and `QuickstartCommand.CommandName` in `CommandPayloadRegistry` (7.3 Task 2.4).

**Primary risks:**

1. **30-minute NFR31 target is ambitious on cold-start.** First-time developers clone the repo, run `git submodule update --init --recursive`, build the solution (NuGet restore + compile: 5-8 min on a warm cache, 12-15 min cold), pull Docker images (Redis Stack + FalkorDB: 1-3 min depending on registry speed), boot Aspire (30-60s), install the CLI (`dotnet pack` + `dotnet tool install`: 1 min), run `memories quickstart` (steps b-f: 10-60s). Mid-case total: 15-25 min. Worst-case (slow network, first-time Docker image pull): 25-35 min. **Mitigation:** the NFR31 walkthrough test in CI uses a **warm** runner (cached nuget, cached docker images) — it's measuring "repeat developer onboards teammate" not "first-ever setup." Document this distinction in the README Quick start so developers whose first run exceeds 30 min don't file NFR31 violations.
2. **`MemoriesClient` may not have `CreateTenantAsync` / `IngestAsync` wired.** The server has the endpoints; the client (`src/Hexalith.Memories.Client.Rest/`) might or might not. **Resolution:** grep at implementation time. If either method is absent, the wizard falls back to printing the equivalent REST call (curl) and marking the step as `advisory` rather than `ok`/`fail` — wizard still exits `0` but the developer has to run the curl manually. This is a graceful degradation, not a failure mode. Track the missing client methods as a follow-up note (Completion Notes).
3. **Port-conflict false negatives on Windows.** `TcpListener.Start` on `127.0.0.1:<port>` may succeed even when Docker Desktop has reserved the port via Hyper-V, leading to "port ok" in the wizard then boot failure in AppHost. **Mitigation:** document the caveat (see Tasks 2.x); the wizard reports the port as best-effort; the ultimate source of truth is the boot step.
4. **`dotnet --list-sdks` output parsing fragile across locales.** On non-English Windows the `dotnet --list-sdks` output may have localized path separators or quote characters. **Mitigation:** parse only the version portion (first token of each line, `Regex.Match(line, @"^(\d+\.\d+\.\d+)")`) — the version format is invariant. If parsing fails (regex returns no matches), mark the step as `advisory` with suggestion "Unable to parse `dotnet --list-sdks` output; verify manually that .NET 9 SDK is installed with `dotnet --version`."
5. **CI walkthrough test (NFR31) flakiness.** The test spins up the full Aspire stack + runs the quickstart; network or Docker flakes can push the timing past 30 min. **Mitigation:** use `--skip-prereq-check` AND `--skip-boot-check` in the CI harness — the fixture already guarantees the stack is up (`AspireIngestionPipelineFixture`). The NFR31 test measures wall-clock from "start of quickstart steps (c onwards)" to "validation search returns a result" — not the cold-boot time. Acceptance criterion: p99 < 60 seconds. That's the enforceable test; the 30-min claim is for the human-running-from-scratch story, documented but not CI-gated.
6. **`--help` audit may reveal pre-7.4 commands missing examples.** The test scans ALL wired commands — if any pre-7.4 command (e.g., `config show`, `search inspect`) lacks an example, the test fails **retroactively**. **Mitigation:** pre-audit at implementation time — grep each `CommandDescription` field for `"Example"` or `"memories "` and fix the ones that miss it AS PART OF Task 6 in this story (NOT a separate cleanup PR). If the audit reveals a missing example, the fix is trivial (add a 2-line example string to the description), so this is pre-absorbed scope, not scope creep. Expect 0-2 fixes; more than 3 means someone's pattern has drifted and deserves a design discussion (check the story's Dev Notes for the pattern).
7. **README rewrite risks churning `ConfigShowGoldenFileTests` (ADR-7.2-002 byte-for-byte backstop).** `ConfigShowGoldenFileTests` tests `memories config show` output, NOT README content — but verify by reading the test file first (same caution applied in 7.3 Dev Notes). Similarly, any test that snapshots the root command's help output (if such a test exists — grep for `RootCommand` + `"Example"` / `"Hexalith.Memories CLI"` in tests) would fail if root help text is edited for NFR30 compliance. Pre-flight: grep `tests/` for references to `RootCommandFactory.Build(...)` or the literal `"foundation tool shipped by Story 7.1"` (current root description) to surface any snapshotted help. If found, update snapshots in the same PR.
8. **Help-text sufficiency vs. verbosity tension.** NFR30 specifies "at least one usage example" — a single example is sufficient. But the established pattern (`TenantListCommand.cs:23-27`) shows THREE examples (bare, `--endpoint` variant, `--format json` variant) for `tenant list`. The audit test should enforce the floor (1 example), not the ceiling (don't mandate 3+ examples for every command — over-mandating burns maintainer effort on help text that nobody reads beyond the first example). Pin the test assertion at "≥1" and document in the story that 1-3 examples per command is the expected range.

## Story

As a developer,
I want a guided quickstart and comprehensive help,
so that I can go from zero to first search result in under 30 minutes.

## Acceptance Criteria

1. **Clean-machine quickstart completes in <30 minutes (NFR31).**
   **Given** a developer with Docker installed on a clean machine and the repository freshly cloned (`git submodule update --init --recursive` already run),
   **When** they follow the README Quick start section end-to-end (build solution → boot AppHost → install CLI → run `memories quickstart`),
   **Then** `memories quickstart` prints a final line with the elapsed wall-clock time (e.g., `"Quickstart complete in 18.4s across 6 steps."`)
   **And** total end-to-end time (from `git submodule update` through first successful search result in the quickstart's validation step) on a warm-cache clean machine is **approximately ≤ 30 minutes** — *unmeasured automated bound; see Task 12.6 for the manual-measurement cadence* (Revision 0.3 — PM finding: the 30-min claim in the PRD is aspirational UX copy, not a CI-gated contract; the story documents but does not auto-enforce it)
   **And** the per-step CI assertion `memories quickstart --skip-prereq-check --skip-boot-check` against `AspireIngestionPipelineFixture` completes in **≤ 60 seconds p99** (the enforceable machine-timed portion — see Risk #5 for the 30-min-vs-60s distinction).

2. **`memories quickstart` runs the six-step wizard (FR57).**
   **Given** the `memories quickstart` command with no additional flags against a running Memories Server,
   **When** executed,
   **Then** stdout shows six labeled step lines — `[1/6] Verifying prerequisites`, `[2/6] Printing stack boot command`, `[3/6] Probing server health`, `[4/6] Provisioning sample tenant`, `[5/6] Ingesting sample document`, `[6/6] Running validation search`
   **And** each step is followed by an outcome line `[N/6] OK: <detail>` (succeeded), `[N/6] SKIP: <detail>` (skipped via flag or idempotent no-op), or `[N/6] FAIL: <detail>\n  Suggestion: <text>` (failed — format mirrors Story 7.3 FR56 error surface)
   **And** on success, exit code is `0`
   **And** on domain failure (e.g., server returned `TENANT_FAILED`), exit code is `1`
   **And** on plumbing failure (Docker unreachable, server never became ready), exit code is `2`
   **And** step 3 (health probe) uses a 60-second default timeout with 1-second polling interval, failing cleanly if the window elapses.

3. **Prerequisite check covers Docker, .NET 9 SDK, port availability, OS, DAPR CLI (step 1).**
   **Given** `memories quickstart` step 1 runs,
   **When** the prerequisite block executes,
   **Then** it runs five sub-checks (listed in TL;DR point 2): Docker daemon reachability, .NET 9 SDK presence, local port availability on [5000, 6379, 6380, 3500, 50001], OS platform detection (informational), DAPR CLI presence (soft-fail)
   **And** each sub-check emits a one-line diagnostic on stdout (`[1/6]   Docker: OK (docker ps in 240ms)`)
   **And** any hard-fail sub-check (Docker, .NET, any port) marks step 1 as `FAIL` with the first-failing sub-check's recovery suggestion
   **And** soft-fail sub-checks (DAPR CLI missing) do NOT fail step 1 but are displayed as `[1/6]   DAPR CLI: skip (optional for local dev)`.

4. **Health probe polls `/health` until ready or timeout (step 3).**
   **Given** `memories quickstart` step 3 runs against an endpoint resolved from 7.1's four-tier resolver,
   **When** the server is already ready,
   **Then** the probe returns within one polling interval (<1s) with `[3/6] OK: Server ready at <endpoint> (120ms)`.

   **Given** the server is booting (reachable but returns 503 or connection-refused),
   **When** the probe retries at 1-second intervals,
   **Then** on successful `200 OK` from `/health` the step succeeds, reporting the total elapsed time
   **And** the 60-second total timeout, if exceeded, marks the step as `FAIL` with suggestion `"Server did not become ready within 60s at <endpoint>. Verify 'dotnet run --project src/Hexalith.Memories.AppHost' is running in another terminal. If the AppHost is running but on a different port (Aspire auto-assigns ports in test fixtures), check the Aspire dashboard for the 'memories-server' port and re-run with '--endpoint http://localhost:<port>'. Last probe error: <LastError>."` (Revision 0.3 — Aspire expert finding: the dashboard surfaces the actual port; the default 5000 in the 7.1 resolver is the `launchSettings.json` binding for local dev, but Aspire Testing fixtures randomize via the AppHost's `AppPort` omit. Diagnostic must name both paths so the user isn't stuck).

5. **Sample tenant provisioning is idempotent (ADR-7.4-004).**
   **Given** `memories quickstart` step 4 runs with `--tenant quickstart-20260416`,
   **When** the tenant does not yet exist on the server,
   **Then** the wizard calls `MemoriesClient` to create it and step reports `[4/6] OK: Created tenant 'quickstart-20260416' (320ms)`.

   **Given** the same command is re-run (same tenant id already exists from a prior run),
   **When** step 4 detects `TENANT_ALREADY_EXISTS` (or the server's equivalent conflict code — pin at implementation time),
   **Then** step reports `[4/6] SKIP: Sample tenant 'quickstart-20260416' already exists — continuing.`
   **And** the wizard does NOT fail
   **And** the final `overallStatus` remains `ok`.

   **Given** the server returns an unexpected error (e.g., `TENANT_FAILED`, `INVALID_TENANT_ID`),
   **When** step 4 translates via `ErrorMessageCatalog.Resolve`,
   **Then** step reports `[4/6] FAIL: <catalog-resolved message>\n  Suggestion: <catalog-resolved suggestion>` and the wizard exits with the catalog-resolved exit code.

6. **Ingestion + validation search confirm the pipeline (steps 5 + 6).**
   **Given** step 5 runs,
   **When** the embedded sample document is ingested via `MemoriesClient.IngestAsync` (or equivalent — grep first),
   **Then** step reports `[5/6] OK: Ingested sample document '<id>' (<elapsed>)`.

   **Given** step 6 runs with the hybrid search matching a deterministic term from the sample,
   **When** the search completes,
   **Then** the wizard asserts at least one result is returned within a 10-second window, retrying up to 3 times at 2-second intervals to tolerate async-write settling
   **And** on success step reports `[6/6] OK: Validation search returned <N> results; top result matches sample (score <confidence>).`
   **And** on failure (zero results after retries) step reports `[6/6] FAIL: Validation search returned zero results. Sample ingestion may not have completed indexing. Suggestion: Run 'memories search query --tenant <id> --query \"hybrid search\"' in a few seconds to retry manually. Check the server logs for ingestion errors.`.

7. **`--dry-run` prints every step's action without mutating state.**
   **Given** `memories quickstart --dry-run` against any endpoint,
   **When** executed,
   **Then** every step is printed with a `DRY-RUN:` prefix on the outcome line (`[4/6] DRY-RUN: Would POST /api/tenants with id='quickstart-20260416'`)
   **And** no REST calls are made to the server
   **And** no files are written
   **And** exit code is `0`
   **And** the `--format json` envelope still emits the full per-step structure with each step's `status: "dry-run"`.

8. **Format-aware output matches Story 7.2/7.3 envelope contract (FR55 + FR56).**
   **Given** `memories quickstart --format human` (default),
   **Then** output is the labeled per-step lines described in AC #2.

   **Given** `memories quickstart --format json`,
   **Then** stdout contains exactly one JSON document `{ "schemaVersion": 1, "command": "quickstart", "data": { "steps": [ { "id": 1, "title": "...", "status": "ok"|"fail"|"skip"|"dry-run", "durationMs": <int>, "message": "...", "suggestion": "...", "errorCode": "<synthetic or server code or null>" }, ... ], "overallStatus": "ok"|"fail", "elapsedMs": <int> } }`
   **And** the top-level envelope `error` slot is **never populated** for `quickstart` — per-step failures are encoded inside `data.steps[N]` (Revision 0.2 — Winston finding: preserves 7.3 mutual-exclusivity invariant; per-step `errorCode` + `message` + `suggestion` carry the failure context without breaking the `data XOR error` contract for non-wizard commands)
   **And** consumers determine failure via `data.overallStatus == "fail"` ⇔ `data.steps[].any(s => s.status == "fail")`
   **And** stdout contains exactly one JSON document; stderr is empty in JSON mode (per ADR-7.3-002).

   **Given** `memories quickstart --format table`,
   **Then** stdout contains a two-column table (`STEP | STATUS`) with one row per step
   **And** per-step detail lines (`OK: Docker reachable`) go to stderr as supplementary nudges so the table pipes cleanly.

9. **Every CLI command has `--help` with at least one usage example (NFR30).**
   **Given** the complete WIRED command tree built by `RootCommandFactory.Build`,
   **When** `CliHelpCompletenessTests.EveryCommand_HasAtLeastOneExample` runs as part of `dotnet test`,
   **Then** the test asserts, for every non-stub command (including `memories`, `memories tenant`, `memories tenant list`, `memories config show`, `memories search`, `memories search query`, `memories search inspect`, `memories quickstart`):
     - `Description` is non-null, non-empty
     - `Description` contains `"Example"` (case-insensitive)
     - `Description` contains at least one line matching regex `@"^\s{4}memories\b"` (four-space-indented `memories ...` invocation)
   **And** stub commands (detected via `command.Tags.Contains("stub")` — added by `NotImplementedCommand.Create`) are excluded from the audit (they'll be audited by the story that wires each group)
   **And** the failure message names the exact command path missing an example.

10. **`memories quickstart --help` displays the wizard's options and one usage example (NFR30 applied to the new command).**
    **Given** `memories quickstart --help`,
    **When** executed,
    **Then** the output shows: the command description, the four flags (`--tenant`, `--skip-boot-check`, `--skip-prereq-check`, `--dry-run`), the global options (`--endpoint`, `--token`, `--verbose`, `--format`), and at least one example block following the `TenantListCommand.cs:23-27` pattern
    **And** `CliHelpCompletenessTests` passes for this command (per AC #9).

11. **Tests cover the full quickstart surface (per-step × per-format × success/failure paths).**
    **Given** the consolidated `tests/Hexalith.Memories.Cli.Tests/` project,
    **When** `dotnet test` runs,
    **Then** it includes:
     - `QuickstartPrerequisiteTests` — per-sub-check unit tests with injected mocks for Docker/dotnet/port checks (use `IProcessRunner` abstraction pattern — see Dev Notes on avoiding real subprocess spawns in unit tests)
     - `QuickstartHealthProbeTests` — `MemoriesClient` mocked via `TestDelegatingHandler` (per 7.2 Task 4.5 / 7.3 Task 7.3); tests: immediate-ready, boot-then-ready (simulate 503 → 200 after 3 polls), never-ready (timeout), cancellation-respected
     - `QuickstartTenantProvisionerTests` — tests: new tenant success, existing tenant (idempotency), unexpected `MemoriesRemoteException` flows to `ErrorMessageCatalog.Resolve`
     - `QuickstartSampleFlowTests` — ingest success/failure; validation search success/retry-then-success/never-succeeds
     - `QuickstartCommandTests` (handler-level) — six-step happy path, each step's failure bubbles correctly, `--dry-run` emits no client calls (assert via mock expectations: zero REST calls), `--skip-prereq-check` + `--skip-boot-check` both honored, per-format output matrix (human / json / table), JSON envelope schema match, exit-code matrix (0 / 1 / 2)
     - `CliHelpCompletenessTests` — NFR30 audit (AC #9)
     - `QuickstartDryRunIntegrationTests` — one integration test: `memories quickstart --dry-run --format json --endpoint http://127.0.0.1:5000` against `AspireIngestionPipelineFixture`, asserts envelope schema and `overallStatus == "ok"` with all steps `status: "dry-run"`.
    **And** the live-wizard integration test `QuickstartLiveIntegrationTests.Quickstart_AgainstLiveFixture_SucceedsWithinSixtySeconds` runs `memories quickstart --skip-prereq-check --skip-boot-check` against `AspireIngestionPipelineFixture` and asserts `overallStatus == "ok"` AND `elapsedMs < 60_000` (the NFR31 CI-gated portion per Risk #5)
    **And** `[Trait("Category", "Integration")]` on live tests so dev agents without Docker can filter via `dotnet test --filter "Category!=Integration"` (7.2/7.3 convention).

12. **README Quick start section covers the full path with timing annotations.**
    **Given** README.md is updated,
    **When** a developer reads the Quick start section,
    **Then** the section includes: prerequisites (Docker, .NET 9 SDK, git), clone + submodule init, build solution, boot AppHost, install CLI, run `memories quickstart`, timing annotations per step (`(~2 min first time, ~30s subsequent)` where the annotation reflects CI-measured values, not guesses)
    **And** each step has an "if this fails, see <link>" pointer to `docs/dev/quickstart.md` or the per-section troubleshooting
    **And** the existing "CLI (preview)" section is preserved with an additive sentence describing the Story 7.4 wizard
    **And** the existing "Local development stack" and "Useful endpoints" sections are preserved unchanged.

13. **`docs/dev/quickstart.md` documents the wizard, the dry-run mode, the JSON envelope, and OS-specific notes.**
    **Given** a developer troubleshoots the wizard,
    **When** they consult `docs/dev/quickstart.md`,
    **Then** the doc covers:
     - Per-step explanation (what it checks, why it matters, what to do if it fails — a decision tree with remediation actions)
     - OS-specific caveats (Windows port reservation, macOS Rosetta, Linux rootless Docker)
     - `--dry-run` mode with a sample transcript
     - `--format json` envelope structure with a worked example
     - Cross-references to `docs/dev/cli-config.md` and `docs/dev/cli-output-formats.md`
     - A "when this is NOT the right command" section pointing to `dotnet run --project AppHost` for users who just want the stack up.

## Tasks / Subtasks

### Task Summary (orientation)

| # | Task | Blocked by | AC coverage |
|---|------|------------|-------------|
| 1 | `QuickstartCommand` scaffold: options, `QuickstartStepResult`, `QuickstartEnvelopeData` record, `CommandName` const, command-name plumbing | — | #2, #7, #8, #10 |
| 2 | `PrerequisiteChecks` — Docker, .NET SDK, ports, OS, DAPR CLI (with `IProcessRunner` abstraction for testability) | — | #3, #11 |
| 3 | `HealthProbe.WaitForReadyAsync` + optional `MemoriesClient.GetHealthAsync` addition | — | #4, #11 |
| 4 | `QuickstartTenantProvisioner.EnsureSampleTenantAsync` — idempotent, `ErrorMessageCatalog`-integrated | 1 | #5, #11 |
| 5 | `QuickstartSampleFlow` — embedded sample text, ingest + validation search with retry | 1, 4 | #6, #11 |
| 6 | `QuickstartCommand.ExecuteAsync` main loop + format-aware output + `--dry-run` branch | 1-5 | #2, #6, #7, #8, #10 |
| 7 | `RootCommandFactory` wiring — remove quickstart from `CommandGroups` stubs, add explicit `root.Subcommands.Add(QuickstartCommand.Build(services))`; add `command.Tags.Add("stub")` in `NotImplementedCommand.Create` | 1 | #9, #10 |
| 8 | `CliJsonContext` + `CommandPayloadRegistry` registration for `QuickstartEnvelopeData` | 1 | #8 |
| 9 | `CliHelpCompletenessTests` + pre-audit fix for any existing wired command missing an example | 7 | #9 |
| 10 | Unit tests — prerequisite, health probe, provisioner, sample flow, command handler per-format × per-outcome matrix | 1-6 | #11 |
| 11 | Integration tests — `QuickstartDryRunIntegrationTests` + `QuickstartLiveIntegrationTests` (with `--skip-*` flags + `elapsedMs < 60_000` assertion) | 6, 10 | #1, #11 |
| 12 | README rewrite — Quick start section with timing annotations + additive "CLI (preview)" sentence | 6 | #12 |
| 13 | `docs/dev/quickstart.md` + cross-references from `docs/dev/cli-config.md` and `docs/dev/cli-output-formats.md` | 6 | #13 |
| 14 | `tools/verify-cli-pack.{ps1,sh}` — add `memories quickstart --help` smoke (help should exit `0` regardless of server state) | 6, 7 | #10 |

Sequential execution (1 → 2 → 3 → ... → 14) is valid and simpler than the parallel streams below.

- [ ] Task 1: `QuickstartCommand` scaffold (AC: #2, #7, #8, #10)
    - [ ] 1.1 Create `src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs`: `public static class QuickstartCommand`. Define `public const string CommandName = "quickstart";` following 7.3 plumbing convention (`TenantListCommand.cs:42`).
    - [ ] 1.2 Create `public static Command Build(IServiceProvider services)` method. Pattern (Revision 0.3 — System.CommandLine expert finding: pin the option-capture convention explicitly): declare `Option<T>` instances as local variables at the top of `Build` (e.g., `var tenantOption = new Option<string?>("--tenant") { Description = "..." };`), add them via `command.Options.Add(tenantOption);`, then **capture them in the action lambda closure** so `ExecuteAsync` receives resolved values via `parseResult.GetValue(tenantOption)` — NOT by re-resolving from `parseResult` by string name. This mirrors `SearchQueryCommand.Build` at `SearchQueryCommand.cs:61-90` exactly. Rationale: closure-captured typed options are compiler-checked, refactor-safe, and impossible to silently break when option names change. Set action via `command.SetAction((parseResult, ct) => ExecuteAsync(services, parseResult, tenantOption, skipBootOption, skipPrereqOption, dryRunOption, ct))` — pass the captured options as method parameters, not as a ParseResult blob.
    - [ ] 1.3 Define `QuickstartCommandDescription` const with the required example block — must satisfy the NFR30 audit from Task 9 (AC #9). Template (adapt wording):
      ```
      Guided quickstart: verify prerequisites, print the stack boot command, probe server health, provision a sample tenant, ingest a sample document, and run a validation search.

      Examples:
          memories quickstart
          memories quickstart --tenant acme-quickstart
          memories quickstart --dry-run --format json
          memories quickstart --skip-prereq-check --skip-boot-check
      ```
    - [ ] 1.4 Define `public sealed record QuickstartStepResult(int Id, string Title, QuickstartStepStatus Status, TimeSpan Duration, string Message, string? Suggestion, string? ErrorCode);` in `src/Hexalith.Memories.Cli/Quickstart/QuickstartStepResult.cs`. **`ErrorCode` added Revision 0.2 (Winston finding)** — per-step failure code replaces top-level envelope `error` slot; nullable, set only when `Status == Fail` and the failure originated from a `MemoriesRemoteException` (catalog-resolved code) or a synthetic CLI code (e.g., `"DOCKER_UNAVAILABLE"`, `"SERVER_NOT_READY"`). `QuickstartStepStatus` is an enum in the same file: `Ok, Fail, Skip, DryRun`. **Apply `[JsonConverter(typeof(JsonStringEnumConverter<QuickstartStepStatus>))]` with `JsonNamingPolicy.KebabCaseLower`** (Revision 0.2 — Amelia finding) so the JSON envelope emits `"status": "ok"|"fail"|"skip"|"dry-run"` — kebab-case locked, not left as implementer choice.
    - [ ] 1.5 Define `public sealed record QuickstartEnvelopeData(IReadOnlyList<QuickstartStepResult> Steps, string OverallStatus, int ElapsedMs);` in `src/Hexalith.Memories.Cli/Quickstart/QuickstartEnvelopeData.cs`. **`OverallStatus` is a string** (not an enum) because the JSON envelope emits `"ok"|"fail"` and consumers need a stable string to switch on; an enum value named `Ok`/`Fail` would serialize as `"Ok"`/`"Fail"` without extra configuration, mismatching the lowercase convention. String with a documented invariant (`"ok"|"fail"`) is simpler.
    - [ ] 1.6 Thread the parsed options into an internal `QuickstartOptions` DTO: `internal sealed record QuickstartOptions(string? TenantId, bool SkipBootCheck, bool SkipPrereqCheck, bool DryRun);`. Parsed in `ExecuteAsync` from `parseResult.GetValue(...)` for each option. This DTO is passed to each private `RunStepXAsync` method so no step method reads `parseResult` directly — keeps step methods pure and testable.
    - [ ] 1.7 The `CommandName` const at `QuickstartCommand:1.1` must be registered in `CommandPayloadRegistry` (see Task 8) — the executor's JSON-error path looks up the envelope payload type by command name per 7.3 Task 2.4 / ADR-7.3-002.

- [ ] Task 2: `PrerequisiteChecks` (AC: #3, #11)
    - [ ] 2.1 Create `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteCheckResult.cs`: `public sealed record PrerequisiteCheckResult(bool Passed, string Diagnostic, string? RecoverySuggestion);`. Three fields; no additional state.
    - [ ] 2.2 Create `src/Hexalith.Memories.Cli/Quickstart/IProcessRunner.cs`: `internal interface IProcessRunner { Task<ProcessResult> RunAsync(string fileName, string arguments, TimeSpan timeout, CancellationToken ct); }`. `ProcessResult` is a record `(int ExitCode, string StdOut, string StdErr, TimeSpan Elapsed)`. This abstraction lets unit tests inject a fake — **no real subprocess spawned in `PrerequisiteChecksTests`**. Register `IProcessRunner` + `DefaultProcessRunner` in `CliServices.cs` (DI). **Why:** subprocess-spawning unit tests flake (PATH differences, docker-not-installed-locally, CI-runner differences) and are slow; the abstraction is cheap (~40 LOC for the default implementation) and makes the test surface deterministic. **Cancellation semantics (Revision 0.3 — async-io expert finding):** the default implementation MUST bind BOTH the caller's `ct` AND the per-call `timeout` via `CancellationTokenSource.CreateLinkedTokenSource(ct)` + `linkedCts.CancelAfter(timeout)`. On either signal, `Process.Kill()` terminates the subprocess and the method returns (or throws `OperationCanceledException` when `ct` fired, vs. a `ProcessResult` with a synthetic timeout exit code when the timeout fired). Rationale: without linked cancellation, Ctrl-C during a prereq sub-check hangs the wizard for up to 5 seconds waiting on `docker ps` to time out — breaks the 7.1 "cancellation exits in ≤1s" convention.
    - [ ] 2.3 Create `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs`: `public sealed class PrerequisiteChecks`. Constructor takes `IProcessRunner` + `TimeProvider` (for testable timeouts — inject `TimeProvider.System` in production, `FakeTimeProvider` in tests). Public methods: `Task<PrerequisiteCheckResult> CheckDockerAsync(CancellationToken ct)`, `Task<PrerequisiteCheckResult> CheckDotnetSdkAsync(CancellationToken ct)`, `Task<PrerequisiteCheckResult> CheckPortAvailabilityAsync(IReadOnlyCollection<int> ports, CancellationToken ct)`, `PrerequisiteCheckResult CheckOsPlatform()`, `Task<PrerequisiteCheckResult> CheckDaprCliAsync(CancellationToken ct)`.
    - [ ] 2.4 Docker check implementation: `await _processRunner.RunAsync("docker", "ps", TimeSpan.FromSeconds(5), ct)`. On exit-code 0 → `(true, $"Docker reachable ({elapsed.TotalMilliseconds:F0}ms)", null)`. On non-zero or timeout: `(false, $"Docker daemon not reachable (exit {exitCode}).", "Install Docker Desktop (https://docs.docker.com/desktop/) or start an existing daemon, then retry. See docs/dev/quickstart.md for OS-specific setup.")`. **Podman-as-docker caveat (Revision 0.2 — Murat finding):** on Linux distros where `docker` is an alias or symlink to `podman`, `Process.Start("docker", "ps")` resolves via PATH (not shell) — if the alias is shell-only, the check fails spuriously. Document in `docs/dev/quickstart.md` OS-specific notes (Task 13.1): "If you use podman-as-docker, the wizard's Docker check will fail even though containers work. Use `--skip-prereq-check` and rely on the boot step's own failure mode as ground truth." No code-side mitigation; PATH-based resolution is the correct default.
    - [ ] 2.5 .NET SDK check implementation: `dotnet --list-sdks` with 5s timeout. Parse output with `Regex.Matches(stdOut, @"^(\d+)\.(\d+)\.\d+", RegexOptions.Multiline)`. Assert at least one match has major >= 9. On pass: `(true, $"..NET SDK {highestVersion} (and {lowerCount} older)", null)`. On fail (no matches, or max major < 9): `(false, "No .NET 9+ SDK found.", "Install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0, then retry.")`. On regex-parse-fails (locale/format edge case per Risk #4): `(true, "Unable to parse dotnet --list-sdks output; skipping version check.", null)` — pass with advisory, do not fail hard.
    - [ ] 2.6 Port availability check implementation: for each port in `[5000, 6379, 6380, 3500, 50001]`, attempt `var listener = new TcpListener(IPAddress.Loopback, port); listener.Start(); listener.Stop();` wrapped in try/catch. `SocketException` (port in use) → `(false, $"Port {port} in use.", $"Port {port} appears in use. Find the owner: 'lsof -i :{port}' (macOS/Linux) or 'netstat -ano | findstr :{port}' (Windows). Stop that process or reconfigure the conflicting service.")` — return immediately on first in-use port (don't enumerate all five for a fail). On all-pass: `(true, $"Ports {string.Join(", ", ports)} available.", null)`.
    - [ ] 2.7 OS check implementation: pure synchronous, reads `RuntimeInformation.IsOSPlatform(...)` for Windows/Linux/OSX/FreeBSD. Result: `(true, $"OS detected: {platform} {RuntimeInformation.OSDescription}", null)`. Always passes.
    - [ ] 2.8 DAPR CLI check implementation: `_processRunner.RunAsync("dapr", "--version", TimeSpan.FromSeconds(3), ct)`. On exit-code 0 → `(true, $"DAPR CLI {parsedVersion} (optional)", null)`. On any failure (including `FileNotFoundException`-equivalent) → `(true, "DAPR CLI not installed (optional for local dev; Aspire manages the sidecar)", null)`. **DAPR CLI never hard-fails.**
    - [ ] 2.9 Wire `PrerequisiteChecks` registration in `src/Hexalith.Memories.Cli/CliServices.cs` (or equivalent DI composition root) alongside existing 7.1/7.2/7.3 services. Singleton scope (stateless class, no per-request need).

- [ ] Task 3: `HealthProbe` + optional `MemoriesClient.GetHealthAsync` (AC: #4, #11)
    - [ ] 3.1 Grep `src/Hexalith.Memories.Client.Rest/` for `GetHealth` / `HealthAsync` / `Ready`. **If present:** reuse it. **If absent:** add a minimal `public async Task<bool> GetHealthAsync(CancellationToken ct)` in `MemoriesClient.cs` using the existing HttpClient: `var response = await _http.GetAsync("/health", ct); return response.IsSuccessStatusCode;`. Swallow `HttpRequestException` and `SocketException` to return `false` — the caller treats those as "not yet ready" rather than errors. Document the method with XML doc: `<summary>Probes the server /health endpoint; returns false on any transport failure (not ready yet) and true on 200 OK.</summary>`.
    - [ ] 3.2 Create `src/Hexalith.Memories.Cli/Quickstart/HealthProbe.cs`: `public sealed class HealthProbe`. Constructor takes `MemoriesClient` + `TimeProvider`. Public method `public async Task<HealthProbeResult> WaitForReadyAsync(TimeSpan totalTimeout, TimeSpan pollInterval, CancellationToken ct)`.
    - [ ] 3.3 `HealthProbeResult` record: `public sealed record HealthProbeResult(bool Ready, TimeSpan Elapsed, string? LastError);`. `LastError` is null on immediate-ready, the last probe's exception message on timeout, or `"Cancelled"` on `ct.IsCancellationRequested`.
    - [ ] 3.4 Probe loop implementation: capture `stopwatch = _timeProvider.GetTimestamp()` at start. While `!ct.IsCancellationRequested`: call `GetHealthAsync`; if true → return `(true, elapsed, null)`. If elapsed >= totalTimeout → return `(false, elapsed, lastError ?? "Timeout elapsed without a successful probe.")`. Otherwise `await Task.Delay(pollInterval, ct)` + continue. **Snapshot** the last exception message when a probe call throws — so the failure diagnostic is actionable ("Server did not become ready within 60s. Last probe error: Connection refused to http://127.0.0.1:5000").
    - [ ] 3.5 Default values for the wizard: `totalTimeout = 60s`, `pollInterval = 1s`. Pass from `QuickstartCommand.ExecuteAsync` — do NOT hardcode in `HealthProbe` (the class is a reusable utility; defaults live at the call site).

- [ ] Task 4: `QuickstartTenantProvisioner` (AC: #5, #11)
    - [ ] 4.1 Grep `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` for tenant-create methods (`CreateTenantAsync`, `ProvisionTenantAsync`, `TenantAsync`). Verify signature and return type at implementation time.
    - [ ] 4.2 Create `src/Hexalith.Memories.Cli/Quickstart/QuickstartTenantProvisioner.cs`: `public sealed class QuickstartTenantProvisioner(MemoriesClient client)`. Public method `public async Task<QuickstartTenantResult> EnsureSampleTenantAsync(string tenantId, CancellationToken ct)`. **Also:** extend `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` with `public virtual async Task<TenantSummary> CreateTenantAsync(string tenantId, string displayName, CancellationToken ct)` — mirror the existing `ListTenantsAsync` pattern (`MemoriesClient.cs:55-72`) for error handling and JSON deserialization. Include the EXPERIMENTAL XML doc remark per TL;DR point 4.
    - [ ] 4.3 `QuickstartTenantResult` record: `public sealed record QuickstartTenantResult(bool Created, bool AlreadyExisted, string? ErrorCode, string? Diagnostic);`. `Created = true AND AlreadyExisted = false` on fresh create; `Created = false AND AlreadyExisted = true` on idempotent rerun; `Created = false AND AlreadyExisted = false AND ErrorCode != null` on failure.
    - [ ] 4.4 Implementation pattern: `try { await client.CreateTenantAsync(tenantId, ...other required fields..., ct); return new QuickstartTenantResult(true, false, null, $"Created tenant '{tenantId}'."); } catch (MemoriesRemoteException ex) when (IsTenantAlreadyExists(ex.Error.Code)) { return new QuickstartTenantResult(false, true, ex.Error.Code, $"Sample tenant '{tenantId}' already exists — continuing."); } catch (MemoriesRemoteException ex) { return new QuickstartTenantResult(false, false, ex.Error.Code, $"Tenant provisioning failed: {ex.Error.Message}"); }`.
    - [ ] 4.5 `IsTenantAlreadyExists(string code)` private helper: grep server source for the exact "already exists" code name — candidates include `TENANT_ALREADY_EXISTS`, `DUPLICATE_TENANT`, a 409-on-conflict synthetic code. Pin the exact literal after grep (`grep -r 'TENANT.*EXIST\|ALREADY.*TENANT\|DUPLICATE.*TENANT' src/Hexalith.Memories.Server/Tenants/`). If the server never emits a distinct code (e.g., always returns success on duplicate because tenant creation is truly idempotent server-side), skip the branch and treat all non-exception responses as "OK fresh create."
    - [ ] 4.6 **FALLBACK** if `MemoriesClient` has no tenant-create method (Risk #2): the provisioner prints the equivalent `curl` call as a console.Out line and returns `QuickstartTenantResult(Created: false, AlreadyExisted: false, ErrorCode: "CLIENT_METHOD_MISSING", Diagnostic: "Client tenant-create method not available; run manually: curl -X POST http://127.0.0.1:5000/api/tenants -H 'Content-Type: application/json' -d '{...}'")`. The wizard's step-4 logic then marks the step as `SKIP` (not `FAIL`) and the wizard continues. Document the missing client method in Completion Notes as follow-up work.

- [ ] Task 5: `QuickstartSampleFlow` (AC: #6, #11)
    - [ ] 5.1 Create `src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs`: `public sealed class QuickstartSampleFlow(MemoriesClient client)`. Public methods `IngestSampleAsync(string tenantId, CancellationToken ct)` returning `SampleIngestResult(bool Success, string? MemoryUnitId, string? ErrorCode, string Diagnostic)` and `ValidateSearchAsync(string tenantId, string memoryUnitId, CancellationToken ct)` returning `SampleValidationResult(bool Success, int ResultCount, double? TopScore, string Diagnostic)`.
    - [ ] 5.2 Embedded sample text: declare `private const string SampleDocumentText = "..."` with ~200 words of **fresh purpose-written English prose** describing what a memory system does in generic user-facing terms (Revision 0.2 — Paige finding — do NOT embed `_bmad-output/planning-artifacts/product-brief-*.md` which is an internal BMAD artifact whose context isn't self-evident to a new user). The prose must contain the deterministic keywords `"hybrid"`, `"search"`, `"memory"`, `"tenant"`, `"case"` used by the validation query. Example opener (tune at implementation time): *"This is a sample memory unit ingested by the `memories quickstart` wizard. It demonstrates the three-axis hybrid search pipeline — syntactic indexing, semantic vector embedding, and causal graph edges — all scoped to a tenant and case. ..."* Target length ~200 words; tone neutral-descriptive; serves double duty as documentation-by-example for any developer who lists the tenant's memory units after running the wizard.
    - [ ] 5.3 `IngestSampleAsync` implementation: extend `MemoriesClient` with `public virtual async Task<MemoryUnit> IngestAsync(string tenantId, string caseId, string content, IReadOnlyDictionary<string, string> metadata, CancellationToken ct)` — mirror the existing `HybridSearchAsync` pattern (`MemoriesClient.cs:109-141`) for request body + error handling + JSON deserialization. Route: confirm at implementation time (likely `POST /api/ingest` or `POST /api/tenants/{tenantId}/ingest`; read `Program.cs` route registration). Call with `tenantId` + case id (use `"quickstart-default"` or equivalent — create the case via a future method or let the server auto-create) + `SampleDocumentText` + metadata map `{"origin": "quickstart", "wizardVersion": "7.4"}`. On success, return `memoryUnit.Id` from the server response. Include the EXPERIMENTAL XML doc remark per TL;DR point 4.
    - [ ] 5.4 `ValidateSearchAsync` implementation: build query with a deterministic term from the sample (e.g., `"hybrid search"` — verified against the sample text). Loop up to 3 attempts with 2-second backoff between attempts (`await Task.Delay(TimeSpan.FromSeconds(2), ct)`). Each attempt: `await client.HybridSearchAsync(tenantId: tenantId, query: query, ...)`. Return `Success = true` when `result.Results.Count >= 1 AND result.Results.Any(r => r.MemoryUnitId == memoryUnitId)` — the sample must appear in the results (not just any result, since another test's fixture might have leftover data in the same tenant). If the sample's id is not in the top-K results after 3 attempts, return `Success = false` with diagnostic naming the top-K ids that DID return (debug aid).
    - [ ] 5.5 Timeout: use a per-attempt 10-second inner `CancellationTokenSource` layered on top of the caller's `ct` so a hung search doesn't block the whole step indefinitely. Total worst-case: 3 attempts × (10s inner + 2s backoff) = 36s — still within step budget.

- [ ] Task 6: `QuickstartCommand.ExecuteAsync` main loop (AC: #2, #6, #7, #8, #10)
    - [ ] 6.1 `private static async Task<int> ExecuteAsync(IServiceProvider services, ParseResult parseResult, CancellationToken ct)`. Resolve services: `CliCommandExecutor`, `CliConsole`, `PrerequisiteChecks`, `HealthProbe`, `QuickstartTenantProvisioner`, `QuickstartSampleFlow`.
    - [ ] 6.2 Parse options into `QuickstartOptions` (Task 1.6). Compute `tenantId = options.TenantId ?? $"quickstart-{DateTime.UtcNow:yyyyMMdd}"` — invariant date format in UTC, no culture dependency.
    - [ ] 6.3 `var stopwatch = Stopwatch.StartNew(); var results = new List<QuickstartStepResult>(); string? sampleMemoryUnitId = null;` Run the six steps sequentially:
      - Step 1: `await RunPrereqStepAsync(prereq, options, ct)` — unless `options.SkipPrereqCheck`, returns `QuickstartStepStatus.Skip` when skipped.
      - Step 2: `RunBootCommandStep(options)` — never fails (just prints the command string), returns `Ok` on real run or `DryRun` when `options.DryRun`.
      - Step 3: `await RunHealthProbeStepAsync(healthProbe, options, ct)` — unless `options.SkipBootCheck`, returns `Skip` when skipped.
      - Step 4: `await RunTenantProvisionStepAsync(tenantProvisioner, tenantId, options, ct)` — `DryRun` when `options.DryRun` (prints the API call without calling).
      - Step 5: `await RunIngestStepAsync(sampleFlow, tenantId, options, ct)` — returns `(QuickstartStepResult, string? memoryUnitId)`; captures `sampleMemoryUnitId = result.memoryUnitId` on success. `DryRun` path skips client call.
      - Step 6: `await RunValidationSearchStepAsync(sampleFlow, tenantId, sampleMemoryUnitId, options, ct)` — **cascade rule (Revision 0.2 — Amelia blocker):** if `sampleMemoryUnitId is null` (step 5 was Skip/DryRun/Fail), step 6 emits `Status = Skip` with message `"Skipped: no sample memory unit id from upstream step 5."` **BEFORE** calling `sampleFlow.ValidateSearchAsync` — never dereference a null id. `DryRun` path also skips client call.
    - [ ] 6.4 **Short-circuit rule:** if any step returns `QuickstartStepStatus.Fail`, subsequent steps are NOT run. Instead, they're added to `results` with `Status = Skip` and message `"Skipped due to upstream failure at step {failedStep.Id}."`. **Exception for step 2 (print boot command):** step 2 is pure stdout, no side effects — it ALWAYS runs regardless of step 1's outcome, so a failed prereq check still surfaces the boot command for the user to run after fixing prerequisites. Short-circuit applies to steps 3-6 only. This prevents cascading confusing errors (e.g., "validation search failed" when it was really "server never came up") while still delivering useful breadcrumbs.
    - [ ] 6.5 After all steps: `stopwatch.Stop(); var overallStatus = results.Any(r => r.Status == QuickstartStepStatus.Fail) ? "fail" : "ok"; var envelope = new QuickstartEnvelopeData(results, overallStatus, (int)stopwatch.ElapsedMilliseconds);`.
    - [ ] 6.6 Format-aware emission (per AC #8):
      - **Human:** for each step, write `$"[{r.Id}/6] {r.Title}"` to console.Out, then outcome line `$"[{r.Id}/6] {statusLabel}: {r.Message}"` (where statusLabel is `OK`/`FAIL`/`SKIP`/`DRY-RUN`) + optional suggestion `$"  Suggestion: {r.Suggestion}"` when present. Final line: `$"Quickstart {overallStatus} in {elapsed}s across {results.Count} steps."` to console.Out.
      - **JSON:** serialize the `CliOutputEnvelope<QuickstartEnvelopeData>` via source-gen registered type info (per Task 8) to console.Out. **Do NOT populate the envelope's top-level `Error` slot** (Revision 0.2 — Winston finding). Per-step failure context lives in `data.steps[N].ErrorCode / Message / Suggestion` — the top-level `error` slot stays null to preserve 7.3's mutual-exclusivity invariant for non-wizard commands. Consumers read `data.overallStatus == "fail"` and enumerate `data.steps[].Where(s => s.Status == Fail)` for failure details.
      - **Table:** print two-column header + separator, then one row per step `{r.Id,-5} | {statusLabel}`; detail lines (`OK: Docker reachable`) go to console.Error.
    - [ ] 6.7 Exit code: `overallStatus == "ok"` → `CliExitCodes.Success` (0); if any step is `Fail` and its `SuggestionCode` resolves via `ErrorMessageCatalog` to exit 1 → `CliExitCodes.DomainError`; else (plumbing, including transport/Docker/port/timeout) → `CliExitCodes.Plumbing`. Prefer the highest-priority exit code across all failing steps (catalog-resolved exit code trumps default).
    - [ ] 6.8 The whole `ExecuteAsync` body is wrapped in `executor.ExecuteAsync(CommandName, async (config, innerCt) => { ... }, ct)` just like the other 7.3 commands — gives us the FR56 transport-failure handling + token sanitization for free, AND registers the `CommandName` for the JSON-error envelope.
    - [ ] 6.9 `--dry-run` branch: every `Run*StepAsync` checks `options.DryRun` at the top. On dry-run: return `QuickstartStepStatus.DryRun` with a message describing what would have happened (e.g., `"Would POST /api/tenants with id='quickstart-20260416'."`). **No client calls made in dry-run mode** — this is the invariant enforced by Task 10.6.

- [ ] Task 7: `RootCommandFactory` wiring + stub-tag marker (AC: #9, #10)
    - [ ] 7.1 Modify `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:
      - Remove the `("quickstart", "Guided onboarding flow (quickstart wizard).", "7.4")` tuple from `CommandGroups` (line `RootCommandFactory.cs:42`).
      - Add `root.Subcommands.Add(QuickstartCommand.Build(services));` alongside the `tenant` / `config` / `search` wiring block at `RootCommandFactory.cs:60-77` — in the same relative position (before the `foreach` loop that registers remaining stubs).
    - [ ] 7.2 Modify `src/Hexalith.Memories.Cli/Commands/NotImplementedCommand.cs`:
      - In `Create`, after `var command = new Command(name, description);`, add `command.Tags.Add("stub");` — the tag is the marker used by `CliHelpCompletenessTests` (Task 9) to filter out stubs from the NFR30 audit.
    - [ ] 7.3 Update the `NotImplementedCommand.Create` XML doc summary to mention the tag: `"Helper that produces a placeholder command printing 'Not yet implemented — tracked in Story 7.X' to stderr and exiting with code <see cref='CliExitCodes.Plumbing'/>. Story 7.1 stubs most groups this way. Stubs are tagged with 'stub' so NFR30 help-completeness tests (Story 7.4) can exclude them."`
    - [ ] 7.4 Pre-audit any existing wired command description for the NFR30 test (Risk #6). Grep `src/Hexalith.Memories.Cli/Commands/*.cs` for `const string *Description` definitions. Verify each description contains `"Example"` (case-insensitive) AND at least one line starting with four spaces + `memories`. Fix any that miss (expected: 0-2 fixes). `ConfigShowCommand.cs:26` is a const `CommandName`, not a description — look for `ConfigShowCommandDescription` or similar. Root command description (`RootCommandFactory.cs:19-24`) already has an example; verify it satisfies the four-space-indent rule.
    - [ ] 7.5 **Stub-tag invariant test (Revision 0.2 — Bob finding):** add `tests/Hexalith.Memories.Cli.Tests/Cli/NotImplementedCommandTaggingTests.cs`. Single `[Fact]`: `NotImplementedCommand_Create_AlwaysTagsStub`. Body: call `NotImplementedCommand.Create(services, "test-name", "test-desc", "7.X")` with a minimal `ServiceCollection`; assert `result.Tags.Contains("stub")`. Prevents the loophole where a future contributor removes `command.Tags.Add("stub");` from Task 7.2 and `CliHelpCompletenessTests` silently starts auditing stubs (would fail with cryptic "stub command missing example" errors instead of a clear "stub factory must tag" failure). Trivial test; catches a real drift vector.

- [ ] Task 8: `CliJsonContext` + `CommandPayloadRegistry` for `QuickstartEnvelopeData` (AC: #8)
    - [ ] 8.1 Add `[JsonSerializable(typeof(QuickstartEnvelopeData))]` and `[JsonSerializable(typeof(CliOutputEnvelope<QuickstartEnvelopeData>))]` to `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs` alongside the existing entries. Ensure `QuickstartEnvelopeData` is a **reference type** (record class — confirmed by the `sealed record` declaration in Task 1.5) so the 7.3 `AllRegisteredEnvelopePayloadsAreReferenceTypes` invariant test (per 7.3 Task 2.2) passes.
    - [ ] 8.2 Add `QuickstartStepResult` and `QuickstartStepStatus` to the source-gen context too (they're nested in the envelope but source-gen requires explicit registration for full AOT safety). Configure `JsonStringEnumConverter<QuickstartStepStatus>` on the enum OR add a `[JsonConverter(typeof(JsonStringEnumConverter<QuickstartStepStatus>))]` attribute so the enum serializes as `"ok"|"fail"|"skip"|"dry-run"` — **lowercase with hyphen for `DryRun` → `"dry-run"`**. The enum name `DryRun` and serialized string `"dry-run"` differ; either use a custom naming policy in the source-gen options OR name the enum member `DryRun` and accept `"DryRun"` as the serialized form (adjust AC #8 expectation to match whichever you pick — document the choice in this task's Completion Notes).
    - [ ] 8.3 Register `QuickstartCommand.CommandName` → `typeof(QuickstartEnvelopeData)` in `src/Hexalith.Memories.Cli/Output/Formatters/CommandPayloadRegistry.cs` (7.3 Task 2.4 file). The executor's JSON-error dispatch uses this map to resolve the envelope's `TPayload` type generically.
    - [ ] 8.4 **Enum round-trip test (Revision 0.3 — .NET AOT expert finding):** add a `[Theory]` test in `tests/Hexalith.Memories.Cli.Tests/Cli/CliJsonContextTests.cs` (extend the 7.3 file) parameterized with each `QuickstartStepStatus` value: `Ok → "ok"`, `Fail → "fail"`, `Skip → "skip"`, `DryRun → "dry-run"`. Body: serialize a minimal `QuickstartEnvelopeData` carrying one step with the test's status, parse stdout back with `JsonDocument`, assert the `data.steps[0].status` string matches the expected kebab-case form. Round-trips the naming-policy + source-gen interaction which has historically been a gotcha for enum serialization under AOT contexts. If this test passes in CI, we know the wire format is stable — AOT regressions surface loudly rather than corrupting enum values silently.

- [ ] Task 9: `CliHelpCompletenessTests` + pre-audit (AC: #9)
    - [ ] 9.1 Create `tests/Hexalith.Memories.Cli.Tests/Cli/CliHelpCompletenessTests.cs`. Standard xUnit test class alongside existing 7.x test files. Use the same DI setup pattern (grep an existing test for `ServiceCollection` bootstrapping — e.g., `CliCommandExecutorTests`).
    - [ ] 9.2 `[Fact] public void EveryWiredCommand_HasAtLeastOneUsageExample()`. Build the root command: `var services = BuildServiceProvider(); var root = RootCommandFactory.Build(services, services.GetRequiredService<CliGlobalOptions>());`.
    - [ ] 9.3 Flatten the command tree: recursive descent. `IEnumerable<Command> Flatten(Command c) { yield return c; foreach (var sub in c.Subcommands) foreach (var nested in Flatten(sub)) yield return nested; }`.
    - [ ] 9.4 Filter: `.Where(c => !c.Tags.Contains("stub"))` (Task 7.2 tag marker). Also skip `HelpCommand` and `VersionCommand` built-ins via `c is not (System.CommandLine.Help.HelpCommand or System.CommandLine.VersionCommand)`.
    - [ ] 9.5 Assert per command:
      - `Assert.False(string.IsNullOrWhiteSpace(command.Description), $"Command '{commandPath}' has an empty description.");`
      - `Assert.Contains("Example", command.Description, StringComparison.OrdinalIgnoreCase);`
      - `Assert.Matches(new Regex(@"^\s{4}memories\b", RegexOptions.Multiline), command.Description);` (four-space-indented `memories` invocation)
      - Failure message names `commandPath` (e.g., `"memories quickstart"`). Build path by walking `.Parent` chain and prepending names.
    - [ ] 9.6 Pre-audit fixes (Risk #6): run the test locally before wiring the quickstart. If any wired command fails the audit, fix its description inline in this task — the fix is adding an example block following the `TenantListCommand.cs:23-27` pattern. Expected: 0-2 commands need fixing.

- [ ] Task 10: Unit tests (AC: #11)
    - [ ] 10.1 `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartPrerequisiteTests.cs`. Inject `FakeProcessRunner` (a simple test double implementing `IProcessRunner`, scripted per-test to return a canned `ProcessResult`). Tests: docker-success, docker-exit-nonzero, docker-timeout, dotnet-9-present, dotnet-only-8-installed, dotnet-parse-failure (advisory), port-all-available, port-one-in-use, os-detection-windows/linux/osx, dapr-present, dapr-missing-soft-fail.
    - [ ] 10.2 `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartHealthProbeTests.cs`. Use `TestDelegatingHandler` (7.2 Task 4.5) to script HTTP responses. Tests: immediate-200-ready, connection-refused-twice-then-200 (simulate boot), timeout-after-60-simulated-seconds (use `FakeTimeProvider` + `await Task.Delay` cooperation pattern — `TimeProvider.System` is too slow for tests), cancellation-respected. **Do NOT** use `Task.Delay` with real wall-clock time in unit tests.
    - [ ] 10.3 `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartTenantProvisionerTests.cs`. Mock `MemoriesClient` (or inject `TestDelegatingHandler` via `MemoriesClient` constructor if the client takes one — grep for the client's constructor signature). Tests: new-tenant-success, already-exists-returns-skip, unexpected-exception-returns-fail-with-code, network-transport-exception-bubbles-to-caller (the provisioner should NOT catch `HttpRequestException` — that's the executor's job per 7.3 ADR split).
    - [ ] 10.4 `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartSampleFlowTests.cs`. Tests: ingest-success, ingest-server-error, validation-search-immediate-success, validation-search-retry-then-success, validation-search-never-succeeds-three-retries (asserts 3 call attempts via mock).
    - [ ] 10.5 `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartCommandTests.cs`. Handler-level tests — inject all five helpers (`PrerequisiteChecks`, `HealthProbe`, `QuickstartTenantProvisioner`, `QuickstartSampleFlow`, and `CliCommandExecutor`) as test doubles. Tests: six-step-happy-path (all OK), step-1-fail-short-circuits-steps-2-6 (assert steps 2-6 are `Skip` with "upstream failure" message), per-format output matrix (human/json/table), JSON envelope schema validation (parse and assert field shapes), exit-code matrix (0 for ok, 1 for domain fail, 2 for plumbing fail).
    - [ ] 10.6 `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartDryRunTests.cs`. Verify `--dry-run` invariant: ZERO calls to `MemoriesClient`, `PrerequisiteChecks`, or `HealthProbe` side-effectful methods. Assert envelope has 6 steps all `Status == DryRun`, `overallStatus == "ok"`, exit code `0`.
    - [ ] 10.7 `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartSkipFlagTests.cs`. Tests: `--skip-prereq-check` → step 1 is `Skip`; `--skip-boot-check` → step 3 is `Skip`; both flags → steps 1 and 3 `Skip`, steps 2/4/5/6 `Ok`; step 2 is NEVER skipped (it's just a print).

- [ ] Task 11: Integration tests (AC: #1, #11)
    - [ ] 11.1 Create `tests/Hexalith.Memories.IntegrationTests/Cli/QuickstartDryRunIntegrationTests.cs`. Annotate `[Collection("AspireIngestionPipeline")]` and `[Trait("Category", "Integration")]` (7.2/7.3 convention). Test: invoke `QuickstartCommand.ExecuteAsync` (or the `MemoriesClient`-fronted path) with `--dry-run --format json` against the live fixture. Parse the JSON envelope. Assert `overallStatus == "ok"`, all 6 steps present, `elapsedMs > 0`. Since `--dry-run`, no side-effects on the fixture — safe to run in any order.
    - [ ] 11.2 Create `tests/Hexalith.Memories.IntegrationTests/Cli/QuickstartLiveIntegrationTests.cs`. Test: `Quickstart_AgainstLiveFixture_SucceedsWithinSixtySeconds`. Invoke the command with `--skip-prereq-check --skip-boot-check --tenant quickstart-test-<unique>` (unique id per test run to avoid cross-test interference). Assert `overallStatus == "ok"`, `elapsedMs < 60_000` (NFR31 CI-gated total bound per Risk #5), all 6 steps `Ok`.
        - **Per-step budgets (Revision 0.2 — Murat finding):** also assert `data.steps[N].durationMs < stepBudgetMs[N]` where the budgets are:
          - Step 1 (prereq check, skipped via flag): < 100ms
          - Step 2 (print boot command): < 100ms
          - Step 3 (health probe, skipped via flag): < 100ms
          - Step 4 (tenant provision): < 5_000ms (network + server state mutation)
          - Step 5 (sample ingest): < 10_000ms (full ingestion pipeline write)
          - Step 6 (validation search with up to 3 retries × 2s backoff + 10s each): < 40_000ms (worst case; typical < 5s on warm stack)
        - Rationale: without per-step budgets, a silent regression in one step (e.g., step 5 drifts from 2s to 8s) gets masked by headroom in other steps until total `elapsedMs` approaches 60_000. Per-step assertions isolate the culprit immediately. Budgets are generous — the intent is regression detection, not tight perf-gating. If a step legitimately slows down due to a new feature, update the budget in the same PR with a comment naming the change.
    - [ ] 11.3 **Do NOT spawn the `memories` binary as a subprocess** (per 7.1 anti-pattern #8 / 7.2 Task 10.3 / 7.3 Task 8.3). Invoke `QuickstartCommand.ExecuteAsync` directly via the DI container — same pattern as `CliErrorMessagesIntegrationTests` (7.3 Task 8).
    - [ ] 11.4 Cleanup verification (Revision 0.2 — Bob finding — make this an acceptance, not a hope): **read `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` at implementation time**. If `DisposeAsync` tears down tenants (or the fixture re-provisions per CI run), the live test's leaked `quickstart-test-<unique>` tenant is bounded — document the teardown mechanism in the test file's XML doc. If the fixture does NOT clean tenants, add a tracking note in Completion Notes (`"AspireIngestionPipelineFixture leaks tenants; quickstart live test inherits the leak. File follow-up for Phase 1.5 fixture hygiene."`) AND add a `try/finally` in the test body to call `tenantProvisioner.DeleteTenantAsync` if such a client method exists — do NOT add a new client method in 7.4 just for test cleanup. Worst case (no delete API, no fixture teardown): the fixture volume is re-created per CI run, so leakage is CI-scoped, not cross-run. Acceptable, but document the chain explicitly.

- [ ] Task 12: README rewrite (AC: #12)
    - [ ] 12.1 Edit `README.md`: rewrite the "Quick start" section (lines 5-14) to the NFR31-compliant walkthrough. Sections in order:
      1. **Prerequisites** (~30s to verify): Docker Desktop, .NET 9 SDK, git
      2. **Clone + submodules** (~2 min cold): `git clone`, `git submodule update --init --recursive`
      3. **Build** (~5 min first time, ~30s incremental): `dotnet build Hexalith.Memories.slnx`
      4. **Boot the stack** (~1 min first, ~20s subsequent): `dotnet run --project src/Hexalith.Memories.AppHost` (in a dedicated terminal)
      5. **Install the CLI** (~1 min first, ~10s subsequent): `dotnet pack src/Hexalith.Memories.Cli -c Release -o ./artifacts && dotnet tool install -g --add-source ./artifacts Hexalith.Memories.Cli`
      6. **Run the guided quickstart** (<1 min): `memories quickstart`
      7. **Total** (warm cache, CI-measured): **~10 min**; cold-start first-time: **~25-30 min**
    - [ ] 12.2 Each step gets one line on "if this fails, see docs/dev/quickstart.md#section-<id>" with an anchor — Task 13 creates those sections.
    - [ ] 12.3 Preserve the existing "Local development stack", "Useful endpoints", "CLI (preview)", "Operations", and any subsequent sections. Do NOT touch them except the additive sentence in "CLI (preview)" noted in Task 12.4.
    - [ ] 12.4 Append to the "CLI (preview)" section's opening paragraph: `"Story 7.4 wires the 'memories quickstart' guided wizard that verifies prerequisites, probes server health, provisions a sample tenant, ingests a sample document, and runs a validation search — completing in <30 minutes on a clean machine (NFR31) and in <60 seconds against a warm stack."` — one sentence, additive, matches the 7.2/7.3 sentence style.
    - [ ] 12.5 Timing annotations must be **measured**, not estimated — run the NFR31 CI walkthrough test at least once before finalizing the timings. If timings are off by more than 30%, update the README values in the same PR. **If measurements unavailable at PR time (Revision 0.2 — Paige finding):** use the literal marker `(approximate; to be measured by NFR31 walkthrough test)` appended to the duration estimate. NEVER ship bare numbers that weren't measured — degrades trust in every other NFR timing claim in the codebase. The approximate marker is a deliberate signal that the number is estimation, not measurement.
    - [ ] 12.6 **NFR31 manual-measurement cadence (Revision 0.3 — PM finding):** add a one-paragraph section to `docs/dev/quickstart.md` titled "Manual NFR31 walkthrough cadence." Owner: story 7.4 → maintainer baton to whoever picks up NFR regression ownership. Cadence: quarterly (or when cold-boot timing drifts noticeably in user feedback). Procedure: fresh Docker-Desktop-wipe, clean clone, stopwatch on `git clone` through first `memories search query` returning a result. Record the run in a `docs/dev/quickstart-walkthrough-log.md` appendix (create if absent; one line per run: date, machine class, total minutes, notes). This makes the "≤ 30 min" claim auditable against reality without CI gating. Not a blocker for story completion; surfacing the cadence in docs is the acceptance.

- [ ] Task 13: `docs/dev/quickstart.md` (AC: #13)
    - [ ] 13.1 Create `docs/dev/quickstart.md`. Sections:
      - **Overview** — what the wizard does and doesn't do (non-interactive, leaves state behind on failure, safe to rerun)
      - **Prerequisites** (mirrors README but deeper — .NET 9 install guides per OS, Docker Desktop install, optional DAPR CLI install)
      - **Per-step walkthrough** — what each step checks, what the outcome means, what to do on failure
      - **Failure decision tree** — for each step's common failure modes, a remediation action or file-an-issue link
      - **OS-specific notes**: Windows (port reservation caveat, Docker Desktop WSL2), macOS (Rosetta on Apple Silicon, Docker Desktop resources), Linux (rootless Docker impact on `docker ps` check)
      - **`--dry-run` mode** — when to use it, sample transcript
      - **`--format json` envelope** — schema + worked example + jq patterns (cross-reference `docs/dev/cli-output-formats.md`)
      - **When this is NOT the right command** — pointer to `dotnet run --project AppHost` for users who just want the stack up
    - [ ] 13.2 Add cross-reference from `docs/dev/cli-config.md` line 5: the existing sentence `"Stories 7.2–7.5 add output formats, rich error messages, a quickstart wizard, and telemetry."` — update to: `"Stories 7.2–7.3 added output formats and rich error messages. Story 7.4 wires the guided quickstart wizard (see quickstart.md). Story 7.5 adds telemetry."`.
    - [ ] 13.3 Add cross-reference from `docs/dev/cli-output-formats.md`: a new row in the "Per-command examples" table for `quickstart` showing the envelope shape with the step array.
    - [ ] 13.4 **Review cadence marker (Revision 0.2 — Paige finding):** prepend to `docs/dev/quickstart.md`: `"<!-- Review cadence: update when server error codes change, CLI flags change, or quarterly — whichever comes first. Last reviewed: 2026-04-16. -->"` Decision-tree docs rot fastest; the marker gives future maintainers a forcing function to re-read when this doc becomes stale, without forcing rewrites on every unrelated edit.

- [ ] Task 14: `tools/verify-cli-pack.{ps1,sh}` — quickstart smoke (AC: #10)
    - [ ] 14.1 `tools/verify-cli-pack.sh`: after the existing Story 7.3 error-translation smoke (`[5/6]`), add a new numbered step `[6/7] memories quickstart --help`. Assert exit code `0` and that stdout contains the literal string `"Example"`. Rationale: `--help` works with no server, so this verifies the command is packaged and the NFR30 help example is embedded. Update the step numbering accordingly ([7/7] becomes the uninstall step).
    - [ ] 14.2 `tools/verify-cli-pack.ps1`: mirror the same step in PowerShell syntax.
    - [ ] 14.3 **Do NOT** add a `memories quickstart --dry-run` smoke to the packaging script. **Real reason (Revision 0.2 — Bob finding, original reason was imprecise):** `--dry-run` skips server REST calls BUT the command still constructs the `MemoriesClient` at DI resolution time, which requires a resolvable endpoint via the 7.1 four-tier resolver. In the packaging script's context (no endpoint flag, no env var, no config file), the resolver falls back to `http://127.0.0.1:5000/` — which succeeds at resolution time but the wizard would then try to connect to a nonexistent local server. `--dry-run` doesn't skip the DI graph, only the per-step client calls. `--help` is the right smoke because it bypasses DI resolution entirely (System.CommandLine short-circuits on `--help`). Document this nuance inline so a future contributor doesn't "fix" what they think is an inconsistency.

## Dev Notes

### Inherited from Stories 7.1 + 7.2 + 7.3 (do not re-derive)

- **All ADRs from 7.1 (8), 7.2 (3), 7.3 (4)**. Especially relevant for 7.4:
  - **ADR-7.2-001 (envelope deprecation policy)** — `quickstart` adds a new `command`-scope envelope shape; additive under `schemaVersion = 1`.
  - **ADR-7.2-001 amendment (7.3)** — field-ordering convention: new optional fields appended, never inserted. 7.4 doesn't change top-level envelope fields; only adds a new `data` payload type.
  - **ADR-7.3-001 (synthetic CLI error codes)** — any quickstart step that bubbles a transport/local error uses the synthetic code convention. Wizard-specific synthetic codes: `QUICKSTART_STEP_FAILED` (generic step failure), `DOCKER_UNAVAILABLE`, `DOTNET_VERSION_INSUFFICIENT`, `PORT_IN_USE`, `SERVER_NOT_READY`, `SAMPLE_VALIDATION_ZERO_RESULTS`. These live in the catalog as domain-style entries (exit code 1) if the failure is actionable by the user's input, or plumbing (exit code 2) if it's environmental (Docker, port, server not ready).
  - **ADR-7.3-002 (JSON-mode errors on stdout)** — the quickstart's JSON envelope goes to stdout on success AND failure; stderr empty in JSON mode. **Exception** for this command: the envelope's `Error` slot AND `Data` slot can both be populated (per ADR-7.4-003) because per-step diagnostic context is inseparable from the failure. Document this exception in ADR-7.4-003 below.
  - **ADR-7.3-003 (domain vs plumbing exit code split)** — applied per step: Docker/port/server-not-ready → plumbing (2); `TENANT_FAILED` / `INVALID_INPUT` during tenant create → domain (1).
  - **ADR-7.3-004 (ErrorMessageCatalog is static, not DI)** — `PrerequisiteChecks`, `HealthProbe`, `QuickstartTenantProvisioner`, `QuickstartSampleFlow` ARE DI-registered (they have state / dependencies), but the catalog they consult stays static.
- **7.1 + 7.2 + 7.3 anti-patterns (30 total) — most relevant to 7.4:**
  - 7.1 #8 (no `memories` subprocess in CI) — Task 11.3 pinning.
  - 7.1 #12 (never log or emit the token) — quickstart's final elapsed-time summary and per-step diagnostics must NOT echo tokens; rely on executor's `SanitizeText`.
  - 7.1 #14 (no emoji in formatter output) — wizard output is plain ASCII labels (`OK`, `FAIL`, `SKIP`, `DRY-RUN`, `[1/6]`). No checkmark emoji, no colored glyphs.
  - 7.2 #1 (no 7.4/7.5 work leaks into earlier stories) — the inverse is also true: 7.4 does NOT touch 7.5 territory (no telemetry, no audit logging).
  - 7.3 #1 (raw `console.Error.WriteLine` for server-error paths) — quickstart's step-failure paths route through the executor's `WriteFormattedError` via `ErrorMessageCatalog.Resolve`, NOT raw writes.
  - 7.3 #7 (write JSON envelope to stdout BEFORE verbose stderr) — applies here: the 6-step envelope must be one JSON document written in one `JsonSerializer.Serialize` call; no interleaved verbose writes during JSON emission.
- **Implementation contracts from 7.1 + 7.2 + 7.3:** command-name plumbing (`ExecuteAsync(commandName, ..., ct)` overload — 7.3 Task 3.8); JSON envelope stdout-only; per-format dispatch in command handlers (not formatters — 7.3 Tasks 4/5); `CommandPayloadRegistry` dispatches source-gen types per command (7.3 Task 2.4); `CliHelpCompletenessTests` tag-filter for stubs (new in 7.4 — reusable by future stories).

### New architectural decisions (locked in this story)

**ADR-7.4-001 — Quickstart prints the boot command; does not spawn subprocesses.**
- **Decision:** The wizard's step 2 prints `"Run in a dedicated terminal: dotnet run --project src/Hexalith.Memories.AppHost"` and moves on. It does NOT spawn the subprocess. Step 3 (health probe) then polls the endpoint until ready, assuming the user ran step 2's suggested command in parallel.
- **Rationale:** Spawning `dotnet run` from a `dotnet tool global` CLI couples CLI state to build-tool state, blocks on an indefinitely-running process (AppHost doesn't exit until killed), and produces ambiguous failure modes (was the failure in my subprocess or the user's environment?). Print-then-poll makes the contract explicit: the user owns the AppHost process; the wizard owns the readiness check.
- **Trade-off:** The user has to coordinate two terminals. Acceptable: developers already do this for most multi-service workflows. The print-explicit-command pattern is the common convention (Kubernetes CLI, Docker Compose instructions, most Aspire docs).
- **Reconsider at:** If a future UX study shows users fail step 2 more than step 3 (misreading the "run this" instruction), add an `--interactive` flag that offers to spawn for them. Don't default to it.
- **Wizard-vs-sample UX footnote (Revision 0.2 — John finding):** the PRD (`prd.md:784`) references `samples/01-quickstart/` as a future onboarding artifact. Story 7.4 ships the wizard path per epics.md explicit scope; the sample-clone path remains a follow-up. **If post-7.4 developer feedback shows sample-clone onboarding outperforms the wizard** (measured by time-to-first-search on first-time developers), 7.4 becomes a deprecation candidate rather than a companion — the sample story becomes the successor. Pre-committing to both would double the onboarding-surface maintenance burden without evidence either is dominant. Track this as a Phase 1.5 decision gate, not a 7.4 concern.

**ADR-7.4-002 — Wizard is non-interactive by default.**
- **Decision:** `memories quickstart` runs unattended — no `Press Enter to continue` prompts, no Y/N confirmations. Each step runs to completion, prints its outcome, and moves on. Failures halt subsequent steps via the short-circuit rule (Task 6.4).
- **Rationale:** Interactive wizards inflate the test surface (TTY-dependent code paths, mocking `Console.ReadLine`), break scripting (`memories quickstart | tee quickstart.log` requires non-interactive), and don't actually improve the UX for developers who are already familiar with their terminal. The six-step output is self-documenting.
- **Trade-off:** No graceful "wait for user to fix X then continue" loop. Acceptable: a failed wizard rerun is trivial (same command again) after the user fixes the problem.
- **Reconsider at:** If user-research (unavailable in Phase 1) shows first-time developers get stuck at the failure point without a prompt-based guide, add an `--interactive` opt-in.

**ADR-7.4-003 — Quickstart encodes per-step failures inside `data.steps[]`, preserving 7.3 mutual-exclusivity.** *(Revision 0.2 — Winston finding.)*
- **Decision:** The `CliOutputEnvelope<QuickstartEnvelopeData>` NEVER populates the top-level `Error` slot. Per-step failures are encoded inside `data.steps[N]` via the new `ErrorCode` field (added to `QuickstartStepResult` in Task 1.4) plus the existing `Message` and `Suggestion` fields. The 7.3 `Debug.Assert((Data is null) != (Error is null))` invariant in `CliOutputEnvelope` stays **untouched** and continues to hold for every command.
- **Rationale:** For short-lived commands (`tenant list`, `config show`), "either success data OR error" is the clean contract. For a multi-step wizard, the per-step data IS the error context — knowing WHICH step failed is essential to debugging. **Original draft** proposed relaxing the invariant to allow both slots populated; this was rejected in Revision 0.2 because a single wizard-specific convenience erodes the contract for all other commands and future contributors lose the "always exactly one" safety net. **The right place** to carry step-level failure detail is inside the step records themselves, not at the envelope level. Consumers already enumerate `data.steps[]` for the happy path; adding `.ErrorCode` to each step costs zero cognitive overhead — they already know the step array is the source of truth.
- **Implementation contract:**
  - `QuickstartStepResult.ErrorCode` is nullable string (Task 1.4).
  - Set to the catalog-resolved code on `MemoriesRemoteException` failures (`ErrorMessageCatalog.Resolve(ex.Error.Code).Code` or the server's code verbatim).
  - Set to a synthetic code on local failures (`"DOCKER_UNAVAILABLE"`, `"DOTNET_VERSION_INSUFFICIENT"`, `"PORT_IN_USE"`, `"SERVER_NOT_READY"`, `"SAMPLE_VALIDATION_ZERO_RESULTS"`).
  - Null when `Status != Fail` (Ok / Skip / DryRun steps have no error code).
  - `Message` and `Suggestion` carry the human-readable text and next-action hint (same role they play in top-level `Error` for other commands).
- **Consumer contract:** JSON consumers read `data.overallStatus`; if `"fail"`, enumerate `data.steps[].Where(s => s.Status == "fail")` for the failing steps' details. Exit code is authoritative: `0`/`1`/`2`/`130` per AC #2 and ADR-7.3-003. Scripts using `jq`: `memories quickstart --format json | jq -e '.data.overallStatus == "ok"'` for pass/fail gate; `jq '.data.steps[] | select(.status == "fail") | {step: .id, code: .errorCode, why: .message, fix: .suggestion}'` for diagnostics.
- **Reconsider at:** If additional multi-step commands emerge (Phase 1.5 might add `memories consistency verify` or similar), keep the same pattern — step-level failure context inside `data.steps[]`, not at the envelope. Formalize as a named `IMultiStepPayload` marker interface only if more than two commands adopt the pattern.

**ADR-7.4-004 — Quickstart is idempotent; no state cleanup.**
- **Decision:** Running `memories quickstart` twice on the same day with default tenant id produces the same end state: one sample tenant, one sample memory unit (not two). The wizard detects pre-existing state (existing tenant via server conflict code) and reports step 4 as `SKIP` rather than `FAIL`. Step 5 may or may not also skip depending on whether the previous run's memory unit still exists; since the sample content is deterministic, re-ingesting it would produce a new memory unit with a different id (the server generates ids), so step 5 creates a new one each run — a harmless leak of ~1KB per run. No cleanup is performed.
- **Rationale:** Cleanup-on-run would require a teardown step (delete tenant, delete memory units) that could fail or race with concurrent wizard runs in multi-developer setups. Cleanup-on-exit (IDisposable-like) would complicate the wizard's already-linear flow and make `--dry-run` semantics confusing (would dry-run also teardown?). The "leave a tiny breadcrumb" model is common for onboarding wizards (Terraform Cloud, Vercel CLI, etc.) and aligns with the "let users poke around" spirit of the wizard.
- **Trade-off:** A developer who runs `memories quickstart` daily accumulates 1 new memory unit per run (~30/month). The sample tenant itself is reused. Tenant-level cleanup is a separate story (Phase 1.5 `memories tenant delete`).
- **Reconsider at:** If cumulative test state becomes a pain point (CI runs producing hundreds of sample units per week), add a `--cleanup-previous` flag that deletes prior runs' sample units before ingesting a new one. Defer until real usage surfaces the need.

**ADR-7.4-005 — On quickstart failure, leave state in place.**
- **Decision:** If step 4 or later fails (partial state committed), the wizard does NOT undo the partial state. The developer is left with whatever got created up to the failure point (tenant exists but no memory unit, or tenant + memory unit exists but search can't find it).
- **Rationale:** Leaving partial state allows the developer to inspect with `memories tenant list`, `memories search query --tenant quickstart-...`, `memories config show`, etc. to diagnose why the wizard failed. Automatic rollback would hide the failure state and make troubleshooting harder. The developer can always `memories quickstart` again after fixing the root cause (idempotent per ADR-7.4-004).
- **Trade-off:** If the wizard fails deep into step 5 and the developer never reruns, the partial tenant leaks. Acceptable: it's a single tenant named `quickstart-YYYYMMDD`, visible in `tenant list`, deletable by any teardown script the developer writes.
- **Reconsider at:** Same as ADR-7.4-004 — if leakage becomes a real problem, add opt-in cleanup behavior (never default-on).

### Hand-off to Story 7.5 (telemetry)

**Revision 0.3 — Time Traveler-Future finding:** Story 7.5 owns search/access telemetry (FR67 — OpenTelemetry correlation IDs, structured logs, custom metrics). When 7.5 instruments the wizard:

- **Telemetry source for per-step timings:** `QuickstartStepResult.Duration` (and its serialized `durationMs` field in the JSON envelope, per AC #8). 7.5 should consume these values directly — do NOT re-measure with a parallel instrumentation layer. Proposed 7.5 metrics: `memories.quickstart.step.duration{step_id, status}`, `memories.quickstart.total.duration{overall_status}`, `memories.quickstart.step.failure_count{step_id, error_code}`.
- **Error code propagation:** `QuickstartStepResult.ErrorCode` (per Revision 0.2 — Winston finding) is the stable failure taxonomy for the wizard. 7.5's audit-log schema should pin on this field for `quickstart` command rows — same treatment as `ErrorResponse.Code` for other commands.
- **Do not export the envelope's `data.steps[]` to external systems verbatim** — it contains user-facing prose (`Message`, `Suggestion`) that's CLI-local. 7.5 telemetry extracts structured fields (`step_id`, `status`, `duration_ms`, `error_code`) only.
- **Avoid double-instrumentation:** if 7.5 wraps the CLI entry point in OpenTelemetry ActivitySource, the wizard's `Duration` measurements must not include the outer activity's own overhead. 7.5 design should measure wizard duration independently OR subtract activity-span overhead from the envelope's `elapsedMs`.

Setting this hand-off up now prevents 7.5 from rebuilding wizard-specific measurement from scratch.

### Repo state the dev agent must rely on

- Stories 7.1, 7.2, 7.3 are `status: done`. The dev agent extends them — does not rebuild any 7.1/7.2/7.3 file from scratch except the explicitly enumerated touch-points: `README.md` (Task 12), `RootCommandFactory.cs` (Task 7.1), `NotImplementedCommand.cs` (Task 7.2 — add `"stub"` tag), `CliJsonContext.cs` (Task 8.1), `CommandPayloadRegistry.cs` (Task 8.3), `CliOutputEnvelope.cs` (relax `Debug.Assert` per ADR-7.4-003), `docs/dev/cli-config.md` (Task 13.2 — one line edit), `docs/dev/cli-output-formats.md` (Task 13.3 — new table row), `tools/verify-cli-pack.{ps1,sh}` (Task 14).
- `MemoriesClient` (`src/Hexalith.Memories.Client.Rest/`) may have `CreateTenantAsync`, `IngestAsync`, `HybridSearchAsync`, `GetHealthAsync` — **grep at implementation time** and list the exact signatures used in Completion Notes. If any method is missing, use the Task 4.6 / Task 5.3 fallback patterns.
- `ErrorMessageCatalog.Resolve(code)` exists since 7.3 and handles unknown codes gracefully (returns default translation with exit code 1). The wizard consumes this directly for any `MemoriesRemoteException` it surfaces.
- `CliCommandExecutor.ExecuteAsync(commandName, handler, ct)` (2-arg overload from 7.3 Task 3.8) is the entry point — quickstart wraps its entire body in one invocation.
- `CliConsole` (from 7.1) exposes `Out`, `Error`, `Format`, `Verbose` — quickstart writes step output to `Out` (human) or via envelope serialization (JSON) or to `Error` (table detail).
- `AspireIngestionPipelineFixture` (from 7.1 Task 7.2) already bootstraps the full stack for integration tests; 7.4's live integration test reuses it.
- **AppHost port binding (Revision 0.3 — Aspire expert finding):** `src/Hexalith.Memories.AppHost/Program.cs:55-56` intentionally omits `AppPort` on the memories-server project — this lets Aspire Testing fixtures auto-randomize ports for parallel test runs. **In local dev**, the server inherits `src/Hexalith.Memories.Server/Properties/launchSettings.json` which binds `http://localhost:5000`. The wizard's default endpoint resolution (7.1 four-tier, ultimate fallback `http://127.0.0.1:5000`) matches this. If a future contributor changes `launchSettings.json` or adds `.WithEndpoint(port: 5000, ...)` to the AppHost, the wizard stays correct; if the server's launch port changes, the wizard's default endpoint breaks silently. Document this dependency in `docs/dev/quickstart.md` (Task 13.1 "Troubleshooting" section) so a user who gets step 3 connection-refused knows to check the Aspire dashboard for the actual port.
- `TestDelegatingHandler` (from 7.2 Task 4.5) scripts HTTP responses for `MemoriesClient` — reuse for `QuickstartHealthProbeTests` and `QuickstartTenantProvisionerTests`.
- `FakeTimeProvider` (from Microsoft.Extensions.TimeProvider.Testing) — if not already referenced, add to `Hexalith.Memories.Cli.Tests.csproj`. Required for testing `HealthProbe` timeouts without real-time `Task.Delay`.
- `samples/` folder DOES NOT EXIST yet (verify with `ls samples/` — expect "No such file or directory"). 7.4 does NOT create it (see "What does NOT ship" point 3).

### Task dependency sketch (for parallelizing dev agents)

The 14 Tasks run roughly in parallel streams:

- **Stream A (substrate):** Task 1 (scaffold), Task 2 (prereq checks), Task 3 (health probe) — all independent. Start here.
- **Stream B (flow helpers, needs Stream A):** Task 4 (tenant provisioner), Task 5 (sample flow) — depend on Task 1 (types) but parallel to each other.
- **Stream C (wiring, needs Streams A + B):** Task 6 (main loop), Task 7 (RootCommandFactory), Task 8 (JSON context). Task 6 is sequential; 7 and 8 parallel.
- **Stream D (tests + help audit, needs Streams A-C):** Task 9 (help completeness + pre-audit), Task 10 (unit tests), Task 11 (integration tests).
- **Stream E (docs, needs Stream C):** Task 12 (README), Task 13 (docs/dev/quickstart.md), Task 14 (packaging smoke). Parallel with Stream D.

Sequential execution (1 → 2 → 3 → ... → 14) is valid and simpler. LLM dev agents should default to linear.

### Anti-patterns to avoid (7.4-specific, layered on top of 7.1/7.2/7.3)

1. **Spawning `dotnet run --project AppHost` from the wizard.** Pinned by ADR-7.4-001. Print the command and poll for readiness; never spawn.
2. **Adding `Console.ReadLine` / `Spectre.Console.Prompt` for interactive confirmations.** Pinned by ADR-7.4-002. Wizard is non-interactive.
3. **Creating a `samples/` folder.** Explicitly out of scope. PRD line 784 reference is for a future story.
4. **Spawning the `memories` binary as a subprocess in tests.** Pinned by 7.1 anti-pattern #8 / 7.2 Task 10.3 / 7.3 Task 8.3. Invoke the command handler via DI.
5. **Wiring `memories tenant create` as a CLI subcommand.** 7.4 uses the existing server endpoint via `MemoriesClient` directly. `tenant create` stays un-wired (7.3's `TenantListCommand.WriteEmptyTenantsNudge` REST-API fallback wording remains correct).
6. **Hardcoding real-time `Task.Delay` in unit tests.** Use `FakeTimeProvider`. Wall-clock delays inflate test time and flake under load.
7. **Reading sample document text from disk at runtime.** Embed as a `const string SampleDocumentText`. File-IO dependency inflates packaging + test surface.
8. **Removing `"stub"` tag on NotImplementedCommand without updating the help audit filter.** The tag is the contract between `NotImplementedCommand` and `CliHelpCompletenessTests`. If the tag format changes, update the test in the same PR.
9. **Making quickstart step 4's tenant id include time-precise UTC seconds.** The daily granularity (`quickstart-YYYYMMDD`) is intentional for idempotency. A timestamp-precise id (`quickstart-YYYYMMDDTHHmmss`) would force every run to create a new tenant, breaking ADR-7.4-004.
10. **Interleaving stderr/stdout writes during JSON-mode output.** Pinned by 7.3 anti-pattern #7. Write the envelope in one `JsonSerializer.Serialize` call; no per-step stderr writes during JSON emission.
11. **Requiring Docker to be running for the unit tests.** Unit tests use `IProcessRunner` fakes and mocked `MemoriesClient`. Docker is only required for the `[Trait("Category", "Integration")]` tests, same as 7.1/7.2/7.3.
12. **Prompting the developer for their API token in quickstart.** The token resolution is owned by the four-tier config resolver (7.1). Quickstart uses whatever the resolver returns. If no token is set and the endpoint requires one, the wizard fails at step 3 (health probe) with the 7.3 TLS-or-auth error surface. Adding a token prompt duplicates the resolver's job.

### Testing approach

- **Unit tests (Tier 1)** — `QuickstartPrerequisiteTests`, `QuickstartHealthProbeTests`, `QuickstartTenantProvisionerTests`, `QuickstartSampleFlowTests`, `QuickstartCommandTests`, `QuickstartDryRunTests`, `QuickstartSkipFlagTests`, `CliHelpCompletenessTests`. No Docker required. Use `FakeTimeProvider`, `TestDelegatingHandler`, `IProcessRunner` fakes.
- **Integration tests (Tier 3)** — `QuickstartDryRunIntegrationTests`, `QuickstartLiveIntegrationTests`. Require Docker + Aspire fixture. Marked `[Trait("Category", "Integration")]` so local dev can filter out.
- **Regression guards** — existing `ConfigShowGoldenFileTests`, `TenantListFormatterTests`, `TokenRedactionTests`, `ErrorCatalogDriftTests` MUST still pass. The only test that may need updating is `CliJsonContextTests.AllRegisteredEnvelopePayloadsAreReferenceTypes` — verify `QuickstartEnvelopeData` is a reference type (it is — `sealed record` without `struct` keyword).
- **NFR31 CI gate** — `QuickstartLiveIntegrationTests` asserts `elapsedMs < 60_000`. This is the machine-enforceable portion; the 30-min human-story bound is documented in README/docs but not CI-gated (Risk #5).
- **NFR30 CI gate** — `CliHelpCompletenessTests.EveryWiredCommand_HasAtLeastOneUsageExample` asserts every wired command has at least one four-space-indented `memories` example in its description. Stub commands excluded via `"stub"` tag.

### Definition of Done

1. `src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs` exists with `CommandName = "quickstart"` and a six-step wizard implementation (prereq check, boot command print, health probe, tenant provision, sample ingest, validation search).
2. `src/Hexalith.Memories.Cli/Quickstart/*.cs` contains the four helpers (`PrerequisiteChecks`, `HealthProbe`, `QuickstartTenantProvisioner`, `QuickstartSampleFlow`) + shared types (`QuickstartStepResult`, `QuickstartStepStatus`, `QuickstartEnvelopeData`, `IProcessRunner` / `DefaultProcessRunner`).
3. `RootCommandFactory.cs` wires `QuickstartCommand.Build(services)` as an explicit subcommand (removed from `CommandGroups` stubs). `NotImplementedCommand.Create` tags stubs with `"stub"` for the help audit filter.
4. `CliJsonContext.cs` registers `QuickstartEnvelopeData`, `QuickstartStepResult`, `QuickstartStepStatus` (with lowercase JSON naming convention). `CommandPayloadRegistry.cs` maps `"quickstart" → typeof(QuickstartEnvelopeData)`.
5. `CliOutputEnvelope.cs` is **unchanged** — the 7.3 mutual-exclusivity `Debug.Assert` remains in force (Revision 0.2 — Winston finding). Per-step failure detail lives in `QuickstartStepResult.ErrorCode` + `Message` + `Suggestion`, not in the envelope's top-level `Error` slot.
6. `memories quickstart` (no flags) runs the six-step wizard successfully against a running stack; exit code `0`; elapsed time < 60s on warm stack.
7. `memories quickstart --dry-run --format json` emits a single JSON envelope with six steps all `status: "dry-run"`, `overallStatus: "ok"`, exit code `0`, NO calls to the server.
8. `memories quickstart --help` displays the command's description, four flags, global options, and at least one example. Satisfies NFR30.
9. `CliHelpCompletenessTests.EveryWiredCommand_HasAtLeastOneUsageExample` passes for all wired commands (pre-audit fixes applied in Task 7.4).
10. Unit tests cover per-step happy path, per-step failures, per-format output (human/json/table), exit-code matrix (0/1/2), `--dry-run` invariant (zero client calls), `--skip-prereq-check` / `--skip-boot-check` honored.
11. Integration tests: `QuickstartDryRunIntegrationTests` passes against fixture; `QuickstartLiveIntegrationTests.Quickstart_AgainstLiveFixture_SucceedsWithinSixtySeconds` asserts `elapsedMs < 60_000` and `overallStatus == "ok"`.
12. `README.md` Quick start section rewritten to the NFR31-compliant walkthrough with CI-measured timings. "CLI (preview)" section has the additive 7.4 sentence.
13. `docs/dev/quickstart.md` exists and covers: per-step walkthrough, failure decision tree, OS-specific notes, `--dry-run` mode, `--format json` envelope, cross-references.
14. `tools/verify-cli-pack.{ps1,sh}` has a `memories quickstart --help` smoke that exits `0` and confirms the example block is packaged.
15. `dotnet build Hexalith.Memories.slnx` clean (0 warnings, 0 errors under `TreatWarningsAsErrors=true`). **All 7.1/7.2/7.3 tests pass without modification** (except `CliJsonContextTests.AllRegisteredEnvelopePayloadsAreReferenceTypes` which extends to cover `QuickstartEnvelopeData`). Token-redaction sentinel never appears in any quickstart output.
16. `sprint-status.yaml` transitions `7-4-quickstart-and-documentation: backlog → ready-for-dev → in-progress → done`.

### References

- Epic 7 overview and Story 7.4 acceptance criteria: [Source: `_bmad-output/planning-artifacts/epics.md:1473-1496`]
- Epic 7 objective ("polished CLI, <30 min onboarding" — Gate 3): [Source: `_bmad-output/planning-artifacts/epics.md:1381-1383`]
- **NFR30** (CLI help completeness + example-per-command): [Source: `_bmad-output/planning-artifacts/prd.md:1006`]
- **NFR31** (README quickstart <30 min on clean machine): [Source: `_bmad-output/planning-artifacts/prd.md:1007, 177`]
- FR53 (CLI command surface): [Source: `_bmad-output/planning-artifacts/prd.md:899`]
- FR57 (discoverable actions, no dead-end states — owned by 7.3 and consumed here): [Source: `_bmad-output/planning-artifacts/prd.md:903`]
- Guided-quickstart command in the MCP/CLI matrix: [Source: `_bmad-output/planning-artifacts/prd.md:738, 759`]
- `samples/01-quickstart/` reference (out of 7.4 scope; follow-up): [Source: `_bmad-output/planning-artifacts/prd.md:784`]
- Architecture CLI specification (distribution, config layering): [Source: `_bmad-output/planning-artifacts/prd.md:743-776`]
- Story 7.3 `NotImplementedCommand` stub for `quickstart` (to be replaced): [Source: `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs:42`]
- `NotImplementedCommand.Create` (where to add `"stub"` tag): [Source: `src/Hexalith.Memories.Cli/Commands/NotImplementedCommand.cs:26-38`]
- Example descriptions pattern (for NFR30 audit): [Source: `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs:20-27`, `src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs:49-56`]
- Command-name plumbing convention (7.3): [Source: `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs:42`, `src/Hexalith.Memories.Cli/Execution/CliCommandExecutor.cs` `ExecuteAsync(commandName, handler, ct)` overload]
- `ErrorMessageCatalog.Resolve` (7.3): [Source: `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`]
- `MemoriesClient` to extend (grep for `CreateTenantAsync` / `IngestAsync` / `HybridSearchAsync` / `GetHealthAsync`): [Source: `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`]
- Server tenant-provisioning endpoint (consumed via client): [Source: `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs`]
- Server health endpoint: [Source: `src/Hexalith.Memories.Server/Program.cs` — `/health`, `/alive`, `/ready` are wired via Aspire ServiceDefaults]
- `CliOutputEnvelope` + mutual-exclusivity assertion (to relax for 7.4): [Source: `src/Hexalith.Memories.Cli/Output/Json/CliOutputEnvelope.cs`]
- `CliJsonContext` source-gen registration: [Source: `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs`]
- `CommandPayloadRegistry` (to add `"quickstart"` entry): [Source: `src/Hexalith.Memories.Cli/Output/Formatters/CommandPayloadRegistry.cs`]
- 7.2 docs to extend with quickstart row: [Source: `docs/dev/cli-output-formats.md`]
- 7.1 docs to cross-reference: [Source: `docs/dev/cli-config.md:5`]
- Existing README Quick start section (to rewrite): [Source: `README.md:5-14`]
- Existing README "CLI (preview)" section (additive edit): [Source: `README.md:38-56`]
- `AspireIngestionPipelineFixture`: [Source: `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`]
- `TestDelegatingHandler`: [Source: `tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs`]
- Previous story (for inherited ADRs and anti-patterns): [Source: `_bmad-output/implementation-artifacts/7-3-actionable-error-messages-and-discoverable-actions.md`]
- Packaging verification scripts (add `quickstart --help` smoke): [Source: `tools/verify-cli-pack.sh`, `tools/verify-cli-pack.ps1`]

## Dev Agent Record

### Agent Model Used

claude-opus-4-6[1m]

### Debug Log References

- Story created from current repo state on 2026-04-16.
- Target story selected automatically from `_bmad-output/implementation-artifacts/sprint-status.yaml` — first backlog entry was `7-4-quickstart-and-documentation`.
- Epic 7 status already `in-progress` (Story 7.1 transitioned it on creation; Story 7.3 just moved to `review` with the remaining backlog items being 7.4 and 7.5). No epic status change required.
- Previous story 7.3 is at `status: review` (per sprint-status.yaml line 114) — its dev notes, ADRs, anti-patterns, and implementation patterns are authoritative and inherited here without re-derivation.
- Verified the `quickstart` stub registration at `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs:42` — `CommandGroups` tuple `("quickstart", "Guided onboarding flow (quickstart wizard).", "7.4")` is the exact entry to remove when 7.4 wires the real command.
- Verified the existing `NotImplementedCommand.Create` at `NotImplementedCommand.cs:26-38` — it prints `"Not yet implemented — tracked in Story 7.X."` to stderr + exits `CliExitCodes.Plumbing`. The `"stub"` tag addition in Task 7.2 is a 1-line change (`command.Tags.Add("stub");` after `var command = new Command(...)`).
- Verified 7.3's `TenantListCommand.WriteEmptyTenantsNudge` references `memories quickstart` as the FR57 "coming soon" pointer (`TenantListCommand.cs:70-73`) — the 7.4 wizard fulfills that promise.
- Verified 7.3's `SearchQueryCommand.EmptyTenantNudge` and `EmptyQueryNudge` both reference `memories quickstart` (`SearchQueryCommand.cs:32-47`) — same "coming soon" pattern.
- AppHost project name verified: `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`. The quickstart's step-2 printed command uses this exact path.
- Existing docs structure confirmed: `docs/dev/cli-config.md` (7.1), `docs/dev/cli-output-formats.md` (7.2/7.3). `docs/dev/quickstart.md` is the new file for 7.4.
- Server health endpoints verified from `README.md:32-36`: `http://localhost:5000/health`, `/alive`, `/ready`. The quickstart uses `/health` (readiness).

### Completion Notes List

<!-- To be populated by the dev agent during implementation -->

### File List

<!-- To be populated by the dev agent during implementation -->

### Change Log

| Date | Version | Description |
| :--- | :--- | :--- |
| 2026-04-16 | 0.1 | Story context created. Status: backlog → ready-for-dev. |
| 2026-04-16 | 0.2 | Post-party-mode review revision: 13 findings applied (Bob/Amelia/Winston/Murat/John/Paige). **Major win — Winston finding (ADR-7.4-003 redesign):** dropped the mutual-exclusivity relaxation; per-step failures now encoded in `QuickstartStepResult.ErrorCode` inside `data.steps[]` rather than the envelope's top-level `Error` slot — preserves 7.3 `Debug.Assert` invariant for all commands. **Blocker fix — Amelia (Task 6.3):** step 6 now explicitly auto-skips when `sampleMemoryUnitId is null` from upstream step-5 failure, preventing null-dereference. **Client-method pinning — Amelia:** grep confirmed `MemoriesClient.ProbeHealthAsync` EXISTS (no new method needed); `CreateTenantAsync` and `IngestAsync` ABSENT — 7.4 takes scope to add both as minimal client wrappers rather than degrading to curl-instruction mode. **Content fix — Paige (Task 5.2):** sample text is now purpose-written fresh prose with pinned validation keywords, not the product-brief excerpt. **Quality fixes:** Task 7.5 adds `NotImplementedCommandTaggingTests` (closes stub-tag loophole); Task 11.2 adds per-step `durationMs` budgets (prevents silent regression masking in total elapsed); Task 11.4 makes fixture-teardown verification an acceptance; Task 14.3 documents the real reason `--dry-run` smoke can't replace `--help` smoke; Task 12.5 pins "approximate" marker for unmeasured timings; Task 13.4 adds review-cadence marker to quickstart.md; Task 2.4 documents podman-as-docker caveat; Task 1.4 pins `KebabCaseLower` JSON enum policy; ADR-7.4-001 adds wizard-vs-sample UX footnote (John finding). Net: 13 changes, 0 scope contraction; 1 scope expansion (two new `MemoriesClient` methods) justified as "cheaper than shipping a degraded wizard"; quality up, contract hygiene preserved. |
| 2026-04-16 | 0.3 | Post-advanced-elicitation revision (Stakeholder Round Table / Expert Panel / Debate Club / User Persona / Time Traveler): 7 novel findings applied. **Major fixes:** (B) AppHost port binding dependency documented — step 3 failure suggestion now points user to Aspire dashboard for actual port when default 5000 fails (Aspire expert finding: launchSettings binds 5000 for local dev but Aspire Testing randomizes); "Repo state" section gains an explicit paragraph on the `launchSettings.json` ↔ wizard-default coupling. (A) NFR31 30-min claim downgraded from `"≤ 30 minutes p99"` to `"approximately ≤ 30 minutes — unmeasured automated bound"` (PM finding: unmeasured NFRs erode trust); new Task 12.6 pins quarterly manual-walkthrough cadence + log file path. **Quality fixes:** (C) Task 1.2 pins closure-captured typed `Option<T>` pattern per `SearchQueryCommand.Build`; (D) new Task 8.4 adds enum round-trip test (`DryRun ↔ "dry-run"`) for AOT regression detection; (E) Task 2.2 `IProcessRunner` pins linked `CancellationTokenSource` semantics (kill subprocess on Ctrl-C within 1s); (F) new `EXPERIMENTAL` XML doc remark for `CreateTenantAsync` + `IngestAsync` (Debate Club synthesis: consistent with `public virtual` convention but signals signature may change in Phase 1.5); Tasks 4.2 and 5.3 updated to include the XML doc requirement. **New section:** "Hand-off to Story 7.5 (telemetry)" in Dev Notes (Time Traveler-Future finding) — pins `durationMs` + `ErrorCode` as the intended 7.5 telemetry source, prevents parallel instrumentation in the future story. Net: 7 changes; 1 moderate diagnostic improvement (B), 1 NFR-honesty fix (A), 5 quality/consistency refinements; zero scope expansion beyond Revision 0.2. |
