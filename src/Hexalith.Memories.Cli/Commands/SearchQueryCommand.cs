// <copyright file="SearchQueryCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;
using System.Text;

using Hexalith.Memories.Cli.Errors;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Builds <c>memories search query</c>. Story 7.2 wires hybrid and single-axis searches end-to-end.</summary>
public static class SearchQueryCommand
{
    /// <summary>CLI-side ceiling on <c>--max-results</c> to bound local-process memory (Task 5.6).</summary>
    public const int MaxResultsCeiling = 1000;

    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "search query";

    /// <summary>
    /// PRD-verbatim empty-state nudge for a zero-result probe that implies an empty tenant (no <c>--query</c>
    /// AND graph-axis only). See AC #2 decision tree.
    /// </summary>
    public const string EmptyTenantNudge =
        "No results. This tenant has no memory units yet. "
        + "Get started: 'memories ingest <file>' to add your first document, or configure a DAPR subscription to auto-index events. "
        + "Run 'memories quickstart' for a guided setup.";

    /// <summary>
    /// Hybrid empty-state nudge for a non-empty query that returned zero results. Honest about the
    /// unknowable distinction between "query didn't match" and "tenant might be empty" — lists actions
    /// that resolve either case (AC #2 hybrid-nudge clause).
    /// </summary>
    public const string EmptyQueryNudge =
        "No results. Either your search terms didn't match anything OR this tenant has no memory units yet. "
        + "To find out: try broader query terms, omit --case to widen scope, "
        + "run 'memories search inspect --tenant <id> --case <id> --id <memoryUnitId>' on a known id to confirm indexing, "
        + "or run 'memories ingest <file>' to add data. "
        + "Run 'memories quickstart' for a guided setup.";

    private const string CommandDescription = """
Search memories using hybrid fusion (default) or a single axis.

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
            Description = "Search axis: syntactic, semantic, nl, graph, or hybrid (default).",
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
            return WriteValidationError(
                console,
                code: "INVALID_INPUT",
                message: "--tenant is required.",
                suggestion: "Run 'memories search query --help' to see required options.");
        }

        string normalizedAxis = axis.Trim().ToLowerInvariant();
        if (normalizedAxis is not ("syntactic" or "semantic" or "nl" or "graph" or "hybrid"))
        {
            return WriteValidationError(
                console,
                code: "INVALID_AXIS",
                message: $"--axis '{axis}' is not recognized. Use syntactic, semantic, nl, graph, or hybrid.",
                suggestion: "Run 'memories search query --help' to see valid axis values.");
        }

        bool requiresQuery = normalizedAxis is "syntactic" or "semantic" or "nl" or "hybrid";
        if (requiresQuery && string.IsNullOrWhiteSpace(query))
        {
            return WriteValidationError(
                console,
                code: "INVALID_INPUT",
                message: $"--query is required for --axis {normalizedAxis}.",
                suggestion: "Run 'memories search query --help' to see valid input combinations.");
        }

        if (maxResults <= 0)
        {
            return WriteValidationError(
                console,
                code: "INVALID_INPUT",
                message: "--max-results must be greater than 0.",
                suggestion: "Run 'memories search query --help' to review the allowed range.");
        }

        if (maxResults > MaxResultsCeiling)
        {
            return WriteValidationError(
                console,
                code: "INVALID_INPUT",
                message: $"--max-results exceeds CLI ceiling of {MaxResultsCeiling}. Request a smaller batch or use pagination (coming in Phase 2).",
                suggestion: "Run 'memories search query --help' and retry with a smaller --max-results value.");
        }

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
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
                result = result with
                {
                    EvidencePacket = EvidencePacketMapper.FromHybridSearchResult(
                        result,
                        CreateEvidenceScope(tenantId!, caseId)),
                };

                // Task 4 (empty-state) and Task 5 (degradation) both live here — Task 4 runs first, Task 5
                // second. Both modify the same method; ordering matters for review diff clarity.
                WriteDegradationNoticeIfNeeded(console, result);
                WriteResultAndEmptyStateNudge(
                    router,
                    console,
                    result,
                    query,
                    normalizedAxis,
                    hasResults: result.Results.Count > 0,
                    hasExplanation: result.Explanation is not null);
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
            single = single with
            {
                EvidencePacket = EvidencePacketMapper.FromSearchResult(
                    single,
                    CreateEvidenceScope(tenantId!, caseId)),
            };
            WriteResultAndEmptyStateNudge(
                router,
                console,
                single,
                query,
                normalizedAxis,
                hasResults: single.Results.Count > 0,
                hasExplanation: single.Explanation is not null);
            return CliExitCodes.Success;
        },
        ct,
        // CR10: project server-originated error responses into the same Evidence Packet grammar the
        // success path exposes, so JSON error consumers see consistent scope/state/recovery semantics.
        evidencePacketFactory: (code, message, suggestion) => EvidencePacketMapper.FromError(
            new ErrorResponse(code, message, suggestion),
            CreateEvidenceScope(tenantId!, caseId),
            query ?? string.Empty)).ConfigureAwait(false);
    }

    private static EvidencePacketScope CreateEvidenceScope(string tenantId, string? caseId)
        => new(tenantId, caseId, EvidencePacketIsolationStatus.Authorized, string.IsNullOrWhiteSpace(caseId) ? "tenant" : "tenant-case");

    private static int WriteValidationError(CliConsole console, string code, string message, string suggestion)
    {
        CliErrorWriter.Write(console, CommandName, code, message, suggestion);
        return CliExitCodes.Plumbing;
    }

    private static void WriteResultAndEmptyStateNudge<T>(
        OutputFormatterRouter router,
        CliConsole console,
        T payload,
        string? query,
        string axis,
        bool hasResults,
        bool hasExplanation)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(payload);

        bool formatterMustRunFirst = console.Format == OutputFormat.Human && !hasResults && hasExplanation;
        if (formatterMustRunFirst)
        {
            router.Write(console.Format, payload, console.Out);
            WriteEmptyStateNudgeIfNeeded(console, query, axis, hasResults);
            return;
        }

        WriteEmptyStateNudgeIfNeeded(console, query, axis, hasResults);
        router.Write(console.Format, payload, console.Out);
    }

    private static void WriteEmptyStateNudgeIfNeeded(CliConsole console, string? query, string axis, bool hasResults)
    {
        if (hasResults)
        {
            return;
        }

        // JSON consumers detect emptiness via `data.results.length === 0`; do not inject nudge text
        // into stdout (would force scripts to grep the envelope for advice text — worse UX).
        if (console.Format == OutputFormat.Json)
        {
            return;
        }

        // Empty-tenant branch (AC #2 PRD-verbatim): no --query AND graph-axis only — the only scenario
        // where the CLI can safely assert "tenant has no memory units yet."
        bool isEmptyTenantProbe = string.IsNullOrWhiteSpace(query) && axis == "graph";
        string nudge = isEmptyTenantProbe ? EmptyTenantNudge : EmptyQueryNudge;

        switch (console.Format)
        {
            case OutputFormat.Table:
                // Table format is interactive-only — nudge goes to stderr to preserve header/separator
                // alignment on stdout for piped consumers.
                console.Error.WriteLine(nudge);
                return;
            default:
                // Human format — normally the nudge is the complete output for zero results. When
                // --explain is set, SearchQueryCommand writes the formatter output first so the caveat
                // remains the first line per Story 7.2's contract.
                console.Out.WriteLine(nudge);
                return;
        }
    }

    private static void WriteDegradationNoticeIfNeeded(CliConsole console, HybridSearchResult result)
    {
        if (!result.Degraded)
        {
            return;
        }

        // JSON consumers detect degradation via `data.degraded === true && data.unavailableAxes`
        // (the envelope already carries the flag). Stderr duplication would be redundant out-of-band
        // signaling — same rationale as ADR-7.3-002 for errors.
        if (console.Format == OutputFormat.Json)
        {
            return;
        }

        IEnumerable<string> axes = result.UnavailableAxes ?? Array.Empty<string>();
        var builder = new StringBuilder();
        builder.AppendLine("Warning: search degraded — partial results only.");

        bool wroteAnyAxis = false;
        foreach (string axis in axes)
        {
            string code = axis.Equals("graph", StringComparison.OrdinalIgnoreCase)
                ? "GRAPH_UNAVAILABLE"
                : "BACKEND_UNAVAILABLE";
            ErrorTranslation translation = ErrorMessageCatalog.Resolve(code);
            string suggestion = translation.CliSuggestion
                ?? "Backend recovers automatically; retry shortly.";
            builder.AppendLine($"  - {axis}: {suggestion}");
            wroteAnyAxis = true;
        }

        if (!wroteAnyAxis)
        {
            // Null-axes guard: server returned Degraded=true with no axis details. FR57 says no
            // dead-end states — surface something actionable rather than suppressing.
            builder.AppendLine("  - (no axis details available) Retry the request; partial-results recovery is best-effort.");
        }

        console.Error.Write(builder.ToString());
    }
}
