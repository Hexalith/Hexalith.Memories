// <copyright file="ConsistencyRepairResultTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Table rendering of <see cref="ConsistencyRepairResult"/>.</summary>
public sealed class ConsistencyRepairResultTableFormatter : IOutputFormatter<ConsistencyRepairResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(ConsistencyRepairResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        TableWriter.Write(
            writer,
            ["tenant", "passes", "totalDiscrepancies", "repaired", "unrepairable", "actions"],
            [[
                value.TenantId,
                value.PassesExecuted.ToString(CultureInfo.InvariantCulture),
                value.TotalDiscrepancies.ToString(CultureInfo.InvariantCulture),
                value.RepairedCount.ToString(CultureInfo.InvariantCulture),
                value.UnrepairableCount.ToString(CultureInfo.InvariantCulture),
                value.Actions.Count.ToString(CultureInfo.InvariantCulture),
            ]]);
    }
}