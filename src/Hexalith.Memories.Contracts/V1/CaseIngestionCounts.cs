// <copyright file="CaseIngestionCounts.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Runtime.Serialization;

/// <summary>Per-stage in-flight counts for a case (Story 6.3 FR10). Indexed and Failed counts are sourced
/// elsewhere (FalkorDB / activity stream) and are NOT carried by this record.</summary>
[DataContract]
public sealed record CaseIngestionCounts(
    [property: DataMember] int Queued,
    [property: DataMember] int Extracting,
    [property: DataMember] int Embedding,
    [property: DataMember] int Indexing);
