// <copyright file="StatusTelemetryCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builds <c>memories status telemetry</c>. Story 7.5 wires the CLI-side reader for
/// <c>GET /api/v1/tenants/{id}/telemetry/summary</c> behind the <c>status</c> command group.
/// </summary>
public static class StatusTelemetryCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "status telemetry";

    private const string CommandDescription = """
Show per-tenant telemetry summary (index sizes, search counters, ingestion queue depth).

Examples:
    memories status telemetry --tenant acme
    memories status telemetry --tenant acme --format json
""";

    /// <summary>Builds the <c>telemetry</c> subcommand under <c>status</c>.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string>("--tenant")
        {
            Description = "Tenant identifier (required).",
            Required = true,
        };

        var command = new Command("telemetry", CommandDescription)
        {
            tenantOption,
        };

        command.SetAction((parseResult, ct) =>
        {
            string tenantId = parseResult.GetValue(tenantOption) ?? string.Empty;
            return ExecuteAsync(services, tenantId, ct);
        });

        return command;
    }

#pragma warning disable HXL001 // Experimental surface — intentional per Task 7.3.
    private static async Task<int> ExecuteAsync(IServiceProvider services, string tenantId, CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            TelemetrySummary summary = await client.GetTelemetrySummaryAsync(tenantId, innerCt).ConfigureAwait(false);

            router.Write(console.Format, summary, console.Out);

            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }
#pragma warning restore HXL001
}
