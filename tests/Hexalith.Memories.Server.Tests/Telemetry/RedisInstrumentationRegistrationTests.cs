// <copyright file="RedisInstrumentationRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Diagnostics;

using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.ServiceDefaults.Telemetry;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using OpenTelemetry;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Trace;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.5 Task 2.4 — Tier-2 registration tests pinning the Redis OTEL instrumentation wiring
/// shipped in <see cref="Extensions.ConfigureOpenTelemetry"/>. Runs without Docker and without
/// Aspire; asserts DI-guard eager-fail behavior, source subscription, and the shared
/// <see cref="StackExchangeRedisInstrumentationOptions"/> flush-interval invariant.
/// </summary>
[Trait("Category", "Unit")]
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class RedisInstrumentationRegistrationTests
{
    [Fact]
    public void TracerRegistration_IncludesRedisInstrumentationSource()
    {
        // AC #3: the built TracerProvider's ActivityListener surface returns AllData for a
        // synthetic ActivitySource whose name matches the Redis instrumentation source. Proves the
        // source is subscribed; does NOT prove the real Redis instrumentation emits spans (that is
        // AC #2's Tier-3 job).
        HostApplicationBuilder builder = BuildHostBuilderWithStubbedRedis();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        _ = provider.GetRequiredService<TracerProvider>();

        using var source = new ActivitySource(Extensions.IntegrationActivityProcessor.RedisSourceName);
        using Activity? activity = source.StartActivity("registration-probe");

        activity.ShouldNotBeNull(
            $"Expected '{Extensions.IntegrationActivityProcessor.RedisSourceName}' to be a subscribed " +
            "ActivitySource on the TracerProvider built by AddServiceDefaults.");
        activity.IsAllDataRequested.ShouldBeTrue(
            "Subscribed source must receive AllData samples.");
    }

    [Fact]
    public void TracerRegistration_ResolvesBothKeyedRedisConnections()
    {
        // AC #3 + Task 2.4(b): both keys resolve without throw. Building the TracerProvider with
        // BOTH 'redis' and 'falkordb' keyed IConnectionMultiplexer stubs registered completes
        // ForceFlush() without InvalidOperationException.
        HostApplicationBuilder builder = BuildHostBuilderWithStubbedRedis();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        TracerProvider tracerProvider = provider.GetRequiredService<TracerProvider>();

        // ForceFlush exercises the full instrumentation pipeline end-to-end without requiring
        // real Redis connections — the AddConnection calls in ConfigureRedisInstrumentation
        // happened during TracerProvider resolution above.
        Should.NotThrow(() => tracerProvider.ForceFlush(), "Both keyed connections must resolve cleanly.");
    }

    [Fact]
    public void TracerRegistration_MissingRedisKey_FailsEagerlyWithDescriptiveMessage()
    {
        // Task 2.4(c): upstream is silent-null on missing key. The DI-guard in
        // Extensions.AddRedisKeyedConnectionGuard is the only correct eager-fail strategy.
        // Register ONLY the 'falkordb' key (omit 'redis'), then assert TracerProvider build
        // throws InvalidOperationException whose Message names BOTH "redis" AND
        // "IConnectionMultiplexer".
        HostApplicationBuilder builder = CreateBuilder();
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.FalkorDbConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.AddServiceDefaults();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => provider.GetRequiredService<TracerProvider>());

        ex.Message.ShouldContain(Extensions.RedisConnectionKey);
        ex.Message.ShouldContain("IConnectionMultiplexer");
    }

    [Fact]
    public void TracerRegistration_MissingFalkorDbKey_FailsEagerlyWithDescriptiveMessage()
    {
        // Task 2.4(c) companion: register ONLY the 'redis' key; TracerProvider build must throw
        // referencing 'falkordb'.
        HostApplicationBuilder builder = CreateBuilder();
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.RedisConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.AddServiceDefaults();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => provider.GetRequiredService<TracerProvider>());

        ex.Message.ShouldContain(Extensions.FalkorDbConnectionKey);
        ex.Message.ShouldContain("IConnectionMultiplexer");
    }

    [Fact]
    public void TracerRegistration_MissingBothKeys_FailsEagerlyFromAddServiceDefaultsEntry()
    {
        // Task 2.4(f): integration test exercising the canonical public entry point
        // (AddServiceDefaults → Build) with NEITHER key registered. Guards that a future refactor
        // splitting the DI-guard into a separate extension cannot leave it unreachable.
        HostApplicationBuilder builder = CreateBuilder();
        builder.AddServiceDefaults();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => provider.GetRequiredService<TracerProvider>());

        ex.Message.ShouldContain("Keyed IConnectionMultiplexer");
        // Either the 'redis' or 'falkordb' key is surfaced first depending on registration
        // order in the TracerProvider build sequence — assert at least one is named.
        bool namesEitherKey = ex.Message.Contains(Extensions.RedisConnectionKey, StringComparison.Ordinal)
            || ex.Message.Contains(Extensions.FalkorDbConnectionKey, StringComparison.Ordinal);
        namesEitherKey.ShouldBeTrue(
            $"Exception message must name at least one missing key; got: {ex.Message}");
    }

    [Fact]
    public void TracerRegistration_RedisInstrumentationDisabled_AllowsServicesWithoutRedisConnections()
    {
        HostApplicationBuilder builder = CreateBuilder();
        builder.AddServiceDefaults(configureRedisInstrumentation: false);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<TracerProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureRedisInstrumentation_AppliesIdenticalFlushIntervalPerKey()
    {
        // Task 2.4(e): we don't accidentally diverge FlushInterval delegates per key (internal
        // invariant). Upstream stores FlushInterval on a shared singleton; this test pins that our
        // own ConfigureRedisInstrumentation delegate resolves to the same interval when invoked
        // against fresh StackExchangeRedisInstrumentationOptions instances. Guards against a
        // future refactor that wires distinct per-key delegates.
        StackExchangeRedisInstrumentationOptions redisOptions = new();
        StackExchangeRedisInstrumentationOptions falkorOptions = new();

        Extensions.ConfigureRedisInstrumentation(redisOptions);
        Extensions.ConfigureRedisInstrumentation(falkorOptions);

        redisOptions.FlushInterval.ShouldBe(Extensions.RedisInstrumentationFlushInterval);
        falkorOptions.FlushInterval.ShouldBe(Extensions.RedisInstrumentationFlushInterval);
        redisOptions.FlushInterval.ShouldBe(falkorOptions.FlushInterval);

        // Also pin the exact value vs the ADR-8.5-001 (e) policy: 100ms in all environments.
        redisOptions.FlushInterval.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void FlushInterval_Policy_IsDocumentedHundredMilliseconds()
    {
        // Pins ADR-8.5-001 (e) — the 100ms value is the policy shipped in both production and
        // test. No env-gated override exists.
        Extensions.RedisInstrumentationFlushInterval.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    private static HostApplicationBuilder BuildHostBuilderWithStubbedRedis()
    {
        HostApplicationBuilder builder = CreateBuilder();
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.RedisConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.FalkorDbConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.AddServiceDefaults();
        return builder;
    }

    private static HostApplicationBuilder CreateBuilder()
        => Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });
}
