# Story 1.2: Memory Unit Domain Model & Contracts

Status: done

## Story

As a developer,
I want a well-defined domain model for memory units, graph edges, metadata fields, and ingestion types in `Contracts.V1`,
So that all services share a consistent, versioned type system with serialization guarantees.

## Acceptance Criteria

1. **Given** the `Hexalith.Memories.Contracts.V1` namespace exists
   **When** I inspect the memory unit model
   **Then** it contains all required fields: Id (ULID string), TenantId, CaseId, Content, ContentHash (SHA-256), SourceUri, SourceType (enum: file, url, event, command, projection, discussion), IngestedBy, IngestedAt, LastUpdated, Status (enum: queued, extracting, embedding, indexing, indexed, failed), Metadata (Dictionary<string, MetadataField>), EmbeddingProvider, EmbeddingDimensions, Classification (optional), FailureDetails (optional)
   **And** MetadataField contains: Value, Origin (enum: human, ai), Confidence (float 0.0-1.0)

2. **Given** the graph edge model exists
   **When** I inspect it
   **Then** it contains: Id (ULID string), SourceId, TargetId, EdgeType (enum: caused_by, correlated_with, references, contains, annotates), Confidence (float), Origin (enum: explicit, inferred), CreatedAt
   **And** default confidence values per edge type are defined as constants (caused_by=1.0, correlated_with=0.8, references=0.5, contains=1.0, annotates=1.0)

3. **Given** the error format is defined
   **When** I inspect ErrorResponse
   **Then** it contains: Code (string), Message (string), Suggestion (string)
   **And** JSON output matches: `{"code": "TENANT_NOT_FOUND", "message": "...", "suggestion": "Run 'memories tenant list'..."}`

4. **Given** any Contracts.V1 type (MemoryUnit, GraphEdge, MetadataField, ErrorResponse, FailureDetails)
   **When** I serialize it to JSON with `System.Text.Json` camelCase policy and deserialize it back
   **Then** the round-trip produces an identical object (contract tests pass)

## Tasks / Subtasks

- [x] Task 1: Create V1 enum types (AC: #1, #2)
    - [x] 1.1 Create `V1/SourceType.cs` — enum: File, Url, Event, Command, Projection, Discussion
    - [x] 1.2 Create `V1/MemoryUnitStatus.cs` — enum: Queued, Extracting, Embedding, Indexing, Indexed, Failed
    - [x] 1.3 Create `V1/EdgeType.cs` — enum: CausedBy, CorrelatedWith, References, Contains, Annotates
    - [x] 1.4 Create `V1/MetadataOrigin.cs` — enum: Human, Ai
    - [x] 1.5 Create `V1/EdgeOrigin.cs` — enum: Explicit, Inferred
    - [x] 1.6 Apply `[JsonConverter(typeof(JsonStringEnumConverter<EnumType>))]` attribute on EACH enum definition with `JsonNamingPolicy.CamelCase` — per-enum attributes are the chosen approach (no global converter object needed)

- [x] Task 2: Create V1 value types (AC: #1, #2, #3)
    - [x] 2.1 Create `V1/MetadataField.cs` — sealed record: Value (string), Origin (MetadataOrigin), Confidence (float). Value is pinned to `string` for MVP serialization safety — any structured value can be JSON-encoded as a string. Phase 2 may widen to `JsonElement` for rich typed metadata.
    - [x] 2.2 Create `V1/FailureDetails.cs` — sealed record: Stage (string), ErrorCode (string), RetryCount (int)
    - [x] 2.3 Create `V1/ErrorResponse.cs` — sealed record: Code (string), Message (string), Suggestion (string). Suggestion MUST contain an actionable CLI command or URL (e.g., `"Run 'memories tenant list' to see available tenants"`) — never generic advice like `"Please try again"`.

- [x] Task 3: Create V1 core domain models (AC: #1, #2)
    - [x] 3.1 Create `V1/MemoryUnit.cs` — sealed record with ALL fields per AC #1
    - [x] 3.2 Create `V1/GraphEdge.cs` — sealed record with ALL fields per AC #2
    - [x] 3.3 Create `V1/EdgeTypeDefaults.cs` — static class with default confidence constants per EdgeType

- [x] Task 4: JSON serialization configuration (AC: #3, #4)
    - [x] 4.1 Create `V1/MemoriesJsonContext.cs` — static class with a `public static JsonSerializerOptions Options` property configured with `JsonNamingPolicy.CamelCase`. This is a plain static class, NOT a `JsonSerializerContext` source generator. Enum converters are on the enum definitions themselves (per-enum `[JsonConverter]` attributes), NOT in the shared options. All downstream projects MUST use this single options instance.
    - [x] 4.2 Verify ErrorResponse serializes as `{"code": "...", "message": "...", "suggestion": "..."}`

- [x] Task 5: Contract round-trip serialization tests (AC: #4) **MUST**
    - [x] 5.1 Create `tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitSerializationTests.cs` — **IMPORTANT:** MemoryUnit contains a `Dictionary` field. Record equality uses reference comparison for dictionaries, so `deserialized.ShouldBe(original)` will FAIL when Metadata has entries. Instead, compare via re-serialized JSON: serialize original → json1, deserialize → re-serialize → json2, assert `json1.ShouldBe(json2)`. For simple records (MetadataField, ErrorResponse, FailureDetails), `ShouldBe` works fine.
    - [x] 5.2 Create `tests/Hexalith.Memories.Contracts.Tests/V1/GraphEdgeSerializationTests.cs`
    - [x] 5.3 Create `tests/Hexalith.Memories.Contracts.Tests/V1/MetadataFieldSerializationTests.cs`
    - [x] 5.4 Create `tests/Hexalith.Memories.Contracts.Tests/V1/ErrorResponseSerializationTests.cs`
    - [x] 5.5 Create `tests/Hexalith.Memories.Contracts.Tests/V1/FailureDetailsSerializationTests.cs`
    - [x] 5.6 Test enum serialization: verify ALL enum values round-trip as camelCase strings (not integers) — test FIRST and LAST values of each enum (e.g., `SourceType.File` AND `SourceType.Discussion`)
    - [x] 5.7 Test nullable fields: verify Classification=null and FailureDetails=null serialize as `null` (not omitted) and deserialize back correctly
    - [x] 5.8 Test empty Metadata dictionary: verify `new Dictionary<string, MetadataField>()` serializes as `{}` and round-trips correctly
    - [x] 5.9 Test Confidence boundary values: verify `0.0f` and `1.0f` round-trip exactly
    - [x] 5.10 Test DateTimeOffset round-trip: verify offset is preserved (not just the UTC instant) — ISO 8601 format with timezone offset
    - [x] 5.11 Test Metadata null resilience: deserialize JSON with `"metadata": null` — verify it produces empty dictionary `[]` (not null) thanks to the default initializer

- [x] Task 6: Build and verify (AC: #1, #2, #3, #4) **MUST**
    - [x] 6.1 Run `dotnet build` — zero warnings, zero errors
    - [x] 6.2 Run `dotnet test` — all tests pass (existing MemoriesInfo test + all new V1 tests)
    - [x] 6.3 Verify existing `MemoriesInfo` class in root namespace is preserved — leave `Placeholder.cs` as-is, do NOT rename it (existing test depends on it working unchanged)

### Review Findings

- [x] \[Review]\[Patch] Enforce camelCase enum wire values and reject integer enum tokens across the Story 1.2 contract surface [src/Hexalith.Memories.Contracts/V1/SourceType.cs:6]
- [x] \[Review]\[Patch] Split unrelated Story 1.3/runtime/doc changes out of the Story 1.2 batch before closing this story [_bmad-output/implementation-artifacts/1-3-content-extraction-via-kreuzberg.md:1]

- [x] \[Review]\[Patch] Store a persistent default empty dictionary for `MemoryUnit.Metadata` and add coverage for the omitted-property path [src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs:25]
- [x] \[Review]\[Patch] Use a collision-free, type-agnostic state-store probe in `DaprStateStoreHealthCheck` [src/Hexalith.Memories.Server/HealthChecks/DaprStateStoreHealthCheck.cs:20]
- [x] \[Review]\[Patch] Validate public `ContentExtractionClient.Extract` inputs at the boundary [src/Hexalith.Memories.Server/Ingestion/ContentExtractionClient.cs:14]

## Definition of Done

1. All V1 types compile with zero warnings under `TreatWarningsAsErrors`
2. All serialization round-trip tests pass
3. Enums serialize as camelCase strings, not integers
4. Nullable fields (Classification, FailureDetails) handled correctly
5. `dotnet build` and `dotnet test` pass across the entire solution

## Dev Notes

### Architecture Compliance

- **Namespace:** All types under `Hexalith.Memories.Contracts.V1` (versioned namespace per Decision D14)
- **Type pattern:** `sealed record` for all domain model types — immutable, value equality, JSON-friendly
- **Enum serialization:** Use per-enum `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` attributes with `JsonNamingPolicy.CamelCase` so enums serialize as `"causedBy"` not `0`. Per-enum attributes are preferred over global converter — simpler, no shared options object to pass around
- **File-scoped namespaces:** `namespace Hexalith.Memories.Contracts.V1;` (Allman braces, per .editorconfig)
- **No external NuGet dependencies:** All types are plain .NET records using only `System.Text.Json` (built-in)
- **Package management:** Do NOT add version numbers to `.csproj` — use `Directory.Packages.props` if new packages needed
- **Preserve existing code:** Keep `Placeholder.cs` as-is in root `Hexalith.Memories.Contracts` namespace — do NOT rename it

### Critical Architectural Constraints

1. **Contracts are the dependency root** — Every other project (Server, Redis, Tests) depends on Contracts. Keep it lean with ZERO external NuGet dependencies beyond the framework.
2. **`Id` fields are string (ULID format)** — ULIDs are generated at the application layer (using `UniqueIdHelper.GenerateSortableUniqueStringId()` from Hexalith.Commons), NOT in the Contracts project. Contracts only declares the field type as `string`.
3. **camelCase JSON everywhere** — `System.Text.Json` with `JsonNamingPolicy.CamelCase`. This matches Redis index field naming, DAPR payload format, and Python interoperability (Decision D22). All downstream projects MUST use `MemoriesJsonContext.Options` for serialization — never create ad-hoc `JsonSerializerOptions`.
4. **Result pattern for domain errors** (Decision D18) — `ErrorResponse` is for API responses. Domain logic uses `DomainResult` from EventStore (separate from this story). Don't conflate the two.
5. **No abstract interfaces** (Decision D9) — Concrete `sealed record` types only. Extract interfaces when a second implementation arrives, not before.
6. **Classification field: schema-present, not enforced** — Include the `Classification` property as `string?` (nullable). It exists in the schema for Phase 4 LLM redaction. Do NOT add validation or logic around it in MVP.
7. **Namespace enforcement** — Every `.cs` file inside the `V1/` folder MUST use `namespace Hexalith.Memories.Contracts.V1;`. No exceptions. A type in the wrong namespace breaks the versioning strategy silently.
8. **Contracts are pure data carriers** — ZERO computed properties, ZERO constructor logic, ZERO method bodies. All fields are caller-supplied. ContentHash is computed by the caller (Server layer), not by the record itself. Same for Id (ULID).
9. **MemoryUnit represents the completed indexed unit** — All `required` fields are populated by the time a MemoryUnit is constructed at the end of the ingestion pipeline. Intermediate pipeline state (e.g., queued with no content yet) lives in DAPR Workflow activity inputs/outputs, NOT as MemoryUnit records. This is why `Content` and `ContentHash` are `required` — they're always present in a fully constructed MemoryUnit.

### MemoryUnit Field Inventory

| Field               | Type                              | Required | Notes                                                      |
| ------------------- | --------------------------------- | -------- | ---------------------------------------------------------- |
| Id                  | string                            | Yes      | ULID format, time-sortable                                 |
| TenantId            | string                            | Yes      | Physical index routing key                                 |
| CaseId              | string                            | Yes      | Strict single-case ownership (FR32)                        |
| Content             | string                            | Yes      | Full-text searchable                                       |
| ContentHash         | string                            | Yes      | SHA-256 of content for dedup                               |
| SourceUri           | string                            | Yes      | File path, URL, or event ID                                |
| SourceType          | SourceType                        | Yes      | Enum: File, Url, Event, Command, Projection, Discussion    |
| IngestedBy          | string                            | Yes      | User or system identity (FR65)                             |
| IngestedAt          | DateTimeOffset                    | Yes      | Ingestion timestamp                                        |
| LastUpdated         | DateTimeOffset                    | Yes      | Last modification timestamp                                |
| Status              | MemoryUnitStatus                  | Yes      | Pipeline stage tracking                                    |
| Metadata            | Dictionary<string, MetadataField> | Yes      | Can be empty dict, never null                              |
| EmbeddingProvider   | string?                           | No       | e.g. "google:text-embedding-004", populated post-embedding |
| EmbeddingDimensions | int?                              | No       | e.g. 768, populated post-embedding                         |
| Classification      | string?                           | No       | Phase 4 enabler, schema-only in MVP                        |
| FailureDetails      | FailureDetails?                   | No       | Populated only when Status=Failed                          |

### GraphEdge Field Inventory

| Field      | Type           | Required | Notes                      |
| ---------- | -------------- | -------- | -------------------------- |
| Id         | string         | Yes      | ULID format                |
| SourceId   | string         | Yes      | Source memory unit ID      |
| TargetId   | string         | Yes      | Target memory unit ID      |
| EdgeType   | EdgeType       | Yes      | Enum with 5 values         |
| Confidence | float          | Yes      | Default varies by EdgeType |
| Origin     | EdgeOrigin     | Yes      | Explicit or Inferred       |
| CreatedAt  | DateTimeOffset | Yes      | Edge creation timestamp    |

### EdgeType Default Confidence Constants

Define in a static class `EdgeTypeDefaults`:

- `CausedBy` → 1.0f
- `CorrelatedWith` → 0.8f
- `References` → 0.5f (default for inferred; explicit may be higher)
- `Contains` → 1.0f
- `Annotates` → 1.0f

### Enum Serialization Pattern

All enums must serialize as camelCase strings. Use per-enum `[JsonConverter]` attributes (NOT a global converter):

```csharp
using System.Text.Json.Serialization;

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Identifies the origin type of ingested content.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SourceType>))]
public enum SourceType
{
    File,
    Url,
    Event,
    Command,
    Projection,
    Discussion
}
```

**Important:** The generic `JsonStringEnumConverter<T>` (available since .NET 9) defaults to exact PascalCase (`"File"`, `"Url"`). To get true camelCase (`"file"`, `"url"`), pass `JsonNamingPolicy.CamelCase` to the converter. However, `[JsonConverter]` attribute syntax does not support constructor arguments. Two options:

1. Accept PascalCase enum strings (e.g., `"File"`) — simpler, still string-based not integer
2. Create a custom attribute wrapper or use global options for camelCase

**Recommended:** Accept PascalCase enum strings. The critical requirement is string serialization (not integers). PascalCase is unambiguous and matches C# enum member names. Verify the actual behavior in tests and document whichever convention you choose.

### Sealed Record Pattern

Follow Hexalith.EventStore conventions for immutable data types:

```csharp
namespace Hexalith.Memories.Contracts.V1;

/// <summary>Represents extracted metadata with origin and confidence tracking.</summary>
public sealed record MetadataField(string Value, MetadataOrigin Origin, float Confidence);
```

- One type per file
- XML doc summary on every public type
- Constructor parameters = record positional properties
- Use `{ get; init; }` syntax only if you need default values or optional fields (use `init` for nullable optionals)
- **Property order MUST match the field inventory table order** — required fields first, nullable fields last. This ensures consistent diffs and readability across the codebase.

For `MemoryUnit` with many fields and nullable optionals, prefer the property syntax:

```csharp
public sealed record MemoryUnit
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    // ... required fields ...
    public Dictionary<string, MetadataField> Metadata { get; init; } = [];
    public string? Classification { get; init; }
    public FailureDetails? FailureDetails { get; init; }
}
```

This allows creating instances with `new MemoryUnit { Id = "...", ... }` and makes optional fields clearly optional.

**Metadata dictionary default:** The `Metadata` property MUST have a default of `[]` (empty collection expression). This prevents null dictionaries during deserialization — `System.Text.Json` will populate the default empty dictionary if the JSON field is missing, avoiding NREs in downstream consumers. Do NOT mark it as `required` — the default handles the absent case.

### Project Structure

```
src/Hexalith.Memories.Contracts/
├── MemoriesInfo.cs                    # Keep existing (root namespace)
├── Hexalith.Memories.Contracts.csproj # No changes needed
└── V1/
    ├── EdgeOrigin.cs                  # enum
    ├── EdgeType.cs                    # enum
    ├── EdgeTypeDefaults.cs            # static class with confidence constants
    ├── ErrorResponse.cs               # sealed record
    ├── FailureDetails.cs              # sealed record
    ├── GraphEdge.cs                   # sealed record
    ├── MemoriesJsonContext.cs          # Static class with shared JsonSerializerOptions (NOT source generator)
    ├── MemoryUnit.cs                  # sealed record
    ├── MemoryUnitStatus.cs            # enum
    ├── MetadataField.cs               # sealed record
    ├── MetadataOrigin.cs              # enum
    └── SourceType.cs                  # enum

tests/Hexalith.Memories.Contracts.Tests/
├── MemoriesInfoTests.cs               # Keep existing
└── V1/
    ├── ErrorResponseSerializationTests.cs
    ├── FailureDetailsSerializationTests.cs
    ├── GraphEdgeSerializationTests.cs
    ├── MemoryUnitSerializationTests.cs
    └── MetadataFieldSerializationTests.cs
```

### Testing Requirements

- **Framework:** xUnit + Shouldly (already in test .csproj)
- **Pattern:** `TypeNameSerializationTests` with `[Fact]` methods
- **Assertion:** Use `deserialized.ShouldBe(original)` for simple records (MetadataField, ErrorResponse, FailureDetails, GraphEdge). For MemoryUnit (which contains a Dictionary), compare via re-serialized JSON strings instead — Dictionary uses reference equality in records, so `ShouldBe` fails after deserialization even when content is identical.
- **JSON options:** Use `MemoriesJsonContext.Options` in all tests — the single shared instance with `JsonNamingPolicy.CamelCase`. Do NOT create ad-hoc options in test files.

Example test pattern:

```csharp
using System.Text.Json;
using Hexalith.Memories.Contracts.V1;
using Shouldly;

namespace Hexalith.Memories.Contracts.Tests.V1;

public class MetadataFieldSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new MetadataField("payment-related", MetadataOrigin.Human, 0.5f);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        var deserialized = JsonSerializer.Deserialize<MetadataField>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void Origin_ShouldSerializeAsString()
    {
        var field = new MetadataField("test", MetadataOrigin.Ai, 0.5f);
        string json = JsonSerializer.Serialize(field, MemoriesJsonContext.Options);

        json.ShouldContain("\"origin\":");
        json.ShouldNotContain("\"origin\":1");  // Must be string, not integer
    }

    [Fact]
    public void ConfidenceBoundary_ZeroAndOne_ShouldRoundTrip()
    {
        var zero = new MetadataField("a", MetadataOrigin.Human, 0.0f);
        var one = new MetadataField("b", MetadataOrigin.Ai, 1.0f);

        var zeroRt = JsonSerializer.Deserialize<MetadataField>(
            JsonSerializer.Serialize(zero, MemoriesJsonContext.Options),
            MemoriesJsonContext.Options);
        var oneRt = JsonSerializer.Deserialize<MetadataField>(
            JsonSerializer.Serialize(one, MemoriesJsonContext.Options),
            MemoriesJsonContext.Options);

        zeroRt.ShouldBe(zero);
        oneRt.ShouldBe(one);
    }
}
```

**Required test cases (comprehensive boundary coverage):**

**MemoryUnit tests:**

- All required fields populated — full round-trip
- Nullable fields (Classification, FailureDetails) set to null — verify null preserved
- Nullable fields populated — verify values preserved
- Empty Metadata dictionary `[]` — verify serializes as `{}` and round-trips
- Populated Metadata dictionary with multiple entries — verify all entries round-trip
- Metadata null resilience — deserialize JSON containing `"metadata": null` and verify result has empty dictionary (not null)
- DateTimeOffset fields — verify ISO 8601 offset preserved (e.g., `+02:00` not collapsed to UTC)

**GraphEdge tests:**

- Full round-trip with each EdgeType value (all 5)
- Confidence boundary: `0.0f` and `1.0f` round-trip exactly
- DateTimeOffset CreatedAt — offset preserved

**Enum tests (cover first AND last values of each enum):**

- `SourceType.File` AND `SourceType.Discussion`
- `MemoryUnitStatus.Queued` AND `MemoryUnitStatus.Failed`
- `EdgeType.CausedBy` AND `EdgeType.Annotates`
- `MetadataOrigin.Human` AND `MetadataOrigin.Ai`
- `EdgeOrigin.Explicit` AND `EdgeOrigin.Inferred`
- Verify string serialization (not integer) for all

**ErrorResponse tests:**

- Typical error format round-trip
- Verify JSON property names are camelCase: `"code"`, `"message"`, `"suggestion"`

**MetadataField tests:**

- Confidence boundary: `0.0f` and `1.0f` round-trip exactly
- Both MetadataOrigin values

### Previous Story Intelligence (from 1-1)

**Patterns established:**

- `.slnx` solution format — manually created
- `Directory.Packages.props` — centralized versions, no versions in .csproj
- `Directory.Build.props` — .NET 10, C# 14, nullable enable, TreatWarningsAsErrors
- `.editorconfig` — Allman braces, \_camelCase fields, I-prefix interfaces, Async suffix, 4-space indent
- Test pattern: xUnit + Shouldly, `[Fact]`, `.ShouldBe()` assertions
- Contracts.Tests already references all test packages

**Debug learnings:**

- `Aspire.Hosting.AppHost` is implicit — don't add to CPM
- Submodule paths: `src/submodules/Hexalith.Commons/`, `src/submodules/Hexalith.EventStore/`
- Existing test: `MemoriesInfoTests.cs` with `MemoriesInfo.Name.ShouldBe("Hexalith.Memories")`

**Files to preserve (do NOT modify or delete):**

- `src/Hexalith.Memories.Contracts/MemoriesInfo.cs` (or `Placeholder.cs`) — existing code
- `tests/Hexalith.Memories.Contracts.Tests/MemoriesInfoTests.cs` — existing test

### Anti-Patterns to Avoid

- **DO NOT add external NuGet packages to Contracts** — keep it framework-only. `System.Text.Json` is built-in.
- **DO NOT generate ULIDs in Contracts** — IDs are plain `string` fields. Generation happens in the Server/domain layer using `UniqueIdHelper` from Hexalith.Commons.
- **DO NOT create interfaces for the domain types** — no `IMemoryUnit`, no `IGraphEdge`. Concrete sealed records only (Decision D9).
- **DO NOT add validation logic to records** — validation is done via FluentValidation in the Server project (Story 1.6+), not in Contracts.
- **DO NOT add Commands, Events, or DomainResult** — those are for later stories (1.3-1.6). This story is strictly the domain MODEL types.
- **DO NOT use integer enum serialization** — all enums MUST serialize as camelCase strings. Verify in tests.
- **DO NOT add `[JsonPropertyName]` attributes** — use global `JsonNamingPolicy.CamelCase` instead. Attributes add noise and are error-prone.
- **DO NOT make Metadata nullable** — it should be `Dictionary<string, MetadataField>` (required), initialized to empty dict if no metadata. Null dictionary causes NREs downstream. Also DO NOT set `Metadata = null` in `with` expressions or object initializers — always use empty `[]` or a populated dictionary.
- **DO NOT delete or rename Placeholder.cs** — it contains `MemoriesInfo.Name` used by existing tests. Leave it exactly as-is.
- **DO NOT use a global `JsonStringEnumConverter` in shared options** — per-enum `[JsonConverter]` attributes are the chosen approach. The shared `JsonSerializerOptions` in `MemoriesJsonContext` provides camelCase property naming only.
- **DO NOT use `float` test values with precision issues** — use exactly representable values like `0.0f`, `0.5f`, `0.8f`, `1.0f` in test data. Avoid values like `0.1f` that cannot be represented exactly in IEEE 754.

### Cross-Cutting Dependency Map

```
Contracts (this story) ← Server, Redis, Tests
  └── V1/                ← All V1 types defined here
       └── No external deps (framework only)
```

### Source of Truth

All patterns must align with **Hexalith.EventStore** conventions:

- `sealed record` for data types
- File-scoped namespaces
- Allman braces
- XML doc summaries on public types
- xUnit + Shouldly for tests

When in doubt, check `src/submodules/Hexalith.EventStore/` for canonical patterns.

### References

- [Source: architecture.md#Memory Unit Field Inventory] — Full field definitions
- [Source: architecture.md#Graph Edge Model] — GraphEdge field definitions
- [Source: architecture.md Decision D6] — Error format: code + message + suggestion
- [Source: architecture.md Decision D9] — Concrete classes, no premature interfaces
- [Source: architecture.md Decision D14] — Versioned namespaces: Contracts.V1
- [Source: architecture.md Decision D18] — Result pattern + error handling layers
- [Source: architecture.md Decision D22] — camelCase JSON, code style, .editorconfig
- [Source: epics.md#Story 1.2] — Acceptance criteria and user story
- [Source: architecture.md#Naming Patterns] — JSON serialization with camelCase
- [Source: architecture.md#Contract Evolution] — V1/V2 namespace strategy

## Change Log

- 2026-03-28: Story created — comprehensive domain model and contracts guide for V1 types
- 2026-03-28: Party mode review applied — per-enum JsonConverter strategy, Metadata dictionary default, Placeholder.cs preserved as-is, boundary test cases (enum first/last, confidence 0.0/1.0, DateTimeOffset offset, empty dictionary), float precision guidance
- 2026-03-28: Advanced elicitation round 1 (5 methods) — metadata null resilience test, V1 namespace enforcement rule, MemoriesJsonContext pinned to static class (not source generator), single shared options mandate, pure data carrier constraint, property order rule, MetadataField.Value string rationale, ErrorResponse.Suggestion actionable guidance
- 2026-03-28: Advanced elicitation round 2 (5 methods) — Dictionary reference equality caveat for MemoryUnit tests (use JSON string comparison), MemoryUnit represents completed state (not intermediate pipeline state), Metadata null forbidden in `with` expressions
- 2026-03-28: Implementation complete — 12 V1 source files, 6 test files, 36 tests passing, 0 warnings. Used C# 14 `field` keyword for Metadata null resilience. Enums serialize as PascalCase strings (per recommendation).

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- **Metadata null resilience:** STJ default initializer `= []` only fires when JSON field is _missing_, not when explicitly `null`. Used C# 14 `field` keyword with null-coalescing init accessor (`init => field = value ?? [];`) to ensure `"metadata": null` in JSON deserializes to empty dictionary.
- **Shouldly ShouldNotContain:** Default is case-insensitive comparison. Used `Case.Sensitive` for PascalCase vs camelCase property name assertions.
- **Enum serialization:** `JsonStringEnumConverter<T>` without constructor args produces PascalCase strings (e.g., `"File"`, `"CausedBy"`). This is acceptable per story Dev Notes recommendation — string-based, not integer.

### Completion Notes List

- ✅ All 5 enum types created with per-enum `[JsonConverter]` attributes (PascalCase string serialization)
- ✅ All 3 value types created as sealed records (MetadataField, FailureDetails, ErrorResponse)
- ✅ MemoryUnit sealed record with 16 fields, `field` keyword for null-safe Metadata property
- ✅ GraphEdge sealed record with positional constructor (7 fields)
- ✅ EdgeTypeDefaults static class with 5 confidence constants
- ✅ MemoriesJsonContext static class with shared `JsonSerializerOptions` (camelCase)
- ✅ 36 tests pass: 7 MemoryUnit, 5 GraphEdge, 4 MetadataField, 2 ErrorResponse, 2 FailureDetails, 10 enum, 6 existing
- ✅ Zero build warnings, zero errors across entire solution
- ✅ Existing Placeholder.cs and MemoriesInfoTests.cs preserved unchanged

### File List

**New files:**

- src/Hexalith.Memories.Contracts/V1/SourceType.cs
- src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs
- src/Hexalith.Memories.Contracts/V1/EdgeType.cs
- src/Hexalith.Memories.Contracts/V1/MetadataOrigin.cs
- src/Hexalith.Memories.Contracts/V1/EdgeOrigin.cs
- src/Hexalith.Memories.Contracts/V1/MetadataField.cs
- src/Hexalith.Memories.Contracts/V1/FailureDetails.cs
- src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs
- src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs
- src/Hexalith.Memories.Contracts/V1/GraphEdge.cs
- src/Hexalith.Memories.Contracts/V1/EdgeTypeDefaults.cs
- src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/GraphEdgeSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/MetadataFieldSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/ErrorResponseSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/FailureDetailsSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/EnumSerializationTests.cs

**Modified files:**

- \_bmad-output/implementation-artifacts/sprint-status.yaml (status: ready-for-dev → in-progress → review)
