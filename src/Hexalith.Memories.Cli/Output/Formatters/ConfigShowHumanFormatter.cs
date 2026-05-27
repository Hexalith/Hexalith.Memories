// <copyright file="ConfigShowHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Output.Json;

/// <summary>
/// Byte-for-byte Story 7.1 reproduction of <c>config show</c> output — three key=value lines in fixed
/// order. Any drift breaks AC #1 / ADR-7.2-002.
/// </summary>
public sealed class ConfigShowHumanFormatter : IOutputFormatter<ConfigShowData>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(ConfigShowData value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"endpoint={value.Endpoint}");
        writer.WriteLine($"resolvedBy={value.ResolvedBy}");
        writer.WriteLine($"tokenConfigured={(value.TokenConfigured ? "true" : "false")}");
    }
}
