// <copyright file="ConsistencyWorkflowStateJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Contracts.V1;

/// <summary>JSON envelope rendering of <see cref="ConsistencyWorkflowState"/>.</summary>
public sealed class ConsistencyWorkflowStateJsonFormatter : IOutputFormatter<ConsistencyWorkflowState>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(ConsistencyWorkflowState value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        // The workflow state is shared between verify and repair; emit under the verify command
        // (both commands call this formatter, and the command name on the envelope is primarily
        // used for JSON-log correlation).
        JsonEnvelopeWriter.Write(writer, ConsistencyVerifyCommand.CommandName, value);
    }
}
