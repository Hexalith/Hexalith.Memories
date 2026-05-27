// <copyright file="StubMemoriesClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

#pragma warning disable HXL001 // MemoriesClient.IngestAsync is HXL001-experimental.

/// <summary>
/// Minimal in-process stand-in for <see cref="MemoriesClient"/>. Override the public virtuals
/// per test. Inherits from <see cref="MemoriesClient"/> so the production tools accept it
/// transparently via DI.
/// </summary>
internal class StubMemoriesClient : MemoriesClient
{
    public StubMemoriesClient()
        : base(
            new HttpClient { BaseAddress = new Uri("http://stub.local/") },
            Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://stub.local/") }),
            NullLogger<MemoriesClient>.Instance)
    {
    }

    public Func<SearchRequest, CancellationToken, Task<SearchResult>>? OnSearch { get; set; }

    public Func<HybridSearchRequest, CancellationToken, Task<HybridSearchResult>>? OnHybridSearch { get; set; }

    public Func<TraversalRequest, CancellationToken, Task<TraversalResult>>? OnTraverse { get; set; }

    public Func<string, string, CancellationToken, Task<Case>>? OnGetCase { get; set; }

    public Func<IngestionInputCapture, CancellationToken, Task<string>>? OnIngest { get; set; }

    public List<SearchRequest> SearchRequests { get; } = [];

    public List<HybridSearchRequest> HybridSearchRequests { get; } = [];

    public List<TraversalRequest> TraversalRequests { get; } = [];

    public List<(string TenantId, string CaseId)> GetCaseCalls { get; } = [];

    public List<IngestionInputCapture> IngestCalls { get; } = [];

    public override Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        SearchRequests.Add(request);
        return OnSearch is not null
            ? OnSearch(request, ct)
            : Task.FromResult(new SearchResult { Results = [], TotalCount = 0, HasIndexedMemoryUnits = true, Query = request.Query ?? string.Empty });
    }

    public override Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
    {
        HybridSearchRequests.Add(request);
        return OnHybridSearch is not null
            ? OnHybridSearch(request, ct)
            : Task.FromResult(new HybridSearchResult { Results = [], TotalCount = 0, Degraded = false, UnavailableAxes = [], Query = request.Query });
    }

    public override Task<TraversalResult> TraverseAsync(
        string tenantId,
        string startNodeId,
        int depth = 2,
        string? caseId = null,
        IReadOnlyList<EdgeType>? edgeTypes = null,
        CancellationToken ct = default,
        int? tokenBudget = null)
    {
        var capture = new TraversalRequest(tenantId, startNodeId, depth, caseId, edgeTypes, tokenBudget);
        TraversalRequests.Add(capture);
        return OnTraverse is not null
            ? OnTraverse(capture, ct)
            : Task.FromResult(new TraversalResult(startNodeId, depth, [], 0));
    }

    public override Task<Case> GetCaseAsync(string tenantId, string caseId, CancellationToken ct = default)
    {
        GetCaseCalls.Add((tenantId, caseId));
        return OnGetCase is not null
            ? OnGetCase(tenantId, caseId, ct)
            : Task.FromResult(new Case(caseId, tenantId, "stub", null, CaseStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0));
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
        var capture = new IngestionInputCapture(tenantId, caseId, sourceUri, content, contentType, ingestedBy, metadata);
        IngestCalls.Add(capture);
        return OnIngest is not null
            ? OnIngest(capture, ct)
            : Task.FromResult("workflow-instance-1");
    }
}

/// <summary>Captured traversal call used by the stub client.</summary>
/// <param name="TenantId">The tenant id.</param>
/// <param name="StartNodeId">The starting node id.</param>
/// <param name="Depth">The clamped depth.</param>
/// <param name="CaseId">The optional case id.</param>
/// <param name="EdgeTypes">The optional edge type filter.</param>
/// <param name="TokenBudget">The optional output token budget.</param>
internal sealed record TraversalRequest(
    string TenantId,
    string StartNodeId,
    int Depth,
    string? CaseId,
    IReadOnlyList<EdgeType>? EdgeTypes,
    int? TokenBudget);

/// <summary>Captured ingestion call used by the stub client.</summary>
/// <param name="TenantId">The tenant id.</param>
/// <param name="CaseId">The case id.</param>
/// <param name="SourceUri">The source URI.</param>
/// <param name="Content">The content bytes.</param>
/// <param name="ContentType">The content type.</param>
/// <param name="IngestedBy">The submitter identity.</param>
/// <param name="Metadata">The metadata map.</param>
internal sealed record IngestionInputCapture(
    string TenantId,
    string CaseId,
    string SourceUri,
    byte[] Content,
    string ContentType,
    string IngestedBy,
    IReadOnlyDictionary<string, MetadataField>? Metadata);

#pragma warning restore HXL001
