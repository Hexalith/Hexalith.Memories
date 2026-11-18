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
    public void BeginGeneration_FirstStartUsesReadinessAlreadyObservedByConsumers()
    {
        var gate = new OpenBaoGenerationGate();
        Task pendingReadiness = gate.SnapshotReadiness();

        OpenBaoGenerationLease initial = gate.BeginGeneration();

        initial.Readiness.Task.ShouldBeSameAs(pendingReadiness);
        initial.GenerationNumber.ShouldBe(1);
    }

    [Fact]
    public async Task BeginGeneration_WithoutStopNotificationRejectsStaleCompletion()
    {
        var gate = new OpenBaoGenerationGate();
        OpenBaoGenerationLease staleLease = gate.BeginGeneration();
        Task staleReadiness = staleLease.Readiness.Task;

        OpenBaoGenerationLease currentLease = gate.BeginGeneration();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => staleReadiness);
        staleLease.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        gate.TryFailCurrent(staleLease, new InvalidOperationException()).ShouldBeFalse();

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
    public void BeginGeneration_ResetsReadinessAfterASuccessfulGeneration()
    {
        var gate = new OpenBaoGenerationGate();
        OpenBaoGenerationLease first = gate.BeginGeneration();
        gate.TryInstallCurrent(first, () => { }).ShouldBeTrue();

        OpenBaoGenerationLease next = gate.BeginGeneration();

        first.Readiness.Task.IsCompletedSuccessfully.ShouldBeTrue();
        next.Readiness.Task.ShouldBeSameAs(gate.SnapshotReadiness());
        next.Readiness.Task.IsCompleted.ShouldBeFalse();
    }
}
