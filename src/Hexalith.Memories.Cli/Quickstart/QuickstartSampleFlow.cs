// <copyright file="QuickstartSampleFlow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

using System.Linq;
using System.Text;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Sample ingestion + validation search for wizard steps 5 and 6. Embeds a deterministic sample
/// document (no file-IO dependency) and confirms the full pipeline by ingesting the sample, then
/// searching for a deterministic term that MUST match the sample.
/// </summary>
public sealed class QuickstartSampleFlow
{
    /// <summary>The default quickstart case name used for idempotent rerun.</summary>
    public const string DefaultCaseName = "quickstart-default";

    /// <summary>
    /// The deterministic validation query run by step 6 (spec AC #6 pins this exact literal — users
    /// can copy-paste it into `memories search query` for manual re-runs). Matches terms embedded in
    /// <see cref="SampleDocumentText"/>. Per-run disambiguation is carried separately in the
    /// document content via a run-unique token (see <see cref="BuildSampleDocument"/>).
    /// </summary>
    public const string ValidationQuery = "hybrid search";

    /// <summary>
    /// Guaranteed-absent canary query used by the negative-match check (Revision 0.4 — Pre-mortem E).
    /// Must NOT appear in <see cref="SampleDocumentText"/>.
    /// </summary>
    public const string NegativeCanaryQuery = "quickstartnomatchcanarytokenx9f4b2p7m1";

    /// <summary>
    /// Fresh purpose-written sample prose (~200 words) describing a generic memory system. Embeds the
    /// deterministic validation keywords (<c>hybrid</c>, <c>search</c>, <c>memory</c>, <c>tenant</c>,
    /// <c>case</c>). Stay generic-descriptive — no PII, credentials, internal URLs, or instruction-shaped
    /// prose (anti-pattern #13; Revision 0.4 — Red Team attack 1). If future stories pass memory unit
    /// content to LLMs, validate this sample does not contain prompt-injection-shaped phrases.
    /// </summary>
    public const string SampleDocumentText = """
        This is a sample memory unit ingested by the memories quickstart wizard. It demonstrates the
        three-axis hybrid search pipeline — syntactic indexing, semantic vector embedding, and causal
        graph edges — all scoped to a tenant and a case. When you run the wizard against a fresh
        server, this document is stored under a sample tenant with a quickstart-default case so that
        the validation search at the end of the wizard has something to retrieve.

        A memory unit captures a single piece of content: a file, a URL, or an event from an upstream
        system. Each memory unit is associated with metadata that records where it came from, when it
        was ingested, and how confident the extractor was about any structured fields it pulled from
        the raw body. Hybrid search combines the three axes so that a query can find this memory even
        when only one or two axes are healthy — keyword matches from syntactic indexing, semantic
        similarity from vector embeddings, and related-work traversal through the graph.

        The wizard uses this sample to confirm that the end-to-end pipeline is working after the first
        run. When you see this memory unit returned from the validation search, you know ingestion,
        embedding, and search are all wired up correctly. Feel free to delete it or leave it in place
        as a reference demo.
        """;

    /// <summary>The content-type used when ingesting the sample text.</summary>
    private const string SampleContentType = "text/plain";

    /// <summary>Ingested-by identifier recorded against the sample memory unit.</summary>
    private const string SampleIngestedBy = "quickstart-wizard";

    private const string ValidationTokenPrefix = "quickstartvalidation";

    private static readonly TimeSpan SearchInnerTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SearchBackoff = TimeSpan.FromSeconds(2);
    private const int MaxValidationAttempts = 3;

    private readonly MemoriesClient _client;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="QuickstartSampleFlow"/> class.</summary>
    /// <param name="client">The REST client.</param>
    /// <param name="timeProvider">The time provider.</param>
    public QuickstartSampleFlow(MemoriesClient client, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _client = client;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Ensures a sample case exists inside <paramref name="tenantId"/> (reusing the first existing
    /// quickstart-default case if one is present — idempotent rerun) and ingests the embedded sample
    /// document into it.
    /// </summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ingest result.</returns>
    public async Task<SampleIngestResult> IngestSampleAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string caseId = await EnsureSampleCaseAsync(tenantId, ct).ConfigureAwait(false);
        string runToken = CreateRunToken();
        byte[] bytes = Encoding.UTF8.GetBytes(BuildSampleDocument(runToken));

        long startTimestamp = _timeProvider.GetTimestamp();
        string workflowInstanceId;
        try
        {
#pragma warning disable HXL001 // Story 7.4 uses the experimental ingest client method by design.
            workflowInstanceId = await _client.IngestAsync(
                tenantId: tenantId,
                caseId: caseId,
                sourceUri: $"quickstart://{tenantId}/{caseId}/{runToken}",
                content: bytes,
                contentType: SampleContentType,
                ingestedBy: SampleIngestedBy,
                metadata: new Dictionary<string, MetadataField>(StringComparer.Ordinal)
                {
                    ["origin"] = new MetadataField("quickstart", MetadataOrigin.Ai, Confidence: 1.0f),
                    ["wizardVersion"] = new MetadataField("7.4", MetadataOrigin.Ai, Confidence: 1.0f),
                    ["runToken"] = new MetadataField(runToken, MetadataOrigin.Ai, Confidence: 1.0f),
                },
                ct).ConfigureAwait(false);
#pragma warning restore HXL001
        }
        catch (MemoriesRemoteException ex)
        {
            return new SampleIngestResult(
                Success: false,
                CaseId: caseId,
                MemoryUnitId: null,
                ValidationQuery: ValidationQuery,
                RunToken: runToken,
                ErrorCode: ex.Error.Code,
                Diagnostic: $"Sample ingestion failed: {ex.Error.Message}");
        }

        TimeSpan elapsed = _timeProvider.GetElapsedTime(startTimestamp);
        string displayId = string.IsNullOrEmpty(workflowInstanceId) ? "(server did not return an id)" : workflowInstanceId;
        return new SampleIngestResult(
            Success: true,
            CaseId: caseId,
            MemoryUnitId: workflowInstanceId,
            ValidationQuery: ValidationQuery,
            RunToken: runToken,
            ErrorCode: null,
            Diagnostic: $"Ingested sample document '{displayId}' ({elapsed.TotalMilliseconds:F0}ms).");
    }

    /// <summary>
    /// Runs a hybrid search for the current run's validation query, retrying up to three times with
    /// 2-second backoff to tolerate async-write settling. Then runs a negative-match canary — a
    /// guaranteed-absent query that must return zero results.
    /// </summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="caseId">The case id where the sample was ingested.</param>
    /// <param name="validationQuery">The per-run validation query embedded in the current sample document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    public async Task<SampleValidationResult> ValidateSearchAsync(string tenantId, string caseId, string runToken, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(runToken);

        var positiveRequest = new HybridSearchRequest(
            TenantId: tenantId,
            Query: ValidationQuery,
            CaseId: caseId,
            MaxResults: 10,
            Explain: false);

        int positiveCount = 0;
        bool positiveMatched = false;
        double? topScore = null;
        string? lastError = null;
        for (int attempt = 1; attempt <= MaxValidationAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var innerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                innerCts.CancelAfter(SearchInnerTimeout);
                HybridSearchResult result = await _client
                    .HybridSearchAsync(positiveRequest, innerCts.Token)
                    .ConfigureAwait(false);

                FusedScoredResult? runMatched = result.Results
                    .FirstOrDefault(r => r.ContentSnippet?.Contains(runToken, StringComparison.Ordinal) == true);
                if (runMatched is not null)
                {
                    positiveCount = result.Results.Count;
                    positiveMatched = true;
                    topScore = runMatched.CompositeScore;
                    break;
                }

                if (result.Results.Count > 0)
                {
                    lastError = $"Search returned {result.Results.Count} result(s), but none carry this run's token in the snippet yet.";
                }
            }
            catch (MemoriesRemoteException ex)
            {
                lastError = ex.Error.Message;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Inner-timeout (CancelAfter) fired — retry. Caller-cancellation falls through to
                // the outer throw via the ct.ThrowIfCancellationRequested at the top of the loop.
                lastError = $"Search attempt {attempt} exceeded the {SearchInnerTimeout.TotalSeconds:F0}s inner timeout.";
            }

            if (attempt < MaxValidationAttempts)
            {
                try
                {
                    await Task.Delay(SearchBackoff, _timeProvider, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        if (!positiveMatched)
        {
            return new SampleValidationResult(
                Success: false,
                PositiveResultCount: positiveCount,
                NegativeResultCount: 0,
                TopScore: null,
                FailureKind: SampleValidationFailureKind.PositiveReturnedZero,
                Diagnostic: $"Validation search returned zero results matching this run's sample after {MaxValidationAttempts} attempts. Sample ingestion may not have completed indexing. {lastError ?? string.Empty}".Trim());
        }

        // Negative-match canary — must return zero for a guaranteed-absent token on the syntactic
        // axis. Hybrid search can legitimately surface semantic nearest-neighbors for nonsense
        // queries, which would make a "zero results" canary nondeterministic.
        var canaryRequest = new SearchRequest(
            TenantId: tenantId,
            Axis: "syntactic",
            Query: NegativeCanaryQuery,
            CaseId: caseId,
            MaxResults: 1,
            Explain: false);
        int canaryCount;
        try
        {
            using var innerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            innerCts.CancelAfter(SearchInnerTimeout);
            SearchResult canaryResult = await _client
                .SearchAsync(canaryRequest, innerCts.Token)
                .ConfigureAwait(false);
            canaryCount = canaryResult.Results.Count;
        }
        catch (MemoriesRemoteException ex)
        {
            return new SampleValidationResult(
                Success: false,
                PositiveResultCount: positiveCount,
                NegativeResultCount: 0,
                TopScore: topScore,
                FailureKind: SampleValidationFailureKind.NegativeCanaryError,
                Diagnostic: $"Negative-match canary query failed unexpectedly: {ex.Error.Message}.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new SampleValidationResult(
                Success: false,
                PositiveResultCount: positiveCount,
                NegativeResultCount: 0,
                TopScore: topScore,
                FailureKind: SampleValidationFailureKind.NegativeCanaryError,
                Diagnostic: $"Negative-match canary query exceeded the {SearchInnerTimeout.TotalSeconds:F0}s inner timeout.");
        }

        if (canaryCount > 0)
        {
            return new SampleValidationResult(
                Success: false,
                PositiveResultCount: positiveCount,
                NegativeResultCount: canaryCount,
                TopScore: topScore,
                FailureKind: SampleValidationFailureKind.NegativeCanaryReturnedResults,
                Diagnostic: $"Negative-match canary returned {canaryCount} results for a query guaranteed absent from the sample — search pipeline is not distinguishing match from no-match.");
        }

        string scoreDisplay = topScore is null
            ? string.Empty
            : $" (score {topScore.Value:F3})";
        return new SampleValidationResult(
            Success: true,
            PositiveResultCount: positiveCount,
            NegativeResultCount: 0,
            TopScore: topScore,
            FailureKind: SampleValidationFailureKind.None,
            Diagnostic: $"Validation search returned {positiveCount} result{(positiveCount == 1 ? string.Empty : "s")}; top result matches sample{scoreDisplay}. Negative canary returned zero results as expected.");
    }

    private async Task<string> EnsureSampleCaseAsync(string tenantId, CancellationToken ct)
    {
        IReadOnlyList<Case> cases = await _client.ListCasesAsync(tenantId, ct).ConfigureAwait(false);
        Case? existing = cases.FirstOrDefault(c => string.Equals(c.Name, DefaultCaseName, StringComparison.Ordinal));
        if (existing is not null)
        {
            return existing.Id;
        }

#pragma warning disable HXL001 // Story 7.4 uses the experimental case-create client method by design.
        Case created = await _client
            .CreateCaseAsync(tenantId, DefaultCaseName, "Sample case created by the memories quickstart wizard.", ct)
            .ConfigureAwait(false);
#pragma warning restore HXL001
        return created.Id;
    }

    private static string BuildSampleDocument(string runToken)
    {
        var builder = new StringBuilder(SampleDocumentText.Length + runToken.Length + 64);

        // Place the run token at the START so it survives snippet truncation on the validation
        // search (FusedScoredResult.ContentSnippet is a truncated preview — spec 7.4 doesn't pin
        // the max length, but leading bytes are the safest anchor).
        _ = builder
            .Append("Quickstart validation token: ")
            .Append(runToken)
            .Append('.')
            .AppendLine()
            .AppendLine()
            .Append(SampleDocumentText);

        return builder.ToString();
    }

    private static string CreateRunToken()
        => $"{ValidationTokenPrefix}{Guid.NewGuid():N}";
}

/// <summary>Outcome of the sample-ingestion step.</summary>
/// <param name="Success">True when the ingest request was accepted.</param>
/// <param name="CaseId">The case id the sample was ingested into (carried into the validation step).</param>
/// <param name="MemoryUnitId">Workflow instance id returned by the server (may be null if the server emits an empty body on 2xx).</param>
/// <param name="ValidationQuery">The deterministic validation query (always <see cref="QuickstartSampleFlow.ValidationQuery"/>) used by step 6 and surfaced in failure suggestions.</param>
/// <param name="RunToken">The per-run unique token embedded in the sample document; the validation step matches on the result's content snippet for run-disambiguation.</param>
/// <param name="ErrorCode">Catalog or server error code on failure.</param>
/// <param name="Diagnostic">Human-readable outcome message.</param>
public sealed record SampleIngestResult(
    bool Success,
    string CaseId,
    string? MemoryUnitId,
    string ValidationQuery,
    string RunToken,
    string? ErrorCode,
    string Diagnostic);

/// <summary>
/// Why a validation search failed — carried on <see cref="SampleValidationResult"/> for step 6's
/// failure-specific diagnostic rendering.
/// </summary>
public enum SampleValidationFailureKind
{
    /// <summary>Validation passed.</summary>
    None,

    /// <summary>The positive-match query returned zero results after all retries.</summary>
    PositiveReturnedZero,

    /// <summary>The negative-match canary returned one or more results.</summary>
    NegativeCanaryReturnedResults,

    /// <summary>The negative-match canary search itself failed with a transport/server error.</summary>
    NegativeCanaryError,
}

/// <summary>Outcome of the validation-search step.</summary>
/// <param name="Success">True when both the positive match and the negative canary passed.</param>
/// <param name="PositiveResultCount">Result count for the positive query.</param>
/// <param name="NegativeResultCount">Result count for the negative canary (expected zero).</param>
/// <param name="TopScore">Fused score of the top positive result.</param>
/// <param name="FailureKind">When <see cref="Success"/> is false, identifies which gate failed.</param>
/// <param name="Diagnostic">Human-readable outcome message.</param>
public sealed record SampleValidationResult(
    bool Success,
    int PositiveResultCount,
    int NegativeResultCount,
    double? TopScore,
    SampleValidationFailureKind FailureKind,
    string Diagnostic);
