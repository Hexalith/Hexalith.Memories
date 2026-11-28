---
title: 'Select previously untagged integration cases in the existing fast lane'
type: 'bugfix'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: '0124b494967373f8458620d904940bc0a7ccdf1a'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-731 records that the current hosted integration-fast filter omits eight product test classes, including the source-inventory producer cases. Enroll the whole recorded group with the existing Category=Integration contract, preserve the fast/slow split, and prove the exact selected cases execute.

## Boundaries & Constraints

Use the existing local filter and coverage catalog. No hosted workflow, CI/CD-owned configuration or submodule changes. Investigate the historically flaky producer case on current source, and fix a reproduced in-scope failure if needed. Use run-with-temp for scratch workspaces. Build in conjunction with the parent if needed to avoid concurrent .NET writes. Do not commit or edit the ledger/spec. The parent owns Git.

</frozen-after-approval>

## Code Map

The DW-731 ledger entry names eight test classes. CONTRIBUTING.md documents the fast filter. tools/test.sh and tools/integration-fast-required-surfaces.txt own local lane selection and evidence. Search current code before assuming the old inventory guard still exists. Related flat producer proof is covered by this broader entry.

## Tasks & Acceptance

- [x] Implement the scoped behavior and the focused regression evidence.

Given the current hosted fast filter, all eight previously untagged classes are discovered and their tests pass without narrowing selection. Given a missing predecessor proof, the fast coverage verifier rejects results. The previously flaky foreign-owner predecessor case also passes in focused verification.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Use exact xUnit discovery identities or per-case TRX data and compare expected executions. Check the producer and coverage regression. Coordinate any container-backed run with the parent and provide exact pass counts and logs.

The Release build produced zero warnings and errors. The hosted filter discovered 735 distinct case identities and all matched the runner TRX. The run passed 731 tests, including all 145 previously omitted cases, with four existing skips. Six focused real predecessor cases also passed, including the historical foreign-owner case. The catalog rejected authentic TRX evidence with either required predecessor family removed. All ten catalog tests passed.

## File List

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/InventoryWirePaginationIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/MemoryErasureProcessProtocolTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/ReferencedSourceProducerIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/SourceInventoryProducerIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/TenantIngestionLifecycleIntegrationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/UnitOwnershipAuthorityIntegrationTests.cs`
- `tests/tooling/integration_fast_coverage/integration_fast_coverage_test.py`
- `tools/integration-fast-required-surfaces.txt`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `448ac4b8b6a51de160d25d6d3b5416e926828283` | 731 fast passes, 4 existing skips, 10 verifier checks | Matched 10/10 |
| 2026-11-28 | code-review | `448ac4b8b6a51de160d25d6d3b5416e926828283` | 731 fast passes, 4 existing skips, 10 verifier checks | Matched 10/10 |
