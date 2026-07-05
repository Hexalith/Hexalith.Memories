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
`memories.rate_limit.rejections` counter tagged only by `tenant_id` and `error_code`.

## Per-tenant ceilings (Story 6.2)

Each tenant has an `EmbeddingRateLimiterActor` (Actor ID = tenant ID). It enforces
the `RateLimitPerMinute` ceiling from `TenantEmbeddingConfig` (Story 1.7 / 5.5). The
actor is independent per tenant; one tenant cannot consume another's budget.

The ceiling is re-read from `TenantConfigurationActor.GetEmbeddingConfigAsync()` on
every embedding activity invocation, so operator updates via
`PUT /api/tenants/{tenantId}/embedding-config` take effect on the next ingestion.

## Shared provider quota (Known Limitation)

If multiple tenants share the same `TenantEmbeddingConfig.ApiSecretKeyName` (i.e., the
same provider API key), they share the provider-level rate limit. Google
`text-embedding-004` free tier, for example, is 1500 req/min TOTAL — not per tenant.

**Mitigation for MVP:** assign distinct `ApiSecretKeyName` values per tenant via the
operator secrets store. The per-tenant ceiling enforced by the actor is a *protection*,
not an *isolation*, when keys are shared.

**Phase 3 roadmap:** a cross-tenant `SharedEmbeddingRateLimiterActor` will coordinate
the shared-key quota across tenants (see architecture §D41 "Shared embedding rate
limiter").

## Provider 429 handling (Stories 6.2, 23.3)

On a provider HTTP 429 response, the embedding transport parses `Retry-After`
(seconds or HTTP-date per RFC 9110 §10.2.3). `GenerateEmbeddingActivity` and
`GenerateChunkEmbeddingsActivity` keep provider feedback activity-owned by invoking
`IEmbeddingRateLimiterActor.ReportRateLimitedAsync(retryAfterSeconds)`, then re-throw a
sanitized `EmbeddingRateLimitException` that carries only the effective retry-after
seconds.

`IngestionWorkflow` handles provider 429s with a DAPR durable timer. It waits for the
effective Retry-After duration through `WorkflowContext.CreateTimer(...)`, then calls the
embedding activity again. This path applies to raw chunk embedding and event
natural-language embedding. Non-provider failures and local actor-budget denials continue
through the normal failure/retry/compensation paths.

The rate-limiter actor zero-floors the tenant budget and positions `WindowStart` so
`TryConsumeAsync` refills at the intended provider retry-open instant, not one extra
minute later. During the closed window, `TryConsumeAsync` returns `false` immediately,
so no provider calls happen.

- Missing / malformed `Retry-After` → activity defaults to **30 s**.
- `Retry-After` values are clamped to `[1, 3600]` seconds at the HTTP boundary.
- The workflow durable retry loop is bounded and deterministic; repeated provider 429s
  eventually fail through the existing failed-unit path, while transient 429s recover
  without exhausting the short generic activity retry budget.

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

`GenerateEmbeddingActivity` adds uniform-random jitter in `[0, 500)` ms BEFORE the
provider call to desynchronize retries after a provider outage (thundering-herd
mitigation — NFR22). DAPR 1.17.6 `WorkflowRetryPolicy` has no jitter parameter, so
jitter lives in the activity rather than the retry policy.

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
`memories.rate_limit.rejections` counter described above.
