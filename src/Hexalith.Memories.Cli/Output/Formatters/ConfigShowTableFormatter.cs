// <copyright file="ConfigShowTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Output.Json;

/// <summary>Two-column table rendering of <c>config show</c>.</summary>
public sealed class ConfigShowTableFormatter : IOutputFormatter<ConfigShowData>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(ConfigShowData value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        var rows = new[]
        {
            (IReadOnlyList<string>)new[] { "endpoint", value.Endpoint },
            (IReadOnlyList<string>)new[] { "resolvedBy", value.ResolvedBy },
            (IReadOnlyList<string>)new[] { "tokenConfigured", value.TokenConfigured ? "true" : "false" },
        };
        TableWriter.Write(writer, new[] { "KEY", "VALUE" }, rows);
    }
}
