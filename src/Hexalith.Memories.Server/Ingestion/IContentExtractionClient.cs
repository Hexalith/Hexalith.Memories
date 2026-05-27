// <copyright file="IContentExtractionClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Defines the contract for content extraction via Kreuzberg.</summary>
public interface IContentExtractionClient
{
    /// <summary>Extracts text content from raw file bytes using Kreuzberg.</summary>
    /// <param name="input">The extraction input containing file bytes and metadata.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The extraction result containing extracted text, content hash, and timestamp.</returns>
    Task<ExtractionResult> ExtractAsync(ExtractionInput input, CancellationToken cancellationToken = default);
}
