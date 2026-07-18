// <copyright file="AccessTelemetryCapabilityProbeResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Bounded result of one behavioral capability probe.</summary>
internal sealed record AccessTelemetryCapabilityProbeResult(string Capability, bool Passed);
