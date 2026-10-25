namespace Hexalith.Memories.IntegrationTests.Fixtures;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using StackExchange.Redis;

/// <summary>
/// Shared Redis Stack container fixture. One container per test collection — not per test.
/// Provides RediSearch (FT) and Redis Vector Search capabilities.
/// </summary>
public sealed class RedisStackFixture : IAsyncLifetime
{
    private const string RedisStackImage = "redis/redis-stack:latest@sha256:880df9c228597cb0d15b585f39a4327d6ee2d8b0d0f155e3f75dba9a761d4ec3";

    private IContainer? _container;
    private string? _nativeConnectionString;

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public string ConnectionString => _nativeConnectionString ?? $"localhost:{_container!.GetMappedPublicPort(6379)}";

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("MEMORIES_TEST_OWNED_NATIVE") == "1"
            && Environment.GetEnvironmentVariable("MEMORIES_TEST_REDISSTACK_CONNECTION") is { Length: > 0 } native)
        {
            var options = ConfigurationOptions.Parse(native);
            if (options.EndPoints.Count != 1 || options.EndPoints[0] is not System.Net.IPEndPoint { Address: var address, Port: > 1024 } || !address.Equals(System.Net.IPAddress.Loopback))
                throw new InvalidOperationException("Owned native test fixture requires one unprivileged loopback endpoint.");
            _nativeConnectionString = native;
            Connection = await ConnectionMultiplexer.ConnectAsync(native);
            return;
        }
        _container = new ContainerBuilder(RedisStackImage)
            .WithPortBinding(0, 6379) // Random host port → container 6379
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(6379)
                    .UntilCommandIsCompleted("redis-cli", "PING"))
            .Build();

        await _container.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
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

[CollectionDefinition("RedisStack")]
public class RedisStackCollection : ICollectionFixture<RedisStackFixture>;
