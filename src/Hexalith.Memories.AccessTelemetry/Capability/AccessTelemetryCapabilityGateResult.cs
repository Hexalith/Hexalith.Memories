// <copyright file="AccessTelemetryCapabilityGateResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Fail-closed exact-profile capability decision.</summary>
internal sealed record AccessTelemetryCapabilityGateResult(
    bool AllowsWrites,
    bool BusinessReadinessAvailable,
    AccessTelemetryHealthState Health,
    AccessTelemetryReason Reason,
    DateTimeOffset? ValidUntilUtc = null);
