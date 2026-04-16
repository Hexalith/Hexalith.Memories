// <copyright file="TenantListCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Builds <c>memories tenant list</c> — the single fully-wired command in Story 7.1.</summary>
public static class TenantListCommand
{
    private const string ListCommandDescription = """
List all tenants registered on the server.

Examples:
    memories tenant list
    memories --endpoint http://127.0.0.1:5000 tenant list
""";

    /// <summary>Builds the <c>list</c> subcommand.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var command = new Command("list", ListCommandDescription);
        command.SetAction((parseResult, ct) => ExecuteAsync(services, ct));
        return command;
    }

    private static async Task<int> ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();

        return await executor.ExecuteAsync(async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            IReadOnlyList<TenantSummary> tenants = await client.ListTenantsAsync(innerCt).ConfigureAwait(false);

            if (tenants.Count == 0)
            {
                console.Out.WriteLine("No tenants found.");
                return CliExitCodes.Success;
            }

            foreach (TenantSummary tenant in tenants)
            {
                console.Out.WriteLine($"{tenant.Id}\t{tenant.DisplayName}");
            }

            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }
}
