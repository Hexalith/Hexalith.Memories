// <copyright file="ExtractionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for content extraction via Kreuzberg.</summary>
/// <param name="SourceUri">The URI identifying the content source.</param>
/// <param name="ContentBytes">The raw file bytes to extract text from.</param>
/// <param name="ContentType">The MIME type of the content (e.g. application/pdf).</param>
/// <param name="SourceType">The origin type of the ingested content.</param>
public sealed record ExtractionInput(
    string SourceUri,
    byte[] ContentBytes,
    string ContentType,
    SourceType SourceType);
