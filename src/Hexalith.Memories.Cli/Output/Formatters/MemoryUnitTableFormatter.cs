// <copyright file="MemoryUnitTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Two-table rendering of a <see cref="MemoryUnit"/>: core fields then metadata fields.</summary>
public sealed class MemoryUnitTableFormatter : IOutputFormatter<MemoryUnit>
{
    private const string DateTimeRoundTripFormat = "o";

    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(MemoryUnit value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        var coreRows = new IReadOnlyList<string>[]
        {
            new[] { "id", value.Id },
            new[] { "tenantId", value.TenantId },
            new[] { "caseId", value.CaseId },
            new[] { "sourceUri", value.SourceUri },
            new[] { "ingestedBy", value.IngestedBy },
            new[] { "ingestedAt", value.IngestedAt.ToString(DateTimeRoundTripFormat, CultureInfo.InvariantCulture) },
            new[] { "status", value.Status.ToString() },
        };
        TableWriter.Write(writer, new[] { "FIELD", "VALUE" }, coreRows);

        writer.WriteLine();

        if (value.Metadata.Count == 0)
        {
            // Task 7.4 carve-out mirrored from the human formatter — emitting a bare header + separator for
            // zero metadata rows is the "looks broken" surface red-team flagged.
            writer.WriteLine("metadata: (none)");
            return;
        }

        var metaRows = new List<IReadOnlyList<string>>(value.Metadata.Count);
        foreach (KeyValuePair<string, MetadataField> kv in value.Metadata)
        {
            string origin = kv.Value.Origin == MetadataOrigin.Human ? "human" : "ai";
            string confidence = kv.Value.Confidence.ToString("F2", CultureInfo.InvariantCulture);
            metaRows.Add(new[] { kv.Key, kv.Value.Value, origin, confidence });
        }

        TableWriter.Write(writer, new[] { "KEY", "VALUE", "ORIGIN", "CONFIDENCE" }, metaRows);
    }
}
