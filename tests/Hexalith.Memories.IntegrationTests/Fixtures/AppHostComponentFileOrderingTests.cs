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

using Microsoft.Extensions.DependencyInjection;

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
        using EnvVarScope randomizePortsScope = EnvVarScope.Set("MEMORIES_ASPIRE_RANDOMIZE_PROJECT_PORTS", "true");
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>()
            .ConfigureAwait(true);

        string ownedDirectory = Path.Combine(
            Path.GetTempPath(),
            "hexalith-memories-dapr",
            $"{daprAppId}-{Process.GetCurrentProcess().Id}");
        string[] expectedStartedResources =
        [
            "memories",
            "memories-dapr-cli",
            "memories-access-telemetry",
            "memories-access-telemetry-dapr-cli",
            "memories-access-telemetry-clock",
            "memories-access-telemetry-clock-dapr-cli",
            "memories-mcp",
            "memories-mcp-dapr-cli",
        ];
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
                observations.AddOrUpdate(@event.Resource.Name, true, (_, previous) => previous);
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

                bool observedReady = componentsReady && runtimeTokenReady && accessTokenReady && redisReady;
                observations.AddOrUpdate(
                    @event.Resource.Name,
                    observedReady,
                    (_, previous) => previous && observedReady);
            }
            catch (Exception exception)
            {
                observations.AddOrUpdate(@event.Resource.Name, false, (_, _) => false);
                observationErrors[@event.Resource.Name] = $"{exception.GetType().Name}: {exception.Message}";
            }
        });

        await using DistributedApplication app = await builder.BuildAsync().ConfigureAwait(true);
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(3));

        await app.StartAsync(cts.Token).ConfigureAwait(true);
        for (int attempt = 0;
            attempt < 600 && expectedStartedResources.Any(resource => !observations.ContainsKey(resource));
            attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token).ConfigureAwait(true);
        }

        string observationContext = $"Started resources: {string.Join(", ", startedResourceNames.Order())}; " +
            $"observer errors: {string.Join(" | ", observationErrors.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}";
        foreach (string resourceName in expectedStartedResources)
        {
            observations.Keys.ShouldContain(resourceName, observationContext);
            observations[resourceName].ShouldBeTrue(observationContext);
        }

        _ = await app.ResourceNotifications.WaitForResourceHealthyAsync("memories-mcp", cts.Token)
            .WaitAsync(TimeSpan.FromMinutes(2), cts.Token).ConfigureAwait(true);

        string repoRoot = ResolveRepoRoot();
        string[] daprConfigurations =
        [
            Path.Combine(repoRoot, "deploy", "dapr", "config.yaml"),
            Path.Combine(repoRoot, "deploy", "dapr", "access-telemetry-lifecycle-config.yaml"),
            Path.Combine(repoRoot, "deploy", "dapr", "access-telemetry-clock-config.yaml"),
        ];
        foreach (string configurationPath in daprConfigurations)
        {
            string configuration = await File.ReadAllTextAsync(configurationPath, cts.Token).ConfigureAwait(true);
            configuration.ShouldContain("- name: HotReload");
            configuration.ShouldContain("enabled: false");
        }

        ResourceLoggerService resourceLogs = app.Services.GetRequiredService<ResourceLoggerService>();
        foreach (string sidecarName in expectedStartedResources.Where(name => name.EndsWith("-dapr-cli", StringComparison.Ordinal)))
        {
            app.ResourceNotifications.TryGetCurrentState(sidecarName, out ResourceEvent? sidecarEvent)
                .ShouldBeTrue(observationContext);
            IResource resource = sidecarEvent!.Resource;
            await foreach (IReadOnlyList<LogLine> batch in resourceLogs.GetAllAsync(resource))
            {
                foreach (LogLine line in batch)
                {
                    line.Content.ShouldNotContain("too many open files", Case.Insensitive);
                    line.Content.ShouldNotContain("inotify_add_watch", Case.Insensitive);
                    line.Content.ShouldNotContain("no space left on device", Case.Insensitive);
                }
            }
        }

        string program = File.ReadAllText(Path.Combine(
            repoRoot, "src", "Hexalith.Memories.AppHost", "Program.cs"));
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
