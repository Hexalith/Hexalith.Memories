// <copyright file="FalkorDbHealthCheckTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using Hexalith.Memories.Server.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.1 Task 2 — unit tests for <see cref="FalkorDbHealthCheck"/>. Verifies
/// the probe classifies GRAPH.LIST responses (healthy with graphs / empty instance
/// / connection refused / driver-level exception) into healthy vs. the registration
/// failure status.
/// </summary>
public class FalkorDbHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenGraphListReturnsGraphs_ShouldReturnHealthy()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        falkorDb.GetDatabase().Returns(db);

        RedisResult graphList = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("tenant-a")),
            RedisResult.Create(new RedisValue("tenant-b")),
            RedisResult.Create(new RedisValue("tenant-c")),
        ]);
        db.ExecuteAsync(Arg.Is("GRAPH.LIST")).Returns(graphList);

        FalkorDbHealthCheck check = new(falkorDb);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("3 graphs");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGraphListEmpty_ShouldReturnHealthy()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        falkorDb.GetDatabase().Returns(db);

        RedisResult empty = RedisResult.Create(Array.Empty<RedisResult>());
        db.ExecuteAsync(Arg.Is("GRAPH.LIST")).Returns(empty);

        FalkorDbHealthCheck check = new(falkorDb);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("0 graphs");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionRefused_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        falkorDb.GetDatabase().Returns(db);

        var expectedException = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "refused");
        db.ExecuteAsync(Arg.Is("GRAPH.LIST")).ThrowsAsync(expectedException);

        FalkorDbHealthCheck check = new(falkorDb);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("FalkorDB unreachable");
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServerException_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        falkorDb.GetDatabase().Returns(db);

        var expectedException = new RedisServerException("ERR internal graph failure");
        db.ExecuteAsync(Arg.Is("GRAPH.LIST")).ThrowsAsync(expectedException);

        FalkorDbHealthCheck check = new(falkorDb);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("server error");
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDriverThrows_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        falkorDb.GetDatabase().Returns(db);

        var expectedException = new InvalidOperationException("malformed FalkorDB response");
        db.ExecuteAsync(Arg.Is("GRAPH.LIST")).ThrowsAsync(expectedException);

        FalkorDbHealthCheck check = new(falkorDb);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("probe failed");
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGraphListReturnsNonArray_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        falkorDb.GetDatabase().Returns(db);

        db.ExecuteAsync(Arg.Is("GRAPH.LIST")).Returns(RedisResult.Create(new RedisValue("unexpected")));

        FalkorDbHealthCheck check = new(falkorDb);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("unexpected response type");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        falkorDb.GetDatabase().Returns(db);

        TaskCompletionSource<RedisResult> pending = new();
        db.ExecuteAsync(Arg.Is("GRAPH.LIST")).Returns(pending.Task);

        FalkorDbHealthCheck check = new(falkorDb);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext(), cts.Token);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("timed out");
        result.Exception.ShouldBeOfType<TaskCanceledException>();
    }

    [Fact]
    public async Task CheckHealthAsync_NullContext_ShouldThrow()
    {
        // Arrange
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        FalkorDbHealthCheck check = new(falkorDb);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => check.CheckHealthAsync(null!));
    }

    [Fact]
    public void Constructor_NullFalkorDb_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new FalkorDbHealthCheck(null!));
    }

    private static HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "falkordb",
                Substitute.For<IHealthCheck>(),
                HealthStatus.Degraded,
                tags: null),
        };
    }
}
