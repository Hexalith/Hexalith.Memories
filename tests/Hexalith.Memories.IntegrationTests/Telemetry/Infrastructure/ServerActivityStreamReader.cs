// <copyright file="ServerActivityStreamReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System.Text.Json;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;

/// <summary>
/// Reads server-side test-only activity breadcrumbs emitted by
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
        Func<CapturedServerActivity, bool>? matchPredicate = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentOutOfRangeException.ThrowIfNegative(logStartIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumEvents);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        TimeSpan pollInterval = TimeSpan.FromMilliseconds(200);

        IReadOnlyList<CapturedServerActivity> latest = [];
        while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
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
            catch (TaskCanceledException)
            {
                break;
            }
        }

        return latest;
    }

    private static IReadOnlyList<CapturedServerActivity> ScanCapturedLogs(
        AspireIngestionPipelineFixture fixture,
        int logStartIndex)
    {
        IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> entries = fixture.GetLogEntriesSince(logStartIndex);
        List<CapturedServerActivity> matches = [];
        foreach (AspireIngestionPipelineFixture.CapturedLogEntry entry in entries)
        {
            if (string.IsNullOrEmpty(entry.Message))
            {
                continue;
            }

            int prefixIndex = entry.Message.IndexOf(InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix, StringComparison.Ordinal);
            if (prefixIndex < 0)
            {
                continue;
            }

            string json = entry.Message[(prefixIndex + InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix.Length)..].Trim();
            CapturedServerActivity? captured = TryParse(entry, json);
            if (captured is not null)
            {
                matches.Add(captured);
            }
        }

        return matches;
    }

    private static CapturedServerActivity? TryParse(
        AspireIngestionPipelineFixture.CapturedLogEntry entry,
        string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            string? sourceName = TryReadString(root, "sourceName");
            string? operationName = TryReadString(root, "operationName");
            string? traceId = TryReadString(root, "traceId");
            string? spanId = TryReadString(root, "spanId");
            string? kind = TryReadString(root, "kind");
            if (string.IsNullOrWhiteSpace(sourceName)
                || string.IsNullOrWhiteSpace(operationName)
                || string.IsNullOrWhiteSpace(traceId)
                || string.IsNullOrWhiteSpace(spanId)
                || string.IsNullOrWhiteSpace(kind))
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

/// <summary>Captured server-side activity breadcrumb written by the integration-only activity processor.</summary>
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