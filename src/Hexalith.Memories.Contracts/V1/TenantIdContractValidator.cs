// <copyright file="TenantIdContractValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.RegularExpressions;

/// <summary>Validates tenant identifiers for cross-project contract types.</summary>
internal static partial class TenantIdContractValidator
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "statestore",
        "memories",
        "dapr",
        "system",
        "admin",
        "default",
        "global",
    };

    /// <summary>Validates a tenant identifier and returns it when valid.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The validated tenant identifier.</returns>
    public static string Validate(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!SafeTenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                $"TenantId '{tenantId}' contains invalid characters. Only alphanumeric and hyphens are allowed.",
                nameof(tenantId));
        }

        if (ReservedNames.Contains(tenantId))
        {
            throw new ArgumentException(
                $"'{tenantId}' is a reserved name and cannot be used as a tenant ID.",
                nameof(tenantId));
        }

        return tenantId;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-]+$")]
    private static partial Regex SafeTenantIdRegex();
}
