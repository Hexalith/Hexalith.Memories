---
title: '24.4 Metric Naming & Committed Dashboards'
type: 'feature'
created: '2026-07-05T19:13:02+02:00'
status: 'done'
baseline_revision: '05e27365f33e830bac583ec111c2a5cfc234ed30'
final_revision: 'd71e21f5024e05565a4f0debc89e60c0de100e0d'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** Memories custom metrics use a split naming scheme: dot-separated names, mixed dot/snake segments, and Prometheus-style snake_case instruments coexist, while the repo has no committed dashboard for the emitted metric set. Operators therefore cannot rely on `MetricTagKeyPolicy` as the single source for dashboard and alert wiring.

**Approach:** Normalize all `MemoriesMeter` instrument names into one dot-separated `memories.*` family while keeping tag keys snake_case, then commit a Grafana dashboard JSON that references only canonical metric names and policy-approved tag keys.

## Boundaries & Constraints

**Always:** Keep `Hexalith.Memories` as the meter name; keep metric tag keys low-cardinality and snake_case; preserve `tenant_id="__rejected__"`; update every repo-owned doc/query that names the changed instruments; pin dashboard references with tests.

**Block If:** A required metric rename cannot be represented by OpenTelemetry `System.Diagnostics.Metrics` or by the dashboard query format without losing the Story 24.4 one-family naming requirement.

**Never:** Do not add high-cardinality tags such as `case_id`, `user`, or `memory_unit_id`; do not change metric emission timing, counters/gauge callbacks, trace sources, or Story 24.3 isolation behavior; do not implement external collector provisioning or live Grafana deployment.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Metric manifest normalized | `MemoriesMeter` contains legacy snake_case or mixed names | All instrument constants use dot-separated `memories.*` names and `MetricTagKeyPolicy` keys match those names | Unit tests fail on drift |
| Dashboard references metrics | Dashboard JSON is committed | Dashboard panels reference canonical metric names and approved tag keys only | Dashboard contract test names invalid metrics/tags |
| Reserved cache metric | Conversation cache hit counter remains reserved, not emitted today | Name is normalized and documented as reserved until SDK exposes hit/miss metadata | No runtime emission is added |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs` -- canonical metric names, instruments, `MetricTagKeyPolicy`, and tag cardinality comments.
- `src/Hexalith.Memories.Server/Telemetry/TelemetryMetricsRecorder.cs` -- endpoint metric recorder using constants; should not need behavior changes except any compile fallout.
- `src/Hexalith.Memories.Server/Program.cs` -- observable-gauge registration for index, queue, NL retry, and handler metrics; compile guard for renamed constants.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` -- embedding API counter emission.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs` -- chunk embedding API counter emission.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs` -- NL duration recorder call.
- `src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs` -- handler mismatch counter emission.
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionTelemetryAdapter.cs` -- dropped-observation counter emission.
- `deploy/grafana/dashboards/memories-operability.json` -- new committed dashboard asset for ingestion, search, queues, index size, rate limits, and handler health.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesMetricsTests.cs` -- name and tag-policy pins for every instrument.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesDashboardTests.cs` -- new JSON dashboard contract tests.
- `docs/dev/telemetry.md` -- source-of-truth metric catalog and dashboard pointer.
- `docs/dev/eventstore-integration.md` -- NL/embedding metric references and tag-key spelling.
- `docs/operations/rate-limiting.md` -- inbound rate-limit metric references.
- `docs/governance/PII_ACKNOWLEDGMENT.md` -- reserved conversation cache metric name.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs` -- rename mixed/snake instruments to the canonical dot family: `memories.rate.limit.rejections`, `memories.pipeline.queue.depth`, `memories.natural.language.description.duration`, `memories.natural.language.embedding.queue.depth`, `memories.natural.language.embedding.queue.bytes`, `memories.conversation.cache.hits`, and `memories.embedding.api.calls`; keep existing tag keys unchanged.
- [x] `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesMetricsTests.cs` -- pin every instrument name, assert every `MetricTagKeyPolicy` key is dot-family, and include the currently unpinned embedding, handler, and dropped-observation metrics.
- [x] `deploy/grafana/dashboards/memories-operability.json` -- add a committed Grafana dashboard with panels for ingestion throughput/failures, search latency/requests by axis, index size, pipeline/NL retry queues, rate-limit rejections, embedding calls, handler mismatches, dropped observations, and reserved cache hits.
- [x] `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesDashboardTests.cs` -- parse the dashboard JSON, verify it is valid, verify panel queries use only `MemoriesMeter.MetricTagKeyPolicy` names converted to Prometheus query names, and reject forbidden tag keys.
- [x] `docs/dev/telemetry.md`, `docs/dev/eventstore-integration.md`, `docs/operations/rate-limiting.md`, `docs/governance/PII_ACKNOWLEDGMENT.md` -- update metric catalogs, PromQL examples, and reserved-metric wording to the normalized names and `tenant_id` tag spelling.
- [x] `_bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md` -- record implementation notes, validation results, and any dashboard/query limitations.

**Acceptance Criteria:**
- Given Memories custom metrics are emitted, when the meter is inspected, then every `MemoriesMeter` instrument name belongs to one dot-separated `memories.*` family and no legacy snake_case metric name remains in source, tests, or docs.
- Given the metric tag policy is the dashboard contract, when tests parse the committed dashboard, then every referenced metric and tag key is present in `MetricTagKeyPolicy` and no forbidden high-cardinality tag is used.
- Given a developer opens the repo without external setup, when they inspect observability assets, then at least one Grafana dashboard JSON is committed and documented as the starter dashboard for the custom Memories metrics.
- Given the conversation cache hit metric is still SDK-gated, when Story 24.4 completes, then the metric name is normalized and included as reserved dashboard/query surface without adding false runtime emission.

## Spec Change Log

## Review Triage Log

### 2026-07-05 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 9: (high 1, medium 8, low 0)
- defer: 0
- reject: 1: (high 0, medium 0, low 1)
- addressed_findings:
  - `[high]` `[patch]` Prometheus-normalized histogram queries and tests used `_ms_*` suffixes for `ms` instruments; updated dashboard, docs, spec notes, and test variants to `_milliseconds_*` per the OpenTelemetry Prometheus translator.
  - `[medium]` `[patch]` Dashboard tests allowed `_total` variants for gauges/histograms; changed variants to be instrument-kind-specific so nonexistent counter/gauge series cannot pass.
  - `[medium]` `[patch]` Dashboard tests validated tags against a global union; changed validation to enforce each referenced metric's own `MetricTagKeyPolicy` tags while allowing histogram `le`.
  - `[medium]` `[patch]` Grafana dashboard used `${DS_PROMETHEUS}` without import metadata; added a Prometheus `__inputs` datasource declaration.
  - `[medium]` `[patch]` Tenant selector read only successful-ingestion series; changed it to list tenant IDs from all `memories_*` custom metric series.
  - `[medium]` `[patch]` Search latency p95 dropped `tenant_id` during all/multi-tenant views; grouped quantiles by `tenant_id`, `axis`, and `le`.
  - `[medium]` `[patch]` Natural-language retry depth and bytes shared one panel/unit; split queue bytes into a separate byte-unit panel.
  - `[medium]` `[patch]` Registered handler count and mismatch rate shared one panel/unit; split mismatch rate into a separate rate panel.
  - `[medium]` `[patch]` The non-readable component YAML comment overstated reserved cache metric protection; corrected it to point at the readable-YAML startup validator.

## Design Notes

The canonical instrument-name shape is dot-separated lowercase words after the `memories` prefix. Prometheus query names in dashboard JSON may use collector-normalized underscores, but tests should derive those query tokens from `MetricTagKeyPolicy` so the dashboard cannot drift from code.

## Implementation Notes

- Normalized all `MemoriesMeter` instrument constants to dot-separated `memories.*` names while preserving existing tag keys and `tenant_id="__rejected__"`.
- Added dashboard contract coverage that parses [../../deploy/grafana/dashboards/memories-operability.json](../../deploy/grafana/dashboards/memories-operability.json), derives allowed Prometheus query names from `MemoriesMeter.MetricTagKeyPolicy`, allows histogram `le`, and rejects `case_id`, `user`, and `memory_unit_id`.
- The dashboard uses Prometheus-normalized query tokens such as `memories_natural_language_description_duration_milliseconds_bucket` and `memories_conversation_cache_hits_total`; these are query names derived from canonical dot instruments, not runtime instrument names.
- The conversation cache panel remains reserved until the Dapr Conversation SDK exposes cache hit/miss metadata; no runtime emission was added.
- Review patched the Grafana datasource import metadata, latency query suffixes, tenant grouping, mixed-unit panels, tenant selector coverage, and dashboard contract tests.

## File List

- [../../src/Hexalith.Memories.Telemetry/MemoriesMeter.cs](../../src/Hexalith.Memories.Telemetry/MemoriesMeter.cs)
- [../../src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptionsValidator.cs](../../src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptionsValidator.cs)
- [../../deploy/grafana/dashboards/memories-operability.json](../../deploy/grafana/dashboards/memories-operability.json)
- [../../tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesMetricsTests.cs](../../tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesMetricsTests.cs)
- [../../tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesDashboardTests.cs](../../tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesDashboardTests.cs)
- [../../docs/dev/telemetry.md](../../docs/dev/telemetry.md)
- [../../docs/dev/eventstore-integration.md](../../docs/dev/eventstore-integration.md)
- [../../docs/operations/rate-limiting.md](../../docs/operations/rate-limiting.md)
- [../../docs/governance/PII_ACKNOWLEDGMENT.md](../../docs/governance/PII_ACKNOWLEDGMENT.md)

## Verification

**Commands:**
- `dotnet build src/Hexalith.Memories.Telemetry/Hexalith.Memories.Telemetry.csproj -m:1 /nodeReuse:false --no-restore` -- expected: telemetry project builds with warnings as errors.
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -- expected: server builds with renamed metric constants.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- expected: focused test project builds.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~MemoriesMetricsTests|FullyQualifiedName~MemoriesDashboardTests|FullyQualifiedName~TelemetryMetricsRecorderTests|FullyQualifiedName~EmbeddingInputContentKindTests|FullyQualifiedName~GenerateEmbeddingActivityTests"` -- expected: metric contract, dashboard contract, and runtime emission tests pass.
- `rg "memories_natural|memories_conversation|memories\\.rate_limit|memories\\.pipeline\\.queue_depth|memories\\.embedding\\.api_calls" src tests docs deploy` -- expected: no source/test runtime instrument names use stale legacy names; only documented Prometheus-normalized query names may remain.
- `git diff --check` -- expected: no whitespace errors.

**Results:**
- `dotnet build src/Hexalith.Memories.Telemetry/Hexalith.Memories.Telemetry.csproj -m:1 /nodeReuse:false --no-restore` -- passed, 0 warnings, 0 errors.
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -- passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- passed on serial rerun, 0 warnings, 0 errors. Earlier parallel build attempts produced output-file contention and were rerun serially.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~MemoriesMetricsTests|FullyQualifiedName~MemoriesDashboardTests|FullyQualifiedName~TelemetryMetricsRecorderTests|FullyQualifiedName~EmbeddingInputContentKindTests|FullyQualifiedName~GenerateEmbeddingActivityTests"` -- passed, 44/44.
- Review rerun: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- passed, 0 warnings, 0 errors.
- Review rerun: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~MemoriesMetricsTests|FullyQualifiedName~MemoriesDashboardTests|FullyQualifiedName~TelemetryMetricsRecorderTests|FullyQualifiedName~EmbeddingInputContentKindTests|FullyQualifiedName~GenerateEmbeddingActivityTests"` -- passed, 44/44.
- Review rerun: `dotnet build src/Hexalith.Memories.Telemetry/Hexalith.Memories.Telemetry.csproj -m:1 /nodeReuse:false --no-restore` -- passed, 0 warnings, 0 errors.
- Review rerun: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -- passed, 0 warnings, 0 errors.
- `rg "memories_natural|memories_conversation|memories\\.rate_limit|memories\\.pipeline\\.queue_depth|memories\\.embedding\\.api_calls|_ms_bucket|_ms_count|_ms_sum" src tests docs deploy _bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md` -- returned only documented Prometheus-normalized dashboard/doc/spec query names for canonical natural-language and conversation metrics; no source/test runtime instrument names use stale legacy names and no `_ms_*` histogram query suffixes remain.
- `git diff --check` -- passed.

## Auto Run Result

**Summary:** Story 24.4 normalized repo-owned Memories custom metric instruments into the dot-separated `memories.*` family, added a committed Grafana starter dashboard, and pinned the dashboard/query contract to `MemoriesMeter.MetricTagKeyPolicy`.

**Files changed:** Metric constants in `MemoriesMeter.cs`; dashboard JSON in `deploy/grafana/dashboards/memories-operability.json`; metric and dashboard tests under `tests/Hexalith.Memories.Server.Tests/Telemetry`; telemetry, EventStore, rate-limit, and PII docs; the readable-YAML validator comment for reserved conversation cache behavior; sprint/story artifacts.

**Review findings breakdown:** 0 intent gaps, 0 bad-spec findings, 9 patch findings addressed (1 high, 8 medium), 0 deferred findings, and 1 low finding rejected as already covered by explicit reserved-cache wording and tests.

**Follow-up review recommendation:** Recommended. The review pass corrected dashboard query suffixes, panel grouping/units, and test contract strictness after implementation, so a later lightweight reviewer pass should re-check Grafana import behavior and Prometheus translator assumptions.

**Verification performed:** Telemetry, server, and server-test project builds passed; focused metric/dashboard/runtime tests passed 44/44; dashboard JSON parsed with `python3 -m json.tool`; stale-name search found no runtime legacy names or `_ms_*` histogram suffixes; `git diff --check` passed.

**Residual risks:** The dashboard is committed and contract-tested, but it has not been imported into a live Grafana instance in this run. Prometheus query names assume the default OpenTelemetry Collector Prometheus translation strategy that normalizes `ms` units to `milliseconds`.
