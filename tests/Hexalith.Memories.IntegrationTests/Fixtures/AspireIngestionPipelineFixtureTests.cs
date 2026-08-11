// <copyright file="AspireIngestionPipelineFixtureTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;

using Hexalith.Memories.AppHost;
using Hexalith.Memories.IntegrationTests.Mcp;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

using Shouldly;

/// <summary>Unit-level guards for <see cref="AspireIngestionPipelineFixture"/> helper methods.</summary>
[Trait("Category", "Integration")]
public sealed class AspireIngestionPipelineFixtureTests
{
    [Fact]
    public void MintDevBearer_ExplicitPastExpiry_CreatesExpiredToken()
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(-1);

        string token = AspireIngestionPipelineFixture.MintDevBearer(
            "tenant-expired-probe",
            expiresAt: expiresAt);

        var jwt = new JsonWebToken(token);
        jwt.ValidTo.ShouldBeLessThan(DateTime.UtcNow);
        jwt.ValidFrom.ShouldBeLessThan(jwt.ValidTo);
    }

    [Fact]
    public void RepositoryRootLocator_NestedCurrentDirectory_ReturnsMarkerDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"memories-root-{Guid.NewGuid():N}");
        string nested = Path.Combine(root, "src", "Hexalith.Memories.AppHost", "bin");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(root, "Hexalith.Memories.slnx"), string.Empty);

        try
        {
            string resolved = RepositoryRootLocator.Resolve(currentDirectory: nested, baseDirectory: nested);
            resolved.ShouldBe(Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RepositoryRootLocator_MissingMarker_Throws()
    {
        string root = Path.Combine(Path.GetTempPath(), $"memories-root-missing-{Guid.NewGuid():N}");
        string nested = Path.Combine(root, "bin");
        Directory.CreateDirectory(nested);

        try
        {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
                RepositoryRootLocator.Resolve(currentDirectory: nested, baseDirectory: nested));

            exception.Message.ShouldContain("Hexalith.Memories.slnx");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveDaprSidecarHttpEndpoint_DistinctExactResources_ReturnDistinctDirectPorts()
    {
        AspireIngestionPipelineFixture.CapturedLogEntry[] entries =
        [
            new(
                LogLevel.Information,
                "memories-dapr-cli.stdout",
                "HTTP server listening on TCP address: :41001"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources.memories-access-telemetry-clock-dapr-cli",
                "HTTP server listening on TCP address: :41002"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources.memories-access-telemetry-dapr-cli.stderr",
                "HTTP server listening on TCP address: :41003"),
        ];

        AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(
            "memories-dapr-cli",
            entries).ShouldBe(new Uri("http://127.0.0.1:41001"));
        AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(
            "memories-access-telemetry-clock-dapr-cli",
            entries).ShouldBe(new Uri("http://127.0.0.1:41002"));
        AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(
            "memories-access-telemetry-dapr-cli",
            entries).ShouldBe(new Uri("http://127.0.0.1:41003"));
    }

    [Fact]
    public void ResolveDaprSidecarHttpEndpoint_NewerMalformedAndSimilarEntries_ReturnLatestExactValidPort()
    {
        const string resourceName = "memories-access-telemetry-clock-dapr-cli";
        AspireIngestionPipelineFixture.CapturedLogEntry[] entries =
        [
            new(
                LogLevel.Information,
                resourceName,
                "HTTP server listening on TCP address: :42001"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :42002"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources." + resourceName + "-extra",
                "HTTP server listening on TCP address: :42991"),
            new(
                LogLevel.Information,
                resourceName + "-extra.stdout",
                "HTTP server listening on TCP address: :42992"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :42003x"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :42004-extra"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :0"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :65536"),
        ];

        Uri endpoint = AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(resourceName, entries);

        endpoint.ShouldBe(new Uri("http://127.0.0.1:42002"));
    }

    [Fact]
    public void ResolveDaprSidecarHttpEndpoint_MissingExactValidEvidence_FailsClosed()
    {
        const string resourceName = "memories-access-telemetry-clock-dapr-cli";
        AspireIngestionPipelineFixture.CapturedLogEntry[] entries =
        [
            new(
                LogLevel.Information,
                resourceName + "-extra",
                "HTTP server listening on TCP address: :43001"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources." + resourceName + ".stdout",
                "HTTP server listening on TCP address: :invalid"),
        ];

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(resourceName, entries));

        exception.Message.ShouldContain(resourceName);
        exception.Message.ShouldContain("direct daprd HTTP endpoint");
        exception.Message.ShouldNotContain("proxy", Case.Insensitive);
    }

    [Fact]
    public async Task WaitForDaprSidecarHttpEndpointAsync_DelayedExactEvidence_Converges()
    {
        const string resourceName = "memories-access-telemetry-clock-dapr-cli";
        int snapshotCount = 0;

        Uri endpoint = await AspireIngestionPipelineFixture.WaitForDaprSidecarHttpEndpointAsync(
            resourceName,
            () => Interlocked.Increment(ref snapshotCount) == 1
                ? []
                :
                [
                    new(
                        LogLevel.Information,
                        "Aspire.Hosting.Resources." + resourceName + ".stdout",
                        "HTTP server listening on TCP address: :44001"),
                ],
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            CancellationToken.None);

        endpoint.ShouldBe(new Uri("http://127.0.0.1:44001"));
        snapshotCount.ShouldBe(2);
    }

    [Fact]
    public async Task WaitForDaprSidecarHttpEndpointAsync_MissingEvidenceUntilDeadline_FailsClosed()
    {
        const string resourceName = "memories-access-telemetry-dapr-cli";

        TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForDaprSidecarHttpEndpointAsync(
                resourceName,
                static () => [],
                TimeSpan.FromMilliseconds(25),
                TimeSpan.FromMilliseconds(5),
                CancellationToken.None));

        exception.Message.ShouldContain(resourceName);
        exception.Message.ShouldContain("direct daprd HTTP endpoint");
        exception.Message.ShouldNotContain("proxy", Case.Insensitive);
    }

    [Fact]
    public async Task WaitForOpenBaoSidecarMatrixReadinessCoreAsync_UsesOnlyExactAuxiliarySidecarProbes()
    {
        List<string> probes = [];

        await AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessCoreAsync(
            (sidecarResourceName, _) =>
            {
                probes.Add($"sidecar:{sidecarResourceName}");
                return Task.CompletedTask;
            },
            (sidecarResourceName, secretName, _) =>
            {
                probes.Add($"secret:{sidecarResourceName}:{secretName}");
                return Task.CompletedTask;
            },
            CancellationToken.None);

        probes.ShouldBe(
        [
            "sidecar:memories-access-telemetry-clock-dapr-cli",
            "secret:memories-access-telemetry-clock-dapr-cli:access-telemetry-clock-key",
            "sidecar:memories-access-telemetry-dapr-cli",
            "secret:memories-access-telemetry-dapr-cli:access-telemetry-marker-key",
        ]);
    }

    [Fact]
    public async Task WaitForOpenBaoSidecarMatrixReadinessCoreAsync_ForwardsCallerCancellationTokenToEveryProbe()
    {
        using var cts = new CancellationTokenSource();
        List<CancellationToken> observedTokens = [];

        await AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessCoreAsync(
            (_, token) =>
            {
                observedTokens.Add(token);
                return Task.CompletedTask;
            },
            (_, _, token) =>
            {
                observedTokens.Add(token);
                return Task.CompletedTask;
            },
            cts.Token);

        observedTokens.Count.ShouldBe(4);
        observedTokens.ShouldAllBe(token => token == cts.Token);
    }

    [Fact]
    public async Task WaitForOpenBaoSidecarMatrixReadinessCoreAsync_ClockSidecarFailure_StopsRemainingProbes()
    {
        List<string> probes = [];

        IOException exception = await Should.ThrowAsync<IOException>(() =>
            AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessCoreAsync(
                (sidecarResourceName, _) =>
                {
                    probes.Add($"sidecar:{sidecarResourceName}");
                    return Task.FromException(new IOException("clock sidecar unavailable"));
                },
                (sidecarResourceName, secretName, _) =>
                {
                    probes.Add($"secret:{sidecarResourceName}:{secretName}");
                    return Task.CompletedTask;
                },
                CancellationToken.None));

        exception.Message.ShouldBe("clock sidecar unavailable");
        probes.ShouldBe(["sidecar:memories-access-telemetry-clock-dapr-cli"]);
    }

    [Fact]
    public void OpenBaoSidecarMatrixReadiness_SharedTopologyStartupDoesNotCallIt()
    {
        MethodInfo sharedTopologyStartup = typeof(AspireIngestionPipelineFixture)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name == "StartTopologyAsync");
        MethodInfo auxiliaryReadiness = typeof(AspireIngestionPipelineFixture)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name == nameof(AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessAsync));

        AsyncMethodCalls(sharedTopologyStartup, auxiliaryReadiness).ShouldBeFalse(
            "clock/lifecycle readiness belongs only to the OpenBao sidecar matrix, not shared fixture startup.");
    }

    [Fact]
    public async Task RethrowAfterCleanupAsync_AllActionsSucceed_PreservesOriginalWithoutDiagnostics()
    {
        var startupException = new InvalidOperationException("startup failed");
        List<string> cleanupOrder = [];

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterCleanupAsync(
                startupException,
                [
                    new("first", () => RecordSuccessfulCleanup("first", cleanupOrder)),
                    new("second", () => RecordSuccessfulCleanup("second", cleanupOrder)),
                ]));

        thrown.ShouldBeSameAs(startupException);
        cleanupOrder.ShouldBe(["first", "second"]);
        thrown.Data.Contains(AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey)
            .ShouldBeFalse();
    }

    [Fact]
    public async Task RethrowAfterCleanupAsync_NullAndMultipleFailures_PreservesOrderAndExistingDiagnostics()
    {
        var startupException = new InvalidOperationException("startup failed");
        startupException.Data[AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey] =
            "existing diagnostic";
        List<string> cleanupOrder = [];

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterCleanupAsync(
                startupException,
                [
                    new("first", () => RecordSuccessfulCleanup("first", cleanupOrder)),
                    new("missing", null),
                    new("third", () => RecordFailedCleanup("third", cleanupOrder, new IOException("third failed"))),
                    new("fourth", () => RecordSuccessfulCleanup("fourth", cleanupOrder)),
                    new("fifth", () => RecordFailedCleanup("fifth", cleanupOrder, new TimeoutException("fifth failed"))),
                    new("sixth", () => RecordSuccessfulCleanup("sixth", cleanupOrder)),
                ]));

        thrown.ShouldBeSameAs(startupException);
        cleanupOrder.ShouldBe(["first", "third", "fourth", "fifth", "sixth"]);
        thrown.Data[AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey]
            .ShouldBe("existing diagnostic");
        AggregateException cleanupFailures = thrown.Data[
            AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey + ".2"]
            .ShouldBeOfType<AggregateException>();
        cleanupFailures.InnerExceptions.Count.ShouldBe(3);
        cleanupFailures.InnerExceptions[0].ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldContain("'missing'");
        cleanupFailures.InnerExceptions[1].ShouldBeOfType<IOException>()
            .Message.ShouldBe("third failed");
        cleanupFailures.InnerExceptions[2].ShouldBeOfType<TimeoutException>()
            .Message.ShouldBe("fifth failed");
    }

    [Fact]
    public async Task RethrowAfterCleanupCoreAsync_DiagnosticsSinkThrows_PreservesOriginalException()
    {
        var startupException = new InvalidOperationException("startup failed");

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterCleanupCoreAsync(
                startupException,
                [
                    new("failed", () => Task.FromException(new IOException("cleanup failed"))),
                ],
                static (_, _) => throw new NotSupportedException("diagnostics unavailable")));

        thrown.ShouldBeSameAs(startupException);
        thrown.Message.ShouldBe("startup failed");
    }

    [Fact]
    public void CreateFailedInitializationCleanupActions_ContainsAllSixOwnedResourceSlotsInOrder()
    {
        var fixture = new AspireIngestionPipelineFixture();

        IReadOnlyList<KeyValuePair<string, Func<Task>?>> cleanupActions =
            fixture.CreateFailedInitializationCleanupActions();

        cleanupActions.Select(static slot => slot.Key).ShouldBe(
        [
            "topology",
            "environment-scopes",
            "temporary-dapr-config",
            "fixture-containers",
            "falkor-volume",
            "redis-volume",
        ]);
        cleanupActions.ShouldAllBe(static slot => slot.Value != null);
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_TransientStatusThenOk_Converges()
    {
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK]);
        int probeCount = 0;

        await AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
            _ =>
            {
                probeCount++;
                return Task.FromResult(statuses.Dequeue());
            },
            "clock-secret-readiness",
            [HttpStatusCode.OK],
            [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.Zero,
            CancellationToken.None);

        probeCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task WaitForEndpointProbeAsync_AuthorizationFailureThenOk_FailsClosedWithoutRetry(
        HttpStatusCode authorizationStatus)
    {
        int probeCount = 0;

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(probeCount++ == 0 ? authorizationStatus : HttpStatusCode.OK),
                "clock-secret-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                CancellationToken.None));

        probeCount.ShouldBe(1);
        exception.Message.ShouldContain($"{(int)authorizationStatus} {authorizationStatus}");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_CancelledBeforeProbe_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        int probeCount = 0;

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(probeCount++ == 0 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK),
                "clock-secret-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                cts.Token));

        probeCount.ShouldBe(0);
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_TransientStatusUntilDeadline_FailsWithSafeStatus()
    {
        TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(HttpStatusCode.ServiceUnavailable),
                "clock-secret-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
                TimeSpan.FromMilliseconds(25),
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(1),
                CancellationToken.None));

        exception.Message.ShouldContain("ServiceUnavailable");
        exception.Message.ShouldNotContain("response body");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_HangingProbe_IsBoundedByOverallDeadline()
    {
        var stopwatch = Stopwatch.StartNew();

        _ = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => new TaskCompletionSource<HttpStatusCode>(
                    TaskCreationOptions.RunContinuationsAsynchronously).Task,
                "bounded-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized],
                TimeSpan.FromMilliseconds(75),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5),
                CancellationToken.None));

        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_PerProbeTimeoutThenOk_Recovers()
    {
        int probeCount = 0;

        await AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
            _ =>
            {
                if (Interlocked.Increment(ref probeCount) == 1)
                {
                    return new TaskCompletionSource<HttpStatusCode>(
                        TaskCreationOptions.RunContinuationsAsynchronously).Task;
                }

                return Task.FromResult(HttpStatusCode.OK);
            },
            "per-probe-timeout",
            [HttpStatusCode.OK],
            [HttpStatusCode.Unauthorized],
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(25),
            TimeSpan.Zero,
            CancellationToken.None);

        probeCount.ShouldBe(2);
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_UnexpectedException_PropagatesWithoutRetry()
    {
        int probeCount = 0;

        FormatException exception = await Should.ThrowAsync<FormatException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromException<HttpStatusCode>(
                    ++probeCount == 1
                        ? new FormatException("programming fault")
                        : new FormatException("unexpected retry")),
                "unexpected-exception",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                CancellationToken.None));

        probeCount.ShouldBe(1);
        exception.Message.ShouldBe("programming fault");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_OverlappingStatusSets_RejectsConfiguration()
    {
        int probeCount = 0;

        ArgumentException exception = await Should.ThrowAsync<ArgumentException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(++probeCount == 1 ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable),
                "overlapping-statuses",
                [HttpStatusCode.OK],
                [HttpStatusCode.OK, HttpStatusCode.Unauthorized],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                CancellationToken.None));

        probeCount.ShouldBe(0);
        exception.ParamName.ShouldBe("failClosedStatusCodes");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_CallerCancellationDuringProbe_Propagates()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));
        int probeCount = 0;

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ =>
                {
                    Interlocked.Increment(ref probeCount);
                    return new TaskCompletionSource<HttpStatusCode>(
                        TaskCreationOptions.RunContinuationsAsynchronously).Task;
                },
                "caller-cancellation",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                cts.Token));

        probeCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.MethodNotAllowed, true)]
    [InlineData(HttpStatusCode.NotAcceptable, true)]
    [InlineData(HttpStatusCode.UnsupportedMediaType, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, false)]
    public void DaprSecretReadinessStatus_PermanentAndTransientBoundaries_AreExplicit(
        HttpStatusCode statusCode,
        bool expectedPermanent)
    {
        AspireIngestionPipelineFixture.IsPermanentDaprSecretProbeStatus(statusCode)
            .ShouldBe(expectedPermanent);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Created, false)]
    public void McpDaprInvokeStatus_OnlyKnownAvailabilityFailuresAreTransient(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        AspireIngestionPipelineFixture.IsTransientMcpDaprInvokeStatus(statusCode)
            .ShouldBe(expectedTransient);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public void McpReadinessStatus_OnlyKnownAvailabilityFailuresAreTransient(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        AspireIngestionPipelineFixture.IsTransientMcpReadinessStatus(statusCode)
            .ShouldBe(expectedTransient);
    }

    [Fact]
    public void RedactSensitiveDiagnostics_TokenLikeSentinels_AreRemoved()
    {
        const string sentinel = "diagnostic-token-sentinel-43891";
        string diagnostic =
            $"Authorization: Bearer {sentinel}; token=\"{sentinel}\"; " +
            $"access_token={sentinel}; vault=hvs.{sentinel}; " +
            $"jwt=eyJhbGciOiJIUzI1NiJ9.{sentinel}.signature";

        string redacted = AspireIngestionPipelineFixture.RedactSensitiveDiagnostics(diagnostic);

        redacted.ShouldNotContain(sentinel);
        redacted.ShouldContain("[REDACTED]");
    }

    [Theory]
    [InlineData("memories")]
    [InlineData("Aspire.Hosting.Resources.memories-mcp")]
    [InlineData("memories-mcp-dapr-cli.stdout")]
    [InlineData("Aspire.Hosting.Resources.memories-mcp.stderr")]
    public void McpDiagnosticResourceCategory_SupportedShapes_AreRecognized(string category)
    {
        McpServerIntegrationTests.IsDiagnosticResourceCategory(category).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Aspire.Hosting.Resources.unrelated")]
    [InlineData("memories-mcp-extra")]
    public void McpDiagnosticResourceCategory_UnrelatedShapes_AreRejected(string category)
    {
        McpServerIntegrationTests.IsDiagnosticResourceCategory(category).ShouldBeFalse();
    }

    [Fact]
    public void OpenBaoTopologyTraits_FastSlowAndPerformanceLanesRemainSeparated()
    {
        GetCategoryTraits(typeof(OpenBaoTopologyIntegrationTests)).ShouldContain("Integration");

        string[] fastMethods =
        [
            nameof(OpenBaoTopologyIntegrationTests.DaprSecretReads_RuntimeAndAccessTelemetryCanariesMatchExpectedFingerprints),
            nameof(OpenBaoTopologyIntegrationTests.ProviderBoundary_DaprScopesKeyScopesPoliciesPrefixesAndBulkOperationsFailClosed),
            nameof(OpenBaoTopologyIntegrationTests.DaprSidecarMatrix_EnforcesExactComponentAndKeyAllowDenyBoundaries),
            nameof(OpenBaoTopologyIntegrationTests.InPlaceOpenBaoRestart_RotatesGenerationAndRecoversDependentSidecars),
            nameof(OpenBaoTopologyIntegrationTests.DiagnosticsModelGeneratedFilesAndLogs_ContainNoSensitiveMaterial),
        ];
        foreach (string methodName in fastMethods)
        {
            MethodInfo method = GetMethod(methodName);
            FactAttribute fact = method.GetCustomAttribute<FactAttribute>()
                .ShouldNotBeNull($"Fast OpenBao guard '{methodName}' must remain an xUnit Fact.");
            fact.Skip.ShouldBeNullOrWhiteSpace($"Fast OpenBao guard '{methodName}' must remain runnable.");

            IReadOnlyList<string> categories = GetCategoryTraits(method);
            categories.ShouldNotContain("IntegrationSlow");
            categories.ShouldNotContain("Performance");
        }

        IReadOnlyList<string> restartCategories = GetCategoryTraits(GetMethod(
            nameof(OpenBaoTopologyIntegrationTests.FullTopologyRestart_ReinitializesOpenBaoAndRecoversPermittedDaprReads)));
        restartCategories.ShouldContain("IntegrationSlow");
        restartCategories.ShouldNotContain("Performance");

        IReadOnlyList<string> nfrCategories = GetCategoryTraits(GetMethod(
            nameof(OpenBaoTopologyIntegrationTests.OpenBaoColdStart_TopologyAcceptsQueriesWithinNfr7)));
        nfrCategories.ShouldContain("IntegrationSlow");
        nfrCategories.ShouldContain("Performance");
    }

    private static bool AsyncMethodCalls(MethodInfo asyncMethod, MethodInfo targetMethod)
    {
        Type stateMachineType = asyncMethod.GetCustomAttribute<AsyncStateMachineAttribute>()
            ?.StateMachineType
            ?? throw new InvalidOperationException($"Method '{asyncMethod.Name}' is not an async state machine.");
        MethodInfo moveNext = stateMachineType.GetMethod(
            "MoveNext",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Async state machine for '{asyncMethod.Name}' has no MoveNext method.");
        byte[] il = moveNext.GetMethodBody()?.GetILAsByteArray()
            ?? throw new InvalidOperationException($"Async state machine for '{asyncMethod.Name}' has no IL body.");

        for (int index = 0; index <= il.Length - 5; index++)
        {
            if (il[index] is not (0x28 or 0x6F))
            {
                continue;
            }

            if (BitConverter.ToInt32(il, index + 1) == targetMethod.MetadataToken)
            {
                return true;
            }
        }

        return false;
    }

    private static Task RecordSuccessfulCleanup(string name, ICollection<string> cleanupOrder)
    {
        cleanupOrder.Add(name);
        return Task.CompletedTask;
    }

    private static Task RecordFailedCleanup(
        string name,
        ICollection<string> cleanupOrder,
        Exception exception)
    {
        cleanupOrder.Add(name);
        return Task.FromException(exception);
    }

    private static MethodInfo GetMethod(string methodName)
        => typeof(OpenBaoTopologyIntegrationTests).GetMethod(methodName)
            ?? throw new InvalidOperationException($"OpenBao topology method '{methodName}' was not found.");

    private static IReadOnlyList<string> GetCategoryTraits(MemberInfo member)
        => member.CustomAttributes
            .Where(attribute => attribute.AttributeType == typeof(TraitAttribute))
            .Where(attribute =>
                attribute.ConstructorArguments.Count == 2 &&
                string.Equals(
                    attribute.ConstructorArguments[0].Value as string,
                    "Category",
                    StringComparison.Ordinal))
            .Select(attribute => attribute.ConstructorArguments[1].Value as string)
            .Where(value => value is not null)
            .Cast<string>()
            .ToArray();
}
