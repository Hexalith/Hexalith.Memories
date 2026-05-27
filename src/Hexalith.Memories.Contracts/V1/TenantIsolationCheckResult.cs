// <copyright file="TenantIsolationCheckResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Result of a single tenant isolation check.</summary>
/// <param name="CheckName">The name of the check that was executed.</param>
/// <param name="Passed">Whether the check passed.</param>
/// <param name="DurationMs">The duration of the check in milliseconds.</param>
public sealed record TenantIsolationCheckResult(
    string CheckName,
    bool Passed,
    double DurationMs)
{
    /// <summary>Gets additional details about the check result.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Details { get; init; }

    /// <summary>Gets actionable operator guidance on failure.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Remediation { get; init; }
}
