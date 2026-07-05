// <copyright file="IEmbeddingProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Provider strategy that owns authentication, request construction, and response parsing for a single embedding
/// provider. Shared transport, auth-retry, rate-limit, and redaction behavior lives in <see cref="EmbeddingProviderTransport"/>
/// so provider implementations never carry copy-pasted send/read/retry/redaction logic (Story 23.9, AC1/AC4).</summary>
internal interface IEmbeddingProvider
{
    /// <summary>Gets the human-readable provider name used in shared transport error messages.</summary>
    string DisplayName { get; }

    /// <summary>Pre-reads and caches the provider credential secret so configuration failures surface before rate-limit
    /// budget is consumed. Does not acquire provider tokens.</summary>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PrimeAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct);

    /// <summary>Acquires the credentials used to authenticate an embedding request.</summary>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The resolved credentials.</returns>
    Task<EmbeddingProviderCredentials> AuthenticateAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct);

    /// <summary>Refreshes credentials after a 401/403 response by evicting cached secrets and re-acquiring them.</summary>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The refreshed credentials.</returns>
    Task<EmbeddingProviderCredentials> RefreshCredentialsAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct);

    /// <summary>Builds the provider-specific HTTP request for one or more input texts.</summary>
    /// <param name="texts">The ordered input texts. For a single request (<paramref name="batch"/> is <c>false</c>) only the first item is used.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="credentials">The credentials applied to the request's authentication header.</param>
    /// <param name="batch">Whether to build the provider batch request shape.</param>
    /// <returns>The prepared HTTP request message.</returns>
    HttpRequestMessage BuildRequest(IReadOnlyList<string> texts, TenantEmbeddingConfig config, EmbeddingProviderCredentials credentials, bool batch);

    /// <summary>Parses a successful provider response body into ordered embedding vectors.</summary>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="expectedCount">The number of submitted inputs; batch responses must return exactly this many vectors.</param>
    /// <param name="expectedDimensions">The configured vector dimension every returned vector must match.</param>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="batch">Whether to parse the provider batch response shape.</param>
    /// <returns>The ordered embedding vectors.</returns>
    IReadOnlyList<float[]> ParseResponse(string responseBody, int expectedCount, int expectedDimensions, string tenantId, bool batch);
}
