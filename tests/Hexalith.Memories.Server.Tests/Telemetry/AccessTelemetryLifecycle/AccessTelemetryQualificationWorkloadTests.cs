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

            File.WriteAllText(
                path,
                "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
                AccessTelemetryQualificationGate.ApprovedProfileSha256 +
                "\",\"expiresUtcMs\":" +
                Now.AddMinutes(16).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
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
    public void Gate_AcceptsAConfigMapProjectionSymlinkOnlyInsideItsMount()
    {
        string mount = Path.Combine(Path.GetTempPath(), $"qualification-gate-{Guid.NewGuid():N}");
        string data = Path.Combine(mount, "..2026_09_06");
        Directory.CreateDirectory(data);
        string projected = Path.Combine(data, "gate.json");
        File.WriteAllText(
            projected,
            "{\"schemaVersion\":1,\"state\":\"enabled\",\"profileSha256\":\"" +
            AccessTelemetryQualificationGate.ApprovedProfileSha256 +
            "\",\"expiresUtcMs\":" +
            Now.AddMinutes(5).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) +
            "}");
        string link = Path.Combine(mount, "gate.json");
        _ = File.CreateSymbolicLink(link, Path.Combine("..2026_09_06", "gate.json"));
        try
        {
            CreateGate(link, "Qualification").TryValidate(out string reason).ShouldBeTrue(reason);

            File.Delete(link);
            string outside = Path.GetTempFileName();
            try
            {
                File.WriteAllText(outside, File.ReadAllText(projected));
                _ = File.CreateSymbolicLink(link, outside);
                CreateGate(link, "Qualification").TryValidate(out reason).ShouldBeFalse();
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
                CreateGate(gatePath, "Qualification"),
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
            await worker.DrainOnceAsync(CancellationToken.None);
            time.Advance(TimeSpan.FromSeconds(1));

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
        }
        finally
        {
            File.Delete(gatePath);
        }
    }

    [Fact]
    public async Task Program_MapsTheQualificationRouteOnlyInTheQualificationEnvironment()
    {
        string gatePath = Path.GetTempFileName();
        File.WriteAllText(
            gatePath,
            "{\"schemaVersion\":1,\"state\":\"disabled\",\"profileSha256\":\"" +
            AccessTelemetryQualificationGate.ApprovedProfileSha256 +
            "\",\"expiresUtcMs\":0}");
        try
        {
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
