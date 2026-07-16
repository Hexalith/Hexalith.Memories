// <copyright file="RetryPolicyBuilder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

/// <summary>Process-global retry-policy table for ingestion-workflow activities (Story 6.3 FR9).</summary>
/// <remarks>
/// <para>Inside a workflow body, callers MUST snapshot the table once per invocation via
/// <see cref="SnapshotAll"/> and read locally for replay determinism — the per-process table is treated as
/// effectively immutable within the lifetime of an instance by convention. <see cref="For"/> is exposed for
/// non-workflow callers (tests, endpoints).</para>
/// </remarks>
public static class RetryPolicyBuilder
{
    /// <summary>The dictionary key reserved for the default policy.</summary>
    public const string DefaultKey = "__default";

    private static IReadOnlyDictionary<string, WorkflowTaskOptions> _snapshot = BuildInitialSnapshot();

    /// <summary>Initializes the global retry-policy table from the supplied <see cref="IngestionSettings"/>.</summary>
    /// <param name="settings">The bound ingestion settings.</param>
    /// <exception cref="InvalidOperationException">Thrown when an entry has <c>MaxAttempts &lt;= 0</c>.</exception>
    public static void Initialize(IngestionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Dictionary<string, WorkflowTaskOptions> map = new(StringComparer.Ordinal)
        {
            [DefaultKey] = ToOptions(new ActivityRetryPolicy()),
        };
        foreach ((string name, ActivityRetryPolicy policy) in settings.RetryPolicies)
        {
            if (policy.MaxAttempts <= 0)
            {
                throw new InvalidOperationException(
                    $"RETRY_CONFIG_INVALID: Ingestion:RetryPolicies:{name}.MaxAttempts must be > 0 (was {policy.MaxAttempts}).");
            }

            map[name] = ToOptions(policy);
        }

        _snapshot = map;
    }

    /// <summary>Returns an immutable snapshot of the current retry-policy table.</summary>
    /// <returns>An immutable dictionary mapping activity name → <see cref="WorkflowTaskOptions"/>.</returns>
    /// <remarks>Workflow bodies MUST capture this once at the top of <c>RunAsync</c> and read from the local
    /// variable on every subsequent activity call. The snapshot lifetime is independent of subsequent
    /// <see cref="Initialize"/> calls.</remarks>
    public static IReadOnlyDictionary<string, WorkflowTaskOptions> SnapshotAll() => _snapshot;

    /// <summary>Resets the retry-policy table to the startup default snapshot.</summary>
    internal static void ResetToDefaults() => _snapshot = BuildInitialSnapshot();

    /// <summary>Convenience for non-workflow callers (tests, endpoints).</summary>
    /// <param name="activityName">The activity class name (e.g., <c>nameof(GenerateEmbeddingActivity)</c>).</param>
    /// <returns>The matching <see cref="WorkflowTaskOptions"/>, or the default if no override exists.</returns>
    /// <remarks>Do NOT use inside a workflow body — call <see cref="SnapshotAll"/> once at the top of
    /// <c>RunAsync</c> and read from the local variable instead.</remarks>
    public static WorkflowTaskOptions For(string activityName) =>
        _snapshot.TryGetValue(activityName, out WorkflowTaskOptions? opts)
            ? opts
            : _snapshot[DefaultKey];

    private static WorkflowTaskOptions ToOptions(ActivityRetryPolicy p) =>
        new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: p.MaxAttempts,
            firstRetryInterval: TimeSpan.FromSeconds(p.FirstRetryIntervalSeconds),
            backoffCoefficient: p.BackoffCoefficient,
            maxRetryInterval: TimeSpan.FromSeconds(p.MaxRetryIntervalSeconds)));

    private static IReadOnlyDictionary<string, WorkflowTaskOptions> BuildInitialSnapshot() =>
        new Dictionary<string, WorkflowTaskOptions>(StringComparer.Ordinal)
        {
            [DefaultKey] = ToOptions(new ActivityRetryPolicy()),
        };
}
