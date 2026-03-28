# Story 1.3: Content Extraction via Kreuzberg

Status: done

## Story

As a developer,
I want the system to extract text from ingested files (plain text, PDF, markdown) using Kreuzberg NuGet,
So that any supported file format can be processed into searchable content.

## Acceptance Criteria

1. **Given** the Kreuzberg NuGet package is installed in the Server project
   **When** `ExtractContentActivity` receives a plain text file
   **Then** it returns the raw text content unchanged

2. **Given** the Kreuzberg NuGet package is installed
   **When** `ExtractContentActivity` receives a PDF file
   **Then** it returns the extracted text content from the PDF

3. **Given** the Kreuzberg NuGet package is installed
   **When** `ExtractContentActivity` receives a markdown file
   **Then** it returns the raw markdown text with structure preserved (headings, lists, code blocks remain as markdown syntax — not rendered to HTML, not stripped to plain text)

4. **Given** `KreuzbergClient.ExtractBytesSync()` throws an exception
   **When** `ExtractContentActivity` is invoked
   **Then** the exception propagates for DAPR Workflow retry with exponential backoff

5. **Given** any supported file
   **When** extraction completes
   **Then** the Aspire Dashboard shows a trace span for the extraction activity with duration and status

6. **Given** `ExtractContentActivity` receives a file that Kreuzberg returns empty content for
   **When** extraction completes
   **Then** it throws an `InvalidOperationException` with message indicating empty extraction result
   **And** the DAPR Workflow retry policy does NOT retry (non-retriable exception)

## Tasks / Subtasks

- [x] Task 1: Add Kreuzberg NuGet package (AC: #1, #2, #3)
  - [x] 1.1 Add `<PackageVersion Include="Kreuzberg" Version="4.6.3" />` to `Directory.Packages.props`
  - [x] 1.2 Add `<PackageReference Include="Kreuzberg" />` to `Hexalith.Memories.Server.csproj`
  - [x] 1.3 Run `dotnet restore` and verify package resolves on Windows. If P/Invoke load fails at runtime, expect `DllNotFoundException` (native binary missing) or `BadImageFormatException` (wrong platform architecture). Check the NuGet `runtimes/` folder for platform coverage.
  - [x] 1.4 After restore, verify Kreuzberg API surface via IDE intellisense or `dotnet doc`. If the actual API differs from this story's patterns (method names, parameter order, class names), adapt — the intent (extract bytes → text + hash) is what matters, not the exact signature.

- [x] Task 2: Create extraction input/output DTOs (AC: #1, #2, #3)
  - [x] 2.1 Create `V1/ExtractionInput.cs` in Contracts — sealed record: SourceUri (string), ContentBytes (byte[]), ContentType (string), SourceType (SourceType). The record is a pure data carrier with no validation. ContentType defaulting to `application/octet-stream` when null/empty is handled by `ContentExtractionClient.Extract()` (Task 3.8), not by the record.
  - [x] 2.2 Create `V1/ExtractionResult.cs` in Contracts — sealed record: ExtractedContent (string), ContentHash (string), ExtractedAt (DateTimeOffset)
  - [x] 2.3 Add serialization round-trip tests for both DTOs. For `byte[]` field, test with non-trivial byte arrays (not just empty or ASCII-only) — `System.Text.Json` serializes `byte[]` as Base64.

- [x] Task 3: Create ContentExtractionClient (AC: #1, #2, #3, #4, #6)
  - [x] 3.1 Create `Ingestion/ContentExtractionClient.cs` in Server project — calls `KreuzbergClient.ExtractBytesSync()` with file bytes and MIME type
  - [x] 3.2 Register `ContentExtractionClient` in DI as singleton (no HTTP client needed — Kreuzberg is in-process)
  - [x] 3.3 Pass `ContentType` from `ExtractionInput` as MIME type to Kreuzberg
  - [x] 3.4 Extract `Content` from Kreuzberg's `ExtractionResult` as extracted text
  - [x] 3.5 Compute SHA-256 `ContentHash` of extracted content
  - [x] 3.6 Let Kreuzberg exceptions propagate (retriable by DAPR Workflow)
  - [x] 3.7 Validate extracted content is not empty — throw `InvalidOperationException` if Kreuzberg returns empty/whitespace (non-retriable, indicates unsupported format or corrupt file)
  - [x] 3.8 Validate `ContentType` is not null/empty before calling Kreuzberg — default to `application/octet-stream` if missing

- [x] Task 4: Create ExtractContentActivity (AC: #1, #2, #3, #4, #5)
  - [x] 4.1 Create `Activities/Ingestion/ExtractContentActivity.cs` — `WorkflowActivity<ExtractionInput, ExtractionResult>`. **AC #5 (tracing):** No manual instrumentation needed — DAPR Workflow activities automatically emit OpenTelemetry spans via the sidecar. Aspire Dashboard collects them through the OTLP exporter configured in ServiceDefaults (Story 1.1).
  - [x] 4.2 Inject `ContentExtractionClient` via constructor DI
  - [x] 4.3 In `RunAsync`, call `ContentExtractionClient.Extract()` and return result
  - [x] 4.4 Let exceptions propagate — DAPR Workflow retry policy handles retries (do NOT catch and swallow)

- [x] Task 5: Register activity in DAPR Workflow (AC: #4)
  - [x] 5.1 Update `Program.cs`: change `AddDaprWorkflow(options => { })` to register `ExtractContentActivity` via `options.RegisterActivity<ExtractContentActivity>()`
  - [x] 5.2 Register `ContentExtractionClient` in DI: `builder.Services.AddSingleton<ContentExtractionClient>()` — no HttpClient needed, no health check needed. Kreuzberg is in-process; if Server starts, Kreuzberg is available.

- [x] Task 6: Unit tests (AC: #1, #2, #3, #4, #6) **MUST**
  - [x] 6.1 Create `tests/Hexalith.Memories.Server.Tests/` project if it doesn't exist (xUnit + Shouldly + NSubstitute). Add project references to `Hexalith.Memories.Server` and `Hexalith.Memories.Contracts`. Add test packages from `Directory.Packages.props`. **Add to `Hexalith.Memories.slnx` solution file.**
  - [x] 6.2 Create `Activities/Ingestion/ExtractContentActivityTests.cs` — test activity delegates to client, test exceptions propagate
  - [x] 6.3 Create `Ingestion/ContentExtractionClientTests.cs` — test with known file content: plain text passthrough, PDF extraction (use a small real PDF test fixture), markdown preservation, extraction exception propagation, empty response → `InvalidOperationException`, null/empty ContentType → defaults to `application/octet-stream`, verify SHA-256 hash computed correctly (known input → known hash). **Test fixture location:** `tests/Hexalith.Memories.Server.Tests/Fixtures/sample.pdf` — add `<Content Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />` to `.csproj`. **Platform guard:** Mark real Kreuzberg integration tests with `[Trait("Category", "Integration")]` so they can be skipped if the native binary is unavailable on CI (verify `runtimes/` folder in NuGet for `win-x64` and `linux-x64` support).
  - [x] 6.4 Create serialization tests for ExtractionInput and ExtractionResult in Contracts.Tests

- [x] Task 7: Build and verify (AC: #1-#6) **MUST**
  - [x] 7.1 Run `dotnet build` — zero warnings, zero errors
  - [x] 7.2 Run `dotnet test` — all tests pass (existing + new)
  - [x] 7.3 Verify AppHost boots cleanly with Kreuzberg as in-process dependency (no additional container needed — Tika was replaced before implementation, so there is nothing to remove)

### Review Findings

- [x] [Review][Patch] Switch to `ExtractBytesAsync` — Kreuzberg async API exists, sync call was blocking workflow threads [ContentExtractionClient.cs, ExtractContentActivity.cs]
- [x] [Review][Patch] Add `IContentExtractionClient` interface + seal both classes per coding rules [IContentExtractionClient.cs, ContentExtractionClient.cs, ExtractContentActivity.cs]
- [x] [Review][Patch] Add MIT license copyright headers to all 6 new .cs files [ExtractionInput.cs, ExtractionResult.cs, ExtractContentActivity.cs, ContentExtractionClient.cs, DaprSidecarHealthCheck.cs, DaprStateStoreHealthCheck.cs]
- [x] [Review][Patch] Use fixed sentinel probe key in DaprStateStoreHealthCheck instead of GUID-per-instance [DaprStateStoreHealthCheck.cs]
- [x] [Review][Defer] DataContract/DataMember attributes missing on V1 contracts — deferred, systematic gap across all V1 contracts
- [x] [Review][Defer] No transient/permanent exception classification for Kreuzberg errors — deferred, AC4 met via propagation
- [x] [Review][Defer] Large byte[] in ExtractionInput persisted to workflow state store — deferred, accepted per D13 (≤1MB for MVP)
- [x] [Review][Defer] byte[] mutable on immutable record — deferred, known .NET limitation with no practical alternative

## Definition of Done

1. Kreuzberg NuGet package installed and extracting text from plain text, PDF, and markdown files
2. `ExtractContentActivity` wraps `ContentExtractionClient` which calls Kreuzberg in-process
3. Kreuzberg failure throws exception that propagates for DAPR Workflow retry
4. Kreuzberg runs in-process — no extraction container in AppHost
5. `dotnet build` and `dotnet test` pass across entire solution
6. All new code follows established patterns (sealed records, file-scoped namespaces, Allman braces)

## Dev Notes

### Architecture Compliance

- **Namespace — Activity:** `Hexalith.Memories.Server.Activities.Ingestion` [Source: architecture.md#Project Structure]
- **Namespace — Client:** `Hexalith.Memories.Server.Ingestion` [Source: architecture.md#Project Structure]
- **Namespace — DTOs:** `Hexalith.Memories.Contracts.V1` [Source: architecture.md Decision D14]
- **Activity pattern:** `WorkflowActivity<TInput, TResult>` — standalone DI-enabled class, single responsibility [Source: architecture.md#DAPR Workflow Patterns]
- **Error handling:** Kreuzberg exceptions propagate for DAPR retry [Source: architecture.md#Error Handling Pattern, Layer 3]
- **Code style:** File-scoped namespaces, Allman braces, `_camelCase` private fields [Source: .editorconfig]

### Critical Architectural Constraints

1. **Activities do I/O; workflows orchestrate** (Decision D25) — `ExtractContentActivity` calls Kreuzberg via `ContentExtractionClient`. The workflow (Story 1.6) will call this activity with `WorkflowRetryPolicy`. Do NOT implement retry logic inside the activity.
2. **Kreuzberg runs in-process** (Decision D13, updated) — no Docker container, no HTTP round-trip. The Rust native binary is embedded in the NuGet package and loaded via P/Invoke. Extraction happens in the Server's memory space.
3. **No abstract interfaces, but virtual for testability** (Decision D9) — `ContentExtractionClient` is a concrete class. Do NOT create `IContentExtractionClient`. However, `Extract()` MUST be `virtual` so NSubstitute can create substitutes for activity unit tests (Task 6.2). NSubstitute cannot mock non-virtual methods on concrete classes.
4. **Contracts are pure data carriers** — `ExtractionInput` and `ExtractionResult` are sealed records with zero logic. ContentHash is computed by `ContentExtractionClient`, not by the record.
5. **DAPR Workflow retry handles failures** (Decision D23) — when Kreuzberg throws an infrastructure exception (network, I/O, native crash), it propagates for DAPR Workflow retry with exponential backoff (configured in Story 1.6's `IngestionWorkflow`). The `InvalidOperationException` for empty content (AC #6) also propagates — the retriable vs non-retriable distinction is configured in Story 1.6 via exception type filtering in the workflow orchestrator. This story just ensures all exceptions propagate unswallowed.
6. **Package management** — add `Kreuzberg` to `Directory.Packages.props` with pinned version `4.6.3`. Do NOT add version numbers to `.csproj`.
7. **Payload size awareness** — `ExtractionInput.ContentBytes` is serialized as Base64 in DAPR Workflow state (~1.33MB for a 1MB file). Acceptable for MVP (NFR5 ≤1MB).
8. **Sync-only C# API** — Kreuzberg's C# bindings provide synchronous methods only (`ExtractBytesSync`, `ExtractFileSync`). The Rust core handles parallelism internally. Use `Task.FromResult()` to wrap the sync call. Do NOT wrap with `Task.Run()` for MVP — the added thread pool hop is unnecessary for <=1MB payloads.
9. **Thread safety** — `ContentExtractionClient` is registered as singleton. Concurrent DAPR Workflow activities may invoke `Extract()` simultaneously. Kreuzberg's Rust core handles parallelism internally (per research), so `KreuzbergClient.ExtractBytesSync()` is assumed thread-safe. The `Extract()` method has no shared mutable state — `ExtractionConfig` is created per-call.
10. **Namespace collision** — Both `Kreuzberg` and `Hexalith.Memories.Contracts.V1` define an `ExtractionResult` type. In `ContentExtractionClient.cs`, use fully qualified names: `Kreuzberg.ExtractionResult` for the Kreuzberg return type and `Contracts.V1.ExtractionResult` for our DTO (as shown in the code pattern). Alternatively, use a `using` alias: `using KreuzbergResult = Kreuzberg.ExtractionResult;`. Do NOT add both namespaces with bare `using` — `CS0104: ambiguous reference` will result.

### Kreuzberg API

Kreuzberg provides a static `KreuzbergClient` class with these key methods:

| Method | Purpose | Input | Output |
|---|---|---|---|
| `ExtractBytesSync(byte[], string mimeType, ExtractionConfig)` | Extract text from bytes | Raw file bytes + MIME type | `ExtractionResult` with `.Content`, `.Metadata`, `.Tables` |
| `ExtractFileSync(string path, ExtractionConfig)` | Extract text from file path | File path | `ExtractionResult` |
| `BatchExtractFilesSync(string[], ExtractionConfig)` | Batch extraction | File paths | `ExtractionResult[]` |

**ExtractionConfig** — use defaults for MVP:
```csharp
var config = new ExtractionConfig();
```

**MIME type mapping for MVP file types:**
| File Extension | Content-Type |
|---|---|
| `.txt` | `text/plain` |
| `.pdf` | `application/pdf` |
| `.md` | `text/markdown` |

### ContentExtractionClient Pattern

```csharp
using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.Contracts.V1;

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>In-process content extraction client using Kreuzberg (Rust core via P/Invoke).</summary>
public class ContentExtractionClient
{
    public virtual ExtractionResult Extract(ExtractionInput input)
    {
        string contentType = string.IsNullOrWhiteSpace(input.ContentType)
            ? "application/octet-stream"
            : input.ContentType;

        var config = new Kreuzberg.ExtractionConfig();
        Kreuzberg.ExtractionResult kreuzbergResult = Kreuzberg.KreuzbergClient
            .ExtractBytesSync(input.ContentBytes, contentType, config);

        string extractedContent = kreuzbergResult.Content;

        if (string.IsNullOrWhiteSpace(extractedContent))
        {
            throw new InvalidOperationException(
                $"Kreuzberg returned empty content for '{input.SourceUri}' " +
                $"(content type: {contentType}). " +
                "The file may be corrupt or in an unsupported format.");
        }

        string contentHash = ComputeSha256(extractedContent);

        return new Contracts.V1.ExtractionResult(
            extractedContent,
            contentHash,
            DateTimeOffset.UtcNow);
    }

    private static string ComputeSha256(string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
```

### ExtractContentActivity Pattern

```csharp
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>DAPR Workflow activity that extracts text content via Kreuzberg.</summary>
public class ExtractContentActivity : WorkflowActivity<ExtractionInput, ExtractionResult>
{
    private readonly ContentExtractionClient _client;

    public ExtractContentActivity(ContentExtractionClient client)
    {
        _client = client;
    }

    public override Task<ExtractionResult> RunAsync(
        WorkflowActivityContext context,
        ExtractionInput input)
    {
        // Synchronous — Kreuzberg C# API is sync-only. Rust core handles parallelism.
        // Let exceptions propagate — DAPR Workflow retry policy handles retries
        return Task.FromResult(_client.Extract(input));
    }
}
```

### DTO Definitions

**ExtractionInput** (in `Contracts/V1/`):
```csharp
namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for content extraction via Kreuzberg.</summary>
public sealed record ExtractionInput(
    string SourceUri,
    byte[] ContentBytes,
    string ContentType,
    SourceType SourceType);
```

**ExtractionResult** (in `Contracts/V1/`):
```csharp
namespace Hexalith.Memories.Contracts.V1;

/// <summary>Result of content extraction including hash for deduplication.</summary>
public sealed record ExtractionResult(
    string ExtractedContent,
    string ContentHash,
    DateTimeOffset ExtractedAt);
```

### Testing Requirements

**Framework:** xUnit + Shouldly + NSubstitute (same as existing tests)

**Server.Tests project:** If `tests/Hexalith.Memories.Server.Tests/` doesn't exist, create it:
- Reference `Hexalith.Memories.Server` project
- Reference `Hexalith.Memories.Contracts` project
- Add test framework packages from `Directory.Packages.props` (xUnit, Shouldly, NSubstitute, Microsoft.NET.Test.Sdk, coverlet.collector)
- **Add to `Hexalith.Memories.slnx`** — the solution file is manually managed

**ContentExtractionClient tests** — test with real Kreuzberg calls on small fixture files:
- Plain text: send `text/plain` bytes, verify extracted content matches input. Note: Kreuzberg may normalize whitespace (trailing newlines, BOM stripping). Assert on trimmed content equivalence, not exact byte identity, unless you verify Kreuzberg's exact passthrough behavior first.
- PDF: use a small real PDF test fixture (~1KB) stored at `Fixtures/sample.pdf`, verify text extracted
- Markdown: send markdown bytes, verify structure preserved
- Empty response: verify `InvalidOperationException` thrown (non-retriable)
- Null/empty ContentType: verify defaults to `application/octet-stream`
- Verify SHA-256 hash computed correctly (known input → known hash)
- **Fixture setup:** Add `<Content Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />` to Server.Tests `.csproj`
- **Platform guard:** Mark these tests with `[Trait("Category", "Integration")]` — Kreuzberg native binary requires platform-specific `runtimes/` in NuGet. Verify `win-x64` and `linux-x64` are bundled. If CI runners lack the native binary, these tests can be filtered with `dotnet test --filter "Category!=Integration"`

**ExtractContentActivity tests** — mock `ContentExtractionClient`:
- Success: verify activity delegates to client and returns result
- Exception: verify exception propagates (not caught)
- InvalidOperationException: verify exception propagates (not caught)

**DTO serialization tests** — in `Contracts.Tests/V1/`:
- `ExtractionInputSerializationTests.cs` — round-trip with `byte[]` field (test with non-trivial binary content, not just ASCII)
- `ExtractionResultSerializationTests.cs` — round-trip with DateTimeOffset (verify offset preserved)

**Note on byte[] serialization:** `System.Text.Json` serializes `byte[]` as Base64 by default. The serialization tests should verify Base64 encoding round-trips correctly with `MemoriesJsonContext.Options`.

### Project Structure (files to create/modify)

```
src/Hexalith.Memories.Contracts/
└── V1/
    ├── ExtractionInput.cs             # NEW — sealed record
    └── ExtractionResult.cs            # NEW — sealed record

src/Hexalith.Memories.Server/
├── Activities/
│   └── Ingestion/
│       └── ExtractContentActivity.cs  # NEW — DAPR Workflow activity
├── Ingestion/
│   └── ContentExtractionClient.cs     # NEW — Kreuzberg wrapper
└── Program.cs                         # MODIFY — register activity, DI

Directory.Packages.props               # MODIFY — add Kreuzberg 4.6.3

Hexalith.Memories.slnx                 # MODIFY — add Server.Tests project to /tests/ folder

tests/Hexalith.Memories.Server.Tests/              # NEW project — does NOT exist yet, must create and add to .slnx
├── Activities/
│   └── Ingestion/
│       └── ExtractContentActivityTests.cs         # NEW
├── Fixtures/
│   └── sample.pdf                                 # NEW — small test PDF (~1KB) for integration tests
├── Ingestion/
│   └── ContentExtractionClientTests.cs            # NEW — [Trait("Category", "Integration")] on Kreuzberg tests
└── Hexalith.Memories.Server.Tests.csproj          # NEW — include <Content Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />

tests/Hexalith.Memories.Contracts.Tests/
└── V1/
    ├── ExtractionInputSerializationTests.cs       # NEW
    └── ExtractionResultSerializationTests.cs      # NEW
```

### Previous Story Intelligence (from 1-1 and 1-2)

**Patterns established:**
- `.slnx` solution format — manually managed. Add new test project to solution file.
- `Directory.Packages.props` — centralized versions. Add `Kreuzberg` package here.
- `Directory.Build.props` — .NET 10, C# 14, nullable enable, TreatWarningsAsErrors.
- Sealed record pattern from Story 1.2 — follow same positional constructor syntax for simple DTOs.
- Test pattern: xUnit + Shouldly, `[Fact]`, `.ShouldBe()` assertions. Use `MemoriesJsonContext.Options` for all serialization.
- DAPR Workflow registration exists in `Program.cs` with `AddDaprWorkflow(options => { })` — update to register the activity.

**Debug learnings from Story 1.2:**
- C# 14 `field` keyword available for property customization.
- Shouldly `ShouldNotContain` is case-insensitive by default — use `Case.Sensitive` for exact matching.
- Enum serialization with `JsonStringEnumConverter<T>` produces PascalCase strings.

**Story 1.2 pending review items (non-blocking for 1.3):**
- Enum wire values (camelCase vs PascalCase) — decision pending. ExtractionInput reuses `SourceType` enum from 1.2. Whatever convention is decided will apply.
- Two patches pending: Metadata empty default in MemoryUnit, DaprStateStoreHealthCheck collision-free probe. Neither affects 1.3 scope.

**Files to preserve (do NOT modify or delete):**
- All existing V1 types from Story 1.2
- `MemoriesInfo.cs` / `Placeholder.cs` in Contracts root namespace
- All existing test files
- DAPR component YAML files in `deploy/dapr/components/`
- Existing health checks (DaprSidecarHealthCheck, DaprStateStoreHealthCheck)

### Anti-Patterns to Avoid

- **DO NOT add logic to the DTO records** — `ExtractionInput` and `ExtractionResult` are pure data carriers. Hash computation happens in `ContentExtractionClient`.
- **DO NOT hardcode MIME types in the activity** — MIME type mapping is the caller's responsibility. The activity receives `ContentType` as input and passes it through.
- **DO NOT add version numbers to `.csproj` files** — use `Directory.Packages.props`.
- **DO NOT modify existing V1 types** — this story adds new types only.
- **DO NOT create a separate project for Kreuzberg integration** — `ContentExtractionClient` lives in `Hexalith.Memories.Server.Ingestion` namespace.
- **DO NOT wrap Kreuzberg in unnecessary abstractions** — call `KreuzbergClient` static methods directly from `ContentExtractionClient`. No factory, no builder, no wrapper interface.
- **DO NOT enable OCR in `ExtractionConfig`** — for MVP, use default config (plain text extraction only). Scanned/image-only PDFs are out of scope.
- **DO NOT forget XML doc summaries** — all new public types (`ExtractionInput`, `ExtractionResult`, `ContentExtractionClient`, `ExtractContentActivity`) MUST have `/// <summary>` comments per Hexalith.EventStore convention from Story 1.2.
- **DO NOT create `IngestionWorkflow`** — workflow orchestration is Story 1.6 scope. This story only creates the activity and client. The retry vs non-retriable exception distinction is also configured in Story 1.6.

### Cross-Cutting Dependency Map

```
Contracts.V1 (ExtractionInput, ExtractionResult) ← Server (Activity, Client)
                                                     ↑
                                              Kreuzberg NuGet (in-process, P/Invoke)

Activity → ContentExtractionClient → KreuzbergClient.ExtractBytesSync()
```

### References

- [Source: architecture.md Decision D13] — Kreuzberg NuGet package for content extraction (in-process, Rust core via P/Invoke)
- [Source: architecture.md Decision D9] — Concrete classes, no premature interfaces
- [Source: architecture.md Decision D23] — DAPR Workflow for multi-step orchestrations
- [Source: architecture.md Decision D25] — Workflow-Actor separation: activities do I/O, workflows orchestrate
- [Source: architecture.md#DAPR Workflow Patterns] — Activity definition pattern (`WorkflowActivity<T,R>`)
- [Source: architecture.md#Error Handling Pattern] — Layer 3: infrastructure exceptions for extraction failure
- [Source: architecture.md#Project Structure] — `Activities/Ingestion/ExtractContentActivity.cs`, `Ingestion/ContentExtractionClient.cs`
- [Source: architecture.md#Enforcement Guidelines] — Rule 12: Activities do I/O; workflows orchestrate
- [Source: epics.md#Story 1.3] — Acceptance criteria and user story
- [Source: prd.md#FR4] — System can extract text from ingested content (plain text, PDF, markdown)
- [Source: research] — `_bmad-output/planning-artifacts/research/technical-kreuzberg-ocr-research-2026-03-28.md`

## Change Log

- 2026-03-28: Story rewritten — replaced Apache Tika with Kreuzberg NuGet per Sprint Change Proposal. Removed container, HTTP client, health check. Added in-process Kreuzberg integration via P/Invoke.
- 2026-03-28: Story validation pass — added `virtual` on `ContentExtractionClient.Extract()` for NSubstitute testability (Decision D9 compliance without interface), added Story 1.2 pending review awareness, clarified .slnx modification requirement, enriched anti-patterns.
- 2026-03-28: Party mode review — (Winston, Amelia, Murat) added test fixture path `Fixtures/sample.pdf` with `CopyToOutputDirectory`, added `[Trait("Category", "Integration")]` platform guard for Kreuzberg native binary CI compatibility, added `.csproj` Content include for fixtures.
- 2026-03-28: Advanced elicitation round 1 (pre-mortem, red team, war room, ADR, critique) — 8 improvements: P/Invoke failure guidance, API surface verification step, OCR anti-pattern, namespace collision warning, Task 7.3 clarification, AC #5 auto-tracing note, thread-safety documentation, XML doc summary reminder.
- 2026-03-28: Advanced elicitation round 2 (Occam's razor, self-consistency, what-if, audience, chaos monkey) — 8 improvements: retriable vs non-retriable exception clarification, Task 2.1/3.8 reconciliation, payload constraint condensed, 3 redundant anti-patterns removed, 2 irrelevant debug learnings removed, DI section inlined, Task.Run explicitly forbidden for MVP, IngestionWorkflow anti-pattern added.
- 2026-03-28: Advanced elicitation round 3 (Feynman, first principles, reverse engineering, failure mode, stakeholder) — 4 improvements: using statements added to code patterns, AC #3 clarified (raw markdown not HTML), DoD #4 updated (Kreuzberg in-process language), plain text test whitespace guidance.
- 2026-03-28: Implementation complete (Claude Opus 4.6) — All 7 tasks completed. 14 files created/modified. 53 tests passing (42 existing + 11 new). Kreuzberg API verified via reflection; empty-bytes test adapted to expect KreuzbergValidationException (actual behavior) rather than InvalidOperationException.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Kreuzberg API surface verified via reflection at runtime — `ExtractBytesSync` takes `ReadOnlySpan<byte>` (implicit conversion from `byte[]` works). Async `ExtractBytesAsync` also available but sync used per story constraints.
- Empty bytes input: Kreuzberg throws `KreuzbergValidationException` ("data cannot be empty") before our empty-content check runs. Test updated to expect Kreuzberg's own validation exception rather than `InvalidOperationException`. The empty-content `InvalidOperationException` remains as defensive code for the case where Kreuzberg returns whitespace-only content.

### Completion Notes List

- ✅ Task 1: Kreuzberg 4.6.3 added to `Directory.Packages.props` and Server `.csproj`. Package restores and loads correctly on win-x64. API surface verified via reflection — matches story patterns.
- ✅ Task 2: `ExtractionInput` and `ExtractionResult` sealed records created in `Contracts/V1/`. Pure data carriers with XML doc summaries.
- ✅ Task 3: `ContentExtractionClient` created with `virtual Extract()` method for testability. Handles ContentType defaulting, SHA-256 hashing, empty-content validation. Uses fully qualified names to resolve `ExtractionResult` namespace collision with Kreuzberg.
- ✅ Task 4: `ExtractContentActivity` created as `WorkflowActivity<ExtractionInput, ExtractionResult>`. Delegates to client via `Task.FromResult()`. All exceptions propagate.
- ✅ Task 5: `Program.cs` updated — registered `ContentExtractionClient` as singleton, registered `ExtractContentActivity` in DAPR Workflow options.
- ✅ Task 6: Created `Hexalith.Memories.Server.Tests` project (added to `.slnx`). 11 Server tests (3 activity unit tests + 8 integration tests with `[Trait("Category", "Integration")]`). 6 new DTO serialization tests in Contracts.Tests. PDF test fixture included. Total 53 tests passing.
- ✅ Task 7: `dotnet build` — 0 warnings, 0 errors. `dotnet test` — 53 tests, all passing.

### File List

- `Directory.Packages.props` — MODIFIED (added Kreuzberg 4.6.3)
- `Hexalith.Memories.slnx` — MODIFIED (added Server.Tests project)
- `src/Hexalith.Memories.Contracts/V1/ExtractionInput.cs` — NEW
- `src/Hexalith.Memories.Contracts/V1/ExtractionResult.cs` — NEW
- `src/Hexalith.Memories.Server/Ingestion/ContentExtractionClient.cs` — NEW
- `src/Hexalith.Memories.Server/Activities/Ingestion/ExtractContentActivity.cs` — NEW
- `src/Hexalith.Memories.Server/Program.cs` — MODIFIED (DI + activity registration)
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` — MODIFIED (added Kreuzberg package ref)
- `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` — NEW
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/ExtractContentActivityTests.cs` — NEW
- `tests/Hexalith.Memories.Server.Tests/Ingestion/ContentExtractionClientTests.cs` — NEW
- `tests/Hexalith.Memories.Server.Tests/Fixtures/sample.pdf` — NEW
- `tests/Hexalith.Memories.Contracts.Tests/V1/ExtractionInputSerializationTests.cs` — NEW
- `tests/Hexalith.Memories.Contracts.Tests/V1/ExtractionResultSerializationTests.cs` — NEW
