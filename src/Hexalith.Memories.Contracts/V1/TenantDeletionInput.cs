// <copyright file="TenantDeletionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for the tenant deletion workflow and its activities.</summary>
public sealed record TenantDeletionInput
{
    /// <summary>Initializes a new instance of the <see cref="TenantDeletionInput"/> class.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    public TenantDeletionInput(string tenantId)
    {
        TenantId = TenantIdContractValidator.Validate(tenantId);
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; init; }
}
