// <copyright file="CaseIngestionCounterLogic.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Hexalith.Memories.Contracts.V1;

/// <summary>Pure transition logic for the case-ingestion counter, mirroring the <see cref="RateLimiterLogic"/>
/// precedent — testable without DAPR plumbing.</summary>
internal sealed class CaseIngestionCounterLogic
{
    private const int MaxTrackedWorkflowSequences = 256;

    /// <summary>Applies a stage transition to the supplied state. Returns the same instance unchanged when
    /// the transitionId has already been applied (idempotent).</summary>
    /// <param name="state">The current actor state.</param>
    /// <param name="previous">The previous-stage bucket (or <c>"none"</c>).</param>
    /// <param name="next">The next-stage bucket (or <c>"none"</c>).</param>
    /// <param name="transitionId">The deterministic transition id from the workflow.</param>
    /// <returns>The new state, or the original instance on idempotent replay.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="previous"/> or <paramref name="next"/> is unknown.</exception>
    public CaseIngestionCounterState Transition(
        CaseIngestionCounterState state,
        string previous,
        string next,
        string transitionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.Equals(state.LastTransitionId, transitionId, StringComparison.Ordinal))
        {
            return state;
        }

        (Dictionary<string, int>? appliedSequences, List<string>? workflowOrder) =
            CreateAppliedSequenceSnapshot(state);
        if (TryParseTransitionId(transitionId, out string? workflowInstanceId, out int sequence))
        {
            appliedSequences ??= new Dictionary<string, int>(StringComparer.Ordinal);
            if (appliedSequences.TryGetValue(workflowInstanceId, out int appliedSequence)
                && sequence <= appliedSequence)
            {
                return state;
            }

            workflowOrder ??= [];
            appliedSequences[workflowInstanceId] = sequence;
            RefreshWorkflowOrder(workflowOrder, workflowInstanceId);
            TrimAppliedSequences(appliedSequences, workflowOrder);
        }

        int q = state.Queued;
        int e = state.Extracting;
        int m = state.Embedding;
        int i = state.Indexing;

        switch (previous)
        {
            case "queued": q = Math.Max(0, q - 1); break;
            case "extracting": e = Math.Max(0, e - 1); break;
            case "embedding": m = Math.Max(0, m - 1); break;
            case "indexing": i = Math.Max(0, i - 1); break;
            case "none": break;
            default: throw new ArgumentException($"Invalid previousStage '{previous}'", nameof(previous));
        }

        switch (next)
        {
            case "queued": q++; break;
            case "extracting": e++; break;
            case "embedding": m++; break;
            case "indexing": i++; break;
            case "none": break;
            default: throw new ArgumentException($"Invalid nextStage '{next}'", nameof(next));
        }

        return new CaseIngestionCounterState(q, e, m, i, transitionId)
        {
            AppliedTransitionSequences = appliedSequences,
            AppliedTransitionWorkflowOrder = workflowOrder?.ToArray(),
        };
    }

    /// <summary>Projects the actor state to the public counts contract.</summary>
    /// <param name="s">The actor state.</param>
    /// <returns>A <see cref="CaseIngestionCounts"/>.</returns>
    public CaseIngestionCounts ToCounts(CaseIngestionCounterState s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new(s.Queued, s.Extracting, s.Embedding, s.Indexing);
    }

    private static (Dictionary<string, int>? AppliedSequences, List<string>? WorkflowOrder)
        CreateAppliedSequenceSnapshot(CaseIngestionCounterState state)
    {
        Dictionary<string, int>? appliedSequences = state.AppliedTransitionSequences is null
            ? null
            : new Dictionary<string, int>(state.AppliedTransitionSequences, StringComparer.Ordinal);
        List<string>? workflowOrder = CreateWorkflowOrderSnapshot(state, appliedSequences);

        // Actor state written before Story 26.7 has only LastTransitionId. Seed that checkpoint on read so
        // an older transition is rejected immediately without requiring an eager state migration. State
        // written by the first Story 26.7 implementation lacks explicit order, so LastTransitionId also
        // reconstructs the only recency fact that version persisted.
        if (TryParseTransitionId(state.LastTransitionId, out string? workflowInstanceId, out int sequence))
        {
            appliedSequences ??= new Dictionary<string, int>(StringComparer.Ordinal);
            if (!appliedSequences.TryGetValue(workflowInstanceId, out int appliedSequence)
                || sequence > appliedSequence)
            {
                appliedSequences[workflowInstanceId] = sequence;
            }

            workflowOrder ??= [];
            RefreshWorkflowOrder(workflowOrder, workflowInstanceId);
        }

        return (appliedSequences, workflowOrder);
    }

    private static List<string>? CreateWorkflowOrderSnapshot(
        CaseIngestionCounterState state,
        Dictionary<string, int>? appliedSequences)
    {
        if (appliedSequences is null)
        {
            return null;
        }

        List<string> workflowOrder = [];
        HashSet<string> addedWorkflowIds = new(StringComparer.Ordinal);
        if (state.AppliedTransitionWorkflowOrder is not null)
        {
            foreach (string workflowInstanceId in state.AppliedTransitionWorkflowOrder)
            {
                if (appliedSequences.ContainsKey(workflowInstanceId)
                    && addedWorkflowIds.Add(workflowInstanceId))
                {
                    workflowOrder.Add(workflowInstanceId);
                }
            }
        }

        // Backward compatibility for state persisted before explicit recency order was introduced.
        foreach (string workflowInstanceId in appliedSequences.Keys)
        {
            if (addedWorkflowIds.Add(workflowInstanceId))
            {
                workflowOrder.Add(workflowInstanceId);
            }
        }

        return workflowOrder;
    }

    private static bool TryParseTransitionId(
        string? transitionId,
        out string workflowInstanceId,
        out int sequence)
    {
        workflowInstanceId = string.Empty;
        sequence = 0;
        if (string.IsNullOrWhiteSpace(transitionId))
        {
            return false;
        }

        int separatorIndex = transitionId.LastIndexOf(':');
        if (separatorIndex <= 0
            || separatorIndex == transitionId.Length - 1
            || !int.TryParse(
                transitionId.AsSpan(separatorIndex + 1),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out sequence)
            || sequence <= 0)
        {
            return false;
        }

        workflowInstanceId = transitionId[..separatorIndex];
        return true;
    }

    private static void RefreshWorkflowOrder(List<string> workflowOrder, string workflowInstanceId)
    {
        _ = workflowOrder.Remove(workflowInstanceId);
        workflowOrder.Add(workflowInstanceId);
    }

    private static void TrimAppliedSequences(
        Dictionary<string, int> appliedSequences,
        List<string> workflowOrder)
    {
        while (appliedSequences.Count > MaxTrackedWorkflowSequences)
        {
            string oldestWorkflowInstanceId = workflowOrder[0];
            workflowOrder.RemoveAt(0);
            _ = appliedSequences.Remove(oldestWorkflowInstanceId);
        }
    }
}
