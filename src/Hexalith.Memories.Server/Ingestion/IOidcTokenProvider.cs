// <copyright file="IOidcTokenProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Provides OAuth2/OIDC access tokens for embedding gateway requests.</summary>
public interface IOidcTokenProvider
{
    /// <summary>Gets a cached or newly acquired access token for a client credentials grant.</summary>
    /// <param name="tokenEndpoint">The absolute OIDC token endpoint.</param>
    /// <param name="clientId">The OIDC client identifier.</param>
    /// <param name="clientSecret">The OIDC client secret.</param>
    /// <param name="scope">The optional token scope.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The bearer access token.</returns>
    Task<string> GetAccessTokenAsync(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope,
        CancellationToken ct);

    /// <summary>Invalidates a cached access token and acquires a replacement under the per-key guard.</summary>
    /// <param name="tokenEndpoint">The absolute OIDC token endpoint.</param>
    /// <param name="clientId">The OIDC client identifier.</param>
    /// <param name="clientSecret">The OIDC client secret.</param>
    /// <param name="scope">The optional token scope.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The newly acquired bearer access token.</returns>
    Task<string> InvalidateAndRefreshAsync(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope,
        CancellationToken ct);
}
