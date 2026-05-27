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
        ConfigureIndexAlreadyExistsWithInfoResolver(
            db,
            indexName => indexName.EndsWith(":memories:vec:nl", StringComparison.Ordinal)
                ? CreateMatchingNaturalLanguageIndexInfo(768)
                : CreateMatchingSemanticIndexInfo(768));
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
        ConfigureIndexAlreadyExistsWithInfoResolver(
            db,
            indexName => indexName.EndsWith(":memories:vec:nl", StringComparison.Ordinal)
                ? CreateMatchingNaturalLanguageIndexInfo(768)
                : CreateMatchingSemanticIndexInfo(1536));
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

    [Fact]
    public async Task RunAsync_CreatesBothIndexes_SameDimensions()
    {
        // Story 9.2 Task 4.5: provisioning must create BOTH the raw and NL semantic indexes at the same
        // tenant-configured dimensions (Risk #5 — shared schema shape enforced via the core helper).
        IDatabase db = Substitute.For<IDatabase>();
        List<object[]> ftCreateCalls = [];
        db.Execute(Arg.Is<string>(c => c == "FT.CREATE"), Arg.Do<object[]>(ftCreateCalls.Add))
            .Returns(RedisResult.Create(new RedisValue("OK")));
        db.Execute(Arg.Is<string>(c => c == "FT.CREATE"), Arg.Do<ICollection<object>>(a => ftCreateCalls.Add([.. a])), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create(new RedisValue("OK")));

        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRedisVectorActivity> logger = Substitute.For<ILogger<ProvisionRedisVectorActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRedisVectorActivity activity = new(redis, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
        List<string> indexNames = [.. ftCreateCalls.Select(args => args.Length > 0 ? args[0]?.ToString() ?? string.Empty : string.Empty)];
        indexNames.ShouldContain("test-tenant:memories:vec");
        indexNames.ShouldContain("test-tenant:memories:vec:nl");
    }

    [Fact]
    public async Task RunAsync_NaturalLanguageIndexAlreadyExistsWithDifferentDimensions_ShouldThrowInvalidOperationException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ConfigureIndexAlreadyExistsWithInfoResolver(
            db,
            indexName => indexName.EndsWith(":memories:vec:nl", StringComparison.Ordinal)
                ? CreateMatchingNaturalLanguageIndexInfo(1536)
                : CreateMatchingSemanticIndexInfo(768));

        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<ProvisionRedisVectorActivity> logger = Substitute.For<ILogger<ProvisionRedisVectorActivity>>();
        var input = new TenantProvisioningInput("test-tenant", "Test") { VectorDimensions = 768 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        ProvisionRedisVectorActivity activity = new(redis, logger);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, input));

        ex.Message.ShouldContain("does not match the expected tenant schema");
        ex.Message.ShouldContain("expected 768 dimensions but found 1536");
    }

    private static void ConfigureIndexAlreadyExistsWithInfoResolver(IDatabase db, Func<string, RedisResult> infoResolver)
    {
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.CREATE"), Arg.Any<object[]>())
            .Returns(_ => throw new RedisServerException("Index already exists"));
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.CREATE"), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw new RedisServerException("Index already exists"));
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Any<object[]>())
            .Returns(callInfo => infoResolver(callInfo.ArgAt<object[]>(1)[0]?.ToString() ?? string.Empty));
        db.Execute(Arg.Is<string>(cmd => cmd == "FT.INFO"), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(callInfo => infoResolver(callInfo.ArgAt<ICollection<object>>(1).FirstOrDefault()?.ToString() ?? string.Empty));
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
            CreateTagAttribute("cloudeventSubject"),
        ]),
    ]);

    private static RedisResult CreateMatchingNaturalLanguageIndexInfo(int dimensions) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("num_docs")),
        RedisResult.Create(new RedisValue("0")),
        RedisResult.Create(new RedisValue("index_definition")),
        RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("prefixes")),
            RedisResult.Create([RedisResult.Create(new RedisValue("test-tenant:vec:nl:"))]),
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
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("identifier")),
                RedisResult.Create(new RedisValue("naturalLanguageDescription")),
                RedisResult.Create(new RedisValue("attribute")),
                RedisResult.Create(new RedisValue("naturalLanguageDescription")),
                RedisResult.Create(new RedisValue("type")),
                RedisResult.Create(new RedisValue("TEXT")),
            ]),
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
