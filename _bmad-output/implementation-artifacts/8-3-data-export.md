# Story 8.3: Data Export

Status: in-progress

**Effort estimate:** ~5 working days end-to-end — 0.5 day case-export service + snapshot + writer (Task 1), 0.5 day tenant-export service (Task 2), 0.25 day import-key reservation + schema envelope (Task 3), 0.5 day REST endpoints (Task 4), 0.25 day client methods (Task 5), 0.5 day CLI commands + formatters + error codes (Task 6), 1.25 days unit tests (Task 7), 0.25 day integration test (Task 8 — skip-path) or 0.75 day (active), 0.5 day docs + sprint-status + final validation (Task 9). Add 0.5 day rebase cost if Story 8.2 lands additional changes to `Program.cs` or `MemoriesClient.cs` before 8.3 finalizes — 8.2 is currently `review`, so auto-merge is expected on the consistency block but the `MemoriesClient` / `CliJsonContext` / `RootCommandFactory` edges have minor line-adjacency risk.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** Developer-facing **portable JSON export** of a case or an entire tenant. Delivers (a) a **streaming** case-scoped export producing a single JSON document containing the case record, every memory unit in the case (with full metadata), and every graph edge whose source OR target is a memory unit in the case; (b) a tenant-scoped export containing all cases + per-case memory units + graph edges + tenant configuration + tenant registry info; (c) REST endpoints that stream the JSON response via `HttpContext.Response.BodyWriter` (no server-side buffering of the full document); (d) typed client methods on `MemoriesClient` that expose the response stream to the caller; (e) `memories export case | tenant` CLI subcommand group that writes to stdout or a `--output` file; (f) snapshot isolation via a captured `snapshotAt` timestamp that filters memory units by `ingestedAt <= snapshotAt`; (g) forward-compatible schema wrapped in an envelope `{ "schemaVersion": 1, "exportedAt": ..., ... }`. Closes **FR71** (developer can export all memory units, metadata, and graph edges for a case or tenant in a portable format).

**What already exists (do NOT rebuild):**

1. **`CaseService.GetCaseAsync` / `GetMemoryUnitAsync` / `ListAnnotationsAsync`** — `src/Hexalith.Memories.Server/Cases/CaseService.cs`. Single-entity reads (Redis HASH + parse). **Reuse** `ParseMemoryUnitFromHash` and `ParseCaseFromHash` by promoting them to `internal static` helpers so the export writer can hydrate records without duplicating the parsing logic. If they are already `internal` (check at implementation time), just call them; if they are private, refactor to `internal static` in-place and update the CaseService callers. Do NOT rebuild the hash→record mapping.
2. **`IGraphQueryBuilder.BuildListCaseMemoryUnitIds(caseId)`** — `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs:51`. Existing parameterized Cypher that returns memory unit IDs linked to a case via `CONTAINS`. **Reuse** for the case-export enumeration path (authoritative: the graph `CONTAINS` edge is the source of truth for "is this MU in this case", not the Redis hash's `caseId` field). Do NOT SCAN Redis for `{tenantId}:mu:*` and filter by `caseId` — that is slower AND inconsistent with how `ListCasesAsync` already derives membership.
3. **`IGraphQueryBuilder.BuildCountCaseMemoryUnits(caseId)`** — used for the progress-bar denominator in the CLI (`N of M` indicator).
4. **`TenantMetricsService.GetMemoryUnitCountAsync`** — existing `SCAN {tenantId}:mu:*` with `ScanPageSize = 250`. **Use the same SCAN pattern** inside the tenant-export writer to enumerate the full memory-unit list (tenant scope has no case-scoped shortcut available). Copy the `GetAnyServer(_redis)` helper + `RedisException → null` handling idiom. *See "Factor-vs-duplicate decisions" in Dev Notes for the duplicate-once-more rule.*
5. **`TenantRegistryService.GetAsync` + `TenantConfigurationView`** — `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` + `src/Hexalith.Memories.Contracts/V1/TenantConfigurationView.cs`. **Reuse** to hydrate the tenant-export's `tenant` section. Include `TenantEmbeddingConfig` as-is — the record already exposes embedding provider + model + keyed-secret references (never the secret value, per Story 5.5 security posture).
6. **`CaseService.ListCasesAsync` + `ListCaseMembersAsync`** — existing methods returning `List<Case>` + `List<CaseMember>`. **Reuse** verbatim.
7. **`MemoriesJsonContext.Options`** — source-gen JSON options with camelCase + enum converter. **Use unconditionally** for all serialized records. AOT-safe; no reflection fallback. Register any new V1 types (Task 3) in the `[JsonSerializable]` attribute list.
8. **`TenantStatusGuard.ValidateTenantExistsAsync`** — existence-only tenant guard, same choice as Story 8.2's consistency endpoints. Export is a diagnostic/archival operation; MUST be allowed on non-`Active` tenants (Provisioning / Deleting / Failed) so operators can back up a tenant before deletion or capture state from a failed tenant before a repair attempt.
9. **`ValidateTenantId` helper in `Program.cs`** + existing error-response shape `ErrorResponse(code, message, recoverySuggestion)`. **Reuse** for input validation.
10. **CLI scaffolding + command groups** — `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`. Add a new `export` top-level command group with two subcommands (`case` / `tenant`) following the `consistency verify / inspect / repair` shape (Story 8.2). The stubbed `NotImplementedCommand` entries stay for 7.x ancillary groups.
11. **`ErrorMessageCatalog`** + existing error-code pattern (`src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`). Extend with new codes: `EXPORT_TENANT_NOT_FOUND`, `EXPORT_CASE_NOT_FOUND`, `EXPORT_WRITE_FAILED`, `EXPORT_OUTPUT_PATH_INVALID`.
12. **`CommandPayloadRegistry`** + existing formatter pattern. Exports are **raw JSON streams**, not structured payloads — the CLI's formatter router does NOT apply. The `--format` global option is IGNORED for export (documented explicitly in CLI help; a `--format=table` flag on `memories export case` returns a deterministic warning and proceeds with JSON). No new formatter registration.

**What 8.3 adds:**

1. **`TenantExportService`** at `src/Hexalith.Memories.Server/Export/TenantExportService.cs` — sealed class (not an interface; Architecture D9 — mock at HttpClient boundary, not at internal service boundary) that writes the export JSON progressively to a `PipeWriter`. Constructor DI:
   - `[FromKeyedServices("redis")] IConnectionMultiplexer redis`
   - `[FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb`
   - `IGraphQueryBuilder graphQueryBuilder`
   - `CaseService caseService`
   - `CaseActivityService activityService` (unused in 8.3 BUT included for symmetry + cheap to inject; if DI complains, drop it)
   - `TenantRegistryService tenantRegistry`
   - `TenantMetricsService tenantMetrics`
   - `ILogger<TenantExportService> logger`
   Public methods:
   - `Task WriteCaseExportAsync(string tenantId, string caseId, PipeWriter writer, CancellationToken ct)` — writes the case-scope export envelope.
   - `Task WriteTenantExportAsync(string tenantId, PipeWriter writer, CancellationToken ct)` — writes the tenant-scope export envelope.
   Both throw `KeyNotFoundException` if the tenant/case doesn't exist (caller maps to 404). Both throw `ArgumentException` on invalid IDs (caller maps to 400).
2. **`ExportWriter` internal helper** at `src/Hexalith.Memories.Server/Export/ExportWriter.cs` — encapsulates `Utf8JsonWriter`-driven streaming semantics. Owns the envelope shape + flush cadence (flush every 1000 memory units or 1 MiB of unflushed output, whichever comes first). Internal sealed; tested via `TenantExportServiceTests` that asserts the emitted JSON tokens.
3. **`ExportEnvelope` schema records** in `src/Hexalith.Memories.Contracts/V1/` (each sealed `public record`, ITANEO header, registered in `MemoriesJsonContext`). Contract records are ALSO usable for **re-import** in a future story (9.x) — preserve IDs verbatim:
   - `ExportManifest(int SchemaVersion, ExportScope Scope, string TenantId, string? CaseId, DateTimeOffset ExportedAt, DateTimeOffset SnapshotAt)` — SchemaVersion = `1`; `CaseId` is null for tenant-scope. (`CounterpartWorkflowInstanceId` deferred to schema v2 — additive change, no v1 bump needed.)
   - `ExportScope` enum: `Case | Tenant` (camelCase via `CamelCaseStringEnumConverter`).
   - `ExportStatistics(int MemoryUnitCount, int EdgeCount, int CaseCount)` — tallied during streaming; emitted as the final envelope section. `CaseCount = 1` for case-scope; `CaseCount = N` for tenant-scope.
   - `ExportedMemoryUnit(MemoryUnit Unit, IReadOnlyList<string> AnnotationTargets)` — a sealed wrapper record that composes the canonical `MemoryUnit` record + the inbound-annotation target ID list. **Decision (revised):** wrap explicitly rather than emitting hybrid sibling fields inline. Rationale: (a) `JsonSerializer.Deserialize<MemoryUnit>` on an export entry silently drops `annotationTargets` (extra field), leaving downstream consumers with no type-safe path to the annotation data; (b) a wrapper is a single `JsonSerializer.Serialize(writer, exportedMu, MemoriesJsonContext.Options)` call from `ExportWriter` — simpler than custom token emission; (c) the "don't couple domain record to export bookkeeping" argument SUPPORTS wrapping (the coupling lives in the wrapper, not in `MemoryUnit`). Register `ExportedMemoryUnit` in `MemoriesJsonContext`. See "Schema shape" in Dev Notes.
   - `ExportedEdge` — a derived shape that includes the fields ingested callers need for re-hydration: `(string Id, string SourceId, string TargetId, string EdgeType, float Confidence, string Origin, DateTimeOffset CreatedAt, string? VerifiedBy, float? PreviousConfidence)`. NOT `GraphEdge` — `GraphEdge` lacks `VerifiedBy` / `PreviousConfidence` which are present on confidence-promoted edges (Story 4.3). Ship a strict superset so edge history round-trips. Register in `MemoriesJsonContext`. **`Id` carries an XML doc comment warning:** FalkorDB edge IDs are graph-instance scoped (stable within one graph lifetime, NOT stable across graph deletions / recreations). A future re-import MUST NOT use `Id` as re-import identity — edges must be recreated from the `(SourceId, TargetId, EdgeType, CreatedAt)` tuple. Preventing silent identity bugs in a future importer.
   - `ExportedTenantConfig` — wraps `TenantConfigurationView` + tenant registry metadata (`TenantStatus`, `CreatedAt`, `LastUpdated`). Do NOT include any secret-value fields — follow the existing `TenantConfigurationView` model which already redacts them.
4. **REST endpoints in `Program.cs`** (insert after the consistency block — line 1266 in the current main — to keep operator-scope endpoints together):
   - `GET /api/tenants/{tenantId}/cases/{caseId}/export` — validates tenant + case; streams the case export. Content-Type: `application/json`; `Content-Disposition: attachment; filename="{tenantId}-{caseId}-{snapshotAt:yyyyMMdd-HHmmss}.json"`; `X-Export-Schema-Version: 1` response header. No request body.
   - `GET /api/tenants/{tenantId}/export` — streams the tenant export. Same headers; filename `{tenantId}-tenant-{snapshotAt:yyyyMMdd-HHmmss}.json`.
   Both endpoints opt OUT of ASP.NET Core response buffering (request the raw `HttpContext` and use `context.Response.BodyWriter` + `context.Response.StartAsync()` so the first byte flushes before enumeration completes — mandatory for the snapshot-isolation contract).
5. **`MemoriesClient` methods** in `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`:
   - `Task<Stream> ExportCaseAsync(string tenantId, string caseId, CancellationToken ct)` — returns `response.Content.ReadAsStreamAsync()` over an `HttpCompletionOption.ResponseHeadersRead` request so the caller pipelines. Throws `MemoriesRemoteException` with `ErrorResponseDecoder.DecodeAsync` on non-2xx. Caller disposes the stream.
   - `Task<Stream> ExportTenantAsync(string tenantId, CancellationToken ct)` — same shape.
   Error-path parity with existing methods.
6. **CLI commands** at `src/Hexalith.Memories.Cli/Commands/`:
   - `ExportCaseCommand.cs` — `memories export case --tenant <t> --case <caseId> [--output <path>] [--force]`. Default output is stdout (binary-safe — add `Console.OpenStandardOutput()` not `Console.Out`). With `--output <path>`: refuse to overwrite unless `--force` is set (consistent with `dotnet publish -o` default); use a temp-file write + atomic rename (`File.Move(tmpPath, finalPath, overwrite: true)` — the `overwrite:true` overload requires .NET 9+; verify against project TFM).
   - `ExportTenantCommand.cs` — `memories export tenant --tenant <t> [--output <path>] [--force]`.
   - `RootCommandFactory.Build` extended to wire the new `export` group (two subcommands + `--help`-on-no-action pattern that the other groups use).
7. **JSON context registration** — add each new V1 record (`ExportManifest`, `ExportScope`, `ExportStatistics`, `ExportedEdge`, `ExportedTenantConfig`) to `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` `[JsonSerializable(...)]` list. Do NOT rely on reflection fallback.
8. **CLI-side JSON context** — `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs` does NOT need new entries (export is opaque stream-forwarding).
9. **Unit + integration tests** — see AC #11 for authoritative inventory.
10. **`docs/dev/export.md`** — developer-facing doc: schema version + backward-compat policy; envelope shape with a worked example; snapshot-isolation semantics; progress + streaming behavior; expected duration/size per unit count; CLI walkthrough; REST API reference; relation to Story 8.2 (a verified-consistent tenant is recommended but NOT required before export — the export captures the current on-disk state, divergences included).

**What does NOT ship:**

- **Re-import / restore.** 8.3 ships export only. Import is a follow-up (Phase 1.5 — tracked in Epic 9 scope if an EventStore ingestion replay is built, or a dedicated `memories import` command in a Phase 2 story). The export schema is designed to be re-imported: IDs are preserved (Memory Unit ULIDs, Case ULIDs, Edge IDs), timestamps are ISO-8601, enums are camelCase strings.
- **Incremental / delta exports.** Each export is a full snapshot at `snapshotAt`. No change-log / diff mode. Operators can diff two export files externally if needed.
- **Cross-tenant exports.** Case-export and tenant-export are per-tenant. A "platform dump of every tenant" would require iterating the tenant registry — defer to a dedicated ops-only story (not in the MVP scope).
- **Compression.** Responses are plain JSON. ASP.NET Core response compression middleware (if enabled in a deployment configuration) will transparently gzip; no export-specific compression toggle is added. Documented in `docs/dev/export.md`.
- **Binary content embedding.** Memory units store content as **text** (UTF-8 string in the `content` Redis hash field per `IndexSyntacticActivity.cs:68`). Raw source bytes (the original uploaded PDF, etc.) are NOT stored server-side — only the extracted text — so the export carries extracted text, never binary. Documented. A future "attachment export" story can add blob-storage integration if raw artifacts become load-bearing.
- **Snapshot-isolation via transaction / MVCC.** Redis + FalkorDB are not transactional across writes. 8.3's snapshot isolation is **advisory**: at export start, capture `snapshotAt = DateTimeOffset.UtcNow`; during enumeration, emit only memory units with `ingestedAt <= snapshotAt`. New memory units ingested during the export will not appear; in-flight memory units whose `ingestedAt` was before `snapshotAt` but whose graph/vector indexing completes after the export visits the graph layer MAY appear inconsistently (present in edges list but absent from the memory-units list, or vice versa). Documented as a known limitation in `docs/dev/export.md` — use consistency verify (Story 8.2) after a critical export to confirm coverage.
- **Authenticated / signed exports.** No digital signature is applied to the export. Operators using compliance export should sign externally (e.g., `gpg --detach-sign`). Rationale: signing requires a key-management story that is separate from the export mechanism and doesn't fit MVP scope.
- **Export progress over Server-Sent Events (SSE).** Progress is provided via CLI stderr progress line `[N of M memory units, K edges]` recomputed as the stream arrives (the CLI parses the stream and counts emitted records). NOT via an orthogonal SSE channel — keeping one content type (`application/json`) keeps the protocol simple and mockable.
- **MCP tool for export.** Epic 10 (MCP Server) does not ship export as an agent-facing tool. Rationale: export emits the full tenant state (which can include embedding secrets' key references, though not the secrets themselves). Operator-gated access only in MVP.
- **Export of failed / soft-deleted memory units.** Memory units with `status = Failed` (Story 6.3) are INCLUDED in the export (they are part of the tenant's state). Memory units that have been hard-deleted via Story 3.5 are not retrievable by definition. Documented.
- **Export of access-telemetry events (Story 7.5).** The `AccessTelemetryEvent` log stream is deliberately a **stdout-only audit channel** — not persisted in Redis/FalkorDB. Operators collect those via the host's log aggregator; they are NOT part of the data-export scope.
- **Audit logging of export calls.** Export endpoints deliberately do NOT emit `AccessTelemetryEvent` (Risk #9). However, export IS a data-exfiltration surface — compliance-grade audit (who exported what, when) is a real requirement that MVP does NOT satisfy. **Risk-accepted for internal operator use; follow-up story must add a dedicated `ExportTelemetryEvent` bank (EventId 8320-8329 reserved).** MVP relies on ASP.NET Core request logs + access to the host's log aggregator.
- **Rate limiting on export endpoints.** Export is a DoS vector (long-running, reads every MU + edge). MVP ships WITHOUT endpoint-level rate limiting; operators are expected to gate these endpoints via network policy or reverse-proxy auth. Document in `docs/dev/export.md` under "Operational guardrails". A future story can add `Microsoft.AspNetCore.RateLimiting` policies if public-ish exposure becomes needed.
- **`CounterpartWorkflowInstanceId` manifest field.** Reserved for future re-import correlation. **Deferred to schema v2** — adding fields later is additive per the documented schema-versioning policy. Keeping v1 minimal reduces the 8.3 surface area.
- **Export of workflow state** (DAPR Workflow state store entries: ingestion workflows in flight, tenant-deletion progress, consistency repair pass-tracking, etc.). Workflow state is ephemeral orchestration data, not domain data. Documented.

**Primary risks:**

1. **Memory-blowup on large tenant export.** Naively loading every memory unit into a `List<MemoryUnit>` before serializing explodes memory on a 1M-unit tenant (~400 MB minimum for text + metadata). **Mitigation:** (a) `ExportWriter` streams through `Utf8JsonWriter` attached to the response's `PipeWriter`; (b) enumeration is pulled via `IAsyncEnumerable<MemoryUnit>` — `server.KeysAsync(pattern: "...", pageSize: 250)` yields keys and the writer emits each unit as it arrives; (c) flush cadence every 1000 units or 1 MiB of unflushed bytes; (d) guard test `TenantExportServiceTests.LargeTenantExport_DoesNotBuffer` mocks 50_000 units and asserts memory pressure stays bounded (peak allocated-bytes counter from a `GC.GetAllocatedBytesForCurrentThread()` probe before/after stays under 10 MiB).
2. **Snapshot-isolation drift.** The AC says "units added during export are either all included or all excluded (snapshot isolation)". True snapshot isolation across Redis + FalkorDB is impossible without MVCC. **Mitigation:** (a) capture `snapshotAt` before enumeration; (b) filter memory units by `ingestedAt <= snapshotAt` at emit time; (c) for edges, the export's `ingestedAt` comparison applies to the edge's `createdAt` property stored on the FalkorDB edge (confirm by inspection — edge creation time IS persisted per the `RETURN r.createdAt` query patterns in `GraphQueryBuilder.cs`); (d) guard test `TenantExportServiceTests.ConcurrentIngest_ExcludesUnitsAfterSnapshot` seeds a unit with `ingestedAt = snapshotAt + 1s` and asserts it is absent from the export; (e) document the known-limitation in `docs/dev/export.md` under "Snapshot semantics".
3. **Cypher injection via caseId / tenantId.** The export endpoints accept `caseId` from the URL path. **Mitigation:** (a) `TenantIdGuard.Validate` already enforces the tenant regex; (b) `caseId` is validated against the case existence via `CaseService.GetCaseAsync` BEFORE the graph query runs — if the case doesn't exist in Redis, export returns 404 without issuing any Cypher; (c) `IGraphQueryBuilder.BuildListCaseMemoryUnitIds` + `BuildTraverseWithEdges` already use parameterized queries (Decision D9); (d) guard test `TenantExportServiceTests.MalformedCaseId_Returns400` asserts rejection at the endpoint.
4. **Graph-edge enumeration on large tenant.** Tenant-export must emit every edge. FalkorDB does not expose a native "list all edges" bulk endpoint. **Mitigation:** (a) iterate memory unit IDs from the SCAN result; (b) for each batch of 100 IDs, run a single parameterized Cypher `UNWIND $ids AS muId OPTIONAL MATCH (m {id: muId})-[r]-(n) RETURN muId, r, n.id` that pulls every edge touching each batch in one roundtrip; (c) de-duplicate edges in the writer via a `HashSet<string>` of edge IDs (FalkorDB edge IDs are stable within a graph); (d) add a new `IGraphQueryBuilder.BuildListEdgesForMemoryUnits(IReadOnlyList<string> memoryUnitIds)` method (NEW method; mirror `BuildBatchCountAnnotations` shape); (e) guard test `TenantExportServiceTests.BatchedEdgeEnumeration_IssuesOneCypherPerHundredIds`.
5. **Client stream abandonment leaks server-side resources.** If the client closes the TCP connection mid-export (Ctrl+C / network flap), the server's async enumeration must notice and terminate. **Mitigation:** (a) the endpoint passes `HttpContext.RequestAborted` as the `CancellationToken` to `WriteTenantExportAsync`; (b) the writer checks `ct.IsCancellationRequested` between batches; (c) `ExportWriter` also disposes the `Utf8JsonWriter` in a `finally` so any buffered bytes are flushed before the response body is discarded (ASP.NET Core logs the truncated response for operational visibility); (d) guard test `TenantExportServiceTests.CancellationMidStream_PropagatesPromptly` cancels the token after 10 units and asserts the enumeration stops within 50 ms.
6. **Inconsistent case-scope edge semantics.** The AC says case-export includes "all graph edges" — but which edges? An edge between a MU in the case and a MU outside the case is ambiguous. **Decision:** include edges where `sourceId` OR `targetId` is in the case — this preserves causal chains that reach into the case from outside (Story 4.1 traversal semantics). The "far endpoint" (outside the case) is NOT resolved into a full `ExportedMemoryUnit`; it is referenced by ID only. The re-import story (future) will need to handle the "dangling reference" case — document this in `docs/dev/export.md` under "Case-scope edge semantics". Guard test `TenantExportServiceTests.CaseExport_CrossCaseEdge_IncludedWithDanglingTarget` pins the behavior.
7. **Disk-write atomicity on CLI `--output`.** If the CLI writes to `--output out.json` and the server stream fails mid-write, a truncated file corrupts the output. **Mitigation:** (a) CLI writes to `out.json.part` first; (b) on successful completion, `File.Move(partPath, finalPath, overwrite: true)` atomically; (c) on failure, delete the `.part` file; (d) if `--output out.json` exists and `--force` is not set, return a clear plumbing error before opening the stream. Guard test `ExportCaseCommandTests.OutputFileExists_NoForce_RefusesWithExit2`.
8. **Schema-version drift between export and import.** Re-import (future story) will need to detect schema version. **Mitigation:** `schemaVersion = 1` is emitted as the FIRST field in the manifest. Importers can read just the first KB and branch on the version. Backward-compat policy documented in `docs/dev/export.md`: **additive-only** changes may keep `schemaVersion = 1`; **breaking** changes bump to `2`. Adding new fields to `ExportedMemoryUnit` is additive (consumers ignore unknown fields via `JsonIgnoreExtraFields`). Removing a field is breaking. Renaming a field is breaking. All documented.
9. **AccessTelemetryEvent pollution.** Story 7.5 emits audit events for search / ingest / traverse / case-access. Export is NOT in that list. Emitting an event for `GET /export` would be a silent regression. **Mitigation:** (a) do NOT wire the export endpoints into any `AccessTelemetryEnricher`-aware middleware; (b) add a regression test `ExportEndpointTests.ExportEndpoints_EmitNoAccessTelemetryEvent` that captures logs over an export call and asserts zero EventIds in the 7500-7599 bank; (c) document in `docs/dev/export.md` that operator-export is out of the AccessTelemetryEvent scope (if operators need auditing of exports, a future story adds a dedicated `ExportTelemetryEvent` bank).

10. **Edge-ID dedup HashSet unbounded growth.** Risk #1's mitigation caps memory-unit buffering but the edge-dedup `HashSet<string>` holds every emitted edge ID for the full duration of the export. A 5M-edge tenant with ~50-char stringified ids ≈ 250 MiB — violates AC #4's 20 MiB budget by 10×. **Mitigation:** (a) use a segmented dedup scheme — batch edges by edge-type bucket, emit each bucket's unique set, then clear; edges from separate buckets cannot alias because the Cypher `type(r)` qualifier is part of identity; (b) alternatively, use a probabilistic filter (`System.Collections.Generic.HashSet` with bounded capacity + spill to Bloom filter at 100K entries — accept 1% false-positive duplicate suppression which is benign for re-import because the re-importer is idempotent on `(SourceId, TargetId, EdgeType, CreatedAt)`); (c) pin the choice in Task 1.2's implementation notes. Guard test `TenantExportServiceTests.DedupMemory_RemainsBounded_OnMillionEdges` mocks a 1M-edge enumeration and asserts dedup-structure allocation stays under 10 MiB.

11. **PipeWriter backpressure under slow reader.** `Utf8JsonWriter.FlushAsync` pushes into the underlying `PipeWriter`; the pipe itself has a default buffer that grows unbounded until `PauseWriterThreshold` is hit. A slow client (deliberate or network-throttled) can make the server buffer the entire export in the pipe — re-introducing Risk #1 through the I/O layer. **Mitigation:** (a) configure `PipeOptions.PauseWriterThreshold = 4 MiB` + `ResumeWriterThreshold = 2 MiB` on the response pipe; (b) `ExportWriter.FlushAsync` must `await pipeWriter.FlushAsync(ct)` and respect the returned `FlushResult.IsCanceled`/`IsCompleted` signals — stop enumeration when the client has disconnected. Guard test `TenantExportServiceTests.SlowReader_DoesNotBufferOnServer` attaches a throttled reader, verifies server-side allocation stays bounded + producer awaits backpressure.

12. **`ingestedAt` clock skew between ingestion code and export snapshot.** AC #5 filters memory units by `ingestedAt <= snapshotAt`. If `ingestedAt` is persisted by Redis-writing code with `DateTimeOffset.UtcNow` on a different pod clock than the export server's `DateTimeOffset.UtcNow`, a ±N ms drift can include/exclude wrong units. **Mitigation:** (a) confirm pre-flight that `ingestedAt` is set on the server pod (not client-supplied) and that Redis is not used to generate timestamps (Redis `TIME` command on a different node would re-introduce drift); (b) document the invariant "`ingestedAt` reads the same wall-clock as `snapshotAt`" in `docs/dev/export.md`; (c) if cross-pod clock sync is not strictly guaranteed in the deployment, bump the snapshot filter to `ingestedAt <= snapshotAt - 500ms` to absorb typical NTP drift (operators lose ~500 ms of just-ingested units from the export — acceptable for an archival operation). Guard test `TenantExportServiceTests.IngestedAtClockSkew_HandledByToleranceWindow`.

**Risk → Guard test mapping** (each risk's mitigation is pinned by a specific test):

| # | Risk | Guard test |
|---|------|-----------|
| 1 | Memory blowup on large export | `TenantExportServiceTests.LargeTenantExport_DoesNotBuffer` |
| 2 | Snapshot isolation drift | `TenantExportServiceTests.ConcurrentIngest_ExcludesUnitsAfterSnapshot` + `TenantExportServiceTests.InFlightIndexing_DivergenceIsObservableAndRecoverable` |
| 3 | Cypher injection via caseId | `ExportEndpointTests.MalformedCaseId_Returns400` + existing `TenantIdGuard` tests |
| 4 | Edge enumeration cost | `TenantExportServiceTests.BatchedEdgeEnumeration_IssuesOneCypherPerHundredIds` |
| 5 | Client stream abandonment | `TenantExportServiceTests.CancellationMidStream_PropagatesPromptly` |
| 6 | Case-scope edge semantics | `TenantExportServiceTests.CaseExport_CrossCaseEdge_IncludedWithDanglingTarget` |
| 7 | `--output` atomicity | `ExportCaseCommandTests.OutputFileExists_NoForce_RefusesWithExit2` + `ExportCaseCommandTests.ServerStreamFails_PartFileDeleted` |
| 8 | Schema-version drift | `ExportManifestTests.SchemaVersionEmittedFirstInManifest` + `ExportManifestTests.UnknownFieldIgnored_RoundTrip` |
| 9 | AccessTelemetryEvent pollution | `ExportEndpointTests.ExportEndpoints_EmitNoAccessTelemetryEvent` |
| 10 | Edge-dedup HashSet unbounded growth | `TenantExportServiceTests.DedupMemory_RemainsBounded_OnMillionEdges` |
| 11 | PipeWriter backpressure under slow reader | `TenantExportServiceTests.SlowReader_DoesNotBufferOnServer` |
| 12 | `ingestedAt` clock skew | `TenantExportServiceTests.IngestedAtClockSkew_HandledByToleranceWindow` |

## Story

As a developer,
I want to export all memory units, metadata, and graph edges for a case or tenant,
so that I can back up knowledge, migrate data, or analyze it externally.

## Acceptance Criteria

1. **Case export produces a portable JSON file with all unit + edge + case data (FR71, epic AC #1).**
   **Given** a case `caseId` in tenant `tenantId` with indexed memory units and graph edges,
   **When** `GET /api/tenants/{tenantId}/cases/{caseId}/export` is called,
   **Then** the response is `200 OK` with `Content-Type: application/json`, `Content-Disposition: attachment; filename="{tenantId}-{caseId}-{snapshotAt}.json"`, and `X-Export-Schema-Version: 1`,
   **And** the body is a single JSON object with the manifest (`schemaVersion=1`, `scope="case"`, `tenantId`, `caseId`, `exportedAt`, `snapshotAt`), `case` (the `Case` record + `members` list via `ListCaseMembersAsync`), `memoryUnits[]` (every MU in the case as an `ExportedMemoryUnit` — a wrapper `{ unit: MemoryUnit, annotationTargets: string[] }` where `unit` carries full metadata: content, contentHash, sourceUri, sourceType, ingestedBy, ingestedAt, lastUpdated, status, metadata, embeddingProvider, embeddingModel, embeddingDimensions, classification, failureDetails), `edges[]` (every edge where source OR target is in the case; serialized as `ExportedEdge`), and `statistics` (`memoryUnitCount`, `edgeCount`, `caseCount=1`).

2. **Tenant export includes all cases + units + edges + tenant config (FR71, epic AC #2).**
   **Given** a tenant `tenantId` with multiple cases,
   **When** `GET /api/tenants/{tenantId}/export` is called,
   **Then** the body contains a manifest with `scope="tenant"` (no caseId), `tenant` section (the `TenantConfigurationView` + tenant registry timestamps + `TenantStatus`, wrapped in `ExportedTenantConfig`), `cases[]` (each `Case` with `members[]`), `memoryUnits[]` (every MU in the tenant, each with its own `caseId` preserved), `edges[]` (every edge in the tenant graph), and `statistics` (`memoryUnitCount`, `edgeCount`, `caseCount=N`)
   **And** the export preserves the case→MU relationship via each MU's `caseId` field (no duplication of MU records across case boundaries; each MU appears exactly once).

3. **Valid JSON with documented, deterministic schema (epic AC #3).**
   **Given** an export file,
   **When** a consumer parses it with `JsonSerializer.Deserialize<ExportManifest>` (reading only the first section),
   **Then** the parse succeeds and yields `SchemaVersion = 1`
   **And** the top-level property order is `manifest`, then `case` (case-scope) or `tenant` + `cases` (tenant-scope), then `memoryUnits`, then `edges`, then `statistics` (so streaming parsers can incrementally consume large exports)
   **And** all memory-unit IDs, edge IDs, case IDs, and tenant IDs are preserved verbatim (re-import-ready)
   **And** enum values are camelCase (e.g., `sourceType: "webpage"`, `status: "ready"`, `edgeType: "causedBy"`, `origin: "human"`).

4. **Streaming output — no server-side buffering of the full document (epic AC #4).**
   **Given** a large tenant,
   **When** the tenant export endpoint is called,
   **Then** the response body starts streaming within 2 seconds of request receipt (first byte of the manifest emitted before enumeration completes; pinned by `ExportEndpointTests.FirstByteUnder2Seconds` with a mocked fast-path case-list so the timing is deterministic under test)
   **And** the server process's managed heap does NOT grow by more than 20 MiB relative to pre-export baseline over the course of a 50_000-unit export (pinned by `TenantExportServiceTests.LargeTenantExport_DoesNotBuffer`)
   **And** edge-dedup state + `PipeWriter` buffer stay bounded under adversarial conditions (pinned by `TenantExportServiceTests.DedupMemory_RemainsBounded_OnMillionEdges` + `TenantExportServiceTests.SlowReader_DoesNotBufferOnServer`)
   **And** the CLI's `memories export tenant --tenant <t>` prints a progress line to stderr roughly every 64 KiB of streamed bytes: `Exported K MB so far` (byte-count-based progress; see Task 5.3 for the rationale for dropping JSON-nesting-aware counting).

5. **Snapshot isolation — units after snapshotAt are excluded; in-flight indexing excluded deterministically (epic AC #5).**
   **Given** an export is in progress,
   **When** new memory units are ingested simultaneously (with `ingestedAt > snapshotAt`),
   **Then** those new units are NOT in the export output
   **And** units whose `ingestedAt <= snapshotAt` AND whose graph indexing completed by `snapshotAt` are included WITH their complete edge set
   **And** units whose `ingestedAt <= snapshotAt` BUT whose graph indexing is in-flight at `snapshotAt` are **excluded entirely** (both the MU and any partial edges) — deterministic exclusion, not a non-deterministic OR (importers must be able to rely on "if a MU is present, its edges are complete")
   **And** edges with `createdAt <= snapshotAt` are included; edges with `createdAt > snapshotAt` are excluded
   **And** the manifest's `snapshotAt` field matches the server's observed timestamp at export-start (± 10 ms tolerance for UTC conversion)
   **And** if the deployment does not guarantee cross-pod clock sync, the snapshot filter applies `snapshotAt - 500ms` as a tolerance window (Risk #12) — documented in `docs/dev/export.md`.

6. **Case-scope edges span case boundaries correctly (new AC — Risk #6).**
   **Given** a case `A` with a memory unit `mu1` that has a `causedBy` edge to a memory unit `mu2` in a different case `B`,
   **When** case `A` is exported,
   **Then** the edge `mu1 -causedBy-> mu2` IS present in `edges[]`
   **And** `mu1` is present in `memoryUnits[]`
   **And** `mu2` is NOT present in `memoryUnits[]` (the dangling-target field is documented as a re-import concern)
   **And** the `statistics.edgeCount` reflects the cross-case edge.

7. **Non-existent tenant / case returns 404 with actionable error response.**
   **Given** a caller requests export for a tenant that doesn't exist,
   **When** `GET /api/tenants/{unknownTenant}/export` is called,
   **Then** the response is `404 Not Found` with body `ErrorResponse(code="TENANT_NOT_FOUND", message, recoverySuggestion)`
   **Given** a caller requests case export where the case doesn't exist in an existing tenant,
   **When** `GET /api/tenants/{tenantId}/cases/{unknownCase}/export` is called,
   **Then** the response is `404 Not Found` with body `ErrorResponse(code="CASE_NOT_FOUND", message, recoverySuggestion)`
   **And** no partial response body is emitted (the error is returned before any streaming begins).

8. **Invalid IDs return 400 with actionable error response.**
   **Given** a caller passes a `tenantId` containing invalid characters (non-alphanumeric, non-hyphen),
   **When** either export endpoint is called,
   **Then** the response is `400 Bad Request` with body `ErrorResponse(code="INVALID_TENANT_ID", ...)`.

9. **CLI commands stream to stdout or `--output` with atomic write (new AC — Risk #7).**
   **Given** `memories export case --tenant <t> --case <c>` is invoked without `--output`,
   **When** the command completes,
   **Then** the JSON export is written to stdout (binary-safe, no console-text mutation)
   **And** exit code is `0` on success
   **And** progress is written to stderr (not stdout).
   **Given** `memories export case --tenant <t> --case <c> --output out.json` is invoked,
   **When** the command runs,
   **Then** the server response streams to `out.json.part`; on success, atomic rename to `out.json`
   **And** if `out.json` exists and `--force` is not set, the command exits with `2` and error code `EXPORT_OUTPUT_PATH_INVALID` before contacting the server
   **And** if the server stream fails mid-write, `out.json.part` is deleted and the command exits with `1` and error code `EXPORT_WRITE_FAILED`.

10. **Client method exposes the response stream to the caller (new AC — derived from #9 + Risk #5).**
    **Given** `MemoriesClient.ExportCaseAsync(tenantId, caseId, ct)` is invoked,
    **When** the server returns `200 OK`,
    **Then** the returned `Stream` is disposable by the caller (owns the underlying `HttpResponseMessage` + network buffer)
    **And** the request is issued with `HttpCompletionOption.ResponseHeadersRead` so the caller receives the stream before the full body is downloaded
    **And** on non-2xx the client throws `MemoriesRemoteException` with the decoded `ErrorResponse` (existing `ErrorResponseDecoder.DecodeAsync` pattern).

11. **Tests cover the export paths.** *(AC #11 is the **authoritative** source for test-class inventory. The "Testing standards" section in Dev Notes documents conventions only — if a count in that section conflicts with a number here, this AC wins.)*
    **Given** the consolidated test projects,
    **When** `dotnet test` runs,
    **Then** the following classes exist and pass (Tier 1 — unit — unless marked Integration):
    - `tests/Hexalith.Memories.Server.Tests/Export/TenantExportServiceTests.cs` — 14 tests: empty-case (manifest + zero units + zero edges + valid statistics); case-export happy path (5 units, 3 edges — assert exact JSON token sequence); tenant-export happy path (3 cases × 5 units each, 10 edges); large-tenant streaming (50_000 units — bounded memory, Risk #1); concurrent-ingest-excluded (snapshotAt filter, Risk #2); **in-flight-indexing is deterministically excluded** — seed a MU with `ingestedAt = snapshotAt - 1ms` but whose graph edges are created at `snapshotAt + 500ms`; assert the export excludes the MU AND its edges (deterministic exclusion, no OR branch; AC #5 contract); batched-edge-enumeration-issues-one-cypher-per-hundred-ids (Risk #4); cross-case edge in case-scope (Risk #6); cancellation-mid-stream (Risk #5); cypher-injection-via-caseId-rejected (delegated to existing `TenantIdGuard`); case-not-found-throws-KeyNotFoundException; **dedup-memory-remains-bounded-on-million-edges** (Risk #10); **slow-reader-does-not-buffer-on-server** (Risk #11); **ingested-at-clock-skew-handled-by-tolerance-window** (Risk #12).
    - `tests/Hexalith.Memories.Server.Tests/Export/ExportWriterTests.cs` — 6 tests: manifest-emits-schema-version-first (Risk #8); flush-cadence-every-1000-units; `ExportedMemoryUnit`-wrapper-shape-round-trips (`unit` + `annotationTargets` sibling properties); edge-fields-include-promotion-metadata; enum-values-emitted-camelCase; **`EdgeId_EmittedAsRawInvariantCultureDecimal`** (Task 1.3 pinning).
    - `tests/Hexalith.Memories.Server.Tests/Endpoints/ExportEndpointTests.cs` — 10 tests using `WebApplicationFactory<Program>`: GET case-export returns 200 with streaming headers; GET tenant-export returns 200 with streaming headers; unknown tenant returns 404; unknown case returns 404; invalid tenantId returns 400; invalid caseId returns 400 (**`CaseIdValidator_RejectsNonUlid`** — explicitly pins the ULID-regex validator added in Task 3.1); X-Export-Schema-Version header present (Risk #8 end-to-end); no AccessTelemetryEvent emitted (Risk #9); **`FirstByteUnder2Seconds`** (AC #4 TTFB pinning — uses a mocked case-list short-circuit for deterministic timing under test).
    - `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientExportTests.cs` — 4 tests: export-case-returns-stream; export-tenant-returns-stream; non-2xx-throws-MemoriesRemoteException; ResponseHeadersRead-used (verified via `TestDelegatingHandler` that asserts the completion option).
    - `tests/Hexalith.Memories.Cli.Tests/Cli/ExportCaseCommandTests.cs` — 6 tests: stdout default; `--output` writes part-file + atomic rename; `--output` existing file without `--force` exits with 2 (Risk #7); `--force` overwrites; server stream failure deletes part-file (Risk #7); **`OutputPathRejectsTraversal`** — `--output ../../../etc/evil.json` exits with `2` + `EXPORT_OUTPUT_PATH_INVALID` unless `--allow-absolute-path` is set (Red Team R2).
    - `tests/Hexalith.Memories.Cli.Tests/Cli/ExportTenantCommandTests.cs` — 4 tests: same shape as case-export but with `--tenant` only; progress line emitted to stderr every 1000 units (mock via a slow-stream stub that yields chunked output).
    - `tests/Hexalith.Memories.Contracts.Tests/V1/ExportContractSerializationTests.cs` — 1 `[Theory]` with one row per new record (`ExportManifest`, `ExportScope`, `ExportStatistics`, `ExportedEdge`, `ExportedTenantConfig`) + 1 round-trip test + 1 enum-camelCase test (mirror `ConsistencyContractSerializationTests` shape from 8.2).
    - `tests/Hexalith.Memories.IntegrationTests/Export/ExportWorkflowIntegrationTests.cs` `[Trait("Category","Integration")]` — 2 scenarios: (1) ingest-3-units-then-export-case-roundtrip (emit → deserialize → assert shape); (2) ingest-2-cases-then-export-tenant-roundtrip. `[Fact(Skip)]` pattern acceptable if the Aspire CS0311 issue from Story 5.6 / 8.1 / 8.2 remains unresolved; un-skip otherwise. Same skip-string convention: `"Aspire fixture build failure tracked in 5.6 Dev Notes"`.

12. **`docs/dev/export.md` documents the schema and behavior.**
    **Given** a developer wants to export data or consume an export file,
    **When** they read `docs/dev/export.md`,
    **Then** the doc covers:
    - **Purpose** — what export captures (MUs + edges + case metadata + tenant config); what it does NOT (workflow state, audit logs, secrets, raw binary source).
    - **Endpoint summary** — table of the two endpoints with request/response shapes and typical latency/size per unit count.
    - **Schema reference** — full manifest shape + per-section shape + a fully-worked 3-unit / 1-edge example (valid, parseable).
    - **Schema versioning** — additive vs. breaking rules; how consumers detect the version.
    - **Snapshot semantics** — `snapshotAt` filter; known limitations around in-flight indexing (Risk #2); recommendation to run `memories consistency verify` before a compliance-critical export.
    - **Case-scope edge semantics** — cross-case edges included with dangling target documented (Risk #6).
    - **Known Compromises** (dedicated section; elevated from inline notes) — (a) cross-case edges in case-scope exports produce dangling `targetId` references that a future re-importer must resolve explicitly; (b) `ExportedEdge.Id` is graph-instance scoped and must NOT be treated as stable identity by a re-importer; (c) snapshot isolation is advisory — in-flight indexing can produce observable divergence between `memoryUnits[]` and `edges[]` for a unit whose MU ingestion committed before `snapshotAt` but whose edge indexing completed after (Risk #2); (d) edge IDs, content, and extracted text are exported; raw source bytes (PDFs, images) are not. Each compromise is stated as a first-class bullet so future importer authors cannot miss them.
    - **Streaming behavior** — why responses stream; how consumers should consume (incremental-parse recipe).
    - **CLI walkthrough** — `memories export case` and `memories export tenant` with `--output` + `--force` + stdout-pipe examples (`... | jq .manifest`).
    - **Expected duration / size** — table: 1K units ≈ ~1 second / ~5 MB; 10K ≈ ~10 s / ~50 MB; 100K ≈ ~2 min / ~500 MB; 1M ≈ ~20 min / ~5 GB. Numbers are **rough baselines**, not SLAs — calibrate after Task 7.
    - **Relation to Story 8.2** — export is orthogonal to consistency; operators may run `verify` before export, but export captures the current state including divergences.
    - **Relation to Story 7.5** — export is NOT in the AccessTelemetryEvent scope (Risk #9); a future story can add a dedicated audit channel.
    - **Phase-2 migration path** — if resumable exports, scheduled exports, or exports outliving a single HTTP connection become required, the 8.3 shape is replaced by a workflow-backed variant: `POST /api/tenants/{tenantId}/export` returns a workflow instance ID; `GET /api/tenants/{tenantId}/export/{instanceId}` polls status; on completion, a pre-signed blob URL is returned. The **JSON schema stays v1** — only the transport envelope changes. Document this explicitly so a future story author knows the schema is NOT the upgrade boundary.
    - **Operational guardrails** — no endpoint-level rate limiting in MVP; operators gate exports via network policy, reverse-proxy auth, or deployment-scoped throttling. Long-running exports should ideally run during off-peak windows; a 1M-unit export can hold a FalkorDB connection for ~20 min.
    - **Out of scope** — re-import, incremental exports, cross-tenant exports, compression, binary embedding, signed exports, SSE progress, MCP tool, AccessTelemetryEvent integration, export-specific audit logging (deferred to dedicated `ExportTelemetryEvent` story), endpoint-level rate limiting, `CounterpartWorkflowInstanceId` (deferred to schema v2) — all enumerated in "What does NOT ship" for traceability.

## Tasks / Subtasks

### Task Summary (orientation)

9 top-level tasks. Tasks 1 + 2 deliver the substrate (service + writer); Task 3 ships the contract records; Tasks 4 + 5 + 6 close the loop with REST + client + CLI; Tasks 7 + 8 are verification; Task 9 ships docs + sprint-status update.

- **Substrate:** Tasks 1 (export service + writer), 2 (contracts)
- **Integration:** Tasks 3 (REST endpoints), 4 (client methods), 5 (CLI)
- **Verification:** Tasks 6 (unit tests), 7 (integration tests)
- **Finalization:** Task 8 (docs), Task 9 (sprint-status + final validation)

---

- [ ] **Task 1: `TenantExportService` + `ExportWriter` (AC: #1, #2, #4, #5, #6, #11)**
  - [ ] 1.1 Create `src/Hexalith.Memories.Server/Export/` folder (new). Mirrors `Consistency/`, `Cases/`, `Tenants/` structure.
  - [ ] 1.2 Create `src/Hexalith.Memories.Server/Export/TenantExportService.cs` — sealed class per "What 8.3 adds" #1. Ctor-inject the keyed connection multiplexers + `IGraphQueryBuilder` + `CaseService` + `TenantRegistryService` + `ILogger<TenantExportService>`. Public methods `WriteCaseExportAsync` + `WriteTenantExportAsync` + `CaptureSnapshotAsync` (see 1.2a). Snapshot capture is the FIRST act: `DateTimeOffset snapshotAt = DateTimeOffset.UtcNow` before any backend call.
  - [ ] 1.2a **Capture-before-start method.** Expose `public Task<ExportSnapshot> CaptureSnapshotAsync(string tenantId, string? caseId, CancellationToken ct)` returning a `record ExportSnapshot(DateTimeOffset SnapshotAt, TenantInfo Tenant, Case? CaseRecord, IReadOnlyList<CaseMember>? Members)`. This runs before the endpoint calls `context.Response.StartAsync()` so tenant/case existence and snapshot timestamp are pinned before headers are committed. Task 3.2 references this method — it is NOT re-defined at the endpoint layer. If tenant missing → throw `KeyNotFoundException` with code `TENANT_NOT_FOUND`; if caseId provided and case missing → throw `KeyNotFoundException` with code `CASE_NOT_FOUND`. Both map to 404 at the endpoint (no response body buffered before the error).
    - Case-scope enumeration:
      1. `CaseService.GetCaseAsync(tenantId, caseId, ct)` → guard; 404 if null (throw `KeyNotFoundException`).
      2. `CaseService.ListCaseMembersAsync(tenantId, caseId, ct)` → emit under `case.members[]`.
      3. `IGraphQueryBuilder.BuildListCaseMemoryUnitIds(caseId)` → query FalkorDB, get MU IDs in case.
      4. For each MU ID (ordered), `CaseService.GetMemoryUnitAsync` → filter `ingestedAt <= snapshotAt` → `CaseService.ListAnnotationsAsync` for the `annotationTargets[]` projection → emit via `ExportWriter.WriteMemoryUnit(mu, annotationTargets)`.
      5. Batch collect MU IDs into 100-id chunks; issue `IGraphQueryBuilder.BuildListEdgesForMemoryUnits(chunk)` per chunk (NEW method in `IGraphQueryBuilder`, Task 1.3); de-duplicate by edge ID via an in-memory `HashSet<string>`; filter `createdAt <= snapshotAt`; emit via `ExportWriter.WriteEdge`.
    - Tenant-scope enumeration:
      1. `TenantRegistryService.GetAsync(tenantId, ct)` → guard; 404 if null.
      2. Get `TenantConfigurationView` via `TenantEndpointHandlers.GetTenantConfigurationAsync`-equivalent path (factor the view-building from the existing handler into a reusable method if not already one — likely already a method on `TenantConfigurationViewBuilder`; confirm before editing).
      3. `CaseService.ListCasesAsync(tenantId, maxResults: int.MaxValue, ct)` — pagination is NOT a real concern for tenant metadata enumeration (cases count is bounded). Emit `cases[]`.
      4. SCAN `{tenantId}:mu:*` via `server.KeysAsync(pattern, pageSize: 250)` (same pattern as `TenantMetricsService.GetMemoryUnitCountAsync`); for each key, `HashGetAllAsync` → parse via `CaseService.ParseMemoryUnitFromHash` (internal) → filter `ingestedAt <= snapshotAt` → emit.
      5. Edge enumeration: accumulate MU IDs from step 4 in 100-id batches; issue `BuildListEdgesForMemoryUnits` per batch (same helper as case-scope); de-duplicate; emit.
  - [ ] 1.3 Extend `IGraphQueryBuilder` + `GraphQueryBuilder` with:
    - `BuildListEdgesForMemoryUnits(IReadOnlyList<string> memoryUnitIds)` — parameterized Cypher: `UNWIND $ids AS muId MATCH (m:MemoryUnit {id: muId})-[r]-(n:MemoryUnit) RETURN id(r) AS edgeId, m.id AS sourceId, n.id AS targetId, type(r) AS edgeType, r.confidence AS confidence, r.origin AS origin, r.createdAt AS createdAt, r.verifiedBy AS verifiedBy, r.previousConfidence AS previousConfidence`. Note: FalkorDB's `id(r)` returns a numeric internal id. **Emission format:** stringify as raw decimal using `long.ToString(CultureInfo.InvariantCulture)` — NO prefix (e.g., `"4273"`, not `"falkor-edge-4273"`). Stable within a graph but NOT stable across graph deletions/recreations (documented as "edge ids are graph-instance scoped" in `docs/dev/export.md`). Guard test `ExportWriterTests.EdgeId_EmittedAsRawInvariantCultureDecimal`.
  - [ ] 1.4 Create `src/Hexalith.Memories.Server/Export/ExportWriter.cs` — internal sealed class. Ctor: `ExportWriter(PipeWriter pipeWriter)`. Opens a `Utf8JsonWriter` over a `Stream` wrapper (`pipeWriter.AsStream()`). Public methods:
    - `WriteManifest(ExportManifest manifest)` — emits the manifest object as the FIRST top-level field (`"manifest": { ... }`).
    - `WriteCase(Case caseRecord, IReadOnlyList<CaseMember> members)` — case-scope only; emits the `case` top-level field.
    - `WriteTenant(ExportedTenantConfig tenant)` — tenant-scope only; emits the `tenant` top-level field.
    - `WriteCasesArrayHeader() / WriteCase(Case caseRecord, members) / WriteCasesArrayFooter()` — tenant-scope emits `cases[]`.
    - `WriteMemoryUnitsArrayHeader() / WriteMemoryUnit(ExportedMemoryUnit entry) / WriteMemoryUnitsArrayFooter()` — the writer just `JsonSerializer.Serialize(writer, entry, MemoriesJsonContext.Options)`, no custom token emission.
    - `WriteEdgesArrayHeader() / WriteEdge(ExportedEdge edge) / WriteEdgesArrayFooter()`.
    - `WriteStatistics(ExportStatistics statistics)` — emits the final `statistics` field.
    - `FlushAsync(CancellationToken ct)` — flushes the underlying writer; called every 1000 memory units or 1 MiB of unflushed output.
    - `DisposeAsync` — flushes + disposes the `Utf8JsonWriter`.
    Implementation detail: use `JsonSerializer.Serialize(writer, value, MemoriesJsonContext.Options)` for each element (source-gen path) — never hand-roll JSON token emission except for array headers/footers.
  - [ ] 1.5 `[LoggerMessage]` partial methods — EventId bank **8300-8399** reserved for Story 8.3. Allocations:
    - `8301 ExportStarted(Info)` — tenantId + scope + snapshotAt.
    - `8302 ExportMemoryUnitsEnumerated(Info)` — progress every 1000 units; tenantId + count.
    - `8303 ExportCompleted(Info)` — tenantId + scope + totalUnits + totalEdges + durationMs + bytesWritten.
    - `8310 ExportCancelled(Warning)` — tenantId + scope + unitsSoFar.
    - `8311 ExportFailed(Error)` — tenantId + scope + exceptionMessage.
  - [ ] 1.6 DI registration in `Program.cs`:
    ```csharp
    builder.Services.AddScoped<TenantExportService>();
    ```

- [ ] **Task 2: Contract records (AC: #1, #2, #3)**
  - [ ] 2.1 Create the following records in `src/Hexalith.Memories.Contracts/V1/` (one file per record, ITANEO header, `public sealed record`, registered in `MemoriesJsonContext`):
    - `ExportManifest.cs` — `(int SchemaVersion, ExportScope Scope, string TenantId, string? CaseId, DateTimeOffset ExportedAt, DateTimeOffset SnapshotAt)`.
    - `ExportScope.cs` — enum `{ Case, Tenant }` + `[JsonConverter(typeof(CamelCaseStringEnumConverter<ExportScope>))]`.
    - `ExportStatistics.cs` — `(int MemoryUnitCount, int EdgeCount, int CaseCount)`.
    - `ExportedEdge.cs` — per "What 8.3 adds" #3. **MUST include an XML doc comment on the `Id` property** stating: `"FalkorDB edge identifier (scoped to the current graph instance). Stable within a single graph lifetime; NOT stable across graph deletions or recreations. Re-import MUST NOT use this value as edge identity — reconstruct edges from the (SourceId, TargetId, EdgeType, CreatedAt) tuple."` This prevents a future importer from writing `if (existing.Id == imported.Id)` and silently failing.
    - `ExportedTenantConfig.cs` — `(TenantConfigurationView Configuration, TenantStatus Status, DateTimeOffset CreatedAt, DateTimeOffset LastUpdated)`.
    - `ExportedMemoryUnit.cs` — `(MemoryUnit Unit, IReadOnlyList<string> AnnotationTargets)`. Sealed record, ITANEO header. `AnnotationTargets` is empty list (not null) when the MU has no inbound annotations — deserialization expects a non-null list.
  - [ ] 2.2 Update `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` `[JsonSerializable]` attribute list with the 6 new records. Run `dotnet build` — missing entries surface as AOT warnings (watch for `System.Text.Json` source-gen diagnostics).

- [ ] **Task 3: REST endpoints in `Program.cs` (AC: #1, #2, #4, #5, #6, #7, #8)**
  - [ ] 3.1 Insert two new minimal-API endpoints in `Program.cs` after the consistency block (around line 1266 in current main, before the cases block — keeps export + consistency + tenant operator endpoints contiguous). The endpoint handlers receive `HttpContext` directly (not individual args) so they can call `context.Response.StartAsync()` + access `context.Response.BodyWriter`:
    - `GET /api/tenants/{tenantId}/cases/{caseId}/export` — validates tenantId via `ValidateTenantId`; validates caseId (not null/empty, no `:` character — match `CaseValidator.ValidateCaseId` if it exists, else add a simple regex guard); `TenantStatusGuard.ValidateTenantExistsAsync`; resolves `TenantExportService`; sets response headers (`Content-Type`, `Content-Disposition`, `X-Export-Schema-Version: 1`); calls `context.Response.StartAsync()` to commit headers; calls `exportService.WriteCaseExportAsync(tenantId, caseId, context.Response.BodyWriter, context.RequestAborted)`; maps `KeyNotFoundException` → 404 BEFORE `StartAsync` (i.e., do the case-existence check synchronously, but DO NOT write headers until after the existence guard passes).
    - `GET /api/tenants/{tenantId}/export` — same shape, calls `WriteTenantExportAsync`.
  - [ ] 3.2 Header ordering is load-bearing: `Content-Disposition` MUST include the snapshotAt-dependent filename, so snapshot capture happens BEFORE headers are written. Use `TenantExportService.CaptureSnapshotAsync` (defined in Task 1.2a) — the endpoint calls it first, maps its exceptions to 404/400, writes headers using the captured `snapshotAt`, calls `context.Response.StartAsync()`, then calls `WriteCaseExportAsync` / `WriteTenantExportAsync` passing the already-captured `ExportSnapshot` (do NOT re-capture — pass it through so the manifest and the filename agree exactly).
  - [ ] 3.3 Error responses for endpoints that return 4xx/5xx — `ErrorResponse` JSON with recovery suggestions following the Story 7.3 actionable-error-messages pattern. Codes:
    - `INVALID_TENANT_ID` (400)
    - `INVALID_CASE_ID` (400)
    - `TENANT_NOT_FOUND` (404) — existing code; reuse.
    - `CASE_NOT_FOUND` (404) — existing code; reuse.
    - `EXPORT_STREAM_FAILED` (5xx — emitted in server logs only, NOT as an HTTP response if the stream has already begun; documented).
  - [ ] 3.4 Export endpoints MUST be excluded from the Access Telemetry Events channel (Story 7.5). Verify by grepping the `AccessTelemetryEnricher` (if it exists) for the endpoint path — NO registration needed for new paths; the enricher is opt-in per endpoint. Add a regression test (Task 6.3).

- [ ] **Task 4: `MemoriesClient` methods (AC: #10, #11)**
  - [ ] 4.1 Add two new methods to `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` following the existing `virtual` style:
    - `Task<Stream> ExportCaseAsync(string tenantId, string caseId, CancellationToken ct)`:
      1. Validate args non-null / non-empty.
      2. `HttpRequestMessage request = new(HttpMethod.Get, $"api/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}/export")`.
      3. `HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)` — critical: `ResponseHeadersRead` enables streaming.
      4. Non-2xx → `await ErrorResponseDecoder.DecodeAsync(response, ct) → throw MemoriesRemoteException`.
      5. On 2xx, return `await response.Content.ReadAsStreamAsync(ct)` — the caller is responsible for disposal of the stream (which holds the response handle). Document ownership in the method's XML doc.
    - `Task<Stream> ExportTenantAsync(string tenantId, CancellationToken ct)` — same shape with `api/tenants/{tenantId}/export`.
  - [ ] 4.2 No `MemoriesJsonContext` registration needed (the client forwards raw bytes).

- [ ] **Task 5: CLI commands (AC: #9, #11)**
  - [ ] 5.1 Create `src/Hexalith.Memories.Cli/Commands/ExportCaseCommand.cs` — shape mirrors `ConsistencyInspectCommand` (non-streaming command uses the formatter router; this one does NOT). Options:
    - `--tenant <t>` (required)
    - `--case <c>` (required)
    - `--output <path>` (optional)
    - `--force` (optional flag; default false — permits overwrite of existing target)
    - `--allow-absolute-path` (optional flag; default false — permits `--output` paths outside the CWD; safety opt-in, Red Team R2)
    Flow:
      1. Pre-flight: if `--output` is set:
         a. Resolve `fullPath = Path.GetFullPath(outputPath)`; validate parent dir exists.
         b. **Path-traversal guard:** if `fullPath` does NOT start with `Environment.CurrentDirectory` AND `--allow-absolute-path` is not set, exit `2` with `EXPORT_OUTPUT_PATH_INVALID` and recovery message "Use --allow-absolute-path to write outside the current working directory, or pick a relative path."
         c. Validate `path` doesn't exist OR `--force` is set (otherwise print error via `ErrorMessageCatalog`, exit `2`).
      2. Open output sink: stdout (`Console.OpenStandardOutput()`) OR the part-file (`new FileStream(path + ".part", FileMode.Create, FileAccess.Write, FileShare.None)`).
      3. Call `MemoriesClient.ExportCaseAsync`; copy the returned `Stream` to the output sink via `sourceStream.CopyToAsync(outputSink, bufferSize: 81920, ct)` — 81 KB buffer matches `Stream.CopyTo`'s default + yields reasonable progress granularity.
      4. Emit progress to stderr via the `CountingStream` decorator (Task 5.3) — one line every 64 KiB of streamed bytes, format `Exported {X.Y} MB`.
      5. On success with `--output`: flush/close the part-file, `File.Move(partPath, finalPath, overwrite: force)`. Exit `0`.
      6. On failure: delete the part-file (if `--output`); exit `1` with `EXPORT_WRITE_FAILED` error envelope.
  - [ ] 5.2 Create `src/Hexalith.Memories.Cli/Commands/ExportTenantCommand.cs` — same shape without `--case`.
  - [ ] 5.3 Progress helper — create `src/Hexalith.Memories.Cli/Export/CountingStream.cs` — a thin `Stream` decorator that counts bytes written and fires a callback every 64 KiB. The callback emits a stderr line `Exported {bytes formatted as MB/GB}` — NO JSON parsing, NO nesting-depth tracking. Rationale: (a) the earlier `StreamingJsonProgressTracker` with `Utf8JsonReader` over a rolling buffer is genuinely tricky (nested `metadata: { topic: { value, origin, confidence } }` objects contribute false `{` increments, and partial tokens at buffer boundaries can miscount); (b) bytes are a universally-understood progress metric; (c) byte-count progress is test-deterministic. Implementation: override `WriteAsync` + `Write` + `CopyToAsync`-target path, accumulate `long _bytesWritten`, fire callback when `_bytesWritten / 65536` crosses a new boundary. No separate tracker class, no parser, no event. AC #4 already specifies the byte-based progress cadence.
  - [ ] 5.4 Register the new command group in `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:
    ```csharp
    private const string ExportCommandDescription = """
    Export memories and edges for a case or tenant as portable JSON.

    Examples:
        memories export case --tenant acme --case case-1 --output case-1.json
        memories export tenant --tenant acme | jq .manifest
    """;

    // In Build():
    var exportCommand = new Command("export", ExportCommandDescription);
    exportCommand.Subcommands.Add(ExportCaseCommand.Build(services));
    exportCommand.Subcommands.Add(ExportTenantCommand.Build(services));
    exportCommand.SetAction(_ => exportCommand.Parse("--help").Invoke());
    root.Subcommands.Add(exportCommand);
    ```
  - [ ] 5.5 Add error codes to `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`:
    - `EXPORT_TENANT_NOT_FOUND` → recovery: "Run 'memories tenant list' to see available tenants."
    - `EXPORT_CASE_NOT_FOUND` → recovery: "Run 'memories case list --tenant <t>' to see available cases."
    - `EXPORT_WRITE_FAILED` → recovery: "Check disk space and write permissions; the part-file has been deleted."
    - `EXPORT_OUTPUT_PATH_INVALID` → recovery: "Use --force to overwrite or pick a non-existing path."
  - [ ] 5.6 No new formatters needed — export is a raw byte stream. Document in `ExportCaseCommand.cs` and `ExportTenantCommand.cs` XML doc that `--format` is IGNORED; if `globalOptions.Format` is non-default, print a one-line warning to stderr and proceed.

- [ ] **Task 6: Unit tests (AC: #11)**
  - [ ] 6.1 `tests/Hexalith.Memories.Server.Tests/Export/TenantExportServiceTests.cs` — 11 tests per AC #11 inventory (10 original + `InFlightIndexing_DivergenceIsObservableAndRecoverable` from Risk #2 extension). Use NSubstitute for `IConnectionMultiplexer(keyed)` + `IGraphQueryBuilder` + `CaseService` (or where feasible, drive through an in-memory fake multiplexer — the `TenantMetricsService` tests have a precedent). Use `PipeWriter` over a `MemoryStream` for the output target; assert the serialized JSON via a `JsonDocument` parse + structural assertions. Shouldly assertions. NOT FluentAssertions.
  - [ ] 6.2 `tests/Hexalith.Memories.Server.Tests/Export/ExportWriterTests.cs` — 5 tests per AC #11.
  - [ ] 6.3 `tests/Hexalith.Memories.Server.Tests/Endpoints/ExportEndpointTests.cs` — 8 tests using `WebApplicationFactory<Program>`. Mirror the `ConsistencyEndpointTests` setup (create a local `ExportEndpointFactory` that stubs `TenantExportService` with NSubstitute to isolate the endpoint routing from the export logic). The AccessTelemetryEvent regression test (Risk #9) captures logs via `TestLogSink` (existing helper used by Story 7.5 tests) and asserts no EventId in 7500-7599.
  - [ ] 6.4 `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientExportTests.cs` — 4 tests using `TestDelegatingHandler`. The `ResponseHeadersRead` test asserts the `HttpCompletionOption` via a custom delegating handler that captures the parameter passed to `SendAsync(req, option, ct)` (the handler's `SendAsync` overload receives the option implicitly — check `HttpClient` internals; if unreachable, inject a stub at the `HttpMessageInvoker` level via subclass of `DelegatingHandler` that tracks the outer call pattern).
  - [ ] 6.5 CLI tests under `tests/Hexalith.Memories.Cli.Tests/Cli/` — two classes (`ExportCaseCommandTests` — 5 tests, `ExportTenantCommandTests` — 4 tests). Use the existing `ConsistencyStubClient` pattern from Story 8.2 (extends `MemoriesClient`; overrides the two new virtual export methods with deterministic streams). Temp-dir via `Path.GetTempPath()` + `Guid.NewGuid()`; clean up in `Dispose` / `finally`.
  - [ ] 6.6 `tests/Hexalith.Memories.Contracts.Tests/V1/ExportContractSerializationTests.cs` — mirror `ConsistencyContractSerializationTests` shape.

- [ ] **Task 7: Integration test (AC: #11)**
  - [ ] 7.1 Create `tests/Hexalith.Memories.IntegrationTests/Export/ExportWorkflowIntegrationTests.cs` with `[Trait("Category","Integration")]`. Two scenarios per AC #11. Apply `[Fact(Skip)]` with reason `"Aspire fixture build failure tracked in 5.6 Dev Notes"` if the Aspire CS0311 issue remains unresolved (verify via `dotnet build tests/Hexalith.Memories.IntegrationTests`); un-skip otherwise. Document skip status in Completion Notes.
  - [ ] 7.2 Fixture: reuse `AspireIngestionPipelineFixture` if present. Otherwise, create a minimal `AspireExportFixture` at `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireExportFixture.cs` that warms up a single tenant with 3 memory units + 1 edge.
  - [ ] 7.3 Seeding pattern: (a) provision a fresh tenant; (b) ingest 3 memory units across 2 cases; (c) manually create one causal edge via the FalkorDB CLI helper used by existing integration tests; (d) `GET /api/tenants/{tenantId}/cases/{caseId}/export`; (e) parse the stream; (f) assert manifest + units + edge present and IDs match.

- [ ] **Task 8: Docs (AC: #12)**
  - [ ] 8.1 Author `docs/dev/export.md` with the sections enumerated in AC #12. Cross-link from `docs/dev/consistency.md` (8.2) under its "See also" section; cross-link from `docs/dev/telemetry.md` (7.5) to note the non-audit scope. Include a dedicated top-level **"Known Compromises"** section (per AC #12) that enumerates dangling cross-case edge targets (Risk #6), graph-scoped edge IDs (not re-import identity), advisory snapshot isolation (Risk #2 in-flight indexing), and raw-bytes-not-exported. Each compromise gets its own subsection with a 2-3 sentence explanation and a clear "Impact on future re-import" line.
  - [ ] 8.2 Worked example: embed a 3-unit / 1-edge export JSON in the doc as a fenced block so developers can copy-paste.

- [ ] **Task 9: Sprint-status + final validation (AC: all)**
  - [ ] 9.1 Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: `8-3-data-export: backlog` → `in-progress` at dev-story start; → `review` at completion. Story file Status: `ready-for-dev` → `in-progress` → `review`.
  - [ ] 9.2 Run full test suite (Server + Cli + Contracts + IntegrationTests) — target: 1823 + new tests; 0 failed. (The current baseline at 8.2 Phase C landing is 1823 passing + 8 skipped integration tests.)
  - [ ] 9.3 Run `dotnet build Hexalith.Memories.slnx` — target: 0 warnings / 0 errors.
  - [ ] 9.4 Manual smoke test: `memories export case --tenant <test> --case <sample-case> | jq .manifest` returns a valid manifest; `memories export tenant --tenant <test> --output /tmp/export.json` writes a valid JSON file; diff against a hand-constructed expected file for the test tenant.

## Dev Notes

### Pre-flight verification (run before Task 1)

1. **Confirm sprint status.**
   ```bash
   grep "8-3-data-export" _bmad-output/implementation-artifacts/sprint-status.yaml
   # Expect: 8-3-data-export: ready-for-dev
   ```
2. **Verify Story 8.2 is review or done** (8.3 depends on the consistency block NOT moving post-8.2, and on any `Program.cs` / `MemoriesClient.cs` / `CliJsonContext` edits being settled).
   ```bash
   grep "8-2-consistency-verification-and-repair" _bmad-output/implementation-artifacts/sprint-status.yaml
   # Expect: 'review' or 'done'. If 'in-progress' or 'ready-for-dev', coordinate landing order.
   ```
3. **Confirm the Graph query builder methods we rely on are unchanged.**
   ```bash
   grep -n "BuildListCaseMemoryUnitIds\|BuildCountCaseMemoryUnits\|BuildMergeCaseNode" src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs
   # Expect: present at lines around 51, 34, 27 (per capture on 2026-04-20).
   ```
4. **Verify `CaseService.ParseMemoryUnitFromHash` + `ParseCaseFromHash` accessibility.**
   ```bash
   grep -n "ParseMemoryUnitFromHash\|ParseCaseFromHash" src/Hexalith.Memories.Server/Cases/CaseService.cs
   # If they are private — refactor to `internal static` at Task 1.2 time (keep the existing call sites working).
   ```
5. **Read the existing export-related doc in `docs/dev/` to avoid conflict.**
   ```bash
   ls docs/dev/export.md 2>/dev/null && echo "File exists — review before overwrite."
   ```
6. **Confirm DAPR + Aspire package versions.** Use `grep -n "Dapr\|Aspire\|StackExchange.Redis\|NFalkorDB" Directory.Packages.props` to confirm — export does not touch DAPR Workflow (deliberately NO workflow involved); only Redis + FalkorDB clients. Verify package versions are stable.
7. **Confirm Aspire fixture build state (Task 7.1 skip decision).**
   ```bash
   dotnet build tests/Hexalith.Memories.IntegrationTests
   # Success → un-skip the two integration tests. Failure with CS0311 → keep the skip.
   ```

If any step surfaces unexpected state, **stop and sync with the SM / user before coding** — the assumptions in this story are pinned to the state captured on 2026-04-20 (post-8.2 Phase C landing).

### Architecture alignment

- **No DAPR Workflow.** Export is a **synchronous streaming HTTP operation**, not an orchestrated workflow. Rationale: (a) DAPR Workflow state-store has a ~1 MiB per-instance limit, which caps the usable size of a workflow result — incompatible with multi-GB exports; (b) workflow engine overhead (state persistence, replay determinism) is unjustified when the operation is a single request-response with no fan-out; (c) streaming response semantics align with HTTP/1.1 chunked transfer — pipe raw bytes to the client. This is a **deliberate design decision** and diverges from Story 8.1 (health) + 8.2 (consistency). Documented in `docs/dev/export.md` under "Architecture notes".
- **FR71 was originally deferred to Phase 2** per architecture.md:1501 (`TenantExportService.cs` added in Phase 2`). Story 8.3 RE-INCLUDES it in MVP per the Epic 8 scope (epics.md:1630). Resolution: implement the scoped, streaming version described here — cheaper than the Phase 2 "full portability" story which could add re-import, differential exports, and signed manifests. The current implementation is **forward-compatible** with a Phase 2 upgrade (additive schema, stable IDs).
- **Parameterized Cypher only** (Decision D9). All FalkorDB operations go through `IGraphQueryBuilder`. Task 1.3 adds `BuildListEdgesForMemoryUnits` using parameter substitution.
- **Source-gen JSON only** (AOT-ready). All serialization uses `MemoriesJsonContext.Options`. No reflection fallback; Task 2.2 registers every new type.
- **EventId banks.** Previous banks: 8100-8199 (8.1 — reserved), 8200-8299 (8.2 — consumed). **Story 8.3 uses 8300-8399.** Concrete allocations: 8301 `ExportStarted`, 8302 `ExportMemoryUnitsEnumerated`, 8303 `ExportCompleted`, 8310 `ExportCancelled`, 8311 `ExportFailed`.
- **CLI global `--format` is IGNORED for export.** The export is a raw JSON stream. Setting `--format=table` prints a deterministic stderr warning and proceeds with the raw JSON. This is a deliberate UX choice documented in `docs/dev/export.md` — users who want tabular views use `jq` or `ConvertFrom-Json` on the output.

### Schema shape

The **canonical** tenant-export JSON shape (single-line examples condensed for readability; real output is pretty-printed at flush boundaries for ergonomics):

```json
{
  "manifest": {
    "schemaVersion": 1,
    "scope": "tenant",
    "tenantId": "acme",
    "caseId": null,
    "exportedAt": "2026-04-20T10:15:30.0000000+00:00",
    "snapshotAt": "2026-04-20T10:15:30.1234567+00:00"
  },
  "tenant": {
    "configuration": { "...": "... TenantConfigurationView — camelCase ..." },
    "status": "active",
    "createdAt": "2026-02-01T09:00:00.0000000+00:00",
    "lastUpdated": "2026-04-20T09:00:00.0000000+00:00"
  },
  "cases": [
    {
      "id": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
      "tenantId": "acme",
      "name": "Q1 Planning",
      "description": null,
      "status": "active",
      "createdAt": "2026-02-01T09:30:00+00:00",
      "lastUpdated": "2026-04-15T10:00:00+00:00",
      "memoryUnitCount": 15,
      "members": [
        { "memberId": "01HQ5QE...", "displayName": "alice@acme.com", "memberType": "user", "role": "editor", "addedAt": "2026-02-02T00:00:00Z" }
      ]
    }
  ],
  "memoryUnits": [
    {
      "unit": {
        "id": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
        "tenantId": "acme",
        "caseId": "01HM5Q9WXGK...",
        "content": "...",
        "contentHash": "sha256:...",
        "sourceUri": "https://...",
        "sourceType": "webpage",
        "ingestedBy": "alice@acme.com",
        "ingestedAt": "2026-02-15T14:00:00+00:00",
        "lastUpdated": "2026-02-15T14:00:00+00:00",
        "status": "ready",
        "metadata": { "topic": { "value": "planning", "origin": "human", "confidence": 1.0 } },
        "embeddingProvider": "google",
        "embeddingModel": "gemini-embedding-001",
        "embeddingDimensions": 768,
        "classification": "document",
        "failureDetails": null
      },
      "annotationTargets": ["01HQ5QE..."]
    }
  ],
  "edges": [
    {
      "id": "falkor-edge-4273",
      "sourceId": "01HM5Q9WXGK...",
      "targetId": "01HM5Q9WXGA...",
      "edgeType": "causedBy",
      "confidence": 0.95,
      "origin": "inferred",
      "createdAt": "2026-02-15T14:01:00+00:00",
      "verifiedBy": null,
      "previousConfidence": null
    }
  ],
  "statistics": {
    "memoryUnitCount": 42,
    "edgeCount": 87,
    "caseCount": 3
  }
}
```

**Case-scope** differs: the top-level object has `case` (singular) instead of `tenant` + `cases`; everything else is the same shape.

**Wrapped memory-unit record.** `MemoryUnit` does NOT carry `annotationTargets` — the domain record stays pure. Export uses a sealed wrapper `ExportedMemoryUnit(MemoryUnit Unit, IReadOnlyList<string> AnnotationTargets)` that composes the domain record with the export-scoped annotation projection. Re-importers parse `ExportedMemoryUnit` once, extract `Unit` into their domain model, and use `AnnotationTargets` to re-create the `annotates` edges. Previous-revision note: an earlier iteration of this spec emitted `annotationTargets` as a sibling field alongside the flattened `MemoryUnit` — rejected because `JsonSerializer.Deserialize<MemoryUnit>` silently drops unknown fields, leaving downstream consumers without type-safe access to the annotation data.

### Previous story intelligence

**Story 8.2 (Consistency Verification & Repair) — status `review` at 8.3 planning time.** Key alignment:

- 8.2 established the **"TL;DR / What does NOT ship / Risk-guard-test table / Pre-flight / AC authoritative"** story-shape template that 8.3 mirrors.
- 8.2 shipped the `Consistency/` service folder. 8.3 adds a new `Export/` folder at the same level — a single-purpose folder per operator concern, mirroring the `Consistency/`, `Cases/`, `Search/`, `Tenants/` layout.
- 8.2 added `BuildCountMemoryUnitEdges` + `BuildEnumerateMemoryUnitIds` to `IGraphQueryBuilder`. 8.3 adds `BuildListEdgesForMemoryUnits`. The three methods form a coherent "bulk-read" surface — do NOT refactor them into a sub-interface; add them directly to `IGraphQueryBuilder` per the existing pattern.
- 8.2's `ConsistencyCommandReceipt` record (start/status workflow pattern) does NOT apply to export — export is not a workflow and has no receipt. DO NOT reuse the receipt shape; export's response is the data itself.
- 8.2's `ConsistencyStubClient` pattern (Cli.Tests) DOES apply — `ExportStubClient` follows the same shape for CLI tests.

**Story 7.5 (Search & Access Telemetry) — done.** Key alignment:

- 7.5 scoped `AccessTelemetryEvent` to four operations: search / ingest / traverse / case-access. Export is NOT in that list. Regression test in 8.3 (Task 6.3) asserts no AccessTelemetryEvent emission for export endpoints.
- 7.5's trace-exclusion filter excludes `/health`, `/alive`, `/ready` from OpenTelemetry tracing. Export endpoints are NOT excluded — they should emit spans (they are long-running and benefit from tracing). Default ASP.NET Core tracing applies.
- 7.5 pinned EventId bank 7500-7599. 8.3 inherits the "one bank per story, top-of-file constant, no overlap" convention.

**Story 3.1 - 3.6 (Case Management) — done.** Key alignment:

- 3.1 shipped `CaseService.CreateCaseAsync` — case creation is non-atomic across Redis + FalkorDB (known limitation). Export CAN observe this: a case present in Redis but not in FalkorDB → `BuildListCaseMemoryUnitIds` returns empty. **Mitigation:** 8.3's case-export treats the Redis `case:` record as authoritative for the case metadata; if the graph query returns 0 memory units, the export emits `memoryUnits: []` + `edges: []` + `statistics` with zeros (not an error). Documented.
- 3.5 shipped memory unit deletion with `DETACH DELETE`. Deleted memory units are not in the export by definition. Soft-delete (status: `Failed`) units ARE included.
- 3.6 shipped annotations. Each annotation is a memory unit with `metadata["_system.annotation_target"]` pointing to the target MU. Export emits the annotation as a regular memory unit; the `annotationTargets[]` field on the TARGET memory unit lists inbound annotation IDs. Round-trippable via the `_system.annotation_target` metadata key.

**Story 5.5 (Tenant Configuration & Listing) — done.** Key alignment:

- 5.5 shipped `TenantConfigurationView` — safe to include in the tenant-export as-is (no secrets leak).
- 5.5 shipped `TenantEmbeddingConfig` — includes provider + model + keyed-secret references (keys stored via DAPR secret store; the references are opaque). Export includes these; re-import (future) can re-bind the keys against the target environment's secret store.

**Story 6.4 (Pipeline State Persistence & Zero Data Loss) — done.** Key alignment:

- 6.4 ships `CaseIngestionCounts` + in-flight workflow state. Export does NOT include workflow state (documented under "What does NOT ship"). Export is DOMAIN data only.

### Merge-conflict protocol

If 8.2 has not fully landed in `main` by the time 8.3 development starts, the two stories overlap in:
1. **`Program.cs`** — 8.2 appends new endpoints around line 1059-1266; 8.3 appends after line 1266. Line-distance enough for auto-merge; any manual resolution: 8.2 wins for the consistency block, 8.3 wins for the export block.
2. **`MemoriesClient.cs`** — 8.2 appends around line 633-806; 8.3 appends after. Same resolution.
3. **`MemoriesJsonContext.cs`** — 8.2 added consistency records; 8.3 adds export records. Order in `[JsonSerializable]` list is not load-bearing; merge by concatenating both blocks.
4. **`RootCommandFactory.cs`** — 8.2 added `consistency` group; 8.3 adds `export` group. The `CommandGroups` stub list and the Build() method — merge by concatenating.
5. **`CliJsonContext.cs`** — 8.2 added entries for receipt + inspection result + workflow state. 8.3 does NOT touch this (export is a raw byte stream on the CLI side).
6. **`ErrorMessageCatalog.cs`** — 8.2 added 5 codes; 8.3 adds 4 codes. No collisions.

If Story 8.4 (Tier-3 Telemetry Integration Tests) lands a change to `AspireIngestionPipelineFixture`, 8.3's Task 7.2 fixture reuse may conflict. Resolution: use 8.4's updated fixture if applicable, create `AspireExportFixture` if not.

### Project structure notes

**Paths (canonical):**

- `src/Hexalith.Memories.Contracts/V1/ExportManifest.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ExportScope.cs` (new enum)
- `src/Hexalith.Memories.Contracts/V1/ExportStatistics.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ExportedEdge.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ExportedTenantConfig.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/ExportedMemoryUnit.cs` (new — wrapper record, Task 2.1)
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (modified — add `[JsonSerializable]` entries for 6 new types)
- `src/Hexalith.Memories.Server/Export/TenantExportService.cs` (new)
- `src/Hexalith.Memories.Server/Export/ExportWriter.cs` (new, internal sealed)
- `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` (modified — add `BuildListEdgesForMemoryUnits`)
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` (modified — implement new method)
- `src/Hexalith.Memories.Server/Program.cs` (modified — register `TenantExportService` + 2 new endpoints)
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` (modified IF `ParseMemoryUnitFromHash` / `ParseCaseFromHash` are currently `private` — bump to `internal static` for `TenantExportService` reuse; verify at Task 1.2 time and amend if needed)
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (modified — 2 new methods)
- `src/Hexalith.Memories.Cli/Commands/ExportCaseCommand.cs` (new)
- `src/Hexalith.Memories.Cli/Commands/ExportTenantCommand.cs` (new)
- `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` (modified — register `export` group)
- `src/Hexalith.Memories.Cli/Export/CountingStream.cs` (new, internal sealed — byte-count progress decorator; replaces the earlier JSON-aware tracker)
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` (modified — 4 new codes)
- `tests/Hexalith.Memories.Server.Tests/Export/` (new folder — 2 test classes)
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ExportEndpointTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientExportTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ExportCaseCommandTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/ExportTenantCommandTests.cs` (new)
- `tests/Hexalith.Memories.Contracts.Tests/V1/ExportContractSerializationTests.cs` (new)
- `tests/Hexalith.Memories.IntegrationTests/Export/ExportWorkflowIntegrationTests.cs` (new; `[Trait("Category","Integration")]`)
- `docs/dev/export.md` (new)
- `docs/dev/consistency.md` (modified — cross-link)
- `docs/dev/telemetry.md` (modified — cross-link — reiterate the non-audit scope)

**Services folder `Export/` vs. top-level `Server/`.** The new service and writer form a cohesive unit. Grouping them in `Export/` mirrors existing `Cases/`, `Search/`, `Tenants/`, `Consistency/`, `HealthChecks/`, `Telemetry/`, `Ingestion/`, `Graph/` — do NOT scatter them across the Server root.

**Factor-vs-duplicate decisions:**

- **`ParseMemoryUnitFromHash` + `ParseCaseFromHash`** — **factor** up to `internal static` if currently private. Rationale: the export service must produce identical parsing behavior as the read path; any drift becomes a silent bug. Behavior-preserving refactor with existing tests as safety net.
- **`GetAnyServer` Redis helper** — **duplicate** the 10 LOC from `TenantMetricsService` into `TenantExportService`. Two callers is not enough to justify a shared helper; if a fourth caller arrives (i.e., after 8.2's enumeration activity + 5.5's metrics service + 8.3's export), consider factoring into `src/Hexalith.Memories.Server/Infrastructure/RedisServerHelper.cs`. This is a `TODO` in Story 8.2's Dev Notes; 8.3 pushes the count to 3 — still under the factor-threshold. Add a comment `// TODO: factor with TenantMetricsService.GetAnyServer + EnumerateMemoryUnitIdsActivity when a 4th caller lands` at the duplicated helper.
- **`ExportWriter` — internal sealed** — not public. Consumers use `TenantExportService` only; the writer is an implementation detail. No `I` interface.

**DTO streaming semantics.** `ExportWriter` writes through a `PipeWriter` obtained from `HttpContext.Response.BodyWriter`. The `Utf8JsonWriter` is initialized over `pipeWriter.AsStream()` (Stream adapter). Flush cadence: **every 1000 memory units** OR **every 1 MiB of unflushed bytes**, whichever occurs first. Flushing calls `utf8Writer.FlushAsync(ct)` + `pipeWriter.FlushAsync(ct)`. This yields a first-byte time on the order of a few ms (after header write + manifest emission) and a bounded memory footprint.

### Fan-out vs. sequential enumeration

**Edge enumeration: batched sequential.** For case-scope export, issue one Cypher per 100 MU IDs (`BuildListEdgesForMemoryUnits`). For tenant-scope, maintain a rolling buffer: as MUs stream in from the `SCAN` enumeration, accumulate MU IDs in a buffer; flush the buffer to the edge query when it reaches 100. This amortizes the FalkorDB round-trip cost without unbounded memory growth.

**Memory unit enumeration: sequential via SCAN.** The `server.KeysAsync(pattern, pageSize: 250)` call yields keys lazily; the writer emits each as the cursor advances. No pre-materialization into a `List<string>`.

**Cancellation.** Every `await` inside the service is passed the same `CancellationToken` (flowing from `HttpContext.RequestAborted`). The enumeration loop checks `ct.IsCancellationRequested` between batches (Risk #5). Disposal of `ExportWriter` in a `finally` flushes any buffered bytes so the client sees a partial-but-parseable document rather than a raw cut-off stream.

### Error-response semantics

**Pre-stream errors (404, 400) ARE real HTTP errors** with `ErrorResponse` JSON body. The endpoint validates tenant + case (and captures the snapshot) BEFORE calling `context.Response.StartAsync()`; any failure here returns a clean HTTP status + JSON.

**Mid-stream errors** — the response has already begun; HTTP headers are already committed. The only recovery is to **terminate the connection** (close the PipeWriter with an error) and log the failure at `LogLevel.Error` with `ExportFailed` EventId 8311. The client observes a truncated response (`HttpRequestException` or similar). The CLI's `--output` path deletes the part-file. The CLI's stdout path leaves the caller with a truncated document — the caller should validate the JSON before consuming (a missing final `}` is a reliable truncation indicator, or a missing `"statistics"` section).

Document this in `docs/dev/export.md` under "Error handling".

### Testing standards

- **Unit test conventions** (from existing projects):
  - xUnit `[Fact]` / `[Theory]`; NSubstitute for mocking; Shouldly for assertions; **NOT** FluentAssertions.
  - Test classes: `ClassNameTests`, methods: `MethodName_Scenario_Expected`.
  - Arrange / Act / Assert comments preserved.
  - For `TenantExportService` tests, use NSubstitute on the DI dependencies; assert the emitted JSON via `JsonDocument.Parse` + `doc.RootElement.GetProperty(...)` + Shouldly `.ShouldBe`.
  - For the "bounded memory" guard test (Risk #1), use `GC.GetAllocatedBytesForCurrentThread()` before/after; compute delta; assert under a **generous** threshold (10 MiB). The test is intentionally non-strict to avoid flakiness on GC behavior.
- **Integration test conventions:**
  - `[Trait("Category","Integration")]`.
  - Reuse the existing Aspire fixture if possible; add `AspireExportFixture` if the warmup is wrong for this story.
  - `[Fact(Skip)]` with reason `"Aspire fixture build failure tracked in 5.6 Dev Notes"` when the Aspire CS0311 issue blocks execution.
- **Test count target (informational — AC #11 is authoritative; drop this table if it drifts):** ~44+ new unit tests + 2 integration tests (possibly skipped). Distribution snapshot — AC #11 wins on any conflict:
  - TenantExportServiceTests: 14
  - ExportWriterTests: 6
  - ExportEndpointTests: 10
  - MemoriesClientExportTests: 4
  - ExportCaseCommandTests: 6
  - ExportTenantCommandTests: 4
  - ExportContractSerializationTests: 1 Theory × 5 rows + 2 standalone tests
  - ExportWorkflowIntegrationTests: 2 (possibly skipped)

### Anti-patterns to avoid

1. **Don't buffer the full export in-memory before serialization.** Risk #1 is load-bearing. Use `Utf8JsonWriter` attached directly to the response `PipeWriter`. NEVER materialize `List<MemoryUnit> all = ...` before serializing.
2. **Don't use a DAPR Workflow for export.** Export is synchronous streaming; workflow state-store limits are incompatible. See "Architecture alignment" above.
3. **Don't register a new formatter for the raw JSON export.** The CLI's formatter router is for structured payloads. Export is a raw byte stream; the CLI pipes bytes through unchanged.
4. **Don't include secrets in the export.** `TenantEmbeddingConfig` already redacts them via `TenantConfigurationView`; do NOT re-inject the raw secret values into `ExportedTenantConfig`. Guard test `ExportContractSerializationTests.TenantConfig_RoundTrip_NoSecrets` enforces this.
5. **Don't use `Uri.EscapeDataString(memoryUnitId)` on path segments** — memory unit IDs are ULIDs (Crockford-base32) which are URL-safe; extra escaping corrupts the path. Match the existing `MemoriesClient` patterns.
6. **Don't raw-Cypher the edge enumeration.** Task 1.3 adds `BuildListEdgesForMemoryUnits` via the builder. Every query goes through `IGraphQueryBuilder`.
7. **Don't skip `HttpCompletionOption.ResponseHeadersRead`.** Without it, `HttpClient` buffers the ENTIRE response body before returning — defeats the streaming contract from the client side. Task 4.1 pins this.
8. **Don't write progress to stdout.** The stdout is the JSON data itself; any progress output on stdout corrupts the file. Use stderr.
9. **Don't emit per-memory-unit Info logs.** Log budget: EventId 8302 fires every 1000 units. 10K-unit export → 10 progress logs, not 10K.
10. **Don't persist the export to server-side disk.** Export is ephemeral; the server streams directly to the client. NO intermediate file. Rationale: disk IO adds latency, creates cleanup risk, and couples export to local storage (complicates horizontal scaling).
11. **Don't add `AnnotationTargets` to `MemoryUnit` directly.** The domain record stays pure; export composes `ExportedMemoryUnit(MemoryUnit, IReadOnlyList<string>)` instead (Task 1.4 + Task 2.1). Coupling export-only fields into `MemoryUnit` drags export concerns into every read path.
12. **Don't add new OpenTelemetry tags to existing activities.** Export endpoints emit default ASP.NET Core spans. No custom activity source for export.
13. **Don't mix ID prefixes in the edge query.** `BuildListEdgesForMemoryUnits` matches on `MemoryUnit {id: muId}` — the bare ULID, NO prefix. Redis key construction uses `{tenantId}:mu:{id}` but that is Redis-layer concern; the graph node stores the bare id.

### Git history context

Recent relevant commits (run `git log --oneline` to confirm ordering):

- `b681a40 Add unit tests for health checks and workflows` — Story 8.1 test coverage; no direct overlap with 8.3 but confirms the unit-test harness patterns.
- `788f40c Add telemetry tests and infrastructure for metrics and activity source validation` — Story 7.5 follow-up; `AccessTelemetryEnricher`-exclusion pattern relevant to Risk #9.
- `958164b Add integration and unit tests for Quickstart CLI functionality` — 7.4 close-out; CLI test patterns to mirror.
- `1d8e3af feat: Update framework setup progress and enhance test suite documentation` — test harness adjustments.
- `4136f83 Add comprehensive CLI error handling and catalog tests` — Story 7.3; `ErrorMessageCatalog` extension pattern relevant to Task 5.5.

Story 8.2 (`review` at planning time) commits are in-flight on the working tree; merge with main before 8.3 dev-story starts.

### Effort breakdown

| Task | Estimate |
|------|---------:|
| Task 1 (export service + writer + edge builder method) | 1.0 day |
| Task 2 (contracts — 5 records) | 0.25 day |
| Task 3 (REST endpoints — 2 endpoints) | 0.5 day |
| Task 4 (client methods — 2 methods) | 0.25 day |
| Task 5 (CLI commands + progress tracker + formatters + errors) | 0.75 day |
| Task 6 (unit tests — ~38 tests) | 1.25 days |
| Task 7 (integration test) | 0.25 day (skip-path) or 0.75 day (active) |
| Task 8 (docs) | 0.25 day |
| Task 9 (sprint-status + final) | 0.5 day |
| **Total** | **~5 days** |

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 8 overview (lines 1527-1530) and Story 8.3 ACs (lines 1630-1660)]
- [Source: _bmad-output/planning-artifacts/prd.md — FR71 (export) line 929]
- [Source: _bmad-output/planning-artifacts/architecture.md — line 1440 (Data Portability folder), line 1501 (FR71 deferred Phase 2 — Story 8.3 re-includes in MVP), line 1514 (resolved gap note)]
- [Source: _bmad-output/implementation-artifacts/8-2-consistency-verification-and-repair.md — story-shape template, risk-guard-test pattern, endpoint-insertion pattern (Program.cs:1059-1266), stub-client CLI test pattern (ConsistencyStubClient)]
- [Source: _bmad-output/implementation-artifacts/8-1-health-checks-and-readiness.md — story-shape template, Aspire CS0311 deferral pattern]
- [Source: _bmad-output/implementation-artifacts/7-5-search-and-access-telemetry.md — AccessTelemetryEvent scope (4 audited operations) + trace-exclusion invariant + regression-test pattern (Risk #9)]
- [Source: _bmad-output/implementation-artifacts/3-1-create-and-list-cases.md — CaseService patterns; case-creation non-atomicity (known-limitation)]
- [Source: _bmad-output/implementation-artifacts/5-5-tenant-configuration-and-listing.md — TenantConfigurationView, TenantMetricsService SCAN pattern]
- [Source: _bmad-output/implementation-artifacts/6-4-pipeline-state-persistence-and-zero-data-loss.md — ingestion-state-persistence scope; why export does not include workflow state]
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs — case + MU read patterns; `ParseMemoryUnitFromHash` + `ParseCaseFromHash` (internal/private; verify at Task 1.2); `ListCasesAsync` + `ListCaseMembersAsync` reuse]
- [Source: src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs — `BuildListCaseMemoryUnitIds`, `BuildCountCaseMemoryUnits`, `BuildEnumerateMemoryUnitIds`; extend with `BuildListEdgesForMemoryUnits`]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs:57-81 — SCAN pattern to mirror for tenant-scope enumeration]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs — tenant metadata reader]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs — `GetTenantConfigurationAsync` for `TenantConfigurationView` hydration]
- [Source: src/Hexalith.Memories.Server/Program.cs:1059-1266 — consistency endpoints (insertion neighbors); line 1269+ cases block]
- [Source: src/Hexalith.Memories.Server/Program.cs (header-writing pattern for endpoints that stream — reference ASP.NET Core minimal-API streaming docs if unfamiliar; `HttpContext.Response.StartAsync()` + `BodyWriter`)]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs — source-gen registration target (add 5 new types)]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs — canonical record; export composes with `annotationTargets[]` without wrapper]
- [Source: src/Hexalith.Memories.Contracts/V1/GraphEdge.cs — existing edge record; `ExportedEdge` is a strict superset]
- [Source: src/Hexalith.Memories.Contracts/V1/CamelCaseStringEnumConverter.cs — enum-converter pattern for `ExportScope`]
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs — client method shape template; `ErrorResponseDecoder.DecodeAsync` on non-2xx]
- [Source: src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs — synchronous CLI command shape template]
- [Source: src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs — command-group registration pattern]
- [Source: src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs — error-code registry target]
- [Source: tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs — theory-per-record shape template for `ExportContractSerializationTests`]
- [Source: tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyVerifyCommandTests.cs — CLI command test pattern to mirror for export tests]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
