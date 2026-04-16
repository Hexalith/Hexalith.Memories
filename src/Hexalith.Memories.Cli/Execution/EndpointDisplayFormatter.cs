// <copyright file="EndpointDisplayFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

/// <summary>Formats endpoint URIs for human-readable CLI output without leaking userinfo or query material.</summary>
internal static class EndpointDisplayFormatter
{
    /// <summary>Returns a sanitized string form of the endpoint for logs and diagnostics.</summary>
    /// <param name="endpoint">The endpoint to format.</param>
    /// <returns>The sanitized endpoint string.</returns>
    public static string Format(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (string.IsNullOrEmpty(endpoint.UserInfo)
            && string.IsNullOrEmpty(endpoint.Query)
            && string.IsNullOrEmpty(endpoint.Fragment))
        {
            return endpoint.ToString();
        }

        UriBuilder builder = new(endpoint)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri.ToString();
    }
}
