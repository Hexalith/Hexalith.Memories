// <copyright file="ProjectCaseGraphActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;

using StackExchange.Redis;

/// <summary>Projects a case aggregate event into the FalkorDB case node read model.</summary>
internal sealed class ProjectCaseGraphActivity(
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    IGraphQueryBuilder graphQueryBuilder) : WorkflowActivity<ProjectCaseCreatedInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, ProjectCaseCreatedInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = graphQueryBuilder.BuildMergeCaseNode(
            input.CaseId,
            input.Name,
            input.TenantId,
            input.CreatedAt);
        await falkor.QueryAsync(input.TenantId, query, parameters).ConfigureAwait(false);
        return true;
    }
}
