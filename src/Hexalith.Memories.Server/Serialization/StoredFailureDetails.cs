// <copyright file="StoredFailureDetails.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

/// <summary>Durable failure details for a failed ingestion unit.</summary>
internal sealed record StoredFailureDetails(
    string Stage,
    string ErrorCode,
    int RetryCount,
    string? ErrorMessage = null,
    DateTimeOffset? LastRetryAt = null);
