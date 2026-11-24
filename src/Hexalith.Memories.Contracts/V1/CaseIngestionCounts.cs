// <copyright file="CaseIngestionCounts.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

/// <summary>Per-stage in-flight counts for a case (Story 6.3 FR10). Indexed and Failed counts are sourced
/// elsewhere (FalkorDB / activity stream) and are NOT carried by this record.</summary>
[DataContract]
public sealed record CaseIngestionCounts(
    [property: DataMember(Name = "Queued"), JsonPropertyName("queued")] int Queued,
    [property: DataMember(Name = "Extracting"), JsonPropertyName("extracting")] int Extracting,
    [property: DataMember(Name = "Embedding"), JsonPropertyName("embedding")] int Embedding,
    [property: DataMember(Name = "Indexing"), JsonPropertyName("indexing")] int Indexing);
