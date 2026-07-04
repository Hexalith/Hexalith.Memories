// <copyright file="IngestionWorkflowStatusMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

/// <summary>Maps Dapr workflow state to the safe public ingestion status contract.</summary>
internal static class IngestionWorkflowStatusMapper
{
    internal static bool TryReadStoredTenantId(WorkflowState? workflowState, out string? tenantId)
    {
        tenantId = null;
        if (!TryReadInput(workflowState, out IngestionInput? input))
        {
            return false;
        }

        tenantId = input.TenantId;
        return !string.IsNullOrWhiteSpace(tenantId);
    }

    internal static bool TryMap(string instanceId, WorkflowState? workflowState, out IngestionWorkflowStatus? status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        status = null;
        if (!TryReadInput(workflowState, out IngestionInput? input))
        {
            return false;
        }

        WorkflowState state = workflowState!;

        string? memoryUnitId = null;
        MemoryUnitStatus? memoryUnitStatus = null;
        string? failureSummary = state.RuntimeStatus switch
        {
            WorkflowRuntimeStatus.Failed => "Workflow failed.",
            WorkflowRuntimeStatus.Terminated => "Workflow was terminated.",
            _ => null,
        };

        if (state.RuntimeStatus == WorkflowRuntimeStatus.Completed)
        {
            try
            {
                IngestionResult? result = state.ReadOutputAs<IngestionResult>();
                if (result is not null)
                {
                    memoryUnitId = result.MemoryUnitId;
                    memoryUnitStatus = result.Status;
                    failureSummary = result.Status == MemoryUnitStatus.Failed
                        ? "Workflow completed with failed ingestion status."
                        : null;
                }
            }
            catch (Exception)
            {
                failureSummary = "Workflow output could not be projected safely.";
            }
        }

        status = new IngestionWorkflowStatus(
            instanceId,
            input.TenantId,
            input.CaseId,
            state.RuntimeStatus.ToString(),
            state.CreatedAt,
            state.LastUpdatedAt,
            memoryUnitId,
            memoryUnitStatus,
            failureSummary);
        return true;
    }

    private static bool TryReadInput(WorkflowState? workflowState, out IngestionInput input)
    {
        input = null!;
        if (workflowState is null || !workflowState.Exists)
        {
            return false;
        }

        try
        {
            IngestionInput? readInput = workflowState.ReadInputAs<IngestionInput>();
            if (readInput is null
                || string.IsNullOrWhiteSpace(readInput.TenantId)
                || string.IsNullOrWhiteSpace(readInput.CaseId))
            {
                return false;
            }

            input = readInput;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
