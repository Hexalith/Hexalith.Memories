// <copyright file="FlagConfigurationSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>
/// Supplies values parsed from the root command's <c>--endpoint</c> / <c>--token</c> global options.
/// Mutable so the CLI entry point can populate the parsed values before the pipeline runs.
/// </summary>
public sealed class FlagConfigurationSource : IConfigurationSource
{
    /// <summary>The endpoint from <c>--endpoint</c>, or <see langword="null"/> when the flag was absent.</summary>
    public Uri? Endpoint { get; set; }

    /// <summary>The token from <c>--token</c>, or <see langword="null"/> when the flag was absent.</summary>
    public string? ApiToken { get; set; }

    /// <inheritdoc />
    public string SourceName => nameof(FlagConfigurationSource);

    /// <inheritdoc />
    public bool TryResolve(out Uri? endpoint, out string? apiToken)
    {
        endpoint = Endpoint;
        apiToken = string.IsNullOrEmpty(ApiToken) ? null : ApiToken;
        return endpoint is not null || apiToken is not null;
    }
}
