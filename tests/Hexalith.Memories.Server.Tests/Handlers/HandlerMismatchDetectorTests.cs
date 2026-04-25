// <copyright file="HandlerMismatchDetectorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Handlers;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public sealed class HandlerMismatchDetectorTests
{
    [Fact]
    public async Task EmptyObservedTypes_WithRoutedEntry_EmitsStaleHandlerInfo_NotWarning()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events", "acme" } },
        };
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(Array.Empty<ObservedEventType>()));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Count.ShouldBe(1);
        HandlerMismatch mismatch = report.Mismatches[0];
        mismatch.Category.ShouldBe(HandlerMismatchCategory.StaleHandler);
        mismatch.Severity.ShouldBe(HandlerMismatchSeverity.Info);
        mismatch.Subject.ShouldBe("acme.events");
        mismatch.Suggestion.ShouldContain("low-volume publishers may legitimately go silent");
        mismatch.Suggestion.ShouldContain("https://docs.hexalith.dev/memories/runbooks/handler-stale-handler");
    }

    [Fact]
    public async Task MultipleVersionsSameStem_EmitsVersionMismatchWarning()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("events", "ClaimSubmittedV2", Count: 5, now),
                new ObservedEventType("events", "ClaimSubmittedV3", Count: 2, now),
            }));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        HandlerMismatch? versionMismatch = report.Mismatches
            .FirstOrDefault(m => m.Category == HandlerMismatchCategory.VersionMismatch);
        versionMismatch.ShouldNotBeNull();
        versionMismatch!.Subject.ShouldBe("ClaimSubmitted");
        versionMismatch.Severity.ShouldBe(HandlerMismatchSeverity.Warning);
        versionMismatch.Suggestion.ShouldContain("review whether all versions are intentional");
    }

    [Fact]
    public async Task SingleVersionOnly_EmitsNoVersionMismatch()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("events", "ClaimSubmittedV2", Count: 5, now),
            }));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.VersionMismatch).ShouldBeFalse();
    }

    [Fact]
    public async Task VeryLongEventType_ShouldBeSkippedFromVersionMismatch()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events", "acme" } },
        };
        string longType = new string('a', 300) + "V2";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("events", longType, Count: 1, now),
                new ObservedEventType("events", longType + "V3", Count: 1, now),
            }));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        // No VersionMismatch emitted — the ReDoS guard skipped both oversized types.
        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.VersionMismatch).ShouldBeFalse();
    }

    [Fact]
    public async Task ObservedTypeWithoutRoutedAggregate_EmitsUnhandledEventTypeWarning()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events/Policies", "acme" } }, // routed aggregate = "Policies"
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 1, now),
            }));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        HandlerMismatch? unhandled = report.Mismatches
            .FirstOrDefault(m => m.Category == HandlerMismatchCategory.UnhandledEventType);
        unhandled.ShouldNotBeNull();
        unhandled!.Severity.ShouldBe(HandlerMismatchSeverity.Warning);
        unhandled.Subject.ShouldBe("Claims/ClaimSubmittedV2");
        unhandled.Suggestion.ShouldContain("SourceToTenantMap");
    }

    [Fact]
    public async Task Summary_ShouldBePopulatedEvenOnEmptyMismatches()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events/Claims", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 3, now),
            }));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Summary.RoutesConfigured.ShouldBe(1);
        report.Summary.ObservationsChecked.ShouldBe(1);
        report.Mismatches.ShouldBeEmpty(); // healthy: single version, aggregate matches routed prefix
    }

    [Fact]
    public async Task VersionMismatch_ExtractsStemFromTerminalSegment()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events/Claims", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("Claims", "MyApp.Claims.ClaimSubmittedV2", Count: 1, now),
                new ObservedEventType("Claims", "MyApp.Claims.ClaimSubmittedV3", Count: 1, now),
            }));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        HandlerMismatch version = report.Mismatches.Single(m => m.Category == HandlerMismatchCategory.VersionMismatch);
        version.Subject.ShouldBe("ClaimSubmitted"); // stem is from terminal segment, NOT the full FQN
    }

    [Fact]
    public async Task VersionsOnDifferentAggregates_ShouldNotProduceCrossAggregateMismatch()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap =
            {
                { "acme.events/Claims", "acme" },
                { "acme.events/Policies", "acme" },
            },
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 1, now),
                new ObservedEventType("Policies", "ClaimSubmittedV3", Count: 1, now),
            }));

        HandlerMismatchDetector detector = BuildDetector(routing, store);
        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.VersionMismatch).ShouldBeFalse();
    }

    private static HandlerMismatchDetector BuildDetector(
        TenantEventRoutingOptions routing,
        IObservedEventTypeStore store)
    {
        IOptionsMonitor<TenantEventRoutingOptions> monitor = Substitute.For<IOptionsMonitor<TenantEventRoutingOptions>>();
        monitor.CurrentValue.Returns(routing);
        return new HandlerMismatchDetector(
            monitor,
            store,
            TimeProvider.System,
            NullLogger<HandlerMismatchDetector>.Instance);
    }
}
