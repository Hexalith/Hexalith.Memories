# Sprint Change Proposal: CI Integration Fast Stabilization

Date: 2026-07-05

Source run: https://github.com/Hexalith/Hexalith.Memories/actions/runs/28741906233/job/85226065386

## Summary

Apply a direct minor adjustment to stabilize the fast integration CI lane. The failing surface was not a feature-scope change; it was a set of integration drift issues around local bearer authentication, tenant provisioning timing, fast Aspire fixture dependencies, paged tenant listing, and chunked semantic index expectations.

## Classification

Change type: direct adjustment

Impact level: minor

Rationale: the fixes preserve the existing product behavior and test intent while aligning tests and fixture wiring with the current server contracts.

## Adjustments

- Replaced local Dapr API token behavior in the REST auth handler with bearer authorization for HTTPS and HTTP loopback calls.
- Added quickstart tenant provisioning timeout control and passed the longer Aspire activation budget from live integration tests.
- Let the fast Aspire fixture use an in-memory command store so the integration lane no longer depends on an undeclared EventStore resource.
- Minted tenant-scoped server bearer tokens for CLI and direct REST integration calls that exercise protected endpoints.
- Preserved explicit tenant provisioning vector dimensions so the Ollama 2560-dimension path provisions matching semantic indexes.
- Updated fake Ollama embedding responses to handle batch input and return one embedding per input.
- Updated semantic projection and deletion paths to account for chunked semantic keys in addition to legacy base semantic hashes.
- Stabilized case and search assertions that read asynchronous projections after deletes.
- Made the REST tenant list client follow paged server responses instead of only reading the first page.
- Removed fixed-id coupling from the CLI nonexistent-tenant error envelope test.
- Aligned focused search, migration, syntactic, graph-scoped, telemetry, and case tests with current backend contracts.

## Verification

- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --filter "FullyQualifiedName~MemoriesClientTests.ListTenantsAsync"`
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~CaseEndpointIntegrationTests.DeleteMemoryUnit_Roundtrip_ShouldReturn204AndRemoveFromCase|FullyQualifiedName~CliTenantListIntegrationTests.ListTenantsAsync_AfterProvisioning_ReturnsCreatedTenant"`
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~CliErrorMessagesIntegrationTests.SearchInspect_NonexistentTenantValidFormat_EmitsCliJsonErrorEnvelope"`
- `bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --results-directory TestResults/integration-fast-fixed-3`

Final fast integration result: 233 passed, 0 failed, 0 skipped.

## Residual Risk

The Git worktree already contained unrelated `references/` submodule pointer changes. They are outside this corrective change and were left untouched.
