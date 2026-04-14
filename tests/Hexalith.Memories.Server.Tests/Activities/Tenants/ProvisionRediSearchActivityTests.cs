// <copyright file="ProvisionRediSearchActivityTests.cs" company="ITANEO">
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

public class ProvisionRediSearchActivityTests
{
    [Fact]
    public async Task RunAsync_IndexAlreadyExistsWithMatchingSchema_ShouldReturnTrue()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ConfigureIndexAlreadyExists(db, CreateMatchingSyntacticIndexInfo());
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRediSearchActivity> logger = Substitute.For<ILogger<ProvisionRediSearchActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRediSearchActivity activity = new(redis, logger);

        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_IndexAlreadyExistsWithDifferentSchema_ShouldThrowInvalidOperationException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ConfigureIndexAlreadyExists(db, CreateMismatchedSyntacticIndexInfo());
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRediSearchActivity> logger = Substitute.For<ILogger<ProvisionRediSearchActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRediSearchActivity activity = new(redis, logger);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, input));

        ex.Message.ShouldContain("does not match the expected tenant schema");
    }

    [Fact]
    public async Task RunAsync_NullInput_ShouldThrowArgumentNullException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRediSearchActivity> logger = Substitute.For<ILogger<ProvisionRediSearchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRediSearchActivity activity = new(redis, logger);

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

    private static RedisResult CreateMatchingSyntacticIndexInfo() => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("num_docs")),
        RedisResult.Create(new RedisValue("0")),
        RedisResult.Create(new RedisValue("index_definition")),
        RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("prefixes")),
            RedisResult.Create([RedisResult.Create(new RedisValue("test-tenant:mu:"))]),
        ]),
        RedisResult.Create(new RedisValue("attributes")),
        RedisResult.Create(
        [
            CreateAttribute("content", "TEXT"),
            CreateAttribute("sourceUriText", "TEXT"),
            CreateAttribute("sourceTypeText", "TEXT"),
            CreateAttribute("metadataText", "TEXT"),
            CreateAttribute("sourceUri", "TAG"),
            CreateAttribute("sourceType", "TAG"),
            CreateAttribute("contentHash", "TAG"),
            CreateAttribute("caseId", "TAG"),
            CreateAttribute("embeddingProvider", "TAG"),
        ]),
    ]);

    private static RedisResult CreateMismatchedSyntacticIndexInfo() => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("index_definition")),
        RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("prefixes")),
            RedisResult.Create([RedisResult.Create(new RedisValue("wrong-tenant:mu:"))]),
        ]),
        RedisResult.Create(new RedisValue("attributes")),
        RedisResult.Create(
        [
            CreateAttribute("content", "TEXT"),
            CreateAttribute("sourceUri", "TAG"),
        ]),
    ]);

    private static RedisResult CreateAttribute(string identifier, string type) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("identifier")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("attribute")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("type")),
        RedisResult.Create(new RedisValue(type)),
    ]);
}
