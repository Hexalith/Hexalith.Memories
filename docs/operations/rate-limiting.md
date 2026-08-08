# Rate Limiting — Per-Tenant and Shared Provider Quotas

## Inbound HTTP request quotas (Story 20.5)

The Memories Server registers ASP.NET Core inbound request limiting with the
`InboundRateLimiting` configuration section:

| Setting | Default | Production default | Description |
|---------|---------|--------------------|-------------|
| `InboundRateLimiting:PermitLimit` | `120` | `600` | Fixed-window permits per partition. |
| `InboundRateLimiting:WindowSeconds` | `60` | `60` | Fixed-window duration. |
| `InboundRateLimiting:QueueLimit` | `0` | `0` | Rejections are immediate; requests are not queued. |

Route/query API traffic is partitioned by authenticated and authorized tenant context, not by raw
caller-supplied route/query values. Body-bound ingest routes use the same tenant quota store after
body binding and tenant authorization. Tenant creation uses an authenticated-principal partition
because the target tenant may not exist yet. Health and Dapr infrastructure endpoints remain aligned
with their explicit anonymous policy.

Rejected requests return HTTP `429` with `ErrorResponse.Code = RATE_LIMIT_EXCEEDED` and emit the
`memories.rate.limit.rejections` counter tagged only by `tenant_id` and `error_code`.

## Embedding-provider admission (Stories 6.2, 23.3, and 23.5)

Each tenant has an `EmbeddingRateLimiterActor` (Actor ID = tenant ID). It enforces
the `RateLimitPerMinute` ceiling from `TenantEmbeddingConfig` (Story 1.7 / 5.5).
Configuration validation rejects a non-positive `RateLimitPerMinute` before admission
can use it. The actor state is independent per tenant. This outbound provider-admission
path is separate from the ASP.NET inbound HTTP limiter above.

Embedding activities read tenant embedding configuration through a process-local,
tenant-keyed cache. `Ingestion:EmbeddingConfigCache:CacheTtlSeconds` defaults to **30**
and is clamped to **1-300 seconds**. Each process retains at most
`MaxCacheEntries` entries per config/fusion-weight cache (default **10,000**, clamped
to **100-1,000,000**). The embedding-config `PUT` invalidates the current process
before rereading the actor; another server process can retain its prior value until
that process's TTL expires. The cache stores the configuration contract, including
the provider secret *name*, but never the resolved secret value.

`GenerateEmbeddingActivity` makes one
`TryConsumeWithCeilingAsync(rateLimitPerMinute)` actor call before its single provider
call. `GenerateChunkEmbeddingsActivity` makes one admission call before each bounded
provider batch (up to `Ingestion:Chunking:MaxChunksPerBatch`, default **32**, chunks per
call). Each admission supplies the cached current ceiling; the actor updates that
ceiling, consumes one token when admitted, and persists the tenant-scoped state before
the provider call. A ceiling change is therefore applied by that process on its first
admission after configuration refresh or invalidation.

## Shared provider quota (Known Limitation)

If multiple tenants resolve `TenantEmbeddingConfig.ApiSecretKeyName` to the same
provider credential, their separate tenant actors do **not** coordinate the
provider-level quota. The effective provider quota is external configuration and must
be confirmed with that provider; this runbook does not assert a numeric provider
limit.

**Mitigation for MVP:** assign distinct `ApiSecretKeyName` values per tenant via the
operator secrets store. The per-tenant ceiling enforced by the actor is a *protection*,
not an *isolation*, when keys are shared.

A former Phase 3 sketch for a cross-tenant `SharedEmbeddingRateLimiterActor` is **not**
an approved current delivery commitment. Treat shared-key coordination as deferred
product work; do not expect that actor in the current tree.

## Provider 429 handling (Stories 6.2, 23.3)

On a provider HTTP 429 response, the embedding transport parses `Retry-After`
(seconds or HTTP-date per RFC 9110 §10.2.3). `GenerateEmbeddingActivity` and
`GenerateChunkEmbeddingsActivity` keep provider feedback activity-owned by invoking
`IEmbeddingRateLimiterActor.ReportRateLimitedAsync(retryAfterSeconds)`, then re-throw a
sanitized `EmbeddingRateLimitException` that carries only the effective retry-after
seconds. They report feedback only for an exception raised while a provider call is in
progress. A local admission denial happens before the provider call and does **not**
report a provider 429.

`IngestionWorkflow` handles provider 429s with a DAPR durable timer. It waits for the
effective Retry-After duration through `WorkflowContext.CreateTimer(...)`, then calls the
embedding activity again. This path applies to raw chunk embedding and event
natural-language embedding. Non-provider failures and local actor-budget denials continue
through the normal failure/retry/compensation paths.

The rate-limiter actor zero-floors the tenant budget and positions `WindowStart` so
the next consume refills at the intended provider retry-open instant, not one extra
minute later. During the closed window, `TryConsumeWithCeilingAsync` returns `false`
immediately, so no provider calls happen.

- Missing / malformed `Retry-After` → activity defaults to **30 s**.
- `Retry-After` values are clamped to `[1, 3600]` seconds at the HTTP boundary.
- The workflow permits at most **five** provider-rate-limit durable waits; a later 429
  fails through the existing failed-unit path. This counter is independent of the
  captured generic activity retry budget, so ordinary failures retain their configured
  retry/failure path.

## Per-tenant CPU gate (Story 6.2)

`PerTenantConcurrencyGate` caps concurrent `ExtractContentActivity` and
`FetchUrlActivity` invocations per tenant. Prevents a tenant's batch from monopolizing
the Memories Server extraction threadpool.

### Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `Ingestion:PerTenantExtractionConcurrency` | `4` | Max concurrent extraction/fetch activities per tenant. |
| `Ingestion:ExtractionGateAcquireTimeoutSeconds` | `300` | Max time a queued activity waits before `TimeoutException`. |

### Operator tuning heuristic

- **Baseline:** `min(4, Environment.ProcessorCount / 2)` per tenant. Default 4 fits a
  typical 8-core dev/CI box with ~2 tenants.
- **Raise** when: (a) the Aspire dashboard shows `ExtractionGateContended` (event 6205)
  firing >10 /min for a tenant with CPU headroom (<60 % process CPU), OR (b) a
  single-tenant deployment where starvation is not a concern.
- **Lower** when: (a) multi-tenant deployment with PDF-heavy batches saturates CPU
  (>85 % sustained), OR (b) embedding provider 429s correlate with extraction spikes.
- **Upper bound:** `Environment.ProcessorCount` — beyond that you're just paying
  context-switching cost.

### Horizontal scale-out

The gate is **process-local**. Horizontal scale-out (distributed semaphore via actor or
Redis lock) is Phase 2. A server process restart resets the gate; in-flight DAPR
Workflow history replays, and activities re-acquire the fresh gate naturally.

## Jitter (Story 6.2)

`GenerateEmbeddingActivity` adds uniform-random jitter in `[0, 500)` ms before a
**retry** provider call, not on the first attempt. Retry detection is process-local and
tracked for at most one hour, keyed by workflow instance/task execution identity.
`GenerateChunkEmbeddingsActivity` does not apply this jitter. DAPR 1.17.6
`WorkflowRetryPolicy` has no jitter parameter, so the single-call jitter lives in the
activity rather than the retry policy.

Jitter is NOT applied to `ExtractContentActivity` / `FetchUrlActivity`: extraction is
CPU-bound locally (no thundering-herd risk), fetch hits arbitrary URLs already spaced
by the workflow retry schedule.

## Metrics and logs

Structured log events (Story 6.2):

| Event ID | Level | Name | Fields |
|----------|-------|------|--------|
| 6201 | Warning | `RateLimitExceededLocally` | `tenantId` |
| 6202 | Warning | `ProviderRateLimitReceived` | `tenantId`, `retryAfterSeconds` |
| 6203 | Information | `RateLimitActorUpdated` | `tenantId`, `remaining`, `windowStart` |
| 6204 | Debug | `ExtractionGateAcquired` | `tenantId`, `availableCount` |
| 6205 | Information | `ExtractionGateContended` | `tenantId`, `queueDepth` |
| 6206 | Warning | `ExtractionGateTimeout` | `tenantId`, `timeoutSeconds` |

OpenTelemetry metric counters for provider/extraction throttling are Epic 8 (Observability &
System Health). Inbound HTTP request rejections use the Story 20.5
`memories.rate.limit.rejections` counter described above.

## Related ingestion guidance

- [Failure recovery and re-ingestion](./failure-recovery.md)
- [Directory ingestion](./directory-ingestion.md)
- [Ingestion workflow determinism (contributor)](../dev/ingestion-workflow-determinism.md)
