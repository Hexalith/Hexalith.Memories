// <copyright file="AccessTelemetryPersistenceResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Result of one serialized persistence attempt.</summary>
internal sealed record AccessTelemetryPersistenceResult(
    AccessTelemetryPersistenceStatus Status,
    AccessTelemetryReason Reason,
    int TtlInSeconds = 0);
