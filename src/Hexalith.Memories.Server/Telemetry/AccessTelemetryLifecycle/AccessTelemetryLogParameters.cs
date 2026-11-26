// <copyright file="AccessTelemetryLogParameters.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Collections.ObjectModel;

/// <summary>Verified request parameters shared by the external and lifecycle logger paths.</summary>
internal sealed class AccessTelemetryLogParameters(
    IReadOnlyDictionary<string, object?> values,
    string? qualificationIdentity,
    bool isValid) : ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(values, StringComparer.Ordinal))
{
    /// <summary>Gets the bounded synthetic identity used for lifecycle qualification accounting.</summary>
    internal string? QualificationIdentity { get; } = qualificationIdentity;

    /// <summary>Gets whether the original parameters qualified for lifecycle persistence.</summary>
    internal bool IsValid { get; } = isValid;
}
