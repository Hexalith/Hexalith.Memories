// <copyright file="MemoryUnitIdLookupJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Emits a <see cref="MemoryUnitIdLookupResponse"/> inside the CLI JSON envelope (Story 18.5).</summary>
public sealed class MemoryUnitIdLookupJsonFormatter : IOutputFormatter<MemoryUnitIdLookupResponse>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(MemoryUnitIdLookupResponse value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        JsonEnvelopeWriter.Write(writer, "search lookup", value);
    }
}
