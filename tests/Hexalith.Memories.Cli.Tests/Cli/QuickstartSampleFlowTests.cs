// <copyright file="QuickstartSampleFlowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Net;
using System.Text;

using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Case = Hexalith.Memories.Contracts.V1.Case;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

public sealed class QuickstartSampleFlowTests
{
    [Fact]
    public async Task IngestSample_Success_ReusesExistingCase()
    {
        var client = new StubSampleClient();
        Case existing = new(
            Id: "case-existing",
            TenantId: "acme",
            Name: QuickstartSampleFlow.DefaultCaseName,
            Description: null,
            Status: CaseStatus.Active,
            CreatedAt: DateTimeOffset.UtcNow,
            LastUpdated: DateTimeOffset.UtcNow,
            MemoryUnitCount: 0);
        client.Cases.Add(existing);

        var flow = new QuickstartSampleFlow(client, TimeProvider.System);
        SampleIngestResult result = await flow.IngestSampleAsync("acme", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.CaseId.ShouldBe("case-existing");
        client.CreateCaseCalls.ShouldBe(0);
        client.IngestCalls.ShouldBe(1);
    }

    [Fact]
    public async Task IngestSample_CreatesCase_WhenMissing()
    {
        var client = new StubSampleClient();
        var flow = new QuickstartSampleFlow(client, TimeProvider.System);

        SampleIngestResult result = await flow.IngestSampleAsync("acme", CancellationToken.None);

        result.Success.ShouldBeTrue();
        client.CreateCaseCalls.ShouldBe(1);
    }

    [Fact]
    public async Task IngestSample_EmbedsDeterministicQuery_AndUniqueRunTokenPerRun()
    {
        var client = new StubSampleClient();
        var flow = new QuickstartSampleFlow(client, TimeProvider.System);

        SampleIngestResult first = await flow.IngestSampleAsync("acme", CancellationToken.None);
        SampleIngestResult second = await flow.IngestSampleAsync("acme", CancellationToken.None);

        // AC #6 pins the validation query as the deterministic literal "hybrid search".
        first.ValidationQuery.ShouldBe(QuickstartSampleFlow.ValidationQuery);
        second.ValidationQuery.ShouldBe(QuickstartSampleFlow.ValidationQuery);

        // Per-run disambiguation is carried in RunToken, embedded in the document body.
        first.RunToken.ShouldNotBeNullOrWhiteSpace();
        second.RunToken.ShouldNotBeNullOrWhiteSpace();
        first.RunToken.ShouldNotBe(second.RunToken);
        client.IngestedContents[0].ShouldContain(first.RunToken);
        client.IngestedContents[1].ShouldContain(second.RunToken);
    }

    [Fact]
    public async Task IngestSample_RemoteError_ReturnsFailure()
    {
        var client = new StubSampleClient();
        client.IngestException = new MemoriesRemoteException(
            HttpStatusCode.BadRequest,
            new ErrorResponse("INVALID_INPUT", "bad payload", "fix it"));

        var flow = new QuickstartSampleFlow(client, TimeProvider.System);
        SampleIngestResult result = await flow.IngestSampleAsync("acme", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_INPUT");
    }

    [Fact]
    public async Task ValidateSearch_ImmediateMatch_Returns()
    {
        const string runToken = "quickstartvalidationabc123";
        var client = new StubSampleClient
        {
            RunTokenInSnippet = runToken,
            PositiveResults = 1,
            NegativeResults = 0,
        };

        var flow = new QuickstartSampleFlow(client, TimeProvider.System);
        SampleValidationResult result = await flow.ValidateSearchAsync(
            "acme",
            "case-1",
            runToken,
            CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.PositiveResultCount.ShouldBe(1);
        result.FailureKind.ShouldBe(SampleValidationFailureKind.None);
        client.ObservedQueries[0].ShouldBe(QuickstartSampleFlow.ValidationQuery);
        client.SearchCalls.ShouldBe(1);
    }

    [Fact]
    public async Task ValidateSearch_RetriesThenSucceeds()
    {
        const string runToken = "quickstartvalidationretry";
        var client = new StubSampleClient
        {
            PositiveResults = 0,
            ResultsAfterPositiveCalls = 2,
            NegativeResults = 0,
            RunTokenInSnippet = runToken,
        };

        var clock = new FakeTimeProvider();
        var flow = new QuickstartSampleFlow(client, clock);

        Task<SampleValidationResult> task = flow.ValidateSearchAsync("acme", "case-1", runToken, CancellationToken.None);

        for (int i = 0; i < 6; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            await Task.Yield();
        }

        SampleValidationResult result = await task;
        result.Success.ShouldBeTrue();
        client.HybridCalls.ShouldBeGreaterThanOrEqualTo(2); // positive retries
        client.SearchCalls.ShouldBe(1); // syntactic canary
    }

    [Fact]
    public async Task ValidateSearch_Fails_WhenPositiveReturnsZeroAfterAllRetries()
    {
        var client = new StubSampleClient
        {
            PositiveResults = 0,
            NegativeResults = 0,
        };

        var clock = new FakeTimeProvider();
        var flow = new QuickstartSampleFlow(client, clock);

        Task<SampleValidationResult> task = flow.ValidateSearchAsync("acme", "case-1", "quickstartvalidationmissing", CancellationToken.None);

        for (int i = 0; i < 10; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            await Task.Yield();
        }

        SampleValidationResult result = await task;
        result.Success.ShouldBeFalse();
        result.FailureKind.ShouldBe(SampleValidationFailureKind.PositiveReturnedZero);
    }

    [Fact]
    public async Task ValidateSearch_Fails_WhenNegativeCanaryReturnsResults()
    {
        const string runToken = "quickstartvalidationcanary";
        var client = new StubSampleClient
        {
            RunTokenInSnippet = runToken,
            PositiveResults = 2,
            NegativeResults = 1,
        };

        var flow = new QuickstartSampleFlow(client, TimeProvider.System);
        SampleValidationResult result = await flow.ValidateSearchAsync("acme", "case-1", runToken, CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.FailureKind.ShouldBe(SampleValidationFailureKind.NegativeCanaryReturnedResults);
        client.SearchCalls.ShouldBe(1);
    }

    [Fact]
    public async Task ValidateSearch_IgnoresResultsWithoutRunToken_UntilTokenAppears()
    {
        // Revision 0.4 — Pre-mortem E: results without the run token in the snippet are
        // semantic-only neighbours. Validation must keep retrying until a result carries this
        // run's token (or all retries are exhausted).
        const string runToken = "quickstartvalidationlexical";
        var client = new StubSampleClient
        {
            PositiveResults = 1,
            RunTokenAppearsAfterPositiveCalls = 2,
            RunTokenInSnippet = runToken,
            ResultsAfterPositiveCalls = 2,
            NegativeResults = 0,
        };

        var clock = new FakeTimeProvider();
        var flow = new QuickstartSampleFlow(client, clock);

        Task<SampleValidationResult> task = flow.ValidateSearchAsync("acme", "case-1", runToken, CancellationToken.None);

        for (int i = 0; i < 6; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            await Task.Yield();
        }

        SampleValidationResult result = await task;

        result.Success.ShouldBeTrue();
        client.HybridCalls.ShouldBeGreaterThanOrEqualTo(2);
        client.SearchCalls.ShouldBe(1);
    }

    private sealed class StubSampleClient : MemoriesClient
    {
        public List<Case> Cases { get; } = [];

        public int CreateCaseCalls { get; private set; }

        public int IngestCalls { get; private set; }

        public int HybridCalls { get; private set; }

        public int SearchCalls { get; private set; }

        public List<string> IngestedContents { get; } = [];

        public List<string> ObservedQueries { get; } = [];

        public MemoriesRemoteException? IngestException { get; set; }

        public int PositiveResults { get; set; }

        public int NegativeResults { get; set; }

        public int ResultsAfterPositiveCalls { get; set; }

        public int RunTokenAppearsAfterPositiveCalls { get; set; }

        public string? RunTokenInSnippet { get; set; }

        public StubSampleClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public override Task<IReadOnlyList<Case>> ListCasesAsync(string tenantId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Case>>(Cases.ToList());

#pragma warning disable HXL001
        public override Task<Case> CreateCaseAsync(string tenantId, string name, string? description, CancellationToken ct)
        {
            CreateCaseCalls++;
            var created = new Case(
                Id: $"case-{Cases.Count}",
                TenantId: tenantId,
                Name: name,
                Description: description,
                Status: CaseStatus.Active,
                CreatedAt: DateTimeOffset.UtcNow,
                LastUpdated: DateTimeOffset.UtcNow,
                MemoryUnitCount: 0);
            Cases.Add(created);
            return Task.FromResult(created);
        }

        public override Task<string> IngestAsync(
            string tenantId,
            string caseId,
            string sourceUri,
            byte[] content,
            string contentType,
            string ingestedBy,
            IReadOnlyDictionary<string, MetadataField>? metadata,
            CancellationToken ct)
        {
            IngestCalls++;
            if (IngestException is not null)
            {
                throw IngestException;
            }

            IngestedContents.Add(Encoding.UTF8.GetString(content));

            return Task.FromResult("workflow-123");
        }
#pragma warning restore HXL001

        public override Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
        {
            HybridCalls++;
            ObservedQueries.Add(request.Query);

            int count = HybridCalls >= ResultsAfterPositiveCalls && ResultsAfterPositiveCalls > 0
                ? 1
                : PositiveResults;

            bool includeRunToken = RunTokenInSnippet is not null
                && (RunTokenAppearsAfterPositiveCalls == 0 || HybridCalls >= RunTokenAppearsAfterPositiveCalls);

            string snippet = includeRunToken
                ? $"Quickstart validation token: {RunTokenInSnippet}. (sample)"
                : "Sample snippet without run token.";

            var results = new List<FusedScoredResult>();
            for (int i = 0; i < count; i++)
            {
                results.Add(new FusedScoredResult
                {
                    MemoryUnitId = $"mu-{i}",
                    CompositeScore = 0.75,
                    ContentSnippet = snippet,
                    SourceUri = "quickstart://sample",
                    SourceType = SourceType.File,
                    SyntacticScore = 0.82,
                });
            }

            return Task.FromResult(new HybridSearchResult
            {
                Results = results,
                TotalCount = count,
                Degraded = false,
                UnavailableAxes = [],
                Query = request.Query,
            });
        }

        public override Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
        {
            SearchCalls++;
            ObservedQueries.Add($"{request.Axis}:{request.Query}");

            int count = string.Equals(request.Query, QuickstartSampleFlow.NegativeCanaryQuery, StringComparison.Ordinal)
                ? NegativeResults
                : 0;

            var results = Enumerable.Range(0, count)
                .Select(i => new ScoredResult
                {
                    MemoryUnitId = $"mu-canary-{i}",
                    Score = 0.91,
                    ContentSnippet = "Sample snippet",
                    SourceUri = "quickstart://sample",
                    SourceType = SourceType.File,
                    Axis = request.Axis ?? string.Empty,
                })
                .ToList();

            return Task.FromResult(new SearchResult
            {
                Results = results,
                TotalCount = count,
                HasIndexedMemoryUnits = true,
                Query = request.Query ?? string.Empty,
            });
        }
    }
}
