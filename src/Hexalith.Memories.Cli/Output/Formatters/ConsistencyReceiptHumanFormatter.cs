// <copyright file="ConsistencyReceiptHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Commands;

/// <summary>Plain-text receipt printed when <c>--wait</c> is NOT supplied.</summary>
public sealed class ConsistencyReceiptHumanFormatter : IOutputFormatter<ConsistencyCommandReceipt>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(ConsistencyCommandReceipt value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"Workflow scheduled: {value.Kind}");
        writer.WriteLine($"  tenant:     {value.TenantId}");
        writer.WriteLine($"  instanceId: {value.WorkflowInstanceId}");
        writer.WriteLine($"  status URL: {value.StatusUrl}");
    }
}
