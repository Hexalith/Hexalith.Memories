namespace Hexalith.Memories.Server.Tests.HealthChecks;

using Dapr.Client;

using Hexalith.Memories.Server.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Unit tests for DaprSidecarHealthCheck — validates health reporting
/// for the DAPR sidecar connectivity used by all workflow activities.
/// </summary>
public class DaprSidecarHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenSidecarHealthy_ShouldReturnHealthy()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        client.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        DaprSidecarHealthCheck healthCheck = new(client);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("responsive");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSidecarNotHealthy_ShouldReturnFailureStatus()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        client.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(false);

        DaprSidecarHealthCheck healthCheck = new(client);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("not responsive");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSidecarThrows_ShouldReturnFailureWithException()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        var expectedException = new HttpRequestException("Connection refused");
        client.CheckHealthAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        DaprSidecarHealthCheck healthCheck = new(client);
        HealthCheckContext context = CreateContext();

        // Act
        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("HttpRequestException");
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public void Constructor_NullDaprClient_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DaprSidecarHealthCheck(null!));
    }

    [Fact]
    public async Task CheckHealthAsync_NullContext_ShouldThrow()
    {
        // Arrange
        DaprClient client = Substitute.For<DaprClient>();
        DaprSidecarHealthCheck healthCheck = new(client);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => healthCheck.CheckHealthAsync(null!));
    }

    private static HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "dapr-sidecar",
                Substitute.For<IHealthCheck>(),
                HealthStatus.Unhealthy,
                tags: null),
        };
    }
}
