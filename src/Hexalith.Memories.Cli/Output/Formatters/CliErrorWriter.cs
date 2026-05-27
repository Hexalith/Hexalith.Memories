// <copyright file="CliErrorWriter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Json;

/// <summary>
/// Shared formatter for CLI-side errors that do not originate from the server response pipeline
/// (for example: local validation or root-level configuration failures). Keeps human/table and JSON
/// error shapes aligned with <see cref="CliCommandExecutor"/>.
/// </summary>
internal static class CliErrorWriter
{
    /// <summary>
    /// Writes an error using the current console format.
    /// </summary>
    /// <param name="console">The CLI console abstraction.</param>
    /// <param name="commandName">The logical command name for JSON envelopes.</param>
    /// <param name="code">The stable error code.</param>
    /// <param name="message">The rendered error message.</param>
    /// <param name="suggestion">The actionable next-step suggestion.</param>
    public static void Write(CliConsole console, string commandName, string code, string message, string suggestion)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(suggestion);

        if (console.Format == OutputFormat.Json)
        {
            JsonErrorEnvelopeWriter.WriteForCommand(
                console.Out,
                commandName,
                new CliErrorPayload(code, message, suggestion));
            return;
        }

        console.Error.WriteLine($"Error: {code}");
        console.Error.WriteLine($"  {message}");
        if (!string.IsNullOrEmpty(suggestion))
        {
            console.Error.WriteLine($"  Suggestion: {suggestion}");
        }
    }
}
