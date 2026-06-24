// <copyright file="MemoriesCommandKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Story 17.3 command actions exposed by the Memories command surface.</summary>
public enum MemoriesCommandKind
{
    /// <summary>Run or refine a search.</summary>
    Search = 0,

    /// <summary>Open ingestion.</summary>
    Ingest,

    /// <summary>Inspect the selected source.</summary>
    InspectSource,

    /// <summary>Verify tenant and case scope.</summary>
    VerifyTenant,

    /// <summary>Open graph context.</summary>
    OpenGraph,

    /// <summary>Retry ingestion.</summary>
    RetryIngestion,

    /// <summary>Export the sanitized packet.</summary>
    ExportPacket,

    /// <summary>Inspect the MCP payload.</summary>
    InspectMcpPayload,
}
