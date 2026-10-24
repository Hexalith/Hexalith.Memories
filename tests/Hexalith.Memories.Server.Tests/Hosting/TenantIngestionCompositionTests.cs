namespace Hexalith.Memories.Server.Tests.Hosting;

using System.Reflection;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

/// <summary>Protects existing binary constructor shapes and production selection of the fenced adapters.</summary>
public sealed class TenantIngestionCompositionTests
{
    [Fact]
    public void ExistingPublicConstructorsRemainAndRegisteredActivitiesSelectTenantAwareAdapters()
    {
        typeof(IndexGraphActivity).GetConstructor([typeof(IConnectionMultiplexer), typeof(IGraphQueryBuilder), typeof(ILogger<IndexGraphActivity>), typeof(IWorkflowPayloadStore)])
            .ShouldNotBeNull();
        typeof(DeleteTenantDataKeysActivity).GetConstructor([typeof(IConnectionMultiplexer), typeof(IAggregateCaseMappingStore),
            typeof(IObservedEventTypeStore), typeof(IHostApplicationLifetime), typeof(ILogger<DeleteTenantDataKeysActivity>)])
            .ShouldNotBeNull();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var payloads = Substitute.For<IWorkflowPayloadStore>();
        var services = new ServiceCollection(); services.AddLogging();
        services.AddKeyedSingleton("redis", redis); services.AddKeyedSingleton("falkordb", Substitute.For<IConnectionMultiplexer>());
        services.AddSingleton<IGraphQueryBuilder, GraphQueryBuilder>(); services.AddSingleton(payloads);
        services.AddSingleton(Substitute.For<IAggregateCaseMappingStore>()); services.AddSingleton(Substitute.For<IObservedEventTypeStore>());
        services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
        services.AddTransient<IndexGraphActivity>(); services.AddTransient<DeleteTenantDataKeysActivity>();
        using ServiceProvider provider = services.BuildServiceProvider();
        typeof(IndexGraphActivity).GetField("_redis", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider.GetRequiredService<IndexGraphActivity>()).ShouldBeSameAs(redis);
        typeof(DeleteTenantDataKeysActivity).GetField("_payloadStore", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider.GetRequiredService<DeleteTenantDataKeysActivity>()).ShouldBeSameAs(payloads);
    }
}
