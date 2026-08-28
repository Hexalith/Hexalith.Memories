// <copyright file="DerivedStoreCorrectionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Durably resumes a deterministic correction operation until terminal convergence or failure.</summary>
internal sealed class DerivedStoreCorrectionWorkflow
    : Workflow<DerivedStoreCorrectionWorkflowInput, DerivedStoreCorrectionStatus>
{
    /// <inheritdoc/>
    public override async Task<DerivedStoreCorrectionStatus> RunAsync(
        WorkflowContext context,
        DerivedStoreCorrectionWorkflowInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        context.SetCustomStatus(DerivedStoreCorrectionState.Running.ToString());
        var retry = new WorkflowTaskOptions(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2,
            maxRetryInterval: TimeSpan.FromMinutes(5)));
        DerivedStoreCorrectionStatus status = await context.CallActivityAsync<DerivedStoreCorrectionStatus>(
            nameof(ApplyDerivedStoreCorrectionActivity),
            input,
            retry);
        context.SetCustomStatus(status.State.ToString());
        return status;
    }
}
