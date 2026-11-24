// <copyright file="OpenBaoTopologyIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;

using Shouldly;

using Xunit;

/// <summary>Contains the nightly integration cases for OpenBaoTopologyIntegrationTests.</summary>
public sealed partial class OpenBaoTopologyIntegrationTests
{
    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task FullTopologyRestart_ReinitializesOpenBaoAndRecoversPermittedDaprReads()
    {
        (await fixture.CanReadOpenBaoRuntimeCanaryAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.CanReadOpenBaoAccessMarkerAsync().ConfigureAwait(true)).ShouldBeTrue();

        int preRestartLogCursor = fixture.LogEntryCount;
        preRestartLogCursor.ShouldBeGreaterThan(0);
        TimeSpan restartDuration = await fixture.RestartTopologyAsync().ConfigureAwait(true);

        IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> currentLogs = fixture.GetLogEntriesSince(0);
        currentLogs.ShouldNotBeEmpty();
        fixture.GetLogEntriesSince(preRestartLogCursor).Take(currentLogs.Count).ShouldBe(currentLogs);

        (await fixture.IsOpenBaoInitializedAndUnsealedAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.CanReadOpenBaoRuntimeCanaryAsync().ConfigureAwait(true)).ShouldBeTrue();
        (await fixture.CanReadOpenBaoAccessMarkerAsync().ConfigureAwait(true)).ShouldBeTrue();
        output.WriteLine($"Disposable topology restart completed in {restartDuration.TotalSeconds:F3}s.");
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    [Trait("Category", "Performance")]
    public void OpenBaoColdStart_TopologyAcceptsQueriesWithinNfr7()
    {
        fixture.OpenBaoColdStartDuration.ShouldBeLessThan(
            TimeSpan.FromSeconds(60),
            "NFR7 measures from all containers running until the root topology accepts queries, excluding image pull.");
        output.WriteLine(
            $"OpenBao cold start: {fixture.OpenBaoColdStartDuration.TotalSeconds:F3}s; " +
            $"OS={Environment.OSVersion}; framework={Environment.Version}; lane=Aspire root integration.");
    }
}
