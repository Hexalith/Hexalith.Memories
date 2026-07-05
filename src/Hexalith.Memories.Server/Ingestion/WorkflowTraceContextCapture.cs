// <copyright file="WorkflowTraceContextCapture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;

/// <summary>Captures ambient W3C trace context before scheduling durable workflows.</summary>
internal sealed class WorkflowTraceContextCapture
{
    /// <summary>Captures the current activity context, when one exists.</summary>
    public WorkflowTraceContext? Capture()
    {
        Activity? current = Activity.Current;
        if (string.IsNullOrWhiteSpace(current?.Id))
        {
            return null;
        }

        return new WorkflowTraceContext
        {
            TraceParent = current.Id,
            TraceState = string.IsNullOrWhiteSpace(current.TraceStateString) ? null : current.TraceStateString,
        };
    }

    /// <summary>Applies the current trace context to an ingestion input when no trace context is already present.</summary>
    public IngestionInput Apply(IngestionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.TraceContext is not null ? input : input with { TraceContext = Capture() };
    }
}
