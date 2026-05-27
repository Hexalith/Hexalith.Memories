// <copyright file="IUrlContentFetcher.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Fetches the body of a URL subject to the SSRF allow-list, size cap, and redirect budget.</summary>
public interface IUrlContentFetcher
{
    /// <summary>Fetches <paramref name="url"/> and returns its body. Throws <see cref="UrlFetchException"/> for any failure classified by the fetcher.</summary>
    Task<UrlFetchResult> FetchAsync(Uri url, CancellationToken cancellationToken);
}
