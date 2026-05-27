// <copyright file="EvidencePacketCliOutputTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Text.Json;

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

public sealed class EvidencePacketCliOutputTests
{
    [Fact]
    public async Task SearchQuery_HybridJson_EmitsAdditiveEvidencePacketWithSameSemantics()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices();
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "claim denied", "--explain" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        JsonElement data = doc.RootElement.GetProperty("data");
        data.GetProperty("results").GetArrayLength().ShouldBe(1);

        JsonElement packet = data.GetProperty("evidencePacket");
        packet.GetProperty("scope").GetProperty("tenantId").GetString().ShouldBe("tenant-a");
        packet.GetProperty("scope").GetProperty("caseId").GetString().ShouldBe("case-a");
        packet.GetProperty("state").GetString().ShouldBe("complete");
        packet.GetProperty("evidence").GetProperty("evidenceStrength").GetString().ShouldBe("strong");
        packet.GetProperty("evidence").GetProperty("caveat").GetString()
            .ShouldBe("Scores measure relevance, not factual accuracy.");
        packet.GetProperty("sources")[0].GetProperty("memoryUnitId").GetString().ShouldBe("mu-001");
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = OutputFormat.Json });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => new StubMemoriesClient()));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private sealed class StubMemoriesClient : MemoriesClient
    {
        public StubMemoriesClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public override Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
            => Task.FromResult(new HybridSearchResult
            {
                Results =
                [
                    new FusedScoredResult
                    {
                        MemoryUnitId = "mu-001",
                        CompositeScore = 0.91,
                        ContentSnippet = "Claim denial language",
                        SourceUri = "mem://tenant-a/case-a/mu-001",
                        SourceType = SourceType.File,
                        SemanticScore = 0.91,
                        SyntacticScore = 0.62,
                        CaseId = request.CaseId,
                        CaseName = "Case A",
                    },
                ],
                TotalCount = 1,
                Degraded = false,
                UnavailableAxes = [],
                Query = request.Query,
                AxesUsed = ["semantic", "syntactic"],
                Explanation = new SearchExplanation
                {
                    Caveat = "Scores measure relevance, not factual accuracy.",
                    AxisDetails = new Dictionary<string, AxisExplanation>
                    {
                        ["semantic"] = new() { NormalizationMethod = "cosine", Description = "cosine similarity" },
                        ["syntactic"] = new() { NormalizationMethod = "bm25_saturation", Description = "BM25 saturation" },
                    },
                },
            });
    }
}
