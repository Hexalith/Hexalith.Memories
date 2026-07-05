// <copyright file="UrlFetchResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Result of fetching a URL via the ingestion workflow's URL fetch activity.</summary>
/// <param name="ContentBytes">Body bytes returned by the remote host (capped by the fetcher).</param>
/// <param name="ContentType">Response Content-Type header (defaults to application/octet-stream when absent).</param>
/// <param name="ContentLength">Number of bytes actually read.</param>
/// <param name="FinalUrl">URL after following redirects (the last hop the fetcher visited).</param>
/// <param name="HttpStatusCode">Final HTTP status code observed (200 on success paths).</param>
/// <param name="PayloadReference">Optional claim-check reference for the fetched body.</param>
public sealed record UrlFetchResult(
    byte[] ContentBytes,
    string ContentType,
    long ContentLength,
    string FinalUrl,
    int HttpStatusCode,
    WorkflowPayloadReference? PayloadReference = null);
