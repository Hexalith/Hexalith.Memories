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

using Microsoft.Extensions.Logging;
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

    [Fact]
    public async Task RoutedEntryWithoutProjectionBinding_WhenRegistryAuthoritative_EmitsProjectionBindingMissingWarning()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "Enterprise/Claims/", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("Claims", "MyApp.Claims.ClaimSubmittedV2", Count: 1, now),
        ]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        HandlerMismatch mismatch = report.Mismatches.Single(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing);
        mismatch.Severity.ShouldBe(HandlerMismatchSeverity.Warning);
        mismatch.Subject.ShouldBe("acme/enterprise/claims/claimsubmitted");
        mismatch.Context.ShouldContain("configured source 'Enterprise/Claims/'");
        mismatch.Context.ShouldContain("expected projection binding key 'acme/enterprise/claims/claimsubmitted'");
        mismatch.Suggestion.ShouldContain("register an authoritative projection binding");
        mismatch.Suggestion.ShouldContain("handler-projection-binding-missing");
    }

    [Fact]
    public async Task RoutedEntryWithMatchingProjectionBinding_WhenRegistryAuthoritative_EmitsNoProjectionBindingWarning()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "/enterprise/CLAIMS/", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("Claims", "MyApp.Claims.ClaimSubmittedV2", Count: 1, now),
        ]);
        ProjectionBinding binding = new(
            TenantId: "acme",
            SourcePrefix: "enterprise/claims",
            AggregateType: "claims",
            ProjectionName: "ClaimsReadModel",
            ProjectionType: "Acme.ClaimsProjection",
            SupportedEventTypePatterns: ["claimsubmitted*"]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [binding])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
    }

    [Fact]
    public async Task BindingForAnotherTenant_DoesNotSatisfySelectedTenant_AndDoesNotLeakProjectionIdentity()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 1, now),
        ]);
        ProjectionBinding foreignBinding = new(
            TenantId: "other",
            SourcePrefix: "enterprise/claims",
            AggregateType: "Claims",
            ProjectionName: "OtherTenantSecretProjection",
            ProjectionType: "Other.SecretProjection",
            SupportedEventTypePatterns: ["ClaimSubmittedV2"]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [foreignBinding])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        HandlerMismatch mismatch = report.Mismatches.Single(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing);
        mismatch.Context.ShouldContain("tenant 'acme'");
        mismatch.Context.ShouldNotContain("OtherTenantSecretProjection");
        mismatch.Context.ShouldNotContain("Other.SecretProjection");
        mismatch.Suggestion.ShouldNotContain("OtherTenantSecretProjection");
    }

    [Fact]
    public async Task ProjectionBindingWithoutConfiguredRoute_EmitsNoProjectionBindingWarning()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
        };
        IObservedEventTypeStore store = BuildObservedStore([]);
        ProjectionBinding binding = new(
            TenantId: "acme",
            SourcePrefix: "enterprise/claims",
            AggregateType: "Claims",
            ProjectionName: "ClaimsReadModel",
            ProjectionType: "Acme.ClaimsProjection",
            SupportedEventTypePatterns: ["*"]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [binding])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
    }

    [Theory]
    [InlineData(ProjectionBindingRegistryAuthority.Unknown)]
    [InlineData(ProjectionBindingRegistryAuthority.NonAuthoritative)]
    [InlineData(ProjectionBindingRegistryAuthority.Unavailable)]
    public async Task NonAuthoritativeProjectionRegistryPosture_DoesNotEmitProjectionBindingWarning(
        ProjectionBindingRegistryAuthority authority)
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        IObservedEventTypeStore store = BuildObservedStore([]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: authority,
                Bindings: [])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectionBindingProviderFailure_DoesNotSuppressExistingDiagnostics()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/policies", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 1, now),
        ]);

        HandlerMismatchDetector detector = BuildDetector(routing, store, new ThrowingProjectionBindingProvider());

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.UnhandledEventType).ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectionBindingMissing_CoexistsWithVersionMismatchAndUnhandledEventType()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 1, now),
            new ObservedEventType("Claims", "ClaimSubmittedV3", Count: 1, now),
            new ObservedEventType("Policies", "PolicyCreatedV1", Count: 1, now),
        ]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeTrue();
        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.VersionMismatch).ShouldBeTrue();
        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.UnhandledEventType).ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectionBindingMissing_MultipleRoutes_AreEmittedInDeterministicOrder()
    {
        // Story 16.1 review F16 — operator-facing diagnostics must be stable so test/CLI consumers can rely on order.
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap =
            {
                { "enterprise/policies", "acme" },
                { "enterprise/claims", "acme" },
                { "enterprise/audits", "acme" },
            },
        };
        IObservedEventTypeStore store = BuildObservedStore([]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [])));

        HandlerMismatchReport report1 = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);
        HandlerMismatchReport report2 = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        List<string> subjects1 = report1.Mismatches
            .Where(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing)
            .Select(m => m.Subject)
            .ToList();
        List<string> subjects2 = report2.Mismatches
            .Where(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing)
            .Select(m => m.Subject)
            .ToList();

        subjects1.Count.ShouldBe(3);
        subjects1.ShouldBe(subjects2);
        // The matcher orders routes case-insensitively before building expectations.
        subjects1.ShouldBe(subjects1.OrderBy(s => s, StringComparer.Ordinal).ToList());
    }

    [Fact]
    public async Task ProjectionBindingProviderFailure_LogsWarningWithExceptionTypeAndDoesNotEmitProjectionBindingMissing()
    {
        // Story 16.1 review F1 — a thrown provider was previously silently swallowed; operators need a structured signal.
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        IObservedEventTypeStore store = BuildObservedStore([]);
        CapturingLogger<HandlerMismatchDetector> logger = new();

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new ThrowingProjectionBindingProvider(),
            logger);

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
        logger.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("Projection binding provider failed", StringComparison.Ordinal)).ShouldBeTrue();
        logger.Entries.Any(e => e.Message.Contains("InvalidOperationException", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectionBindingSnapshotTenantMismatch_LogsWarningAndDoesNotEmitProjectionBindingMissing()
    {
        // Story 16.1 review F3 — an adopter returning the wrong tenant's snapshot was previously silent.
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        IObservedEventTypeStore store = BuildObservedStore([]);
        CapturingLogger<HandlerMismatchDetector> logger = new();

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "wrong-tenant",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [])),
            logger);

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
        logger.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("tenant mismatch", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectionBindingSnapshotWithNullBindings_LogsWarningAndDoesNotEmitProjectionBindingMissing()
    {
        // Story 16.1 review F2 — an Authoritative snapshot with null Bindings was previously indistinguishable from a clean report.
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        IObservedEventTypeStore store = BuildObservedStore([]);
        CapturingLogger<HandlerMismatchDetector> logger = new();

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: null!)),
            logger);

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
        logger.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("null Bindings", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectionBindingBindingWithNullEntryInList_DoesNotNREAndDoesNotEmitProjectionBindingMissing()
    {
        // Story 16.1 review F9 — a null entry inside Bindings used to NRE inside the matcher and silently downgrade to "no warning".
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 1, now),
        ]);
        ProjectionBinding validBinding = new(
            TenantId: "acme",
            SourcePrefix: "enterprise/claims",
            AggregateType: "claims",
            ProjectionName: "ClaimsReadModel",
            ProjectionType: "Acme.ClaimsProjection",
            SupportedEventTypePatterns: ["claimsubmitted"]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [null!, validBinding])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Any(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectionBindingProviderCancellation_RethrowsRegardlessOfExceptionType()
    {
        // Story 16.1 review F4 — cancellation must never be swallowed regardless of the wrapping exception type.
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise/claims", "acme" } },
        };
        IObservedEventTypeStore store = BuildObservedStore([]);
        using CancellationTokenSource cts = new();

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new CancellationAwareThrowingProvider(cts));

        await Should.ThrowAsync<Exception>(
            () => detector.DetectAsync("acme", TimeSpan.FromHours(24), cts.Token));
    }

    [Fact]
    public async Task ProjectionBindingWithDotStyleRouteAndSlashStyleBinding_AreTreatedAsEquivalentSources()
    {
        // Story 16.1 review F5 — dot-style routes (`acme.events`) and slash-style bindings (`acme/events`)
        // must canonicalize to the same source key; previously the OR-aggregate fallback masked the gap.
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "enterprise.claims", "acme" } },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("claims", "ClaimSubmittedV2", Count: 1, now),
        ]);
        ProjectionBinding binding = new(
            TenantId: "acme",
            SourcePrefix: "enterprise/claims",
            AggregateType: null,
            ProjectionName: "ClaimsReadModel",
            ProjectionType: "Acme.ClaimsProjection",
            SupportedEventTypePatterns: ["claimsubmitted"]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [binding])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Where(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectionBindingMatcher_NormalizesSlashCasingDuplicatesWildcardAndEventVersionSuffixes()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap =
            {
                { "/Enterprise/Claims/", "acme" },
                { "enterprise.claims", "acme" },
            },
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = BuildObservedStore(
        [
            new ObservedEventType("CLAIMS", "MyApp.Claims.ClaimSubmittedV2", Count: 1, now),
        ]);
        ProjectionBinding binding = new(
            TenantId: "ACME",
            SourcePrefix: "enterprise/claims",
            AggregateType: "claims",
            ProjectionName: "ClaimsReadModel",
            ProjectionType: "Acme.ClaimsProjection",
            SupportedEventTypePatterns: ["claimsubmitted"]);

        HandlerMismatchDetector detector = BuildDetector(
            routing,
            store,
            new StaticProjectionBindingProvider(new ProjectionBindingSnapshot(
                TenantId: "acme",
                Authority: ProjectionBindingRegistryAuthority.Authoritative,
                Bindings: [binding])));

        HandlerMismatchReport report = await detector.DetectAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);

        report.Mismatches.Where(m => m.Category == HandlerMismatchCategory.ProjectionBindingMissing).ShouldBeEmpty();
    }

    private static HandlerMismatchDetector BuildDetector(
        TenantEventRoutingOptions routing,
        IObservedEventTypeStore store,
        IProjectionBindingProvider? projectionBindingProvider = null,
        ILogger<HandlerMismatchDetector>? logger = null)
    {
        IOptionsMonitor<TenantEventRoutingOptions> monitor = Substitute.For<IOptionsMonitor<TenantEventRoutingOptions>>();
        monitor.CurrentValue.Returns(routing);
        return new HandlerMismatchDetector(
            monitor,
            store,
            projectionBindingProvider ?? new DefaultProjectionBindingProvider(),
            TimeProvider.System,
            logger ?? NullLogger<HandlerMismatchDetector>.Instance);
    }

    private static IObservedEventTypeStore BuildObservedStore(IReadOnlyList<ObservedEventType> observedTypes)
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(observedTypes));
        return store;
    }

    private sealed class StaticProjectionBindingProvider(ProjectionBindingSnapshot snapshot) : IProjectionBindingProvider
    {
        public ValueTask<ProjectionBindingSnapshot> GetBindingsAsync(string tenantId, CancellationToken cancellationToken)
            => ValueTask.FromResult(snapshot);
    }

    private sealed class ThrowingProjectionBindingProvider : IProjectionBindingProvider
    {
        public ValueTask<ProjectionBindingSnapshot> GetBindingsAsync(string tenantId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("projection discovery unavailable");
    }

    private sealed class CancellationAwareThrowingProvider : IProjectionBindingProvider
    {
        private readonly CancellationTokenSource _trigger;

        public CancellationAwareThrowingProvider(CancellationTokenSource trigger) => _trigger = trigger;

        public ValueTask<ProjectionBindingSnapshot> GetBindingsAsync(string tenantId, CancellationToken cancellationToken)
        {
            _trigger.Cancel();
            throw new AggregateException(new OperationCanceledException(cancellationToken));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }
}
