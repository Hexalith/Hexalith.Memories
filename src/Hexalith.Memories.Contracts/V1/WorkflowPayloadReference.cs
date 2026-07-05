// <copyright file="WorkflowPayloadReference.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Tenant-scoped claim-check reference for ingestion workflow payload data.</summary>
/// <param name="Id">Stable payload identifier unique within the tenant-scoped payload namespace.</param>
/// <param name="Sha256Hash">Lowercase hexadecimal SHA-256 hash of the stored bytes.</param>
/// <param name="ByteLength">Stored byte length.</param>
/// <param name="ContentKind">Payload content kind.</param>
/// <param name="TenantId">Tenant scope for the payload.</param>
/// <param name="MemoryUnitId">Memory-unit scope for the payload.</param>
public sealed record WorkflowPayloadReference(
    string Id,
    string Sha256Hash,
    long ByteLength,
    WorkflowPayloadKind ContentKind,
    string TenantId,
    string MemoryUnitId);
