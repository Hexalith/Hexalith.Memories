// <copyright file="AccessTelemetryQualificationRejectionAccountingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;
using System.Security.Cryptography;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>Guards the per-segment pre-queue rejection breakdown kept by qualification accounting.</summary>
public sealed class AccessTelemetryQualificationRejectionAccountingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 16, 27, 52, TimeSpan.Zero);

    [Fact]
    public void Accounting_SeparatesPreQueueRejectionKindsAndReportsMonotonicDeltas()
    {
        const string Correlation = "qualification-0123456789abcdef0123456789abcdef";
        var accounting = new AccessTelemetryQualificationAccounting();
        accounting.RecordSanitizerUnavailable(Correlation, AccessTelemetryReason.RemoteValidationPending, "Secret");
        AccessTelemetryQualificationRejectionBreakdown before = accounting.RejectionsForCorrelation(Correlation);

        accounting.RecordSanitizerUnavailable(Correlation, AccessTelemetryReason.RemoteValidationPending, "Secret");
        accounting.RecordSanitizerUnavailable(Correlation, AccessTelemetryReason.CapabilityUnproven, "RemoteValidation");
        accounting.RecordSanitizerUnavailable(Correlation, lifecycleReason: null, bootstrapStage: null);
        accounting.RecordSanitizerUnavailable(Correlation, (AccessTelemetryReason)int.MaxValue, "fixture-private-stage");
        accounting.RecordSanitationFailed(Correlation);
        accounting.RecordSanitationFailed(correlation: null);

        AccessTelemetryQualificationRejectionBreakdown delta = accounting.RejectionsForCorrelation(Correlation).Since(before);
        delta.SanitizerUnavailable.ShouldBe(4);
        delta.SanitationFailed.ShouldBe(1);
        delta.SanitizerUnavailableLifecycleReasons.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["CapabilityUnproven"] = 1,
            ["RemoteValidationPending"] = 1,
            [AccessTelemetryQualificationRejectionBreakdown.Unobserved] = 2,
        });
        delta.SanitizerUnavailableBootstrapStages.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["RemoteValidation"] = 1,
            ["Secret"] = 1,
            [AccessTelemetryQualificationRejectionBreakdown.Unobserved] = 2,
        });
        accounting.ForCorrelation(Correlation).Rejected.ShouldBe(6);
        accounting.Current.Rejected.ShouldBe(7);
        AccessTelemetryQualificationRejectionBreakdown unknown = accounting.RejectionsForCorrelation("qualification-unknown");
        unknown.SanitizerUnavailable.ShouldBe(0);
        unknown.SanitationFailed.ShouldBe(0);
        unknown.SanitizerUnavailableLifecycleReasons.ShouldBeEmpty();
        unknown.SanitizerUnavailableBootstrapStages.ShouldBeEmpty();
        unknown.LifecycleHealth.ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        unknown.LifecycleReason.ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        unknown.Bootstrap.ShouldBe(AccessTelemetryLifecycleBootstrapSnapshot.NotObserved);
    }

    [Fact]
    public void Breakdown_ReportsOnlyBoundedLifecycleAndBootstrapNames()
    {
        AccessTelemetryQualificationRejectionBreakdown.ReasonName(AccessTelemetryReason.ClockUntrusted).ShouldBe("ClockUntrusted");
        AccessTelemetryQualificationRejectionBreakdown.ReasonName(null).ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        AccessTelemetryQualificationRejectionBreakdown.ReasonName((AccessTelemetryReason)42).ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        foreach (AccessTelemetryLifecycleBootstrapStage stage in Enum.GetValues<AccessTelemetryLifecycleBootstrapStage>())
        {
            AccessTelemetryQualificationRejectionBreakdown.StageName(stage.ToString()).ShouldBe(stage.ToString());
        }

        foreach (string? stage in new[] { null, string.Empty, "2", "secret", " Secret", "Published,Secret", "fixture-private-stage" })
        {
            AccessTelemetryQualificationRejectionBreakdown.StageName(stage).ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved, stage);
        }

        AccessTelemetryQualificationRejectionBreakdown observed = AccessTelemetryQualificationRejectionBreakdown.Empty
            .WithLifecycle(new AccessTelemetryLifecycleStatusSnapshot(AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.ConfigurationInvalid))
            .WithBootstrap(new AccessTelemetryLifecycleBootstrapSnapshot(3, "Secret", "Secret", "SecretMissing", null));
        observed.LifecycleHealth.ShouldBe("Unhealthy");
        observed.LifecycleReason.ShouldBe("ConfigurationInvalid");
        observed.Bootstrap.ShouldBe(new AccessTelemetryLifecycleBootstrapSnapshot(3, "Secret", "Secret", "SecretMissing", null));
        AccessTelemetryQualificationRejectionBreakdown unobserved = observed.WithLifecycle(null).WithBootstrap(null);
        unobserved.LifecycleHealth.ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        unobserved.LifecycleReason.ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        unobserved.Bootstrap.ShouldBe(AccessTelemetryLifecycleBootstrapSnapshot.NotObserved);
        unobserved.Bootstrap.Stage.ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        AccessTelemetryQualificationRejectionBreakdown undefined = observed.WithLifecycle(
            new AccessTelemetryLifecycleStatusSnapshot((AccessTelemetryHealthState)42, (AccessTelemetryReason)42));
        undefined.LifecycleHealth.ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
        undefined.LifecycleReason.ShouldBe(AccessTelemetryQualificationRejectionBreakdown.Unobserved);
    }

    [Fact]
    public void Breakdown_ExposesOwnedReadOnlyDictionariesThatNoCallerCanMutate()
    {
        var reasons = new Dictionary<string, long>(StringComparer.Ordinal) { ["RemoteValidationPending"] = 2 };
        var stages = new SortedDictionary<string, long>(StringComparer.Ordinal) { ["Secret"] = 2 };
        var breakdown = new AccessTelemetryQualificationRejectionBreakdown(
            2,
            0,
            reasons,
            stages,
            AccessTelemetryQualificationRejectionBreakdown.Unobserved,
            AccessTelemetryQualificationRejectionBreakdown.Unobserved,
            AccessTelemetryLifecycleBootstrapSnapshot.NotObserved);

        // The constructor copies its inputs, so later caller mutation is not observable.
        reasons["RemoteValidationPending"] = 99;
        reasons["Injected"] = 1;
        stages.Clear();
        breakdown.SanitizerUnavailableLifecycleReasons.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["RemoteValidationPending"] = 2 });
        breakdown.SanitizerUnavailableBootstrapStages.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["Secret"] = 2 });

        // A non-destructive copy also owns read-only copies, and the shared Empty instance cannot be mutated.
        AccessTelemetryQualificationRejectionBreakdown copied = breakdown with { SanitizerUnavailableLifecycleReasons = reasons };
        reasons.Clear();
        copied.SanitizerUnavailableLifecycleReasons.Count.ShouldBe(2);
        foreach (IReadOnlyDictionary<string, long> exposed in new[]
        {
            breakdown.SanitizerUnavailableLifecycleReasons,
            breakdown.SanitizerUnavailableBootstrapStages,
            copied.SanitizerUnavailableLifecycleReasons,
            AccessTelemetryQualificationRejectionBreakdown.Empty.SanitizerUnavailableLifecycleReasons,
            AccessTelemetryQualificationRejectionBreakdown.Empty.SanitizerUnavailableBootstrapStages,
        })
        {
            IDictionary<string, long> writable = exposed.ShouldBeAssignableTo<IDictionary<string, long>>()!;
            writable.IsReadOnly.ShouldBeTrue();
            _ = Should.Throw<NotSupportedException>(() => writable["Injected"] = 1);
        }

        // Snapshots taken from live accounting are independent of later rejections.
        const string Correlation = "qualification-0123456789abcdef0123456789abcdef";
        var accounting = new AccessTelemetryQualificationAccounting();
        accounting.RecordSanitizerUnavailable(Correlation, AccessTelemetryReason.RemoteValidationPending, "NotStarted");
        AccessTelemetryQualificationRejectionBreakdown snapshot = accounting.RejectionsForCorrelation(Correlation);
        accounting.RecordSanitizerUnavailable(Correlation, AccessTelemetryReason.CapabilityUnproven, "Configuration");
        snapshot.SanitizerUnavailableLifecycleReasons.Keys.ShouldBe(["RemoteValidationPending"]);
        snapshot.SanitizerUnavailableBootstrapStages.Keys.ShouldBe(["NotStarted"]);
        ReferenceEquals(snapshot.SanitizerUnavailableLifecycleReasons, accounting.RejectionsForCorrelation(Correlation).SanitizerUnavailableLifecycleReasons).ShouldBeFalse();
    }

    [Fact]
    public void Logger_SplitsAMixedSegmentByTheBranchThatRanAndKeepsTheTotalConsistent()
    {
        const string RunId = "dd8512086bb377547e89eda7d028b986";
        const string SegmentId = "cohort-a";
        var time = new FakeTimeProvider(Now);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(16, 16 * 1024);
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        var accessor = new AccessTelemetrySanitizerAccessor();
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, accessor, status, time, accounting, progress);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        ILogger<AccessTelemetryCategory> logger = loggerFactory.CreateLogger<AccessTelemetryCategory>();
        int ordinal = 0;
        void Emit(int count)
        {
            for (int index = 0; index < count; index++, ordinal++)
            {
                AccessTelemetryLog.LogTenantLifecycleAccess(logger, CreateEvent(RunId, SegmentId, ordinal));
            }
        }

        // Before any attempt, then while remote validation is in flight with a later pending reason, then after
        // publication, then with a refusing sanitizer.
        Emit(3);
        progress.BeginAttempt();
        progress.Enter(AccessTelemetryLifecycleBootstrapStage.Secret);
        progress.Enter(AccessTelemetryLifecycleBootstrapStage.RemoteValidation);
        status.Publish(AccessTelemetryHealthState.Degraded, AccessTelemetryReason.CapabilityUnproven);
        Emit(2);
        accessor.Publish(new AccessTelemetrySanitizer(
            RandomNumberGenerator.GetBytes(32), "development-key-1", time, new MonotonicRecordIdGenerator(), TimeSpan.FromHours(1), qualificationMode: true));
        progress.CompletePublished(time.GetUtcNow());
        status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
        Emit(4);
        accessor.Publish(new AccessTelemetrySanitizer(
            RandomNumberGenerator.GetBytes(32), "development-key-1", time, new MonotonicRecordIdGenerator(), TimeSpan.MaxValue, qualificationMode: true));
        Emit(1);

        string correlation = "qualification-" + AccessTelemetryQualificationWorkloadRunner.QualificationCorrelation(RunId, SegmentId);
        AccessTelemetryQualificationAccountingSnapshot counters = accounting.ForCorrelation(correlation);
        AccessTelemetryQualificationRejectionBreakdown breakdown = accounting.RejectionsForCorrelation(correlation);
        counters.Attempted.ShouldBe(10);
        counters.Enqueued.ShouldBe(4);
        queue.Count.ShouldBe(4);
        counters.Rejected.ShouldBe(6);
        breakdown.SanitizerUnavailable.ShouldBe(5);
        breakdown.SanitationFailed.ShouldBe(1);
        breakdown.SanitizerUnavailableLifecycleReasons.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["CapabilityUnproven"] = 2,
            ["RemoteValidationPending"] = 3,
        });
        breakdown.SanitizerUnavailableBootstrapStages.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["NotStarted"] = 3,
            ["RemoteValidation"] = 2,
        });
        (breakdown.SanitizerUnavailable + breakdown.SanitationFailed).ShouldBe(counters.Rejected);
        breakdown.SanitizerUnavailableLifecycleReasons.Values.Sum().ShouldBe(breakdown.SanitizerUnavailable);
        breakdown.SanitizerUnavailableBootstrapStages.Values.Sum().ShouldBe(breakdown.SanitizerUnavailable);
    }

    [Fact]
    public void Logger_WithoutBootstrapProgressCountsUnavailableRejectionsAsUnobservedStage()
    {
        const string RunId = "dd8512086bb377547e89eda7d028b986";
        const string SegmentId = "cohort-a";
        var time = new FakeTimeProvider(Now);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(16, 16 * 1024);
        using var provider = new AccessTelemetryLifecycleLoggerProvider(
            queue, new AccessTelemetrySanitizerAccessor(), new AccessTelemetryLifecycleStatus(enabled: true), time, accounting);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        ILogger<AccessTelemetryCategory> logger = loggerFactory.CreateLogger<AccessTelemetryCategory>();

        AccessTelemetryLog.LogTenantLifecycleAccess(logger, CreateEvent(RunId, SegmentId, 0));

        AccessTelemetryQualificationRejectionBreakdown breakdown = accounting.RejectionsForCorrelation(
            "qualification-" + AccessTelemetryQualificationWorkloadRunner.QualificationCorrelation(RunId, SegmentId));
        breakdown.SanitizerUnavailable.ShouldBe(1);
        breakdown.SanitizerUnavailableLifecycleReasons.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["RemoteValidationPending"] = 1 });
        breakdown.SanitizerUnavailableBootstrapStages.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal)
        {
            [AccessTelemetryQualificationRejectionBreakdown.Unobserved] = 1,
        });
    }

    private static AccessTelemetryEvent CreateEvent(string runId, string segmentId, int ordinal)
        => AccessTelemetryLog.CreateEvent(
            7506,
            "qualification-tenant",
            AccessTelemetryLog.OperationTenantLifecycle,
            caseId: null,
            user: "qualification-runner",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["operation"] = "tenant-create",
                ["state"] = "completed",
                ["workflowInstanceIdPrefix"] = AccessTelemetryQualificationWorkloadRunner.QualificationIdentity(runId, segmentId, ordinal),
            },
            resultCount: null,
            durationMs: 0,
            AccessTelemetryLog.OutcomeOk,
            errorCode: null,
            currentActivity: null) with
        {
            Timestamp = Now.AddMilliseconds(ordinal).UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        };
}
