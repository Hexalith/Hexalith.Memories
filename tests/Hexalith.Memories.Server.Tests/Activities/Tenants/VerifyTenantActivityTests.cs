// <copyright file="VerifyTenantActivityTests.cs" company="ITANEO">
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

public class VerifyTenantActivityTests
{
    [Fact]
    public async Task RunAsync_AllBackendsExistAndAreEmpty_ShouldReturnTrue()
    {
        IDatabase redisDb = Substitute.For<IDatabase>();
        ConfigureFtInfo(redisDb, "test-tenant:memories:idx", CreateFtInfoResult(0));
        ConfigureFtInfo(redisDb, "test-tenant:memories:vec", CreateFtInfoResult(0));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        IDatabase falkorDb = Substitute.For<IDatabase>();
        ConfigureGraphQuery(falkorDb, CreateGraphCountResult("0"));
        IConnectionMultiplexer falkor = Substitute.For<IConnectionMultiplexer>();
        falkor.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(falkorDb);

        ILogger<VerifyTenantActivity> logger = Substitute.For<ILogger<VerifyTenantActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };

        VerifyTenantActivity activity = new(redis, falkor, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_UnreadableGraphCount_ShouldThrowInvalidOperationException()
    {
        IDatabase redisDb = Substitute.For<IDatabase>();
        ConfigureFtInfo(redisDb, "test-tenant:memories:idx", CreateFtInfoResult(0));
        ConfigureFtInfo(redisDb, "test-tenant:memories:vec", CreateFtInfoResult(0));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        IDatabase falkorDb = Substitute.For<IDatabase>();
        ConfigureGraphQuery(falkorDb, CreateGraphCountResult("not-a-number"));
        IConnectionMultiplexer falkor = Substitute.For<IConnectionMultiplexer>();
        falkor.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(falkorDb);

        ILogger<VerifyTenantActivity> logger = Substitute.For<ILogger<VerifyTenantActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };

        VerifyTenantActivity activity = new(redis, falkor, logger);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, input));

        ex.Message.ShouldContain("returned an unreadable node count");
    }

    [Fact]
    public async Task RunAsync_NonEmptyRedisIndex_ShouldThrowInvalidOperationException()
    {
        IDatabase redisDb = Substitute.For<IDatabase>();
        ConfigureFtInfo(redisDb, "test-tenant:memories:idx", CreateFtInfoResult(1));
        ConfigureFtInfo(redisDb, "test-tenant:memories:vec", CreateFtInfoResult(0));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        IDatabase falkorDb = Substitute.For<IDatabase>();
        ConfigureGraphQuery(falkorDb, CreateGraphCountResult("0"));
        IConnectionMultiplexer falkor = Substitute.For<IConnectionMultiplexer>();
        falkor.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(falkorDb);

        ILogger<VerifyTenantActivity> logger = Substitute.For<ILogger<VerifyTenantActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };

        VerifyTenantActivity activity = new(redis, falkor, logger);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, input));

        ex.Message.ShouldContain("RediSearch index 'test-tenant:memories:idx' is not empty");
    }

    private static void ConfigureFtInfo(IDatabase db, string indexName, RedisResult result)
    {
        db.ExecuteAsync(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Is<object[]>(args => args.Length == 1 && args[0] != null && args[0].ToString() == indexName))
            .Returns(result);
        db.ExecuteAsync(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Is<ICollection<object>>(args => args.Count == 1 && args.First().ToString() == indexName), Arg.Any<CommandFlags>())
            .Returns(result);
    }

    private static void ConfigureGraphQuery(IDatabase db, RedisResult result)
    {
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(result);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>()).Returns(result);
    }

    private static RedisResult CreateFtInfoResult(int docCount) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("num_docs")),
        RedisResult.Create(new RedisValue(docCount.ToString())),
    ]);

    private static RedisResult CreateGraphCountResult(string countValue)
    {
        RedisResult headers = RedisResult.Create(
        [
            RedisResult.Create(
            [
                RedisResult.Create((RedisValue)1),
                RedisResult.Create(new RedisValue("count(n)")),
            ]),
        ]);

        RedisResult data = RedisResult.Create(
        [
            RedisResult.Create(
            [
                RedisResult.Create(
                [
                    RedisResult.Create((RedisValue)2),
                    RedisResult.Create(new RedisValue(countValue)),
                ]),
            ]),
        ]);

        RedisResult stats = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("Cached execution: 0")),
            RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
        ]);

        return RedisResult.Create([headers, data, stats]);
    }
}