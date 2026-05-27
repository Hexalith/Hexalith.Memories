// <copyright file="CheckMemoryUnitExistsActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Indexing;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class CheckMemoryUnitExistsActivityTests
{
    [Fact]
    public async Task RunAsync_SyntacticHashPresent_ReturnsTrue()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.KeyExistsAsync("tenant-a:mu:mu-001", Arg.Any<CommandFlags>()).Returns(true);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        CheckMemoryUnitExistsActivity activity = new(redis);

        bool exists = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-a"));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_SyntacticHashMissing_ReturnsFalse()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.KeyExistsAsync("tenant-a:mu:mu-001", Arg.Any<CommandFlags>()).Returns(false);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        CheckMemoryUnitExistsActivity activity = new(redis);

        bool exists = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new ConsistencyInput("mu-001", "tenant-a"));

        exists.ShouldBeFalse();
    }
}