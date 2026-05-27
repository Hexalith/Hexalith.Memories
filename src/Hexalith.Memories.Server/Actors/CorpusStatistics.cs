// <copyright file="CorpusStatistics.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using System.Runtime.Serialization;

/// <summary>
/// Per-tenant corpus statistics cached by <see cref="CorpusStatisticsActor"/>.
/// Used as DAPR actor internal state — not a public API contract.
/// </summary>
/// <param name="DocumentCount">The number of documents in the tenant's RediSearch index.</param>
/// <param name="AverageDocumentLength">The average document length in bytes (computed from <c>DocTableSizeMB / NumDocs</c>).</param>
/// <param name="LastRefreshedAt">The timestamp when statistics were last refreshed from RediSearch.</param>
[DataContract]
public sealed record CorpusStatistics(
    [property: DataMember] int DocumentCount,
    [property: DataMember] double AverageDocumentLength,
    [property: DataMember] DateTimeOffset LastRefreshedAt);
