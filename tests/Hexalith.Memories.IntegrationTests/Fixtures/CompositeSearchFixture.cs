namespace Hexalith.Memories.IntegrationTests.Fixtures;

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

    /// <summary>Host connection string for the Redis Stack container (Aspire-shaped <c>host:port</c>).</summary>
    public string RedisConnectionString { get; private set; } = string.Empty;

    /// <summary>Host connection string for the FalkorDB container (Aspire-shaped <c>host:port</c>).</summary>
    public string FalkorDbConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await Task.WhenAll(StartFalkorDbAsync(cancellationToken), StartRedisStackAsync(cancellationToken));
    }

    private async Task StartFalkorDbAsync(CancellationToken cancellationToken)
    {
        (_falkorDbContainer, FalkorDbConnection) = await RedisContainerStartup.StartAsync(FalkorDbImage, cancellationToken);
        FalkorDbConnectionString = RedisStackFixture.OwnedLoopbackConnectionString(_falkorDbContainer.GetMappedPublicPort(6379));
    }

    private async Task StartRedisStackAsync(CancellationToken cancellationToken)
    {
        (_redisStackContainer, RedisConnection) = await RedisContainerStartup.StartAsync(RedisStackImage, cancellationToken);
        RedisConnectionString = RedisStackFixture.OwnedLoopbackConnectionString(_redisStackContainer.GetMappedPublicPort(6379));
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
