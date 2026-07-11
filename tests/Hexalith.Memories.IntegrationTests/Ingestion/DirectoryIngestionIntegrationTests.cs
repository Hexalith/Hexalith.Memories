// <copyright file="DirectoryIngestionIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class DirectoryIngestionIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public DirectoryIngestionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [RunnableSkippedFact("Requires Aspire AppHost fixture — unskip with Story 6.3 retry harness OR Epic 7 e2e harness")]
    public void DirectoryIngestion_MixedFiles_ShouldIndexSupportedAndSkipUnsupported()
    {
        // Scenario (Story 6.1 AC5, AC6):
        //   1. Create a temp directory with 5 supported files (.md, .pdf, .txt) + 2 unsupported (.exe, .iso).
        //   2. Inject the temp directory into Ingestion:AllowedDirectoryRoots via the Aspire fixture.
        //   3. POST /api/v1/ingest/directory; assert 202 with 5 enqueued, 2 skipped.
        //   4. Poll GET /api/v1/ingest/batches/{batchId} until all instances terminal.
        //   5. Assert 5 indexed.
        _ = _fixture;
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture — unskip with Story 6.3 retry harness")]
    public void DirectoryIngestion_CrossTenantIsolation_ShouldNotSerialize()
    {
        // Scenario (Story 6.1 AC11):
        //   Schedule a 100-file batch for t1, simultaneously schedule a single-file ingest for t2,
        //   assert t2 latency stays within 2× single-tenant baseline. Coarse assertion; true load
        //   isolation / chaos tests are deferred to Phase 2.
        _ = _fixture;
    }
}
