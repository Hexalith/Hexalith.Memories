# Story 8.4: End-to-End Telemetry Integration Tests (Tier-3 / Aspire)

Status: backlog

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

**Effort estimate:** ~3 working days end-to-end — 1 day Aspire fixture wiring + in-memory OTLP exporter plumbing, 1 day test authoring + debugging cross-process trace propagation, 1 day docs + CI gating. Adjust if the Aspire fixture build error (`CS0311 on IDaprSidecarResource`, first surfaced in Story 5.6 Dev Notes) has not been resolved — add 0.5-1 day for AppHost build-error investigation.

## TL;DR

**What ships:** Two **Tier-3 / `[Trait("Category","Integration")]`** test classes that close Story 7.5's documented Docker-dependent follow-ups — **Task 11.3 (`AspireEndToEndTraceTests`)** and **Task 11.4 (`AuditLogStreamIntegrationTests`)** — by running the full CLI → ingress → Memories Server → Redis/FalkorDB chain against the existing `AspireIngestionPipelineFixture` with an in-memory OTLP exporter attached. Closes the authoritative **NFR28** gate (distributed trace propagation across DAPR hops) and the authoritative **FR67** gate (audit-event emission captured from the deployed stack's stdout JSON log stream).

**What already exists (do NOT rebuild):**

1. **`AspireIngestionPipelineFixture`** — Epic 6's Aspire integration fixture; already boots the Memories AppHost with Redis, FalkorDB, DAPR sidecar, and the Memories Server in Docker. Reuse verbatim.
2. **`OpenTelemetry.Exporter.InMemory`** — already in `Directory.Packages.props` (Story 7.5 Rev 1.0).
3. **`MemoriesActivitySource` + `MemoriesMeter` constants + `EndpointTelemetryScope`** — Story 7.5 substrate; the four instrumented endpoints (search / ingest / traverse / case-access) already emit activities, metrics, and audit events.
4. **CLI opt-in telemetry bootstrap** — Story 7.5 Task 6 (`CliTelemetryBootstrap`); already registers `AddHttpClientInstrumentation` + OTLP exporter when the `HEXALITH_MEMORIES_OTEL_ENDPOINT` env var is set. Reuse by pointing the env var at the test's in-memory collector endpoint.
5. **Tier-2 CI-enforceable coverage** — Story 7.5 Rev 1.3 `TracePropagationNoDockerTests` + `AuditLogStreamTests` + Rev 1.4 `TelemetrySummaryEndpointTests` already guard ~80% of what can break without Docker. 8.4 does NOT replace them; it complements them with the real-DAPR-hop gate.
6. **`AccessTelemetryEvent` schema + `CapturingAuditLoggerProvider`** — Story 7.5; reuse the capturing provider pattern for the fixture variant (source stdout JSON log stream, not the in-process `ILogger` pipeline).

**What 8.4 adds:**

1. **`tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs`** `[Trait("Category", "Integration")]` — attaches an in-memory OTLP exporter to the Server's OpenTelemetry builder inside the Aspire fixture (via an integration-only extension or test-only environment override); invokes the CLI **via DI** (NOT a subprocess — Story 7.1 anti-pattern #8); asserts captured spans share a single `TraceId` end-to-end: CLI root span → HttpClient → Server AspNetCore → `memories.search` activity → downstream Redis span (if `StackExchange.Redis.Extensions.OpenTelemetry` instrumentation is registered) → downstream FalkorDB span (if registered).
2. **`tests/Hexalith.Memories.IntegrationTests/Telemetry/AuditLogStreamIntegrationTests.cs`** `[Trait("Category", "Integration")]` — runs one search + one ingest + one traverse + one case-access operation through the fixture; captures the Server container's stdout JSON log stream; asserts one `AccessTelemetryEvent` per operation with the AC #4 schema (Story 7.5), `schemaVersion == 1`, EventId in `7500-7599`, and a `traceId` matching the corresponding span's trace id.
3. **Test-side OTLP exporter wiring** — a narrow extension point so integration tests can attach an `InMemoryExporter<Activity>` / `InMemoryExporter<LogRecord>` to the Server's running OpenTelemetry pipeline WITHOUT modifying production code. Likely approach: (a) an env var (`HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1`) read in `ConfigureOpenTelemetry` that appends an extra processor exposed via a well-known static collector; OR (b) an AspireBuilder test extension that intercepts the container's OTLP export to a test-side collector process. Prefer (a) — smaller surface, no second container. Evaluate during Task 1.
4. **`docs/dev/telemetry.md` — "End-to-end trace verification" section** — documents how to run 8.4 locally (`docker compose up` gate) and what the test output tells operators. Cross-links to Story 7.5's Tier-2 variants with an explicit "what each tier proves" table.
5. **CI gating decision record** — short ADR-8.4-001 at `docs/dev/telemetry.md` on whether 8.4 runs on every PR (Docker-provisioned runner required) or only on a nightly / merge-queue lane. Default: merge-queue lane — the Tier-2 variants gate PRs; Tier-3 gates release promotion.

**What does NOT ship:**

- **Replacing Tier-2 variants.** `TracePropagationNoDockerTests` + `AuditLogStreamTests` + `TelemetrySummaryEndpointTests` stay as the CI-on-every-PR gate; 8.4 is **additive**, running on the Docker-capable lane only.
- **MCP → Server trace propagation.** Story 7.5 Rev 0.3 finding 1c explicitly assigns this to Epic 10's MCP story (the MCP protocol introduces the first real DAPR service-invocation hop Memories originates). 8.4 covers CLI → Server via HTTP; the MCP story extends the same fixture pattern for the DAPR-hop variant.
- **Percentile / histogram end-to-end assertions.** Story 7.5 Rev 0.2 deferred p50/p99 aggregation; the Tier-3 fixture does not re-open that scope. Histogram emission IS exercised (the Server records `memories.search.duration` per request), but assertions on the histogram shape are out of scope — the in-memory exporter captures the fact that the metric was emitted, not specific percentile values.
- **Test-side OTLP collector process.** Do NOT spin up a separate Jaeger / Tempo container for the test. The in-memory exporter runs inside the test process; the Server's OTLP endpoint points at it via an in-process loopback. This keeps the fixture boot time under the existing Epic 6 budget.
- **Authoritative Kubernetes / helm-chart traces.** Production deployment shapes are orthogonal; the Aspire fixture uses Aspire's container orchestration, not Kubernetes. Operator-facing Kubernetes telemetry guidance stays in `docs/dev/telemetry.md` as documentation, not tested behavior.
- **Per-axis trace breakdown inside hybrid search.** Story 7.5 AC #3 clarified that `axis=hybrid` records the whole-request wall-clock — per-axis sub-spans inside a hybrid call are a Phase 1.5 concern. 8.4 asserts trace propagation at the request boundary, not sub-axis granularity.
- **Re-testing Story 7.5's substrate invariants.** `MemoriesActivitySourceTests`, `MemoriesMetricsTests`, `EndpointTelemetryScopeTests`, `RollingCounterStoreTests`, `AccessTelemetryLogTests`, `AccessTelemetryEventSchemaTests`, `TelemetryHealthExclusionTests`, `OpenTelemetryRegistrationTests` all stay green and do NOT need duplication at Tier-3. The fixture-level tests ONLY assert cross-process invariants the Tier-2 variants cannot reach.

**Primary risks:**

1. **Aspire fixture build error blocks test authoring.** Story 5.6 Dev Notes flagged a `CS0311 on IDaprSidecarResource` error that prevents the AppHost from building under some SDK versions. If 8.4 inherits this, Tasks 1-2 cannot run. **Mitigation:** Task 0 (pre-flight) verifies the fixture builds and boots; if the error reproduces, the first day's effort is AppHost investigation + fix (possibly just a package bump or `using` reorder per the Story 5.6 note). Escalate to a separate fix-forward story if >0.5 day of work.
2. **Cross-process OTLP capture is fragile.** The Server container's OTLP exporter must route to an in-process listener in the test. If the chosen approach (env-var-triggered in-memory processor) doesn't survive Aspire's service-discovery rewrites, fall back to a test-side OTLP receiver (in-process gRPC server) at localhost:4318 and configure the container to export there. **Mitigation:** Task 1 spikes both approaches; chooses the simpler one; the fallback path is documented so a future contributor can flip without re-inventing.
3. **Trace id collision across parallel test runs.** If two integration tests run in parallel (xUnit parallelism within the `IntegrationTests` assembly), both capture activities into the same in-memory exporter. Tests must filter by test-scoped trace id (same `TestRootScope` pattern Story 7.5 Rev 1.3 established). **Mitigation:** reuse the `TestRootScope` from `TracePropagationNoDockerTests` — copy/adapt or extract to `Hexalith.Memories.TestHelpers` if worth the refactor.
4. **Audit-log capture from stdout is timing-sensitive.** Container stdout is asynchronous; a test that asserts immediately after a request may race. **Mitigation:** the test uses a polling read with a timeout (5 seconds) against the container's log stream; tolerates multiple log lines before the target event; explicit matching by `eventId` + `traceId`. No `Thread.Sleep` — use `IAsyncEnumerable` over the stream.
5. **Docker CI cost.** Running two Tier-3 test classes on every PR roughly doubles CI minutes vs. Tier-2-only runs. **Mitigation:** ADR-8.4-001 explicitly routes Tier-3 to the merge-queue lane (not per-PR). Per-PR runs stay fast via Tier-2 coverage; release-gate runs exercise Tier-3. Document the lane split in `.github/workflows/` + `docs/dev/telemetry.md`.
6. **Redis / FalkorDB instrumentation may not be registered.** Story 7.5 deferred adding `StackExchange.Redis.Extensions.OpenTelemetry` instrumentation — the substrate assumes it MAY be present, not that it IS. If absent, the "downstream Redis span" assertion silently succeeds (no Redis span found, test still passes the CLI → Server chain). **Mitigation:** the trace-propagation test asserts the CLI → Server chain as the primary invariant; the Redis span assertion is a secondary check that logs a `skip` trait when the instrumentation is absent. Adding Redis OTEL instrumentation itself is out of scope (separate story, probably in Epic 11 CI hardening).

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
   **And** when the instrumentation is NOT registered, the test emits an xUnit `Skip` trait for this specific assertion with reason `"Redis OTEL instrumentation not registered"` — the AC #1 invariant still gates test pass/fail.

3. **Audit-log capture from stdout (FR67 authoritative gate).**
   **Given** the fixture is running with the Server container's stdout available via the Aspire container API,
   **When** one operation of each instrumented type runs (`search`, `ingest`, `traverse`, `case-access`) against a provisioned tenant,
   **Then** the stdout log stream contains exactly one structured JSON line per operation that parses as an `AccessTelemetryEvent`
   **And** each parsed event has `schemaVersion == 1`, `eventId` in `7500-7599`, non-empty `tenantId`, matching `operationType`, non-empty `traceId` + `spanId`, and a `durationMs >= 0`
   **And** health-endpoint probes (`/health`, `/alive`, `/ready`) produce zero `AccessTelemetryEvent` entries in the same window (regression guard for Story 7.5 AC #5).

4. **TraceId cross-reference between span and audit event.**
   **Given** AC #1's captured `memories.search` activity and AC #3's search `AccessTelemetryEvent`,
   **When** both are compared,
   **Then** `auditEvent.TraceId == searchActivity.TraceId.ToString()` AND `auditEvent.SpanId == searchActivity.SpanId.ToString()` — same invariant Story 7.5 Tier-2 asserts, now proven end-to-end across the real deployed stack.

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
    - [ ] 0.1 Build `Hexalith.Memories.AppHost` and run the existing `AspireIngestionPipelineFixture` smoke test. If the Story 5.6 Dev Notes `CS0311 on IDaprSidecarResource` error reproduces, diagnose and fix (likely package alignment) before Task 1. Exit-criterion: fixture boots a full Aspire environment and the smoke test passes.

- [ ] **Task 1: Test-side in-memory OTLP capture.** (AC: #1, #5)
    - [ ] 1.1 Spike the env-var-triggered in-memory processor approach: add a Server-side code path that, when `HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1` is set in the environment, appends an `InMemoryExporter<Activity>` / `InMemoryExporter<LogRecord>` to the existing OpenTelemetry pipeline and exposes the collected items via a static shared buffer (thread-safe; reset per test via a public helper method).
    - [ ] 1.2 If (1.1) is infeasible (Aspire rewrites service discovery for the container's OTLP exporter in a way that breaks loopback), fall back to a test-process in-memory gRPC OTLP receiver: `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/InMemoryOtlpReceiver.cs` listening on a dynamic localhost port; configure the Server container's `OTEL_EXPORTER_OTLP_ENDPOINT` to point at it. Document the chosen path in ADR-8.4-002.
    - [ ] 1.3 Unit test the chosen capture mechanism in isolation: `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/OtlpCaptureMechanismTests.cs` asserts that with the trigger set, activities are captured; without it, nothing is appended.
    - [ ] 1.4 Verify AC #5: run the existing `OpenTelemetryRegistrationTests` + `ServiceDefaults` smoke — both MUST pass unchanged when the trigger is not set.

- [ ] **Task 2: `AspireEndToEndTraceTests`.** (AC: #1, #2, #4)
    - [ ] 2.1 Create `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` `[Trait("Category", "Integration")]`. Reuse the `AspireIngestionPipelineFixture` fixture.
    - [ ] 2.2 Test case `CliSearch_EndToEnd_SingleTraceIdAcrossAllHops`: provision a tenant via the fixture helper; set `HEXALITH_MEMORIES_OTEL_ENDPOINT` to the in-memory capture endpoint; invoke `MemoriesClient.SearchAsync(...)` via the fixture's DI root (Story 7.1 anti-pattern #8 — NOT a subprocess); capture all activities; assert the CLI root span + HttpClient span + AspNetCore span + `memories.search` activity share one `TraceId`; assert parent-child chain.
    - [ ] 2.3 Test case `CliSearch_EndToEnd_RedisInstrumentation_OptionalSpan`: if `StackExchange.Redis.Extensions.OpenTelemetry` is on the `Hexalith.Memories.Server` references, assert a Redis span in the same trace; otherwise `Skip.If(!RedisOtelPresent, reason)`. Use reflection on the Server's `TracerProvider` to detect registration — do NOT inspect the Server's csproj directly (fragile).
    - [ ] 2.4 Test case `CliSearch_AuditEvent_TraceIdMatchesSpan` (AC #4): uses capture from 2.2; asserts the Server-emitted `AccessTelemetryEvent` (pulled from the in-memory log exporter OR from container stdout — pick the simpler capture) carries matching `TraceId` + `SpanId` from the `memories.search` activity.

- [ ] **Task 3: `AuditLogStreamIntegrationTests`.** (AC: #3)
    - [ ] 3.1 Create `tests/Hexalith.Memories.IntegrationTests/Telemetry/AuditLogStreamIntegrationTests.cs` `[Trait("Category", "Integration")]`. Helper `AsyncEnumerable<string> ReadServerLogStream(CancellationToken)` against the fixture's container API; filters to lines that parse as JSON AND contain an `eventId` in `7500-7599`.
    - [ ] 3.2 Test case `SearchOperation_EmitsOneAuditEvent_WithAC4Schema`: provision tenant; run one search; assert one audit event matching AC #4 schema (schemaVersion=1, operationType="search", durationMs >= 0, non-empty traceId + spanId, non-null timestamp).
    - [ ] 3.3 Test cases (3 total) for ingest / traverse / case-access — same structure.
    - [ ] 3.4 Test case `HealthProbes_EmitZeroAuditEvents` (regression guard per AC #3 + Story 7.5 AC #5): run 5 consecutive `/health` probes via the fixture's HTTP client; assert zero `AccessTelemetryEvent` entries in the window.
    - [ ] 3.5 Test case `SchemaVersion_IsOneForAllEmittedEvents`: aggregate across tests 3.2-3.3; assert every captured event has `schemaVersion == 1` (future-proofing — fails loudly if a breaking field change slips in).

- [ ] **Task 4: CI wiring.** (AC: #6)
    - [ ] 4.1 Update `.github/workflows/` to route `[Trait("Category","Integration")]` tests to a Docker-provisioned merge-queue lane, NOT the default per-PR CI job. Reference the existing Category-based filter convention (Stories 7.1-7.4 established `Category!=Integration` on default runs).
    - [ ] 4.2 ADR-8.4-001 at `docs/dev/telemetry.md`: one-paragraph decision record on Tier-2 (PR gate) vs Tier-3 (release gate) lane split. Cite CI-minute cost + Docker-availability constraints.
    - [ ] 4.3 If Epic 11 (CI/CD) has already landed the merge-queue workflow, verify 8.4's tests run green in that lane. If not, flag as a soft dependency and document in 8.4's Dev Notes.

- [ ] **Task 5: Documentation.** (AC: #7)
    - [ ] 5.1 Extend `docs/dev/telemetry.md` with a new "End-to-end trace verification" section covering the tier split, how to run locally, and how to interpret failures.
    - [ ] 5.2 Add a "Tier split" table to the doc:

      | Tier | Runs on | Gates | Story |
      | ---- | ------- | ----- | ----- |
      | Tier-1 (unit) | Every PR | Substrate correctness | 7.5 Tasks 8-10 |
      | Tier-2 (WebApplicationFactory) | Every PR | In-process trace + audit invariants | 7.5 Tasks 11.1, 11.2; 9.1 |
      | Tier-3 (Aspire fixture) | Merge-queue lane | End-to-end NFR28 + FR67 gate | **8.4 (this story)** |

    - [ ] 5.3 Cross-link Story 7.5's Tier-2 variants so a future dev reading 7.5's Change Log Rev 1.3/1.4 finds 8.4 as the closure.

- [ ] **Task 6: Cleanup and close out.**
    - [ ] 6.1 Update Story 7.5's Review Findings + Tasks 11.3 + 11.4 to `[x]` with a reference to this story's merge commit.
    - [ ] 6.2 Update `sprint-status.yaml`: `8-4-end-to-end-telemetry-integration-tests: done` (or `review` pending merge).
    - [ ] 6.3 Update Epic 7 retrospective note to confirm Gate 3 (Developer Experience + Operational Observability) is now end-to-end gated, not just substrate-gated.

## Dev Notes

### Inherited from Story 7.5 (do not re-derive)

- All ADRs from 7.5 apply unchanged: ADR-7.5-001..005.
- Substrate constants (`MemoriesActivitySource.SourceName`, `MemoriesMeter.Name`, EventId bank `7500-7599`, `RejectedTenantTag = "__rejected__"`).
- `AccessTelemetryEvent` schema version policy: additive fields stay at `schemaVersion=1`, breaking changes bump.
- Health-endpoint trace exclusion: preserve `Extensions.ShouldTraceHttpRequest` filter byte-identical.

### Implementation contracts for 8.4

- **Tier-2 tests are canonical.** If a Tier-3 test fails in a way a Tier-2 test would have caught, the fix lives at Tier-2 and the Tier-3 test stays as the gate. Do NOT let Tier-3 drift into substrate-testing territory.
- **No subprocess CLI invocations.** Story 7.1 anti-pattern #8 holds: the CLI is invoked via DI, in-process, inside the test's assembly. The CLI's `Program.Main` is NOT spawned as a child process — if it were, the in-memory exporter would not share process with the CLI's emitting spans.
- **Fixture fixture sharing.** Reuse the `AspireIngestionPipelineFixture` as an xUnit `IClassFixture` or collection fixture; do NOT boot a fresh Aspire environment per test class (cost prohibitive). Telemetry collector state is reset per test via a public helper on the in-memory capture mechanism.

### Test fixture cleanup

The Aspire fixture is ephemeral per run (Story 7.4 convention); audit events reference tenants that exist only for the fixture lifetime, so no cleanup is required.

### References

- Story 7.5 Section: Tasks 11.3, 11.4 (the explicit deferral source).
- Story 7.5 Risk #4: Trace propagation across DAPR hops.
- Story 7.5 Risk #8: Health-endpoint filter regression.
- Story 7.5 Change Log Rev 1.3 + Rev 1.4: the Tier-2 closure that 8.4 extends.
- Architecture line 82: `"OpenTelemetry traces MUST propagate across all DAPR hops"`.
- Architecture line 152: DAPR sidecar auto-restart pattern (relevant to Tier-3 fixture boot).

## Dev Agent Record

### Agent Model Used

(Populated at dev time.)

### File List

(Populated at dev time — new + modified files.)

### Change Log

| Date       | Version | Description                                                                                                                                          |
| :--------- | :------ | :--------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-04-18 | 0.1     | Story context created as documented follow-up from Story 7.5 Rev 1.4. Status: backlog. Covers Story 7.5 Tasks 11.3 + 11.4 plus the CI gating ADR.    |
