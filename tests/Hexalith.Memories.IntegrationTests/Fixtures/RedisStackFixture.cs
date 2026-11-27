namespace Hexalith.Memories.IntegrationTests.Fixtures;

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

    private IContainer? _container;
    private string? _nativeConnectionString;

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    /// <summary>Gets the immutable fixture-owned container identity when the container lane is selected.</summary>
    internal string? ContainerIdentity => _container?.Id;

    public string ConnectionString => _nativeConnectionString ?? OwnedLoopbackConnectionString(_container!.GetMappedPublicPort(6379));

    internal static string OwnedLoopbackConnectionString(ushort port) => $"127.0.0.1:{port}";

    /// <summary>
    /// Connects once Redis accepts a client handshake. A mapped TCP port is not enough: redis-stack
    /// can listen before AUTH/PING succeed, and a single aborted ConnectAsync fails the collection.
    /// </summary>
    /// <param name="connectionString">The loopback Redis endpoint.</param>
    /// <param name="protocol">Optional RESP protocol. Leave unset to use the client default.</param>
    /// <param name="timeout">How long to retry the handshake. Defaults to 30 seconds.</param>
    /// <returns>An open multiplexer.</returns>
    internal static async Task<ConnectionMultiplexer> ConnectWhenReadyAsync(
        string connectionString,
        RedisProtocol? protocol = null,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(30);
        DateTimeOffset deadline = DateTimeOffset.UtcNow + limit;
        int failedAttempts = 0;
        RedisConnectionException? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            ConfigurationOptions options = ConfigurationOptions.Parse(connectionString);
            if (protocol is { } selected)
            {
                options.Protocol = selected;
            }

            options.AbortOnConnectFail = true;
            options.ConnectRetry = 1;
            options.ConnectTimeout = 2000;
            try
            {
                return await ConnectionMultiplexer.ConnectAsync(options);
            }
            catch (RedisConnectionException ex)
            {
                last = ex;
                failedAttempts++;
                TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                await Task.Delay(RedisReadinessBackoff.GetDelay(
                    failedAttempts,
                    Random.Shared.NextDouble(),
                    remaining));
            }
        }

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
            .WithPortBinding(0, 6379) // Random host port → container 6379
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(6379))
            .Build();

        await _container.StartAsync();

        Connection = await ConnectWhenReadyAsync(ConnectionString);
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
