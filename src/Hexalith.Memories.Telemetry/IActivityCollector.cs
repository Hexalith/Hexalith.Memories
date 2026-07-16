// <copyright file="IActivityCollector.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Story 8.4 — abstraction over the in-memory <see cref="Activity"/> sink used by the Tier-3
/// integration tests. The implementation lives in the integration-test assembly
/// (<c>tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/InMemorySpanCollector.cs</c>)
/// per ADR-8.4-002 (option B — test-only placement). This interface stays in the production
/// telemetry assembly so <c>ServiceDefaults.ConfigureOpenTelemetry</c> can resolve the collector
/// from DI at processor-creation time without taking a test-only dependency.
/// </summary>
public interface IActivityCollector
{
    /// <summary>Gets the mutable collection that the in-memory exporter appends to. Implementations
    /// MUST return a thread-safe collection (<see cref="System.Collections.Concurrent.ConcurrentBag{T}"/>
    /// or equivalent) — the OpenTelemetry exporter calls <see cref="ICollection{T}.Add"/> from arbitrary
    /// background threads when activities stop.</summary>
    ICollection<Activity> Activities { get; }
}
