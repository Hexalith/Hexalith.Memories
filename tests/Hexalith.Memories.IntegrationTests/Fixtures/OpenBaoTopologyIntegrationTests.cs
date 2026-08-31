// <copyright file="OpenBaoTopologyIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.IO;
using System.Net;

using Hexalith.Memories.AppHost;

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
        await fixture.WaitForOpenBaoSidecarMatrixReadinessAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

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
    [Trait("Category", "IntegrationSlow")]
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
        Uri daprEndpointBeforeRestart = fixture.DaprSidecarHttpEndpoint;

        (await fixture.RestartOpenBaoGenerationInPlaceAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.IsOpenBaoInitializedAndUnsealedAsync().ConfigureAwait(true)).ShouldBeTrue();
        fixture.DaprSidecarHttpEndpoint.ShouldNotBe(
            daprEndpointBeforeRestart,
            "the restart regression must rotate the primary Dapr endpoint before proving actor recovery.");
        var actorConfig = await fixture
            .CreateTenantConfigurationActorProxy(AspireIngestionPipelineFixture.OpenBaoRecoveryTenantId)
            .GetEmbeddingConfigAsync()
            .ConfigureAwait(true);
        actorConfig.Dimensions.ShouldBeGreaterThan(0);
        output.WriteLine(
            "In-place OpenBao restart installed a rotated generation and recovered all dependent sidecars and actor clients.");
    }

    [Fact]
    public async Task DiagnosticsModelGeneratedFilesAndLogs_ContainNoSensitiveMaterial()
    {
        (await fixture.HasOpenBaoSensitiveDisclosureAsync().ConfigureAwait(true)).ShouldBeFalse(
            "Seed canaries, scoped tokens, bootstrap tokens, and unseal keys must be absent from model, diagnostics, " +
            $"generated YAML/HCL, and logs. Surface: {fixture.OpenBaoSensitiveDisclosureSurface ?? "none"}.");
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    [Trait("Category", "Performance")]
    public void OpenBaoColdStart_TopologyAcceptsQueriesWithinNfr7()
    {
        fixture.OpenBaoColdStartDuration.ShouldBeLessThan(
            TimeSpan.FromSeconds(60),
            "NFR7 measures from all containers running until the root topology accepts queries, excluding image pull.");
        output.WriteLine(
            $"OpenBao cold start: {fixture.OpenBaoColdStartDuration.TotalSeconds:F3}s; " +
            $"OS={Environment.OSVersion}; framework={Environment.Version}; lane=Aspire root integration.");
    }

    /// <summary>
    /// Story 29.2 -- <c>Hexalith.Memories.Aspire</c>'s two hosting extensions
    /// (<c>AddHexalithMemoriesSearchIndexServer</c>, <c>AddHexalithMemoriesAccessTelemetry</c>) take an
    /// externally-provisioned secret-store <c>IResourceBuilder&lt;IDaprComponentResource&gt;</c> instead of
    /// building their own; they never construct a <c>secretstores.local.file</c> component. The root
    /// AppHost this fixture exercises does not call either extension (Story 29.2's Design Notes place that
    /// rewiring out of scope), so the live evidence above --
    /// <see cref="DaprSecretReads_RuntimeAndAccessTelemetryCanariesMatchExpectedFingerprints"/>,
    /// <see cref="ProviderBoundary_DaprScopesKeyScopesPoliciesPrefixesAndBulkOperationsFailClosed"/>, and
    /// <see cref="DaprSidecarMatrix_EnforcesExactComponentAndKeyAllowDenyBoundaries"/> -- is what proves the
    /// component shape a consumer must supply (<c>secretstores.hashicorp.vault</c> named
    /// <c>secretstore</c> / <c>access-telemetry-secrets</c>) resolves OpenBao values through Dapr without
    /// disclosure and fails closed on cross-prefix reads. Supplying that exact resource into either
    /// generalized extension inherits the same proof. This guard pins the extension source so a regression
    /// back to a hard-coded local-file component fails here too, not only in
    /// <c>Hexalith.Memories.Server.Tests</c>.
    /// </summary>
    [Fact]
    public void GeneralizedAspireExtensions_RequireTheExternallyProvisionedComponentShapeThisFixtureProvesResolvesOpenBaoWithoutDisclosure()
    {
        string root = RepositoryRootLocator.Resolve();
        string[] extensionSources =
        [
            File.ReadAllText(Path.Combine(root, "src", "Hexalith.Memories.Aspire", "HexalithMemoriesServerExtensions.cs")),
            File.ReadAllText(Path.Combine(root, "src", "Hexalith.Memories.Aspire", "HexalithMemoriesAccessTelemetryExtensions.cs")),
        ];

        foreach (string source in extensionSources)
        {
            source.ShouldNotContain("\"secretstores.local.file\"", Case.Sensitive);
            source.ShouldContain("IResourceBuilder<IDaprComponentResource> secretStore,", Case.Sensitive);
            source.ShouldContain("ArgumentNullException.ThrowIfNull(secretStore);", Case.Sensitive);
        }
    }

    private static bool IsSuccess(HttpStatusCode statusCode)
        => (int)statusCode is >= 200 and <= 299;
}
