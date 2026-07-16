// <copyright file="EmbeddingResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Result of embedding generation containing the vector and provider metadata.</summary>
public sealed record EmbeddingResult
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingResult"/> record with positional fields for backward compatibility.</summary>
    /// <param name="vector">The embedding vector with dimensions matching the tenant's configured model.</param>
    /// <param name="provider">The embedding provider identifier (e.g. "google:gemini-embedding-001").</param>
    /// <param name="dimensions">The number of dimensions in the vector.</param>
    public EmbeddingResult(float[] vector, string provider, int dimensions)
    {
        Vector = vector;
        Provider = provider;
        Dimensions = dimensions;
    }

    /// <summary>Gets the embedding vector with dimensions matching the tenant's configured model.</summary>
    public float[] Vector { get; init; }

    /// <summary>Gets the embedding provider identifier (e.g. "google:gemini-embedding-001").</summary>
    public string Provider { get; init; }

    /// <summary>Gets the number of dimensions in the vector.</summary>
    public int Dimensions { get; init; }

    /// <summary>
    /// Gets the embedding model identifier (e.g. "gemini-embedding-001") used to generate this vector.
    /// Nullable to avoid breaking any DAPR-replayed historical workflow state that pre-dates Story 5.5 (FR70).
    /// </summary>
    public string? Model { get; init; }
}
