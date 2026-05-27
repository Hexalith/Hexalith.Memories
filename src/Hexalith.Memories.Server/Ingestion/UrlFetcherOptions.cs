// <copyright file="UrlFetcherOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Configuration for the URL fetcher used by FetchUrlActivity. Bound from the "Ingestion:UrlFetcher" section.</summary>
public sealed class UrlFetcherOptions
{
    /// <summary>Gets or sets a value indicating whether private, loopback, link-local, and reserved hosts are permitted. Defaults to false — SSRF defense.</summary>
    public bool AllowPrivateHosts { get; set; }

    /// <summary>Gets or sets the per-request fetch timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Gets or sets the maximum number of redirects followed before TOO_MANY_REDIRECTS is raised.</summary>
    public int MaxRedirects { get; set; } = 5;

    /// <summary>Gets or sets the payload size cap in bytes (default 1 MB, mirrors NFR5).</summary>
    public long MaxBytes { get; set; } = 1_048_576L;
}
