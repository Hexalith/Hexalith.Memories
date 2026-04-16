// <copyright file="CliGlobalOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

/// <summary>Holds the three global options advertised on the root command (AC #3a / ADR-7.1-005).</summary>
public sealed class CliGlobalOptions
{
    /// <summary>The <c>--endpoint</c> option.</summary>
    public Option<string?> EndpointOption { get; } = new("--endpoint")
    {
        Description = "Override the Memories Server endpoint URL (takes precedence over env var and config file).",
        Recursive = true,
    };

    /// <summary>The <c>--token</c> option.</summary>
    public Option<string?> TokenOption { get; } = new("--token")
    {
        Description = "API token (prefer HEXALITH_MEMORIES_API_TOKEN env var; argv is visible in shell history).",
        Recursive = true,
    };

    /// <summary>The <c>--verbose</c> option.</summary>
    public Option<bool> VerboseOption { get; } = new("--verbose")
    {
        Description = "Emit additional diagnostic output to stderr (exception type / message; never the token).",
        Recursive = true,
    };
}
