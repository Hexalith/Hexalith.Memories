// <copyright file="GenerateEmbeddingActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Diagnostics;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class GenerateEmbeddingActivityTests
{
    private const string TenantId = "test-tenant";
    private const string TestText = "Hello world";

    [Fact]
    public async Task RunAsync_SuccessfulEmbedding_ReturnsCorrectResult()
    {
        float[] expectedVector = new float[768];
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(expectedVector);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(config);
        rateLimiter.TryConsumeAsync().Returns(true);
        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);

        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        EmbeddingResult result = await activity.RunAsync(context, new EmbeddingInput(TenantId, TestText));

        result.Vector.ShouldBe(expectedVector);
        result.Provider.ShouldBe("google:gemini-embedding-001");
        result.Dimensions.ShouldBe(768);
    }

    [Fact]
    public async Task RunAsync_LocalRateLimitRefused_ThrowsAndDoesNotReportToActor()
    {
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient([]);
        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: false, out IEmbeddingRateLimiterActor rateLimiter);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);

        EmbeddingRateLimitException ex = await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => activity.RunAsync(context, new EmbeddingInput(TenantId, TestText)));

        ex.TenantId.ShouldBe(TenantId);
        await rateLimiter.DidNotReceive().ReportRateLimitedAsync(Arg.Any<int>());
        await embeddingClient.DidNotReceive().GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ActiveMigrationMarkerWithOldConfig_ShouldNotCallProvider()
    {
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient([]);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(EmbeddingProviderDefaults.Google());
        rateLimiter.TryConsumeAsync().Returns(true);
        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);

        GenerateEmbeddingActivity activity = CreateActivity(
            embeddingClient,
            actorProxyFactory,
            redis: CreateRedisWithActiveMarker(TenantId));

        await Should.ThrowAsync<Hexalith.Memories.Server.Migration.EmbeddingMigrationWriteBlockedException>(
            () => activity.RunAsync(Substitute.For<WorkflowActivityContext>(), new EmbeddingInput(TenantId, TestText)));

        await embeddingClient.DidNotReceive().PrimeApiKeyAsync(
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
        await embeddingClient.DidNotReceive().GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_EmbeddingClientThrowsNon429_ExceptionPropagates()
    {
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient([]);
        embeddingClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmbeddingApiException(500, "internal error", TenantId));

        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: true, out IEmbeddingRateLimiterActor rateLimiter);
        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<EmbeddingApiException>(
            () => activity.RunAsync(context, new EmbeddingInput(TenantId, TestText)));

        await rateLimiter.DidNotReceive().ReportRateLimitedAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task RunAsync_Provider429WithRetryAfter_CallsReportRateLimitedWithValue()
    {
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient([]);
        embeddingClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmbeddingRateLimitException(TenantId) { RetryAfterSeconds = 60 });

        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: true, out IEmbeddingRateLimiterActor rateLimiter);
        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => activity.RunAsync(context, new EmbeddingInput(TenantId, TestText)));

        await rateLimiter.Received(1).ReportRateLimitedAsync(60);
    }

    [Fact]
    public async Task RunAsync_Provider429WithoutRetryAfter_DefaultsToThirtySeconds()
    {
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient([]);
        embeddingClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmbeddingRateLimitException(TenantId)); // RetryAfterSeconds = 0 (default)

        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: true, out IEmbeddingRateLimiterActor rateLimiter);
        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => activity.RunAsync(context, new EmbeddingInput(TenantId, TestText)));

        await rateLimiter.Received(1).ReportRateLimitedAsync(30);
    }

    [Fact]
    public async Task RunAsync_RetryAttempt_DelaysBeforeProviderCall()
    {
        float[] expectedVector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient([]);
        embeddingClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<float[]>(new EmbeddingRateLimitException(TenantId) { RetryAfterSeconds = 30 }),
                Task.FromResult(expectedVector));

        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: true, out IEmbeddingRateLimiterActor rateLimiter);
        FixedJitterSource jitter = new(123);
        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory, jitter);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        context.InstanceId.Returns("workflow-1");

        await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => activity.RunAsync(context, new EmbeddingInput(TenantId, TestText)));

        jitter.CallCount.ShouldBe(0);

        Stopwatch stopwatch = Stopwatch.StartNew();
        EmbeddingResult result = await activity.RunAsync(context, new EmbeddingInput(TenantId, TestText));
        stopwatch.Stop();

        result.Vector.ShouldBe(expectedVector);
        jitter.CallCount.ShouldBe(1);
        stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(100);
        await rateLimiter.Received(1).ReportRateLimitedAsync(30);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_InvalidTenantId_ThrowsArgumentException(string? tenantId)
    {
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(new float[768]);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, new EmbeddingInput(tenantId!, TestText)));
    }

    // Story 5.5 AC5 / FR69 — ceiling is pulled per invocation.
    [Fact]
    public async Task RunAsync_PropagatesConfiguredRateLimitCeilingToRateLimiter()
    {
        float[] vector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(vector);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(
            EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 500 });
        rateLimiter.TryConsumeAsync().Returns(true);

        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);

        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        _ = await activity.RunAsync(context, new EmbeddingInput(TenantId, TestText));

        await rateLimiter.Received(1).SetCeilingAsync(500);
        await rateLimiter.Received(1).TryConsumeAsync();
    }

    // Story 6.2 AC7 regression guard — updated tenant config must be reflected on each activity run.
    [Fact]
    public async Task RunAsync_CeilingChangedBetweenInvocations_ReflectsLatestConfig()
    {
        float[] vector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(vector);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(
            EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 500 },
            EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 100 });
        rateLimiter.TryConsumeAsync().Returns(true);

        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);

        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await activity.RunAsync(context, new EmbeddingInput(TenantId, TestText));
        await activity.RunAsync(context, new EmbeddingInput(TenantId, TestText));

        Received.InOrder(() =>
        {
            rateLimiter.SetCeilingAsync(500);
            rateLimiter.TryConsumeAsync();
            rateLimiter.SetCeilingAsync(100);
            rateLimiter.TryConsumeAsync();
        });
    }

    [Fact]
    public async Task RunAsync_PopulatesEmbeddingResultModelFromConfig()
    {
        float[] vector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(vector);
        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: true, out _);
        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        EmbeddingResult result = await activity.RunAsync(context, new EmbeddingInput(TenantId, TestText));

        result.Model.ShouldBe(EmbeddingProviderDefaults.GoogleModelName);
    }

    [Fact]
    public async Task RunAsync_UsesCorrectActorIdFromTenantId()
    {
        float[] vector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(vector);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(EmbeddingProviderDefaults.Google());
        rateLimiter.TryConsumeAsync().Returns(true);

        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);

        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await activity.RunAsync(context, new EmbeddingInput(TenantId, TestText));

        actorProxyFactory.Received(1).CreateActorProxy<IEmbeddingRateLimiterActor>(
            Arg.Is<ActorId>(id => id.ToString() == TenantId),
            "EmbeddingRateLimiterActor");
        actorProxyFactory.Received(1).CreateActorProxy<ITenantConfigurationActor>(
            Arg.Is<ActorId>(id => id.ToString() == TenantId),
            "TenantConfigurationActor");
    }

    [Theory]
    [InlineData(EmbeddingContentKind.Payload, "payload")]
    [InlineData(EmbeddingContentKind.NaturalLanguageDescription, "naturalLanguageDescription")]
    public async Task ContentKind_PropagatesToTelemetryTag(EmbeddingContentKind kind, string expectedTagValue)
    {
        // Story 9.2 Risk #6 guard — the EmbeddingApiCalls counter must carry a `content_kind` tag
        // whose value reflects the input kind. Operators use the resulting 2:1 tag split (payload :
        // naturalLanguageDescription) to size per-tenant rate-limit ceilings for dual embedding.
        float[] vector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(vector);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(EmbeddingProviderDefaults.Google());
        rateLimiter.TryConsumeAsync().Returns(true);
        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);

        GenerateEmbeddingActivity activity = CreateActivity(embeddingClient, actorProxyFactory);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        // Use a tenant_id unique to this theory case so parallel-running telemetry tests cannot
        // contaminate the observed list via the static MemoriesMeter.Instance singleton.
        string uniqueTenant = $"ckprop-{kind}-{Guid.NewGuid():N}";

        List<(string tenantId, string contentKind, long delta)> observed = [];
        using System.Diagnostics.Metrics.MeterListener listener = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == Hexalith.Memories.Telemetry.MemoriesMeter.Name
                && instrument.Name == Hexalith.Memories.Telemetry.MemoriesMeter.EmbeddingApiCallsName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string tenant = string.Empty;
            string ckind = string.Empty;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "tenant_id")
                {
                    tenant = tag.Value?.ToString() ?? string.Empty;
                }
                else if (tag.Key == "content_kind")
                {
                    ckind = tag.Value?.ToString() ?? string.Empty;
                }
            }

            if (tenant == uniqueTenant)
            {
                observed.Add((tenant, ckind, measurement));
            }
        });
        listener.Start();

        EmbeddingInput input = new(uniqueTenant, TestText, kind);
        await activity.RunAsync(context, input);

        listener.Dispose();

        (string tenantId, string contentKind, long delta) single = observed.ShouldHaveSingleItem();
        single.tenantId.ShouldBe(uniqueTenant);
        single.contentKind.ShouldBe(expectedTagValue);
        single.delta.ShouldBe(1);
    }

    private static GenerateEmbeddingActivity CreateActivity(
        EmbeddingClient embeddingClient,
        IActorProxyFactory actorProxyFactory,
        IJitterSource? jitterSource = null,
        IConnectionMultiplexer? redis = null)
        => new(
            embeddingClient,
            actorProxyFactory,
            jitterSource ?? new FixedJitterSource(0),
            NullLogger<GenerateEmbeddingActivity>.Instance,
            redis ?? CreateRedisWithoutMarker());

    private static EmbeddingClient CreateMockEmbeddingClient(float[] vectorToReturn)
    {
        EmbeddingClient client = Substitute.For<EmbeddingClient>(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            CreateConfiguration(),
            CreateHostEnvironment());
        client.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        client.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(vectorToReturn);
        return client;
    }

    private static IConfiguration CreateConfiguration(bool useFakeEmbedding = false)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = useFakeEmbedding.ToString(),
            })
            .Build();

    private static IHostEnvironment CreateHostEnvironment(string environmentName = "Development")
    {
        IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environmentName);
        return hostEnvironment;
    }

    private static IActorProxyFactory CreateMockActorProxyFactory(bool allowed, out IEmbeddingRateLimiterActor rateLimiter)
    {
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(EmbeddingProviderDefaults.Google());
        rateLimiter.TryConsumeAsync().Returns(allowed);

        factory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        factory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);

        return factory;
    }

    private static IConnectionMultiplexer CreateRedisWithoutMarker()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<HashEntry>()));
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();

        // F23: production calls IConnectionMultiplexer.GetDatabase() with no args; stub the no-arg overload directly
        // so the marker-read path is actually exercised under these tests.
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static IConnectionMultiplexer CreateRedisWithActiveMarker(string tenantId)
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(new[]
            {
                new HashEntry("tenantId", tenantId),
                new HashEntry("targetProvider", EmbeddingProviderDefaults.OllamaProviderName),
                new HashEntry("targetModel", EmbeddingProviderDefaults.OllamaModelName),
                new HashEntry("targetDimensions", EmbeddingProviderDefaults.Ollama().Dimensions),
                new HashEntry("status", "started"),
            }));
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();

        // F23: production calls IConnectionMultiplexer.GetDatabase() with no args; stub the no-arg overload directly
        // so the marker-read path is actually exercised under these tests.
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private sealed class FixedJitterSource : IJitterSource
    {
        private readonly int _value;

        public FixedJitterSource(int value) => _value = value;

        public int CallCount { get; private set; }

        public int NextMilliseconds(int maxExclusive = 500)
        {
            CallCount++;
            return _value;
        }
    }
}
