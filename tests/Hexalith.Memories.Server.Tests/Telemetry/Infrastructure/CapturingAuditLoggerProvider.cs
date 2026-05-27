// <copyright file="CapturingAuditLoggerProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

/// <summary>
/// Story 7.5 Rev 1.3 — <see cref="ILoggerProvider"/> that captures entries written to the
/// <c>Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory</c> logger category in-process. Every log
/// entry produced by the <c>[LoggerMessage]</c>-generated emitters in <see cref="Telemetry.AccessTelemetryLog"/>
/// passes its <see cref="AccessTelemetryEvent"/> argument as a structured-state key <c>AuditEvent</c>; this
/// provider pulls that key out of the <c>TState</c> message template so tests can assert the AC #4 JSON
/// shape directly on the record without reparsing the formatted message string.
/// </summary>
internal sealed class CapturingAuditLoggerProvider : ILoggerProvider
{
    private const string AccessTelemetryCategoryName = "Hexalith.Memories.Server.Telemetry.AccessTelemetryCategory";

    private readonly object _sync = new();
    private readonly List<AuditLogCapture> _captures = [];

    /// <summary>Gets a snapshot of the captured audit events in emission order (empty for non-telemetry categories).</summary>
    public IReadOnlyList<AuditLogCapture> Captures
    {
        get
        {
            lock (_sync)
            {
                return [.. _captures];
            }
        }
    }

    /// <summary>Returns only the captures whose category matches the access-telemetry logger, in emission order.</summary>
    public IReadOnlyList<AuditLogCapture> AccessTelemetryCaptures
    {
        get
        {
            lock (_sync)
            {
                return [.. _captures.Where(c => string.Equals(c.Category, AccessTelemetryCategoryName, StringComparison.Ordinal))];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _sync, _captures);

    public void Dispose()
    {
        // Nothing to dispose — the ConcurrentBag lives as long as the factory.
    }

    private sealed class CapturingLogger(string category, object sync, List<AuditLogCapture> captures) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            AccessTelemetryEvent? auditEvent = TryExtractAuditEvent(state);
            string message = formatter(state, exception);
            lock (sync)
            {
                captures.Add(new AuditLogCapture(logLevel, eventId.Id, category, auditEvent, message));
            }
        }

        private static AccessTelemetryEvent? TryExtractAuditEvent<TState>(TState state)
        {
            // [LoggerMessage] source-generated emitters expose the message-template arguments via the
            // IReadOnlyList<KeyValuePair<string, object?>> shape implemented by a generated LogState struct.
            // Walk the values for any AccessTelemetryEvent — the exact key name depends on the template
            // ("AuditEvent" for "{@AuditEvent}", but we match by value type to be tolerant of future renames).
            if (state is IReadOnlyList<KeyValuePair<string, object?>> keyedState)
            {
                foreach (KeyValuePair<string, object?> kv in keyedState)
                {
                    if (kv.Value is AccessTelemetryEvent evt)
                    {
                        return evt;
                    }
                }
            }

            // Fallback: if the source gen changed shape, walk IEnumerable<KeyValuePair<string, object?>>.
            if (state is IEnumerable<KeyValuePair<string, object?>> enumerableState)
            {
                foreach (KeyValuePair<string, object?> kv in enumerableState)
                {
                    if (kv.Value is AccessTelemetryEvent evt)
                    {
                        return evt;
                    }
                }
            }

            return null;
        }
    }
}

/// <summary>Captured audit log entry — the <see cref="AccessTelemetryEvent"/> is extracted from the
/// <c>{@AuditEvent}</c> structured-destructuring argument when the log category is the access-telemetry
/// category; other categories produce a capture with a null <see cref="AuditEvent"/>.</summary>
/// <param name="Level">Log level (Information for 7501-7505; Warning for 7511-7515).</param>
/// <param name="EventId">Event id (7501-7599 bank for audit emitters).</param>
/// <param name="Category">Logger category name.</param>
/// <param name="AuditEvent">Audit event record (null if the log entry was not an audit emission).</param>
/// <param name="Message">Formatted message.</param>
internal sealed record AuditLogCapture(
    LogLevel Level,
    int EventId,
    string Category,
    AccessTelemetryEvent? AuditEvent,
    string Message);
