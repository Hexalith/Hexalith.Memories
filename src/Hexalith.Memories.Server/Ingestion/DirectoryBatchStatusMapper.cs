// <copyright file="DirectoryBatchStatusMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Maps workflow runtime state into the user-facing directory batch status contract.
/// </summary>
internal static class DirectoryBatchStatusMapper
{
    internal static BatchInstanceStatus MapInstance(BatchFileRef file, WorkflowState? workflowState)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (workflowState is null || !workflowState.Exists)
        {
            return new BatchInstanceStatus(file.InstanceId, "queued", null, file.SourceUri);
        }

        string status = workflowState.RuntimeStatus switch
        {
            WorkflowRuntimeStatus.Pending => "queued",
            WorkflowRuntimeStatus.Running => "extracting",
            WorkflowRuntimeStatus.Suspended => "queued",
            WorkflowRuntimeStatus.Terminated => "failed",
            WorkflowRuntimeStatus.Failed => "failed",
            WorkflowRuntimeStatus.Completed => ExtractCompletedStatus(workflowState),
            _ => "extracting",
        };

        string? memoryUnitId = null;
        if (workflowState.RuntimeStatus == WorkflowRuntimeStatus.Completed)
        {
            try
            {
                IngestionResult? result = workflowState.ReadOutputAs<IngestionResult>();
                if (result is not null)
                {
                    memoryUnitId = result.MemoryUnitId;
                }
            }
            catch (Exception)
            {
                // Ignore deserialization failures; the runtime status is still the authoritative signal.
            }
        }

        return new BatchInstanceStatus(file.InstanceId, status, memoryUnitId, file.SourceUri);
    }

    internal static string ExtractCompletedStatus(WorkflowState workflowState)
    {
        ArgumentNullException.ThrowIfNull(workflowState);

        try
        {
            IngestionResult? result = workflowState.ReadOutputAs<IngestionResult>();
            if (result is null)
            {
                return "indexed";
            }

            return result.Status switch
            {
                MemoryUnitStatus.Indexed => "indexed",
                MemoryUnitStatus.Failed => "failed",
                _ => result.Status.ToString().ToLowerInvariant(),
            };
        }
        catch (Exception)
        {
            return "indexed";
        }
    }

    internal static BatchStatusCounts BuildCounts(IReadOnlyList<BatchInstanceStatus> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);

        int queued = 0;
        int extracting = 0;
        int embedding = 0;
        int indexing = 0;
        int indexed = 0;
        int failed = 0;

        foreach (BatchInstanceStatus instance in instances)
        {
            switch (instance.Status)
            {
                case "queued": queued++; break;
                case "extracting": extracting++; break;
                case "embedding": embedding++; break;
                case "indexing": indexing++; break;
                case "indexed": indexed++; break;
                case "failed": failed++; break;
                default: extracting++; break;
            }
        }

        return new BatchStatusCounts(queued, extracting, embedding, indexing, indexed, failed);
    }
}