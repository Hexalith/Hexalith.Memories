// <copyright file="TenantProvisioningResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Result of the tenant provisioning workflow.</summary>
public sealed record TenantProvisioningResult(
    string TenantId,
    TenantStatus Status,
    string Message)
{
    /// <summary>Gets the machine-readable error code, when provisioning does not succeed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; init; }

    /// <summary>Gets the list of backends that were cleaned up during compensation, if provisioning failed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? CompensatedBackends { get; init; }
}
