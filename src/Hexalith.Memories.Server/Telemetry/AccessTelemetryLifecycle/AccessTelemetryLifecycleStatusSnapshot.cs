// <copyright file="AccessTelemetryLifecycleStatusSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded lifecycle-only health state.</summary>
internal sealed record AccessTelemetryLifecycleStatusSnapshot(
    AccessTelemetryHealthState Health,
    AccessTelemetryReason Reason);
