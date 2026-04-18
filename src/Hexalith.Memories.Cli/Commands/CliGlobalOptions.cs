// <copyright file="CliGlobalOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

/// <summary>Holds the global options advertised on the root command (ADR-7.1-005 + Story 7.2 <c>--format</c>).</summary>
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

    /// <summary>The <c>--format</c> option (Story 7.2 / ADR-7.2-001). Raw string — validated in <c>ApplyGlobalOptions</c>.</summary>
    public Option<string?> FormatOption { get; } = new("--format")
    {
        Description = "Output format: human (default), json, table.",
        Recursive = true,
    };

    /// <summary>The <c>--telemetry</c> flag (Story 7.5). Opt-in CLI OTLP trace emission; defaults to off.</summary>
    public Option<bool> TelemetryOption { get; } = new("--telemetry")
    {
        Description = "Enable OTLP trace export from the CLI (uses HEXALITH_MEMORIES_OTEL_ENDPOINT when set, otherwise the local Aspire OTLP endpoint http://localhost:18889). Off by default to preserve cold-start latency.",
        Recursive = true,
    };
}
