# EventStore / Hexalith Module Event Integration — DAPR Pub/Sub Subscription (Story 9.1)

Zero-code DAPR pub/sub subscription for `Hexalith.Memories`. Configure one topic, publish CloudEvents
to it, and the Memories server ingests the event payload into the existing ingestion workflow —
no mapping code required. Scope: **one topic per deployment**; dual-embeddings and causal-edge
indexing are Story 9.2+.

This is the **cross-module event-intake contract** for all Hexalith modules — Tenants, Parties, and
future modules publish their domain event streams through this same Dapr pub/sub path. It is **not** an
EventStore-only integration: the package name reflects the publisher SDK, but any Hexalith module is a
first-class publisher. See [§1.6 Route surface for Hexalith modules](#16-route-surface-for-hexalith-modules)
for the canonical module-to-Memories flow and a shared-topic `SourceToTenantMap` example covering two
modules (`hexalith/tenants` and `hexalith/parties`) on one topic.

- **Status:** Phase 1.5
- **Package:** [`Hexalith.Memories.EventStore`](../../src/Hexalith.Memories.EventStore/) (NuGet-publishable)
- **Server wiring:** [`Hexalith.Memories.Server/EventStoreIntegration/`](../../src/Hexalith.Memories.Server/EventStoreIntegration/)
- **Broker component:** [`deploy/dapr/components/pubsub.yaml`](../../deploy/dapr/components/pubsub.yaml)

---

## 1. Setup

### 1.1 Add the package (consumer) or project reference (in-repo host)

In-repo host (Memories Server already does this):

```xml
<ProjectReference Include="..\Hexalith.Memories.EventStore\Hexalith.Memories.EventStore.csproj" />
```

Downstream hosts referencing the NuGet package:

```bash
dotnet add package Hexalith.Memories.EventStore
```

### 1.2 Register services

```csharp
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(Hexalith.Memories.EventStore.EventIngestionController).Assembly);

// In the Memories Server, this calls AddMemoriesEventStoreIntegration and wires the five adapter
// implementations (workflow scheduler, tenant status, case creation, telemetry, preflight dedup).
builder.Services.AddServerEventStoreIntegration(builder.Configuration);

var app = builder.Build();

// Middleware + controller mapping order (ADR 9.1 Middleware order).
app.UseCloudEvents();
app.MapControllers();
app.MapSubscribeHandler();
// EventStore resolves the controller topic from MEMORIES_EVENTSTORE_TOPIC before Dapr reads endpoint
// metadata, so the canonical /dapr/subscribe probe exposes the concrete topic value.
```

### 1.2.1 Public wiring surface (stable) - Story 18.1

The two extension methods downstream hosts and in-repo wiring depend on are **stable, no-redis** signatures:

```csharp
// src/Hexalith.Memories.Server/EventStoreIntegration/ServerEventStoreIntegrationExtensions.cs
internal static IServiceCollection AddServerEventStoreIntegration(
    this IServiceCollection services,
    IConfiguration configuration);

// src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs
public static IServiceCollection AddMemoriesEventStoreIntegration(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<EventStoreIntegrationBuilder>? configure = null);
```

Neither takes a `redis` parameter, and **no `AddHexalithEventStore` redis-parameter overload exists on the
Memories side**. Redis is wired implicitly via the DAPR state-store / pub-sub components (see section 1.3).
`AddHexalithEventStore` itself lives **only** in the `Hexalith.EventStore` submodule
([`references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs`](../../references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs)),
and even there takes no redis parameter.

The Parties consumer intake (MEM-1, 2026-05-27) reported an `AddHexalithEventStore` redis-parameter "drift";
a grounded codebase review found this was a **stale submodule pin** on the consumer side, not a current
Memories API. The signatures above are the authoritative wiring surface. See
[public-surface-stability.md](./public-surface-stability.md) for the matching project/assembly/namespace
stability contract and the compile-time resolution guard that enforces it.

### 1.3 Wire the pub/sub broker

Production deployments bind-mount `deploy/dapr/components/pubsub.yaml` and inject:

- `PUBSUB_REDIS_HOST` — broker endpoint.
- `PUBSUB_REDIS_PASSWORD` — broker credential (via the platform secret manager).
- `MEMORIES_EVENTSTORE_TOPIC` — topic name to subscribe to. **Must match** `EventStoreIntegration:Routing:Topic`.

Aspire (local dev) registers the component programmatically in
[`src/Hexalith.Memories.AppHost/Program.cs`](../../src/Hexalith.Memories.AppHost/Program.cs) — the broker
reuses the existing Redis dependency.

### 1.4 Configure routing

`appsettings.Development.json`:

```json
{
    "EventStoreIntegration": {
        "Routing": {
            "PubSubName": "pubsub",
            "Topic": "memories-events",
            "SourceToTenantMap": {
                "enterprise/claims": "acme-claims",
                "enterprise/billing": "acme-billing"
            },
            "AutoCreateCases": true,
            "CaseNameTemplate": "events:{aggregateType}",
            "MaxAutoCreatedCasesPerTenant": 100,
            "PreflightDedupEnabled": true,
            "PreflightDedupTtl": "1.00:00:00"
        }
    }
}
```

### 1.5 Environment defaults table

| Option | Development | Production | Rationale |
| --- | --- | --- | --- |
| `AutoCreateCases`              | `true`      | `false`                                                 | Development optimizes for zero-config DX (PRD §534). Production requires explicit tenant/case provisioning so a mis-routed publisher can't silently create cases. ADR 9.1-C. |
| `MaxAutoCreatedCasesPerTenant` | `100`       | `100`                                                   | Hard cap is a safety backstop regardless of environment.                                                                                                                     |
| `PreflightDedupEnabled`        | `true`      | `true`                                                  | Saves 1-3 s of embedding compute per at-least-once redelivery. Fails open on Redis outage. ADR 9.1-B.                                                                        |
| `PreflightDedupTtl`            | `24h`       | **Must be ≥ DAPR resiliency max-duration + 10% buffer** | See §7 TTL coupling.                                                                                                                                                         |

### 1.6 Route surface for Hexalith modules

The Memories Server is the sidecar-managed event subscriber for Hexalith modules. Domain modules publish
CloudEvents to the configured DAPR pub/sub component and shared-topic pattern; they do not call Memories
REST ingestion directly for domain event streams.

**Canonical cross-module event flow.** Every Hexalith module event reaches Memories through exactly this
path, with no per-module ingestion code:

`Hexalith module -> Dapr pub/sub component -> Memories Server sidecar -> POST /events/ingest -> EventIngestionService -> DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)`

A publisher only needs Dapr pub/sub write access to the shared topic and a `source` prefix that is mapped
in `SourceToTenantMap`. The REST `POST /api/ingest` path remains for **external content** ingestion only;
Hexalith module event streams always use the pub/sub path above.

**Shared-topic `SourceToTenantMap` example (two Hexalith modules on one topic).** Both `hexalith/tenants`
and `hexalith/parties` publish to the same `memories-events` topic; the `source` prefix is what routes each
module's stream to its configured tenant:

```json
{
    "EventStoreIntegration": {
        "Routing": {
            "PubSubName": "pubsub",
            "Topic": "memories-events",
            "SourceToTenantMap": {
                "hexalith/tenants": "tenant-events",
                "hexalith/parties": "party-events"
            }
        }
    }
}
```

Source-prefix matching is **longest-prefix-wins and case-insensitive**; `source` is the publisher's
**stable identity**, not a deploy-time URL (see §5); and an unmapped `source` drops as `unknown-source`
(EventId 9110) with **no DAPR retry** (see §6). One topic serves every module in a deployment — independent
topics require separate Memories deployments per topic until multi-topic routing is approved (see §8).

The published operation surface for DAPR ACL and route review is:

- `GET /dapr/subscribe` for subscription discovery.
- `POST /events/ingest` for DAPR pub/sub delivery through the Memories Server sidecar.

`/process` is not part of the Memories event-ingest surface. ACLs or downstream runbooks that reference
`/process` are using the wrong operation path.

When one shared topic cannot satisfy independent routing or retention requirements, run separate Memories
deployments per topic until multi-topic routing in a single deployment is approved.

---

## 2. CloudEvents envelope requirements

| Field             | Required | Notes                                                                                                                                                                     |
| ----------------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `id`              | **Yes**  | Drives idempotency — the existing `DedupKeyBuilder` hashes this as `sourceUri`. Must be globally unique per at-least-once semantics.                                      |
| `source`          | **Yes**  | Publisher-supplied URI-reference. Matched longest-prefix (case-insensitive) against `SourceToTenantMap`.                                                                  |
| `type`            | **Yes**  | Aggregate type extracted from the **second dotted segment** (e.g. `MyApp.Claims.ClaimSubmittedV2` → `Claims`). Falls back to the full type when no second segment exists. |
| `subject`         | Optional | Aggregate identifier. Absent values persist as `(unset)`. Exact-match filterable via the `cloudevent.subject` metadata field (AC #2, #4).                                 |
| `time`            | Optional | ISO-8601 publisher-supplied timestamp. Preserved verbatim; **never replaced with server time** (clock-skew risk).                                                         |
| `datacontenttype` | Optional | Defaults to `application/json` when absent.                                                                                                                               |
| `data`            | **Yes**  | Event payload. Missing or null `data` returns `400 INVALID_CLOUDEVENT`.                                                                                                   |

### 2.1 Aggregate-type extraction

The convention `<Namespace>.<Aggregate>.<Event>V<n>` keeps `Aggregate` stable across event versions.
Publishers using different conventions must either send the aggregate name as the second dotted
segment or fall back to the full type string (the router handles this automatically).

### 2.2 Exact-match subject filtering

`cloudevent.subject` is copied into `IngestionInput.Metadata` and flows through the same exact-match
metadata index path used by search filters. Queries filtering on `cloudevent.subject` return events
for a specific aggregate without scanning the entire topic's backlog. Use `GET /api/search?...&subject=<value>`
to drive the dedicated exact-match TAG filter instead of a fuzzy metadata text match.

---

## 3. At-least-once + dead-letter + replay semantics

DAPR pub/sub is at-least-once. The subscription endpoint translates each outcome to the HTTP-status
DAPR expects:

| Situation                                                | HTTP                                    | DAPR behavior                                                 |
| -------------------------------------------------------- | --------------------------------------- | ------------------------------------------------------------- |
| Accepted — workflow scheduled                            | 200 + `accepted`                        | No retry.                                                     |
| Duplicate — preflight or workflow-level dedup rejected   | 200 + `duplicate`                       | No retry.                                                     |
| Unknown source                                           | 200 + `unknown-source` + Warning log    | No retry (publisher never mapped).                            |
| Tenant not found                                         | 500 + `tenant-not-found` + Warning log  | Retry; reaches DLT only if operators configure DAPR retry + dead-letter topics. |
| Tenant deleting or unavailable                          | 500 + `tenant-deleting` + Warning log   | Retry; reaches DLT only if operators configure DAPR retry + dead-letter topics. |
| Auto-create disabled                                     | 200 + `auto-create-disabled`            | No retry (operator opted out).                                |
| Case cap exceeded                                        | 200 + `case-cap-exceeded` + Warning log | No retry.                                                     |
| Tenant provisioning                                      | 500                                     | Retries until tenant becomes active or retry budget exhausts. |
| Malformed envelope (`id`/`source`/`type`/`data` missing) | 400 + `INVALID_CLOUDEVENT`              | Dead-letter topic (if configured); else dropped.              |
| Transient scheduling failure                             | 500 (preflight reservation released)    | DAPR retries on clean key.                                    |

### 3.1 Replay semantics

Hexalith.EventStore supports event replay. Replayed events use the same envelope + `cloudevent.id` —
the memory unit already exists, `CheckIdempotencyActivity` returns duplicate, the workflow returns
early. This is intentional for normal at-least-once redelivery but **WRONG for operator-triggered
replay-after-tenant-restore**.

To rebuild memory from an event stream, operators must first delete the tenant's memory units
(Story 3.5) **OR** recreate the tenant via the provisioning workflow, then re-publish. Story 9.1
does NOT add a `forceReplay` bypass — that would regress the at-least-once idempotency guarantee.

### 3.2 Routing-config changes must quiesce the stream

If an operator flips `AutoCreateCases` off → on or renames a `SourceToTenantMap` entry between
two redeliveries of the same event, the case mapping changes and the second delivery produces a
second memory unit (different `caseId` → different dedup key). Document this at the operator level:
"routing config changes require a quiesce of the publisher first, then a drain of in-flight events,
then the flip."

---

## 4. Publisher trust & spoofing — deploy-time mitigations

**Threat:** any process with DAPR pub/sub write access can publish CloudEvents with an arbitrary
`source` string. An insider or compromised service publishing `source=enterprise/hr` routes to the
`hr-tenant` without any authentication check. `source` is an auth-like boundary with no auth.

**MVP mitigations (deploy-time, not code):**

1. Restrict publisher access via `publishAllowedTopics` and component-level access control in
   [`deploy/dapr/components/pubsub.yaml`](../../deploy/dapr/components/pubsub.yaml).
2. Do not expose the broker externally. All publisher traffic must go through an authenticated
   service mesh or sidecar with a mutual-TLS policy.
3. Log `memories_eventstore_unknownsource_total{source}` per-source (EventId 9110) and alert on
   unexpected sources (§6 Alerting).

**Phase 2 evolution (out of scope for 9.1):** signed JWT in a CloudEvents extension attribute
(e.g. `tenantidtoken`) verified against a tenant public key in TenantRegistry. Out of scope here.

---

## 5. Source-stability publisher contract

Publishers MUST treat `source` as a stable identifier, not a deploy-time URL. A routine
certificate-migration or hostname-flip that changes `https://` ↔ `http://` or adds a port number
breaks longest-prefix matching and causes events to be silently dropped as `UnknownSource`.

**Contract:**

- `source` is an opaque identifier chosen at publisher design time.
- Operators configuring `SourceToTenantMap` should match on a semantic prefix
  (e.g. `enterprise/claims`) — not a full URL.
- Publishers changing `source` must coordinate a `SourceToTenantMap` config update + rolling
  deploy with the subscriber; a unilateral change on the publisher side is a breaking change.

---

## 6. Alerting recommendations

| Signal                                                | Source            | Recommended alert                                                                                                           |
| ----------------------------------------------------- | ----------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `memories_eventstore_unknownsource_total{source=...}` | EventId 9110      | Rate of increase > 0 for 5 min pages the subscriber team. Indicates publisher drift or a misconfigured `SourceToTenantMap`. |
| EventId 9111 / 9112 (tenant deleting/unavailable or missing) | Warning      | Rate > 0 for 5 min warns operators to fix tenant rollout/registry state or inspect DAPR retry/DLT.                         |
| EventId 9121 (invalid-envelope)                       | Error             | Rate > 5/min pages. Indicates a publisher is emitting malformed CloudEvents.                                                |
| EventId 9120 (schedule-failed)                        | Error             | Rate > 1/min pages. Transient DAPR sidecar / workflow runtime problem.                                                      |
| EventId 9105 (routing-config-unknown-tenant)          | Critical, startup | Fail-fast crash — do not restart the pod without fixing config.                                                             |
| EventId 9114 (case-cap-exceeded)                      | Warning           | Warn operator; likely aggregate-type cardinality misconfiguration (e.g. `type` encoded into an id).                         |

---

## 7. Preflight TTL ↔ DAPR retry-policy alignment

`TenantEventRoutingOptions.PreflightDedupTtl` (default `24h`) MUST be aligned with the DAPR
resiliency policy `max-duration`. If a message delayed longer than `PreflightDedupTtl` is
redelivered, the preflight key has expired, the workflow runs a second time, and a duplicate
memory unit is created. The workflow-level permanent dedup key (no TTL) remains authoritative so
this is a correctness **optimization**, not a correctness hole — but operators should still align
the two so the fast path stays fast.

**Rule of thumb:** `PreflightDedupTtl ≥ DAPR resiliency max-duration + 10%`. If your retry policy
allows 72h, set `PreflightDedupTtl` to at least `79:12:00` (79.2 h). Or explicitly cap the
resiliency policy's `max-duration` at `23h` so the default 24h TTL covers it.

---

## 8. Known limitations

- **Single-topic subscription per deployment.** A consumer needing N topics runs N deployments.
  Multi-topic routing is a future refinement.
- **Replay vs idempotency.** See §3.1. Story 9.1 does not provide a force-replay bypass.
- **Case cap.** Each tenant is hard-capped at 100 auto-created cases (`MaxAutoCreatedCasesPerTenant`);
  once exceeded, new aggregate-types drop with `case-cap-exceeded` until operators raise the cap
  or pre-provision cases.
- **Causal-edge indexing and dual embeddings.** Story 9.2 owns these. Story 9.1 adds exactly one
  memory unit per event with standard single-embedding ingestion.
- **Subject may be absent.** Missing `subject` is preserved as `(unset)` — grouping by aggregate
  requires the publisher to send a subject.
- **Publisher spoofing.** `source` is not authenticated. See §4.
- **No dead-letter configuration in-repo.** `pubsub.yaml` does not define a DLT; operators opt into
  DAPR DLT at the component level when needed.

---

## 9. Troubleshooting — "Why didn't my event appear?"

1. **Check subscription discovery.** `curl $DAPR_HTTP_ENDPOINT/dapr/subscribe` should include an
   entry with `pubsubname=pubsub` + your configured topic + `route=/events/ingest`. If empty, the
   controller's environment-backed subscription metadata did not resolve. Usually means `MEMORIES_EVENTSTORE_TOPIC` is
   not set or does not match `EventStoreIntegration:Routing:Topic`.
2. **Check source mapping.** If you see EventId 9110 (`UnknownSource`) for your event, the `source`
   field does not match any `SourceToTenantMap` prefix. Matching is case-insensitive longest-prefix.
3. **Check tenant status.** EventId 9111 → tenant is deleting or unavailable (retry); 9102 → tenant
   is provisioning (retry); 9112 → tenant does not exist (retry). Fix the tenant rollout/registry
   state first, then inspect DAPR retry/DLT if the event remains undelivered.
4. **Check malformed envelope.** EventId 9121 → the envelope is missing `id`/`source`/`type`/`data`.
   Review the publisher to ensure all required fields are present and non-empty.
5. **Check workflow scheduling.** EventId 9120 → DAPR workflow scheduling threw. Check the DAPR
   sidecar and the workflow runtime health.
6. **Check case cap.** EventId 9114 → the tenant hit `MaxAutoCreatedCasesPerTenant`. Raise the cap
   or pre-create the case.
7. **Check replay drift.** If you re-published an event and it didn't re-index: this is expected
   (idempotency). See §3.1.

---

## 10. Worked example — from publish to searchable memory

```csharp
// Publisher (downstream service).
var daprClient = new DaprClientBuilder().Build();
await daprClient.PublishEventAsync(
    pubsubName: "pubsub",
    topicName: "memories-events",
    data: new { amount = 100, currency = "EUR", claimId = "claim-42" },
    metadata: new Dictionary<string, string>
    {
        ["cloudevent.id"] = $"claim-{Guid.NewGuid():N}",
        ["cloudevent.source"] = "enterprise/claims",
        ["cloudevent.type"] = "MyApp.Claims.ClaimSubmittedV2",
        ["cloudevent.subject"] = "claim-42",
    });
```

Expected server behavior (all green):

1. DAPR sidecar POSTs the CloudEvent to `/events/ingest`.
2. `EventIngestionController` → `EventIngestionService` → routes `source=enterprise/claims` →
   `acme-claims` tenant → case `events:Claims` (auto-created on first event for this aggregate-type).
3. Preflight dedup reserves `dedup:acme-claims:<caseId>:<sha256(cloudeventid)>`.
4. `IngestionWorkflow` extracts the JSON payload, generates an embedding, indexes it syntactically,
   semantically, and into the graph.
5. Within 5 s (NFR6), the memory unit is searchable:

```bash
curl "$MEMORIES_URL/api/search?tenantId=acme-claims&query=claim-42&axis=syntactic"
# → HybridSearchResult containing the new memory unit
```

Story 9.1 scope: the memory unit is searchable. Causal-edge indexing and a second NL-description
embedding come in Story 9.2.

With Story 9.2 shipped, the same event now produces **two** Redis Vector hashes:

1. `acme-claims:vec:<memoryUnitId>` — raw JSON payload embedding (unchanged from 9.1).
2. `acme-claims:vecnl:<memoryUnitId>` — LLM-authored natural-language description embedding, plus a
   `naturalLanguageDescription` field for operator inspection. See "Dual embedding pipeline" below.

FalkorDB gains `caused_by` and `correlated_with` edges when CloudEvent extensions supply
`causationid` / `correlationid`, respectively. See "Correlation root semantics" below.

## Dual embedding pipeline

Story 9.2 adds a second embedding axis scoped to `SourceType.Event` memory units: the
`IngestionWorkflow`, after generating the raw-payload embedding, calls
`GenerateNaturalLanguageDescriptionActivity` (DAPR Conversation API), re-runs
`GenerateEmbeddingActivity` with `ContentKind = NaturalLanguageDescription`, and writes the result to
the new per-tenant vector index `{tenant}:memories:vec:nl`.

Story 21.3 moved NL hashes from the legacy nested `{tenant}:vec:nl:*` prefix to the disjoint
`{tenant}:vecnl:*` prefix. The public NL index name remains `{tenant}:memories:vec:nl`. Running the
embedding vector live migration copies verified legacy NL hashes to the disjoint prefix, deletes the
legacy copy only after target verification, and drops/recreates the raw and NL RediSearch indexes
without `DD` so raw KNN no longer indexes NL-only hashes.

**Why NL:** the raw JSON embedding captures schema structure (field names, data types). A technical
event like `CounterIncrementedV1 {"counterId": "cart", "delta": 1}` embeds similarly across all counter
events regardless of semantic meaning. The NL description rewrites the event as a sentence
(`"A shopping cart counter was incremented by one unit."`) — the embedding now captures the business
action rather than the schema shape. Hybrid search on the NL axis surfaces semantically-close events
across schema variants.

**Prompt template (verbatim — do not edit without updating the cleaner):**

- System: `You are an event summarizer. Given a JSON event payload of type {EventType}, write a single natural-language sentence (≤40 words) describing what business action occurred. Do NOT repeat field names. Focus on domain meaning. Return ONLY the sentence, no preamble or JSON.`
- User: `Event type: {EventType}\nAggregate: {AggregateType ?? "(unspecified)"}\nPayload:\n{truncatedJson}`
- `Temperature = 0.1`, `MaxTokens = 80`, no tools, no streaming (DAPR alpha limit).

**Performance envelope:** NFR6's <5s indexing freshness is relaxed to **<7s for `SourceType.Event`**
because the LLM call adds 1-3s (p95) to the critical path. Non-event sources still honor <5s.

**Doubled embedding API volume:** each event now calls the embedding provider **twice** — once for
the raw payload, once for the NL description. Both calls go through the SAME
`EmbeddingRateLimiterActor` per tenant (so total throughput is preserved correctly), but operators
must size the `RateLimitPerMinute` ceiling with the 2x factor in mind. Telemetry exposes
`memories.embedding.api_calls{content_kind="payload|naturalLanguageDescription",tenant=...}` so the
2:1 split is observable.

## LLM provider swap procedure

1. Prepare `deploy/dapr/components/conversation-llm.yaml` with the target provider (e.g.,
   `type: conversation.anthropic`) — keep the component name `"llm"` so existing workloads route
   through it without code changes.
2. Store the provider API key in the DAPR secret store component (`secretstore`).
3. Validate in staging: `dapr components -k` lists the new component; publish a test event; verify
   `9150 NaturalLanguageDescriptionGenerated` log contains the new `llmProvider` attribute.
4. Deploy: the component is swapped atomically — DAPR reloads components on file change. No server
   restart required.
5. Monitor: `memories.natural_language.description.duration_ms` histogram stabilizes within ~5 min.

## Natural language embedding retry queue

When the DAPR Conversation API is unavailable, `IngestionWorkflow` catches the typed
`NaturalLanguageDescriptionUnavailableException`, sets `IngestionResult.NaturalLanguageEmbeddingStatus
= Queued`, and enqueues a retry record via `QueueNaturalLanguageEmbeddingRetryActivity`. The memory
unit remains searchable on the raw-semantic + syntactic + graph axes; only the business-meaning axis
is delayed. `NaturalLanguageEmbeddingRetryHostedService` drains the queue on a configurable interval
(default 60s, `NaturalLanguage:RetryIntervalSeconds`).

**Operator commands:**

```bash
# Backlog count (live queue)
redis-cli ZCARD nl-embedding-retry:{tenant}

# Oldest 10 entries with queue timestamps
redis-cli ZRANGE nl-embedding-retry:{tenant} 0 9 WITHSCORES

# Dead-letter count (records that exhausted MaxRetryAttempts)
redis-cli ZCARD nl-embedding-retry-dead:{tenant}

# Peek a dead-letter entry (operator can manually re-enqueue by copying to live queue)
redis-cli ZRANGE nl-embedding-retry-dead:{tenant} 0 0 WITHSCORES

# Re-enqueue a dead-lettered entry — Redis ZADD to the live queue with a fresh score
redis-cli ZADD nl-embedding-retry:{tenant} "$(date +%s%N | cut -c1-17)" '<record-json>'
```

**Recommended Prometheus alerts:**

- `rate(memories_natural_language_embedding_queue_depth[5m]) > 10` sustained 15m — backlog growing.
- `memories_natural_language_embedding_queue_depth > 1000` — dead-letter candidate escalation.

**Event IDs:** `9152` queued · `9153` retry succeeded · `9170` backlog warning · `9179` backlog error
· `9180` dead-letter.

## Gap markers and retroactive resolution

When `CausationId` points to a memory unit that has not yet been ingested (out-of-order delivery),
`IndexGraphActivity` creates a **stub node** via `BuildMergeStubNode(causationId, stubCreatedAt)`.
The stub carries `isStub = true` + `stubCreatedAt` but no content. `GraphTraversalService` detects
stubs via the explicit `isStub` flag (preferred) or the legacy content-absent heuristic (fallback for
pre-9.2 data — retires after `IsStubBackfillMigration` runs, see deferred-work.md).

When the missing cause event later arrives, `BuildMergeMemoryUnitNode`'s MERGE promotes the stub:
`isStub = false`, all content fields populated, `stubCreatedAt` preserved as historical evidence.
The activity reads `previousIsStub` from the MERGE result — when `true`, event `9154 StubNodeResolved`
is emitted (carries `stubCreatedAt` + `resolvedAt` so operators can compute retroactive resolution
latency = `resolvedAt - stubCreatedAt`).

**Orphan stub detection** (stubs that were never resolved — publisher disappeared, source event lost):

```cypher
MATCH (m:MemoryUnit)
WHERE m.isStub = true
  AND m.stubCreatedAt < (datetime() - duration('PT24H'))
RETURN m.id AS memoryUnitId, m.stubCreatedAt AS createdAt
ORDER BY m.stubCreatedAt ASC
LIMIT 100
```

Run this hourly per tenant; operators decide whether to (a) re-publish the missing source event,
(b) drop the stub via a maintenance command, or (c) leave it as a documented dataset gap.

## Correlation root semantics

CloudEvent `correlationid` (extension attribute or envelope field) groups events that participate
in the same business workflow. Story 9.2 fixes a subtle implementation detail:

- **Edge direction:** `(root)-[:CORRELATED_WITH]->(current)` — the edge points **from** the root
  event **to** the current event. Walking `direction=outbound, edgeType=CorrelatedWith` from the
  root returns all correlated events; walking `direction=inbound` from any correlated event returns
  only the root.
- **Self-edge guard:** when `CorrelationId == MemoryUnitId` (the event IS the correlation root — a
  common publisher convention where the first event carries `correlationid = cloudevent.id`), NO
  edge is created. Event `9155 CorrelationIdSelfEdgeSkipped` is emitted at Debug level.
- **No fan-out:** if 10 events share a `correlationid`, the graph has **10 edges** (one per event to
  the root), NOT 90 edges (fan-out between every pair). Operator expectation mismatches this
  direction sometimes — the intent is "the group of correlated events" treated as a hub-and-spoke
  around the root.

**Worked example.** Publisher sends a root event `R` with `correlationid = R_id`, then correlated
events `A`, `B`, `C` each with `correlationid = R_id`. Resulting graph:

```
(R) ──[CORRELATED_WITH]──► (A)
(R) ──[CORRELATED_WITH]──► (B)
(R) ──[CORRELATED_WITH]──► (C)
```

R itself has no `CORRELATED_WITH` self-edge — the self-edge guard suppressed it.

## Deployment — quiescing event ingestion

Story 9.2 adds new activities to `IngestionWorkflow` (`GenerateNaturalLanguageDescriptionActivity`,
`IndexNaturalLanguageSemanticActivity`, `QueueNaturalLanguageEmbeddingRetryActivity`). DAPR Workflow
replay is deterministic — an in-flight 9.1-shape `IngestionWorkflow` history will fail replay under
9.2 code if its next activity call doesn't match the new code path.

**Mitigation (dual-layer):**

1. **Runbook (operator-side discipline):** before deploying 9.2, pause `SourceType.Event` ingestion
   for **≥2 minutes**. File / URL ingestion is unaffected — the SourceType-gated block never enters
   the new code path for those sources.
2. **Code-level gate (fail-safe):** `WorkflowReplaySafetyHostedService` (`IHostedLifecycleService`
   implementation — runs before any other hosted service) queries DAPR Workflow for active
   workflow instances and delays startup by 5s polls until count reaches 0, with a 5-min total
   timeout. Events `9171` (per-poll Warning), `9172` (single-shot Critical on timeout),
   `9173` (sidecar unreachable — fail open per Improvement Z).

## Local dev — `conversation.echo`

`deploy/dapr/components/conversation-llm.yaml` defaults to `type: conversation.echo` for local dev

- Aspire runs. The echo component returns the input unchanged — meaning the "NL description" is the
  raw JSON payload bytes. **Dev embeddings for the NL vector will be identical to the raw embedding.**
  This is intentional — the pipeline shape is exercised end-to-end without a real LLM cost.

* Event `9162 ConversationApiIsEchoComponent` fires at Warning whenever the resolved component name
  is `conversation.echo`. Local dev logs this every time.
* Production loads `appsettings.Production.json` which overrides `DaprComponentName` to a real
  provider component. `NaturalLanguageDescriptionOptionsValidator` emits Critical event `9161
EchoComponentNotAllowedInProduction` if Production somehow resolves to the echo component.

## Known limitations

- **No per-tenant LLM provider.** MVP ships one system-wide `llm` DAPR component. Per-tenant routing
  is Phase 2.
- **No streaming LLM responses.** DAPR Conversation API alpha does not expose streaming.
- **No retroactive backfill for pre-9.2 events.** Events ingested before 9.2 shipped have only the
  raw embedding. Operators who want backfill can mark the source events for re-ingestion via the
  existing `ReIngestionCoordinator` — the re-run generates the NL embedding naturally.
- **LLM hallucination is possible.** Low-temperature prompts reduce drift, but the NL description
  is best-effort summarization — not domain truth. Descriptions are tagged `origin=ai` with a
  confidence signal (logprobs-derived when the provider exposes them; `null` otherwise) so UIs can
  render "AI-inferred (estimate unavailable)" distinctly. Users can correct descriptions via Story
  3.6 annotations-and-corrections.
- **Response cache is cross-tenant at the sidecar level.** `deploy/dapr/components/conversation-llm.yaml`
  ships with `responseCacheTTL: 0s` (caching disabled) by default. Enabling caching requires explicit
  operator acknowledgment (`NaturalLanguage:AcceptCrossTenantCacheSharing = true` or env var
  `HEXALITH_ACCEPT_CROSS_TENANT_CACHE_SHARING=1`) or the startup validator fails fast with event
  `9164`. The blast-radius of a leak is a privacy incident — operators OPT IN, never opt out.

## LLM hallucination posture

NL descriptions are produced by a large-language model responding to a single-sentence summarization
prompt. Even with low temperature (`0.1`) and an explicit "focus on domain meaning" instruction, the
model may:

- Invert semantic polarity (summarize a `PolicyRenewed` event as "policy canceled");
- Invent fields not present in the payload (attribute amounts or counterparties the event does not
  carry);
- Mis-classify the business action (summarize an administrative no-op as a substantive change).

Because the NL description is persisted into the tenant NL semantic hash, a hallucinated summary
can surface in any future consumer that queries that hash directly. Story 9.2 ships
`NaturalLanguageSemanticSearchService` as a library-only surface; the default `HybridSearchService`
axis is unchanged until a later opt-in rollout. The project's defensive posture:

1. **Provenance tagging.** Every persisted description in the NL semantic hash carries
   `descriptionOrigin = "ai"`. When `NaturalLanguage:PersistInMetadata = true`, the duplicate
   metadata field is also tagged with `MetadataOrigin.Ai`. UI surfaces MUST render AI-inferred
   descriptions distinctly from operator-authored text.
2. **Confidence signal.** Current 9.2 behavior is intentionally unmeasured: Dapr.AI 1.17.6 does
   not expose logprobs on the shipped SDK surface, so `descriptionConfidence` is currently `null`
   and `descriptionConfidenceSource = "constant"`. UIs render an "AI-inferred (unmeasured)"
   affordance rather than a misleading pseudo-percentage. If a future SDK/provider exposes
   logprobs, the field can become measured without changing the provenance model.
3. **User correction path.** Story 3.6 annotations-and-corrections accept operator-authored
   descriptions that supersede the AI-generated one for display purposes. The original NL embedding
   remains indexed (so a user-visible correction doesn't silently change search behavior), but the
   UI shows the corrected text.
4. **Operator response to systematic drift.** If operators observe that NL descriptions for a given
   `event.type` are systematically wrong (e.g., an event schema the LLM doesn't understand), the
   correct remediation is to:
    - File the prompt change as a story (the system prompt is a code artifact, versioned alongside
      `GenerateNaturalLanguageDescriptionActivity`).
    - Consider swapping the DAPR Conversation component to a provider better aligned with the domain
      (YAML change, no code redeploy — see "LLM provider swap procedure" above).
    - Re-ingest the affected events via `ReIngestionCoordinator` once the prompt / provider change
      lands.
5. **What 9.2 does NOT ship.** Automated quality monitoring of NL descriptions (e.g., comparing the
   NL embedding distance to the raw embedding as a drift signal) is Phase 2. There is also no
   reliable numeric drift detector on the current SDK surface because `descriptionConfidence`
   remains intentionally unmeasured today. Operators rely on sampling, correction reports, and
   targeted NL-hash inspection instead of a built-in confidence threshold.

## PII scrubbing posture

`deploy/dapr/components/conversation-llm.yaml` ships with `piiScrubbing: false` (MVP decision). The
NL description MAY contain PII that appeared in the raw event payload; the description is indexed
into the per-tenant NL vector store (no cross-tenant leakage, but within-tenant PII propagation is
possible). Before deploying 9.2 to any tenant with PII-subject workloads, Product Owner + Legal /
Compliance stakeholders MUST sign off on a governance artifact documenting:

- Known behavior: NL descriptions may echo payload PII.
- Per-tenant opt-in: operators can flip `piiScrubbing: true` via component metadata as a future
  action (no code change required).
- Visible control: test
  `GenerateNaturalLanguageDescriptionActivityTests.PayloadWithCustomerPii_SummaryMayContainPii_DocumentedBehavior`
  serves as the code-level acknowledgment; a test is not consent — the signed artifact is.

## §11 Handler registration & mismatch detection (Story 9.3)

Story 9.3 ships a **read-only** registry + mismatch detector surfacing FR62. The feature adds zero
hot-path behaviour; it reports what the server already sees. Experimental surface (`HXL002`).

### §11.1 Listing registered handlers

```bash
$ curl http://localhost:5000/api/handlers
{
  "pubSubName": "pubsub",
  "topic": "events",
  "asOf": "2026-04-24T12:00:00+00:00",
  "subscriptionStatus": "active",
  "handlers": [
    {
      "tenantId": "acme",
      "sourcePrefix": "acme.events/claims",
      "eventTypePatterns": ["Claims"],
      "eventsProcessedCount": 5,
      "lastEventAt": "2026-04-24T11:59:00+00:00",
      "observedEventTypes": [
        {"aggregateType":"Claims","eventType":"ClaimSubmittedV2","count":3,"lastSeenAt":"2026-04-24T11:59:00+00:00"},
        {"aggregateType":"Claims","eventType":"ClaimApprovedV2","count":2,"lastSeenAt":"2026-04-24T11:58:00+00:00"}
      ],
      "error": null
    }
  ]
}
```

CLI:

```bash
memories handlers list                       # human (default)
memories --format json handlers list
memories --format table handlers list
```

**When to check:** after a config-deploy that touched `EventStoreIntegration:Routing:SourceToTenantMap`, or when
investigating "why didn't event X flow?" — the per-handler `eventsProcessedCount` + `lastEventAt` columns
confirm traffic is landing.

### §11.2 Mismatch categories

`GET /api/tenants/{tenantId}/handlers/mismatches` returns four categories with severity +
actionable suggestion. Each `Suggestion` ends with a runbook URL of the form
`https://docs.hexalith.dev/memories/runbooks/handler-{category-kebab-case}`.

1. **`UnhandledEventType` (Warning)** — an event was observed on a topic but no `SourceToTenantMap`
   entry routes its aggregateType to any tenant. Example: publisher emits
   `MyApp.Policies.PolicyCreatedV1` but only `SourceToTenantMap` entries for `acme.claims` exist —
   the detector reports the unhandled aggregate "Policies" at Warning.

2. **`StaleHandler` (Info)** — a `SourceToTenantMap` entry is configured for a tenant but the
   observation store returns an empty list for the 24h window (canonical definition:
   `observedTypes.Count == 0`). Info severity, **not** Warning — low-volume publishers can
   legitimately go quiet for a day or more. The Suggestion explicitly acknowledges this (see
   ADR-9.3-004 enum minimalism below for why `Error` severity is absent).

3. **`VersionMismatch` (Warning)** — two or more versions of the same event-name stem are observed
   concurrently. Example: `MyApp.Claims.ClaimSubmittedV2` AND `MyApp.Claims.ClaimSubmittedV3` both
   seen — the stem `ClaimSubmitted` (from the terminal segment, NOT the full FQN) is reported at
   Warning with version counts in the Context.

4. **`ProjectionBindingMissing` (Warning)** — a `SourceToTenantMap` route exists for the selected
   tenant and an authoritative projection registry is available, but no runtime projection binding
   covers the normalized route/event key. Example: route `enterprise/claims -> acme` exists, events
   like `MyApp.Claims.ClaimSubmittedV2` are observed, and the authoritative registry returns no
   `acme/enterprise/claims/claimsubmitted` binding. The Context identifies the configured source,
   selected tenant, expected projection-binding key, and the remediation stays additive to the
   existing report shape.

Stem-extraction regex: `^(.+?)(V\d+)$` compiled with `MatchTimeout = 100ms` and `CultureInvariant`,
operating on the TERMINAL `.`-separated segment of the event type. Inputs over 256 chars are
skipped with log event 9141 (Risk #5 ReDoS defense).

### §11.3 Observation window + staleness semantics

The observation store is a Redis-backed 24h rolling window per
`(tenantId, aggregateType, eventType)`. Keys are `{tenantId}:eventstore:observed-aggregates` (set),
`{tenantId}:eventstore:observed:{aggregateType}` (sorted set scored by unix-ms), and
`{tenantId}:eventstore:observed-count:{aggregateType}` (hash). TTL on every write is 48h (2× the
24h window) — if a tenant stops emitting entirely, its keys self-expire with no external cleanup.

Two **independent** mechanisms keep the store bounded:

- **TTL (48h)** — self-cleanup of the Redis keys when no further writes refresh them.
- **Window (24h)** — cutoff applied at read time via `ZRANGEBYSCORE (now - 24h) → +∞`. Older
  observations still exist in Redis until TTL expires them; they're simply excluded from detector
  output.

**Why `StaleHandler` is Info, not Warning:** an event stream that sees one event per week is NOT
stale, but a 24h window calls it stale every Monday. Info severity prevents paging rules firing on
low-volume legitimate silence. Future work may add `--since 7d` to widen the window per-invocation
(tracked as deferred-work `Story-9.3-SinceFlagForLowVolume`).

**Boundary semantics (R3-10):** the 24h window is **inclusive-start, exclusive-end relative to
"now."** An observation at T is included in a read at (now) when `T >= now - 24h`. Two successive
snapshots at T+24h and T+24h+2ms can legitimately disagree about a single observation on the edge —
this is correct, not a bug.

### §11.4 Troubleshooting flows

**Scenario A: publisher changed topic.** Operator suddenly sees `StaleHandler` rows accumulating.
Check the publisher's topic configuration — if it changed from `events` to `events-v2` without a
subscription update, the routing-map still points at the old topic and the 24h window will
progressively surface the silence. Fix: update `EventStoreIntegration:Routing:Topic` via
appsettings or env-var and restart the server.

**Scenario B: new event version rolled out without subscription update.** Operator sees
`VersionMismatch` rows with two versions and non-trivial counts on both. This is the expected
signal of an in-progress migration. Action: either (a) ensure the consumer is schema-tolerant of
both versions, OR (b) after the publisher has fully migrated and V2 traffic has dropped to zero,
the mismatch self-resolves.

**Scenario C: route configured, projection unbound.** Operator sees `ProjectionBindingMissing`.
Action: register an authoritative projection binding for the same tenant/source/event key, or update
`EventStoreIntegration:Routing:SourceToTenantMap` if the route no longer maps to a runtime-bound
projection. A matching binding proves declared route-to-binding coverage only. It does not prove the
projection is live, healthy, caught up, or consuming successfully.

**Scenario D: registry absent or unknown.** The detector preserves the existing mismatch output and
does not emit `ProjectionBindingMissing` when the provider reports `Unknown`, `NonAuthoritative`, or
`Unavailable`. This is the default posture for deployments that have not opted into authoritative
projection binding metadata.

**Scenario E: binding belongs to another tenant.** A binding for tenant `other` never satisfies a
route for tenant `acme`. The warning is still scoped to the selected tenant and expected key, and
does not enumerate the other tenant's projection inventory or expose projection implementation
details, endpoints, credentials, or DI internals.

### §11.4.1 Projection binding registry contract

Story 16.1 adds a small repository-owned projection registry contract in
`Hexalith.Memories.EventStore`: `IProjectionBindingProvider`, `ProjectionBindingSnapshot`,
`ProjectionBinding`, and `ProjectionBindingRegistryAuthority`.

The default provider returns:

```json
{
  "tenantId": "acme",
  "authority": "unknown",
  "bindings": []
}
```

Hosts that can prove runtime-bound projection consumers may replace the provider with an
authoritative implementation. Adopter-facing shape:

```json
{
  "tenantId": "acme",
  "authority": "authoritative",
  "bindings": [
    {
      "tenantId": "acme",
      "sourcePrefix": "enterprise/claims",
      "aggregateType": "Claims",
      "projectionName": "ClaimsReadModel",
      "projectionType": "Acme.ClaimsProjection",
      "supportedEventTypePatterns": ["ClaimSubmitted*"]
    }
  ]
}
```

Canonical comparison key: tenant id + normalized route source prefix + normalized event pattern.
Normalization is deterministic:

- tenant ids, source prefixes, aggregate tokens, and event terminal segments compare
  case-insensitively by lower-casing with invariant culture;
- source prefixes trim whitespace, convert `\` and `.` to `/`, collapse repeated `/`, and trim
  leading or trailing `/`. As a result, dot-style routes (`acme.events`) and slash-style bindings
  (`acme/events`) canonicalize to the same source key;
- aggregate tokens are derived from the final `/` segment and then final `.` segment of the route;
- event names compare on the terminal `.` segment and strip a trailing `V<digits>` version suffix.
  The strip is applied on both the binding pattern and the observed event side, so
  `["ClaimSubmittedV2"]` covers V2, V3, V99, etc. Version drift is reported separately by the
  `VersionMismatch` diagnostic when concurrent versions are observed; operators who need version-pinned
  binding semantics should request that follow-up via deferred work;
- `*` covers all events for the matched route/aggregate, and suffix `*` acts as a prefix pattern.
  Leading or embedded wildcards (`*Submitted`, `Claim*Submitted`) are not supported and will be
  treated as literal characters that never match — declare the wildcard at the end of the pattern;
- when no events have yet been observed for a route's aggregate, the expectation event key collapses
  to `*` and any matching binding satisfies coverage. Once real events are observed, the expectation
  becomes specific; this can produce a behavioral cliff where a route covered by an over-broad
  binding starts emitting `ProjectionBindingMissing` warnings as events arrive — declare narrow
  patterns to avoid the cliff;
- duplicate configured keys are reported once in deterministic order.

When a binding declares both `SourcePrefix` and `AggregateType`, either field's match satisfies
coverage (OR semantics). This is intentional: hosts may declare bindings using whichever field
maps cleanest to their projection registry. The dot-to-slash source normalization above closes the
most common false-cover case (notation difference). If a binding declares only one of the two
fields, that field must match the route or aggregate token respectively.

Provider failure posture (Authority semantics):

- `Unknown` — no authoritative provider available; never emit `ProjectionBindingMissing` warnings.
- `NonAuthoritative` — advisory metadata only; never emit `ProjectionBindingMissing` warnings.
- `Authoritative` — the snapshot is trusted to enumerate every runtime-bound projection for the
  tenant; missing bindings produce `ProjectionBindingMissing` warnings.
- `Unavailable` — the provider attempted a snapshot but cannot guarantee completeness; never emit
  warnings.

If the provider throws (any exception not tied to cancellation), the detector logs event 9150
(`Projection binding provider failed`) at Warning level with the exception type, skips the
projection-binding cross-check for that report, and preserves all other handler-mismatch diagnostics.
If the provider returns a snapshot whose `TenantId` does not equal the requested tenant, the detector
logs event 9151 and skips the cross-check. If the provider returns an `Authoritative` snapshot whose
`Bindings` collection is null, the detector logs event 9152 and skips the cross-check (treated as
Unavailable). Cancellation always re-throws regardless of the wrapping exception type.

`DiscoveryResult.Projections` in `Hexalith.EventStore` remains reference material, not the default
source of truth. It describes discovered projection types but does not by itself prove tenant-scoped
runtime binding authority in `Hexalith.Memories`; a host may adapt it only through the narrow
`IProjectionBindingProvider` contract when it can supply the required authority posture.

### §11.5 Telemetry substrate separation (ADR-9.3-002)

Story 9.3's `IObservedEventTypeStore` is a SEPARATE interface from Story 7.5's in-process
`RollingCounterStore`. Future contributors MUST NOT extend the 5m/5-slot ring to cover handler
observation — the two stores have different invariants (5m in-process, MeterListener fast path vs
24h Redis-backed, rare operator-triggered reads) and coupling them binds future changes to both.

### §11.6 Operator kill switch (ADR-9.3-001 behavioural clause)

Set `EventStoreIntegration:Observation:Enabled = false` in `appsettings.json` to disable observation
writes at runtime. The setting is read through `IOptionsMonitor<EventStoreObservationOptions>` —
the hot path picks up the change on the next event. Log event 9143 is emitted once at startup and
on every transition. Use this as an escape hatch if Redis observation writes begin degrading
ingestion p99 during an incident; durable audit coverage remains via `AccessTelemetryLog`.

### §11.7 Experimental surface (ADR-9.3-003)

Both endpoints expose the `X-Memories-API-Experimental: HXL002` response header on every 2xx
response (AC #24). SDK callers see the compile-time `[Experimental("HXL002")]` attribute from
`Hexalith.Memories.Client.Rest`; raw-HTTP callers (`curl`, Postman, bespoke integrations) use the
header as the runtime signal that this surface may change without a major-version bump. When 9.x+
graduates `HXL002` to stable, the header is removed in the same release.

### §11.8 Enum minimalism (ADR-9.3-004)

`HandlerSubscriptionStatus` is 3-state (`Active/Unknown/Disabled`) — `Paused` is deliberately
absent because Hexalith has no pause semantics. `HandlerMismatchSeverity` is 2-valued
(`Info/Warning`) — `Error` is deliberately absent because no mismatch is action-blocking at the
ingestion level, and `Error` severity would be interpreted by downstream paging rules as
page-worthy (see Risk #2). Operator-facing enums pay a compounding cost per value; minimalism
preserves clarity.

### §11.9 Authentication/authorization

The handlers endpoints are now covered by the Server JWT bearer fallback authorization policy added
in Story 20.1. Tenant-scoped handler mismatch inspection is also covered by the Story 20.2 tenant
authorization guard. The Dapr pub/sub discovery and delivery paths (`/dapr/subscribe` and
`POST /events/ingest`) remain explicit anonymous infrastructure exceptions so the sidecar can
discover subscriptions and deliver CloudEvents; publisher trust is still controlled by Dapr
component access, topic ACLs, and deployment topology rather than by the application bearer policy.
