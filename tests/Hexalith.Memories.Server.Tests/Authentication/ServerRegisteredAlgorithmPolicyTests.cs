namespace Hexalith.Memories.Server.Tests.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

/// <summary>Exercises the composition root's effective named JWT options and signed-token policy.</summary>
public sealed class ServerRegisteredAlgorithmPolicyTests
{
    [Theory]
    [InlineData("RS256")]
    [InlineData("ES256")]
    public async Task RegisteredExplicitAlgorithmsAcceptOnlySelectedAsymmetricTokens(string algorithm)
    {
        using RSA rsa = RSA.Create(2048);
        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        SecurityKey selected = algorithm == "RS256" ? new RsaSecurityKey(rsa) : new ECDsaSecurityKey(ec);
        var symmetric = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("hexalith-memories-test-signing-key-32b"));
        using var factory = new EventStoreWebAppFactory();
        factory.ConfigurationOverrides["Authentication:JwtBearer:ValidAlgorithms:0"] = algorithm;
        factory.ConfigurationOverrides["Authentication:JwtBearer:Authority"] = "https://issuer.example/";
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = new OpenIdConnectConfiguration { Issuer = "hexalith-memories-test" };
                metadata.SigningKeys.Add(selected);
                metadata.SigningKeys.Add(symmetric);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            })));
        using HttpClient client = configured.CreateClient();
        JwtBearerOptions effective = configured.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        effective.TokenValidationParameters.ValidAlgorithms.ShouldBe([algorithm]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(selected, algorithm));
        (await client.GetAsync("/api/v1/handlers", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(symmetric, "HS256"));
        (await client.GetAsync("/api/v1/handlers", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void RegisteredAbsentAlgorithmsRetainHistoricalDefaults()
    {
        using var factory = new EventStoreWebAppFactory();
        factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters.ValidAlgorithms.ShouldBe(["HS256", "RS256", "ES256"]);
        // Retain the init-only setter required by already compiled programmatic consumers.
        typeof(MemoriesServerAuthenticationOptions).GetProperty(nameof(MemoriesServerAuthenticationOptions.ValidAlgorithms))!
            .SetMethod!.ReturnParameter.GetRequiredCustomModifiers().ShouldContain(typeof(System.Runtime.CompilerServices.IsExternalInit));
    }

    [Fact]
    public void RegisteredAuthenticationOptionsRetainConfigurationReloadNotifications()
    {
        using var factory = new EventStoreWebAppFactory();
        factory.ConfigurationOverrides["Authentication:JwtBearer:ValidAlgorithms:0"] = "RS256";
        IServiceProvider services = factory.Services;
        IOptionsMonitor<MemoriesServerAuthenticationOptions> monitor = services.GetRequiredService<IOptionsMonitor<MemoriesServerAuthenticationOptions>>();
        monitor.CurrentValue.ValidAlgorithms.ShouldBe(["RS256"]);
        IOptionsMonitor<JwtBearerOptions> bearer = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        bearer.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters.ValidAlgorithms.ShouldBe(["RS256"]);
        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        configuration["Authentication:JwtBearer:ValidAlgorithms:0"] = "ES256";
        ((IConfigurationRoot)configuration).Reload();
        monitor.CurrentValue.ValidAlgorithms.ShouldBe(["ES256"]);
        // The registered bearer configurator deliberately consumes cached IOptions.
        // Domain options reload; changing the effective authentication policy requires restart.
        bearer.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters.ValidAlgorithms.ShouldBe(["RS256"]);
    }

    [Fact]
    public void RegisteredProgrammaticAlgorithmsApplyWhenExplicitConfigurationIsAbsent()
    {
        using var factory = new EventStoreWebAppFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddTransient<IOptionsFactory<MemoriesServerAuthenticationOptions>, ProgrammaticServerAuthenticationOptionsFactory>();
            services.Configure<MemoriesServerAuthenticationOptions>(options =>
            {
                var algorithms = (IList<string>)options.ValidAlgorithms;
                algorithms.Clear();
                algorithms.Add("RS256");
            });
        }));
        configured.Services.GetRequiredService<IConfiguration>().GetSection("Authentication:JwtBearer:ValidAlgorithms").Exists().ShouldBeFalse();
        configured.Services.GetRequiredService<IOptionsMonitor<MemoriesServerAuthenticationOptions>>().CurrentValue.ValidAlgorithms.ShouldBe(["RS256"]);
        configured.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters.ValidAlgorithms.ShouldBe(["RS256"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RegisteredInvalidExplicitAlgorithmsFailValidation(string invalid)
    {
        using var factory = new EventStoreWebAppFactory();
        factory.ConfigurationOverrides["Authentication:JwtBearer:ValidAlgorithms:0"] = invalid;
        Should.Throw<OptionsValidationException>(() => factory.Services);
    }

    [Fact]
    public void RegisteredExplicitEmptyJsonAlgorithmArrayFailsValidation()
    {
        using var factory = new EventStoreWebAppFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes("{\"Authentication\":{\"JwtBearer\":{\"ValidAlgorithms\":[]}}}")))));
        Should.Throw<OptionsValidationException>(() => configured.Services);
    }

    private static string Token(SecurityKey key, string algorithm)
        => new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = "hexalith-memories-test", Audience = "hexalith-memories-server",
            Subject = new ClaimsIdentity([new Claim("sub", "synthetic-user"), new Claim("tenant_id", "tenant-a")]),
            Expires = DateTime.UtcNow.AddMinutes(5), SigningCredentials = new SigningCredentials(key, algorithm),
        });
}
