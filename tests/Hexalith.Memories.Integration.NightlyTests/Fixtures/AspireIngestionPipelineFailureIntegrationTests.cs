// <copyright file="AspireIngestionPipelineFailureIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

/// <summary>Exercises failure diagnostics against the full fixture-owned Aspire topology.</summary>
[Trait("Category", "IntegrationSlow")]
public sealed class AspireIngestionPipelineFailureIntegrationTests
{
    private readonly (string? RedisVolume, string? FalkorVolume, string? DaprConfig) _originalEnvironment = ReadFixtureEnvironment();
    private (string RedisVolume, string FalkorVolume, string DaprConfig) _ownedEnvironment;
    private CancellationToken _applicationStopped;

    [Fact]
    public async Task FailedOllamaStartup_AttachesLiveBackendDiagnosticsBeforeFixtureCleanup()
    {
        var injectedFailure = new InvalidOperationException("Injected failure after the initial Memories health gate.");
        string expectedRedis = string.Empty;
        string expectedOpenBao = string.Empty;
        await using var fixture = new AspireIngestionPipelineFixture(
            EmbeddingProviderTestMode.OllamaOidcFake,
            embeddingProviderSecret: null,
            beforeClientSetup: async (app, cancellationToken) =>
            {
                await CaptureOwnedTopologyAsync(app, cancellationToken);
                expectedRedis = ExpectedDiagnostic(app, "memories-vectors", "redis", 6379, proxied: false);
                expectedOpenBao = ExpectedDiagnostic(app, "openbao", "http", 8200, proxied: true);
                throw injectedFailure;
            });

        var observed = await Should.ThrowAsync<InvalidOperationException>(() =>
            fixture.InitializeAsync(TestContext.Current.CancellationToken).AsTask());

        observed.ShouldBeSameAs(injectedFailure);
        observed.Data[AspireIngestionPipelineFixture.OllamaStartupRedisPortDataKey].ShouldBe(expectedRedis);
        observed.Data[AspireIngestionPipelineFixture.OllamaStartupOpenBaoPortDataKey].ShouldBe(expectedOpenBao);
        observed.Data.Contains(AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey).ShouldBeFalse();
        // A failed InitializeAsync must clean up before the fixture owner can dispose it.
        await AssertOwnedTopologyWasRemovedAsync();
    }

    [Fact]
    public async Task FailedRestart_AttachesReplacementBackendDiagnosticsBeforeFixtureDisposal()
    {
        var injectedFailure = new InvalidOperationException("Injected failure after the replacement Memories health gate.");
        CancellationToken initialApplicationStopped = default;
        string expectedRedis = string.Empty;
        string expectedFalkor = string.Empty;
        bool injectFailure = false;
        await using (var fixture = new AspireIngestionPipelineFixture(
            EmbeddingProviderTestMode.GoogleFake,
            embeddingProviderSecret: null,
            beforeClientSetup: async (app, cancellationToken) =>
            {
                if (!injectFailure)
                {
                    initialApplicationStopped = GetApplicationStopped(app);
                    return;
                }

                initialApplicationStopped.IsCancellationRequested.ShouldBeTrue();
                await CaptureOwnedTopologyAsync(app, cancellationToken);
                expectedRedis = ExpectedDiagnostic(app, "memories-vectors", "redis", 6379, proxied: false);
                expectedFalkor = ExpectedDiagnostic(app, "memories-graphs", "falkordb", 6379, proxied: false);
                throw injectedFailure;
            }))
        {
            await fixture.InitializeAsync(TestContext.Current.CancellationToken);
            injectFailure = true;

            var observed = await Should.ThrowAsync<InvalidOperationException>(() => fixture.RestartTopologyAsync());

            observed.ShouldBeSameAs(injectedFailure);
            observed.Data[AspireIngestionPipelineFixture.RestartBackendPortsDataKey]
                .ShouldBe(string.Join(Environment.NewLine, expectedRedis, expectedFalkor));
        }

        await AssertOwnedTopologyWasRemovedAsync();
    }

    private static (string? RedisVolume, string? FalkorVolume, string? DaprConfig) ReadFixtureEnvironment()
        => (
            Environment.GetEnvironmentVariable("MEMORIES_REDIS_VOLUME_NAME"),
            Environment.GetEnvironmentVariable("MEMORIES_FALKOR_VOLUME_NAME"),
            Environment.GetEnvironmentVariable("MEMORIES_DAPR_CONFIG_PATH"));

    private static CancellationToken GetApplicationStopped(DistributedApplication app)
        => app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopped;

    private static string ExpectedDiagnostic(
        DistributedApplication app,
        string resource,
        string endpoint,
        int targetPort,
        bool proxied)
    {
        Uri allocated = app.GetEndpoint(resource, endpoint);
        allocated.IsLoopback.ShouldBeTrue();
        allocated.Port.ShouldBeGreaterThan(0);
        string host = string.Equals(allocated.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            ? IPAddress.Loopback.ToString()
            : allocated.DnsSafeHost;
        return $"{resource}/{endpoint}: state=Running, proxied={proxied}, containerTargetPort={targetPort}, " +
            $"hostPort={host}:{allocated.Port}, listener=reachable";
    }

    private static async Task<bool> IsDockerVolumePresentAsync(string volumeName, CancellationToken cancellationToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(30));
        string volumes = await AspireIngestionPipelineFixture.RunProcessCommandAsync(
            "docker", $"volume ls --quiet --filter name={volumeName}", budget.Token);
        return volumes.Split('\n', StringSplitOptions.TrimEntries).Contains(volumeName, StringComparer.Ordinal);
    }

    private async Task CaptureOwnedTopologyAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        (string? redisVolume, string? falkorVolume, string? daprConfig) = ReadFixtureEnvironment();
        _ownedEnvironment = (redisVolume.ShouldNotBeNull(), falkorVolume.ShouldNotBeNull(), daprConfig.ShouldNotBeNull());
        _applicationStopped = GetApplicationStopped(app);
        _applicationStopped.IsCancellationRequested.ShouldBeFalse();
        File.Exists(_ownedEnvironment.DaprConfig).ShouldBeTrue();
        (await IsDockerVolumePresentAsync(_ownedEnvironment.RedisVolume, cancellationToken)).ShouldBeTrue();
        (await IsDockerVolumePresentAsync(_ownedEnvironment.FalkorVolume, cancellationToken)).ShouldBeTrue();
    }

    private async Task AssertOwnedTopologyWasRemovedAsync()
    {
        _applicationStopped.IsCancellationRequested.ShouldBeTrue();
        ReadFixtureEnvironment().ShouldBe(_originalEnvironment);
        Directory.Exists(Path.GetDirectoryName(_ownedEnvironment.DaprConfig)).ShouldBeFalse();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (await IsDockerVolumePresentAsync(_ownedEnvironment.RedisVolume, cancellationToken)).ShouldBeFalse();
        (await IsDockerVolumePresentAsync(_ownedEnvironment.FalkorVolume, cancellationToken)).ShouldBeFalse();
    }
}
