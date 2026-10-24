// <copyright file="AccessTelemetryQualificationRejectionBreakdownTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

/// <summary>
/// Guards the bounded, secret-free pre-queue rejection breakdown returned by the fixed qualification workload.
/// </summary>
/// <remarks>
/// The retained consumed local run <c>dd8512086bb377547e89eda7d028b986</c> returned 125 attempted / 125 rejected /
/// zero enqueued for cohort A without any reason. These cases replay that exact run and segment identity through the
/// production-rate runner (125 records in one second) resolved from dependency injection. They distinguish an
/// unpublished sanitizer from a sanitizer refusal and, through the bootstrap progress, emission before publication from
/// a persistently failing bootstrap stage. They use only the response JSON, so they compile against the pre-breakdown
/// response shape.
/// </remarks>
public sealed class AccessTelemetryQualificationRejectionBreakdownTests
{
    private const string RetainedRunId = "dd8512086bb377547e89eda7d028b986";
    private const string RetainedSegmentId = "cohort-a";

    // The retained local-profile declaration selected this marker-key generation for the Server.
    private const string RetainedMarkerKeyGeneration = "development-key-1";

    // Approximate retained cohort-A emission; record identities depend only on run, segment and ordinal.
    private static readonly DateTimeOffset RetainedEmission = DateTimeOffset.FromUnixTimeMilliseconds(1792859272600);

    [Fact]
    public async Task UnpublishedSanitizer_ReproducesTheRetainedAllRejectedShapeAndReportsTheLifecycleReason()
    {
        var time = new FakeTimeProvider(RetainedEmission);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(125, 125 * 1024);
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        status.Publish(AccessTelemetryHealthState.Degraded, AccessTelemetryReason.CapabilityUnproven);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        var accessor = new AccessTelemetrySanitizerAccessor();

        JsonElement json = await RunRetainedSegmentAsync(time, accounting, queue, accessor, status, progress, persistQueued: false);

        // The retained response counters are reproduced exactly by the matched source.
        json.GetProperty("attempted").GetInt64().ShouldBe(125);
        json.GetProperty("rejected").GetInt64().ShouldBe(125);
        foreach (string name in new[] { "enqueued", "acknowledged", "persisted", "transactionAcknowledgements", "dropped", "conflicted" })
        {
            json.GetProperty(name).GetInt64().ShouldBe(0, name);
        }

        queue.Count.ShouldBe(0);
        JsonElement breakdown = RequireBreakdown(json);
        breakdown.GetProperty("sanitizerUnavailable").GetInt64().ShouldBe(125);
        breakdown.GetProperty("sanitationFailed").GetInt64().ShouldBe(0);
        Counts(breakdown, "sanitizerUnavailableLifecycleReasons").ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["CapabilityUnproven"] = 125 });

        // No bootstrap attempt started before emission: the "emission before publication" signature.
        Counts(breakdown, "sanitizerUnavailableBootstrapStages").ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["NotStarted"] = 125 });
        breakdown.GetProperty("lifecycleHealth").GetString().ShouldBe("Degraded");
        breakdown.GetProperty("lifecycleReason").GetString().ShouldBe("CapabilityUnproven");
        RequireBootstrap(breakdown, attempts: 0, stage: "NotStarted", failureStage: null, failureKind: null);
        breakdown.GetProperty("bootstrap").GetProperty("firstPublishedUtcMs").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task PublishedSanitizer_AcceptsEveryRetainedCohortIdentityAndReportsAnEmptyBreakdown()
    {
        var time = new FakeTimeProvider(RetainedEmission);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(125, 125 * 1024);
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        var accessor = new AccessTelemetrySanitizerAccessor();
        Publish(accessor, time, TimeSpan.FromHours(1));
        progress.BeginAttempt();
        progress.CompletePublished(time.GetUtcNow().AddSeconds(-30));
        status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);

        JsonElement json = await RunRetainedSegmentAsync(time, accounting, queue, accessor, status, progress, persistQueued: true);

        json.GetProperty("attempted").GetInt64().ShouldBe(125);
        json.GetProperty("enqueued").GetInt64().ShouldBe(125);
        json.GetProperty("persisted").GetInt64().ShouldBe(125);
        json.GetProperty("rejected").GetInt64().ShouldBe(0);
        JsonElement breakdown = RequireBreakdown(json);
        breakdown.GetProperty("sanitizerUnavailable").GetInt64().ShouldBe(0);
        breakdown.GetProperty("sanitationFailed").GetInt64().ShouldBe(0);
        Counts(breakdown, "sanitizerUnavailableLifecycleReasons").ShouldBeEmpty();
        Counts(breakdown, "sanitizerUnavailableBootstrapStages").ShouldBeEmpty();
        breakdown.GetProperty("lifecycleHealth").GetString().ShouldBe("Healthy");
        breakdown.GetProperty("lifecycleReason").GetString().ShouldBe("None");
        RequireBootstrap(breakdown, attempts: 1, stage: "Published", failureStage: null, failureKind: null);
        breakdown.GetProperty("bootstrap").GetProperty("firstPublishedUtcMs").GetInt64()
            .ShouldBe(RetainedEmission.AddSeconds(-30).ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task SanitizerRefusal_IsCountedSeparatelyFromSanitizerUnavailability()
    {
        var time = new FakeTimeProvider(RetainedEmission);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(125, 125 * 1024);
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        var accessor = new AccessTelemetrySanitizerAccessor();

        // Controlled refusal double: an unrepresentable expiry makes the published sanitizer refuse every record.
        Publish(accessor, time, TimeSpan.MaxValue);
        progress.BeginAttempt();
        progress.CompletePublished(time.GetUtcNow());

        JsonElement json = await RunRetainedSegmentAsync(time, accounting, queue, accessor, status, progress, persistQueued: false);

        json.GetProperty("attempted").GetInt64().ShouldBe(125);
        json.GetProperty("enqueued").GetInt64().ShouldBe(0);
        json.GetProperty("rejected").GetInt64().ShouldBe(125);
        JsonElement breakdown = RequireBreakdown(json);
        breakdown.GetProperty("sanitizerUnavailable").GetInt64().ShouldBe(0);
        breakdown.GetProperty("sanitationFailed").GetInt64().ShouldBe(125);
        Counts(breakdown, "sanitizerUnavailableLifecycleReasons").ShouldBeEmpty();
        Counts(breakdown, "sanitizerUnavailableBootstrapStages").ShouldBeEmpty();
        breakdown.GetProperty("lifecycleHealth").GetString().ShouldBe("Healthy");
        breakdown.GetProperty("lifecycleReason").GetString().ShouldBe("None");
        RequireBootstrap(breakdown, attempts: 1, stage: "Published", failureStage: null, failureKind: null);
    }

    [Fact]
    public async Task EarlierRejectedBaseline_IsSubtractedSoTheRunAfterPublicationReportsNoRejection()
    {
        var time = new FakeTimeProvider(RetainedEmission);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(125, 125 * 1024);
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        var accessor = new AccessTelemetrySanitizerAccessor();
        string correlation = "qualification-" + AccessTelemetryQualificationWorkloadRunner.QualificationCorrelation(RetainedRunId, RetainedSegmentId);

        // An earlier attempt for the same correlation was rejected before publication through the real logger path.
        using (var provider = new AccessTelemetryLifecycleLoggerProvider(queue, accessor, status, time, accounting, progress))
        using (ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider)))
        {
            ILogger<AccessTelemetryCategory> logger = loggerFactory.CreateLogger<AccessTelemetryCategory>();
            for (int ordinal = 0; ordinal < 7; ordinal++)
            {
                AccessTelemetryLog.LogTenantLifecycleAccess(logger, CreateEvent(RetainedRunId, RetainedSegmentId, ordinal, time));
            }
        }

        AccessTelemetryQualificationRejectionBreakdown baseline = accounting.RejectionsForCorrelation(correlation);
        baseline.SanitizerUnavailable.ShouldBe(7);
        baseline.SanitizerUnavailableLifecycleReasons.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["RemoteValidationPending"] = 7 });
        baseline.SanitizerUnavailableBootstrapStages.ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["NotStarted"] = 7 });
        accounting.ForCorrelation(correlation).Rejected.ShouldBe(7);

        Publish(accessor, time, TimeSpan.FromHours(1));
        progress.BeginAttempt();
        progress.CompletePublished(time.GetUtcNow());
        status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);

        JsonElement json = await RunRetainedSegmentAsync(time, accounting, queue, accessor, status, progress, persistQueued: true);

        json.GetProperty("attempted").GetInt64().ShouldBe(125);
        json.GetProperty("persisted").GetInt64().ShouldBe(125);
        json.GetProperty("rejected").GetInt64().ShouldBe(0);
        JsonElement breakdown = RequireBreakdown(json);
        breakdown.GetProperty("sanitizerUnavailable").GetInt64().ShouldBe(0);
        breakdown.GetProperty("sanitationFailed").GetInt64().ShouldBe(0);
        Counts(breakdown, "sanitizerUnavailableLifecycleReasons").ShouldBeEmpty();
        Counts(breakdown, "sanitizerUnavailableBootstrapStages").ShouldBeEmpty();

        // The cumulative correlation still holds the earlier rejections; only the per-segment delta is empty.
        accounting.ForCorrelation(correlation).Rejected.ShouldBe(7);
        accounting.RejectionsForCorrelation(correlation).SanitizerUnavailable.ShouldBe(7);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistentRemoteDenial_ReportsTheFailedStageAndKindWithoutPublication(bool includeCapabilityDiagnostic)
    {
        var time = new FakeTimeProvider(RetainedEmission);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(125, 125 * 1024);
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        var accessor = new AccessTelemetrySanitizerAccessor();
        var diagnostic = new AccessTelemetryCapabilityDiagnosticSnapshot("PreflightRefused", "LocalProfileInvalid",
            AccessTelemetryCapabilityDiagnosticValues.Capabilities.ToDictionary(name => name, _ => new AccessTelemetryCapabilityOutcome("NotStarted", "None"), StringComparer.Ordinal));
        DaprClient dapr = AccessTelemetryLifecycleBootstrapProgressTests.CreateDapr(
            new AccessTelemetryRuntimeValidationResponse(false, AccessTelemetryReason.CapabilityUnproven)
            {
                CapabilityDiagnostic = includeCapabilityDiagnostic ? diagnostic : null,
            });
        var bootstrap = AccessTelemetryLifecycleBootstrapProgressTests.CreateBootstrap(dapr, accessor, status, time, progress);

        await bootstrap.StartAsync(CancellationToken.None);
        try
        {
            await AccessTelemetryLifecycleBootstrapProgressTests.WaitForAsync(() => progress.Current.Attempts >= 1);

            // The denial branch records progress before publishing its reason; wait so every record sees that reason.
            await AccessTelemetryLifecycleBootstrapProgressTests.WaitForAsync(() => status.Current.Reason == AccessTelemetryReason.CapabilityUnproven);

            JsonElement json = await RunRetainedSegmentAsync(time, accounting, queue, accessor, status, progress, persistQueued: false);

            json.GetProperty("rejected").GetInt64().ShouldBe(125);
            JsonElement breakdown = RequireBreakdown(json);
            breakdown.GetProperty("sanitizerUnavailable").GetInt64().ShouldBe(125);
            breakdown.GetProperty("sanitationFailed").GetInt64().ShouldBe(0);
            Counts(breakdown, "sanitizerUnavailableLifecycleReasons").ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["CapabilityUnproven"] = 125 });
            Counts(breakdown, "sanitizerUnavailableBootstrapStages").ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["RemoteValidation"] = 125 });
            breakdown.GetProperty("lifecycleHealth").GetString().ShouldBe("Degraded");
            breakdown.GetProperty("lifecycleReason").GetString().ShouldBe("CapabilityUnproven");
            RequireBootstrap(breakdown, attempts: 1, stage: "RemoteValidation", failureStage: "RemoteValidation", failureKind: "RemoteDenied", capabilityDiagnostic: includeCapabilityDiagnostic);
            breakdown.GetProperty("bootstrap").GetProperty("firstPublishedUtcMs").ValueKind.ShouldBe(JsonValueKind.Null);
            breakdown.GetProperty("bootstrap").TryGetProperty("capabilityDiagnostic", out JsonElement retained).ShouldBe(includeCapabilityDiagnostic);
            if (includeCapabilityDiagnostic)
            {
                retained.GetProperty("disposition").GetString().ShouldBe("PreflightRefused");
                retained.GetProperty("preflightReason").GetString().ShouldBe("LocalProfileInvalid");
                retained.GetProperty("capabilities").EnumerateObject().Count().ShouldBe(10);
                progress.ObserveCapability(null);
                progress.Current.CapabilityDiagnostic.ShouldBeNull();
                retained.GetProperty("capabilities").EnumerateObject().Count().ShouldBe(10);
            }
            accessor.Current.ShouldBeNull();
        }
        finally
        {
            await bootstrap.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PublicationDuringTheBurst_CountsOnlyPrePublicationStagesAndRecordsTheFirstPublicationTime()
    {
        var time = new FakeTimeProvider(RetainedEmission);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(125, 125 * 1024);
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        var accessor = new AccessTelemetrySanitizerAccessor();
        progress.BeginAttempt();
        progress.Enter(AccessTelemetryLifecycleBootstrapStage.Secret);
        progress.Enter(AccessTelemetryLifecycleBootstrapStage.RemoteValidation);

        // After 60 records reach the lifecycle provider, bootstrap publishes in the same order as the hosted service.
        var publishAfter = new PublishAfterProvider(60, () =>
        {
            Publish(accessor, time, TimeSpan.FromHours(1));
            progress.CompletePublished(time.GetUtcNow());
            status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
        });

        JsonElement json = await RunRetainedSegmentAsync(time, accounting, queue, accessor, status, progress, persistQueued: true, publishAfter);

        json.GetProperty("attempted").GetInt64().ShouldBe(125);
        json.GetProperty("rejected").GetInt64().ShouldBe(60);
        json.GetProperty("enqueued").GetInt64().ShouldBe(65);
        json.GetProperty("persisted").GetInt64().ShouldBe(65);
        JsonElement breakdown = RequireBreakdown(json);
        breakdown.GetProperty("sanitizerUnavailable").GetInt64().ShouldBe(60);
        Counts(breakdown, "sanitizerUnavailableLifecycleReasons").ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["RemoteValidationPending"] = 60 });
        Counts(breakdown, "sanitizerUnavailableBootstrapStages").ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal) { ["RemoteValidation"] = 60 });
        RequireBootstrap(breakdown, attempts: 1, stage: "Published", failureStage: null, failureKind: null);
        long firstPublished = breakdown.GetProperty("bootstrap").GetProperty("firstPublishedUtcMs").GetInt64();
        firstPublished.ShouldBeInRange(json.GetProperty("startedUtcMs").GetInt64(), json.GetProperty("emitFinishedUtcMs").GetInt64());
    }

    private static async Task<JsonElement> RunRetainedSegmentAsync(
        FakeTimeProvider time,
        AccessTelemetryQualificationAccounting accounting,
        BoundedAccessTelemetryQueue queue,
        AccessTelemetrySanitizerAccessor accessor,
        AccessTelemetryLifecycleStatus status,
        AccessTelemetryLifecycleBootstrapProgress progress,
        bool persistQueued,
        ILoggerProvider? afterLifecycle = null)
    {
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, accessor, status, time, accounting, progress);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(provider);
            if (afterLifecycle is not null)
            {
                builder.AddProvider(afterLifecycle);
            }
        });
        string gatePath = Path.GetTempFileName();
        File.WriteAllText(
            gatePath,
            "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" + AccessTelemetryQualificationGate.ApprovedProfileSha256 +
            "\",\"expiresUtcMs\":" + time.GetUtcNow().AddMinutes(5).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) + "}");
        try
        {
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns("Qualification");
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AccessTelemetryQualification:GatePath"] = gatePath })
                .Build();
            var services = new ServiceCollection();
            services.AddSingleton(loggerFactory.CreateLogger<AccessTelemetryCategory>());
            services.AddSingleton(accounting);
            services.AddSingleton(new AccessTelemetryQualificationGate(environment, configuration, time));
            services.AddSingleton<TimeProvider>(time);
            services.AddSingleton(status);
            services.AddSingleton(progress);
            services.AddSingleton<AccessTelemetryQualificationWorkloadRunner>();
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            AccessTelemetryQualificationWorkloadRunner runner = serviceProvider.GetRequiredService<AccessTelemetryQualificationWorkloadRunner>();
            long attemptedBefore = accounting.Current.Attempted;

            Task<AccessTelemetryQualificationWorkloadResult> pending = runner.RunAsync(
                RetainedRunId,
                RetainedSegmentId,
                time.GetUtcNow().ToUnixTimeMilliseconds(),
                CancellationToken.None);
            for (int attempt = 0; attempt < 200 && accounting.Current.Attempted - attemptedBefore < 125; attempt++)
            {
                await Task.Delay(5, TestContext.Current.CancellationToken);
            }

            (accounting.Current.Attempted - attemptedBefore).ShouldBe(125);
            if (persistQueued)
            {
                IReadOnlyList<AccessTelemetryRecord> batch = queue.PeekBatch(125, 125 * 1024);
                batch.Count.ShouldBe(queue.Count);
                accounting.RecordPersisted(batch, 0, batch.Count);
            }

            time.Advance(TimeSpan.FromSeconds(1));
            AccessTelemetryQualificationWorkloadResult result = await pending.WaitAsync(
                TimeSpan.FromSeconds(30),
                TestContext.Current.CancellationToken);
            result.RunId.ShouldBe(RetainedRunId);
            result.SegmentId.ShouldBe(RetainedSegmentId);
            result.RecordIds.Count.ShouldBe(125);
            return JsonSerializer.SerializeToElement(result, MemoriesJsonContext.Options);
        }
        finally
        {
            File.Delete(gatePath);
        }
    }

    private static void Publish(AccessTelemetrySanitizerAccessor accessor, TimeProvider time, TimeSpan retention)
        => accessor.Publish(new AccessTelemetrySanitizer(
            RandomNumberGenerator.GetBytes(32),
            RetainedMarkerKeyGeneration,
            time,
            new MonotonicRecordIdGenerator(),
            retention,
            qualificationMode: true));

    private static AccessTelemetryEvent CreateEvent(string runId, string segmentId, int ordinal, TimeProvider time)
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
            Timestamp = time.GetUtcNow().AddMilliseconds(ordinal).UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        };

    private static JsonElement RequireBreakdown(JsonElement json)
    {
        json.TryGetProperty("rejectionBreakdown", out JsonElement breakdown)
            .ShouldBeTrue("the fixed-workload response must separate pre-queue rejection causes");
        breakdown.ValueKind.ShouldBe(JsonValueKind.Object);
        breakdown.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldBe(
            ["bootstrap", "lifecycleHealth", "lifecycleReason", "sanitationFailed", "sanitizerUnavailable", "sanitizerUnavailableBootstrapStages", "sanitizerUnavailableLifecycleReasons"]);
        return breakdown;
    }

    private static void RequireBootstrap(JsonElement breakdown, long attempts, string stage, string? failureStage, string? failureKind, bool capabilityDiagnostic = false)
    {
        JsonElement bootstrap = breakdown.GetProperty("bootstrap");
        bootstrap.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldBe(
            capabilityDiagnostic
                ? ["attempts", "capabilityDiagnostic", "failureKind", "failureStage", "firstPublishedUtcMs", "stage"]
                : ["attempts", "failureKind", "failureStage", "firstPublishedUtcMs", "stage"]);
        bootstrap.GetProperty("attempts").GetInt64().ShouldBe(attempts);
        bootstrap.GetProperty("stage").GetString().ShouldBe(stage);
        bootstrap.GetProperty("failureStage").GetString().ShouldBe(failureStage);
        bootstrap.GetProperty("failureKind").GetString().ShouldBe(failureKind);
    }

    private static Dictionary<string, long> Counts(JsonElement breakdown, string name)
        => breakdown.GetProperty(name)
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetInt64(), StringComparer.Ordinal);

    /// <summary>Runs a publication action once, after the lifecycle provider has seen a fixed number of records.</summary>
    private sealed class PublishAfterProvider(int records, Action publish) : ILoggerProvider, ILogger
    {
        private int _seen;

        public ILogger CreateLogger(string categoryName) => this;

        public void Dispose()
        {
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id == 7506 && Interlocked.Increment(ref _seen) == records)
            {
                publish();
            }
        }
    }
}
