// <copyright file="ConfigShowJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Output.Json;

/// <summary>Emits <c>config show</c> data inside the CLI JSON envelope.</summary>
public sealed class ConfigShowJsonFormatter : IOutputFormatter<ConfigShowData>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(ConfigShowData value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        JsonEnvelopeWriter.Write(writer, "config show", value);
    }
}
