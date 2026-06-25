// <copyright file="MemoryUnitIdLookupHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Renders a <see cref="MemoryUnitIdLookupResponse"/> for interactive viewing (Story 18.5).</summary>
public sealed class MemoryUnitIdLookupHumanFormatter : IOutputFormatter<MemoryUnitIdLookupResponse>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(MemoryUnitIdLookupResponse value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine($"memoryUnitId={value.MemoryUnitId}");
    }
}
