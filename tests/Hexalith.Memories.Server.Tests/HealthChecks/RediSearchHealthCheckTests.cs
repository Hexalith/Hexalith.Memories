// <copyright file="RediSearchHealthCheckTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using Hexalith.Memories.Server.HealthChecks;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.1 Task 1 — unit tests for <see cref="RediSearchHealthCheck"/>. Validates the
/// probe classifies Redis responses (healthy FT._LIST array / LOADING / module-missing
/// / connection refused) into <see cref="HealthStatus.Healthy"/> vs. the registration
/// failure status (expected <see cref="HealthStatus.Degraded"/> in production).
/// </summary>
public class RediSearchHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenFtListReturnsIndexes_ShouldReturnHealthy()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        RedisResult list = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue(IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "syntactic"))),
            RedisResult.Create(new RedisValue(IndexSchemaDefinitions.BuildSyntacticKey("tenant-b", "syntactic"))),
        ]);
        db.ExecuteAsync(Arg.Is("FT._LIST")).Returns(list);

        RediSearchHealthCheck check = new(redis);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("2 indexes");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionRefused_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        var expectedException = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "refused");
        db.ExecuteAsync(Arg.Is("FT._LIST")).ThrowsAsync(expectedException);

        RediSearchHealthCheck check = new(redis);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("RediSearch unreachable");
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
        db.ExecuteAsync(Arg.Is("FT._LIST")).ThrowsAsync(expectedException);

        RediSearchHealthCheck check = new(redis);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("temporarily unavailable");
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenModuleMissing_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        var expectedException = new RedisServerException("ERR unknown command 'FT._LIST'");
        db.ExecuteAsync(Arg.Is("FT._LIST")).ThrowsAsync(expectedException);

        RediSearchHealthCheck check = new(redis);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("module missing");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenFtListReturnsNonArray_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        db.ExecuteAsync(Arg.Is("FT._LIST")).Returns(RedisResult.Create(new RedisValue("unexpected")));

        RediSearchHealthCheck check = new(redis);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("unexpected response type");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ShouldReturnFailureStatus()
    {
        // Arrange
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase db = Substitute.For<IDatabase>();
        redis.GetDatabase().Returns(db);

        TaskCompletionSource<RedisResult> pending = new();
        db.ExecuteAsync(Arg.Is("FT._LIST")).Returns(pending.Task);

        RediSearchHealthCheck check = new(redis);
        HealthCheckContext context = CreateContext();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        HealthCheckResult result = await check.CheckHealthAsync(context, cts.Token);

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
        RediSearchHealthCheck check = new(redis);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => check.CheckHealthAsync(null!));
    }

    [Fact]
    public void Constructor_NullRedis_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RediSearchHealthCheck(null!));
    }

    private static HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "redisearch",
                Substitute.For<IHealthCheck>(),
                HealthStatus.Degraded,
                tags: null),
        };
    }
}
