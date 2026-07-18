// <copyright file="AccessTelemetryRuntimeValidationResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded exact-profile writer validation result.</summary>
public sealed record AccessTelemetryRuntimeValidationResponse(
    bool AllowsWrites,
    AccessTelemetryReason Reason);
