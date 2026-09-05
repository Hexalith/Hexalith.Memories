// <copyright file="AccessTelemetryLifecycleLogger.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

/// <summary>Typed-state lifecycle logger that never invokes formatters or propagates exceptions.</summary>
internal sealed class AccessTelemetryLifecycleLogger : ILogger
{
    private static readonly string RequiredCategory = typeof(AccessTelemetryCategory).FullName!;
    private readonly bool _categoryMatches;
    private readonly BoundedAccessTelemetryQueue _queue;
    private readonly AccessTelemetrySanitizerAccessor _sanitizerAccessor;
    private readonly AccessTelemetryLifecycleStatus? _status;
    private readonly TimeProvider? _timeProvider;
    private readonly AccessTelemetryQualificationAccounting? _qualificationAccounting;

    /// <summary>Initializes a category-bound lifecycle logger.</summary>
    public AccessTelemetryLifecycleLogger(
        string categoryName,
        BoundedAccessTelemetryQueue queue,
        AccessTelemetrySanitizer sanitizer)
        : this(categoryName, queue, CreateAccessor(sanitizer), null, null, null)
    {
    }

    /// <summary>Initializes a category-bound lifecycle logger with fail-closed secret bootstrap.</summary>
    public AccessTelemetryLifecycleLogger(
        string categoryName,
        BoundedAccessTelemetryQueue queue,
        AccessTelemetrySanitizerAccessor sanitizerAccessor)
        : this(categoryName, queue, sanitizerAccessor, null, null, null)
    {
    }

    /// <summary>Initializes a category-bound lifecycle logger with health activity tracking.</summary>
    public AccessTelemetryLifecycleLogger(
        string categoryName,
        BoundedAccessTelemetryQueue queue,
        AccessTelemetrySanitizerAccessor sanitizerAccessor,
        AccessTelemetryLifecycleStatus? status,
        TimeProvider? timeProvider,
        AccessTelemetryQualificationAccounting? qualificationAccounting = null)
    {
        _categoryMatches = string.Equals(categoryName, RequiredCategory, StringComparison.Ordinal);
        _queue = queue;
        _sanitizerAccessor = sanitizerAccessor;
        _status = status;
        _timeProvider = timeProvider;
        _qualificationAccounting = qualificationAccounting;
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
        => _categoryMatches && logLevel is LogLevel.Information or LogLevel.Warning;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            AccessTelemetryEvent? source = ExtractTypedState(state);
            if (source is not null)
            {
                _qualificationAccounting?.RecordAttempted();
            }
            if (source is not null && _status is not null && _timeProvider is not null)
            {
                _status.RecordActivity(_timeProvider.GetUtcNow());
            }

            AccessTelemetrySanitizer? sanitizer = _sanitizerAccessor.Current;
            if (source is not null && sanitizer is not null && sanitizer.TrySanitize(logLevel, eventId, source, out AccessTelemetryRecord? record, out _))
            {
                ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Accepted, AccessTelemetryReason.None);
                if (_queue.TryEnqueue(record!, out AccessTelemetryReason reason))
                {
                    ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Enqueued, AccessTelemetryReason.None);
                    _qualificationAccounting?.RecordEnqueued();
                    ServerAccessTelemetryLifecycleMetrics.RecordQueue(
                        _queue.Count,
                        _queue.ByteCount,
                        _queue.OldestEmittedAtUtc,
                        _timeProvider ?? TimeProvider.System);
                }
                else
                {
                    ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Dropped, reason);
                    _qualificationAccounting?.RecordDropped();
                }
            }
            else if (source is not null && sanitizer is null)
            {
                ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Rejected, AccessTelemetryReason.RemoteValidationPending);
                _qualificationAccounting?.RecordRejected(1);
            }
            else if (source is not null)
            {
                ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Rejected, AccessTelemetryReason.SchemaMismatch);
                _qualificationAccounting?.RecordRejected(1);
            }
        }
        catch (Exception caught) when (caught is not OutOfMemoryException and not StackOverflowException)
        {
            // Logging infrastructure is never allowed to alter the business request outcome.
        }
    }

    private static AccessTelemetrySanitizerAccessor CreateAccessor(AccessTelemetrySanitizer sanitizer)
    {
        var accessor = new AccessTelemetrySanitizerAccessor();
        accessor.Publish(sanitizer);
        return accessor;
    }

    private static AccessTelemetryEvent? ExtractTypedState<TState>(TState state)
    {
        if (state is AccessTelemetryEvent direct)
        {
            return direct;
        }

        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (KeyValuePair<string, object?> value in values)
            {
                if (value.Value is AccessTelemetryEvent typed)
                {
                    return typed;
                }
            }
        }

        return null;
    }
}
