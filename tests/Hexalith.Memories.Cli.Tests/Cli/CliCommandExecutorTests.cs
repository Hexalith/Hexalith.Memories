// <copyright file="CliCommandExecutorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Net.Sockets;
using System.Security.Authentication;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public class CliCommandExecutorTests
{
    private static readonly Uri LocalhostEndpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task ExecuteAsync_HttpRequestException_PrintsAppHostHintAndReturnsCode2()
    {
        // Story 7.3 AC #1: connection-failure message includes the AppHost recovery hint.
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = CreateExecutor(stdout, stderr, verbose: false);

        int exitCode = await executor.ExecuteAsync(
            (_, _) => throw new HttpRequestException(
                "connection refused",
                new SocketException((int)SocketError.ConnectionRefused)),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Cannot connect to Memories Server at http://127.0.0.1:5000/");
        stderr.ToString().ShouldContain("Is the service running?");
        stderr.ToString().ShouldContain("dotnet run --project Hexalith.Memories.AppHost");

        // AC #11: no stack trace on the default path.
        stderr.ToString().ShouldNotContain("   at ");
        stdout.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_SocketException_SameAppHostHintAndCode2()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = CreateExecutor(stdout, stderr, verbose: false);

        int exitCode = await executor.ExecuteAsync(
            (_, _) => throw new SocketException((int)SocketError.ConnectionRefused),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Cannot connect to Memories Server at http://127.0.0.1:5000/");
        stderr.ToString().ShouldContain("dotnet run --project Hexalith.Memories.AppHost");
    }

    [Fact]
    public async Task ExecuteAsync_TlsException_PrintsCertFailureMessage()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = CreateExecutor(stdout, stderr, verbose: false);

        int exitCode = await executor.ExecuteAsync(
            (_, _) => throw new AuthenticationException("bad cert"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("SSL certificate validation failed");
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutViaTaskCanceled_PrintsTimeoutMessage()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = CreateExecutor(stdout, stderr, verbose: false);

        int exitCode = await executor.ExecuteAsync(
            (_, _) => throw new TaskCanceledException("timed out"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("timed out after 30s");
    }

    [Fact]
    public async Task ExecuteAsync_UserCancellation_PrintsCancelledAndReturns130()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = CreateExecutor(stdout, stderr, verbose: false);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await executor.ExecuteAsync(
            (_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(0);
            },
            cts.Token);

        exitCode.ShouldBe(CliExitCodes.Cancelled);
        stderr.ToString().ShouldContain("Cancelled.");
    }

    [Fact]
    public async Task ExecuteAsync_UnexpectedException_MapsToPlumbingExitCodeNotDotNetDefault()
    {
        // Outermost catch: even arbitrary RuntimeException must land on exit 2, not .NET's default 1 (reserved for 7.3).
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = CreateExecutor(stdout, stderr, verbose: false);

        int exitCode = await executor.ExecuteAsync(
            (_, _) => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        exitCode.ShouldNotBe(CliExitCodes.DomainError);
    }

    [Fact]
    public async Task ExecuteAsync_VerboseMode_ScrubsTokenFromExceptionMessage()
    {
        // Task 10.4: even in verbose mode, configured token substring must not appear in output.
        const string tokenValue = "UNIQUE-TOKEN-SENTINEL-DO-NOT-LEAK";
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = CreateExecutor(stdout, stderr, verbose: true, token: tokenValue);

        int exitCode = await executor.ExecuteAsync(
            (_, _) => throw new InvalidOperationException($"request to https://user:{tokenValue}@host/ failed"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        string combined = stdout.ToString() + stderr.ToString();
        combined.ShouldNotContain(tokenValue);
        combined.ShouldContain("<redacted>");
    }

    [Fact]
    public async Task ExecuteAsync_InsecureTokenTransport_BlocksBeforeCallingHandler()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        // http + non-localhost + token → pipeline must refuse and the handler must NOT run.
        var pipeline = new ResolvedConfigPipeline(
        [
            new InlineSource(new Uri("http://remote.example.com/"), "abc"),
        ]);

        var console = new CliConsole { Out = stdout, Error = stderr, Verbose = false };
        var mutator = new MemoriesClientOptionsMutator();
        IOptionsMonitor<MemoriesClientOptions> monitor = Substitute.For<IOptionsMonitor<MemoriesClientOptions>>();
        monitor.CurrentValue.Returns(mutator.Options);
        var executor = new CliCommandExecutor(pipeline, monitor, mutator, console);

        bool handlerCalled = false;
        int exitCode = await executor.ExecuteAsync(
            (_, _) =>
            {
                handlerCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        handlerCalled.ShouldBeFalse();
        stderr.ToString().ShouldContain("Refusing to send API token over http://");
    }

    private static CliCommandExecutor CreateExecutor(
        TextWriter stdout,
        TextWriter stderr,
        bool verbose,
        string? token = null)
    {
        var pipeline = new ResolvedConfigPipeline([new InlineSource(LocalhostEndpoint, token)]);
        var console = new CliConsole { Out = stdout, Error = stderr, Verbose = verbose };
        var mutator = new MemoriesClientOptionsMutator();
        IOptionsMonitor<MemoriesClientOptions> monitor = Substitute.For<IOptionsMonitor<MemoriesClientOptions>>();
        monitor.CurrentValue.Returns(mutator.Options);
        return new CliCommandExecutor(pipeline, monitor, mutator, console);
    }

    private sealed class InlineSource : IConfigurationSource
    {
        private readonly Uri _endpoint;
        private readonly string? _token;

        public InlineSource(Uri endpoint, string? token)
        {
            _endpoint = endpoint;
            _token = token;
        }

        public string SourceName => "InlineSource";

        public bool TryResolve(out Uri? endpoint, out string? apiToken)
        {
            endpoint = _endpoint;
            apiToken = _token;
            return true;
        }
    }
}
