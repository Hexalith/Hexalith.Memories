// <copyright file="InMemoryTelemetryEnvironment.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Telemetry;

/// <summary>
/// Story 8.4 — env-var contract that activates the in-memory OTLP capture branch in
/// <c>ServiceDefaults.ConfigureOpenTelemetry</c>. Only the exact string <c>"1"</c> activates the
/// branch (Risk 7 mitigation): values like <c>"true"</c>, <c>"on"</c>, <c>" 1"</c>, <c>"01"</c>
/// silently leave the in-memory exporter unregistered, but the consumer is warned via
/// <c>Console.Error</c> when a non-empty mismatching value is observed.
/// </summary>
public static class InMemoryTelemetryEnvironment
{
    /// <summary>Env var name. Set to exactly <c>"1"</c> to activate the in-memory tracing + logging exporters.</summary>
    public const string EnvVar = "HEXALITH_MEMORIES_TELEMETRY_INMEMORY";

    /// <summary>The single value that activates the branch.</summary>
    public const string EnabledValue = "1";

    /// <summary>Returns <c>true</c> when the supplied value exactly equals <see cref="EnabledValue"/>.</summary>
    /// <param name="value">Raw env-var value (may be null or empty).</param>
    /// <returns><c>true</c> if exactly <c>"1"</c>; <c>false</c> otherwise (including null / empty / variants like "true").</returns>
    public static bool IsEnabled(string? value) => string.Equals(value, EnabledValue, System.StringComparison.Ordinal);

    /// <summary>Returns the warning text emitted to stderr when the env var is set but to a non-activating value.
    /// Extracted as a method so unit tests can assert the exact text without re-deriving it.</summary>
    /// <param name="value">The non-activating value the consumer supplied.</param>
    /// <returns>Warning text suitable for <c>Console.Error.WriteLine</c>.</returns>
    public static string FormatIgnoredValueWarning(string value)
        => $"[telemetry] {EnvVar}={value} — only \"{EnabledValue}\" activates; ignoring";
}
