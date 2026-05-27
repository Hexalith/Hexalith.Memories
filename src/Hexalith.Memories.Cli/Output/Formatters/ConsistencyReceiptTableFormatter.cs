// <copyright file="ConsistencyReceiptTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Commands;

/// <summary>Table rendering of the scheduling receipt.</summary>
public sealed class ConsistencyReceiptTableFormatter : IOutputFormatter<ConsistencyCommandReceipt>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(ConsistencyCommandReceipt value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        TableWriter.Write(
            writer,
            ["kind", "tenant", "workflowInstanceId", "statusUrl"],
            [[value.Kind, value.TenantId, value.WorkflowInstanceId, value.StatusUrl.ToString()]]);
    }
}
