// <copyright file="AccessTelemetryQualificationWorkloadTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;
using System.Net;
using System.Security.Cryptography;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;
using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FixedWorkload_PinsTheApprovedPerWriterRateAndDuration()
    {
        AccessTelemetryQualificationWorkloadRunner.RecordsPerSecond.ShouldBe(125);
        AccessTelemetryQualificationWorkloadRunner.SteadyStateSeconds.ShouldBe(1_800);
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
                AccessTelemetryQualificationGate.ApprovedProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            AccessTelemetryQualificationGate gate = CreateGate(path, "Qualification");

            gate.TryValidate(out string reason).ShouldBeTrue();
            reason.ShouldBe("none");

            CreateGate(path, Environments.Production).TryValidate(out _).ShouldBeFalse();
            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                AccessTelemetryQualificationGate.ApprovedProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMilliseconds(-1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
                "}");
            gate.TryValidate(out reason).ShouldBeFalse();
            reason.ShouldBe("qualification_gate_invalid_or_expired");
        }
        finally
        {
            File.Delete(path);
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
            TimeSpan.FromHours(24)));
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
                CreateGate(gatePath, "Qualification"),
                time,
                recordsPerSecond: 1,
                steadyStateSeconds: 1);
            Task<AccessTelemetryQualificationWorkloadResult> pending = runner.RunAsync(CancellationToken.None);
            for (int attempt = 0; attempt < 20 && queue.Count == 0; attempt++)
            {
                await Task.Yield();
            }

            queue.Count.ShouldBe(1);
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
            await worker.DrainOnceAsync(CancellationToken.None);
            time.Advance(TimeSpan.FromSeconds(1));

            AccessTelemetryQualificationWorkloadResult result = await pending;
            result.Attempted.ShouldBe(1);
            result.Acknowledged.ShouldBe(1);
            result.Persisted.ShouldBe(1);
            result.Dropped.ShouldBe(0);
            result.Rejected.ShouldBe(0);
        }
        finally
        {
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
            TimeSpan.FromHours(24)));
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
                CreateGate(gatePath, "Qualification"),
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
                Task<HttpResponseMessage> acceptedTask = client.SendAsync(acceptedRequest);
                for (int attempt = 0; attempt < 100 && accounting.Current.Enqueued == 0 && !acceptedTask.IsCompleted; attempt++)
                {
                    await Task.Delay(10);
                }

                accounting.Current.Enqueued.ShouldBe(1, acceptedTask.IsCompleted
                    ? (await acceptedTask).StatusCode.ToString()
                    : "request still running");
                accounting.RecordPersisted(1);
                await Task.Yield();
                time.Advance(TimeSpan.FromSeconds(1));
                HttpResponseMessage accepted = await acceptedTask;
                accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
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

    private static AccessTelemetryQualificationGate CreateGate(string path, string environmentName)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryQualification:GatePath"] = path,
            })
            .Build();
        return new AccessTelemetryQualificationGate(environment, configuration, new FakeTimeProvider(Now));
    }

    private static string WriteGate(DateTimeOffset expires)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
            AccessTelemetryQualificationGate.ApprovedProfileSha256 +
            "\",\"expiresUtcMs\":" +
            expires.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
            "}");
        return path;
    }
}
