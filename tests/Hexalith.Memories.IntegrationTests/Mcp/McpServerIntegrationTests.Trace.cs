// <copyright file="McpServerIntegrationTests.Trace.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Mcp;

using System.Diagnostics;

using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;
using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.Telemetry;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Shouldly;

/// <summary>Verifies trace propagation from the MCP tool to the upstream Memories Server.</summary>
public sealed partial class McpServerIntegrationTests
{
    /// <summary>Proves the parent spans of one authenticated search tool request across processes.</summary>
    [Fact]
    public async Task TraceHop_McpToServer_PreservesTraceparent()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        await using McpClient client = await CreateMcpClientAsync(tenantId);

        var traceId = ActivityTraceId.CreateRandom();
        _fixture.DaprTraces.SelectTrace(traceId.ToString());
        try
        {
            await AssertMcpSearchTraceAsync(client, tenantId, traceId);
        }
        finally
        {
            _fixture.DaprTraces.SelectTrace(null);
        }
    }

    private async Task AssertMcpSearchTraceAsync(McpClient client, string tenantId, ActivityTraceId traceId)
    {
        string? clientToolSpanId = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Extensions.IntegrationActivityProcessor.McpSourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => options.Parent.TraceId == traceId
                ? ActivitySamplingResult.AllDataAndRecorded
                : ActivitySamplingResult.None,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == traceId
                    && activity.Kind == ActivityKind.Client
                    && activity.OperationName == Extensions.IntegrationActivityProcessor.McpSearchToolOperationName)
                {
                    clientToolSpanId = activity.SpanId.ToString();
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        int logStartIndex = _fixture.LogEntryCount;
        CallToolResult result;
        using (new Activity("integration.mcp.search")
            .SetIdFormat(ActivityIdFormat.W3C)
            .SetParentId(traceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded)
            .Start())
        {
            result = await client.CallToolAsync(
                "search_memory",
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["tenantId"] = tenantId,
                    ["query"] = "mcp-trace-probe",
                    ["axes"] = "syntactic",
                },
                cancellationToken: cancellationToken);
        }

        result.IsError.ShouldNotBe(true);
        result.Content.ShouldNotBeEmpty();
        clientToolSpanId.ShouldNotBeNullOrWhiteSpace();

        string expectedTraceId = traceId.ToString();
        TimeSpan timeout = AuditEventStreamReader.ResolveTimeout();
        IReadOnlyList<CapturedServerActivity> mcpActivities = await ServerActivityStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 2,
            timeout,
            cancellationToken,
            activity => activity.TraceId == expectedTraceId && (IsMcpSearchTool(activity) || IsMcpHttpClient(activity)),
            resourceName: "memories-mcp");
        IReadOnlyList<CapturedServerActivity> serverActivities = await ServerActivityStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 2,
            timeout,
            cancellationToken,
            activity => activity.TraceId == expectedTraceId && (IsServerHttp(activity) || IsServerSearch(activity)));

        CapturedServerActivity mcpTool = SelectTraceSpan(mcpActivities, expectedTraceId, IsMcpSearchTool, "MCP search tool");
        CapturedServerActivity mcpHttp = SelectTraceSpan(mcpActivities, expectedTraceId, IsMcpHttpClient, "MCP outbound HTTP");
        CapturedServerActivity serverHttp = SelectTraceSpan(serverActivities, expectedTraceId, IsServerHttp, "Memories Server inbound HTTP");
        CapturedServerActivity serverSearch = SelectTraceSpan(serverActivities, expectedTraceId, IsServerSearch, "Memories Server search");
        mcpTool.ParentSpanId.ShouldBe(clientToolSpanId);
        mcpHttp.ParentSpanId.ShouldBe(mcpTool.SpanId);
        serverSearch.ParentSpanId.ShouldBe(serverHttp.SpanId);
        (string mcpSidecarSpanId, string serverSidecarSpanId) = await _fixture.DaprTraces.ReadParentChainAsync(
            serverHttp.ParentSpanId,
            mcpHttp.SpanId,
            timeout,
            cancellationToken);

        _output.WriteLine($"Trace {expectedTraceId} parent-to-child chain\n"
            + $"client tool {clientToolSpanId} -> MCP tool {mcpTool.SpanId} -> MCP HTTP {mcpHttp.SpanId} -> "
            + $"MCP sidecar {mcpSidecarSpanId} -> Server sidecar {serverSidecarSpanId} -> "
            + $"Server HTTP {serverHttp.SpanId} -> Server search {serverSearch.SpanId}.");
    }

    private static CapturedServerActivity SelectTraceSpan(
        IReadOnlyList<CapturedServerActivity> activities,
        string traceId,
        Func<CapturedServerActivity, bool> matchesSpan,
        string description)
    {
        CapturedServerActivity[] matchingSpans = activities.Where(activity => activity.TraceId == traceId && matchesSpan(activity)).ToArray();
        matchingSpans.Length.ShouldBe(1, $"Expected one {description} span in trace {traceId}.");
        return matchingSpans[0];
    }

    private static bool IsMcpSearchTool(CapturedServerActivity activity)
        => activity.Kind == nameof(ActivityKind.Server)
            && activity.SourceName == Extensions.IntegrationActivityProcessor.McpSourceName
            && activity.OperationName == Extensions.IntegrationActivityProcessor.McpSearchToolOperationName;

    private static bool IsMcpHttpClient(CapturedServerActivity activity)
        => activity.Kind == nameof(ActivityKind.Client)
            && activity.SourceName == Extensions.IntegrationActivityProcessor.HttpClientSourceName;

    private static bool IsServerHttp(CapturedServerActivity activity)
        => activity.Kind == nameof(ActivityKind.Server)
            && activity.SourceName == Extensions.IntegrationActivityProcessor.AspNetCoreSourceName;

    private static bool IsServerSearch(CapturedServerActivity activity)
        => activity.SourceName == MemoriesActivitySource.SourceName && activity.OperationName == MemoriesActivitySource.SearchRequest;
}
