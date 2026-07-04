// <copyright file="ServerTenantClaimsTransformationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using System.Security.Claims;

using Hexalith.Memories.Server.Authentication;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Tenant claim normalization tests for the Memories Server bearer principal.</summary>
[Trait("Category", "Unit")]
public sealed class ServerTenantClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_ConfiguredTenantClaim_AddsNormalizedTenantClaims()
    {
        ClaimsPrincipal principal = CreatePrincipal(new Claim("tenant_id", "tenant-a tenant-b"));
        ServerTenantClaimsTransformation transformation = CreateTransformation();

        ClaimsPrincipal transformed = await transformation.TransformAsync(principal);

        GetTenants(transformed).ShouldBe(["tenant-a", "tenant-b"], ignoreOrder: false);
    }

    [Fact]
    public async Task TransformAsync_TenantsJsonArray_AddsNormalizedTenantClaims()
    {
        ClaimsPrincipal principal = CreatePrincipal(new Claim("tenants", "[\"tenant-a\",\"tenant-b\"]"));
        ServerTenantClaimsTransformation transformation = CreateTransformation();

        ClaimsPrincipal transformed = await transformation.TransformAsync(principal);

        GetTenants(transformed).ShouldBe(["tenant-a", "tenant-b"], ignoreOrder: false);
    }

    [Theory]
    [InlineData("tid")]
    [InlineData("tenant")]
    public async Task TransformAsync_AlternateTenantClaims_AddsNormalizedTenantClaim(string claimType)
    {
        ClaimsPrincipal principal = CreatePrincipal(new Claim(claimType, "tenant-a"));
        ServerTenantClaimsTransformation transformation = CreateTransformation();

        ClaimsPrincipal transformed = await transformation.TransformAsync(principal);

        GetTenants(transformed).ShouldBe(["tenant-a"], ignoreOrder: false);
    }

    [Fact]
    public async Task TransformAsync_ConflictingCaseInsensitiveTenantClaims_ReturnsUnauthenticatedPrincipal()
    {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("tenant_id", "tenant-a"),
            new Claim("Tenant_Id", "tenant-b"));
        ServerTenantClaimsTransformation transformation = CreateTransformation();

        ClaimsPrincipal transformed = await transformation.TransformAsync(principal);

        transformed.Identity?.IsAuthenticated.ShouldBeFalse();
        GetTenants(transformed).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_SubjectClaim_AddsNameIdentifierWhenMissing()
    {
        ClaimsPrincipal principal = CreatePrincipal(new Claim("sub", "operator-42"));
        ServerTenantClaimsTransformation transformation = CreateTransformation();

        ClaimsPrincipal transformed = await transformation.TransformAsync(principal);

        transformed.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("operator-42");
    }

    private static ServerTenantClaimsTransformation CreateTransformation()
        => new(
            Options.Create(new MemoriesServerAuthenticationOptions
            {
                Issuer = ServerTestBearerToken.Issuer,
                Audience = ServerTestBearerToken.Audience,
                SigningKey = ServerTestBearerToken.SigningKey,
            }),
            NullLogger<ServerTenantClaimsTransformation>.Instance);

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "Bearer"));

    private static string[] GetTenants(ClaimsPrincipal principal)
        => [.. principal.FindAll(ServerTenantClaimsTransformation.TenantClaimType).Select(c => c.Value)];
}
