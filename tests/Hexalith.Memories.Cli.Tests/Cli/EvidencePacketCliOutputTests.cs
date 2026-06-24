// <copyright file="EvidencePacketCliOutputTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Net;
using System.Text.Json;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.TestHelpers.EvidencePackets;

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

    [Fact]
    public async Task SearchQuery_HybridJson_EmptyResult_EmitsEmptyEvidencePacket()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            onHybridSearch: (request, _) => Task.FromResult(new HybridSearchResult
            {
                Results = [],
                TotalCount = 0,
                Degraded = false,
                UnavailableAxes = [],
                Query = request.Query,
                AxesUsed = ["semantic"],
            }));
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "no matches" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        JsonElement packet = ReadEvidencePacket(stdout);

        packet.GetProperty("state").GetString().ShouldBe("empty");
        packet.GetProperty("sources").GetArrayLength().ShouldBe(0);
        packet.GetProperty("evidence").GetProperty("evidenceStrength").GetString().ShouldBe("none");
        packet.GetProperty("omittedDetails").GetProperty("reason").GetString().ShouldBe("none");
        packet.GetProperty("recovery")[0].GetProperty("kind").GetString().ShouldBe("broadenScope");
    }

    [Fact]
    public async Task SearchQuery_HybridJson_DegradedTokenBudget_EmitsCombinedOmissionMetadata()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            onHybridSearch: (request, _) => Task.FromResult(new HybridSearchResult
            {
                Results =
                [
                    new FusedScoredResult
                    {
                        MemoryUnitId = "mu-001",
                        CompositeScore = 0.83,
                        ContentSnippet = "Partial hybrid evidence",
                        SourceUri = "mem://tenant-a/case-a/mu-001",
                        SourceType = SourceType.File,
                        SemanticScore = 0.83,
                        SyntacticScore = 0.61,
                        CaseId = request.CaseId,
                        CaseName = "Case A",
                    },
                ],
                TotalCount = 3,
                Degraded = true,
                UnavailableAxes = ["graph"],
                Query = request.Query,
                AxesUsed = ["semantic", "syntactic"],
                OmittedCount = 2,
                EstimatedTokensTotal = 2_048,
                OmittedReason = OmittedReason.TokenBudget,
            }));
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "claim denied" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        JsonElement packet = ReadEvidencePacket(stdout);
        JsonElement omitted = packet.GetProperty("omittedDetails");

        packet.GetProperty("state").GetString().ShouldBe("degraded");
        packet.GetProperty("evidence").GetProperty("degraded").GetBoolean().ShouldBeTrue();
        packet.GetProperty("evidence").GetProperty("unavailableAxes")[0].GetString().ShouldBe("graph");
        omitted.GetProperty("reason").GetString().ShouldBe("combined");
        ReadStringArray(omitted.GetProperty("fieldNames")).ShouldContain("sources");
        ReadStringArray(omitted.GetProperty("fieldNames")).ShouldContain("evidence.unavailableAxes");
        ReadStringArray(omitted.GetProperty("detailGroups")).ShouldContain("rankedResults");
        ReadStringArray(omitted.GetProperty("detailGroups")).ShouldContain("backendDiagnostics");
        JsonElement handle = omitted.GetProperty("expansionHandles")[0];
        handle.GetProperty("handle").GetString()!.ShouldStartWith("ep:v1:");
        handle.GetProperty("targetDetailGroup").GetString().ShouldBe("rankedResults");
        handle.GetProperty("tenantId").GetString().ShouldBe("tenant-a");
        handle.GetProperty("caseId").GetString().ShouldBe("case-a");
        ReadRecoveryKinds(packet).ShouldContain("retry");
        ReadRecoveryKinds(packet).ShouldContain("inspectBackendHealth");
    }

    [Fact]
    public async Task SearchQuery_SingleAxisJson_EmitsEvidencePacketWithSameSemantics()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            onSearch: (request, _) => Task.FromResult(new SearchResult
            {
                Results =
                [
                    new ScoredResult
                    {
                        MemoryUnitId = "mu-010",
                        Score = 0.77,
                        ContentSnippet = "Syntactic evidence",
                        SourceUri = "mem://tenant-a/case-a/mu-010",
                        SourceType = SourceType.File,
                        Axis = request.Axis,
                        CaseId = request.CaseId,
                        CaseName = "Case A",
                    },
                ],
                TotalCount = 1,
                HasIndexedMemoryUnits = true,
                Query = request.Query ?? string.Empty,
                AxesUsed = [request.Axis],
            }));
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "claim denied", "--axis", "syntactic" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        JsonElement packet = ReadEvidencePacket(stdout);

        packet.GetProperty("scope").GetProperty("tenantId").GetString().ShouldBe("tenant-a");
        packet.GetProperty("scope").GetProperty("caseId").GetString().ShouldBe("case-a");
        packet.GetProperty("state").GetString().ShouldBe("complete");
        packet.GetProperty("evidence").GetProperty("axesUsed")[0].GetString().ShouldBe("syntactic");
        packet.GetProperty("evidence").GetProperty("axisEvidence")[0].GetProperty("axis").GetString().ShouldBe("syntactic");
        packet.GetProperty("sources")[0].GetProperty("memoryUnitId").GetString().ShouldBe("mu-010");
    }

    [Fact]
    public async Task SearchQuery_SingleAxisJson_TokenBudgetCompressed_EmitsExpansionGuidance()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            onSearch: (request, _) => Task.FromResult(new SearchResult
            {
                Results =
                [
                    new ScoredResult
                    {
                        MemoryUnitId = "mu-011",
                        Score = 0.81,
                        ContentSnippet = "Compressed evidence",
                        SourceUri = "mem://tenant-a/case-a/mu-011",
                        SourceType = SourceType.File,
                        Axis = request.Axis,
                        CaseId = request.CaseId,
                        CaseName = "Case A",
                    },
                ],
                TotalCount = 4,
                HasIndexedMemoryUnits = true,
                Query = request.Query ?? string.Empty,
                AxesUsed = [request.Axis],
                OmittedCount = 3,
                EstimatedTokensTotal = 4_096,
                OmittedReason = OmittedReason.TokenBudget,
            }));
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "claim denied", "--axis", "semantic" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        JsonElement packet = ReadEvidencePacket(stdout);
        JsonElement omitted = packet.GetProperty("omittedDetails");

        packet.GetProperty("state").GetString().ShouldBe("pendingExpansion");
        omitted.GetProperty("omittedCount").GetInt32().ShouldBe(3);
        omitted.GetProperty("estimatedTokensTotal").GetInt64().ShouldBe(4_096);
        omitted.GetProperty("reason").GetString().ShouldBe("tokenBudget");
        JsonElement handle = omitted.GetProperty("expansionHandles")[0];
        handle.GetProperty("handle").GetString()!.ShouldStartWith("ep:v1:");
        handle.GetProperty("kind").GetString().ShouldBe("increaseTokenBudget");
        handle.GetProperty("targetDetailGroup").GetString().ShouldBe("rankedResults");
        packet.GetProperty("recovery")[0].GetProperty("kind").GetString().ShouldBe("increaseTokenBudget");
    }

    [Fact]
    public async Task SearchQuery_HybridJson_ServerForbidden_EmitsErrorEnvelopeWithUnauthorizedEvidencePacket()
    {
        // CR10: server-originated error responses now carry an Evidence Packet alongside the error.
        (IServiceProvider services, StringWriter stdout, _) = BuildServices(
            onHybridSearch: (_, _) => Task.FromException<HybridSearchResult>(
                new MemoriesRemoteException(
                    HttpStatusCode.Forbidden,
                    new ErrorResponse(
                        "TENANT_FORBIDDEN",
                        "Denied for tenant-b at C:\\secret\\trace.txt.",
                        "Switch to tenant-b or reconnect to redis://backend-key/0."))));
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "claim denied" })
            .InvokeAsync();

        exit.ShouldNotBe(CliExitCodes.Success);
        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        JsonElement root = doc.RootElement;
        root.GetProperty("error").GetProperty("code").GetString().ShouldBe("TENANT_FORBIDDEN");
        root.TryGetProperty("data", out _).ShouldBeFalse();

        JsonElement packet = root.GetProperty("evidencePacket");
        packet.GetProperty("state").GetString().ShouldBe("unauthorized");
        packet.GetProperty("scope").GetProperty("isolationStatus").GetString().ShouldBe("unauthorized");
        packet.GetProperty("scope").GetProperty("tenantId").GetString().ShouldBe("tenant-a");
        packet.GetProperty("recovery")[0].GetProperty("kind").GetString().ShouldBe("checkAuthorization");

        // The packet must not leak the sensitive server diagnostic text or cross-tenant identifiers.
        string packetText = packet.GetRawText();
        packetText.ShouldNotContain("C:\\secret", Shouldly.Case.Sensitive);
        packetText.ShouldNotContain("redis://backend-key", Shouldly.Case.Sensitive);
        packetText.ShouldNotContain("tenant-b", Shouldly.Case.Sensitive);
    }

    [Fact]
    public async Task SearchQuery_HybridJson_NoExplain_EmitsDefaultCaveat()
    {
        // CR18: without --explain the server returns no explanation, so the packet uses the default caveat.
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            onHybridSearch: (request, _) => Task.FromResult(new HybridSearchResult
            {
                Results =
                [
                    new FusedScoredResult
                    {
                        MemoryUnitId = "mu-001",
                        CompositeScore = 0.88,
                        ContentSnippet = "Hybrid evidence",
                        SourceUri = "mem://tenant-a/case-a/mu-001",
                        SourceType = SourceType.File,
                        SemanticScore = 0.88,
                        CaseId = request.CaseId,
                        CaseName = "Case A",
                    },
                ],
                TotalCount = 1,
                Degraded = false,
                UnavailableAxes = [],
                Query = request.Query,
                AxesUsed = ["semantic"],
            }));
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "claim denied" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        JsonElement packet = ReadEvidencePacket(stdout);
        packet.GetProperty("evidence").GetProperty("caveat").GetString()
            .ShouldBe("Scores measure query-result relevance, not factual accuracy or data completeness.");
    }

    [Fact]
    public async Task SearchQuery_HybridJson_MatchesSharedCanonicalPacket()
    {
        // CR1: the CLI surface emits exactly the shared canonical packet for the canonical input.
        (IServiceProvider services, StringWriter stdout, _) = BuildServices(
            onHybridSearch: (_, _) => Task.FromResult(EvidencePacketCanonicalFixtures.HybridComplete()));
        Command command = SearchQueryCommand.Build(services);

        int exit = await command
            .Parse(new[] { "query", "--tenant", "tenant-a", "--case", "case-a", "--query", "claim denied" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Success);
        JsonElement packet = ReadEvidencePacket(stdout);
        EvidencePacketCanonicalFixtures.Canonicalize(packet.GetRawText())
            .ShouldBe(EvidencePacketCanonicalFixtures.Canonicalize(EvidencePacketCanonicalFixtures.HybridCompletePacket()));
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        Func<SearchRequest, CancellationToken, Task<SearchResult>>? onSearch = null,
        Func<HybridSearchRequest, CancellationToken, Task<HybridSearchResult>>? onHybridSearch = null)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = OutputFormat.Json });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => new StubMemoriesClient(onSearch, onHybridSearch)));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private static JsonElement ReadEvidencePacket(StringWriter stdout)
    {
        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        return doc.RootElement.GetProperty("data").GetProperty("evidencePacket").Clone();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement array)
        => array.EnumerateArray().Select(item => item.GetString()!).ToArray();

    private static IReadOnlyList<string> ReadRecoveryKinds(JsonElement packet)
        => packet.GetProperty("recovery").EnumerateArray().Select(item => item.GetProperty("kind").GetString()!).ToArray();

    private sealed class StubMemoriesClient : MemoriesClient
    {
        private readonly Func<SearchRequest, CancellationToken, Task<SearchResult>>? _onSearch;
        private readonly Func<HybridSearchRequest, CancellationToken, Task<HybridSearchResult>>? _onHybridSearch;

        public StubMemoriesClient(
            Func<SearchRequest, CancellationToken, Task<SearchResult>>? onSearch,
            Func<HybridSearchRequest, CancellationToken, Task<HybridSearchResult>>? onHybridSearch)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _onSearch = onSearch;
            _onHybridSearch = onHybridSearch;
        }

        public override Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
            => _onSearch is not null
                ? _onSearch(request, ct)
                : Task.FromResult(new SearchResult
                {
                    Results = [],
                    TotalCount = 0,
                    HasIndexedMemoryUnits = true,
                    Query = request.Query ?? string.Empty,
                    AxesUsed = [request.Axis],
                });

        public override Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
            => _onHybridSearch is not null
                ? _onHybridSearch(request, ct)
                : Task.FromResult(new HybridSearchResult
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
