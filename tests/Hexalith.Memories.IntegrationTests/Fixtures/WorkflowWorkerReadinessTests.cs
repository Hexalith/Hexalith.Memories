// <copyright file="WorkflowWorkerReadinessTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using System.Text.Json;

using Shouldly;

/// <summary>Checks the read-only workflow readiness boundary without starting an AppHost.</summary>
[Trait("Category", "Integration")]
public sealed class WorkflowWorkerReadinessTests
{
    private const string AppId = "memories-it-owned-worker";

    [Fact]
    public async Task MissingWorkerThenPartialRegistrationThenReady_ConvergesWithoutMutations()
    {
        string[] observations =
        [
            $$"""{"id":"{{AppId}}"}""",
            Metadata(connectedWorkers: 0),
            Metadata(connectedWorkers: 1, registered: false),
            Metadata(connectedWorkers: 1, hostReady: false),
            Metadata(connectedWorkers: 1),
        ];
        int probes = 0;

        await AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
            _ => Task.FromResult(JsonDocument.Parse(observations[probes++])),
            AppId,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            CancellationToken.None);

        probes.ShouldBe(observations.Length);
    }

    [Fact]
    public async Task ReadyWorkerFromAnotherApp_FailsClosedWithoutRetry()
    {
        int probes = 0;
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
                _ =>
                {
                    probes++;
                    return Task.FromResult(JsonDocument.Parse(Metadata(1).Replace(AppId, "another-app", StringComparison.Ordinal)));
                },
                AppId,
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                CancellationToken.None));

        probes.ShouldBe(1);
        exception.Message.ShouldContain("fixture-owned Dapr app");
    }

    [Fact]
    public async Task MissingWorkerUntilDeadline_FailsWithSafeDiagnostics()
    {
        TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
                _ => Task.FromResult(JsonDocument.Parse($$"""{"id":"{{AppId}}","secret":"metadata-secret-sentinel"}""")),
                AppId,
                TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(5),
                CancellationToken.None));

        exception.Message.ShouldContain("workflow worker did not become ready");
        exception.Message.ShouldNotContain("metadata-secret-sentinel");
    }

    [Fact]
    public async Task HangingMetadataProbe_IsBoundedByOverallDeadline()
    {
        var neverCompletes = new TaskCompletionSource<JsonDocument>(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
                _ => neverCompletes.Task,
                AppId,
                TimeSpan.FromMilliseconds(30),
                TimeSpan.Zero,
                CancellationToken.None));
    }

    [Fact]
    public async Task CallerCancelledBeforeProbe_PropagatesWithoutReadingMetadata()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        int probes = 0;

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
                _ =>
                {
                    probes++;
                    return Task.FromResult(JsonDocument.Parse(Metadata(1)));
                },
                AppId,
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                cts.Token));

        probes.ShouldBe(0);
    }

    [Fact]
    public async Task CallerCancelledDuringMetadataProbe_PreservesCancellation()
    {
        using var cts = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var neverCompletes = new TaskCompletionSource<JsonDocument>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task waiting = AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
            _ =>
            {
                started.SetResult();
                return neverCompletes.Task;
            },
            AppId,
            TimeSpan.FromSeconds(5),
            TimeSpan.Zero,
            cts.Token);
        await started.Task;
        cts.Cancel();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => waiting);
    }

    [Fact]
    public async Task MetadataAuthorizationFailure_PropagatesWithoutRetry()
    {
        int probes = 0;
        _ = await Should.ThrowAsync<HttpRequestException>(() =>
            AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
                _ =>
                {
                    probes++;
                    return Task.FromException<JsonDocument>(new HttpRequestException(
                        "Metadata is forbidden.", null, HttpStatusCode.Forbidden));
                },
                AppId,
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                CancellationToken.None));

        probes.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TransientMetadataTransportFailureThenWorkerReady_Converges(HttpStatusCode? statusCode)
    {
        int probes = 0;
        await AspireIngestionPipelineFixture.WaitForWorkflowWorkerReadyAsync(
            _ => ++probes == 1
                ? Task.FromException<JsonDocument>(new HttpRequestException("The sidecar is reconnecting.", null, statusCode))
                : Task.FromResult(JsonDocument.Parse(Metadata(1))),
            AppId,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            CancellationToken.None);

        probes.ShouldBe(2);
    }

    private static string Metadata(int connectedWorkers, bool registered = true, bool hostReady = true)
    {
        string actors = registered
            ? $$"""[{"type":"dapr.internal.default.{{AppId}}.workflow","count":0},{"type":"dapr.internal.default.{{AppId}}.activity","count":0}]"""
            : $$"""[{"type":"dapr.internal.default.{{AppId}}.workflow","count":0}]""";
        return $$"""{"id":"{{AppId}}","workflows":{"connectedWorkers":{{connectedWorkers}}},"actorRuntime":{"hostReady":{{hostReady.ToString().ToLowerInvariant()}}},"actors":{{actors}}}""";
    }
}
