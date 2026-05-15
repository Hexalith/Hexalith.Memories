# Embedding Provider Operations

This guide covers the tenant embedding provider options, the Ollama gateway contract, Keycloak setup,
DAPR secret layout, and the Story 13.6 migration runbook.

## Provider Choice

| Option | Runtime status | Use when | Production posture |
|--------|----------------|----------|--------------------|
| Google API key | Runtime-supported | You want managed embeddings and existing 768-dimension defaults. | Supported through DAPR secret-store API key lookup. |
| Ollama OIDC | Runtime-supported | You operate a self-hosted Ollama-compatible gateway and want Memories Server to acquire a bearer token with client credentials. | Supported production path for Ollama in the current server runtime. |
| Ollama local/no-auth or upstream API key | **Config schema only — not yet runtime-accepted** | You run a local developer Ollama instance, or a trusted internal gateway already handles auth before traffic reaches Ollama. | The current `EmbeddingClient` accepts Ollama only with `oidc-client-credentials`. Tenant configuration will accept this shape, but ingestion will fail until a provider-specific API-key/no-auth runtime contract is committed. Use Ollama OIDC for any path that must succeed today. |

Changing provider, model, dimensions, or an Ollama `BaseUrl` is a breaking embedding change. Do not treat it
as a simple configuration edit for tenants with existing data; run the migration section below or explicitly
record that no prior embeddings exist.

## Operator Checkpoints

1. Gateway endpoint answers `POST {BASE_URL}/api/embed`.
2. Keycloak client can issue a client-credentials access token with audience `{AUDIENCE}`.
3. DAPR secret store contains `{SECRET_NAME}` and the sidecar configuration allows that secret name.
4. Tenant embedding configuration is written through the committed tenant configuration API.
5. Existing tenants are dry-run inventoried and either migrated or recorded as having no prior data.
6. Post-migration verification proves the tenant writes 2560-dimension Ollama vectors and hybrid search returns new data.

TLS certificates, wildcard DNS, cert-manager installation, GPU scheduling, Ollama capacity, and production
Keycloak hosting are operator-owned infrastructure. This repository supplies configuration contracts,
tests, and the migration tool only.

## Tenant Configuration Matrix

All secret values stay in DAPR or the platform secret manager. `ApiSecretKeyName` is only the secret-name
reference and is safe to return from configuration APIs.

| Field | Google API key | Ollama OIDC | Ollama local/no-auth or upstream API key (config schema only — see runtime note above) |
|-------|----------------|-------------|------------------------------------------|
| `Provider` | `google` | `ollama` | `ollama` |
| `Model` | `gemini-embedding-001` | `qwen3-embedding:4b` | `qwen3-embedding:4b` |
| `Dimensions` | `768`, `1536`, or `3072` for the default Google model | `2560` | `2560` for the committed model |
| `RateLimitPerMinute` | Up to the Google provider ceiling | Up to the Ollama operator ceiling | Set to local/gateway capacity |
| `ApiSecretKeyName` | DAPR secret containing the Google API key | DAPR secret containing the OIDC `client_secret` | DAPR secret reference if an upstream API key contract is used; otherwise currently not a server-runtime path |
| `ReindexRequired` | `true` when provider/model/dimensions change | `true` when provider/model/dimensions/base URL change | `true` when provider/model/dimensions/base URL change |
| `BaseUrl` | Empty or ignored by current Google dispatch | Gateway base URL, for example `https://{HOSTNAME}` | Local/gateway base URL on a trusted network |
| `AuthMode` | `api-key` | `oidc-client-credentials` | `api-key` in config shape only; not accepted by the current Ollama runtime dispatch |
| `OidcTokenEndpoint` | Empty | `{TOKEN_ENDPOINT}` | Empty |
| `OidcClientId` | Empty | `{CLIENT_ID}` | Empty |
| `OidcScope` | Empty | `openid` if the Keycloak audience/client-scope setup requires it | Empty |

Example tenant request shape:

```json
{
  "provider": "ollama",
  "model": "qwen3-embedding:4b",
  "dimensions": 2560,
  "rateLimitPerMinute": 6000,
  "apiSecretKeyName": "{SECRET_NAME}",
  "reindexRequired": true,
  "baseUrl": "https://{HOSTNAME}",
  "authMode": "oidc-client-credentials",
  "oidcTokenEndpoint": "{TOKEN_ENDPOINT}",
  "oidcClientId": "{CLIENT_ID}",
  "oidcScope": "openid"
}
```

## Ollama Gateway Contract

Memories Server joins the configured Ollama `BaseUrl` with `/api/embed`. The default local Ollama API is
served under `/api`; the gateway base should therefore be the gateway or Ollama origin, not a path ending
in `/api/embed`.

Request:

```http
POST /api/embed
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json
```

```json
{
  "model": "qwen3-embedding:4b",
  "input": "{TEXT_TO_EMBED}"
}
```

Response:

```json
{
  "model": "qwen3-embedding:4b",
  "embeddings": [[0.01, -0.02, 0.03]]
}
```

The gateway must validate bearer JWTs before forwarding to Ollama. Validate issuer `{ISSUER}`, audience
`{AUDIENCE}`, signature keys from `{JWKS_URL}`, expiry, and TLS. Reject tokens whose `aud` does not match
the gateway audience.

Generic Envoy-equivalent sketch:

```yaml
listeners:
  - name: https-ollama-gateway
    address: 0.0.0.0:443
    tls:
      certificate: "{TLS_CERTIFICATE_REF}"
    jwt_authn:
      issuer: "{ISSUER}"
      audiences: ["{AUDIENCE}"]
      jwks_uri: "{JWKS_URL}"
    routes:
      - match:
          path_prefix: "/api/"
        upstream:
          host: "{OLLAMA_BACKEND_HOST}"
          port: 11434
```

Use the equivalent controls for your ingress or service mesh when Envoy is not the chosen gateway.

## Keycloak Recipe

1. Create or select realm `{REALM}`.
2. Create confidential OIDC client `{CLIENT_ID}`.
3. Enable client authentication and service accounts.
4. Disable standard, direct access, and implicit browser flows unless another operator-owned use case requires them.
5. Configure the client secret in Keycloak and store the value only in the platform secret manager or DAPR secret store.
6. Use token endpoint `/realms/{REALM}/protocol/openid-connect/token`; the full configured value becomes `{TOKEN_ENDPOINT}`.
7. If the audience mapper is on an optional client scope, request `openid` or the operator-defined scope through `OidcScope`.
8. Add an audience mapper that writes `{AUDIENCE}` into the access-token `aud` claim. A hardcoded audience mapper is the clearest option when the gateway does not rely on Keycloak client-role audience resolution.
9. Set an access-token lifespan short enough for revocation response and long enough to avoid excessive token endpoint traffic. Start with minutes, not hours, then tune against observed gateway/token load.

The Memories token request uses `application/x-www-form-urlencoded` client credentials with
`grant_type=client_credentials`, `client_id={CLIENT_ID}`, the secret value resolved from `{SECRET_NAME}`,
and optional `scope`.

## Token Endpoint Transport Policy

Production OIDC token endpoints must use `https://`. Memories validates `OidcTokenEndpoint` before
tenant configuration persistence and before direct token acquisition, so a non-loopback `http://`
endpoint fails before any client credentials are sent.

Local development and deterministic fake-server tests may use HTTP only for these literal loopback
hosts:

- `http://localhost/...`
- `http://127.0.0.1/...`
- `http://[::1]/...`

Other HTTP token endpoints are rejected, including public hostnames, private IPv4 ranges, link-local
metadata hosts, Docker or internal aliases such as `host.docker.internal`, DNS aliases such as
`localtest.me`, and broader loopback literals such as `127.0.0.2`. The exception is intentionally
literal-host based; DNS names that resolve to loopback are not treated as local.

The "literal-host" check runs against `Uri.Host` after .NET URI canonicalization, not against the
raw input string. As a side-effect, alternative textual forms of the same loopback address are also
accepted: decimal IPv4 (`http://2130706433/...`), IPv4 with leading zeros (`http://127.0.0.001/...`),
and expanded or padded IPv6 (`http://[0:0:0:0:0:0:0:1]/...`, `http://[::0001]/...`) all canonicalize
to `127.0.0.1` or `[::1]` and are treated identically to the canonical forms. They remain real
loopback addresses; operators should still prefer the canonical `localhost`, `127.0.0.1`, or `[::1]`
forms because logs and audit records show the canonicalized value, not the originally configured
form. The acceptance of these alternative forms is pinned by tests so future refactors that move to
a stricter raw-string match would surface as a test break rather than silently rejecting an existing
local development setup.

Validation errors name the field or argument and the HTTPS/local-loopback rule, but they do not echo
the full token endpoint URL. This keeps realm paths, accidental credential-looking path segments,
query strings, fragments, embedded credentials, bearer-shaped text, and client secrets out of
operator-visible errors. Tenant configuration stores only secret names, token response previews are
redacted before truncation, bearer-shaped values are redacted, and raw secret values remain in DAPR or
the platform secret manager only.

## DAPR Secret Store

Local AppHost uses `secretstores.local.file` named `secretstore` and points at repo-root `secrets.json`.
That file is local/dev only.

```json
{
  "{SECRET_NAME}": "{SECRET_VALUE}"
}
```

If the DAPR configuration uses secret scopes, include each tenant secret name:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: memories-config
spec:
  secrets:
    scopes:
      - storeName: secretstore
        defaultAccess: deny
        allowedSecrets:
          - "{SECRET_NAME}"
```

Production deployments should bind `secretstore` to the platform secret manager. Secret values are never
stored in `TenantEmbeddingConfig`, never returned by tenant configuration APIs, and never logged.

## Migration Runbook

The committed tool is `tools/MigrateEmbeddingVectors`. It connects to Redis and the DAPR sidecar directly;
defaults are `MEMORIES_REDIS` or `localhost:6379`, and `DAPR_HTTP_ENDPOINT` or `http://localhost:3500`.
You can also pass `--redis` and `--dapr-http`.

Dry-run one tenant:

```powershell
dotnet run --project tools\MigrateEmbeddingVectors -- --dry-run --tenant {TENANT_ID} --format human --redis {REDIS_CONNECTION} --dapr-http {DAPR_HTTP_ENDPOINT}
```

Dry-run all tenants for automation:

```powershell
dotnet run --project tools\MigrateEmbeddingVectors -- --dry-run --format json --redis {REDIS_CONNECTION} --dapr-http {DAPR_HTTP_ENDPOINT}
```

Live Path A migration:

```powershell
dotnet run --project tools\MigrateEmbeddingVectors -- --live --tenant {TENANT_ID} --target-provider ollama --target-model qwen3-embedding:4b --target-dimensions 2560 --batch-size 100 --yes --redis {REDIS_CONNECTION} --dapr-http {DAPR_HTTP_ENDPOINT}
```

### Live Migration Coordination

Live migration uses a durable tenant-scoped migration marker. The cutover point begins after the
marker is written and before semantic indexes are dropped or tenant embedding configuration is changed.
While the marker is active, ingestion for that tenant may continue only when its provider, model, and
dimensions match the marker target. Stale in-flight work that still carries the old provider/model is
blocked at the raw and natural-language semantic write boundaries before Redis hash persistence.

The marker is tenant-scoped; unrelated tenants are not paused. Tenant-specific ingestion downtime is not
required for the committed policy, but operators should expect stale in-flight workflows for the migrating
tenant to retry or surface an automation-readable "blocked by active tenant migration marker" failure until
they run with the target configuration.

| State | Runtime behavior | Operator expectation |
|-------|------------------|----------------------|
| Marker active, new ingestion reads target config | Provider call and semantic writes proceed with target provider/model/dimensions. | Normal operation for the migrating tenant. |
| Marker active, stale ingestion has old config or old embedding result | Generation is blocked before the provider call when observed in time; raw/NL semantic writes are always blocked before Redis persistence. | Let workflow retry after target config is visible, or rerun ingestion after migration if retries are exhausted. |
| Live migration aborts, is cancelled, or records tenant/unit failures | Marker remains active and protective. | Fix the underlying failure and resume; do not manually clear the marker unless you have independently verified no mixed-provider vectors can be written. |
| Resume succeeds and migration finishes cleanly | Marker is stamped completed after raw and natural-language re-embedding finish without failures. | Run dry-run verification and canary ingestion. |

Resume after an interruption:

```powershell
dotnet run --project tools\MigrateEmbeddingVectors -- --live --tenant {TENANT_ID} --resume --yes --redis {REDIS_CONNECTION} --dapr-http {DAPR_HTTP_ENDPOINT}
```

Final verification:

```powershell
dotnet run --project tools\MigrateEmbeddingVectors -- --dry-run --tenant {TENANT_ID} --format json --redis {REDIS_CONNECTION} --dapr-http {DAPR_HTTP_ENDPOINT}
```

Expected human summary shape:

```text
{MESSAGE}
{TENANT_ID}: affected={BOOLEAN} current={PROVIDER}/{MODEL}/{DIMENSIONS} target=ollama/qwen3-embedding:4b/2560 syntactic={COUNT} raw={COUNT} nl={COUNT} raw processed/skipped/missing/failed={P}/{S}/{M}/{F} nl processed/skipped/missing/failed={P}/{S}/{M}/{F} manualFollowUp={BOOLEAN}
```

Expected progress shape for live human output:

```text
{TENANT_ID} {CONTENT_KIND} batch {BATCH_NUMBER}: processed={COUNT} skipped={COUNT} missing={COUNT} failed={COUNT} total={COUNT} percent={PERCENT} elapsed={DURATION}
```

Expected JSON output has camel-case fields from `EmbeddingMigrationResult`, including `message`,
`exitCode`, `tenants`, `failures`, per-tenant `currentConfig`, `targetConfig`, `counts`, `raw`,
`naturalLanguage`, and `manualFollowUpRequired`.

Rollback:

```powershell
dotnet run --project tools\MigrateEmbeddingVectors -- --rollback --tenant {TENANT_ID} --yes --redis {REDIS_CONNECTION} --dapr-http {DAPR_HTTP_ENDPOINT}
```

Rollback is fail-closed for Path A unless retained previous-version indexes exist. Path B coexistence or
restore is only available when retained previous-version indexes were intentionally kept by a later
deployment convention; this tool does not invent previous index contents after Path A has dropped active
indexes.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Wrong vector dimensions | Confirm tenant `Dimensions`, Redis index dimensions, and model all match `2560` for `qwen3-embedding:4b`; rerun dry-run. |
| Missing audience | Evaluate a Keycloak access token and confirm the access-token `aud` contains `{AUDIENCE}`. |
| Invalid client secret | Rotate the Keycloak secret, update `{SECRET_NAME}` in the secret manager, and restart the server/sidecar if the platform requires cache refresh. |
| Missing DAPR secret | Confirm the secret exists and is listed in DAPR `allowedSecrets` for `secretstore`. |
| 401/403 after token refresh | Check gateway issuer, audience, JWKS, clock skew, and token lifespan. |
| Empty hybrid search after migration | Verify migration failures, Redis raw/NL counts, tenant active status, and that a fresh canary ingestion can write/search one unit. |
| Unexpected rollback expectation | Confirm whether retained previous-version indexes exist. Path A alone cannot restore dropped active indexes. |

## References

- [Ollama generate embeddings API](https://docs.ollama.com/api/embed)
- [Keycloak server administration guide](https://www.keycloak.org/docs/latest/server_admin/)
