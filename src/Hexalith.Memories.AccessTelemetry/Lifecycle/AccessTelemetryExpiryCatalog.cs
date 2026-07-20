// <copyright file="AccessTelemetryExpiryCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Text.Json.Serialization;

/// <summary>Bounded catalog of active expiry minutes for deterministic bucket traversal.</summary>
internal sealed record AccessTelemetryExpiryCatalog(
    [property: JsonPropertyName("activeMinutes")] IReadOnlyList<long> ActiveMinutes);
