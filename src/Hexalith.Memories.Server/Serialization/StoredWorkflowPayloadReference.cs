// <copyright file="StoredWorkflowPayloadReference.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

/// <summary>Durable claim-check reference associated with a failed ingestion unit.</summary>
internal sealed record StoredWorkflowPayloadReference(
    string Id,
    string Sha256Hash,
    long ByteLength,
    WorkflowPayloadKind ContentKind,
    string TenantId,
    string MemoryUnitId);
