// <copyright file="AppHostComponentFileOrderingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Testing;

using Hexalith.Memories.AppHost;
using Hexalith.Memories.TestHelpers.Process;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using StackExchange.Redis;

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
        const string priorGenerationMarker = "# prior Redis generation";
        string pubSubPath = Path.Combine(ownedDirectory, "pubsub.yaml");
        var redisRestartObservation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool observeNextMcpStart = false;

        DistributedApplicationEventSubscription startObserver = builder.Eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, cancellationToken) =>
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
                if (@event.Resource.Name == "memories-mcp" && Volatile.Read(ref observeNextMcpStart))
                {
                    try
                    {
                        string pubSub = await File.ReadAllTextAsync(pubSubPath, cancellationToken).ConfigureAwait(true);
                        redisRestartObservation.TrySetResult(!pubSub.Contains(priorGenerationMarker, StringComparison.Ordinal));
                    }
                    catch (Exception exception)
                    {
                        redisRestartObservation.TrySetException(exception);
                    }
                }

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
                if (consumer is "server" or "lifecycle" or "clock")
                {
                    string stateStore = await File.ReadAllTextAsync(
                        Path.Combine(ownedDirectory, "statestore.yaml")).ConfigureAwait(true);
                    redisReady = stateStore.Contains("redisHost", StringComparison.Ordinal) &&
                        !stateStore.Contains("value: \"127.0.0.1:6379\"", StringComparison.Ordinal);
                    string pubSub = await File.ReadAllTextAsync(
                        Path.Combine(ownedDirectory, "pubsub.yaml")).ConfigureAwait(true);
                    IResource redis = builder.Resources.Single(resource => resource.Name == "memories-vectors");
                    redis.TryGetEndpoints(out IEnumerable<EndpointAnnotation>? endpoints).ShouldBeTrue();
                    AllocatedEndpoint allocated = endpoints!.Single(endpoint => endpoint.Name == "redis")
                        .AllocatedEndpoint.ShouldNotBeNull();
                    redisReady = redisReady && pubSub.Contains("type: pubsub.redis", StringComparison.Ordinal) &&
                        pubSub.Contains($"value: \"{allocated.Address}:{allocated.Port}\"", StringComparison.Ordinal);
                    if (consumer == "clock")
                    {
                        // The clock's pubsub consumer must have a responsive Redis at the start boundary.
                        using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(
                            $"{allocated.Address}:{allocated.Port},abortConnect=true,connectTimeout=3000")
                            .WaitAsync(cancellationToken).ConfigureAwait(true);
                        _ = await connection.GetDatabase().PingAsync().WaitAsync(cancellationToken).ConfigureAwait(true);
                    }
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

        const string redisResource = "memories-vectors";
        app.ResourceNotifications.TryGetCurrentState(redisResource, out ResourceEvent? originalRedis).ShouldBeTrue();
        DateTime? originalStart = originalRedis!.Snapshot.StartTimeStamp;
        originalStart.ShouldNotBeNull();

        // A restart can reuse the same host port. Mark the generated file so we can tell that
        // a consumer waited for the new generation's rewrite rather than cached readiness.
        await File.AppendAllTextAsync(pubSubPath, $"{Environment.NewLine}{priorGenerationMarker}{Environment.NewLine}", cts.Token)
            .ConfigureAwait(true);
        ExecuteCommandResult redisRestart = await app.ResourceCommands.ExecuteCommandAsync(
            redisResource, KnownResourceCommands.RestartCommand, cts.Token).ConfigureAwait(true);
        redisRestart.Success.ShouldBeTrue(redisRestart.Message);
        _ = await app.ResourceNotifications.WaitForResourceAsync(
            redisResource,
            current => current.Snapshot.State?.Text == KnownResourceStates.Running &&
                current.Snapshot.StartTimeStamp > originalStart,
            cts.Token).ConfigureAwait(true);

        Volatile.Write(ref observeNextMcpStart, true);
        ExecuteCommandResult mcpRestart = await app.ResourceCommands.ExecuteCommandAsync(
            "memories-mcp", KnownResourceCommands.RestartCommand, cts.Token).ConfigureAwait(true);
        mcpRestart.Success.ShouldBeTrue(mcpRestart.Message);
        bool rewriteObservedAtStart = await redisRestartObservation.Task.WaitAsync(cts.Token).ConfigureAwait(true);
        rewriteObservedAtStart.ShouldBeTrue("MCP startup must observe the current Redis generation's pub/sub file.");
        _ = await app.ResourceNotifications.WaitForResourceHealthyAsync("memories-mcp", cts.Token).ConfigureAwait(true);

        builder.Eventing.Unsubscribe(startObserver);
        await AssertClockWaitsForRedisProbeAsync(app, pubSubPath, cts.Token).ConfigureAwait(true);
        await app.StopAsync(cts.Token).ConfigureAwait(true);
    }

    private static async Task AssertClockWaitsForRedisProbeAsync(
        DistributedApplication app,
        string pubSubPath,
        CancellationToken cancellationToken)
    {
        const string rejection = "-ERR controlled clock readiness probe";
        DistributedApplicationModel model = app.Services.GetRequiredService<DistributedApplicationModel>();
        IDistributedApplicationEventing eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        IResource redis = model.Resources.Single(resource => resource.Name == "memories-vectors");
        IResource clock = model.Resources.Single(resource => resource.Name == "memories-access-telemetry-clock");
        EndpointAnnotation endpoint = redis.Annotations.OfType<EndpointAnnotation>().Single(annotation => annotation.Name == "redis");
        AllocatedEndpoint original = endpoint.AllocatedEndpoint.ShouldNotBeNull();
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        var controlled = new AllocatedEndpoint(endpoint, IPAddress.Loopback.ToString(), ((IPEndPoint)listener.LocalEndpoint).Port);
        using var probeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var probeReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseReply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task PublishStartAsync(IResource resource) => eventing.PublishAsync(
            new BeforeResourceStartedEvent(resource, app.Services),
            EventDispatchBehavior.BlockingSequential,
            cancellationToken);

        // The topology is already up. Publish the real startup callbacks on a fresh Redis
        // generation while the healthy OpenBao generation and the running services stay in place.
        endpoint.AllocatedEndpoint = controlled;
        Task responses = ReplyToRedisProbesAsync(probeCancellation.Token);
        try
        {
            await PublishStartAsync(redis).ConfigureAwait(true);
            await Task.WhenAny(probeReceived.Task, responses).Unwrap().WaitAsync(cancellationToken).ConfigureAwait(true);

            Task clockStart = PublishStartAsync(clock);
            // The only way for the clock callback to see this exact error is to await its
            // Redis gate. A clock with no enrollment can return while the reply is withheld.
            releaseReply.SetResult();
            Exception? failure = await Record.ExceptionAsync(() =>
                Task.WhenAny(clockStart, responses).Unwrap().WaitAsync(cancellationToken)).ConfigureAwait(true);
            failure.ShouldBeOfType<RedisProbeNonRetryableException>(
                "Clock startup must await the readiness probe for its current Redis allocation.");
            failure!.Message.ShouldContain(rejection);
            (await File.ReadAllTextAsync(pubSubPath, cancellationToken).ConfigureAwait(true))
                .ShouldContain($"value: \"{controlled.Address}:{controlled.Port}\"");
        }
        finally
        {
            endpoint.AllocatedEndpoint = original;
            await probeCancellation.CancelAsync().ConfigureAwait(true);
            await responses.ConfigureAwait(true);
        }

        await PublishStartAsync(redis).ConfigureAwait(true);
        await PublishStartAsync(clock).WaitAsync(cancellationToken).ConfigureAwait(true);

        async Task ReplyToRedisProbesAsync(CancellationToken stopToken)
        {
            byte[] expectedPing = "*1\r\n$4\r\nPING\r\n"u8.ToArray();
            byte[] ping = new byte[expectedPing.Length];
            byte[] reply = System.Text.Encoding.ASCII.GetBytes(rejection + "\r\n");
            try
            {
                while (true)
                {
                    using TcpClient probe = await listener.AcceptTcpClientAsync(stopToken).ConfigureAwait(true);
                    using NetworkStream stream = probe.GetStream();
                    try
                    {
                        await stream.ReadExactlyAsync(ping, stopToken).ConfigureAwait(true);
                        ping.ShouldBe(expectedPing);
                        probeReceived.TrySetResult();
                        await releaseReply.Task.WaitAsync(stopToken).ConfigureAwait(true);
                        await stream.WriteAsync(reply, stopToken).ConfigureAwait(true);
                    }
                    catch (IOException)
                    {
                        // Redis may close an expired attempt while the clock callback is being dispatched.
                    }
                }
            }
            catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
            {
            }
        }
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
