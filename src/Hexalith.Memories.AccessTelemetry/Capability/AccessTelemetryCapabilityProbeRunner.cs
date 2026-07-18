// <copyright file="AccessTelemetryCapabilityProbeRunner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using System.Collections.ObjectModel;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Builds and publishes a fail-closed exact-profile decision from behavioral evidence.</summary>
internal sealed class AccessTelemetryCapabilityProbeRunner
{
    private static readonly ReadOnlyCollection<string> CapabilityNames = Array.AsReadOnly(
    [
        nameof(AccessTelemetryCapabilityProfile.StrongCrudAndEtags),
        nameof(AccessTelemetryCapabilityProfile.MultiKeyTransactionsAndConflicts),
        nameof(AccessTelemetryCapabilityProfile.ActorReactivationFailoverAndReminders),
        nameof(AccessTelemetryCapabilityProfile.EffectivePerRecordTtl),
        nameof(AccessTelemetryCapabilityProfile.RecordAndRequestBounds),
        nameof(AccessTelemetryCapabilityProfile.TwoWriterThroughputDuringPurge),
        nameof(AccessTelemetryCapabilityProfile.DeclaredDurabilityAndFailureBehavior),
        nameof(AccessTelemetryCapabilityProfile.TenantIsolationAndEncryption),
        nameof(AccessTelemetryCapabilityProfile.PhysicalCapacityEvidence),
        nameof(AccessTelemetryCapabilityProfile.ReclamationEvidenceHooks),
    ]);

    private readonly IReadOnlyList<IAccessTelemetryCapabilityProbe> _probes;
    private readonly AccessTelemetryRuntimeGate _runtimeGate;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _runGate = new(1, 1);

    /// <summary>Initializes the runner with explicitly registered behavioral probes.</summary>
    public AccessTelemetryCapabilityProbeRunner(
        IEnumerable<IAccessTelemetryCapabilityProbe> probes,
        AccessTelemetryRuntimeGate runtimeGate,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(probes);
        _probes = probes.ToArray();
        _runtimeGate = runtimeGate ?? throw new ArgumentNullException(nameof(runtimeGate));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets the exact behavioral evidence names required for a passing profile.</summary>
    public static IReadOnlyList<string> RequiredCapabilities => CapabilityNames;

    /// <summary>Runs every registered probe and atomically publishes the resulting restart-scoped decision.</summary>
    public async Task<AccessTelemetryCapabilityGateResult> RunAsync(
        AccessTelemetryCapabilityProbeContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _runGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            AccessTelemetryCapabilityGateResult decision;
            try
            {
                AccessTelemetryCapabilityProbeResult[] results = await Task.WhenAll(
                    _probes.Select(probe => probe.ProbeAsync(cancellationToken))).ConfigureAwait(false);
                decision = Evaluate(context, results);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
            {
                decision = FailedDecision();
            }

            _runtimeGate.Publish(decision);
            return decision;
        }
        finally
        {
            _ = _runGate.Release();
        }
    }

    private AccessTelemetryCapabilityGateResult Evaluate(
        AccessTelemetryCapabilityProbeContext context,
        IReadOnlyList<AccessTelemetryCapabilityProbeResult> results)
    {
        if (results.Count != CapabilityNames.Count ||
            results.Any(result => result is null || !result.Passed || !CapabilityNames.Contains(result.Capability)) ||
            results.Select(result => result.Capability).Distinct(StringComparer.Ordinal).Count() != CapabilityNames.Count)
        {
            return FailedDecision();
        }

        HashSet<string> passed = results.Select(result => result.Capability).ToHashSet(StringComparer.Ordinal);
        var profile = new AccessTelemetryCapabilityProfile
        {
            ComponentProfileHash = context.ComponentProfileHash,
            ExactVersionPinned = context.ExactVersionPinned,
            DaprOnlyBoundary = context.DaprOnlyBoundary,
            StrongCrudAndEtags = passed.Contains(nameof(AccessTelemetryCapabilityProfile.StrongCrudAndEtags)),
            MultiKeyTransactionsAndConflicts = passed.Contains(nameof(AccessTelemetryCapabilityProfile.MultiKeyTransactionsAndConflicts)),
            ActorReactivationFailoverAndReminders = passed.Contains(nameof(AccessTelemetryCapabilityProfile.ActorReactivationFailoverAndReminders)),
            EffectivePerRecordTtl = passed.Contains(nameof(AccessTelemetryCapabilityProfile.EffectivePerRecordTtl)),
            RecordAndRequestBounds = passed.Contains(nameof(AccessTelemetryCapabilityProfile.RecordAndRequestBounds)),
            TwoWriterThroughputDuringPurge = passed.Contains(nameof(AccessTelemetryCapabilityProfile.TwoWriterThroughputDuringPurge)),
            DeclaredDurabilityAndFailureBehavior = passed.Contains(nameof(AccessTelemetryCapabilityProfile.DeclaredDurabilityAndFailureBehavior)),
            TenantIsolationAndEncryption = passed.Contains(nameof(AccessTelemetryCapabilityProfile.TenantIsolationAndEncryption)),
            PhysicalCapacityEvidence = passed.Contains(nameof(AccessTelemetryCapabilityProfile.PhysicalCapacityEvidence)),
            ReclamationEvidenceHooks = passed.Contains(nameof(AccessTelemetryCapabilityProfile.ReclamationEvidenceHooks)),
            IsAlpha = context.IsAlpha,
            CapacityEvidenceId = context.CapacityEvidenceId,
            PhysicalReclamationEvidenceId = context.PhysicalReclamationEvidenceId,
            ValidUntilUtc = context.ValidUntilUtc,
        };
        return AccessTelemetryCapabilityGate.Evaluate(
            profile,
            context.ComponentProfileHash,
            context.Production,
            context.AllowAlpha,
            _timeProvider.GetUtcNow());
    }

    private static AccessTelemetryCapabilityGateResult FailedDecision()
        => new(
            false,
            true,
            AccessTelemetryHealthState.Unhealthy,
            AccessTelemetryReason.CapabilityUnproven);
}
