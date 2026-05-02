# Story 13.1: Extend EmbeddingProviderDefaults to Accept Ollama

Status: review

**Effort estimate:** ~0.75–1.0 working day. Breakdown:

- **0.10 day — Task 0:** Pre-impl verification spikes (model-name drift confirmation, regex impact, dimension-validation pattern reuse, rate-limit ceiling decision).
- **0.20 day — Task 1:** `EmbeddingProviderDefaults.cs` — add `OllamaProviderName` + `OllamaModelName` + `OllamaDimensions` + `OllamaMaxRateLimitPerMinute` constants; add `Ollama()` factory method mirroring the shape of `Google()`.
- **0.25 day — Task 2:** `EmbeddingProviderDefaults.Validate(...)` — convert the hard-coded `Provider == "google"` check into a supported-provider list; extend `ModelNamePattern` regex to admit the colon character (`qwen3-embedding:4b` model identifier); add Ollama-specific dimension assertion (`qwen3-embedding:4b` ⇔ 2560); apply the Ollama rate-limit ceiling when `Provider == "ollama"`.
- **0.25 day — Task 3:** Tests — add the Ollama factory + validation test cases to `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` mirroring the existing Google scenarios; add a regression test asserting all existing Google test cases still pass without modification; add a guard test asserting the unsupported-provider error message lists every supported provider name; add the dim-pinning regression test surfaced by the Round 2 elicitation pre-mortem (`Validate_OllamaQwen3_AcceptsExactly2560`).
- **0.10 day — Task 4:** Final validation — full `Hexalith.Memories.Server.Tests` slice green; `Hexalith.Memories.Contracts.Tests` slice green (no regressions); `dotnet build` 0W/0E across the solution.

**HARD prerequisite:** None. Story 13.1 is the foundation for Epic 13; everything else (13.2 OidcTokenProvider, 13.3 EmbeddingClient, 13.4 TenantEmbeddingConfig OIDC fields, 13.5 actor surface, 13.6 migration tool, 13.7 docs + integration) depends on this one. **13.1 must be `done` before 13.2 / 13.3 / 13.4 start.**

**SOFT prerequisite:** None. Story 13.1 is parallel-safe against any in-flight Epic 12 story (the file scope is fully disjoint).

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** `EmbeddingProviderDefaults` learns the word `"ollama"`. Concretely: (1) a new `Ollama()` factory method returns a `TenantEmbeddingConfig` with sensible self-hosted defaults (`qwen3-embedding:4b`, 2560 dimensions, 6000 req/min). (2) `Validate(...)` accepts both `"google"` and `"ollama"` as valid provider names — anything else throws `ArgumentException` whose message lists exactly the supported providers, so no consumer of this method needs to special-case the provider string. (3) The `ModelNamePattern` regex is extended to admit the colon character, because Ollama's canonical model identifier `qwen3-embedding:4b` carries a `:` between model and tag and the existing regex rejects it. (4) An Ollama-specific dimension assertion enforces that `qwen3-embedding:4b` is paired with `Dimensions = 2560` (exactly mirroring the existing Google `gemini-embedding-001` ⇔ `(768 | 1536 | 3072)` rule). **No behavioral change for existing Google callers.**

**What 13.1 explicitly does NOT do (deferred to sibling stories):**

- Does NOT add the OIDC fields (`BaseUrl`, `AuthMode`, `OidcTokenEndpoint`, `OidcClientId`, `OidcScope`) to `TenantEmbeddingConfig`. **That is Story 13.4.** Story 13.1's `Ollama()` factory therefore does **not** populate those fields and `Validate` does **not** branch on them yet. The Story 13.1 ACs explicitly call out that the OIDC-required-when-AuthMode-oidc-client-credentials check is added by 13.4.
- Does NOT touch `EmbeddingClient.cs`. The actual HTTP dispatch to Ollama is **Story 13.3**.
- Does NOT touch `OidcTokenProvider`. **Story 13.2** writes that class.
- Does NOT touch `TenantConfigurationActor`. **Story 13.5** plumbs the new fields through actor state.
- Does NOT migrate any existing tenant data. **Story 13.6** ships the migration tool.
- Does NOT touch `appsettings.json` or the AppHost. Server defaults stay as-is — the operator wires Ollama tenants explicitly via the provisioning workflow once 13.4 + 13.5 land.

**What already exists (do NOT rebuild):**

1. **`EmbeddingProviderDefaults` class.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`. **Edit, do not rewrite.** Static partial class with `[GeneratedRegex]` source-generators; preserve the `partial` keyword and the source-generator pattern. Two existing `[GeneratedRegex]` partial methods — `ApiSecretKeyNamePattern()` and `ModelNamePattern()` — stay where they are; `ModelNamePattern` gets its regex string extended.

2. **`TenantEmbeddingConfig` record.** `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`. **Do NOT edit in this story.** All seven existing `init` properties (`Provider`, `Model`, `Dimensions`, `RateLimitPerMinute`, `ApiSecretKeyName`, `ReindexRequired`) stay verbatim. The new OIDC properties are 13.4's scope, not 13.1's. The `Ollama()` factory in 13.1 populates only the existing seven properties.

3. **`Google()` factory method.** Returns `TenantEmbeddingConfig { Provider = "google", Model = "gemini-embedding-001", Dimensions = 768, RateLimitPerMinute = 1500, ApiSecretKeyName = "google-embedding-api-key", ReindexRequired = false }`. **Use as the shape template for the new `Ollama()` factory** — same property order, same record-init style, same XML doc shape.

4. **`GetBreakingChangeFields(currentConfig, proposedConfig)` method.** Already correctly identifies `provider`, `model`, `dimensions` as the breaking-change set. **Do NOT touch in this story.** Story 13.4 will potentially add `BaseUrl` to the breaking-change set when `BaseUrl` actually exists on the record. Touching it now means re-touching it in 13.4 — wasted churn.

5. **`EmbeddingProviderDefaultsTests` test class.** `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`. 18 existing test cases covering Google. **Add to this file, do NOT create a sibling file.** Mirror the `Google_*` and `Validate_*` naming convention. Use Shouldly assertions (`ShouldBe`, `ShouldThrow<ArgumentException>`, `ShouldNotThrow`) — do NOT introduce a different assertion library.

6. **DI registration of embedding defaults.** None — `EmbeddingProviderDefaults` is a `static partial class`, no DI involvement. **Do NOT register anything new in `Program.cs`.** Static methods are called directly by `EmbeddingProviderConfigurationService` and the provisioning workflow.

7. **`ArgumentException` is the chosen exception type for validation failures.** Every existing throw uses `ArgumentException` with `nameof(config.{field})` as the parameter name. **Match this convention** — do NOT introduce `ValidationException`, `InvalidOperationException`, or a custom `EmbeddingConfigException`.

8. **Source-generated regexes.** Both existing patterns use `[GeneratedRegex(...)]` partial methods. **Use the same pattern for any new regex**; do not introduce `Regex.IsMatch(...)` with inline strings (NU1902-style perf regression) or `new Regex(...)` instances.

9. **Existing `IngestionSettings` / appsettings.** **Out of scope for 13.1.** The default lookup path stays identical; tenants explicitly opting into Ollama get a `TenantEmbeddingConfig` constructed from `EmbeddingProviderDefaults.Ollama()` (or hand-constructed via the provisioning workflow once 13.5 lands).

**What 13.1 adds:**

1. **`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`** — EDIT. Five additions, one substitution:

   - **ADD** `public const string OllamaProviderName = "ollama";` next to the existing `GoogleProviderName` constant. Lowercase for case-insensitive match parity with how `Validate` already compares the existing constant.

   - **ADD** `public const string OllamaModelName = "qwen3-embedding:4b";` next to the existing `GoogleModelName` constant. Note the colon — this is the canonical Ollama identifier and **must** be storable verbatim in `TenantEmbeddingConfig.Model`. Verify by string equality against the `Model` property after `Ollama()` factory invocation.

   - **ADD** `private const int OllamaDimensions = 2560;` next to the existing `GoogleMaxRateLimitPerMinute` constant. This is the dimension count `qwen3-embedding:4b` actually emits — confirmed by the operator and by the Sprint Change Proposal §2.5.

   - **ADD** `private const int OllamaMaxRateLimitPerMinute = 60_000;` next to the existing `GoogleMaxRateLimitPerMinute`. Rationale: Ollama is self-hosted, so the rate limit exists to protect the operator's own backend throughput rather than a shared external quota. 60 000 req/min = 1000 req/sec is well above any expected workload but still puts a defensive ceiling on accidental misconfiguration. Document the rationale in an XML doc comment on the constant.

   - **ADD** `public static TenantEmbeddingConfig Ollama() => new() { ... };` mirroring the existing `Google()` factory:
     ```csharp
     /// <summary>Returns the default Ollama embedding configuration using qwen3-embedding:4b (2560 dimensions, self-hosted).</summary>
     /// <returns>A <see cref="TenantEmbeddingConfig"/> with Ollama defaults.</returns>
     public static TenantEmbeddingConfig Ollama() => new()
     {
         Provider = OllamaProviderName,
         Model = OllamaModelName,
         Dimensions = OllamaDimensions,
         RateLimitPerMinute = 6000,                  // 100 req/sec sustained — comfortable headroom on a single self-hosted Ollama node
         ApiSecretKeyName = "memories-embedding-client-secret",  // DAPR Secrets store key for the OIDC client_secret (see Story 13.4)
         ReindexRequired = false,
     };
     ```
     Property order matches `Google()` verbatim. Trailing comma after the last property matches the existing convention. The `ApiSecretKeyName` placeholder name `memories-embedding-client-secret` matches the Sprint Change Proposal §4.5 DAPR Secrets store entry exactly — operators get a default that "just works" if they follow the runbook.

   - **REPLACE** the `Validate(config)` provider check at lines 76–81. Current code:
     ```csharp
     if (!string.Equals(config.Provider, GoogleProviderName, StringComparison.OrdinalIgnoreCase))
     {
         throw new ArgumentException(
             $"Provider '{config.Provider}' is not supported in the MVP implementation. Only '{GoogleProviderName}' is currently supported.",
             nameof(config.Provider));
     }
     ```
     Replace with a list-based check:
     ```csharp
     if (!IsSupportedProvider(config.Provider))
     {
         throw new ArgumentException(
             $"Provider '{config.Provider}' is not supported. Supported providers: '{GoogleProviderName}', '{OllamaProviderName}'.",
             nameof(config.Provider));
     }
     ```
     plus a new private helper:
     ```csharp
     private static bool IsSupportedProvider(string provider) =>
         string.Equals(provider, GoogleProviderName, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(provider, OllamaProviderName, StringComparison.OrdinalIgnoreCase);
     ```
     **Why a helper instead of inline `||`:** future provider additions (openai, mistral) extend the helper without touching the throw-site error message construction. Keeps the diff small and the next extension cheap.

   - **EXTEND** `ModelNamePattern` regex from `^[A-Za-z0-9._-]+$` to `^[A-Za-z0-9.:_-]+$` — single character added (`:`). The colon is required for Ollama's `model:tag` identifier convention (`qwen3-embedding:4b`). The change is non-narrowing for existing inputs (every previously-valid string remains valid). Verify the existing `Validate_ModelWithUnsafeCharacters_ShouldThrow` test (line 144 of the test file) still passes — it uses `gemini/embedding/001` (forward slash, not colon), and forward slash is still rejected.

   - **ADD** an Ollama-specific dimension assertion mirroring the existing Google one. Current Google check at lines 95–101:
     ```csharp
     if (string.Equals(config.Model, GoogleModelName, StringComparison.OrdinalIgnoreCase) &&
         config.Dimensions is not (768 or 1536 or 3072))
     {
         throw new ArgumentException(
             $"Model '{GoogleModelName}' only supports dimensions 768, 1536, or 3072.",
             nameof(config.Dimensions));
     }
     ```
     Add immediately after:
     ```csharp
     if (string.Equals(config.Model, OllamaModelName, StringComparison.OrdinalIgnoreCase) &&
         config.Dimensions != OllamaDimensions)
     {
         throw new ArgumentException(
             $"Model '{OllamaModelName}' only supports {OllamaDimensions} dimensions.",
             nameof(config.Dimensions));
     }
     ```
     Same shape, same exception type, same parameter name. Tied to the constant so a future model swap updates one place.

   - **EXTEND** the rate-limit ceiling check (currently hard-codes `GoogleMaxRateLimitPerMinute`). Current check at lines 108–113:
     ```csharp
     if (config.RateLimitPerMinute > GoogleMaxRateLimitPerMinute)
     {
         throw new ArgumentException(
             $"RateLimitPerMinute must be {GoogleMaxRateLimitPerMinute} or less to prevent monopolizing shared API keys.",
             nameof(config.RateLimitPerMinute));
     }
     ```
     Replace with a per-provider lookup:
     ```csharp
     int maxRateLimit = string.Equals(config.Provider, OllamaProviderName, StringComparison.OrdinalIgnoreCase)
         ? OllamaMaxRateLimitPerMinute
         : GoogleMaxRateLimitPerMinute;

     if (config.RateLimitPerMinute > maxRateLimit)
     {
         throw new ArgumentException(
             $"RateLimitPerMinute must be {maxRateLimit} or less for provider '{config.Provider}'.",
             nameof(config.RateLimitPerMinute));
     }
     ```
     The existing "monopolizing shared API keys" wording is provider-specific (it only applies to Google's shared cloud quota); the new message generalizes. **Do NOT keep both error strings** — drift between the old and new wording will silently invalidate any existing test that asserts on the message text. (Currently none do — verified — but the principle holds.)

2. **`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`** — EDIT. Add the Ollama scenarios. **Append, do NOT reorganize the existing tests.** Match the Shouldly + xUnit conventions exactly. Required new test methods:

   - **`Ollama_ShouldReturnCorrectDefaults`** — calls `EmbeddingProviderDefaults.Ollama()` and asserts every field: `Provider == "ollama"`, `Model == "qwen3-embedding:4b"`, `Dimensions == 2560`, `RateLimitPerMinute == 6000`, `ApiSecretKeyName == "memories-embedding-client-secret"`, `ReindexRequired == false`. Mirrors `Google_ShouldReturnCorrectDefaults`.

   - **`Validate_OllamaProvider_ShouldNotThrow`** — `Validate(EmbeddingProviderDefaults.Ollama())` does not throw.

   - **`Validate_OllamaWithEmptyModel_ShouldThrow`** — `Validate(EmbeddingProviderDefaults.Ollama() with { Model = "" })` throws `ArgumentException`. (Theory with `[InlineData("")]` and `[InlineData("   ")]` is preferred — mirrors `Validate_EmptyModel_ShouldThrow`.)

   - **`Validate_OllamaUnsupportedDimension_ShouldThrow`** — `Validate(EmbeddingProviderDefaults.Ollama() with { Dimensions = 768 })` throws `ArgumentException`. The 768 here is intentionally the Google default — guards against accidentally migrating a Google config to Ollama provider name without updating dimensions.

   - **`Validate_OllamaWithModelColon_ShouldNotThrow`** — `Validate(EmbeddingProviderDefaults.Ollama())` (which has `Model = "qwen3-embedding:4b"`) does not throw. **This is the single most critical test in this story** — it asserts the regex extension actually accepts the colon character. If this test fails, every downstream Epic 13 story is blocked.

   - **`Validate_OllamaModelWithUnsafeCharacters_ShouldThrow`** — Theory with `[InlineData("model/with/slash")]`, `[InlineData("model with space")]`, `[InlineData("model;semi")]` — confirms that the regex extension did NOT accidentally weaken validation beyond the colon.

   - **`Validate_UnsupportedProvider_ErrorMessageListsSupportedProviders`** — calls `Validate(EmbeddingProviderDefaults.Google() with { Provider = "openai" })` and asserts the thrown `ArgumentException.Message` contains both `"google"` and `"ollama"`. Guards against future drift where someone adds a new supported provider but forgets to update the error message.

   - **`Validate_OllamaRateLimitAtMaximum_ShouldNotThrow`** — `Validate(EmbeddingProviderDefaults.Ollama() with { RateLimitPerMinute = 60_000 })` does not throw. Mirrors `Validate_RateLimitAtMaximum_ShouldNotThrow` for Google.

   - **`Validate_OllamaRateLimitExceedsMaximum_ShouldThrow`** — `Validate(EmbeddingProviderDefaults.Ollama() with { RateLimitPerMinute = 60_001 })` throws `ArgumentException`. Mirrors `Validate_RateLimitExceedsMaximum_ShouldThrow` for Google.

   - **`Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow`** — `Validate(EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 5000 })` throws `ArgumentException`. Confirms the per-provider rate-limit ceiling is correctly partitioned: 5000 > Google's 3000 and < Ollama's 60 000, but the Google config must still hit the Google ceiling.

   - **`Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow`** — `Validate(EmbeddingProviderDefaults.Ollama() with { Model = "qwen3-embedding:4b", Dimensions = 768 })` throws. Catches the cross-pollination disaster where someone copy-pastes a Google dimension count into an Ollama config.

   - **`GetBreakingChangeFields_GoogleToOllama_ShouldReportProviderModelAndDimensions`** — `GetBreakingChangeFields(EmbeddingProviderDefaults.Google(), EmbeddingProviderDefaults.Ollama())` returns `["provider", "model", "dimensions"]`. Confirms migration is correctly flagged as a breaking change.

3. **No other files in this story.** The file scope is hard-bounded to those two files.

## Story

As a **backend developer**,
I want `EmbeddingProviderDefaults` to recognize `ollama` as a valid provider name with sensible defaults for `qwen3-embedding:4b` (2560 dimensions),
So that downstream Epic 13 stories (13.2 OIDC token provider, 13.3 Ollama-aware `EmbeddingClient`, 13.4 `TenantEmbeddingConfig` OIDC fields, 13.5 actor surface, 13.6 migration tool, 13.7 docs + integration tests) can each branch on `Provider == "ollama"` without re-litigating provider-name validation, factory shape, or dimension constraints.

## Acceptance Criteria

1. **AC1 — `Ollama()` factory exists and returns canonical defaults.** A new public static method `EmbeddingProviderDefaults.Ollama()` returns a `TenantEmbeddingConfig` with `Provider = "ollama"`, `Model = "qwen3-embedding:4b"`, `Dimensions = 2560`, `RateLimitPerMinute = 6000`, `ApiSecretKeyName = "memories-embedding-client-secret"`, `ReindexRequired = false`. Property order and record-init shape match the existing `Google()` factory verbatim.

2. **AC2 — `Validate(...)` accepts both providers.** `Validate(EmbeddingProviderDefaults.Google())` and `Validate(EmbeddingProviderDefaults.Ollama())` both succeed (no exception). All 18 existing tests continue to pass without modification.

3. **AC3 — Unsupported-provider error message lists supported providers.** `Validate(config with { Provider = "openai" })` (or any value other than `google`/`ollama`) throws `ArgumentException` whose `Message` property contains the substring `"google"` AND the substring `"ollama"`. The error wording deliberately enumerates the supported set so callers can fix the input without consulting source code.

4. **AC4 — Model-name regex admits colon.** The `ModelNamePattern` regex is extended to `^[A-Za-z0-9.:_-]+$`. `Validate(EmbeddingProviderDefaults.Ollama())` (with `Model = "qwen3-embedding:4b"`) does NOT throw on the model-name regex check. Existing `Validate_ModelWithUnsafeCharacters_ShouldThrow` test (using `gemini/embedding/001`) still throws — the extension does NOT weaken validation beyond the colon character.

5. **AC5 — Ollama-specific dimension assertion.** `Validate(config with { Provider = "ollama", Model = "qwen3-embedding:4b", Dimensions = D })` succeeds when `D == 2560` and throws `ArgumentException` for any other `D > 0`. Mirrors the existing Google `gemini-embedding-001` ⇔ `(768 | 1536 | 3072)` rule.

6. **AC6 — Per-provider rate-limit ceiling.** `Validate(...)` enforces `RateLimitPerMinute ≤ 3000` for Google and `RateLimitPerMinute ≤ 60_000` for Ollama. A 5000 req/min Google config fails; a 5000 req/min Ollama config succeeds; a 60_001 req/min Ollama config fails.

7. **AC7 — `GetBreakingChangeFields` flags Google→Ollama migration as breaking.** `GetBreakingChangeFields(Google(), Ollama())` returns the array `["provider", "model", "dimensions"]` (in that order — the order matches the existing implementation's field-by-field append).

8. **AC8 — Source-generator pattern preserved.** The class remains `public static partial class EmbeddingProviderDefaults`. Both existing `[GeneratedRegex(...)]` partial methods (`ApiSecretKeyNamePattern`, `ModelNamePattern`) compile cleanly with the regex string change. **No new instance regex (`new Regex(...)`) is introduced.**

9. **AC9 — Convention compliance.** Every new throw uses `ArgumentException` with `nameof(config.{field})` as the parameter name. New constants are placed alphabetically-by-provider (Google constants together, Ollama constants together) within the existing constants region. New XML doc comments match the existing one-line `<summary>` shape.

10. **AC10 — Test coverage.** Test methods listed in §"What 13.1 adds" item 2 are present and green. The full `Hexalith.Memories.Server.Tests` slice runs without regressions vs. the pre-change baseline.

11. **AC11 — File scope discipline (per Epic 12 Story 12.3 A4 enforcement).** The diff modifies **exactly two files**: `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` and `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`. **No other file is touched.** Specifically, `TenantEmbeddingConfig.cs` is **NOT** modified — its OIDC field additions are Story 13.4's scope, not 13.1's. If the dev agent finds itself "needing" to touch a third file, that is a signal the agent has expanded scope into a sibling story; STOP and update this story file before proceeding.

## Tasks / Subtasks

- [x] **Task 0: Pre-impl verification spikes (AC: confirms model-name drift assumption + regex impact)**
  - [x] Subtask 0.1: Verify the current Google default model name. Read `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:19` and confirm `GoogleModelName = "gemini-embedding-001"`. The Sprint Change Proposal §2.3 references `text-embedding-004` in the OLD AC text — this is **stale wording in the proposal**, not a pending code change. Story 13.1 keeps the existing Google default verbatim. **Do NOT "correct" the Google model name back to `text-embedding-004` based on the proposal text.**
  - [x] Subtask 0.2: Verify the existing `ModelNamePattern` regex. Read line 126: `[GeneratedRegex("^[A-Za-z0-9._-]+$")]`. Confirm the colon character is NOT in the character class. The Ollama identifier `qwen3-embedding:4b` will fail this regex unchanged → AC4 mandates extending the class to `^[A-Za-z0-9.:_-]+$`.
  - [x] Subtask 0.3: Verify the existing `Validate_ModelWithUnsafeCharacters_ShouldThrow` test (`tests/.../EmbeddingProviderDefaultsTests.cs:144`) uses `gemini/embedding/001`, which contains forward slash, not colon. Confirm the AC4-required regex extension still rejects forward slash → existing test stays green.
  - [x] Subtask 0.4: Decide and document the Ollama rate-limit ceiling. Default to `60_000` (1000 req/sec) per §"What 13.1 adds" item 1 rationale — operator-overridable via tenant config. Document the choice in an XML doc comment on `OllamaMaxRateLimitPerMinute`.

- [x] **Task 1: Add Ollama constants and `Ollama()` factory (AC1, AC8, AC9)**
  - [x] Subtask 1.1: Add `public const string OllamaProviderName = "ollama";` immediately below `GoogleProviderName`.
  - [x] Subtask 1.2: Add `public const string OllamaModelName = "qwen3-embedding:4b";` immediately below `GoogleModelName`.
  - [x] Subtask 1.3: Add `private const int OllamaDimensions = 2560;` immediately below `GoogleMaxRateLimitPerMinute`. XML doc: `/// <summary>Default dimension count emitted by qwen3-embedding:4b on a self-hosted Ollama deployment.</summary>`.
  - [x] Subtask 1.4: Add `private const int OllamaMaxRateLimitPerMinute = 60_000;` immediately below `OllamaDimensions`. XML doc: `/// <summary>Self-hosted Ollama has no provider-side quota; this ceiling protects operator backend throughput from accidental misconfiguration.</summary>`.
  - [x] Subtask 1.5: Add the `Ollama()` factory method immediately below `Google()`. Match the property-init order, trailing-comma convention, and XML doc shape of `Google()` verbatim.

- [x] **Task 2: Replace provider validation with supported-provider list and extend assertions (AC2, AC3, AC4, AC5, AC6)**
  - [x] Subtask 2.1: Replace the hard-coded Google-only check at lines 76–81 with the `IsSupportedProvider(config.Provider)`-based check per §"What 13.1 adds" item 1. Update the error message to list both `GoogleProviderName` and `OllamaProviderName`.
  - [x] Subtask 2.2: Add the private `IsSupportedProvider(string)` helper near the other private members (above the `[GeneratedRegex]` partial methods). One-line expression-bodied implementation.
  - [x] Subtask 2.3: Extend the `ModelNamePattern` regex string from `^[A-Za-z0-9._-]+$` to `^[A-Za-z0-9.:_-]+$`. Single character added inside the character class. Verify build succeeds (the source generator regenerates the partial method body from the new attribute string).
  - [x] Subtask 2.4: Add the Ollama dimension assertion immediately below the existing Google dimension assertion at lines 95–101. Same shape, comparing `OllamaModelName` ⇔ `OllamaDimensions`.
  - [x] Subtask 2.5: Replace the rate-limit ceiling check at lines 108–113 with the per-provider-aware version per §"What 13.1 adds" item 1. Generalize the error message wording.

- [x] **Task 3: Add Ollama test coverage (AC10)**
  - [x] Subtask 3.1: Append `Ollama_ShouldReturnCorrectDefaults` test using Shouldly. Mirror `Google_ShouldReturnCorrectDefaults` shape.
  - [x] Subtask 3.2: Append `Validate_OllamaProvider_ShouldNotThrow`.
  - [x] Subtask 3.3: Append `Validate_OllamaWithEmptyModel_ShouldThrow` as a `[Theory]` with `[InlineData("")]` and `[InlineData("   ")]`.
  - [x] Subtask 3.4: Append `Validate_OllamaUnsupportedDimension_ShouldThrow` (uses `Dimensions = 768` to catch the cross-pollination case).
  - [x] Subtask 3.5: Append `Validate_OllamaWithModelColon_ShouldNotThrow` — the regex-extension proof test. **Do NOT skip this test.**
  - [x] Subtask 3.6: Append `Validate_OllamaModelWithUnsafeCharacters_ShouldThrow` as a `[Theory]` with `[InlineData("model/with/slash")]`, `[InlineData("model with space")]`, `[InlineData("model;semi")]`.
  - [x] Subtask 3.7: Append `Validate_UnsupportedProvider_ErrorMessageListsSupportedProviders` — assert message contains both `"google"` and `"ollama"`.
  - [x] Subtask 3.8: Append `Validate_OllamaRateLimitAtMaximum_ShouldNotThrow` and `Validate_OllamaRateLimitExceedsMaximum_ShouldThrow`.
  - [x] Subtask 3.9: Append `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` (uses `RateLimitPerMinute = 5000` with `Provider = "google"`). Confirms ceilings are per-provider.
  - [x] Subtask 3.10: Append `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` — combines `Provider = "ollama"`, `Model = "qwen3-embedding:4b"`, `Dimensions = 768` and expects throw.
  - [x] Subtask 3.11: Append `GetBreakingChangeFields_GoogleToOllama_ShouldReportProviderModelAndDimensions`.
  - [x] Subtask 3.12: Append `Validate_OllamaQwen3_AcceptsExactly2560` as a `[Theory]` with `[InlineData(2559)]`, `[InlineData(2561)]`, `[InlineData(768)]`, `[InlineData(1024)]`, `[InlineData(1536)]` — every case expects throw. **Pins the qwen3-embedding-2560 arm tamper-evident**: any future widening of this dimension assertion (e.g., when a third Ollama model is added with overlapping dim values) trips at least one negative case. Surfaced by Round 2 elicitation pre-mortem; mitigates the mutation Murat warned would survive the existing `Validate_OllamaUnsupportedDimension_ShouldThrow` (which only tested 768).

- [x] **Task 4: Final validation (AC2, AC10, AC11)**
  - [x] Subtask 4.1: Run focused: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"`. Expected: all existing 18 tests + the new ~13 tests green.
  - [x] Subtask 4.2: Run the full Server.Tests slice: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj`. Expected: existing baseline (1378/1382 per Story 9.2 Session 2 Dev Notes) preserved — the 4 known pre-existing failures (`IngestionInputValidatorTests.Validate_Event_WithNullBytes_Throws`, `DocumentationCompletenessTests`, 2× `ProvisionRedi*`) are NOT 13.1's responsibility.
  - [x] Subtask 4.3: Run the Contracts.Tests slice: `dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj`. Expected: green (no `TenantEmbeddingConfig` change in 13.1, so the contract suite should not regress).
  - [x] Subtask 4.4: Run `dotnet build` against the solution. Expected: 0 warnings, 0 errors.
  - [x] Subtask 4.5: `git diff --stat` and confirm exactly 2 files changed: `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` and `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`. **If any third file appears in the diff, STOP and reread AC11.**

## File Scope

**Allowed to modify:**

- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`

**Explicitly forbidden in this story:**

- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` — Story 13.4's scope. Adding OIDC fields here is a scope leak.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs` — Story 13.3's scope.
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` (does not yet exist) — Story 13.2's scope.
- `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs` — Story 13.5's scope.
- `src/Hexalith.Memories.Server/Program.cs` — none of the changes 13.1 makes require DI or composition-root edits.
- `src/Hexalith.Memories.Server/appsettings.json` and `appsettings.Development.json` — operational defaults stay as-is.
- `src/Hexalith.Memories.AppHost/Program.cs` — no env-var propagation needed for 13.1.
- `Directory.Packages.props` — no new package references needed.

If a `Scope-Override:` is required, document it in the commit message body and the story file's "Completion Notes" before merging.

## Dev Notes

### Architecture compliance

- **Decision D4 (Architecture §line 269 / 375 / 550):** the proposal updates D4 from "Google embedding only in MVP" → "Multi-provider from MVP, Ollama default, Google opt-in". Story 13.1 lands the FIRST piece of that decision: making the validation accept both providers. The default-provider-flip itself happens later (Story 13.4 ships the OIDC config fields, Story 13.5 ships the actor plumbing, then operators flip per-tenant via the provisioning workflow).
- **`EmbeddingProvider` field format (Architecture §line 114):** `{provider}:{model}` — e.g., `ollama:qwen3-embedding:4b`. Note the **double colon** (`ollama:` + `qwen3-embedding:4b`) → the `EmbeddingProvider` field stored on a memory unit will be `"ollama:qwen3-embedding:4b"`. This is **NOT** Story 13.1's concern (it lives in `EmbeddingClient` / the persistence layer), but if the dev sees it during cross-file reading, it is intentional and correct. The architecture line 114 confirms the format works as-is.
- **`TenantEmbeddingConfig` is the single source of truth for per-tenant embedding config (Architecture §line 105–135).** Story 13.1 does not modify the record; it only adds factory + validation logic that consumes it.
- **Tenant-level isolation guarantees (Epic 5 retrospective findings):** unchanged by Story 13.1. Validation is per-config and stateless; tenant-context is enforced upstream of `EmbeddingProviderDefaults` calls.

### Library / framework requirements

- **.NET 10.0** — target framework. Source-generated regexes (`[GeneratedRegex]`) are first-class.
- **xUnit + Shouldly** — existing test stack. Do NOT introduce FluentAssertions, NUnit, or Moq.
- **`System.Text.RegularExpressions`** — already imported in `EmbeddingProviderDefaults.cs`. No new `using` directives needed unless the `IsSupportedProvider` helper provokes one (it should not — `string.Equals` is in `System` which is implicitly imported).

### Anti-patterns to avoid

These have all bitten Hexalith.Memories stories before — surface them so the dev agent does not relive them.

1. **Do NOT introduce a `EmbeddingProviderName` enum.** The existing convention is string constants (`GoogleProviderName`). An enum-based shape is a much bigger change (record migration, JSON serialization, AOT context regen) and is out of scope for 13.1. Future stories could refactor — not this one.

2. **Do NOT collapse `Google()` and `Ollama()` into a single `Default(string providerName)` factory.** The dual-factory shape mirrors the operator-facing intent ("give me the canonical Google/Ollama config") and matches existing test seeding patterns. Collapsing them is a micro-DRY trap that produces less readable code.

3. **Do NOT log the model name as `LogInformation`.** Model names are not secret, but several stories upstream (especially Story 9.2 dev notes) caution against generic high-cardinality log fields. There is no `LogInformation` in `EmbeddingProviderDefaults` today; **do NOT introduce one** in this story.

4. **Do NOT add `[Obsolete]` on the existing `Google()` factory.** It remains a first-class supported configuration. Marking it obsolete would create build warnings across every existing call site (provisioning workflow, tests, fixtures) that 13.1 has no business touching.

5. **Do NOT relax the `ApiSecretKeyName` regex.** It is `^[a-z0-9-]+$` and stays that way. Story 13.4 will document that for OIDC mode this name keys the `client_secret` value rather than the API key, but the format constraint is unchanged. Existing tests like `Validate_InvalidApiSecretKeyNames_ShouldThrow` (line 84 of test file) MUST still throw on `"key with spaces"`, `"KEY_UPPER"`, etc.

6. **Do NOT add a "default provider" indicator field to `TenantEmbeddingConfig`.** The factory return value IS the default; there is no need for a `bool IsDefault` or similar marker.

7. **Do NOT short-circuit the validation when `Provider == "ollama"`.** Every existing check (model-name-not-blank, dimensions-positive, rate-limit-positive, secret-key-name-format) applies to Ollama too. The replacement only relaxes the **provider-name** acceptance, not the rest.

8. **Do NOT use `string.Compare(...) == 0` or `==` for case-insensitive provider comparison.** The convention is `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`. Match it.

### Testing requirements

- **Test framework:** xUnit + Shouldly (existing in `EmbeddingProviderDefaultsTests.cs`).
- **Coverage target:** 100% of the new branches added in `Validate(...)` (the per-provider rate-limit ceiling, the Ollama dimension assertion, the supported-provider list check). The existing test file already has near-complete branch coverage for the Google path; mirror the same density for Ollama.
- **Naming convention:** `{Method}_{Scenario}_{Expectation}` matching existing names (`Validate_DimensionsZero_ShouldThrow`, `Google_ShouldReturnCorrectDefaults`).
- **Theories:** prefer `[Theory]` + `[InlineData]` for parameterized scenarios (multiple invalid inputs, multiple valid dimensions). Match the existing pattern at lines 84–105 and 125–134.
- **Assertion style:** Shouldly only. `config.Provider.ShouldBe("ollama")`. `Should.NotThrow(() => ...)`. `Should.Throw<ArgumentException>(() => ...)`. The throw assertions can additionally inspect the message via `.Message.ShouldContain("...")` for AC3 (the supported-provider error message test).
- **No mocks.** `EmbeddingProviderDefaults` is pure. There is nothing to mock.
- **No async tests.** All methods are synchronous.

### Pre-impl verification — proposal-vs-code drift

The Sprint Change Proposal contains two pieces of stale wording the dev agent MUST NOT interpret as a pending code change:

1. **Google model name drift:** Proposal §2.3 OLD AC text references `text-embedding-004` (Story 1.4 epic AC text). The actual code at `EmbeddingProviderDefaults.cs:19` runs `gemini-embedding-001` and has done so since Story 1.4 final close-out. **Do NOT change `GoogleModelName` to `text-embedding-004`.** The proposal's NEW AC text (which 13.1 honors) is provider-agnostic and says "the configured provider's embedding endpoint and returns a vector matching the configured dimensions" — i.e., the code is fine.

2. **Default rate limit drift:** Proposal §4.1 NEW table lists Google "1500 req/min" — matches the existing `Google()` factory default. The 3000 req/min ceiling is the per-provider HARD MAX for Google. Both numbers are correct in the existing code; do not "harmonize" them.

### Roundtable review notes (2026-04-29)

After the initial story landed at status `ready-for-dev`, Jerome ran a four-agent BMAD roundtable review (Amelia, Winston, Murat, John).

**Round 1 findings (initially absorbed):**

1. **`ParseProvider` canonical splitter (Winston's catch, accepted by Amelia, mutation-validated by Murat).** The persisted `EmbeddingProvider` field uses `{provider}:{model}` format — for Ollama this becomes `ollama:qwen3-embedding:4b` (TWO colons). Any naive `field.Split(':')[1]` silently drops the `:4b` tag and corrupts the model name across every downstream consumer (Stories 13.2 / 13.3 / 13.6 each parse this field). Risk: silent data corruption in production. **Status after Round 2: ROLLED BACK from 13.1. See Round 2 finding #2 below — moved to Story 13.3 with explicit contract obligation.**

2. **Dim-assertion pattern is a switch-statement-in-disguise (Amelia + Winston converged).** Current shape: `model == GoogleModelName ⇒ {768,1536,3072}`, `model == OllamaModelName ⇒ 2560`. Adds an arm per future Ollama model. Not refactored in 13.1 (out of scope; would expand the diff and threaten AC11). **Tracked debt: when a third Ollama model is added (e.g., `nomic-embed-text` 768 or `mxbai-embed-large` 1024), the next story owner MUST refactor to a model→dimensions dictionary populated at registration time.** Surfaced explicitly so it doesn't become silently accumulated debt.

3. **John (PM) challenged the existence of 13.1 as a separate story from 13.4.** Winston defended the boundary on blast-radius grounds (stateless validation vs. domain-entity migration). **Decision after Round 2: question re-opened — see Round 2 finding #5 below.**

**Round 1 findings deferred (not adopted into 13.1):**
- Murat's request for an FsCheck property test on the regex character class — accepted as good practice but deferred; the `[InlineData]` cases are sufficient for the foundation story and Story 13.7 (integration tests) is the better home for property-based coverage.
- Murat's request to fix the 4 known baseline failures — out of scope; tracked under existing deferred-work entries.
- ADR D4 update — Winston flagged that `_bmad-output/planning-artifacts/architecture.md` line 269 / 375 / 550 still says "Google embedding only in MVP." Out of 13.1's file scope. Tracked as a pre-Story-13.4 micro-task: amend D4 inline with 13.4's planning-artifact pass.

### Round 2 elicitation findings (2026-04-29)

After Round 1's absorption pass, Jerome ran `/bmad-advanced-elicitation` with all 5 selected methods (Pre-mortem, Devil's Advocate, Meta-Prompting, First Principles, Reverse Engineering). Outcomes:

1. **Pre-mortem analysis (risk-lens; ABSORBED INTO STORY).** A future scenario was constructed: 3 months post-ship, a third Ollama model (`nomic-embed-text` 768) gets added to `Validate()` as a new switch arm; the developer accidentally widens the existing `qwen3-embedding:4b` arm to accept 768 OR 2560 alongside the new arm; the existing dim-assertion test (which only checks 768) doesn't fire because the mutation now also accepts 768. Tenant data corrupts silently. Mitigation: a tamper-evident pinning test added as Subtask 3.12 — `Validate_OllamaQwen3_AcceptsExactly2560` with `[Theory]` over five negative dim values (2559, 2561, 768, 1024, 1536). Any future widening of the qwen3-embedding-2560 arm trips at least one case.

2. **Devil's Advocate against AC12 absorption (CHANGED PRIOR DECISION; ROLLED BACK).** The Round 1 absorption of `ParseProvider` into 13.1 was reconsidered. The case against: (a) three agents converging is a smell — Winston framed the risk dramatically, Amelia accepted under that framing, Murat validated test-set without questioning placement; (b) AC11 was designed to resist "while we're here" scope additions backed by hypothetical risk; (c) Round 1's argument "stays in the same 2 files so AC11 is preserved" honored AC11's letter but violated its spirit; (d) the right home for the parser is 13.3 with a mandatory contract test, not 13.1 with a defensive utility. **Action: AC12 removed, Task 1 (ParseProvider TDD) removed, effort estimate reverted to 0.75–1.0 day, story shrunk back to ~390 lines. Story 13.3 inherits the obligation — its story file, when written, MUST include a `ParseProvider`-equivalent contract test that pins `("ollama:qwen3-embedding:4b", "ollama", "qwen3-embedding:4b")` — i.e., the model name preserves the intra-model colon. The threat model Winston identified is REAL; only the placement was wrong.**

3. **Meta-Prompting analysis (NOT ABSORBED — escalation to Story 12.3).** The story file is 6× larger than the artifact it produces (~390 lines for ~1 day of work, ~1k tokens of code). The shape optimizes for "no LLM dev agent could possibly stumble" rather than "minimum sufficient context." The right fix is not "make every Epic 13 story shorter" but "ship Story 12.3 (file-scope CI check, currently `backlog`) so that the defensive sections in story files become enforceable as automated guardrails instead of textual warnings." **Open question for Jerome: should Story 12.3 be sequenced ahead of further Epic 13 work?** Every Epic 13 story will pay the same compound documentation tax until 12.3 ships. Not a story-13.1 edit; surfaced for Epic-level decision.

4. **First Principles Analysis (ABSORBED AS TRACKED DEBT).** Stripping inherited assumptions from Story 1.7's MVP-Google-only premise: the static class is a registry pretending to be a utility, the string-typed `Provider` is overhead today (no runtime extension is actually wired), the factory pattern is over-engineered for the present need (a `For(providerName)` parameter-driven API would be smaller), and dimensions are a model property, not a provider property (the switch should be a `IDictionary<string, int[]>`). 13.1 inherits these constraints for blast-radius reasons but they are tracked debt. **Re-open trigger: when a third Ollama model is added OR when per-tenant DI override is required — at that point, an "Embedding registry refactor" story should be scoped before extending the existing class.**

5. **Reverse Engineering (NOT ABSORBED — re-opens Epic 13 shape question).** Walking backwards from Epic 13's end-state (Ollama tenant ingests + searches end-to-end), the consumption graph reveals tight coupling: 13.1+13.4+13.5 all touch the config layer; 13.2's only consumer is 13.3. A 4-story shape (Multi-provider config / OIDC HTTP dispatch / Migration / Tests+Docs) is defensible alongside the 7-story shape (smaller blast radius per merge). Reverse engineering does not pick a winner — both shapes have merit. The decision criterion: confidence in 13.4's schema migration risk-free-ness. **Open question for Jerome: collapse 13.1+13.4+13.5 into one config-layer story, or keep separate?** Not a story-13.1 edit; surfaced for Epic-level decision.

**Net effect of Round 2 on Story 13.1:**
- AC count: 12 → 11 (AC12 removed).
- Task count: 5 → 4 (Task 1 ParseProvider TDD removed; renumbered).
- Subtask count: Task 3 gains Subtask 3.12 (Pre-mortem dim-pinning test).
- Effort estimate: 0.85–1.1 day → 0.75–1.0 day.
- Tracked debt: registry-refactor and ParseProvider-belongs-to-13.3 noted.
- Open Epic-level questions: Story 12.3 sequencing, 13.1+13.4+13.5 collapse — both surfaced but unresolved (intentionally outside 13.1's scope).

### Why this story is small

Story 13.1 looks like a "trivial" add-a-string change. It is not: getting the **exact set of edits** right is what unblocks 13.2/13.3/13.4 cleanly. The disasters this story is sized to prevent are:

- **The colon-in-model-name regex trap.** A future dev story (13.4 or 13.5) would silently fail validation when reading an Ollama tenant config from actor state, with a confusing "Model must contain only letters, numbers, dots, underscores, and hyphens" error. Catching it now at the foundation layer is one regex character; catching it later is a multi-store debugging session.

- **The cross-pollination dimension trap.** Without AC5 / AC10 dimension-mismatch tests, a copy-paste between Google and Ollama configs would persist a 768-dim vector index for a 2560-dim model, and Redis Vector Search would silently truncate or reject. That fails Story 1.5 (three-backend indexing) invariants and corrupts ingestion. Cheap to catch at the validation layer.

- **The supported-provider error-message trap.** Without AC3 / AC10 enumeration-of-supported-providers test, future provider additions (openai, mistral) would extend `IsSupportedProvider` but forget to update the error message, so users see "supported providers: google" while submitting `"openai"` and getting rejected — a deeply confusing UX bug.

Spend the small budget here so the rest of Epic 13 is debt-free.

### References

- **Source code (UPDATE files for 13.1):**
  - `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` (full file read; lines 14–128 are all relevant)
  - `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` (full file read; 18 existing tests at lines 14–179)

- **Source code (READ-ONLY context, NOT to be edited in 13.1):**
  - `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` (target record — properties unchanged in 13.1)

- **Planning artifacts:**
  - `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-29.md` §1 (issue summary), §2.3 (Story 1.4 / 1.7 AC amendments — informational; not in 13.1 scope), §2.4 (Memories Server file changes — 13.1 only owns the `EmbeddingProviderDefaults.cs` line item), §4.4 (Story 13.1 epic-level summary)
  - `_bmad-output/planning-artifacts/epics.md` Epic 13 §"Story 13.1" (acceptance criteria — copied and expanded above)
  - `_bmad-output/planning-artifacts/prd.md` §"Embedding Provider Configuration" (lines 675–691 per the proposal — read for context, not edited in 13.1)
  - `_bmad-output/planning-artifacts/architecture.md` line 114 (`EmbeddingProvider` field format), lines 269 / 375 / 550 (Decision D4)

- **Sprint status:**
  - `_bmad-output/implementation-artifacts/sprint-status.yaml` (Epic 13 added 2026-04-29; Story 13.1 status flips backlog → ready-for-dev when this file lands)

- **Adjacent stories for cross-context:**
  - `_bmad-output/implementation-artifacts/1-4-embedding-generation.md` (the original Google-only embedding story; documents the `EmbeddingClient` shape that 13.3 will extend)
  - `_bmad-output/implementation-artifacts/1-7-embedding-provider-configuration.md` (the `IEmbeddingProvider` abstraction commitment that 13.1 honors)
  - `_bmad-output/implementation-artifacts/5-1-tenant-provisioning-workflow.md` (the workflow that calls `EmbeddingProviderDefaults` factories — read to confirm 13.1's `Ollama()` factory shape lines up)

- **Retrospectives (lessons applied):**
  - `_bmad-output/implementation-artifacts/epic-1-retro-2026-04-27.md` (epic closeout — confirms `gemini-embedding-001` is the live Google model name; no pending migration)
  - `_bmad-output/implementation-artifacts/epic-5-retro-2026-04-29.md` (tenant isolation findings — none impact 13.1, but confirm `TenantEmbeddingConfig` round-trip patterns)

### Project Structure Notes

- The change is a **strict additive extension** of an existing static partial class. No new files. No new namespaces. No new project references. No new `using` directives expected (verify with the dev agent's IDE — if any new `using` appears, it is a sign of unintentional API surface change).
- Naming alignment: `OllamaProviderName` / `OllamaModelName` mirror `GoogleProviderName` / `GoogleModelName`. The factory `Ollama()` mirrors `Google()`. The constant `OllamaMaxRateLimitPerMinute` mirrors `GoogleMaxRateLimitPerMinute`. No naming creativity required — and if the dev agent feels creative, that is a code smell for this story.

## Project Context Reference

There is no `project-context.md` file under `_bmad-output/` or the project root at the time this story was authored — the BMad `persistent_facts` glob `{project-root}/**/project-context.md` resolved to zero files. If/when one is added in the future, the dev agent should read it before starting Task 0. As a substitute, this story file pins all the project-specific conventions inline (anti-patterns, test framework, naming, library choices, file scope discipline) that a project context would normally surface.

## Dev Agent Record

### Agent Model Used

claude-opus-4-7 (1M context).

### Debug Log References

- Build environment note: `global.json` pins SDK `10.0.201` with `rollForward: latestFeature`; only `10.0.102` and `10.0.103` were installed locally, so `dotnet` is invoked from `/tmp` to bypass the project-rooted `global.json` resolution and consume `10.0.103`. The 13.1 diff does not modify `global.json` (AC11 file scope discipline preserved).
- Submodules `src/submodules/Hexalith.Commons` and `src/submodules/Hexalith.EventStore` were uninitialized at session start; ran `git submodule update --init --recursive` before invoking the build. The recursive clone surfaces a known nested self-referencing path inside `Hexalith.EventStore` (`Hexalith.EventStore/Hexalith.Tenants/...` repeating) — top-level submodules clone correctly and the build/test pipeline is unaffected.

### Completion Notes List

- ✅ All 11 ACs satisfied. Implementation is a strictly additive extension of the existing `static partial class EmbeddingProviderDefaults` plus appended Ollama tests in `EmbeddingProviderDefaultsTests`.
- ✅ Two-file diff confirmed via `git diff --stat`: `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` (+47/-7) and `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` (+129/-0). `TenantEmbeddingConfig.cs` is **not** touched (Story 13.4 scope), `EmbeddingClient.cs` is not touched (13.3), `OidcTokenProvider.cs` is not introduced (13.2).
- ✅ Source-generator pattern preserved: class remains `public static partial class`, both `[GeneratedRegex]` partial methods compile cleanly with the regex-string change. No `new Regex(...)` introduced. No new `using` directives required.
- ✅ Convention compliance: every throw uses `ArgumentException` with `nameof(config.{field})` parameter name; new constants grouped Google-then-Ollama; `string.Equals(..., StringComparison.OrdinalIgnoreCase)` used for all provider/model comparisons; XML-doc shape mirrors existing one-line `<summary>`s.
- ✅ Test results captured at implementation time:
  - **Focused** (`--filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"`): **48/48 green** (26 existing + 22 new — the 12 added test methods include 3 theories that expand to 2/3/5 InlineData cases).
  - **Full Server.Tests slice**: **1542/1542 green** — substantially better than the 1378/1382 baseline cited by Story 9.2 Session 2 Dev Notes (intermediate stories cleared the 4 known pre-existing failures and grew the suite by ~160 tests). No regressions introduced.
  - **Contracts.Tests slice**: **468/468 green** (expected — `TenantEmbeddingConfig.cs` unchanged in 13.1).
  - **Solution build** (`dotnet build Hexalith.Memories.slnx`): **0 warnings, 0 errors** across all 18 projects.
- ✅ Round 2 pre-mortem mitigation in place: `Validate_OllamaQwen3_AcceptsExactly2560` is implemented as a `[Theory]` with `InlineData(2559, 2561, 768, 1024, 1536)` — every case expects `ArgumentException`. Any future widening of the qwen3-embedding-2560 arm trips at least one negative case, satisfying the tamper-evident pin Subtask 3.12 mandates.
- ✅ Tracked debt items from Round 1/2 elicitation are intentionally NOT addressed in this story (per AC11 + the Round 2 rollback decision):
  - `ParseProvider` canonical splitter for the persisted `{provider}:{model}` field carrying a `:` inside `{model}` → owned by Story 13.3 with the contract test `("ollama:qwen3-embedding:4b", "ollama", "qwen3-embedding:4b")`.
  - Switch-statement-shaped dim-assertion → re-open trigger when a third Ollama model lands; refactor to `IDictionary<string, int[]>` then.
  - Story 12.3 (file-scope CI check, currently `backlog`) → escalates Meta-Prompting recommendation outside 13.1's scope.

### File List

- **Modified:** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- **Modified:** `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`

### Change Log

| Date       | Change                                                                                                                                                                  | Author |
|------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|
| 2026-04-30 | Story 13.1 implementation complete: added `OllamaProviderName`/`OllamaModelName` constants, private `OllamaDimensions`/`OllamaMaxRateLimitPerMinute` constants, `Ollama()` factory, `IsSupportedProvider` helper, extended `ModelNamePattern` regex to admit colon, added Ollama-specific dimension assertion, partitioned rate-limit ceiling per provider; appended 12 new test methods (22 cases incl. theory expansion) — focused 48/48, Server.Tests 1542/1542, Contracts.Tests 468/468, build 0W/0E. Status: ready-for-dev → review. | Amelia (claude-opus-4-7) |
| 2026-05-02 | Code review (3-layer adversarial) complete. Auditor verdict: PASS on all 11 ACs. 24 raw findings → 16 unique after dedup → 1 decision-needed (HIGH, tolerant-defaults pattern bundle), 0 patches, 10 deferred (tracked debt 13.1-RV1..RV10), 5 dismissed (noise / false positives). See `### Review Findings` section below. | claude-opus-4-7 |

### Review Findings

**Triage:** 1 decision-needed, 0 patches, 10 deferred, 5 dismissed. Layers: Blind Hunter (10 raw), Edge Case Hunter (13 raw), Acceptance Auditor (1 spec-mismatch nit + PASS verdict on every AC). Implementation is spec-faithful. Findings cluster around (a) spec-level scope questions tracked for Story 13.4, and (b) pre-existing patterns that predate 13.1.

#### Decision needed

- [ ] **[Review][Decision] D1 — Tolerant defaults pattern in `Validate(...)` lets cross-pollinated configs slip through.** The dim assertion is keyed on model name only (not provider+model), and `ModelNamePattern` (`^[A-Za-z0-9.:_-]+$`) requires no alphanumeric. Concretely, the validator currently accepts: (a) `Provider="google", Model="qwen3-embedding:4b", Dimensions=2560` (Google provider with Ollama model + Ollama dim); (b) `Provider="ollama", Model="gemini-embedding-001", Dimensions=768` (Ollama provider with Google model + Google dim); (c) `Provider="ollama", Model="totally-fake", Dimensions=1` (arbitrary positive dim with unknown model); (d) `Model=":::"` or `Model="-"` (regex passes pure-punctuation). Sources: Blind B2/B4 + Edge E1/E2/E3/E7/E8 (bundled per `feedback_tolerance_idioms.md` — single review category, not isolated bugs). Implementation matches spec verbatim — this is a spec-scope question, not an implementation defect. **Options:** **(a)** extend 13.1 scope to enforce provider→{model-allowlist}→{dim-allowlist} coupling and tighten regex to require ≥1 alphanumeric. *Cost:* explicitly violates AC11 file-scope discipline; touches validation logic that 13.4 owns. **(b)** defer to Story 13.4 as `13.1-RV11` tracked debt — TenantEmbeddingConfig will gain OIDC fields there, so the validator will already need a re-pass. *Cost:* downstream stories 13.2/13.3 will write tests against the looser current contract. **(c)** split a new micro-story 13.1.1 to harden Validate before 13.2/13.3 land. *Cost:* adds a sequencing dependency to the Epic 13 critical path.

#### Deferred (tracked debt — added to `_bmad-output/implementation-artifacts/deferred-work.md`)

- [x] **[Review][Defer] 13.1-RV1 — `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` test name vs body.** Test uses `RateLimitPerMinute=5000`, ABOVE Google's 3000 ceiling but BELOW Ollama's 60_000 ceiling. Name is internally inconsistent with the value — the test correctly verifies per-provider partitioning; the name is misleading. Spec-mandated; rename should accompany the next provider addition. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272`)
- [x] **[Review][Defer] 13.1-RV2 — `Validate_OllamaQwen3_AcceptsExactly2560` named "accepts" but asserts "rejects".** Every `[InlineData]` value (2559, 2561, 768, 1024, 1536) expects throw; no positive case covers 2560 except via the default factory. Spec-mandated name (Subtask 3.12). Suggested cleanup: rename to `Validate_OllamaQwen3_RejectsAnyDimensionExcept2560` and add explicit `[InlineData(2560)] => ShouldNotThrow` companion. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307`)
- [x] **[Review][Defer] 13.1-RV3 — `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` body uses Ollama model, not Google.** Test name says "GoogleModel"; body uses `Model="qwen3-embedding:4b"`. Dev followed spec body verbatim — spec body itself is internally inconsistent with the test name. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284`)
- [x] **[Review][Defer] 13.1-RV4 — Provider whitespace UX gap.** `Validate(... with { Provider = " ollama" })` throws `Provider ' ollama' is not supported. Supported providers: 'google', 'ollama'.` — technically correct but unhelpfully obscures the leading-whitespace cause. No security risk. Trim before comparing or surface a "leading/trailing whitespace?" hint. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)
- [x] **[Review][Defer] 13.1-RV5 — Per-provider rate-limit ternary fragile for future providers.** `int maxRateLimit = provider == ollama ? 60_000 : 3_000` silently uses Google's ceiling for any unknown provider added through `IsSupportedProvider`. Refactor to `IDictionary<string,int>` ceiling lookup at the same maintenance pass that introduces the per-model dim registry (Round 1 finding §2 / spec "When a third Ollama model is added"). (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145`)
- [x] **[Review][Defer] 13.1-RV6 — `Dimensions = int.MaxValue` accepted (only `<=0` rejected).** Pre-existing gap — the `<=0` lower-bound predates 13.1; no upper bound exists for unknown models. A 2.1B-dim vector would 404 at the index store rather than failing at config-time. Cap at a shared upper bound (e.g., 16_384) when the embedding registry refactor lands. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117`)
- [x] **[Review][Defer] 13.1-RV7 — `GetBreakingChangeFields` case-sensitivity contract not pinned.** Pre-13.1 `GetBreakingChangeFields` uses `OrdinalIgnoreCase` for Provider/Model — a regression flipping to ordinal would silently report a casing-only delta as a breaking change. No test pins this. Pre-existing. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67`)
- [x] **[Review][Defer] 13.1-RV8 — No null-config test for `Validate(null!)`.** `ArgumentNullException.ThrowIfNull(config)` is at the top of `Validate` but no test pins the contract. Pre-existing pattern. Cheap to add at next maintenance pass. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`)
- [x] **[Review][Defer] 13.1-RV9 — Default Ollama RateLimit (6000) vs ceiling (60_000) divergence undocumented at call-site.** Spec rationale exists ("100 req/sec sustained — comfortable headroom on a single self-hosted Ollama node") but is not echoed at the factory; the constant doc on `OllamaMaxRateLimitPerMinute` only documents the ceiling. Add an inline XML comment at the `Ollama()` factory's `RateLimitPerMinute = 6000` line when 13.5 wires the actor surface. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57`)
- [x] **[Review][Defer] 13.1-RV10 — Mixed-case provider/model strings persisted verbatim.** `OrdinalIgnoreCase` matching but no normalization of stored values. A tenant config persisting `Provider="Ollama"` survives validation; a downstream comparator using ordinal equality (e.g., the `EmbeddingProvider` `{provider}:{model}` parser owed by Story 13.3) would silently disagree. Story 13.3's `ParseProvider` contract test is the natural enforcement point. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)

#### Dismissed (noise / false positive)

- ~~`ApiSecretKeyName "memories-embedding-client-secret"` reused for self-hosted Ollama is suspicious / risks credential cross-wiring.~~ Spec context: the Hexalith Ollama deployment is **gated by Keycloak OIDC client_credentials** (Story 13.2). The secret name keys the OIDC `client_secret`, not a vanilla-Ollama API key. Spec deliberately chose OIDC-flow naming. Per-provider scoping would conflict with the OIDC-gateway abstraction. (Sources: Blind B5, Edge E11)
- ~~`GetBreakingChangeFields_GoogleToOllama` ordered comparison is risky if implementation uses unordered enumeration.~~ Implementation uses sequential field-by-field append (verified read of pre-13.1 source); order is stable. AC7 explicitly mandates ordered output. (Source: Blind B8)
- ~~Public `Ollama()` factory introduced but no callers shown — risk of orphaned API.~~ Intentional: provisioning workflow consumer is wired in Story 13.5. `EmbeddingProviderDefaults` is `static partial class`, so DI is not the integration point. Spec §"What already exists" item 6 is explicit. (Source: Blind B10)
- ~~No `Validate_OllamaRateLimit_ZeroOrNegative` test for the new provider path.~~ Lower-bound check `<=0` is shared (not per-provider) and already exercised by `Validate_RateLimitZero_ShouldThrow`. Same pattern. (Source: Blind B9)
- ~~Case-sensitive vs case-insensitive comparison risk for downstream consumers.~~ Subsumed by 13.1-RV10 (which captures the persistence-vs-comparison gap with the correct enforcement point). (Source: Blind B3)
