// <copyright file="SearchEndpointDegradationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.IO;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 5.6 AC1, AC2, AC3, AC6 tests. The minimal-API search endpoint delegates live in
/// <c>Program.cs</c> and are not directly unit-testable; these tests protect the composable pieces
/// that drive endpoint behavior: transient-error classification, log-event dispatch, and the
/// <see cref="ErrorResponse"/> contract shape for the three new 503 codes.
/// </summary>
public class SearchEndpointDegradationTests
{
    // ============================================================================================
    // IsTransientRedisError — the classification rule that decides whether a RedisServerException
    // maps to 503 BACKEND_UNAVAILABLE or falls through as missing-data (empty 200).
    // ============================================================================================

    [Theory]
    [InlineData("LOADING Redis is loading the dataset in memory")]
    [InlineData("loading the dataset")]
    [InlineData("BUSY Redis is busy running a script")]
    [InlineData("OOM command not allowed when used memory > 'maxmemory'")]
    public void IsTransientRedisError_LoadingBusyOom_ShouldReturnTrue(string message)
    {
        RedisServerException ex = new(message);

        SearchEndpointDegradationLog.IsTransientRedisError(ex).ShouldBeTrue();
    }

    [Theory]
    [InlineData("no such index")]
    [InlineData("ERR no such index")]
    [InlineData("Unknown Index name")]
    [InlineData("ERR Unknown Index name: idx:memory-tenant-1")]
    public void IsTransientRedisError_MissingIndex_ShouldReturnFalse(string message)
    {
        // Regression guard: "no such index" / "Unknown Index name" are internal missing-data
        // conditions handled by SyntacticSearchService as empty results — NOT unavailability.
        RedisServerException ex = new(message);

        SearchEndpointDegradationLog.IsTransientRedisError(ex).ShouldBeFalse();
    }

    [Theory]
    [InlineData("WRONGTYPE Operation against a key holding the wrong kind of value")]
    [InlineData("ERR wrong number of arguments")]
    [InlineData("ERR unknown command 'FOOBAR'")]
    public void IsTransientRedisError_OtherRedisErrors_ShouldReturnFalse(string message)
    {
        RedisServerException ex = new(message);

        SearchEndpointDegradationLog.IsTransientRedisError(ex).ShouldBeFalse();
    }

    [Theory]
    [InlineData("ERR Syntax error at offset 12 near '@content:{secret}'")]
    [InlineData("Could not parse query")]
    public void IsTransientRedisError_QueryParserErrors_ShouldReturnFalse(string message)
    {
        RedisServerException ex = new(message);

        SearchEndpointDegradationLog.IsTransientRedisError(ex).ShouldBeFalse();
    }

    [Fact]
    public void IsTransientRedisError_NullException_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => SearchEndpointDegradationLog.IsTransientRedisError(null!));
    }

    // ============================================================================================
    // Log events — 5601 (backend-unavailable), 5602 (graph-unavailable), 5603 (hybrid total failure)
    // ============================================================================================

    [Fact]
    public void LogBackendUnavailable_ShouldEmitEventId5601AtWarning()
    {
        CapturingLogger logger = new();

        SearchEndpointDegradationLog.LogBackendUnavailable(logger, "syntactic", "tenant-a", "RedisConnectionException", "per-axis");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].EventId.Id.ShouldBe(5601);
        logger.Entries[0].Message.ShouldContain("syntactic");
        logger.Entries[0].Message.ShouldContain("tenant-a");
        logger.Entries[0].Message.ShouldContain("RedisConnectionException");
        logger.Entries[0].Message.ShouldContain("per-axis");
    }

    [Fact]
    public void LogGraphUnavailable_ShouldEmitEventId5602AtWarning()
    {
        CapturingLogger logger = new();

        SearchEndpointDegradationLog.LogGraphUnavailable(logger, "graph", "tenant-a", "mu-start", "RedisTimeoutException", "per-axis");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].EventId.Id.ShouldBe(5602);
        logger.Entries[0].Message.ShouldContain("graph");
        logger.Entries[0].Message.ShouldContain("tenant-a");
        logger.Entries[0].Message.ShouldContain("mu-start");
        logger.Entries[0].Message.ShouldContain("per-axis");
    }

    [Fact]
    public void LogHybridTotalFailure_ShouldEmitEventId5603AtWarning()
    {
        CapturingLogger logger = new();

        SearchEndpointDegradationLog.LogHybridTotalFailure(
            logger,
            "tenant-a",
            "syntactic, semantic, graph",
            "syntactic, semantic, graph",
            "all enabled axes unavailable",
            "total");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].EventId.Id.ShouldBe(5603);
        logger.Entries[0].Message.ShouldContain("tenant-a");
        logger.Entries[0].Message.ShouldContain("syntactic");
        logger.Entries[0].Message.ShouldContain("total");
    }

    [Fact]
    public void DescribeFailureReason_WithTransientRedisServerException_ShouldPreferTransientKeyword()
    {
        SearchEndpointDegradationLog.DescribeFailureReason(
            new RedisServerException("LOADING Redis is loading the dataset in memory"))
            .ShouldBe("LOADING");
    }

    // ============================================================================================
    // Response builders — direct tests for status-code routing and Retry-After behavior.
    // ============================================================================================

    [Fact]
    public async Task BuildBackendUnavailableResponse_ShouldReturn503WithRetryAfter()
    {
        DefaultHttpContext context = new();
        CapturingLogger logger = new();

        IResult result = SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(
            context,
            logger,
            "syntactic",
            "tenant-a",
            new RedisConnectionException(ConnectionFailureType.SocketFailure, "down"));

        (int statusCode, ErrorResponse? error, IHeaderDictionary headers) = await ExecuteErrorResultAsync(result, context);

        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("BACKEND_UNAVAILABLE");
        headers["Retry-After"].ToString().ShouldBe("5");
        logger.Entries[0].EventId.Id.ShouldBe(5601);
    }

    [Fact]
    public async Task BuildGraphUnavailableResponse_ShouldReturn503WithRetryAfter()
    {
        DefaultHttpContext context = new();
        CapturingLogger logger = new();

        IResult result = SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(
            context,
            logger,
            "tenant-a",
            "mu-1",
            new RedisTimeoutException("timed out", CommandStatus.Unknown));

        (int statusCode, ErrorResponse? error, IHeaderDictionary headers) = await ExecuteErrorResultAsync(result, context);

        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("GRAPH_UNAVAILABLE");
        headers["Retry-After"].ToString().ShouldBe("5");
        logger.Entries[0].EventId.Id.ShouldBe(5602);
    }

    [Theory]
    [InlineData(false, "GRAPH_UNAVAILABLE", 5602)]
    [InlineData(true, "BACKEND_UNAVAILABLE", 5601)]
    public async Task BuildGraphScopedAxisFailureResponse_ShouldClassifyByInnerSearchStarted(
        bool innerSearchStarted,
        string expectedCode,
        int expectedEventId)
    {
        DefaultHttpContext context = new();
        CapturingLogger logger = new();

        IResult result = SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
            context,
            logger,
            "semantic",
            "tenant-a",
            "mu-1",
            innerSearchStarted,
            new RedisConnectionException(ConnectionFailureType.SocketFailure, "down"));

        (int statusCode, ErrorResponse? error, IHeaderDictionary headers) = await ExecuteErrorResultAsync(result, context);

        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        error.ShouldNotBeNull();
        error.Code.ShouldBe(expectedCode);
        headers["Retry-After"].ToString().ShouldBe("5");
        logger.Entries[0].EventId.Id.ShouldBe(expectedEventId);
    }

    [Fact]
    public async Task BuildAllBackendsUnavailableResponse_ShouldReturn503WithRetryAfter()
    {
        DefaultHttpContext context = new();
        CapturingLogger logger = new();

        IResult result = SearchEndpointDegradationResponses.BuildAllBackendsUnavailableResponse(
            context,
            logger,
            "tenant-a",
            new[] { "semantic", "graph" },
            new[] { "graph", "semantic" });

        (int statusCode, ErrorResponse? error, IHeaderDictionary headers) = await ExecuteErrorResultAsync(result, context);

        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("ALL_BACKENDS_UNAVAILABLE");
        error.Message.ShouldContain("graph, semantic");
        headers["Retry-After"].ToString().ShouldBe("5");
        logger.Entries[0].EventId.Id.ShouldBe(5603);
    }

    [Fact]
    public async Task BuildGraphTimeoutResponse_ShouldReturn504WithoutRetryAfter()
    {
        DefaultHttpContext context = new();

        IResult result = SearchEndpointDegradationResponses.BuildGraphTimeoutResponse();

        (int statusCode, ErrorResponse? error, IHeaderDictionary headers) = await ExecuteErrorResultAsync(result, context);

        statusCode.ShouldBe(StatusCodes.Status504GatewayTimeout);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("GRAPH_TIMEOUT");
        headers.ContainsKey("Retry-After").ShouldBeFalse();
    }

    // ============================================================================================
    // Task 7.5 — ErrorResponse body round-trip per new 503 code.
    // Catches accidental code-string drift (e.g., BACKEND_UNAVAILABLE → INVALID_INPUT) that
    // would silently break dashboards and alerts keyed on these codes.
    // ============================================================================================

    [Theory]
    [InlineData("BACKEND_UNAVAILABLE", "Search backend is unavailable.", "Retry the request; the backend auto-recovers when Redis reconnects.")]
    [InlineData("GRAPH_UNAVAILABLE", "Graph backend is unavailable.", "Retry the request; graph auto-recovers when FalkorDB reconnects. Check infrastructure status.")]
    [InlineData("ALL_BACKENDS_UNAVAILABLE", "All enabled search backends are unavailable: syntactic, semantic, graph.", "Check infrastructure status (Redis Stack, FalkorDB). The service auto-recovers when backends reconnect; retry the request.")]
    public void ErrorResponse_NewDegradation503Codes_ShouldRoundTripWithNonEmptyMessageAndSuggestion(
        string code,
        string message,
        string suggestion)
    {
        ErrorResponse original = new(code, message, suggestion);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ErrorResponse? deserialized = JsonSerializer.Deserialize<ErrorResponse>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Code.ShouldBe(code);
        deserialized.Message.ShouldNotBeNullOrEmpty();
        deserialized.Suggestion.ShouldNotBeNullOrEmpty();
        deserialized.Message.ShouldBe(message);
        deserialized.Suggestion.ShouldBe(suggestion);
    }

    // Helpers ------------------------------------------------------------------------------------

    private static async Task<(int StatusCode, ErrorResponse? Error, IHeaderDictionary Headers)> ExecuteErrorResultAsync(
        IResult result,
        DefaultHttpContext context)
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        context.RequestServices = serviceProvider;
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        string body = await reader.ReadToEndAsync();
        ErrorResponse? error = string.IsNullOrWhiteSpace(body)
            ? null
            : JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options);
        return (context.Response.StatusCode, error, context.Response.Headers);
    }

    /// <summary>Minimal logger that records every emitted event for assertion — mirrors the
    /// pattern in <see cref="Tenants.TenantContextEnforcementTests"/>.</summary>
    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
