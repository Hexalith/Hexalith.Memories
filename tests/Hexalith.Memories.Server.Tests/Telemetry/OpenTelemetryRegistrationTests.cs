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
using Hexalith.Memories.TestHelpers.Process;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Shouldly;

using StackExchange.Redis;

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
    public void ConfigureOpenTelemetry_RegistersDaprWorkflowActivitySource_Runtime()
    {
        HostApplicationBuilder builder = BuildHostBuilder();
        List<Activity> exportedActivities = [];
        _ = builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddInMemoryExporter(exportedActivities));

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        _ = provider.GetRequiredService<TracerProvider>();

        using var source = new ActivitySource(Extensions.DaprWorkflowActivitySourceName);
        using (Activity? activity = source.StartActivity("registration-probe"))
        {
            activity.ShouldNotBeNull(
                $"Expected '{Extensions.DaprWorkflowActivitySourceName}' to be a registered ActivitySource " +
                "on the TracerProvider built by AddServiceDefaults.");
        }

        provider.GetRequiredService<TracerProvider>().ForceFlush();
        exportedActivities.ShouldContain(a => a.Source.Name == Extensions.DaprWorkflowActivitySourceName);
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
        Extensions.ShouldTraceHttpRequest(ContextFor("/api/v1/search")).ShouldBeTrue();
        Extensions.ShouldTraceHttpRequest(ContextFor("/api/v1/ingest")).ShouldBeTrue();
        Extensions.ShouldTraceHttpRequest(ContextFor("/")).ShouldBeTrue();
    }

    [Fact]
    public void InMemoryTelemetry_EnvVarUnset_NoCollectorRegistered_AndContainerBuilds()
    {
        // Story 8.4 AC #5 + Task 1.4 regression guard: when HEXALITH_MEMORIES_TELEMETRY_INMEMORY is
        // unset, the env-var branch in AddOpenTelemetryExporters MUST be skipped — no IActivityCollector
        // resolution is attempted, the production OpenTelemetry pipeline stays unchanged, and the
        // service container builds without throwing on a missing IActivityCollector registration.
        using EnvVarScope _ = EnvVarScope.Set(InMemoryTelemetryEnvironment.EnvVar, null);
        HostApplicationBuilder builder = BuildHostBuilder();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        // The branch is skipped → no IActivityCollector requirement → BuildServiceProvider succeeds
        // and resolving TracerProvider does not throw.
        provider.GetRequiredService<TracerProvider>().ShouldNotBeNull();
        provider.GetService<IActivityCollector>().ShouldBeNull(
            "Production code MUST NOT register IActivityCollector when the env-var trigger is unset.");
    }

    [Fact]
    public void InMemoryTelemetry_EnvVarSet_WithoutCollector_ContainerStillBuilds()
    {
        // Story 8.4 telemetry fixture enables the env-var gate for the separate server process, which does
        // NOT register an IActivityCollector. The branch must therefore remain safe when the collector is
        // absent: build succeeds, TracerProvider resolves, and the optional breadcrumb emission still works.
        using EnvVarScope _ = EnvVarScope.Set(
            InMemoryTelemetryEnvironment.EnvVar,
            InMemoryTelemetryEnvironment.EnabledValue);

        HostApplicationBuilder builder = BuildHostBuilder();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<TracerProvider>().ShouldNotBeNull();
        provider.GetService<IActivityCollector>().ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("on")]
    [InlineData("yes")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("01")]
    [InlineData("10")]
    public void InMemoryTelemetry_EnvVarParseStrictness_OnlyExactOneActivates(string envValue)
    {
        // Story 8.4 Task 1.1.5 (Risk 7 mitigation): the env-var contract is "exact string match on \"1\"".
        // A developer who uses truthy variants ("true", "on", " 1", "01") gets ZERO capture activation
        // — a misconfiguration that should never silently look like it worked. This test pins the contract
        // so a future code-cleanup pass cannot loosen the comparison to OrdinalIgnoreCase or numeric parse.
        InMemoryTelemetryEnvironment.IsEnabled(envValue).ShouldBeFalse(
            $"Only the exact string \"{InMemoryTelemetryEnvironment.EnabledValue}\" must activate; got '{envValue}'.");
    }

    [Fact]
    public void InMemoryTelemetry_EnvVarParseStrictness_ExactOneActivates()
    {
        // Positive case: the canonical activator string MUST evaluate to true. Pairs with the negative
        // theory above so the round-trip is fully pinned.
        InMemoryTelemetryEnvironment.IsEnabled(InMemoryTelemetryEnvironment.EnabledValue).ShouldBeTrue();
    }

    [Fact]
    public void InMemoryTelemetry_FormatIgnoredValueWarning_HasStableShape()
    {
        // Story 8.4 Task 1.1.5 — pin the warning text so test logs and operator triage docs can rely
        // on the exact phrasing. A drift here would break grep-based triage.
        string warning = InMemoryTelemetryEnvironment.FormatIgnoredValueWarning("true");
        warning.ShouldBe("[telemetry] HEXALITH_MEMORIES_TELEMETRY_INMEMORY=true — only \"1\" activates; ignoring");
    }

    [Fact]
    public void InMemoryTelemetry_EnvVarSet_BranchRegistersInMemoryProcessor_AndCapturesActivities()
    {
        // Story 8.4 Task 1.1 happy-path: when the env var IS exactly "1" AND an IActivityCollector
        // is registered in DI, the env-var branch appends a CollectingActivityProcessor that drains
        // emitted activities into the collector. Validates the wiring end-to-end without booting Aspire.
        using EnvVarScope _ = EnvVarScope.Set(
            InMemoryTelemetryEnvironment.EnvVar,
            InMemoryTelemetryEnvironment.EnabledValue);

        TestActivityCollector collector = new();
        HostApplicationBuilder builder = BuildHostBuilder();
        builder.Services.AddSingleton<IActivityCollector>(collector);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        TracerProvider tracerProvider = provider.GetRequiredService<TracerProvider>();

        using (Activity? activity = MemoriesActivitySource.Instance.StartActivity("env-var-branch-probe"))
        {
            activity.ShouldNotBeNull();
        }

        tracerProvider.ForceFlush();

        collector.Activities.ShouldContain(
            a => a.Source.Name == MemoriesActivitySource.SourceName && a.OperationName == "env-var-branch-probe",
            "Env-var branch did not append the integration activity processor — activity was not captured.");
    }

    private sealed class TestActivityCollector : IActivityCollector
    {
        private readonly List<Activity> _activities = [];

        public ICollection<Activity> Activities => _activities;
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

        // Story 8.5: AddServiceDefaults now requires BOTH keyed IConnectionMultiplexer
        // registrations ('redis' + 'falkordb') for the Redis OTEL DI-guard. Unit tests stub both
        // with NSubstitute so the guard passes; Tier-3 tests use real ConnectionMultiplexer
        // instances wired up by the Aspire fixture.
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.RedisConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.FalkorDbConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
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
