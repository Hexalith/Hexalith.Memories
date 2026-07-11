// <copyright file="CliCommandExecutorErrorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 7.3 Task 7.3 — format-aware error surface. Asserts per-error-class translation, exit-code
/// classification, JSON envelope shape on stdout, and human/table multi-line stderr blocks.
/// </summary>
public sealed class CliCommandExecutorErrorTests
{
    private static readonly Uri LocalhostEndpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task ServerReportedTenantNotFound_Human_EmitsMultiLineStderrAndExitsDomain()
    {
        (CliCommandExecutor executor, StringWriter stdout, StringWriter stderr) = BuildExecutor(OutputFormat.Human);
        ErrorResponse error = new("TENANT_NOT_FOUND", "Tenant 'acme' does not exist.", "List available tenants with GET /api/v1/tenants");

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new MemoriesRemoteException(HttpStatusCode.NotFound, error),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.DomainError);
        stdout.ToString().ShouldBeEmpty();
        stderr.ToString().ShouldContain("Error: TENANT_NOT_FOUND");
        stderr.ToString().ShouldContain("Tenant 'acme' does not exist.");
        stderr.ToString().ShouldContain("Suggestion: Run 'memories tenant list'");
    }

    [Fact]
    public async Task ServerReportedTenantNotFound_Json_EmitsEnvelopeOnStdoutWithErrorSlot()
    {
        (CliCommandExecutor executor, StringWriter stdout, StringWriter stderr) = BuildExecutor(OutputFormat.Json);
        ErrorResponse error = new("TENANT_NOT_FOUND", "Tenant 'acme' does not exist.", "List available tenants");

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new MemoriesRemoteException(HttpStatusCode.NotFound, error),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.DomainError);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("command").GetString().ShouldBe("tenant list");
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse(
            "success envelope's data slot must be absent on error envelopes (WhenWritingNull suppression).");
        JsonElement err = doc.RootElement.GetProperty("error");
        err.GetProperty("code").GetString().ShouldBe("TENANT_NOT_FOUND");
        err.GetProperty("message").GetString()!.ShouldContain("Tenant 'acme' does not exist.");
        err.GetProperty("suggestion").GetString()!.ShouldContain("memories tenant list");
    }

    [Fact]
    public async Task ServerReportedBackendUnavailable_ExitsPlumbingNotDomain()
    {
        // Server-side plumbing codes come down the wire as MemoriesRemoteException but must still exit 2.
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);
        ErrorResponse error = new("BACKEND_UNAVAILABLE", "Backend down.", "Retry.");

        int exitCode = await executor.ExecuteAsync(
            "search query",
            (_, _) => throw new MemoriesRemoteException(HttpStatusCode.ServiceUnavailable, error),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: BACKEND_UNAVAILABLE");
    }

    [Fact]
    public async Task HttpRequestException_Human_EmitsAppHostHintExitPlumbing()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new HttpRequestException(
                "refused",
                new SocketException((int)SocketError.ConnectionRefused)),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: CONNECTION_REFUSED");
        stderr.ToString().ShouldContain("Cannot connect to Memories Server at http://127.0.0.1:5000/");
        stderr.ToString().ShouldContain("dotnet run --project Hexalith.Memories.AppHost");
    }

    [Fact]
    public async Task HttpRequestException_Json_EmitsEnvelopeWithSyntheticCode()
    {
        (CliCommandExecutor executor, StringWriter stdout, StringWriter stderr) = BuildExecutor(OutputFormat.Json);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new HttpRequestException(
                "refused",
                new SocketException((int)SocketError.ConnectionRefused)),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldBeEmpty();
        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("CONNECTION_REFUSED");
        doc.RootElement.GetProperty("error").GetProperty("message").GetString()!.ShouldContain("Cannot connect");
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task HttpRequestException_WithoutConnectionRefusal_DoesNotClaimLocalAppHostRecovery()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new HttpRequestException("dns failure"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: UNEXPECTED_ERROR");
        stderr.ToString().ShouldContain("failed before a response was received");
        stderr.ToString().ShouldNotContain("dotnet run --project Hexalith.Memories.AppHost");
    }

    [Fact]
    public async Task SocketException_MapsToConnectionRefused_Human()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new SocketException((int)SocketError.ConnectionRefused),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: CONNECTION_REFUSED");
        stderr.ToString().ShouldContain("dotnet run --project Hexalith.Memories.AppHost");
    }

    [Fact]
    public async Task TaskCanceledExceptionAsTimeout_MapsToRequestTimeout()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new TaskCanceledException("slow"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: REQUEST_TIMEOUT");
        stderr.ToString().ShouldContain("timed out after 30s");
    }

    [Fact]
    public async Task AuthenticationException_MapsToTlsError()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new AuthenticationException("bad cert"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: TLS_ERROR");
        stderr.ToString().ShouldContain("SSL certificate validation failed");
    }

    [Fact]
    public async Task UriFormatException_MapsToInvalidEndpoint()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new UriFormatException("bad"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: INVALID_ENDPOINT");
    }

    [Fact]
    public async Task OutermostException_MapsToUnexpectedErrorExitPlumbing()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("Error: UNEXPECTED_ERROR");
    }

    [Fact]
    public async Task Cancellation_WritesCancelledLineAndExitsOneThirty_NotRoutedThroughEnvelope()
    {
        // Cancellation is NOT routed through the error envelope (Task 3.7). Even in JSON mode, stderr
        // gets the "Cancelled." line; stdout stays empty.
        (CliCommandExecutor executor, StringWriter stdout, StringWriter stderr) = BuildExecutor(OutputFormat.Json);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(0);
            },
            cts.Token);

        exitCode.ShouldBe(CliExitCodes.Cancelled);
        stderr.ToString().ShouldContain("Cancelled.");
        stdout.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task UnknownServerCode_DefaultsToDomainExitOneWithVerboseHint_Human()
    {
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human);
        ErrorResponse error = new("FUTURE_CODE_NOT_IN_CATALOG", "the server said so", "the server suggested so");

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new MemoriesRemoteException(HttpStatusCode.BadRequest, error),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.DomainError);

        // Server's own message passes through when CliMessage is null.
        stderr.ToString().ShouldContain("the server said so");

        // Server's suggestion is overridden by the default "--verbose" hint for unknown codes.
        stderr.ToString().ShouldContain("Run with --verbose for diagnostic detail.");
    }

    [Fact]
    public async Task ServerReportedError_Human_SymmetricallySanitizesMessageAndSuggestion()
    {
        // Story 7.3 Task 3.1 security/hacker fix: BOTH message and suggestion are sanitized when sourced
        // from the server, so a token leaked through the server's Suggestion text is scrubbed on render.
        const string token = "SECRET-SENTINEL-TOKEN";
        (CliCommandExecutor executor, _, StringWriter stderr) = BuildExecutor(OutputFormat.Human, token: token);
        ErrorResponse error = new(
            "FUTURE_CODE_NOT_IN_CATALOG",
            $"message with token={token}",
            $"suggestion with token={token}");

        int exitCode = await executor.ExecuteAsync(
            "tenant list",
            (_, _) => throw new MemoriesRemoteException(HttpStatusCode.BadRequest, error),
            CancellationToken.None);

        exitCode.ShouldBe(CliExitCodes.DomainError);

        // The catalog entry for FUTURE_CODE_NOT_IN_CATALOG overrides the suggestion with a compile-time
        // constant, so the sanitization symmetry is best asserted on the message — plus we verify no
        // leaked token anywhere in the combined output.
        string combined = stderr.ToString();
        combined.ShouldNotContain(token);
        combined.ShouldContain("<redacted>");
    }

    private static (CliCommandExecutor Executor, StringWriter Stdout, StringWriter Stderr) BuildExecutor(
        OutputFormat format,
        string? token = null)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var pipeline = new ResolvedConfigPipeline([new InlineSource(LocalhostEndpoint, token)]);
        var console = new CliConsole { Out = stdout, Error = stderr, Verbose = false, Format = format };
        var mutator = new MemoriesClientOptionsMutator();
        IOptionsMonitor<MemoriesClientOptions> monitor = Substitute.For<IOptionsMonitor<MemoriesClientOptions>>();
        monitor.CurrentValue.Returns(mutator.Options);
        var executor = new CliCommandExecutor(pipeline, monitor, mutator, console);
        return (executor, stdout, stderr);
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
