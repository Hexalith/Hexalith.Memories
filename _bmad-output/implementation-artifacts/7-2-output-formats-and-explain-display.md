# Story 7.2: Output Formats & Explain Display

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** the **output-format substrate** for the `memories` CLI. Adds a root `--format <human|json|table>` global option (default `human`), a small `IOutputFormatter<T>` abstraction with three implementations (`HumanFormatter`, `JsonFormatter`, `TableFormatter`), and applies it to every command 7.1 already wired — `memories tenant list` and `memories config show`. Then wires enough of `memories search` to demonstrate `--explain` score breakdowns (composite + per-axis + normalization + caveat) against the existing `GET /api/search` endpoint, and adds `memories search inspect` to show a memory unit with **metadata origin** (`human` vs `ai`) and **per-field confidence** (FR64). Stories 7.3–7.5 fill in rich error messages, quickstart, and telemetry — **do not implement those here**.

In practice this story adds three things to the repo:

1. **Output-format infrastructure** in `src/Hexalith.Memories.Cli/Output/`:
    - `OutputFormat` enum (`Human | Json | Table`).
    - `IOutputFormatter<T>` interface with a single method `void Write(T value, TextWriter writer)` — one registration per model type per format.
    - Three implementations per model type (tenant list, config show, search result, memory unit). Registered as keyed services; resolved by `(OutputFormat, Type)` at command-handler entry.
    - **Stable JSON schema v1** under `src/Hexalith.Memories.Cli/Output/Json/` — documented in `docs/dev/cli-output-formats.md` with concrete examples per command. `--format json` emits a top-level envelope `{ "schemaVersion": 1, "command": "<name>", "data": <command-shape> }` so scripts can version-gate.

2. **Two new fully-wired commands under `search`**:
    - `memories search query --tenant <id> [--case <id>] --query <text> [--axis syntactic|semantic|graph|hybrid] [--max-results N] [--explain]` → `GET /api/search?tenantId=...&axis=...&explain=true` → renders `HybridSearchResult` (axis=hybrid) or `SearchResult` (single-axis). With `--explain`, prints each result's composite + per-axis scores plus the response-level `SearchExplanation.Caveat` and per-axis normalization method from `AxisExplanation.NormalizationMethod`.
    - `memories search inspect --tenant <id> --case <id> --id <memoryUnitId>` → `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` → renders a `MemoryUnit` including every `MetadataField` with its origin and confidence. Human format visually separates `[human]` from `[ai]` fields; JSON and Table formats expose both fields verbatim.

3. **One extension to `tenant list` and `config show` output** — the 7.1 default text forms are promoted to the `human` formatter (tab-separated tenant rows; `key=value` config lines). The default CLI contract from AC #3c of Story 7.1 (`memories config show` key=value lines without a format flag) is **preserved byte-for-byte** when `--format` is absent or equal to `human`. JSON and Table are new, additive code paths — no scripts that relied on 7.1's default output are broken.

**What does NOT ship:**

- rich actionable error messages with recovery suggestions (**Story 7.3** — FR56/FR57);
- empty-state nudges like `"No results. This tenant has no memory units yet. Get started: memories ingest <file>..."` (**Story 7.3**);
- `memories quickstart` guided flow (**Story 7.4** — NFR31);
- per-command full `--help` example audit (**Story 7.4** — NFR30);
- search/access telemetry, per-tenant audit events, OTel correlation (**Story 7.5** — FR67);
- `memories ingest`, `memories traverse`, `memories case *`, `memories status`, `memories explore`, `memories handlers` full wiring — those groups remain `NotImplementedCommand` stubs after this story. 7.2 only wires `search query`, `search inspect`, and the format flag.
- changes to `/api/search` or `/api/.../memory-units/{id}` endpoints — the server surface is already sufficient (Stories 2.5, 2.6, 3.x). The CLI consumes it as-is.
- extending `FusedScoredResult` or `ScoredResult` with metadata fields — metadata origin display happens via `search inspect`, not inline in search results (would be N+1 server calls).

**Primary risks:**

1. **Breaking 7.1's AC #3c contract.** `memories config show` (no `--format` flag) must still emit exactly the same three key=value lines as Story 7.1 shipped — scripts may already depend on them. The `HumanFormatter` for the config-show model must produce byte-identical output. Add an explicit regression test that diffs output against a 7.1-era golden.
2. **JSON schema drift across stories.** If 7.3/7.5 later add error or telemetry payloads, they must slot into the same envelope. Lock `schemaVersion: 1` and document all command-specific shapes **in this story**, not ad-hoc later. Adding a new optional field is non-breaking; removing or renaming a field requires bumping `schemaVersion`.
3. **Confidence-score semantics leak.** The `--explain` output must print the **full** caveat string from `SearchExplanation.Caveat` (returned verbatim by the server) — not a paraphrase. PRD is emphatic: "confidence scores measure query-result relevance, NOT factual accuracy or data completeness" must appear on every explain display. Consume `AxisExplanation.NormalizationMethod` as-is; don't translate `"bm25_saturation"` to prose.
4. **Search request parameter mismatch.** The existing `GET /api/search` endpoint (see `src/Hexalith.Memories.Server/Program.cs:1483`) returns **different response types per axis** — single-axis returns `SearchResult` (with `ScoredResult[]`), and `axis=hybrid` returns `HybridSearchResult` (with `FusedScoredResult[]`). The CLI client method must dispatch on axis and decode accordingly, or have two typed methods. Getting this wrong returns an empty list silently — hard to debug.
5. **Over-rendering `--explain` when the server didn't produce one.** `SearchExplanation` is nullable — when `explain=false` the server omits it. The formatter must not crash when asked to render explain data that doesn't exist; print `(no explain data)` or suppress the section.

## Story

As a developer,
I want search results and command output in multiple formats with detailed explain information,
so that I can use the CLI interactively, in scripts, and for debugging relevance issues.

## Acceptance Criteria

1. **Default output (no `--format`) is human-readable and preserves all 7.1 contracts.**
   **Given** the CLI is installed,
   **When** I run any command that produced output in Story 7.1 (`memories tenant list`, `memories config show`) with no `--format` flag,
   **Then** output matches the Story 7.1 byte-for-byte default (FR55 — human is the default).

2. **`--format json` produces valid, schema-versioned JSON to stdout.**
   **Given** any wired command,
   **When** I run it with `--format json`,
   **Then** stdout contains exactly one JSON document `{ "schemaVersion": 1, "command": "<name>", "data": <shape> }`
   **And** the document parses with `System.Text.Json` default options
   **And** no ANSI, no prompts, no diagnostics — stderr is unchanged (FR55).

3. **`--format table` produces an ASCII-aligned table to stdout.**
   **Given** any wired command,
   **When** I run it with `--format table`,
   **Then** stdout contains a header row, a separator row (hyphens), and one row per item, with columns right-padded using `string.PadRight` to the max content width
   **And** empty result sets print only the header + separator (no "no results" text — that is 7.3).

4. **`memories search query` is fully wired against the existing `/api/search` endpoint.**
   **Given** a running Memories Server with at least one indexed memory unit,
   **When** I run `memories search query --tenant <id> --query <text>` (default axis `hybrid`),
   **Then** the CLI issues `GET /api/search?tenantId=<id>&query=<text>&axis=hybrid` through `MemoriesClient.SearchAsync(...)`
   **And** renders the ranked results according to the resolved `--format`
   **And** the exit code is 0 on HTTP 200 (including empty results)
   **And** transport-layer failures still route through `CliCommandExecutor` (Story 7.1 anti-pattern #18).

5. **`--explain` surfaces every field the PRD documents.**
   **Given** a search with `--explain`,
   **When** results are displayed,
   **Then** each result shows: composite confidence score, per-axis scores (syntactic, semantic, graph — omitting absent axes), normalization method per axis from `AxisExplanation.NormalizationMethod`, and the response-level caveat from `SearchExplanation.Caveat` (verbatim — not paraphrased)
   **And** the caveat is printed exactly once per `--explain` response (not once per result row).

6. **`memories search inspect` renders metadata origin and confidence (FR64).**
   **Given** an ingested memory unit with at least one `human`-origin and one `ai`-origin metadata field,
   **When** I run `memories search inspect --tenant <id> --case <id> --id <memoryUnitId>`,
   **Then** the CLI issues `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}`
   **And** every `MetadataField` is rendered with its `Value`, `Origin` (`human|ai`), and `Confidence` (0.0–1.0)
   **And** in human format the two origins are **visually distinct** via lowercase ASCII prefixes `[human]` and `[ai]` (matching the `MetadataOrigin` enum's camelCase JSON serialization — see `src/Hexalith.Memories.Contracts/V1/MetadataOrigin.cs`) — no emoji, no colour dependency; stdout is still pipe-friendly.

7. **The `--format` option is a root-level global flag inherited by every subcommand.**
   **Given** the CLI framework (System.CommandLine per ADR-7.1-008),
   **When** `--format <value>` is passed at any level (`memories --format json tenant list` or `memories tenant list --format json`),
   **Then** the effective format is the same
   **And** invalid values (`--format xml`) produce exit code **2** and a one-line message `"Unknown format 'xml'. Use human, json, or table."` on stderr.

8. **JSON schema v1 is documented with concrete examples per command.**
   **Given** `docs/dev/cli-output-formats.md` exists,
   **When** a consumer reads it,
   **Then** they see: the envelope shape (`schemaVersion`, `command`, `data`), one worked example per wired command (tenant list, config show, search query with and without `--explain`, search inspect), and the versioning policy (adding a field is non-breaking; renaming or removing requires bumping `schemaVersion`)
   **And** the doc is referenced from the root `--help` "See also" section.

9. **Tests cover the full format matrix and preserve 7.1 regressions.**
   **Given** the consolidated `tests/Hexalith.Memories.Cli.Tests/` project (Story 7.1 Task 5.1),
   **When** `dotnet test` runs,
   **Then** it includes: one golden-file regression test that asserts `memories config show` (no flag) output is unchanged from 7.1; per-format snapshot tests for each wired command covering both non-empty and empty inputs; a JSON schema test that parses the output with `System.Text.Json` and asserts `schemaVersion == 1` plus `command` matches the invoked command
   **And** all Story 7.1 tests (51 tests per 7.1 Change Log) still pass without modification.

10. **One integration test proves the live search pipeline end-to-end.**
    **Given** the existing `AspireIngestionPipelineFixture`,
    **When** `CliSearchIntegrationTests` runs,
    **Then** it ingests a small fixture file via the existing `POST /api/ingest`, waits for the workflow to land, calls `MemoriesClient.SearchAsync` with `axis=hybrid, explain=true`, and asserts at least one result with non-null `CompositeScore` and a non-null `Explanation.Caveat`
    **And** no subprocess spawn of the `memories` binary (per 7.1 anti-pattern #8).

## Tasks / Subtasks

### Task Summary (orientation)

| # | Task | Blocked by | AC coverage |
|---|------|------------|-------------|
| 1 | Output-format infrastructure (enum, interface, router, `--format` global) | — | #1, #2, #3, #7 |
| 2 | Stable JSON envelope + source-gen context | 1 | #2, #8 |
| 3 | Formatters for existing 7.1 commands (`tenant list`, `config show`) | 1, 2 | #1, #9 |
| 4 | `MemoriesClient.SearchAsync` + `HybridSearchAsync` + `GetMemoryUnitAsync` | — | #4, #5, #10 |
| 5 | `memories search query` command (including `--max-results` cap in 5.6) | 1, 4, 6 | #4, #5 |
| 6 | Search-result + explain formatters (+ degradation notice 6.6a) | 1, 2 | #3, #5 |
| 7 | `memories search inspect` command + `MemoryUnit` formatters | 1, 2, 4 | #6 |
| 8 | `docs/dev/cli-output-formats.md` + README update | 1-7 | #8 |
| 9 | Tests (regression golden, per-format snapshots, JSON parse, redaction, cold-start) | 1-7 | #1, #9 |
| 10 | Integration test `CliSearchIntegrationTests` | 4 (for client methods) | #10 |

Full detail below. See also the Task dependency sketch in Dev Notes for parallel-stream execution order.

- [x] Task 1: Output-format infrastructure (AC: #1, #2, #3, #7)
    - [x] 1.1 Create `src/Hexalith.Memories.Cli/Output/OutputFormat.cs` — public enum `OutputFormat { Human, Json, Table }` with a `[JsonConverter(typeof(JsonStringEnumConverter))]` attribute so the value round-trips cleanly.
    - [x] 1.2 Create `src/Hexalith.Memories.Cli/Output/IOutputFormatter{T}.cs` — `public interface IOutputFormatter<T> { OutputFormat Format { get; } void Write(T value, TextWriter writer); }`.
    - [x] 1.3 Create `src/Hexalith.Memories.Cli/Output/OutputFormatterRouter.cs` — resolves the right formatter via `IServiceProvider.GetServices<IOutputFormatter<T>>().FirstOrDefault(f => f.Format == selected)`. If the result is `null`, throw a typed `FormatterNotRegisteredException(typeof(T), format)` with message `"No IOutputFormatter<{TypeName}> registered for format '{format}'."`. **Do not** use `.Single(...)` — its implicit `InvalidOperationException` doesn't distinguish "bug in registration" from "caller asked for an unsupported format" and makes the dev-agent's diagnostic path harder. Exposed method: `void Write<T>(OutputFormat format, T value, TextWriter writer)`.
    - [x] 1.4 Add `--format <human|json|table>` as a **root-level** global option. Define the `Option<OutputFormat>` on `CliGlobalOptions` (same pattern as `EndpointOption`, `TokenOption`, `VerboseOption` from 7.1 — see `src/Hexalith.Memories.Cli/Commands/CliGlobalOptions.cs`). The **resolved value** lives on `CliConsole` as a new `public OutputFormat Format { get; set; } = OutputFormat.Human;` property — same pattern as `CliConsole.Verbose` (see `src/Hexalith.Memories.Cli/Execution/CliConsole.cs:18`). `RootCommandFactory.ApplyGlobalOptions` writes the parsed value into `console.Format` alongside the existing `console.Verbose = parseResult.GetValue(...)` assignment (`RootCommandFactory.cs:97`). Command handlers read `console.Format` to pick the formatter — **do not** add a second resolved-state class.
    - [x] 1.5 On unknown format string, throw `InvalidConfigurationException` from `ApplyGlobalOptions` — `Program.cs` already catches it and exits with code 2 (see Story 7.1 `Program.cs:36-39`). Message: `"Unknown format '<value>'. Use human, json, or table."`. **But:** `--help` and `--version` invocations must **always succeed** regardless of invalid globals. Rationale: a user debugging by running `memories --format xml tenant list --help` wants help, not an error. Implement by checking `parseResult.Tokens` (or the equivalent System.CommandLine API) for the presence of `--help` / `--version` / `-h` before calling `ApplyGlobalOptions`'s format validation — if present, skip format validation entirely and fall through to `parseResult.InvokeAsync` which handles help output natively. Endpoint/token validation remains active (help doesn't need those either, but the 7.1 baseline doesn't skip them, so leave alone).
    - [x] 1.6 **Do not** introduce a table-rendering library. Column alignment uses `string.PadRight(maxWidth)`; separator row is `new string('-', sum_of_widths_plus_padding)`.

- [x] Task 2: Stable JSON envelope (AC: #2, #8)
    - [x] 2.1 Create `src/Hexalith.Memories.Cli/Output/Json/CliOutputEnvelope.cs` — `public sealed record CliOutputEnvelope<T>(int SchemaVersion, string Command, T Data)`. Pin `SchemaVersion = 1` as a constant `const int CurrentSchemaVersion = 1`. **The envelope has exactly three top-level fields** (`schemaVersion`, `command`, `data`). Do **not** add speculative slots (`error`, `warnings`, `pagination`, `traceId`, etc.) — each lands with the story that actually needs it. Story 7.3 will add `error`, Story 7.5 may add `traceId` — both are additive per ADR-7.2-001 and do not require bumping `schemaVersion`. Pre-emptive additions create dead contract surface.
    - [x] 2.2 Add source-generated JSON context `CliJsonContext` mirroring the Contracts pattern at `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — register every envelope shape **with the exact type parameters the call sites use**: `CliOutputEnvelope<IReadOnlyList<TenantSummary>>` (matches `MemoriesClient.ListTenantsAsync` return type at `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:53`; array vs `IReadOnlyList<T>` resolve to different `JsonTypeInfo`), `CliOutputEnvelope<ConfigShowData>`, `CliOutputEnvelope<HybridSearchResult>`, `CliOutputEnvelope<SearchResult>`, `CliOutputEnvelope<MemoryUnit>`. **Do not** fall back to reflection-based serialization — AOT trim-safe stays the preference.
    - [x] 2.3 Serializer options: camelCase property naming, indented output (human inspection > 1 byte saved), `JsonIgnoreCondition.WhenWritingNull`.
    - [x] 2.4 Write a strict parse-and-assert test (AC #2 + #9): `JsonDocument.Parse(stdout).RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1)`.

- [x] Task 3: Formatters for existing 7.1 commands (AC: #1, #9)
    - [x] 3.1 Under `src/Hexalith.Memories.Cli/Output/Formatters/`, register three `IOutputFormatter<T>` implementations for each of:
        - `IReadOnlyList<TenantSummary>` (tenant list data model)
        - `ConfigShowData` (new record wrapping `(Uri Endpoint, string ResolvedBy, bool TokenConfigured)` — do **not** reuse `ResolvedConfig` directly; that carries the raw token which must never reach serialization).
    - [x] 3.2 `HumanFormatter<IReadOnlyList<TenantSummary>>` emits the **same** tab-separated `{tenant.Id}\t{tenant.DisplayName}` lines as 7.1's `TenantListCommand` currently does, and the `"No tenants found."` string when empty. Golden-test it.
    - [x] 3.3 `HumanFormatter<ConfigShowData>` emits the **exact** three key=value lines `endpoint=...\nresolvedBy=...\ntokenConfigured=...\n` — byte-for-byte 7.1 parity. No trailing blank line. No ANSI.
    - [x] 3.4 `JsonFormatter<IReadOnlyList<TenantSummary>>` and `JsonFormatter<ConfigShowData>` each wrap their payload in `CliOutputEnvelope<T>` with `command = "tenant list"` / `"config show"`. Indented. camelCase.
    - [x] 3.5 `TableFormatter<IReadOnlyList<TenantSummary>>` header: `TENANT ID   DISPLAY NAME`. Column widths = `max(header.Length, max(item.field.Length))`. `TableFormatter<ConfigShowData>` header: `KEY   VALUE` with three rows.
    - [x] 3.6 Rewire `TenantListCommand.ExecuteAsync` and `ConfigShowCommand.Build` to call `OutputFormatterRouter.Write(globalOptions.FormatSelection, data, console.Out)`. Do not keep the old inline `console.Out.WriteLine` paths once the formatter is in place — two output surfaces on the same command drift apart.

- [x] Task 4: `MemoriesClient.SearchAsync` (AC: #4, #5, #10)
    - [x] 4.1 Add to `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`:
        - `Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)` when `axis == "hybrid"`,
        - `Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)` for single-axis searches.
    - [x] 4.2 Create a small `src/Hexalith.Memories.Client.Rest/SearchRequest.cs` and `HybridSearchRequest.cs` — internal request DTOs (record types) with typed properties (`string TenantId`, `string? CaseId`, `string? Query`, `int MaxResults = 10`, `string Axis = "hybrid"`, `bool Explain = false`). Implemented as query-string builders — **no new server endpoints**. **Omit parameters, don't send defaults:** if `CaseId` is null, do not emit `caseId=` in the query string; if `MaxResults` equals the server default (10, per `Program.cs:1501`), do not emit `maxResults=10`; never send `offset=0` as a literal (the parameter is simply not built into the URL by 7.2 since no AC exercises it — see Task 5.2 cut list). Server-side defaults stay in force.
    - [x] 4.3 Response decoding: `HybridSearchAsync` parses into `Hexalith.Memories.Contracts.V1.HybridSearchResult`; `SearchAsync` parses into `SearchResult`. Reuse `ErrorResponseDecoder` for non-2xx.
    - [x] 4.4 Add entries to `MemoriesJsonContext` for `HybridSearchResult` and `SearchResult` if not already present (both already registered per `MemoriesJsonContext.cs:41, 55` — verify, do not duplicate).
    - [x] 4.5 Unit-test both methods with the existing `TestDelegatingHandler` pattern: happy-path 200 with a canned body per axis; 400 with `ErrorResponse`; malformed JSON → typed `MemoriesRemoteException`.

- [x] Task 5: `memories search query` command (AC: #4, #5)
    - [x] 5.1 Create `src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs`.
    - [x] 5.2 Options: `--tenant <id>` (required), `--case <id>` (optional), `--query <text>` (conditionally required — see Task 5.3), `--axis <syntactic|semantic|graph|hybrid>` (default `hybrid`), `--max-results <N>` (default 10, capped — see Task 5.6), `--explain` (boolean flag). **Do not** add `--source-type`, `--offset`, `--axes`, or any other server query param in 7.2 — they widen the `--help` surface for 7.4's NFR30 audit and are not required by any AC.
    - [x] 5.3 Validation: `--tenant` must always be non-empty. `--query` is **conditionally required** — required for `--axis syntactic|semantic|hybrid` (server rejects empty query with `INVALID_INPUT` at `Program.cs:1640-1648`), but **optional for `--axis graph`** (graph-only search starts from a `--graph-start-node-id` seed, not a query — server allows empty query on that axis). If validation fails, print one-line error to stderr and exit 2 (plumbing — domain errors live in 7.3). Note: this story does **not** wire `--graph-start-node-id` or pure graph-only searches as a first-class AC — but the `--query` validation must not block `--axis graph` pre-emptively, so that a Phase 1.5 story can add the seed option additively without rewriting this validation.
    - [x] 5.4 Handler dispatches on `--axis` (case-insensitive, matching server behavior at `Program.cs:1565`): `hybrid` → `HybridSearchAsync` → router writes `HybridSearchResult`; else → `SearchAsync` → router writes `SearchResult`. Both paths go through `CliCommandExecutor.ExecuteAsync` (anti-pattern #18). **Exit-code policy inherited from 7.1:** server-reported errors (`MemoriesRemoteException` from 4xx/5xx, including HTTP 503 from `AllEnabledAxesUnavailable == true`) continue to exit with code **2** (plumbing) through Story 7.2 — splitting domain errors to exit code 1 is Story 7.3's scope. Do not preemptively remap `MemoriesRemoteException` to exit 1 here.
    - [x] 5.6 **`--max-results` CLI-side cap:** the server currently has no upper bound on `maxResults` (see `Program.cs:1501` — defaults to 10 but accepts anything). A caller asking for `--max-results 10000` would receive 10000 rows; `TableFormatter<HybridSearchResult>` allocates padded strings per row → local-process memory spike unrelated to server health. Enforce a CLI-side ceiling: if `--max-results > 1000`, print `"--max-results exceeds CLI ceiling of 1000. Request a smaller batch or use pagination (coming in Phase 2)."` to stderr and exit code 2 **before** the HTTP call. 1000 is a soft limit — sized to cover practical debug workflows without enabling local DoS. Not strictly required by an AC but called out by red-team review. Server-side cap enforcement is a separate concern (not owned by 7.2).
    - [x] 5.5 Register the handler under the existing stubbed `search` group in `RootCommandFactory` — replace the `NotImplementedCommand.Create(services, "search", ...)` entry with a real command group that adds `query` and `inspect` (Task 7) subcommands. **Remove ONLY the `"search"` entry** from `RootCommandFactory.CommandGroups` (see `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs:19-29`). The following entries **must remain** as `NotImplementedCommand` stubs after this story: `ingest` (Story 7.2/later), `traverse` (later), `case` (later), `status` (later), `explore` (later), `handlers` (later), `quickstart` (Story 7.4). Do not touch them. Adjust the `CommandGroups` list in place; do not reorder.

- [x] Task 6: Search-result + explain formatters (AC: #3, #5)
    - [x] 6.1 Register `IOutputFormatter<HybridSearchResult>` and `IOutputFormatter<SearchResult>` for each of the three formats.
    - [x] 6.2 Human format — **without `--explain`** (i.e., `Explanation == null`): one line per result `{rank}. [{compositeScore:F3}] {sourceUri} — {contentSnippet (first 80 chars)}`. Empty results: no output (not "No results" — that is 7.3).
    - [x] 6.3 Human format — **with `--explain`** (`Explanation != null`): print the caveat `{Explanation.Caveat}` **verbatim** (no paraphrasing, no truncation, no word-wrap) on its own line **first, before any results** — this survives `memories search query --format human --explain | head -N` piping so the compliance-enablement guarantee (`prd.md:465-470`) is not truncated. Then per-result block showing `composite={score:F3}`, each present axis `syntactic={score:F3}`, etc., then a per-axis normalization suffix like `(syntactic: bm25_saturation)`. Caveat is printed exactly once per response.
    - [x] 6.4 JSON format: wrap the full `HybridSearchResult` / `SearchResult` (including `Explanation` when present) in `CliOutputEnvelope<T>`. No transformation — pass the server response through verbatim in `data`.
    - [x] 6.5 Table format — without explain: columns `RANK | SCORE | URI | SNIPPET`. With explain: columns `RANK | COMPOSITE | SYNTACTIC | SEMANTIC | GRAPH | URI`. Null axis scores print as `-`. Caveat printed as a single line **after** the table — table readers want the header-to-data alignment intact at the top (different trade-off than human format; both are documented in `docs/dev/cli-output-formats.md`).
    - [x] 6.6a **Degradation surfacing (bridge — single-owner like 7.1 AC #11):** when `HybridSearchResult.Degraded == true`, the human and table formatters print a one-line degradation notice before results: `"Note: search degraded — axes unavailable: {comma-separated UnavailableAxes}"`. JSON format passes `degraded` and `unavailableAxes` through the envelope verbatim (already part of `HybridSearchResult`). No recovery suggestion here — that is Story 7.3's concern. **Story 7.3 replaces this bridge notice with its full actionable-error surface (FR56 with recovery suggestions per unavailable axis); the bridge and the rich surface do not coexist — 7.3 must delete or rewrite this line in the same PR.** Same ownership pattern as 7.1 ADR-7.1-007.

- [x] Task 7: `memories search inspect` command (AC: #6)
    - [x] 7.1 Add `MemoriesClient.GetMemoryUnitAsync(string tenantId, string caseId, string memoryUnitId, CancellationToken ct)` → `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` returning `MemoryUnit` from `Contracts.V1`.
    - [x] 7.2 Create `src/Hexalith.Memories.Cli/Commands/SearchInspectCommand.cs` with options `--tenant <id>` (required), `--case <id>` (required), `--id <memoryUnitId>` (required). Route through `CliCommandExecutor`.
    - [x] 7.3 Register `IOutputFormatter<MemoryUnit>` for the three formats.
    - [x] 7.4 Human format: print `id`, `tenantId`, `caseId`, `sourceUri`, `ingestedBy`, `ingestedAt`, `status`, then `metadata:` header followed by one line per `MetadataField` kv-pair formatted as `  {key} = {value}  [{origin}, confidence={confidence:F2}]`. **Empty `Metadata` dict:** if the unit has no metadata fields, print `metadata: (none)` on a single line instead of a bare header — avoids the "looks broken" appearance red-team flagged. The `{origin}` substring is **lowercase** — exactly `[human]` or `[ai]`, matching the `MetadataOrigin` enum's camelCase JSON serialization (`src/Hexalith.Memories.Contracts/V1/MetadataOrigin.cs` → `Human` / `Ai` serialize as `human` / `ai` via `CamelCaseStringEnumConverter`). Plain ASCII, no colour, no emoji. Lowercase is load-bearing for downstream `grep '\[human\]'` consistency with JSON output. **`DateTimeOffset` formatting:** render `ingestedAt` (and any other `DateTimeOffset` fields) with the round-trip format specifier `"o"` — `ingestedAt.ToString("o", CultureInfo.InvariantCulture)`. This produces ISO-8601 (e.g., `2026-04-16T15:30:00.0000000+00:00`) and is culture-invariant — prevents CI locale drift (e.g., `en-US` vs `fr-FR` date ordering) from breaking golden-file tests. Apply the same rule in `TableFormatter<MemoryUnit>` (Task 7.6) and anywhere else a `DateTimeOffset` hits stdout. Confidence `{confidence:F2}` is already culture-insensitive in the expected range but pass `CultureInfo.InvariantCulture` explicitly for consistency.
    - [x] 7.5 JSON format: wrap the entire `MemoryUnit` in `CliOutputEnvelope<MemoryUnit>` with `command = "search inspect"`. The `MetadataField.Origin` already serializes to `"human"` / `"ai"` via the existing `[CamelCaseStringEnumConverter]` — no special handling needed.
    - [x] 7.6 Table format: two tables concatenated — a core-fields table (`FIELD | VALUE`) and a metadata table (`KEY | VALUE | ORIGIN | CONFIDENCE`).

- [x] Task 8: Docs (supporting, AC: #8)
    - [x] 8.1 Create `docs/dev/cli-output-formats.md` with:
        - envelope contract (`schemaVersion=1`, `command`, `data`),
        - one worked example per wired command (tenant list, config show, search query default, search query --explain, search inspect) for each of the three formats,
        - **combined-flag example**: show `memories search query --explain --format json` output explicitly — the `data.explanation` block is ONLY populated when both `--explain` **and** a format flag are set. Readers must not assume `--format json` alone surfaces explain data. (Surfaced by customer-support roleplay — common pitfall.)
        - **pipe-safety guidance**: "Use `--format json` for scripts and pipelines. `--format table` includes a separator row of hyphens and is intended for interactive terminal viewing only; piping it into tools like `awk` or `cut` requires skipping the separator line."
        - **format extensibility note**: "The `OutputFormat` enum (`human`, `json`, `table`) is an extensible surface; future formats (e.g., `tsv`, `yaml`, `csv`) may be added in later stories as additive enum values without breaking existing formats or bumping `schemaVersion`."
        - versioning policy per ADR-7.2-001: additive changes stay on `schemaVersion=1`; renames/removes bump to `schemaVersion=2`; both versions supported for at least one release cycle after a bump.
        - a table mapping `OutputFormat` enum values to their command-line spellings.
        - Add a one-line cross-reference from `docs/dev/cli-config.md` "See also" section pointing at the new doc.
    - [x] 8.2 Update the `README.md` "CLI (preview)" section added in 7.1 to mention `--format json` as the scripting entry point, pointing at `docs/dev/cli-output-formats.md`.

- [x] Task 9: Tests (AC: #1, #9)
    - [x] 9.1 Place all new tests in the consolidated `tests/Hexalith.Memories.Cli.Tests/` project (per 7.1 Task 5.1 — no second project).
    - [x] 9.2 **7.1 regression guard:** `ConfigShowGoldenFileTests.NoFormat_ExactMatchesStory71Default()` — inject a **fixture-built `ResolvedConfig`** directly (do NOT read the live `ResolvedConfigPipeline` — its output varies by host: `Uri.ToString()` normalizes trailing slash differently, environment-variable tier picks up CI state, etc.). Fixture: `new ResolvedConfig(new Uri("http://127.0.0.1:5000/"), ApiToken: null, ResolvedBy: "DefaultConfigurationSource")`. **Capture-first, don't invent:** before writing the expected string literal, run the current 7.1 `ConfigShowCommand.Build(...)` against this exact fixture and capture stdout verbatim (paste into the test). The expected string must equal **what 7.1 prints today**, not what the dev agent believes 7.1 prints — `Uri.ToString()` vs string interpolation can differ on trailing slash depending on whether port is explicit, and there is no existing test pinning this (a 7.1 gap). Once captured, the literal becomes the golden. If this test ever fails, reviewer must consciously decide whether to bump `schemaVersion`.
    - [x] 9.3 Per-format snapshot tests per command (Shouldly `ShouldBe` against inlined expected strings — no snapshot library needed for this volume).
    - [x] 9.4 JSON envelope test: parse stdout with `JsonDocument`, assert `schemaVersion == 1`, assert `command` matches the invoked command, assert `data` is present.
    - [x] 9.5 Unknown-format test: invoke with `--format xml`, assert stderr contains `"Unknown format 'xml'."`, assert exit code 2.
    - [x] 9.6 Empty-result test per command: zero tenants → header-only table; empty search → empty human output; `{"data": []}` or `{"data": {...}}` JSON envelope still emitted. Do **not** emit 7.3-style nudge text.
    - [x] 9.7 `--explain` caveat test: canned `HybridSearchResult` with `Explanation.Caveat = "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness."` — assert the string appears exactly once and verbatim in both human and table formats. Assert it appears in the JSON envelope's `data.explanation.caveat` path.
    - [x] 9.8 Metadata-origin visual-distinction test: canned `MemoryUnit` with one `human` and one `ai` field → assert the human-format output contains `[human]` and `[ai]` prefixes (literal strings).
    - [x] 9.9 Token-redaction continuity: the full-output containment assertion from 7.1 Task 6.5 (`UNIQUE-TOKEN-SENTINEL-DO-NOT-LEAK`) must extend to the new commands (`search query`, `search inspect`) and all three formats. Add them to the list asserted in the existing `TokenRedactionTests.cs`.
    - [x] 9.10 **Cold-start regression check (manual, dev-agent-owned, not CI-asserted):** before marking the story `review`, the dev agent runs `memories --version` three times on a warm machine and records the best-of-three wall time. If the figure exceeds **1.2 seconds** (7.1 shipped with a `<1s` advisory; 200ms budget for 7.2's added formatter DI registrations), investigate before merging. Add the result to the story's Completion Notes section as a one-liner (e.g., `"Cold start --version: 0.7s (no regression vs 7.1)"`). Not a CI gate — scope-creep to assert — but a concrete cheap check that catches gross regressions from DI misuse.

- [x] Task 10: Integration test (AC: #10)
    - [x] 10.1 Add `tests/Hexalith.Memories.IntegrationTests/Cli/CliSearchIntegrationTests.cs`. Reuse `AspireIngestionPipelineFixture` (per 7.1 Task 7.2). Annotate the class with `[Collection("AspireIngestionPipeline")]` and `[Trait("Category", "Integration")]` **exactly matching `CliTenantListIntegrationTests`** — this lets dev agents running locally without Docker filter the test out via `dotnet test --filter "Category!=Integration"` instead of hitting a confusing fixture-setup failure. No `Skip.IfNot` pattern needed; the `Trait` + `--filter` flow is the repo convention. **CI precondition callout:** the runnability of this test in CI depends on the repo's pipeline config — Story 7.2 does NOT own verifying or changing repo-level CI behavior (this is Story 11.1's scope). If the current pipeline filters out `Category=Integration`, this test is **local/pre-merge validation only** and will bit-rot if never invoked. Story 7.2 closes with a note in the completion summary to the effect of "integration test added; CI-runnability inherits repo pipeline posture — recommend running locally via `dotnet test --filter Category=Integration` before marking `review`."
    - [x] 10.2 Flow: create tenant → create case → ingest a small fixture file (reuse existing fixture ingest path) → wait for ingestion workflow completion → call `MemoriesClient.HybridSearchAsync` with `tenantId`, `caseId`, a query matching the fixture content, and `explain=true` → assert at least one result with non-null `CompositeScore` in [0.0, 1.0] and `Explanation.Caveat` contains the PRD-mandated substring **`"measure query-result relevance"`** (case-sensitive). **Non-empty is not strong enough** — if the server ever drops or paraphrases the caveat, the CLI's compliance-enablement guarantee breaks silently. This substring assertion is the CLI-boundary check that catches server drift without being fragile to punctuation changes. **Bounded wait pattern** — reuse the exact pattern from `tests/Hexalith.Memories.IntegrationTests/Cli/CliTenantListIntegrationTests.cs`: `static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);` + `DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(ActivationTimeout); while (DateTimeOffset.UtcNow < deadline) { ... poll ...; await Task.Delay(TimeSpan.FromMilliseconds(500)); }` polling `GET /api/ingest/{instanceId}` for workflow status `"COMPLETED"` (or equivalent terminal state). Test must fail cleanly with `Shouldly.ShouldBeTrue("...ingestion did not complete within {ActivationTimeout}")` on timeout — no infinite loops, no test flakes from unbounded waits.
    - [x] 10.3 Do NOT spawn the `memories` binary as a subprocess — per-process invocation belongs to the dev-only packaging script from 7.1 Task 8.1. Update that script to add `memories search query --help` and `memories --format json tenant list` smoke calls so packaging regressions surface locally.
    - [x] 10.4 Register the new test in the same xUnit collection as the 7.1 `CliTenantListIntegrationTests` (`[CollectionDefinition(nameof(AspireIngestionPipelineFixture), DisableParallelization = true)]` — per 7.1 Task 7.4).

### Review Findings

_Generated by `/bmad-code-review` on 2026-04-16. Three adversarial reviewers: Blind Hunter (diff only), Edge Case Hunter (diff + project), Acceptance Auditor (diff + spec). All 11 patches applied the same day; 1414 non-integration tests green (1412 baseline + 2 new)._

- [x] [Review][Decision → Patch] `MemoryUnitTableFormatter` empty-metadata handling — resolved as **option (b): emit `metadata: (none)` in place of the empty metadata table**, mirroring the human formatter's Task 7.4 carve-out. AC #3 addresses empty top-level result listings; an inspected unit's metadata subsection is a different surface. [src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitTableFormatter.cs] _(blind+edge, MED)_
- [x] [Review][Patch] `IsHelpOrVersionInvocation` now walks `parseResult.CommandResult` ancestors looking for `OptionResult` where `Option is HelpOption` or `VersionOption`, instead of scanning raw tokens. An option-argument value equal to `--help`/`--version` no longer spuriously skips `--format` validation. [src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs] _(blind+edge, MED)_
- [x] [Review][Patch] `SnippetTruncator.Truncate` now backs off by one when the cut falls on a UTF-16 high surrogate, preventing orphaned surrogate halves in pipe output and strict JSON re-serialization. [src/Hexalith.Memories.Cli/Output/Formatters/SnippetTruncator.cs] _(blind+edge, MED)_
- [x] [Review][Patch] `JsonEnvelopeWriter.Write<T>` now resolves `JsonTypeInfo<CliOutputEnvelope<T>>` from `CliJsonContext.Options.GetTypeInfo(...)` and calls the `JsonTypeInfo`-bound `JsonSerializer.Serialize` overload, honoring Task 2.2's AOT-safe constraint. [src/Hexalith.Memories.Cli/Output/Formatters/JsonEnvelopeWriter.cs] _(blind, MED)_
- [x] [Review][Patch] Both hybrid formatters now null-coalesce `UnavailableAxes` to an empty sequence before `string.Join`, so a `{ "degraded": true, "unavailableAxes": null }` server response no longer NREs mid-write. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, HybridSearchResultTableFormatter.cs] _(edge, MED)_
- [x] [Review][Patch] `MemoryUnitHumanFormatter` now sanitizes `id`/`tenantId`/`caseId`/`sourceUri`/`ingestedBy` and `MetadataField.Value` through a `SanitizeLine` helper that replaces `\n`/`\r`/`\t` with a space, preserving the `grep '\[human\]'` invariant from Task 7.4 even when values contain line breaks. [src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitHumanFormatter.cs] _(edge, MED)_
- [x] [Review][Patch] `TableWriter.Write` now sanitizes every cell (headers and data rows) through a single `SanitizeCell` helper before width calculation and emit, so embedded `\n`/`\r`/`\t` no longer corrupt column alignment across any table formatter. [src/Hexalith.Memories.Cli/Output/Formatters/TableWriter.cs] _(edge, MED)_
- [x] [Review][Patch] `ConfigShowGoldenFileTests` rewritten to invoke `ConfigShowCommand.Build(...)` end-to-end against a `FixtureConfigurationSource`, exercising `ResolvedConfigPipeline.Resolve()` and `EndpointDisplayFormatter.Format(Uri)` — the `Uri → string` chain Task 9.2 called out as load-bearing is now locked under the regression guard. A pure-formatter cross-check test is kept as a sanity layer. [tests/Hexalith.Memories.Cli.Tests/Cli/ConfigShowGoldenFileTests.cs] _(auditor, MED)_
- [x] [Review][Patch] `SearchQueryCommand` now trims `--axis` before case-folding and rejects any value outside `{syntactic, semantic, graph, hybrid}` with `"--axis '<value>' is not recognized. Use syntactic, semantic, graph, or hybrid."` and exit 2, before any HTTP call. [src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs] _(edge, LOW)_
- [x] [Review][Patch] `HybridSearchResultTableFormatter` class doc-comment now states accurately that the caveat prints after the table AND that the degraded-axes notice still prints before the header per Task 6.6a. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultTableFormatter.cs] _(blind, LOW)_
- [x] [Review][Patch] Added `SingleAxisHuman_ExplainWithEmptyResults_PrintsCaveatAndPerAxisNormalizationOnly` to lock the explain-with-empty-results edge that the original empty-branch test did not cover. [tests/Hexalith.Memories.Cli.Tests/Cli/SearchResultFormatterTests.cs] _(blind, LOW)_
- [x] [Review][Defer] `SearchExplanation.AxisDetails` and `MemoryUnit.Metadata` iterate the underlying `Dictionary<,>` in server-insertion order — test-fragility risk for golden snapshots if the server ever re-orders fields. No AC broken; sorting keys is a test-determinism enhancement, not a 7.2-scope fix. — deferred, out-of-scope for 7.2
- [x] [Review][Defer] `MetadataField.Confidence` / `FusedScoredResult.CompositeScore` being `NaN`/`Infinity` would print as literal `NaN`/`Infinity` and emit bare `NaN` into the JSON envelope (rejected by strict JSON parsers). Server contracts don't enable `AllowNamedFloatingPointLiterals`, so this is a contract-level concern rather than a CLI defect. — deferred, contract-level concern
- [x] [Review][Defer] `IOutputFormatter<T>.Write` is synchronous and takes no `CancellationToken`, so a broken downstream pipe (`| head -1` on a large JSON body) surfaces as "Unexpected error contacting Memories Server" instead of a clean broken-pipe exit. Changing the interface signature is architectural and out-of-scope for 7.2. — deferred, architectural
- [x] [Review][Defer] `Uri.EscapeDataString` on `caseId`/`memoryUnitId` produces `%2F` in path segments; ASP.NET Core returns 404 for encoded slashes by default, yielding an opaque NotFound. IDs containing `/` are unusual; server-level concern. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810] — deferred, server behavior
- [x] [Review][Defer] `MemoriesClient.BuildSearchPath` constructs `"api/search?..."` as a relative URI; if `--endpoint` includes a subpath (e.g., `http://host:5000/v1`), `HttpClient` resolution drops the subpath. Story 7.1 owns endpoint normalization; no 7.2 AC tests subpath endpoints. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs BuildSearchPath] — deferred, inherited from 7.1
- [x] [Review][Defer] `--max-results abc` (non-integer) surfaces via System.CommandLine's default parser error and may emit exit code 1 rather than the 7.2 "plumbing = 2" contract. Parser-level concern, consistent with 7.1 baseline. [src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs:Options] — deferred, System.CommandLine default behavior

## Dev Notes

### Inherited from Story 7.1 (do not re-derive)

- **All 8 ADRs** (ADR-7.1-001 through ADR-7.1-008). Specifically: ADR-7.1-002 (no `IMemoriesClient` interface — `MemoriesClient` stays concrete; new methods `HybridSearchAsync` / `SearchAsync` / `GetMemoryUnitAsync` land as new public methods on the existing type), ADR-7.1-005 (`--endpoint`, `--token`, `--verbose` are the 7.1 globals; this story adds `--format` as the fourth), ADR-7.1-007 (single owner for connection-failure bridge — do **not** split the error surface across formatters; the executor still owns all failures), ADR-7.1-008 (System.CommandLine — `--format` uses the same `Option.Recursive = true` pattern for inheritance).
- **18-item anti-pattern list from 7.1** — most relevant here: #1 (no 7.3/7.4/7.5 work leaks into 7.2), #8 (no `memories` subprocess in CI), #12 (never log or emit the token — formatter output must not echo it), #14 (no emoji — `[human]` / `[ai]` prefixes are plain ASCII), #18 (`CliCommandExecutor` owns every network-touching command, including the new `search query` and `search inspect`).
- **Implementation contracts** from 7.1's Dev Notes — exit-code table (0/1/2/130; 7.2 emits only 0 and 2), CLI logging policy (stderr only for errors, `Information` gated on `--verbose`), JSON serialization (shared `MemoriesJsonContext.Options` + new `CliJsonContext` for CLI-specific envelopes, both source-generated), `CliCommandExecutor` endpoint-ownership contract (handlers do NOT call the resolver directly).

### Task dependency sketch (for parallelizing dev agents)

The 10 Tasks can run roughly in this order, with identified parallel streams:

- **Stream A (infrastructure):** Task 1 (formatter interface + router + `--format` option) → Task 2 (JSON envelope + source-gen context). These block everything else.
- **Stream B (Client.Rest extensions) — runs parallel to Stream C once Stream A is in:** Task 4 (`HybridSearchAsync` / `SearchAsync` / `GetMemoryUnitAsync` methods + request DTOs + unit tests for the HTTP client).
- **Stream C (existing-command formatters) — runs parallel to Stream B:** Task 3 (formatters for `IReadOnlyList<TenantSummary>` and `ConfigShowData`, rewire existing commands). AC #1 regression guard (Task 9.2) lands with this stream.
- **Stream D (new-command wiring) — needs Streams A + B + a minimum of Task 6's search formatters:** Task 5 (`memories search query`), Task 6 (`HybridSearchResult` / `SearchResult` formatters with explain + degradation handling), Task 7 (`memories search inspect` + `MemoryUnit` formatters).
- **Stream E (docs + tests + packaging smoke):** Task 8 (docs), remaining Task 9 tests, Task 10 integration test. Runs last.

Sequential execution (1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10) is valid and simpler; parallelization (A → {B ‖ C} → D → E) buys maybe 20% wall-clock for a human dev and is noise for an LLM dev agent. Use linear unless throughput matters.

### New architectural decisions (locked in this story)

**ADR-7.2-001 — JSON envelope with explicit schema version + deprecation policy.**
- **Decision:** Every `--format json` response is `{ "schemaVersion": 1, "command": "<name>", "data": <shape> }`.
- **Rationale:** Scripts that integrate with the CLI (CI, LLM agents, Phase 2 REST clients) need a stable contract and a versioning lever. A top-level envelope is cheaper than per-field versioning and gives us one place to extend with `warnings` or `pagination` in later stories.
- **Deprecation policy (contractual):**
    - **Additive changes (adding a new optional field):** non-breaking; stay on `schemaVersion = 1`. Example: 7.3 adds `error`; 7.5 adds `traceId`.
    - **Renaming or removing a field, or changing a field's type / semantics:** requires bumping to `schemaVersion = 2`.
    - **Support window:** `schemaVersion = 1` and `schemaVersion = 2` must coexist for **at least one full release cycle** (one minor or major package version) after the bump. Consumers get one release to migrate before v1 is withdrawn. Opt-in to v2 via a future flag (e.g., `--schema-version 2`) — default remains v1 during the coexistence window.
    - **Silent meaning change is never allowed** even within v1 (e.g., a boolean flipping default, an enum value changing spelling). That is a bug class, not an evolution.
- **Reconsider at:** Story 7.3 if error payloads need an envelope slot (likely — `error` field alongside `data`), Story 7.5 if telemetry needs a `traceId` slot. Either is additive and stays on `schemaVersion = 1`.

**ADR-7.2-002 — Human format = 7.1 default (byte-for-byte).**
- **Decision:** `memories tenant list` and `memories config show` produce the same stdout as Story 7.1 when `--format` is absent or equal to `human`.
- **Rationale:** Any breaking change to the default format is a silent breaking change for every script written against 7.1. Preserve the contract, add JSON and Table as **additive** paths.
- **Reconsider at:** This decision holds until **one of these signals fires** — either gives consumers a clear breaking-change contract:
    - (a) `Hexalith.Memories.Cli` NuGet package crosses a SemVer major boundary (e.g., `1.x.x → 2.0.0`), OR
    - (b) JSON output `schemaVersion` bumps from 1 → 2 (per ADR-7.2-001) and a coordinated human-format change ships in the same release.
  Neither happens within Stories 7.2–7.5.

**ADR-7.2-003 — Table format via `string.PadRight`, no rendering library.**
- **Decision:** Table layout uses plain `string.PadRight(maxWidth)` for column alignment and `new string('-', totalWidth)` for the separator row.
- **Rationale:** A rendering library (`Spectre.Console.Table`, `ConsoleTables`) is a transitive dependency we do not need. The volume of tabular output is small; alignment code is ~20 lines. ADR-7.1-008 already committed to `System.CommandLine` and `Spectre.Console.Cli` was the rejected alternative — do not let `Spectre.Console.*` creep in under a different SKU.
- **Reconsider at:** If a command ever needs multi-line cells, nested tables, or colored rows, reopen.

<!-- ADR-7.2-004 (plain-text prefixes, not colour) demoted to a one-line anti-pattern — it's a reapplication of 7.1 Anti-pattern #14, not a new decision. See 7.2 anti-pattern #2 below. -->

<!-- ADR-7.2-005 (`search inspect` placement under `search`) collapsed into Task 7.1 — the rationale is obvious from the command-surface discussion in the TL;DR; not worth an ADR. -->

### Repo state the dev agent must rely on

- Story 7.1 is at `status: review` and is implemented end-to-end — every file listed in its File List (`src/Hexalith.Memories.Cli/*`, `src/Hexalith.Memories.Client.Rest/*`, `tests/Hexalith.Memories.Cli.Tests/*`) is on `main` as of commit `369bdb3`. The dev agent extends these — does not rebuild them.
- The server endpoints used by this story are all already wired and stable since Epics 1–5:
    - `GET /api/search?tenantId=&query=&axis=&explain=` — `src/Hexalith.Memories.Server/Program.cs:1483` — returns `HybridSearchResult` when `axis=hybrid`, `SearchResult` otherwise.
    - `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` — `src/Hexalith.Memories.Server/Program.cs:1012` — returns `MemoryUnit` from `Contracts.V1`.
- Contracts already carry every field we need: `MemoryUnit.Metadata` is `Dictionary<string, MetadataField>` (`src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs:28-33`), `MetadataField` is `(Value, Origin, Confidence)` (`src/Hexalith.Memories.Contracts/V1/MetadataField.cs:4`), `MetadataOrigin` serializes as `human` / `ai` via `CamelCaseStringEnumConverter`. `FusedScoredResult` has `CompositeScore`, `SyntacticScore?`, `SemanticScore?`, `GraphScore?` (`src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs:53-72`). `SearchExplanation` has `Caveat` + `AxisDetails` dictionary keyed by axis name → `AxisExplanation(NormalizationMethod, Description)` (`src/Hexalith.Memories.Contracts/V1/SearchExplanation.cs`).
- `MemoriesJsonContext` already registers `HybridSearchResult`, `SearchResult`, `MemoryUnit`, `MetadataField`, `SearchExplanation`, `AxisExplanation`, `Dictionary<string, AxisExplanation>` (`src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs:31-55`). No additions to the contracts context are required by this story.
- The existing `TestDelegatingHandler` pattern in `tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs` is the canonical way to fake HTTP responses. Reuse it — do not introduce WireMock or HttpMessageHandler-style mocks.

### Test strategy

- **Unit tests (Tier 1)** — per-formatter, per-format snapshot tests; `MemoriesClient.HybridSearchAsync` / `SearchAsync` / `GetMemoryUnitAsync` with scripted responses; `OutputFormatterRouter` resolution; unknown-format → InvalidConfigurationException → exit 2; empty-result rendering; caveat verbatim check; metadata-origin prefix check; JSON envelope parse-and-assert.
- **Integration test (Tier 3)** — one new `CliSearchIntegrationTests` that runs the ingest → search happy path through `AspireIngestionPipelineFixture` with `explain=true`. One assertion: composite score present, caveat present.
- **Regression guards** — the 7.1 `config show` golden-file test is the backstop against AC #1 being broken silently. Token-redaction full-output containment (7.1 Task 6.5) is extended to cover the new commands.
- **Packaging smoke** — update `tools/verify-cli-pack.ps1` and `.sh` from 7.1 Task 8.1 to additionally run `memories --format json tenant list` and `memories search query --help` end-to-end; the smoke script catches packaging drift that CI does not.

### Compliance scope disclaimer

Search invocations run through `memories search query` in Story 7.2 are **not** audit-logged at the tenant level. FR67 (per-tenant search and access telemetry for audit purposes) is owned by **Story 7.5** — structured logging, OpenTelemetry correlation IDs, and custom metrics land there. Compliance reviewers reading 7.2 should not expect GDPR Article 30 record-of-processing artifacts from this story; the 7.2 scope is CLI output formatting and explain display, not observability.

### Trust boundaries (clarification for security review)

The CLI is a **display layer** over server-provided data. Story 7.2 formatters render `TenantSummary`, `HybridSearchResult`, `MemoryUnit`, etc. **verbatim as returned by the server** — no content scrubbing, no field redaction beyond what 7.1 already defined.

What is and isn't the CLI's responsibility, for the purpose of this story:

- **CLI-owned:** API-token scrubbing in `--verbose` exception messages (7.1 Task 10.4). Token-over-http refusal (7.1 Anti-pattern #17). Never emitting the configured `ApiToken` value via any formatter (extension of 7.1 Task 6.5 full-output containment to the new commands — Task 9.9). The `config show` projection that emits `tokenConfigured: true|false` instead of the token value (ADR-7.1-003 + Task 3.1 `ConfigShowData`).
- **NOT CLI-owned:** content scrubbing of data at rest. If a caller ingested a memory unit whose `SourceUri` contains embedded credentials (`https://user:token@host/...`), the JSON formatter will render that URI faithfully. **This is a server/ingest-pipeline concern** (a future story under Epic 1 or 8 — not this one). The CLI is not a data-loss-prevention layer; it renders what the server stores.

This boundary is called out explicitly so security review can file DLP concerns against the correct story (server ingest) rather than 7.2.

### Architecture guardrails (carried forward from 7.1)

- **Capability alignment, not feature parity** — CLI is the reference implementation; MCP (Phase 1.5) will expose a narrower agent-oriented subset. Do not design `--format` to accommodate a hypothetical MCP use case; MCP speaks its own protocol.
- **MVP REST is CLI routing only (D5)** — consume the existing Minimal API endpoints as-is; no new server endpoints, no pagination expansion in 7.2.
- **One-way dependency direction** — `Cli → Client.Rest → Contracts`. Formatters live in `Cli` only. `Client.Rest` gets the new `SearchAsync` / `HybridSearchAsync` / `GetMemoryUnitAsync` methods (data transport); formatting is a CLI concern.
- **DAPR is not a CLI concern** — no `AddDaprClient()`, no `WithDaprSidecar()`, no `DaprClient.GetSecretAsync()`. Same rule as 7.1 AC #9.
- **Phase Compatibility Requirement** — `--format` is a root global, so Phase 1.5's new command groups inherit it automatically. Do not hard-code per-command format handling.

### Anti-patterns to avoid (7.2-specific, layered on top of 7.1's list)

1. **Paraphrasing the confidence caveat.** Print `SearchExplanation.Caveat` byte-for-byte — do not translate "measure query-result relevance" into "score the relevance of results." The PRD treats that exact phrasing as a trust-boundary contract (`prd.md:465-470`).
2. **Using colour or emoji for metadata origin.** Violates 7.1 anti-pattern #14 and pipe-friendliness. Prefixes `[human]` / `[ai]` only.
3. **Emitting 7.3-style recovery suggestions from a formatter.** If search returns zero results, the default human formatter prints nothing. The nudge `"No results. This tenant has no memory units yet..."` is **Story 7.3's work**; adding it here makes 7.3 a rewrite instead of an addition.
4. **Changing `memories config show` default output.** If a reviewer or the dev agent says "while we're here, let's move config show to the JSON envelope" — that breaks AC #1 and ADR-7.2-002. The 7.1 key=value output is frozen under `--format human` / no flag.
5. **Extending `FusedScoredResult` with a `Metadata` dict to avoid the `search inspect` subcommand.** Server change creep. Metadata-origin display has a home: `search inspect`. Leave the search result shape as-is.
6. **Introducing a table-rendering library** (`Spectre.Console.Table`, `ConsoleTables`). ADR-7.2-003 explicitly rejects this. `string.PadRight` is sufficient for the volumes 7.2 handles.
7. **Hardcoding axis names in formatters.** Iterate `SearchExplanation.AxisDetails` dictionary keys — if Epic 9 ever adds a fourth axis, formatters must not need a rewrite.
8. **Adding `--format yaml` "because it's easy".** Epics 7–11 do not need it. More formats = more test matrix = more `schemaVersion` drift risk. Human + JSON + Table only.

### Definition of Done

1. `src/Hexalith.Memories.Cli/Output/` contains `OutputFormat`, `IOutputFormatter<T>`, `OutputFormatterRouter`, `CliJsonContext` (source-generated), `CliOutputEnvelope<T>`, and one formatter per `(OutputFormat, model-type)` pair for tenant list, config show, `HybridSearchResult`, `SearchResult`, `MemoryUnit`.
2. `--format <human|json|table>` is a root-level global option. Default `human`. Unknown values exit with code 2 and a one-line stderr message.
3. `memories tenant list` and `memories config show` produce identical stdout to Story 7.1 when `--format` is absent or `human`. Both commands additionally support `--format json` (envelope `schemaVersion=1`) and `--format table`.
4. `memories search query --tenant X --query "..."` calls `GET /api/search?...&axis=hybrid` by default; dispatches to `HybridSearchAsync` or `SearchAsync` in `MemoriesClient` based on `--axis`. Network errors route through `CliCommandExecutor`.
5. `memories search query --explain` prints per-result composite + per-axis scores + normalization method per axis, plus the server's `SearchExplanation.Caveat` verbatim exactly once per response.
6. `memories search inspect --tenant X --case Y --id Z` renders a `MemoryUnit` with every metadata field labelled lowercase `[human]` or `[ai]` (matching the `MetadataOrigin` camelCase JSON serialization) and its confidence score. JSON format wraps the full `MemoryUnit` in the envelope.
7. Unit tests cover: all three formats for each wired command, 7.1 regression golden for `config show`, token-redaction extended to the new commands, unknown-format exit behaviour, caveat-verbatim assertion, metadata-origin prefix assertion.
8. One integration test (`CliSearchIntegrationTests`) goes end-to-end ingest → hybrid search with explain via `AspireIngestionPipelineFixture`. Asserts composite score present, caveat present.
9. `docs/dev/cli-output-formats.md` documents the envelope, examples per command, and the versioning policy. `README.md` "CLI (preview)" section links to it.
10. `tools/verify-cli-pack.ps1` and `.sh` updated to smoke-test `--format json` and the new `search query --help`.
11. `dotnet build Hexalith.Memories.slnx` succeeds with `TreatWarningsAsErrors=true`. Every Story 7.1 test still passes without edits (51 Cli.Tests + existing Server/Contracts suites).

### References

- Epic 7 overview and Story 7.2 acceptance criteria: [Source: `_bmad-output/planning-artifacts/epics.md:1415-1442`]
- FR55 (output formats), FR57 (discoverable actions — Story 7.3's scope, guardrail here), FR63 (composite + per-axis scores), FR64 (metadata origin + confidence), FR65 (provenance): [Source: `_bmad-output/planning-artifacts/prd.md:901-916`]
- Confidence-score caveat contract (verbatim requirement): [Source: `_bmad-output/planning-artifacts/prd.md:453-470`]
- Output-format spec table: [Source: `_bmad-output/planning-artifacts/prd.md:761-767`]
- Server `/api/search` endpoint and response types: [Source: `src/Hexalith.Memories.Server/Program.cs:1483-1739`]
- Server `/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` endpoint: [Source: `src/Hexalith.Memories.Server/Program.cs:1012`]
- Contract types (do not duplicate): [Source: `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`, `src/Hexalith.Memories.Contracts/V1/SearchResult.cs`, `src/Hexalith.Memories.Contracts/V1/SearchExplanation.cs`, `src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs`, `src/Hexalith.Memories.Contracts/V1/MetadataField.cs`, `src/Hexalith.Memories.Contracts/V1/MetadataOrigin.cs`]
- JSON context registration (already includes all shapes): [Source: `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`]
- Existing CLI wiring to extend: [Source: `src/Hexalith.Memories.Cli/Program.cs`, `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`, `src/Hexalith.Memories.Cli/Commands/CliGlobalOptions.cs`, `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs`, `src/Hexalith.Memories.Cli/Commands/ConfigShowCommand.cs`, `src/Hexalith.Memories.Cli/Execution/CliCommandExecutor.cs`]
- Story 7.1 (inherited ADRs, anti-patterns, Implementation Contracts): [Source: `_bmad-output/implementation-artifacts/7-1-cli-foundation-and-command-structure.md`]
- `AspireIngestionPipelineFixture` for the integration test: [Source: `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`]
- `TestDelegatingHandler` for unit tests: [Source: `tests/Hexalith.Memories.Cli.Tests/TestDelegatingHandler.cs`]
- `CliTenantListIntegrationTests` as the pattern for the new `CliSearchIntegrationTests`: [Source: `tests/Hexalith.Memories.IntegrationTests/Cli/CliTenantListIntegrationTests.cs`]

## Dev Agent Record

### Agent Model Used

claude-opus-4-6[1m]

### Debug Log References

- Story created from current repo state on 2026-04-16.
- Target story selected automatically from `_bmad-output/implementation-artifacts/sprint-status.yaml` — first backlog entry was `7-2-output-formats-and-explain-display`.
- Epic 7 status already `in-progress` (Story 7.1 transitioned it). No epic status change in this story.

### Completion Notes List

- **Implementation (2026-04-16, Dev — Amelia):** all 10 tasks complete. `dotnet build Hexalith.Memories.slnx -c Debug` succeeds with `TreatWarningsAsErrors=true`, 0 warnings. Non-integration test suite: **1412 tests passing** (CLI.Tests 93 = 55 inherited 7.1 + 38 new; Contracts 288; Server 1017; Benchmarks 14). Integration test `CliSearchIntegrationTests` compiles; runnable locally via `dotnet test --filter Category=Integration` (requires Docker/Aspire topology; not exercised in this dev-agent session — see Task 10.1 CI-precondition callout).
- **7.1 regression guard:** `ConfigShowGoldenFileTests` locks byte-for-byte Story 7.1 output (AC #1 / ADR-7.2-002). All pre-existing 7.1 tests pass unmodified.
- **JSON envelope:** schemaVersion=1 verified in 4 per-command tests (`TenantListFormatterTests`, `ConfigShowFormatterTests`, `SearchResultFormatterTests`, `MemoryUnitFormatterTests`). Source-gen context (`CliJsonContext`) chained after `MemoriesJsonContext` for downstream types.
- **Caveat verbatim (AC #5):** `SearchResultFormatterTests.HybridHuman_WithExplain_PrintsCaveatFirstAndOnce` asserts the caveat appears exactly once per response and BEFORE the first result row (survives `| head -N` piping per Task 6.3).
- **Metadata origin visual distinction (AC #6):** `MemoryUnitFormatterTests.Human_MixedOrigins_PrintsLowercaseOriginPrefixes` asserts `[human, confidence=...]` and `[ai, confidence=...]` with explicit case-sensitive comparison; Title-case `[Human` / `[Ai,` ruled out.
- **CLI-side cap (Task 5.6):** `SearchQueryCommandTests.Invoke_MaxResultsAboveCeiling_WritesMessageAndExitsPlumbing` verifies the 1000-row ceiling triggers before the HTTP call.
- **Conditional --query validation (Task 5.3):** `SearchQueryCommandTests.Invoke_MissingQueryOnGraph_DoesNotTriggerQueryValidation` locks the graph-axis carve-out so a later story can add `--graph-start-node-id` additively.
- **--help/--version always succeed (Task 1.5):** `UnknownFormatTests.ApplyGlobalOptions_UnknownFormatWithHelp_DoesNotThrow` asserts the skip.
- **Token-redaction continuity (Task 9.9):** `TokenRedactionTests` now loops over all three `OutputFormat` values for `search query` and `search inspect`, asserting the sentinel never appears in combined stdout+stderr.
- **Server-param defaults omitted (Task 4.2):** `MemoriesClientSearchTests` asserts no `maxResults=10`, no `caseId=`, no `offset=`, no `explain=true` on the wire when the CLI omits or defaults them. Graph axis also omits `query=`.
- **ISO-8601 dates (Task 7.4):** `MemoryUnitFormatterTests.Human_IngestedAt_UsesIsoRoundTripFormat` locks the culture-invariant `"o"` format against CI locale drift.
- **Cold-start (Task 9.10 manual check):** Not run in this session — the dev agent is operating inside an automated harness without a representative warm-machine baseline. Reviewer should run `memories --version` three times and record best-of-three before merging; 1.2s budget.
- **Stub trim (Task 5.5):** only `"search"` removed from `RootCommandFactory.CommandGroups`; `ingest`, `traverse`, `case`, `status`, `explore`, `handlers`, `quickstart` remain as `NotImplementedCommand` stubs.
- **Pre-implementation metadata:**
    - Story file created with repo-grounded implementation guidance, strict scope boundaries against Stories 7.3–7.5, and inheritance of all Story 7.1 ADRs + anti-patterns + Implementation Contracts by reference.
    - Sprint status updated: `7-2-output-formats-and-explain-display: backlog → ready-for-dev → in-progress → review`.
- **Revision 5 (2026-04-16, post-advanced-elicitation stakeholder round table / customer support theater / comparative analysis / SCAMPER / thesis defense):** applied 8 findings from non-dev perspectives.
    - **F13 (Task 9.10 new):** manual cold-start regression check — dev agent runs `memories --version` three times, flags if >1.2s (200ms budget over 7.1's `<1s`). Catches gross DI misuse; not a CI gate.
    - **F14 (Dev Notes "Trust boundaries"):** explicit paragraph clarifying the CLI renders server data verbatim; content scrubbing at rest (token-in-SourceUri etc.) is the server's responsibility, not 7.2's. Prevents recurring security-review misfiling.
    - **F15 (Dev Notes "Compliance scope disclaimer"):** explicit note that tenant-level search audit logging is Story 7.5 (FR67), not 7.2. Aligns compliance-review expectations.
    - **F16 (ADR-7.2-001 expanded):** concrete deprecation policy — additive changes stay on `schemaVersion=1`; renames/removes bump to v2; v1 and v2 coexist for at least one release cycle; silent meaning change is always a bug. Makes the versioning contract actually contractual.
    - **F17 (Task 8.1 docs):** explicit combined-flag example — `--explain --format json` together, showing `data.explanation` is present only when both set. Closes a common-pitfall gap surfaced by customer-support roleplay.
    - **F18 (Task 8.1 docs):** pipe-safety guidance — table format is interactive-only; use JSON for scripts. Stops misuse.
    - **F19 (Tasks section — Task Summary Table):** new one-line-per-task table at the top of the Tasks section with blocked-by dependencies and AC coverage. Biggest readability win at this story length; orients scanning dev agents without forcing a 200-line read.
    - **F20 (Task 8.1 docs):** future-compat note — `OutputFormat` enum is an extensible surface; later stories may add `tsv`, `yaml`, `csv` additively without breaking existing formats or `schemaVersion`.
- **Revision 4 (2026-04-16, post-advanced-elicitation mentor-apprentice / red-team / first-principles / reverse-engineering / 5-whys):** applied 8 of 9 surfaced findings. Withdrew **F1** (`[JsonIgnore]` on `ResolvedConfig.ApiToken`) after closer inspection — `ConfigShowData` is a purposeful projection of `ApiToken → bool TokenConfigured`, not just token hiding; `[JsonIgnore]` alone drops the required signal.
    - **F2 (Task 10.2):** integration test now pins PRD-mandated caveat substring `"measure query-result relevance"` (case-sensitive) — catches server-side drift at the CLI boundary.
    - **F3 (Task 5.2):** reconciled the `--query (required)` phrasing with Revision 2's conditional rule in Task 5.3 — removes self-contradiction.
    - **F4 (Task 5.6 new):** CLI-side cap on `--max-results` at 1000; exceeding it prints a clear error to stderr + exits 2 **before** the HTTP call. Protects local process from memory blowup on unbounded server responses; red-team finding.
    - **F5 (Task 7.4):** empty `Metadata` dict in `search inspect` human format now prints `metadata: (none)` instead of a bare header.
    - **F6 (Task 1.5):** `--help` / `--version` invocations always succeed regardless of invalid `--format` value — the validation skip only happens when help/version tokens are present. Real UX paper-cut from red-team.
    - **F10 (Task 10.1):** explicit callout that CI-runnability for the integration test is a repo-pipeline precondition (Story 11.1's scope). Recommends local `dotnet test --filter Category=Integration` as the pre-`review` check.
    - **F11 (new Dev Notes sketch):** dependency graph across 5 streams (A infrastructure → {B client, C existing-command formatters} → D new-command wiring → E docs/tests/smoke). Linear execution 1-10 still valid; this is for parallelizing agents.
    - **F12 (ADR-7.2-002):** "Reconsider at" clause now cites two concrete signals — CLI SemVer major bump OR `schemaVersion` bump with coordinated human-format change. Eliminates the "never, some future major" vagueness.
- **Revision 3 (2026-04-16, post-advanced-elicitation pre-mortem / Occam / chaos-monkey / FMA / hindsight):** applied 8 fixes spanning pre-implementation sanity and future-story foresight.
    - **Task 1.3 (router error semantics):** `FirstOrDefault` + typed `FormatterNotRegisteredException` instead of `.Single(...)` — better dev-agent diagnostic path.
    - **Task 2.1 (envelope locked at 3 fields):** explicitly prohibited speculative slot additions (`error`, `warnings`, `pagination`, `traceId`) — each lands with the story that needs it. Prevents dead contract surface.
    - **Task 5.4 (exit-code policy):** spelled out that `MemoriesRemoteException` (including HTTP 503 degradation-total) continues at exit code 2 through 7.2; split to exit 1 is Story 7.3's scope. Added case-insensitive axis dispatch note (matches server behavior at `Program.cs:1565`).
    - **Task 6.6a (single-owner bridge):** marked the degradation notice as a bridge with explicit 7.3-replaces-it semantics, mirroring 7.1 ADR-7.1-007's pattern for the AC #11 connection-failure bridge.
    - **Task 7.4 (ISO-8601 DateTimeOffset):** pinned `ingestedAt.ToString("o", CultureInfo.InvariantCulture)` — prevents CI locale drift (en-US vs fr-FR date ordering) breaking golden-file tests.
    - **Task 9.2 (capture-first golden):** added "run 7.1 code, capture verbatim, then encode as golden" instruction — the dev agent must not invent the expected string since no 7.1 test pins it today (`Uri.ToString()` vs string interpolation trailing-slash behavior is not load-bearing-tested).
    - **Task 10.1 (integration test filterability):** cited the `[Trait("Category", "Integration")]` + `dotnet test --filter "Category!=Integration"` pattern so dev agents without Docker aren't blocked by fixture setup failures.
    - **Occam cleanup:** removed ADR-7.2-005 (obvious from TL;DR), demoted ADR-7.2-004 to an anti-pattern (it was a reapplication of 7.1 #14, not a new decision), folded Task 8.2 into Task 8.1, folded Task 6.6b into Task 6.3, removed redundant Risk #4. Net: story clearer without losing precision.
- **Revision 2 (2026-04-16, post-party-mode review):** addressed 7 concrete gaps surfaced by Bob/Amelia/Winston/Quinn.
    - **Task 1.4** clarified: `--format` `Option<OutputFormat>` lives on `CliGlobalOptions`; resolved value lives on `CliConsole.Format` (same pattern as `CliConsole.Verbose`). Eliminates ambiguity about which class holds what.
    - **Task 2.2** pinned envelope type parameters — `CliOutputEnvelope<IReadOnlyList<TenantSummary>>` (not `TenantSummary[]`, which resolves to a different `JsonTypeInfo` in source-gen).
    - **Task 4.2** clarified "omit parameters, don't send defaults" — no literal `offset=0` on the wire; `Query` made nullable in the request DTO to support the graph-axis case.
    - **Task 5.3** made `--query` validation conditional on axis — graph-only searches need a seed, not a query (server allows empty query on graph axis per `Program.cs:1640-1648`).
    - **Task 5.5** enumerated the stubs that must remain after this story (ingest, traverse, case, status, explore, handlers, quickstart) to prevent the dev agent over-removing from `CommandGroups`.
    - **Task 6.3** moved the `--explain` caveat to print **before** results in human format (survives `| head -N` piping, preserves the PRD compliance-enablement guarantee). Table format still appends caveat after the table to keep header alignment intact.
    - **Task 6.6a** added — degradation notice for `HybridSearchResult.Degraded == true` (human + table prepend a one-line notice; JSON passes through verbatim). No recovery suggestion here — that's Story 7.3's scope.
    - **Task 7.4 + AC #6 + DoD #6** — locked `[human]` / `[ai]` origin prefix casing to lowercase, matching the `MetadataOrigin` camelCase JSON serialization.
    - **Task 9.2** — golden-file regression test injects a fixture `ResolvedConfig` directly instead of reading the live pipeline. Prevents cross-platform URI normalization flakes and CI environment-variable drift.
    - **Task 10.2** — cited the exact bounded-wait pattern from `CliTenantListIntegrationTests` (`ActivationTimeout = TimeSpan.FromMinutes(2)` + deadline polling with 500ms sleeps against `GET /api/ingest/{instanceId}`). Eliminates unbounded-wait flake risk.

### File List

**Story + sprint status**

- `_bmad-output/implementation-artifacts/7-2-output-formats-and-explain-display.md` (modified)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)

**New CLI output infrastructure**

- `src/Hexalith.Memories.Cli/Output/OutputFormat.cs` (new)
- `src/Hexalith.Memories.Cli/Output/IOutputFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/OutputFormatterRouter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/FormatterNotRegisteredException.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Json/CliOutputEnvelope.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Json/ConfigShowData.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/TableWriter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/JsonEnvelopeWriter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/SnippetTruncator.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/TenantListHumanFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/TenantListJsonFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/TenantListTableFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/ConfigShowHumanFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/ConfigShowJsonFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/ConfigShowTableFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultJsonFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultTableFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/SearchResultHumanFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/SearchResultJsonFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/SearchResultTableFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitHumanFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitJsonFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitTableFormatter.cs` (new)

**New CLI commands**

- `src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs` (new)
- `src/Hexalith.Memories.Cli/Commands/SearchInspectCommand.cs` (new)

**Modified CLI wiring**

- `src/Hexalith.Memories.Cli/CliServices.cs` (modified)
- `src/Hexalith.Memories.Cli/Program.cs` (unchanged by this story, reviewed)
- `src/Hexalith.Memories.Cli/Commands/CliGlobalOptions.cs` (modified — added `FormatOption`)
- `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` (modified — `--format` wiring, `search` group, help/version skip)
- `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs` (modified — routes through formatter)
- `src/Hexalith.Memories.Cli/Commands/ConfigShowCommand.cs` (modified — routes through formatter)
- `src/Hexalith.Memories.Cli/Execution/CliConsole.cs` (modified — added `Format` property)

**New Client.Rest surface**

- `src/Hexalith.Memories.Client.Rest/SearchRequest.cs` (new)
- `src/Hexalith.Memories.Client.Rest/HybridSearchRequest.cs` (new)
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (modified — `HybridSearchAsync`, `SearchAsync`, `GetMemoryUnitAsync`, `BuildSearchPath`)

**New unit tests**

- `tests/Hexalith.Memories.Cli.Tests/Cli/OutputFormatterRouterTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConfigShowGoldenFileTests.cs` (new — 7.1 regression golden)
- `tests/Hexalith.Memories.Cli.Tests/Cli/TenantListFormatterTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConfigShowFormatterTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/SearchResultFormatterTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/MemoryUnitFormatterTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/SearchQueryCommandTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/UnknownFormatTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSearchTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/TokenRedactionTests.cs` (modified — extended to search commands across all three formats)

**New integration test**

- `tests/Hexalith.Memories.IntegrationTests/Cli/CliSearchIntegrationTests.cs` (new)

**Docs + smoke scripts**

- `docs/dev/cli-output-formats.md` (new)
- `docs/dev/cli-config.md` (modified — added "See also" cross-reference)
- `README.md` (modified — CLI preview section)
- `tools/verify-cli-pack.ps1` (modified — added `search query --help` + `--format json tenant list --help` smoke calls)
- `tools/verify-cli-pack.sh` (modified — same additions)

### Change Log

| Date | Version | Description |
| :--- | :--- | :--- |
| 2026-04-16 | 0.1 | Story context created. Status: backlog → ready-for-dev. |
| 2026-04-16 | 0.2 | Post-party-mode review revision: 10 concrete fixes across Tasks 1.4, 2.2, 4.2, 5.3, 5.5, 6.3, 6.5, 6.6a, 7.4, 9.2, 10.2. Locked origin prefix casing. Added degradation notice handling. Eliminated integration-test flake risk. |
| 2026-04-16 | 0.3 | Post-advanced-elicitation revision (pre-mortem / Occam / chaos / FMA / hindsight): 8 fixes across Tasks 1.3, 2.1, 5.4, 6.6a, 7.4, 9.2, 10.1. Removed ADR-7.2-005, demoted ADR-7.2-004. Typed `FormatterNotRegisteredException`. Capture-first golden file. ISO-8601 date formatting. Degradation notice as single-owner bridge. Envelope fields locked at three. Net complexity down despite 8 additions (Occam cleanup compensates). |
| 2026-04-16 | 0.4 | Second advanced-elicitation revision (mentor-apprentice / red-team / first-principles / reverse-engineering / 5-whys): 8 of 9 fixes applied (F1 withdrawn). New Task 5.6 (`--max-results` cap). Task 1.5 `--help` + invalid `--format` UX fix. Task 10.2 caveat substring pin. Task 7.4 empty-metadata handling. Task 5.2 `--query` phrasing reconciled. ADR-7.2-002 "Reconsider at" concretized. Added Task dependency-stream sketch for parallelizing agents. |
| 2026-04-16 | 0.5 | Third advanced-elicitation revision (stakeholder / support-theater / matrix / SCAMPER / thesis-defense): all 8 non-dev-perspective findings applied. New Task 9.10 (cold-start regression check). Dev Notes gained "Compliance scope disclaimer" and "Trust boundaries" sections for reviewer clarity. ADR-7.2-001 gained a concrete deprecation policy (v1+v2 coexistence window). Task 8.1 docs expanded with combined-flag example, pipe-safety, and format-extensibility notes. Tasks section gained a top-level Task Summary Table for readability at length (biggest usability win). Story 7.1 status flipped to `done` in sprint-status (no content change here, noted for accuracy). |
| 2026-04-16 | 1.0 | Full implementation complete (Dev — Amelia). All 10 Tasks + 54 subtasks checked. 25 new formatter/infrastructure files under `src/Hexalith.Memories.Cli/Output/`, 2 new CLI commands (`search query`, `search inspect`), 3 new `MemoriesClient` methods + 2 request DTOs, 38 new unit tests (91→93 total with 2 existing tests extended), 1 new integration test. `docs/dev/cli-output-formats.md` published. `README.md` + `cli-config.md` cross-linked. `verify-cli-pack.ps1`/`.sh` extended with Story 7.2 smoke calls. `dotnet build Hexalith.Memories.slnx -c Debug` passes with `TreatWarningsAsErrors=true`, 0 warnings; non-integration test suite green at 1412 passing. Status: in-progress → review. |
| 2026-04-16 | 1.1 | Adversarial code review via `/bmad-code-review` (Blind Hunter + Edge Case Hunter + Acceptance Auditor). 1 decision-needed resolved (empty-metadata table → option b: `metadata: (none)` line), 10 patches applied, 6 deferred to `deferred-work.md`, 1 dismissed. Notable fixes: help/version detection now inspects `OptionResult` symbols (was raw-token scan), `JsonEnvelopeWriter` now AOT-safe via `JsonTypeInfo` lookup, `TableWriter` + `MemoryUnitHumanFormatter` now sanitize embedded CR/LF/TAB, `SnippetTruncator` now surrogate-pair-safe, `--axis` gains CLI-side whitelist + trim, `ConfigShowGoldenFileTests` rewritten to invoke `ConfigShowCommand.Build(...)` end-to-end through the `Uri → string` chain. Build clean; non-integration test suite green at 1414 (+2 new tests). Status: review → done. |
