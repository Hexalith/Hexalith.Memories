// <copyright file="BatchedGraphDeletionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for a single batch of the FalkorDB graph node deletion activity.</summary>
public sealed record BatchedGraphDeletionInput
{
    /// <summary>Initializes a new instance of the <see cref="BatchedGraphDeletionInput"/> class.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="batchSize">The number of nodes to delete in a single batch.</param>
    /// <param name="batchNumber">The zero-based batch index.</param>
    public BatchedGraphDeletionInput(string tenantId, int batchSize = 500, int batchNumber = 0)
    {
        TenantId = TenantIdContractValidator.Validate(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentOutOfRangeException.ThrowIfNegative(batchNumber);

        BatchSize = batchSize;
        BatchNumber = batchNumber;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; init; }

    /// <summary>Gets the number of nodes to delete in a single batch.</summary>
    public int BatchSize { get; init; } = 500;

    /// <summary>Gets the zero-based batch index.</summary>
    public int BatchNumber { get; init; }
}
