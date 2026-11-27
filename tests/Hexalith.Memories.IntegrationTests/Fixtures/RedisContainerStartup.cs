// <copyright file="RedisContainerStartup.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using StackExchange.Redis;

/// <summary>Starts disposable Redis-compatible backends through their actual host port.</summary>
internal static class RedisContainerStartup
{
    /// <summary>Starts the container and retries once with a fresh allocation if startup times out.</summary>
    /// <param name="image">The pinned container image.</param>
    /// <param name="cancellationToken">Cancellation requested by the test runner.</param>
    /// <returns>The owned container and open connection.</returns>
    internal static async Task<(IContainer Container, ConnectionMultiplexer Connection)> StartAsync(
        string image,
        CancellationToken cancellationToken)
    {
        try
        {
            return await StartAttemptAsync();
        }
        catch (TimeoutException) when (!cancellationToken.IsCancellationRequested)
        {
            // Docker can publish a host port in its inspection data without making it reachable.
            // Replace the failed disposable container so the retry gets a new port.
            return await StartAttemptAsync();
        }

        async Task<(IContainer Container, ConnectionMultiplexer Connection)> StartAttemptAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();
            IContainer container = new ContainerBuilder(image).WithPortBinding(0, 6379).Build();
            try
            {
                ConnectionMultiplexer connection = await RedisStackFixture.StartOwnedContainerAsync(container, cancellationToken);
                return (container, connection);
            }
            catch (Exception startupFailure)
            {
                try
                {
                    await container.DisposeAsync();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException("Failed to discard a backend container after startup failed.", startupFailure, cleanupFailure);
                }

                throw;
            }
        }
    }
}
