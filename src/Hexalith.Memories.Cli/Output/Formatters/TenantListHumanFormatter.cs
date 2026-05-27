// <copyright file="TenantListHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Byte-for-byte Story 7.1 reproduction of the <c>tenant list</c> human output.</summary>
public sealed class TenantListHumanFormatter : IOutputFormatter<IReadOnlyList<TenantSummary>>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(IReadOnlyList<TenantSummary> value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        if (value.Count == 0)
        {
            writer.WriteLine("No tenants found.");
            return;
        }

        foreach (TenantSummary tenant in value)
        {
            writer.WriteLine($"{tenant.Id}\t{tenant.DisplayName}");
        }
    }
}
