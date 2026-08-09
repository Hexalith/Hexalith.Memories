// <copyright file="EventIngestionWorkflowSchedulerAdapterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

using System.Collections.Generic;
using System.Linq;

public sealed class EventIngestionWorkflowSchedulerAdapterTests
{
    [Fact]
    public async Task AddMemoriesServerServices_ResolvesServerAdapterAndDelegatesExactArguments()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        IIngestionWorkflowScheduler inner = Substitute.For<IIngestionWorkflowScheduler>();
        builder.AddMemoriesServerServices();
        builder.Services.RemoveAll<IIngestionWorkflowScheduler>();
        builder.Services.AddSingleton(inner);
        using CancellationTokenSource cancellationSource = new();
        CancellationToken cancellationToken = cancellationSource.Token;
        IngestionInput input = new()
        {
            TenantId = "tenant-event",
            CaseId = "case-event",
            SourceUri = "event://source/42",
            ContentBytes = [1, 2, 3],
            ContentType = "application/json",
            SourceType = SourceType.Event,
            IngestedBy = "eventstore",
        };
        inner.ScheduleAsync("event-instance", input, cancellationToken).Returns("scheduled-event-instance");
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        IEventIngestionWorkflowScheduler resolved =
            provider.GetRequiredService<IEventIngestionWorkflowScheduler>();
        string result = await resolved.ScheduleAsync("event-instance", input, cancellationToken);

        resolved.ShouldBeOfType<EventIngestionWorkflowSchedulerAdapter>();
        result.ShouldBe("scheduled-event-instance");
        await inner.Received(1).ScheduleAsync(
            "event-instance",
            Arg.Is<IngestionInput>(candidate => ReferenceEquals(candidate, input)),
            cancellationToken);
    }

    [Fact]
    public void AddMemoriesServerServices_RegistersKeyedRedisConnectionsAndDaprStores()
    {
        // review P8 / patch #10: composition asserts concrete Dapr store ImplementationType.
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        builder.AddMemoriesServerServices();

        bool redisRegistered = builder.Services.Any(d =>
            d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer)
            && string.Equals(d.ServiceKey as string, "redis", StringComparison.Ordinal));
        bool falkorRegistered = builder.Services.Any(d =>
            d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer)
            && string.Equals(d.ServiceKey as string, "falkordb", StringComparison.Ordinal));
        ServiceDescriptor? mapping = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IAggregateCaseMappingStore));
        ServiceDescriptor? observed = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IObservedEventTypeStore));

        redisRegistered.ShouldBeTrue();
        falkorRegistered.ShouldBeTrue();
        mapping.ShouldNotBeNull();
        observed.ShouldNotBeNull();
        mapping!.ImplementationType.ShouldBe(typeof(DaprAggregateCaseMappingStore));
        observed!.ImplementationType.ShouldBe(typeof(DaprObservedEventTypeStore));
    }

    [Fact]
    public void AddMemoriesServerServices_WithHostEmbeddingProvidersConfig_SeedsCurrentOptionsAndOllama()
    {
        // review patch #9: host configuration (not only in-test Configure) must drive the static seam.
        EmbeddingProviderDefaultsOptions previous = EmbeddingProviderDefaults.CurrentOptions;
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmbeddingProviders:Ollama:BaseUrl"] = "https://ollama.host-config.test",
                ["EmbeddingProviders:Ollama:OidcTokenEndpoint"] = "https://idp.host-config.test/token",
                ["EmbeddingProviders:Ollama:OidcClientId"] = "host-config-client",
                ["EmbeddingProviders:Ollama:OidcScope"] = "host-config-scope",
                ["EmbeddingProviders:Google:ApiBaseUrl"] = "https://generativelanguage.host-config.test/",
            });
            builder.AddServiceDefaults(configureRedisInstrumentation: false);
            builder.AddMemoriesServerServices();

            EmbeddingProviderDefaults.CurrentOptions.Ollama.BaseUrl.ShouldBe("https://ollama.host-config.test");
            EmbeddingProviderDefaults.CurrentOptions.Ollama.OidcClientId.ShouldBe("host-config-client");
            EmbeddingProviderDefaults.CurrentOptions.Ollama.OidcScope.ShouldBe("host-config-scope");
            TenantEmbeddingConfig ollama = EmbeddingProviderDefaults.Ollama();
            ollama.BaseUrl.ShouldBe("https://ollama.host-config.test");
            ollama.OidcClientId.ShouldBe("host-config-client");
            ollama.OidcScope.ShouldBe("host-config-scope");
        }
        finally
        {
            EmbeddingProviderDefaults.Configure(previous);
        }
    }
}
