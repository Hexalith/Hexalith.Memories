// <copyright file="UrlIngestionIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class UrlIngestionIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public UrlIngestionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact(Skip = "Requires Aspire AppHost fixture + scripted local HTTP server — unskip with Story 6.3 retry harness OR Epic 7 e2e harness")]
    public void UrlIngestion_SmallTextPage_ShouldCompleteAndBeSearchable()
    {
        // Scenario (Story 6.1 AC1):
        //   1. Stand up a local Kestrel stub that serves a small markdown page on a random port.
        //   2. Configure Ingestion:UrlFetcher:AllowPrivateHosts=true for this test.
        //   3. POST /api/ingest/url pointing at http://127.0.0.1:{port}/doc.md.
        //   4. Poll GET /api/ingest/{instanceId} until RuntimeStatus=Completed.
        //   5. Assert IngestionResult.Status=Indexed and /api/search returns the indexed unit.
        _ = _fixture;
    }

    [Fact(Skip = "Requires Aspire AppHost fixture — unskip with Story 6.3 retry harness")]
    public void UrlIngestion_404Url_ShouldFailAfterRetries()
    {
        // Scenario (Story 6.1 AC2):
        //   Stub returns 404; assert eventual IngestionResult.Status=Failed with
        //   FailureDetails.ErrorCode="URL_CLIENT_ERROR" after retry budget exhaustion.
        _ = _fixture;
    }

    [Fact(Skip = "Requires Aspire AppHost fixture — unskip with Story 6.3 retry harness")]
    public void UrlIngestion_PrivateIpWithAllowDisabled_ShouldRejectBeforeScheduling()
    {
        // Scenario (Story 6.1 AC3):
        //   POST /api/ingest/url with http://169.254.169.254/ → 400 INVALID_URL,
        //   and no workflow is scheduled (verify by inspecting the workflow state store).
        _ = _fixture;
    }
}
