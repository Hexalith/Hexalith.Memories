// <copyright file="BulkReIngestionResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Aggregated outcome of a bulk re-ingestion request (Story 6.3 FR12).</summary>
public sealed record BulkReIngestionResponse(
    int Scheduled,
    int NotFound,
    int Conflicted,
    int Errored,
    IReadOnlyList<ReIngestedUnitInfo> Units);
