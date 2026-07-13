// <copyright file="TenantAuthorizationEndpointFilterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using System.Security.Claims;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

/// <summary>Tests for server tenant endpoint authorization.</summary>
[Trait("Category", "Unit")]
public sealed class TenantAuthorizationEndpointFilterTests
{
    [Fact]
    public void TryAuthorizeTenant_MatchingTenant_AllowsAndSnapshotsTenant()
    {
        DefaultHttpContext context = CreateContext("tenant-a");

        bool authorized = TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
            context,
            "tenant-a",
            "test-endpoint",
            NullLogger<TenantAuthorizationEndpointFilter>.Instance,
            out IResult? result);

        authorized.ShouldBeTrue();
        result.ShouldBeNull();
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey].ShouldBe("tenant-a");
    }

    [Fact]
    public async Task TryAuthorizeTenant_MissingTenantClaim_ReturnsTenantForbidden()
    {
        DefaultHttpContext context = CreateContext();

        bool authorized = TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
            context,
            "tenant-a",
            "test-endpoint",
            NullLogger<TenantAuthorizationEndpointFilter>.Instance,
            out IResult? result);

        authorized.ShouldBeFalse();
        ErrorResponse error = await ExecuteErrorAsync(result);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
    }

    [Fact]
    public async Task TryAuthorizeTenant_MalformedRequestedTenant_ReturnsTenantForbiddenWithoutEchoingInput()
    {
        DefaultHttpContext context = CreateContext("tenant-a");
        string poisoned = "tenant-a\u202E";

        bool authorized = TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
            context,
            poisoned,
            "test-endpoint",
            NullLogger<TenantAuthorizationEndpointFilter>.Instance,
            out IResult? result);

        authorized.ShouldBeFalse();
        ErrorResponse error = await ExecuteErrorAsync(result);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        error.Message.ShouldNotContain(poisoned);
        error.Suggestion.ShouldNotContain(poisoned);
    }

    [Fact]
    public void IsWellFormedTenantId_TenantWithUnderscore_ReturnsTrue()
        => TenantAuthorizationEndpointFilter.IsWellFormedTenantId("tenant_a").ShouldBeTrue();

    [Fact]
    public async Task TryAuthorizeTenant_MismatchedTenant_ReturnsTenantForbidden()
    {
        DefaultHttpContext context = CreateContext("tenant-a");

        bool authorized = TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
            context,
            "tenant-b",
            "test-endpoint",
            NullLogger<TenantAuthorizationEndpointFilter>.Instance,
            out IResult? result);

        authorized.ShouldBeFalse();
        ErrorResponse error = await ExecuteErrorAsync(result);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
    }

    [Fact]
    public void AuthorizedTenantAccessor_ReturnsRequestScopedSnapshot()
    {
        var context = new DefaultHttpContext();
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey] = "tenant-a";
        var accessor = new AuthorizedTenantAccessor(new HttpContextAccessor { HttpContext = context });

        accessor.TryGetAuthorizedTenant(out string tenantId).ShouldBeTrue();
        tenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public void TryAuthorizeTenant_SameRequestAlreadyAuthorizedForTenant_AllowsIdempotently()
    {
        DefaultHttpContext context = CreateContext("tenant-a");
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey] = "tenant-a";

        bool authorized = TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
            context,
            "tenant-a",
            "endpoint-filter-after-middleware",
            NullLogger<TenantAuthorizationEndpointFilter>.Instance,
            out IResult? result);

        authorized.ShouldBeTrue();
        result.ShouldBeNull();
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey].ShouldBe("tenant-a");
    }

    [Fact]
    public void TryAuthorizeTenant_SameRequestContainsDifferentAuthorizedTenant_FailsClosed()
    {
        DefaultHttpContext context = CreateContext("tenant-b");
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey] = "tenant-a";

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
                context,
                "tenant-b",
                "conflicting-endpoint",
                NullLogger<TenantAuthorizationEndpointFilter>.Instance,
                out _));

        exception.Message.ShouldBe("HttpContext.Items contains conflicting tenant authorization state.");
    }

    private static DefaultHttpContext CreateContext(params string[] tenants)
    {
        var context = new DefaultHttpContext();
        Claim[] claims = [.. tenants.Select(tenant => new Claim(ServerTenantClaimsTransformation.TenantClaimType, tenant))];
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        return context;
    }

    private static Task<ErrorResponse> ExecuteErrorAsync(IResult? result)
    {
        result.ShouldNotBeNull();
        JsonHttpResult<ErrorResponse> json = result.ShouldBeOfType<JsonHttpResult<ErrorResponse>>();
        json.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        ErrorResponse? error = json.Value;
        error.ShouldNotBeNull();
        return Task.FromResult(error);
    }
}
