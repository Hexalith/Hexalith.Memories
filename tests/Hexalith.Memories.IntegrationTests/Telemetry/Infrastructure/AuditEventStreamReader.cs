// <copyright file="AuditEventStreamReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>
/// Story 8.4 — extracts <see cref="AccessTelemetryEvent"/> records from the Aspire fixture's
/// captured Server-resource log stream. The Memories Server uses <c>AddJsonConsole</c>; Aspire
/// orchestration captures the Server's stdout and surfaces each line into the AppHost's logging
/// pipeline (which the fixture's <c>_logProvider</c> taps via <c>GetLogEntriesSince</c>).
/// <para>
/// FR67 path: this reader is the operator-pipeline gate per ADR-8.4-003. Every emitted audit event
/// (EventId 7501-7515) MUST surface here for an operator's SIEM / log aggregator to receive it. If
/// the read returns zero matching lines, EITHER the Server didn't emit OR Aspire's stdout pipe
/// dropped the line — both are observable failures the test should call out.
/// </para>
/// <para>
/// Timing tolerance: the reader polls the captured log entries every 200ms up to a configurable
/// timeout (default 10s, overridable via <c>TELEMETRY_E2E_STDOUT_TIMEOUT_SECONDS</c> per Task 3.6).
/// The polling avoids <c>Thread.Sleep</c>; cancellation honors the supplied
/// <see cref="CancellationToken"/>.
/// </para>
/// </summary>
internal static class AuditEventStreamReader
{
    /// <summary>EventId range allocated to Story 7.5 audit events (7501-7515 inclusive — the
    /// success bank 7501-7505 + the error bank 7511-7515; 7516-7599 reserved for future bumps).</summary>
    public const int MinEventId = 7501;

    /// <summary>Inclusive upper bound on Story 7.5 audit event ids.</summary>
    public const int MaxEventId = 7599;

    /// <summary>Env var name overriding the default polling timeout in seconds (Task 3.6).</summary>
    public const string TimeoutEnvVar = "TELEMETRY_E2E_STDOUT_TIMEOUT_SECONDS";

    /// <summary>Default polling timeout when <see cref="TimeoutEnvVar"/> is unset.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Returns the effective polling timeout — env-var override if present and parses to
    /// a positive integer; default otherwise.</summary>
    /// <returns>The configured timeout.</returns>
    public static TimeSpan ResolveTimeout()
    {
        string? raw = Environment.GetEnvironmentVariable(TimeoutEnvVar);
        if (!string.IsNullOrWhiteSpace(raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)
            && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return DefaultTimeout;
    }

    /// <summary>Polls the fixture's captured log stream for <see cref="AccessTelemetryEvent"/>
    /// records emitted since <paramref name="logStartIndex"/>. Returns once at least
    /// <paramref name="minimumEvents"/> matching events are found, OR the timeout elapses (in
    /// which case the reader returns whatever it has collected, including an empty list — the
    /// caller's count-first assertion fails loudly with the captured stdout dump).</summary>
    /// <param name="fixture">The shared Aspire fixture exposing the captured log stream.</param>
    /// <param name="logStartIndex">The 0-based index in the captured log buffer to start scanning from.</param>
    /// <param name="minimumEvents">Minimum number of matching events to wait for before returning early. Use 1 for "at least one" semantics.</param>
    /// <param name="timeout">Polling timeout (use <see cref="ResolveTimeout"/> for the env-var-aware default).</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Captured <see cref="AccessTelemetryEvent"/> records in emission order, with their
    /// originating raw stdout JSON line for triage on assertion failure.</returns>
    public static async Task<IReadOnlyList<CapturedAuditEvent>> ReadAsync(
        AspireIngestionPipelineFixture fixture,
        int logStartIndex,
        int minimumEvents,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentOutOfRangeException.ThrowIfNegative(logStartIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumEvents);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        TimeSpan pollInterval = TimeSpan.FromMilliseconds(200);

        IReadOnlyList<CapturedAuditEvent> latest = [];
        while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            latest = ScanCapturedLogs(fixture, logStartIndex);
            if (latest.Count >= minimumEvents)
            {
                return latest;
            }

            try
            {
                await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        return latest;
    }

    /// <summary>Returns the last <paramref name="maxLines"/> raw stdout lines (any category) since
    /// <paramref name="logStartIndex"/>. Used for diagnostic dumps on test failure when no audit
    /// events arrive — gives a future debugger the immediate context.</summary>
    /// <param name="fixture">Shared Aspire fixture.</param>
    /// <param name="logStartIndex">Start index in the captured log buffer.</param>
    /// <param name="maxLines">Maximum number of trailing lines to return.</param>
    /// <returns>The trailing log lines.</returns>
    public static IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> TailRawLogs(
        AspireIngestionPipelineFixture fixture,
        int logStartIndex,
        int maxLines = 50)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> all = fixture.GetLogEntriesSince(logStartIndex);
        if (all.Count <= maxLines)
        {
            return all;
        }

        return [.. all.Skip(all.Count - maxLines)];
    }

    private static IReadOnlyList<CapturedAuditEvent> ScanCapturedLogs(
        AspireIngestionPipelineFixture fixture,
        int logStartIndex)
    {
        IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> entries = fixture.GetLogEntriesSince(logStartIndex);
        List<CapturedAuditEvent> matches = [];
        foreach (AspireIngestionPipelineFixture.CapturedLogEntry entry in entries)
        {
            if (string.IsNullOrEmpty(entry.Message))
            {
                continue;
            }

            // Aspire's resource-log forwarder prepends a sequence + timestamp prefix
            // (e.g. "24: 2026-04-20T14:21:50.9890000Z {...json...}") to each captured stdout line, so the
            // line does not start with '{'. Locate the first '{' to find the JSON payload boundary.
            int braceIndex = entry.Message.IndexOf('{', StringComparison.Ordinal);
            if (braceIndex < 0)
            {
                continue;
            }

            string jsonLine = entry.Message[braceIndex..];
            CapturedAuditEvent? captured = TryParse(entry, jsonLine);
            if (captured is not null)
            {
                matches.Add(captured);
            }
        }

        return matches;
    }

    private static CapturedAuditEvent? TryParse(
        AspireIngestionPipelineFixture.CapturedLogEntry entry,
        string jsonLine)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonLine);
            JsonElement root = doc.RootElement;

            // AddJsonConsole emits the structured log shape {"EventId":<int>,"LogLevel":...,
            // "Category":...,"Message":"<formatted>","State":{...}}. Microsoft.Extensions.Logging's
            // [LoggerMessage] source-generated emitters PASS the AccessTelemetryEvent record to the
            // structured `{@AuditEvent}` placeholder, but AddJsonConsole does NOT natively destructure
            // record-type arguments to nested JSON — it serializes via record.ToString(). The resulting
            // top-level "Message" field carries the C# record `ToString()` output (e.g.
            // `Search access error AccessTelemetryEvent { SchemaVersion = 1, EventId = 7511, ..., TraceId = ..., SpanId = ... }`)
            // and so does State."@AuditEvent". We extract fields from that string with regex.
            if (!TryReadEventId(root, out int eventId)
                || eventId < MinEventId
                || eventId > MaxEventId)
            {
                return null;
            }

            string? category = null;
            if (root.TryGetProperty("Category", out JsonElement categoryElement)
                && categoryElement.ValueKind == JsonValueKind.String)
            {
                category = categoryElement.GetString();
            }

            // Filter to the AccessTelemetryCategory specifically — other Server-side logs that happen to
            // share the EventId range would be a contract violation, but we belt-and-suspender it here.
            if (!string.IsNullOrEmpty(category)
                && !category.EndsWith("AccessTelemetryCategory", StringComparison.Ordinal))
            {
                return null;
            }

            string? auditFormatted = ExtractAuditEventToStringText(root);
            if (string.IsNullOrEmpty(auditFormatted))
            {
                return null;
            }

            AccessTelemetryEvent? auditEvent = TryParseRecordToString(auditFormatted, eventId);
            return auditEvent is null
                ? null
                : new CapturedAuditEvent(entry.Category, entry.Level, eventId, jsonLine, auditEvent);
        }
        catch (JsonException)
        {
            // Not a valid JSON entry — skip silently. Operators triaging can read TailRawLogs() to inspect.
            return null;
        }
    }

    private static bool TryReadEventId(JsonElement root, out int eventId)
    {
        eventId = 0;
        if (root.TryGetProperty("EventId", out JsonElement element)
            || root.TryGetProperty("eventId", out element))
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.TryGetInt32(out eventId),
                JsonValueKind.String => int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out eventId),
                _ => false,
            };
        }

        return false;
    }

    private static string? ExtractAuditEventToStringText(JsonElement root)
    {
        // Preferred path: top-level "Message" string contains the formatted log line, which embeds the
        // record ToString. Format: "<prefix>AccessTelemetryEvent { ... }".
        if (root.TryGetProperty("Message", out JsonElement messageElement)
            && messageElement.ValueKind == JsonValueKind.String)
        {
            string? message = messageElement.GetString();
            if (!string.IsNullOrEmpty(message)
                && message.Contains("AccessTelemetryEvent {", StringComparison.Ordinal))
            {
                return message;
            }
        }

        // Alternate path: State.@AuditEvent or State.AuditEvent string field.
        if (root.TryGetProperty("State", out JsonElement state) && state.ValueKind == JsonValueKind.Object)
        {
            foreach (string key in new[] { "@AuditEvent", "AuditEvent" })
            {
                if (state.TryGetProperty(key, out JsonElement audit) && audit.ValueKind == JsonValueKind.String)
                {
                    return audit.GetString();
                }
            }
        }

        return null;
    }

    private static AccessTelemetryEvent? TryParseRecordToString(string text, int eventId)
    {
        // Locate the `AccessTelemetryEvent { ... }` substring inside the formatted message.
        Match recordMatch = RecordToStringPattern.Match(text);
        if (!recordMatch.Success)
        {
            return null;
        }

        string body = recordMatch.Groups["body"].Value;

        // Split on `, FieldName = ` boundaries. The split-and-pair approach tolerates field values that
        // themselves contain commas (e.g. `Dictionary`2[System.String,System.Object]`) by anchoring each
        // boundary to a `, <PascalIdentifier> =` lookahead.
        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        MatchCollection fieldMatches = RecordFieldPattern.Matches(body);
        foreach (Match m in fieldMatches)
        {
            string name = m.Groups["name"].Value;
            string value = m.Groups["value"].Value.Trim();
            fields[name] = value;
        }

        if (fields.Count == 0)
        {
            return null;
        }

        return new AccessTelemetryEvent
        {
            EventId = eventId,
            SchemaVersion = TryParseInt(fields, "SchemaVersion", AccessTelemetryEvent.CurrentSchemaVersion),
            Timestamp = NullIfEmpty(fields.GetValueOrDefault("Timestamp")) ?? string.Empty,
            TenantId = NullIfEmpty(fields.GetValueOrDefault("TenantId")) ?? string.Empty,
            OperationType = NullIfEmpty(fields.GetValueOrDefault("OperationType")) ?? string.Empty,
            CaseId = NullIfEmpty(fields.GetValueOrDefault("CaseId")),
            User = NullIfEmpty(fields.GetValueOrDefault("User")) ?? string.Empty,
            QueryParams = new Dictionary<string, object?>(0),
            ResultCount = TryParseNullableInt(fields.GetValueOrDefault("ResultCount")),
            DurationMs = TryParseLong(fields, "DurationMs", 0),
            Outcome = NullIfEmpty(fields.GetValueOrDefault("Outcome")) ?? string.Empty,
            ErrorCode = NullIfEmpty(fields.GetValueOrDefault("ErrorCode")),
            TraceId = NullIfEmpty(fields.GetValueOrDefault("TraceId")),
            SpanId = NullIfEmpty(fields.GetValueOrDefault("SpanId")),
        };
    }

    private static int TryParseInt(Dictionary<string, string> fields, string name, int fallback)
        => fields.TryGetValue(name, out string? raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? v
            : fallback;

    private static long TryParseLong(Dictionary<string, string> fields, string name, long fallback)
        => fields.TryGetValue(name, out string? raw) && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v)
            ? v
            : fallback;

    private static int? TryParseNullableInt(string? raw)
        => !string.IsNullOrEmpty(raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? v
            : null;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>Captures the body of `AccessTelemetryEvent { ... }` (the toString-format payload).</summary>
    private static readonly Regex RecordToStringPattern = new(
        @"AccessTelemetryEvent\s*\{\s*(?<body>[^}]*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Splits the body into `Name = value` pairs. Each value runs up to (but not including) the
    /// next `, PascalIdentifier =` boundary or end-of-body. Tolerates field values that contain commas
    /// (e.g. <c>Dictionary`2[System.String,System.Object]</c>) by anchoring on the next field boundary.</summary>
    private static readonly Regex RecordFieldPattern = new(
        @"(?<name>[A-Z]\w*)\s*=\s*(?<value>.*?)(?=,\s*[A-Z]\w*\s*=|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
}

/// <summary>Captured Server-side audit log entry along with the original raw JSON line for triage.</summary>
/// <param name="Category">Log entry category as captured by the AppHost log provider.</param>
/// <param name="Level">Log level.</param>
/// <param name="EventId">EventId from the JSON line (7501-7515 for Story 7.5 audit events).</param>
/// <param name="RawJsonLine">Original raw JSON line; included so test failure diagnostics show the actual stdout content.</param>
/// <param name="AuditEvent">Deserialized <see cref="AccessTelemetryEvent"/>.</param>
internal sealed record CapturedAuditEvent(
    string Category,
    Microsoft.Extensions.Logging.LogLevel Level,
    int EventId,
    string RawJsonLine,
    AccessTelemetryEvent AuditEvent);
