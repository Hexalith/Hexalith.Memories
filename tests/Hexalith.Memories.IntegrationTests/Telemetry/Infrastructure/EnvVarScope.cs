// <copyright file="EnvVarScope.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System;

/// <summary>
/// Story 8.4 (Amelia, party-mode review 2026-04-20) — disposable that snapshots a process env var on
/// construction and restores the prior value on dispose. Use everywhere the integration tests set
/// <c>HEXALITH_MEMORIES_OTEL_ENDPOINT</c> or <c>HEXALITH_MEMORIES_TELEMETRY_INMEMORY</c> so static
/// env-var state cannot bleed across parallel test collections (Tier-2 + Tier-3 may both touch the
/// same process under <c>dotnet test</c>).
/// <para>
/// Usage: <c>using var _ = EnvVarScope.Set(InMemoryTelemetryEnvironment.EnvVar, "1");</c>
/// </para>
/// </summary>
internal sealed class EnvVarScope : IDisposable
{
    private readonly string _name;
    private readonly string? _previousValue;

    private EnvVarScope(string name, string? previousValue)
    {
        _name = name;
        _previousValue = previousValue;
    }

    /// <summary>Sets the env var to <paramref name="value"/> after snapshotting the prior value.</summary>
    /// <param name="name">Env var name.</param>
    /// <param name="value">Value to set (use <c>null</c> to clear within the scope).</param>
    /// <returns>A scope that restores the previous value on dispose.</returns>
    public static EnvVarScope Set(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string? previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        return new EnvVarScope(name, previous);
    }

    /// <inheritdoc/>
    public void Dispose() => Environment.SetEnvironmentVariable(_name, _previousValue);
}
