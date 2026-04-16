// <copyright file="MemoriesAuthHandler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using System.Net.Http.Headers;

using Microsoft.Extensions.Options;

/// <summary>
/// Delegating handler that attaches authentication headers when a token is configured. HTTPS endpoints use
/// <c>Authorization: Bearer {token}</c>. Plain-HTTP loopback endpoints use <c>dapr-api-token: {token}</c>.
/// Plain-HTTP non-loopback endpoints are rejected to avoid sending tokens over insecure transport.
/// </summary>
public sealed class MemoriesAuthHandler : DelegatingHandler
{
    private const string DaprApiTokenHeader = "dapr-api-token";

    private readonly IOptionsMonitor<MemoriesClientOptions> _options;

    /// <summary>Initializes a new instance of the <see cref="MemoriesAuthHandler"/> class.</summary>
    /// <param name="options">The options monitor.</param>
    public MemoriesAuthHandler(IOptionsMonitor<MemoriesClientOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        MemoriesClientOptions snapshot = _options.CurrentValue;
        string? token = snapshot.ApiToken;
        if (!string.IsNullOrEmpty(token))
        {
            Uri? target = request.RequestUri is { IsAbsoluteUri: true } absolute
                ? absolute
                : snapshot.Endpoint;
            if (target is not null)
            {
                if (IsIngressStyle(target))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                else if (IsSidecarStyle(target))
                {
                    request.Headers.Remove(DaprApiTokenHeader);
                    request.Headers.Add(DaprApiTokenHeader, token);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Refusing to send API token over http:// to non-localhost host '{target.Host}'. Use https:// or unset the token.");
                }
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Decides whether the target looks like an ingress URL (attach <c>Authorization: Bearer</c>).
    /// </summary>
    /// <param name="target">The request target URI.</param>
    /// <returns><see langword="true"/> for ingress-style.</returns>
    internal static bool IsIngressStyle(Uri target)
        => string.Equals(target.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool IsSidecarStyle(Uri target)
        => string.Equals(target.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        && (string.Equals(target.Host, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(target.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(target.Host, "::1", StringComparison.Ordinal));
}
