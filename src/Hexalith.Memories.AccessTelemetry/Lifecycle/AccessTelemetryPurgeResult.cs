// <copyright file="AccessTelemetryPurgeResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Bounded result of one purge actor turn.</summary>
internal sealed record AccessTelemetryPurgeResult(
    int Processed,
    int Purged,
    int VerifiedAbsent,
    bool HasMore,
    long? LastExpiryMinute = null,
    int? LastExpiryShard = null);
