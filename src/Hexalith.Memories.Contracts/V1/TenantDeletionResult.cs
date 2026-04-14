// <copyright file="TenantDeletionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Result of the tenant deletion workflow.</summary>
public sealed record TenantDeletionResult(
    string TenantId,
    TenantStatus Status,
    string Message)
{
    /// <summary>Gets the list of backends that were successfully cleaned up during deletion.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? DeletedBackends { get; init; }
}
