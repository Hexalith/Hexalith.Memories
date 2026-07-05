namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

internal sealed class ReversingResultFuser : IResultFuser
{
    public int CallCount { get; private set; }

    public ValueTask<IReadOnlyList<FusedScoredResult>> RerankAsync(
        SearchQuery query,
        FusionWeights weights,
        IReadOnlyList<FusedScoredResult> results,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return ValueTask.FromResult<IReadOnlyList<FusedScoredResult>>(results.Reverse().ToList());
    }
}
