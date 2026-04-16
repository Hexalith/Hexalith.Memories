// <copyright file="TenantListCommand.cs" company="ITANEO">
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

/// <summary>Builds <c>memories tenant list</c>. Story 7.2 routes output through <see cref="OutputFormatterRouter"/>.</summary>
public static class TenantListCommand
{
    private const string ListCommandDescription = """
List all tenants registered on the server.

Examples:
    memories tenant list
    memories --endpoint http://127.0.0.1:5000 tenant list
    memories --format json tenant list
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

    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "tenant list";

    private static async Task<int> ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();
            IReadOnlyList<TenantSummary> tenants = await client.ListTenantsAsync(innerCt).ConfigureAwait(false);

            router.Write(console.Format, tenants, console.Out);

            // FR57 empty-state nudge (Task 4.3): append a second informational line AFTER the formatter
            // writes so ADR-7.2-002 byte-for-byte parity for "No tenants found." is preserved.
            if (tenants.Count == 0)
            {
                WriteEmptyTenantsNudge(console);
            }

            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }

    private static void WriteEmptyTenantsNudge(CliConsole console)
    {
        const string nudge =
            "Get started: provisioning a tenant via 'memories tenant create' will be wired in a later story; "
            + "for now, provision via the server's REST API at POST /api/tenants. "
            + "Run 'memories quickstart' for a guided setup.";

        switch (console.Format)
        {
            case OutputFormat.Json:
                // JSON consumers detect emptiness via `data.length === 0`; do not inject nudge text
                // into stdout or stderr.
                return;
            case OutputFormat.Table:
                // Interactive nudge for humans viewing a table — stderr so piped consumers can 2>/dev/null.
                console.Error.WriteLine(nudge);
                return;
            default:
                // Human format — appended to stdout after "No tenants found.".
                console.Out.WriteLine(nudge);
                return;
        }
    }
}
