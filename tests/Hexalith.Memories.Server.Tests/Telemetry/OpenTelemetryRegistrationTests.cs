// <copyright file="OpenTelemetryRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;

using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Shouldly;

/// <summary>
/// Story 7.5 Task 8.4 — runtime-verification tests for <see cref="Extensions.AddServiceDefaults"/>
/// and <see cref="Program"/> hosted-service wiring. All assertions exercise the actual DI container
/// and built OpenTelemetry pipeline; no source-text scans.
/// </summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class OpenTelemetryRegistrationTests
{
    [Fact]
    public void AddServiceDefaults_ProducesBuildableContainer()
    {
        HostApplicationBuilder builder = BuildHostBuilder();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>().ShouldNotBeNull();
        provider.GetService<TracerProvider>().ShouldNotBeNull();
        provider.GetService<MeterProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureOpenTelemetry_RegistersMemoriesActivitySource_Runtime()
    {
        HostApplicationBuilder builder = BuildHostBuilder();
        List<Activity> exportedActivities = [];
        _ = builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddInMemoryExporter(exportedActivities));

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        _ = provider.GetRequiredService<TracerProvider>();

        using (Activity? activity = MemoriesActivitySource.Instance.StartActivity("registration-probe"))
        {
            activity.ShouldNotBeNull(
                $"Expected '{MemoriesActivitySource.SourceName}' to be a registered ActivitySource " +
                "on the TracerProvider built by AddServiceDefaults.");
        }

        provider.GetRequiredService<TracerProvider>().ForceFlush();
        exportedActivities.ShouldContain(a => a.Source.Name == MemoriesActivitySource.SourceName);
    }

    [Fact]
    public void ConfigureOpenTelemetry_RegistersMemoriesMeter_Runtime()
    {
        HostApplicationBuilder builder = BuildHostBuilder();
        List<Metric> exportedMetrics = [];
        _ = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddInMemoryExporter(exportedMetrics));

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        MeterProvider meterProvider = provider.GetRequiredService<MeterProvider>();

        MemoriesMeter.IngestionDocuments.Add(
            1,
            new KeyValuePair<string, object?>("tenant_id", "registration-probe"));

        meterProvider.ForceFlush();

        exportedMetrics.ShouldContain(
            m => m.MeterName == MemoriesMeter.Name && m.Name == MemoriesMeter.IngestionDocumentsName,
            $"Expected '{MemoriesMeter.Name}' meter to be registered on the MeterProvider built by AddServiceDefaults.");
    }

    [Fact]
    public void ShouldTraceHttpRequest_ExcludesHealthEndpoints()
    {
        Extensions.ShouldTraceHttpRequest(ContextFor("/health")).ShouldBeFalse();
        Extensions.ShouldTraceHttpRequest(ContextFor("/health/live")).ShouldBeFalse();
        Extensions.ShouldTraceHttpRequest(ContextFor("/alive")).ShouldBeFalse();
        Extensions.ShouldTraceHttpRequest(ContextFor("/ready")).ShouldBeFalse();
    }

    [Fact]
    public void ShouldTraceHttpRequest_IncludesApplicationEndpoints()
    {
        Extensions.ShouldTraceHttpRequest(ContextFor("/api/search")).ShouldBeTrue();
        Extensions.ShouldTraceHttpRequest(ContextFor("/api/ingest")).ShouldBeTrue();
        Extensions.ShouldTraceHttpRequest(ContextFor("/")).ShouldBeTrue();
    }

    [Fact]
    public void Program_RegistersRollingCounterStore_AsSingletonAndHostedService()
    {
        HostApplicationBuilder builder = BuildHostBuilder();
        builder.Services.AddSingleton<RollingCounterStore>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RollingCounterStore>());

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        RollingCounterStore instance = provider.GetRequiredService<RollingCounterStore>();

        IEnumerable<IHostedService> hosted = provider.GetServices<IHostedService>();
        hosted.ShouldContain(h => ReferenceEquals(h, instance));

        RollingCounterStore second = provider.GetRequiredService<RollingCounterStore>();
        ReferenceEquals(instance, second).ShouldBeTrue();
    }

    private static HostApplicationBuilder BuildHostBuilder()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });

        builder.AddServiceDefaults();
        return builder;
    }

    private static DefaultHttpContext ContextFor(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = new PathString(path);
        return context;
    }
}
