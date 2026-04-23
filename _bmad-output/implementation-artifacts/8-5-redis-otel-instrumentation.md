# Story 8.5: Redis OTEL Instrumentation & Story 8.4 AC #2 Hardening

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Plain-English summary

Story 8.4 shipped the Tier-3 end-to-end telemetry tests, but one piece was left as a documented follow-up: **proving that Redis client calls (RediSearch, Redis Vector, and FalkorDB's Redis-protocol endpoint) emit OpenTelemetry spans inside the same distributed trace as the CLI → Server hop.** Right now they don't, because no Redis-OTEL instrumentation is registered in `ServiceDefaults.ConfigureOpenTelemetry`. Story 8.4 put a **self-expiring soft-skip** in place (`Ac2RedisSkipReviewBy.ReviewByDate = 2026-10-01`) so nobody silently forgets. This story registers the instrumentation, flips Story 8.4's AC #2 from soft-skip into a hard assertion, and retires the skip infrastructure. **Customer outcome:** reduces MTTR on search-latency incidents by enabling per-backend span attribution in Grafana / Datadog / the Aspire dashboard. Operators can distinguish whether a slow search is bounded by BM25 (RediSearch), vector (Redis Vector), graph (FalkorDB), or the ingestion pipeline — today that breakdown is invisible, so on-call guesses or adds ad-hoc logging post-hoc. **Tracking:** GitHub issue [#9](https://github.com/Hexalith/Hexalith.Memories/issues/9).

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

1. **Package reference — pinned to `OpenTelemetry.Instrumentation.StackExchangeRedis 1.15.1-beta.1`** (latest as of 2026-04-21, targets `net8.0` + `net10.0`, requires `StackExchange.Redis >= 2.6.122` — `Directory.Packages.props:44` pins `StackExchange.Redis 2.12.4`, so compatible). Package is still `-beta.N` / prerelease: the ADR-8.5-001 addendum must explicitly accept the prerelease tag and document the upgrade-on-GA policy. **ActivitySource name (verified against upstream source):** `"OpenTelemetry.Instrumentation.StackExchangeRedis"` (assembly-name-derived constant at `StackExchangeRedisConnectionInstrumentation.ActivitySourceName`). Individual span `OperationName` is `"OpenTelemetry.Instrumentation.StackExchangeRedis.Execute"`. The community `StackExchange.Redis.Extensions.OpenTelemetry` candidate is REJECTED — it tracks `StackExchange.Redis.Extensions` (a different higher-level wrapper Hexalith does not use) and adds an unused dependency surface.
2. **Registration call inside `ConfigureOpenTelemetry`** — the official package ships keyed-service overloads (`AddRedisInstrumentation(object serviceKey)`) that internally call `AddSource(...)` AND bind the `IConnectionMultiplexer` from the DI container via `IKeyedServiceProvider.GetKeyedService`. Since Hexalith registers two keyed connections (`"redis"` and `"falkordb"` at `src/Hexalith.Memories.Server/Program.cs:97-100`), register BOTH:
   ```csharp
   .AddRedisInstrumentation("redis")
   .AddRedisInstrumentation("falkordb")
   ```
   Do NOT also call `.AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis")` — the overload already does that internally; a duplicate `AddSource` is idempotent but noisy. The registration must live inside the existing `WithTracing` lambda at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:59-64`, placed BEFORE `AddOpenTelemetryExporters` so the Tier-3 test-side `IntegrationActivityProcessor` sees the new source.
3. **`IntegrationActivityProcessor` filter update** — the existing stderr breadcrumb filter (`ShouldEmitActivityBreadcrumb` at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:175-178`) currently emits for `MemoriesActivitySource.SourceName` OR (`ActivityKind.Server` AND `"Microsoft.AspNetCore"`). Extend to also emit the Redis source `"OpenTelemetry.Instrumentation.StackExchangeRedis"` — but ONLY when the span's parent-chain walk (max-depth 16) reaches an ancestor in `{MemoriesActivitySource.SourceName, "Microsoft.AspNetCore"}`. Orphan Redis activity (connection-pool housekeeping, idle `PING` traffic) is silently dropped. See Task 2.3 + 2.6.
4. **Hard assertion** in `AspireEndToEndTraceTests.CliSearch_EndToEnd_SingleTraceIdAcrossAllHops`:
   - ≥1 captured activity with `Source.Name == "OpenTelemetry.Instrumentation.StackExchangeRedis"` (source pinned in "What 8.5 adds" #1 above).
   - Same `TraceId` as the AspNetCore server span.
   - Parent chain reaches the CLI root via the same `AssertParentChainReachesCliRoot` traversal helper (cycle detection + depth ceiling already in place).
5. **Removal** of `Ac2RedisSkipReviewBy.cs` + `Ac2SkipReviewByTests.cs` + the `telemetry.redis.instrumentation.skipped` log line (if emitted) + any remaining "deferred" / "skip review-by" text in Story 8.4 and `docs/dev/telemetry.md`.

**What does NOT ship:**

- **Redis metric instrumentation.** Tracer-only. Redis command counts / latency histograms are a separate concern if ever needed.
- **FalkorDB-specific span semantics.** FalkorDB speaks Redis protocol, so the generic Redis spans cover its commands (`GRAPH.QUERY`, `GRAPH.LIST`, etc.). A FalkorDB-specific `memories.graph.*` activity source is out of scope.
- **Sampling policy changes.** Continues using `AlwaysOnSampler` in Tier-3 tests; production samplers unchanged.
- **Per-command filtering.** If the Redis instrumentation floods the trace with housekeeping pings (`PING`, `ECHO`), filter at the reader level in tests, not at instrumentation level in production.

**Primary risks:**

1. **Package prerelease tag.** `OpenTelemetry.Instrumentation.StackExchangeRedis 1.15.1-beta.1` is still `-beta.N`. Breaking changes are possible before GA. **Mitigation:** pin the exact version in `Directory.Packages.props`; accept the prerelease tag explicitly in ADR-8.5-001; add a `NoWarn=NU5104` (or equivalent) on the consuming `.csproj` if MSBuild rejects the prerelease pin on a production-flagged package; add a task-level follow-up note to revisit when stable ships.
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
   **Then** at least one `Activity` whose `Source.Name == "OpenTelemetry.Instrumentation.StackExchangeRedis"` (the pinned `StackExchangeRedisConnectionInstrumentation.ActivitySourceName` from the upstream package) is emitted per backend Redis call
   **And** each such `Activity` shares the `TraceId` of the originating AspNetCore request
   **And** the registration covers BOTH keyed connections (`redis` for RediSearch + Redis Vector, `falkordb` for the graph DB) via two `.AddRedisInstrumentation(serviceKey)` calls as pinned in ADR-8.5-001.

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
   **Then** the registered `TracerProvider` exposes an `ActivityListener` that returns `ActivitySamplingResult.AllData` for a synthetic `ActivitySource.StartActivity(...)` call on an `ActivitySource` whose name is `"OpenTelemetry.Instrumentation.StackExchangeRedis"` (pure registration pin — proves the source is subscribed; does NOT prove the real Redis instrumentation emits spans, which is AC #2's job at Tier-3)
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

- [ ] **Task 1: Package pinning + ADR-8.5-001.** (AC: #1)
    - [ ] 1.1 Add `<PackageVersion Include="OpenTelemetry.Instrumentation.StackExchangeRedis" Version="1.15.1-beta.1" />` to `Directory.Packages.props` (immediately after the `OpenTelemetry.Instrumentation.*` block at lines 21-23). Add a `<PackageReference>` (no version — CPM resolves) to `src/Hexalith.Memories.ServiceDefaults/Hexalith.Memories.ServiceDefaults.csproj` since registration lives there.
    - [ ] 1.2 Write ADR-8.5-001 in `docs/dev/telemetry.md` immediately after ADR-8.4-003. Must cover: (a) candidate comparison (official OT contrib package vs community `StackExchange.Redis.Extensions.OpenTelemetry`) with one-sentence rejection rationale for the community package; (b) explicit acceptance of the `-beta.N` prerelease tag with the upgrade-on-GA policy, AND require NuGet `<packageSourceMapping>` (in `NuGet.config`) that pins all `OpenTelemetry.Instrumentation.*` packages to the official `nuget.org` source with signature verification enabled. Supply-chain mitigation: the prerelease window is when typosquat-adjacent attacks (e.g., a malicious `OpenTelemetry.Instrumentation.StackExchangeRedis.Extensions` uploaded by an attacker) are cheapest, and source-mapping forecloses the package-cache-poisoning route; (c) ActivitySource name pin `"OpenTelemetry.Instrumentation.StackExchangeRedis"` and span OperationName `"OpenTelemetry.Instrumentation.StackExchangeRedis.Execute"`; (d) the keyed-services registration pattern (two `AddRedisInstrumentation(serviceKey, configure)` calls); (e) **flush-semantics policy — `FlushInterval = 100ms` in ALL environments (production + test).** Rationale: the originally-drafted env-gated test-only override (keyed on `InMemoryTelemetryEnvironment.EnvVar`) was removed as unnecessary config surface — production trivially absorbs 10Hz drain-thread wakes per multiplexer, and coupling the Redis flush gate to an existing env var would leak into any future non-Tier-3 in-memory path. A single `ConfigureRedis` delegate is passed to both `AddRedisInstrumentation` calls; both keyed connections MUST resolve to identical `FlushInterval` (asserted at Task 2.4(e)); (f) **missing-key eager-fail discipline — pinned to the DI-guard path.** Upstream `AddRedisInstrumentation(serviceKey)` returns silently when the keyed `IConnectionMultiplexer` is absent (verified against `TracerProviderBuilderExtensions.AddRedisInstrumentation` + `TracerProviderBuilderBase.Build()` — upstream does NOT throw). Therefore `ConfigureOpenTelemetry` MUST wrap each `AddRedisInstrumentation(key, ...)` call with a pre-registration DI guard via `tracing.AddInstrumentation(sp => { if (sp.GetKeyedService<IConnectionMultiplexer>(key) is null) throw new InvalidOperationException($"Keyed IConnectionMultiplexer '{key}' not registered — Story 8.5 Redis OTEL needs both 'redis' and 'falkordb' keys"); ... })`. The "rely on upstream-native throw" path is REJECTED. See Task 2.1 example shape; (g) **upgrade-on-GA trigger (dated, not loose).** Revisit the prerelease pin within 14 days of `OpenTelemetry.Instrumentation.StackExchangeRedis 1.15.0` (non-prerelease) shipping on nuget.org, OR by **2026-09-30** whichever comes first. Record as a dated entry in `_bmad-output/implementation-artifacts/deferred-work.md` with owner + review-by, NOT a loose code comment; (h) **FalkorDB `db.system` semantic-conventions debt.** The instrumentation tags BOTH `redis` and `falkordb` connections with `db.system=redis` / `db.system.name=redis` — upstream cannot distinguish FalkorDB from Redis because FalkorDB speaks the Redis protocol. This misclassifies FalkorDB queries as Redis in APM backends (Honeycomb, Datadog). Pin the chosen remediation path (see Task 2.7 for Path A "ship now" vs Path B "documented debt"); default recommendation Path A. Either way, record the decision and rationale here.
    - [ ] 1.3 Verify `StackExchange.Redis` central version (`Directory.Packages.props:44` currently `2.12.4`) still satisfies the new dependency's `>= 2.6.122` floor — no upgrade required.

- [ ] **Task 2: Register instrumentation in ServiceDefaults.** (AC: #1, #3)
    - [ ] 2.1 Extend the `WithTracing` lambda inside `ConfigureOpenTelemetry<TBuilder>` at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:59-64`. Append TWO `.AddRedisInstrumentation(serviceKey, configure)` calls — one per keyed connection — placed AFTER `.AddHttpClientInstrumentation()` but BEFORE `builder.AddOpenTelemetryExporters()` (`line 66`). Ship `FlushInterval = 100ms` in BOTH production and test — the env-gated override originally drafted here was removed (see ADR-8.5-001 (e) for rationale: unnecessary config surface; 10Hz drain-thread wakes per multiplexer are trivial in production). Each `AddRedisInstrumentation(key, ...)` call MUST be preceded by a DI-keyed-service guard that throws `InvalidOperationException` when the keyed `IConnectionMultiplexer` is absent (ADR-8.5-001 (f) — upstream is silent-null on missing key). Example target shape:
      ```csharp
      // Single FlushInterval applied to both keyed Redis connections.
      // Rationale for 100ms everywhere: ADR-8.5-001 (e). Do not env-gate.
      void ConfigureRedis(StackExchangeRedisInstrumentationOptions options)
          => options.FlushInterval = TimeSpan.FromMilliseconds(100);

      // DI-guard: upstream AddRedisInstrumentation silently no-ops when the
      // keyed multiplexer is absent. Fail eagerly at TracerProvider build
      // with a descriptive message. Pinned in ADR-8.5-001 (f).
      static void AddGuardedRedisInstrumentation(
          TracerProviderBuilder tracing,
          string serviceKey,
          Action<StackExchangeRedisInstrumentationOptions> configure)
      {
          tracing.AddInstrumentation(sp =>
          {
              var mux = sp.GetKeyedService<IConnectionMultiplexer>(serviceKey);
              if (mux is null)
              {
                  throw new InvalidOperationException(
                      $"Keyed IConnectionMultiplexer '{serviceKey}' not registered — "
                      + "Story 8.5 Redis OTEL needs both 'redis' and 'falkordb' keys.");
              }
              // Return the validated multiplexer as the "instrumentation" payload.
              // It is inert to the SDK (no Profile/Begin/End methods on the expected
              // shape) but does NOT leak a `System.Object` into diagnostic reflection
              // surfaces (e.g., test assertions on tracerProvider's instrumentation list).
              // The real instrumentation is attached by the AddRedisInstrumentation call
              // below. See Task 2.4(e) + Self-Consistency analysis in Rev 0.5.
              return mux;
          });
          tracing.AddRedisInstrumentation(serviceKey, configure);
      }

      builder.Services
          .AddOpenTelemetry()
          .WithTracing(tracing =>
          {
              tracing
                  .AddSource(builder.Environment.ApplicationName)
                  .AddSource(MemoriesActivitySource.SourceName)
                  .AddAspNetCoreInstrumentation(opts => opts.Filter = ShouldTraceHttpRequest)
                  .AddHttpClientInstrumentation();
              // Two calls are mandatory (not redundant): each registers its keyed
              // multiplexer with the shared StackExchangeRedisInstrumentation singleton.
              // Skipping either drops that connection's spans entirely.
              AddGuardedRedisInstrumentation(tracing, "redis", ConfigureRedis);
              AddGuardedRedisInstrumentation(tracing, "falkordb", ConfigureRedis);
          });
      ```
      Do NOT add `.AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis")` — the `AddRedisInstrumentation` overload does that internally (verified against `TracerProviderBuilderExtensions.AddRedisInstrumentation` upstream source). Dev-time note: if the sentinel-`AddInstrumentation` trick proves awkward, the equivalent DI-build-time check via `IStartupFilter` or `IValidateOptions<StackExchangeRedisInstrumentationOptions>` is acceptable — pin whichever the dev agent lands in ADR-8.5-001 (f). The "rely on upstream-native throw" path is rejected regardless.
    - [ ] 2.2 Do NOT resolve `IConnectionMultiplexer` directly in `ServiceDefaults.Extensions.cs`. The `AddRedisInstrumentation(object serviceKey)` overload resolves via `sp.GetKeyedService<IConnectionMultiplexer>(serviceKey)` at the `AddInstrumentation(sp => ...)` callback time, which is AFTER the service collection is built — this is the correct pattern for keyed DI. The two keyed registrations at `src/Hexalith.Memories.Server/Program.cs:97-100` satisfy the lookup.
    - [ ] 2.3 Extend `IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb` at `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:175-178`. Current exact-match set: `MemoriesActivitySource.SourceName` OR (`ActivityKind.Server` AND `"Microsoft.AspNetCore"`). Add the Redis source `"OpenTelemetry.Instrumentation.StackExchangeRedis"` (introduce a `RedisSourceName` private const next to `AspNetCoreSourceName` at line 135 for readability) with a parent-chain reachability guard: an emitted Redis breadcrumb MUST have an ancestor whose `Source.Name` is `MemoriesActivitySource.SourceName` OR `"Microsoft.AspNetCore"` (walk `Activity.Parent` up to max-depth 16 — mirror the `AssertParentChainReachesCliRoot` convention from Story 8.4). Housekeeping Redis activity under connection-pool maintenance has no such ancestor and is silently dropped from stderr breadcrumb output. **Triage visibility — emit a DEBUG-level `ILogger` entry on drop.** Format: `"redis breadcrumb dropped: {reason} operation={Activity.OperationName} source={Activity.Source.Name} depth={walkedDepth}"` with `reason ∈ { "orphan_no_parent", "parent_chain_not_reachable", "depth_exceeded_16" }`. DEBUG level means no production log-volume impact, but operators chasing "why didn't my Redis span show up?" have a triage signal without code spelunking. Inject `ILogger<IntegrationActivityProcessor>` via the processor constructor if not already present — no behavior change on INFO/WARN/ERROR. Spec forbids emitting Redis breadcrumbs for orphan activities TO STDERR; DEBUG-log visibility is the only escape hatch.
    - [ ] 2.4 Add `TracerRegistration_IncludesRedisInstrumentationSource` + `TracerRegistration_ResolvesBothKeyedRedisConnections` + `TracerRegistration_MissingKeyedMultiplexer_FailsEagerly` to `tests/Hexalith.Memories.Server.Tests/Telemetry/OpenTelemetryRegistrationTests.cs` (Tier-2, `[Trait("Category", "Unit")]`, no Docker). Stub the keyed `IConnectionMultiplexer` registrations with `ConnectionMultiplexer.Connect("localhost:0,abortConnect=false")` replaced by a mock `IConnectionMultiplexer` (NSubstitute is already in use in the test project — check for it; if absent use `Mock<IConnectionMultiplexer>` from `Moq` or a hand-rolled `NoOpConnectionMultiplexer` stub). Assert:
      - **(a) Source subscribed** — a synthetic `using var src = new ActivitySource("OpenTelemetry.Instrumentation.StackExchangeRedis"); using var activity = src.StartActivity(...)` under the built `TracerProvider`'s `ActivityListener` surface produces `activity is not null` AND `activity.IsAllDataRequested == true`. Proves the source is sampled; does NOT require the real Redis instrumentation to emit.
      - **(b) Both keys resolve without throw** — build the `TracerProvider` with both `redis` + `falkordb` keyed `IConnectionMultiplexer` stubs registered. `tracerProvider.ForceFlush()` completes without `InvalidOperationException`.
      - **(c) Missing key fails eagerly via DI-guard (upstream is silent-null).** Upstream `AddRedisInstrumentation(serviceKey)` returns silently when the keyed `IConnectionMultiplexer` is absent — verified against `TracerProviderBuilderExtensions.AddRedisInstrumentation` + `TracerProviderBuilderBase.Build()`. Upstream does NOT throw. The DI-guard path pinned in ADR-8.5-001 (f) and shown in Task 2.1's `AddGuardedRedisInstrumentation` helper is the only correct eager-fail strategy. Test: register ONLY the `redis` key (omit `falkordb`), then build the `TracerProvider` and assert it throws `InvalidOperationException` whose `Message` contains both `"falkordb"` AND `"IConnectionMultiplexer"`. Tests MUST NOT rely on upstream-native throw behavior — that path is rejected.
      - **(d) Test tier discipline** — `[Trait("Category", "Unit")]` + `[Collection(TelemetryTestCollection.Name)]` per suite convention; no `DistributedApplicationTestingBuilder`; no container.
      - **(e) We don't accidentally diverge `FlushInterval` delegates per key (internal invariant).** Upstream `StackExchangeRedisConnectionInstrumentation` stores `FlushInterval` on a shared singleton — per-connection intervals are NOT an upstream-exposed shape (the LAST `AddRedisInstrumentation`'s options callback overwrites previous ones). This test does NOT assert a property of upstream; it asserts a property of OUR wiring. Register both `redis` and `falkordb` with the shipped `ConfigureRedis` delegate from Task 2.1, invoke the captured `Action<StackExchangeRedisInstrumentationOptions>` against two fresh `StackExchangeRedisInstrumentationOptions` instances (one per key), and assert both resolve to `FlushInterval == TimeSpan.FromMilliseconds(100)`. Guards against a future refactor that wires distinct `ConfigureRedis`-equivalent delegates per key, which would silently diverge behavior. Pins ADR-8.5-001 (e).
      - **(f) DI-guard throws from the `AddServiceDefaults` public entry.** The tests in (c) build the `TracerProvider` directly via the internal `AddGuardedRedisInstrumentation` helper. This additional test exercises the canonical public entry point: given an `IHostApplicationBuilder` with NEITHER `redis` nor `falkordb` keyed `IConnectionMultiplexer` registered, calling `builder.AddServiceDefaults()` and then `builder.Build()` MUST surface an `InvalidOperationException` whose `Message` contains both `"Keyed IConnectionMultiplexer"` AND the missing key name. Rationale: pins that a future refactor splitting the DI-guard into a separate extension (e.g., `AddRedisTelemetry()`) still fires from the canonical entry. Also guards the Red-Team attack where a reordered registration leaves the guard unreachable.
    - [ ] 2.5 Ship the default attribution shape: `≥1 Redis span` (conservative). Per-connection attribution (`≥1 per keyed connection`) is NOT enforced in AC — spec deliberately ships the cheaper assertion. Document the conservative pick in ADR-8.5-001 with a one-line rationale pointer to operator-feedback escalation if demand emerges.
    - [ ] 2.6 Extend `IntegrationActivityProcessor`'s existing unit coverage (`tests/Hexalith.Memories.Server.Tests/Telemetry/`): either modify an existing `ShouldEmitActivityBreadcrumbTests` class or add one if none exists. Pin the new orphan-guard behavior: (a) Redis activity under a `memories.search` parent → emitted; (b) Redis activity with no `Activity.Parent` chain reaching a Memories/AspNetCore ancestor → NOT emitted TO STDERR, but DEBUG log entry with `reason="orphan_no_parent"` or `"parent_chain_not_reachable"` captured via `ILogger<IntegrationActivityProcessor>` test double; (c) depth-16 traversal boundary honored (reason `"depth_exceeded_16"` on the DEBUG log).

    - [ ] 2.7 **FalkorDB semantic-conventions debt** (ADR-8.5-001 (h)). FalkorDB spans are tagged `db.system=redis` / `db.system.name=redis` because the upstream instrumentation cannot distinguish FalkorDB from Redis at the protocol level. Pick ONE remediation path:
      - **Path A — Ship now (default recommendation):** add `FalkorDbSemanticAttributeProcessor : BaseProcessor<Activity>` in `src/Hexalith.Memories.ServiceDefaults/Telemetry/`. On `OnEnd`, inspect `activity.GetTagItem("server.address")` (or fall back to `net.peer.name`) — if it matches the FalkorDB resource hostname (configurable; default `"falkordb"` which Aspire assigns), rewrite `db.system` and `db.system.name` to `"falkordb"` via `activity.SetTag(...)`. **Register INSIDE the `WithTracing` lambda** via `tracing.AddProcessor<FalkorDbSemanticAttributeProcessor>()`, placed AFTER both `AddGuardedRedisInstrumentation` calls but BEFORE `builder.AddOpenTelemetryExporters()`. Do NOT use the separate `builder.Services.ConfigureOpenTelemetryTracerProvider(...)` extension — processor-order vs exporter-order must be deterministic, and co-locating the processor registration inside the same `WithTracing` lambda guarantees it (no risk of a 3rd-party processor finalizing the activity before the FalkorDB rewrite fires — Red-Team Attack #3). Unit-pin: (a) **rewrite-hit** — synthetic `Activity` with `server.address=falkordb` tag, assert `db.system` rewritten; (b) **rewrite-miss** — synthetic `Activity` with `server.address=redis`, assert `db.system=redis` unchanged; (c) **processor-order** — build a `TracerProvider` via `Sdk.CreateTracerProviderBuilder()` + the real `ConfigureOpenTelemetry` entry, then reflect on the processor chain and assert `FalkorDbSemanticAttributeProcessor` appears BEFORE the exporter processor (use the same reflection shape as existing `TracerProviderSdk` introspection in `tests/Hexalith.Memories.Server.Tests/Telemetry/`; if absent, add a small helper `TracerProviderAssertions.GetProcessorChain(TracerProvider)` that walks the `ReflectionReadonly` processor list). Three tests total for Path A.
      - **Path B — Documented debt:** accept the `db.system=redis` ambiguity, record a dated follow-up in `_bmad-output/implementation-artifacts/deferred-work.md` with owner + review-by date (suggested **2026-09-30** to align with ADR-8.5-001 (g)'s GA-upgrade window). No runtime change.
      - **Cost comparison:** Path A ≈ 60 lines + 2 unit tests. Path B ≈ 1 deferred-work entry.
      - **Pin the final choice in ADR-8.5-001 (h)** with rationale. If Path B, the deferred-work entry is a Task 2.7 deliverable.

- [ ] **Task 3: Flip Story 8.4 AC #2 to hard assertion.** (AC: #2)
    - [ ] 3.1 Update `AspireEndToEndTraceTests.CliSearch_EndToEnd_SingleTraceIdAcrossAllHops` — add a Redis-source activity count-first gate (`Count >= 1`), a `TraceId`-match assertion, and a parent-chain reachability assertion. Reuse `AssertParentChainReachesCliRoot` with the max-depth + cycle-detection already in place.

    - [ ] 3.1.1 **Replace the `ForceFlushAsync(2s) + settle-delay` smell with a bounded polling loop (Redis-span presence assertion only).** The Redis instrumentation's `DrainThread` ticks on `FlushInterval` (100ms per ADR-8.5-001 (e)), but `TracerProvider.ForceFlushAsync` does NOT accelerate that internal queue — it only flushes the tracer-level batch processor. A hard `Task.Delay` after `ForceFlush` is a documented flake source. Introduce `TelemetryAsserts.WaitForActivityAsync(IReadOnlyCollection<Activity> collector, Func<Activity, bool> predicate, TimeSpan timeout = default, TimeSpan pollInterval = default)` in `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/TelemetryAsserts.cs` (create the file if absent). Defaults: `timeout = TimeSpan.FromSeconds(5)`, `pollInterval = TimeSpan.FromMilliseconds(50)`. Each iteration calls `TracerProvider.ForceFlushAsync(TimeSpan.FromMilliseconds(500))` then re-scans `collector` for the predicate. Returns `true` on first hit, `false` on timeout; caller asserts with `Assert.True(result, "Redis span did not appear within 5s — check FlushInterval + DI-guard wiring")`. Document the flake-minimization property in the XML-doc. The existing `ForceFlushAsync(2s)` call in `CliSearch_EndToEnd_SingleTraceIdAcrossAllHops` stays for non-Redis assertions (AspNetCore + `memories.*` sources) — the polling helper is ADDITIVE and used ONLY for the Redis-span presence predicate. Do NOT rip out the existing flush call.
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
        - **Add a new "Verify Redis spans locally" sub-section** with concrete runnable commands so a new developer can sanity-check Redis instrumentation without spinning up the Tier-3 suite. Minimum content: (i) `dotnet run --project src/Hexalith.Memories.AppHost` to boot the Aspire stack; (ii) trigger a representative Redis-path request via `curl` or `dotnet run --project src/Hexalith.Memories.Cli -- search "canary"`; (iii) open the Aspire dashboard Traces view at the dashboard URL printed on boot; (iv) expected span shape shown as a text-diffable YAML fenced block — `Source.Name: OpenTelemetry.Instrumentation.StackExchangeRedis`, `OperationName: OpenTelemetry.Instrumentation.StackExchangeRedis.Execute`, `db.system: redis` (or `falkordb` post-Task-2.7 Path A), matching `TraceId` to the HTTP-in span, parent chain reaching the CLI root. (v) Troubleshooting bullet: "No Redis spans appear → check (a) `AddServiceDefaults` is called; (b) both keyed `IConnectionMultiplexer` are registered; (c) the DI-guard didn't throw at startup (check stdout for 'Keyed IConnectionMultiplexer')." Rationale: silent-failure of Redis instrumentation is undetectable locally today; this section closes the feedback loop for new devs.
    - [ ] 4.2 Update `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/AuditEventStreamReader.cs` XML-doc comments if they mention the skip path — keep behavior unchanged.
    - [ ] 4.3 **Instrumentation inventory (root-cause fix for the 'aspirational spec, no auditor' pattern that deferred 8.4 AC #2).** Introduce a canonical **Instrumentation Inventory** table in `docs/dev/telemetry.md` with columns: (a) `ActivitySource.Name`, (b) Registration site (`file:line`), (c) What it covers, (d) Tier-2 registration test class + method. Populate for every OTEL source the Server subscribes: `Microsoft.AspNetCore`, `System.Net.Http`, `MemoriesActivitySource.SourceName` (`memories.*`), `OpenTelemetry.Instrumentation.StackExchangeRedis` (new from 8.5), plus the default `AddSource(builder.Environment.ApplicationName)`. Then add `InstrumentationInventoryTests` in `tests/Hexalith.Memories.Server.Tests/Telemetry/` that: (i) parses the markdown table from `docs/dev/telemetry.md`; (ii) for each listed `ActivitySource.Name`, uses the same listener-sampling shape as AC #3's Tier-2 check to assert the source is subscribed by the built `TracerProvider`. Parsing tolerates minor whitespace changes as long as column names stay stable (regex-match on `| *{SourceName} *|` within the inventory-table block). Rationale: Story 8.4 shipped with Redis spans MISSING from end-to-end traces because no code-vs-doc parity check existed. This test closes the pattern — the NEXT instrumentation gap fails a unit test, not a Tier-3 assertion six months after ship. Tier-2, `[Trait("Category", "Unit")]`, no Docker.

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

- **Package (pinned, verified against upstream 2026-04-23):** `OpenTelemetry.Instrumentation.StackExchangeRedis 1.15.1-beta.1` (released 2026-04-21 per nuget.org). Targets `net8.0` + `net10.0`. Dependency: `StackExchange.Redis >= 2.6.122` — compatible with Hexalith's pinned `StackExchange.Redis 2.12.4`. Prerelease tag MUST be accepted explicitly in ADR-8.5-001.
- **Rejected alternative package:** `StackExchange.Redis.Extensions.OpenTelemetry` — community-maintained add-on for the `StackExchange.Redis.Extensions` wrapper library, which Hexalith does NOT use. Rejecting it avoids an unused dependency surface.
- **ActivitySource name (verified in upstream `StackExchangeRedisConnectionInstrumentation.cs`):** `"OpenTelemetry.Instrumentation.StackExchangeRedis"` — derived from `Assembly.GetName().Name`. Individual span `OperationName` is `"OpenTelemetry.Instrumentation.StackExchangeRedis.Execute"`.
- **Registration API (verified in upstream `TracerProviderBuilderExtensions.cs`):** the `AddRedisInstrumentation(this TracerProviderBuilder, object serviceKey)` overload is the correct entry point for keyed DI connections. It internally calls `AddSource(StackExchangeRedisConnectionInstrumentation.ActivitySourceName)` and `AddInstrumentation(sp => ...)` which resolves `sp.GetKeyedService<IConnectionMultiplexer>(serviceKey)` lazily. Registering the same overload twice with different `serviceKey` values adds both connections to a single shared `StackExchangeRedisInstrumentation` singleton.
- **Semantic attributes emitted by the instrumentation:** `db.system=redis` (legacy `SemanticConventions.AttributeDbSystem`) and `db.system.name=redis` (new `SemanticConventions.AttributeDbSystemName`) + `db.redis.database_index` for the Redis DB index. Use these tags to disambiguate `redis` vs `falkordb` backends if per-connection assertion becomes necessary — FalkorDB also self-identifies as `db.system=redis` because it speaks the Redis protocol; `server.address` / `net.peer.name` is the canonical discriminator (`redis` resource vs `falkordb` resource Aspire hostnames).
- **Keyed DI site (unchanged since Story 1.5):** `src/Hexalith.Memories.Server/Program.cs:97-100` registers the two `IConnectionMultiplexer` entries via `AddKeyedSingleton<IConnectionMultiplexer>("redis", ...)` and `AddKeyedSingleton<IConnectionMultiplexer>("falkordb", ...)`. The `ServiceDefaults` registration at Task 2.1 resolves these at instrumentation-attachment time — it does NOT re-register them.
- **Flush semantics (from upstream `StackExchangeRedisConnectionInstrumentation.DrainThread`):** the instrumentation uses a background drain thread with a configurable `FlushInterval` (package default 10s). **ADR-8.5-001 (e) pins `FlushInterval = 100ms` in ALL environments** — the originally-drafted env-gated test-only override was dropped as unnecessary config surface (production trivially absorbs 10Hz drain-thread wakes per multiplexer). `TracerProvider.ForceFlushAsync` does NOT accelerate the instrumentation's internal drain — it only flushes the tracer-level batch processor. For Redis-span presence assertions, Tier-3 tests use the bounded polling helper `TelemetryAsserts.WaitForActivityAsync(5s, 50ms)` introduced in Task 3.1.1 instead of the `ForceFlush + Task.Delay` smell. Non-Redis span assertions continue to use the existing `ForceFlushAsync(2s)` unchanged.
- **Aspire 13.1.3**: `DistributedApplicationTestingBuilder` + project-as-process model unchanged from Story 8.4. No new fixture work required.
- **W3C TraceContext propagation**: the `traceparent` header transits from CLI → Server via HttpClient instrumentation (already in place). Redis spans inherit the server's `Activity.Current` so they automatically share the `TraceId` — no extra propagation plumbing needed.

### References

- GitHub issue: [#9 — Story 8.4 follow-up: Register Redis OTEL instrumentation + harden AC #2 Redis span assertion](https://github.com/Hexalith/Hexalith.Memories/issues/9)
- Story 8.4 (parent): `_bmad-output/implementation-artifacts/8-4-end-to-end-telemetry-integration-tests.md`
- Story 8.4 AC #2 text: `_bmad-output/implementation-artifacts/8-4-end-to-end-telemetry-integration-tests.md:77-83`
- Story 8.4 ADR-8.4-002 (capture model): `docs/dev/telemetry.md`
- Self-expiring skip helper (to retire): `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/Ac2RedisSkipReviewBy.cs`
- Unit tests for skip helper (to retire): `tests/Hexalith.Memories.IntegrationTests/Telemetry/Ac2SkipReviewByTests.cs`
- ServiceDefaults telemetry wiring: `src/Hexalith.Memories.ServiceDefaults/Extensions.cs:42-69` (ConfigureOpenTelemetry) and `86-124` (AddOpenTelemetryExporters + IntegrationActivityProcessor filter at `175-178`)
- Story 8.4 Tier-3 test classes (to extend): `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs`
- Tier-2 registration test suite (to extend): `tests/Hexalith.Memories.Server.Tests/Telemetry/OpenTelemetryRegistrationTests.cs`
- Keyed `IConnectionMultiplexer` registration site: `src/Hexalith.Memories.Server/Program.cs:97-100`
- Central package manifest: `Directory.Packages.props:21-23` (OpenTelemetry.Instrumentation block) + `:25` (Exporter.InMemory) + `:44` (StackExchange.Redis 2.12.4)
- Upstream instrumentation package (verified): `https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.StackExchangeRedis`
- Upstream `ActivitySourceName` constant: `StackExchangeRedisConnectionInstrumentation.cs` → `internal static readonly string ActivitySourceName = Assembly.GetName().Name!;`
- Upstream keyed-services overload: `TracerProviderBuilderExtensions.AddRedisInstrumentation(this TracerProviderBuilder, object serviceKey)`

### Project Structure Notes

- **Module boundary preserved:** Redis OTEL registration ships inside `Hexalith.Memories.ServiceDefaults`. No change to `Hexalith.Memories.Server.Program.cs` keyed-connection registrations; no change to `Hexalith.Memories.Redis` composition. External consumers (`Hexalith.Memories.Cli`, `Hexalith.Memories.Client.*`, `Hexalith.Memories.Mcp` when it ships) inherit the behavior by calling `AddServiceDefaults(...)` as they already do — no consumer-side API change.
- **Central package management:** Hexalith uses CPM (`Directory.Packages.props` — verify `ManagePackageVersionsCentrally=true` in `Directory.Build.props`). New `<PackageReference>` on the ServiceDefaults project MUST omit the `Version` attribute; version lives only in `Directory.Packages.props`.
- **Prerelease-pin policy:** Hexalith's submodules (e.g., `src/submodules/Hexalith.EventStore/Directory.Packages.props`) already track `OpenTelemetry.Instrumentation.* 1.15.0` at the main (non-submodule) root; the 8.5 addition at `1.15.1-beta.1` is one patch tick ahead. Coordinate with any future `OpenTelemetry.Instrumentation.*` upgrade in the main repo to keep the Aspire + Http + AspNetCore + Redis block at a consistent minor.
- **Tier-2 test project:** `tests/Hexalith.Memories.Server.Tests/` is the correct home for Task 2.4/2.6 registration tests (already hosts `OpenTelemetryRegistrationTests`, `TelemetrySummaryEndpointTests`, etc.). `[Collection(TelemetryTestCollection.Name)]` remains the pattern for env-var-serializing suites.
- **Tier-3 test project:** `tests/Hexalith.Memories.IntegrationTests/Telemetry/` hosts `AspireEndToEndTraceTests` and is the correct home for the AC #2 hard-assertion code change. The existing `[Collection("AspireIngestionPipeline")]` + `[Trait("Category", "Integration")]` combination stays unchanged.

### Previous story intelligence (Story 8.4 — immediate predecessor)

- **Cross-process Aspire reality:** `DistributedApplicationTestingBuilder` runs the Memories Server as a separate process. In-test-process DI sharing of `IActivityCollector` is infeasible. The hybrid capture model proven in 8.4 is: (i) CLI-side spans via `InMemorySpanCollector` in `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/`; (ii) Server-side spans surfaced via stderr activity breadcrumbs emitted by `IntegrationActivityProcessor` (`src/Hexalith.Memories.ServiceDefaults/Extensions.cs:133-185`) and parsed by `ServerActivityStreamReader`. Redis spans MUST flow through path (ii) — which is why the Task 2.3 filter widening is load-bearing.
- **Count-first assertion convention (codified in 8.4 Rev 0.5):** any captured collection asserts `Count >= expected` BEFORE any `.All(...)` / `.Single(...)` / `.First(...)` predicate. A vacuous empty collection must never pass silently. The new Redis-span assertion in Task 3.1 MUST follow this convention.
- **Parent-chain traversal:** `AspireEndToEndTraceTests.AssertParentChainReachesCliRoot` enforces cycle detection (visited-set), max-depth = 16, and root-termination at `memories.cli.invoke`. Reuse verbatim for the Redis-span assertion — do not re-implement.
- **Force-flush rule:** tests reading the Tier-3 captures must call `TracerProvider.ForceFlushAsync(TimeSpan.FromSeconds(2))` before reading non-Redis spans. Redis spans add a NEW wrinkle: the Redis instrumentation's own background `DrainThread` ticks on `FlushInterval` (100ms post-8.5 per ADR-8.5-001 (e)) — the tracer-level ForceFlush does not accelerate that internal queue. Shipped approach (Task 3.1.1): the bounded polling helper `TelemetryAsserts.WaitForActivityAsync(5s timeout, 50ms poll)` replaces the `ForceFlush + settle-delay` smell for Redis-span presence assertions. Non-Redis span assertions continue to use the existing `ForceFlushAsync(2s)` pattern unchanged.
- **EnvVarScope discipline:** `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/EnvVarScope.cs` (Story 8.4 Rev 0.7/0.8) serializes env-var mutations across parallel test collections with an `AcquireTimeout=2min` gate + same-thread re-entry detection. If 8.5 needs any new env var (it should NOT — the Redis instrumentation is always-on when registered), reuse `EnvVarScope.Set(name, value)` rather than bare `Environment.SetEnvironmentVariable`.
- **Aspire dashboard OTLP noise:** Story 8.4 Dev Notes observed `127.0.0.1:4317 actively refused` warnings in Server stdout from the Aspire dashboard's OTLP exporter. Benign. Adding Redis spans increases telemetry volume, but the in-memory capture + audit-log reader pipelines used by Tier-3 tests do NOT depend on the dashboard OTLP path. No mitigation needed.

### Git intelligence

- **Tracking commit for this story:** `d7495a3` (2026-04-23) added the Story 8.5 file to tracking, rewired `Ac2RedisSkipReviewBy.TrackingReference` to GitHub issue #9, and updated sprint-status. This commit does NOT touch `ServiceDefaults/Extensions.cs` nor any test file — the implementation work begins with Task 1.
- **Recent related commits (top-5 on main):**
    - `727d3ce` Refactor `AuditLogStreamIntegrationTests` and related infrastructure — confirms the Story 8.4 Tier-3 test shape is still in flux; coordinate with any in-flight refactors on the `AspireEndToEndTraceTests` file before Task 3.1.
    - `1cb0e46` Refactor code structure for improved readability and maintainability — low-risk; no telemetry-pipeline impact.
    - `7532477` refactor(tests): replace tenant ID generation with active tenant provisioning in integration tests — confirms `ProvisionActiveTenantAsync` is the canonical pattern; reuse for any new test tenancy setup.
    - `54b8bec` Add integration tests for event ingestion pipeline and outcome handling — unrelated to telemetry.
- **Uncommitted work:** repo is clean at start of Story 8.5 (per `git status` snapshot 2026-04-23). Land Task 1 changes on a dedicated `feature/8-5-redis-otel-instrumentation` branch.

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
| 2026-04-23 | 0.2     | Promoted to ready-for-dev. Pinned `OpenTelemetry.Instrumentation.StackExchangeRedis 1.15.1-beta.1` (verified against upstream `open-telemetry/opentelemetry-dotnet-contrib` 2026-04-23) and the `AddRedisInstrumentation(object serviceKey)` keyed-DI overload. Canonical `ActivitySourceName = "OpenTelemetry.Instrumentation.StackExchangeRedis"` (derived from assembly name at `StackExchangeRedisConnectionInstrumentation.cs`) recorded. Rejected the community `StackExchange.Redis.Extensions.OpenTelemetry` candidate upfront. Task 1 reframed from a spike to a deterministic pin. Task 2 sharpened with the exact target shape inside `ConfigureOpenTelemetry`'s `WithTracing` lambda (lines 59-64 of `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`), plus a parent-chain-reachability guard for the breadcrumb filter at `IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb` (lines 175-178). Added Task 2.6 (orphan-guard unit coverage). Added Project Structure Notes (CPM discipline, module boundary preservation, submodule version coordination), Previous Story Intelligence (8.4 hybrid capture model, count-first convention, parent-chain traversal reuse, ForceFlush + Redis DrainThread interaction, EnvVarScope discipline), and Git Intelligence (recent commits relevant to the Tier-3 test surface). Added the flush-semantics caveat — the Redis instrumentation's background `DrainThread` runs on a 10s `FlushInterval` default; tests either accept a settle delay or override to `100ms` via the options callback. References expanded with line-accurate paths. |
| 2026-04-23 | 0.3     | Party-mode review tightening pass. (1) **AC #3 sharpened** — replaced ambiguous "sees activities from the Redis OTEL source" wording with a listener-sampling assertion shape: the `TracerProvider`'s `ActivityListener` returns `ActivitySamplingResult.AllData` for a synthetic `ActivitySource.StartActivity(...)` on the Redis source. Disambiguates Tier-2 (registration pin, this AC) from Tier-3 (real emission, AC #2). (2) **Task 2.1 code block expanded** — added the `FlushInterval` override wired to the real `InMemoryTelemetryEnvironment.EnvVar` (`HEXALITH_MEMORIES_TELEMETRY_INMEMORY == "1"`) test gate; production path retains the 10s package default, test path overrides to 100ms. Verified env var name against `src/Hexalith.Memories.Telemetry/InMemoryTelemetryEnvironment.cs`. (3) **Task 2.4 assertion (c) rewritten** — eager-fail semantics made load-bearing: either upstream `AddRedisInstrumentation` throws at `TracerProvider.Build()` when a keyed `IConnectionMultiplexer` is missing, or `ConfigureOpenTelemetry` inserts a pre-registration guard. New test `TracerRegistration_MissingKeyedMultiplexer_FailsEagerly` pins whichever path is chosen. (4) **ADR-8.5-001 Task 1.2 extended** with bullets (e) flush-semantics policy and (f) missing-key eager-fail discipline. Pre-dev readiness confirmed: Task 1 unblocked; Task 2.1 and Task 2.4 now self-contained for dev execution. |
| 2026-04-23 | 0.4     | Advanced-elicitation pass (all five methods applied: Challenge-from-Critical-Perspective, Pre-mortem, Expert-Panel-Review, ADR-stress-test, Failure-Mode-Analysis). Eight actionable spec corrections landed: (1) **Task 2.4(e) added** — assert identical `FlushInterval` across both keyed registrations, pinning that the shared `StackExchangeRedisInstrumentation` singleton can't diverge on flush cadence. (2) **Task 2.4(c) rewritten** — upstream `AddRedisInstrumentation(serviceKey)` is silent-null on missing key (re-verified against `TracerProviderBuilderExtensions.AddRedisInstrumentation` + `TracerProviderBuilderBase.Build()`). The DI-guard path is the only correct eager-fail strategy; the "rely on upstream-native throw" option is REJECTED. (3) **`FlushInterval = 100ms` env gate dropped** — ship 100ms in both production and test. Rationale: config-surface reduction, no measured cost (10Hz drain wakes per multiplexer trivial in prod). Propagates to Task 2.1 code block (env-check removed, `AddGuardedRedisInstrumentation` helper introduced inline) + ADR-8.5-001 (e) rewritten + Latest-technical-specifics + Previous-story-intelligence force-flush-rule. (4) **Task 2.7 added** — FalkorDB `db.system=redis` tagging misclassifies graph queries as Redis in APM backends; spec now requires picking Path A (ship `FalkorDbSemanticAttributeProcessor` that rewrites `db.system=falkordb` based on `server.address`) or Path B (documented deferred debt in `deferred-work.md` with 2026-09-30 review-by); default recommendation Path A. ADR-8.5-001 (h) records the chosen path. (5) **Task 3.1.1 added** — replace the `ForceFlushAsync + settle-delay` smell with `TelemetryAsserts.WaitForActivityAsync(5s, 50ms)` bounded polling helper for Redis-span presence only; non-Redis assertions keep the existing 2s ForceFlush. (6) **Env-gate invariant obsoleted** by (3) — no separate spec clause needed since the gate no longer exists. (7) **`IntegrationActivityProcessor` breadcrumb-drop DEBUG log** added to Task 2.3 — `reason ∈ { "orphan_no_parent", "parent_chain_not_reachable", "depth_exceeded_16" }` so operators can triage "why didn't my Redis span show up?" without code spelunking; unit-pinned in Task 2.6 via `ILogger<IntegrationActivityProcessor>` test double. (8) **ADR-8.5-001 (g) sharpened** — GA-upgrade trigger is now "within 14 days of `1.15.0` stable shipping OR by 2026-09-30, whichever first" with a `deferred-work.md` entry requirement (not a loose code comment). Pre-dev readiness confirmed: all eight corrections self-contained for dev execution; no re-elicitation blockers. |
| 2026-04-23 | 0.5     | Second advanced-elicitation pass (all five reshuffled methods applied: 5-Whys-Deep-Dive, Red-Team-vs-Blue-Team, Self-Consistency-Validation, Rubber-Duck-Debugging-Evolved, Feynman-Technique). Seven spec corrections + two wording tweaks landed: (1) **Task 4.3 added — Instrumentation Inventory** table in `docs/dev/telemetry.md` + `InstrumentationInventoryTests` at Tier-2 asserting code ↔ doc parity for all registered OTEL sources. Closes the root-cause pattern surfaced by 5-Whys: Story 8.4 AC #2 deferred because the telemetry spec was aspirational, not auditable; the next instrumentation gap now fails a unit test instead of a Tier-3 assertion six months after ship. (2) **ADR-8.5-001 (b) extended** — require NuGet `packageSourceMapping` pinning all `OpenTelemetry.Instrumentation.*` packages to the official nuget.org source with signature verification. Red-Team Attack #1 mitigation: typosquat-adjacent supply-chain attacks are cheapest during the `1.15.1-beta.1` prerelease window; source-mapping forecloses the package-cache-poisoning route. (3) **Task 2.4(f) added** — integration test that exercises the `AddServiceDefaults` public entry (not the internal `AddGuardedRedisInstrumentation` helper) with a broken keyed multiplexer registration; asserts the DI-guard `InvalidOperationException` still surfaces from the canonical entry point. Red-Team Attack #2 mitigation: pins that a future refactor splitting the guard into a separate extension cannot leave it unreachable. (4) **Task 2.7 Path A sharpened** — register `FalkorDbSemanticAttributeProcessor` INSIDE the `WithTracing` lambda via `tracing.AddProcessor<>(...)` (not via separate `builder.Services.ConfigureOpenTelemetryTracerProvider(...)`), guaranteeing deterministic processor-vs-exporter order; added a processor-order unit test (third test in Path A). Red-Team Attack #3 mitigation: forecloses the race where a 3rd-party `AddProcessor` finalizes the activity before the FalkorDB rewrite fires. (5) **Task 2.1 code block: sentinel return changed** from `return new object();` to `return mux;` (return the validated multiplexer) with explanatory comment. Self-Consistency-Validation of 3 independent approaches surfaced a `System.Object` diagnostic-leak risk with `new object()` in reflection surfaces; returning `mux` is equally inert to the SDK but leaks nothing into diagnostic APIs. (6) **Task 2.4(e) wording reframed** — recast as "we don't accidentally diverge `FlushInterval` delegates per key (internal invariant)" instead of "identical `FlushInterval` across both keyed registrations" (which misrepresented upstream's shape). Rubber-Duck-Level-3 surfaced that upstream stores `FlushInterval` on a shared singleton — per-connection intervals are not an upstream-exposed shape, so the test guards against OUR future drift, not upstream's. (7) **Plain-English summary amended** — added a customer-outcome sentence: MTTR-reduction on search-latency incidents via per-backend span attribution in Grafana / Datadog / Aspire dashboard. Rubber-Duck-Level-1 surfaced that the spec had no customer-visible claim. (8) **Task 4.1 extended** — new "Verify Redis spans locally" sub-section in `docs/dev/telemetry.md` with runnable commands, expected-span YAML-shape, and a troubleshooting bullet. Feynman-Technique surfaced that silent-failure of Redis instrumentation is locally undetectable today. (9) **Task 2.1 inline comment added** — 2-line explanation above the two `AddGuardedRedisInstrumentation` calls clarifying why both registrations are mandatory (keyed-DI plumbing into the shared singleton, not redundancy). Feynman-Technique junior-engineer gap. All changes are self-contained; Rev 0.5 ships as ready-for-dev. |
