// <copyright file="RetryPolicyBuilderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Server.Ingestion;

using Shouldly;

[
Collection(RetryPolicyBuilderStateCollection.Name)
]
public class RetryPolicyBuilderTests
{
    public RetryPolicyBuilderTests()
        => RetryPolicyBuilder.ResetToDefaults();

    [Fact]
    public void Initialize_WithEmptySettings_ForReturnsDefault()
    {
        RetryPolicyBuilder.Initialize(new IngestionSettings());

        WorkflowTaskOptions options = RetryPolicyBuilder.For("AnyActivity");
        WorkflowRetryPolicy policy = options.RetryPolicy!;

        policy.MaxNumberOfAttempts.ShouldBe(5);
        policy.FirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(2));
        policy.BackoffCoefficient.ShouldBe(1.5);
        policy.MaxRetryInterval.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Initialize_WithOverride_ForReturnsOverride()
    {
        IngestionSettings settings = new()
        {
            RetryPolicies = new(StringComparer.Ordinal)
            {
                ["GenerateEmbeddingActivity"] = new ActivityRetryPolicy
                {
                    MaxAttempts = 3,
                    FirstRetryIntervalSeconds = 4.0,
                    BackoffCoefficient = 2.0,
                    MaxRetryIntervalSeconds = 60.0,
                },
            },
        };
        RetryPolicyBuilder.Initialize(settings);

        WorkflowRetryPolicy embedding = RetryPolicyBuilder.For("GenerateEmbeddingActivity").RetryPolicy!;
        embedding.MaxNumberOfAttempts.ShouldBe(3);
        embedding.FirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(4));
        embedding.BackoffCoefficient.ShouldBe(2.0);
        embedding.MaxRetryInterval.ShouldBe(TimeSpan.FromSeconds(60));

        WorkflowRetryPolicy other = RetryPolicyBuilder.For("ExtractContentActivity").RetryPolicy!;
        other.MaxNumberOfAttempts.ShouldBe(5);
        other.FirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void For_BeforeInitialize_ReturnsDefault()
    {
        WorkflowTaskOptions options = RetryPolicyBuilder.For("AnyActivity");
        WorkflowRetryPolicy policy = options.RetryPolicy!;

        policy.MaxNumberOfAttempts.ShouldBe(5);
        policy.FirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(2));
        policy.BackoffCoefficient.ShouldBe(1.5);
        policy.MaxRetryInterval.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Initialize_CalledTwice_UsesLatest()
    {
        IngestionSettings first = new()
        {
            RetryPolicies = new(StringComparer.Ordinal)
            {
                ["A"] = new ActivityRetryPolicy { MaxAttempts = 1 },
            },
        };
        IngestionSettings second = new()
        {
            RetryPolicies = new(StringComparer.Ordinal)
            {
                ["A"] = new ActivityRetryPolicy { MaxAttempts = 9 },
            },
        };

        RetryPolicyBuilder.Initialize(first);
        RetryPolicyBuilder.Initialize(second);

        RetryPolicyBuilder.For("A").RetryPolicy!.MaxNumberOfAttempts.ShouldBe(9);
    }

    [Fact]
    public void Initialize_WithZeroMaxAttempts_ThrowsRetryConfigInvalid()
    {
        IngestionSettings settings = new()
        {
            RetryPolicies = new(StringComparer.Ordinal)
            {
                ["BadActivity"] = new ActivityRetryPolicy { MaxAttempts = 0 },
            },
        };

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => RetryPolicyBuilder.Initialize(settings));
        ex.Message.ShouldContain("RETRY_CONFIG_INVALID");
        ex.Message.ShouldContain("BadActivity");
    }

    [Fact]
    public void SnapshotAll_ReturnsImmutableSnapshot_IndependentOfSubsequentInitialize()
    {
        RetryPolicyBuilder.Initialize(new IngestionSettings
        {
            RetryPolicies = new(StringComparer.Ordinal)
            {
                ["X"] = new ActivityRetryPolicy { MaxAttempts = 7 },
            },
        });
        IReadOnlyDictionary<string, WorkflowTaskOptions> snapshot = RetryPolicyBuilder.SnapshotAll();
        int beforeAttempts = snapshot["X"].RetryPolicy!.MaxNumberOfAttempts;

        // Reinitialize with different settings
        RetryPolicyBuilder.Initialize(new IngestionSettings
        {
            RetryPolicies = new(StringComparer.Ordinal)
            {
                ["X"] = new ActivityRetryPolicy { MaxAttempts = 99 },
            },
        });

        // Original snapshot must still report the captured value
        beforeAttempts.ShouldBe(7);
        snapshot["X"].RetryPolicy!.MaxNumberOfAttempts.ShouldBe(7);
    }
}
