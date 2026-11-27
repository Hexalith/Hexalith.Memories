namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Hexalith.Memories.AppHost;

using StackExchange.Redis;

/// <summary>
/// Shared Redis Stack container fixture. One container per test collection — not per test.
/// Provides RediSearch (FT) and Redis Vector Search capabilities.
/// </summary>
public sealed class RedisStackFixture : IAsyncLifetime
{
    internal const string RedisStackImage = "redis/redis-stack:latest@sha256:880df9c228597cb0d15b585f39a4327d6ee2d8b0d0f155e3f75dba9a761d4ec3";

    private const int RedisPort = 6379;
    private static readonly TimeSpan _handshakeTimeout = TimeSpan.FromSeconds(30);

    private IContainer? _container;
    private string? _nativeConnectionString;

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    /// <summary>Gets the immutable fixture-owned container identity when the container lane is selected.</summary>
    internal string? ContainerIdentity => _container?.Id;

    public string ConnectionString => _nativeConnectionString ?? OwnedLoopbackConnectionString(_container!.GetMappedPublicPort(RedisPort));

    internal static string OwnedLoopbackConnectionString(ushort port) => $"127.0.0.1:{port}";

    /// <summary>Starts an owned Redis container and connects through its published loopback port.</summary>
    /// <param name="container">The disposable Redis container owned by the caller.</param>
    /// <param name="cancellationToken">Cancellation requested by the test runner.</param>
    /// <param name="protocol">The RESP protocol to negotiate, if specified.</param>
    /// <param name="startupTimeout">The maximum startup budget, defaulting to two minutes.</param>
    /// <returns>An open multiplexer.</returns>
    internal static async Task<ConnectionMultiplexer> StartOwnedContainerAsync(
        IContainer container,
        CancellationToken cancellationToken,
        RedisProtocol? protocol = null,
        TimeSpan? startupTimeout = null)
    {
        TimeSpan limit = startupTimeout ?? TimeSpan.FromMinutes(2);
        Stopwatch elapsed = Stopwatch.StartNew();
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        startup.CancelAfter(limit);
        string phase = "Docker startup";
        string? endpoint = null;
        try
        {
            // Testcontainers already waits for Docker port allocation. Keep the network probe
            // in the Redis client so an unavailable port cannot enter Docker's exit-code wait.
            await container.StartAsync(startup.Token);
            phase = "mapped port lookup";
            endpoint = OwnedLoopbackConnectionString(container.GetMappedPublicPort(RedisPort));
            phase = "Redis handshake";
            TimeSpan remaining = limit - elapsed.Elapsed;
            return await ConnectWhenReadyAsync(
                endpoint,
                protocol,
                remaining < _handshakeTimeout ? remaining : _handshakeTimeout,
                startup.Token);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            TestcontainersStates state = container.State;
            string id = state is TestcontainersStates.Created or TestcontainersStates.Running
                or TestcontainersStates.Paused or TestcontainersStates.Exited ? container.Id : "unavailable";
            string message = $"Owned Redis failed during {phase} (container {id}, last Docker state {state}, endpoint {endpoint ?? "unavailable"}).";
            if (startup.IsCancellationRequested || ex is TimeoutException or RedisConnectionException)
            {
                throw new TimeoutException(message, ex);
            }

            throw new InvalidOperationException(message, ex);
        }
    }

    /// <summary>
    /// Connects once Redis accepts a client handshake. A mapped TCP port is not enough: redis-stack
    /// can listen before AUTH/PING succeed, and a single aborted ConnectAsync fails the collection.
    /// </summary>
    /// <param name="connectionString">The loopback Redis endpoint.</param>
    /// <param name="protocol">Optional RESP protocol. Leave unset to use the client default.</param>
    /// <param name="timeout">How long to retry the handshake. Defaults to 30 seconds.</param>
    /// <param name="cancellationToken">Cancellation requested by the caller.</param>
    /// <returns>An open multiplexer.</returns>
    internal static async Task<ConnectionMultiplexer> ConnectWhenReadyAsync(
        string connectionString,
        RedisProtocol? protocol = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        TimeSpan limit = timeout ?? _handshakeTimeout;
        Stopwatch elapsed = Stopwatch.StartNew();
        int failedAttempts = 0;
        RedisConnectionException? last = null;
        TimeSpan remaining = limit;
        while (remaining > TimeSpan.Zero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConfigurationOptions options = ConfigurationOptions.Parse(connectionString);
            if (protocol is { } selected)
            {
                options.Protocol = selected;
            }

            options.AbortOnConnectFail = true;
            options.ConnectRetry = 1;
            options.ConnectTimeout = (int)Math.Clamp(remaining.TotalMilliseconds, 1, 2000);
            try
            {
                ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(options);
                if (cancellationToken.IsCancellationRequested)
                {
                    connection.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return connection;
            }
            catch (RedisConnectionException ex)
            {
                last = ex;
                failedAttempts++;
                remaining = limit - elapsed.Elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                await Task.Delay(RedisReadinessBackoff.GetDelay(
                    failedAttempts,
                    Random.Shared.NextDouble(),
                    remaining), cancellationToken);
                remaining = limit - elapsed.Elapsed;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw last ?? new RedisConnectionException(
            ConnectionFailureType.UnableToConnect,
            CommandFlags.None,
            "Redis did not accept a client handshake before the readiness deadline.");
    }

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("MEMORIES_TEST_OWNED_NATIVE") == "1"
            && Environment.GetEnvironmentVariable("MEMORIES_TEST_REDISSTACK_CONNECTION") is { Length: > 0 } native)
        {
            var options = ConfigurationOptions.Parse(native);
            if (options.EndPoints.Count != 1 || options.EndPoints[0] is not System.Net.IPEndPoint { Address: var address, Port: > 1024 } || !address.Equals(System.Net.IPAddress.Loopback))
                throw new InvalidOperationException("Owned native test fixture requires one unprivileged loopback endpoint.");
            _nativeConnectionString = native;
            Connection = await ConnectWhenReadyAsync(native);
            return;
        }

        _container = new ContainerBuilder(RedisStackImage)
            .WithPortBinding(0, RedisPort)
            .Build();

        Connection = await StartOwnedContainerAsync(_container, TestContext.Current.CancellationToken);
        // The Redis client handshake and CONFIG call verify the service after the port is mapped.
        // This fixture owns its container. Legacy completeness requires errors on timeout,
        // because RETURN can otherwise report a self-consistent partial result set.
        await Connection.GetDatabase().ExecuteAsync("FT.CONFIG", "SET", "ON_TIMEOUT", "FAIL");
    }

    public async ValueTask DisposeAsync()
    {
        if (Connection is not null)
        {
            await Connection.CloseAsync();
            Connection.Dispose();
        }

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
