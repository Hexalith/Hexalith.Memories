// <copyright file="ConsistencyVerificationResultTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Table rendering of <see cref="ConsistencyVerificationResult"/>.</summary>
public sealed class ConsistencyVerificationResultTableFormatter : IOutputFormatter<ConsistencyVerificationResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(ConsistencyVerificationResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        TableWriter.Write(
            writer,
            ["tenant", "totalUnits", "consistent", "inconsistent", "discrepancies"],
            [[
                value.TenantId,
                value.TotalUnits.ToString(CultureInfo.InvariantCulture),
                value.ConsistentCount.ToString(CultureInfo.InvariantCulture),
                value.InconsistentCount.ToString(CultureInfo.InvariantCulture),
                value.Discrepancies.Count.ToString(CultureInfo.InvariantCulture),
            ]]);
    }
}