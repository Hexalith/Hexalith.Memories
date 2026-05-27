// <copyright file="TraversalResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>The result of a causal chain traversal from a starting memory unit.</summary>
public sealed record TraversalResult
{
    /// <summary>Initializes a new instance of the <see cref="TraversalResult"/> class.</summary>
    /// <param name="StartNodeId">The starting memory unit identifier.</param>
    /// <param name="Depth">The requested traversal depth.</param>
    /// <param name="Nodes">The returned traversal nodes.</param>
    /// <param name="TotalNodeCount">The total nodes found before truncation.</param>
    [JsonConstructor]
    public TraversalResult(
        string StartNodeId,
        int Depth,
        IReadOnlyList<TraversalNode> Nodes,
        int TotalNodeCount)
    {
        this.StartNodeId = StartNodeId;
        this.Depth = Depth;
        this.Nodes = Nodes;
        this.TotalNodeCount = TotalNodeCount;
        TotalCount = TotalNodeCount;
    }

    /// <summary>Gets the starting memory unit identifier.</summary>
    public string StartNodeId { get; init; }

    /// <summary>Gets the requested traversal depth.</summary>
    public int Depth { get; init; }

    /// <summary>Gets the returned traversal nodes.</summary>
    public IReadOnlyList<TraversalNode> Nodes { get; init; }

    /// <summary>Gets the total nodes found before truncation.</summary>
    public int TotalNodeCount { get; init; }

    /// <summary>Gets the total count of traversal nodes before truncation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int TotalCount { get; init; }

    /// <summary>Gets the gap markers for missing nodes detected during traversal (FR49).</summary>
    public IReadOnlyList<TraversalGapMarker> GapMarkers { get; init; } = [];

    /// <summary>Gets the count of nodes omitted due to response truncation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OmittedCount { get; init; }

    /// <summary>Gets the estimated token count before truncation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long EstimatedTokensTotal { get; init; }

    /// <summary>Gets the reason nodes were omitted from the response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OmittedReason OmittedReason { get; init; }

    /// <summary>Gets a value indicating whether any expected backend was unavailable during traversal.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Degraded { get; init; }

    /// <summary>Gets the unavailable backend names.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? UnavailableAxes { get; init; }

    /// <summary>Gets a value indicating whether the primary causal path survived truncation.</summary>
    [JsonIgnore]
    public bool PrimaryPathIntact { get; init; } = true;

    /// <summary>Gets the wire value for broken primary paths.</summary>
    [JsonInclude]
    [JsonPropertyName("primaryPathIntact")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal bool? PrimaryPathIntactJson
    {
        get => PrimaryPathIntact ? null : false;
        init => PrimaryPathIntact = value ?? true;
    }
}
