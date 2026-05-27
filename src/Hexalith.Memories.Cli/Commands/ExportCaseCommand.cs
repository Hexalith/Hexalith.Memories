// <copyright file="ExportCaseCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;
using System.Globalization;

using Hexalith.Memories.Cli.Errors;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Export;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Story 8.3 — builds <c>memories export case</c>. Streams a case export to stdout or to a file
/// via <c>--output</c> (writes to a <c>.part</c> file + atomic rename on success; deletes the
/// part-file on failure). The CLI's global <c>--format</c> is ignored — the payload is raw JSON.
/// </summary>
public static class ExportCaseCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "export case";

    private const string CommandDescription = """
Export a single case (case record + memory units + graph edges) as portable JSON.

Examples:
    memories export case --tenant acme --case 01HM5Q9WXGK6T8Q4Z5Y6V7W8X9 --output case-1.json
    memories export case --tenant acme --case 01HM5Q9WXGK6T8Q4Z5Y6V7W8X9 | jq .manifest
""";

    private const int StreamBufferSize = 81920;

    /// <summary>Builds the <c>case</c> subcommand.</summary>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string>("--tenant")
        {
            Description = "Tenant identifier (required).",
            Required = true,
        };

        var caseOption = new Option<string>("--case")
        {
            Description = "Case identifier (required, 26-char Crockford ULID).",
            Required = true,
        };

        var outputOption = new Option<string?>("--output")
        {
            Description = "Write the JSON export to this file (default: stdout).",
        };

        var forceOption = new Option<bool>("--force")
        {
            Description = "Allow overwriting an existing --output file.",
        };

        var allowAbsoluteOption = new Option<bool>("--allow-absolute-path")
        {
            Description = "Allow --output paths outside the current working directory (safety opt-in).",
        };

        var command = new Command("case", CommandDescription)
        {
            tenantOption,
            caseOption,
            outputOption,
            forceOption,
            allowAbsoluteOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(caseOption),
            parseResult.GetValue(outputOption),
            parseResult.GetValue(forceOption),
            parseResult.GetValue(allowAbsoluteOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        string? caseId,
        string? outputPath,
        bool force,
        bool allowAbsolutePath,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(caseId))
        {
            CliErrorWriter.Write(
                console,
                CommandName,
                code: "INVALID_INPUT",
                message: "--tenant and --case are required.",
                suggestion: "Run 'memories export case --help' to see required options.");
            return CliExitCodes.Plumbing;
        }

        if (console.Format != OutputFormat.Human)
        {
            console.Error.WriteLine($"warning: --format={console.Format.ToString().ToLowerInvariant()} is ignored for 'export case'; output is raw JSON.");
        }

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            ExportOutputSink? sink = ExportCliHelpers.PrepareOutputSink(console, CommandName, outputPath, force, allowAbsolutePath);
            if (sink is null)
            {
                return CliExitCodes.Plumbing;
            }

            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            try
            {
                await using Stream responseStream = await client.ExportCaseAsync(tenantId!, caseId!, innerCt).ConfigureAwait(false);
                await ExportCliHelpers.StreamToSinkAsync(console, responseStream, sink, StreamBufferSize, innerCt).ConfigureAwait(false);
                sink.Commit();
                return CliExitCodes.Success;
            }
            catch (Exception)
            {
                sink.Abort();
                throw;
            }
        }, ct).ConfigureAwait(false);
    }
}
