// <copyright file="AccessTelemetryRuntimeValidationRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Exact-profile writer validation request.</summary>
public sealed record AccessTelemetryRuntimeValidationRequest(
    string ConfigurationEpoch,
    string ComponentProfileHash);
