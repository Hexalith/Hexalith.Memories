// <copyright file="WorkflowPayloadStoreOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Options for transient ingestion workflow claim-check payloads.</summary>
public sealed class WorkflowPayloadStoreOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ingestion:WorkflowPayloadStore";

    /// <summary>Gets or sets the Dapr state store name.</summary>
    public string StateStoreName { get; set; } = "statestore";

    /// <summary>Gets or sets the transient payload time-to-live in hours.</summary>
    public int TtlHours { get; set; } = 24;
}
