// <copyright file="DeleteFalkorDbGraphActivityTests.cs" company="ITANEO">
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

public class DeleteFalkorDbGraphActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldReturnTrue()
    {
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<DeleteFalkorDbGraphActivity> logger = Substitute.For<ILogger<DeleteFalkorDbGraphActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteFalkorDbGraphActivity activity = new(falkorDb, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_GraphNotFound_ShouldSwallowGracefully()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(_ => throw Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Graph not found"));
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns<RedisResult>(_ => throw Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Graph not found"));

        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        ILogger<DeleteFalkorDbGraphActivity> logger = Substitute.For<ILogger<DeleteFalkorDbGraphActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteFalkorDbGraphActivity activity = new(falkorDb, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisResult fakeGraphResult = RedisResult.Create(
        [
            RedisResult.Create(Array.Empty<RedisResult>()),
            RedisResult.Create(Array.Empty<RedisResult>()),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("Nodes deleted: 0")),
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
