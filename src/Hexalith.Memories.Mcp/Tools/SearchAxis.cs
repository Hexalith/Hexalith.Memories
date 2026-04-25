// <copyright file="SearchAxis.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tools;

using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;

/// <summary>The four search axes accepted by the <c>search_memory</c> MCP tool.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<SearchAxis>))]
internal enum SearchAxis
{
    /// <summary>BM25 lexical search via RediSearch.</summary>
    Syntactic,

    /// <summary>Vector / embedding similarity search.</summary>
    Semantic,

    /// <summary>Fused multi-axis hybrid search.</summary>
    Hybrid,
}
