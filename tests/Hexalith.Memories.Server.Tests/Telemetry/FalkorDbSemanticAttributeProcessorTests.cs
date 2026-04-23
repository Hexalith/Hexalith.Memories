// <copyright file="FalkorDbSemanticAttributeProcessorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Diagnostics;
using System.Linq;

using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.ServiceDefaults.Telemetry;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using OpenTelemetry;
using OpenTelemetry.Trace;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.5 Task 2.7 Path A — pins the <see cref="FalkorDbSemanticAttributeProcessor"/> behavior
/// shipped to close the <c>db.system=redis</c> semantic-conventions debt. Tests cover
/// rewrite-hit, alias-hit, rewrite-miss, non-Redis-source skip, and exporter-order via a
/// functional export assertion.
/// </summary>
[Trait("Category", "Unit")]
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class FalkorDbSemanticAttributeProcessorTests
{
    [Fact]
    public void OnEnd_RewriteHit_WhenServerAddressMatchesFalkorDbHostname()
    {
        // Synthetic Activity from the Redis OTEL source tagged with server.address=falkordb.
        // Processor MUST rewrite db.system + db.system.name to "falkordb".
        using var source = new ActivitySource(FalkorDbSemanticAttributeProcessor.RedisSourceName);
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = source.StartActivity("GRAPH.QUERY");
        activity.ShouldNotBeNull();
        _ = activity.SetTag("server.address", FalkorDbSemanticAttributeProcessor.DefaultFalkorDbHostname);
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemTag, "redis");
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemNameTag, "redis");

        var processor = new FalkorDbSemanticAttributeProcessor();
        processor.OnEnd(activity);

        activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemTag)
            .ShouldBe(FalkorDbSemanticAttributeProcessor.FalkorDbSystemValue);
        activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemNameTag)
            .ShouldBe(FalkorDbSemanticAttributeProcessor.FalkorDbSystemValue);
    }

    [Fact]
    public void OnEnd_RewriteHit_WhenConfiguredAliasMatchesNetPeerName()
    {
        using var source = new ActivitySource(FalkorDbSemanticAttributeProcessor.RedisSourceName);
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = source.StartActivity("GRAPH.QUERY");
        activity.ShouldNotBeNull();
        _ = activity.SetTag("net.peer.name", "graph-cache.internal");
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemTag, "redis");
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemNameTag, "redis");

        var processor = new FalkorDbSemanticAttributeProcessor([
            FalkorDbSemanticAttributeProcessor.DefaultFalkorDbHostname,
            "graph-cache.internal",
        ]);
        processor.OnEnd(activity);

        activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemTag)
            .ShouldBe(FalkorDbSemanticAttributeProcessor.FalkorDbSystemValue);
        activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemNameTag)
            .ShouldBe(FalkorDbSemanticAttributeProcessor.FalkorDbSystemValue);
    }

    [Fact]
    public void OnEnd_RewriteMiss_WhenServerAddressIsRedis()
    {
        // Synthetic Activity from the Redis OTEL source tagged with server.address=redis.
        // Processor MUST leave db.system=redis unchanged.
        using var source = new ActivitySource(FalkorDbSemanticAttributeProcessor.RedisSourceName);
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = source.StartActivity("HMGET");
        activity.ShouldNotBeNull();
        _ = activity.SetTag("server.address", "redis");
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemTag, "redis");
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemNameTag, "redis");

        var processor = new FalkorDbSemanticAttributeProcessor();
        processor.OnEnd(activity);

        activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemTag).ShouldBe("redis");
        activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemNameTag).ShouldBe("redis");
    }

    [Fact]
    public void OnEnd_NonRedisSource_Skipped()
    {
        // Activities from other sources (Memories, AspNetCore) MUST NOT have their tags touched
        // even if they coincidentally carry a server.address tag. Rewrite applies only to the
        // Redis OTEL source.
        using var source = new ActivitySource("Hexalith.Memories");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = source.StartActivity("memories.search");
        activity.ShouldNotBeNull();
        _ = activity.SetTag("server.address", FalkorDbSemanticAttributeProcessor.DefaultFalkorDbHostname);
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemTag, "redis");

        var processor = new FalkorDbSemanticAttributeProcessor();
        processor.OnEnd(activity);

        // Unchanged because the source name does not match.
        activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemTag).ShouldBe("redis");
    }

    [Fact]
    public void AddServiceDefaults_ExportsRewrittenFalkorDbTags_ToExporter()
    {
        // Processor-order guard: if a span exported through the AddServiceDefaults pipeline
        // already carries db.system=falkordb, then FalkorDbSemanticAttributeProcessor executed
        // before the exporter. This is more robust than reflecting internal processor types.
        var exported = new System.Collections.Generic.List<ExportedActivitySnapshot>();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.RedisConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.FalkorDbConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.AddServiceDefaults();
        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
            tracing.AddProcessor(new SimpleActivityExportProcessor(new CapturingActivityExporter(exported))));

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        TracerProvider tracerProvider = provider.GetRequiredService<TracerProvider>();
        using var source = new ActivitySource(FalkorDbSemanticAttributeProcessor.RedisSourceName);
        using Activity? activity = source.StartActivity("GRAPH.QUERY");
        activity.ShouldNotBeNull();
        _ = activity.SetTag("server.address", FalkorDbSemanticAttributeProcessor.DefaultFalkorDbHostname);
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemTag, "redis");
        _ = activity.SetTag(FalkorDbSemanticAttributeProcessor.DbSystemNameTag, "redis");
        activity.Dispose();

        tracerProvider.ForceFlush();

        ExportedActivitySnapshot exportedSpan = exported.ShouldHaveSingleItem(
            "Expected the custom exporter to see exactly one synthetic Redis span through the AddServiceDefaults pipeline.");
        exportedSpan.DbSystem.ShouldBe(
            FalkorDbSemanticAttributeProcessor.FalkorDbSystemValue,
            "Expected the exporter to observe the rewritten db.system tag, which proves FalkorDbSemanticAttributeProcessor ran before export.");
        exportedSpan.DbSystemName.ShouldBe(FalkorDbSemanticAttributeProcessor.FalkorDbSystemValue);
    }

    private sealed record ExportedActivitySnapshot(string? DbSystem, string? DbSystemName);

    private sealed class CapturingActivityExporter(System.Collections.Generic.ICollection<ExportedActivitySnapshot> exported)
        : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (Activity activity in batch)
            {
                exported.Add(new ExportedActivitySnapshot(
                    activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemTag) as string,
                    activity.GetTagItem(FalkorDbSemanticAttributeProcessor.DbSystemNameTag) as string));
            }

            return ExportResult.Success;
        }
    }
}
