// <copyright file="AccessTelemetryStateStoreTestRecords.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using System.Globalization;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Deterministic record and expiry-entry builders shared by the state-adapter tests.</summary>
internal static class AccessTelemetryStateStoreTestRecords
{
    /// <summary>Gets the canonical expiry instant used by the state-adapter tests.</summary>
    public static DateTimeOffset Expiry { get; } = new(2026, 7, 20, 12, 30, 0, TimeSpan.Zero);

    /// <summary>Builds the minute/shard expiry entry that matches a record.</summary>
    public static AccessTelemetryExpiryEntry CreateEntry(AccessTelemetryRecord record)
        => new(
            record.RecordId,
            AccessTelemetryExpiryIndex.GetExpiryMinute(DateTimeOffset.Parse(record.ExpiresAtUtc, CultureInfo.InvariantCulture)),
            AccessTelemetryExpiryIndex.GetShard(record.RecordId),
            record.EnvelopeHash,
            record.ExpiresAtUtc);

    /// <summary>
    /// Builds a canonical, hash-sealed record for the supplied identifier and expiry.
    /// <paramref name="durationMs"/> is the contract-valid way to vary the sealed envelope while
    /// holding the identifier and expiry fixed; <c>OperationType</c> cannot be varied on its own
    /// because the canonicalizer binds it to <c>EventId</c>.
    /// </summary>
    public static AccessTelemetryRecord CreateRecord(
        string recordId,
        DateTimeOffset? expiry = null,
        int durationMs = 42,
        char tenantMarkerFill = 'a')
    {
        AccessTelemetryRecord record = new()
        {
            AcceptedAtUtc = Format(Expiry.AddHours(-1)),
            DurationMs = durationMs,
            EmittedAtUtc = Format(Expiry.AddHours(-1)),
            EnvelopeHash = string.Empty,
            EventId = 7501,
            ExpiresAtUtc = Format(expiry ?? Expiry),
            MarkerKeyId = "mk-2026a",
            OperationType = "search",
            Outcome = "ok",
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "all-authorized",
                ["explain"] = false,
                ["queryLengthBucket"] = "33-128",
                ["subjectPresent"] = true,
                ["weightProfile"] = "configured",
            },
            RecordId = recordId,
            ResultCount = 1,
            SchemaVersion = 1,
            TenantMarker = new string(tenantMarkerFill, 64),
        };
        return record with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(record) };
    }

    /// <summary>Formats an instant with the canonical millisecond UTC contract shape.</summary>
    public static string Format(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    /// <summary>Gets the record state key an adapter derives for an identifier.</summary>
    public static string RecordKey(string recordId)
        => $"records/{AccessTelemetryExpiryIndex.GetShard(recordId):D2}/{recordId}";

    /// <summary>Gets the expiry-bucket state key an adapter derives for an entry.</summary>
    public static string BucketKey(AccessTelemetryExpiryEntry entry)
        => string.Create(CultureInfo.InvariantCulture, $"expiry-bucket/{entry.ExpiryMinute:D12}/{entry.Shard:D2}");
}
