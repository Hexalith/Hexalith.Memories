// <copyright file="SearchQueryCommand.cs" company="ITANEO">
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

/// <summary>Builds <c>memories search query</c>. Story 7.2 wires hybrid and single-axis searches end-to-end.</summary>
public static class SearchQueryCommand
{
    /// <summary>CLI-side ceiling on <c>--max-results</c> to bound local-process memory (Task 5.6).</summary>
    public const int MaxResultsCeiling = 1000;

    private const string CommandDescription = """
Search memories using three-axis hybrid fusion (default) or a single axis.

Examples:
    memories search query --tenant acme --query "customer escalation"
    memories search query --tenant acme --query "..." --axis semantic --explain
    memories search query --tenant acme --query "..." --format json
""";

    /// <summary>Builds the <c>query</c> subcommand.</summary>
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
        var caseOption = new Option<string?>("--case")
        {
            Description = "Limit the search to a specific case.",
        };
        var queryOption = new Option<string?>("--query")
        {
            Description = "Free-text query. Required for syntactic/semantic/hybrid; optional for graph.",
        };
        var axisOption = new Option<string>("--axis")
        {
            Description = "Search axis: syntactic, semantic, graph, or hybrid (default).",
            DefaultValueFactory = _ => "hybrid",
        };
        var maxResultsOption = new Option<int>("--max-results")
        {
            Description = "Max rows (default: server default 10; CLI ceiling: 1000).",
            DefaultValueFactory = _ => 10,
        };
        var explainOption = new Option<bool>("--explain")
        {
            Description = "Ask the server for explain metadata (caveat + normalization methods).",
        };

        var command = new Command("query", CommandDescription)
        {
            tenantOption,
            caseOption,
            queryOption,
            axisOption,
            maxResultsOption,
            explainOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(caseOption),
            parseResult.GetValue(queryOption),
            parseResult.GetValue(axisOption) ?? "hybrid",
            parseResult.GetValue(maxResultsOption),
            parseResult.GetValue(explainOption),
            ct));

        return command;
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider services,
        string? tenantId,
        string? caseId,
        string? query,
        string axis,
        int maxResults,
        bool explain,
        CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            console.Error.WriteLine("--tenant is required.");
            return CliExitCodes.Plumbing;
        }

        string normalizedAxis = axis.Trim().ToLowerInvariant();
        if (normalizedAxis is not ("syntactic" or "semantic" or "graph" or "hybrid"))
        {
            console.Error.WriteLine(
                $"--axis '{axis}' is not recognized. Use syntactic, semantic, graph, or hybrid.");
            return CliExitCodes.Plumbing;
        }

        bool requiresQuery = normalizedAxis is "syntactic" or "semantic" or "hybrid";
        if (requiresQuery && string.IsNullOrWhiteSpace(query))
        {
            console.Error.WriteLine($"--query is required for --axis {normalizedAxis}.");
            return CliExitCodes.Plumbing;
        }

        if (maxResults <= 0)
        {
            console.Error.WriteLine("--max-results must be greater than 0.");
            return CliExitCodes.Plumbing;
        }

        if (maxResults > MaxResultsCeiling)
        {
            console.Error.WriteLine(
                $"--max-results exceeds CLI ceiling of {MaxResultsCeiling}. Request a smaller batch or use pagination (coming in Phase 2).");
            return CliExitCodes.Plumbing;
        }

        return await executor.ExecuteAsync(async (config, innerCt) =>
        {
            MemoriesClient client = services.GetRequiredService<MemoriesClient>();

            if (normalizedAxis == "hybrid")
            {
                var request = new HybridSearchRequest(
                    TenantId: tenantId!,
                    Query: query!,
                    CaseId: caseId,
                    MaxResults: maxResults,
                    Explain: explain);
                HybridSearchResult result = await client.HybridSearchAsync(request, innerCt).ConfigureAwait(false);
                router.Write(console.Format, result, console.Out);
                return CliExitCodes.Success;
            }

            var singleRequest = new SearchRequest(
                TenantId: tenantId!,
                Axis: normalizedAxis,
                Query: query,
                CaseId: caseId,
                MaxResults: maxResults,
                Explain: explain);
            SearchResult single = await client.SearchAsync(singleRequest, innerCt).ConfigureAwait(false);
            router.Write(console.Format, single, console.Out);
            return CliExitCodes.Success;
        }, ct).ConfigureAwait(false);
    }
}
