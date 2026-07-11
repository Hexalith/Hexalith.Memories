---
title: 'Story 25.6: MCP Tool Executor'
type: 'refactor'
created: '2026-07-11'
status: 'done'
baseline_revision: '829b585f8398af23d1ad1b97639347d0a0af1c86'
final_revision: '98482393580e847f06cca1d5484435b08ba427db'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06-cross-tenant-negative-evidence.md'
warnings: ['oversized']
---

<intent-contract>

## Intent

**Problem:** The four MCP tools duplicate validation, tenant authorization, client execution, and exception mapping. Each currently discards the authorization filter's approved tenant snapshot and re-reads it through a second request-item seam, so a future tool could omit or drift from tenant scoping.

**Approach:** Introduce a shared `McpToolExecutor.RunAsync(...)` that orders tool-specific validation, performs one tool-boundary authorization decision, passes the approved tenant snapshot to the operation, and centrally maps execution failures. Route every registered tool through it and remove the redundant authorized-tenant accessor.

## Boundaries & Constraints

**Always:** Preserve validation-before-authorization ordering; use exact ordinal tenant-claim matching; pass only the filter-returned tenant snapshot downstream; rethrow cancellation; preserve structured/text error payloads, sanitization, evidence packets, tool schemas, clamps, token-budget normalization, and cross-tenant denial before any `MemoriesClient` call. Keep the separate bearer-token authorization enforced by the upstream Memories Server.

**Block If:** Implementation requires changing a public MCP tool name, description, parameter schema, success/error wire shape, claim contract, or upstream server authorization; or a real consumer of `IAuthorizedTenantAccessor` exists outside the four tools and their tests.

**Never:** Trust the caller tenant after authorization; store/re-read the approved tenant through `HttpContext.Items`; weaken malformed/mismatched-tenant handling; expose exception details; absorb `OperationCanceledException`; alter search/traversal budgets or limits; or remove the DAPR-hop server authorization defense.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Valid call | Valid tool input and exact tenant claim | Validate, authorize once, invoke once with the approved tenant, retain existing result shape | No error expected |
| Invalid tool input | Missing/unsupported tool-specific value | Return the existing validation result before authorization or client execution | Existing validation code/message/suggestion |
| Tenant denied | Malformed, missing, or mismatched tenant claim | Do not invoke the operation or client | Existing opaque structured authorization result; malformed input is not echoed |
| Remote failure | `MemoriesRemoteException` from the operation | Preserve remote structured error and evidence mapping | `McpErrorMapper.Map` |
| Unexpected failure | Non-cancellation exception from the operation | Return sanitized structured/text error | `McpErrorMapper.MapGeneric` |
| Cancellation | Operation throws `OperationCanceledException` | Propagate cancellation unchanged | Rethrow |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Mcp/Tools/{SearchMemoryTool,IngestContentTool,TraverseRelationsTool,GetCaseInfoTool}.cs` -- four registered tools with duplicated validation/authorization/catch flow.
- `src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs` -- canonical exact-match tenant decision already returns the approved snapshot.
- `src/Hexalith.Memories.Mcp/Authentication/AuthorizedTenantAccessor.cs` -- redundant `HttpContext.Items` snapshot seam used only by tools/tests.
- `src/Hexalith.Memories.Mcp/{McpErrorMapper,McpCompositionRoot}.cs` -- stable error mapping and scoped composition boundary.
- `tests/Hexalith.Memories.Mcp.Tests/` -- tool schemas, routing, budgets, evidence, auth, and error contracts.
- `tests/Hexalith.Memories.IntegrationTests/Mcp/McpAuthenticationIntegrationTests.cs` -- live matching/cross-tenant MCP boundary evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Memories.Mcp/McpToolExecutor.cs` -- add the scoped executor; invoke tool validation before one filter call, pass its returned tenant snapshot to the async operation, centralize remote/generic mapping, and rethrow cancellation.
- `src/Hexalith.Memories.Mcp/Tools/{SearchMemoryTool,IngestContentTool,TraverseRelationsTool,GetCaseInfoTool}.cs` -- depend on the executor and express only tool-specific validation, normalization, client calls, success serialization, and evidence mapping; preserve attributes/signatures.
- `src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs` -- retain exact-match decision/logging and returned snapshot while removing request-item storage and stale-item behavior.
- `src/Hexalith.Memories.Mcp/Authentication/AuthorizedTenantAccessor.cs` -- delete the superseded interface/implementation.
- `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs` -- register the scoped executor and remove accessor registration without changing inbound or upstream authorization.
- `tests/Hexalith.Memories.Mcp.Tests/McpToolTestFactory.cs` -- construct the shared executor/auth context for focused tool tests.
- `tests/Hexalith.Memories.Mcp.Tests/TenantClaimAuthorizationTests.cs` -- replace accessor/item assertions with approved-snapshot and denial assertions.
- `tests/Hexalith.Memories.Mcp.Tests/McpToolExecutorTests.cs` -- cover ordering, exact approved snapshot, denial-before-operation, remote/generic mapping, and cancellation propagation.
- `tests/Hexalith.Memories.Mcp.Tests/{SearchMemoryTool,IngestContentTool,TraverseRelationsTool,GetCaseInfoTool}Tests.cs` -- update construction and add mismatched-tenant negative cases proving no client call for every tool while retaining routing, clamp, budget, error, and evidence assertions.
- `tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs` -- update DI and add a conformance guard that every registered MCP tool is wired through `McpToolExecutor`; retain exact schema assertions.

**Acceptance Criteria:**
- Given any registered MCP tool, when its implementation is inspected by the conformance test, then it depends on the shared executor and cannot use the removed accessor seam.
- Given a caller requests a tenant absent from its claims, when any of the four tools runs, then the tool returns the established authorization envelope before any client dependency is invoked.
- Given valid inputs and an exact tenant claim, when a tool executes, then the client receives the filter-approved tenant exactly once and existing success text, structured content, evidence scope, budgets, and tool schemas remain unchanged.
- Given an authorized tool operation raises a remote, unexpected, or cancellation failure, when the tool call completes or is cancelled, then its observable result keeps structured remote errors, sanitized unexpected errors, or propagated cancellation respectively.

## Spec Change Log

## Review Triage Log

### 2026-07-11 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 3: (high 0, medium 3, low 0)
- defer: 1: (high 0, medium 1, low 0)
- reject: 13: (high 0, medium 2, low 11)
- addressed_findings:
  - `[medium]` `[patch]` Added an executor test that proves the caller cancellation token is forwarded to the authorized operation, distinct from cancellation-exception propagation.
  - `[medium]` `[patch]` Made a non-error validation callback result fail closed as a sanitized internal error before authorization or operation execution.
  - `[medium]` `[patch]` Replaced raw-byte IL scanning with instruction-boundary-aware decoding so operand bytes cannot false-satisfy the shared-executor conformance guard.

## Design Notes

`RunAsync` should accept the requested tenant, tool name, a tool-specific validation callback, an authorized async operation, and the cancellation token. The executor owns callback ordering and exception policy; the operation receives the immutable approved tenant so tool code never re-reads caller or ambient request state. The upstream token minted in `McpCompositionRoot` remains an independent cross-hop defense and is not part of this redundancy cleanup.

Audit anchor A39 was re-verified on 2026-07-11. The four tools still repeat the flow; the prior “double authorization” shorthand is specifically one authorization decision plus a redundant accessor read/failure seam.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --configuration Release -m:1 /nr:false` -- expected: succeeds with zero warnings/errors.
- `dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Release/net10.0/Hexalith.Memories.Mcp.Tests.dll -parallel none -noLogo` -- expected: complete MCP unit/contract assembly passes, including all-four cross-tenant denial and executor conformance.
- `dotnet build Hexalith.Memories.slnx --configuration Release -m:1 /nr:false` -- expected: succeeds with zero warnings/errors.
- `git diff --check -- src/Hexalith.Memories.Mcp tests/Hexalith.Memories.Mcp.Tests _bmad-output/implementation-artifacts/spec-25-6-mcp-tool-executor.md` -- expected: no whitespace errors.

## Auto Run Result

Status: done

Summary: Added a shared MCP tool executor that owns tool-specific validation ordering, one exact-match tenant authorization decision, authorized-tenant snapshot propagation, and centralized execution error handling. All four MCP tools now use the executor; the redundant `HttpContext.Items` accessor seam is removed, public tool contracts are preserved, and all-four cross-tenant denial evidence is attached to the change.

Files changed:
- `src/Hexalith.Memories.Mcp/McpToolExecutor.cs` -- adds the shared validation, authorization, operation, cancellation, and error-mapping policy with fail-closed validation callback handling.
- `src/Hexalith.Memories.Mcp/Authentication/AuthorizedTenantAccessor.cs` -- removes the obsolete ambient authorized-tenant accessor.
- `src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs` -- retains exact ordinal claim authorization and its returned snapshot without request-item storage.
- `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs` -- registers the scoped executor and removes accessor registration while preserving upstream bearer authorization.
- `src/Hexalith.Memories.Mcp/Tools/{SearchMemoryTool,IngestContentTool,TraverseRelationsTool,GetCaseInfoTool}.cs` -- routes every registered tool through the executor while retaining schemas, normalization, budgets, evidence mapping, and success serialization.
- `tests/Hexalith.Memories.Mcp.Tests/McpToolTestFactory.cs` -- builds focused executor/auth contexts for tool tests.
- `tests/Hexalith.Memories.Mcp.Tests/TenantClaimAuthorizationTests.cs` -- pins returned snapshots, absence of ambient storage, ordinal matching, and denial behavior.
- `tests/Hexalith.Memories.Mcp.Tests/McpToolExecutorTests.cs` -- covers validation ordering, fail-closed callback results, snapshot/token propagation, denial, remote/generic errors, and cancellation.
- `tests/Hexalith.Memories.Mcp.Tests/{SearchMemoryTool,IngestContentTool,TraverseRelationsTool,GetCaseInfoTool}Tests.cs` -- updates construction and proves mismatched tenants cannot invoke any client path.
- `tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs` -- preserves exact schemas and adds instruction-aware shared-executor conformance checks.
- `tests/Hexalith.Memories.Mcp.Tests/EvidencePacketMcpParityTests.cs` -- updates construction while retaining MCP evidence-packet parity coverage.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- records the pre-existing missing-log mismatch surfaced during review.
- `_bmad-output/implementation-artifacts/spec-25-6-mcp-tool-executor.md` -- records the implementation contract, review triage, verification, and result.

Review findings breakdown: patched 3 medium findings, deferred 1 medium pre-existing logging issue, and rejected 13 findings (2 medium, 11 low). Follow-up review recommended: true because the final review added a fail-closed execution guard, cancellation-token proof, and nontrivial instruction-aware conformance logic.

Verification performed:
- MCP test project Release build passed with 0 warnings and 0 errors.
- Complete MCP unit/contract assembly passed 102/102 tests with 0 skipped.
- Matrix audit passed for valid, invalid, denied, remote, unexpected, and cancellation scenarios; all covering tests executed.
- Full `Hexalith.Memories.slnx` Release build passed with 0 warnings and 0 errors after review patches.
- Scoped `git diff --check` passed.

Residual risks: The Docker/DAPR-gated live MCP integration suite was compiled through the solution build but not executed. Existing live MCP authentication integration coverage remains unchanged; this run adds focused all-four denial-before-client proof in the non-Docker MCP test assembly.
