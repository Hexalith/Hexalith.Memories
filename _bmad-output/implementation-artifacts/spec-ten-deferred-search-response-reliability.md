---
title: 'Resolve ten deferred search response findings'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make search requests, results and diagnostics reliable across REST, CLI and MCP.

- Given a successful HTTP search, when either response envelope is read, then it reports its normalized offset and maximum page size. Clients may read old payloads and request subsequent hybrid pages (DW-230).
- Given an endpoint with a path prefix, when either REST search method runs, then the request remains beneath that prefix with or without a trailing slash (DW-254).
- Given undefined numeric source types or non-integer CLI maximum results, when search validates its input, then it returns the existing structured validation result and avoids retrieval (DW-255, DW-529).
- Given search explanation or inspected metadata dictionaries, when CLI human and table formatters list keys, then their relevant key order is ordinal. Given a non-finite score or confidence, CLI rejects the payload before writing results in any format (DW-250, DW-251).
- Given an MCP search error with a well-formed tenant and accepted scope identifiers, then the packet retains known scope without claiming isolation. Unknown and malformed inputs remain suppressed. Shared constants define the existing permissions context vocabulary (DW-428, DW-436).
- Given an MCP error, then its envelope serializes using generated metadata. An unexpected tool failure records the tool and exception type in server logs without including caller values or raw exception text (DW-439, DW-538).

</frozen-after-approval>

## Implementation Notes

- Baseline `0205d8e714b74e4aa1d0362e33c9b22eedf91f2e` has 289 open ledger entries. Scope covers the request-to-response search flow and shared MCP diagnostics. The server already has a normalized effective window at all successful returns. REST search uses one common path builder. The CLI executor handles failure before successful output. MCP authorization and exception handling are centralized in the tool executor.
- Pagination fields are additive. Keep the existing hybrid request positional constructor. Preserve current scope strings and their distinct meanings. Keep operation cancellations out of general failure logging.


- The HTTP response reports the normalized request window even when publication filtering, truncation, or an empty page reduces the returned rows. The existing hybrid fusion pipeline recomputes candidates for each window. The guide documents the resulting ranking and count behavior.
- The REST client preserves search endpoint prefixes and checks ranking scores, explanation weights, packet scores, and metadata confidence before returning them to CLI or MCP callers. The CLI uses the existing structured input and response errors.
- MCP uses its established tenant identifier syntax to retain a requested identifier with unknown isolation. The executor logs the exception type, HRESULT, and registered tool name. Genuine HTTP timeouts map to NETWORK_ERROR when the caller remains active. Other operation cancellation continues to propagate.
- The pre-change AppHost started with run-with-temp aspire start --non-interactive --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj. Aspire describe reported OpenBao exited with code 1 and Keycloak unhealthy. The AppHost was stopped. Validation used focused project test suites.

- Debug builds used source project references and disposable artifacts. Direct xUnit execution passed 476 CLI, 130 MCP, 613 Contracts, and 132 focused Server tests. The Server selectors covered search endpoints, syntactic search, hybrid fusion, search metadata, public contract serialization, and endpoint authorization.
- The BMAD dry run reported 279 open numbered records after ten closures. The review follow-up is appended in the attended workflow format.

## Cross-Tenant Negative Evidence

**Surfaces:** Authenticated REST search tenant routing and MCP tenant authorization before search dispatch.

**Tests:** `ServerEndpointAuthorizationTests.SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`, `ServerEndpointAuthorizationTests.SearchEndpoint_WithMalformedTenant_ReturnsTenantForbiddenBeforeSearchDependencies`, `SearchMemoryToolTests.DisallowedTenant_ReturnsAuthorizationErrorWithoutCallingClient`, and `McpToolExecutorTests.RunAsync_DenialPreventsOperation`.

**Command:** `dotnet exec "$TMPDIR/artifacts/bin/Hexalith.Memories.Mcp.Tests/debug/Hexalith.Memories.Mcp.Tests.dll"` and `dotnet exec "$TMPDIR/artifacts/bin/Hexalith.Memories.Server.Tests/debug/Hexalith.Memories.Server.Tests.dll" -class '*SearchEndpointContractTests' -class '*SyntacticSearchServiceTests' -class '*HybridSearchServiceTests' -class '*SearchResponseMetadataApplierTests' -class '*PublicContractSerializationCoverageTests' -class '*ServerEndpointAuthorizationTests'`.

**Result:** All nine named negative cases passed within the two test lanes. Each of the five HTTP search axes rejected the mismatched tenant before Dapr, actor, Redis, or FalkorDB calls. Malformed HTTP and MCP tenant IDs and MCP claim mismatches were rejected before downstream calls.

## Review Triage Log

- Medium, deferred. The existing hybrid service sizes each axis candidate set from offset plus maxResults. Changing the window can change result positions without a corpus change. The contract guide describes this behavior and the ledger tracks stable hybrid pagination separately.
- Medium, patched. Validate source type before case lookup. The endpoint regression uses a missing case and asserts that neither case lookup nor retrieval runs.
- Medium, patched. Reject comma lists for the single-valued source type. Defined numeric values and names normalize to their canonical filter text.
- Medium, patched. Preserve the accepted case identifier syntax without an added length restriction. The MCP test covers a 129-character identifier.
- Medium, patched. Malformed MCP case identifiers fail before invoking the REST client, which also avoids upstream error prose that could echo the raw case.
- False. The tenant scope records the requested identifier under the established MCP syntax and uses unknown isolation. A REST tenant ID rejection remains a structured upstream error and the packet makes no existence claim.
- Medium, patched. Move validation and authorization inside the executor's failure boundary. Tests verify sanitized logging and an error result when validation throws.
- Medium, patched. Distinguish HTTP deadlines from operation cancellation using the timeout cause. The REST timeout test verifies the cause and MCP tests verify mapping and propagation.
- Medium, patched. Check both optional packet source scores and per-axis evidence scores alongside the retrieval rows. An overflow fixture verifies the nested axis rejection.
- Low, patched. Formatter regressions compare mixed-case strings that distinguish ordinal from culture-sensitive ordering.
- Low, patched. The client XML summary refers to the renamed request path parameter.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-search-response-reliability.md`
- `docs/dev/evidence-packet.md`
- `src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitHumanFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitTableFormatter.cs`
- `src/Hexalith.Memories.Cli/Output/Formatters/SearchResultHumanFormatter.cs`
- `src/Hexalith.Memories.Client.Rest/HybridSearchRequest.cs`
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`
- `src/Hexalith.Memories.Contracts/V1/EvidencePacketPermissionsContext.cs`
- `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`
- `src/Hexalith.Memories.Contracts/V1/SearchResult.cs`
- `src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs`
- `src/Hexalith.Memories.Mcp/McpErrorJsonContext.cs`
- `src/Hexalith.Memories.Mcp/McpErrorMapper.cs`
- `src/Hexalith.Memories.Mcp/McpErrorPayload.cs`
- `src/Hexalith.Memories.Mcp/McpToolExecutor.cs`
- `src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`
- `src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/MemoryUnitFormatterTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/SearchQueryCommandTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/SearchResultFormatterTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSearchTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSendDecodeTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpToolExecutorTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpToolTestFactory.cs`
- `tests/Hexalith.Memories.Mcp.Tests/SearchMemoryToolTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/ToolFailureTestLogger.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
