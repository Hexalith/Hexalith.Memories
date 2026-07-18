// <copyright file="AccessTelemetryLifecycleLoggerProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Microsoft.Extensions.Logging;

/// <summary>Isolated lifecycle logger provider for the exact access-telemetry category.</summary>
[ProviderAlias("AccessTelemetryLifecycle")]
internal sealed class AccessTelemetryLifecycleLoggerProvider : ILoggerProvider
{
    private readonly BoundedAccessTelemetryQueue _queue;
    private readonly AccessTelemetrySanitizerAccessor _sanitizerAccessor;

    /// <summary>Initializes the lifecycle provider.</summary>
    public AccessTelemetryLifecycleLoggerProvider(BoundedAccessTelemetryQueue queue, AccessTelemetrySanitizer sanitizer)
        : this(queue, CreateAccessor(sanitizer))
    {
    }

    /// <summary>Initializes the lifecycle provider with fail-closed secret bootstrap.</summary>
    public AccessTelemetryLifecycleLoggerProvider(
        BoundedAccessTelemetryQueue queue,
        AccessTelemetrySanitizerAccessor sanitizerAccessor)
    {
        _queue = queue;
        _sanitizerAccessor = sanitizerAccessor;
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
        => new AccessTelemetryLifecycleLogger(categoryName, _queue, _sanitizerAccessor);

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    private static AccessTelemetrySanitizerAccessor CreateAccessor(AccessTelemetrySanitizer sanitizer)
    {
        var accessor = new AccessTelemetrySanitizerAccessor();
        accessor.Publish(sanitizer);
        return accessor;
    }
}
