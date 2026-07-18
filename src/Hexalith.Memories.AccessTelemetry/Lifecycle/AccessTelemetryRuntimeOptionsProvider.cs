// <copyright file="AccessTelemetryRuntimeOptionsProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Atomically publishes the latest Dapr-resolved, validated lifecycle options.</summary>
internal sealed class AccessTelemetryRuntimeOptionsProvider
{
    private AccessTelemetryOptions _current;
    private int _isReady;

    /// <summary>Initializes the provider with fail-closed configured values.</summary>
    public AccessTelemetryRuntimeOptionsProvider(AccessTelemetryOptions configured)
    {
        _current = configured ?? throw new ArgumentNullException(nameof(configured));
    }

    /// <summary>Gets the latest options snapshot.</summary>
    public AccessTelemetryOptions Current => Volatile.Read(ref _current);

    /// <summary>Gets whether authoritative configuration has been resolved and validated.</summary>
    public bool IsReady => Volatile.Read(ref _isReady) != 0;

    /// <summary>Publishes one authoritative options snapshot.</summary>
    public void Publish(AccessTelemetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Volatile.Write(ref _current, options);
        Volatile.Write(ref _isReady, 1);
    }

    /// <summary>Closes the provider while retaining the last snapshot for diagnostics.</summary>
    public void FailClosed() => Volatile.Write(ref _isReady, 0);
}
