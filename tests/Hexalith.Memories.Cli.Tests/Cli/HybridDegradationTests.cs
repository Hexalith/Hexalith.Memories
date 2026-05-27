// <copyright file="HybridDegradationTests.cs" company="ITANEO">
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 7.3 Task 7.5 — degradation rewrite (AC #6). Asserts that per-axis warnings land on stderr,
/// that the 7.2 bridge line is gone, that JSON mode emits nothing to stderr, and that a null
/// <c>UnavailableAxes</c> payload does not crash the CLI.
/// </summary>
public sealed class HybridDegradationTests
{
    [Fact]
    public async Task DegradedGraphAxis_Human_MultiLineStderrBlockWithGraphSuggestion()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            OutputFormat.Human,
            degraded: true,
            unavailable: ["graph"]);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query.Parse(new[] { "query", "--tenant", "t1", "--query", "x" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string warn = stderr.ToString();
        warn.ShouldContain("Warning: search degraded — partial results only.");
        warn.ShouldContain("- graph:");
        warn.ShouldContain("FalkorDB");

        // The 7.2 bridge line must not appear on any stream.
        stdout.ToString().ShouldNotContain("Note: search degraded");
        warn.ShouldNotContain("Note: search degraded");
    }

    [Fact]
    public async Task DegradedMultipleAxes_Table_WritesAllBulletsToStderr()
    {
        (IServiceProvider services, _, StringWriter stderr) = BuildServices(
            OutputFormat.Table,
            degraded: true,
            unavailable: ["syntactic", "semantic"]);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query.Parse(new[] { "query", "--tenant", "t1", "--query", "x" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string warn = stderr.ToString();
        warn.ShouldContain("- syntactic:");
        warn.ShouldContain("- semantic:");
        warn.ShouldContain("Redis Stack");
    }

    [Fact]
    public async Task DegradedJson_SuppressesStderrBlockBecauseEnvelopeAlreadyCarriesFlag()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            OutputFormat.Json,
            degraded: true,
            unavailable: ["graph"]);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query.Parse(new[] { "query", "--tenant", "t1", "--query", "x" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        stdout.ToString().ShouldContain("\"degraded\": true");
    }

    [Fact]
    public async Task NotDegraded_NoStderrOutput()
    {
        (IServiceProvider services, _, StringWriter stderr) = BuildServices(
            OutputFormat.Human,
            degraded: false,
            unavailable: []);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query.Parse(new[] { "query", "--tenant", "t1", "--query", "x" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldNotContain("Warning: search degraded");
    }

    [Fact]
    public async Task Degraded_NullUnavailableAxes_DoesNotCrashAndEmitsGracefulWarning()
    {
        // Null-axes guard: server bug shape where Degraded is true but UnavailableAxes is null. FR57
        // says no dead-end states — the CLI must surface something actionable rather than crashing or
        // silently suppressing the warning.
        (IServiceProvider services, _, StringWriter stderr) = BuildServices(
            OutputFormat.Human,
            degraded: true,
            unavailable: null);

        System.CommandLine.Command query = SearchQueryCommand.Build(services);
        int exit = await query.Parse(new[] { "query", "--tenant", "t1", "--query", "x" }).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        string warn = stderr.ToString();
        warn.ShouldContain("Warning: search degraded — partial results only.");
        warn.ShouldContain("(no axis details available)");
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        bool degraded,
        IReadOnlyList<string>? unavailable)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ =>
            new DegradedStubClient(degraded, unavailable)));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private sealed class DegradedStubClient : MemoriesClient
    {
        private readonly bool _degraded;
        private readonly IReadOnlyList<string>? _unavailable;

        public DegradedStubClient(bool degraded, IReadOnlyList<string>? unavailable)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _degraded = degraded;
            _unavailable = unavailable;
        }

        public override Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
        {
            var sample = new FusedScoredResult
            {
                MemoryUnitId = "mu-1",
                CompositeScore = 0.5,
                ContentSnippet = "snippet",
                SourceUri = "mem://case/mu-1",
                SourceType = SourceType.File,
            };
            return Task.FromResult(new HybridSearchResult
            {
                Results = [sample],
                TotalCount = 1,
                Degraded = _degraded,
                UnavailableAxes = _unavailable!,
                Query = request.Query ?? string.Empty,
            });
        }
    }
}
