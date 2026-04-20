// <copyright file="ExportTenantCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Export;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Story 8.3 — builds <c>memories export tenant</c>. Streams a tenant export to stdout or to a
/// file via <c>--output</c> with the same atomic-write semantics as <c>export case</c>.
/// </summary>
public static class ExportTenantCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "export tenant";

    private const string CommandDescription = """
Export a full tenant (config + cases + memory units + graph edges) as portable JSON.

Examples:
    memories export tenant --tenant acme --output tenant.json
    memories export tenant --tenant acme | jq .manifest
""";

    private const int StreamBufferSize = 81920;

    /// <summary>Builds the <c>tenant</c> subcommand.</summary>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string>("--tenant")
        {
            Description = "Tenant identifier (required).",
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

        var command = new Command("tenant", CommandDescription)
        {
            tenantOption,
            outputOption,
            forceOption,
            allowAbsoluteOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(outputOption),
            parseResult.GetValue(forceOption),
            parseResult.GetValue(allowAbsoluteOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        string? outputPath,
        bool force,
        bool allowAbsolutePath,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            CliErrorWriter.Write(
                console,
                CommandName,
                code: "INVALID_INPUT",
                message: "--tenant is required.",
                suggestion: "Run 'memories export tenant --help' to see required options.");
            return CliExitCodes.Plumbing;
        }

        if (console.Format != OutputFormat.Human)
        {
            console.Error.WriteLine($"warning: --format={console.Format.ToString().ToLowerInvariant()} is ignored for 'export tenant'; output is raw JSON.");
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
                await using Stream responseStream = await client.ExportTenantAsync(tenantId!, innerCt).ConfigureAwait(false);
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
