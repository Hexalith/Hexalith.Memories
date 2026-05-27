// <copyright file="ConsistencyWorkflowService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Workflows;

/// <summary>
/// Default <see cref="IConsistencyWorkflowService"/> backed by <see cref="DaprWorkflowClient"/>.
/// </summary>
public sealed class ConsistencyWorkflowService : IConsistencyWorkflowService
{
    private readonly DaprWorkflowClient _workflowClient;

    /// <summary>Initializes a new instance of the <see cref="ConsistencyWorkflowService"/> class.</summary>
    public ConsistencyWorkflowService(DaprWorkflowClient workflowClient)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        _workflowClient = workflowClient;
    }

    /// <inheritdoc/>
    public async Task ScheduleVerificationAsync(string instanceId, ConsistencyVerificationInput input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);

        cancellationToken.ThrowIfCancellationRequested();
        _ = await _workflowClient
            .ScheduleNewWorkflowAsync(nameof(ConsistencyVerificationWorkflow), instanceId, input)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ConsistencyVerificationStatus?> GetVerificationStatusAsync(string instanceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        cancellationToken.ThrowIfCancellationRequested();

        WorkflowState? state = await _workflowClient.GetWorkflowStateAsync(instanceId).ConfigureAwait(false);
        return state is null ? null : ProjectVerificationStatus(state, instanceId);
    }

    /// <inheritdoc/>
    public async Task ScheduleRepairAsync(string instanceId, ConsistencyRepairInput input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);

        cancellationToken.ThrowIfCancellationRequested();
        _ = await _workflowClient
            .ScheduleNewWorkflowAsync(nameof(ConsistencyRepairWorkflow), instanceId, input)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ConsistencyRepairStatus?> GetRepairStatusAsync(string instanceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        cancellationToken.ThrowIfCancellationRequested();

        WorkflowState? state = await _workflowClient.GetWorkflowStateAsync(instanceId).ConfigureAwait(false);
        return state is null ? null : ProjectRepairStatus(state, instanceId);
    }

    private static ConsistencyVerificationStatus ProjectVerificationStatus(WorkflowState state, string instanceId)
        => new(
            InstanceId: instanceId,
            Status: state.RuntimeStatus.ToString(),
            CreatedAt: state.CreatedAt,
            LastUpdatedAt: state.LastUpdatedAt,
            Progress: TryReadCustomStatus<ConsistencyWorkflowProgress>(state),
            Result: TryReadOutput<ConsistencyVerificationResult>(state));

    private static ConsistencyRepairStatus ProjectRepairStatus(WorkflowState state, string instanceId)
        => new(
            InstanceId: instanceId,
            Status: state.RuntimeStatus.ToString(),
            CreatedAt: state.CreatedAt,
            LastUpdatedAt: state.LastUpdatedAt,
            Progress: TryReadCustomStatus<ConsistencyWorkflowProgress>(state),
            Result: TryReadOutput<ConsistencyRepairResult>(state));

    private static T? TryReadCustomStatus<T>(WorkflowState state)
        where T : class
    {
        try
        {
            return state.ReadCustomStatusAs<T>();
        }
        catch
        {
            return null;
        }
    }

    private static T? TryReadOutput<T>(WorkflowState state)
        where T : class
    {
        if (state.RuntimeStatus != WorkflowRuntimeStatus.Completed)
        {
            return null;
        }

        try
        {
            return state.ReadOutputAs<T>();
        }
        catch
        {
            return null;
        }
    }
}