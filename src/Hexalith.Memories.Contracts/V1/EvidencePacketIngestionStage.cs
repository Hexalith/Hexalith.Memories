// <copyright file="EvidencePacketIngestionStage.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Stable ingestion stage taxonomy exposed by Evidence Packet source metadata.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EvidencePacketIngestionStage>))]
public enum EvidencePacketIngestionStage
{
    /// <summary>The producer cannot determine the ingestion stage.</summary>
    Unknown = 0,

    /// <summary>The content was received by the ingestion surface.</summary>
    Received,

    /// <summary>The content is being fetched.</summary>
    Fetching,

    /// <summary>The content is being extracted.</summary>
    Extracting,

    /// <summary>The content is being chunked.</summary>
    Chunking,

    /// <summary>The content is being embedded.</summary>
    Embedding,

    /// <summary>The content is being indexed.</summary>
    Indexing,

    /// <summary>The content completed ingestion.</summary>
    Completed,

    /// <summary>The content failed ingestion.</summary>
    Failed,

    /// <summary>The content is queued for retry.</summary>
    Retrying,
}
