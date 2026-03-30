// <copyright file="AspireIngestionPipelineFixture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Starts the full Aspire topology for end-to-end ingestion workflow tests.</summary>
public sealed class AspireIngestionPipelineFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IDistributedApplicationTestingBuilder? _builder;
    private string? _previousAspNetCoreEnvironment;
    private string? _previousDotNetEnvironment;
    private string? _previousFakeEmbedding;

    /// <summary>Gets the HTTP client for the Memories Server resource.</summary>
    public HttpClient MemoriesClient { get; private set; } = null!;

    /// <summary>Gets the Redis Stack connection for backend verification.</summary>
    public IConnectionMultiplexer RedisConnection { get; private set; } = null!;

    /// <summary>Gets the FalkorDB connection for backend verification.</summary>
    public IConnectionMultiplexer FalkorDbConnection { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _previousAspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        _previousDotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        _previousFakeEmbedding = Environment.GetEnvironmentVariable("Memories__Testing__UseFakeEmbedding");

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("Memories__Testing__UseFakeEmbedding", "true");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>()
            .ConfigureAwait(false);

        _ = _builder.Services.AddLogging(logging =>
        {
            _ = logging.SetMinimumLevel(LogLevel.Warning);
            _ = logging.AddFilter("Aspire.", LogLevel.Warning);
        });

        _app = await _builder.BuildAsync().ConfigureAwait(false);
        await _app.StartAsync(cts.Token).ConfigureAwait(false);

        _ = await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("memories-server", cts.Token)
            .WaitAsync(TimeSpan.FromMinutes(3), cts.Token)
            .ConfigureAwait(false);

        MemoriesClient = _app.CreateHttpClient("memories-server");
        MemoriesClient.Timeout = TimeSpan.FromSeconds(60);

        await WaitForEndpointAsync(
            MemoriesClient,
            "/health",
            [HttpStatusCode.OK],
            TimeSpan.FromMinutes(3),
            TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        Uri redisEndpoint = _app.GetEndpoint("redis", "redis");
        Uri falkorEndpoint = _app.GetEndpoint("falkordb", "falkordb");

        RedisConnection = await ConnectionMultiplexer.ConnectAsync(redisEndpoint.Authority).ConfigureAwait(false);
        FalkorDbConnection = await ConnectionMultiplexer.ConnectAsync(falkorEndpoint.Authority).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        MemoriesClient.Dispose();

        if (RedisConnection is not null)
        {
            await RedisConnection.CloseAsync().ConfigureAwait(false);
            RedisConnection.Dispose();
        }

        if (FalkorDbConnection is not null)
        {
            await FalkorDbConnection.CloseAsync().ConfigureAwait(false);
            FalkorDbConnection.Dispose();
        }

        if (_app is not null)
        {
            await _app.DisposeAsync().ConfigureAwait(false);
        }

        if (_builder is not null)
        {
            await _builder.DisposeAsync().ConfigureAwait(false);
        }

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _previousAspNetCoreEnvironment);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", _previousDotNetEnvironment);
        Environment.SetEnvironmentVariable("Memories__Testing__UseFakeEmbedding", _previousFakeEmbedding);
    }

    private static async Task WaitForEndpointAsync(
        HttpClient client,
        string url,
        IReadOnlyCollection<HttpStatusCode> expectedStatusCodes,
        TimeSpan timeout,
        TimeSpan pollInterval)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        Exception? lastException = null;
        HttpStatusCode? lastStatusCode = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                lastStatusCode = response.StatusCode;

                if (expectedStatusCodes.Contains(response.StatusCode))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(pollInterval).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Endpoint '{url}' did not become ready within {timeout}. " +
            $"Last status: {lastStatusCode?.ToString() ?? "n/a"}. " +
            $"Last error: {lastException?.Message ?? "n/a"}.");
    }
}

[CollectionDefinition("AspireIngestionPipeline", DisableParallelization = true)]
public sealed class AspireIngestionPipelineCollection : ICollectionFixture<AspireIngestionPipelineFixture>;