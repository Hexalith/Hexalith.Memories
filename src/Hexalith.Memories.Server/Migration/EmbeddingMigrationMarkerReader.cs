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
    public static async Task<EmbeddingMigrationMarker?> ReadActiveMarkerAsync(
        IDatabase db,
        string tenantId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ct.ThrowIfCancellationRequested();

        HashEntry[]? entries = await db.HashGetAllAsync(GetActiveMarkerKey(tenantId)).WaitAsync(ct).ConfigureAwait(false);
        if (entries is null || entries.Length == 0)
        {
            return null;
        }

        Dictionary<string, string> values = entries.ToDictionary(
            e => e.Name.ToString(),
            e => e.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        if (!values.TryGetValue("status", out string? status)
            || string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)
            || !values.TryGetValue("targetProvider", out string? provider)
            || !values.TryGetValue("targetModel", out string? model)
            || !values.TryGetValue("targetDimensions", out string? dimensionsText)
            || !int.TryParse(dimensionsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dimensions))
        {
            return null;
        }

        string markerTenant = values.GetValueOrDefault("tenantId") ?? tenantId;
        EmbeddingMigrationMarker marker = new(markerTenant, provider, model, dimensions, status);
        return marker.IsActive ? marker : null;
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

        string normalizedProvider = NormalizeProvider(provider);
        if (string.Equals(normalizedProvider, marker.TargetProvider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(model, marker.TargetModel, StringComparison.OrdinalIgnoreCase)
            && dimensions == marker.TargetDimensions)
        {
            return;
        }

        throw new EmbeddingMigrationWriteBlockedException(
            marker.TenantId,
            marker.TargetProvider,
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
        int separator = provider.IndexOf(':', StringComparison.Ordinal);
        return separator > 0 ? provider[..separator] : provider;
    }
}
