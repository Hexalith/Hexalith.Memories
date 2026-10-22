// <copyright file="AccessTelemetryQualificationWorkloadTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;

using Dapr.Client;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

/// <summary>Guards the one no-input, exact-profile qualification workload.</summary>
[Collection("DaprTokenEnvironment")]
public sealed class AccessTelemetryQualificationWorkloadTests
{
    // Independently pin the adopted isolated-target identity and historical profiles.
    // Gate fixtures must not derive their inputs from the runtime constant.
    private const string Pg2ProfileSha256 = "95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb";
    private const string FormerPg2ProfileSha256 = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe";
    private const string Pg1ProfileSha256 = "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14";
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FixedWorkload_PinsTheApprovedPerWriterRateAndDuration()
    {
        AccessTelemetryQualificationWorkloadRunner.RecordsPerSecond.ShouldBe(125);
        AccessTelemetryQualificationWorkloadRunner.SteadyStateSeconds.ShouldBe(1_800);
        AccessTelemetryQualificationWorkloadRunner.SegmentSeconds.ShouldBe(1);
        AccessTelemetryQualificationEndpointExtensions.Route.ShouldBe(
            "/operations/access-telemetry/qualification/fixed-workload");
    }

    [Fact]
    public void Gate_AcceptsOnlyCurrentExactProfileInQualification()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                Pg2ProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            AccessTelemetryQualificationGate gate = CreateGate(path, "Qualification");

            gate.TryValidate(out string reason).ShouldBeTrue();
            reason.ShouldBe("none");

            CreateGate(path, Environments.Production).TryValidate(out _).ShouldBeFalse();
            foreach (string? emptyPath in new[] { string.Empty, " ", "\t" })
            {
                IHostEnvironment qualification = Substitute.For<IHostEnvironment>();
                qualification.EnvironmentName.Returns("Qualification");
                IConfiguration empty = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AccessTelemetryQualification:GatePath"] = emptyPath,
                    })
                    .Build();
                new AccessTelemetryQualificationGate(qualification, empty, new FakeTimeProvider(Now))
                    .TryValidate(out string unavailable)
                    .ShouldBeFalse();
                unavailable.ShouldBe("qualification_gate_unavailable");
            }

            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                Pg2ProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMilliseconds(-1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                Pg2ProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(15).AddSeconds(4).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            gate.TryValidate(out reason).ShouldBeTrue(reason);

            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                Pg2ProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(15).AddSeconds(6).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                Pg2ProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(16).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                Pg2ProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                ",\"extra\":true}");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                new string('f', 64) +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(Pg1ProfileSha256)]
    [InlineData(FormerPg2ProfileSha256)]
    [InlineData("95AA36E8ABA8D9914566F6DF894C33098739251040DD0CC4B2917F76A994CCDB")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("7F9F69322353CB22EC1254F1D486EE12337C9A9D579DBC80D6D842D32B339EFE")]
    public void Gate_RejectsHistoricalOrInexactProfile(string profileSha256)
    {
        string path = WriteGate(Now.AddMinutes(1), profileSha256);
        try
        {
            CreateGate(path, "Qualification").TryValidate(out string reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    [InlineData("Staging")]
    public void Gate_RejectsPg2OutsideQualification(string environmentName)
    {
        string path = WriteGate(Now.AddMinutes(1));
        try
        {
            CreateGate(path, environmentName).TryValidate(out string reason).ShouldBeFalse();
            reason.ShouldBe("qualification_environment_required");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Gate_RevalidatesPg2ExpiryRenewalAndRevocationWithoutCaching()
    {
        var time = new FakeTimeProvider(Now);
        string path = WriteGate(Now.AddMinutes(1));
        try
        {
            AccessTelemetryQualificationGate gate = CreateGate(path, "Qualification", time);
            gate.TryValidate(out string reason).ShouldBeTrue(reason);

            time.Advance(TimeSpan.FromMinutes(1));
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            DateTimeOffset renewedExpiry = time.GetUtcNow().AddMinutes(15).AddSeconds(5);
            foreach (string historicalProfile in new[] { Pg1ProfileSha256, FormerPg2ProfileSha256 })
            {
                WriteGate(path, renewedExpiry, historicalProfile);
                gate.TryValidate(out reason).ShouldBeFalse();
                reason.ShouldBe("qualification_gate_invalid_or_expired");
            }

            WriteGate(path, renewedExpiry);
            gate.TryValidate(out reason).ShouldBeTrue(reason);

            WriteGate(path, renewedExpiry.AddMilliseconds(1));
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            WriteGate(path, renewedExpiry, state: "disabled");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            WriteGate(path, renewedExpiry);
            gate.TryValidate(out reason).ShouldBeTrue(reason);

            File.Delete(path);
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_unavailable");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Gate_AcceptsAConfigMapProjectionSymlinkOnlyInsideItsMount()
    {
        string mount = Path.Combine(Path.GetTempPath(), $"qualification-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(mount);
        var time = new FakeTimeProvider(Now);
        PublishProjectedGate(mount, "..initial", Now.AddMinutes(1));
        string link = Path.Combine(mount, "gate.json");
        _ = File.CreateSymbolicLink(link, Path.Combine("..data", "gate.json"));
        try
        {
            AccessTelemetryQualificationGate gate = CreateGate(link, "Qualification", time);
            gate.TryValidate(out string reason).ShouldBeTrue(reason);

            time.Advance(TimeSpan.FromMinutes(1));
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            DateTimeOffset renewedExpiry = time.GetUtcNow().AddMinutes(1);
            PublishProjectedGate(mount, "..renewed", renewedExpiry);
            gate.TryValidate(out reason).ShouldBeTrue(reason);

            PublishProjectedGate(mount, "..historical", renewedExpiry, Pg1ProfileSha256);
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            PublishProjectedGate(mount, "..disabled", renewedExpiry, state: "disabled");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");

            PublishProjectedGate(mount, "..current", renewedExpiry);
            gate.TryValidate(out reason).ShouldBeTrue(reason);

            File.Delete(link);
            string outside = Path.GetTempFileName();
            try
            {
                File.WriteAllText(outside, File.ReadAllText(Path.Combine(mount, "..data", "gate.json")));
                _ = File.CreateSymbolicLink(link, outside);
                gate.TryValidate(out reason).ShouldBeFalse();
                reason.ShouldBe("qualification_gate_unavailable");
            }
            finally
            {
                File.Delete(outside);
            }
        }
        finally
        {
            Directory.Delete(mount, recursive: true);
        }
    }

    [Fact]
    public void Accounting_ReportsOnlyMonotonicDeltas()
    {
        var accounting = new AccessTelemetryQualificationAccounting();
        AccessTelemetryQualificationAccountingSnapshot before = accounting.Current;

        accounting.RecordAttempted();
        accounting.RecordEnqueued();
        accounting.RecordPersisted(1);
        accounting.RecordRejected(2, conflicted: true);
        accounting.RecordDropped();

        accounting.Current.Since(before).ShouldBe(
            new AccessTelemetryQualificationAccountingSnapshot(1, 1, 1, 2, 1, 2));
    }

    [Fact]
    public async Task Runner_OverlappingProducerAndWorkerPersistAll125RecordsWithExactSegmentAccounting()
    {
        var time = new FakeTimeProvider(Now);
        var queue = new BoundedAccessTelemetryQueue(125, 125 * 1024);
        var accounting = new AccessTelemetryQualificationAccounting();
        var accessor = new AccessTelemetrySanitizerAccessor();
        accessor.Publish(new AccessTelemetrySanitizer(
            RandomNumberGenerator.GetBytes(32), "mk-qualification", time,
            new MonotonicRecordIdGenerator(), TimeSpan.FromHours(1), qualificationMode: true));
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        using var provider = new AccessTelemetryLifecycleLoggerProvider(queue, accessor, status, time, accounting);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var firstPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lastPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var resumeProducer = new ManualResetEventSlim();
        using var finishEmission = new ManualResetEventSlim();
        int published = 0;
        int producerThread = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == ServerAccessTelemetryLifecycleMetrics.MeterName &&
                instrument.Name == AccessTelemetryMetricContract.Records)
            {
                current.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            // Scope this barrier to the one synchronous fixed-workload producer.
            if (Environment.CurrentManagedThreadId != Volatile.Read(ref producerThread))
            {
                return;
            }

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "state" && Equals(tag.Value, "enqueued"))
                {
                    published++;
                    if (published == 1)
                    {
                        firstPublished.SetResult();
                        resumeProducer.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                    }
                    else if (published == 125)
                    {
                        lastPublished.SetResult();
                        finishEmission.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                        // Delivery is complete before the runner observes the end of emission.
                        time.Advance(TimeSpan.FromSeconds(1));
                    }
                }
            }
        });
        listener.Start();
        var persisted = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
        client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                IReadOnlyList<AccessTelemetryRecord> batch = call.Arg<IReadOnlyList<AccessTelemetryRecord>>();
                foreach (AccessTelemetryRecord record in batch)
                {
                    persisted.Add(record.RecordId, AccessTelemetryCanonicalizer.CanonicalizeRecord(record));
                }

                return new AccessTelemetryWriteBatchResponse { Accepted = batch.Count, Rejected = 0, Reason = AccessTelemetryReason.None };
            });
        var worker = new AccessTelemetryDeliveryWorker(queue, client, time, new AccessTelemetryOptions(), status, accounting);
        string gatePath = WriteGate(Now.AddMinutes(5));
        Task<AccessTelemetryQualificationWorkloadResult>? emitting = null;
        try
        {
            var runner = new AccessTelemetryQualificationWorkloadRunner(
                loggerFactory.CreateLogger<AccessTelemetryCategory>(), accounting,
                CreateGate(gatePath, "Qualification", time), time);
            emitting = Task.Run(() =>
            {
                Volatile.Write(ref producerThread, Environment.CurrentManagedThreadId);
                return runner.RunAsync("run-overlap", "writer-1-segment-0001", Now.ToUnixTimeMilliseconds(),
                    waitForAcknowledgement: false, CancellationToken.None);
            });
            await firstPublished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await worker.DrainOnceAsync(CancellationToken.None);
            string correlation = "qualification-" + AccessTelemetryQualificationWorkloadRunner.QualificationCorrelation(
                "run-overlap", "writer-1-segment-0001");
            // The consumer acknowledges before the producer returns from its first Log call.
            accounting.ForCorrelation(correlation).ShouldBe(new AccessTelemetryQualificationAccountingSnapshot(1, 1, 1, 0, 0, 0));
            emitting.IsCompleted.ShouldBeFalse();
            resumeProducer.Set();
            await lastPublished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            queue.Count.ShouldBe(124);
            await worker.DrainOnceAsync(CancellationToken.None);
            accounting.ForCorrelation(correlation).Persisted.ShouldBe(125);
            emitting.IsCompleted.ShouldBeFalse();
            finishEmission.Set();
            AccessTelemetryQualificationWorkloadResult emitted = await emitting.WaitAsync(TimeSpan.FromSeconds(5));
            emitted.Attempted.ShouldBe(125);
            emitted.Enqueued.ShouldBe(125);
            AccessTelemetryQualificationWorkloadResult result = await runner.RunAsync(
                "run-overlap", "writer-1-segment-0001", Now.ToUnixTimeMilliseconds(), CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5));

            result.Attempted.ShouldBe(125);
            result.Enqueued.ShouldBe(125);
            result.Persisted.ShouldBe(125);
            result.Acknowledged.ShouldBe(125);
            result.Rejected.ShouldBe(0);
            result.Dropped.ShouldBe(0);
            result.Conflicted.ShouldBe(0);
            persisted.Count.ShouldBe(125);
            persisted.Keys.Order(StringComparer.Ordinal).ShouldBe(result.RecordIds.Order(StringComparer.Ordinal));
            persisted.Values.ShouldAllBe(static bytes => bytes.Length <= 1024);
            accounting.Current.ShouldBe(new AccessTelemetryQualificationAccountingSnapshot(125, 125, 125, 0, 0, 0));
            accounting.ForCorrelation(correlation).ShouldBe(accounting.Current);
            queue.Count.ShouldBe(0);
            queue.ByteCount.ShouldBe(0);
        }
        finally
        {
            resumeProducer.Set();
            finishEmission.Set();
            time.Advance(TimeSpan.FromMinutes(6));
            if (emitting is not null)
            {
                try
                {
                    await emitting.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (InvalidOperationException)
                {
                    // Gate expiry releases a producer if an assertion failed before normal completion.
                }
            }

            File.Delete(gatePath);
        }
    }

    [Fact]
    public async Task Runner_EmitsThroughTheLifecycleProviderAndReportsDeliveryAcknowledgement()
    {
        var time = new FakeTimeProvider(Now);
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        var accounting = new AccessTelemetryQualificationAccounting();
        var accessor = new AccessTelemetrySanitizerAccessor();
        accessor.Publish(new AccessTelemetrySanitizer(
            RandomNumberGenerator.GetBytes(32),
            "mk-qualification",
            time,
            new MonotonicRecordIdGenerator(),
            TimeSpan.FromHours(24),
            qualificationMode: true));
        using var provider = new AccessTelemetryLifecycleLoggerProvider(
            queue,
            accessor,
            new AccessTelemetryLifecycleStatus(enabled: true),
            time,
            accounting);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        string gatePath = WriteGate(Now.AddMinutes(5));
        try
        {
            var runner = new AccessTelemetryQualificationWorkloadRunner(
                loggerFactory.CreateLogger<AccessTelemetryCategory>(),
                accounting,
                CreateGate(gatePath, "Qualification", time),
                time,
                recordsPerSecond: 1,
                steadyStateSeconds: 1);
            Task<AccessTelemetryQualificationWorkloadResult> pending = runner.RunAsync(
                "run-001",
                "writer-1-segment-0001",
                Now.ToUnixTimeMilliseconds(),
                CancellationToken.None);
            Task<AccessTelemetryQualificationWorkloadResult> concurrent = runner.RunAsync(
                "run-001",
                "writer-1-segment-0001",
                Now.ToUnixTimeMilliseconds(),
                CancellationToken.None);
            using var cancelledWaiter = new CancellationTokenSource();
            Task<AccessTelemetryQualificationWorkloadResult> cancelled = runner.RunAsync(
                "run-001",
                "writer-1-segment-0001",
                Now.ToUnixTimeMilliseconds(),
                cancelledWaiter.Token);
            cancelledWaiter.Cancel();
            for (int attempt = 0; attempt < 20 && queue.Count == 0; attempt++)
            {
                await Task.Yield();
            }

            queue.Count.ShouldBe(1);
            await Should.ThrowAsync<OperationCanceledException>(() => cancelled);
            IAccessTelemetryDeliveryClient client = Substitute.For<IAccessTelemetryDeliveryClient>();
            client.SendAsync(Arg.Any<IReadOnlyList<AccessTelemetryRecord>>(), Arg.Any<CancellationToken>())
                .Returns(new AccessTelemetryWriteBatchResponse
                {
                    Accepted = 1,
                    Rejected = 0,
                    Reason = AccessTelemetryReason.None,
                });
            var worker = new AccessTelemetryDeliveryWorker(
                queue,
                client,
                time,
                new AccessTelemetryOptions(),
                new AccessTelemetryLifecycleStatus(enabled: true),
                accounting);
            time.Advance(TimeSpan.FromSeconds(1));
            await worker.DrainOnceAsync(CancellationToken.None);
            time.Advance(TimeSpan.FromMilliseconds(100));

            AccessTelemetryQualificationWorkloadResult result = await pending;
            (await concurrent).ShouldBe(result);
            result.RunId.ShouldBe("run-001");
            result.SegmentId.ShouldBe("writer-1-segment-0001");
            result.Attempted.ShouldBe(1);
            result.Enqueued.ShouldBe(1);
            result.Acknowledged.ShouldBe(1);
            result.Persisted.ShouldBe(1);
            result.Dropped.ShouldBe(0);
            result.Rejected.ShouldBe(0);
            result.RecordIds.Count.ShouldBe(1);
            result.RecordIds[0].ShouldMatch("^[0-9A-HJKMNP-TV-Z]{26}$");
            result.EmitFinishedUtcMs.ShouldBe(result.StartedUtcMs + 1000);
            result.FinishedUtcMs.ShouldBe(result.EmitFinishedUtcMs);
            result.AcknowledgedUtcMs.ShouldBeGreaterThan(result.EmitFinishedUtcMs);
            AccessTelemetryQualificationWorkloadResult retry = await runner.RunAsync(
                "run-001",
                "writer-1-segment-0001",
                Now.ToUnixTimeMilliseconds(),
                CancellationToken.None);
            retry.ShouldBe(result);
            queue.Count.ShouldBe(0);
            await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
                "run-001",
                "writer-1-segment-0001",
                Now.AddMilliseconds(1).ToUnixTimeMilliseconds(),
                CancellationToken.None));

            time.Advance(TimeSpan.FromMinutes(5));
            InvalidOperationException expiredReplay = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
                "run-001",
                "writer-1-segment-0001",
                Now.ToUnixTimeMilliseconds(),
                CancellationToken.None));
            expiredReplay.Message.ShouldBe("qualification_gate_invalid_or_expired");
            accounting.Current.Attempted.ShouldBe(1);
            queue.Count.ShouldBe(0);
        }
        finally
        {
            File.Delete(gatePath);
        }
    }

    [Fact]
    public async Task Program_MapsTheQualificationRouteOnlyInTheQualificationEnvironment()
    {
        string? originalAppToken = Environment.GetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable);
        string? originalDaprToken = Environment.GetEnvironmentVariable(
            DaprTokenStartupValidator.DaprApiTokenEnvironmentVariable);
        string gatePath = Path.GetTempFileName();
        File.WriteAllText(
            gatePath,
            "{\"schemaVersion\":1,\"state\":\"disabled\",\"profileSha256\":\"" +
            Pg2ProfileSha256 +
            "\",\"expiresUtcMs\":0}");
        try
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                "qualification-program-app-token");
            Environment.SetEnvironmentVariable(
                DaprTokenStartupValidator.DaprApiTokenEnvironmentVariable,
                "qualification-program-dapr-token");
            using var baseFactory = new TelemetryWebAppFactory();
            using WebApplicationFactory<Program> qualificationFactory = baseFactory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Qualification");
                ConfigureProgramFactory(builder, gatePath);
            });
            using WebApplicationFactory<Program> productionFactory = baseFactory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Production);
                ConfigureProgramFactory(builder, gatePath);
            });

            using HttpClient qualificationClient = qualificationFactory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpClient productionClient = productionFactory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpRequestMessage qualificationRequest = CreateProgramRequest();
            using HttpRequestMessage productionRequest = CreateProgramRequest();
            qualificationRequest.Headers.Add(
                DaprApplicationTokenMiddleware.DaprApiTokenHeader,
                "qualification-program-app-token");
            productionRequest.Headers.Add(
                DaprApplicationTokenMiddleware.DaprApiTokenHeader,
                "qualification-program-app-token");

            using HttpResponseMessage qualificationResponse = await qualificationClient.SendAsync(
                qualificationRequest,
                TestContext.Current.CancellationToken);
            using HttpResponseMessage productionResponse = await productionClient.SendAsync(
                productionRequest,
                TestContext.Current.CancellationToken);

            qualificationResponse.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            productionResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                originalAppToken);
            Environment.SetEnvironmentVariable(
                DaprTokenStartupValidator.DaprApiTokenEnvironmentVariable,
                originalDaprToken);
            File.Delete(gatePath);
        }
    }

    [Fact]
    public async Task Endpoint_RejectsAnUntrustedCallerAndAcceptsTheDaprTokenWithCurrentGate()
    {
        const string AppToken = "qualification-app-token";
        string? originalToken = Environment.GetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable);
        string gatePath = WriteGate(Now.AddMinutes(5));
        var time = new FakeTimeProvider(Now);
        var accounting = new AccessTelemetryQualificationAccounting();
        var queue = new BoundedAccessTelemetryQueue(8, 8192);
        var accessor = new AccessTelemetrySanitizerAccessor();
        accessor.Publish(new AccessTelemetrySanitizer(
            RandomNumberGenerator.GetBytes(32),
            "mk-qualification",
            time,
            new MonotonicRecordIdGenerator(),
            TimeSpan.FromHours(24),
            qualificationMode: true));
        using var provider = new AccessTelemetryLifecycleLoggerProvider(
            queue,
            accessor,
            new AccessTelemetryLifecycleStatus(enabled: true),
            time,
            accounting);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(provider));
        try
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                AppToken);
            WebApplicationBuilder builder = WebApplication.CreateBuilder(
                new WebApplicationOptions { EnvironmentName = "Qualification" });
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton(new AccessTelemetryQualificationWorkloadRunner(
                loggerFactory.CreateLogger<AccessTelemetryCategory>(),
                accounting,
                CreateGate(gatePath, "Qualification", time),
                time,
                recordsPerSecond: 1,
                steadyStateSeconds: 1));
            WebApplication app = builder.Build();
            app.UseMiddleware<DaprApplicationTokenMiddleware>();
            app.MapAccessTelemetryQualificationEndpoint();
            await app.StartAsync();
            try
            {
                HttpClient client = app.GetTestClient();
                HttpResponseMessage rejected = await client.PostAsync(
                    AccessTelemetryQualificationEndpointExtensions.Route,
                    content: null);
                rejected.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

                using var acceptedRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    AccessTelemetryQualificationEndpointExtensions.Route);
                acceptedRequest.Headers.Add(DaprApplicationTokenMiddleware.DaprApiTokenHeader, AppToken);
                acceptedRequest.Headers.Add(AccessTelemetryQualificationEndpointExtensions.RunHeader, "run-001");
                acceptedRequest.Headers.Add(
                    AccessTelemetryQualificationEndpointExtensions.SegmentHeader,
                    "writer-1-segment-0001");
                acceptedRequest.Headers.Add(
                    AccessTelemetryQualificationEndpointExtensions.EmittedUtcMsHeader,
                    Now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
                Task<HttpResponseMessage> acceptedTask = client.SendAsync(acceptedRequest);
                for (int attempt = 0; attempt < 100 && accounting.Current.Enqueued == 0 && !acceptedTask.IsCompleted; attempt++)
                {
                    await Task.Delay(10);
                }

                accounting.Current.Enqueued.ShouldBe(1, acceptedTask.IsCompleted
                    ? (await acceptedTask).StatusCode.ToString()
                    : "request still running");
                IReadOnlyList<AccessTelemetryRecord> batch = queue.PeekBatch(1, 8192);
                accounting.RecordPersisted(batch, 0, batch.Count);
                await Task.Yield();
                time.Advance(TimeSpan.FromSeconds(1));
                HttpResponseMessage accepted = await acceptedTask;
                accepted.StatusCode.ShouldBe(HttpStatusCode.OK);

                AccessTelemetryQualificationAccountingSnapshot beforeRevocation = accounting.Current;
                WriteGate(gatePath, time.GetUtcNow().AddMinutes(5), state: "disabled");
                using var revokedRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    AccessTelemetryQualificationEndpointExtensions.Route);
                foreach (var header in acceptedRequest.Headers)
                {
                    revokedRequest.Headers.Add(header.Key, header.Value);
                }

                using HttpResponseMessage revoked = await client.SendAsync(revokedRequest);
                revoked.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
                (await revoked.Content.ReadAsStringAsync()).ShouldContain("qualification_gate_invalid_or_expired");
                accounting.Current.ShouldBe(beforeRevocation);
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                originalToken);
            File.Delete(gatePath);
        }
    }

    [Fact]
    public void CrockfordQualificationIds_MatchTheSharedPythonGoldenVector()
    {
        AccessTelemetrySanitizer.CreateQualificationRecordId(
            AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenIdentity)
            .ShouldBe(AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenRecordId);
        AccessTelemetrySanitizer.CreateQualificationRecordId(
            AccessTelemetryQualificationWorkloadRunner.QualificationIdentity(
                AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenRunId,
                AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenSegmentId,
                AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenOrdinal))
            .ShouldBe(AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenRunSegmentRecordId);
    }

    [Fact]
    public async Task HostedQualificationBootstrap_PublishesCrockfordIdentitiesMatchingTheRunner()
    {
        var time = new FakeTimeProvider(Now);
        var accessor = new AccessTelemetrySanitizerAccessor();
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Qualification");
        byte[] markerKey = RandomNumberGenerator.GetBytes(32);
        AccessTelemetryOptions options = new()
        {
            Enabled = true,
            Retention = TimeSpan.FromHours(24),
            RetentionSource = RetentionConfigurationSource.DevelopmentDefault,
            DeploymentId = "development",
            ComponentProfileHash = new string('a', 64),
            ConfigurationEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            MarkerKeyReference = "access-telemetry-marker",
            MarkerKeyGeneration = "mk-2026a",
            AttestationVerificationKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CapacityEvidenceId = "development-capacity",
            PhysicalReclamationEvidenceId = "pending-story-27-3",
            PhysicalReclamationReporterImageDigest = new string('d', 64),
        };
        DaprClient dapr = Substitute.For<DaprClient>();
        dapr.GetSecretAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Dictionary<string, string>
            {
                ["access-telemetry-marker"] = Convert.ToBase64String(markerKey),
            }));
        dapr.CreateInvokeMethodRequest(
                Arg.Any<HttpMethod>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<KeyValuePair<string, string>>>(),
                Arg.Any<AccessTelemetryRuntimeValidationRequest>())
            .Returns(new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1/v1/access-telemetry/validate"));
#pragma warning disable CS0618 // DaprClient 1.18 typed service invocation is obsolete without a native typed helper.
        dapr.InvokeMethodAsync<AccessTelemetryRuntimeValidationResponse>(
                Arg.Any<HttpRequestMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(new AccessTelemetryRuntimeValidationResponse(true, AccessTelemetryReason.None));
#pragma warning restore CS0618
        var bootstrap = new AccessTelemetryLifecycleBootstrapService(
            dapr,
            options,
            accessor,
            status,
            time,
            new MonotonicRecordIdGenerator(),
            environment);

        await bootstrap.StartAsync(CancellationToken.None);
        try
        {
            for (int attempt = 0; attempt < 50 && accessor.Current is null; attempt++)
            {
                await Task.Delay(20);
            }

            AccessTelemetrySanitizer sanitizer = accessor.Current.ShouldNotBeNull();
            AccessTelemetryEvent source = AccessTelemetryLog.CreateEvent(
                7506,
                "qualification-tenant",
                AccessTelemetryLog.OperationTenantLifecycle,
                caseId: null,
                user: "qualification-runner",
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["operation"] = "tenant-create",
                    ["state"] = "completed",
                    ["workflowInstanceIdPrefix"] = AccessTelemetryQualificationWorkloadRunner.QualificationIdentity(
                        AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenRunId,
                        AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenSegmentId,
                        AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenOrdinal),
                },
                resultCount: null,
                durationMs: 0,
                AccessTelemetryLog.OutcomeOk,
                errorCode: null,
                currentActivity: null) with
            {
                Timestamp = Now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            };
            sanitizer.TrySanitize(
                LogLevel.Information,
                new EventId(7506),
                source,
                out AccessTelemetryRecord? record,
                out AccessTelemetryReason reason).ShouldBeTrue();
            reason.ShouldBe(AccessTelemetryReason.None);
            record.ShouldNotBeNull();
            record.RecordId.ShouldBe(AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenRunSegmentRecordId);
        }
        finally
        {
            await bootstrap.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public void Sanitizer_UsesStorePersistTimeForQualificationAcceptedAtUtc()
    {
        var time = new FakeTimeProvider(Now);
        var sanitizer = new AccessTelemetrySanitizer(
            RandomNumberGenerator.GetBytes(32),
            "mk-qualification",
            time,
            new MonotonicRecordIdGenerator(),
            TimeSpan.FromHours(24),
            qualificationMode: true);
        AccessTelemetryEvent source = AccessTelemetryLog.CreateEvent(
            7506,
            "qualification-tenant",
            AccessTelemetryLog.OperationTenantLifecycle,
            caseId: null,
            user: "qualification-runner",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["operation"] = "tenant-create",
                ["state"] = "completed",
                ["workflowInstanceIdPrefix"] = AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenIdentity,
            },
            resultCount: null,
            durationMs: 0,
            AccessTelemetryLog.OutcomeOk,
            errorCode: null,
            currentActivity: null) with
        {
            Timestamp = Now.AddSeconds(-12).UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        };

        sanitizer.TrySanitize(
            LogLevel.Information,
            new EventId(7506),
            source,
            out AccessTelemetryRecord? record,
            out AccessTelemetryReason reason).ShouldBeTrue();
        reason.ShouldBe(AccessTelemetryReason.None);
        record.ShouldNotBeNull();
        record.AcceptedAtUtc.ShouldBe("2026-09-05T12:00:00.000Z");
        record.EmittedAtUtc.ShouldBe("2026-09-05T11:59:48.000Z");
        record.RecordId.ShouldBe(AccessTelemetryQualificationWorkloadRunner.CrockfordGoldenRecordId);
    }

    [Fact]
    public async Task Runner_EvictsAFaultedSegmentLazySoTheIdentityCanRetry()
    {
        var time = new FakeTimeProvider(Now);
        var accounting = new AccessTelemetryQualificationAccounting();
        var logger = new ThrowOnceLogger();
        string gatePath = WriteGate(Now.AddMinutes(5));
        try
        {
            var runner = new AccessTelemetryQualificationWorkloadRunner(
                logger,
                accounting,
                CreateGate(gatePath, "Qualification", time),
                time,
                recordsPerSecond: 1,
                steadyStateSeconds: 1);
            InvalidOperationException first = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
                "run-retry",
                "writer-1-segment-0001",
                Now.ToUnixTimeMilliseconds(),
                CancellationToken.None));
            first.Message.ShouldBe("first_fault");

            Task<AccessTelemetryQualificationWorkloadResult> retry = runner.RunAsync(
                "run-retry",
                "writer-1-segment-0001",
                Now.ToUnixTimeMilliseconds(),
                waitForAcknowledgement: false,
                CancellationToken.None);
            for (int attempt = 0; attempt < 50 && !retry.IsCompleted; attempt++)
            {
                time.Advance(TimeSpan.FromSeconds(1));
                await Task.Delay(10);
            }

            AccessTelemetryQualificationWorkloadResult result = await retry.WaitAsync(TimeSpan.FromSeconds(5));
            result.EmitFinishedUtcMs.ShouldBeGreaterThan(0);
            result.FinishedUtcMs.ShouldBe(result.EmitFinishedUtcMs);
        }
        finally
        {
            File.Delete(gatePath);
        }
    }

    [Fact]
    public async Task Endpoint_MapsCancelledWaitAsyncToABoundedNon500()
    {
        const string AppToken = "qualification-cancel-token";
        string? originalToken = Environment.GetEnvironmentVariable(
            DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable);
        string gatePath = WriteGate(Now.AddMinutes(5));
        var time = new FakeTimeProvider(Now);
        var accounting = new AccessTelemetryQualificationAccounting();
        try
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                AppToken);
            WebApplicationBuilder builder = WebApplication.CreateBuilder(
                new WebApplicationOptions { EnvironmentName = "Qualification" });
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton(new AccessTelemetryQualificationWorkloadRunner(
                Substitute.For<ILogger<AccessTelemetryCategory>>(),
                accounting,
                CreateGate(gatePath, "Qualification", time),
                time,
                recordsPerSecond: 1,
                steadyStateSeconds: 1));
            WebApplication app = builder.Build();
            app.UseMiddleware<DaprApplicationTokenMiddleware>();
            app.MapAccessTelemetryQualificationEndpoint();
            await app.StartAsync();
            try
            {
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                HttpContext context = await app.GetTestServer().SendAsync(http =>
                {
                    http.Request.Method = HttpMethods.Post;
                    http.Request.Path = AccessTelemetryQualificationEndpointExtensions.Route;
                    http.Request.Headers[DaprApplicationTokenMiddleware.DaprApiTokenHeader] = AppToken;
                    http.Request.Headers[AccessTelemetryQualificationEndpointExtensions.RunHeader] = "run-001";
                    http.Request.Headers[AccessTelemetryQualificationEndpointExtensions.SegmentHeader] =
                        "writer-1-segment-0001";
                    http.Request.Headers[AccessTelemetryQualificationEndpointExtensions.EmittedUtcMsHeader] =
                        Now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
                    http.Features.Set<IHttpRequestLifetimeFeature>(new CancelledRequestLifetime(cancelled.Token));
                });
                context.Response.StatusCode.ShouldBe(StatusCodes.Status499ClientClosedRequest);
                time.Advance(TimeSpan.FromMinutes(6));
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DaprApplicationTokenMiddleware.AppApiTokenEnvironmentVariable,
                originalToken);
            File.Delete(gatePath);
        }
    }

    [Theory]
    [InlineData("Development", false, false, 10, true)]
    [InlineData("Production", false, false, 10, false)]
    [InlineData("Qualification", false, false, 10, false)]
    [InlineData("Development", true, false, 10, false)]
    [InlineData("Development", false, true, 10, false)]
    [InlineData("Development", false, false, 16, false)]
    [InlineData("Development", false, false, -1, false)]
    public async Task LocalGateRequiresOwnedRunAndShortLocalProfile(string environmentName, bool foreignRun, bool fullProfile, int minutes, bool expected)
    {
        string sourceHash = new('a', 64);
        string canonical = System.Text.Json.JsonSerializer.Serialize(new
        {
            purpose = "local-functional-retention", runId = "owned-run", sourceMappingHash = sourceHash, componentType = "state.postgresql/v2",
            runtimeImage = "ghcr.io/dapr/daprd:1.18.1@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8",
            postgresqlImage = "docker.io/library/postgres:18.6-trixie@sha256:5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722",
            sslMode = "verify-full", retentionSeconds = 3600, qualificationCredit = false,
        });
        string profile = fullProfile ? Pg2ProfileSha256 : Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)));
        string path = WriteGate(Now.AddMinutes(minutes), profile);
        try
        {
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryQualification:GatePath"] = path,
                ["AccessTelemetryQualification:LocalProfileHash"] = profile,
                ["AccessTelemetryLifecycle:ComponentProfileHash"] = profile,
                ["AccessTelemetryLifecycle:Enabled"] = "true",
                ["AccessTelemetryLifecycle:Retention"] = "01:00:00",
                ["AccessTelemetryLifecycle:LocalFunctionalProfileJson"] = canonical,
                ["AccessTelemetryLifecycle:LocalFunctionalSourceMappingHash"] = sourceHash,
                ["HEXALITH_SOURCE_MAPPING_HASH"] = sourceHash,
                ["AccessTelemetryLifecycle:CapabilityEvidence:ValidUntilUtc"] = Now.AddHours(2).ToString("O", CultureInfo.InvariantCulture),
                ["AccessTelemetryLifecycle:LocalFunctionalRunId"] = "owned-run",
                ["AccessTelemetryLifecycle:DeploymentId"] = "owned-run",
                ["HEXALITH_G4_RUN_ID"] = foreignRun ? "foreign-run" : "owned-run",
            }).Build();
            var time = new FakeTimeProvider(Now);
            var gate = new AccessTelemetryQualificationGate(environment, configuration, time);
            gate.TryValidate(out _).ShouldBe(expected);
            IAccessTelemetryHeartbeatClient client = Substitute.For<IAccessTelemetryHeartbeatClient>();
            client.SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>()).Returns(new WriterHeartbeatResponse
            {
                Accepted = true, Reason = AccessTelemetryReason.None, ActiveGeneration = "mk-a",
            });
            var worker = new AccessTelemetryHeartbeatWorker(
                client, configuration.GetSection("AccessTelemetryLifecycle").Get<AccessTelemetryOptions>()!,
                new AccessTelemetryWriterIdentity(new MonotonicRecordIdGenerator()), new BoundedAccessTelemetryQueue(8, 8192),
                new AccessTelemetryLifecycleStatus(enabled: true), time, gate);
            await worker.SendOnceAsync(CancellationToken.None);
            await client.Received(expected ? 1 : 0).SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>());
            if (expected)
            {
                WriteGate(path, Now.AddMinutes(10), profile, "disabled");
                await worker.SendOnceAsync(CancellationToken.None);
                await client.Received(1).SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>());
                WriteGate(path, Now.AddMinutes(10), profile);
                await worker.SendOnceAsync(CancellationToken.None);
                await client.Received(2).SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>());
                time.Advance(TimeSpan.FromMinutes(10));
                await worker.SendOnceAsync(CancellationToken.None);
                await client.Received(2).SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>());
                WriteGate(path, time.GetUtcNow().AddMinutes(10), profile);
                await worker.SendOnceAsync(CancellationToken.None);
                await client.Received(3).SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>());
                File.Delete(path);
                await worker.SendOnceAsync(CancellationToken.None);
                await client.Received(3).SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>());
                WriteGate(path, time.GetUtcNow().AddMinutes(10), profile);
                time.Advance(TimeSpan.FromHours(2));
                WriteGate(path, time.GetUtcNow().AddMinutes(10), profile);
                await worker.SendOnceAsync(CancellationToken.None);
                await client.Received(3).SendAsync(Arg.Any<WriterHeartbeat>(), Arg.Any<CancellationToken>());
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static AccessTelemetryQualificationGate CreateGate(
        string path,
        string environmentName,
        TimeProvider? timeProvider = null)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryQualification:GatePath"] = path,
            })
            .Build();
        return new AccessTelemetryQualificationGate(environment, configuration, timeProvider ?? new FakeTimeProvider(Now));
    }

    private static HttpRequestMessage CreateProgramRequest()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AccessTelemetryQualificationEndpointExtensions.Route);
        request.Headers.Add(AccessTelemetryQualificationEndpointExtensions.RunHeader, "run-program");
        request.Headers.Add(
            AccessTelemetryQualificationEndpointExtensions.SegmentHeader,
            "writer-1-segment-0001");
        request.Headers.Add(
            AccessTelemetryQualificationEndpointExtensions.EmittedUtcMsHeader,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
        return request;
    }

    private static void ConfigureProgramFactory(IWebHostBuilder builder, string gatePath)
    {
        builder.UseSetting("AccessTelemetryQualification:GatePath", gatePath);
        builder.UseSetting("Authentication:JwtBearer:Issuer", "hexalith-memories-test");
        builder.UseSetting("Authentication:JwtBearer:Audience", "hexalith-memories-server");
        builder.UseSetting("Authentication:JwtBearer:SigningKey", ServerTestBearerToken.SigningKey);
        builder.UseSetting("Authentication:JwtBearer:RequireHttpsMetadata", "false");
    }

    private static void PublishProjectedGate(
        string mount,
        string projectionName,
        DateTimeOffset expires,
        string profileSha256 = Pg2ProfileSha256,
        string state = "enabled")
    {
        string projection = Path.Combine(mount, projectionName);
        Directory.CreateDirectory(projection);
        WriteGate(Path.Combine(projection, "gate.json"), expires, profileSha256, state);
        string nextLink = Path.Combine(mount, "..data_tmp");
        _ = Directory.CreateSymbolicLink(nextLink, projectionName);
        string currentLink = Path.Combine(mount, "..data");
        // Managed directory moves cannot replace a directory symlink atomically.
        // Switch the projection between validations to exercise rereading ..data.
        if (Directory.Exists(currentLink))
        {
            Directory.Delete(currentLink);
        }

        Directory.Move(nextLink, currentLink);
    }

    private static string WriteGate(DateTimeOffset expires, string profileSha256 = Pg2ProfileSha256)
    {
        string path = Path.GetTempFileName();
        WriteGate(path, expires, profileSha256);
        return path;
    }

    private static void WriteGate(
        string path,
        DateTimeOffset expires,
        string profileSha256 = Pg2ProfileSha256,
        string state = "enabled")
    {
        File.WriteAllText(
            path,
            "{\"schemaVersion\":1,\"state\":\"" + state + "\",\"profileSha256\":\"" +
            profileSha256 +
            "\",\"expiresUtcMs\":" +
            expires.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
            "}");
    }

    private sealed class ThrowOnceLogger : ILogger<AccessTelemetryCategory>
    {
        private int _calls;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                throw new InvalidOperationException("first_fault");
            }
        }
    }

    private sealed class CancelledRequestLifetime(CancellationToken token) : IHttpRequestLifetimeFeature
    {
        public CancellationToken RequestAborted { get; set; } = token;

        public void Abort()
        {
        }
    }
}
