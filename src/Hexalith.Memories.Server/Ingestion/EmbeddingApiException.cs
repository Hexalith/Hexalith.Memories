// <copyright file="EmbeddingApiException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Thrown when the embedding API returns an error or an unexpected response.</summary>
public class EmbeddingApiException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingApiException"/> class.</summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    /// <param name="responseBody">The response body from the API.</param>
    /// <param name="tenantId">The tenant identifier for context.</param>
    public EmbeddingApiException(int statusCode, string responseBody, string tenantId)
        : base($"Embedding API error (HTTP {statusCode}) for tenant '{tenantId}': {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        TenantId = tenantId;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingApiException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="tenantId">The tenant identifier for context.</param>
    public EmbeddingApiException(string message, string tenantId)
        : base(message)
    {
        TenantId = tenantId;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingApiException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="tenantId">The tenant identifier for context.</param>
    /// <param name="innerException">The inner exception.</param>
    public EmbeddingApiException(string message, string tenantId, Exception innerException)
        : base(message, innerException)
    {
        TenantId = tenantId;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingApiException"/> class.</summary>
    public EmbeddingApiException()
        : base()
    {
        TenantId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingApiException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EmbeddingApiException(string message, Exception innerException)
        : base(message, innerException)
    {
        TenantId = string.Empty;
    }

    /// <summary>Gets the response body from the API.</summary>
    public string? ResponseBody { get; }

    /// <summary>Gets the HTTP status code returned by the API.</summary>
    public int? StatusCode { get; }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; }
}
