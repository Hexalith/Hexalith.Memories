// <copyright file="InsecureTokenTransportException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>Thrown when the resolver would send a token over plain HTTP to a non-localhost host (anti-pattern #17).</summary>
public sealed class InsecureTokenTransportException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="InsecureTokenTransportException"/> class.</summary>
    /// <param name="endpoint">The endpoint the CLI was about to send a token to.</param>
    public InsecureTokenTransportException(Uri endpoint)
        : base($"Refusing to send API token over http:// to non-localhost host '{endpoint.Host}'. Use https:// or unset the token.")
    {
        Endpoint = endpoint;
    }

    /// <summary>Gets the endpoint that triggered the guard.</summary>
    public Uri Endpoint { get; }

    /// <summary>Returns <see langword="true"/> when a token + endpoint combination should be rejected.</summary>
    /// <param name="endpoint">The resolved endpoint.</param>
    /// <param name="apiToken">The resolved API token.</param>
    /// <returns><see langword="true"/> when the guard trips.</returns>
    public static bool ShouldRefuse(Uri endpoint, string? apiToken)
    {
        if (string.IsNullOrEmpty(apiToken))
        {
            return false;
        }

        if (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string host = endpoint.Host;
        return !(string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.Ordinal));
    }
}
