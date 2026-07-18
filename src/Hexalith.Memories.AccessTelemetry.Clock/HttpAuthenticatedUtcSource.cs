// <copyright file="HttpAuthenticatedUtcSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

using System.Net.Http.Headers;
using System.Net.Http.Json;

/// <summary>HTTPS UTC authority authenticated with deployment-injected bearer material.</summary>
internal sealed class HttpAuthenticatedUtcSource : IAuthenticatedUtcSource
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly string _authenticationToken;

    /// <summary>Initializes an independently configured UTC authority.</summary>
    public HttpAuthenticatedUtcSource(
        HttpClient httpClient,
        string sourceId,
        Uri endpoint,
        string authenticationToken)
    {
        if (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            throw new ArgumentException("Authenticated UTC source endpoints must use HTTPS.", nameof(endpoint));
        }

        _httpClient = httpClient;
        SourceId = sourceId;
        _endpoint = endpoint;
        _authenticationToken = authenticationToken;
    }

    /// <inheritdoc/>
    public string SourceId { get; }

    /// <inheritdoc/>
    public async Task<AuthenticatedUtcSample> GetUtcSampleAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationToken);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        AuthenticatedUtcResponse body = await response.Content.ReadFromJsonAsync<AuthenticatedUtcResponse>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Authenticated UTC source returned an empty response.");
        if (body.UncertaintyMilliseconds is < 0 or > 1000)
        {
            throw new InvalidOperationException("Authenticated UTC source uncertainty is outside the bounded input range.");
        }

        DateTimeOffset midpoint = DateTimeOffset.FromUnixTimeMilliseconds(body.UnixMilliseconds);
        return new AuthenticatedUtcSample(
            SourceId,
            midpoint.AddMilliseconds(-body.UncertaintyMilliseconds),
            midpoint.AddMilliseconds(body.UncertaintyMilliseconds),
            Authenticated: true);
    }
}
