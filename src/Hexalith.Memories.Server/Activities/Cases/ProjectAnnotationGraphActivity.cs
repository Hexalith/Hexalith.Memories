// <copyright file="ProjectAnnotationGraphActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using StackExchange.Redis;

/// <summary>Projects an annotation stub node and ANNOTATES edge into FalkorDB.</summary>
internal sealed class ProjectAnnotationGraphActivity(
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder) : WorkflowActivity<AnnotationProjectionInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, AnnotationProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());
        (string stubQuery, IDictionary<string, object> stubParams) = graphQueryBuilder.BuildMergeStubNode(
            input.AnnotationMemoryUnitId,
            DateTimeOffset.UtcNow);
        await falkor.QueryAsync(input.TenantId, stubQuery, stubParams).ConfigureAwait(false);

        (string edgeQuery, IDictionary<string, object> edgeParams) = graphQueryBuilder.BuildMergeEdge(
            input.AnnotationMemoryUnitId,
            input.TargetMemoryUnitId,
            EdgeType.Annotates,
            EdgeTypeDefaults.Annotates,
            EdgeOrigin.Explicit);
        await falkor.QueryAsync(input.TenantId, edgeQuery, edgeParams).ConfigureAwait(false);
        return true;
    }
}
