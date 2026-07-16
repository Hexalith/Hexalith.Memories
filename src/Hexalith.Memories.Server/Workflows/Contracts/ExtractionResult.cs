// <copyright file="ExtractionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Result of content extraction including hash for deduplication.</summary>
/// <param name="ExtractedContent">The text content extracted from the source.</param>
/// <param name="ContentHash">SHA-256 hash of the extracted content for deduplication.</param>
/// <param name="ExtractedAt">The timestamp when extraction completed.</param>
/// <param name="ExtractedContentReference">Optional claim-check reference for the extracted text.</param>
public sealed record ExtractionResult(
    string ExtractedContent,
    string ContentHash,
    DateTimeOffset ExtractedAt,
    WorkflowPayloadReference? ExtractedContentReference = null);
