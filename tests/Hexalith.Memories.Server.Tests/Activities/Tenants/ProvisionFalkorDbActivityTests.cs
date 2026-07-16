// <copyright file="ProvisionFalkorDbActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class ProvisionFalkorDbActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldReturnTrue()
    {
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<ProvisionFalkorDbActivity> logger = Substitute.For<ILogger<ProvisionFalkorDbActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionFalkorDbActivity activity = new(falkorDb, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_NullInput_ShouldThrowArgumentNullException()
    {
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<ProvisionFalkorDbActivity> logger = Substitute.For<ILogger<ProvisionFalkorDbActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionFalkorDbActivity activity = new(falkorDb, logger);

        await Should.ThrowAsync<ArgumentNullException>(() => activity.RunAsync(context, null!));
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        // FalkorDB.QueryAsync calls db.ExecuteAsync("GRAPH.QUERY", ...) internally.
        // The result must be a 3-element array: [headers, data, statistics].
        RedisResult fakeGraphResult = RedisResult.Create(
        [
            RedisResult.Create(Array.Empty<RedisResult>()),
            RedisResult.Create(Array.Empty<RedisResult>()),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("Nodes created: 1")),
                RedisResult.Create(new RedisValue("Nodes deleted: 1")),
                RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
            ]),
        ]);

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(fakeGraphResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(fakeGraphResult);

        return (falkorDb, db);
    }
}
