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
/// The polling avoids <c>Thread.Sleep</c>; caller-initiated cancellation of the supplied
/// <see cref="CancellationToken"/> propagates as <see cref="OperationCanceledException"/> so callers
/// can distinguish cancellation from deadline-elapsed-with-empty-match.
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
    /// a positive integer; default otherwise. Surfaces a stderr warning when the env var is set
    /// but rejected (non-integer, non-positive) so the fallback is not silent.</summary>
    /// <returns>The configured timeout.</returns>
    public static TimeSpan ResolveTimeout()
    {
        string? raw = Environment.GetEnvironmentVariable(TimeoutEnvVar);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultTimeout;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)
            && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        Console.Error.WriteLine(
            $"[telemetry] {TimeoutEnvVar}={raw} — only positive integers (seconds) activate; " +
            $"ignoring and falling back to default {DefaultTimeout.TotalSeconds}s");
        return DefaultTimeout;
    }

    /// <summary>Polls the fixture's captured log stream for <see cref="AccessTelemetryEvent"/>
    /// records emitted since <paramref name="logStartIndex"/>. Returns once at least
    /// <paramref name="minimumEvents"/> matching events are found, OR the polling deadline elapses (in
    /// which case the reader returns whatever it has collected, including an empty list — the
    /// caller's count-first assertion fails loudly with the captured stdout dump).
    /// <para>Caller-initiated cancellation via <paramref name="cancellationToken"/> surfaces as
    /// <see cref="OperationCanceledException"/> so callers can distinguish cancellation from
    /// a deadline-elapsed empty match.</para></summary>
    /// <param name="fixture">The shared Aspire fixture exposing the captured log stream.</param>
    /// <param name="logStartIndex">The 0-based index in the captured log buffer to start scanning from.</param>
    /// <param name="minimumEvents">Minimum number of matching events to wait for before returning early. Use 1 for "at least one" semantics.</param>
    /// <param name="timeout">Polling timeout (use <see cref="ResolveTimeout"/> for the env-var-aware default).</param>
    /// <param name="cancellationToken">Cooperative cancellation. Rethrows <see cref="OperationCanceledException"/> on caller-initiated cancellation.</param>
    /// <param name="matchPredicate">Optional predicate determining which captured events count toward the
    /// early-return threshold. When omitted, every parsed audit event counts.</param>
    /// <returns>Captured <see cref="AccessTelemetryEvent"/> records in emission order, with their
    /// originating raw stdout JSON line for triage on assertion failure.</returns>
    public static async Task<IReadOnlyList<CapturedAuditEvent>> ReadAsync(
        AspireIngestionPipelineFixture fixture,
        int logStartIndex,
        int minimumEvents,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Func<CapturedAuditEvent, bool>? matchPredicate = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentOutOfRangeException.ThrowIfNegative(logStartIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumEvents);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        TimeSpan pollInterval = TimeSpan.FromMilliseconds(200);

        IReadOnlyList<CapturedAuditEvent> latest = [];
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            latest = ScanCapturedLogs(fixture, logStartIndex);
            int matchingCount = matchPredicate is null
                ? latest.Count
                : latest.Count(matchPredicate);
            if (matchingCount >= minimumEvents)
            {
                return latest;
            }

            try
            {
                await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Caller-initiated cancellation distinguishable from deadline-elapsed: rethrow.
                throw;
            }
            catch (OperationCanceledException)
            {
                // Delay cancellation not attributable to our CT (highly unlikely) — treat as
                // deadline elapse and exit the poll with the last-scanned snapshot.
                break;
            }
        }

        return latest;
    }

    /// <summary>Scans once with no polling window — useful for negative-space assertions where the
    /// caller has already waited for stragglers to land and just wants the current snapshot.</summary>
    /// <param name="fixture">Shared Aspire fixture.</param>
    /// <param name="logStartIndex">Start index in the captured log buffer.</param>
    /// <returns>All audit events parsed from the captured log stream since the start index.</returns>
    public static IReadOnlyList<CapturedAuditEvent> Scan(AspireIngestionPipelineFixture fixture, int logStartIndex)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentOutOfRangeException.ThrowIfNegative(logStartIndex);
        return ScanCapturedLogs(fixture, logStartIndex);
    }

    /// <summary>Returns the last <paramref name="maxLines"/> raw stdout lines (any category) since
    /// <paramref name="logStartIndex"/>. Used for diagnostic dumps on test failure when no audit
    /// events arrive — gives a future debugger the immediate context.</summary>
    /// <param name="fixture">Shared Aspire fixture.</param>
    /// <param name="logStartIndex">Start index in the captured log buffer.</param>
    /// <param name="maxLines">Maximum number of trailing lines to return. Must be positive.</param>
    /// <returns>The trailing log lines.</returns>
    public static IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> TailRawLogs(
        AspireIngestionPipelineFixture fixture,
        int logStartIndex,
        int maxLines = 50)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLines);

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
            // and so does State."@AuditEvent". We extract fields from that string with a stateful tokenizer
            // that tolerates nested `{}` / `[]` in field values (Dictionary`2[...] etc.).
            if (!TryReadEventId(root, out int eventId)
                || eventId < MinEventId
                || eventId > MaxEventId)
            {
                return null;
            }

            if (!root.TryGetProperty("Category", out JsonElement categoryElement)
                || categoryElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? category = categoryElement.GetString();

            // Filter to the AccessTelemetryCategory specifically — other Server-side logs that happen to
            // share the EventId range would be a contract violation, but we belt-and-suspender it here.
            if (string.IsNullOrWhiteSpace(category)
                || !category.EndsWith("AccessTelemetryCategory", StringComparison.Ordinal))
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
        // Locate the `AccessTelemetryEvent { ... }` substring using a balanced-brace scanner so
        // nested `{}` inside field values (e.g. user-supplied strings, dict representations in future
        // schemas) do NOT truncate the body prematurely. The previous `[^}]*` regex could not handle
        // nested braces at all; the stateful tokenizer below tolerates `{}` + `[]` at any depth.
        int typeIndex = text.IndexOf("AccessTelemetryEvent", StringComparison.Ordinal);
        if (typeIndex < 0)
        {
            return null;
        }

        int braceStart = text.IndexOf('{', typeIndex);
        if (braceStart < 0)
        {
            return null;
        }

        int braceEnd = FindMatchingBrace(text, braceStart);
        if (braceEnd < 0)
        {
            return null;
        }

        string body = text[(braceStart + 1)..braceEnd];
        Dictionary<string, string> fields = TokenizeRecordFields(body);
        if (fields.Count == 0)
        {
            return null;
        }

        if (!TryParseRequiredInt(fields, "SchemaVersion", out int schemaVersion)
            || !TryParseRequiredInt(fields, "EventId", out int parsedEventId)
            || parsedEventId != eventId
            || !TryParseRequiredLong(fields, "DurationMs", out long durationMs)
            || !TryParseOptionalInt(fields.GetValueOrDefault("ResultCount"), out int? resultCount))
        {
            return null;
        }

        string? timestamp = GetRequiredString(fields, "Timestamp");
        string? tenantId = GetRequiredString(fields, "TenantId");
        string? operationType = GetRequiredString(fields, "OperationType");
        string? user = GetRequiredString(fields, "User");
        string? outcome = GetRequiredString(fields, "Outcome");
        string? traceId = GetRequiredString(fields, "TraceId");
        string? spanId = GetRequiredString(fields, "SpanId");

        // Every required field (per the AccessTelemetryEvent contract) MUST parse; a missing field
        // means the record ToString shape has changed or the line was truncated — refuse to return a
        // half-populated event that would satisfy "ShouldNotBeNull" assertions vacuously.
        if (timestamp is null
            || tenantId is null
            || operationType is null
            || user is null
            || outcome is null
            || traceId is null
            || spanId is null
            || !fields.ContainsKey("QueryParams"))
        {
            return null;
        }

        // QueryParams is rendered by C# record ToString as the type name
        // ("System.Collections.Generic.Dictionary`2[...]") — the actual key/value content is NOT
        // recoverable from ToString. We mark the field as "present" via ObservedFromToString by
        // returning an empty dictionary here, but a test that asserts specific QueryParam values
        // cannot work against the stdout-capture path (ADR-8.4-003 documents this limitation). Use
        // the Tier-2 in-process log capture if you need to assert QueryParam content.
        return new AccessTelemetryEvent
        {
            EventId = eventId,
            SchemaVersion = schemaVersion,
            Timestamp = timestamp,
            TenantId = tenantId,
            OperationType = operationType,
            CaseId = NullIfEmpty(fields.GetValueOrDefault("CaseId")),
            User = user,
            QueryParams = new Dictionary<string, object?>(0),
            ResultCount = resultCount,
            DurationMs = durationMs,
            Outcome = outcome,
            ErrorCode = NullIfEmpty(fields.GetValueOrDefault("ErrorCode")),
            TraceId = traceId,
            SpanId = spanId,
        };
    }

    /// <summary>
    /// Given the position of an opening <c>{</c> in <paramref name="text"/>, returns the matching
    /// closing <c>}</c> index (0-based), honoring nested <c>{}</c> and <c>[]</c> pairs so embedded
    /// dictionary / array notations do not terminate the outer record prematurely. Returns -1 when
    /// no matching close exists (truncated input).
    /// </summary>
    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        int braceDepth = 0;
        int bracketDepth = 0;
        for (int i = openBraceIndex; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
                    braceDepth++;
                    break;
                case '}':
                    braceDepth--;
                    if (braceDepth == 0 && bracketDepth == 0)
                    {
                        return i;
                    }

                    break;
                case '[':
                    bracketDepth++;
                    break;
                case ']':
                    bracketDepth--;
                    break;
            }
        }

        return -1;
    }

    /// <summary>
    /// Tokenizes the body of a C# record <c>ToString()</c> output (already stripped of the outer
    /// <c>{ ... }</c>). Field boundaries are recognized only when the scanner is at depth zero of
    /// nested <c>{}</c> and <c>[]</c>; values can therefore contain commas and even full nested
    /// record forms without being mis-split.
    /// </summary>
    /// <param name="body">Body text — the contents between the outer braces.</param>
    /// <returns>Map of field name to raw value text, preserving the first occurrence per name.</returns>
    private static Dictionary<string, string> TokenizeRecordFields(string body)
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        int cursor = 0;
        while (cursor < body.Length)
        {
            while (cursor < body.Length && (body[cursor] == ' ' || body[cursor] == ','))
            {
                cursor++;
            }

            if (cursor >= body.Length)
            {
                break;
            }

            int nameStart = cursor;
            if (!char.IsUpper(body[cursor]))
            {
                // Not a field boundary token — skip a character and continue scanning.
                cursor++;
                continue;
            }

            while (cursor < body.Length && (char.IsLetterOrDigit(body[cursor]) || body[cursor] == '_'))
            {
                cursor++;
            }

            string name = body[nameStart..cursor];

            // Expect " = " between field name and value.
            while (cursor < body.Length && body[cursor] == ' ')
            {
                cursor++;
            }

            if (cursor >= body.Length || body[cursor] != '=')
            {
                // Name but no '=' — not a field boundary after all; bail on further parsing.
                break;
            }

            cursor++; // consume '='
            while (cursor < body.Length && body[cursor] == ' ')
            {
                cursor++;
            }

            int valueStart = cursor;
            int braceDepth = 0;
            int bracketDepth = 0;
            while (cursor < body.Length)
            {
                char current = body[cursor];
                if (current == '{')
                {
                    braceDepth++;
                }
                else if (current == '}')
                {
                    braceDepth--;
                }
                else if (current == '[')
                {
                    bracketDepth++;
                }
                else if (current == ']')
                {
                    bracketDepth--;
                }
                else if (current == ',' && braceDepth == 0 && bracketDepth == 0)
                {
                    // Look-ahead: a real field boundary starts with ", " followed by an UpperCase identifier
                    // and an '='. Commas inside values (e.g. "Dictionary`2[System.String,System.Object]")
                    // are already shielded by the bracket depth; this safeguards against spaces varying.
                    int look = cursor + 1;
                    while (look < body.Length && body[look] == ' ')
                    {
                        look++;
                    }

                    if (look < body.Length && char.IsUpper(body[look]))
                    {
                        int idStart = look;
                        while (look < body.Length && (char.IsLetterOrDigit(body[look]) || body[look] == '_'))
                        {
                            look++;
                        }

                        int idEnd = look;
                        while (look < body.Length && body[look] == ' ')
                        {
                            look++;
                        }

                        if (idEnd > idStart && look < body.Length && body[look] == '=')
                        {
                            break;
                        }
                    }
                }

                cursor++;
            }

            string value = body[valueStart..cursor].TrimEnd();
            if (!result.ContainsKey(name))
            {
                result[name] = value;
            }
        }

        return result;
    }

    private static string? GetRequiredString(Dictionary<string, string> fields, string name)
        => fields.TryGetValue(name, out string? raw)
            ? NullIfEmpty(raw)
            : null;

    private static bool TryParseRequiredInt(Dictionary<string, string> fields, string name, out int value)
    {
        value = 0;
        return fields.TryGetValue(name, out string? raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseRequiredLong(Dictionary<string, string> fields, string name, out long value)
    {
        value = 0;
        return fields.TryGetValue(name, out string? raw)
            && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseOptionalInt(string? raw, out int? value)
    {
        string? normalized = NullIfEmpty(raw);
        if (normalized is null)
        {
            value = null;
            return true;
        }

        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static string? NullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        return string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "<null>", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed;
    }
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
