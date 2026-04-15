# Rate Limiting — Per-Tenant and Shared Provider Quotas

## Per-tenant ceilings (Story 6.2)

Each tenant has an `EmbeddingRateLimiterActor` (Actor ID = tenant ID). It enforces
the `RateLimitPerMinute` ceiling from `TenantEmbeddingConfig` (Story 1.7 / 5.5). The
actor is independent per tenant; one tenant cannot consume another's budget.

The ceiling is re-read from `TenantConfigurationActor.GetEmbeddingConfigAsync()` on
every embedding activity invocation, so operator updates via
`PATCH /api/tenants/{tenantId}/config` take effect on the next ingestion.

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

## Provider 429 handling (Story 6.2)

On a provider HTTP 429 response, `GenerateEmbeddingActivity` parses `Retry-After`
(seconds or HTTP-date per RFC 9110 §10.2.3), invokes
`IEmbeddingRateLimiterActor.ReportRateLimitedAsync(retryAfterSeconds)` which zero-floors
the tenant's budget and pushes `WindowStart` to `now + retryAfterSeconds`, then re-throws
`EmbeddingRateLimitException`. The DAPR Workflow retry policy (5 attempts, exponential
backoff, 5 min cap) handles the retry. During the Retry-After window, `TryConsumeAsync`
returns `false` immediately — no provider calls happen — so the retry cost is just
workflow scheduling overhead.

- Missing / malformed `Retry-After` → activity defaults to **30 s**.
- `Retry-After` values are clamped to `[1, 3600]` seconds at the HTTP boundary.
- `Retry-After > ~26 s`: the workflow exhausts its retry budget and the unit transitions
  to `Failed`. Story 6.3 adds re-ingestion UX for this case.

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

OpenTelemetry metric counters are Epic 8 (Observability & System Health).
