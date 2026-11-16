---
title: 'Record the approved erasure process qualification scope'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
baseline_commit: '7a5c892508700b2e5c71c921cdaeedf68de5f03d'
---

## Intent

This owning-repository scope record supports the user-approved Parties build spec
`spec-five-process-erasure-boundary-deferrals.md` (approved 2026-11-16).
The approved intent adds private process-replacement qualification for historical
chunk/NL production, workflow purge and admission, legacy NL migration, and retry
erasure. The Parties repository owns the full plan, review triage and retained
verification packet. Its frozen intent SHA-256 is
`e51ca85c3526fb0f8d7b6deb5d3d97ed16c66e9edd6fc4802f30e627fed1d62b`.

The user explicitly approved the owner test changes and main publication. The
allow-list below deliberately includes the affected C# test files under the
repository's forbidden-default test area. Production source, public contracts,
dependency pins and nested submodules are outside this implementation scope.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/DaprStateSidecarCollection.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/DaprStateSidecarFixture.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/FalkorDbCollection.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/FalkorDbFixture.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/MemoryErasureProcessMessage.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/MemoryErasureProcessProtocol.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/MemoryErasureProcessWorker.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackCollection.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/HistoricalProcessArtifactWorkflow.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/MemoryErasureProcessProtocolTests.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/MemoryErasureProcessRestartTests.cs` - owning test qualification or existing fixture parity
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/NaturalLanguageErasureIntegrationTests.cs` - owning test qualification or existing fixture parity
- `_bmad-output/implementation-artifacts/spec-five-process-erasure-boundaries.md` - owning scope and verification record

## Verification

Before review fixes, independent parent source builds passed all 100 existing
algorithm controls without exclusions and all 14 new process/protocol controls,
with zero failed/skipped/errors. All 20 observed worker births, eight sidecar exits
and both owned backing containers were independently closed. Independent three-layer review completed. Positive artifact assertions,
replacement-worker admission/scheduling, retained stale retry input and permanent
fence checks were corrected and passed their narrow process selectors. The
parent records final reconciled-source checks and publication in its packet.
The parent packet is `_bmad-output/implementation-artifacts/tests/five-process-erasure-boundaries-2026-11-16/`
in Hexalith.Parties; exact commands, source-hook bindings and artifact identities
are retained there. Historical failed attempts remain separate.

## Cross-Tenant Negative Evidence

**Not triggered:** this commit changes test-only fixtures and controls. It changes
no production tenant-isolation surface listed by the repository's maintained
surface inventory. Same-tenant survivors and separate modern progress remain
covered by the process lane; production historical inventory, writer exclusion,
package adoption, deployment and external owner acceptance remain open.
