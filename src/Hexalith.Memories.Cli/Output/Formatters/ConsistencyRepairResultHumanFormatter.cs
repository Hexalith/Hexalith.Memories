// <copyright file="ConsistencyRepairResultHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Human-readable rendering of <see cref="ConsistencyRepairResult"/>.</summary>
public sealed class ConsistencyRepairResultHumanFormatter : IOutputFormatter<ConsistencyRepairResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(ConsistencyRepairResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("Consistency repair completed.");
        writer.WriteLine($"  tenant:                {value.TenantId}");
        writer.WriteLine($"  repair passes:         {value.PassesExecuted}");
        writer.WriteLine($"  total discrepancies:   {value.TotalDiscrepancies.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  repaired count:        {value.RepairedCount.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  unrepairable count:    {value.UnrepairableCount.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  action records:        {value.Actions.Count.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  started:               {value.StartedAt:O}");
        writer.WriteLine($"  completed:             {value.CompletedAt:O}");
        writer.WriteLine($"  duration:              {value.Duration}");

        if (value.Actions.Count == 0)
        {
            writer.WriteLine("  actions:               none");
            return;
        }

        writer.WriteLine("Actions:");
        foreach (RepairActionRecord action in value.Actions)
        {
            string outcome = action.Succeeded ? "succeeded" : action.FailureReason ?? "failed";
            writer.WriteLine($"  - {action.MemoryUnitId}: {action.Applied} ({outcome})");
        }
    }
}