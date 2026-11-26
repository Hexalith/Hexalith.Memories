// <copyright file="EdgeConfidenceEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using NSubstitute;
using Shouldly;

/// <summary>Exercises edge-type shape validation and tenant denial through the HTTP endpoint.</summary>
public sealed class EdgeConfidenceEndpointTests
{
    [Theory]
    [InlineData("999", "tenant-a", HttpStatusCode.BadRequest, "INVALID_REQUEST_BODY")]
    [InlineData("\"references, annotates\"", "tenant-a", HttpStatusCode.BadRequest, "INVALID_REQUEST_BODY")]
    [InlineData("\"references, correlatedWith\"", "tenant-a", HttpStatusCode.BadRequest, "INVALID_REQUEST_BODY")]
    [InlineData("\"references\",\"EdgeType\":\"references, correlatedWith\"", "tenant-a", HttpStatusCode.BadRequest, "INVALID_REQUEST_BODY")]
    [InlineData("\"references, annotates\"", "tenant-b", HttpStatusCode.Forbidden, "TENANT_FORBIDDEN")]
    public async Task Patch_InvalidEdgeTypeOrUnauthorizedTenant_StopsBeforeGraphAccess(
        string edgeTypeJson,
        string tenantId,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        using EventStoreWebAppFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));
        using StringContent content = new(
            $$"""{"sourceNodeId":"source","targetNodeId":"target","edgeType":{{edgeTypeJson}},"newConfidence":0.75,"verifiedBy":"operator"}""",
            Encoding.UTF8,
            "application/json");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using HttpResponseMessage response = await client.PatchAsync(
            $"/api/v1/tenants/{tenantId}/edges/confidence", content, cancellationToken);

        response.StatusCode.ShouldBe(expectedStatus);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            MemoriesJsonContext.Options, cancellationToken);
        error.ShouldNotBeNull().Code.ShouldBe(expectedCode);
        factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
    }
}
