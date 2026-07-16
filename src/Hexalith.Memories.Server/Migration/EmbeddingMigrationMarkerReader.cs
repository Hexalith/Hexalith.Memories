// <copyright file="EmbeddingMigrationMarkerReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

using StackExchange.Redis;

/// <summary>Reads and enforces the durable tenant-scoped embedding migration marker.</summary>
public static class EmbeddingMigrationMarkerReader
{
    private const string ActiveMarkerSuffix = ":embedding-migration:active";

    /// <summary>Reads the active tenant migration marker, when one exists.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The active marker, or <see langword="null"/> when no active marker protects the tenant.</returns>
    public static async Task<EmbeddingMigrationMarker?> ReadActiveMarkerAsync(
        IConnectionMultiplexer redis,
        string tenantId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        IDatabase db = redis.GetDatabase();
        return await ReadActiveMarkerAsync(db, tenantId, ct).ConfigureAwait(false);
    }

    /// <summary>Reads the active tenant migration marker from a Redis database.</summary>
    /// <param name="db">The Redis database.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The active marker, or <see langword="null"/> when no active marker protects the tenant.</returns>
    /// <exception cref="EmbeddingMigrationMarkerCorruptException">Thrown when an active-marker hash exists but is malformed; fails closed rather than silently disabling the guard.</exception>
    public static async Task<EmbeddingMigrationMarker?> ReadActiveMarkerAsync(
        IDatabase db,
        string tenantId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ct.ThrowIfCancellationRequested();

        // F9: DemandMaster avoids replica-lag fail-open between cutover write and replica catch-up.
        HashEntry[]? entries = await db.HashGetAllAsync(GetActiveMarkerKey(tenantId), CommandFlags.DemandMaster).WaitAsync(ct).ConfigureAwait(false);
        if (entries is null || entries.Length == 0)
        {
            return null;
        }

        Dictionary<string, string> values = entries.ToDictionary(
            e => e.Name.ToString(),
            e => e.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        // F5: fail-closed — a present-but-malformed marker hash must not silently disable the guard.
        if (!values.TryGetValue("status", out string? status))
        {
            throw new EmbeddingMigrationMarkerCorruptException(tenantId, "missing status field");
        }

        if (string.Equals(status, MigrationMarkerStatus.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!values.TryGetValue("targetProvider", out string? provider)
            || !values.TryGetValue("targetModel", out string? model)
            || !values.TryGetValue("targetDimensions", out string? dimensionsText)
            || !int.TryParse(dimensionsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dimensions))
        {
            throw new EmbeddingMigrationMarkerCorruptException(tenantId, "missing or unparseable target fields");
        }

        // F14: if the stored tenantId is present and differs from the requested tenant, refuse to construct a marker
        // that would leak a foreign tenant id into exceptions, logs, and telemetry.
        if (values.TryGetValue("tenantId", out string? storedTenant)
            && !string.IsNullOrWhiteSpace(storedTenant)
            && !string.Equals(storedTenant, tenantId, StringComparison.OrdinalIgnoreCase))
        {
            throw new EmbeddingMigrationMarkerCorruptException(
                tenantId,
                $"stored tenantId '{storedTenant}' does not match the requested tenant");
        }

        return new EmbeddingMigrationMarker(tenantId, provider, model, dimensions, status);
    }

    /// <summary>Throws when the attempted write does not match an active tenant migration marker.</summary>
    /// <param name="marker">The active migration marker.</param>
    /// <param name="provider">The attempted provider or provider/model identifier.</param>
    /// <param name="model">The attempted model.</param>
    /// <param name="dimensions">The attempted vector dimensions.</param>
    public static void EnsureWriteMatchesMarker(
        EmbeddingMigrationMarker? marker,
        string provider,
        string model,
        int dimensions)
    {
        if (marker is null)
        {
            return;
        }

        // F12: normalise both sides identically so case, colon-composite, or leading-colon variants do not silently
        // pass the guard while producing case-distinct downstream Redis state.
        string normalizedProvider = NormalizeProvider(provider);
        string normalizedTarget = NormalizeProvider(marker.TargetProvider);
        if (string.Equals(normalizedProvider, normalizedTarget, StringComparison.OrdinalIgnoreCase)
            && string.Equals(model, marker.TargetModel, StringComparison.OrdinalIgnoreCase)
            && dimensions == marker.TargetDimensions)
        {
            return;
        }

        throw new EmbeddingMigrationWriteBlockedException(
            marker.TenantId,
            normalizedTarget,
            marker.TargetModel,
            marker.TargetDimensions,
            normalizedProvider,
            model,
            dimensions);
    }

    /// <summary>Gets the tenant-scoped active marker key.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The Redis key used by runtime ingestion guards.</returns>
    public static string GetActiveMarkerKey(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return tenantId + ActiveMarkerSuffix;
    }

    private static string NormalizeProvider(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        // F12: handle leading-colon explicitly — an input like ":foo" cannot be normalised to a non-empty provider head.
        int separator = provider.IndexOf(':', StringComparison.Ordinal);
        if (separator == 0)
        {
            throw new ArgumentException(
                "Provider must not start with a colon separator; expected a non-empty provider segment.",
                nameof(provider));
        }

        string head = separator > 0 ? provider[..separator] : provider;
        return head.Trim();
    }
}
