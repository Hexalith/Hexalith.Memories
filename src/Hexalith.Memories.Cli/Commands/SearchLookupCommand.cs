// <copyright file="SearchLookupCommand.cs" company="ITANEO">
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
/// Builds <c>memories search lookup</c> — Story 18.5 diagnostic that resolves a source URI to its canonical
/// <c>MemoryUnitId</c> by exact key (not free-text search). Unlike <c>search inspect</c> (which throws on a
/// 404), the underlying client method returns <see langword="null"/> on a miss, so this command has an explicit
/// not-found branch that exits with <see cref="CliExitCodes.NotFound"/>.
/// </summary>
public static class SearchLookupCommand
{
    private const string CommandDescription = """
Resolve a source URI to its canonical memory-unit id by exact key (Story 18.5).

Returns a structured not-found (exit 4) when no committed unit maps to the URI — it never falls back to
free-text search.

Example:
    memories search lookup --tenant acme --case case-123 --source-uri file:///doc.pdf
""";

    /// <summary>Command name used in JSON envelopes and error payloads.</summary>
    public const string CommandName = "search lookup";

    /// <summary>Builds the <c>lookup</c> subcommand.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string>("--tenant") { Description = "Tenant identifier.", Required = true };
        var caseOption = new Option<string>("--case") { Description = "Case identifier.", Required = true };
        var sourceUriOption = new Option<string>("--source-uri") { Description = "Exact source URI to resolve.", Required = true };

        var command = new Command("lookup", CommandDescription)
        {
            tenantOption,
            caseOption,
            sourceUriOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(caseOption),
            parseResult.GetValue(sourceUriOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        string? caseId,
        string? sourceUri,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(caseId)
            || string.IsNullOrWhiteSpace(sourceUri))
        {
            CliErrorWriter.Write(
                console,
                CommandName,
                code: "INVALID_INPUT",
                message: "--tenant, --case, and --source-uri are required.",
                suggestion: "Run 'memories search lookup --help' to see required options.");
            return CliExitCodes.Plumbing;
        }

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            string? memoryUnitId = await client
                .LookupMemoryUnitIdBySourceUriAsync(tenantId!, caseId!, sourceUri!, innerCt)
                .ConfigureAwait(false);

            if (memoryUnitId is null)
            {
                // Structured not-found — distinct from a server-side domain failure (which would have thrown
                // a MemoriesRemoteException that the executor maps to DomainError).
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "MEMORY_UNIT_NOT_FOUND",
                    message: $"No memory unit maps to source URI '{sourceUri}' in case '{caseId}'.",
                    suggestion: "Verify the tenant, case, and source URI; the unit may not be committed yet.");
                return CliExitCodes.NotFound;
            }

            router.Write(console.Format, new MemoryUnitIdLookupResponse { MemoryUnitId = memoryUnitId }, console.Out);
            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }
}
