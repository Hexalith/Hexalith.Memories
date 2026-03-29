namespace Hexalith.Memories.IntegrationTests.Fixtures;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using StackExchange.Redis;

/// <summary>
/// Shared FalkorDB container fixture. One container per test collection — not per test.
/// Each test uses a unique graph name (tenant ID) for isolation within the shared container.
/// </summary>
public sealed class FalkorDbFixture : IAsyncLifetime
{
    private const string FalkorDbImage = "falkordb/falkordb:latest@sha256:4b7c79901ad409a39655f049b772adbc499b92ee2e01db80c3502572444df84d";

    private IContainer? _container;

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public string ConnectionString => $"localhost:{_container!.GetMappedPublicPort(6379)}";

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage(FalkorDbImage)
            .WithPortBinding(0, 6379) // FalkorDB listens on 6379 inside the container
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(6379)
                    .UntilCommandIsCompleted("redis-cli", "PING"))
            .Build();

        await _container.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
    }

    public async Task DisposeAsync()
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

[CollectionDefinition("FalkorDB")]
public class FalkorDbCollection : ICollectionFixture<FalkorDbFixture>;
