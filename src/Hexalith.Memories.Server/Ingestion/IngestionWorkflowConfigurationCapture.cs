// <copyright file="IngestionWorkflowConfigurationCapture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Options;

/// <summary>Captures mutable host ingestion options into durable workflow input contracts.</summary>
internal sealed class IngestionWorkflowConfigurationCapture
{
    private readonly IOptions<IngestionSettings> _ingestionSettings;
    private readonly IOptions<NaturalLanguageDescriptionOptions> _naturalLanguageOptions;

    public IngestionWorkflowConfigurationCapture(
        IOptions<IngestionSettings> ingestionSettings,
        IOptions<NaturalLanguageDescriptionOptions> naturalLanguageOptions)
    {
        ArgumentNullException.ThrowIfNull(ingestionSettings);
        ArgumentNullException.ThrowIfNull(naturalLanguageOptions);
        _ingestionSettings = ingestionSettings;
        _naturalLanguageOptions = naturalLanguageOptions;
    }

    /// <summary>Captures the current host configuration values into a durable workflow configuration.</summary>
    public IngestionWorkflowConfiguration Capture()
        => Create(_ingestionSettings.Value, _naturalLanguageOptions.Value);

    /// <summary>Returns <paramref name="input"/> with freshly captured workflow configuration.</summary>
    public IngestionInput Apply(IngestionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input with { WorkflowConfiguration = Capture() };
    }

    /// <summary>Issues a new identifier in the configured service generation; unconfigured hosts retain ordinary ingestion without completeness authority.</summary>
    internal string NewUnitId() => UnitOwnershipAuthority.Qualify(Guid.NewGuid().ToString(), _ingestionSettings.Value.OwnershipGeneration);

    /// <summary>Places a service-generated ULID in the configured generation namespace.</summary>
    internal string QualifyUnitId(string identifier) => UnitOwnershipAuthority.Qualify(identifier, _ingestionSettings.Value.OwnershipGeneration);

    internal static IngestionWorkflowConfiguration Create(
        IngestionSettings ingestionSettings,
        NaturalLanguageDescriptionOptions naturalLanguageOptions)
    {
        ArgumentNullException.ThrowIfNull(ingestionSettings);
        ArgumentNullException.ThrowIfNull(naturalLanguageOptions);

        Dictionary<string, WorkflowActivityRetryPolicy> overrides = new(StringComparer.Ordinal);
        foreach ((string activityName, ActivityRetryPolicy retryPolicy) in ingestionSettings.RetryPolicies)
        {
            overrides[activityName] = ToWorkflowPolicy(retryPolicy, $"Ingestion:RetryPolicies:{activityName}");
        }

        UnitOwnershipAuthority.ValidateConfiguration(ingestionSettings);
        return new IngestionWorkflowConfiguration
        {
            Retry = new IngestionActivityRetryConfiguration
            {
                Default = new WorkflowActivityRetryPolicy(),
                ActivityOverrides = overrides,
            },
            NaturalLanguage = new NaturalLanguageWorkflowOptions
            {
                PersistInMetadata = naturalLanguageOptions.PersistInMetadata,
            },
        };
    }

    private static WorkflowActivityRetryPolicy ToWorkflowPolicy(ActivityRetryPolicy policy, string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.MaxAttempts <= 0)
        {
            throw new InvalidOperationException(
                $"RETRY_CONFIG_INVALID: {configurationPath}.MaxAttempts must be > 0 (was {policy.MaxAttempts}).");
        }

        return new WorkflowActivityRetryPolicy
        {
            MaxAttempts = policy.MaxAttempts,
            FirstRetryIntervalSeconds = policy.FirstRetryIntervalSeconds,
            BackoffCoefficient = policy.BackoffCoefficient,
            MaxRetryIntervalSeconds = policy.MaxRetryIntervalSeconds,
        };
    }
}
