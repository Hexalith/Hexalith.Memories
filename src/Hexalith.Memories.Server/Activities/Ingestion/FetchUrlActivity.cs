// <copyright file="FetchUrlActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

using System.Diagnostics;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>
/// Workflow activity that fetches the body of a URL via <see cref="IUrlContentFetcher"/>. Throws
/// <see cref="UrlFetchException"/> on failure so the workflow retry policy can handle it.
/// </summary>
public sealed class FetchUrlActivity : WorkflowTraceLinkedActivity<FetchUrlInput, UrlFetchResult>
{
    private readonly IUrlContentFetcher _fetcher;
    private readonly PerTenantConcurrencyGate _gate;
    private readonly ILogger<FetchUrlActivity> _logger;
    private readonly IWorkflowPayloadStore? _payloadStore;

    public FetchUrlActivity(
        IUrlContentFetcher fetcher,
        PerTenantConcurrencyGate gate,
        ILogger<FetchUrlActivity> logger,
        IWorkflowPayloadStore? payloadStore = null)
    {
        ArgumentNullException.ThrowIfNull(fetcher);
        ArgumentNullException.ThrowIfNull(gate);
        ArgumentNullException.ThrowIfNull(logger);
        _fetcher = fetcher;
        _gate = gate;
        _logger = logger;
        _payloadStore = payloadStore;
    }

    /// <inheritdoc/>
    protected override async Task<UrlFetchResult> RunActivityAsync(WorkflowActivityContext context, FetchUrlInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);

        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out Uri? uri))
        {
            throw new UrlFetchException("INVALID_URL", "URL must be an absolute URI.");
        }

        string redacted = IngestionEndpointLog.RedactUrl(uri);
        IngestionEndpointLog.LogUrlFetchStarted(_logger, input.MemoryUnitId, redacted);

        await using IAsyncDisposable lease = await _gate
            .AcquireAsync(input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            UrlFetchResult result = await _fetcher.FetchAsync(uri, CancellationToken.None).ConfigureAwait(false);
            stopwatch.Stop();

            string finalRedacted = Uri.TryCreate(result.FinalUrl, UriKind.Absolute, out Uri? finalUri)
                ? IngestionEndpointLog.RedactUrl(finalUri)
                : redacted;

            IngestionEndpointLog.LogUrlFetchCompleted(
                _logger,
                input.MemoryUnitId,
                result.HttpStatusCode,
                result.ContentLength,
                stopwatch.ElapsedMilliseconds,
                finalRedacted);

            if (_payloadStore is null)
            {
                return result;
            }

            WorkflowPayloadReference reference = await _payloadStore
                .SaveAsync(
                    input.TenantId,
                    input.MemoryUnitId,
                    WorkflowPayloadKind.FetchedUrlBytes,
                    result.ContentBytes,
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);

            return result with
            {
                ContentBytes = [],
                PayloadReference = reference,
            };
        }
        catch (UrlFetchException ex)
        {
            stopwatch.Stop();
            IngestionEndpointLog.LogUrlFetchFailed(
                _logger,
                input.MemoryUnitId,
                ex.ErrorCode,
                ex.HttpStatusCode,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
