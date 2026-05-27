// <copyright file="EmptyStateNudgeTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 7.3 Task 7.4 — empty-state nudge matrix (AC #2, #5). Covers the hybrid query-miss nudge per
/// format and the tenant-list empty-state nudge per format.
/// </summary>
public sealed class EmptyStateNudgeTests
{
    private const string ExplainCaveat =
        "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness.";

    [Fact]
    public async Task SearchQuery_EmptyHybridResult_Human_WritesHybridNudgeToStdout()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Human,
            emptyHybrid: true);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--query", "needle" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("No results. Either your search terms didn't match");
        stdout.ToString().ShouldContain("memories quickstart");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchQuery_EmptyHybridResult_Json_SuppressesNudgeAndEmitsSuccessEnvelope()
    {
        (IServiceProvider services, StringWriter stdout, _) = BuildServices(
            format: OutputFormat.Json,
            emptyHybrid: true);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--query", "needle" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();
        output.ShouldNotContain("No results. Either your search terms");
        output.ShouldNotContain("memories quickstart");
        output.ShouldContain("\"schemaVersion\": 1");
        output.ShouldContain("\"data\":");
    }

    [Fact]
    public async Task SearchQuery_EmptyHybridResult_Table_WritesNudgeToStderrOnlyKeepsStdoutAligned()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Table,
            emptyHybrid: true);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--query", "needle" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldContain("No results. Either your search terms didn't match");
        stdout.ToString().ShouldNotContain("No results. Either your search terms");
    }

    [Fact]
    public async Task SearchQuery_EmptyHybridExplain_Human_PrintsCaveatBeforeNudge()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Human,
            emptyHybrid: true);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--query", "needle", "--explain" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();
        output.ShouldStartWith(ExplainCaveat);
        output.ShouldContain(SearchQueryCommand.EmptyQueryNudge);
        output.IndexOf(SearchQueryCommand.EmptyQueryNudge, StringComparison.Ordinal)
            .ShouldBeGreaterThan(output.IndexOf(ExplainCaveat, StringComparison.Ordinal));
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchQuery_EmptyGraphProbeExplain_Human_PrintsCaveatBeforePrdNudge()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Human,
            emptySingleAxis: true);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--axis", "graph", "--explain" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();
        output.ShouldStartWith(ExplainCaveat);
        output.ShouldContain(SearchQueryCommand.EmptyTenantNudge);
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchQuery_EmptyGraphProbe_Json_SuppressesPrdNudgeAndEmitsSuccessEnvelope()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Json,
            emptySingleAxis: true);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--axis", "graph" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldNotContain(SearchQueryCommand.EmptyTenantNudge);
        stdout.ToString().ShouldContain("\"schemaVersion\": 1");
        stdout.ToString().ShouldContain("\"data\":");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchQuery_EmptyGraphProbe_Table_WritesPrdNudgeToStderrOnly()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Table,
            emptySingleAxis: true);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--axis", "graph" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldContain(SearchQueryCommand.EmptyTenantNudge);
        stdout.ToString().ShouldNotContain(SearchQueryCommand.EmptyTenantNudge);
    }

    [Fact]
    public async Task TenantList_EmptyResult_Human_AppendsNudgeAfterFormatterOutput()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Human,
            emptyTenants: true);

        System.CommandLine.Command tenant = BuildTenantListCommand(services);
        int exit = await tenant.Parse(new[] { "list" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();

        // The 7.2 formatter's "No tenants found." line is preserved byte-for-byte FIRST, then the
        // FR57 nudge is appended as a second line by the handler.
        int firstLineEnd = output.IndexOf('\n');
        firstLineEnd.ShouldBeGreaterThan(0);
        output[..firstLineEnd].ShouldContain("No tenants found.");
        output.ShouldContain("Get started:");
        output.ShouldContain("memories quickstart");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task TenantList_EmptyResult_Json_NudgeSuppressed()
    {
        (IServiceProvider services, StringWriter stdout, _) = BuildServices(
            format: OutputFormat.Json,
            emptyTenants: true);

        System.CommandLine.Command tenant = BuildTenantListCommand(services);
        int exit = await tenant.Parse(new[] { "list" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();
        output.ShouldContain("\"data\": []");
        output.ShouldNotContain("Get started:");
        output.ShouldNotContain("memories quickstart");
    }

    [Fact]
    public async Task TenantList_EmptyResult_Table_NudgeOnStderrKeepsTableAligned()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            format: OutputFormat.Table,
            emptyTenants: true);

        System.CommandLine.Command tenant = BuildTenantListCommand(services);
        int exit = await tenant.Parse(new[] { "list" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldContain("Get started:");
        stdout.ToString().ShouldNotContain("Get started:");
    }

    private static System.CommandLine.Command BuildTenantListCommand(IServiceProvider services)
    {
        var root = new System.CommandLine.Command("tenant");
        root.Subcommands.Add(TenantListCommand.Build(services));
        return root;
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        bool emptyHybrid = false,
        bool emptyTenants = false,
        bool emptySingleAxis = false)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });

        // Replace the DI-registered HTTP-based MemoriesClient with a stub that returns canned responses.
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(sp =>
            new StubMemoriesClient(emptyHybrid, emptyTenants, emptySingleAxis)));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private sealed class StubMemoriesClient : MemoriesClient
    {
        private readonly bool _emptyHybrid;
        private readonly bool _emptyTenants;
        private readonly bool _emptySingleAxis;

        public StubMemoriesClient(bool emptyHybrid, bool emptyTenants, bool emptySingleAxis)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _emptyHybrid = emptyHybrid;
            _emptyTenants = emptyTenants;
            _emptySingleAxis = emptySingleAxis;
        }

        public override Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
        {
            if (_emptyHybrid)
            {
                return Task.FromResult(new HybridSearchResult
                {
                    Results = [],
                    TotalCount = 0,
                    Degraded = false,
                    UnavailableAxes = [],
                    Query = request.Query ?? string.Empty,
                    Explanation = request.Explain ? BuildExplanation() : null,
                });
            }

            return base.HybridSearchAsync(request, ct);
        }

        public override Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
        {
            if (_emptySingleAxis)
            {
                return Task.FromResult(new SearchResult
                {
                    Results = [],
                    TotalCount = 0,
                    HasIndexedMemoryUnits = false,
                    Query = request.Query ?? string.Empty,
                    Explanation = request.Explain ? BuildExplanation() : null,
                });
            }

            return base.SearchAsync(request, ct);
        }

        public override Task<IReadOnlyList<TenantSummary>> ListTenantsAsync(CancellationToken ct)
        {
            if (_emptyTenants)
            {
                return Task.FromResult<IReadOnlyList<TenantSummary>>(Array.Empty<TenantSummary>());
            }

            return base.ListTenantsAsync(ct);
        }

        private static SearchExplanation BuildExplanation() => new()
        {
            Caveat = ExplainCaveat,
            AxisDetails = new Dictionary<string, AxisExplanation>
            {
                ["syntactic"] = new() { NormalizationMethod = "bm25_saturation", Description = "BM25 saturation" },
            },
        };
    }
}
