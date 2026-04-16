// <copyright file="TenantListJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Emits <c>tenant list</c> data inside the CLI JSON envelope.</summary>
public sealed class TenantListJsonFormatter : IOutputFormatter<IReadOnlyList<TenantSummary>>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(IReadOnlyList<TenantSummary> value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        JsonEnvelopeWriter.Write(writer, "tenant list", value);
    }
}
