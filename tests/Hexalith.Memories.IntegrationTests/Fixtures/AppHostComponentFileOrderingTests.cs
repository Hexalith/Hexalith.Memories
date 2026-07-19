// <copyright file="AppHostComponentFileOrderingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Testing;

using Hexalith.Memories.TestHelpers.Process;

using Shouldly;

/// <summary>
/// Behavioral guard that each Dapr sidecar sees complete current-generation Redis and OpenBao files
/// at its actual <see cref="BeforeResourceStartedEvent"/> boundary.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppHostComponentFileOrderingTests
{
    [Fact]
    public async Task SidecarStart_ObservesCurrentRedisAndOpenBaoGenerationFiles()
    {
        string daprAppId = $"memories-ordering-{Guid.NewGuid():N}";
        using EnvVarScope daprAppIdScope = EnvVarScope.Set("MEMORIES_DAPR_APP_ID", daprAppId);
        using EnvVarScope keycloakScope = EnvVarScope.Set("EnableKeycloak", "false");
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>()
            .ConfigureAwait(true);

        string ownedDirectory = Path.Combine(
            Path.GetTempPath(),
            "hexalith-memories-dapr",
            $"{daprAppId}-{Process.GetCurrentProcess().Id}");
        var observations = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
        var observationErrors = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        var startedResourceNames = new ConcurrentBag<string>();

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, _) =>
        {
            startedResourceNames.Add(@event.Resource.Name);
            string? consumer = @event.Resource.Name switch
            {
                "memories" or "memories-dapr" or "memories-dapr-cli" => "server",
                "memories-access-telemetry" or "memories-access-telemetry-dapr" or
                    "memories-access-telemetry-dapr-cli" => "lifecycle",
                "memories-access-telemetry-clock" or "memories-access-telemetry-clock-dapr" or
                    "memories-access-telemetry-clock-dapr-cli" => "clock",
                "memories-mcp" or "memories-mcp-dapr" or "memories-mcp-dapr-cli" => "mcp",
                _ => null,
            };
            if (consumer is null)
            {
                return;
            }

            if (consumer == "mcp")
            {
                observations[consumer] = true;
                return;
            }

            try
            {
                string runtimeComponent = await File.ReadAllTextAsync(
                    Path.Combine(ownedDirectory, "secretstore.yaml")).ConfigureAwait(true);
                string accessComponent = await File.ReadAllTextAsync(
                    Path.Combine(ownedDirectory, "access-telemetry-secrets.yaml")).ConfigureAwait(true);
                bool runtimeTokenReady = !string.IsNullOrWhiteSpace(await File.ReadAllTextAsync(
                    Path.Combine(ownedDirectory, "openbao-runtime.token")).ConfigureAwait(true));
                bool accessTokenReady = !string.IsNullOrWhiteSpace(await File.ReadAllTextAsync(
                    Path.Combine(ownedDirectory, "openbao-access-telemetry.token")).ConfigureAwait(true));
                bool componentsReady = runtimeComponent.Contains("type: secretstores.hashicorp.vault", StringComparison.Ordinal) &&
                    runtimeComponent.Contains("hexalith/memories/runtime", StringComparison.Ordinal) &&
                    runtimeComponent.Contains("vaultTokenMountPath", StringComparison.Ordinal) &&
                    !runtimeComponent.Contains("http://127.0.0.1:1", StringComparison.Ordinal) &&
                    accessComponent.Contains("type: secretstores.hashicorp.vault", StringComparison.Ordinal) &&
                    accessComponent.Contains("hexalith/memories/access-telemetry", StringComparison.Ordinal) &&
                    !accessComponent.Contains("http://127.0.0.1:1", StringComparison.Ordinal);
                bool redisReady = true;
                if (consumer is "server" or "lifecycle")
                {
                    string stateStore = await File.ReadAllTextAsync(
                        Path.Combine(ownedDirectory, "statestore.yaml")).ConfigureAwait(true);
                    redisReady = stateStore.Contains("redisHost", StringComparison.Ordinal) &&
                        !stateStore.Contains("value: \"127.0.0.1:6379\"", StringComparison.Ordinal);
                }

                observations[consumer] = componentsReady && runtimeTokenReady && accessTokenReady && redisReady;
            }
            catch (Exception exception)
            {
                observations[consumer] = false;
                observationErrors[consumer] = $"{exception.GetType().Name}: {exception.Message}";
            }
        });

        await using DistributedApplication app = await builder.BuildAsync().ConfigureAwait(true);
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(3));

        await app.StartAsync(cts.Token).ConfigureAwait(true);
        for (int attempt = 0; attempt < 600 && observations.Count < 4; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token).ConfigureAwait(true);
        }

        string observationContext = $"Started resources: {string.Join(", ", startedResourceNames.Order())}; " +
            $"observer errors: {string.Join(" | ", observationErrors.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}";
        observations.Keys.ShouldContain("server", observationContext);
        observations.Keys.ShouldContain("lifecycle", observationContext);
        observations.Keys.ShouldContain("clock", observationContext);
        observations.Keys.ShouldContain("mcp", observationContext);
        observations["server"].ShouldBeTrue(observationContext);
        observations["lifecycle"].ShouldBeTrue(observationContext);
        observations["clock"].ShouldBeTrue(observationContext);

        _ = await app.ResourceNotifications.WaitForResourceHealthyAsync("memories-mcp", cts.Token)
            .WaitAsync(TimeSpan.FromMinutes(2), cts.Token).ConfigureAwait(true);

        string program = File.ReadAllText(Path.Combine(
            ResolveRepoRoot(), "src", "Hexalith.Memories.AppHost", "Program.cs"));
        int mcpStart = program.IndexOf("IResourceBuilder<ProjectResource> mcp", StringComparison.Ordinal);
        int mcpEnd = program.IndexOf("_ = mcp;", mcpStart, StringComparison.Ordinal);
        string mcpComposition = program[mcpStart..mcpEnd];
        mcpComposition.ShouldNotContain("secretStore");
        mcpComposition.ShouldNotContain("accessTelemetrySecrets");
        mcpComposition.ShouldNotContain("openBao");

        await app.StopAsync(cts.Token).ConfigureAwait(true);
    }

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }
}
