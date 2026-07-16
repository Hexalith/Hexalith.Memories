// <copyright file="TenantProvisioningInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for the tenant provisioning workflow.</summary>
public sealed record TenantProvisioningInput(string TenantId, string DisplayName)
{
    /// <summary>Gets the number of vector dimensions for the semantic index. Defaults to 768.</summary>
    public int VectorDimensions { get; init; } = 768;
}
