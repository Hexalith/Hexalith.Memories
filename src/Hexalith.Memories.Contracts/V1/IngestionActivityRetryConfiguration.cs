// <copyright file="IngestionActivityRetryConfiguration.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Durable retry policy table captured for ingestion workflow activities.</summary>
public sealed record IngestionActivityRetryConfiguration
{
    /// <summary>Gets the default retry policy used when an activity has no override.</summary>
    public WorkflowActivityRetryPolicy Default { get; init; } = new();

    /// <summary>Gets activity-specific retry policy overrides keyed by activity class name.</summary>
    public Dictionary<string, WorkflowActivityRetryPolicy> ActivityOverrides
    {
        get => field ??= new Dictionary<string, WorkflowActivityRetryPolicy>(StringComparer.Ordinal);
        init => field = value switch
        {
            null => new Dictionary<string, WorkflowActivityRetryPolicy>(StringComparer.Ordinal),
            Dictionary<string, WorkflowActivityRetryPolicy> existing when ReferenceEquals(existing.Comparer, StringComparer.Ordinal) => existing,
            _ => new Dictionary<string, WorkflowActivityRetryPolicy>(value, StringComparer.Ordinal),
        };
    }
}
