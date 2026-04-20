// <copyright file="ConsistencyWorkflowStateHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Plain-text rendering of <see cref="ConsistencyWorkflowState"/>.</summary>
public sealed class ConsistencyWorkflowStateHumanFormatter : IOutputFormatter<ConsistencyWorkflowState>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(ConsistencyWorkflowState value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"Instance:     {value.InstanceId}");
        writer.WriteLine($"Status:       {value.Status}");
        writer.WriteLine($"Created at:   {value.CreatedAt:O}");
        writer.WriteLine($"Last updated: {value.LastUpdatedAt:O}");
    }
}
