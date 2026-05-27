// <copyright file="ILogRecordCollector.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Telemetry;

using System.Collections.Generic;

/// <summary>
/// Story 8.4 — abstraction over the in-memory log-record sink used by the Tier-3 integration
/// tests for AC #4 (TraceId/SpanId cross-reference between activity and audit event via the
/// in-process log exporter, per ADR-8.4-003). The collector receives <see cref="object"/>
/// rather than <c>OpenTelemetry.Logs.LogRecord</c> so this production-side interface does not
/// take a hard dependency on the OpenTelemetry logs SDK; the integration-test implementation
/// boxes <c>LogRecord</c> instances into the collection. Tests downcast back to inspect
/// <c>TraceId</c>, <c>SpanId</c>, <c>EventId</c>, and the structured state.
/// </summary>
public interface ILogRecordCollector
{
    /// <summary>Gets the mutable collection that the in-memory log exporter appends to.
    /// Implementations MUST return a thread-safe collection — exporter calls happen on
    /// background threads as records flush.</summary>
    ICollection<object> Records { get; }
}
