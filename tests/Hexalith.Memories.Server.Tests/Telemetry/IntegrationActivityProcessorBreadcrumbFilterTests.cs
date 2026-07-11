// <copyright file="IntegrationActivityProcessorBreadcrumbFilterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;

using Shouldly;

/// <summary>
/// Story 8.5 Task 2.6 — orphan-guard coverage for the extended breadcrumb filter in
/// <see cref="Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb"/>.
/// Pins:
/// <list type="bullet">
///   <item><description>Redis activity under a Memories/AspNetCore ancestor → emitted.</description></item>
///   <item><description>Redis activity with no parent or no reachable Memories/AspNetCore ancestor → NOT emitted to stderr; DEBUG log with the drop reason is captured.</description></item>
///   <item><description>Depth-16 traversal boundary honored with reason <c>"depth_exceeded_16"</c>.</description></item>
/// </list>
/// </summary>
[Trait("Category", "Unit")]
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class IntegrationActivityProcessorBreadcrumbFilterTests
{
    private const string RedisSourceName = Extensions.IntegrationActivityProcessor.RedisSourceName;
    private const string AspNetCoreSourceName = Extensions.IntegrationActivityProcessor.AspNetCoreSourceName;

    [Fact]
    public void MemoriesSource_AlwaysEmits()
    {
        using ActivitySource src = new(MemoriesActivitySource.SourceName);
        using ActivityListener listener = Listener();
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = src.StartActivity("memories.search");
        activity.ShouldNotBeNull();

        Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb(activity).ShouldBeTrue();
    }

    [Fact]
    public void AspNetCoreServerKind_Emits()
    {
        using ActivitySource src = new(AspNetCoreSourceName);
        using ActivityListener listener = Listener();
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = src.StartActivity("GET /api/v1/search", ActivityKind.Server);
        activity.ShouldNotBeNull();

        Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb(activity).ShouldBeTrue();
    }

    [Fact]
    public void RedisActivity_UnderMemoriesParent_Emits()
    {
        using ActivityListener listener = Listener();
        ActivitySource.AddActivityListener(listener);

        using ActivitySource memoriesSrc = new(MemoriesActivitySource.SourceName);
        using Activity? parent = memoriesSrc.StartActivity("memories.search");
        parent.ShouldNotBeNull();

        using ActivitySource redisSrc = new(RedisSourceName);
        using Activity? redis = redisSrc.StartActivity("HMGET");
        redis.ShouldNotBeNull();
        redis.Parent.ShouldBe(parent);

        CapturingLogger logger = new();

        Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb(redis, logger).ShouldBeTrue();
    }

    [Fact]
    public void RedisActivity_OrphanNoParent_DropsAndLogsReason()
    {
        using ActivityListener listener = Listener();
        ActivitySource.AddActivityListener(listener);

        using ActivitySource redisSrc = new(RedisSourceName);
        using Activity? redis = redisSrc.StartActivity("PING");
        redis.ShouldNotBeNull();
        redis.Parent.ShouldBeNull();

        CapturingLogger logger = new();

        bool emitted = Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb(redis, logger);
        emitted.ShouldBeFalse();

        AssertLoggedDropReason(logger, expectedReason: "orphan_no_parent");
    }

    [Fact]
    public void RedisActivity_ParentChainNotReachable_DropsAndLogsReason()
    {
        using ActivityListener listener = Listener();
        ActivitySource.AddActivityListener(listener);

        // Parent is neither Memories nor AspNetCore — the Redis span cannot reach a relevant
        // ancestor.
        using ActivitySource unrelatedSrc = new("Some.Unrelated.Source");
        using Activity? unrelatedParent = unrelatedSrc.StartActivity("housekeeping");
        unrelatedParent.ShouldNotBeNull();

        using ActivitySource redisSrc = new(RedisSourceName);
        using Activity? redis = redisSrc.StartActivity("INFO");
        redis.ShouldNotBeNull();
        redis.Parent.ShouldBe(unrelatedParent);

        CapturingLogger logger = new();

        bool emitted = Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb(redis, logger);
        emitted.ShouldBeFalse();

        AssertLoggedDropReason(logger, expectedReason: "parent_chain_not_reachable");
    }

    [Fact]
    public void RedisActivity_DepthExceeded_DropsWithDepthExceededReason()
    {
        using ActivityListener listener = Listener();
        ActivitySource.AddActivityListener(listener);

        // Build a deep chain of unrelated-source activities so the Redis span's parent walk
        // exceeds the 16-depth ceiling before finding a matching ancestor.
        using ActivitySource unrelatedSrc = new("Some.Unrelated.Source");
        List<Activity> chain = [];
        try
        {
            for (int i = 0; i < Extensions.IntegrationActivityProcessor.MaxParentChainDepth + 2; i++)
            {
                Activity? link = unrelatedSrc.StartActivity($"link-{i}");
                link.ShouldNotBeNull();
                chain.Add(link);
            }

            using ActivitySource redisSrc = new(RedisSourceName);
            using Activity? redis = redisSrc.StartActivity("PING");
            redis.ShouldNotBeNull();

            CapturingLogger logger = new();

            bool emitted = Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb(redis, logger);
            emitted.ShouldBeFalse();

            AssertLoggedDropReason(logger, expectedReason: "depth_exceeded_16");
        }
        finally
        {
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                chain[i].Dispose();
            }
        }
    }

    [Fact]
    public void UnrelatedSource_NeverEmits()
    {
        using ActivitySource src = new("Some.Unrelated.Source");
        using ActivityListener listener = Listener();
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = src.StartActivity("unrelated");
        activity.ShouldNotBeNull();

        Extensions.IntegrationActivityProcessor.ShouldEmitActivityBreadcrumb(activity).ShouldBeFalse();
    }

    private static ActivityListener Listener()
        => new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };

    private static void AssertLoggedDropReason(
        CapturingLogger logger,
        string expectedReason)
    {
        bool seen = logger.Entries.Any(e =>
            e.Level == LogLevel.Debug
            && e.Message.Contains(expectedReason, StringComparison.Ordinal));
        seen.ShouldBeTrue(
            $"Expected a DEBUG log entry containing reason='{expectedReason}' but captured: "
            + string.Join(" | ", logger.Entries.Select(e => $"[{e.Level}] {e.Message}")));
    }

    /// <summary>
    /// Hand-rolled capturing logger. <see cref="Extensions.IntegrationActivityProcessor"/> is
    /// internal, so NSubstitute's Castle.DynamicProxy cannot generate an
    /// <c>ILogger&lt;IntegrationActivityProcessor&gt;</c> proxy without adding Castle's strong-name
    /// to <c>[InternalsVisibleTo]</c>. A hand-rolled stub sidesteps that.
    /// </summary>
    private sealed class CapturingLogger : ILogger<Extensions.IntegrationActivityProcessor>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            _entries.Add((logLevel, message));
        }
    }
}
