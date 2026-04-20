// <copyright file="ConsistencyWorkflowStateTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Table rendering of <see cref="ConsistencyWorkflowState"/>.</summary>
public sealed class ConsistencyWorkflowStateTableFormatter : IOutputFormatter<ConsistencyWorkflowState>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(ConsistencyWorkflowState value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        TableWriter.Write(
            writer,
            ["instanceId", "status", "createdAt", "lastUpdatedAt"],
            [
                [value.InstanceId, value.Status, value.CreatedAt.ToString("O"), value.LastUpdatedAt.ToString("O")],
            ]);
    }
}
