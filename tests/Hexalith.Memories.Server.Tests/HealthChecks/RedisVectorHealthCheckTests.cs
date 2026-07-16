// <copyright file="RedisVectorHealthCheckTests.cs" company="ITANEO">
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
/// Story 8.1 Task 1 — unit tests for <see cref="RedisVectorHealthCheck"/>. Covers
/// success detection (search module present), absence detection (module missing
/// from MODULE LIST), lenient parser behavior on ambiguous responses, and
/// failure-status classification on connectivity / server failures.
/// </summary>
public class RedisVectorHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenSearchModulePresent_ShouldReturnHealthy()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        RedisResult moduleList = BuildModuleList(("search", "20811"), ("ReJSON", "20608"));
        db.ExecuteAsync(Arg.Is("MODULE"), Arg.Any<object[]>()).Returns(moduleList);

        RedisVectorHealthCheck check = new(redis);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenVectorModuleAbsent_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        RedisResult moduleList = BuildModuleList(("ReJSON", "20608"), ("timeseries", "10801"));
        db.ExecuteAsync(Arg.Is("MODULE"), Arg.Any<object[]>()).Returns(moduleList);

        RedisVectorHealthCheck check = new(redis);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("Vector module absent");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionRefused_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        var expectedException = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "refused");
        db.ExecuteAsync(Arg.Is("MODULE"), Arg.Any<object[]>()).ThrowsAsync(expectedException);

        RedisVectorHealthCheck check = new(redis);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("Redis Vector unreachable");
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServerLoading_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        var expectedException = new RedisServerException("LOADING Redis is loading the dataset in memory");
        db.ExecuteAsync(Arg.Is("MODULE"), Arg.Any<object[]>()).ThrowsAsync(expectedException);

        RedisVectorHealthCheck check = new(redis);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("temporarily unavailable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnknownCommand_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        var expectedException = new RedisServerException("ERR unknown command 'MODULE'");
        db.ExecuteAsync(Arg.Is("MODULE"), Arg.Any<object[]>()).ThrowsAsync(expectedException);

        RedisVectorHealthCheck check = new(redis);

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("module missing");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        TaskCompletionSource<RedisResult> pending = new();
        db.ExecuteAsync(Arg.Is("MODULE"), Arg.Any<object[]>()).Returns(pending.Task);

        RedisVectorHealthCheck check = new(redis);
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
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        RedisVectorHealthCheck check = new(redis);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => check.CheckHealthAsync(null!));
    }

    [Fact]
    public void Constructor_NullRedis_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RedisVectorHealthCheck(null!));
    }

    private static RedisResult BuildModuleList(params (string Name, string Version)[] modules)
    {
        RedisResult[] entries = modules.Select(m => RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("name")),
            RedisResult.Create(new RedisValue(m.Name)),
            RedisResult.Create(new RedisValue("ver")),
            RedisResult.Create(new RedisValue(m.Version)),
        ])).ToArray();

        return RedisResult.Create(entries);
    }

    private static HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "redis-vector",
                Substitute.For<IHealthCheck>(),
                HealthStatus.Degraded,
                tags: null),
        };
    }
}
