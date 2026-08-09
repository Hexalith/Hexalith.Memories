// <copyright file="DaprStateSidecarFixture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Dapr.Client;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

/// <summary>Tier-2 live Dapr sidecar + Redis <c>statestore</c> for EventStore Dapr-state store proofs
/// (spec-infrastructure-dependency-abstraction A4). Starts a Redis Stack container, writes a local
/// <c>statestore</c> component, and launches <c>daprd</c> from <c>~/.dapr/bin</c>.</summary>
public sealed class DaprStateSidecarFixture : IAsyncLifetime
{
    private const string RedisStackImage = "redis/redis-stack:latest@sha256:880df9c228597cb0d15b585f39a4327d6ee2d8b0d0f155e3f75dba9a761d4ec3";

    private IContainer? _redis;
    private Process? _daprd;
    private string? _componentsDir;
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();

    public string AppId { get; } = $"memories-ida-state-{Guid.NewGuid():N}"[..40];

    public string StateStoreName { get; } = "statestore";

    public string DaprHttpEndpoint { get; private set; } = string.Empty;

    public string DaprGrpcEndpoint { get; private set; } = string.Empty;

    public DaprClient DaprClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _redis = new ContainerBuilder(RedisStackImage)
            .WithPortBinding(0, 6379)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(6379)
                    .UntilCommandIsCompleted("redis-cli", "PING"))
            .Build();
        await _redis.StartAsync().ConfigureAwait(false);

        string redisHost = $"127.0.0.1:{_redis.GetMappedPublicPort(6379)}";
        _componentsDir = Path.Combine(Path.GetTempPath(), $"memories-ida-dapr-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(_componentsDir);
        await File.WriteAllTextAsync(
            Path.Combine(_componentsDir, "statestore.yaml"),
            $$"""
              apiVersion: dapr.io/v1alpha1
              kind: Component
              metadata:
                name: {{StateStoreName}}
              spec:
                type: state.redis
                version: v1
                metadata:
                  - name: redisHost
                    value: "{{redisHost}}"
                  - name: enableTLS
                    value: "false"
                  - name: actorStateStore
                    value: "false"
              """).ConfigureAwait(false);

        int httpPort = GetFreePort();
        int grpcPort = GetFreePort();
        int metricsPort = GetFreePort();
        DaprHttpEndpoint = $"http://127.0.0.1:{httpPort}";
        DaprGrpcEndpoint = $"http://127.0.0.1:{grpcPort}";

        string daprdPath = ResolveDaprdPath();
        _daprd = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = daprdPath,
                ArgumentList =
                {
                    "--app-id", AppId,
                    "--dapr-http-port", httpPort.ToString(),
                    "--dapr-grpc-port", grpcPort.ToString(),
                    "--metrics-port", metricsPort.ToString(),
                    "--resources-path", _componentsDir,
                    "--log-level", "info",
                    "--enable-app-health-check=false",
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };
        _daprd.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (_stdout)
                {
                    _ = _stdout.AppendLine(e.Data);
                }
            }
        };
        _daprd.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (_stderr)
                {
                    _ = _stderr.AppendLine(e.Data);
                }
            }
        };

        if (!_daprd.Start())
        {
            throw new InvalidOperationException($"Failed to start daprd at '{daprdPath}'.");
        }

        _daprd.BeginOutputReadLine();
        _daprd.BeginErrorReadLine();

        await WaitForSidecarHealthyAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);

        DaprClient = new DaprClientBuilder()
            .UseHttpEndpoint(DaprHttpEndpoint)
            .UseGrpcEndpoint(DaprGrpcEndpoint)
            .Build();

        // Probe the state store once so a misconfigured component fails fixture init, not the first test.
        await DaprClient.SaveStateAsync(StateStoreName, $"probe:{Guid.NewGuid():N}", "ok").ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        DaprClient?.Dispose();

        if (_daprd is not null)
        {
            try
            {
                if (!_daprd.HasExited)
                {
                    _daprd.Kill(entireProcessTree: true);
                    _ = _daprd.WaitForExit(5000);
                }
            }
            catch
            {
                // best-effort teardown
            }

            _daprd.Dispose();
            _daprd = null;
        }

        if (_redis is not null)
        {
            await _redis.DisposeAsync().ConfigureAwait(false);
            _redis = null;
        }

        if (_componentsDir is not null && Directory.Exists(_componentsDir))
        {
            try
            {
                Directory.Delete(_componentsDir, recursive: true);
            }
            catch
            {
                // best-effort
            }
        }
    }

    private async Task WaitForSidecarHealthyAsync(TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(2) };
        Exception? last = null;
        while (!cts.IsCancellationRequested)
        {
            if (_daprd?.HasExited == true)
            {
                throw new InvalidOperationException(
                    $"daprd exited with code {_daprd.ExitCode} before becoming healthy.\n" +
                    $"stdout:\n{Tail(_stdout)}\nstderr:\n{Tail(_stderr)}");
            }

            try
            {
                using HttpResponseMessage response = await http
                    .GetAsync($"{DaprHttpEndpoint}/v1.0/healthz/outbound", cts.Token)
                    .ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NoContent || response.IsSuccessStatusCode)
                {
                    return;
                }

                last = new InvalidOperationException($"healthz status {(int)response.StatusCode}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
            }

            await Task.Delay(250, cts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"daprd healthz did not become ready within {timeout}. Last error: {last}\n" +
            $"stdout:\n{Tail(_stdout)}\nstderr:\n{Tail(_stderr)}");
    }

    private static string ResolveDaprdPath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string candidate = Path.Combine(home, ".dapr", "bin", "daprd");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        throw new InvalidOperationException(
            $"daprd not found at '{candidate}'. Install the Dapr CLI runtime (dapr init) before running LiveSidecar suites.");
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string Tail(StringBuilder builder)
    {
        lock (builder)
        {
            string text = builder.ToString();
            return text.Length <= 4000 ? text : text[^4000..];
        }
    }
}

[CollectionDefinition("DaprStateSidecar", DisableParallelization = true)]
public sealed class DaprStateSidecarCollection : ICollectionFixture<DaprStateSidecarFixture>;
