// <copyright file="ServerActivityStreamReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System.Globalization;
using System.Text.Json;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;

/// <summary>
/// Reads test-only activity breadcrumbs emitted by a selected Aspire resource through
/// <see cref="Hexalith.Memories.ServiceDefaults.Extensions"/> when
/// <see cref="InMemoryTelemetryEnvironment.EnvVar"/> is enabled.
/// </summary>
internal static class ServerActivityStreamReader
{
    public static async Task<IReadOnlyList<CapturedServerActivity>> ReadAsync(
        AspireIngestionPipelineFixture fixture,
        int logStartIndex,
        int minimumEvents,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Func<CapturedServerActivity, bool>? matchPredicate = null,
        string resourceName = "memories")
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentOutOfRangeException.ThrowIfNegative(logStartIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumEvents);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        TimeSpan pollInterval = TimeSpan.FromMilliseconds(200);

        IReadOnlyList<CapturedServerActivity> latest = [];
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                if (IsAtPollingDeadline(deadline, pollInterval))
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            latest = ScanCapturedLogs(fixture.GetLogEntriesSince(logStartIndex), logStartIndex, resourceName);
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
            catch (OperationCanceledException) when (IsAtPollingDeadline(deadline, pollInterval))
            {
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Caller-initiated cancellation: always rethrow so the caller observes its own
                // cancellation distinguishably from deadline elapse. Deadline-elapse exits via
                // the while-loop predicate (`DateTimeOffset.UtcNow < deadline`), not via this catch.
                throw;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return latest;
    }

    private static bool IsAtPollingDeadline(DateTimeOffset deadline, TimeSpan pollInterval)
        => DateTimeOffset.UtcNow + pollInterval >= deadline;

    /// <summary>Parses activity breadcrumbs from the captured log tail.</summary>
    internal static IReadOnlyList<CapturedServerActivity> ScanCapturedLogs(
        IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> entries,
        int logStartIndex,
        string resourceName = "memories")
    {
        List<CapturedServerActivity> matches = [];
        string resourceSuffix = $".Resources.{resourceName}";
        for (int i = 0; i < entries.Count; i++)
        {
            AspireIngestionPipelineFixture.CapturedLogEntry entry = entries[i];
            if (!string.Equals(entry.Category, resourceName, StringComparison.Ordinal)
                && !entry.Category.EndsWith(resourceSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            string payload = StripAspireLogPrefix(entry.Message);
            if (!payload.StartsWith(InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            string json = payload[InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix.Length..].Trim();
            CapturedServerActivity? captured = TryParse(entry, json);
            if (captured is null)
            {
                throw new InvalidDataException(
                    $"Malformed server activity breadcrumb at captured log index {logStartIndex + i}. "
                    + "Expected JSON with sourceName, operationName, traceId, spanId and kind.");
            }

            matches.Add(captured);
        }

        return matches;
    }

    private static string StripAspireLogPrefix(string message)
    {
        // Aspire formats captured resource lines as "<line>: <timestamp> <message>".
        string payload = message.TrimStart();
        int lineEnd = payload.IndexOf(": ", StringComparison.Ordinal);
        if (lineEnd <= 0 || !int.TryParse(payload.AsSpan(0, lineEnd), NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return payload;
        }

        int timeStart = lineEnd + 2;
        int timeEnd = payload.IndexOf(' ', timeStart);
        return timeEnd > timeStart && DateTimeOffset.TryParse(payload.AsSpan(timeStart, timeEnd - timeStart),
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? payload[(timeEnd + 1)..]
            : payload;
    }

    private static CapturedServerActivity? TryParse(
        AspireIngestionPipelineFixture.CapturedLogEntry entry,
        string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? sourceName = TryReadString(root, "sourceName");
            string? operationName = TryReadString(root, "operationName");
            string? traceId = TryReadString(root, "traceId");
            string? spanId = TryReadString(root, "spanId");
            string? kind = TryReadString(root, "kind");
            if (string.IsNullOrWhiteSpace(sourceName)
                || string.IsNullOrWhiteSpace(operationName)
                || string.IsNullOrWhiteSpace(traceId)
                || string.IsNullOrWhiteSpace(spanId)
                || string.IsNullOrWhiteSpace(kind)
                || (root.TryGetProperty("parentSpanId", out JsonElement parent)
                    && parent.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)))
            {
                return null;
            }

            return new CapturedServerActivity(
                entry.Category,
                entry.Level,
                sourceName,
                operationName,
                traceId,
                spanId,
                TryReadString(root, "parentSpanId"),
                kind,
                json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out JsonElement element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}

/// <summary>Captured activity breadcrumb written by the integration-only activity processor.</summary>
internal sealed record CapturedServerActivity(
    string Category,
    LogLevel Level,
    string SourceName,
    string OperationName,
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string Kind,
    string RawJsonPayload);
