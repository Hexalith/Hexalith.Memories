---
title: 'Verify the live MCP-to-server trace relationship'
type: 'chore'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: 'b7f49e3396b5008508a075e4edece5c2d6a41046'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-179 asks for a Tier-3 parent/child trace proof from the authenticated MCP request to the Memories Server. The old ledger notes say cross-process capture was missing. The current fixture can enable test-only activity breadcrumbs through the shared ServiceDefaults on both processes.

## Boundaries & Constraints

Use disposable Aspire topology and the existing authenticated MCP fixture. Reuse test-only breadcrumb capture or an existing equivalent. Keep production behavior and compatibility surface. Capture only the data necessary to attribute spans and avoid logging arbitrary URLs or secret values. Coordinate .NET/Docker verification with the parent. Use run-with-temp for scratch work. Do not edit the ledger/spec or commit.

</frozen-after-approval>

## Code Map

tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.Fast.cs and Telemetry/Infrastructure/ServerActivityStreamReader.cs own the existing CLI-to-server proof. AspireIngestionPipelineFixture exposes a log start index and sets InMemoryTelemetryEnvironment.EnvVar. Source Extensions.cs owns test-only ActivityBreadcrumbPrefix. McpServerIntegrationTests owns the existing authenticated tool client pattern.

## Tasks & Acceptance

- [x] Implement and verify the focused trace proof.

Given an authenticated MCP tool request to the live topology, the test proves its MCP span and Server request share the same trace and a captured causal parent chain. A test that matches only by timestamp or unrelated activity names is insufficient. Preserve the existing CLI trace tests.

## Implementation Notes

## Spec Change Log

## Review Triage Log

The review fixes verify each configured Dapr exporter role, keep request capture bounded, preserve fixture cleanup, and require the live trace identity in fast integration results. Negative tests verify the opt-in subscription and breadcrumb filter.

## Verification

Run the live focused test with actual Aspire/Dapr resources and confirm the exact relationships it observed. If a genuine prerequisite still blocks the proof, report it with the most focused run that demonstrates it.

Release builds of fast and nightly IntegrationTests passed. The focused fast run passed eleven cases. The existing nightly CLI-to-Redis trace passed. The live MCP call produced seven linked spans from the client tool through both Dapr sidecars to the server search. The fixture retained only span and parent IDs for one selected trace.

## File List

- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- `tests/Hexalith.Memories.Integration.NightlyTests/Hexalith.Memories.Integration.NightlyTests.csproj`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixtureTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/ScriptedHttpServer.cs`
- `tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.Trace.cs`
- `tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/DaprTraceCapture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/ServerActivityStreamReader.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/IntegrationActivityProcessorBreadcrumbFilterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/OpenTelemetryRegistrationTests.cs`
- `tests/tooling/integration_fast_coverage/integration_fast_coverage_test.py`
- `tools/integration-fast-required-surfaces.txt`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `f2dfda9d22203496c43ca0c1d0dad80e2d58f8e2` | 11 focused fast, 1 existing nightly trace, 1 final live trace, 13 follow-up .NET and 10 verifier cases | Matched 13/13 |
| 2026-11-28 | code-review | `ef169af067a241dde1591f23edfc5d278660b107` | 11 focused fast, 1 existing nightly trace, 1 final live trace, 13 follow-up .NET and 10 verifier cases | Matched 13/13 |
