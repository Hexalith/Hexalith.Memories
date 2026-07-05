// <copyright file="WorkflowPayloadStoreEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Internal Dapr state value for a claim-checked workflow payload.</summary>
internal sealed record WorkflowPayloadStoreEntry(
    WorkflowPayloadReference Reference,
    byte[] Payload,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
