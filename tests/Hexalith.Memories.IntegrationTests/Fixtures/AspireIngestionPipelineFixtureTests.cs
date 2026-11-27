// <copyright file="AspireIngestionPipelineFixtureTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Dapr.Actors.Client;

using Hexalith.Memories.AppHost;
using Hexalith.Memories.IntegrationTests.Mcp;
using Hexalith.Memories.IntegrationTests.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

using NSubstitute;

using Shouldly;

using FusionWeights = Hexalith.Memories.Contracts.V1.FusionWeights;

/// <summary>Unit-level guards for <see cref="AspireIngestionPipelineFixture"/> helper methods.</summary>
[Trait("Category", "Integration")]
public sealed class AspireIngestionPipelineFixtureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReconnectPrimaryDaprClients_WhenAllocationFails_DisposesCandidatesAndPreservesActiveClients(
        bool failCreatingProxyFactory)
    {
        await using var fixture = new AspireIngestionPipelineFixture();
        Uri activeEndpoint = new("http://127.0.0.1:3501");
        Uri candidateEndpoint = new("http://127.0.0.1:3502");
        var activeState = new DaprClientRotationTrackingHandler();
        var activeActor = new DaprClientRotationTrackingHandler();
        var candidateState = new DaprClientRotationTrackingHandler();
        var candidateActor = new DaprClientRotationTrackingHandler();
        fixture.ReconnectPrimaryDaprClients(
            activeEndpoint,
            endpoint => new HttpClient(activeState) { BaseAddress = endpoint },
            () => activeActor,
            static (options, handler) => new ActorProxyFactory(options, handler));
        int activeGeneration = fixture.PrimaryDaprClientGeneration;
        InvalidOperationException injectedFailure = new("Injected client allocation failure.");

        var observedFailure = Should.Throw<InvalidOperationException>(() => fixture.ReconnectPrimaryDaprClients(
            candidateEndpoint,
            endpoint => new HttpClient(candidateState) { BaseAddress = endpoint },
            () => failCreatingProxyFactory ? candidateActor : throw injectedFailure,
            (_, _) => throw injectedFailure));

        observedFailure.ShouldBeSameAs(injectedFailure);
        candidateState.DisposeCount.ShouldBe(1);
        candidateActor.DisposeCount.ShouldBe(failCreatingProxyFactory ? 1 : 0);
        fixture.PrimaryDaprClientGeneration.ShouldBe(activeGeneration);
        fixture.DaprSidecarHttpEndpoint.ShouldBe(activeEndpoint);
        activeState.DisposeCount.ShouldBe(0);
        activeActor.DisposeCount.ShouldBe(0);
        _ = await fixture.GetDaprWorkflowStateDiagnosticAsync("workflow", TestContext.Current.CancellationToken);
        await fixture.CreateTenantConfigurationActorProxy("tenant-rotation").SetFusionWeightsAsync(new FusionWeights());
        activeState.RequestCount.ShouldBe(1);
        activeActor.RequestCount.ShouldBe(1);
    }

    [Fact]
    public void LogCapture_AfterRestart_ReadsOnlyTheCurrentLifetimeAtEitherCursor()
    {
        using var previous = new AspireIngestionPipelineFixture.TestLogProvider(0);
        ILogger staleLogger = ((ILoggerProvider)previous).CreateLogger("before restart");
        staleLogger.LogWarning("First lifetime");
        int beforeRestart = previous.Count;

        using var current = new AspireIngestionPipelineFixture.TestLogProvider(beforeRestart);
        ILogger logger = ((ILoggerProvider)current).CreateLogger("after restart");
        logger.LogWarning("Second lifetime");
        staleLogger.LogWarning("Late first-lifetime message");
        int cursor = current.Count;
        logger.LogWarning("Later second-lifetime message");

        current.Count.ShouldBe(beforeRestart + 2);
        current.GetEntriesSince(0).Select(entry => entry.Message)
            .ShouldBe(["Second lifetime", "Later second-lifetime message"]);
        current.GetEntriesSince(beforeRestart).Count.ShouldBe(2);
        current.GetEntriesSince(cursor).ShouldHaveSingleItem().Message.ShouldBe("Later second-lifetime message");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RestartPortDiagnostic_DistinguishesAllocatedPortFromReachableListener(bool listening)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
        if (listening)
        {
            socket.Listen(backlog: 1);
        }

        int port = ((IPEndPoint)socket.LocalEndPoint!).Port;
        var resource = new ContainerResource("memories-vectors");
        var endpoint = new EndpointAnnotation(ProtocolType.Tcp, name: "redis", targetPort: 6379);
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "0.0.0.0", port);
        resource.Annotations.Add(endpoint);

        string diagnostic = await AspireIngestionPipelineFixture.DescribeBackendPortAsync(
            resource, "redis", KnownResourceStates.Running);

        diagnostic.ShouldContain("memories-vectors/redis: state=Running, proxied=True, containerTargetPort=6379");
        diagnostic.ShouldContain($"hostPort=127.0.0.1:{port}");
        diagnostic.ShouldContain(listening ? "listener=reachable" : "listener=ConnectionRefused");
    }

    [Fact]
    public async Task RestartPortDiagnostic_PartialReadiness_DoesNotProbeUnallocatedOrNonLoopbackAddresses()
    {
        var resource = new ContainerResource("memories-graphs");
        var endpoint = new EndpointAnnotation(ProtocolType.Tcp, name: "falkordb", targetPort: 6379, isProxied: false);
        resource.Annotations.Add(endpoint);

        string pending = await AspireIngestionPipelineFixture.DescribeBackendPortAsync(resource, "falkordb", state: null);
        pending.ShouldContain("state=unobserved, proxied=False, containerTargetPort=6379");
        pending.ShouldContain("hostPort=unallocated, listener=unprobed");

        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "192.0.2.1", 16379);
        string remote = await AspireIngestionPipelineFixture.DescribeBackendPortAsync(resource, "falkordb", "Starting");
        remote.ShouldContain("hostPort=16379, listener=unprobed-non-loopback");
        remote.ShouldNotContain("192.0.2.1");
    }

    [Fact]
    public void OwnedSidecarListenersUseAllocatedPortsAndRetainExactRuntimeOptions()
    {
        var builder = Aspire.Hosting.DistributedApplication.CreateBuilder();
        _ = Aspire.Hosting.IDistributedApplicationResourceBuilderExtensions.WithDaprSidecar(
            builder.AddProject<Projects.Hexalith_Memories_Server>("owned"), sidecar =>
                sidecar.WithOptions(new CommunityToolkit.Aspire.Hosting.Dapr.DaprSidecarOptions
                { AppId = "exact-owner", DaprHttpPort = 3500, DaprGrpcPort = 50001, MetricsPort = 9090, Config = "owned.yaml" }));
        AspireIngestionPipelineFixture.ConfigureOwnedDaprPorts(builder.Resources);
        var options = builder.Resources.OfType<CommunityToolkit.Aspire.Hosting.Dapr.IDaprSidecarResource>().Single().Annotations
            .OfType<CommunityToolkit.Aspire.Hosting.Dapr.DaprSidecarOptionsAnnotation>().Single().Options;
        options.DaprHttpPort.ShouldBeNull();
        options.DaprGrpcPort.ShouldBeNull();
        options.MetricsPort.ShouldBeNull();
        options.AppId.ShouldBe("exact-owner");
        options.Config.ShouldBe("owned.yaml");
    }

    [Fact]
    public void OwnedBackendListenersUseDirectDynamicPortsWithoutChangingContainerTargets()
    {
        var redis = new ContainerResource("memories-vectors");
        var falkor = new ContainerResource("memories-graphs");
        var openBao = new ContainerResource("openbao");
        var redisEndpoint = new EndpointAnnotation(ProtocolType.Tcp, name: "redis", targetPort: 6379);
        var falkorEndpoint = new EndpointAnnotation(ProtocolType.Tcp, name: "falkordb", targetPort: 6379);
        var openBaoEndpoint = new EndpointAnnotation(ProtocolType.Tcp, name: "http", targetPort: 8200);
        redis.Annotations.Add(redisEndpoint);
        falkor.Annotations.Add(falkorEndpoint);
        openBao.Annotations.Add(openBaoEndpoint);

        AspireIngestionPipelineFixture.ConfigureOwnedBackendPorts([redis, falkor, openBao]);

        redisEndpoint.IsExplicitlyProxied.ShouldBe(false);
        redisEndpoint.Port.ShouldBe(0);
        redisEndpoint.TargetPort.ShouldBe(6379);
        falkorEndpoint.IsExplicitlyProxied.ShouldBe(false);
        falkorEndpoint.Port.ShouldBe(0);
        falkorEndpoint.TargetPort.ShouldBe(6379);
        openBaoEndpoint.IsExplicitlyProxied.ShouldBeNull();
        openBaoEndpoint.Port.ShouldBeNull();
    }

    [Fact]
    public async Task OwnedGatewaySelectsTheSameSingleSymmetricModeAsTheFixtureTokenFactory()
    {
        var gateway = new Aspire.Hosting.ApplicationModel.ProjectResource("eventstore");
        AspireIngestionPipelineFixture.ConfigureGatewayTestAuthentication(gateway);
        var environment = new Dictionary<string, object>
        {
            ["Authentication__JwtBearer__Authority"] = "https://inherited.invalid/realm",
            ["Authentication__JwtBearer__SigningKey"] = "inherited",
        };
        var context = new Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext(
            new Aspire.Hosting.DistributedApplicationExecutionContext(new Aspire.Hosting.DistributedApplicationExecutionContextOptions(Aspire.Hosting.DistributedApplicationOperation.Run)), gateway, environment, TestContext.Current.CancellationToken);
        await gateway.Annotations.OfType<Aspire.Hosting.ApplicationModel.EnvironmentCallbackAnnotation>().ShouldHaveSingleItem().Callback(context);
        environment["Authentication__JwtBearer__Authority"].ShouldBe(string.Empty);
        environment["Authentication__JwtBearer__SigningKey"].ShouldBe(AspireIngestionPipelineFixture.ServerDevSigningKey);
        var token = new JsonWebToken(AspireIngestionPipelineFixture.MintServerBearer("tenant"));
        token.Issuer.ShouldBe(environment["Authentication__JwtBearer__Issuer"]);
        token.Audiences.ShouldContain((string)environment["Authentication__JwtBearer__Audience"]);
        token.Alg.ShouldBe("HS256");
    }

    [Fact]
    public void MintDevBearer_ExplicitPastExpiry_CreatesExpiredToken()
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(-1);

        string token = AspireIngestionPipelineFixture.MintDevBearer(
            "tenant-expired-probe",
            expiresAt: expiresAt);

        var jwt = new JsonWebToken(token);
        jwt.ValidTo.ShouldBeLessThan(DateTime.UtcNow);
        jwt.ValidFrom.ShouldBeLessThan(jwt.ValidTo);
    }

    [Fact]
    public void RepositoryRootLocator_NestedCurrentDirectory_ReturnsMarkerDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"memories-root-{Guid.NewGuid():N}");
        string nested = Path.Combine(root, "src", "Hexalith.Memories.AppHost", "bin");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(root, "Hexalith.Memories.slnx"), string.Empty);

        try
        {
            string resolved = RepositoryRootLocator.Resolve(currentDirectory: nested, baseDirectory: nested);
            resolved.ShouldBe(Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RepositoryRootLocator_MissingMarker_Throws()
    {
        string root = Path.Combine(Path.GetTempPath(), $"memories-root-missing-{Guid.NewGuid():N}");
        string nested = Path.Combine(root, "bin");
        Directory.CreateDirectory(nested);

        try
        {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
                RepositoryRootLocator.Resolve(currentDirectory: nested, baseDirectory: nested));

            exception.Message.ShouldContain("Hexalith.Memories.slnx");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveDaprSidecarHttpEndpoint_DistinctExactResources_ReturnDistinctDirectPorts()
    {
        AspireIngestionPipelineFixture.CapturedLogEntry[] entries =
        [
            new(
                LogLevel.Information,
                "memories-dapr-cli.stdout",
                "HTTP server listening on TCP address: :41001"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources.memories-access-telemetry-clock-dapr-cli",
                "HTTP server listening on TCP address: :41002"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources.memories-access-telemetry-dapr-cli.stderr",
                "HTTP server listening on TCP address: :41003"),
        ];

        AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(
            "memories-dapr-cli",
            entries).ShouldBe(new Uri("http://127.0.0.1:41001"));
        AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(
            "memories-access-telemetry-clock-dapr-cli",
            entries).ShouldBe(new Uri("http://127.0.0.1:41002"));
        AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(
            "memories-access-telemetry-dapr-cli",
            entries).ShouldBe(new Uri("http://127.0.0.1:41003"));
    }

    [Fact]
    public void ResolveDaprSidecarHttpEndpoint_NewerMalformedAndSimilarEntries_ReturnLatestExactValidPort()
    {
        const string resourceName = "memories-access-telemetry-clock-dapr-cli";
        AspireIngestionPipelineFixture.CapturedLogEntry[] entries =
        [
            new(
                LogLevel.Information,
                resourceName,
                "HTTP server listening on TCP address: :42001"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :42002"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources." + resourceName + "-extra",
                "HTTP server listening on TCP address: :42991"),
            new(
                LogLevel.Information,
                resourceName + "-extra.stdout",
                "HTTP server listening on TCP address: :42992"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :42003x"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :42004-extra"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :0"),
            new(
                LogLevel.Information,
                resourceName + ".stdout",
                "HTTP server listening on TCP address: :65536"),
        ];

        Uri endpoint = AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(resourceName, entries);

        endpoint.ShouldBe(new Uri("http://127.0.0.1:42002"));
    }

    [Fact]
    public void ResolveDaprSidecarHttpEndpoint_MissingExactValidEvidence_FailsClosed()
    {
        const string resourceName = "memories-access-telemetry-clock-dapr-cli";
        AspireIngestionPipelineFixture.CapturedLogEntry[] entries =
        [
            new(
                LogLevel.Information,
                resourceName + "-extra",
                "HTTP server listening on TCP address: :43001"),
            new(
                LogLevel.Information,
                "Hexalith.Memories.AppHost.Resources." + resourceName + ".stdout",
                "HTTP server listening on TCP address: :invalid"),
        ];

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.ResolveDaprSidecarHttpEndpoint(resourceName, entries));

        exception.Message.ShouldContain(resourceName);
        exception.Message.ShouldContain("direct daprd HTTP endpoint");
        exception.Message.ShouldNotContain("proxy", Case.Insensitive);
    }

    [Fact]
    public async Task WaitForDaprSidecarHttpEndpointAsync_DelayedExactEvidence_Converges()
    {
        const string resourceName = "memories-access-telemetry-clock-dapr-cli";
        int snapshotCount = 0;

        Uri endpoint = await AspireIngestionPipelineFixture.WaitForDaprSidecarHttpEndpointAsync(
            resourceName,
            () => Interlocked.Increment(ref snapshotCount) == 1
                ? []
                :
                [
                    new(
                        LogLevel.Information,
                        "Aspire.Hosting.Resources." + resourceName + ".stdout",
                        "HTTP server listening on TCP address: :44001"),
                ],
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            CancellationToken.None);

        endpoint.ShouldBe(new Uri("http://127.0.0.1:44001"));
        snapshotCount.ShouldBe(2);
    }

    [Fact]
    public async Task WaitForDaprSidecarHttpEndpointAsync_MissingEvidenceUntilDeadline_FailsClosed()
    {
        const string resourceName = "memories-access-telemetry-dapr-cli";

        TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForDaprSidecarHttpEndpointAsync(
                resourceName,
                static () => [],
                TimeSpan.FromMilliseconds(25),
                TimeSpan.FromMilliseconds(5),
                CancellationToken.None));

        exception.Message.ShouldContain(resourceName);
        exception.Message.ShouldContain("direct daprd HTTP endpoint");
        exception.Message.ShouldNotContain("proxy", Case.Insensitive);
    }

    [Fact]
    public async Task WaitForOpenBaoSidecarMatrixReadinessCoreAsync_UsesFreshListenersAndExactScopedResponses()
    {
        using var logs = new AspireIngestionPipelineFixture.TestLogProvider(0);
        ILogger clock = ((ILoggerProvider)logs).CreateLogger("memories-access-telemetry-clock-dapr-cli");
        ILogger lifecycle = ((ILoggerProvider)logs).CreateLogger("memories-access-telemetry-dapr-cli");
        clock.LogInformation("HTTP server listening on TCP address: :44001");
        lifecycle.LogInformation("HTTP server listening on TCP address: :44003");
        int restartCursor = logs.Count;
        var probes = new ConcurrentQueue<(int Port, string Path)>();
        int probeCount = 0;
        const string clockPath = "/v1.0/secrets/access-telemetry-secrets/access-telemetry-clock-key";
        const string lifecyclePath = "/v1.0/secrets/access-telemetry-secrets/access-telemetry-marker-key";

        Task readiness = AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessCoreAsync(
            logs,
            restartCursor,
            endpoint => new HttpClient(new DaprClientRotationTrackingHandler(RespondAsync)) { BaseAddress = endpoint },
            SHA256.HashData("clock-fixture"u8),
            SHA256.HashData("marker-fixture"u8),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMilliseconds(5),
            TestContext.Current.CancellationToken);

        probes.ShouldBeEmpty();
        readiness.IsCompleted.ShouldBeFalse();
        lifecycle.LogInformation("HTTP server listening on TCP address: :44003");
        clock.LogInformation("HTTP server listening on TCP address: :44001");
        await readiness;

        probes.ToArray().ShouldBe(
        [
            (44001, clockPath),
            (44002, clockPath),
            (44002, clockPath),
            (44002, clockPath),
            (44003, lifecyclePath),
        ]);

        Task<HttpResponseMessage> RespondAsync(HttpRequestMessage request, CancellationToken _)
        {
            Uri requestUri = request.RequestUri.ShouldNotBeNull();
            probes.Enqueue((requestUri.Port, requestUri.AbsolutePath));
            int attempt = ++probeCount;
            if (attempt == 1)
            {
                clock.LogInformation("HTTP server listening on TCP address: :44002");
            }

            string body = attempt switch
            {
                2 => "{\"access-telemetry-clock-key\":\"clock-fixture\"}",
                3 => "{\"signing-key-pkcs8\":\"incorrect-fixture\"}",
                5 => "{\"access-telemetry-marker-key\":\"marker-fixture\"}",
                _ => "{\"signing-key-pkcs8\":\"clock-fixture\"}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    [Fact]
    public async Task WaitForOpenBaoSidecarMatrixReadinessCoreAsync_CallerCancellationStopsAnActiveProbe()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var logs = new AspireIngestionPipelineFixture.TestLogProvider(0);
        ((ILoggerProvider)logs).CreateLogger("memories-access-telemetry-clock-dapr-cli")
            .LogInformation("HTTP server listening on TCP address: :44001");
        var probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task readiness = AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessCoreAsync(
            logs,
            0,
            endpoint => new HttpClient(new DaprClientRotationTrackingHandler(RespondAsync)) { BaseAddress = endpoint },
            SHA256.HashData("clock-fixture"u8),
            SHA256.HashData("marker-fixture"u8),
            TimeSpan.FromSeconds(5),
            TimeSpan.Zero,
            cts.Token);

        await probeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => readiness);

        async Task<HttpResponseMessage> RespondAsync(HttpRequestMessage _, CancellationToken token)
        {
            probeStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException("Cancellation should stop this probe.");
        }
    }

    [Fact]
    public async Task WaitForOpenBaoSidecarMatrixReadinessCoreAsync_ClockSidecarFailure_StopsRemainingProbes()
    {
        using var logs = new AspireIngestionPipelineFixture.TestLogProvider(0);
        ((ILoggerProvider)logs).CreateLogger("memories-access-telemetry-clock-dapr-cli")
            .LogInformation("HTTP server listening on TCP address: :44001");
        using var handler = new DaprClientRotationTrackingHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessCoreAsync(
                logs,
                0,
                endpoint => new HttpClient(handler, disposeHandler: false) { BaseAddress = endpoint },
                SHA256.HashData("clock-fixture"u8),
                SHA256.HashData("marker-fixture"u8),
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("403 Forbidden");
        handler.RequestCount.ShouldBe(1);
    }

    [Fact]
    public void OpenBaoSidecarMatrixReadiness_InPlaceRestartWaitsForTheNewGenerationOutsideSharedStartup()
    {
        Type fixtureType = typeof(AspireIngestionPipelineFixture);
        const BindingFlags instanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo sharedTopologyStartup = fixtureType.GetMethod("StartTopologyAsync", instanceNonPublic)
            .ShouldNotBeNull();
        MethodInfo fullTopologyRestart = fixtureType.GetMethod(nameof(AspireIngestionPipelineFixture.RestartTopologyAsync))
            .ShouldNotBeNull();
        MethodInfo inPlaceRestart = fixtureType.GetMethod(
            nameof(AspireIngestionPipelineFixture.RestartOpenBaoGenerationInPlaceAsync), instanceNonPublic)
            .ShouldNotBeNull();
        MethodInfo[] auxiliaryReadinessMethods = fixtureType.GetMethods(instanceNonPublic)
            .Where(method => method.Name == nameof(AspireIngestionPipelineFixture.WaitForOpenBaoSidecarMatrixReadinessAsync))
            .ToArray();
        MethodInfo generationReadiness = auxiliaryReadinessMethods.Single(method =>
            method.GetParameters() is [{ ParameterType: var cursorType }, _] && cursorType == typeof(int));

        AsyncMethodCalls(inPlaceRestart, generationReadiness).ShouldBeTrue();
        auxiliaryReadinessMethods.ShouldAllBe(method => !AsyncMethodCalls(sharedTopologyStartup, method));
        auxiliaryReadinessMethods.ShouldAllBe(method => !AsyncMethodCalls(fullTopologyRestart, method));
    }

    [Fact]
    public void CreateDaprSidecarClient_DirectEndpoint_UsesResolvedLoopbackAddressAndThirtySecondTimeout()
    {
        Uri endpoint = new("http://127.0.0.1:41002");
        using HttpClient client = AspireIngestionPipelineFixture.CreateDaprSidecarClient(endpoint);

        client.BaseAddress.ShouldNotBeNull();
        client.BaseAddress!.Scheme.ShouldBe(Uri.UriSchemeHttp);
        client.BaseAddress.Host.ShouldBe("127.0.0.1");
        client.BaseAddress.Port.ShouldBe(41002);
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        client.DefaultRequestHeaders.Contains("dapr-api-token")
            .ShouldBe(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAPR_API_TOKEN")));
    }

    [Fact]
    public void CreateDaprSidecarClient_NamedSidecarFactory_IsInternalAndRequiresRunningTopology()
    {
        MethodInfo namedFactory = typeof(AspireIngestionPipelineFixture)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == nameof(AspireIngestionPipelineFixture.CreateDaprSidecarClient) &&
                method.GetParameters() is [{ ParameterType: { } parameterType }] &&
                parameterType == typeof(string));

        namedFactory.IsPublic.ShouldBeFalse();
        namedFactory.IsAssembly.ShouldBeTrue();

        var fixture = new AspireIngestionPipelineFixture();
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            fixture.CreateDaprSidecarClient("memories-access-telemetry-clock-dapr-cli"));
        exception.Message.ShouldContain("topology is not running");
    }

    [Fact]
    public void AccessTelemetryAspireRoutedTests_JoinSharedCollectionWithoutSecondAppHostOrAspireDaprCliHealth()
    {
        CollectionAttribute collection = typeof(AccessTelemetryAspireRoutedIntegrationTests)
            .GetCustomAttribute<CollectionAttribute>()
            .ShouldNotBeNull();
        collection.Name.ShouldBe("AspireIngestionPipeline");

        string source = File.ReadAllText(Path.Combine(
            RepositoryRootLocator.Resolve(),
            "tests",
            "Hexalith.Memories.IntegrationTests",
            "Telemetry",
            "AccessTelemetryAspireRoutedIntegrationTests.cs"));
        string compacted = string.Concat(source.Where(static character => !char.IsWhiteSpace(character)));

        compacted.ShouldContain("WaitForOpenBaoSidecarMatrixReadinessAsync");
        compacted.ShouldContain("CreateDaprSidecarClient");
        compacted.ShouldNotContain("DistributedApplicationTestingBuilder.CreateAsync");
        compacted.ShouldNotContain("DistributedApplicationTestingBuilder");
        compacted.ShouldNotContain("WaitForResourceHealthyAsync");
        compacted.ShouldNotContain("WaitForHealthyAsync");

        typeof(AccessTelemetryAspireRoutedIntegrationTests).Assembly
            .GetTypes()
            .Select(static type => type.Name)
            .ShouldNotContain("AccessTelemetryAspireRoutedCollection");
    }

    [Theory]
    [InlineData(EmbeddingProviderTestMode.OllamaOidcFake)]
    [InlineData(EmbeddingProviderTestMode.GoogleFake)]
    public async Task FailedInitialization_CapturesOllamaRedisPortBeforeCleanup(EmbeddingProviderTestMode providerMode)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
        socket.Listen(backlog: 1);
        int port = ((IPEndPoint)socket.LocalEndPoint!).Port;

        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });
        ContainerResource redis = builder.AddContainer("memories-vectors", "redis").Resource;
        var endpoint = new EndpointAnnotation(ProtocolType.Tcp, name: "redis", targetPort: 6379);
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "127.0.0.1", port);
        redis.Annotations.Add(endpoint);

        await using DistributedApplication app = builder.Build();
        await app.ResourceNotifications.PublishUpdateAsync(
            redis,
            static snapshot => snapshot with { State = new ResourceStateSnapshot(KnownResourceStates.Running, Style: null) });
        var startupException = new TimeoutException("owned Redis startup failed");
        string? capturedBeforeCleanup = null;
        bool cleanupRan = false;

        TimeoutException thrown = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterInitializationFailureAsync(
                startupException,
                app,
                providerMode,
                [
                    new("topology", () =>
                    {
                        capturedBeforeCleanup =
                            startupException.Data[AspireIngestionPipelineFixture.OllamaStartupRedisPortDataKey] as string;
                        socket.Dispose();
                        cleanupRan = true;
                        return Task.CompletedTask;
                    }),
                ]));

        thrown.ShouldBeSameAs(startupException);
        cleanupRan.ShouldBeTrue();
        if (providerMode == EmbeddingProviderTestMode.OllamaOidcFake)
        {
            capturedBeforeCleanup.ShouldNotBeNull();
            capturedBeforeCleanup.ShouldContain(
                "memories-vectors/redis: state=Running, proxied=True, containerTargetPort=6379");
            capturedBeforeCleanup.ShouldContain($"hostPort=127.0.0.1:{port}, listener=reachable");
            capturedBeforeCleanup.ShouldNotContain("memories-graphs");
        }
        else
        {
            capturedBeforeCleanup.ShouldBeNull();
        }
    }

    [Fact]
    public async Task FailedInitialization_CapturesOpenBaoAllocationAndReachabilityBeforeCleanup()
    {
        const string syntheticSeed = "synthetic-openbao-seed-canary";
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
        int port = ((IPEndPoint)socket.LocalEndPoint!).Port;

        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });
        ContainerResource openBao = builder.AddContainer(OpenBaoDevelopmentProfile.ResourceName, "openbao")
            .WithEnvironment("OPENBAO_TEST_SEED", syntheticSeed).Resource;
        var endpoint = new EndpointAnnotation(
            ProtocolType.Tcp, name: OpenBaoDevelopmentProfile.EndpointName, targetPort: OpenBaoDevelopmentProfile.ContainerPort);
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "127.0.0.1", port);
        openBao.Annotations.Add(endpoint);

        await using DistributedApplication app = builder.Build();
        await app.ResourceNotifications.PublishUpdateAsync(
            openBao,
            static snapshot => snapshot with { State = new ResourceStateSnapshot(KnownResourceStates.Running, Style: null) });
        var startupException = new TimeoutException("owned OpenBao initialization failed");
        string? capturedBeforeCleanup = null;

        TimeoutException thrown = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterInitializationFailureAsync(
                startupException,
                app,
                EmbeddingProviderTestMode.OllamaOidcFake,
                [
                    new("topology", () =>
                    {
                        capturedBeforeCleanup =
                            startupException.Data[AspireIngestionPipelineFixture.OllamaStartupOpenBaoPortDataKey] as string;
                        socket.Dispose();
                        return Task.CompletedTask;
                    }),
                ]));

        thrown.ShouldBeSameAs(startupException);
        capturedBeforeCleanup.ShouldNotBeNull();
        capturedBeforeCleanup.ShouldContain("openbao/http: state=Running, proxied=True, containerTargetPort=8200");
        capturedBeforeCleanup.ShouldContain($"hostPort=127.0.0.1:{port}, listener=ConnectionRefused");
        capturedBeforeCleanup.ShouldNotContain(syntheticSeed);
        capturedBeforeCleanup.ShouldNotContain("OPENBAO_TEST_SEED");
    }

    [Fact]
    public async Task FailedInitialization_WhenDiagnosticAttachmentThrows_PreservesOriginalAndRunsCleanup()
    {
        Exception startupException = Substitute.For<Exception>();
        startupException.Data.Returns(static _ => throw new NotSupportedException("exception data unavailable"));
        List<string> cleanupOrder = [];

        Exception? thrown = await Record.ExceptionAsync(() =>
            AspireIngestionPipelineFixture.RethrowAfterInitializationFailureAsync(
                startupException,
                app: null,
                EmbeddingProviderTestMode.OllamaOidcFake,
                [
                    new("topology", () => RecordSuccessfulCleanup("topology", cleanupOrder)),
                    new("environment", () => RecordSuccessfulCleanup("environment", cleanupOrder)),
                ]));

        thrown.ShouldBeSameAs(startupException);
        cleanupOrder.ShouldBe(["topology", "environment"]);
    }

    [Fact]
    public async Task RethrowAfterCleanupAsync_AllActionsSucceed_PreservesOriginalWithoutDiagnostics()
    {
        var startupException = new InvalidOperationException("startup failed");
        List<string> cleanupOrder = [];

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterCleanupAsync(
                startupException,
                [
                    new("first", () => RecordSuccessfulCleanup("first", cleanupOrder)),
                    new("second", () => RecordSuccessfulCleanup("second", cleanupOrder)),
                ]));

        thrown.ShouldBeSameAs(startupException);
        cleanupOrder.ShouldBe(["first", "second"]);
        thrown.Data.Contains(AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey)
            .ShouldBeFalse();
    }

    [Fact]
    public async Task RethrowAfterCleanupAsync_NullAndMultipleFailures_PreservesOrderAndExistingDiagnostics()
    {
        var startupException = new InvalidOperationException("startup failed");
        startupException.Data[AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey] =
            "existing diagnostic";
        List<string> cleanupOrder = [];

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterCleanupAsync(
                startupException,
                [
                    new("first", () => RecordSuccessfulCleanup("first", cleanupOrder)),
                    new("missing", null),
                    new("third", () => RecordFailedCleanup("third", cleanupOrder, new IOException("third failed"))),
                    new("fourth", () => RecordSuccessfulCleanup("fourth", cleanupOrder)),
                    new("fifth", () => RecordFailedCleanup("fifth", cleanupOrder, new TimeoutException("fifth failed"))),
                    new("sixth", () => RecordSuccessfulCleanup("sixth", cleanupOrder)),
                ]));

        thrown.ShouldBeSameAs(startupException);
        cleanupOrder.ShouldBe(["first", "third", "fourth", "fifth", "sixth"]);
        thrown.Data[AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey]
            .ShouldBe("existing diagnostic");
        AggregateException cleanupFailures = thrown.Data[
            AspireIngestionPipelineFixture.FailedInitializationCleanupFailuresDataKey + ".2"]
            .ShouldBeOfType<AggregateException>();
        cleanupFailures.InnerExceptions.Count.ShouldBe(3);
        cleanupFailures.InnerExceptions[0].ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldContain("'missing'");
        cleanupFailures.InnerExceptions[1].ShouldBeOfType<IOException>()
            .Message.ShouldBe("third failed");
        cleanupFailures.InnerExceptions[2].ShouldBeOfType<TimeoutException>()
            .Message.ShouldBe("fifth failed");
    }

    [Fact]
    public async Task RethrowAfterCleanupCoreAsync_DiagnosticsSinkThrows_PreservesOriginalException()
    {
        var startupException = new InvalidOperationException("startup failed");

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.RethrowAfterCleanupCoreAsync(
                startupException,
                [
                    new("failed", () => Task.FromException(new IOException("cleanup failed"))),
                ],
                static (_, _) => throw new NotSupportedException("diagnostics unavailable")));

        thrown.ShouldBeSameAs(startupException);
        thrown.Message.ShouldBe("startup failed");
    }

    [Fact]
    public void CreateFailedInitializationCleanupActions_ContainsAllSixOwnedResourceSlotsInOrder()
    {
        var fixture = new AspireIngestionPipelineFixture();

        IReadOnlyList<KeyValuePair<string, Func<Task>?>> cleanupActions =
            fixture.CreateFailedInitializationCleanupActions();

        cleanupActions.Select(static slot => slot.Key).ShouldBe(
        [
            "topology",
            "environment-scopes",
            "temporary-dapr-config",
            "fixture-containers",
            "falkor-volume",
            "redis-volume",
        ]);
        cleanupActions.ShouldAllBe(static slot => slot.Value != null);
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_TransientStatusThenOk_Converges()
    {
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK]);
        int probeCount = 0;

        await AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
            _ =>
            {
                probeCount++;
                return Task.FromResult(statuses.Dequeue());
            },
            "clock-secret-readiness",
            [HttpStatusCode.OK],
            [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.Zero,
            CancellationToken.None);

        probeCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task WaitForEndpointProbeAsync_AuthorizationFailureThenOk_FailsClosedWithoutRetry(
        HttpStatusCode authorizationStatus)
    {
        int probeCount = 0;

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(probeCount++ == 0 ? authorizationStatus : HttpStatusCode.OK),
                "clock-secret-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                CancellationToken.None));

        probeCount.ShouldBe(1);
        exception.Message.ShouldContain($"{(int)authorizationStatus} {authorizationStatus}");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_CancelledBeforeProbe_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        int probeCount = 0;

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(probeCount++ == 0 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK),
                "clock-secret-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                cts.Token));

        probeCount.ShouldBe(0);
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_TransientStatusUntilDeadline_FailsWithSafeStatus()
    {
        TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(HttpStatusCode.ServiceUnavailable),
                "clock-secret-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
                TimeSpan.FromMilliseconds(25),
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(1),
                CancellationToken.None));

        exception.Message.ShouldContain("ServiceUnavailable");
        exception.Message.ShouldNotContain("response body");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_HangingProbe_IsBoundedByOverallDeadline()
    {
        var stopwatch = Stopwatch.StartNew();

        _ = await Should.ThrowAsync<TimeoutException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => new TaskCompletionSource<HttpStatusCode>(
                    TaskCreationOptions.RunContinuationsAsynchronously).Task,
                "bounded-readiness",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized],
                TimeSpan.FromMilliseconds(75),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5),
                CancellationToken.None));

        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_PerProbeTimeoutThenOk_Recovers()
    {
        int probeCount = 0;

        await AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
            _ =>
            {
                if (Interlocked.Increment(ref probeCount) == 1)
                {
                    return new TaskCompletionSource<HttpStatusCode>(
                        TaskCreationOptions.RunContinuationsAsynchronously).Task;
                }

                return Task.FromResult(HttpStatusCode.OK);
            },
            "per-probe-timeout",
            [HttpStatusCode.OK],
            [HttpStatusCode.Unauthorized],
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(25),
            TimeSpan.Zero,
            CancellationToken.None);

        probeCount.ShouldBe(2);
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_UnexpectedException_PropagatesWithoutRetry()
    {
        int probeCount = 0;

        FormatException exception = await Should.ThrowAsync<FormatException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromException<HttpStatusCode>(
                    ++probeCount == 1
                        ? new FormatException("programming fault")
                        : new FormatException("unexpected retry")),
                "unexpected-exception",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                CancellationToken.None));

        probeCount.ShouldBe(1);
        exception.Message.ShouldBe("programming fault");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_OverlappingStatusSets_RejectsConfiguration()
    {
        int probeCount = 0;

        ArgumentException exception = await Should.ThrowAsync<ArgumentException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ => Task.FromResult(++probeCount == 1 ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable),
                "overlapping-statuses",
                [HttpStatusCode.OK],
                [HttpStatusCode.OK, HttpStatusCode.Unauthorized],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                CancellationToken.None));

        probeCount.ShouldBe(0);
        exception.ParamName.ShouldBe("failClosedStatusCodes");
    }

    [Fact]
    public async Task WaitForEndpointProbeAsync_CallerCancellationDuringProbe_Propagates()
    {
        using var cts = new CancellationTokenSource();
        int probeCount = 0;

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            AspireIngestionPipelineFixture.WaitForEndpointProbeAsync(
                _ =>
                {
                    Interlocked.Increment(ref probeCount);
                    cts.Cancel();
                    return new TaskCompletionSource<HttpStatusCode>(
                        TaskCreationOptions.RunContinuationsAsynchronously).Task;
                },
                "caller-cancellation",
                [HttpStatusCode.OK],
                [HttpStatusCode.Unauthorized],
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                cts.Token));

        probeCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.MethodNotAllowed, true)]
    [InlineData(HttpStatusCode.NotAcceptable, true)]
    [InlineData(HttpStatusCode.UnsupportedMediaType, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, false)]
    public void DaprSecretReadinessStatus_PermanentAndTransientBoundaries_AreExplicit(
        HttpStatusCode statusCode,
        bool expectedPermanent)
    {
        AspireIngestionPipelineFixture.IsPermanentDaprSecretProbeStatus(statusCode)
            .ShouldBe(expectedPermanent);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Created, false)]
    public void McpDaprInvokeStatus_OnlyKnownAvailabilityFailuresAreTransient(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        AspireIngestionPipelineFixture.IsTransientMcpDaprInvokeStatus(statusCode)
            .ShouldBe(expectedTransient);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public void McpReadinessStatus_OnlyKnownAvailabilityFailuresAreTransient(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        AspireIngestionPipelineFixture.IsTransientMcpReadinessStatus(statusCode)
            .ShouldBe(expectedTransient);
    }

    [Fact]
    public void RedactSensitiveDiagnostics_TokenLikeSentinels_AreRemoved()
    {
        const string sentinel = "diagnostic-token-sentinel-43891";
        string diagnostic =
            $"Authorization: Bearer {sentinel}; token=\"{sentinel}\"; " +
            $"access_token={sentinel}; vault=hvs.{sentinel}; " +
            $"jwt=eyJhbGciOiJIUzI1NiJ9.{sentinel}.signature";

        string redacted = AspireIngestionPipelineFixture.RedactSensitiveDiagnostics(diagnostic);

        redacted.ShouldNotContain(sentinel);
        redacted.ShouldContain("[REDACTED]");
    }

    [Theory]
    [InlineData("memories")]
    [InlineData("Aspire.Hosting.Resources.memories-mcp")]
    [InlineData("memories-mcp-dapr-cli.stdout")]
    [InlineData("Aspire.Hosting.Resources.memories-mcp.stderr")]
    public void McpDiagnosticResourceCategory_SupportedShapes_AreRecognized(string category)
    {
        McpServerIntegrationTests.IsDiagnosticResourceCategory(category).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Aspire.Hosting.Resources.unrelated")]
    [InlineData("memories-mcp-extra")]
    public void McpDiagnosticResourceCategory_UnrelatedShapes_AreRejected(string category)
    {
        McpServerIntegrationTests.IsDiagnosticResourceCategory(category).ShouldBeFalse();
    }

    private static bool AsyncMethodCalls(MethodInfo asyncMethod, MethodInfo targetMethod)
    {
        Type stateMachineType = asyncMethod.GetCustomAttribute<AsyncStateMachineAttribute>()
            ?.StateMachineType
            ?? throw new InvalidOperationException($"Method '{asyncMethod.Name}' is not an async state machine.");
        MethodInfo moveNext = stateMachineType.GetMethod(
            "MoveNext",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Async state machine for '{asyncMethod.Name}' has no MoveNext method.");
        byte[] il = moveNext.GetMethodBody()?.GetILAsByteArray()
            ?? throw new InvalidOperationException($"Async state machine for '{asyncMethod.Name}' has no IL body.");

        for (int index = 0; index <= il.Length - 5; index++)
        {
            if (il[index] is not (0x28 or 0x6F))
            {
                continue;
            }

            if (BitConverter.ToInt32(il, index + 1) == targetMethod.MetadataToken)
            {
                return true;
            }
        }

        return false;
    }

    private static Task RecordSuccessfulCleanup(string name, ICollection<string> cleanupOrder)
    {
        cleanupOrder.Add(name);
        return Task.CompletedTask;
    }

    private static Task RecordFailedCleanup(
        string name,
        ICollection<string> cleanupOrder,
        Exception exception)
    {
        cleanupOrder.Add(name);
        return Task.FromException(exception);
    }

    private static MethodInfo GetMethod(string methodName)
        => typeof(OpenBaoTopologyIntegrationTests).GetMethod(methodName)
            ?? throw new InvalidOperationException($"OpenBao topology method '{methodName}' was not found.");

    private static IReadOnlyList<string> GetCategoryTraits(MemberInfo member)
        => member.CustomAttributes
            .Where(attribute => attribute.AttributeType == typeof(TraitAttribute))
            .Where(attribute =>
                attribute.ConstructorArguments.Count == 2 &&
                string.Equals(
                    attribute.ConstructorArguments[0].Value as string,
                    "Category",
                    StringComparison.Ordinal))
            .Select(attribute => attribute.ConstructorArguments[1].Value as string)
            .Where(value => value is not null)
            .Cast<string>()
            .ToArray();
}
