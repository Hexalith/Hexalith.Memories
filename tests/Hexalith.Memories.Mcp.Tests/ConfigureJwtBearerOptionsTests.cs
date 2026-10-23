// <copyright file="ConfigureJwtBearerOptionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Mcp.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

public sealed class ConfigureJwtBearerOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("RS256")]
    [InlineData("ES256")]
    public void RegisteredPolicy_ReplacesExplicitAlgorithmsAndValidatesTokens(string? selected)
    {
        using IHost host = RegisteredHost(selected is null ? [] : new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:ValidAlgorithms:0"] = selected,
        });
        JwtBearerOptions bearer = host.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        string[] expected = selected is null ? ["HS256", "RS256", "ES256"] : [selected];
        bearer.TokenValidationParameters.ValidAlgorithms.ShouldBe(expected);
        using RSA rsa = RSA.Create(2048);
        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        SecurityKey[] keys = [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 64))), new RsaSecurityKey(rsa), new ECDsaSecurityKey(ec)];
        string[] algorithms = ["HS256", "RS256", "ES256"];
        for (int index = 0; index < algorithms.Length; index++)
        {
            var handler = new JwtSecurityTokenHandler();
            string token = handler.CreateEncodedJwt(new SecurityTokenDescriptor
            {
                Issuer = "issuer", Audience = "audience", Expires = DateTime.UtcNow.AddMinutes(5),
                Subject = new ClaimsIdentity([new Claim("sub", "test")]),
                SigningCredentials = new SigningCredentials(keys[index], algorithms[index]),
            });
            TokenValidationParameters policy = bearer.TokenValidationParameters.Clone();
            policy.IssuerSigningKey = null;
            policy.IssuerSigningKeys = keys;
            if (expected.Contains(algorithms[index], StringComparer.Ordinal))
            {
                handler.ValidateToken(token, policy, out _).Identity!.IsAuthenticated.ShouldBeTrue();
            }
            else
            {
                Should.Throw<SecurityTokenException>(() => handler.ValidateToken(token, policy, out _));
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("none")]
    [InlineData("invalid")]
    public async Task RegisteredPolicy_InvalidExplicitAlgorithmsFailStartup(string algorithm)
    {
        using IHost host = RegisteredHost(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:ValidAlgorithms:0"] = algorithm,
        });
        (await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken)))
            .Message.ShouldContain("ValidAlgorithms");
    }

    [Theory]
    [InlineData("RS256")]
    [InlineData("ES256")]
    public async Task RegisteredBearerPipelineRejectsHs256WhenOnlyAsymmetricAlgorithmIsConfigured(string selected)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "issuer", ["Authentication:JwtBearer:Audience"] = "audience",
            ["Authentication:JwtBearer:SigningKey"] = new string('x', 64),
            ["Authentication:JwtBearer:ValidAlgorithms:0"] = selected,
        });
        builder.WebHost.UseTestServer();
        McpCompositionRoot.ConfigureServices(builder.Services);
        using RSA rsa = RSA.Create(2048);
        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        SecurityKey asymmetricKey = selected == "RS256" ? new RsaSecurityKey(rsa) : new ECDsaSecurityKey(ec);
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            options.TokenValidationParameters.IssuerSigningKeys = [asymmetricKey]);
        await using WebApplication app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization();
        app.MapGet("/protected", () => "authorized").RequireAuthorization();
        await app.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = app.GetTestClient();
        foreach (bool asymmetric in new[] { false, true })
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "issuer", Audience = "audience", Expires = DateTime.UtcNow.AddMinutes(5),
                Subject = new ClaimsIdentity([new Claim("sub", "caller"), new Claim("tenant_id", "tenant-a")]),
                SigningCredentials = new SigningCredentials(asymmetric ? asymmetricKey : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 64))),
                    asymmetric ? selected : "HS256"),
            };
            using var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor));
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(asymmetric ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
        }
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void RegisteredAuthenticationOptionsRetainConfigurationReloadNotifications()
    {
        using IHost host = RegisteredHost(new Dictionary<string, string?> { ["Authentication:JwtBearer:ValidAlgorithms:0"] = "RS256" });
        var configuration = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        var monitor = host.Services.GetRequiredService<IOptionsMonitor<MemoriesMcpAuthenticationOptions>>();
        monitor.CurrentValue.ValidAlgorithms.ShouldBe(["RS256"]);
        int notifications = 0;
        using IDisposable? subscription = monitor.OnChange(_ => notifications++);
        configuration["Authentication:JwtBearer:ValidAlgorithms:0"] = "ES256";
        configuration.Reload();
        notifications.ShouldBeGreaterThan(0);
        monitor.CurrentValue.ValidAlgorithms.ShouldBe(["ES256"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("RS256")]
    public async Task RegisteredPolicy_ExplicitEmptyOrScalarListFailsStartup(string? value)
    {
        using IHost host = RegisteredHost(new Dictionary<string, string?> { ["Authentication:JwtBearer:ValidAlgorithms"] = value });
        (await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken)))
            .Message.ShouldContain("ValidAlgorithms");
    }

    private static IHost RegisteredHost(Dictionary<string, string?> algorithms)
        => Host.CreateDefaultBuilder().UseEnvironment("Test")
            .ConfigureAppConfiguration(configuration => configuration.Sources.Clear())
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:Issuer"] = "issuer",
                ["Authentication:JwtBearer:Audience"] = "audience",
                ["Authentication:JwtBearer:SigningKey"] = new string('x', 64),
            }.Concat(algorithms)))
            .ConfigureServices(services => McpCompositionRoot.ConfigureServices(services)).Build();

    [Fact]
    public void Configure_SetsStrictTokenValidationParameters()
    {
        var options = new JwtBearerOptions();
        var configure = new ConfigureJwtBearerOptions(
            Options.Create(new MemoriesMcpAuthenticationOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                SigningKey = new string('x', 32),
                ValidAlgorithms = ["HS256"],
            }),
            NullLoggerFactory.Instance);

        configure.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        TokenValidationParameters parameters = options.TokenValidationParameters;
        parameters.ValidateIssuer.ShouldBeTrue();
        parameters.ValidateAudience.ShouldBeTrue();
        parameters.ValidateIssuerSigningKey.ShouldBeTrue();
        parameters.ValidateLifetime.ShouldBeTrue();
        parameters.RequireExpirationTime.ShouldBeTrue();
        parameters.RequireSignedTokens.ShouldBeTrue();
        parameters.ValidIssuer.ShouldBe("issuer");
        parameters.ValidAudience.ShouldBe("audience");
        parameters.ValidAlgorithms.ShouldBe(["HS256"]);
        parameters.ClockSkew.ShouldBe(TimeSpan.FromMinutes(1));
        parameters.IssuerSigningKey.ShouldBeOfType<SymmetricSecurityKey>();
    }

    [Fact]
    public void Configure_OidcMode_SetsAuthorityAndHttpsMetadata()
    {
        var options = new JwtBearerOptions();
        var configure = new ConfigureJwtBearerOptions(
            Options.Create(new MemoriesMcpAuthenticationOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                Authority = "https://login.example.test",
                RequireHttpsMetadata = true,
            }),
            NullLoggerFactory.Instance);

        configure.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        options.Authority.ShouldBe("https://login.example.test");
        options.RequireHttpsMetadata.ShouldBeTrue();
    }
}
