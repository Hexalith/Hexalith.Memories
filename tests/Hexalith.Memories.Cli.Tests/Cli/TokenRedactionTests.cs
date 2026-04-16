// <copyright file="TokenRedactionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Full-output containment test (Task 6.5 / anti-pattern #12): the token value must NEVER appear anywhere
/// in the combined stdout+stderr of any CLI command that touches the resolver, regardless of which source
/// contributed the token. Uses an obviously-distinct sentinel so future telemetry/verbose-mode additions
/// can't accidentally echo it through a different field.
/// </summary>
public class TokenRedactionTests
{
    private const string TokenSentinel = "UNIQUE-TOKEN-SENTINEL-DO-NOT-LEAK";
    private const string EndpointCredentialSentinel = "SUPERSECRET";

    [Fact]
    public void ConfigShow_WithTokenConfigured_DoesNotLeakTokenValue()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceProvider services = BuildServices(stdout, stderr, TokenSentinel);

        System.CommandLine.Command show = ConfigShowCommand.Build(services);
        int exitCode = show.Parse("show").Invoke();

        exitCode.ShouldBe(CliExitCodes.Success);
        string combined = stdout.ToString() + stderr.ToString();
        combined.ShouldNotContain(TokenSentinel);
        combined.ShouldContain("tokenConfigured=true");
    }

    [Fact]
    public void ConfigShow_WithCredentialedEndpoint_DoesNotLeakEndpointUserInfo()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceProvider services = BuildServices(
            stdout,
            stderr,
            token: string.Empty,
            endpoint: $"https://user:{EndpointCredentialSentinel}@example.com/");

        System.CommandLine.Command show = ConfigShowCommand.Build(services);
        int exitCode = show.Parse("show").Invoke();

        exitCode.ShouldBe(CliExitCodes.Success);
        string combined = stdout.ToString() + stderr.ToString();
        combined.ShouldNotContain(EndpointCredentialSentinel);
        combined.ShouldContain("endpoint=https://example.com/");
    }

    [Fact]
    public async Task Executor_UnhandledException_DoesNotLeakTokenInVerboseOutput()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceProvider services = BuildServices(stdout, stderr, TokenSentinel, verbose: true);
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();

        int exitCode = await executor.ExecuteAsync(
            (_, _) => throw new InvalidOperationException($"failed at https://user:{TokenSentinel}@host/"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        string combined = stdout.ToString() + stderr.ToString();
        combined.ShouldNotContain(TokenSentinel);
    }

    [Fact]
    public async Task Executor_UnhandledException_DoesNotLeakEndpointUserInfoInVerboseOutput()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceProvider services = BuildServices(
            stdout,
            stderr,
            token: TokenSentinel,
            endpoint: $"https://user:{EndpointCredentialSentinel}@example.com/",
            verbose: true);
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();

        int exitCode = await executor.ExecuteAsync(
            (config, _) => throw new InvalidOperationException($"failed at {config.Endpoint}"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        string combined = stdout.ToString() + stderr.ToString();
        combined.ShouldNotContain(EndpointCredentialSentinel);
        combined.ShouldContain("https://example.com/");
    }

    private static IServiceProvider BuildServices(
        StringWriter stdout,
        StringWriter stderr,
        string token,
        string endpoint = "https://ingress.example.com/",
        bool verbose = false)
    {
        IServiceCollection services = CliServices.BuildCollection();

        // Replace the console with our capture writers.
        services.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Verbose = verbose });

        ServiceProvider provider = services.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri(endpoint);
        flag.ApiToken = token;
        return provider;
    }
}
