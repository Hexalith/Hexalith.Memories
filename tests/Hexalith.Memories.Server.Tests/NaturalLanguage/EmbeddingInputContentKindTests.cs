// <copyright file="EmbeddingInputContentKindTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.Diagnostics.Metrics;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

/// <summary>Story 9.2 Task 3.4 / Risk #17 — wire-compat + telemetry coverage for the new
/// <c>EmbeddingInput.ContentKind</c> positional parameter. The positional-record shape is load-bearing
/// for durable workflow replay: paused workflows whose history carries the 9.1 JSON shape
/// <c>{"TenantId":"t","ContentText":"c"}</c> MUST continue to deserialize under 9.2+ code with
/// <c>ContentKind == Payload</c> applied via the default value.</summary>
public sealed class EmbeddingInputContentKindTests
{
    [Fact]
    public void DefaultConstructor_SetsPayloadKind()
    {
        EmbeddingInput input = new("t", "content");

        input.ContentKind.ShouldBe(EmbeddingContentKind.Payload);
    }

    [Fact]
    public void ExplicitNaturalLanguageKind_IsPreserved()
    {
        EmbeddingInput input = new("t", "content", EmbeddingContentKind.NaturalLanguageDescription);

        input.ContentKind.ShouldBe(EmbeddingContentKind.NaturalLanguageDescription);
    }

    [Fact]
    public void HistoricalJsonPayload_DeserializesWithDefaultContentKind()
    {
        // Simulate a 9.1-shape payload — ContentKind property does NOT exist in the JSON.
        string historicalJson = "{\"TenantId\":\"t-alpha\",\"ContentText\":\"hello world\"}";

        EmbeddingInput? result = JsonSerializer.Deserialize<EmbeddingInput>(historicalJson);

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe("t-alpha");
        result.ContentText.ShouldBe("hello world");
        result.ContentKind.ShouldBe(
            EmbeddingContentKind.Payload,
            customMessage: "Default value MUST apply when the historical JSON shape is deserialized — "
                + "paused workflow replay relies on this (Risk #17).");
    }

    [Fact]
    public void RoundTripJsonSerialization_PreservesContentKind()
    {
        EmbeddingInput original = new("t", "content", EmbeddingContentKind.NaturalLanguageDescription);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        EmbeddingInput? round = JsonSerializer.Deserialize<EmbeddingInput>(json, MemoriesJsonContext.Options);

        round.ShouldNotBeNull();
        round.TenantId.ShouldBe(original.TenantId);
        round.ContentText.ShouldBe(original.ContentText);
        round.ContentKind.ShouldBe(EmbeddingContentKind.NaturalLanguageDescription);
    }

    [Fact]
    public async Task ContentKind_PropagatesToEmbeddingApiCallsMetricTag()
    {
        List<KeyValuePair<string, object?>> captured = [];
        using MeterListener listener = new()
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == MemoriesMeter.Name
                    && instrument.Name == MemoriesMeter.EmbeddingApiCallsName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                captured.Add(tag);
            }
        });
        listener.Start();

        EmbeddingClient client = CreateMockEmbeddingClient(new float[768]);
        IActorProxyFactory factory = CreateMockActorProxyFactory(allowed: true);
        GenerateEmbeddingActivity activity = CreateActivity(client, factory);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EmbeddingInput("t", "content", EmbeddingContentKind.NaturalLanguageDescription));

        captured.ShouldContain(t =>
            t.Key == "content_kind"
            && (string)t.Value! == "naturalLanguageDescription");
        captured.ShouldContain(t => t.Key == "tenant_id" && (string)t.Value! == "t");
    }

    [Fact]
    public async Task PreNineTwoEmbeddingActivityHistory_ReplaysSuccessfully()
    {
        // Story 9.2 Review D4 / Risk #17 (unit variant) — simulates a paused 9.1 workflow whose
        // durable history carries the V1 positional EmbeddingInput shape ({TenantId, ContentText}).
        // Deterministic replay requires the 9.2 code to:
        //   (1) deserialize the historical JSON without exception,
        //   (2) default ContentKind to Payload,
        //   (3) drive GenerateEmbeddingActivity.RunAsync to completion — same outcome as a fresh call,
        //   (4) emit telemetry with content_kind=payload (not a surprise value).
        // A deterministic-replay violation would surface as a deserialization exception or a
        // divergent activity result. The unit variant validates all four invariants without
        // requiring a real durable-task state snapshot.
        string historicalJson = "{\"TenantId\":\"t-historical\",\"ContentText\":\"pre-9.2 event payload\"}";

        EmbeddingInput replayed = JsonSerializer.Deserialize<EmbeddingInput>(historicalJson)!;
        replayed.TenantId.ShouldBe("t-historical");
        replayed.ContentKind.ShouldBe(EmbeddingContentKind.Payload);

        List<string> capturedKinds = [];
        using MeterListener listener = new()
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == MemoriesMeter.Name
                    && instrument.Name == MemoriesMeter.EmbeddingApiCallsName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "content_kind" && tag.Value is string s)
                {
                    capturedKinds.Add(s);
                }
            }
        });
        listener.Start();

        float[] vector = new float[768];
        EmbeddingClient client = CreateMockEmbeddingClient(vector);
        IActorProxyFactory factory = CreateMockActorProxyFactory(allowed: true);
        GenerateEmbeddingActivity activity = CreateActivity(client, factory);

        EmbeddingResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            replayed);

        result.Vector.ShouldBe(vector);
        capturedKinds.ShouldContain(
            "payload",
            customMessage: "Replay of a 9.1-shape EmbeddingInput MUST route through the payload tag — "
                + "divergence here would indicate a workflow-replay determinism hazard.");
    }

    [Fact]
    public async Task PayloadKind_EmitsPayloadContentKindTag()
    {
        List<string> capturedKinds = [];
        using MeterListener listener = new()
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == MemoriesMeter.Name
                    && instrument.Name == MemoriesMeter.EmbeddingApiCallsName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "content_kind" && tag.Value is string s)
                {
                    capturedKinds.Add(s);
                }
            }
        });
        listener.Start();

        EmbeddingClient client = CreateMockEmbeddingClient(new float[768]);
        IActorProxyFactory factory = CreateMockActorProxyFactory(allowed: true);
        GenerateEmbeddingActivity activity = CreateActivity(client, factory);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EmbeddingInput("t", "content"));

        capturedKinds.ShouldContain("payload");
    }

    private static GenerateEmbeddingActivity CreateActivity(
        EmbeddingClient embeddingClient,
        IActorProxyFactory factory)
        => new(
            embeddingClient,
            factory,
            new FixedJitterSource(0),
            NullLogger<GenerateEmbeddingActivity>.Instance);

    private static EmbeddingClient CreateMockEmbeddingClient(float[] vectorToReturn)
    {
        EmbeddingClient client = Substitute.For<EmbeddingClient>(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            new ConfigurationBuilder().Build(),
            CreateHostEnvironment());
        client.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        client.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(vectorToReturn);
        return client;
    }

    private static IActorProxyFactory CreateMockActorProxyFactory(bool allowed)
    {
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(EmbeddingProviderDefaults.Google());
        rateLimiter.TryConsumeAsync().Returns(allowed);
        factory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        factory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);
        return factory;
    }

    private static IHostEnvironment CreateHostEnvironment()
    {
        IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns("Development");
        return hostEnvironment;
    }

    private sealed class FixedJitterSource(int fixedMilliseconds) : IJitterSource
    {
        public int NextMilliseconds(int maxExclusive) => fixedMilliseconds;
    }
}
