// <copyright file="BenchmarkQuery.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Models;

/// <summary>A benchmark query with expected ground truth results.</summary>
public sealed record BenchmarkQuery(
    string QueryId,
    string Query,
    string Description,
    IReadOnlyList<string> ExpectedResults,
    string? GraphStartNodeId,
    IReadOnlyList<string> RequiredAxes);
