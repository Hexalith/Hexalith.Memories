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
    /// <summary>Gets the list of retrieval-axis deletion steps completed successfully.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("deletedBackends")]
    public IReadOnlyList<string>? DeletedAxes { get; init; }
}
