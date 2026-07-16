// <copyright file="RestoreReindexUnitActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Restore;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Restore;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;
using Hexalith.Memories.Server.Workflows.Contracts;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Tests restore re-index attribution compatibility.</summary>
[Trait("Category", "Unit")]
public sealed class RestoreReindexUnitActivityTests
{
    [Theory]
    [InlineData("google", "google", "gemini-embedding-001")]
    [InlineData("google:gemini-embedding-001", "google", "gemini-embedding-001")]
    [InlineData("GOOGLE:GEMINI-EMBEDDING-001", "google", "gemini-embedding-001")]
    [InlineData("ollama:qwen3-embedding:4b", "ollama", "qwen3-embedding:4b")]
    public void MatchesProviderAttribution_CompatibleStoredForms_ReturnTrue(
        string attribution,
        string provider,
        string model)
        => RestoreReindexUnitActivity.MatchesProviderAttribution(attribution, provider, model).ShouldBeTrue();

    [Theory]
    [InlineData("google:text-embedding-004", "google", "gemini-embedding-001")]
    [InlineData("ollama:qwen3-embedding:4b", "google", "gemini-embedding-001")]
    [InlineData("openai", "google", "gemini-embedding-001")]
    public void MatchesProviderAttribution_IncompatibleStoredForms_ReturnFalse(
        string attribution,
        string provider,
        string model)
        => RestoreReindexUnitActivity.MatchesProviderAttribution(attribution, provider, model).ShouldBeFalse();

    [Fact]
    public async Task RunAsync_WritesExactChunkFieldsAndCountsRestoredUnit()
    {
        (RestoreReindexUnitActivity activity, IDatabase db, ITenantIndexReadinessVerifier readiness, EmbeddingClient client) =
            CreateActivity("hello world");

        RestoreReindexResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreReindexInput("acme", "mu-1"));

        result.MemoryUnitId.ShouldBe("mu-1");
        result.ChunkCount.ShouldBe(1);
        await readiness.Received(1).EnsureReadyAsync(
            db,
            "acme",
            TenantIndexFamily.Semantic,
            3,
            CancellationToken.None);
        await client.Received(1).GenerateBatchAsync(
            Arg.Is<IReadOnlyList<string>>(values => values.Count == 1 && values[0] == "hello world"),
            "acme",
            Arg.Any<TenantEmbeddingConfig>(),
            CancellationToken.None);
        await db.Received(1).HashSetAsync(
            "acme:vec:mu-1:0",
            Arg.Is<HashEntry[]>(entries => HasExactSemanticFields(entries)));
    }

    [Fact]
    public async Task RunAsync_ActiveMigrationMismatch_FailsBeforeEmbeddingOrWrite()
    {
        (RestoreReindexUnitActivity activity, IDatabase db, _, EmbeddingClient client) = CreateActivity("hello");
        db.HashGetAllAsync(
                EmbeddingMigrationMarkerReader.GetActiveMarkerKey("acme"),
                CommandFlags.DemandMaster)
            .Returns(
            [
                new HashEntry("status", "reindexing"),
                new HashEntry("tenantId", "acme"),
                new HashEntry("targetProvider", "ollama"),
                new HashEntry("targetModel", "other"),
                new HashEntry("targetDimensions", 3),
            ]);

        await Should.ThrowAsync<EmbeddingMigrationWriteBlockedException>(() => activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreReindexInput("acme", "mu-1")));

        await client.DidNotReceive().GenerateBatchAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
        await db.DidNotReceive().HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_ZeroChunks_FailsInsteadOfCountingUnitRestored()
    {
        (RestoreReindexUnitActivity activity, _, _, _) = CreateActivity("   ");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RestoreReindexInput("acme", "mu-1")));

        exception.Message.ShouldContain("zero semantic chunks");
    }

    private static (RestoreReindexUnitActivity, IDatabase, ITenantIndexReadinessVerifier, EmbeddingClient) CreateActivity(
        string content)
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue[]>(),
                Arg.Any<CommandFlags>())
            .Returns(
            [
                (RedisValue)content,
                "case-1",
                "google:text-embedding-004",
                "text-embedding-004",
                "3",
                "subject-1",
            ]);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        TenantEmbeddingConfig config = new("google", "text-embedding-004", 3, 60, "embedding-key");
        ITenantEmbeddingConfigProvider configProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        configProvider.GetAsync("acme", Arg.Any<CancellationToken>()).Returns(config);
        ITenantIndexReadinessVerifier readiness = Substitute.For<ITenantIndexReadinessVerifier>();

        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        EmbeddingClient client = Substitute.For<EmbeddingClient>(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = "false",
            }).Build(),
            environment);
        client.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        client.GenerateBatchAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<string>(),
                Arg.Any<TenantEmbeddingConfig>(),
                Arg.Any<CancellationToken>())
            .Returns(new[] { new[] { 1f, 2f, 3f } });
        client.ClearReceivedCalls();

        RestoreReindexUnitActivity activity = new(
            redis,
            client,
            configProvider,
            Options.Create(new ContentChunkingOptions()),
            Substitute.For<ILogger<RestoreReindexUnitActivity>>(),
            readiness);
        return (activity, db, readiness, client);
    }

    private static bool HasExactSemanticFields(HashEntry[] entries)
    {
        HashSet<string> actual = entries.Select(static entry => entry.Name.ToString()).ToHashSet(StringComparer.Ordinal);
        HashSet<string> expected =
        [
            "embedding",
            "tenantId",
            "memoryUnitId",
            "caseId",
            "embeddingProvider",
            "embeddingModel",
            "embeddingDimensions",
            "chunkSequence",
            "chunkStartOffset",
            "chunkEndOffset",
            "chunkText",
            "cloudeventSubject",
        ];
        return actual.SetEquals(expected);
    }
}
