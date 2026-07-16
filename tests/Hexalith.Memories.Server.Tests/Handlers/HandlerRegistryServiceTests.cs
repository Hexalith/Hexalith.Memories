// <copyright file="HandlerRegistryServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Handlers;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public sealed class HandlerRegistryServiceTests
{
    [Fact]
    public async Task EmptyTopic_ShouldReturnDisabledStatus_WithNoHandlers()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = string.Empty,
        };

        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        HandlerRegistryService service = BuildService(routing, store, daprClient: null);

        HandlerRegistrationSnapshot snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        snapshot.SubscriptionStatus.ShouldBe(HandlerSubscriptionStatus.Disabled);
        snapshot.Handlers.ShouldBeEmpty();
    }

    [Fact]
    public async Task EmptySourceToTenantMap_WithTopic_ShouldReturnUnknownStatus()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
        };

        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        HandlerRegistryService service = BuildService(routing, store, daprClient: null);

        HandlerRegistrationSnapshot snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        snapshot.SubscriptionStatus.ShouldBe(HandlerSubscriptionStatus.Unknown);
        snapshot.Handlers.ShouldBeEmpty();
    }

    [Fact]
    public async Task ActiveTenantWithEvents_ShouldAggregateEventsProcessedCount()
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
                new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 3, now.AddMinutes(-5)),
                new ObservedEventType("Claims", "ClaimApprovedV2", Count: 2, now.AddMinutes(-10)),
            }));

        DaprClient daprClient = BuildDaprClientReturningTenant(
            "acme",
            new TenantRegistryEntry(new TenantInfo("acme", "Acme", TenantStatus.Active, now), WorkflowInstanceId: null));

        HandlerRegistryService service = BuildService(routing, store, daprClient);

        HandlerRegistrationSnapshot snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        snapshot.SubscriptionStatus.ShouldBe(HandlerSubscriptionStatus.Active);
        snapshot.Handlers.Count.ShouldBe(1);
        snapshot.Handlers[0].EventsProcessedCount.ShouldBe(5L);
        snapshot.Handlers[0].ObservedEventTypes.Count.ShouldBe(2);
        snapshot.Handlers[0].Error.ShouldBeNull();
        snapshot.Handlers[0].EventTypePatterns.ShouldContain("Claims");
    }

    [Fact]
    public async Task ObservationStoreThrows_ShouldReturnPartialSnapshotWithErrorRow_NotFiveHundred()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events", "acme" } },
        };

        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Throws(new StackExchange.Redis.RedisConnectionException(
                StackExchange.Redis.ConnectionFailureType.UnableToConnect, "boom"));

        DaprClient daprClient = BuildDaprClientReturningTenant(
            "acme",
            new TenantRegistryEntry(new TenantInfo("acme", "Acme", TenantStatus.Active, DateTimeOffset.UtcNow), null));

        HandlerRegistryService service = BuildService(routing, store, daprClient);

        // Contract: Redis failure for ONE tenant must NOT throw; returns a sentinel error row.
        HandlerRegistrationSnapshot snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        snapshot.Handlers.Count.ShouldBe(1);
        snapshot.Handlers[0].Error.ShouldBe("OBSERVATION_READ_FAILED");
        snapshot.Handlers[0].EventsProcessedCount.ShouldBe(0L);
        snapshot.Handlers[0].ObservedEventTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingTenant_ShouldNotAppearInHandlers()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events", "acme" } },
        };

        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        DaprClient daprClient = BuildDaprClientReturningTenant(
            "acme",
            new TenantRegistryEntry(new TenantInfo("acme", "Acme", TenantStatus.Deleting, DateTimeOffset.UtcNow), null));

        HandlerRegistryService service = BuildService(routing, store, daprClient);
        HandlerRegistrationSnapshot snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        snapshot.Handlers.ShouldBeEmpty();
    }

    [Fact]
    public async Task MultiplePrefixesSameTenant_ShouldCollapseOneReadAndEmitMultipleRows()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap =
            {
                { "acme.claims", "acme" },
                { "acme.policies", "acme" },
            },
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(new[]
            {
                new ObservedEventType("Claims", "ClaimSubmittedV2", Count: 3, now),
            }));

        DaprClient daprClient = BuildDaprClientReturningTenant(
            "acme",
            new TenantRegistryEntry(new TenantInfo("acme", "Acme", TenantStatus.Active, now), null));

        HandlerRegistryService service = BuildService(routing, store, daprClient);
        HandlerRegistrationSnapshot snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        snapshot.Handlers.Count.ShouldBe(2);
        snapshot.Handlers.All(h => h.TenantId == "acme").ShouldBeTrue();
        _ = await store.Received(1).GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AfterStartupGrace_NewScopedInstance_ShouldNotResetSubscriptionStatusToUnknown()
    {
        TenantEventRoutingOptions routing = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            SourceToTenantMap = { { "acme.events", "acme" } },
        };

        FakeTimeProvider timeProvider = new(DateTimeOffset.Parse("2026-04-25T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        ProcessLifetimeClock processLifetimeClock = new(timeProvider);

        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.GetAllObservedTypesAsync("acme", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ObservedEventType>>(Array.Empty<ObservedEventType>()));

        DaprClient daprClient = BuildDaprClientReturningTenant(
            "acme",
            new TenantRegistryEntry(new TenantInfo("acme", "Acme", TenantStatus.Active, timeProvider.GetUtcNow()), null));

        timeProvider.Advance(HandlerRegistryService.StartupGraceWindow + TimeSpan.FromSeconds(1));

        HandlerRegistryService service = BuildService(routing, store, daprClient, timeProvider, processLifetimeClock);
        HandlerRegistrationSnapshot snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        snapshot.SubscriptionStatus.ShouldBe(HandlerSubscriptionStatus.Active);
    }

    private static HandlerRegistryService BuildService(
        TenantEventRoutingOptions routing,
        IObservedEventTypeStore store,
        DaprClient? daprClient,
        TimeProvider? timeProvider = null,
        ProcessLifetimeClock? processLifetimeClock = null)
    {
        IOptionsMonitor<TenantEventRoutingOptions> monitor = Substitute.For<IOptionsMonitor<TenantEventRoutingOptions>>();
        monitor.CurrentValue.Returns(routing);

        DaprClient clientForTenant = daprClient ?? Substitute.For<DaprClient>();
        TenantRegistryService tenantRegistry = new(clientForTenant, NullLogger<TenantRegistryService>.Instance);
        TimeProvider effectiveTimeProvider = timeProvider ?? TimeProvider.System;
        ProcessLifetimeClock effectiveProcessLifetimeClock = processLifetimeClock ?? new ProcessLifetimeClock(effectiveTimeProvider);

        return new HandlerRegistryService(
            monitor,
            store,
            tenantRegistry,
            effectiveTimeProvider,
            effectiveProcessLifetimeClock,
            NullLogger<HandlerRegistryService>.Instance);
    }

    private static DaprClient BuildDaprClientReturningTenant(string tenantId, TenantRegistryEntry? entry)
    {
        DaprClient client = Substitute.For<DaprClient>();
        StoredTenantRegistryEntry? storedEntry = entry is null ? null : PersistenceModelMapper.ToStored(entry);
        client
            .GetStateAsync<StoredTenantRegistryEntry?>(
                Arg.Any<string>(),
                Arg.Is<string>(s => s.Contains(tenantId, StringComparison.Ordinal)),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(storedEntry));
        return client;
    }
}
