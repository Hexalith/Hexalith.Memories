# Sprint Change Proposal — Migrate Embedding Provider to Self-Hosted Ollama (Keycloak-Protected)

**Date:** 2026-04-29
**Author:** Jerome (with BMad correct-course workflow)
**Status:** APPROVED 2026-04-29 — Epic 13 added to `epics.md` and `sprint-status.yaml`. Stories 13.1–13.7 are the work carrier; AC amendments to Stories 1.4 / 1.7 / 5.1 / 5.5 are documented here and applied at the planning-artifact layer (those stories remain `done`).
**Scope classification:** **MAJOR** — touches Architecture, PRD, multiple Epics, infra, and requires a vector-store reindex.

---

## 1. Issue Summary

The current MVP locks the embedding pipeline to **Google Generative Language API only** (`generativelanguage.googleapis.com/v1beta/models/{model}:embedContent`). Provider validation (`EmbeddingProviderDefaults.Validate`) explicitly rejects any provider other than `"google"`, and authentication is a static API key (`x-goog-api-key`) resolved via DAPR secret store per tenant.

The operator has a self-hosted **Ollama** instance available with the model **`qwen3-embedding:4b`** (Q4_K_M, 2,5 GB, 2560 dimensions) and wants to:

1. Switch the embedding provider to Ollama (cost, sovereignty, latency control).
2. Expose Ollama via the public route **`https://llm.tache.ai`** (DNS already configured → `82.67.127.189`; wildcard TLS `*.tache.ai` already provisioned in K8s as `tache-ai-tls`).
3. Protect the public route via the existing **Keycloak** instance (`auth.tache.ai`) — no native auth on Ollama itself.
4. Use **one consistent path** for both local (server-side) and external callers — no localhost fallback in the application code.

**Trigger context:** decision taken during operational architecture review on 2026-04-29 after Epics 1–6 were completed. Embedding code paths (Stories 1.4 / 1.7 / 5.1 / 5.5) were finalized against the Google-only assumption documented in PRD §"Embedding Provider Configuration" and architecture decision D4 ("Google embedding only in MVP").

---

## 2. Impact Analysis

### 2.1 Epic Impact

| Epic | Status | Impact |
|---|---|---|
| Epic 1 — Ingestion Pipeline | ✅ completed | Stories 1.4 (Embedding Generation) and 1.7 (Provider Configuration) require **AC amendments**, not code rollback. The `IEmbeddingProvider` abstraction declared in Story 1.7 was preserved — extension is in scope. |
| Epic 5 — Tenant Provisioning | ✅ completed | Stories 5.1 (Provisioning Workflow) and 5.5 (Configuration & Listing) require AC amendments to reference the Ollama provider option and the new auth secret shape. |
| Epic 6 — Operations & Resilience | ✅ completed | No story-level changes; runbook addendum required. |
| **Epic 13 (NEW)** — Embedding Provider Pluggability + Vector Migration | ➕ new | Adds Ollama provider implementation, OIDC client, dimension migration tool. |

### 2.2 Story Impact (concrete amendments)

| Story | File | Lines | Type |
|---|---|---|---|
| Story 1.4 — Embedding Generation | `epics.md` | 467, 482-485 | Amend ACs: replace "Google text-embedding-004 / 768 dim" with "configured provider (`google` or `ollama`) and dimensions per `TenantEmbeddingConfig`"; replace "x-goog-api-key" wording with "provider-specific auth (API key for Google, OIDC Bearer for Ollama)". |
| Story 1.7 — Provider Configuration | `epics.md` | 570, 578-581 | Amend ACs: provider enum is now `google`/`ollama`; the new field `BaseUrl` is required for Ollama; the new field `AuthMode` (`apiKey` \| `oidcClientCredentials`) is mandatory. |
| Story 5.1 — Tenant Provisioning | `epics.md` | ~1080 | Add AC: "If provider = `ollama`, the provisioning workflow stores `BaseUrl`, `AuthMode`, and the OIDC client_secret reference in DAPR Secrets store, and creates the Redis Vector index with the configured `dimensions` (e.g., 2560 for `qwen3-embedding:4b`)." |
| Story 5.5 — Tenant Configuration & Listing | `epics.md` | ~1200 | Add AC: configuration view masks the OIDC client_secret value but exposes the `ApiSecretKeyName` reference, the `BaseUrl`, the `AuthMode`, and the `Provider:Model:Dimensions` triple. |

### 2.3 Artifact Conflicts

| Artifact | File | Section / Line | Required change |
|---|---|---|---|
| PRD — Supported Providers | `prd.md` | 675-682 | Replace MVP table to list `ollama` as a first-class supported provider and qualify `google`/`openai`/`mistral` as optional/cloud. Add column "Auth Mode" with values `api-key` or `oidc-client-credentials`. |
| PRD — Configuration per tenant | `prd.md` | 685-691 | Add rows: `baseUrl` (required for ollama, derived for cloud providers), `authMode` (api-key or oidc-client-credentials), `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`. |
| PRD — Critical constraints | `prd.md` | 694 | Already calls out reindex requirement on provider switch — no change needed, just confirms migration plan. |
| PRD — Async Ingestion Pipeline | `prd.md` | 697-715 | Insert sub-bullet under "embedding" stage: "for OIDC-authenticated providers, an in-process token cache refreshes Bearer tokens 30 s before expiry; failures fall back to the workflow retry policy". |
| Architecture — Security Architecture | `architecture.md` | 196 | Generalize "DAPR Secrets scoping for embedding keys" to cover both static API keys and OIDC client_secret values. |
| Architecture — Trust Boundary | `architecture.md` | 202 | No change in trust model; the Memories Server remains the only consumer of OIDC client_secret. |
| Architecture — Provider abstraction | `architecture.md` | 269, 375, 550 | Update D4 from "Google embedding only in MVP" → "Multi-provider from start; Ollama (self-hosted, OIDC-protected) is the MVP default; Google retained as opt-in cloud provider". |
| Architecture — `EmbeddingProvider` field format | `architecture.md` | 114 | Confirm format `{provider}:{model}` works as-is (e.g., `ollama:qwen3-embedding:4b`). |
| Architecture — Index naming | `architecture.md` | 519 | Already supports `{tenantId}:{model-version}:semantic` — required for live coexistence during migration (no design change, just usage). |
| Architecture — DAPR Conversation API (D26) | `architecture.md` | 567 | Out of scope — the LLM chat path (DAPR Conversation API for `GenerateNaturalLanguageDescriptionActivity`) is unchanged. Document reaffirms the separation. |

### 2.4 Technical/Code Impact

**Files to modify (Memories Server):**

- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` — register `OllamaProviderName = "ollama"`; update `Validate()` to accept it; expose default `Dimensions = 2560`, `Model = "qwen3-embedding:4b"`, `BaseUrl = "https://llm.tache.ai"`.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` — split `BuildEndpointUrl()` and `SendEmbeddingRequestAsync()` per provider:
  - Google: existing path (kept as opt-in).
  - Ollama: `POST {BaseUrl}/api/embed` (Ollama-native format: `{ "model": "qwen3-embedding:4b", "input": "..." }` → `{ "embeddings": [[...]] }`).
  - Replace `x-goog-api-key` injection with conditional `Authorization: Bearer <jwt>` for Ollama, where the JWT is provided by a new `IOidcTokenProvider`.
- **NEW** `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` — implements client_credentials grant against Keycloak, in-memory cache (expires_in − 30 s), 1 token per `(TokenEndpoint, ClientId)` tuple, thread-safe, retry-on-401 with cache invalidation.
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` — add **non-breaking** optional fields:
  - `string? BaseUrl` (required for ollama, ignored for google).
  - `string AuthMode` = `"api-key"` (default) or `"oidc-client-credentials"`.
  - `string? OidcTokenEndpoint`, `string? OidcClientId`, `string? OidcScope` — required if `AuthMode = "oidc-client-credentials"`.
  - `ApiSecretKeyName` semantics extended: for OIDC mode, holds the **client_secret** key.
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` — surface the new fields in `GetEmbeddingConfigAsync()`.
- `src/Hexalith.Memories.Server/Program.cs` — register `IOidcTokenProvider` (HttpClient with Polly retry); register a typed `HttpClient` for the embedding endpoint with sensible timeouts (≥10 s for cold-start tolerance).
- `src/Hexalith.Memories.Server/appsettings.json` — add `Embedding:Ollama` defaults (Dimensions=2560, Model=qwen3-embedding:4b, BaseUrl=https://llm.tache.ai, AuthMode=oidc-client-credentials, TokenEndpoint=https://auth.tache.ai/realms/tache/protocol/openid-connect/token, ClientId=memories-embedding, Scope=openid).
- `src/Hexalith.Memories.AppHost/Program.cs` — propagate the OIDC config env vars (parallel to the existing `PropagateJwtBearerAuthenticationEnvironment`).

**Tests to add:**
- `OidcTokenProviderTests` — cache hit/miss, refresh-before-expiry, 401 retry.
- `EmbeddingClientTests` — Ollama provider request/response shape, OIDC injection, fallback on token revocation.
- Integration test `OllamaEmbeddingEndToEnd` (gated by env flag) — full ingest using a mocked Ollama-compatible HTTP endpoint.

**Infrastructure prerequisites (already provisioned by the operator before code work begins):**
- A self-hosted Ollama instance reachable at `https://llm.tache.ai` serving `qwen3-embedding:4b` (2560-dim).
- A Keycloak confidential client (`memories-embedding`) in realm `tache` with `serviceAccountsEnabled` and an audience mapper adding `llm.tache.ai` to access tokens.
- DAPR Secrets store entry `memories-embedding-client-secret` holding the client secret.

**Operational steps (out of code):**
- Place the client_secret in DAPR Secrets store under key `memories-embedding-client-secret`.

### 2.5 Data / Vector Store Impact

`qwen3-embedding:4b` outputs **2560-dimension** vectors; Google `text-embedding-004` outputs **768**. Redis Vector Search index schemas are immutable on creation. Two paths:

- **Path A — Wipe & reindex (recommended given current data volume is "test-data" per repo).** Drop `{tenantId}:semantic` indexes, recreate with `dim 2560`, replay ingestion for each tenant. Simplest, smallest blast radius, cleanest end state.
- **Path B — Concurrent versioned indexes (`{tenantId}:google-768:semantic` and `{tenantId}:ollama-2560:semantic`)** — already supported by the architecture (line 519). Allows live coexistence during a migration window. More code (read-side fan-out, write-side dual-write toggle, cutover script). Recommended only if production-grade tenants exist.

**Default proposal: Path A**, with Path B kept as a documented option per tenant (operator-controlled).

---

## 3. Recommended Approach

**Selected path: Direct Adjustment (Option 1 from the change-management checklist), with one NEW code epic.**

Rationale:
- The `IEmbeddingProvider`-style abstraction (Story 1.7 AC) and the versioned index naming scheme (Story 1.5 AC) were both *designed for this kind of extension*, so there is no rollback or fundamental replan needed.
- The existing JWT Bearer plumbing (MCP Server) is for **inbound** authentication and is unrelated to the **outbound** OIDC client we're adding — the two coexist cleanly.
- DAPR Secrets store already holds the per-tenant credentials (currently API keys, soon OIDC client_secrets) — same plumbing, different value.
- A self-hosted Ollama gateway exposed at `https://llm.tache.ai` and protected by Keycloak JWT auth is treated as an **infrastructure prerequisite** owned by the operator. The .NET code only consumes the gateway and does not touch the AppHost wiring.

**Trade-offs considered and rejected:**
- *Path B vector migration (concurrent versioned indexes):* deferred — adds complexity not justified by current data volume.

**Scope, effort, risk:**
- **Effort:** Medium. ~5–7 dev-days for the .NET changes + tests.
- **Risk:** Medium. Main risks: (a) Keycloak client_secret rotation procedure missing → ingestion stalls; (b) wildcard cert renewal not automated (no cert-manager) — known issue, out of scope for this change.
- **Timeline:** can fit in a single sprint provided the operator accepts a one-shot reindex window.

---

## 4. Detailed Change Proposals

### 4.1 PRD edits

**`prd.md` lines 675-682 (Supported Providers table)**

```
OLD:
| Provider | Model (default)        | Dimensions | Rate Limit (default) |
|----------|------------------------|------------|----------------------|
| Google   | text-embedding-004     | 768        | 1500 req/min         |
| OpenAI   | text-embedding-3-small | 1536       | 3000 req/min         |
| Mistral  | mistral-embed          | 1024       | Varies               |

NEW:
| Provider | Model (default)        | Dimensions | Auth Mode                | Rate Limit (default) |
|----------|------------------------|------------|--------------------------|----------------------|
| Ollama   | qwen3-embedding:4b     | 2560       | oidc-client-credentials  | self-hosted (no provider quota) |
| Google   | text-embedding-004     | 768        | api-key                  | 1500 req/min         |
| OpenAI   | text-embedding-3-small | 1536       | api-key                  | 3000 req/min         |
| Mistral  | mistral-embed          | 1024       | api-key                  | Varies               |

Default for new tenants: Ollama (self-hosted via https://llm.tache.ai, JWT-protected).
```

**`prd.md` lines 685-691 (Configuration per tenant)** — append rows:

```
| baseUrl              | Provider endpoint URL (required for ollama; derived for cloud)              | Tenant config              |
| authMode             | "api-key" | "oidc-client-credentials"                                       | Tenant config              |
| oidcTokenEndpoint    | OIDC token endpoint (required if authMode = oidc-client-credentials)        | Tenant config              |
| oidcClientId         | OIDC client_id                                                              | Tenant config              |
| oidcScope            | Optional OIDC scope                                                         | Tenant config              |
| apiSecretKeyName     | DAPR secret name holding either api-key or client_secret per authMode       | Tenant config              |
```

### 4.2 Epic / Story edits

**Story 1.4 (epics.md L467) — AC1**

```
OLD:
**Given** a tenant is configured with Google embedding provider (text-embedding-004, 768 dimensions)
**When** `GenerateEmbeddingActivity` receives extracted text content
**Then** it calls the Google embedding API and returns a 768-dimension vector

NEW:
**Given** a tenant is configured with an embedding provider (default: Ollama qwen3-embedding:4b, 2560 dimensions)
**When** `GenerateEmbeddingActivity` receives extracted text content
**Then** it calls the configured provider's embedding endpoint and returns a vector matching the configured dimensions
**And** the EmbeddingProvider field is populated as `{provider}:{model}` (e.g., `ollama:qwen3-embedding:4b`)
```

**Story 1.4 (epics.md L482-485) — AC4**

```
OLD:
**Given** the embedding API key is configured
**When** the system accesses it
**Then** it reads from DAPR Secrets API (deployed) or .NET User Secrets (local dev)
**And** the key is never stored in config files or environment variables

NEW:
**Given** the tenant's `authMode` is `api-key`
**Then** the API key is read from DAPR Secrets API (deployed) or User Secrets (local dev) and never stored in config or env

**Given** the tenant's `authMode` is `oidc-client-credentials`
**Then** the `client_secret` is read from DAPR Secrets API by name (`apiSecretKeyName`)
**And** the `IOidcTokenProvider` performs `client_credentials` grant against `oidcTokenEndpoint`
**And** the resulting Bearer JWT is cached in-memory until 30 s before expiry
**And** a 401/403 response invalidates the cache and triggers exactly one refresh+retry
**And** neither the `client_secret` nor any access_token is ever written to logs at Info or above
```

**Story 1.7 (epics.md L570) — AC1**

```
OLD:
**Then** I can specify: provider (google), model (text-embedding-004), dimensions (768), rateLimitPerMinute (1500)

NEW:
**Then** I can specify: provider (`ollama` | `google` | future: `openai` | `mistral`), model, dimensions, rateLimitPerMinute, authMode, baseUrl (required for ollama), and OIDC fields (required if authMode = oidc-client-credentials)
```

**Story 1.7 (epics.md L578-581) — AC3**

```
OLD:
**Given** MVP supports Google only
**Then** the provider field accepts an enum/string that can be extended to openai, mistral, custom in future phases

NEW:
**Given** MVP supports `ollama` (default, self-hosted, OIDC-authenticated) and `google` (cloud, api-key)
**Then** the provider field accepts an enum/string that can be extended to openai, mistral, and future providers in later phases
**And** `EmbeddingProviderDefaults.Validate()` accepts both `"ollama"` and `"google"` as valid values
```

**Story 5.5 — append AC**

```
**Given** the tenant configuration is read via the listing endpoint
**When** the response is serialized
**Then** `apiSecretKeyName` is exposed (name only, never value)
**And** `oidcTokenEndpoint`, `oidcClientId`, `oidcScope`, `baseUrl`, `authMode`, `provider`, `model`, `dimensions` are exposed as plaintext config metadata
**And** the `client_secret` value itself is never exposed
```

### 4.3 Architecture edits

**`architecture.md` line 196 (Security Architecture)**

```
OLD:
- **DAPR Secrets scoping** — Configure DAPR secret scopes so only Memories Server app-id can access embedding keys.

NEW:
- **DAPR Secrets scoping** — Configure DAPR secret scopes so only Memories Server app-id can access embedding credentials, regardless of `authMode` (api-key for cloud providers, OIDC client_secret for self-hosted Ollama). MCP Server has no direct secret access. The OIDC token provider runs in-process within Memories Server only — issued Bearer JWTs are never persisted, never surfaced via APIs, never logged at Info+.
```

**`architecture.md` lines 375 + 550 (Decision D4)**

```
OLD:
| D4 | Google embedding only in MVP | Solo developer scope; IEmbeddingProvider abstraction makes additions trivial | PRD Deviations |

NEW:
| D4 | Multi-provider embedding from MVP — Ollama (self-hosted, OIDC) is default; Google (cloud, api-key) is opt-in | Sovereignty + cost control; existing IEmbeddingProvider abstraction supports both without refactor; OIDC client_credentials gives a single auth path for both local and external callers | PRD Deviations |
```

### 4.4 New stories

**Epic 13 — Embedding Provider Pluggability + Vector Migration**

To be executed via the BMAD developer workflow (`bmad-create-story` → `bmad-dev-story`).

- **Story 13.1** — Extend `EmbeddingProviderDefaults` to accept `ollama`. **AC:** unit tests pass; `Validate(config with provider=ollama)` no longer throws.
- **Story 13.2** — Implement `OidcTokenProvider` (client_credentials grant, cache, retry-on-401). **AC:** `OidcTokenProviderTests` cover cache hit/miss, refresh-before-expiry, 401-invalidate-and-retry, concurrency.
- **Story 13.3** — Extend `EmbeddingClient` to support Ollama via the **Ollama-native** request shape exposed by the upstream gateway. **AC:** for `provider=ollama`, request hits `{BaseUrl}/api/embed` with Bearer JWT and `{model, input}` body; response parsed from `embeddings[0]` (length 2560 for `qwen3-embedding:4b`).
- **Story 13.4** — Extend `TenantEmbeddingConfig` with new fields (non-breaking, optional). **AC:** existing Google tenants continue to work without re-provisioning; new Ollama tenants validate the OIDC fields.
- **Story 13.5** — Extend `TenantConfigurationActor` to surface and persist new fields. **AC:** state migration is non-destructive; existing actor state deserializes with new fields defaulted to null.
- **Story 13.6** — Vector migration tool: drop and recreate `{tenantId}:semantic` index with new dimensions, then replay ingestion for affected tenants. **AC:** dry-run mode lists affected tenants and content counts; live mode reports per-tenant progress; rollback toggle re-creates the previous index from `{tenantId}:google-768:semantic` if both have been kept.
- **Story 13.7** — Update integration tests + Aspire test fixtures + write the operator-facing deployment guide. **AC:** (a) test suite green against both `ollama` and `google` (existing fake) providers; (b) `docs/operations/embedding-providers.md` documents the gateway contract (Ollama-native HTTP API, Bearer JWT with audience claim, JWKS validation), provides a generic anonymized Envoy + Ollama stack example with placeholders (`{ISSUER}`, `{AUDIENCE}`, `{JWKS_URL}`, `{HOSTNAME}`), and lists every `TenantEmbeddingConfig` field operators must supply for each provider option (Google, self-hosted Ollama with OIDC, Ollama local without auth).

### 4.5 Configuration & Secrets

**Keycloak realm `tache`** (existing realm, reused):
- Client ID: `memories-embedding`
- Access Type: `confidential`
- Service Accounts Enabled: yes
- Standard / Direct / Implicit flows: disabled
- Audience mapper: `oidc-audience-mapper` adding `llm.tache.ai` to access tokens
- Access Token Lifespan: 600 s

**DAPR Secrets store entries** (operator):
- `memories-embedding-client-secret` → the client_secret string from Keycloak.

**`secrets.json` (local dev)** (gitignored, AppHost auto-creates):
```json
{
  "memories-embedding-client-secret": "<dev secret>",
  "Embedding": {
    "Ollama": {
      "Auth": {
        "TokenEndpoint": "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
        "ClientId": "memories-embedding",
        "Scope": "openid"
      }
    }
  }
}
```

---

## 5. Implementation Handoff

**Scope classification: MAJOR** — this proposal touches code (.NET), data (vector reindex), and multiple committed Epics' AC text.

| Stream | Owner | Deliverables |
|---|---|---|
| Code (Stories 13.1–13.7) | Developer agent (`bmad-agent-dev`) | `EmbeddingClient`, `EmbeddingProviderDefaults`, `OidcTokenProvider`, `TenantEmbeddingConfig`, `TenantConfigurationActor`, `appsettings.json`, AppHost env propagation, tests. |
| Data migration (Story 13.6) | Developer + Operator | `tools/MigrateEmbeddingDimensions/` console app or extend `Hexalith.Memories.Cli`, dry-run & live, runbook entry. |
| Documentation | Tech writer / Developer | Update `docs/dev/embedding-providers.md`, `architecture.md` D4, retrospective addenda for Epics 5/6 if needed. |

**Sequencing:**
1. Code (Epic 13) lands behind a per-tenant config flip — existing Google tenants are unaffected during build/test of the Ollama path.
2. Data migration (Story 13.6) executes per tenant, controlled rollout.
3. Default for new tenants flips to Ollama once Epic 13 is shipped.

**Success criteria — code (Epic 13):**
- Memories Server passes the existing test suite plus the new tests against both providers.
- One reference tenant has been migrated end-to-end (drop index, reingest, verify search recall) within the agreed downtime window.
- No `client_secret` or Bearer JWT appears in logs at Info+.

**Out of scope of this proposal (deferred):**
- cert-manager installation (still wildcard-managed).
- Multi-node / GPU-sharing.
- Replacing the DAPR Conversation API path (chat) — stays as today.
- Phase B coexistence with versioned semantic indexes — kept as documented option only.

---

## Approval

- [x] Operator approves scope and trade-offs (2026-04-29)
- [x] Operator confirms acceptance of one-shot reindex (Path A) — to be executed during Epic 13 (2026-04-29)
- [x] Realm name `tache`, client_id `memories-embedding`, audience `llm.tache.ai` confirmed (2026-04-29)

Remaining work hands off to:
- Developer agent (`bmad-agent-dev`) for Epic 13 implementation (`.NET` code, vector migration tool, tests)

**Acceptance trail:**
- 2026-04-29 — Epic 13 inserted into `_bmad-output/planning-artifacts/epics.md` after the "Decision Point: Beyond Epic 12" section, including all 7 stories (13.1–13.7) with acceptance criteria.
- 2026-04-29 — `_bmad-output/implementation-artifacts/sprint-status.yaml` updated: `epic-13` = `in-progress`, `13-1`..`13-7` = `backlog`, `epic-13-retrospective` = `optional`.
- 2026-04-29 — Story 13.1 file created at `_bmad-output/implementation-artifacts/13-1-extend-embedding-provider-defaults-to-accept-ollama.md` (status: `ready-for-dev`).
