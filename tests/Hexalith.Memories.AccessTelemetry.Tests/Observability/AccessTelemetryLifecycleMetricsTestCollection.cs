// <copyright file="AccessTelemetryLifecycleMetricsTestCollection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Observability;

/// <summary>
/// xUnit collection shared by every test that observes the process-wide static
/// <c>AccessTelemetryLifecycleMetrics.Records</c> counter via a <see cref="System.Diagnostics.Metrics.MeterListener"/>.
/// Prevents cross-class parallelism from letting one test's <c>MeterListener</c> capture another
/// concurrently-running test's emissions.
/// <para>
/// Every test class that either records to <c>AccessTelemetryLifecycleMetrics</c> or asserts on its
/// emitted tags via a <see cref="System.Diagnostics.Metrics.MeterListener"/> MUST be annotated with
/// <c>[Collection(AccessTelemetryLifecycleMetricsTestCollection.Name)]</c> — otherwise a concurrently
/// running class (e.g. one exercising <c>AccessTelemetryLifecycleProcessor</c>) emits real measurements
/// that leak into the listener under test.
/// </para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AccessTelemetryLifecycleMetricsTestCollection
{
    /// <summary>Collection name — applied via <c>[Collection(AccessTelemetryLifecycleMetricsTestCollection.Name)]</c>.</summary>
    public const string Name = "AccessTelemetryLifecycleMetrics";
}
