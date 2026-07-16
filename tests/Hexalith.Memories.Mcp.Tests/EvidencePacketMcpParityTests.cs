// <copyright file="EvidencePacketMcpParityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Mcp.Tools;
using Hexalith.Memories.TestHelpers.EvidencePackets;

using ModelContextProtocol.Protocol;

using Shouldly;

/// <summary>
/// Cross-surface parity and determinism for the MCP <c>search_memory</c> packet projection
/// (Story 2.7 / CR1, CR18, CR26). Proves the MCP tool emits exactly the shared canonical packet in both
/// <see cref="CallToolResult.StructuredContent"/> and the text-content fallback, and that per-axis
/// evidence ordering is deterministic.
/// </summary>
public sealed class EvidencePacketMcpParityTests
{
    [Fact]
    public async Task HybridResult_StructuredAndTextFallback_ShouldMatchSharedCanonicalPacket()
    {
        var stub = new StubMemoriesClient
        {
            OnHybridSearch = (_, _) => Task.FromResult(EvidencePacketCanonicalFixtures.HybridComplete()),
        };
        SearchMemoryTool tool = CreateTool(stub, tenantId: "tenant-a");

        CallToolResult result = await tool.SearchAsync(
            tenantId: "tenant-a",
            query: "claim denied",
            @case: "case-a",
            axes: SearchAxis.Hybrid,
            cancellationToken: TestContext.Current.CancellationToken);

        string canonical = EvidencePacketCanonicalFixtures.Canonicalize(
            EvidencePacketCanonicalFixtures.HybridCompletePacket());

        // StructuredContent packet == shared canonical JSON.
        string structuredPacket = result.StructuredContent!.Value.GetProperty("evidencePacket").GetRawText();
        EvidencePacketCanonicalFixtures.Canonicalize(structuredPacket).ShouldBe(canonical);

        // Text fallback carries the SAME packet (CR26 — structural, not a brittle substring match).
        EvidencePacketCanonicalFixtures.CanonicalizeEmbedded(ExtractText(result)).ShouldBe(canonical);
    }

    [Fact]
    public async Task SingleAxisResult_StructuredContent_ShouldMatchSharedCanonicalPacket()
    {
        var stub = new StubMemoriesClient
        {
            OnSearch = (_, _) => Task.FromResult(EvidencePacketCanonicalFixtures.SingleComplete()),
        };
        SearchMemoryTool tool = CreateTool(stub, tenantId: "tenant-a");

        CallToolResult result = await tool.SearchAsync(
            tenantId: "tenant-a",
            query: "claim denied",
            @case: "case-a",
            axes: SearchAxis.Semantic,
            cancellationToken: TestContext.Current.CancellationToken);

        string canonical = EvidencePacketCanonicalFixtures.Canonicalize(
            EvidencePacketCanonicalFixtures.SingleCompletePacket());
        string structuredPacket = result.StructuredContent!.Value.GetProperty("evidencePacket").GetRawText();
        EvidencePacketCanonicalFixtures.Canonicalize(structuredPacket).ShouldBe(canonical);
    }

    [Fact]
    public async Task HybridResult_AxisEvidence_ShouldBeDeterministicallyOrdered()
    {
        // Two independent requests (fresh tool + auth context each, since authorization is request-scoped)
        // must yield the same per-axis ordering regardless of the scrambled AxesUsed input order.
        string[] first = await InvokeAndReadAxisOrderAsync();
        string[] second = await InvokeAndReadAxisOrderAsync();

        first.ShouldBe(["graph", "semantic", "syntactic"]);
        second.ShouldBe(first);
    }

    private static async Task<string[]> InvokeAndReadAxisOrderAsync()
    {
        var stub = new StubMemoriesClient
        {
            OnHybridSearch = (request, _) => Task.FromResult(new HybridSearchResult
            {
                Results =
                [
                    new FusedScoredResult
                    {
                        MemoryUnitId = "mu-001",
                        CompositeScore = 0.80,
                        ContentSnippet = "Multi-axis evidence",
                        SourceUri = "mem://tenant-a/case-a/mu-001",
                        SourceType = SourceType.File,
                        SyntacticScore = 0.51,
                        SemanticScore = 0.80,
                        GraphScore = 0.33,
                        CaseId = request.CaseId,
                        CaseName = "Case A",
                    },
                ],
                TotalCount = 1,
                Degraded = false,
                UnavailableAxes = [],
                Query = request.Query,
                AxesUsed = ["semantic", "graph", "syntactic"],
            }),
        };
        SearchMemoryTool tool = CreateTool(stub, tenantId: "tenant-a");

        CallToolResult result = await tool.SearchAsync(
            tenantId: "tenant-a",
            query: "claim denied",
            @case: "case-a",
            axes: SearchAxis.Hybrid,
            cancellationToken: TestContext.Current.CancellationToken);

        return result.StructuredContent!.Value
            .GetProperty("evidencePacket")
            .GetProperty("evidence")
            .GetProperty("axisEvidence")
            .EnumerateArray()
            .Select(axis => axis.GetProperty("axis").GetString()!)
            .ToArray();
    }

    private static string ExtractText(CallToolResult result)
    {
        var block = result.Content[0] as TextContentBlock;
        block.ShouldNotBeNull();
        return block!.Text;
    }

    private static SearchMemoryTool CreateTool(StubMemoriesClient stub, string tenantId)
        => new(stub, McpToolTestFactory.CreateExecutor(tenantId));
}
