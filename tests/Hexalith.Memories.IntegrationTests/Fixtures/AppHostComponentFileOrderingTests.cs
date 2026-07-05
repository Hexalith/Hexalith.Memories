// <copyright file="AppHostComponentFileOrderingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.IO;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Testing;

using Shouldly;

/// <summary>
/// Story 15.6 AC #7 — behavioral guard that the DAPR sidecars do not start until the AppHost has
/// rewritten the local <c>statestore.yaml</c> with the Aspire-allocated Redis endpoint. The previous
/// implementation of this test was a source-text grep on Program.cs that could pass even when the
/// runtime ordering was broken (Story 15.6 code review patch).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppHostComponentFileOrderingTests
{
    [RunnableSkippedFact("Story 15.6 AC #7 behavioral guard — requires Docker (Redis/FalkorDB containers). Runs in the Aspire integration lane only; the default test lane does not provision containers. Unskip when the integration lane is wired up.")]
    public async Task SidecarStart_DoesNotBeginUntilStatestoreYamlIsRewrittenWithAllocatedRedisHost()
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>()
            .ConfigureAwait(true);

        // Capture the statestore.yaml content observed at the moment the memories-dapr sidecar
        // begins starting. This tap subscribes AFTER Program.cs's production subscriber, so Aspire
        // dispatches it second; the production subscriber awaits the rewrite TCS before it returns,
        // so by the time this tap runs the rewrite must be complete and any 127.0.0.1 placeholder
        // must have been replaced with the allocated Redis host:port.
        string? capturedStateStoreContent = null;
        string? capturedSidecarResourceName = null;

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, _) =>
        {
            if (@event.Resource.Name is not ("memories-dapr"
                or "memories-dapr-cli"
                or "memories-mcp-dapr"
                or "memories-mcp-dapr-cli"))
            {
                return;
            }

            capturedSidecarResourceName ??= @event.Resource.Name;

            string? statestoreYamlPath = LocateMostRecentStatestoreYaml();
            if (statestoreYamlPath is not null && File.Exists(statestoreYamlPath))
            {
                capturedStateStoreContent ??= await File.ReadAllTextAsync(statestoreYamlPath).ConfigureAwait(true);
            }
        });

        await using DistributedApplication app = await builder.BuildAsync().ConfigureAwait(true);
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(3));

        await app.StartAsync(cts.Token).ConfigureAwait(true);

        if (capturedSidecarResourceName is not null)
        {
            // Wait for the sidecar resource that actually exists in this DAPR hosting version
            // (for example memories-dapr-cli). The tap above runs before the sidecar starts, so
            // by the time that resource is healthy, capturedStateStoreContent reflects the file
            // state at the start barrier.
            _ = await app.ResourceNotifications
                .WaitForResourceAsync(
                    capturedSidecarResourceName,
                    e => e.Snapshot.State?.Text is "Running" or "Finished",
                    cts.Token)
                .ConfigureAwait(true);
        }

        capturedStateStoreContent.ShouldNotBeNull(
            "BeforeResourceStartedEvent did not fire for any DAPR sidecar — the rewrite-ordering invariant cannot be asserted.");
        capturedStateStoreContent!.ShouldContain(
            "redisHost",
            Case.Sensitive,
            "statestore.yaml should contain the redisHost metadata key once written.");
        capturedStateStoreContent.ShouldNotContain(
            "value: \"127.0.0.1:",
            Case.Sensitive,
            "The DAPR sidecar started before AppHost rewrote statestore.yaml with the Aspire-allocated Redis endpoint (Story 15.6 AC #2 / #7 regression).");

        await app.StopAsync(cts.Token).ConfigureAwait(true);
    }

    private static string? LocateMostRecentStatestoreYaml()
    {
        // AppHost writes component YAMLs under %TEMP%/hexalith-memories-dapr/{daprAppId}-{pid}/.
        // Locate the most recent statestore.yaml under any PID directory.
        string root = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr");
        if (!Directory.Exists(root))
        {
            return null;
        }

        return Directory.EnumerateFiles(root, "statestore.yaml", SearchOption.AllDirectories)
            .Select(static path => (Path: path, Modified: File.GetLastWriteTimeUtc(path)))
            .OrderByDescending(static entry => entry.Modified)
            .Select(static entry => entry.Path)
            .FirstOrDefault();
    }
}
