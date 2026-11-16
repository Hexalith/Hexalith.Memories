// <copyright file="DegradationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

/// <summary>Contains the fast integration cases for DegradationIntegrationTests.</summary>
public sealed partial class DegradationIntegrationTests
{
    [Fact(Skip = "26.3-SEMANTIC-CAPABILITY-FAULT: Redis Stack hosts both syntactic and semantic indexes, so stopping it cannot isolate only the semantic capability in the current topology. Owner: search maintainers. Unskip when: semantic failure can be injected independently of the shared Redis resource.")]
    public void HybridSearch_RedisVectorStopped_ShouldReturn200Degraded()
    {
        // Scenario:
        //   1. Stop the Redis Vector container.
        //   2. POST a hybrid search with axes=syntactic,semantic,graph.
        //   3. Expect 200 OK with degraded=true, unavailableAxes=["semantic"],
        //      results from syntactic + graph.
    }

    [Fact(Skip = "26.3-ALL-BACKENDS-STATESTORE: Redis Stack is also the DAPR workflow and actor state store, so stopping it destroys the control plane before the search-only all-backends response can be observed. Owner: AppHost maintainers. Unskip when: DAPR state and search indexes use separate resources.")]
    public void HybridSearch_AllBackendsStopped_ShouldReturn503AllBackendsUnavailable()
    {
        // Scenario:
        //   1. Stop Redis Stack + FalkorDB.
        //   2. POST a hybrid search.
        //   3. Expect 503 with ErrorResponse.Code == "ALL_BACKENDS_UNAVAILABLE" and
        //      Retry-After: 5 header. Body message lists all enabled axes.
    }

    [Fact(Skip = "26.3-SINGLE-AXIS-REDIS-COLLAPSE: Redis Stack is shared by search indexes and DAPR state, so stopping it cannot isolate the syntactic backend while keeping the API control plane healthy. Owner: AppHost maintainers. Unskip when: search Redis is separated from workflow and actor state.")]
    public void SingleAxisSearch_RedisStopped_ShouldReturn503BackendUnavailable()
    {
        // Scenario:
        //   1. Stop Redis Stack.
        //   2. GET /api/v1/search?axis=syntactic.
        //   3. Expect 503 with ErrorResponse.Code == "BACKEND_UNAVAILABLE" and
        //      Retry-After: 5 header.
    }
}
