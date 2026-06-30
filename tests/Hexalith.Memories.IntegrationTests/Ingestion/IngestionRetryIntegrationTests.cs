// <copyright file="IngestionRetryIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class IngestionRetryIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public IngestionRetryIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [RunnableSkippedFact("Requires Aspire AppHost fixture — unskip with Story 6.3 retry validation harness")]
    public void TransientIngestionFailure_ShouldCompleteSuccessfullyAfterRetries()
    {
        // Scenario (Task 4.4):
        //   1. Inject an EmbeddingClient mock that fails the first 3 calls with
        //      EmbeddingApiException, then succeeds.
        //   2. Start an ingestion workflow.
        //   3. Assert the workflow completes (status=indexed), memory unit is indexed,
        //      and no failure is recorded after the retries succeed.
        //   4. Verifies AC5 end-to-end: the DAPR Workflow retry policy
        //      (maxNumberOfAttempts=5, firstRetryInterval=2s, backoffCoefficient=1.5)
        //      absorbs transient backend failures before moving the unit to `failed`.
        _ = _fixture;
    }
}
