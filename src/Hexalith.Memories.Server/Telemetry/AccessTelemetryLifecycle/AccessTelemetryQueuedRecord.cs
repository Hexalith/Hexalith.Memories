// <copyright file="AccessTelemetryQueuedRecord.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Queue entry with precomputed canonical byte accounting.</summary>
internal sealed record AccessTelemetryQueuedRecord(AccessTelemetryRecord Record, int CanonicalBytes);
