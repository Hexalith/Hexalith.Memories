// <copyright file="ClockAttestationValidationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded trusted-clock verification result.</summary>
public sealed record ClockAttestationValidationResult(
    bool IsValid,
    AccessTelemetryReason Reason,
    long? TrustedUnixMilliseconds = null);
