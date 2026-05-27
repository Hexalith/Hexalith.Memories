// <copyright file="ResolvedConfigPipeline.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>
/// Walks the registered <see cref="IConfigurationSource"/> list in DI order and returns the first
/// non-empty endpoint. Token resolution walks the same list independently — the first non-null token
/// wins even if it comes from a lower-priority source than the endpoint (AC #3a).
/// </summary>
public sealed class ResolvedConfigPipeline
{
    private readonly IReadOnlyList<IConfigurationSource> _sources;

    /// <summary>Initializes a new instance with the registered sources in priority order.</summary>
    /// <param name="sources">The sources, highest priority first.</param>
    public ResolvedConfigPipeline(IEnumerable<IConfigurationSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = [.. sources];
    }

    /// <summary>Resolves the final endpoint, token, and winning source.</summary>
    /// <returns>The <see cref="ResolvedConfig"/>.</returns>
    public ResolvedConfig Resolve()
    {
        Uri? winningEndpoint = null;
        string? winningToken = null;
        string? endpointSource = null;

        foreach (IConfigurationSource source in _sources)
        {
            if (!source.TryResolve(out Uri? endpoint, out string? apiToken))
            {
                continue;
            }

            if (winningEndpoint is null && endpoint is not null)
            {
                winningEndpoint = endpoint;
                endpointSource = source.SourceName;
            }

            if (winningToken is null && !string.IsNullOrEmpty(apiToken))
            {
                winningToken = apiToken;
            }

            if (winningEndpoint is not null && winningToken is not null)
            {
                break;
            }
        }

        if (winningEndpoint is null)
        {
            // Default source guarantees an endpoint — this is only reached if the caller forgot to register it.
            throw new InvalidOperationException(
                "No configuration source supplied an endpoint. Register DefaultConfigurationSource as the last tier.");
        }

        return new ResolvedConfig(winningEndpoint, winningToken, endpointSource ?? nameof(DefaultConfigurationSource));
    }
}
