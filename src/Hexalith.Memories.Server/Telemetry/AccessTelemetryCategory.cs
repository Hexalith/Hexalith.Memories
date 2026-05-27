// <copyright file="AccessTelemetryCategory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

/// <summary>
/// Marker type used as the generic parameter for <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}"/>
/// on audit-event emitters so operators can route the dedicated category
/// <c>Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory</c> to a separate log sink / filter rule —
/// keeping search queries (which may carry privacy-sensitive terms on regulated tenants) out of general
/// operational log streams. Documented in <c>docs/dev/telemetry.md</c> (Rev 0.3 — Red Team finding).
/// </summary>
internal sealed class AccessTelemetryCategory
{
}
