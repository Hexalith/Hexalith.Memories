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
