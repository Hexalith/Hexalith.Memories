// <copyright file="TelemetryTestCollection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

/// <summary>
/// Story 7.5 Rev 1.3 — xUnit collection shared by every telemetry test that touches the process-global
/// <see cref="System.Diagnostics.ActivitySource"/> or <see cref="System.Diagnostics.Metrics.Meter"/>
/// singletons exposed by <c>Hexalith.Memories.Telemetry</c>. Prevents cross-class parallelism from
/// polluting the per-test <c>MeterListener</c> / <c>ActivityListener</c> captures.
/// <para>
/// Every telemetry test class that registers a listener on <c>MemoriesMeter.Instance</c> or
/// <c>MemoriesActivitySource.Instance</c> MUST be annotated with
/// <c>[Collection(TelemetryTestCollection.Name)]</c> — otherwise a concurrent Tier-2
/// <see cref="TelemetryWebAppFactory"/>-driven test emits real measurements that leak into its listener.
/// </para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TelemetryTestCollection
{
    /// <summary>Collection name — applied via <c>[Collection(TelemetryTestCollection.Name)]</c>.</summary>
    public const string Name = "Telemetry.Shared";
}
