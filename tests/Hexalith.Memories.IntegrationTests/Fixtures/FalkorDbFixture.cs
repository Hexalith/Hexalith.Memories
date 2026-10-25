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
    private string? _nativeConnectionString;

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public string ConnectionString => _nativeConnectionString ?? $"localhost:{_container!.GetMappedPublicPort(6379)}";

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("MEMORIES_TEST_OWNED_NATIVE") == "1"
            && Environment.GetEnvironmentVariable("MEMORIES_TEST_FALKORDB_CONNECTION") is { Length: > 0 } native)
        {
            var options = ConfigurationOptions.Parse(native);
            if (options.EndPoints.Count != 1 || options.EndPoints[0] is not System.Net.IPEndPoint { Address: var address, Port: > 1024 } || !address.Equals(System.Net.IPAddress.Loopback))
                throw new InvalidOperationException("Owned native test fixture requires one unprivileged loopback endpoint.");
            _nativeConnectionString = native;
            Connection = await ConnectionMultiplexer.ConnectAsync(native);
            return;
        }
        _container = new ContainerBuilder(FalkorDbImage)
            .WithPortBinding(0, 6379) // FalkorDB listens on 6379 inside the container
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(6379)
                    .UntilCommandIsCompleted("redis-cli", "PING"))
            .Build();

        await _container.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
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

[CollectionDefinition("FalkorDB")]
public class FalkorDbCollection : ICollectionFixture<FalkorDbFixture>;
