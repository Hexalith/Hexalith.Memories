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
    private readonly AccessTelemetryLifecycleStatus? _status;
    private readonly TimeProvider? _timeProvider;

    /// <summary>Initializes the lifecycle provider.</summary>
    public AccessTelemetryLifecycleLoggerProvider(BoundedAccessTelemetryQueue queue, AccessTelemetrySanitizer sanitizer)
        : this(queue, CreateAccessor(sanitizer), null, null)
    {
    }

    /// <summary>Initializes the lifecycle provider with fail-closed secret bootstrap.</summary>
    public AccessTelemetryLifecycleLoggerProvider(
        BoundedAccessTelemetryQueue queue,
        AccessTelemetrySanitizerAccessor sanitizerAccessor)
        : this(queue, sanitizerAccessor, null, null)
    {
    }

    /// <summary>Initializes the lifecycle provider with runtime health activity tracking.</summary>
    public AccessTelemetryLifecycleLoggerProvider(
        BoundedAccessTelemetryQueue queue,
        AccessTelemetrySanitizerAccessor sanitizerAccessor,
        AccessTelemetryLifecycleStatus? status,
        TimeProvider? timeProvider)
    {
        _queue = queue;
        _sanitizerAccessor = sanitizerAccessor;
        _status = status;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
        => new AccessTelemetryLifecycleLogger(categoryName, _queue, _sanitizerAccessor, _status, _timeProvider);

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
