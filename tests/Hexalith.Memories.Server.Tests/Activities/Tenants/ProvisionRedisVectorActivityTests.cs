// <copyright file="ProvisionRedisVectorActivityTests.cs" company="ITANEO">
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

public class ProvisionRedisVectorActivityTests
{
    [Fact]
    public async Task RunAsync_IndexAlreadyExistsWithMatchingSchema_ShouldReturnTrue()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ConfigureIndexAlreadyExists(db, CreateMatchingSemanticIndexInfo(768));
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRedisVectorActivity> logger = Substitute.For<ILogger<ProvisionRedisVectorActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRedisVectorActivity activity = new(redis, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_IndexAlreadyExistsWithDifferentDimensions_ShouldThrowInvalidOperationException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ConfigureIndexAlreadyExists(db, CreateMatchingSemanticIndexInfo(1536));
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRedisVectorActivity> logger = Substitute.For<ILogger<ProvisionRedisVectorActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRedisVectorActivity activity = new(redis, logger);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, input));

        ex.Message.ShouldContain("does not match the expected tenant schema");
        ex.Message.ShouldContain("expected 768 dimensions but found 1536");
    }

    [Fact]
    public async Task RunAsync_NullInput_ShouldThrowArgumentNullException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRedisVectorActivity> logger = Substitute.For<ILogger<ProvisionRedisVectorActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRedisVectorActivity activity = new(redis, logger);

        await Should.ThrowAsync<ArgumentNullException>(() => activity.RunAsync(context, null!));
    }

    private static void ConfigureIndexAlreadyExists(IDatabase db, RedisResult infoResult)
    {
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.CREATE"), Arg.Any<object[]>())
            .Returns(_ => throw new RedisServerException("Index already exists"));
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.CREATE"), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw new RedisServerException("Index already exists"));
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Any<object[]>())
            .Returns(infoResult);
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(infoResult);
    }

    private static IConnectionMultiplexer CreateMockMultiplexer(IDatabase db)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static RedisResult CreateMatchingSemanticIndexInfo(int dimensions) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("num_docs")),
        RedisResult.Create(new RedisValue("0")),
        RedisResult.Create(new RedisValue("index_definition")),
        RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("prefixes")),
            RedisResult.Create([RedisResult.Create(new RedisValue("test-tenant:vec:"))]),
        ]),
        RedisResult.Create(new RedisValue("attributes")),
        RedisResult.Create(
        [
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("identifier")),
                RedisResult.Create(new RedisValue("embedding")),
                RedisResult.Create(new RedisValue("attribute")),
                RedisResult.Create(new RedisValue("embedding")),
                RedisResult.Create(new RedisValue("type")),
                RedisResult.Create(new RedisValue("VECTOR")),
                RedisResult.Create(new RedisValue("dim")),
                RedisResult.Create(new RedisValue(dimensions.ToString())),
            ]),
            CreateTagAttribute("memoryUnitId"),
            CreateTagAttribute("caseId"),
        ]),
    ]);

    private static RedisResult CreateTagAttribute(string identifier) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("identifier")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("attribute")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("type")),
        RedisResult.Create(new RedisValue("TAG")),
    ]);
}
