namespace Hexalith.Memories.IntegrationTests.Fixtures;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using StackExchange.Redis;

/// <summary>
/// Composite fixture providing both FalkorDB and Redis Stack containers.
/// Both containers start in parallel via Task.WhenAll to halve startup time.
/// </summary>
public sealed class CompositeSearchFixture : IAsyncLifetime
{
    private const string FalkorDbImage = "falkordb/falkordb:latest@sha256:4b7c79901ad409a39655f049b772adbc499b92ee2e01db80c3502572444df84d";
    private const string RedisStackImage = "redis/redis-stack:latest@sha256:880df9c228597cb0d15b585f39a4327d6ee2d8b0d0f155e3f75dba9a761d4ec3";

    private IContainer? _falkorDbContainer;
    private IContainer? _redisStackContainer;

    public IConnectionMultiplexer FalkorDbConnection { get; private set; } = null!;

    public IConnectionMultiplexer RedisConnection { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        try
        {
            _falkorDbContainer = new ContainerBuilder(FalkorDbImage)
                .WithPortBinding(0, 6379)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilInternalTcpPortIsAvailable(6379)
                        .UntilCommandIsCompleted("redis-cli", "PING"))
                .Build();

            _redisStackContainer = new ContainerBuilder(RedisStackImage)
                .WithPortBinding(0, 6379)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilInternalTcpPortIsAvailable(6379)
                        .UntilCommandIsCompleted("redis-cli", "PING"))
                .Build();

            await Task.WhenAll(
                _falkorDbContainer.StartAsync(),
                _redisStackContainer.StartAsync());

            FalkorDbConnection = await ConnectionMultiplexer.ConnectAsync(
                $"localhost:{_falkorDbContainer.GetMappedPublicPort(6379)}");
            RedisConnection = await ConnectionMultiplexer.ConnectAsync(
                $"localhost:{_redisStackContainer.GetMappedPublicPort(6379)}");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (FalkorDbConnection is not null)
        {
            await FalkorDbConnection.CloseAsync();
            FalkorDbConnection.Dispose();
        }

        if (RedisConnection is not null)
        {
            await RedisConnection.CloseAsync();
            RedisConnection.Dispose();
        }

        if (_falkorDbContainer is not null)
        {
            await _falkorDbContainer.StopAsync();
            await _falkorDbContainer.DisposeAsync();
        }

        if (_redisStackContainer is not null)
        {
            await _redisStackContainer.StopAsync();
            await _redisStackContainer.DisposeAsync();
        }
    }
}

[CollectionDefinition("GraphSearch")]
public class GraphSearchCollection : ICollectionFixture<CompositeSearchFixture>;
