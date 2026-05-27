// <copyright file="ExportCliHelpers.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Export;

using System.Globalization;

using Hexalith.Memories.Cli.Errors;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output.Formatters;

/// <summary>Shared helpers for the CLI export commands (Story 8.3).</summary>
internal static class ExportCliHelpers
{
    /// <summary>
    /// Validates <paramref name="outputPath"/>, opens the part-file (or stdout), and returns the
    /// prepared sink. Returns <see langword="null"/> after writing a CLI error when the path is
    /// invalid, missing <c>--force</c>, or escapes the working directory without opt-in.
    /// </summary>
    public static ExportOutputSink? PrepareOutputSink(
        CliConsole console,
        string commandName,
        string? outputPath,
        bool force,
        bool allowAbsolutePath)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return ExportOutputSink.ForStdout(Console.OpenStandardOutput());
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(outputPath);
        }
        catch (ArgumentException ex)
        {
            CliErrorWriter.Write(
                console,
                commandName,
                code: "EXPORT_OUTPUT_PATH_INVALID",
                message: $"Output path '{outputPath}' is not a valid filesystem path: {ex.Message}",
                suggestion: ErrorMessageCatalog.Resolve("EXPORT_OUTPUT_PATH_INVALID").CliSuggestion!);
            return null;
        }

        if (!allowAbsolutePath)
        {
            string cwd = Path.GetFullPath(Environment.CurrentDirectory);
            string cwdWithSep = cwd.EndsWith(Path.DirectorySeparatorChar) ? cwd : cwd + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(cwdWithSep, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fullPath, cwd, StringComparison.OrdinalIgnoreCase))
            {
                CliErrorWriter.Write(
                    console,
                    commandName,
                    code: "EXPORT_OUTPUT_PATH_INVALID",
                    message: $"Output path '{outputPath}' resolves outside the current working directory.",
                    suggestion: "Use --allow-absolute-path to write outside the current working directory, or pick a relative path.");
                return null;
            }
        }

        string? parentDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            CliErrorWriter.Write(
                console,
                commandName,
                code: "EXPORT_OUTPUT_PATH_INVALID",
                message: $"Parent directory of '{outputPath}' does not exist.",
                suggestion: "Create the parent directory and retry, or choose a different --output path.");
            return null;
        }

        if (File.Exists(fullPath) && !force)
        {
            CliErrorWriter.Write(
                console,
                commandName,
                code: "EXPORT_OUTPUT_PATH_INVALID",
                message: $"Output file '{outputPath}' already exists.",
                suggestion: "Re-run with --force to overwrite, or choose a different --output path.");
            return null;
        }

        try
        {
            return ExportOutputSink.ForFile(fullPath, force);
        }
        catch (IOException ex)
        {
            CliErrorWriter.Write(
                console,
                commandName,
                code: "EXPORT_WRITE_FAILED",
                message: $"Failed to open output file '{outputPath}': {ex.Message}",
                suggestion: ErrorMessageCatalog.Resolve("EXPORT_WRITE_FAILED").CliSuggestion!);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            CliErrorWriter.Write(
                console,
                commandName,
                code: "EXPORT_WRITE_FAILED",
                message: $"Access denied writing '{outputPath}': {ex.Message}",
                suggestion: ErrorMessageCatalog.Resolve("EXPORT_WRITE_FAILED").CliSuggestion!);
            return null;
        }
    }

    /// <summary>Pipes <paramref name="source"/> into <paramref name="sink"/> with byte-count progress on stderr.</summary>
    public static async Task StreamToSinkAsync(
        CliConsole console,
        Stream source,
        ExportOutputSink sink,
        int bufferSize,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sink);

        CountingStream counter = new(
            sink.Stream,
            bytesWritten =>
            {
                double mb = bytesWritten / (1024d * 1024d);
                console.Error.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"Exported {mb:F2} MB"));
            },
            leaveOpen: true);

        await source.CopyToAsync(counter, bufferSize, ct).ConfigureAwait(false);
        await counter.FlushAsync(ct).ConfigureAwait(false);
    }
}
