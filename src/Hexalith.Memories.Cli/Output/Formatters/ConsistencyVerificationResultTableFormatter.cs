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

        // S6-D6 (re-review 2026-04-25): the `discrepancies` column shows the in-payload list count
        // (truncatable) so it stays consistent with `notes` (also in-payload). Operators detect
        // truncation by reading `TotalDiscrepancyCount` / `TotalNoteCount` in the JSON output or by
        // observing log events 8204 / 8210.
        TableWriter.Write(
            writer,
            ["tenant", "totalUnits", "consistent", "inconsistent", "notes", "discrepancies"],
            [[
                value.TenantId,
                value.TotalUnits.ToString(CultureInfo.InvariantCulture),
                value.ConsistentCount.ToString(CultureInfo.InvariantCulture),
                value.InconsistentCount.ToString(CultureInfo.InvariantCulture),
                value.NoteCount.ToString(CultureInfo.InvariantCulture),
                value.Discrepancies.Count.ToString(CultureInfo.InvariantCulture),
            ]]);
    }
}