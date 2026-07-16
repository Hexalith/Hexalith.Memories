// <copyright file="TenantEndpointFilterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.IO;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Tenants;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

/// <summary>Story 25.2 coverage for reusable tenant id and tenant status endpoint filters.</summary>
public sealed class TenantEndpointFilterTests
{
    [Fact]
    public async Task TenantIdValidationEndpointFilter_InvalidRouteTenant_ReturnsInvalidTenantIdAndSkipsNext()
    {
        DefaultHttpContext httpContext = CreateHttpContext("bad tenant!");
        EndpointFilterInvocationContext invocation = new DefaultEndpointFilterInvocationContext(httpContext);
        var filter = new TenantIdValidationEndpointFilter();
        bool nextCalled = false;

        object? result = await filter.InvokeAsync(invocation, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.ShouldBeFalse();
        IResult httpResult = result as IResult ?? throw new InvalidOperationException("Filter did not return an HTTP result.");
        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(httpResult);
        statusCode.ShouldBe(StatusCodes.Status400BadRequest);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Fact]
    public async Task TenantIdValidationEndpointFilter_BlankResolvedTenant_ReturnsInvalidTenantIdAndSkipsNext()
    {
        DefaultHttpContext httpContext = CreateHttpContext(" ");
        EndpointFilterInvocationContext invocation = new DefaultEndpointFilterInvocationContext(httpContext);
        var filter = new TenantIdValidationEndpointFilter();
        bool nextCalled = false;

        object? result = await filter.InvokeAsync(invocation, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.ShouldBeFalse();
        IResult httpResult = result as IResult ?? throw new InvalidOperationException("Filter did not return an HTTP result.");
        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(httpResult);
        statusCode.ShouldBe(StatusCodes.Status400BadRequest);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Fact]
    public async Task TenantIdValidationEndpointFilter_QueryTenantDoesNotOverrideBodyTenant()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.QueryString = new QueryString("?tenantId=bad tenant!");
        EndpointFilterInvocationContext invocation = new DefaultEndpointFilterInvocationContext(
            httpContext,
            new object?[]
            {
                new IngestionInput
                {
                    TenantId = "tenant-a",
                    CaseId = "case-1",
                    SourceUri = "memory://tenant-a/case-1/unit-1",
                    ContentType = "text/plain",
                    SourceType = SourceType.File,
                    IngestedBy = "operator-1",
                },
            });
        var filter = new TenantIdValidationEndpointFilter();
        bool nextCalled = false;

        object? result = await filter.InvokeAsync(invocation, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.ShouldBeTrue();
        IResult httpResult = result as IResult ?? throw new InvalidOperationException("Filter did not return an HTTP result.");
        int statusCode = await ExecuteStatusAsync(httpResult);
        statusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task TenantStatusEndpointFilter_ActiveOnlyMissingTenant_Returns404AndSkipsNext()
    {
        var filter = new TenantStatusEndpointFilter(CreateGuardReturning("missing", null), TenantStatusValidationMode.ActiveOnly);
        EndpointFilterInvocationContext invocation = new DefaultEndpointFilterInvocationContext(CreateHttpContext("missing"));
        bool nextCalled = false;

        object? result = await filter.InvokeAsync(invocation, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.ShouldBeFalse();
        IResult httpResult = result as IResult ?? throw new InvalidOperationException("Filter did not return an HTTP result.");
        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(httpResult);
        statusCode.ShouldBe(StatusCodes.Status404NotFound);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task TenantStatusEndpointFilter_BlankTenant_Returns400AndSkipsNext()
    {
        var filter = new TenantStatusEndpointFilter(CreateGuardReturning("tenant-a", null), TenantStatusValidationMode.ActiveOnly);
        EndpointFilterInvocationContext invocation = new DefaultEndpointFilterInvocationContext(CreateHttpContext(" "));
        bool nextCalled = false;

        object? result = await filter.InvokeAsync(invocation, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.ShouldBeFalse();
        IResult httpResult = result as IResult ?? throw new InvalidOperationException("Filter did not return an HTTP result.");
        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(httpResult);
        statusCode.ShouldBe(StatusCodes.Status400BadRequest);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Fact]
    public async Task TenantStatusEndpointFilter_ActiveOnlyNonActiveTenant_Returns409AndSkipsNext()
    {
        TenantInfo tenant = new("tenant-deleting", "Tenant", TenantStatus.Deleting, DateTimeOffset.UtcNow);
        var filter = new TenantStatusEndpointFilter(CreateGuardReturning(tenant.Id, tenant), TenantStatusValidationMode.ActiveOnly);
        EndpointFilterInvocationContext invocation = new DefaultEndpointFilterInvocationContext(CreateHttpContext(tenant.Id));
        bool nextCalled = false;

        object? result = await filter.InvokeAsync(invocation, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.ShouldBeFalse();
        IResult httpResult = result as IResult ?? throw new InvalidOperationException("Filter did not return an HTTP result.");
        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(httpResult);
        statusCode.ShouldBe(StatusCodes.Status409Conflict);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_DELETING");
    }

    [Fact]
    public async Task TenantStatusEndpointFilter_ExistsOnlyNonActiveTenant_AllowsEndpointBody()
    {
        TenantInfo tenant = new("tenant-deleting", "Tenant", TenantStatus.Deleting, DateTimeOffset.UtcNow);
        var filter = new TenantStatusEndpointFilter(CreateGuardReturning(tenant.Id, tenant), TenantStatusValidationMode.ExistsOnly);
        EndpointFilterInvocationContext invocation = new DefaultEndpointFilterInvocationContext(CreateHttpContext(tenant.Id));

        object? result = await filter.InvokeAsync(
            invocation,
            _ => ValueTask.FromResult<object?>(Results.Ok(new { ran = true })));

        IResult httpResult = result as IResult ?? throw new InvalidOperationException("Filter did not return an HTTP result.");
        int statusCode = await ExecuteStatusAsync(httpResult);
        statusCode.ShouldBe(StatusCodes.Status200OK);
    }

    private static DefaultHttpContext CreateHttpContext(string tenantId)
    {
        DefaultHttpContext context = new();
        context.Request.RouteValues["tenantId"] = tenantId;
        return context;
    }

    private static TenantStatusGuard CreateGuardReturning(string tenantId, TenantInfo? tenant)
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        StoredTenantRegistryEntry? entry = tenant is null
            ? null
            : PersistenceModelMapper.ToStored(new TenantRegistryEntry(tenant, null));
        daprClient.GetStateAsync<StoredTenantRegistryEntry?>(
                "statestore",
                $"tenant-registry-{tenantId}",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);
        return new TenantStatusGuard(new TenantRegistryService(
            daprClient,
            Substitute.For<ILogger<TenantRegistryService>>()));
    }

    private static async Task<(int StatusCode, ErrorResponse? Error)> ExecuteErrorAsync(IResult result)
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext context = new() { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();
        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        ErrorResponse? error = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body,
            MemoriesJsonContext.Options);
        return (context.Response.StatusCode, error);
    }

    private static async Task<int> ExecuteStatusAsync(IResult result)
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext context = new() { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();
        await result.ExecuteAsync(context);
        return context.Response.StatusCode;
    }
}
