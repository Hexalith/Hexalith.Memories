// <copyright file="EvidencePacketCanonicalFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.TestHelpers.EvidencePackets;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Single cross-surface source of truth for Evidence Packet parity tests (Story 2.7 / CR1). The same
/// canonical lower-level inputs flow through contract, CLI, MCP, and server tests, and every surface is
/// asserted against the JSON the shared <see cref="EvidencePacketMapper"/> produces from those inputs.
/// This proves no surface invents its own packet mapping or drops contract fields.
/// </summary>
public static class EvidencePacketCanonicalFixtures
{
    /// <summary>Canonical authorized tenant/case scope used by the success-path fixtures.</summary>
    public static EvidencePacketScope AuthorizedScope { get; } =
        new("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case");

    /// <summary>Canonical authorized tenant-wide (no case) scope.</summary>
    public static EvidencePacketScope TenantWideScope { get; } =
        new("tenant-a", null, EvidencePacketIsolationStatus.Authorized, "tenant");

    /// <summary>Canonical unauthorized scope for cross-scope/forbidden fixtures.</summary>
    public static EvidencePacketScope UnauthorizedScope { get; } =
        new("tenant-b", "case-b", EvidencePacketIsolationStatus.Unauthorized, "tenant-case");

    /// <summary>Canonical explain metadata shared by the success-path fixtures.</summary>
    public static SearchExplanation Explanation { get; } = new()
    {
        Caveat = "Scores measure query-result relevance, not factual accuracy or data completeness.",
        AxisDetails = new Dictionary<string, AxisExplanation>
        {
            ["semantic"] = new() { NormalizationMethod = "cosine", Description = "cosine similarity" },
            ["syntactic"] = new() { NormalizationMethod = "bm25_saturation", Description = "BM25 saturation" },
        },
    };

    /// <summary>Builds the canonical complete hybrid search result (one strong source, no degradation).</summary>
    /// <returns>The canonical hybrid result.</returns>
    public static HybridSearchResult HybridComplete() => new()
    {
        Results =
        [
            new FusedScoredResult
            {
                MemoryUnitId = "mu-001",
                CompositeScore = 0.91,
                ContentSnippet = "The claim was denied due to a coverage gap.",
                SourceUri = "mem://tenant-a/case-a/mu-001",
                SourceType = SourceType.File,
                SyntacticScore = 0.62,
                SemanticScore = 0.91,
                GraphScore = null,
                CaseId = "case-a",
                CaseName = "Case A",
                AnnotationsCount = 2,
            },
        ],
        TotalCount = 1,
        Degraded = false,
        UnavailableAxes = [],
        Query = "claim denied",
        Explanation = Explanation,
        AxesUsed = ["semantic", "syntactic"],
    };

    /// <summary>Builds the canonical complete single-axis search result.</summary>
    /// <returns>The canonical single-axis result.</returns>
    public static SearchResult SingleComplete() => new()
    {
        Results =
        [
            new ScoredResult
            {
                MemoryUnitId = "mu-001",
                Score = 0.91,
                ContentSnippet = "The claim was denied due to a coverage gap.",
                SourceUri = "mem://tenant-a/case-a/mu-001",
                SourceType = SourceType.File,
                Axis = "semantic",
                CaseId = "case-a",
                CaseName = "Case A",
                AnnotationsCount = 2,
            },
        ],
        TotalCount = 1,
        HasIndexedMemoryUnits = true,
        Query = "claim denied",
        Explanation = Explanation,
        AxesUsed = ["semantic"],
    };

    /// <summary>Builds the canonical degraded hybrid result (graph axis unavailable).</summary>
    /// <returns>The canonical degraded hybrid result.</returns>
    public static HybridSearchResult HybridDegraded() => HybridComplete() with
    {
        Degraded = true,
        UnavailableAxes = ["graph"],
        OmittedReason = OmittedReason.BackendDegraded,
    };

    /// <summary>Builds the canonical token-budget-compressed single-axis result.</summary>
    /// <returns>The canonical token-budget result.</returns>
    public static SearchResult SingleTokenBudget() => SingleComplete() with
    {
        TotalCount = 4,
        OmittedCount = 3,
        EstimatedTokensTotal = 1_024,
        OmittedReason = OmittedReason.TokenBudget,
    };

    /// <summary>Builds the canonical empty single-axis result for an authorized scope.</summary>
    /// <returns>The canonical empty result.</returns>
    public static SearchResult SingleEmpty() => new()
    {
        Results = [],
        TotalCount = 0,
        HasIndexedMemoryUnits = true,
        Query = "claim denied",
        AxesUsed = ["semantic"],
    };

    /// <summary>Builds the canonical forbidden error response (sanitization fixtures embed sensitive text).</summary>
    /// <returns>The canonical forbidden error.</returns>
    public static ErrorResponse ForbiddenError() => new(
        "TENANT_FORBIDDEN",
        "Denied for tenant-b at C:\\secret\\trace.txt with Bearer abc123def456ghi789jkl012mno345pqr678.",
        "Use tenant-b or reconnect to redis://backend-key/0.");

    /// <summary>Maps the canonical hybrid complete input through the shared mapper.</summary>
    /// <returns>The canonical hybrid complete packet.</returns>
    public static EvidencePacket HybridCompletePacket()
        => EvidencePacketMapper.FromHybridSearchResult(HybridComplete(), AuthorizedScope);

    /// <summary>Maps the canonical single complete input through the shared mapper.</summary>
    /// <returns>The canonical single complete packet.</returns>
    public static EvidencePacket SingleCompletePacket()
        => EvidencePacketMapper.FromSearchResult(SingleComplete(), AuthorizedScope);

    /// <summary>Maps the canonical degraded hybrid input through the shared mapper.</summary>
    /// <returns>The canonical degraded packet.</returns>
    public static EvidencePacket HybridDegradedPacket()
        => EvidencePacketMapper.FromHybridSearchResult(HybridDegraded(), AuthorizedScope);

    /// <summary>Maps the canonical token-budget input through the shared mapper.</summary>
    /// <returns>The canonical token-budget packet.</returns>
    public static EvidencePacket SingleTokenBudgetPacket()
        => EvidencePacketMapper.FromSearchResult(SingleTokenBudget(), AuthorizedScope);

    /// <summary>Maps the canonical empty input through the shared mapper.</summary>
    /// <returns>The canonical empty packet.</returns>
    public static EvidencePacket SingleEmptyPacket()
        => EvidencePacketMapper.FromSearchResult(SingleEmpty(), AuthorizedScope);

    /// <summary>
    /// Normalizes any Evidence Packet projection (record or raw JSON) into a canonical JSON string by
    /// round-tripping through <see cref="MemoriesJsonContext.Options"/>. Both sides of a parity assertion
    /// pass through this method so member ordering and formatting are identical regardless of which
    /// surface serialized first.
    /// </summary>
    /// <param name="packet">The Evidence Packet to normalize.</param>
    /// <returns>The canonical JSON string.</returns>
    public static string Canonicalize(EvidencePacket packet)
        => JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);

    /// <summary>Normalizes a raw Evidence Packet JSON fragment emitted by a surface into canonical JSON.</summary>
    /// <param name="packetJson">The raw <c>evidencePacket</c> JSON a surface emitted.</param>
    /// <returns>The canonical JSON string.</returns>
    public static string Canonicalize(string packetJson)
    {
        EvidencePacket? packet = JsonSerializer.Deserialize<EvidencePacket>(packetJson, MemoriesJsonContext.Options);
        return packet is null
            ? throw new JsonException("Evidence Packet JSON deserialized to null.")
            : Canonicalize(packet);
    }

    /// <summary>Normalizes the <c>evidencePacket</c> node within a surface JSON document, if present.</summary>
    /// <param name="surfaceJson">The full surface JSON (e.g., a CLI envelope or MCP structured content).</param>
    /// <returns>The canonical JSON string for the embedded packet.</returns>
    public static string CanonicalizeEmbedded(string surfaceJson)
    {
        using var document = JsonDocument.Parse(surfaceJson);
        JsonElement packetElement = FindEvidencePacket(document.RootElement)
            ?? throw new JsonException("No evidencePacket node found in surface JSON.");
        return Canonicalize(packetElement.GetRawText());
    }

    private static JsonElement? FindEvidencePacket(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (property.NameEquals("evidencePacket"))
                    {
                        return property.Value;
                    }

                    JsonElement? nested = FindEvidencePacket(property.Value);
                    if (nested is not null)
                    {
                        return nested;
                    }
                }

                return null;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    JsonElement? nested = FindEvidencePacket(item);
                    if (nested is not null)
                    {
                        return nested;
                    }
                }

                return null;
            default:
                return null;
        }
    }
}
