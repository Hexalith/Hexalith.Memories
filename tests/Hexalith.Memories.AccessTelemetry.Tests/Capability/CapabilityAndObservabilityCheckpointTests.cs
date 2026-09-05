// <copyright file="CapabilityAndObservabilityCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Capability;

using System.Diagnostics.Metrics;

using Hexalith.Memories.AccessTelemetry.Capability;
using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Observability;
using Hexalith.Memories.AccessTelemetry.Tests.Observability;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>Story 27.2 C5 checkpoint for capability, authority, and observability gates.</summary>
[Collection(AccessTelemetryLifecycleMetricsTestCollection.Name)]
public sealed class CapabilityAndObservabilityCheckpointTests
{
    [Fact]
    public void CapabilityGate_AllRequiredEvidenceForExactProfile_Passes()
    {
        AccessTelemetryCapabilityGateResult result = AccessTelemetryCapabilityGate.Evaluate(
            ValidProfile(),
            new string('a', 64),
            production: true,
            allowAlpha: false,
            DateTimeOffset.UtcNow);

        result.AllowsWrites.ShouldBeTrue();
        result.Health.ShouldBe(AccessTelemetryHealthState.Healthy);
        result.BusinessReadinessAvailable.ShouldBeTrue();
    }

    [Theory]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.StrongCrudAndEtags))]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.MultiKeyTransactionsAndConflicts))]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.ActorReactivationFailoverAndReminders))]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.EffectivePerRecordTtl))]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.TwoWriterThroughputDuringPurge))]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.TenantIsolationAndEncryption))]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.PhysicalCapacityEvidence))]
    [InlineData(nameof(AccessTelemetryCapabilityProfile.ReclamationEvidenceHooks))]
    public void CapabilityGate_MissingAnyRequiredBehavior_BlocksOnlyLifecycleWrites(string property)
    {
        AccessTelemetryCapabilityProfile profile = ValidProfile().WithCapability(property, value: false);

        AccessTelemetryCapabilityGateResult result = AccessTelemetryCapabilityGate.Evaluate(
            profile,
            new string('a', 64),
            production: true,
            allowAlpha: false,
            DateTimeOffset.UtcNow);

        result.AllowsWrites.ShouldBeFalse();
        result.Health.ShouldBe(AccessTelemetryHealthState.Unhealthy);
        result.Reason.ShouldBe(AccessTelemetryReason.CapabilityUnproven);
        result.BusinessReadinessAvailable.ShouldBeTrue();
    }

    [Fact]
    public void CapabilityGate_ProfileHashStalenessAlphaAndVersionPin_FailClosed()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AccessTelemetryCapabilityGate.Evaluate(ValidProfile(), new string('b', 64), true, false, now).AllowsWrites.ShouldBeFalse();
        AccessTelemetryCapabilityGate.Evaluate(ValidProfile() with { ValidUntilUtc = now.AddSeconds(-1) }, new string('a', 64), true, false, now).AllowsWrites.ShouldBeFalse();
        AccessTelemetryCapabilityGate.Evaluate(ValidProfile() with { IsAlpha = true }, new string('a', 64), true, false, now).AllowsWrites.ShouldBeFalse();
        AccessTelemetryCapabilityGate.Evaluate(ValidProfile() with { IsAlpha = true }, new string('a', 64), true, true, now).AllowsWrites.ShouldBeTrue();
        AccessTelemetryCapabilityGate.Evaluate(ValidProfile() with { ExactVersionPinned = false }, new string('a', 64), true, true, now).AllowsWrites.ShouldBeFalse();
    }

    [Fact]
    public async Task CapabilityProbeRunner_AllRequiredBehavioralProofs_PublishesHealthyExactProfile()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var runtimeGate = new AccessTelemetryRuntimeGate();
        var runner = new AccessTelemetryCapabilityProbeRunner(
            AccessTelemetryCapabilityProbeRunner.RequiredCapabilities.Select(
                capability => new StubCapabilityProbe(capability, passed: true)),
            runtimeGate,
            TimeProvider.System);

        AccessTelemetryCapabilityGateResult result = await runner.RunAsync(
            ProbeContext(now.AddHours(1)),
            CancellationToken.None);

        result.AllowsWrites.ShouldBeTrue();
        runtimeGate.Current.ShouldBe(result);
    }

    [Fact]
    public void RuntimeGate_ClosesImmediatelyWhenPublishedEvidenceExpires()
    {
        DateTimeOffset now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(now);
        var runtimeGate = new AccessTelemetryRuntimeGate(clock);
        runtimeGate.Publish(new AccessTelemetryCapabilityGateResult(
            true,
            true,
            AccessTelemetryHealthState.Healthy,
            AccessTelemetryReason.None,
            now.AddSeconds(30)));

        runtimeGate.Current.AllowsWrites.ShouldBeTrue();
        clock.Advance(TimeSpan.FromSeconds(30));

        runtimeGate.Current.AllowsWrites.ShouldBeFalse();
        runtimeGate.Current.Reason.ShouldBe(AccessTelemetryReason.CapabilityUnproven);
        runtimeGate.Current.BusinessReadinessAvailable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("failed")]
    [InlineData("duplicate")]
    [InlineData("faulted")]
    public async Task CapabilityProbeRunner_IncompleteAmbiguousOrFaultedEvidence_FailsClosed(string failure)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<IAccessTelemetryCapabilityProbe> probes = AccessTelemetryCapabilityProbeRunner.RequiredCapabilities
            .Select(capability => (IAccessTelemetryCapabilityProbe)new StubCapabilityProbe(capability, passed: true))
            .ToList();
        string target = AccessTelemetryCapabilityProbeRunner.RequiredCapabilities[0];
        if (failure == "missing")
        {
            probes.RemoveAt(0);
        }
        else if (failure == "failed")
        {
            probes[0] = new StubCapabilityProbe(target, passed: false);
        }
        else if (failure == "duplicate")
        {
            probes.Add(new StubCapabilityProbe(target, passed: true));
        }
        else
        {
            probes[0] = new StubCapabilityProbe(target, passed: true, throws: true);
        }

        var runtimeGate = new AccessTelemetryRuntimeGate();
        var runner = new AccessTelemetryCapabilityProbeRunner(probes, runtimeGate, TimeProvider.System);

        AccessTelemetryCapabilityGateResult result = await runner.RunAsync(
            ProbeContext(now.AddHours(1)),
            CancellationToken.None);

        result.AllowsWrites.ShouldBeFalse();
        result.Reason.ShouldBe(AccessTelemetryReason.CapabilityUnproven);
        result.BusinessReadinessAvailable.ShouldBeTrue();
        runtimeGate.Current.ShouldBe(result);
    }

    [Fact]
    public void AuthorityPolicy_SeparatesWriterLifecycleClockInspectorAndAdapterActions()
    {
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.ServerWriter, AccessTelemetryAuthorityAction.Write).ShouldBeTrue();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.ServerWriter, AccessTelemetryAuthorityAction.Read).ShouldBeFalse();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.ServerWriter, AccessTelemetryAuthorityAction.Delete).ShouldBeFalse();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.LifecycleService, AccessTelemetryAuthorityAction.Delete).ShouldBeTrue();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.Clock, AccessTelemetryAuthorityAction.SignTime).ShouldBeTrue();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.Clock, AccessTelemetryAuthorityAction.Write).ShouldBeFalse();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.Inspector, AccessTelemetryAuthorityAction.SanitizedInspect).ShouldBeTrue();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.Inspector, AccessTelemetryAuthorityAction.RotateKeys).ShouldBeFalse();
        AccessTelemetryAuthorityPolicy.Allows(AccessTelemetryAuthority.AdapterEvidence, AccessTelemetryAuthorityAction.PhysicalEvidence).ShouldBeTrue();
    }

    [Fact]
    public void LifecycleCounter_EmitsOnlyBoundedStateAndReasonLabels()
    {
        List<KeyValuePair<string, object?>> tags = [];
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Name == "memories.access.telemetry.lifecycle.records")
            {
                current.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, measurementTags, _) => tags.AddRange(measurementTags.ToArray()));
        listener.Start();

        AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Dropped, AccessTelemetryReason.QueueFull);

        tags.ShouldBe(
        [
            new KeyValuePair<string, object?>("state", "dropped"),
            new KeyValuePair<string, object?>("reason", "queue_full"),
        ]);
    }

    [Fact]
    public void LifecycleGauges_UseLiveClockAndAggregateHealthWithoutInventingPhysicalEvidence()
    {
        DateTimeOffset now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(now);
        List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> doubles = [];
        List<(string Name, long Value, KeyValuePair<string, object?>[] Tags)> integers = [];
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == AccessTelemetryLifecycleMetrics.MeterName)
            {
                current.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            doubles.Add((instrument.Name, value, tags.ToArray())));
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            integers.Add((instrument.Name, value, tags.ToArray())));
        listener.Start();

        AccessTelemetryLifecycleMetrics.RecordRuntimeGate(
            true,
            now.AddSeconds(30),
            AccessTelemetryHealthState.Healthy,
            AccessTelemetryReason.None,
            clock);
        AccessTelemetryLifecycleMetrics.RecordProcessorHealth(
            AccessTelemetryHealthState.Degraded,
            AccessTelemetryReason.DependencyUnavailable);
        AccessTelemetryLifecycleMetrics.RecordAttestation(CreateAttestation(now.AddSeconds(-10)), now, clock);
        AccessTelemetryLifecycleMetrics.RecordExpiryState(7, now.AddSeconds(-30), now.AddSeconds(-20));
        AccessTelemetryLifecycleMetrics.RecordCapacity(450, 900);
        AccessTelemetryLifecycleMetrics.RecordPhysicalEvidence(present: false);
        AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Dropped, AccessTelemetryReason.QueueFull);
        AccessTelemetryLifecycleMetrics.RecordDaprLatency(11);
        AccessTelemetryLifecycleMetrics.RecordAttestationLatency(12);
        AccessTelemetryLifecycleMetrics.RecordStateLatency(13);
        AccessTelemetryLifecycleMetrics.RecordExpiryLag(30);
        AccessTelemetryLifecycleMetrics.RecordPurgeLatency(14);
        AccessTelemetryLifecycleMetrics.RecordReminder(succeeded: true);
        listener.RecordObservableInstruments();

        Last(integers, AccessTelemetryMetricContract.CapacityRecords).Value.ShouldBe(450);
        Last(doubles, AccessTelemetryMetricContract.CapacityUtilization).Value.ShouldBe(0.5);
        Last(integers, AccessTelemetryMetricContract.ExpiryIndexDepth).Value.ShouldBe(7);
        Last(doubles, AccessTelemetryMetricContract.AttestationAge).Value.ShouldBe(10);
        Last(doubles, AccessTelemetryMetricContract.ExpiryOldestDueAge).Value.ShouldBe(30);
        Last(doubles, AccessTelemetryMetricContract.PurgeCohortAge).Value.ShouldBe(20);
        Last(integers, AccessTelemetryMetricContract.Profile).Tags.ShouldContain(
            new KeyValuePair<string, object?>("state", "matched"));
        Last(integers, AccessTelemetryMetricContract.Health).Tags.ShouldContain(
            new KeyValuePair<string, object?>("state", "degraded"));
        integers.ShouldNotContain(measurement =>
            measurement.Name == AccessTelemetryMetricContract.PhysicalEvidenceLastTimestamp);

        AccessTelemetryLifecycleMetrics.RecordPhysicalEvidence(present: true, now.AddSeconds(-60));
        clock.Advance(TimeSpan.FromSeconds(5));
        listener.RecordObservableInstruments();

        Last(doubles, AccessTelemetryMetricContract.AttestationAge).Value.ShouldBe(15);
        Last(doubles, AccessTelemetryMetricContract.ExpiryOldestDueAge).Value.ShouldBe(35);
        Last(doubles, AccessTelemetryMetricContract.PurgeCohortAge).Value.ShouldBe(25);
        Last(integers, AccessTelemetryMetricContract.PhysicalEvidenceLastTimestamp).Value
            .ShouldBe(now.AddSeconds(-60).ToUnixTimeSeconds());
        string[] observedNames = doubles.Select(static measurement => measurement.Name)
            .Concat(integers.Select(static measurement => measurement.Name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        foreach (string expected in AccessTelemetryMetricContract.MetricTagKeyPolicy.Keys.Except(
            [
                AccessTelemetryMetricContract.QueueRecords,
                AccessTelemetryMetricContract.QueueBytes,
                AccessTelemetryMetricContract.QueueOldestAge,
            ],
            StringComparer.Ordinal))
        {
            observedNames.ShouldContain(expected);
        }

        AccessTelemetryLifecycleMetrics.RecordProcessorHealth(
            AccessTelemetryHealthState.Healthy,
            AccessTelemetryReason.None);
        listener.RecordObservableInstruments();
        Last(integers, AccessTelemetryMetricContract.Health).Tags.ShouldContain(
            new KeyValuePair<string, object?>("state", "no_data"));
        AccessTelemetryLifecycleMetrics.RecordProcessorHealth(
            AccessTelemetryHealthState.Healthy,
            AccessTelemetryReason.None,
            clock.GetUtcNow());
        listener.RecordObservableInstruments();
        Last(integers, AccessTelemetryMetricContract.Health).Tags.ShouldContain(
            new KeyValuePair<string, object?>("state", "healthy"));

        AccessTelemetryLifecycleMetrics.RecordProcessorHealth(
            AccessTelemetryHealthState.Unhealthy,
            AccessTelemetryReason.ClockUntrusted);
        listener.RecordObservableInstruments();
        Last(integers, AccessTelemetryMetricContract.Health).Tags.ShouldContain(
            new KeyValuePair<string, object?>("state", "unhealthy"));
        Last(integers, AccessTelemetryMetricContract.Health).Tags.ShouldContain(
            new KeyValuePair<string, object?>("reason", "clock_untrusted"));

        AccessTelemetryLifecycleMetrics.RecordProcessorHealth(
            AccessTelemetryHealthState.Healthy,
            AccessTelemetryReason.None);
        clock.Advance(TimeSpan.FromSeconds(25));
        listener.RecordObservableInstruments();
        Last(integers, AccessTelemetryMetricContract.Profile).Tags.ShouldContain(
            new KeyValuePair<string, object?>("state", "unproven"));
        Last(integers, AccessTelemetryMetricContract.Health).Tags.ShouldContain(
            new KeyValuePair<string, object?>("reason", "capability_unproven"));
    }

    [Fact]
    public void HealthPrecedence_IsUnhealthyThenDegradedThenNoDataOrHealthy()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AccessTelemetryHealthEvaluator.Evaluate(true, true, true, now.AddMinutes(-16), now).State.ShouldBe(AccessTelemetryHealthState.NoData);
        AccessTelemetryHealthEvaluator.Evaluate(true, true, true, now.AddMinutes(-14), now).State.ShouldBe(AccessTelemetryHealthState.Healthy);
        AccessTelemetryHealthEvaluator.Evaluate(true, true, false, now.AddMinutes(-16), now).State.ShouldBe(AccessTelemetryHealthState.Degraded);
        AccessTelemetryHealthEvaluator.Evaluate(true, false, false, now.AddMinutes(-16), now).State.ShouldBe(AccessTelemetryHealthState.Unhealthy);
        AccessTelemetryHealthEvaluator.Evaluate(false, true, true, null, now).State.ShouldBe(AccessTelemetryHealthState.Unhealthy);
    }

    [Fact]
    public void HealthDetails_HaveOperationalFieldsWithoutRawOrIdentifierValues()
    {
        AccessTelemetryHealthSnapshot snapshot = AccessTelemetryHealthEvaluator.CreateFailure(
            AccessTelemetryReason.RemoteValidationPending);

        snapshot.Cause.ShouldNotBeNullOrWhiteSpace();
        snapshot.Impact.ShouldNotBeNullOrWhiteSpace();
        snapshot.Owner.ShouldNotBeNullOrWhiteSpace();
        snapshot.NextAction.ShouldNotBeNullOrWhiteSpace();
        string combined = $"{snapshot.Cause} {snapshot.Impact} {snapshot.Owner} {snapshot.NextAction}";
        combined.ShouldNotContain("tenant-a", Case.Sensitive);
        combined.ShouldNotContain("record-", Case.Sensitive);
        combined.ShouldNotContain("redis", Case.Insensitive);
        combined.ShouldNotContain("process", Case.Insensitive);
    }

    private static AccessTelemetryCapabilityProfile ValidProfile()
        => new()
        {
            ComponentProfileHash = new string('a', 64),
            ExactVersionPinned = true,
            DaprOnlyBoundary = true,
            StrongCrudAndEtags = true,
            MultiKeyTransactionsAndConflicts = true,
            ActorReactivationFailoverAndReminders = true,
            EffectivePerRecordTtl = true,
            RecordAndRequestBounds = true,
            TwoWriterThroughputDuringPurge = true,
            DeclaredDurabilityAndFailureBehavior = true,
            TenantIsolationAndEncryption = true,
            PhysicalCapacityEvidence = true,
            ReclamationEvidenceHooks = true,
            CapacityEvidenceId = "capacity-development",
            PhysicalReclamationEvidenceId = "pending-story-27-3",
            ValidUntilUtc = DateTimeOffset.UtcNow.AddHours(1),
        };

    private static (string Name, T Value, KeyValuePair<string, object?>[] Tags) Last<T>(
        IEnumerable<(string Name, T Value, KeyValuePair<string, object?>[] Tags)> measurements,
        string name)
        => measurements.Last(measurement => string.Equals(measurement.Name, name, StringComparison.Ordinal));

    private static SignedClockAttestation CreateAttestation(DateTimeOffset issuedAt)
        => new()
        {
            DeploymentId = "deployment-a",
            AppId = "memories-access-telemetry",
            ServiceInstanceId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XB",
            ProcessEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XC",
            ComponentProfileHash = new string('a', 64),
            RequestingProcessEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            RequestingServiceInstanceId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XA",
            Nonce = "01HM5Q9WXGK6T8Q4Z5Y6V7W8XD",
            NotBeforeUnixMilliseconds = issuedAt.AddMilliseconds(-10).ToUnixTimeMilliseconds(),
            NotAfterUnixMilliseconds = issuedAt.AddMilliseconds(10).ToUnixTimeMilliseconds(),
            IssuedAtUnixMilliseconds = issuedAt.ToUnixTimeMilliseconds(),
            ExpiresAtUnixMilliseconds = issuedAt.AddSeconds(30).ToUnixTimeMilliseconds(),
            SignerKeyEpoch = "clock-key-1",
            Signature = Convert.ToBase64String(new byte[64]),
        };

    private static AccessTelemetryCapabilityProbeContext ProbeContext(DateTimeOffset validUntilUtc)
        => new()
        {
            ComponentProfileHash = new string('a', 64),
            ExactVersionPinned = true,
            DaprOnlyBoundary = true,
            Production = true,
            AllowAlpha = false,
            IsAlpha = false,
            CapacityEvidenceId = "capacity-development",
            PhysicalReclamationEvidenceId = "pending-story-27-3",
            ValidUntilUtc = validUntilUtc,
        };

    private sealed class StubCapabilityProbe(string capability, bool passed, bool throws = false)
        : IAccessTelemetryCapabilityProbe
    {
        public Task<AccessTelemetryCapabilityProbeResult> ProbeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return throws
                ? Task.FromException<AccessTelemetryCapabilityProbeResult>(new InvalidOperationException("probe failed"))
                : Task.FromResult(new AccessTelemetryCapabilityProbeResult(capability, passed));
        }
    }
}
