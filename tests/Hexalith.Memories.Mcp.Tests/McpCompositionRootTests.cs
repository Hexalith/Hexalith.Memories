// <copyright file="McpCompositionRootTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Mcp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Disables xUnit cross-collection parallelism for env-var-mutating tests.</summary>
[CollectionDefinition("EnvironmentVariableSerialized", DisableParallelization = true)]
public sealed class EnvironmentVariableSerializedCollection
{
}

/// <summary>
/// Tests for MCP service composition decisions that affect DAPR service invocation.
/// </summary>
[Collection("EnvironmentVariableSerialized")]
public sealed class McpCompositionRootTests
{
    [Fact]
    public void ResolveMemoriesServerAppId_FromConfiguration_WhenUnset_ReturnsDefault()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        McpCompositionRoot.ResolveMemoriesServerAppId(configuration)
            .ShouldBe(McpCompositionRoot.MemoriesServerAppId);
    }

    [Fact]
    public void ResolveMemoriesServerAppId_FromConfiguration_WhenConfigured_TrimsValue()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [McpCompositionRoot.MemoriesServerAppIdEnvVar] = "  memories-it-123  ",
            })
            .Build();

        McpCompositionRoot.ResolveMemoriesServerAppId(configuration).ShouldBe("memories-it-123");
    }

    [Fact]
    public void ResolveMemoriesServerAppId_FromEnvironmentVariablesProvider_SurfacesAppHostInjectedValue()
    {
        // review P7: hosts must include the environment-variables configuration provider for
        // AppHost-injected env vars to be visible through IConfiguration.
        using var scope = new EnvScope((McpCompositionRoot.MemoriesServerAppIdEnvVar, "  memories-env-456  "));
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        McpCompositionRoot.ResolveMemoriesServerAppId(configuration).ShouldBe("memories-env-456");
    }

    [Fact]
    public void ResolveMemoriesServerAppId_WithoutEnvironmentVariablesProvider_MissesProcessEnv()
    {
        using var scope = new EnvScope((McpCompositionRoot.MemoriesServerAppIdEnvVar, "memories-env-missed"));
        IConfiguration configuration = new ConfigurationBuilder().Build();

        McpCompositionRoot.ResolveMemoriesServerAppId(configuration)
            .ShouldBe(McpCompositionRoot.MemoriesServerAppId);
    }

    [Fact]
    public async Task StartAsync_InvalidProductionMcpAuthenticationOptions_FailsDuringStartupValidation()
    {
        using IHost host = Host.CreateDefaultBuilder()
            .UseEnvironment("Production")
            .ConfigureAppConfiguration(configuration =>
            {
                Dictionary<string, string?> settings = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Authentication:JwtBearer:Issuer"] = "issuer",
                    ["Authentication:JwtBearer:Audience"] = "audience",
                    ["Authentication:JwtBearer:Authority"] = "https://login.example.test",
                    ["Authentication:JwtBearer:SigningKey"] = "production-static-signing-key-32-bytes",
                };

                _ = configuration.AddInMemoryCollection(settings);
            })
            .ConfigureServices(services => McpCompositionRoot.ConfigureServices(services))
            .Build();

        OptionsValidationException exception = await Should.ThrowAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("Production");
        exception.Message.ShouldContain("SigningKey");
        exception.Message.ShouldNotContain("production-static-signing-key-32-bytes");
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous = new(StringComparer.Ordinal);

        public EnvScope(params (string Key, string? Value)[] values)
        {
            foreach ((string key, string? value) in values)
            {
                _previous[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach ((string key, string? prior) in _previous)
            {
                Environment.SetEnvironmentVariable(key, prior);
            }
        }
    }
}
