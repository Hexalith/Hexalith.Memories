# Story 8.5: Redis OTEL Instrumentation & Story 8.4 AC #2 Hardening

Status: backlog

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Plain-English summary

Story 8.4 shipped the Tier-3 end-to-end telemetry tests, but one piece was left as a documented follow-up: **proving that Redis client calls (RediSearch, Redis Vector, and FalkorDB's Redis-protocol endpoint) emit OpenTelemetry spans inside the same distributed trace as the CLI → Server hop.** Right now they don't, because no Redis-OTEL instrumentation is registered in `ServiceDefaults.ConfigureOpenTelemetry`. Story 8.4 put a **self-expiring soft-skip** in place (`Ac2RedisSkipReviewBy.ReviewByDate = 2026-10-01`) so nobody silently forgets. This story registers the instrumentation, flips Story 8.4's AC #2 from soft-skip into a hard assertion, and retires the skip infrastructure. **Tracking:** GitHub issue [#9](https://github.com/Hexalith/Hexalith.Memories/issues/9).

## TL;DR

**What ships:**

1. Redis OTEL instrumentation registered on the Memories Server's tracer pipeline in `src/Hexalith.Memories.ServiceDefaults/Extensions.cs#ConfigureOpenTelemetry`, covering both `IConnectionMultiplexer` connections (`redis` — shared RediSearch + Redis Vector; `falkordb` — Redis-protocol graph DB).
2. Story 8.4's AC #2 soft-skip (`Ac2RedisSkipReviewBy.*`) **retired**. `AspireEndToEndTraceTests.CliSearch_EndToEnd_SingleTraceIdAcrossAllHops` gains a hard assertion that at least one Redis client span (per-connection-tagged) appears in the captured trace with a matching `TraceId`.
3. Tier-2 unit coverage in `tests/Hexalith.Memories.Server.Tests/Telemetry/` that pins the Redis instrumentation registration without Docker (asserts the tracer's `ActivitySource` list includes the Redis source).
4. `docs/dev/telemetry.md` updated: removes the "skip until 2026-10-01" language; adds a Redis-span row to the "what each captured signal proves" table; removes ADR-8.4-002's AC #2 deferral caveat.

**What already exists:**

1. **Two keyed `IConnectionMultiplexer` registrations** — `redis` (in `Hexalith.Memories.Redis` composition) and `falkordb` (in `Hexalith.Memories.Server` composition). Reuse as-is; instrumentation attaches by `ActivitySource`, not by connection identity, but per-connection disambiguation uses the OTEL resource attributes already wired on Aspire resources.
2. **`ConfigureOpenTelemetry<TBuilder>`** at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:37-92`. Already configures `WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource(MemoriesActivitySource.SourceName)...)`. Extend the `WithTracing` lambda; do NOT replace it.
3. **Story 8.4 Tier-3 fixture** — `AspireIngestionPipelineFixture` + `AspireEndToEndTraceTests` + `AuditEventStreamReader` + `ServerActivityStreamReader`. The `ServerActivityStreamReader` already surfaces Server-side activities via stderr breadcrumbs; the Redis spans flow through the same path (the `IntegrationActivityProcessor` filter in `ServiceDefaults.Extensions` decides which sources to emit — see Task 2 below).
4. **Self-expiring skip infrastructure** to retire — `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/Ac2RedisSkipReviewBy.cs` + `tests/Hexalith.Memories.IntegrationTests/Telemetry/Ac2SkipReviewByTests.cs`.

**What 8.5 adds:**

1. **Package reference** — pick one at implementation time based on which tracks the current `StackExchange.Redis 2.x` cleanly:
   - `OpenTelemetry.Instrumentation.StackExchangeRedis` (official, currently `1.0.0-rc.9` / experimental tag as of 2026-04).
   - `StackExchange.Redis.Extensions.OpenTelemetry` (community, tracks `StackExchange.Redis.Extensions`).

   Both produce spans with source `"OpenTelemetry.Instrumentation.StackExchangeRedis"`. Task 1 spike decides and pins the choice in an ADR addendum to `docs/dev/telemetry.md`.
2. **Registration call** inside `ConfigureOpenTelemetry` — resolves the `IConnectionMultiplexer` keyed services and attaches the instrumentation to each. The instrumentation must be added BEFORE the `CollectingActivityProcessor` env-var branch so test-side capture catches the new source.
3. **`IntegrationActivityProcessor` filter update** — the existing stderr breadcrumb filter (`ShouldEmitActivityBreadcrumb` at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` around line 578) currently exact-matches `"Microsoft.AspNetCore"`. Extend to also emit the Redis source so Tier-3 tests can reach the span via `ServerActivityStreamReader`.
4. **Hard assertion** in `AspireEndToEndTraceTests.CliSearch_EndToEnd_SingleTraceIdAcrossAllHops`:
   - ≥1 captured activity with `Source.Name == "OpenTelemetry.Instrumentation.StackExchangeRedis"` (or whichever source name Task 1 pins).
   - Same `TraceId` as the AspNetCore server span.
   - Parent chain reaches the CLI root via the same `AssertParentChainReachesCliRoot` traversal helper (cycle detection + depth ceiling already in place).
5. **Removal** of `Ac2RedisSkipReviewBy.cs` + `Ac2SkipReviewByTests.cs` + the `telemetry.redis.instrumentation.skipped` log line (if emitted) + any remaining "deferred" / "skip review-by" text in Story 8.4 and `docs/dev/telemetry.md`.

**What does NOT ship:**

- **Redis metric instrumentation.** Tracer-only. Redis command counts / latency histograms are a separate concern if ever needed.
- **FalkorDB-specific span semantics.** FalkorDB speaks Redis protocol, so the generic Redis spans cover its commands (`GRAPH.QUERY`, `GRAPH.LIST`, etc.). A FalkorDB-specific `memories.graph.*` activity source is out of scope.
- **Sampling policy changes.** Continues using `AlwaysOnSampler` in Tier-3 tests; production samplers unchanged.
- **Per-command filtering.** If the Redis instrumentation floods the trace with housekeeping pings (`PING`, `ECHO`), filter at the reader level in tests, not at instrumentation level in production.

**Primary risks:**

1. **Package experimental tag.** `OpenTelemetry.Instrumentation.StackExchangeRedis` has been `rc.9` for a long time. **Mitigation:** Task 1 evaluates both candidates and picks the one with cleaner `StackExchange.Redis 2.x` compatibility. If neither is acceptable, document in the ADR and revert to shipping nothing + re-deferring AC #2 with a new review-by date.
2. **Aspire dashboard OTLP noise.** Story 8.4 observed Aspire dashboard OTLP exporter timeout warnings (`127.0.0.1:4317 actively refused`) that are benign. Registering Redis instrumentation increases the span volume; ensure the timeout noise does not cascade into test flakes. **Mitigation:** the Tier-3 tests continue to use the in-memory collector + audit-log readers, not the OTLP dashboard exporter; Redis span capture traverses the same `IntegrationActivityProcessor` path that Story 8.4 proved end-to-end.
3. **Span explosion floods `ServerActivityStreamReader`.** Each Redis command emits a span; a single search triggers many backend calls. The existing stderr breadcrumb capture may balloon log output. **Mitigation:** Task 2 tightens the breadcrumb filter to only emit Redis spans whose parent chain reaches `memories.search` or `memories.cli.invoke` — incidental housekeeping Redis activity is ignored.
4. **Cross-connection attribution ambiguity.** Both `redis` and `falkordb` emit spans from the same OTEL source. Distinguishing them requires reading `Activity.GetTagItem("db.name")` or `server.address`. **Mitigation:** Task 2.5 decides whether to assert presence of BOTH connection attributions or just ≥1 Redis span. Default: ≥1 Redis span (conservative) — per-connection attribution becomes a follow-up if operator feedback demands it.

## Story

As the Memories release manager,
I want Redis client calls (RediSearch, Redis Vector, and FalkorDB) to emit OpenTelemetry spans inside the same distributed trace as the originating request,
so that operators can attribute search / traverse latency to the correct backend — and so Story 8.4's AC #2 stops being a self-expiring soft-skip.

## Acceptance Criteria

1. **Redis OTEL instrumentation registered.**
   **Given** the Memories Server is running via Aspire or a plain `dotnet run`,
   **When** a search / ingest / traverse / case-access request runs,
   **Then** at least one `Activity` whose `Source.Name` equals the Redis OTEL source (as pinned by Task 1's ADR — `"OpenTelemetry.Instrumentation.StackExchangeRedis"` at candidate time) is emitted per backend Redis call
   **And** each such `Activity` shares the `TraceId` of the originating AspNetCore request
   **And** the registration covers both keyed connections (`redis` for RediSearch + Redis Vector, `falkordb` for the graph DB).

2. **Story 8.4 AC #2 flipped to hard assertion.**
   **Given** the Tier-3 fixture boots the Memories Server with the new instrumentation,
   **When** `AspireEndToEndTraceTests.CliSearch_EndToEnd_SingleTraceIdAcrossAllHops` runs,
   **Then** at least one Redis-source activity is captured in the end-to-end trace (same `TraceId` as the AspNetCore server span + parent chain reaches the CLI root)
   **And** the `Ac2RedisSkipReviewBy` helper + `Ac2SkipReviewByTests` + `telemetry.redis.instrumentation.skipped` log line are deleted
   **And** Story 8.4's AC #2 text and DoD item 10 are updated to reflect hard-assertion-as-shipped
   **And** `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/Ac2RedisSkipReviewBy.cs` and its unit tests no longer exist in the repo.

3. **Tier-2 registration pin.**
   **Given** `OpenTelemetryRegistrationTests` in `tests/Hexalith.Memories.Server.Tests/Telemetry/`,
   **When** a new test `TracerRegistration_IncludesRedisInstrumentationSource` runs,
   **Then** the registered `TracerProvider` (inspected via the `ActivityListener` + `ActivitySource.StartActivity` pattern used elsewhere in the suite) sees activities from the Redis OTEL source
   **And** the test runs without Docker and without Aspire — pure unit-tier, `[Trait("Category", "Unit")]`.

4. **No regression on Story 7.5 substrate or 8.4 Tier-3 invariants.**
   **Given** the full Tier-2 telemetry suite + the 8.4 Tier-3 suite,
   **When** they run on the same runner as before,
   **Then** all pre-existing passes stay green (Server.Tests Tier-2 telemetry 128/128 + IntegrationTests Tier-3 telemetry 8/8).

5. **Documentation refreshed.**
   **Given** `docs/dev/telemetry.md`,
   **When** a reader opens the "End-to-end trace verification" section,
   **Then** the "what each captured signal proves" table gains a Redis-span row
   **And** the ADR-8.4-002 AC #2 deferral language is rewritten to reflect that the Redis hard-assertion ships via Story 8.5
   **And** the `telemetry.redis.instrumentation.skipped` warning line description is removed
   **And** the "skip until 2026-10-01" language is removed.

## Tasks / Subtasks

- [ ] **Task 1: Package evaluation spike + ADR.** (AC: #1)
    - [ ] 1.1 Evaluate `OpenTelemetry.Instrumentation.StackExchangeRedis` (official, experimental tag) and `StackExchange.Redis.Extensions.OpenTelemetry` (community) against current `StackExchange.Redis` and `.NET 10` versions in `Directory.Packages.props`. Build a throwaway branch that references each, runs the existing Tier-2 telemetry suite, and observes span emission.
    - [ ] 1.2 Pick one. Add an ADR addendum section to `docs/dev/telemetry.md` (ADR-8.5-001) with one sentence of rationale per candidate and the chosen package + version.
    - [ ] 1.3 Add the package to `Directory.Packages.props` (central version management).

- [ ] **Task 2: Register instrumentation in ServiceDefaults.** (AC: #1, #3)
    - [ ] 2.1 Extend `ConfigureOpenTelemetry<TBuilder>` in `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` with the Redis instrumentation call inside the `WithTracing` lambda. Place BEFORE the `CollectingActivityProcessor` env-var branch so the test-side capture sees the source.
    - [ ] 2.2 Resolve the keyed `IConnectionMultiplexer` connections (`redis`, `falkordb`) and pass both into the instrumentation registration so spans attribute to the correct backend. Exact wiring depends on Task 1's chosen package.
    - [ ] 2.3 Extend `IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb` (or equivalent filter) to include the Redis source name — but gate emission on parent-chain reachability from `memories.search` / `memories.cli.invoke` (Risk 3 mitigation).
    - [ ] 2.4 Add `TracerRegistration_IncludesRedisInstrumentationSource` to `OpenTelemetryRegistrationTests` (Tier-2, no Docker). Assert `ActivityListener` sees activities from the Redis source after a mock `IConnectionMultiplexer` command.
    - [ ] 2.5 Decide per-connection attribution assertion shape: ≥1 Redis span (default) vs ≥1 per keyed connection. Default-pick documented in ADR-8.5-001. If a future operator-feedback issue demands per-connection, extend then.

- [ ] **Task 3: Flip Story 8.4 AC #2 to hard assertion.** (AC: #2)
    - [ ] 3.1 Update `AspireEndToEndTraceTests.CliSearch_EndToEnd_SingleTraceIdAcrossAllHops` — add a Redis-source activity count-first gate (`Count >= 1`), a `TraceId`-match assertion, and a parent-chain reachability assertion. Reuse `AssertParentChainReachesCliRoot` with the max-depth + cycle-detection already in place.
    - [ ] 3.2 Delete `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/Ac2RedisSkipReviewBy.cs`.
    - [ ] 3.3 Delete `tests/Hexalith.Memories.IntegrationTests/Telemetry/Ac2SkipReviewByTests.cs`.
    - [ ] 3.4 Remove the `telemetry.redis.instrumentation.skipped` warning-level log line (if emitted anywhere — grep for `redis.instrumentation.skipped` across the repo).
    - [ ] 3.5 Update Story 8.4 (`_bmad-output/implementation-artifacts/8-4-end-to-end-telemetry-integration-tests.md`):
        - AC #2 text: remove "skip" / "informational-only" / "review-by" language; replace with hard-assertion wording mirroring AC #1.
        - DoD item 10: flip to "✓ — Redis OTEL instrumentation shipped via Story 8.5, AC #2 hard-asserted".
        - Tasks 2.3 + 7: strikethrough with reference to Story 8.5 merge commit.
        - Change Log: add Rev 1.0 entry "Story 8.5 closed the AC #2 deferral; Redis spans proven end-to-end."

- [ ] **Task 4: Documentation refresh.** (AC: #5)
    - [ ] 4.1 Update `docs/dev/telemetry.md` "End-to-end trace verification" section:
        - Add Redis-span row to the "what each captured signal proves" table.
        - Remove the "AC #2 skip until 2026-10-01" callout.
        - Rewrite the ADR-8.4-002 addendum that mentioned AC #2 deferral.
        - Add ADR-8.5-001 (Task 1's package choice) as a peer of ADR-8.4-001 / -002 / -003.
    - [ ] 4.2 Update `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/AuditEventStreamReader.cs` XML-doc comments if they mention the skip path — keep behavior unchanged.

- [ ] **Task 5: Regression validation.** (AC: #4)
    - [ ] 5.1 Run full Tier-2 telemetry suite: `dotnet test tests/Hexalith.Memories.Server.Tests/ --filter "FullyQualifiedName~Telemetry"`. Expect 128+1 passing (existing 128 + new `TracerRegistration_IncludesRedisInstrumentationSource`).
    - [ ] 5.2 Run Tier-3 telemetry suite (Docker required): `dotnet test tests/Hexalith.Memories.IntegrationTests/ --filter "FullyQualifiedName~AspireEndToEndTraceTests|FullyQualifiedName~AuditLogStreamIntegrationTests"`. Expect 8/8 green with the new Redis-span assertion in `CliSearch_EndToEnd_SingleTraceIdAcrossAllHops`.
    - [ ] 5.3 Run `bash ./tools/test.sh --filter "Category!=Integration"` to confirm the ~1296+ unit-tier slice stays green.
    - [ ] 5.4 Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: set `8-5-redis-otel-instrumentation: review` after Tasks 1-4 complete + Task 5.1-5.3 green.

- [ ] **Task 6: Close GitHub issue #9.**
    - [ ] 6.1 After merge, close `https://github.com/Hexalith/Hexalith.Memories/issues/9` with a reference to the merge commit.

## Dev Notes

### Inherited from Story 8.4 (do not re-derive)

- **All ADRs from 8.4 apply unchanged**: ADR-8.4-001 (CI lane split — merge-queue + nightly bridge), ADR-8.4-002 (test-only `InMemorySpanCollector` placement), ADR-8.4-003 (audit-event capture path per test — both tests use container stdout).
- **Cross-process Aspire reality**: `DistributedApplicationTestingBuilder` runs the Server as a separate process. Test-side capture wires via stderr activity breadcrumbs (`ServerActivityStreamReader`) and audit-log stdout (`AuditEventStreamReader`), NOT via in-test-process OTEL exporters. Redis spans flow through the same stderr breadcrumb path.
- **Count-first assertion convention**: any captured collection asserts `Count >= expected` before predicate checks (Story 8.4 Risk 1 codification). The new Redis-span assertion follows the same convention.
- **Activity parent-chain traversal**: `AssertParentChainReachesCliRoot` already enforces cycle detection (visited-set) + max-depth = 16 + root-termination at `memories.cli.invoke`. Reuse as-is.
- **Force-flush both pipelines**: tests calling the Tier-3 path continue to call `TracerProvider.ForceFlushAsync(TimeSpan.FromSeconds(2))` before reading the collectors.

### Implementation contracts for 8.5

- **No production-visible API changes.** The Redis instrumentation registration lives entirely inside `ConfigureOpenTelemetry`'s `WithTracing` lambda. External consumers of `Hexalith.Memories.Server` / `.ServiceDefaults` see no new types.
- **No new env vars.** The instrumentation is always-on when `ConfigureOpenTelemetry` runs — same as AspNetCore instrumentation. No feature flag.
- **Breadcrumb filter narrowness.** `IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb` currently exact-matches `"Microsoft.AspNetCore"`. Widen to also accept the Redis OTEL source, but add a parent-chain reachability predicate so incidental Redis activity (health-probe housekeeping, connection pool maintenance) does NOT flood stderr. Pin the filter via a new `ShouldEmitActivityBreadcrumbTests` if one doesn't exist.
- **Deletion discipline.** When deleting `Ac2RedisSkipReviewBy.cs` + `Ac2SkipReviewByTests.cs`, also grep for string references — the class name may appear in `docs/dev/telemetry.md`, in Story 8.4's Task 7 description, and in any remaining test class header comments.

### Rejected alternatives

- **Ship a new env-var feature flag** (e.g., `HEXALITH_MEMORIES_REDIS_OTEL_DISABLED=1`). Rejected — unnecessary surface. The instrumentation is either wanted or not; feature-flagging it adds a path no operator would enable by default.
- **Write a custom `IActivitySource`-based Redis instrumentation.** Rejected — reinvents what the two candidate packages provide. Use the ecosystem implementation.
- **Only instrument the `redis` connection, not `falkordb`.** Rejected — FalkorDB speaks Redis protocol and any graph-query latency should attribute to the same trace. The instrumentation attaches per-multiplexer; skipping one connection buys nothing and confuses operators.

### Latest technical specifics

- **Candidate packages at authoring time (2026-04-22):**
    - `OpenTelemetry.Instrumentation.StackExchangeRedis` — official Anthropic/OpenTelemetry project package, typically at `1.0.0-rc.N` with experimental tag. Source name: `"OpenTelemetry.Instrumentation.StackExchangeRedis"`.
    - `StackExchange.Redis.Extensions.OpenTelemetry` — community-maintained; tracks `StackExchange.Redis.Extensions`. Source name differs (check at implementation time).
- **`StackExchange.Redis`**: current pinned version is in `Directory.Packages.props`; verify compatibility at Task 1.
- **Aspire 13.1.3**: `DistributedApplicationTestingBuilder` + project-as-process model unchanged from Story 8.4. No new fixture work required.
- **W3C TraceContext propagation**: the `traceparent` header transits from CLI → Server via HttpClient instrumentation (already in place). Redis spans inherit the server's `Activity.Current` so they automatically share the `TraceId` — no extra propagation plumbing needed.

### References

- GitHub issue: [#9 — Story 8.4 follow-up: Register Redis OTEL instrumentation + harden AC #2 Redis span assertion](https://github.com/Hexalith/Hexalith.Memories/issues/9)
- Story 8.4 (parent): `_bmad-output/implementation-artifacts/8-4-end-to-end-telemetry-integration-tests.md`
- Story 8.4 AC #2 text: lines 77-83 of the parent story
- Story 8.4 ADR-8.4-002 (capture model): `docs/dev/telemetry.md`
- Self-expiring skip helper (to retire): `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/Ac2RedisSkipReviewBy.cs`
- Unit tests for skip helper (to retire): `tests/Hexalith.Memories.IntegrationTests/Telemetry/Ac2SkipReviewByTests.cs`
- ServiceDefaults telemetry wiring: `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:37-92`
- Story 8.4 Tier-3 test classes (to extend): `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs`
- Tier-2 registration test suite (to extend): `tests/Hexalith.Memories.Server.Tests/Telemetry/OpenTelemetryRegistrationTests.cs`

### Definition of Done

1. Tasks 1-5 complete with all subtasks checked.
2. `OpenTelemetryRegistrationTests.TracerRegistration_IncludesRedisInstrumentationSource` green on the per-PR lane.
3. `AspireEndToEndTraceTests.CliSearch_EndToEnd_SingleTraceIdAcrossAllHops` green on the nightly bridge lane with the new hard Redis-span assertion; Story 8.4's remaining Tier-3 cases stay green.
4. `Ac2RedisSkipReviewBy.cs` + `Ac2SkipReviewByTests.cs` + any `redis.instrumentation.skipped` signal **deleted from the repo**.
5. Story 8.4's AC #2 + DoD item 10 updated in source to reflect hard-assertion-as-shipped; Change Log entry added.
6. `docs/dev/telemetry.md` refreshed per Task 4.
7. Sprint-status.yaml set to `review`, then `done` on merge.
8. GitHub issue #9 closed with a reference to the merge commit (Task 6).

## Dev Agent Record

### Agent Model Used

_Not yet developed._

### Debug Log References

_Not yet developed._

### Completion Notes List

_Not yet developed._

### File List

_Not yet developed._

### Change Log

| Date       | Version | Description                                                                                                                                                           |
| :--------- | :------ | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-04-22 | 0.1     | Story context created as follow-up to Story 8.4's deferred AC #2 (Tasks 2.3 + 7.3). Linked to GitHub issue #9. Placed under Epic 8 (Observability & System Health). Status: backlog. |
