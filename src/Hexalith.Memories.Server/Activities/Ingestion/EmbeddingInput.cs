// <copyright file="EmbeddingInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Input for the embedding generation activity.</summary>
/// <param name="TenantId">The tenant identifier for rate limiting and secret scoping.</param>
/// <param name="ContentText">The extracted text content to generate embeddings for.</param>
public sealed record EmbeddingInput(
    string TenantId,
    string ContentText);
