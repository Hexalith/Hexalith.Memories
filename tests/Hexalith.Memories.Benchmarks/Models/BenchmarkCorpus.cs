// <copyright file="BenchmarkCorpus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Models;

using Hexalith.Memories.Contracts.V1;

/// <summary>Deserialized benchmark corpus containing memory units and graph edges.</summary>
public sealed record BenchmarkCorpus(
    IReadOnlyList<BenchmarkMemoryUnit> MemoryUnits,
    IReadOnlyList<BenchmarkEdge> Edges);

/// <summary>A single memory unit in the benchmark corpus, including pre-computed embedding vector.</summary>
public sealed record BenchmarkMemoryUnit(
    string Id,
    string Content,
    string SourceUri,
    SourceType SourceType,
    string TenantId,
    string CaseId,
    float[] Vector);

/// <summary>A graph edge between two nodes in the benchmark corpus.</summary>
public sealed record BenchmarkEdge(
    string SourceId,
    string TargetId,
    EdgeType EdgeType,
    float Confidence,
    EdgeOrigin Origin);
