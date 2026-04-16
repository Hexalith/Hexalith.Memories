// <copyright file="SearchInspectCommand.cs" company="ITANEO">
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

/// <summary>Builds <c>memories search inspect</c> — surfaces metadata origin and confidence (FR64).</summary>
public static class SearchInspectCommand
{
    private const string CommandDescription = """
Inspect a single memory unit, including metadata origin (human/ai) and confidence (FR64).

Example:
    memories search inspect --tenant acme --case case-123 --id mu-abc
""";

    /// <summary>Builds the <c>inspect</c> subcommand.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string>("--tenant") { Description = "Tenant identifier.", Required = true };
        var caseOption = new Option<string>("--case") { Description = "Case identifier.", Required = true };
        var idOption = new Option<string>("--id") { Description = "Memory unit identifier.", Required = true };

        var command = new Command("inspect", CommandDescription)
        {
            tenantOption,
            caseOption,
            idOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(caseOption),
            parseResult.GetValue(idOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        string? caseId,
        string? memoryUnitId,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(caseId)
            || string.IsNullOrWhiteSpace(memoryUnitId))
        {
            CliErrorWriter.Write(
                console,
                CommandName,
                code: "INVALID_INPUT",
                message: "--tenant, --case, and --id are required.",
                suggestion: "Run 'memories search inspect --help' to see required options.");
            return CliExitCodes.Plumbing;
        }

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            MemoryUnit unit = await client.GetMemoryUnitAsync(tenantId!, caseId!, memoryUnitId!, innerCt).ConfigureAwait(false);
            router.Write(console.Format, unit, console.Out);
            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }

    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "search inspect";
}
