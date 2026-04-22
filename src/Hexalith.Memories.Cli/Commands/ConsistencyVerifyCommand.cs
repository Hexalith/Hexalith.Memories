// <copyright file="ConsistencyVerifyCommand.cs" company="ITANEO">
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
/// Story 8.2 — builds <c>memories consistency verify</c>. Schedules
/// <c>ConsistencyVerificationWorkflow</c> and either prints the instance id (fire-and-forget)
/// or polls status until completion when <c>--wait</c> is supplied.
/// </summary>
public static class ConsistencyVerifyCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "consistency verify";

    /// <summary>Poll interval used when <c>--wait</c> is set and no <see cref="ConsistencyPollOptions"/> override is registered.</summary>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);

    /// <summary>Hard cap on the <c>--wait</c> poll duration when no <see cref="ConsistencyPollOptions"/> override is registered.</summary>
    public static readonly TimeSpan DefaultPollTimeout = TimeSpan.FromMinutes(30);

    private const string CommandDescription = """
Schedule a consistency-verification workflow for a tenant. Reports per-unit discrepancies
across the three backends (RediSearch, Redis Vector, FalkorDB).

Examples:
    memories consistency verify --tenant acme
    memories consistency verify --tenant acme --wait
    memories consistency verify --tenant acme --batch-size 1000 --wait
""";

    /// <summary>Builds the <c>verify</c> subcommand under <c>consistency</c>.</summary>
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

        var waitOption = new Option<bool>("--wait")
        {
            Description = "Poll workflow status until completion (up to 30 minutes).",
        };

        var command = new Command("verify", CommandDescription)
        {
            tenantOption,
            batchSizeOption,
            waitOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(batchSizeOption),
            parseResult.GetValue(waitOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        int? batchSize,
        bool wait,
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
                suggestion: "Run 'memories consistency verify --help' to see required options.");
            return CliExitCodes.Plumbing;
        }

        ConsistencyPollOptions pollOptions = services.GetService<IOptions<ConsistencyPollOptions>>()?.Value
            ?? new ConsistencyPollOptions();

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            ConsistencyVerificationRequest request = new(tenantId!, batchSize);

            Uri statusUrl = await client
                .StartConsistencyVerificationAsync(tenantId!, request, innerCt)
                .ConfigureAwait(false);
            string instanceId = ExtractWorkflowInstanceId(statusUrl);

            if (!wait)
            {
                var receipt = new ConsistencyCommandReceipt(tenantId!, instanceId, "verify", statusUrl);
                router.Write(console.Format, receipt, console.Out);
                return CliExitCodes.Success;
            }

            ConsistencyVerificationStatus? finalStatus = await PollUntilCompleteAsync(
                client,
                tenantId!,
                instanceId,
                fetch: (tid, id, c) => client.GetConsistencyVerificationStatusAsync(tid, id, c),
                isTerminal: state => IsTerminalStatus(state.Status),
                pollOptions,
                innerCt).ConfigureAwait(false);

            if (finalStatus is null)
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "CONSISTENCY_VERIFY_NOT_FOUND",
                    message: $"Verification workflow '{instanceId}' could not be located for tenant '{tenantId}'.",
                    suggestion: "Use 'memories consistency verify --tenant <id>' without --wait to re-start the workflow.");
                return CliExitCodes.DomainError;
            }

            if (!IsTerminalStatus(finalStatus.Status))
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "CONSISTENCY_WORKFLOW_TIMEOUT",
                    message: $"Verification workflow '{instanceId}' did not complete within {pollOptions.PollTimeout}.",
                    suggestion: $"Poll '{statusUrl}' directly or rerun without --wait to receive the scheduling receipt immediately.");
                return CliExitCodes.Plumbing;
            }

            if (!string.Equals(finalStatus.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "CONSISTENCY_VERIFY_FAILED",
                    message: $"Verification workflow '{instanceId}' finished with status '{finalStatus.Status}'.",
                    suggestion: $"Inspect server logs and poll '{statusUrl}' for more detail.");
                return CliExitCodes.DomainError;
            }

            if (finalStatus.Result is null)
            {
                CliErrorWriter.Write(
                    console,
                    CommandName,
                    code: "INVALID_RESPONSE",
                    message: $"Verification workflow '{instanceId}' completed without a result payload.",
                    suggestion: "Check that the server version matches the client's Contracts.V1 version.");
                return CliExitCodes.Plumbing;
            }

            router.Write(console.Format, finalStatus.Result, console.Out);
            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }

    internal static async Task<TStatus?> PollUntilCompleteAsync<TStatus>(
        MemoriesClient client,
        string tenantId,
        string instanceId,
        Func<string, string, CancellationToken, Task<TStatus?>> fetch,
        Func<TStatus, bool> isTerminal,
        ConsistencyPollOptions options,
        CancellationToken ct)
        where TStatus : class
    {
        ArgumentNullException.ThrowIfNull(options);

        using var timeoutCts = new CancellationTokenSource(options.PollTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        TStatus? lastStatus = null;

        while (!linkedCts.IsCancellationRequested)
        {
            lastStatus = await fetch(tenantId, instanceId, linkedCts.Token).ConfigureAwait(false);
            if (lastStatus is null)
            {
                return null;
            }

            if (isTerminal(lastStatus))
            {
                return lastStatus;
            }

            if (options.PollInterval <= TimeSpan.Zero)
            {
                continue;
            }

            try
            {
                await Task.Delay(options.PollInterval, linkedCts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        return lastStatus ?? await fetch(tenantId, instanceId, ct).ConfigureAwait(false);
    }

    internal static bool IsTerminalStatus(string status)
        => string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Terminated", StringComparison.OrdinalIgnoreCase);

    internal static string ExtractWorkflowInstanceId(Uri statusUrl)
    {
        ArgumentNullException.ThrowIfNull(statusUrl);

        string candidate = statusUrl.Segments[^1].Trim('/');
        return !string.IsNullOrWhiteSpace(candidate)
            ? Uri.UnescapeDataString(candidate)
            : throw new InvalidOperationException($"Workflow status URL '{statusUrl}' does not contain an instance identifier.");
    }
}
