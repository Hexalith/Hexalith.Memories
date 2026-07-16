// <copyright file="HandlersListCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Story 9.3 — builds <c>memories handlers list</c>. Reads the server's registered handler
/// snapshot via <see cref="MemoriesClient.ListHandlersAsync"/> (experimental HXL002 surface) and
/// routes through <see cref="OutputFormatterRouter"/> for the three output formats.</summary>
public static class HandlersListCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "handlers list";

    private const string ListCommandDescription = """
List registered event handlers.

Examples:
    memories handlers list
    memories --format json handlers list
""";

    /// <summary>Builds the <c>list</c> subcommand.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Command command = new("list", ListCommandDescription);
        command.SetAction((parseResult, ct) => ExecuteAsync(services, ct));
        return command;
    }

    private static async Task<int> ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();

#pragma warning disable HXL002
            HandlerRegistrationSnapshot snapshot = await client.ListHandlersAsync(innerCt).ConfigureAwait(false);
#pragma warning restore HXL002

            router.Write(console.Format, snapshot, console.Out);

            if (snapshot.Handlers.Count == 0)
            {
                WriteEmptyHandlersNudge(console);
            }

            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }

    private static void WriteEmptyHandlersNudge(CliConsole console)
    {
        const string nudge =
            "No handlers registered. Configure EventStoreIntegration:Routing:SourceToTenantMap in appsettings "
            + "to bind CloudEvents sources to tenants. See docs/dev/eventstore-integration.md §11.";

        switch (console.Format)
        {
            case OutputFormat.Json:
                return;
            case OutputFormat.Table:
                console.Error.WriteLine(nudge);
                return;
            default:
                console.Out.WriteLine(nudge);
                return;
        }
    }
}
