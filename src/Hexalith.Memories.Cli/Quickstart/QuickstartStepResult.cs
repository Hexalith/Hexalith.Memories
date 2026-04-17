// <copyright file="QuickstartStepResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Wizard step status. Serialized as kebab-case-lower strings ("ok", "fail", "skip", "dry-run")
/// per Revision 0.2 — Amelia finding (JSON wire format locked independent of enum member name).
/// </summary>
[JsonConverter(typeof(QuickstartStepStatusJsonConverter))]
public enum QuickstartStepStatus
{
    /// <summary>Step executed successfully.</summary>
    Ok,

    /// <summary>Step failed; wizard short-circuits remaining steps.</summary>
    Fail,

    /// <summary>Step skipped via flag or idempotent no-op.</summary>
    Skip,

    /// <summary>Step described what it would have done; no side effects (--dry-run).</summary>
    DryRun,
}

/// <summary>
/// Converts <see cref="QuickstartStepStatus"/> to/from kebab-case-lower JSON strings ("ok", "fail",
/// "skip", "dry-run").
/// </summary>
internal sealed class QuickstartStepStatusJsonConverter : JsonConverter<QuickstartStepStatus>
{
    /// <inheritdoc/>
    public override QuickstartStepStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return value switch
        {
            "ok" => QuickstartStepStatus.Ok,
            "fail" => QuickstartStepStatus.Fail,
            "skip" => QuickstartStepStatus.Skip,
            "dry-run" => QuickstartStepStatus.DryRun,
            _ => throw new JsonException($"Unknown quickstart step status '{value}'."),
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, QuickstartStepStatus value, JsonSerializerOptions options)
    {
        string serialized = value switch
        {
            QuickstartStepStatus.Ok => "ok",
            QuickstartStepStatus.Fail => "fail",
            QuickstartStepStatus.Skip => "skip",
            QuickstartStepStatus.DryRun => "dry-run",
            _ => throw new JsonException($"Unsupported quickstart step status '{value}'."),
        };
        writer.WriteStringValue(serialized);
    }
}

/// <summary>
/// Result of a single wizard step. Per-step failure context lives on this record (ADR-7.4-003 —
/// preserves the 7.3 envelope mutual-exclusivity invariant by keeping failure details inside
/// <c>data.steps[]</c> rather than the top-level <c>error</c> slot).
/// </summary>
/// <param name="Id">Step number (1 through 6).</param>
/// <param name="Title">Human-readable step title (e.g., "Verifying prerequisites").</param>
/// <param name="Status">Outcome of the step.</param>
/// <param name="Duration">Wall-clock time spent on this step.</param>
/// <param name="Message">Human-readable one-line outcome (mirrors the stdout line).</param>
/// <param name="Suggestion">Actionable next-step suggestion when <paramref name="Status"/> is <see cref="QuickstartStepStatus.Fail"/>; null otherwise.</param>
/// <param name="ErrorCode">Catalog-resolved or synthetic CLI code when <paramref name="Status"/> is <see cref="QuickstartStepStatus.Fail"/>; null otherwise.</param>
public sealed record QuickstartStepResult(
    int Id,
    string Title,
    QuickstartStepStatus Status,
    [property: JsonIgnore] TimeSpan Duration,
    string Message,
    string? Suggestion,
    string? ErrorCode)
{
    /// <summary>Wall-clock time spent on this step, serialized as integer milliseconds.</summary>
    [JsonPropertyName("durationMs")]
    public int DurationMs => checked((int)Math.Round(Duration.TotalMilliseconds, MidpointRounding.AwayFromZero));
}
