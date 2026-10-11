namespace Hexalith.Memories.AccessTelemetry.Tests.Clock;

using System.Net;
using System.Security.Cryptography;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Clock;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

/// <summary>HTTP readiness checks for clock signing and source availability.</summary>
public sealed class ClockSigningReadinessHealthCheckTests
{
    [Fact]
    public async Task Ready_ValidSigningKeyAndThreeSources_ReturnsOk()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        await using var factory = new ClockFactory(KeyClient(Convert.ToBase64String(key.ExportPkcs8PrivateKey())), true);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage ready = await client.GetAsync("/ready", TestContext.Current.CancellationToken);
        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Ready_MissingSigningKeyOrSources_ReturnsUnavailableWhileAliveStaysOk(
        bool hasKey,
        bool hasSources)
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        await using var factory = new ClockFactory(
            KeyClient(hasKey ? Convert.ToBase64String(key.ExportPkcs8PrivateKey()) : null),
            hasSources);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage ready = await client.GetAsync("/ready", TestContext.Current.CancellationToken);
        using HttpResponseMessage alive = await client.GetAsync("/alive", TestContext.Current.CancellationToken);
        ready.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        alive.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static DaprClient KeyClient(string? encoded)
    {
        DaprClient client = Substitute.For<DaprClient>();
        client.GetSecretAsync(
                "clock-test-secret-store",
                "clock-test-signing-key",
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(encoded is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["signing-key-pkcs8"] = encoded });
        return client;
    }

    private sealed class ClockFactory(DaprClient daprClient, bool hasSources)
        : WebApplicationFactory<ClockAttestationService>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.UseEnvironment("Development");
            _ = builder.UseSetting("Clock:AllowDevelopmentSources", hasSources.ToString());
            _ = builder.UseSetting("Clock:SecretStoreName", "clock-test-secret-store");
            _ = builder.UseSetting("Clock:SigningKeySecretName", "clock-test-signing-key");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DaprClient>();
                services.AddSingleton(daprClient);
            });
        }
    }
}
