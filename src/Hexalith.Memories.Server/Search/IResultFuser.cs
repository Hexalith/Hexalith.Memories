// <copyright file="IResultFuser.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>Post-fusion hook that may reorder already-fused hybrid results.</summary>
internal interface IResultFuser
{
    /// <summary>Returns the final fused result ordering for a hybrid query.</summary>
    /// <param name="query">The original hybrid search query.</param>
    /// <param name="weights">The fusion weights used by the pure fusion engine.</param>
    /// <param name="results">The pure fusion output before pagination.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The reordered fused results.</returns>
    ValueTask<IReadOnlyList<FusedScoredResult>> RerankAsync(
        SearchQuery query,
        FusionWeights weights,
        IReadOnlyList<FusedScoredResult> results,
        CancellationToken cancellationToken);
}
