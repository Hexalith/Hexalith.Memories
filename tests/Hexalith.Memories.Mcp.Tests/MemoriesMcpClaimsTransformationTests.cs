// <copyright file="MemoriesMcpClaimsTransformationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Security.Claims;

using Hexalith.Memories.Mcp.Authentication;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

public sealed class MemoriesMcpClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_NormalizesTenantClaimAndSubject()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "subject-1"),
                new Claim("tenant_id", "tenant-a"),
            ],
            "Bearer"));
        var transform = CreateTransform();

        ClaimsPrincipal result = await transform.TransformAsync(principal);

        result.FindFirst(ClaimTypes.NameIdentifier)!.Value.ShouldBe("subject-1");
        result.FindAll(MemoriesMcpClaimsTransformation.TenantClaimType)
            .Select(c => c.Value)
            .ShouldBe(["tenant-a"]);
    }

    [Fact]
    public async Task TransformAsync_ParsesTenantsJsonArray()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("tenants", "[\"tenant-a\",\"tenant-b\"]")],
            "Bearer"));
        var transform = CreateTransform();

        ClaimsPrincipal result = await transform.TransformAsync(principal);

        result.FindAll(MemoriesMcpClaimsTransformation.TenantClaimType)
            .Select(c => c.Value)
            .ShouldBe(["tenant-a", "tenant-b"]);
    }

    [Fact]
    public async Task TransformAsync_DuplicateTenantClaimsDifferingByCase_ReturnsUnauthenticatedPrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenant_id", "tenant-a"),
                new Claim("Tenant_Id", "tenant-b"),
            ],
            "Bearer"));
        var transform = CreateTransform();

        ClaimsPrincipal result = await transform.TransformAsync(principal);

        result.Identity!.IsAuthenticated.ShouldBeFalse();
        result.Claims.ShouldBeEmpty();
    }

    private static MemoriesMcpClaimsTransformation CreateTransform()
        => new(
            Options.Create(new MemoriesMcpAuthenticationOptions()),
            NullLogger<MemoriesMcpClaimsTransformation>.Instance);
}
