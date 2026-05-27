// <copyright file="ConsistencyRepairCommand.cs" company="ITANEO">
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
using Microsoft.Extensions.Options;

/// <summary>
/// Story 8.2 — builds <c>memories consistency repair</c>. Repair is a mutating
/// operation: the command requires <c>--yes</c> (or a confirmation prompt on an
/// interactive TTY) before scheduling the workflow.
/// </summary>
public static class ConsistencyRepairCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "consistency repair";

    private const string CommandDescription = """
Schedule a consistency-repair workflow for a tenant. Repair is a MUTATING operation —
requires --yes to skip the interactive confirmation prompt.

Examples:
    memories consistency repair --tenant acme --yes
    memories consistency repair --tenant acme --wait --yes
    memories consistency repair --tenant acme --include-unrepairable --yes
""";

    /// <summary>Builds the <c>repair</c> subcommand.</summary>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string>("--tenant")
        {
            Description = "Tenant identifier (required).",
            Required = true,
        };

        var batchSizeOption = new Option<int?>("--batch-size")
        {
            Description = "Optional per-batch fan-out size (10-5000). Default is 500.",
        };

        var includeUnrepairableOption = new Option<bool>("--include-unrepairable")
        {
            Description = "Record RepairActionRecord entries even for Unrepairable units.",
        };

        var waitOption = new Option<bool>("--wait")
        {
            Description = "Poll workflow status until completion (up to 30 minutes).",
        };

        var yesOption = new Option<bool>("--yes")
        {
            Description = "Skip interactive confirmation (required on non-TTY stdin).",
        };

        var command = new Command("repair", CommandDescription)
        {
            tenantOption,
            batchSizeOption,
            includeUnrepairableOption,
            waitOption,
            yesOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(batchSizeOption),
            parseResult.GetValue(includeUnrepairableOption),
            parseResult.GetValue(waitOption),
            parseResult.GetValue(yesOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        int? batchSize,
        bool includeUnrepairable,
        bool wait,
        bool yes,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            CliErrorWriter.Write(
                console,
                CommandName,
                code: "INVALID_INPUT",
                message: "--tenant is required.",
                suggestion: "Run 'memories consistency repair --help' to see required options.");
            return CliExitCodes.Plumbing;
        }

        if (!yes)
        {
            if (!console.IsInteractive)
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "CONFIRMATION_REQUIRED",
                    message: "Repair is a mutating operation. Re-run with --yes to confirm.",
                    suggestion: "Append --yes to acknowledge that this will write to the vector / graph backends.");
                return CliExitCodes.Plumbing;
            }

            bool confirmed = await ConfirmRepairAsync(console, tenantId!, ct).ConfigureAwait(false);
            if (!confirmed)
            {
                await console.Error.WriteLineAsync("Repair cancelled.").ConfigureAwait(false);
                return CliExitCodes.Cancelled;
            }
        }

        ConsistencyPollOptions pollOptions = services.GetService<IOptions<ConsistencyPollOptions>>()?.Value
            ?? new ConsistencyPollOptions();

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            ConsistencyRepairRequest request = new(tenantId!, batchSize, includeUnrepairable);

            Uri statusUrl = await client
                .StartConsistencyRepairAsync(tenantId!, request, innerCt)
                .ConfigureAwait(false);
            string instanceId = ConsistencyVerifyCommand.ExtractWorkflowInstanceId(statusUrl);

            if (!wait)
            {
                var receipt = new ConsistencyCommandReceipt(tenantId!, instanceId, "repair", statusUrl);
                router.Write(console.Format, receipt, console.Out);
                return CliExitCodes.Success;
            }

            ConsistencyRepairStatus? finalStatus = await ConsistencyVerifyCommand
                .PollUntilCompleteAsync(
                    client,
                    tenantId!,
                    instanceId,
                    fetch: (tid, id, c) => client.GetConsistencyRepairStatusAsync(tid, id, c),
                    isTerminal: state => ConsistencyVerifyCommand.IsTerminalStatus(state.Status),
                    pollOptions,
                    innerCt).ConfigureAwait(false);

            if (finalStatus is null)
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "CONSISTENCY_REPAIR_NOT_FOUND",
                    message: $"Repair workflow '{instanceId}' could not be located for tenant '{tenantId}'.",
                    suggestion: "Re-run 'memories consistency repair --tenant <id>' without --wait to re-schedule.");
                return CliExitCodes.DomainError;
            }

            if (!ConsistencyVerifyCommand.IsTerminalStatus(finalStatus.Status))
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "CONSISTENCY_WORKFLOW_TIMEOUT",
                    message: $"Repair workflow '{instanceId}' did not complete within {pollOptions.PollTimeout}.",
                    suggestion: $"Poll '{statusUrl}' directly or rerun without --wait to receive the scheduling receipt immediately.");
                return CliExitCodes.Plumbing;
            }

            if (!string.Equals(finalStatus.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "CONSISTENCY_REPAIR_FAILED",
                    message: $"Repair workflow '{instanceId}' finished with status '{finalStatus.Status}'.",
                    suggestion: $"Inspect server logs and poll '{statusUrl}' for more detail.");
                return CliExitCodes.DomainError;
            }

            if (finalStatus.Result is null)
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "INVALID_RESPONSE",
                    message: $"Repair workflow '{instanceId}' completed without a result payload.",
                    suggestion: "Check that the server version matches the client's Contracts.V1 version.");
                return CliExitCodes.Plumbing;
            }

            router.Write(console.Format, finalStatus.Result, console.Out);
            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }

    private static async Task<bool> ConfirmRepairAsync(CliConsole console, string tenantId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(console);

        await console.Error.WriteAsync($"Repair tenant '{tenantId}' now? [y/N]: ").ConfigureAwait(false);
        await console.Error.FlushAsync().ConfigureAwait(false);

        string? answer = await console.In.ReadLineAsync().ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        return string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
    }
}
