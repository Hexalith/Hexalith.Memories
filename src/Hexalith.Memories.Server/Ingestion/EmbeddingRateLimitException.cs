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
}
