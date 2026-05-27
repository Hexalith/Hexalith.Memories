# Embedding Providers

Provider runtime behavior is implemented around `TenantEmbeddingConfig`, `EmbeddingProviderDefaults`,
`EmbeddingClient`, and the tenant configuration API. Operators should use
[Embedding Provider Operations](../operations/embedding-providers.md) for gateway, Keycloak, DAPR
secret-store, and migration runbook details.

## Provider Matrix

| Provider path | Current use |
|---------------|-------------|
| Google API key | Default managed-provider path and legacy fake integration-test path. |
| Ollama OIDC | Committed self-hosted Ollama path. Uses `POST /api/embed`, OIDC client credentials, DAPR secret lookup, and 2560-dimension `qwen3-embedding:4b` vectors. |
| Ollama local/no-auth | Local or trusted-network option only. The config model can describe it, but current server dispatch accepts Ollama only with `oidc-client-credentials`. |

## Test Surfaces

- `AspireIngestionPipelineFixture` defaults to `EmbeddingProviderTestMode.GoogleFake` and keeps
  `Memories__Testing__UseFakeEmbedding=true`.
- `EmbeddingProviderTestMode.OllamaOidcFake` disables fake embeddings, writes a local DAPR secret-store
  entry, and overrides the test DAPR config so the fake OIDC secret name is allowed.
- `OllamaOidcFakeServer` provides the deterministic `POST /api/embed` endpoint and Keycloak-style
  client-credentials token endpoint.
- `OllamaEmbeddingEndToEndTests` provisions/configures an Ollama tenant through the API, ingests one
  unit, asserts 2560-dimension provider metadata, and verifies hybrid search.

## Useful Commands

```powershell
dotnet test tests\Hexalith.Memories.IntegrationTests\Hexalith.Memories.IntegrationTests.csproj --filter "Story13_7" --no-restore
```

```powershell
dotnet test tests\Hexalith.Memories.IntegrationTests\Hexalith.Memories.IntegrationTests.csproj --filter "FullyQualifiedName~OllamaEmbeddingEndToEndTests" --no-restore
```

```powershell
dotnet build Hexalith.Memories.slnx --no-restore
```
