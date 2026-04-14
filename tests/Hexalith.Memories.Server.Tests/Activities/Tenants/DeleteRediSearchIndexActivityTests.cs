// <copyright file="DeleteRediSearchIndexActivityTests.cs" company="ITANEO">
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

public class DeleteRediSearchIndexActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldReturnTrue()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(RedisResult.Create(new RedisValue("OK")));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create(new RedisValue("OK")));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteRediSearchIndexActivity> logger = Substitute.For<ILogger<DeleteRediSearchIndexActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteRediSearchIndexActivity activity = new(redis, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_UnknownIndex_ShouldSwallowGracefully()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(_ => throw new RedisServerException("Unknown index name"));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw new RedisServerException("Unknown index name"));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteRediSearchIndexActivity> logger = Substitute.For<ILogger<DeleteRediSearchIndexActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteRediSearchIndexActivity activity = new(redis, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }
}
