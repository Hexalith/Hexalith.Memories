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
    public async Task SearchQuery_TransportFailure_DoesNotLeakTokenInAnyFormat()
    {
        foreach (Hexalith.Memories.Cli.Output.OutputFormat format in Enum.GetValues<Hexalith.Memories.Cli.Output.OutputFormat>())
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            IServiceProvider services = BuildServices(
                stdout,
                stderr,
                TokenSentinel,
                endpoint: "http://127.0.0.1:65000/",
                verbose: true);
            CliConsole console = services.GetRequiredService<CliConsole>();
            console.Format = format;

            System.CommandLine.Command query = SearchQueryCommand.Build(services);
            int exit = await query.Parse(new[] { "query", "--tenant", "t1", "--query", "needle" }).InvokeAsync();

            string combined = stdout.ToString() + stderr.ToString();
            combined.ShouldNotContain(TokenSentinel);
            exit.ShouldBe(CliExitCodes.Plumbing, $"format={format}");
        }
    }

    [Fact]
    public async Task SearchInspect_TransportFailure_DoesNotLeakTokenInAnyFormat()
    {
        foreach (Hexalith.Memories.Cli.Output.OutputFormat format in Enum.GetValues<Hexalith.Memories.Cli.Output.OutputFormat>())
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            IServiceProvider services = BuildServices(
                stdout,
                stderr,
                TokenSentinel,
                endpoint: "http://127.0.0.1:65000/",
                verbose: true);
            CliConsole console = services.GetRequiredService<CliConsole>();
            console.Format = format;

            System.CommandLine.Command inspect = SearchInspectCommand.Build(services);
            int exit = await inspect
                .Parse(new[] { "inspect", "--tenant", "t1", "--case", "c1", "--id", "mu-1" })
                .InvokeAsync();

            string combined = stdout.ToString() + stderr.ToString();
            combined.ShouldNotContain(TokenSentinel);
            exit.ShouldBe(CliExitCodes.Plumbing, $"format={format}");
        }
    }

    [Fact]
    public async Task Executor_MemoriesRemoteException_ScrubsTokenFromBothMessageAndSuggestion()
    {
        // Story 7.3 Task 3.1: BOTH message AND suggestion flow through SanitizeText when sourced from
        // the server. Catalog suggestions are compile-time constants and bypass sanitization. Unknown
        // codes fall through to the server's suggestion, so both fields must be scrubbed symmetrically.
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceProvider services = BuildServices(stdout, stderr, TokenSentinel, verbose: false);
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        var error = new Hexalith.Memories.Contracts.V1.ErrorResponse(
            "UNKNOWN_FUTURE_CODE_XYZ",
            $"Server message leaked {TokenSentinel} somehow.",
            $"Server suggestion leaked {TokenSentinel} somehow.");

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new Hexalith.Memories.Client.Rest.MemoriesRemoteException(
                System.Net.HttpStatusCode.BadRequest,
                error),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.DomainError);
        string combined = stdout.ToString() + stderr.ToString();
        combined.ShouldNotContain(TokenSentinel);
        combined.ShouldContain("<redacted>");
    }

    [Fact]
    public async Task Executor_ServerErrorInJsonMode_DoesNotLeakTokenInEnvelope()
    {
        // ADR-7.3-002: JSON errors land on stdout. The envelope must not carry a token in any field.
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceProvider services = BuildServices(stdout, stderr, TokenSentinel, verbose: false);
        CliConsole console = services.GetRequiredService<CliConsole>();
        console.Format = Hexalith.Memories.Cli.Output.OutputFormat.Json;
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        var error = new Hexalith.Memories.Contracts.V1.ErrorResponse(
            "UNKNOWN_FUTURE_CODE_XYZ",
            $"Message {TokenSentinel}.",
            $"Suggestion {TokenSentinel}.");

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new Hexalith.Memories.Client.Rest.MemoriesRemoteException(
                System.Net.HttpStatusCode.BadRequest,
                error),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.DomainError);
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

        // Replace the primary HTTP handler so transport-failure tests don't perform real TCP
        // connects (previously ~2s per refused-connection attempt on Windows). The throw mirrors
        // what HttpClient surfaces for a refused endpoint, keeping the CLI's error-mapping path
        // under test.
        services.AddHttpClient<Hexalith.Memories.Client.Rest.MemoriesClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new ThrowingHandler());

        ServiceProvider provider = services.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri(endpoint);
        flag.ApiToken = token;
        return provider;
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => throw new HttpRequestException("Connection refused (test stub)");
    }
}
