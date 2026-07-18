// <copyright file="AccessTelemetrySanitizerAccessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>Atomically exposes sanitizer key material only after Dapr-secret bootstrap succeeds.</summary>
internal sealed class AccessTelemetrySanitizerAccessor
{
    private AccessTelemetrySanitizer? _current;

    /// <summary>Gets the active sanitizer or null while lifecycle writes are fail-closed.</summary>
    public AccessTelemetrySanitizer? Current => Volatile.Read(ref _current);

    /// <summary>Publishes a sanitizer built from validated retention and secret material.</summary>
    public void Publish(AccessTelemetrySanitizer sanitizer)
    {
        ArgumentNullException.ThrowIfNull(sanitizer);
        Volatile.Write(ref _current, sanitizer);
    }

    /// <summary>Removes key material immediately when a terminal validation change closes writes.</summary>
    public void Clear() => Volatile.Write(ref _current, null);
}
