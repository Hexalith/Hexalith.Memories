// <copyright file="ConsistencyInspectCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Story 8.2 — builds <c>memories consistency inspect</c>. Synchronous per-memory-unit
/// probe that reports presence + detail for each of the three backends.
/// </summary>
public static class ConsistencyInspectCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "consistency inspect";

    private const string CommandDescription = """
Inspect consistency for a single memory unit across all three backends.

Example:
    memories consistency inspect --tenant acme --id wf-file-instance-7
""";

    /// <summary>Builds the <c>inspect</c> subcommand.</summary>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string>("--tenant")
        {
            Description = "Tenant identifier (required).",
            Required = true,
        };

        var idOption = new Option<string>("--id")
        {
            Description = "Opaque memory unit identifier; pass the exact value returned by Memories.",
            Required = true,
        };

        var command = new Command("inspect", CommandDescription)
        {
            tenantOption,
            idOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(idOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        string? memoryUnitId,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(memoryUnitId))
        {
            CliErrorWriter.Write(
                console,
                CommandName,
                code: "INVALID_INPUT",
                message: "--tenant and --id are required.",
                suggestion: "Run 'memories consistency inspect --help' to see required options.");
            return CliExitCodes.Plumbing;
        }

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            ConsistencyInspectionResult result = await client
                .InspectConsistencyAsync(tenantId!, memoryUnitId!, innerCt)
                .ConfigureAwait(false);

            router.Write(console.Format, result, console.Out);
            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }
}
