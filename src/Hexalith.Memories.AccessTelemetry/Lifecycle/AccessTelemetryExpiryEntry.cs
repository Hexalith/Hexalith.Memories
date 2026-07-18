// <copyright file="AccessTelemetryExpiryEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Portable minute/shard expiry-index entry.</summary>
internal sealed record AccessTelemetryExpiryEntry(
    string RecordId,
    long ExpiryMinute,
    int Shard,
    string EnvelopeHash,
    string ExpiresAtUtc);
