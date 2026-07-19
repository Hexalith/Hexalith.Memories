// <copyright file="OpenBaoTopologyIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;

using Shouldly;

using Xunit;

/// <summary>Live isolation, restart, disclosure, and cold-start evidence for the root AppHost topology.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class OpenBaoTopologyIntegrationTests(
    AspireIngestionPipelineFixture fixture,
    ITestOutputHelper output)
{
    [Fact]
    public async Task DaprSecretReads_RuntimeAndAccessTelemetryCanariesMatchExpectedFingerprints()
    {
        HttpStatusCode runtimeStatus = await fixture.GetDaprSecretStatusAsync(
            "secretstore",
            AspireIngestionPipelineFixture.OpenBaoRuntimeCanarySecretName).ConfigureAwait(true);
        HttpStatusCode accessStatus = await fixture.GetDaprSecretStatusAsync(
            "access-telemetry-secrets",
            "access-telemetry-marker-key").ConfigureAwait(true);
        output.WriteLine($"Dapr secret statuses: runtime={(int)runtimeStatus}; access={(int)accessStatus}.");
        runtimeStatus.ShouldBe(HttpStatusCode.OK);
        accessStatus.ShouldBe(HttpStatusCode.OK);
        (await fixture.CanReadOpenBaoRuntimeCanaryAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.CanReadOpenBaoAccessMarkerAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.IsOpenBaoInitializedAndUnsealedAsync().ConfigureAwait(true)).ShouldBeTrue();
    }

    [Fact]
    public async Task ProviderBoundary_DaprScopesKeyScopesPoliciesPrefixesAndBulkOperationsFailClosed()
    {
        (await fixture.AreOpenBaoCrossPrefixIdentitiesDeniedAsync().ConfigureAwait(true)).ShouldBeTrue(
            "Both OpenBao identities must be denied before resolving a secret from the opposite shared application prefix.");

        HttpStatusCode runtimeTraversal = await fixture.GetDaprSecretStatusAsync(
            "secretstore",
            "../access-telemetry/access-telemetry-marker-key").ConfigureAwait(true);
        HttpStatusCode accessTraversal = await fixture.GetDaprSecretStatusAsync(
            "access-telemetry-secrets",
            $"../runtime/{AspireIngestionPipelineFixture.OpenBaoRuntimeCanarySecretName}").ConfigureAwait(true);
        IsSuccess(runtimeTraversal).ShouldBeFalse(
            "The Server runtime component must not cross into the access-telemetry prefix.");
        IsSuccess(accessTraversal).ShouldBeFalse(
            "The Server access component must not cross into the runtime prefix.");

        HttpStatusCode runtimeBulk = await fixture.GetDaprBulkSecretStatusAsync("secretstore").ConfigureAwait(true);
        HttpStatusCode accessBulk = await fixture.GetDaprBulkSecretStatusAsync("access-telemetry-secrets").ConfigureAwait(true);
        IsSuccess(runtimeBulk).ShouldBeFalse("The runtime policy deliberately grants no list capability.");
        IsSuccess(accessBulk).ShouldBeFalse("The access policy deliberately grants no list capability.");
    }

    [Fact]
    public async Task DaprSidecarMatrix_EnforcesExactComponentAndKeyAllowDenyBoundaries()
    {
        const string runtimeStore = "secretstore";
        const string accessStore = "access-telemetry-secrets";
        const string markerKey = "access-telemetry-marker-key";
        const string clockKey = "access-telemetry-clock-key";

        (await fixture.DaprSecretMatchesAsync(
            "memories-dapr-cli",
            runtimeStore,
            AspireIngestionPipelineFixture.OpenBaoRuntimeCanarySecretName,
            AspireIngestionPipelineFixture.OpenBaoRuntimeCanarySecretName,
            fixture.RuntimeCanaryFingerprint).ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.DaprSecretMatchesAsync(
            "memories-dapr-cli",
            accessStore,
            markerKey,
            markerKey,
            fixture.AccessTelemetryMarkerFingerprint).ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.GetDaprSecretStatusAsync(
            "memories-dapr-cli",
            accessStore,
            clockKey).ConfigureAwait(true)).ShouldBe(HttpStatusCode.Forbidden);

        (await fixture.DaprSecretMatchesAsync(
            "memories-access-telemetry-dapr-cli",
            accessStore,
            markerKey,
            markerKey,
            fixture.AccessTelemetryMarkerFingerprint).ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.GetDaprSecretStatusAsync(
            "memories-access-telemetry-dapr-cli",
            accessStore,
            clockKey).ConfigureAwait(true)).ShouldBe(HttpStatusCode.Forbidden);
        (await fixture.GetDaprSecretStatusAsync(
            "memories-access-telemetry-dapr-cli",
            runtimeStore,
            AspireIngestionPipelineFixture.OpenBaoRuntimeCanarySecretName).ConfigureAwait(true))
            .ShouldBe(HttpStatusCode.Unauthorized);

        (await fixture.DaprSecretMatchesAsync(
            "memories-access-telemetry-clock-dapr-cli",
            accessStore,
            clockKey,
            "signing-key-pkcs8",
            fixture.AccessTelemetryClockFingerprint).ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.GetDaprSecretStatusAsync(
            "memories-access-telemetry-clock-dapr-cli",
            accessStore,
            markerKey).ConfigureAwait(true)).ShouldBe(HttpStatusCode.Forbidden);
        (await fixture.GetDaprSecretStatusAsync(
            "memories-access-telemetry-clock-dapr-cli",
            runtimeStore,
            AspireIngestionPipelineFixture.OpenBaoRuntimeCanarySecretName).ConfigureAwait(true))
            .ShouldBe(HttpStatusCode.Unauthorized);

        (await fixture.GetDaprSecretStatusAsync(
            "memories-mcp-dapr-cli",
            runtimeStore,
            AspireIngestionPipelineFixture.OpenBaoRuntimeCanarySecretName).ConfigureAwait(true))
            .ShouldBe(HttpStatusCode.InternalServerError);
        (await fixture.GetDaprSecretStatusAsync(
            "memories-mcp-dapr-cli",
            accessStore,
            markerKey).ConfigureAwait(true)).ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task FullTopologyRestart_ReinitializesOpenBaoAndRecoversPermittedDaprReads()
    {
        (await fixture.CanReadOpenBaoRuntimeCanaryAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.CanReadOpenBaoAccessMarkerAsync().ConfigureAwait(true)).ShouldBeTrue();

        TimeSpan restartDuration = await fixture.RestartTopologyAsync().ConfigureAwait(true);

        (await fixture.IsOpenBaoInitializedAndUnsealedAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.CanReadOpenBaoRuntimeCanaryAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.CanReadOpenBaoAccessMarkerAsync().ConfigureAwait(true)).ShouldBeTrue();
        output.WriteLine($"Disposable topology restart completed in {restartDuration.TotalSeconds:F3}s.");
    }

    [Fact]
    public async Task InPlaceOpenBaoRestart_RotatesGenerationAndRecoversDependentSidecars()
    {
        (await fixture.RestartOpenBaoGenerationInPlaceAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.IsOpenBaoInitializedAndUnsealedAsync().ConfigureAwait(true)).ShouldBeTrue();
        output.WriteLine("In-place OpenBao restart installed a rotated generation and recovered all dependent sidecars.");
    }

    [Fact]
    public async Task DiagnosticsModelGeneratedFilesAndLogs_ContainNoSensitiveMaterial_AndColdStartMeetsNfr7()
    {
        (await fixture.HasOpenBaoSensitiveDisclosureAsync().ConfigureAwait(true)).ShouldBeFalse(
            "Seed canaries, scoped tokens, bootstrap tokens, and unseal keys must be absent from model, diagnostics, " +
            $"generated YAML/HCL, and logs. Surface: {fixture.OpenBaoSensitiveDisclosureSurface ?? "none"}.");
        fixture.OpenBaoColdStartDuration.ShouldBeLessThan(
            TimeSpan.FromSeconds(60),
            "NFR7 measures from all containers running until the root topology accepts queries, excluding image pull.");
        output.WriteLine(
            $"OpenBao cold start: {fixture.OpenBaoColdStartDuration.TotalSeconds:F3}s; " +
            $"OS={Environment.OSVersion}; framework={Environment.Version}; lane=Aspire root integration.");
    }

    private static bool IsSuccess(HttpStatusCode statusCode)
        => (int)statusCode is >= 200 and <= 299;
}
