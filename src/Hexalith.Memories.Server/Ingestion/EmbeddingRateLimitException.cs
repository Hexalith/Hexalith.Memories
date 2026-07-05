// <copyright file="EmbeddingRateLimitException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Thrown when the embedding API rate limit is exceeded for a tenant.</summary>
public class EmbeddingRateLimitException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingRateLimitException"/> class.</summary>
    /// <param name="tenantId">The tenant identifier that hit the rate limit.</param>
    public EmbeddingRateLimitException(string tenantId)
        : base($"Embedding rate limit exceeded for tenant '{tenantId}'.")
    {
        TenantId = tenantId;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingRateLimitException"/> class for a provider 429.</summary>
    /// <param name="tenantId">The tenant identifier that hit the provider rate limit.</param>
    /// <param name="retryAfterSeconds">The effective retry-after delay in seconds.</param>
    public EmbeddingRateLimitException(string tenantId, int retryAfterSeconds)
        : base(EmbeddingRateLimitRetryAfter.AppendProviderMarker(
            $"Embedding provider rate limit exceeded for tenant '{tenantId}'.",
            retryAfterSeconds))
    {
        TenantId = tenantId;
        RetryAfterSeconds = EmbeddingRateLimitRetryAfter.NormalizeSeconds(retryAfterSeconds);
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingRateLimitException"/> class.</summary>
    public EmbeddingRateLimitException()
        : base()
    {
        TenantId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingRateLimitException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EmbeddingRateLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
        TenantId = string.Empty;
    }

    /// <summary>Gets the tenant identifier that hit the rate limit.</summary>
    public string TenantId { get; }

    /// <summary>Gets the value parsed from the provider's <c>Retry-After</c> response header (Story 6.2).
    /// <c>0</c> means the header was absent, unparseable, or referred to a past HTTP-date — in which case
    /// the activity defaults to a 30 s pause. Positive values are already clamped to <c>[1, 3600]</c> at
    /// the HTTP boundary.</summary>
    public int RetryAfterSeconds { get; init; }
}
