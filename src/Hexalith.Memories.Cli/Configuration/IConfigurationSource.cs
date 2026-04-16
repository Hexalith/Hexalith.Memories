// <copyright file="IConfigurationSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>
/// A source the endpoint resolver consults for its contribution to the final
/// <see cref="ResolvedConfig"/>. Sources are evaluated in DI registration order; the first one that
/// reports a non-empty endpoint wins (AC #3a / #3b).
/// </summary>
public interface IConfigurationSource
{
    /// <summary>
    /// Attempts to resolve config from this source. Sources return partial values: a source may supply
    /// only an endpoint, only a token, or both. The pipeline combines them using the same per-key
    /// precedence (first non-null wins) — so the flag can supply an endpoint and the env var can supply
    /// the token independently.
    /// </summary>
    /// <param name="endpoint">Outputs the endpoint contributed by this source, or <see langword="null"/>.</param>
    /// <param name="apiToken">Outputs the token contributed by this source, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the source contributed at least one value.</returns>
    bool TryResolve(out Uri? endpoint, out string? apiToken);

    /// <summary>Gets the short name used by <c>memories config show</c> (AC #3c) to report which source won.</summary>
    string SourceName { get; }
}
