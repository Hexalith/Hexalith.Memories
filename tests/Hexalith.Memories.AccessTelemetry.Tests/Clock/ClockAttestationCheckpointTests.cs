// <copyright file="ClockAttestationCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Clock;

using System.Security.Cryptography;

using Hexalith.Memories.AccessTelemetry.Clock;
using Hexalith.Memories.AccessTelemetry.Contracts;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

/// <summary>Story 27.2 C4 checkpoint for independent trusted-clock evidence.</summary>
public sealed class ClockAttestationCheckpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AttestAsync_ThreeAuthenticatedIndependentSources_ProducesSignedMajorityInterval()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var service = CreateService(key, CreateSource("a", -80, 50), CreateSource("b", -20, 70), CreateSource("c", 0, 100));
        ClockAttestationRequest request = CreateRequest("nonce-1");

        SignedClockAttestation attestation = await service.AttestAsync(request, CancellationToken.None);

        (attestation.NotAfterUnixMilliseconds - attestation.NotBeforeUnixMilliseconds).ShouldBeLessThanOrEqualTo(250);
        attestation.ExpiresAtUnixMilliseconds.ShouldBe(attestation.IssuedAtUnixMilliseconds + 30_000);
        attestation.Signature.ShouldNotBeNullOrWhiteSpace();
        var replay = new BoundedNonceReplayCache(64);
        ClockAttestationValidationResult result = ClockAttestationVerifier.Verify(
            attestation,
            CreateContext(request),
            key.ExportSubjectPublicKeyInfo(),
            Now,
            replay);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task AttestAsync_FewerThanThreeIndependentAuthenticatedSources_FailsClosed()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        IAuthenticatedUtcSource unauthenticated = CreateSource("c", 0, 50, authenticated: false);
        var service = CreateService(key, CreateSource("a", -20, 20), CreateSource("b", -10, 30), unauthenticated);

        ClockAttestationException exception = await Should.ThrowAsync<ClockAttestationException>(
            () => service.AttestAsync(CreateRequest("nonce-2"), CancellationToken.None));

        exception.Reason.ShouldBe(AccessTelemetryReason.ClockUntrusted);
    }

    [Fact]
    public async Task AttestAsync_MajorityIntervalWiderThan250Milliseconds_FailsClosed()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var service = CreateService(key, CreateSource("a", -300, 300), CreateSource("b", -250, 350), CreateSource("c", -200, 400));

        await Should.ThrowAsync<ClockAttestationException>(
            () => service.AttestAsync(CreateRequest("nonce-3"), CancellationToken.None));
    }

    [Theory]
    [InlineData("deployment-x", "memories-server", "profile-a", "nonce-4")]
    [InlineData("deployment-a", "wrong-app", "profile-a", "nonce-4")]
    [InlineData("deployment-a", "memories-server", "wrong-profile", "nonce-4")]
    [InlineData("deployment-a", "memories-server", "profile-a", "wrong-nonce")]
    public async Task Verify_ContextProfileOrNonceMismatch_FailsClosed(
        string deployment,
        string appId,
        string profile,
        string nonce)
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ClockAttestationRequest request = CreateRequest("nonce-4");
        SignedClockAttestation attestation = await CreateService(
            key,
            CreateSource("a", -20, 20),
            CreateSource("b", -10, 30),
            CreateSource("c", 0, 40)).AttestAsync(request, CancellationToken.None);
        var context = new ClockAttestationValidationContext(deployment, appId, profile, nonce, "process-a", "service-a");

        ClockAttestationValidationResult result = ClockAttestationVerifier.Verify(
            attestation,
            context,
            key.ExportSubjectPublicKeyInfo(),
            Now,
            new BoundedNonceReplayCache(64));

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(AccessTelemetryReason.ClockUntrusted);
    }

    [Fact]
    public async Task Verify_ReplayStaleDeltaOrTamperedSignature_FailsClosed()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ClockAttestationRequest request = CreateRequest("nonce-5");
        SignedClockAttestation attestation = await CreateService(
            key,
            CreateSource("a", -20, 20),
            CreateSource("b", -10, 30),
            CreateSource("c", 0, 40)).AttestAsync(request, CancellationToken.None);
        var replay = new BoundedNonceReplayCache(64);

        ClockAttestationVerifier.Verify(attestation, CreateContext(request), key.ExportSubjectPublicKeyInfo(), Now, replay).IsValid.ShouldBeTrue();
        ClockAttestationVerifier.Verify(attestation, CreateContext(request), key.ExportSubjectPublicKeyInfo(), Now, replay).IsValid.ShouldBeFalse();
        ClockAttestationVerifier.Verify(attestation, CreateContext(request), key.ExportSubjectPublicKeyInfo(), Now.AddSeconds(31), new BoundedNonceReplayCache(64)).IsValid.ShouldBeFalse();
        ClockAttestationVerifier.Verify(attestation, CreateContext(request), key.ExportSubjectPublicKeyInfo(), Now.AddSeconds(2), new BoundedNonceReplayCache(64)).IsValid.ShouldBeFalse();
        ClockAttestationVerifier.Verify(
            attestation with { Signature = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) },
            CreateContext(request),
            key.ExportSubjectPublicKeyInfo(),
            Now,
            new BoundedNonceReplayCache(64)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task NewServiceProcess_ChangesServiceInstanceAndProcessEpoch()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ClockAttestationService first = CreateService(key, CreateSource("a", 0, 10), CreateSource("b", 0, 20), CreateSource("c", 0, 30));
        ClockAttestationService restarted = CreateService(key, CreateSource("a", 0, 10), CreateSource("b", 0, 20), CreateSource("c", 0, 30));

        SignedClockAttestation before = await first.AttestAsync(CreateRequest("nonce-6"), CancellationToken.None);
        SignedClockAttestation after = await restarted.AttestAsync(CreateRequest("nonce-7"), CancellationToken.None);

        before.ServiceInstanceId.ShouldNotBe(after.ServiceInstanceId);
        before.ProcessEpoch.ShouldNotBe(after.ProcessEpoch);
    }

    private static ClockAttestationValidationContext CreateContext(ClockAttestationRequest request)
        => new(request.DeploymentId, request.AppId, request.ComponentProfileHash, request.Nonce, "process-a", "service-a");

    private static ClockAttestationRequest CreateRequest(string nonce)
        => new()
        {
            DeploymentId = "deployment-a",
            AppId = "memories-server",
            ComponentProfileHash = "profile-a",
            Nonce = nonce,
            RequestingProcessEpoch = "process-a",
            RequestingServiceInstanceId = "service-a",
        };

    private static ClockAttestationService CreateService(ECDsa key, params IAuthenticatedUtcSource[] sources)
        => new(sources, new EcdsaClockAttestationSigner(key, "clock-key-1"), new FakeTimeProvider(Now), new MonotonicRecordIdGenerator());

    private static IAuthenticatedUtcSource CreateSource(
        string id,
        int lowerOffsetMilliseconds,
        int upperOffsetMilliseconds,
        bool authenticated = true)
    {
        IAuthenticatedUtcSource source = Substitute.For<IAuthenticatedUtcSource>();
        source.SourceId.Returns(id);
        source.GetUtcSampleAsync(Arg.Any<CancellationToken>()).Returns(new AuthenticatedUtcSample(
            id,
            Now.AddMilliseconds(lowerOffsetMilliseconds),
            Now.AddMilliseconds(upperOffsetMilliseconds),
            authenticated));
        return source;
    }
}
