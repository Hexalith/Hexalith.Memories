// <copyright file="CliConsole.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

using Hexalith.Memories.Cli.Output;

/// <summary>Abstraction over stdout/stderr so tests can capture CLI output.</summary>
public sealed class CliConsole
{
    /// <summary>Gets or sets the stdin reader (used by interactive confirmation prompts).</summary>
    public TextReader In { get; set; } = Console.In;

    /// <summary>Gets or sets the stdout writer (command output only — pipe-friendly).</summary>
    public TextWriter Out { get; set; } = Console.Out;

    /// <summary>Gets or sets the stderr writer (errors, diagnostics, cancellation notice).</summary>
    public TextWriter Error { get; set; } = Console.Error;

    /// <summary>Gets or sets the process verbose flag.</summary>
    public bool Verbose { get; set; }

    /// <summary>Gets or sets a value indicating whether stdin behaves like an interactive terminal.</summary>
    public bool IsInteractive { get; set; } = !Console.IsInputRedirected;

    /// <summary>Gets or sets the resolved output format (Story 7.2). Default: <see cref="OutputFormat.Human"/>.</summary>
    public OutputFormat Format { get; set; } = OutputFormat.Human;
}
