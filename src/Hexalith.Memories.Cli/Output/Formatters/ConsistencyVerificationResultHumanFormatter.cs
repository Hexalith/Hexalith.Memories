// <copyright file="ConsistencyVerificationResultHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Human-readable rendering of <see cref="ConsistencyVerificationResult"/>.</summary>
public sealed class ConsistencyVerificationResultHumanFormatter : IOutputFormatter<ConsistencyVerificationResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(ConsistencyVerificationResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("Consistency verification completed.");
        writer.WriteLine($"  tenant:              {value.TenantId}");
        writer.WriteLine($"  total units:         {value.TotalUnits.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  consistent units:    {value.ConsistentCount.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  inconsistent units:  {value.InconsistentCount.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  discrepancy records: {value.Discrepancies.Count.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"  enumeration cut off: {value.EnumerationTruncated}");
        writer.WriteLine($"  started:             {value.StartedAt:O}");
        writer.WriteLine($"  completed:           {value.CompletedAt:O}");
        writer.WriteLine($"  duration:            {value.Duration}");

        if (value.Discrepancies.Count == 0)
        {
            writer.WriteLine("  discrepancies:       none");
            return;
        }

        writer.WriteLine("Discrepancies:");
        foreach (ConsistencyDiscrepancy discrepancy in value.Discrepancies)
        {
            writer.WriteLine($"  - {discrepancy.MemoryUnitId}: {discrepancy.Recommendation}");
        }
    }
}