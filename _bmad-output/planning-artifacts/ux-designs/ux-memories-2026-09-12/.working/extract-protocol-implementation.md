# Protocol and Implementation Contract Extraction

Source-inspection snapshot for the UX migration, 2026-09-12. This note records what current source safely supports. It does not change the phase or product decisions in `prd.md`; where code and PRD diverge, the PRD remains the scope authority and the implementation difference is a delivery gap.

## What is safe to name

- The MCP server registers exactly four tools: `search_memory`, `ingest_content`, `traverse_relations`, and `get_case_info`. The composition root registers those four concrete tool types, and contract/integration tests expect exactly those names. [`src/Hexalith.Memories.Mcp/McpCompositionRoot.cs`:81–87; `tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs`:30–67; `tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs`:18–35, 52–84]
- The canonical public trust type is `Hexalith.Memories.Contracts.V1.EvidencePacket`. It is explicitly described as cross-surface and is registered in the shared V1 JSON context. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:6–29; `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`:14–68]
- Shared JSON uses web defaults and camel-case property names. The type and enum names below are C# names; wire fields are their lower-camel forms and enum values use the repository’s camel-case string converters unless noted otherwise. [`src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`:210–223; `src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs`:3–14]
- REST route constants live in `Contracts.V1.MemoriesRoutes` under `/api/v1` and are shared by server and REST client. They are path constants, not a wire DTO. [`src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs`:8–30]

## MCP tool schemas and current behavior

### `search_memory`

Registered method: `SearchMemoryTool.SearchAsync`.

| Input field | Required | Current type/default/behavior |
|---|---:|---|
| `tenantId` | yes | `string`; non-empty; later matched exactly to an authenticated tenant claim. |
| `query` | yes | `string`; non-empty natural-language or keyword query. |
| `case` | no | nullable `string`; scopes search to a case. The public MCP field is literally `case`, not `caseId`. |
| `axes` | no | `SearchAxis`, default `Hybrid`. Values: `Syntactic`, `Semantic`, `Nl`, `Hybrid`. |
| `maxResults` | no | `int`, default 10, clamped to 1..100. |
| `tokenBudget` | no | nullable `int`; only positive values are forwarded, otherwise null. |
| `explain` | no | `bool`, default false. |

Source: [`src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`:44–71, 73–100].

Behavior:

- `Hybrid` builds the public REST-client type `HybridSearchRequest(TenantId, Query, CaseId, MaxResults, Explain, TokenBudget)` and returns a `Contracts.V1.HybridSearchResult` with an attached `EvidencePacket`. [`src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`:102–120; `src/Hexalith.Memories.Client.Rest/HybridSearchRequest.cs`:8–24]
- Other MCP axes build `SearchRequest(TenantId, Axis, Query, CaseId, SourceType, MetadataQuery, Subject, MaxResults, Offset, Explain, TokenBudget, AttributeFilters)` and return `Contracts.V1.SearchResult` with an attached `EvidencePacket`. [`src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`:123–139; `src/Hexalith.Memories.Client.Rest/SearchRequest.cs`:8–36]
- `SearchAxis` deliberately excludes `Graph`; the tool description directs graph requests to `traverse_relations`. The generated schema may render enum literals in PascalCase even though deserialization is case-insensitive and descriptions teach lowercase. [`src/Hexalith.Memories.Mcp/Tools/SearchAxis.cs`:12–27; `src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`:54–70; `tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs`:137–162]
- Tenant and case Evidence Packet scope is produced as `EvidencePacketScope(tenantId, caseId, Authorized, "tenant"|"tenant-case")` only after the shared executor authorizes the tenant claim. [`src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`:97–139, 153–154; `src/Hexalith.Memories.Mcp/McpToolExecutor.cs`:52–76]

Safe response fields:

- `SearchResult`: `results`, `totalCount`, `hasIndexedMemoryUnits`, `query`, optional `explanation`, optional `caseGroups`, `omittedCount`, `estimatedTokensTotal`, `omittedReason`, `degraded`, optional `unavailableAxes`, optional `axesUsed`, optional `evidencePacket`. [`src/Hexalith.Memories.Contracts/V1/SearchResult.cs`:10–69]
- `HybridSearchResult`: `results`, `totalCount`, `degraded`, `unavailableAxes`, nullable `allEnabledAxesUnavailable`, `query`, optional `explanation`, optional `caseGroups`, `omittedCount`, `estimatedTokensTotal`, `omittedReason`, optional `axesUsed`, optional `evidencePacket`. [`src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`:10–64]
- Each `FusedScoredResult`: `memoryUnitId`, `compositeScore`, `contentSnippet`, `sourceUri`, `sourceType`, nullable `syntacticScore`, `semanticScore`, `graphScore`, `nlScore`, optional `caseId`, `caseName`, and `annotationsCount`. [`src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`:67–107]

### `ingest_content`

Registered method: `IngestContentTool.IngestAsync`.

| Input field | Required | Current type/default/behavior |
|---|---:|---|
| `tenantId` | yes | `string`; non-empty and tenant-claim authorized. |
| `caseId` | yes | `string`; non-empty. |
| `content` | yes | `string`; UTF-8 text or base64 bytes according to description, but the current implementation always calls `Encoding.UTF8.GetBytes(content)`. |
| `sourceType` | no | `McpSourceType`; default `File`. Enum also advertises `Url` and `Event`, but both are rejected with `UNSUPPORTED_SOURCE_TYPE`. |
| `sourceUri` | no | nullable `string`; default `mcp://content`. |
| `contentType` | no | nullable `string`; default `text/plain`. |
| `ingestedBy` | no | `string`; default `mcp`. |

Source: [`src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`:20–27, 45–72, 74–135, 143–154].

Current success payload is the internal implementation type `IngestContentResponse` with one stable wire field, `workflowInstanceId`. It is not a public `Contracts.V1` record, so the wire field is safer for UX examples than the C# type name. [`src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`:133–140; `tests/Hexalith.Memories.Mcp.Tests/IngestContentToolTests.cs`:20–34]

### `traverse_relations`

Registered method: `TraverseRelationsTool.TraverseAsync`.

| Input field | Required | Current type/default/behavior |
|---|---:|---|
| `tenantId` | yes | `string`; non-empty and tenant-claim authorized. |
| `from` | yes | `string` memory-unit id. |
| `depth` | no | `int`, default 3, clamped to 0..10. |
| `edgeType` | no | nullable comma-separated `string`; accepts enum names case-insensitively and documents `causedBy`, `correlatedWith`, `references`, `contains`, `annotates`. |
| `tokenBudget` | no | nullable `int`; only positive values are forwarded. |
| `caseId` | no | nullable `string`; the current flattened graph scope. No `graphScope` object is exposed. |

Source: [`src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs`:42–66, 68–123, 126–150; `tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs`:191–203].

Current success type is `Contracts.V1.TraversalResult`:

- `startNodeId`, `depth`, `nodes`, `totalNodeCount`, `totalCount`, `gapMarkers`, `omittedCount`, `estimatedTokensTotal`, `omittedReason`, `degraded`, optional `unavailableAxes`, and wire-only `primaryPathIntact: false` when the primary path was broken; true is omitted. [`src/Hexalith.Memories.Contracts/V1/TraversalResult.cs`:10–83]
- `TraversalNode` fields are `memoryUnitId`, `contentSnippet`, `sourceUri`, `sourceType`, `ingestedAt`, `hopDistance`, and `edges`. [`src/Hexalith.Memories.Contracts/V1/TraversalNode.cs`:1–11]
- `TraversalEdgeInfo` fields are `edgeType`, `confidence`, `origin`, `connectedNodeId`, `direction`, optionally `verifiedBy` and `previousConfidence`. [`src/Hexalith.Memories.Contracts/V1/TraversalEdgeInfo.cs`:1–19]
- `TraversalGapMarker` fields are `missingNodeId`, `hopDistance`, and `edges`. [`src/Hexalith.Memories.Contracts/V1/TraversalGapMarker.cs`:8–15]

### `get_case_info`

Registered method: `GetCaseInfoTool.GetCaseAsync`.

Inputs are required `tenantId` and `caseId` strings. The success response is the public `Contracts.V1.Case` record with fields `id`, `tenantId`, `name`, optional `description`, `status`, `createdAt`, `lastUpdated`, and `memoryUnitCount`. `CaseStatus` values are `active`, `closed`, and `deleting`. [`src/Hexalith.Memories.Mcp/Tools/GetCaseInfoTool.cs`:36–79; `src/Hexalith.Memories.Contracts/V1/Case.cs`:10–20; `src/Hexalith.Memories.Contracts/V1/CaseStatus.cs`:10–17]

The tool description mentions “member count” and “recent activity timestamps,” but `Case` has no member-count field and only exposes `lastUpdated`. Those richer claims are not safe to put in the migrated response contract. [`src/Hexalith.Memories.Mcp/Tools/GetCaseInfoTool.cs`:41–48; `src/Hexalith.Memories.Contracts/V1/Case.cs`:10–20]

## MCP transport, authorization, and errors

- Successful tools return a `CallToolResult` whose text content is the JSON serialization and whose `structuredContent` is the same typed payload; `isError=false`. Shared `MemoriesJsonContext.Options` governs serialization. [`src/Hexalith.Memories.Mcp/McpToolResultSerializer.cs`:15–49]
- Every tool first performs tool-specific validation, then exact tenant-claim authorization, then invokes the operation with the authorized tenant snapshot. Malformed, unauthenticated, missing-context, and tenant-mismatch calls fail before the upstream operation. [`src/Hexalith.Memories.Mcp/McpToolExecutor.cs`:34–89; `src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs`:16–74]
- Tenant ids must match `^[A-Za-z0-9_-]{1,128}$` and an inbound tenant claim exactly using ordinal comparison. [`src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs`:29–74, 100–101]
- The validated caller bearer is forwarded unchanged to the Memories Server; MCP does not mint a replacement identity. [`src/Hexalith.Memories.Mcp/McpCompositionRoot.cs`:120–140]

The public V1 server error is:

`ErrorResponse(Code, Message, Suggestion)` → wire `{ code, message, suggestion }`. [`src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs`:1–4]

MCP errors use an implementation-private `McpErrorPayload`. Its current wire fields are safe to describe, but its private C# type name should not be treated as a public contract:

`{ code, service, tool, message, suggestion, evidencePacket? }`

The `CallToolResult` also sets `isError=true` and includes a formatted text line. Remote server errors retain their code; transport failures map to `NETWORK_ERROR`, unexpected failures to `INTERNAL_ERROR`, tenant mismatch to `TENANT_FORBIDDEN`, and malformed tenant id to `TENANT_MALFORMED`. Unexpected text is sanitized and stack traces are not exposed. [`src/Hexalith.Memories.Mcp/McpErrorMapper.cs`:17–40, 42–105, 108–185, 194–203]

Authorization errors attach an Evidence Packet with `state=unauthorized`, no sources/evidence, omission reason `authorization`, and `checkAuthorization` recovery. Other mapped failures use `state=degraded` and retry-oriented recovery. [`src/Hexalith.Memories.Mcp/McpErrorMapper.cs`:116–152; `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:195–283]

## Canonical Evidence Packet type and wire fields

### Top level

`EvidencePacket` wire fields:

| Field | Type |
|---|---|
| `scope` | `EvidencePacketScope` |
| `result` | `EvidencePacketResultSummary` |
| `sources` | list of `EvidencePacketSource` |
| `evidence` | `EvidencePacketEvidence` |
| `graph` | `EvidencePacketGraphSummary` |
| `state` | `EvidencePacketState` |
| `omittedDetails` | `EvidencePacketOmittedDetails` |
| `recovery` | list of `EvidencePacketRecoveryAction` |
| `metadata` | optional `EvidencePacketMetadata` |

Source: [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:10–29].

### Nested records

- `EvidencePacketScope`: `tenantId`, optional `caseId`, `isolationStatus`, `permissionsContext`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:31–40]
- `EvidencePacketResultSummary`: `query`, `totalCount`, `returnedCount`, nullable `hasIndexedMemoryUnits`, optional `summary`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:42–53]
- `EvidencePacketSource`: `rank`, `memoryUnitId`, `sourceUri`, `sourceType`, `snippet`, nullable `score`, optional `caseId`, `caseName`, `annotationsCount`, optional `timestamp`, `freshness`, `ingestion`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:55–80]
- `EvidencePacketEvidence`: `evidenceStrength`, `caveat`, `axesUsed`, `unavailableAxes`, `degraded`, nullable `allEnabledAxesUnavailable`, `axisEvidence`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:82–97]
- `EvidencePacketAxisEvidence`: `axis`, nullable `score`, optional `normalizationMethod`, optional `description`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:99–108]
- `EvidencePacketGraphSummary`: `available`, `relatedPath`, `edgeTypes`, `gapMarkers`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:110–119]
- `EvidencePacketOmittedDetails`: `omittedCount`, `estimatedTokensTotal`, `reason`, `fieldNames`, `detailGroups`, `expansionHandles`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:121–134]
- `EvidencePacketExpansionHandle`: `handle`, `kind`, `targetDetailGroup`, `tenantId`, optional `caseId`, `guidance`. Treat `handle` as opaque even though the current mapper emits an `ep:v1:…` prefix. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:136–149; `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:610–615]
- `EvidencePacketRecoveryAction`: `kind`, `label`, `guidance`, `target`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:151–160]
- `EvidencePacketMetadata`: optional `freshness`, `benchmark`, and `mcpSchema`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMetadata.cs`:10–17]
- `EvidencePacketFreshness`: `state` plus optional `producedAt`, `lastCheckedAt`, `expiresAt`, `ageSeconds`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketFreshness.cs`:10–21]
- `EvidencePacketIngestionMetadata`: `stage` plus optional `stageDetail`, `updatedAt`, `retryCount`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketIngestionMetadata.cs`:10–19]
- `EvidencePacketMcpSchema`: `toolName`, `schemaName`, `schemaVersion`, optional `transport`. The type exists, but current production tool mapping does not populate it. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMcpSchema.cs`:10–19; `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:109–124, 177–192]

### Enum vocabularies currently on the wire

| Type | Current camel-case wire values |
|---|---|
| `EvidencePacketIsolationStatus` | `authorized`, `unknown`, `unauthorized` |
| `EvidencePacketState` | `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, `pendingExpansion` |
| `EvidencePacketEvidenceStrength` | `none`, `unknown`, `weak`, `moderate`, `strong` |
| `EvidencePacketOmissionReason` | `none`, `tokenBudget`, `density`, `redaction`, `policy`, `authorization`, `backendUnavailable`, `trueAbsence`, `combined` |
| `EvidencePacketRecoveryKind` | `none`, `retry`, `increaseTokenBudget`, `increaseMaxResults`, `fetchMemoryUnit`, `broadenScope`, `checkAuthorization`, `inspectBackendHealth`, `useTraversal` |

Source: [`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`:162–287].

The lower search envelopes use `OmittedReason` values `none`, `tokenBudget`, `backendDegraded`, and `combined`. The mapper translates these into the broader Evidence Packet omission vocabulary. [`src/Hexalith.Memories.Contracts/V1/OmittedReason.cs`:10–24; `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:349–358]

### Current mapper semantics

- State precedence is unauthorized → degraded → pending expansion when anything was omitted → empty → weak → complete. `partial` and `stale` exist in the enum but are not emitted by `FromSearchResult` / `FromHybridSearchResult`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:285–316]
- Current evidence-strength thresholds use the best mapped score: ≤0 = `none`, <0.4 = `weak`, <0.75 = `moderate`, otherwise `strong`. Single-axis strength becomes `unknown` unless every used axis is `semantic`, because current syntactic/graph values are treated as unbounded raw scores. Hybrid strength uses the normalized composite score. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:88–90, 157–158, 318–347, 587–592]
- Search source mapping fills identity, URI/type, snippet, score, case, and annotation count. It does not currently fill source `timestamp`, `freshness`, or `ingestion`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:72–83, 141–152]
- Search Evidence Packets currently set `graph.available=false` with empty path/types/gaps, even for hybrid results. Graph evidence remains present separately as nullable `graphScore` on `FusedScoredResult`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:109–124, 177–192; `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`:85–95]
- Token-budget omission records `sources` / `rankedResults`, produces a tenant/case-scoped expansion handle, and advises rerunning with a larger `tokenBudget` or `maxResults`. Degraded-axis omission records `evidence.unavailableAxes` / `backendDiagnostics`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:360–410]
- Recovery mapping is explicit: unauthorized → `checkAuthorization`; degraded → `retry` + `inspectBackendHealth`; token omission → `increaseTokenBudget`; other expansion → `increaseMaxResults`; empty/weak → `broadenScope`. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:412–472]
- The default caveat is exactly: “Scores measure query-result relevance, not factual accuracy or data completeness.” [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:13–15]

## Status and ingestion vocabularies

### Canonical stored unit status

`MemoryUnitStatus` values are:

`Queued → Extracting → Embedding → Indexing → Indexed`, plus `Failed`.

Wire values are `queued`, `extracting`, `embedding`, `indexing`, `indexed`, `failed`. [`src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs`:3–15]

For human UX, retain the PRD’s explicit mapping:

- PRD `pending` ≙ contract `Queued`;
- PRD `projecting` ≙ contract `Indexing`;
- the other four names correspond directly.

Do not expose a third human lifecycle vocabulary merely because another diagnostic enum exists.

### Evidence Packet diagnostic ingestion stage

`EvidencePacketIngestionStage` currently has:

`unknown`, `received`, `fetching`, `extracting`, `chunking`, `embedding`, `indexing`, `completed`, `failed`, `retrying`.

This is described in code as a stable source-metadata taxonomy, but it is not the same enum as `MemoryUnitStatus` and contains values the PRD expressly treats as details rather than product states. Moreover, current search mapping does not populate it. It is safe to name as a protocol field only; it is not safe to promote into the UX lifecycle without reconciliation. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketIngestionStage.cs`:10–43; `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:72–83, 141–152]

### Freshness

`EvidencePacketFreshnessState` currently has:

`unknown`, `current`, `stale`, `expired`, `pending`.

The PRD contract instead requires `current` / `aging` / `stale` / `unknown`. There is no current `aging` enum value, while implementation has `expired` and `pending`. Use PRD terms in the UX spine and record the code enum as an implementation divergence requiring contract reconciliation. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketFreshnessState.cs`:10–28]

## Current CLI registration and exact grammar

The root describes itself as “preview.” All global options are recursive and therefore available on subcommands:

- `--endpoint <uri>`
- `--token <value>`
- `--verbose`
- `--format human|json|table`
- `--telemetry`

Source: [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:19–25, 94–104, 156–157; `src/Hexalith.Memories.Cli/Commands/CliGlobalOptions.cs`:10–46].

### Implemented command tree

| Exact grammar | Registration/source status |
|---|---|
| `memories tenant list` | Real command. [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:105–109; `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs`:17–42] |
| `memories config show` | Real command. [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:111–115; `src/Hexalith.Memories.Cli/Commands/ConfigShowCommand.cs`:35–44] |
| `memories search query --tenant <id> [--case <id>] [--query <text>] [--axis syntactic|semantic|nl|graph|hybrid] [--max-results <n>] [--explain]` | Real command; axis default `hybrid`, max-results default 10 and accepted range 1..1000; query required except for graph. There is no `--from` and no CLI `--token-budget`. [`src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs`:20–27, 59–115, 131–176] |
| `memories search inspect --tenant <id> --case <id> --id <memoryUnitId>` | Real command. [`src/Hexalith.Memories.Cli/Commands/SearchInspectCommand.cs`:18–54] |
| `memories search lookup --tenant <id> --case <id> --source-uri <uri>` | Real command. [`src/Hexalith.Memories.Cli/Commands/SearchLookupCommand.cs`:35–65] |
| `memories quickstart [--tenant <id>] [--skip-boot-check] [--skip-prereq-check] [--dry-run] [--tenant-timeout-seconds <positive-int>]` | Real command. [`src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs`:68–145] |
| `memories status telemetry --tenant <id>` | Real command; no `status --case` or `status --failed` registration exists. [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:128–132; `src/Hexalith.Memories.Cli/Commands/StatusTelemetryCommand.cs`:17–59] |
| `memories consistency verify --tenant <id> [--batch-size <10..5000>] [--wait]` | Real command. [`src/Hexalith.Memories.Cli/Commands/ConsistencyVerifyCommand.cs`:35–81] |
| `memories consistency inspect --tenant <id> --id <memoryUnitId>` | Real command. [`src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs`:30–64] |
| `memories consistency repair --tenant <id> [--batch-size <10..5000>] [--include-unrepairable] [--wait] [--yes]` | Real command. [`src/Hexalith.Memories.Cli/Commands/ConsistencyRepairCommand.cs`:39–89] |
| `memories export case --tenant <id> --case <id> [--output <path>] [--force] [--allow-absolute-path]` | Real command. [`src/Hexalith.Memories.Cli/Commands/ExportCaseCommand.cs`:40–90] |
| `memories export tenant --tenant <id> [--output <path>] [--force] [--allow-absolute-path]` | Real command. [`src/Hexalith.Memories.Cli/Commands/ExportTenantCommand.cs`:37–80] |
| `memories handlers list` | Real command. [`src/Hexalith.Memories.Cli/Commands/HandlersListCommand.cs`:20–46] |
| `memories handlers mismatches --tenant <id> [--severity info|warning] [--only-warning] [--exclude-stale]` | Real command; JSON output intentionally remains unfiltered. [`src/Hexalith.Memories.Cli/Commands/HandlersMismatchesCommand.cs`:23–85, 100–130] |

Root grouping and registration: [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:105–154].

### Actual `NotImplementedCommand` registrations

Only four top-level groups are currently created by `NotImplementedCommand.Create`:

- `memories ingest`
- `memories traverse`
- `memories case`
- `memories explore`

All four point to Story 7.2 in `CommandGroups`. Invoking one prints `Not yet implemented — tracked in Story 7.2.` to stderr and returns `CliExitCodes.Plumbing` (2). [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:81–88, 159–163; `src/Hexalith.Memories.Cli/Commands/NotImplementedCommand.cs`:14–44; `src/Hexalith.Memories.Cli/Execution/CliExitCodes.cs`:12–25]

Current CLI exit codes are `Success=0`, `DomainError=1`, `Plumbing=2`, `NotFound=4`, and `Cancelled=130`. [`src/Hexalith.Memories.Cli/Execution/CliExitCodes.cs`:8–35]

The following PRD target verbs are **absent from the current command tree**, not individually registered stubs:

- `tenant create`, `tenant delete`, `tenant verify`;
- `status --case` / `status --failed`;
- all `case` subcommands, because the entire top-level `case` group is one placeholder;
- full `ingest` and `traverse` grammars, because each is one placeholder with no options/subcommands.

Evidence: [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:81–88, 105–163].

### CLI JSON/error contract

- `CliOutputEnvelope<T>` schema version is 1. Success emits `{ schemaVersion, command, data }`; error emits `{ schemaVersion, command, error }`. Exactly one of `data` or `error` is populated. Search-query errors may append `evidencePacket`; search success carries it inside `data`. [`src/Hexalith.Memories.Cli/Output/Json/CliOutputEnvelope.cs`:10–38, 41–86, 101–114]
- `CliErrorPayload(Code, Message, Suggestion)` intentionally mirrors `Contracts.V1.ErrorResponse` in shape but may contain CLI-translated wording. [`src/Hexalith.Memories.Cli/Output/Json/CliErrorPayload.cs`:8–17]
- JSON-mode errors are written to stdout. [`src/Hexalith.Memories.Cli/Output/Formatters/JsonErrorEnvelopeWriter.cs`:14–23, 35–49]

## Confirmed divergences from the PRD extraction

The UX spines should cite the PRD for intended phase/scope and use these findings only to describe current delivery accurately.

1. **MCP graph axis:** PRD narratives describe `search_memory` as having graph/hybrid control. Current `SearchAxis` has no `Graph` value and directs callers to `traverse_relations`. Hybrid results can still carry graph contributions. [`src/Hexalith.Memories.Mcp/Tools/SearchAxis.cs`:12–27; `src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`:54–70]
2. **MCP ingest breadth:** the schema advertises `File`, `Url`, and `Event`, but current validation accepts only `File`. URL/event return `UNSUPPORTED_SOURCE_TYPE`. [`src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`:55–72, 99–115, 143–154]
3. **Ingest payload semantics:** documentation says binary may be base64, but the implementation UTF-8 encodes the literal input string; it does not base64-decode it. [`src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`:62–63, 116–132]
4. **Provenance:** MCP exposes caller-controlled `ingestedBy` and forwards it unchanged; the REST endpoint schedules the supplied `IngestionInput.IngestedBy` unchanged. This does not implement the PRD requirement that authenticated external provenance bind to normalized `sub` and caller-supplied provenance be rejected/ignored. Do not bless the current MCP parameter as the desired UX. [`src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`:70–71, 118–132; `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs`:56–75, 92–142]
5. **Evidence Packet coverage:** only `search_memory` success attaches an Evidence Packet. `ingest_content` returns `workflowInstanceId`, `traverse_relations` returns `TraversalResult`, and `get_case_info` returns `Case`. Errors across tools attach a packet. The PRD’s cross-surface envelope aspiration is therefore only partially realized. [`src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`:102–139; `src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`:116–140; `src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs`:108–123; `src/Hexalith.Memories.Mcp/Tools/GetCaseInfoTool.cs`:74–79; `src/Hexalith.Memories.Mcp/McpErrorMapper.cs`:168–185]
6. **Evidence metadata population:** `freshness`, per-source `ingestion`, and `mcpSchema` types exist but current production search mapping leaves them null. Do not show them as guaranteed live MCP data. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:72–124, 141–192]
7. **Freshness vocabulary:** code uses `unknown/current/stale/expired/pending`; PRD requires `current/aging/stale/unknown`. Defer human UX terms to the PRD and flag contract reconciliation. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketFreshnessState.cs`:10–28]
8. **Ingestion vocabulary:** `EvidencePacketIngestionStage` introduces `received/fetching/chunking/completed/retrying` beyond `MemoryUnitStatus` and PRD’s one state vocabulary. Keep these as diagnostic details, not top-level lifecycle states. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketIngestionStage.cs`:10–43; `src/Hexalith.Memories.Contracts/V1/MemoryUnitStatus.cs`:5–15]
9. **Single-axis score semantics:** the mapper treats syntactic/graph single-axis scores as raw/unbounded and emits `evidenceStrength=unknown`; this differs from the PRD’s stated normalized single-axis score semantics. Do not derive confidence labels from raw axis values in UX. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:87–90, 587–592]
10. **NL Evidence Packet gap:** `FusedScoredResult` exposes `nlScore`, but `BuildHybridAxisEvidence` and fallback `InferHybridAxes` inspect only syntactic, semantic, and graph scores. NL contribution may therefore be absent/null in packet `axisEvidence`. [`src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`:85–95; `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:497–510, 555–576]
11. **Graph summary gap:** search packet `graph` is always an unavailable/empty summary even when a hybrid result has graph contributions. Use `traverse_relations` for current graph detail rather than implying `EvidencePacketGraphSummary` is populated. [`src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`:109–124, 177–192]
12. **Case info description:** the tool promises member count/recent activity, but its actual `Case` payload lacks member count and exposes only `lastUpdated`. [`src/Hexalith.Memories.Mcp/Tools/GetCaseInfoTool.cs`:41–48, 74–77; `src/Hexalith.Memories.Contracts/V1/Case.cs`:10–20]
13. **CLI stub classification:** PRD groups target verbs as `NotImplementedCommand` stubs; current code has only four top-level placeholders. Tenant create/delete/verify and status case/failed are absent registrations. [`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:81–88, 105–163]
14. **Search start-node boundary:** current search grammar has no `--from`, matching the pinned PRD rule that public search auto-seeds graph work and explicit starts belong to traversal. [`src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs`:59–115]
15. **CLI empty-state copy:** current search copy advertises the stub `memories ingest` and a DAPR subscription; the PRD says Event auto-indexing should not appear in the Phase 1 hint until Phase 1.5 ships/launches. Treat this as current copy debt, not a migrated wording source. [`src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs`:29–48]

## Migration usage rule

Safe to reference directly in `EXPERIENCE.md`:

- exact four MCP tool names and the input/wire fields above;
- `EvidencePacket` and its public nested record/enum field names;
- `ErrorResponse { code, message, suggestion }`;
- `MemoryUnitStatus` machine values with the PRD’s human mapping;
- exact implemented CLI grammar and the four actual top-level stub names;
- fail-closed tenant authorization and recovery semantics.

Reference with an explicit “current implementation” qualifier:

- evidence-strength thresholds;
- `EvidencePacketFreshnessState` and `EvidencePacketIngestionStage` values;
- current per-tool Evidence Packet coverage;
- `get_case_info` response breadth;
- MCP `ingestedBy`.

Do not elevate private/internal type names such as `McpErrorPayload` or `IngestContentResponse` into the product contract. Use their confirmed wire fields instead. Continue to defer phase, launch, human-facing ingestion vocabulary, and freshness vocabulary decisions to `prd.md`.
