// <copyright file="RetryFailureIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>
/// Story 6.3 integration tests for retry, failure visibility, and re-ingestion. All scenarios are
/// <c>[Fact(Skip)]</c> — Story 6.4 (pipeline state persistence) or Epic 7 (e2e harness) unskips them
/// once the Aspire fixture is wired to deterministic 500-producing provider test doubles.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class RetryFailureIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public RetryFailureIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [RunnableSkippedFact("Unskipped by Story 6.4 (pipeline state persistence) or Epic 7 e2e harness — requires Aspire fixture + deterministic 500-producing provider test double.")]
    public void IngestUrl_ProviderReturns500_ExhaustsRetriesAndPersistsFailedUnit()
    {
        // AC3, AC4: ingest a URL whose backend returns 500. Assert workflow exhausts retries, the
        // failed-unit hash exists, the per-case sorted-set entry exists, GET /failed-units returns
        // it, GET /memory-units/{id} returns Status=Failed with populated FailureDetails, AND the
        // existing IngestionFailed activity-stream event still fires.
        _ = _fixture;
    }

    [RunnableSkippedFact("Unskipped by Story 6.4 (pipeline state persistence) or Epic 7 e2e harness — requires Aspire fixture + deterministic 500-producing provider test double.")]
    public void ReIngestSingle_PreservesMemoryUnitId_AndClearsRegistry()
    {
        // AC9: re-ingest a failed unit via POST. Assert the new workflow's instanceId equals the
        // original memory-unit-id (annotations + graph edges survive); failed-unit hash and dedup
        // key are cleared atomically.
        _ = _fixture;
    }

    [RunnableSkippedFact("Unskipped by Story 6.4 (pipeline state persistence) or Epic 7 e2e harness — requires Aspire fixture + deterministic 500-producing provider test double.")]
    public void ReIngestBulk_MixedOutcomes_EnumeratedInResponse()
    {
        // AC10: bulk re-ingest 5 units; one is missing, one is mid-claim by another caller, one
        // hits a Redis hiccup. Assert the response has Scheduled=2, NotFound=1, Conflicted=1,
        // Errored=1 with each per-unit Outcome populated.
        _ = _fixture;
    }

    [RunnableSkippedFact("Unskipped by Story 6.4 (pipeline state persistence) or Epic 7 e2e harness — requires Aspire fixture + deterministic 500-producing provider test double.")]
    public void CounterActor_TracksConcurrentInflightWorkflows()
    {
        // AC5, AC6: schedule 3 in Embedding, 2 in Extracting, 1 in Queued. Assert
        // GET /cases/{caseId}/status returns ExtractingCount=2, EmbeddingCount=3, QueuedCount=1.
        _ = _fixture;
    }
}
