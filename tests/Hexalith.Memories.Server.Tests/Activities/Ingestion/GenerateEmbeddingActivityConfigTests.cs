// <copyright file="GenerateEmbeddingActivityConfigTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Shouldly;

/// <summary>
/// ATDD acceptance tests for GenerateEmbeddingActivity tenant config integration (Story 1.7, AC #2).
/// TDD Red Phase: These tests validate that the activity reads config from TenantConfigurationActor
/// and passes it to EmbeddingClient. Remove Skip attributes once Story 1.7 is implemented.
/// </summary>
public class GenerateEmbeddingActivityConfigTests
{
    private const string TenantId = "test-tenant";
    private const string TestText = "Hello world";

    [Fact(Skip = "TDD Red Phase — Story 1.7: Activity not yet refactored for tenant config")]
    public async Task RunAsync_ShouldReadConfigFromTenantConfigurationActor()
    {
        // Arrange — AC #2: activity reads tenant's provider configuration
        // Mock ITenantConfigurationActor to return custom config
        // Mock EmbeddingClient, IEmbeddingRateLimiterActor

        // Act
        // var result = await activity.RunAsync(context, input);

        // Assert — verify TenantConfigurationActor.GetEmbeddingConfigAsync() was called
        // tenantConfigActor.Received(1).GetEmbeddingConfigAsync();
        throw new NotImplementedException("TDD Red Phase — implement config reading in GenerateEmbeddingActivity");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: Activity not yet refactored for tenant config")]
    public async Task RunAsync_ShouldPassConfigToEmbeddingClient()
    {
        // Arrange — AC #2: config values passed to EmbeddingClient
        // Setup TenantConfigurationActor to return: model=gemini-embedding-001, dims=768
        // Mock EmbeddingClient.GenerateAsync to accept TenantEmbeddingConfig param

        // Act
        // var result = await activity.RunAsync(context, input);

        // Assert — verify EmbeddingClient received the tenant config
        // await embeddingClient.Received(1).GenerateAsync(
        //     TestText, TenantId,
        //     Arg.Is<TenantEmbeddingConfig>(c => c.Model == "gemini-embedding-001"),
        //     Arg.Any<CancellationToken>());
        throw new NotImplementedException("TDD Red Phase — implement config passthrough to EmbeddingClient");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: Activity not yet refactored for tenant config")]
    public async Task RunAsync_ShouldSetRateLimiterCeilingFromConfig()
    {
        // Arrange — AC #2: rate limiter ceiling from tenant config
        // Setup TenantConfigurationActor to return rateLimitPerMinute=500

        // Act
        // var result = await activity.RunAsync(context, input);

        // Assert — SetCeilingAsync called unconditionally before TryConsumeAsync
        // Received.InOrder(() =>
        // {
        //     rateLimiter.SetCeilingAsync(500);
        //     rateLimiter.TryConsumeAsync();
        // });
        throw new NotImplementedException("TDD Red Phase — implement rate limiter ceiling from config");
    }

    [Fact(Skip = "TDD Red Phase — Story 1.7: Activity not yet refactored for tenant config")]
    public async Task RunAsync_ShouldReturnDynamicProviderAndDimensions()
    {
        // Arrange — AC #2: provider/dimensions from config, not hardcoded
        // Setup config with model="gemini-embedding-001", dims=768

        // Act
        // var result = await activity.RunAsync(context, input);

        // Assert — result uses config values, not "google:text-embedding-004"/768 constants
        // result.Provider.ShouldBe("google:gemini-embedding-001");
        // result.Dimensions.ShouldBe(768);
        throw new NotImplementedException("TDD Red Phase — implement dynamic provider/dimensions in result");
    }
}
