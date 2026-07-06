// <copyright file="ErrorResultsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.IO;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>Story 25.2 coverage for common endpoint error result factories.</summary>
public sealed class ErrorResultsTests
{
    [Fact]
    public void InvalidTenantId_PreservesEnvelopeShape()
    {
        ErrorResponse error = ErrorResults.InvalidTenantId("TenantId 'bad tenant' contains invalid characters.");

        error.Code.ShouldBe("INVALID_TENANT_ID");
        error.Message.ShouldBe("TenantId 'bad tenant' contains invalid characters.");
        error.Suggestion.ShouldBe("Use only alphanumeric characters and hyphens for tenant identifiers.");
    }

    [Theory]
    [InlineData("TENANT_NOT_FOUND", StatusCodes.Status404NotFound)]
    [InlineData("TENANT_DELETING", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_PROVISIONING", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_FAILED", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_UNAVAILABLE", StatusCodes.Status409Conflict)]
    public async Task TenantStatusResult_PreservesTenantStateStatusMapping(string code, int expectedStatus)
    {
        IResult result = ErrorResults.TenantStatusResult(new ErrorResponse(code, "message", "suggestion"));

        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(result);

        statusCode.ShouldBe(expectedStatus);
        error.ShouldNotBeNull();
        error.Code.ShouldBe(code);
    }

    [Fact]
    public async Task RateLimitExceededResult_ReturnsStable429Envelope()
    {
        IResult result = ErrorResults.RateLimitExceededResult();

        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(result);

        statusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("RATE_LIMIT_EXCEEDED");
        error.Message.ShouldBe("The tenant request rate limit was exceeded.");
        error.Suggestion.ShouldBe("Retry after the limiter window resets.");
    }

    [Fact]
    public async Task LookupBackendUnavailableResult_ReturnsStable503Envelope()
    {
        IResult result = ErrorResults.LookupBackendUnavailableResult();

        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(result);

        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("LOOKUP_BACKEND_UNAVAILABLE");
        error.Suggestion.ShouldBe("Retry shortly; do not treat this as 'no unit exists'.");
    }

    [Fact]
    public async Task UnhandledExceptionResult_ReturnsSanitized500Envelope()
    {
        IResult result = ErrorResults.UnhandledExceptionResult();

        (int statusCode, ErrorResponse? error) = await ExecuteErrorAsync(result);

        statusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("UNHANDLED_EXCEPTION");
        error.Message.ShouldNotContain("stack", Shouldly.Case.Insensitive);
    }

    [Fact]
    public void SetRetryAfter_ExistingHeader_DoesNotOverwriteRetryAfterHeader()
    {
        DefaultHttpContext context = new();
        context.Response.Headers.RetryAfter = "11";

        ErrorResults.SetRetryAfter(context, 5);

        context.Response.Headers.RetryAfter.ToString().ShouldBe("11");
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
}
