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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

using NSubstitute;

using Shouldly;

/// <summary>HTTP readiness checks for clock signing and source availability.</summary>
public sealed class ClockSigningReadinessHealthCheckTests
{
    [Fact]
    public async Task Ready_ValidSigningKeyAndThreeSources_ReturnsOk()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        await using var factory = new ClockFactory(KeyClient(Convert.ToBase64String(key.ExportPkcs8PrivateKey())), true,
            Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), "test-epoch");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage ready = await client.GetAsync("/ready", TestContext.Current.CancellationToken);
        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_ProductionConfiguredAuthenticatedHttpSources_ReturnsOk()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var sourceHandler = new ConfiguredUtcSourceHandler();
        await using var factory = new ClockFactory(
            KeyClient(Convert.ToBase64String(key.ExportPkcs8PrivateKey())),
            hasSources: false,
            Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
            "test-epoch",
            productionSourceHandler: sourceHandler);
        using HttpClient client = factory.CreateClient();

        factory.Services.GetRequiredService<IHostEnvironment>().IsProduction().ShouldBeTrue();
        IAuthenticatedUtcSource[] sources = factory.Services.GetServices<IAuthenticatedUtcSource>().ToArray();
        sources.Length.ShouldBe(3);
        sources.Select(static source => source.GetType())
            .ShouldAllBe(static type => type == typeof(HttpAuthenticatedUtcSource));
        using HttpResponseMessage ready = await client.GetAsync("/ready", TestContext.Current.CancellationToken);

        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
        sourceHandler.RequestedSourceIds.Order().ShouldBe(new[] { "clock-a", "clock-b", "clock-c" });
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
            hasSources,
            Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
            "test-epoch");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage ready = await client.GetAsync("/ready", TestContext.Current.CancellationToken);
        using HttpResponseMessage alive = await client.GetAsync("/alive", TestContext.Current.CancellationToken);
        ready.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        alive.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("missing-key")]
    [InlineData("wrong-key")]
    [InlineData("wrong-epoch")]
    [InlineData("invalid-signature")]
    [InlineData("stale-evidence")]
    public async Task Ready_UnverifiableEvidence_ReturnsUnavailableWhileAliveStaysOk(string failure)
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string? verificationKey = failure switch
        {
            "missing-key" => null,
            "wrong-key" => Convert.ToBase64String(otherKey.ExportSubjectPublicKeyInfo()),
            _ => Convert.ToBase64String(signingKey.ExportSubjectPublicKeyInfo()),
        };
        await using var factory = new ClockFactory(
            KeyClient(Convert.ToBase64String(signingKey.ExportPkcs8PrivateKey())),
            failure != "stale-evidence",
            verificationKey,
            failure == "wrong-epoch" ? "other-epoch" : "test-epoch",
            staleSources: failure == "stale-evidence",
            invalidSignature: failure == "invalid-signature");
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

    private sealed class ClockFactory(
        DaprClient daprClient,
        bool hasSources,
        string? verificationKey,
        string expectedEpoch,
        bool staleSources = false,
        bool invalidSignature = false,
        ConfiguredUtcSourceHandler? productionSourceHandler = null)
        : WebApplicationFactory<ClockAttestationService>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.UseEnvironment(productionSourceHandler is null ? "Development" : "Production");
            _ = builder.UseSetting("Clock:AllowDevelopmentSources", hasSources.ToString());
            _ = builder.UseSetting("Clock:SecretStoreName", "clock-test-secret-store");
            _ = builder.UseSetting("Clock:SigningKeySecretName", "clock-test-signing-key");
            _ = builder.UseSetting("Clock:SignerKeyEpoch", "test-epoch");
            if (productionSourceHandler is not null)
            {
                foreach ((int index, string sourceId) in new[] { "clock-a", "clock-b", "clock-c" }.Index())
                {
                    _ = builder.UseSetting($"Clock:Sources:{index}:Id", sourceId);
                    _ = builder.UseSetting($"Clock:Sources:{index}:Endpoint", $"https://{sourceId}.example.test/utc");
                    _ = builder.UseSetting($"Clock:Sources:{index}:AuthenticationToken", $"token-{sourceId}");
                }
            }

            if (verificationKey is not null)
            {
                _ = builder.UseSetting("AccessTelemetryLifecycle:AttestationVerificationKey", verificationKey);
            }

            _ = builder.UseSetting("AccessTelemetryLifecycle:ClockSignerKeyEpoch", expectedEpoch);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DaprClient>();
                services.AddSingleton(daprClient);
                if (productionSourceHandler is not null)
                {
                    // The readiness test exercises Production source registration without process-wide token mutation.
                    foreach (ServiceDescriptor validator in services.Where(static service =>
                        service.ServiceType == typeof(IHostedService) &&
                        service.ImplementationType?.Name == "DaprTokenStartupValidator").ToArray())
                    {
                        _ = services.Remove(validator);
                    }

                    services.Configure<HttpClientFactoryOptions>("authenticated-utc", options =>
                        options.HttpMessageHandlerBuilderActions.Add(handler => handler.PrimaryHandler = productionSourceHandler));
                }

                if (invalidSignature)
                {
                    services.RemoveAll<IClockAttestationSigner>();
                    services.AddSingleton<IClockAttestationSigner>(new InvalidSigner());
                }

                if (staleSources)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        services.AddSingleton<IAuthenticatedUtcSource>(new StaleSource($"stale-{index}"));
                    }
                }
            });
        }
    }

}
