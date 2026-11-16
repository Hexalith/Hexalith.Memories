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
public sealed partial class OpenBaoTopologyIntegrationTests(
    AspireIngestionPipelineFixture fixture,
    ITestOutputHelper output)
{

    // [Review][Patch]: the source-grep guard formerly here (`GeneralizedAspireExtensions_...`) duplicated
    // assertions already covered by `HexalithMemoriesServerExtensions_...`/`AccessTelemetryExtension_...` in
    // `Hexalith.Memories.Server.Tests/Deployment/HexalithMemoriesAspireSecretStoreTests.cs`. This fixture's
    // live evidence above -- <see cref="DaprSecretReads_RuntimeAndAccessTelemetryCanariesMatchExpectedFingerprints"/>,
    // <see cref="ProviderBoundary_DaprScopesKeyScopesPoliciesPrefixesAndBulkOperationsFailClosed"/>, and
    // <see cref="DaprSidecarMatrix_EnforcesExactComponentAndKeyAllowDenyBoundaries"/> -- already proves the
    // component shape a consumer must supply resolves OpenBao values through Dapr without disclosure; the
    // structural regression guard for the generalized extensions' source belongs solely in the unit-test
    // project, not duplicated here where it could be mistaken for broader live/integration proof.

    private static bool IsSuccess(HttpStatusCode statusCode)
        => (int)statusCode is >= 200 and <= 299;
}
