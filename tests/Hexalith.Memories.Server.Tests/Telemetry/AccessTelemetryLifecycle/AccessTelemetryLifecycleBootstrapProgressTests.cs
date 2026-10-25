// <copyright file="AccessTelemetryLifecycleBootstrapProgressTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

using Dapr;
using Dapr.Client;

using Grpc.Core;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Guards the bounded, secret-free bootstrap progress that separates emission before sanitizer publication from a
/// persistently failing Dapr configuration, secret or remote-validation call.
/// </summary>
public sealed class AccessTelemetryLifecycleBootstrapProgressTests
{
    private const string MarkerKeyReference = "access-telemetry-marker";
    private const string FixtureSecretText = "fixture-secret-endpoint-detail";
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 16, 27, 52, TimeSpan.Zero);

    [Fact]
    public void Classify_MapsEachFailureToTheFixedAllowListWithoutDetail()
    {
        static HttpResponseMessage Response(HttpStatusCode code) => new(code);
        (Exception Failure, AccessTelemetryLifecycleBootstrapFailureKind Kind)[] cases =
        [
            (new FormatException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.Format),
            (new JsonException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.Format),
            (new TimeoutException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.Timeout),
            (new TaskCanceledException(FixtureSecretText, new TimeoutException()), AccessTelemetryLifecycleBootstrapFailureKind.Timeout),
            (new OperationCanceledException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.Canceled),
            (new InvocationException("lifecycle", "v1/access-telemetry/validate", new HttpRequestException(FixtureSecretText), Response(HttpStatusCode.Unauthorized)), AccessTelemetryLifecycleBootstrapFailureKind.RemoteDenied),
            (new InvocationException("lifecycle", "v1/access-telemetry/validate", new HttpRequestException(FixtureSecretText), Response(HttpStatusCode.Forbidden)), AccessTelemetryLifecycleBootstrapFailureKind.RemoteDenied),
            (new InvocationException("lifecycle", "v1/access-telemetry/validate", new HttpRequestException(FixtureSecretText), Response(HttpStatusCode.InternalServerError)), AccessTelemetryLifecycleBootstrapFailureKind.DaprApi),
            (new InvocationException("lifecycle", "v1/access-telemetry/validate", new JsonException(FixtureSecretText), Response(HttpStatusCode.OK)), AccessTelemetryLifecycleBootstrapFailureKind.Format),
            (new InvocationException("lifecycle", "v1/access-telemetry/validate", new HttpRequestException(FixtureSecretText), null!), AccessTelemetryLifecycleBootstrapFailureKind.Transport),
            (new InvocationException("lifecycle", "v1/access-telemetry/validate", new RpcException(new Status(StatusCode.PermissionDenied, FixtureSecretText))), AccessTelemetryLifecycleBootstrapFailureKind.RemoteDenied),
            (new InvocationException("lifecycle", "v1/access-telemetry/validate", new RpcException(new Status(StatusCode.Unavailable, FixtureSecretText))), AccessTelemetryLifecycleBootstrapFailureKind.Transport),
            (new DaprException(FixtureSecretText, new RpcException(new Status(StatusCode.DeadlineExceeded, FixtureSecretText))), AccessTelemetryLifecycleBootstrapFailureKind.Timeout),
            (new DaprException(FixtureSecretText, new RpcException(new Status(StatusCode.Unavailable, FixtureSecretText))), AccessTelemetryLifecycleBootstrapFailureKind.Transport),
            (new DaprException(FixtureSecretText, new RpcException(new Status(StatusCode.Cancelled, FixtureSecretText))), AccessTelemetryLifecycleBootstrapFailureKind.Canceled),
            (new DaprException(FixtureSecretText, new RpcException(new Status(StatusCode.PermissionDenied, FixtureSecretText))), AccessTelemetryLifecycleBootstrapFailureKind.RemoteDenied),
            (new RpcException(new Status(StatusCode.Unauthenticated, FixtureSecretText)), AccessTelemetryLifecycleBootstrapFailureKind.RemoteDenied),
            (new DaprException(FixtureSecretText, new RpcException(new Status(StatusCode.NotFound, FixtureSecretText))), AccessTelemetryLifecycleBootstrapFailureKind.DaprApi),
            (new DaprException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.DaprApi),
            (new DaprApiException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.DaprApi),
            (new RpcException(new Status(StatusCode.Unavailable, FixtureSecretText)), AccessTelemetryLifecycleBootstrapFailureKind.Transport),
            (new HttpRequestException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.Transport),
            (new SocketException((int)SocketError.ConnectionRefused), AccessTelemetryLifecycleBootstrapFailureKind.Transport),
            (new IOException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.Transport),
            (new InvalidOperationException(FixtureSecretText), AccessTelemetryLifecycleBootstrapFailureKind.Other),
        ];

        foreach ((Exception failure, AccessTelemetryLifecycleBootstrapFailureKind kind) in cases)
        {
            AccessTelemetryLifecycleBootstrapProgress.Classify(failure).ShouldBe(kind, failure.GetType().Name);
            var progress = new AccessTelemetryLifecycleBootstrapProgress();
            progress.BeginAttempt();
            progress.CompleteFailure(AccessTelemetryLifecycleBootstrapProgress.Classify(failure));
            string json = JsonSerializer.Serialize(progress.Current, MemoriesJsonContext.Options);
            json.ShouldNotContain(FixtureSecretText);
            json.ShouldNotContain("validate");
            json.ShouldNotContain("lifecycle");
        }
    }

    [Fact]
    public void Progress_KeepsTheFirstPublicationAndOnlyBoundedNames()
    {
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        progress.Current.ShouldBe(AccessTelemetryLifecycleBootstrapSnapshot.NotStarted);
        progress.Current.Stage.ShouldBe("NotStarted");

        progress.BeginAttempt();
        progress.Current.Stage.ShouldBe("Configuration");
        progress.Enter(AccessTelemetryLifecycleBootstrapStage.Secret);
        progress.CompleteFailure((AccessTelemetryLifecycleBootstrapFailureKind)42);
        progress.Current.ShouldBe(new AccessTelemetryLifecycleBootstrapSnapshot(1, "Secret", "Secret", "Other", null));

        progress.BeginAttempt();
        progress.Enter((AccessTelemetryLifecycleBootstrapStage)42);
        progress.Current.Stage.ShouldBe(AccessTelemetryLifecycleBootstrapSnapshot.Unobserved);
        progress.CompletePublished(Now);
        progress.BeginAttempt();
        progress.CompletePublished(Now.AddSeconds(10));
        progress.Current.ShouldBe(new AccessTelemetryLifecycleBootstrapSnapshot(3, "Published", "Secret", "Other", Now.ToUnixTimeMilliseconds()));

        string json = JsonSerializer.Serialize(progress.Current, MemoriesJsonContext.Options);
        using JsonDocument document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
            .ShouldBe(["attempts", "failureKind", "failureStage", "firstPublishedUtcMs", "stage"]);
    }

    [Fact]
    public async Task HostedBootstrap_RecordsTheFailedStageThenTheFirstPublicationWithoutChangingStatusSemantics()
    {
        var time = new TimerCountingTimeProvider(Now);
        var accessor = new AccessTelemetrySanitizerAccessor();
        var status = new AccessTelemetryLifecycleStatus(enabled: true);

        // A distinct starting status proves that the unchanged catch-all, not the initial value, published the result.
        status.Publish(AccessTelemetryHealthState.NoData, AccessTelemetryReason.None);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        DaprClient dapr = CreateDapr(new AccessTelemetryRuntimeValidationResponse(true, AccessTelemetryReason.None));
        dapr.GetSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<Dictionary<string, string>>(
                    new DaprException(FixtureSecretText, new RpcException(new Status(StatusCode.Unavailable, FixtureSecretText)))),
                _ => Task.FromResult(MarkerSecret()));
        AccessTelemetryLifecycleBootstrapService bootstrap = CreateBootstrap(dapr, accessor, status, time, progress);

        await bootstrap.StartAsync(CancellationToken.None);
        try
        {
            await WaitForAsync(() => progress.Current.Attempts >= 1);
            progress.Current.ShouldBe(new AccessTelemetryLifecycleBootstrapSnapshot(1, "Secret", "Secret", "Transport", null));

            // The catch-all records progress before it publishes status, so wait for the published status.
            await WaitForAsync(() => status.Current.Health == AccessTelemetryHealthState.Degraded &&
                status.Current.Reason == AccessTelemetryReason.RemoteValidationPending);
            accessor.Current.ShouldBeNull();
            JsonSerializer.Serialize(progress.Current, MemoriesJsonContext.Options).ShouldNotContain(FixtureSecretText);

            // The unchanged five-second catch-all retry then publishes; the earlier failure stays visible. Advance only
            // after the retry delay is registered so the fake clock cannot skip it.
            await WaitForAsync(() => time.Timers >= 1);
            time.Advance(TimeSpan.FromSeconds(5));
            await WaitForAsync(() => progress.Current.Attempts >= 2);
            progress.Current.ShouldBe(new AccessTelemetryLifecycleBootstrapSnapshot(
                2, "Published", "Secret", "Transport", Now.AddSeconds(5).ToUnixTimeMilliseconds()));
            await WaitForAsync(() => status.Current.Health == AccessTelemetryHealthState.Healthy);
            status.Current.Reason.ShouldBe(AccessTelemetryReason.None);
            accessor.Current.ShouldNotBeNull();
        }
        finally
        {
            await bootstrap.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task HostedBootstrap_ReportsEachRefusalAtItsStageWithItsBoundedKind()
    {
        // Missing secret: unchanged Unhealthy/ConfigurationInvalid status.
        await AssertFirstAttemptAsync(
            configure: dapr => dapr.GetSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new Dictionary<string, string>())),
            options: null,
            expected: new AccessTelemetryLifecycleBootstrapSnapshot(1, "Secret", "Secret", "SecretMissing", null),
            AccessTelemetryHealthState.Unhealthy,
            AccessTelemetryReason.ConfigurationInvalid);

        // Malformed marker key: unchanged Unhealthy/ConfigurationInvalid status.
        await AssertFirstAttemptAsync(
            configure: dapr => dapr.GetSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new Dictionary<string, string> { [MarkerKeyReference] = "not base64 " + FixtureSecretText })),
            options: null,
            expected: new AccessTelemetryLifecycleBootstrapSnapshot(1, "Secret", "Secret", "Format", null),
            AccessTelemetryHealthState.Unhealthy,
            AccessTelemetryReason.ConfigurationInvalid);

        // Invalid configuration: a local-profile intent outside Development never reaches the secret stage.
        await AssertFirstAttemptAsync(
            configure: _ => { },
            options: CreateOptions() with { LocalFunctionalRunId = "fixture-run" },
            expected: new AccessTelemetryLifecycleBootstrapSnapshot(1, "Configuration", "Configuration", "ConfigurationInvalid", null),
            AccessTelemetryHealthState.Unhealthy,
            AccessTelemetryReason.ConfigurationInvalid);

        // Sidecar or application token denial (HTTP 401) on validation: unchanged catch-all Degraded/RemoteValidationPending.
        await AssertFirstAttemptAsync(
            configure: dapr =>
            {
#pragma warning disable CS0618 // DaprClient 1.18 typed service invocation is obsolete without a native typed helper.
                dapr.InvokeMethodAsync<AccessTelemetryRuntimeValidationResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                    .ThrowsAsync(new InvocationException(
                        "lifecycle",
                        "v1/access-telemetry/validate",
                        new HttpRequestException(FixtureSecretText),
                        new HttpResponseMessage(HttpStatusCode.Unauthorized)));
#pragma warning restore CS0618
            },
            options: null,
            expected: new AccessTelemetryLifecycleBootstrapSnapshot(1, "RemoteValidation", "RemoteValidation", "RemoteDenied", null),
            AccessTelemetryHealthState.Degraded,
            AccessTelemetryReason.RemoteValidationPending);

        // A failing Dapr configuration call: the catch-all records the classified kind at the configuration stage.
        await AssertFirstAttemptAsync(
            configure: dapr => dapr.GetConfiguration(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new DaprException(FixtureSecretText, new RpcException(new Status(StatusCode.Unavailable, FixtureSecretText)))),
            options: CreateOptions() with { RetentionSource = RetentionConfigurationSource.DaprConfiguration },
            expected: new AccessTelemetryLifecycleBootstrapSnapshot(1, "Configuration", "Configuration", "Transport", null),
            AccessTelemetryHealthState.Degraded,
            AccessTelemetryReason.RemoteValidationPending);

        // Explicit lifecycle refusal: unchanged Degraded status carrying the remote reason.
        await AssertFirstAttemptAsync(
            configure: dapr =>
            {
#pragma warning disable CS0618 // DaprClient 1.18 typed service invocation is obsolete without a native typed helper.
                dapr.InvokeMethodAsync<AccessTelemetryRuntimeValidationResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                    .Returns(new AccessTelemetryRuntimeValidationResponse(false, AccessTelemetryReason.CapabilityUnproven));
#pragma warning restore CS0618
            },
            options: null,
            expected: new AccessTelemetryLifecycleBootstrapSnapshot(1, "RemoteValidation", "RemoteValidation", "RemoteDenied", null),
            AccessTelemetryHealthState.Degraded,
            AccessTelemetryReason.CapabilityUnproven);
    }

    [Fact]
    public async Task HostedBootstrap_InPlaceMarkerKeyChangeIsTerminalAndRecordedAtTheStageInFlight()
    {
        var time = new TimerCountingTimeProvider(Now);
        var accessor = new AccessTelemetrySanitizerAccessor();
        var status = new AccessTelemetryLifecycleStatus(enabled: true);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        DaprClient dapr = CreateDapr(new AccessTelemetryRuntimeValidationResponse(true, AccessTelemetryReason.None));

        // The refresh returns a different marker key under the same generation.
        dapr.GetSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MarkerSecret()), _ => Task.FromResult(MarkerSecret()));
        AccessTelemetryLifecycleBootstrapService bootstrap = CreateBootstrap(dapr, accessor, status, time, progress);

        await bootstrap.StartAsync(CancellationToken.None);
        try
        {
            await WaitForAsync(() => progress.Current.Attempts >= 1);
            progress.Current.ShouldBe(new AccessTelemetryLifecycleBootstrapSnapshot(1, "Published", null, null, Now.ToUnixTimeMilliseconds()));

            // Advance only after the refresh delay is registered so the fake clock cannot skip it.
            await WaitForAsync(() => time.Timers >= 1);
            time.Advance(TimeSpan.FromSeconds(10));
            await WaitForAsync(() => progress.Current.Attempts >= 2);
            progress.Current.ShouldBe(new AccessTelemetryLifecycleBootstrapSnapshot(
                2, "RemoteValidation", "RemoteValidation", "ConfigurationInvalid", Now.ToUnixTimeMilliseconds()));

            // The sanitizer is cleared before the terminal status is published.
            await WaitForAsync(() => status.Current.Health == AccessTelemetryHealthState.Unhealthy &&
                status.Current.Reason == AccessTelemetryReason.ConfigurationInvalid);
            accessor.Current.ShouldBeNull();
            status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
            status.Current.Health.ShouldBe(AccessTelemetryHealthState.Unhealthy, "the in-place key change is terminal");
        }
        finally
        {
            await bootstrap.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>Creates a Dapr client double that returns a valid marker secret and the given validation response.</summary>
    /// <param name="validation">The lifecycle validation response.</param>
    /// <returns>The configured double.</returns>
    internal static DaprClient CreateDapr(AccessTelemetryRuntimeValidationResponse validation)
    {
        DaprClient dapr = Substitute.For<DaprClient>();
        dapr.GetSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MarkerSecret()));
        dapr.CreateInvokeMethodRequest(
                Arg.Any<HttpMethod>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<KeyValuePair<string, string>>>(),
                Arg.Any<AccessTelemetryRuntimeValidationRequest>())
            .Returns(_ => new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1/v1/access-telemetry/validate"));
#pragma warning disable CS0618 // DaprClient 1.18 typed service invocation is obsolete without a native typed helper.
        dapr.InvokeMethodAsync<AccessTelemetryRuntimeValidationResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(validation);
#pragma warning restore CS0618
        return dapr;
    }

    /// <summary>Creates the production bootstrap service over doubles for its external calls.</summary>
    /// <param name="dapr">The Dapr client double.</param>
    /// <param name="accessor">The sanitizer accessor.</param>
    /// <param name="status">The lifecycle status.</param>
    /// <param name="time">The fake clock.</param>
    /// <param name="progress">The bootstrap progress under test.</param>
    /// <param name="options">Optional replacement options.</param>
    /// <returns>The unstarted bootstrap service.</returns>
    internal static AccessTelemetryLifecycleBootstrapService CreateBootstrap(
        DaprClient dapr,
        AccessTelemetrySanitizerAccessor accessor,
        AccessTelemetryLifecycleStatus status,
        TimeProvider time,
        AccessTelemetryLifecycleBootstrapProgress progress,
        AccessTelemetryOptions? options = null)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Qualification");
        return new AccessTelemetryLifecycleBootstrapService(
            dapr,
            options ?? CreateOptions(),
            accessor,
            status,
            time,
            new MonotonicRecordIdGenerator(),
            environment,
            progress);
    }

    /// <summary>Waits a bounded real time (about 30 seconds) for a background condition.</summary>
    /// <param name="condition">The condition to observe.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    internal static async Task WaitForAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 3000 && !condition(); attempt++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        condition().ShouldBeTrue();
    }

    private static async Task AssertFirstAttemptAsync(
        Action<DaprClient> configure,
        AccessTelemetryOptions? options,
        AccessTelemetryLifecycleBootstrapSnapshot expected,
        AccessTelemetryHealthState health,
        AccessTelemetryReason reason)
    {
        var time = new FakeTimeProvider(Now);
        var accessor = new AccessTelemetrySanitizerAccessor();
        var status = new AccessTelemetryLifecycleStatus(enabled: true);

        // A distinct starting status proves that the unchanged refusal path, not the initial value, published the result.
        status.Publish(AccessTelemetryHealthState.NoData, AccessTelemetryReason.None);
        var progress = new AccessTelemetryLifecycleBootstrapProgress();
        DaprClient dapr = CreateDapr(new AccessTelemetryRuntimeValidationResponse(true, AccessTelemetryReason.None));
        configure(dapr);
        AccessTelemetryLifecycleBootstrapService bootstrap = CreateBootstrap(dapr, accessor, status, time, progress, options);
        await bootstrap.StartAsync(CancellationToken.None);
        try
        {
            await WaitForAsync(() => progress.Current.Attempts >= 1);
            progress.Current.ShouldBe(expected);
            await WaitForAsync(() => status.Current.Health == health && status.Current.Reason == reason);
            accessor.Current.ShouldBeNull();
            JsonSerializer.Serialize(progress.Current, MemoriesJsonContext.Options).ShouldNotContain(FixtureSecretText);
        }
        finally
        {
            await bootstrap.StopAsync(CancellationToken.None);
        }
    }

    private static Dictionary<string, string> MarkerSecret()
        => new() { [MarkerKeyReference] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) };

    private static AccessTelemetryOptions CreateOptions()
        => new()
        {
            Enabled = true,
            Retention = TimeSpan.FromHours(24),
            RetentionSource = RetentionConfigurationSource.DevelopmentDefault,
            DeploymentId = "development",
            ComponentProfileHash = new string('a', 64),
            ConfigurationEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            MarkerKeyReference = MarkerKeyReference,
            MarkerKeyGeneration = "mk-2026a",
            AttestationVerificationKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CapacityEvidenceId = "development-capacity",
            PhysicalReclamationEvidenceId = "pending-story-27-3",
            PhysicalReclamationReporterImageDigest = new string('d', 64),
        };

    /// <summary>Counts registered timers so a test advances fake time only after a delay is pending.</summary>
    private sealed class TimerCountingTimeProvider(DateTimeOffset start) : FakeTimeProvider(start)
    {
        private int _timers;

        public int Timers => Volatile.Read(ref _timers);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ITimer timer = base.CreateTimer(callback, state, dueTime, period);
            _ = Interlocked.Increment(ref _timers);
            return timer;
        }
    }
}
