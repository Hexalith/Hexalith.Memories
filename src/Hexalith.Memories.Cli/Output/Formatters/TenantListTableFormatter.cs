// <copyright file="TenantListTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Tabular rendering of <c>tenant list</c>.</summary>
public sealed class TenantListTableFormatter : IOutputFormatter<IReadOnlyList<TenantSummary>>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(IReadOnlyList<TenantSummary> value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        IEnumerable<IReadOnlyList<string>> rows = value.Select(t => (IReadOnlyList<string>)new[] { t.Id, t.DisplayName });
        TableWriter.Write(writer, new[] { "TENANT ID", "DISPLAY NAME" }, rows);
    }
}
