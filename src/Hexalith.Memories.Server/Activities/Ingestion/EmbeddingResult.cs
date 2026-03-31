// <copyright file="EmbeddingResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Result of embedding generation containing the vector and provider metadata.</summary>
/// <param name="Vector">The embedding vector with dimensions matching the tenant's configured model.</param>
/// <param name="Provider">The embedding provider identifier (e.g. "google:gemini-embedding-001").</param>
/// <param name="Dimensions">The number of dimensions in the vector.</param>
public sealed record EmbeddingResult(
    float[] Vector,
    string Provider,
    int Dimensions);
