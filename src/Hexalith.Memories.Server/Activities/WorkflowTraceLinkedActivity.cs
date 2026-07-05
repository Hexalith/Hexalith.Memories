// <copyright file="WorkflowTraceLinkedActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities;

using System.Diagnostics;
using System.Reflection;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Telemetry;

/// <summary>Base class for workflow activities that emit a span linked to the original request trace.</summary>
/// <typeparam name="TInput">Activity input type.</typeparam>
/// <typeparam name="TOutput">Activity output type.</typeparam>
public abstract class WorkflowTraceLinkedActivity<TInput, TOutput> : WorkflowActivity<TInput, TOutput>
    where TInput : IWorkflowTraceContextCarrier
{
    /// <inheritdoc />
    public sealed override async Task<TOutput> RunAsync(WorkflowActivityContext context, TInput input)
    {
        using Activity? activity = StartLinkedActivity(input);
        try
        {
            TOutput output = await RunActivityAsync(context, input).ConfigureAwait(false);
            activity?.SetTag(MemoriesActivitySource.TagOutcome, "ok");
            return output;
        }
        catch (Exception ex)
        {
            activity?.SetTag(MemoriesActivitySource.TagOutcome, "error");
            activity?.SetTag(MemoriesActivitySource.TagErrorCode, ex.GetType().Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>Runs the activity implementation.</summary>
    protected abstract Task<TOutput> RunActivityAsync(WorkflowActivityContext context, TInput input);

    private Activity? StartLinkedActivity(TInput input)
    {
        List<ActivityLink>? links = TryCreateLink(input.TraceContext, out ActivityContext parentContext)
            ? [new ActivityLink(parentContext)]
            : null;
        Activity? activity = MemoriesActivitySource.Instance.StartActivity(
            MemoriesActivitySource.WorkflowActivity,
            ActivityKind.Internal,
            parentContext: default,
            tags: null,
            links: links);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(MemoriesActivitySource.TagOperation, GetType().Name);
        SetStringTag(activity, MemoriesActivitySource.TagTenantId, ReadStringProperty(input, "TenantId"));
        SetStringTag(activity, MemoriesActivitySource.TagCaseId, ReadStringProperty(input, "CaseId"));
        SetStringTag(activity, MemoriesActivitySource.TagMemoryUnitId, ReadStringProperty(input, "MemoryUnitId"));
        object? sourceType = ReadProperty(input, "SourceType");
        if (sourceType is not null)
        {
            activity.SetTag(MemoriesActivitySource.TagSourceType, sourceType.ToString());
        }

        return activity;
    }

    private static bool TryCreateLink(WorkflowTraceContext? traceContext, out ActivityContext parentContext)
    {
        parentContext = default;
        return traceContext is not null
            && ActivityContext.TryParse(traceContext.TraceParent, traceContext.TraceState, out parentContext);
    }

    private static string? ReadStringProperty(TInput input, string propertyName)
        => ReadProperty(input, propertyName) as string;

    private static object? ReadProperty(TInput input, string propertyName)
        => typeof(TInput)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(input);

    private static void SetStringTag(Activity activity, string tagName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            activity.SetTag(tagName, value);
        }
    }
}
