// <copyright file="CounterTransitionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for <c>UpdateCaseIngestionCounterActivity</c> (Story 6.3 FR10). Stage values:
/// <c>"none"</c>, <c>"queued"</c>, <c>"extracting"</c>, <c>"embedding"</c>, <c>"indexing"</c>.
/// <c>previousStage="none"</c> means increment-only; <c>nextStage="none"</c> means decrement-only.
/// <c>TransitionId</c> is <c>"{instanceId}:{sequence}"</c> so the actor deduplicates replayed transitions.</summary>
public sealed record CounterTransitionInput(
    string TenantId,
    string CaseId,
    string PreviousStage,
    string NextStage,
    string TransitionId,
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
