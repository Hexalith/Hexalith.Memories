// <copyright file="AccessTelemetryAspireRoutedIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

/// <summary>Hosted Dapr routing, clock, actor, state, and health evidence for Story 27.2.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class AccessTelemetryAspireRoutedIntegrationTests(AspireIngestionPipelineFixture fixture)
{
    private const string ConfigurationEpoch = "01J00000000000000000000000";
    private const string ComponentProfileHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ClockSidecarResourceName = "memories-access-telemetry-clock-dapr-cli";
    private const string LifecycleSidecarResourceName = "memories-access-telemetry-dapr-cli";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task AppHost_DaprRoutesClockHeartbeatActorStateInspectionAndHealth()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(StartupTimeout);
        CancellationToken cancellationToken = timeout.Token;
        await fixture.WaitForOpenBaoSidecarMatrixReadinessAsync(cancellationToken)
            .ConfigureAwait(true);

        using HttpClient lifecycleSidecar = fixture.CreateDaprSidecarClient(LifecycleSidecarResourceName);
        using HttpClient clockSidecar = fixture.CreateDaprSidecarClient(ClockSidecarResourceName);

        SignedClockAttestation lifecycleAttestation = await RequestAttestationAsync(
            lifecycleSidecar,
            "memories-access-telemetry",
            "01J00000000000000000000001",
            "01J00000000000000000000002",
            "01J00000000000000000000003",
            cancellationToken).ConfigureAwait(true);
        lifecycleAttestation.Signature.ShouldNotBeNullOrWhiteSpace();
        lifecycleAttestation.NotBeforeUnixMilliseconds.ShouldBeLessThanOrEqualTo(lifecycleAttestation.IssuedAtUnixMilliseconds);
        lifecycleAttestation.ExpiresAtUnixMilliseconds.ShouldBeGreaterThan(lifecycleAttestation.IssuedAtUnixMilliseconds);

        AccessTelemetryRuntimeValidationResponse validation = await WaitForValidationAsync(
            clockSidecar,
            cancellationToken).ConfigureAwait(true);
        validation.AllowsWrites.ShouldBeTrue(validation.Reason.ToString());

        const string writerProcessEpoch = "01J00000000000000000000012";
        const string writerServiceInstanceId = "01J00000000000000000000013";
        SignedClockAttestation writerAttestation = await RequestAttestationAsync(
            lifecycleSidecar,
            "memories",
            "01J00000000000000000000011",
            writerProcessEpoch,
            writerServiceInstanceId,
            cancellationToken).ConfigureAwait(true);
        var heartbeat = new WriterHeartbeatRequest
        {
            Heartbeat = new WriterHeartbeat
            {
                DeploymentId = "development",
                ServiceInstanceId = writerServiceInstanceId,
                ProcessEpoch = writerProcessEpoch,
                MarkerKeyGeneration = "development-marker",
                OldKeyQueueCount = 0,
                LeaseExpiresAtUnixMilliseconds = writerAttestation.IssuedAtUnixMilliseconds + 29_000,
            },
            ClockAttestation = writerAttestation,
        };
        using HttpResponseMessage heartbeatHttp = await clockSidecar.PostAsJsonAsync(
            "/v1.0/invoke/memories-access-telemetry/method/v1/access-telemetry/heartbeat",
            heartbeat,
            cancellationToken).ConfigureAwait(true);
        heartbeatHttp.EnsureSuccessStatusCode();
        WriterHeartbeatResponse heartbeatResponse = (await heartbeatHttp.Content
            .ReadFromJsonAsync<WriterHeartbeatResponse>(cancellationToken)
            .ConfigureAwait(true))!;
        heartbeatResponse.Accepted.ShouldBeTrue(heartbeatResponse.Reason.ToString());

        using HttpResponseMessage inspectionHttp = await clockSidecar.GetAsync(
            "/v1.0/invoke/memories-access-telemetry/method/v1/access-telemetry/inspect",
            cancellationToken).ConfigureAwait(true);
        inspectionHttp.EnsureSuccessStatusCode();
        AccessTelemetryInspectionResponse inspection = (await inspectionHttp.Content
            .ReadFromJsonAsync<AccessTelemetryInspectionResponse>(cancellationToken)
            .ConfigureAwait(true))!;
        inspection.Health.ShouldBe(AccessTelemetryHealthState.Healthy);
        inspection.Reason.ShouldBe(AccessTelemetryReason.None);
        inspection.ConfigurationEpoch.ShouldBe(ConfigurationEpoch);

        using JsonDocument actorState = await lifecycleSidecar.GetFromJsonAsync<JsonDocument>(
            "/v1.0/actors/AccessTelemetryLifecycleActor/global/state/lifecycle-control",
            cancellationToken).ConfigureAwait(true) ?? throw new InvalidOperationException("The routed actor state response was empty.");
        JsonElement root = actorState.RootElement;
        root.GetProperty("writers").EnumerateObject().ShouldNotBeEmpty();
        root.GetProperty("configuration").GetProperty("epoch").GetString().ShouldBe(ConfigurationEpoch);
        root.GetProperty("configuration").GetProperty("componentProfileHash").GetString().ShouldBe(ComponentProfileHash);
    }

    private static async Task<SignedClockAttestation> RequestAttestationAsync(
        HttpClient callerSidecar,
        string appId,
        string nonce,
        string processEpoch,
        string serviceInstanceId,
        CancellationToken cancellationToken)
    {
        var request = new ClockAttestationRequest
        {
            DeploymentId = "development",
            AppId = appId,
            ComponentProfileHash = ComponentProfileHash,
            Nonce = nonce,
            RequestingProcessEpoch = processEpoch,
            RequestingServiceInstanceId = serviceInstanceId,
        };
        using HttpResponseMessage response = await callerSidecar.PostAsJsonAsync(
            "/v1.0/invoke/memories-access-telemetry-clock/method/v1/time/attest",
            request,
            cancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SignedClockAttestation>(cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException("The routed clock response was empty.");
    }

    private static async Task<AccessTelemetryRuntimeValidationResponse> WaitForValidationAsync(
        HttpClient callerSidecar,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpResponseMessage response = await callerSidecar.PostAsJsonAsync(
                "/v1.0/invoke/memories-access-telemetry/method/v1/access-telemetry/validate",
                new AccessTelemetryRuntimeValidationRequest(ConfigurationEpoch, ComponentProfileHash),
                cancellationToken).ConfigureAwait(true);
            response.EnsureSuccessStatusCode();
            AccessTelemetryRuntimeValidationResponse validation = await response.Content
                .ReadFromJsonAsync<AccessTelemetryRuntimeValidationResponse>(cancellationToken)
                .ConfigureAwait(true)
                ?? throw new InvalidOperationException("The routed validation response was empty.");
            if (validation.AllowsWrites)
            {
                return validation;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(true);
        }
    }
}
