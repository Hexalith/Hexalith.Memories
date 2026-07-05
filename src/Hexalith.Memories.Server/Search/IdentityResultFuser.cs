// <copyright file="IdentityResultFuser.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>Default post-fusion hook that preserves the deterministic fusion order.</summary>
internal sealed class IdentityResultFuser : IResultFuser
{
    /// <inheritdoc/>
    public ValueTask<IReadOnlyList<FusedScoredResult>> RerankAsync(
        SearchQuery query,
        FusionWeights weights,
        IReadOnlyList<FusedScoredResult> results,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(results);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(results);
    }
}
