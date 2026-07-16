// <copyright file="BatchStatusCounts.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Aggregated status counts across all workflow instances in a directory ingestion batch.</summary>
public sealed record BatchStatusCounts(
    int Queued,
    int Extracting,
    int Embedding,
    int Indexing,
    int Indexed,
    int Failed);
