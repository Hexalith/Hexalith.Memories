// <copyright file="ConsistencyReceiptJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Commands;

/// <summary>JSON envelope for the scheduling receipt.</summary>
public sealed class ConsistencyReceiptJsonFormatter : IOutputFormatter<ConsistencyCommandReceipt>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(ConsistencyCommandReceipt value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        // Command name is inferred from the receipt's Kind field so the JSON envelope's
        // "command" field correctly reflects "consistency verify" vs. "consistency repair".
        string commandName = value.Kind switch
        {
            "repair" => ConsistencyRepairCommand.CommandName,
            _ => ConsistencyVerifyCommand.CommandName,
        };
        JsonEnvelopeWriter.Write(writer, commandName, value);
    }
}
