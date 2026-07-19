// <copyright file="OpenBaoGenerationGateTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>Concurrency contract tests for refreshable OpenBao generation ownership.</summary>
[Trait("Category", "Integration")]
public sealed class OpenBaoGenerationGateTests
{
    [Fact]
    public void TryBeginGeneration_DuplicateEndpointEventDoesNotAcquireInitializationOwnership()
    {
        var gate = new OpenBaoGenerationGate();

        gate.TryBeginGeneration(out OpenBaoGenerationLease owner).ShouldBeTrue();
        gate.TryBeginGeneration(out OpenBaoGenerationLease observer).ShouldBeFalse();

        observer.ShouldBeSameAs(owner);
        owner.GenerationNumber.ShouldBe(1);
    }

    [Fact]
    public async Task MarkStopped_InvalidatesOldLeaseAndRejectsItsLateArtifactInstall()
    {
        var gate = new OpenBaoGenerationGate();
        _ = gate.TryBeginGeneration(out OpenBaoGenerationLease staleLease);
        Task staleReadiness = staleLease.Readiness.Task;

        gate.MarkStopped();
        _ = await Should.ThrowAsync<OperationCanceledException>(() => staleReadiness);
        gate.TryBeginGeneration(out OpenBaoGenerationLease currentLease).ShouldBeTrue();

        bool staleInstallRan = false;
        gate.TryInstallCurrent(staleLease, () => staleInstallRan = true).ShouldBeFalse();
        staleInstallRan.ShouldBeFalse();

        bool currentInstallRan = false;
        gate.TryInstallCurrent(currentLease, () => currentInstallRan = true).ShouldBeTrue();
        currentInstallRan.ShouldBeTrue();
        currentLease.GenerationNumber.ShouldBe(2);
        currentLease.Readiness.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public void MarkStopped_DuplicateNotificationPreservesPendingNextGenerationReadiness()
    {
        var gate = new OpenBaoGenerationGate();
        _ = gate.TryBeginGeneration(out _);

        gate.MarkStopped();
        Task pendingReadiness = gate.SnapshotReadiness();
        gate.MarkStopped();

        gate.SnapshotReadiness().ShouldBeSameAs(pendingReadiness);
        pendingReadiness.IsCompleted.ShouldBeFalse();
    }
}
