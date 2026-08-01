// <copyright file="AspireIngestionPipelineFixtureTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.Net;
using System.Reflection;

using Hexalith.Memories.AppHost;
using Hexalith.Memories.IntegrationTests.Mcp;

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
