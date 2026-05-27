// <copyright file="CapturingEventStoreLogProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

/// <summary>Captures log entries emitted by the EventStore package (category prefix
/// <c>Hexalith.Memories.EventStore.</c>) so Tier-2 tests can assert severity / event-id / message content
/// without reparsing formatted strings. Mirrors <see cref="Telemetry.Infrastructure.CapturingAuditLoggerProvider"/>
/// in shape but is scoped to the 9100-9199 EventId bank.</summary>
internal sealed class CapturingEventStoreLogProvider : ILoggerProvider
{
    private const string EventStoreCategoryPrefix = "Hexalith.Memories.EventStore.";

    private readonly object _sync = new();
    private readonly List<EventStoreLogCapture> _captures = [];

    public IReadOnlyList<EventStoreLogCapture> EventStoreCaptures
    {
        get
        {
            lock (_sync)
            {
                return [.. _captures.Where(c => c.Category.StartsWith(EventStoreCategoryPrefix, System.StringComparison.Ordinal))];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _sync, _captures);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, object sync, List<EventStoreLogCapture> captures) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            System.Exception? exception,
            System.Func<TState, System.Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            lock (sync)
            {
                captures.Add(new EventStoreLogCapture(logLevel, eventId.Id, category, message));
            }
        }
    }
}

internal sealed record EventStoreLogCapture(
    LogLevel Level,
    int EventId,
    string Category,
    string Message);
