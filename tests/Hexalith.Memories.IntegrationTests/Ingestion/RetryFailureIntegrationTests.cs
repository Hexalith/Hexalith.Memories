// <copyright file="RetryFailureIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>
/// Story 6.3 integration deferrals for bulk re-ingestion and counter-stage concurrency.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class RetryFailureIntegrationTests
{
    [Fact(Skip = "26.3-BULK-REINGEST-HICCUP: The bulk outcome matrix requires deterministic missing, claimed, and Redis-error units in one request, but the fixture has no request-scoped Redis fault hook. Owner: ingestion maintainers. Unskip when: the fixture exposes per-unit claim and Redis fault controls without process-global mutation.")]
    public void ReIngestBulk_MixedOutcomes_EnumeratedInResponse()
    {
        // AC10: bulk re-ingest 5 units; one is missing, one is mid-claim by another caller, one
        // hits a Redis hiccup. Assert the response has Scheduled=2, NotFound=1, Conflicted=1,
        // Errored=1 with each per-unit Outcome populated.
    }

    [Fact(Skip = "26.3-COUNTER-STAGE-BARRIER: Concurrent stage counts require pausing six real workflows at exact durable-workflow stage barriers that the AppHost fixture cannot currently control. Owner: workflow maintainers. Unskip when: test-only stage barriers can pause and release real workflow instances deterministically.")]
    public void CounterActor_TracksConcurrentInflightWorkflows()
    {
        // AC5, AC6: schedule 3 in Embedding, 2 in Extracting, 1 in Queued. Assert
        // GET /cases/{caseId}/status returns ExtractingCount=2, EmbeddingCount=3, QueuedCount=1.
    }
}
