// <copyright file="ConsistencyInspectionJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Contracts.V1;

/// <summary>JSON envelope rendering of <see cref="ConsistencyInspectionResult"/>.</summary>
public sealed class ConsistencyInspectionJsonFormatter : IOutputFormatter<ConsistencyInspectionResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(ConsistencyInspectionResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        JsonEnvelopeWriter.Write(writer, ConsistencyInspectCommand.CommandName, value);
    }
}
