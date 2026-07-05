// <copyright file="WorkflowPayloadKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Classifies claim-checked ingestion workflow payloads.</summary>
public enum WorkflowPayloadKind
{
    /// <summary>Unknown payload kind.</summary>
    Unknown = 0,

    /// <summary>Original non-URL source bytes supplied at scheduling time.</summary>
    SourceBytes = 1,

    /// <summary>Bytes fetched from a URL source.</summary>
    FetchedUrlBytes = 2,

    /// <summary>Extracted textual content.</summary>
    ExtractedText = 3,

    /// <summary>Text for one extracted content chunk.</summary>
    ChunkText = 4,

    /// <summary>Binary single-precision vector bytes for one extracted content chunk.</summary>
    ChunkVector = 5,
}
