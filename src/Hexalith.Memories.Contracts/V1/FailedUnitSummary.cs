// <copyright file="FailedUnitSummary.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Public projection of a persisted failed memory unit (Story 6.3 FR11).</summary>
public sealed record FailedUnitSummary(
    string MemoryUnitId,
    string CaseId,
    string SourceUri,
    SourceType SourceType,
    string Stage,
    string ErrorCode,
    string? ErrorMessage,
    int RetryCount,
    DateTimeOffset? LastRetryAt,
    DateTimeOffset FailedAt);
