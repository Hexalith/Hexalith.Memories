// <copyright file="EventIngestionTelemetryAdapterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Telemetry;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>Story 9.3 — unit tests for <see cref="EventIngestionTelemetryAdapter"/>. Focus on the
/// observation-write gate (R3-8 Accepted-only, kill switch, whitespace guards, __rejected__ tenant
/// guard) and the bounded fire-and-forget contract. Tier-2 integration coverage of slow-Redis
/// behaviour is in the integration-test suite.</summary>
public sealed class EventIngestionTelemetryAdapterTests
{
    [Fact]
    public void AcceptedOutcome_WithValidFields_ShouldInvokeObservationStore()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: true);

        adapter.RecordIngestion(
            tenantId: "acme",
            caseId: "case-1",
            cloudEventId: "evt-1",
            aggregateType: "Claims",
            cloudEventType: "MyApp.Claims.ClaimSubmittedV2",
            outcome: EventIngestionOutcome.Accepted,
            durationMs: 5);

        // Fire-and-forget — wait briefly for the Task.Run to schedule + execute.
        WaitForObservationWrite(store);

        _ = store.Received(1).RecordObservationAsync(
            "acme", "Claims", "MyApp.Claims.ClaimSubmittedV2",
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DuplicateOutcome_ShouldNotWriteToObservationStore_R3Dash8()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: true);

        adapter.RecordIngestion(
            tenantId: "acme",
            caseId: "case-1",
            cloudEventId: "evt-1",
            aggregateType: "Claims",
            cloudEventType: "MyApp.Claims.ClaimSubmittedV2",
            outcome: EventIngestionOutcome.Duplicate,
            durationMs: 5);

        Thread.Sleep(30);
        _ = store.DidNotReceive().RecordObservationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UnknownSourceOutcome_ShouldNotWriteToObservationStore()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: true);

        adapter.RecordIngestion(
            tenantId: "__rejected__",
            caseId: null,
            cloudEventId: "evt-1",
            aggregateType: null,
            cloudEventType: "MyApp.Claims.ClaimSubmittedV2",
            outcome: EventIngestionOutcome.UnknownSource,
            durationMs: 5);

        Thread.Sleep(30);
        _ = store.DidNotReceive().RecordObservationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RejectedTenantTag_OnAccepted_ShouldNotWriteToObservationStore()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: true);

        adapter.RecordIngestion(
            tenantId: "__rejected__",
            caseId: "case-1",
            cloudEventId: "evt-1",
            aggregateType: "Claims",
            cloudEventType: "MyApp.Claims.ClaimSubmittedV2",
            outcome: EventIngestionOutcome.Accepted,
            durationMs: 5);

        Thread.Sleep(30);
        _ = store.DidNotReceive().RecordObservationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void NullCloudEventType_OnAccepted_ShouldNotWriteToObservationStore()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: true);

        adapter.RecordIngestion(
            tenantId: "acme",
            caseId: "case-1",
            cloudEventId: "evt-1",
            aggregateType: "Claims",
            cloudEventType: null,
            outcome: EventIngestionOutcome.Accepted,
            durationMs: 5);

        Thread.Sleep(30);
        _ = store.DidNotReceive().RecordObservationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WhitespaceCloudEventType_OnAccepted_ShouldNotWriteToObservationStore()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: true);

        adapter.RecordIngestion(
            tenantId: "acme",
            caseId: "case-1",
            cloudEventId: "evt-1",
            aggregateType: "Claims",
            cloudEventType: "   ",
            outcome: EventIngestionOutcome.Accepted,
            durationMs: 5);

        Thread.Sleep(30);
        _ = store.DidNotReceive().RecordObservationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void KillSwitchDisabled_OnAccepted_ShouldNotWriteToObservationStore()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: false);

        adapter.RecordIngestion(
            tenantId: "acme",
            caseId: "case-1",
            cloudEventId: "evt-1",
            aggregateType: "Claims",
            cloudEventType: "MyApp.Claims.ClaimSubmittedV2",
            outcome: EventIngestionOutcome.Accepted,
            durationMs: 5);

        Thread.Sleep(30);
        _ = store.DidNotReceive().RecordObservationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ObservationStoreThrows_ShouldNotSurfaceException()
    {
        IObservedEventTypeStore store = Substitute.For<IObservedEventTypeStore>();
        store.RecordObservationAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));

        EventIngestionTelemetryAdapter adapter = BuildAdapter(store, enabled: true);

        // Hot-path contract: the caller never sees an exception from the fire-and-forget write.
        Should.NotThrow(() => adapter.RecordIngestion(
            tenantId: "acme",
            caseId: "case-1",
            cloudEventId: "evt-1",
            aggregateType: "Claims",
            cloudEventType: "MyApp.Claims.ClaimSubmittedV2",
            outcome: EventIngestionOutcome.Accepted,
            durationMs: 5));

        Thread.Sleep(30); // let the fire-and-forget task complete
    }

    private static EventIngestionTelemetryAdapter BuildAdapter(
        IObservedEventTypeStore store,
        bool enabled)
    {
        EventStoreObservationOptions options = new() { Enabled = enabled };
        IOptionsMonitor<EventStoreObservationOptions> monitor =
            Substitute.For<IOptionsMonitor<EventStoreObservationOptions>>();
        monitor.CurrentValue.Returns(options);

        return new EventIngestionTelemetryAdapter(
            NullLogger<AccessTelemetryCategory>.Instance,
            NullLogger<EventIngestionTelemetryAdapter>.Instance,
            store,
            monitor);
    }

    private static void WaitForObservationWrite(IObservedEventTypeStore store)
    {
        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(10);
            IReadOnlyList<NSubstitute.Core.ICall> calls = store.ReceivedCalls() is { } rc
                ? new List<NSubstitute.Core.ICall>(rc)
                : new List<NSubstitute.Core.ICall>();
            foreach (NSubstitute.Core.ICall call in calls)
            {
                if (call.GetMethodInfo().Name == nameof(IObservedEventTypeStore.RecordObservationAsync))
                {
                    return;
                }
            }
        }
    }
}
