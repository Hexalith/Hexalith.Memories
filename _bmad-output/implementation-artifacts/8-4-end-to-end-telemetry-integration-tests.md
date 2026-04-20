# Story 8.4: End-to-End Telemetry Integration Tests (Tier-3 / Aspire)

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

**Effort estimate:** ~3 working days end-to-end — 1 day Aspire fixture wiring + in-memory OTLP exporter plumbing, 1 day test authoring + debugging cross-process trace propagation, 1 day docs + CI gating. Adjust if the Aspire fixture build error (`CS0311 on IDaprSidecarResource`, historically surfaced in Story 5.6 Dev Notes) reproduces on the current `Hexalith.Memories.AppHost` — as of 2026-04-20 the AppHost uses `WithDaprSidecar(...)` + `IResourceBuilder<IDaprComponentResource>` cleanly, so the risk is believed resolved; add 0.5-1 day only if Task 0 surfaces a regression.

## TL;DR

**What ships:** Two **Tier-3 / `[Trait("Category","Integration")]`** test classes that close Story 7.5's documented Docker-dependent follow-ups — **Task 11.3 (`AspireEndToEndTraceTests`)** and **Task 11.4 (`AuditLogStreamIntegrationTests`)** — by running the full CLI → ingress → Memories Server → Redis/FalkorDB chain against the existing `AspireIngestionPipelineFixture` with an in-memory OTLP exporter attached. Closes the authoritative **NFR28** gate (distributed trace propagation across DAPR hops) and the authoritative **FR67** gate (audit-event emission captured from the deployed stack's stdout JSON log stream).

**What already exists (do NOT rebuild):**

1. **`AspireIngestionPipelineFixture`** at `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` — Epic 6's Aspire integration fixture; already boots the Memories AppHost with Redis, FalkorDB, DAPR sidecar, and the Memories Server via `Aspire.Hosting.Testing.DistributedApplicationTestingBuilder`. Exposes `MemoriesClient : HttpClient`, `DaprSidecarHttpEndpoint : Uri`, `RedisConnection : IConnectionMultiplexer`, `FalkorDbConnection : IConnectionMultiplexer`, a `TestLogProvider _logProvider` with `LogEntryCount`, plus actor-proxy factories. Reuse verbatim.
2. **`OpenTelemetry.Exporter.InMemory 1.13.1`** — already pinned in `Directory.Packages.props:25`.
3. **`MemoriesActivitySource` + `MemoriesMeter`** at `src/Hexalith.Memories.Telemetry/` — Story 7.5 substrate (shared project referenced by both Server and CLI per ADR-7.5-002). Source name = `Hexalith.Memories`, Meter name = `Hexalith.Memories`. `MemoriesMeter.RejectedTenantTag = "__rejected__"` for the cardinality-safe rejected-tenant path.
4. **Server-side telemetry pipeline** at `src/Hexalith.Memories.Server/Telemetry/` — `EndpointTelemetryScope` (the shared wrapper the four instrumented endpoints invoke), `AccessTelemetryLog` (`[LoggerMessage]` emitters with the EventId 7501-7599 bank and `AccessTelemetryCategory` marker type), `TelemetryMetricsRecorder`, `TelemetrySnapshotCache`, `RollingCounterStore`, `TelemetrySummaryService`. The four instrumented endpoints (search / ingest / traverse / case-access) already emit activities, metrics, and audit events.
5. **CLI opt-in telemetry bootstrap** at `src/Hexalith.Memories.Cli/Execution/CliTelemetryBootstrap.cs` (Story 7.5 Task 6); registers `AddHttpClientInstrumentation` + OTLP exporter when `HEXALITH_MEMORIES_OTEL_ENDPOINT` is set or `--telemetry` is passed. Reuse by pointing the env var at the test's in-memory collector endpoint. URI scheme filter already rejects non-http/https schemes (Rev 0.3 hardening).
6. **ServiceDefaults OTLP wiring** at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:81-92` — gates `UseOtlpExporter()` on the `OTEL_EXPORTER_OTLP_ENDPOINT` configuration value. This is the Server-side hook Task 1 will extend (never replace) for in-memory capture. Health-filter predicate `ShouldTraceHttpRequest` rejects `/health`, `/alive`, `/ready` (Story 7.5 Rev 1.1 extracted it from the lambda for testability).
7. **Tier-2 CI-enforceable coverage** — `TracePropagationNoDockerTests` + `AuditLogStreamTests` + `TelemetrySummaryEndpointTests` + `EndpointTelemetryScopeTests` + `TelemetryMetricsRecorderTests` + `RollingCounterStoreTests` + `MemoriesActivitySourceTests` + `MemoriesMetricsTests` + `AccessTelemetryLogTests` + `AccessTelemetryEventSchemaTests` + `AuditEventSchemaVersioningTests` + `OpenTelemetryRegistrationTests` + `TelemetryHealthExclusionTests` under `tests/Hexalith.Memories.Server.Tests/Telemetry/`. These guard ~80% of what can break without Docker. 8.4 does NOT replace them; it complements them with the real-DAPR-hop gate.
8. **`TelemetryWebAppFactory` + `TestRootScope` + `CapturingAuditLoggerProvider`** at `tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/` — the Tier-2 WebApplicationFactory + test-scoped trace-id + audit-log capture pattern established in Story 7.5 Rev 1.3. Mirror the shape for the Tier-3 variant (NOT the mechanism — the Tier-3 variant captures from the real container's stdout, not the in-process `ILogger` pipeline).

**What 8.4 adds:**

1. **`tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs`** `[Trait("Category", "Integration")]` — attaches an in-memory OTLP exporter to the Server's OpenTelemetry builder inside the Aspire fixture (via an integration-only extension or test-only environment override); invokes the CLI **via DI** (NOT a subprocess — Story 7.1 anti-pattern #8); asserts captured spans share a single `TraceId` end-to-end: CLI root span → HttpClient → Server AspNetCore → `memories.search` activity → downstream Redis span (if `StackExchange.Redis.Extensions.OpenTelemetry` instrumentation is registered) → downstream FalkorDB span (if registered).
2. **`tests/Hexalith.Memories.IntegrationTests/Telemetry/AuditLogStreamIntegrationTests.cs`** `[Trait("Category", "Integration")]` — runs one search + one ingest + one traverse + one case-access operation through the fixture; captures the Server container's stdout JSON log stream; asserts one `AccessTelemetryEvent` per operation with the Story 7.5 AC #4 schema, `schemaVersion == 1`, EventId in `7500-7599`, and a `traceId` matching the corresponding span's trace id.
3. **Test-side OTLP exporter wiring** — a narrow extension point so integration tests can attach an `InMemoryExporter<Activity>` / `InMemoryExporter<LogRecord>` to the Server's running OpenTelemetry pipeline WITHOUT modifying production code. Preferred approach (Task 1.1 spike): an env var (`HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1`) read in `ConfigureOpenTelemetry` (or a new `ServiceDefaults.Telemetry.InMemoryExporterExtensions` file kept in a `DEBUG`/internal-scope namespace) that appends an extra processor exposing collected items via a thread-safe static collector with a `Reset()` helper. Fallback (Task 1.2): AspireBuilder test extension that spins up an in-process gRPC OTLP receiver on a dynamic localhost port and configures the Server container's `OTEL_EXPORTER_OTLP_ENDPOINT` to point at it. Prefer (1) — smaller surface, no second container. Task 1 spike must decide and pin via ADR-8.4-002.
4. **`docs/dev/telemetry.md` — "End-to-end trace verification" section** — documents how to run 8.4 locally (Docker available gate) and what the test output tells operators. Cross-links to Story 7.5's Tier-2 variants with an explicit "what each tier proves" table.
5. **CI gating decision record** — short ADR-8.4-001 at `docs/dev/telemetry.md` on whether 8.4 runs on every PR (Docker-provisioned runner required) or only on a nightly / merge-queue lane. Default: merge-queue lane — the Tier-2 variants gate PRs; Tier-3 gates release promotion.

**What does NOT ship:**

- **Replacing Tier-2 variants.** `TracePropagationNoDockerTests` + `AuditLogStreamTests` + `TelemetrySummaryEndpointTests` stay as the CI-on-every-PR gate; 8.4 is **additive**, running on the Docker-capable lane only.
- **MCP → Server trace propagation.** Story 7.5 Rev 0.3 finding 1c assigns this to Epic 10's MCP story (the MCP protocol introduces the first real DAPR service-invocation hop Memories originates). 8.4 covers CLI → Server via HTTP; the MCP story extends the same fixture pattern for the DAPR-hop variant.
- **Percentile / histogram end-to-end assertions.** Story 7.5 Rev 0.2 deferred p50/p99 aggregation; the Tier-3 fixture does not re-open that scope. Histogram emission IS exercised (`memories.search.duration` is recorded per request), but assertions on the histogram shape are out of scope — the in-memory exporter captures the fact that the metric was emitted, not specific percentile values.
- **Test-side OTLP collector process (unless fallback is chosen).** Do NOT spin up a separate Jaeger / Tempo container. The in-memory exporter runs inside the test process; the Server's OTLP endpoint points at it via an in-process loopback. This keeps the fixture boot time under the existing Epic 6 budget.
- **Authoritative Kubernetes / helm-chart traces.** Production deployment shapes are orthogonal; the Aspire fixture uses Aspire's container orchestration, not Kubernetes. Operator-facing Kubernetes telemetry guidance stays in `docs/dev/telemetry.md` as documentation, not tested behavior.
- **Per-axis trace breakdown inside hybrid search.** Story 7.5 AC #3 clarified that `axis=hybrid` records the whole-request wall-clock — per-axis sub-spans inside a hybrid call are a Phase 1.5 concern. 8.4 asserts trace propagation at the request boundary, not sub-axis granularity.
- **Re-testing Story 7.5's substrate invariants.** `MemoriesActivitySourceTests`, `MemoriesMetricsTests`, `EndpointTelemetryScopeTests`, `RollingCounterStoreTests`, `AccessTelemetryLogTests`, `AccessTelemetryEventSchemaTests`, `TelemetryHealthExclusionTests`, `OpenTelemetryRegistrationTests` all stay green and do NOT need duplication at Tier-3. The fixture-level tests ONLY assert cross-process invariants the Tier-2 variants cannot reach.

**Primary risks:**

1. **Aspire fixture build error regression.** Story 5.6 Dev Notes historically flagged a `CS0311 on IDaprSidecarResource` error on some SDK versions. Current AppHost (`src/Hexalith.Memories.AppHost/Program.cs`, as of 2026-04-20) uses `WithDaprSidecar(sidecar => ...)` cleanly — no direct `IDaprSidecarResource` generic constraint — and siblings 8.1-8.3 have integration tests green. **Mitigation:** Task 0 (pre-flight) verifies the fixture builds and boots; if the error reproduces, the first day's effort is AppHost investigation + fix. Escalate to a separate fix-forward story if >0.5 day of work.
2. **Cross-process OTLP capture is fragile.** The Server container's OTLP exporter must route to an in-process listener in the test. If the chosen approach (env-var-triggered in-memory processor) doesn't survive Aspire's service-discovery rewrites, fall back to a test-side OTLP receiver (in-process gRPC server) at a dynamic localhost port and configure the container to export there via `OTEL_EXPORTER_OTLP_ENDPOINT`. **Mitigation:** Task 1.1 spikes the env-var approach; Task 1.2 documents the fallback path in ADR-8.4-002 so a future contributor can flip without re-inventing.
3. **Trace id collision across parallel test runs.** If two integration tests run in parallel (xUnit parallelism within the `IntegrationTests` assembly), both capture activities into the same in-memory exporter. Tests must filter by test-scoped trace id. **Mitigation:** reuse the `TestRootScope` pattern from `TracePropagationNoDockerTests` (`tests/Hexalith.Memories.Server.Tests/Telemetry/TracePropagationNoDockerTests.cs:61-96`) — either copy/adapt or extract to a new `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/` helper file. Alternatively, disable parallelism for the 8.4 test classes via `[Collection("Telemetry Integration")]`.
4. **Audit-log capture from stdout is timing-sensitive.** Container stdout is asynchronous; a test that asserts immediately after a request may race. **Mitigation:** the test uses a polling read with a 5-second timeout against the container's log stream; tolerates multiple log lines before the target event; explicit matching by `EventId` + `traceId`. Use `IAsyncEnumerable` over the stream (no `Thread.Sleep`). The fixture already wires a `TestLogProvider _logProvider`; inspect whether it aggregates Server container stdout or only Aspire resource logs — if only Aspire logs, add a container-stdout capture helper.
5. **Docker CI cost.** Running two Tier-3 test classes on every PR roughly doubles CI minutes vs. Tier-2-only runs. **Mitigation:** ADR-8.4-001 explicitly routes Tier-3 to the merge-queue lane (not per-PR). Per-PR runs stay fast via Tier-2 coverage; release-gate runs exercise Tier-3. Document the lane split in `.github/workflows/` + `docs/dev/telemetry.md`.
6. **Redis / FalkorDB instrumentation may not be registered.** Story 7.5 deferred adding `StackExchange.Redis.Extensions.OpenTelemetry` instrumentation — the substrate assumes it MAY be present, not that it IS. If absent, the "downstream Redis span" assertion silently succeeds (no Redis span found, test still passes the CLI → Server chain). **Mitigation:** the trace-propagation test asserts the CLI → Server chain as the primary invariant; the Redis span assertion is a secondary check that logs a `Skip` trait when the instrumentation is absent. Adding Redis OTEL instrumentation itself is out of scope (separate story, probably in Epic 11 CI hardening).

## Story

As the Memories release manager,
I want end-to-end Tier-3 integration tests that verify distributed traces propagate across CLI → Server → backends AND audit events reach the deployed stack's stdout log stream,
so that I can ship releases with confidence that NFR28 and FR67 hold on real infrastructure — not just on the Tier-2 in-process approximation.

## Acceptance Criteria

1. **Single TraceId end-to-end (NFR28 authoritative gate).**
   **Given** the `AspireIngestionPipelineFixture` is running with an in-memory OTLP exporter wired to capture both the CLI's emitted spans and the Server container's emitted spans,
   **When** the CLI invokes `memories search query --tenant <provisioned-tenant> --query "<any>"` via the DI entry point (NOT a subprocess) with `HEXALITH_MEMORIES_OTEL_ENDPOINT` set to the fixture's OTLP endpoint,
   **Then** the captured span collection contains at least: one CLI root span (`memories.cli.invoke`), one outbound HTTP client span, one AspNetCore server span, and one `memories.search` activity
   **And** all of these spans share a single `TraceId`
   **And** the parent-child relationships match W3C TraceContext semantics (CLI root → HttpClient → AspNetCore → `memories.search`).

2. **Optional downstream Redis / FalkorDB span attribution.**
   **Given** AC #1's captured span collection,
   **When** `StackExchange.Redis.Extensions.OpenTelemetry` or equivalent Redis OTEL instrumentation is registered at time of test execution,
   **Then** at least one Redis client span appears in the same trace (same `TraceId` as AC #1's AspNetCore span)
   **And** when the instrumentation is NOT registered, the test emits an xUnit `Skip` trait for this specific assertion with reason `"Redis OTEL instrumentation not registered"` — the AC #1 invariant still gates test pass/fail
   **And** the skip event is surfaced via a `telemetry.redis.instrumentation.skipped` log line at `Warning` level on the merge-queue lane so the skip does not silently persist forever — AC #2 is **informational-only** until Redis OTEL instrumentation lands (tracked as a separate Epic 11 item); contributors are expected to convert the skip to a hard assertion once instrumentation registers.

3. **Audit-log capture from stdout (FR67 authoritative gate).**
   **Given** the fixture is running with the Server container's stdout available via the Aspire container API,
   **When** one operation of each instrumented type runs (`search`, `ingest`, `traverse`, `case-access`) against a provisioned tenant,
   **Then** the stdout log stream contains exactly one structured JSON line per operation that parses as an `AccessTelemetryEvent`
   **And** each parsed event has `schemaVersion == 1`, `eventId` in `7500-7599`, non-empty `tenantId`, matching `operationType`, non-empty `traceId` + `spanId`, and a `durationMs >= 0`
   **And** health-endpoint probes (`/health`, `/alive`, `/ready`) produce zero `AccessTelemetryEvent` entries in the same window (regression guard for Story 7.5 AC #5).

4. **TraceId cross-reference between span and audit event.**
   **Given** AC #1's captured `memories.search` activity and AC #3's search `AccessTelemetryEvent`,
   **When** both are compared,
   **Then** `auditEvent.TraceId == searchActivity.TraceId.ToString()` AND `auditEvent.SpanId == searchActivity.SpanId.ToString()` — same invariant Story 7.5 Tier-2 asserts (`AuditLogStreamTests`), now proven end-to-end across the real deployed stack.

5. **Test-side OTLP capture does not affect production behavior.**
   **Given** the in-memory exporter wiring added in Task 1,
   **When** the Server is run without the 8.4 test env var / configuration trigger,
   **Then** no in-memory exporter is registered
   **And** no new `IHostedService` / processor is appended to the production OpenTelemetry pipeline
   **And** Story 7.5's `OpenTelemetryRegistrationTests` continue to pass unchanged.

6. **CI lane gating.**
   **Given** the GitHub Actions workflow matrix,
   **When** a PR is opened,
   **Then** 8.4's tests run only on the merge-queue / release lane (NOT the per-PR lane)
   **And** ADR-8.4-001 documents the lane split with a one-sentence rationale per lane
   **And** `docs/dev/telemetry.md` lists which tier runs on which lane so contributors know where regressions surface.

7. **Documentation.**
   **Given** an operator or developer wants to verify telemetry end-to-end locally,
   **When** they read `docs/dev/telemetry.md`'s new "End-to-end trace verification" section,
   **Then** the doc covers: how to run the Tier-3 tests locally (`dotnet test --filter Category=Integration` with Docker available), what each captured span proves about the deployed stack, the tier split (Tier-2 per-PR vs Tier-3 merge-queue), and how to interpret failures (OTLP wiring vs DAPR hop vs audit pipeline).

## Tasks / Subtasks

- [ ] **Task 0: Pre-flight — verify Aspire fixture builds and boots.**
    - [ ] 0.1 Build `Hexalith.Memories.AppHost` (`dotnet build src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`). The current AppHost (2026-04-20) uses `WithDaprSidecar(sidecar => ...)` with `IResourceBuilder<IDaprComponentResource>`; the Story 5.6 `CS0311 on IDaprSidecarResource` issue is believed resolved — but if the build fails, diagnose (likely package alignment between `CommunityToolkit.Aspire.Hosting.Dapr 9.7.0` and Aspire 13.1.3) before proceeding.
    - [ ] 0.2 Run one existing `AspireIngestionPipelineFixture`-backed test (e.g. an `IngestionPipelineTests` case under `tests/Hexalith.Memories.IntegrationTests/Ingestion/`) with Docker running to confirm the fixture boots end-to-end. Exit-criterion: fixture starts a full Aspire environment (Redis + FalkorDB + DAPR sidecar + Memories Server) and the existing integration test passes.

- [ ] **Task 1: Test-side in-memory OTLP capture.** (AC: #1, #5)
    - [ ] 1.1 Spike the env-var-triggered in-memory processor approach. Preferred placement: extend `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` `AddOpenTelemetryExporters` with an `IF (Environment.GetEnvironmentVariable("HEXALITH_MEMORIES_TELEMETRY_INMEMORY") == "1")` branch that appends `.AddInMemoryExporter(InMemorySpanCollector.Activities)` to the `WithTracing` chain and the logging pipeline. **ADR-8.4-002 PINNED: option B (test-only surface).** Place `InMemorySpanCollector` under `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/InMemorySpanCollector.cs` — NOT in `src/Hexalith.Memories.Telemetry/`. Rationale (Winston, party-mode review 2026-04-20): a production-visible static mutable collector leaks internal state into consumer IntelliSense across the Hexalith ecosystem and creates a foot-gun for plugin authors, even when gated with `[Experimental("HXL008")]` + `[EditorBrowsable(Never)]`. Test-only placement keeps the blast radius bounded; the env-var branch inside `ServiceDefaults` references the test-only collector via a DI-registered `IActivityCollector` abstraction registered only when the env var is set. Provide `Reset()` to clear between tests AND call `await TracerProvider.ForceFlushAsync()` before `Reset()` in fixture dispose to avoid activity-drain / reset races.
    - [ ] 1.2 If (1.1) is infeasible (Aspire rewrites service discovery for the container's OTLP exporter in a way that breaks loopback), fall back to a test-process in-memory gRPC OTLP receiver: `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/InMemoryOtlpReceiver.cs` listening on a dynamic localhost port; configure the Server container's `OTEL_EXPORTER_OTLP_ENDPOINT` via `IResourceBuilder.WithEnvironment` to point at it. Document the chosen path in ADR-8.4-002 (new section in `docs/dev/telemetry.md`).
    - [ ] 1.3 Unit test the chosen capture mechanism in isolation: `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/OtlpCaptureMechanismTests.cs` asserts that with the trigger set, activities are captured; without it, nothing is appended. This test does NOT require Docker — it builds a minimal host in-process. Mark with `[Trait("Category", "Unit")]` to exclude from the integration-only filter.
    - [ ] 1.4 Verify AC #5: run the existing `OpenTelemetryRegistrationTests` + `ServiceDefaults` smoke — both MUST pass unchanged when the trigger is not set. Add a dedicated regression test `OpenTelemetryRegistrationTests.WithoutInMemoryTrigger_NoInMemoryExporterRegistered` if one does not already cover this.

- [ ] **Task 2: `AspireEndToEndTraceTests`.** (AC: #1, #2, #4)
    - [ ] 2.1 Create `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` `[Trait("Category", "Integration")]`. Reuse `AspireIngestionPipelineFixture` via `IClassFixture<AspireIngestionPipelineFixture>` or collection fixture.
    - [ ] 2.2 Test case `CliSearch_EndToEnd_SingleTraceIdAcrossAllHops`: provision a tenant via the fixture helper; set `HEXALITH_MEMORIES_OTEL_ENDPOINT` to the in-memory capture endpoint AND `HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1` before invoking; invoke `MemoriesClient.SearchAsync(...)` via the fixture's DI root (Story 7.1 anti-pattern #8 — NOT a subprocess); capture all activities; assert the CLI root span (`memories.cli.invoke`) + HttpClient span + AspNetCore span + `memories.search` activity share one `TraceId`; assert parent-child chain via `Activity.ParentId`.
    - [ ] 2.3 Test case `CliSearch_EndToEnd_RedisInstrumentation_OptionalSpan`: detect Redis OTEL instrumentation at runtime by reflecting on the Server's `TracerProvider` registered sources (NOT by inspecting csproj) — `TracerProvider` exposes registered instrumentation names. If `StackExchange.Redis.Extensions.OpenTelemetry` is present, assert a Redis span in the same trace; otherwise use `Skip.If(!RedisOtelPresent, "Redis OTEL instrumentation not registered")` AND emit a `Warning`-level log line `telemetry.redis.instrumentation.skipped` so the skip is surfaced in merge-queue-lane logs (per AC #2 soft-fail-visibility requirement). xUnit v2 API: requires `Xunit.SkippableFact` package; add to `Directory.Packages.props` as a new test-project dep and gate the fact with `[SkippableFact]` (see Latest technical specifics for full pinning).
    - [ ] 2.4 Test case `CliSearch_AuditEvent_TraceIdMatchesSpan` (AC #4): uses capture from 2.2; asserts the Server-emitted `AccessTelemetryEvent` (pulled from the in-memory log exporter OR from container stdout — pick the simpler capture that works with the Task 1 outcome) carries matching `TraceId` + `SpanId` from the `memories.search` activity.
    - [ ] 2.5 Reset the in-memory collector between tests via the `Reset()` helper on `InMemorySpanCollector` to avoid cross-test pollution under the shared fixture.

- [ ] **Task 3: `AuditLogStreamIntegrationTests`.** (AC: #3)
    - [ ] 3.1 Create `tests/Hexalith.Memories.IntegrationTests/Telemetry/AuditLogStreamIntegrationTests.cs` `[Trait("Category", "Integration")]`. Helper `IAsyncEnumerable<string> ReadServerLogStream(CancellationToken)` against the fixture's container API (leverage `Aspire.Hosting.Testing` log-reading primitives or extend `AspireIngestionPipelineFixture._logProvider` if it already captures Server container stdout — inspect `TestLogProvider` at implementation time). Filter to lines that parse as JSON AND contain an `eventId` in `7500-7599`.
    - [ ] 3.2 Test case `SearchOperation_EmitsOneAuditEvent_WithAC4Schema`: provision tenant; run one search; assert one audit event matching Story 7.5 AC #4 schema (`schemaVersion=1`, `operationType="search"`, `durationMs >= 0`, non-empty `traceId` + `spanId`, non-null `timestamp`). Use `Hexalith.Memories.Contracts.V1.AccessTelemetryEvent` for deserialization (already serializable via `MemoriesJsonContext`).
    - [ ] 3.3 Test cases `IngestOperation_EmitsOneAuditEvent_WithAC4Schema`, `TraverseOperation_EmitsOneAuditEvent_WithAC4Schema`, `CaseAccessOperation_EmitsOneAuditEvent_WithAC4Schema` — same structure, one per operation type. `operationType` constants come from `AccessTelemetryLog.OperationSearch|OperationIngest|OperationTraverse|OperationCaseAccess`.
    - [ ] 3.4 Test case `HealthProbes_EmitZeroAuditEvents` (regression guard per AC #3 + Story 7.5 AC #5): run 5 consecutive `GET /health`, `/alive`, `/ready` probes via the fixture's HTTP client; assert zero `AccessTelemetryEvent` entries in the window. Mirror the Tier-2 guard at `tests/Hexalith.Memories.Server.Tests/Telemetry/TelemetryHealthExclusionTests.cs`.
    - [ ] 3.5 Test case `SchemaVersion_IsOneForAllEmittedEvents`: aggregate across tests 3.2-3.3; assert every captured event has `schemaVersion == 1` (future-proofing — fails loudly if a breaking field change slips in).
    - [ ] 3.6 Use a 5-second `TaskCompletionSource<T>` + `CancellationTokenSource` polling loop for stdout reads; no `Thread.Sleep`, no new `Polly` dependency (not present in `Directory.Packages.props` at time of writing — use BCL primitives instead). Cancel via `CancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5))`; on cancellation, fail with a diagnostic that dumps the last N stdout lines for triage.

- [ ] **Task 4: CI wiring.** (AC: #6)
    - [ ] 4.1 Update `.github/workflows/` (inspect current layout at implementation time — Epic 11 may have landed the merge-queue workflow). Route `[Trait("Category","Integration")]` tests to a Docker-provisioned merge-queue lane, NOT the default per-PR CI job. Reference the existing Category-based filter convention (Stories 7.1-7.4 established `Category!=Integration` on default runs).
    - [ ] 4.2 ADR-8.4-001 at `docs/dev/telemetry.md`: one-paragraph decision record on Tier-2 (PR gate) vs Tier-3 (release gate) lane split. Cite CI-minute cost + Docker-availability constraints. Adjacent to ADR-8.4-002 (Task 1 decision on in-memory vs OTLP-receiver capture).
    - [ ] 4.3 If Epic 11 (CI/CD) has NOT landed the merge-queue workflow, flag as a soft dependency and document in 8.4's Dev Notes; provisionally gate the Tier-3 lane via a `nightly.yml` workflow as a bridge. Record this bridge in ADR-8.4-001.

- [ ] **Task 5: Documentation.** (AC: #7)
    - [ ] 5.1 Extend `docs/dev/telemetry.md` with a new "End-to-end trace verification" section covering the tier split, how to run locally (`dotnet test --filter Category=Integration`, Docker required), and how to interpret failures.
    - [ ] 5.2 Add a "Tier split" table to the doc:

      | Tier | Runs on | Gates | Story |
      | ---- | ------- | ----- | ----- |
      | Tier-1 (unit) | Every PR | Substrate correctness | 7.5 Tasks 8-10 |
      | Tier-2 (WebApplicationFactory) | Every PR | In-process trace + audit invariants | 7.5 Tasks 11.1, 11.2 |
      | Tier-3 (Aspire fixture) | Merge-queue lane | End-to-end NFR28 + FR67 gate | **8.4 (this story)** |

    - [ ] 5.3 Cross-link Story 7.5's Tier-2 variants so a future dev reading 7.5's Change Log Rev 1.3/1.4 finds 8.4 as the closure.
    - [ ] 5.4 Add a "Failure interpretation cheatsheet" subsection: what does failure at each tier tell you? (Tier-1 → substrate regression; Tier-2 → `EndpointTelemetryScope` regression; Tier-3 → OTLP wiring OR real DAPR hop OR audit pipeline regression).

- [ ] **Task 6: Cleanup and close out.**
    - [ ] 6.1 Update Story 7.5's Review Findings + Tasks 11.3 + 11.4 to `[x]` with a reference to this story's merge commit hash.
    - [ ] 6.2 Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: `8-4-end-to-end-telemetry-integration-tests: done` (or `review` pending merge).
    - [ ] 6.3 Update Epic 7 retrospective note (if not yet written) or leave a marker in Epic 8's retrospective to confirm Gate 3 (Developer Experience + Operational Observability) is now end-to-end gated, not just substrate-gated.

## Dev Notes

### Inherited from Story 7.5 (do not re-derive)

- **All ADRs from 7.5 apply unchanged**: ADR-7.5-001 (`AccessTelemetryEvent` in `Contracts.V1`), ADR-7.5-002 (shared `Hexalith.Memories.Telemetry` project), ADR-7.5-003..005.
- **Substrate constants**: `MemoriesActivitySource.SourceName = "Hexalith.Memories"`, `MemoriesMeter.Name = "Hexalith.Memories"`, EventId bank `7500-7599`, `MemoriesMeter.RejectedTenantTag = "__rejected__"`.
- **`AccessTelemetryEvent` schema version policy**: additive fields stay at `schemaVersion=1`, breaking changes bump the integer. Frozen manifest lives in `AccessTelemetryEventSchemaTests`.
- **Health-endpoint trace exclusion**: `Extensions.ShouldTraceHttpRequest` filter at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:72-79` must stay byte-identical semantically; do NOT re-inline the lambda.
- **CLI anti-pattern #8 (7.1)**: CLI is invoked via DI in-process, NOT spawned as `Program.Main` subprocess — otherwise in-memory exporter does not share process with the CLI's emitting spans.

### Implementation contracts for 8.4

- **Tier-2 tests are canonical.** If a Tier-3 test fails in a way a Tier-2 test would have caught, the fix lives at Tier-2 and the Tier-3 test stays as the gate. Do NOT let Tier-3 drift into substrate-testing territory.
- **No subprocess CLI invocations.** Story 7.1 anti-pattern #8 holds: the CLI is invoked via DI, in-process, inside the test's assembly.
- **Fixture sharing.** Reuse `AspireIngestionPipelineFixture` as an xUnit `IClassFixture` or collection fixture; do NOT boot a fresh Aspire environment per test class (cost prohibitive). Telemetry collector state is reset per test via a public `Reset()` helper on the in-memory capture mechanism.
- **Token / secret hygiene.** Do NOT log `HEXALITH_MEMORIES_OTEL_ENDPOINT` if it contains credentials (e.g. Honeycomb API key in query string) — the Tier-3 test uses a loopback endpoint so this is unlikely, but the documentation should note the production caveat.
- **Test-scope env var hygiene.** Every test that sets `HEXALITH_MEMORIES_OTEL_ENDPOINT` or `HEXALITH_MEMORIES_TELEMETRY_INMEMORY` MUST restore the previous value (or clear) in the `Dispose` / finally block. Static env-var state bleeds across parallel tests otherwise.

### Previous story intelligence

- **Story 7.5 Rev 1.4 (2026-04-18)** landed Tier-2 `AuditLogStreamTests` (6 cases) + `TelemetrySummaryEndpointTests` (4 cases) on the shared `TelemetryWebAppFactory`. The factory serializes telemetry tests via `TelemetryTestCollection` (xUnit collection fixture) to prevent concurrent `MeterListener` / `ActivityListener` pollution. **Apply the same serialization to 8.4's test classes** via a new `[Collection("Telemetry Integration")]` marker in `tests/Hexalith.Memories.IntegrationTests/Telemetry/` to avoid cross-test collector pollution under the shared Aspire fixture.
- **Story 7.5 Round 3 re-review (2026-04-18)** hardened several corner cases:
    - `Uri.TryCreate(..., UriKind.Absolute, ...)` must reject non-http/https schemes (in `CliTelemetryBootstrap.ResolveEndpoint`). Task 2.2 sets `HEXALITH_MEMORIES_OTEL_ENDPOINT` to `http://localhost:<port>`; do NOT use `file://` or other non-http schemes.
    - `RollingCounterStore` dispose only in `Dispose()`, not in `StopAsync` — relevant if 8.4's tests spin the host twice on the same DI container.
    - Malformed `HEXALITH_MEMORIES_OTEL_ENDPOINT` now emits a warning via `Console.Error`. 8.4 tests MUST use valid http endpoints to avoid noisy log output.
- **Sibling 8.2 (Consistency Verification & Repair)** is currently in-progress (review fix pass 1 as of 2026-04-20) — its infra-level Aspire tests at `tests/Hexalith.Memories.IntegrationTests/Consistency/ConsistencyWorkflowIntegrationTests.cs` use `AspireIngestionPipelineFixture` and are a reference pattern for 8.4's test-layout conventions.
- **Sibling 8.3 (Data Export)** is in-progress and touches `MemoriesJsonContext.cs` — if 8.4 needs to register a new type in the source-gen context (e.g. for in-memory capture buffers), check 8.3's pending changes to avoid merge conflicts on that file.
- **Sibling 8.1 (Health Checks & Readiness)** is done. Its `HealthEndpointIntegrationTests.cs` at `tests/Hexalith.Memories.IntegrationTests/Health/` is a reference for Tier-3 HTTP probe assertions — 8.4 Task 3.4 mirrors its pattern for the health-exclusion regression guard.

### Git intelligence (most recent commits on main, as of 2026-04-20)

- `b681a40` — Add unit tests for health checks and workflows (Story 8.1 closure)
- `788f40c` — Add telemetry tests and infrastructure for metrics and activity source validation (Story 7.5 Rev 1.3/1.4 telemetry Tier-2 coverage, directly under this story)
- `958164b` — Add integration and unit tests for Quickstart CLI functionality (Story 7.4)
- `1d8e3af` — feat: Update framework setup progress and enhance test suite documentation
- `4136f83` — Add comprehensive CLI error handling and catalog tests (Story 7.3)

Patterns to follow: structured namespace layout under `tests/Hexalith.Memories.IntegrationTests/<Feature>/`; fixture-based sharing via `IClassFixture<T>`; `[Trait("Category", "Integration")]` for Docker-gated tests; Shouldly for assertions; NSubstitute for test doubles; `IAsyncLifetime` for async setup/teardown.

### Project Structure Notes

- New files all live under `tests/Hexalith.Memories.IntegrationTests/Telemetry/` (new folder — first occupant) with an `Infrastructure/` subfolder for helpers. Aligns with the existing folder-per-feature convention (`Consistency/`, `Health/`, `Ingestion/`, `Search/`, etc.).
- No new NuGet packages — `OpenTelemetry.Exporter.InMemory 1.13.1` is already in `Directory.Packages.props:25`.
- If the in-memory capture surface (Task 1.1) is made production-visible (option A), it lives in `src/Hexalith.Memories.Telemetry/` alongside `MemoriesActivitySource.cs` + `MemoriesMeter.cs` and flows into both Server + CLI references. If kept test-only (option B), it lives under `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/`. Decision pinned by ADR-8.4-002 in Task 1.
- No schema changes to `AccessTelemetryEvent` — ADR-7.5-001 immutability still holds.
- Potential merge-conflict hotspots: `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` (if Task 1.1 extends `AddOpenTelemetryExporters`); `docs/dev/telemetry.md` (new section); `_bmad-output/implementation-artifacts/sprint-status.yaml`.

### Latest technical specifics

- **OpenTelemetry .NET SDK**: `1.13.1` for `OpenTelemetry.Exporter.InMemory`. The `AddInMemoryExporter(ICollection<Activity>)` overload stores activities into a caller-provided collection — use `ConcurrentBag<Activity>` or `ConcurrentQueue<Activity>` for thread-safety under xUnit parallelism. The `AddInMemoryExporter(ICollection<LogRecord>)` variant captures `LogRecord` for the logging pipeline (needed for AC #4's `AccessTelemetryEvent` capture if going via the in-memory log exporter route vs container stdout).
- **Aspire 13.1.3 + CommunityToolkit.Aspire.Hosting.Dapr 9.7.0**: the `WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", <value>)` pattern on `IResourceBuilder<ProjectResource>` is the supported hook for overriding the Server's OTLP exporter endpoint in Task 1.2 fallback. Aspire's default OTLP wiring auto-injects the dashboard endpoint; the test override wins when set before `builder.Build()`.
- **xUnit v2.9.3 PINNED** (verified against `Directory.Packages.props:<xunit>` as of 2026-04-20): `Xunit.SkippableFact` is NOT currently in `Directory.Packages.props` and MUST be added by Task 2.3 for the AC #2 `Skip.If` path. Recommended version: `Xunit.SkippableFact 1.4.13`. API: decorate the fact with `[SkippableFact]` (not `[Fact]`), then call `Skip.If(condition, reason)` inside the test body. Do NOT use `Assert.Skip(...)` — that API is xUnit v3 only and will not compile against v2.9.3. `[Trait("Category", "Integration")]` filter via `dotnet test --filter Category=Integration` excludes non-integration tests; conversely `Category!=Integration` excludes the Tier-3 lane from the per-PR job.
- **Env-var test hygiene helper** (Amelia, party-mode review 2026-04-20): extract a single disposable `EnvVarScope` helper under `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/EnvVarScope.cs` — `using var _ = EnvVarScope.Set("HEXALITH_MEMORIES_OTEL_ENDPOINT", url);` — that snapshots the prior value on construction and restores it on dispose. Use everywhere that sets `HEXALITH_MEMORIES_OTEL_ENDPOINT` or `HEXALITH_MEMORIES_TELEMETRY_INMEMORY`. Static mutable env-var state bleeds across parallel test collections otherwise.
- **W3C TraceContext propagation**: `traceparent` header format `00-<trace-id-32-hex>-<span-id-16-hex>-<flags-2-hex>`. The Tier-2 `AuditLogStreamTests.BuildRequest` helper at line 37-50 is the reference for injecting a test-scoped trace id.

### References

- Story 7.5 Section: Tasks 11.3, 11.4 (the explicit deferral source). See `_bmad-output/implementation-artifacts/7-5-search-and-access-telemetry.md:302-303`.
- Story 7.5 Risk #4: Trace propagation across DAPR hops.
- Story 7.5 Risk #8: Health-endpoint filter regression.
- Story 7.5 Change Log Rev 1.3 + Rev 1.4: the Tier-2 closure that 8.4 extends.
- Architecture observability requirements: `_bmad-output/planning-artifacts/architecture.md:82` (NFR27-29 restatement — "OpenTelemetry traces must propagate across all DAPR hops").
- Architecture Tier-3 test classification: `_bmad-output/planning-artifacts/architecture.md:666` (`Hexalith.Memories.IntegrationTests/` as Tier 3 Aspire e2e).
- Architecture CI lane pattern: `_bmad-output/planning-artifacts/architecture.md:1182` (`integration.yml # Tier 3 Aspire e2e (optional/nightly)`).
- Epics.md Story 8.4: `_bmad-output/planning-artifacts/epics.md:1598-1628`.
- Existing Tier-2 test patterns: `tests/Hexalith.Memories.Server.Tests/Telemetry/TracePropagationNoDockerTests.cs`, `tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs`, `tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/TelemetryWebAppFactory.cs`.
- Aspire fixture: `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`.
- ServiceDefaults telemetry wiring: `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:37-92`.
- Telemetry substrate: `src/Hexalith.Memories.Telemetry/MemoriesActivitySource.cs`, `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs`.
- Server telemetry pipeline: `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs`, `AccessTelemetryLog.cs`, `TelemetryMetricsRecorder.cs`.
- CLI telemetry bootstrap: `src/Hexalith.Memories.Cli/Execution/CliTelemetryBootstrap.cs`.
- `Hexalith.Memories.Contracts.V1.AccessTelemetryEvent` schema: `src/Hexalith.Memories.Contracts/V1/AccessTelemetryEvent.cs` (source-gen registered in `MemoriesJsonContext.cs`).

### Definition of Done

1. Tasks 0-6 complete with all subtasks checked.
2. Both new test classes (`AspireEndToEndTraceTests`, `AuditLogStreamIntegrationTests`) green on the merge-queue lane against a Docker-available runner.
3. All Tier-2 tests from Story 7.5 still green on the per-PR lane (no regression, AC #5 proven).
4. `docs/dev/telemetry.md` updated with the "End-to-end trace verification" section + tier-split table + ADR-8.4-001 + ADR-8.4-002 (Task 1 decision).
5. Sprint-status.yaml updated to `done` (or `review` pending merge) for `8-4-end-to-end-telemetry-integration-tests`.
6. Story 7.5's Task 11.3 + 11.4 checkboxes flipped to `[x]` with a reference back to 8.4's merge commit.
7. Change Log section below updated with the dev revision.
8. **Epic 11 bridge fallback shipped if needed**: if the Epic 11 merge-queue workflow is unlanded at completion, a `nightly.yml` bridge workflow MUST be committed under `.github/workflows/` AND ADR-8.4-001 MUST explicitly reflect the bridge-as-shipped decision. A story cannot be marked done with an "assumed but uncommitted" CI lane — either the merge-queue lane exists or the nightly bridge does.

## Dev Agent Record

### Agent Model Used

(Populated at dev time.)

### Debug Log References

(Populated at dev time — capture `dotnet test` output hashes, Aspire fixture boot logs, OTLP receiver logs if the fallback path is chosen.)

### Completion Notes List

(Populated at dev time. Document: chosen Task 1 path (1.1 env-var vs 1.2 OTLP receiver) + ADR-8.4-002 pin, Redis OTEL instrumentation registration status at time of test (AC #2 skip-or-assert), final CI lane (merge-queue vs nightly bridge per Task 4.3), any Task 0 AppHost issues encountered.)

### File List

(Populated at dev time — new + modified files.)

### Change Log

| Date       | Version | Description                                                                                                                                                                                                                                                              |
| :--------- | :------ | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-04-18 | 0.1     | Story context created as documented follow-up from Story 7.5 Rev 1.4. Status: backlog. Covers Story 7.5 Tasks 11.3 + 11.4 plus the CI gating ADR.                                                                                                                    |
| 2026-04-20 | 0.2     | Promoted to ready-for-dev. Added Dev Agent Record sections, Project Structure Notes, previous-story intelligence (7.5 Rev 1.4 landing + 8.1-8.3 sibling context), git intelligence, latest-tech specifics, and Definition of Done. Verified paths against current main. |
| 2026-04-20 | 0.3     | Applied party-mode review hardening (Murat / Winston / Amelia / Bob). (1) AC #2 reframed as informational-only with a `telemetry.redis.instrumentation.skipped` warning-log surfacing requirement so the Redis-instrumentation skip cannot silently persist. (2) ADR-8.4-002 pinned upfront to option B (test-only `InMemorySpanCollector` under `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/`, not a production-visible surface) with a `ForceFlushAsync` before `Reset()` requirement to avoid activity-drain races. (3) Task 2.3 pinned to xUnit v2.9.3 + `Xunit.SkippableFact 1.4.13` (verified `xunit 2.9.3` in `Directory.Packages.props`); `Assert.Skip` explicitly forbidden. (4) Task 3.6 dropped the spurious `Polly` reference — `TaskCompletionSource` + `CancellationTokenSource` only. (5) DoD item 8 added: CI-lane-or-bridge-must-be-committed closure gate. (6) Added `EnvVarScope` helper requirement to Latest technical specifics to prevent env-var bleed across parallel test collections. No scope change; ACs 1, 3-7 unchanged. Effort estimate unchanged at ~3 days. |
