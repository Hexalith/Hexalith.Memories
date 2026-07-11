// <copyright file="McpToolExecutorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Net;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Mcp;

using ModelContextProtocol.Protocol;

using Shouldly;

public sealed class McpToolExecutorTests
{
    [Fact]
    public async Task RunAsync_ValidationPrecedesAuthorizationAndOperation()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor("tenant-a");
        bool operationCalled = false;

        CallToolResult result = await executor.RunAsync(
            "malformed tenant",
            "test_tool",
            mapper => mapper.MapValidation("INVALID_INPUT", "invalid input", "fix input", "test_tool"),
            (_, _) =>
            {
                operationCalled = true;
                return Task.FromResult(new CallToolResult());
            },
            TestContext.Current.CancellationToken);

        operationCalled.ShouldBeFalse();
        AssertIsErrorWithCode(result, "INVALID_INPUT");
    }

    [Fact]
    public async Task RunAsync_NonErrorValidationResult_ReturnsSanitizedInternalErrorWithoutOperation()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor();
        bool operationCalled = false;

        CallToolResult result = await executor.RunAsync(
            "acme",
            "test_tool",
            _ => new CallToolResult { IsError = false },
            (_, _) =>
            {
                operationCalled = true;
                return Task.FromResult(new CallToolResult());
            },
            TestContext.Current.CancellationToken);

        operationCalled.ShouldBeFalse();
        AssertIsErrorWithCode(result, McpErrorMapper.InternalErrorCode);
        result.StructuredContent!.Value.GetProperty("message").GetString().ShouldBe(McpErrorMapper.SanitizedFailureMessage);
    }

    [Fact]
    public async Task RunAsync_PassesExactApprovedTenantSnapshotToOperation()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor("tenant-a", "tenant-b");
        string? capturedTenant = null;
        int operationCalls = 0;

        CallToolResult result = await executor.RunAsync(
            "tenant-b",
            "test_tool",
            _ => null,
            (approvedTenant, _) =>
            {
                operationCalls++;
                capturedTenant = approvedTenant;
                return Task.FromResult(new CallToolResult());
            },
            TestContext.Current.CancellationToken);

        result.IsError.ShouldNotBe(true);
        operationCalls.ShouldBe(1);
        capturedTenant.ShouldBe("tenant-b");
    }

    [Fact]
    public async Task RunAsync_DenialPreventsOperation()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor("tenant-a");
        bool operationCalled = false;

        CallToolResult result = await executor.RunAsync(
            "tenant-b",
            "test_tool",
            _ => null,
            (_, _) =>
            {
                operationCalled = true;
                return Task.FromResult(new CallToolResult());
            },
            TestContext.Current.CancellationToken);

        operationCalled.ShouldBeFalse();
        AssertIsErrorWithCode(result, "TENANT_FORBIDDEN");
    }

    [Fact]
    public async Task RunAsync_RemoteFailureUsesStructuredRemoteMapping()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor();

        CallToolResult result = await executor.RunAsync(
            "acme",
            "test_tool",
            _ => null,
            (_, _) => throw new MemoriesRemoteException(
                HttpStatusCode.NotFound,
                new ErrorResponse("REMOTE_CODE", "remote message", "remote suggestion")),
            TestContext.Current.CancellationToken);

        AssertIsErrorWithCode(result, "REMOTE_CODE");
        result.StructuredContent!.Value.GetProperty("message").GetString().ShouldBe("remote message");
    }

    [Fact]
    public async Task RunAsync_UnexpectedFailureUsesSanitizedGenericMapping()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor();
        const string sensitiveDetail = "secret caller input";

        CallToolResult result = await executor.RunAsync(
            "acme",
            "test_tool",
            _ => null,
            (_, _) => throw new InvalidOperationException(sensitiveDetail),
            TestContext.Current.CancellationToken);

        AssertIsErrorWithCode(result, McpErrorMapper.InternalErrorCode);
        result.Content[0].ShouldBeOfType<TextContentBlock>().Text.ShouldNotContain(sensitiveDetail);
        result.StructuredContent!.Value.GetProperty("message").GetString().ShouldBe(McpErrorMapper.SanitizedFailureMessage);
    }

    [Fact]
    public async Task RunAsync_ForwardsCallerCancellationTokenToOperation()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor();
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken capturedToken = default;

        _ = await executor.RunAsync(
            "acme",
            "test_tool",
            _ => null,
            (_, operationToken) =>
            {
                capturedToken = operationToken;
                return Task.FromResult(new CallToolResult());
            },
            cancellationSource.Token);

        capturedToken.ShouldBe(cancellationSource.Token);
    }

    [Fact]
    public async Task RunAsync_OperationCancellationPropagatesUnchanged()
    {
        McpToolExecutor executor = McpToolTestFactory.CreateExecutor();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        OperationCanceledException actual = await Should.ThrowAsync<OperationCanceledException>(
            () => executor.RunAsync(
                "acme",
                "test_tool",
                _ => null,
                (_, _) => Task.FromCanceled<CallToolResult>(cancellationSource.Token),
                CancellationToken.None));

        actual.CancellationToken.ShouldBe(cancellationSource.Token);
    }

    private static void AssertIsErrorWithCode(CallToolResult result, string expectedCode)
    {
        result.IsError.ShouldBe(true);
        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(expectedCode);
    }
}
