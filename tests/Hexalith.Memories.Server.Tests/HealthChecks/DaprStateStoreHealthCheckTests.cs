namespace Hexalith.Memories.Server.Tests.HealthChecks;

using Dapr.Client;

using Hexalith.Memories.Server.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Unit tests for DaprStateStoreHealthCheck — validates health reporting
/// for the DAPR state store used by deduplication and actor state persistence.
/// </summary>
public class DaprStateStoreHealthCheckTests
{
    private const string StoreName = "statestore";

    [Fact]
    public async Task CheckHealthAsync_WhenStateStoreAccessible_ShouldReturnHealthy()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        client.GetStateAsync<byte[]?>(
                StoreName,
                "__health_probe__",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        DaprStateStoreHealthCheck healthCheck = new(client, StoreName);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain(StoreName);
        result.Description.ShouldContain("accessible");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStateStoreThrows_ShouldReturnFailureWithException()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        var expectedException = new Dapr.DaprException("State store unavailable");
        client.GetStateAsync<byte[]?>(
                StoreName,
                "__health_probe__",
                cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        DaprStateStoreHealthCheck healthCheck = new(client, StoreName);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("not accessible");
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public void Constructor_NullDaprClient_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DaprStateStoreHealthCheck(null!, StoreName));
    }

    [Fact]
    public void Constructor_NullStoreName_ShouldThrow()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DaprStateStoreHealthCheck(client, null!));
    }

    [Fact]
    public async Task CheckHealthAsync_NullContext_ShouldThrow()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        DaprStateStoreHealthCheck healthCheck = new(client, StoreName);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => healthCheck.CheckHealthAsync(null!));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimeoutOccurs_ShouldReturnFailure()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        client.GetStateAsync<byte[]?>(
                StoreName,
                "__health_probe__",
                cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        DaprStateStoreHealthCheck healthCheck = new(client, StoreName);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("TaskCanceledException");
    }

    private static HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "dapr-statestore",
                Substitute.For<IHealthCheck>(),
                HealthStatus.Unhealthy,
                tags: null),
        };
    }
}
