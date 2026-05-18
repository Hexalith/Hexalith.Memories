# Story 1.4: Embedding Generation

Status: done

## Story

As a developer,
I want the system to generate vector embeddings for extracted content using Google text-embedding-004,
So that memory units can be searched by semantic similarity.

## Acceptance Criteria

1. **Given** a tenant is configured with Google embedding provider (text-embedding-004, 768 dimensions)
   **When** `GenerateEmbeddingActivity` receives extracted text content
   **Then** it calls the Google embedding API and returns a 768-dimension vector
   **And** the EmbeddingProvider and EmbeddingDimensions fields are populated on the memory unit

2. **Given** an `EmbeddingRateLimiterActor` exists for the tenant (actor ID = tenant ID)
   **When** embedding generation is requested
   **Then** the actor checks the rate budget before proceeding
   **And** if the budget is exhausted, the activity waits until the rate window resets

3. **Given** the embedding API returns a 429 (rate limited) response
   **When** the activity handles the error
   **Then** DAPR Workflow retry policy triggers exponential backoff
   **And** no data loss occurs

4. **Given** the embedding API key is configured
   **When** the system accesses it
   **Then** it reads from the DAPR Secrets API using the configured secret store (including the local file secret store in local development)
   **And** the key is never stored in config files or environment variables

## Tasks / Subtasks

- [x] Task 1: Create embedding input/output types (AC: #1)
    - [x] 1.1 Create `Server/Activities/Ingestion/EmbeddingInput.cs` — sealed record: TenantId (string), ContentText (string). MVP hardcodes Google text-embedding-004 in `EmbeddingClient` — do NOT add ProviderConfig or Dimensions fields here (YAGNI until Story 1.7 adds provider configuration).
    - [x] 1.2 Create `Server/Activities/Ingestion/EmbeddingResult.cs` — sealed record: Vector (float[]), Provider (string — always `"google:text-embedding-004"` in MVP), Dimensions (int — always 768 in MVP)

- [x] Task 2: Create EmbeddingClient (AC: #1, #4)
    - [x] 2.1 Create `Server/Ingestion/EmbeddingClient.cs` — concrete class (Decision D9: no premature interface). This is a **typed HttpClient**: constructor MUST accept `HttpClient` as first parameter (injected by `IHttpClientFactory` via `AddHttpClient<EmbeddingClient>()`). Also takes `DaprClient` for secret retrieval.
    - [x] 2.2 Implement `GenerateAsync(string text, string tenantId, CancellationToken ct)` → returns `float[]` (768-dimension vector). Validate response vector length: if `values.Length != 768`, throw `EmbeddingApiException` with message `"Expected 768 dimensions but received {actual}. Google API may have returned truncated or malformed response."`. This prevents silent corruption downstream in vector indexing.
    - [x] 2.3 API call: `POST https://generativelanguage.googleapis.com/v1/models/text-embedding-004:embedContent` with `x-goog-api-key` header. Request body: `{"content":{"parts":[{"text":"..."}]}}`. Response: `{"embedding":{"values":[...]}}`
    - [x] 2.4 Secret retrieval: call `DaprClient.GetSecretAsync("secretstore", "google-embedding-api-key")` to get the API key. Use **lazy-init caching** (`_apiKey ??= await FetchKeyAsync()`) — the typed HttpClient is transient (new instance per request), so without caching the secret would be fetched on every embedding call. Lazy-init ensures one fetch per instance. Acceptable for MVP per Security Architecture.
    - [x] 2.5 Input validation at boundary: throw `ArgumentException` if `text` is null/empty. Do NOT validate `tenantId` here — domain validation is upstream in `IngestionValidator` (Decision D12).
    - [x] 2.6 On HTTP 429 response: throw a typed `EmbeddingRateLimitException` (create this class) so the DAPR Workflow retry policy can catch and backoff.
    - [x] 2.7 On other HTTP errors: throw `EmbeddingApiException` (create this class) with status code, response body, and tenant context for debugging.
    - [x] 2.8 On DAPR secret store failure (sidecar down, misconfigured store, wrong key name): throw `EmbeddingApiException` with a clear message and actionable suggestion (e.g., `"Failed to retrieve embedding API key from DAPR secret store 'secretstore'. Ensure the DAPR sidecar is running and deploy/dapr/components/secretstore.yaml is configured."`). Do NOT let raw `DaprException` propagate undecorated.
    - [x] 2.9 On malformed JSON response from Google API (missing `embedding.values` path): throw `EmbeddingApiException` with the raw response body for debugging. Use `JsonDocument.Parse` with try/catch — do NOT assume response shape.
    - [x] 2.10 Configure HTTP timeout: set `HttpClient.Timeout` to 30 seconds in the `AddHttpClient<EmbeddingClient>()` registration (Task 6.3). Google embedding API typically responds in <2 seconds; 30s is a generous ceiling that prevents hanging requests from blocking the ingestion pipeline indefinitely.

- [x] Task 3: Create EmbeddingRateLimiterActor (AC: #2)
    - [x] 3.1 Create `Server/Actors/IEmbeddingRateLimiterActor.cs` — actor interface inheriting `IActor`. Methods: `Task<bool> TryConsumeAsync()`, `Task ResetAsync()`, `Task<RateLimitState> GetStateAsync()`, `Task SetCeilingAsync(int ceiling)`. Note: `TryConsumeAsync` takes NO parameter — MVP is single-document only (NFR5), always consumes 1 token per call.
    - [x] 3.2 Create `Server/Actors/RateLimitState.cs` — sealed record: Remaining (int), WindowStart (DateTime), CeilingPerMinute (int)
    - [x] 3.3 Create `Server/Actors/RateLimiterLogic.cs` — plain class containing the rate limiting business logic (window check, budget decrement, reset). Constructor accepts `TimeProvider` for deterministic time control in tests. Use `_timeProvider.GetUtcNow()` (returns `DateTimeOffset`) for all time reads — never `DateTime.UtcNow`. This is the testable unit — the actor delegates to it.

    ```csharp
    // Constructor pattern:
    public RateLimiterLogic(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    // In production (actor): new RateLimiterLogic(TimeProvider.System)
    // In tests: new RateLimiterLogic(fakeTimeProvider)
    ```

    - [x] 3.4 Create `Server/Actors/EmbeddingRateLimiterActor.cs` — `internal class` inheriting `Actor, IEmbeddingRateLimiterActor`. Actor ID = tenant ID (set by caller via `ActorId`). Thin host that delegates all logic to `RateLimiterLogic`. Loads/saves state via `StateManager`.
    - [x] 3.5 Implement `TryConsumeAsync` (in `RateLimiterLogic`): accept current `RateLimitState` and current time. If current window expired (>1 minute since WindowStart), reset Remaining to CeilingPerMinute and update WindowStart. Decrement Remaining by 1. Return tuple: `(bool allowed, RateLimitState newState)`. Actor calls this, then persists via `StateManager.SetStateAsync`.
    - [x] 3.6 **Default state bootstrapping:** `GetOrAddStateAsync("rateState", new RateLimitState { Remaining = 1500, WindowStart = <now>, CeilingPerMinute = 1500 })`. The actor MUST be functional immediately without Story 1.7 having configured the tenant. Default ceiling = 1500 (Google text-embedding-004 rate limit).
    - [x] 3.7 `SetCeilingAsync(int ceiling)`: update CeilingPerMinute in persisted state, reject non-positive values, and clamp the current window to the new ceiling. Called during tenant configuration (Story 1.7). Until then, default 1500 is used.
    - [x] 3.8 Implement `ResetAsync`: reset Remaining to ceiling, update WindowStart to now.
    - [x] 3.9 Implement `GetStateAsync`: return current `RateLimitState`.

- [x] Task 4: Create GenerateEmbeddingActivity (AC: #1, #2, #3)
    - [x] 4.1 Create `Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` — inherits `WorkflowActivity<EmbeddingInput, EmbeddingResult>`. Constructor takes `EmbeddingClient` and `IActorProxyFactory` via DI.
    - [x] 4.2 In `RunAsync`: (1) Validate `TenantId` and `ContentText`. (2) Prime embedding client secret access so preflight failures happen before rate-limit budget is consumed. (3) Create actor proxy via `_actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(new ActorId(input.TenantId), nameof(EmbeddingRateLimiterActor))`. (4) Call `rateLimiter.TryConsumeAsync()` (no parameter — always 1 token in MVP). (5) If rate limit exhausted, throw `EmbeddingRateLimitException` — DAPR Workflow retry policy handles backoff. (6) Call `_embeddingClient.GenerateAsync(input.ContentText, input.TenantId, ct)`. (7) Return `EmbeddingResult` with vector, provider `"google:text-embedding-004"`, and dimensions 768.
    - [x] 4.3 Do NOT catch exceptions in the activity — let them propagate to the workflow retry policy (Decision D25: workflows handle retry, activities do I/O).

- [x] Task 5: Create exception types (AC: #3)
    - [x] 5.1 Create `Server/Ingestion/EmbeddingRateLimitException.cs` — `public class` inheriting `Exception`. Constructor takes `tenantId` (string).
    - [x] 5.2 Create `Server/Ingestion/EmbeddingApiException.cs` — `public class` inheriting `Exception`. Constructor takes `statusCode` (int), `responseBody` (string), `tenantId` (string).

- [x] Task 6: Register actor and activity in Program.cs (AC: #1, #2)
    - [x] 6.1 Register `EmbeddingRateLimiterActor` in `AddActors()` options: `options.Actors.RegisterActor<EmbeddingRateLimiterActor>()`
    - [x] 6.2 Register `GenerateEmbeddingActivity` in `AddDaprWorkflow()` options: `options.RegisterActivity<GenerateEmbeddingActivity>()`
    - [x] 6.3 Register `EmbeddingClient` in DI with timeout:
        ```csharp
        builder.Services.AddHttpClient<EmbeddingClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        ```
        This registers `EmbeddingClient` as a **typed client**. The factory injects `HttpClient` as the first constructor parameter automatically. `EmbeddingClient` instances are transient (new per request), but `HttpClient` handlers are pooled by the factory. Do NOT register `EmbeddingClient` as singleton — it would hold a stale `HttpClient`.
    - [x] 6.4 Register `DaprClient` is already done (`builder.Services.AddDaprClient()`) — no change needed.

- [x] Task 7: Configure DAPR secret store for local dev (AC: #4)
    - [x] 7.1 Create `deploy/dapr/components/secretstore.yaml` — local file secret store component (already existed from prior story)
    - [x] 7.2 Create `deploy/dapr/secrets.json` with placeholder: `{"google-embedding-api-key": "YOUR_KEY_HERE"}`
    - [x] 7.3 Add `secrets.json` to `.gitignore` — already present in .gitignore
    - [x] 7.4 Create `deploy/dapr/secrets.json.example` with placeholder — committed to repo for developer guidance

- [x] Task 8: Unit tests for EmbeddingClient (AC: #1, #3, #4) **MUST**
    - [x] 8.1 Create `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`
    - [x] 8.2 Test: successful embedding generation — mock `HttpMessageHandler` to return 768-dimension vector, verify result array length and values
    - [x] 8.3 Test: HTTP 429 response — verify `EmbeddingRateLimitException` is thrown with correct tenant ID
    - [x] 8.4 Test: HTTP 500 response — verify `EmbeddingApiException` is thrown with status code and response body
    - [x] 8.5 Test: null/empty text input — verify `ArgumentException` is thrown
    - [x] 8.6 Test: secret retrieval — verify `DaprClient.GetSecretAsync` is called with correct store name and key name
    - [x] 8.7 Test: DAPR secret store unavailable — mock `DaprClient.GetSecretAsync` to throw, verify `EmbeddingApiException` is thrown with actionable message containing "secretstore" and configuration guidance
    - [x] 8.8 Test: malformed JSON response — mock handler to return `{"unexpected": "shape"}`, verify `EmbeddingApiException` is thrown with the raw response body
    - [x] 8.9 Test: wrong dimension count — mock handler to return only 100 floats instead of 768, verify `EmbeddingApiException` is thrown with dimension mismatch message
    - [x] 8.10 Test: HTTP timeout — mock handler to delay beyond 30 seconds, verify `TaskCanceledException` propagates (handled by workflow retry)

- [x] Task 9: Unit tests for RateLimiterLogic (AC: #2) **MUST**
    - [x] 9.1 Create `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs` — test the extracted business logic class directly, NOT the actor. The actor is a thin DAPR host and does not need its own unit tests.
    - [x] 9.2 Test: first call within window — returns `(true, updatedState)` with Remaining decremented by 1
    - [x] 9.3 Test: budget exhausted — returns `(false, state)` after consuming all tokens (call 1501 times from ceiling 1500)
    - [x] 9.4 Test: window reset at boundary — inject `TimeProvider` (or `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`). Advance time by exactly 60 seconds, verify budget resets to ceiling. Advance by 59 seconds, verify budget does NOT reset. Deterministic, never flaky.
    - [x] 9.5 Test: default state — verify initial state has Remaining=1500, CeilingPerMinute=1500
    - [x] 9.6 Test: custom ceiling — set ceiling to 500, verify budget resets to 500 after window expires

- [x] Task 10: Unit tests for GenerateEmbeddingActivity (AC: #1, #2) **MUST**
    - [x] 10.1 Create `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`
    - [x] 10.2 Test: successful embedding — mock `EmbeddingClient` and `IActorProxyFactory`, verify activity returns correct `EmbeddingResult`
    - [x] 10.3 Test: rate limit exhausted — mock actor `TryConsumeAsync` to return false, verify `EmbeddingRateLimitException` is thrown
    - [x] 10.4 Test: embedding client throws — verify exception propagates (not caught by activity)

- [x] Task 11: Create Server.Tests project if not exists (AC: all)
    - [x] 11.1 Project already exists at `tests/Hexalith.Memories.Server.Tests/`
    - [x] 11.2 Project reference to Server already present
    - [x] 11.3 NSubstitute, Shouldly already in test project; added Microsoft.Extensions.TimeProvider.Testing
    - [x] 11.4 Already in `.slnx` solution file

- [x] Task 12: Build and verify (AC: all) **MUST**
    - [x] 12.1 Run `dotnet build` — zero warnings, zero errors
    - [x] 12.2 Run `dotnet test` — all 88 tests pass (53 contracts + 35 server)
    - [x] 12.3 Verify existing `MemoriesInfo` test still passes (regression check)

## Dev Notes

### Architecture Compliance

- **Namespace for activity types:** `Hexalith.Memories.Server.Activities.Ingestion` — feature-based namespace per architecture naming patterns
- **Namespace for ingestion services:** `Hexalith.Memories.Server.Ingestion` — for `EmbeddingClient` and exception types
- **Namespace for actors:** `Hexalith.Memories.Server.Actors`
- **File-scoped namespaces:** `namespace Hexalith.Memories.Server.Ingestion;` (Allman braces per .editorconfig)
- **Decision D4:** Google embedding only in MVP. `EmbeddingClient` is hardcoded to Google text-embedding-004. Story 1.7 will add the provider configuration abstraction.
- **Decision D9:** Concrete classes, no premature interfaces. `EmbeddingClient` is a concrete class. `IEmbeddingRateLimiterActor` is an interface because DAPR Actors REQUIRE interfaces — this is a framework constraint, not premature abstraction.
- **Decision D24:** DAPR Actors for per-tenant stateful singletons. Actor ID = tenant ID.
- **Decision D25:** Workflows orchestrate processes, actors manage per-entity state, activities do I/O. `GenerateEmbeddingActivity` does I/O (calls embedding API). It does NOT manage state — the actor does that.
- **Package management:** Do NOT add version numbers to `.csproj` — use `Directory.Packages.props`. If a new NuGet package is needed (e.g., for Google API client), add it to `Directory.Packages.props` first.

### Critical Architectural Constraints

1. **Activities never call external services directly except through their injected service.** `GenerateEmbeddingActivity` delegates to `EmbeddingClient`. The activity itself does no HTTP calls.
2. **Activities do NOT catch exceptions.** Let exceptions propagate to the DAPR Workflow retry policy. The workflow decides retry behavior.
3. **Actor interfaces are REQUIRED for DAPR Actors.** This is the one place where Decision D9's "no premature interfaces" does NOT apply — DAPR's actor model mandates interfaces.
4. **API keys via DAPR Secrets only.** Never `IConfiguration`, never environment variables in production. For local dev, use the DAPR local file secret store component.
5. **No external Google SDK dependency.** Use plain `HttpClient` with `IHttpClientFactory` to call the Google Generative Language REST API. This keeps Contracts lean and avoids heavy Google Cloud SDK dependencies. The API is simple enough for raw HTTP.
6. **EmbeddingProvider field format:** `"google:text-embedding-004"` — provider:model format, matching the `MemoryUnit.EmbeddingProvider` field (nullable string, populated post-embedding).
7. **Rate limiter actor state persists via DAPR actor state store (Redis).** Survives process restarts. Window resets are time-based, not restart-based.
8. **HttpClient lifecycle:** Use `IHttpClientFactory` via `AddHttpClient<EmbeddingClient>()` — never create `HttpClient` directly. This prevents socket exhaustion and enables resilience policies.
9. **Typed client constructor pattern:** `EmbeddingClient` constructor MUST accept `HttpClient` as its first parameter — this is how `IHttpClientFactory` typed clients work. The factory creates the `HttpClient` and passes it to the constructor. Do NOT accept `IHttpClientFactory` in the constructor.
10. **TimeProvider for testable time:** `RateLimiterLogic` MUST accept `TimeProvider` (built-in .NET 8+) via constructor for deterministic time control. Use `TimeProvider.System` in production, `FakeTimeProvider` in tests. Never use `DateTime.UtcNow` or `DateTime.Now` directly in rate limiting logic.

### Google Embedding API Specifics

**Endpoint:** `POST https://generativelanguage.googleapis.com/v1/models/text-embedding-004:embedContent`

**Request headers:**

- `Content-Type: application/json`
- `x-goog-api-key: {API_KEY}`

**Request body:**

```json
{
    "content": {
        "parts": [{ "text": "Your text content here" }]
    }
}
```

**Response (200 OK):**

```json
{
  "embedding": {
    "values": [0.0123, -0.0456, ...]
  }
}
```

**Response array:** 768 float values for `text-embedding-004` model.

**Error responses:**

- `429 Too Many Requests` — rate limit exceeded, throw `EmbeddingRateLimitException`
- `400 Bad Request` — invalid input (e.g., empty text)
- `401/403` — invalid API key
- `500` — server error

**Input token limit:** `text-embedding-004` supports up to ~2048 tokens per request (~8000 characters). If input text exceeds this, the API returns a 400 error. For MVP, let the 400 propagate as `EmbeddingApiException` — content chunking is a Story 1.6/future concern. The `ExtractContentActivity` (Story 1.3) is responsible for content preparation; this story assumes text arrives within API limits.

**Rate limits:** 1500 requests/minute for `text-embedding-004` (free tier). Per-tenant actor enforces this ceiling.

### DAPR Secrets Configuration

**Local development:** DAPR local file secret store:

```yaml
# deploy/dapr/components/secretstore.yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
    name: secretstore
spec:
    type: secretstores.local.file
    version: v1
    metadata:
        - name: secretsFile
          value: ./secrets.json
```

**Secret retrieval in code:**

```csharp
var secret = await _daprClient.GetSecretAsync("secretstore", "google-embedding-api-key");
string apiKey = secret["google-embedding-api-key"];
```

**Deployed environments:** Replace with DAPR-supported secret store (Azure Key Vault, HashiCorp Vault, etc.) — same code, different component YAML.

### Project Structure Notes

```
src/Hexalith.Memories.Server/
├── Activities/
│   └── Ingestion/
│       ├── EmbeddingInput.cs                  # NEW — sealed record
│       ├── EmbeddingResult.cs                 # NEW — sealed record
│       └── GenerateEmbeddingActivity.cs       # NEW — WorkflowActivity
├── Actors/
│   ├── IEmbeddingRateLimiterActor.cs          # NEW — actor interface
│   ├── EmbeddingRateLimiterActor.cs           # NEW — actor implementation (thin DAPR host)
│   ├── RateLimiterLogic.cs                    # NEW — testable business logic (injected TimeProvider)
│   └── RateLimitState.cs                      # NEW — sealed record
├── Ingestion/
│   ├── EmbeddingClient.cs                     # NEW — HTTP client
│   ├── EmbeddingRateLimitException.cs         # NEW — typed exception
│   └── EmbeddingApiException.cs               # NEW — typed exception
└── Program.cs                                 # MODIFIED — register actor + activity

tests/Hexalith.Memories.Server.Tests/          # NEW project if not exists
├── Activities/
│   └── GenerateEmbeddingActivityTests.cs      # NEW
├── Actors/
│   └── RateLimiterLogicTests.cs               # NEW — tests business logic, not actor infra
└── Ingestion/
    └── EmbeddingClientTests.cs                # NEW

deploy/dapr/components/
└── secretstore.yaml                           # NEW

deploy/dapr/
├── secrets.json                               # NEW (gitignored)
└── secrets.json.example                       # NEW (committed)
```

Alignment: Matches architecture.md project structure exactly. Feature-based namespaces under `Server/`. Actors have their own folder. Activities grouped by pipeline stage under `Activities/Ingestion/`.

### Testing Requirements

- **Framework:** xUnit + Shouldly + NSubstitute (aligned with EventStore per Decision D16)
- **Actor testing pattern:** Business logic lives in `RateLimiterLogic` (plain class with `TimeProvider`). Test `RateLimiterLogic` directly — never test the DAPR actor infrastructure. Use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` for deterministic window boundary tests. Add this package to `Directory.Packages.props` if not present.
- **Activity testing pattern:** Mock `EmbeddingClient` and `IActorProxyFactory` via NSubstitute. Call `RunAsync` directly without workflow engine (per Testability Architecture).
- **HTTP testing pattern:** Mock `HttpMessageHandler` for `EmbeddingClient` tests. Use `NSubstitute.Substitute.For<HttpMessageHandler>()` or a custom `DelegatingHandler` that returns predetermined responses.
- **DaprClient mocking:** `DaprClient` is an abstract class with virtual methods — `NSubstitute.Substitute.For<DaprClient>()` works because NSubstitute can mock abstract classes. Set up: `daprClient.GetSecretAsync("secretstore", "google-embedding-api-key", Arg.Any<Dictionary<string,string>>(), Arg.Any<CancellationToken>()).Returns(new Dictionary<string,string> { ["google-embedding-api-key"] = "test-key" })`. Note the 4-parameter overload — the 2-parameter convenience method may not be virtual.
- **Assertion:** Use `.ShouldBe()` for value comparisons, `.ShouldThrow<T>()` for exception assertions.
- **JSON options:** Use `MemoriesJsonContext.Options` for any JSON serialization in tests.

### Previous Story Intelligence (from 1-2)

**Patterns established:**

- `.slnx` solution format — manually created
- `Directory.Packages.props` — centralized versions, no versions in .csproj
- `Directory.Build.props` — .NET 10, C# 14, nullable enable, TreatWarningsAsErrors
- `.editorconfig` — Allman braces, `_camelCase` fields, I-prefix interfaces, Async suffix, 4-space indent
- Test pattern: xUnit + Shouldly, `[Fact]`, `.ShouldBe()` assertions
- Enum serialization: PascalCase strings via per-enum `[JsonConverter]` attributes
- `sealed record` for all data types — immutable, value equality
- `MemoriesJsonContext.Options` — single shared `JsonSerializerOptions` instance
- C# 14 `field` keyword used for null-coalescing property accessors

**Debug learnings from 1-2:**

- `Aspire.Hosting.AppHost` is implicit — don't add to CPM
- Submodule paths: `src/submodules/Hexalith.Commons/`, `src/submodules/Hexalith.EventStore/`
- `Shouldly.ShouldNotContain` is case-insensitive by default — use `Case.Sensitive`
- Existing MemoriesInfo test must remain passing (regression guard)

**Files to preserve (do NOT modify or delete):**

- All existing V1 contract types in `src/Hexalith.Memories.Contracts/V1/`
- `tests/Hexalith.Memories.Contracts.Tests/` — all existing tests
- `src/Hexalith.Memories.Server/Program.cs` — modify (add registrations), do NOT rewrite

### Git Intelligence

Recent commits show:

- Story 1.2 established all V1 contract types (MemoryUnit with `EmbeddingProvider` and `EmbeddingDimensions` fields)
- Story 1.3 (content extraction via Kreuzberg) is in review — this story's implementation is independent of 1.3 code; it receives text as input
- Submodule updates for Hexalith.EventStore indicate active upstream development

### Anti-Patterns to Avoid

- **DO NOT use Google Cloud SDK NuGet package** — raw `HttpClient` + REST API is sufficient for the simple embedContent endpoint. Avoids heavy dependency.
- **DO NOT create `IEmbeddingClient` interface** — concrete class only (Decision D9). Extract interface when second provider arrives (Story 1.7 or Phase 1.5).
- **DO NOT store API keys in appsettings.json or environment variables** — DAPR Secrets API only (AC #4).
- **DO NOT catch exceptions in GenerateEmbeddingActivity** — let them propagate to workflow retry policy (Decision D25).
- **DO NOT create a global/shared rate limiter** — per-tenant actors only (Decision D24). Shared rate limiter is Phase 3.
- **DO NOT add validation logic for tenant existence in EmbeddingClient** — tenant validation is upstream in `IngestionValidator` (Decision D12).
- **DO NOT use `System.Numerics.Tensors`** — the vector is a plain `float[]`. Tensor types are for computation, not storage/transfer.
- **DO NOT batch embedding requests** — single-document embedding calls per NFR5. Batching is a future optimization.
- **DO NOT add EmbeddingProvider configuration management** — that's Story 1.7. This story hardcodes Google text-embedding-004 with 768 dimensions.
- **DO NOT use `ConfigurationManager` or `IOptions<T>` for the API key** — DAPR Secrets API only. Configuration (rate limits, provider selection) is Story 1.7.
- **DO NOT pass unused ProviderConfig/Dimensions in EmbeddingInput** — MVP hardcodes Google. Adding dead data fields wastes tokens and confuses the dev agent. Provider configuration is Story 1.7.
- **DO NOT accept `IHttpClientFactory` in EmbeddingClient constructor** — use the typed client pattern: accept `HttpClient` as first parameter. The factory injects it automatically.
- **DO NOT use `DateTime.UtcNow` directly in rate limiter logic** — inject `TimeProvider` for testable time. `DateTime.UtcNow` makes window boundary tests flaky.
- **DO NOT test the DAPR actor class directly** — test `RateLimiterLogic` (the extracted business logic class). The actor is a thin host that delegates; testing it couples you to DAPR infrastructure.

### Cross-Cutting Dependency Map

```
Contracts.V1 (MemoryUnit, MemoryUnitStatus) ← Server (this story adds to Server)
                                                ↑
                                          DAPR Client (secrets)
                                          DAPR Actors (rate limiter)
                                          DAPR Workflow (activity registration)
                                          HttpClient (Google API)
```

### References

- [Source: architecture.md#DAPR Actor Patterns] — EmbeddingRateLimiterActor interface, implementation, registration, proxy usage
- [Source: architecture.md#DAPR Workflow Patterns] — Activity definition pattern, workflow registration
- [Source: architecture.md#Security Architecture] — DAPR Secrets scoping for embedding keys
- [Source: architecture.md#Cross-Cutting Concern #5] — Per-tenant rate limiting via actors
- [Source: architecture.md#Decision D4] — Google embedding only in MVP
- [Source: architecture.md#Decision D9] — Concrete classes, extract interface when needed
- [Source: architecture.md#Decision D24] — DAPR Actors for per-tenant singletons
- [Source: architecture.md#Decision D25] — Workflow-Actor-Activity separation of concerns
- [Source: architecture.md#Testability Architecture] — Actor logic testable without DAPR, activities testable independently
- [Source: architecture.md#Data Flow] — EmbeddingRateLimiterActor.TryConsumeAsync() → GenerateEmbeddingActivity → Google Embedding API
- [Source: architecture.md#File Structure] — Server/Ingestion/EmbeddingClient.cs, Server/Activities/Ingestion/GenerateEmbeddingActivity.cs, Server/Actors/EmbeddingRateLimiterActor.cs
- [Source: epics.md#Story 1.4] — Acceptance criteria, user story
- [Source: epics.md#Story 1.7] — Embedding provider configuration (future story, informs but does not block this story)
- [Source: prd.md#Embedding Provider Configuration] — Provider table, per-tenant configuration fields, critical constraints

## Implementation Readiness Addendum (2026-05-18)

This story is historical completed scope and may remain closed. If it is reopened, reimplemented, or used as a template for a future technical story, completion must include observable proof that embedding generation advances the developer ingestion/search journey.

Required future-rework evidence:

1. A developer-observable embedding result containing provider, model, dimensions, and memory-unit metadata mapping.
2. Activity, API, CLI, trace, or integration-harness evidence showing the embedding result crosses the ingestion boundary.
3. Secret-redaction evidence proving embedding credentials are not emitted in logs, CLI output, API responses, or test snapshots.
4. Rate-limit or retry evidence showing 429 behavior reaches the workflow recovery path.

Mocked `EmbeddingClient`, rate-limiter, and exception unit tests alone are not sufficient evidence for future work.

## Definition of Done

1. `EmbeddingClient` calls Google text-embedding-004 API and returns 768-dim float[] with dimension validation
2. `RateLimiterLogic` implements sliding window rate limiting with `TimeProvider` injection — fully testable without DAPR
3. `EmbeddingRateLimiterActor` is a thin host delegating to `RateLimiterLogic` (1500 req/min default, functional without tenant config)
4. `GenerateEmbeddingActivity` validates inputs, primes secret access, checks rate limiter, then calls embedding client
5. API key retrieved exclusively via DAPR Secrets API with actionable error on failure
6. HTTP 429, malformed responses, dimension mismatches, and timeouts all surface as typed exceptions for workflow retry
7. All unit tests pass (EmbeddingClient incl. secret failure + dimension validation, RateLimiterLogic with FakeTimeProvider, Activity)
8. `dotnet build` zero warnings, `dotnet test` all pass
9. No regression in existing tests

## Change Log

- 2026-03-29: Code review follow-up — aligned AC4 wording with the implemented DAPR local file secret store, added input/preflight validation before rate-limit consumption, hardened `SetCeiling` validation/clamping, and added 5 follow-up tests.
- 2026-03-29: Implementation complete — all 12 tasks done, 20 new tests passing, zero regressions. EmbeddingClient (typed HttpClient → Google text-embedding-004), RateLimiterLogic (TimeProvider-injected, FakeTimeProvider-tested), EmbeddingRateLimiterActor (thin DAPR host), GenerateEmbeddingActivity (rate-check → embed → result), exception types, DI registration, DAPR secret store config.
- 2026-03-28: Story created — comprehensive embedding generation guide with Google API specifics, DAPR Actor patterns, and secret management
- 2026-03-28: Party mode review applied — 7 improvements: lazy-init secret caching for transient typed client, removed dead ProviderConfig/Dimensions from EmbeddingInput, explicit typed client constructor pattern (HttpClient first param), actor default state bootstrapping with CeilingPerMinute=1500, secret store failure test case, TimeProvider injection for deterministic actor window tests, extracted RateLimiterLogic as testable service class
- 2026-03-28: Advanced elicitation (5 methods: pre-mortem, red team, first principles, failure mode, critique) — 8 improvements: TimeProvider constructor pattern with code example, DaprClient mock setup (abstract class with 4-param overload), Google API input token limit (~2048 tokens) documented, HTTP 30s timeout configured, malformed JSON response handling, response vector dimension validation (768 check), 4 new test cases (malformed JSON, wrong dimensions, timeout, secret failure), Definition of Done updated for RateLimiterLogic

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- `WorkflowActivityContext` does not expose `CancellationToken` — used `CancellationToken.None` in `GenerateEmbeddingActivity.RunAsync` (consistent with existing `ExtractContentActivity` pattern)
- `EmbeddingClient` changed from `sealed` to non-sealed with `virtual GenerateAsync` — NSubstitute cannot proxy sealed classes. Decision D9 says "concrete class, no premature interface" which is still honored; the class is concrete, just not sealed.

### Completion Notes List

- All 12 tasks completed successfully
- 25 new unit tests added (11 EmbeddingClient, 8 RateLimiterLogic, 6 GenerateEmbeddingActivity)
- All 95 tests pass (53 existing contracts + 42 server), zero regressions
- `dotnet test Hexalith.Memories.slnx` succeeded after review follow-up fixes
- DAPR secret store was already configured from prior work; added secrets.json and secrets.json.example
- `Microsoft.Extensions.TimeProvider.Testing` added to Directory.Packages.props for deterministic time tests
- Exception types include all required constructors for proper exception hierarchy compliance

### File List

**New files:**

- `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingInput.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingResult.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`
- `src/Hexalith.Memories.Server/Actors/IEmbeddingRateLimiterActor.cs`
- `src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs`
- `src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs`
- `src/Hexalith.Memories.Server/Actors/RateLimitState.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingRateLimitException.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingApiException.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Actors/RateLimiterLogicTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`
- `deploy/dapr/secrets.json` (gitignored)
- `deploy/dapr/secrets.json.example`

**Modified files:**

- `src/Hexalith.Memories.Server/Program.cs` — registered actor, activity, and typed HttpClient
- `Directory.Packages.props` — added Microsoft.Extensions.TimeProvider.Testing
- `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` — added TimeProvider.Testing package ref

### Review Findings

- [x] [Review][Patch] Align AC4 local-development secret wording with the implemented DAPR local file secret store [_bmad-output/implementation-artifacts/1-4-embedding-generation.md:23]
- [x] [Review][Patch] Validate tenant identifiers before creating the actor proxy [src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:37]
- [x] [Review][Patch] Avoid spending rate-limit budget before preflight failures such as secret retrieval [src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:40]
- [x] [Review][Patch] Harden `SetCeiling` to reject non-positive values and clamp the current window to the new ceiling [src/Hexalith.Memories.Server/Actors/RateLimiterLogic.cs:62]
- [x] [Review][Defer] End-to-end embedding flow is not wired into orchestration [src/Hexalith.Memories.Server/Program.cs:41] — deferred: orchestration and memory-unit persistence belong to upcoming ingestion workflow work and depend on the final pipeline shape.
- [x] [Review][Defer] Rate-limiting scope conflicts with credential scope [src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:37] — deferred: Story 1.7 introduces provider configuration and is the right place to decide per-tenant vs per-credential quota enforcement.
- [x] [Review][Defer] Story transition rationale is comment-only and not machine-readable [_bmad-output/implementation-artifacts/sprint-status.yaml:39] — deferred, pre-existing
- [x] [Review][Defer] Story status requires manual dual-write across tracking files [_bmad-output/implementation-artifacts/1-4-embedding-generation.md:3] — deferred, pre-existing
