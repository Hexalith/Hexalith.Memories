// <copyright file="InMemorySpanCollector.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Hexalith.Memories.Telemetry;

/// <summary>
/// Story 8.4 Task 1.1 — test-only <see cref="IActivityCollector"/> implementation. Stored under
/// <c>tests/</c> per ADR-8.4-002 (option B): the collector is NOT a production-visible surface
/// because a static mutable activity sink in a consumer-facing package is a foot-gun for plugin
/// authors across the Hexalith ecosystem (Winston, party-mode review 2026-04-20).
/// <para>
/// Thread-safety: <see cref="Activities"/> exposes a synchronized <see cref="ICollection{T}"/>
/// wrapper backed by a <see cref="List{T}"/> with internal lock on every mutation. The
/// OpenTelemetry simple-export processor invokes <c>Add</c> on the activity-emitting thread, but
/// concurrent activities on parallel threads CAN race; the lock keeps the list consistent.
/// </para>
/// <para>
/// Reset semantics: callers MUST <c>await TracerProvider.ForceFlushAsync(...)</c> BEFORE invoking
/// <see cref="Reset"/> to avoid the activity-drain / reset race documented in Risk 10. The test
/// fixture's <c>DisposeAsync</c> follows that protocol.
/// </para>
/// </summary>
internal sealed class InMemorySpanCollector : IActivityCollector
{
    private readonly SynchronizedActivityList _activities = new();

    /// <inheritdoc/>
    public ICollection<Activity> Activities => _activities;

    /// <summary>Returns a stable snapshot of currently-collected activities, ordered by start time.</summary>
    public IReadOnlyList<Activity> Snapshot() => _activities.SnapshotOrderedByStart();

    /// <summary>Clears the collected activities. Callers MUST flush the tracer provider first
    /// (see class-level Risk 10 note).</summary>
    public void Reset() => _activities.Clear();

    /// <summary>Story 8.4 Task 2.3.5 — formats the captured span tree for triage on test failure.
    /// Each line: <c>{SpanId} parent={ParentSpanId} name={OperationName} trace={TraceId} source={Source.Name}</c>,
    /// sorted by start time. Surfaces instrumentation changes (added intermediate spans, missing expected
    /// spans) in one readable block so a future dev does not need to re-run with breakpoints.</summary>
    /// <returns>A multi-line string suitable for <c>TestContext.Output</c> attachment.</returns>
    public string FormatSpanTree()
    {
        StringBuilder sb = new();
        IReadOnlyList<Activity> ordered = Snapshot();
        sb.AppendLine($"InMemorySpanCollector: {ordered.Count} captured activities (ordered by StartTimeUtc):");
        foreach (Activity a in ordered)
        {
            sb.AppendLine(
                $"  {a.SpanId} parent={a.ParentSpanId} name={a.OperationName} trace={a.TraceId} source={a.Source.Name}");
        }

        return sb.ToString();
    }

    /// <summary>Synchronized <see cref="ICollection{T}"/> wrapper. <see cref="System.Collections.Concurrent.ConcurrentBag{T}"/>
    /// only implements <c>IProducerConsumerCollection&lt;T&gt;</c>, not <c>ICollection&lt;T&gt;</c> (no remove
    /// semantics), so OpenTelemetry's <c>AddInMemoryExporter(ICollection&lt;Activity&gt;)</c>-style API rejects
    /// it. This wrapper gives us thread-safe Add semantics with the right interface — minimal surface,
    /// reads via snapshot copy.</summary>
    private sealed class SynchronizedActivityList : ICollection<Activity>
    {
        private readonly object _gate = new();
        private readonly List<Activity> _items = [];

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _items.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public void Add(Activity item)
        {
            lock (_gate)
            {
                _items.Add(item);
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _items.Clear();
            }
        }

        public bool Contains(Activity item)
        {
            lock (_gate)
            {
                return _items.Contains(item);
            }
        }

        public void CopyTo(Activity[] array, int arrayIndex)
        {
            lock (_gate)
            {
                _items.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(Activity item)
        {
            lock (_gate)
            {
                return _items.Remove(item);
            }
        }

        public IEnumerator<Activity> GetEnumerator()
        {
            // Snapshot under lock so iteration does not race with concurrent Add.
            Activity[] snapshot;
            lock (_gate)
            {
                snapshot = [.. _items];
            }

            return ((IEnumerable<Activity>)snapshot).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IReadOnlyList<Activity> SnapshotOrderedByStart()
        {
            Activity[] snapshot;
            lock (_gate)
            {
                snapshot = [.. _items];
            }

            return [.. snapshot.OrderBy(a => a.StartTimeUtc)];
        }
    }
}
