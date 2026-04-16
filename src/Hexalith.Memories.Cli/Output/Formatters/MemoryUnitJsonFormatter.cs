// <copyright file="MemoryUnitJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Emits the full <see cref="MemoryUnit"/> inside the CLI JSON envelope — no transformation.</summary>
public sealed class MemoryUnitJsonFormatter : IOutputFormatter<MemoryUnit>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(MemoryUnit value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        JsonEnvelopeWriter.Write(writer, "search inspect", value);
    }
}
