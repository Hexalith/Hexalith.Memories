// <copyright file="CliCommandExecutor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

using System.Net.Sockets;
using System.Security.Authentication;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Errors;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Options;

/// <summary>
/// Owner of every network-touching subcommand. Resolves endpoint/token once, applies the insecure-transport
/// guard, updates <see cref="MemoriesClientOptions"/> so <see cref="MemoriesAuthHandler"/> sees the live
/// values, and maps every failure mode to the Story 7.3 actionable-error surface: the
/// <see cref="ErrorMessageCatalog"/> resolves server <c>ErrorResponse.Code</c> values to
/// <c>(message, suggestion, exitCode)</c> triples; transport exceptions get synthetic CLI codes
/// (ADR-7.3-001) and exit code <c>2</c>; domain errors exit <c>1</c>; cancellation exits <c>130</c>.
/// Per-format dispatch: human/table render a multi-line block on stderr; JSON emits a
/// <c>{ schemaVersion, command, error }</c> envelope on stdout (ADR-7.3-002).
/// </summary>
public sealed class CliCommandExecutor
{
    /// <summary>Command-name sentinel used when a failure fires before any handler resolved a real name.</summary>
    public const string RootCommandName = "memories";

    private readonly ResolvedConfigPipeline _pipeline;
    private readonly IOptionsMonitor<MemoriesClientOptions> _optionsMonitor;
    private readonly IOptionsMutator _mutator;
    private readonly CliConsole _console;

    /// <summary>Initializes a new instance of the <see cref="CliCommandExecutor"/> class.</summary>
    /// <param name="pipeline">The endpoint resolver pipeline.</param>
    /// <param name="optionsMonitor">The options monitor for diagnostics.</param>
    /// <param name="mutator">The options mutator that updates the live <see cref="MemoriesClientOptions"/>.</param>
    /// <param name="console">The CLI console abstraction.</param>
    public CliCommandExecutor(
        ResolvedConfigPipeline pipeline,
        IOptionsMonitor<MemoriesClientOptions> optionsMonitor,
        IOptionsMutator mutator,
        CliConsole console)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(mutator);
        ArgumentNullException.ThrowIfNull(console);

        _pipeline = pipeline;
        _optionsMonitor = optionsMonitor;
        _mutator = mutator;
        _console = console;
    }

    /// <summary>Gets the live <see cref="MemoriesClientOptions"/> snapshot (for diagnostics).</summary>
    public MemoriesClientOptions CurrentOptions => _optionsMonitor.CurrentValue;

    /// <summary>
    /// Executes a network-touching handler with unified endpoint resolution and exception mapping. Uses
    /// <see cref="RootCommandName"/> as the command identifier in JSON envelopes for pre-7.3 call sites
    /// that did not plumb a specific command name.
    /// </summary>
    /// <param name="handler">The handler body, receiving the resolved config.</param>
    /// <param name="ct">Cancellation token (driven by the CLI SIGINT wiring).</param>
    /// <returns>The process exit code.</returns>
    public Task<int> ExecuteAsync(
        Func<ResolvedConfig, CancellationToken, Task<int>> handler,
        CancellationToken ct)
        => ExecuteAsync(RootCommandName, handler, ct);

    /// <summary>
    /// Executes a network-touching handler with unified endpoint resolution and exception mapping.
    /// </summary>
    /// <param name="commandName">The invoked command name (e.g., <c>search query</c>). Used as the <c>command</c> value in JSON error envelopes.</param>
    /// <param name="handler">The handler body, receiving the resolved config.</param>
    /// <param name="ct">Cancellation token (driven by the CLI SIGINT wiring).</param>
    /// <returns>The process exit code.</returns>
    public async Task<int> ExecuteAsync(
        string commandName,
        Func<ResolvedConfig, CancellationToken, Task<int>> handler,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        ArgumentNullException.ThrowIfNull(handler);

        // Story 7.5 — wrap the invocation in a root CLI span. Null-safe when no listener attached.
        using System.Diagnostics.Activity? cliActivity = MemoriesActivitySource.Instance.StartActivity(MemoriesActivitySource.CliInvoke);
        cliActivity?.SetTag(MemoriesActivitySource.TagCommand, commandName);

        ResolvedConfig config;
        try
        {
            config = _pipeline.Resolve();
        }
        catch (UriFormatException uriFormatException)
        {
            EmitError(
                commandName,
                code: "INVALID_ENDPOINT",
                message: "Configured endpoint is not a valid URI. Check the --endpoint flag, HEXALITH_MEMORIES_ENDPOINT, or config file.",
                resolvedSuggestion: ErrorMessageCatalog.Resolve("INVALID_ENDPOINT").CliSuggestion!);
            WriteVerbose(config: null, exception: uriFormatException, token: null);
            return CliExitCodes.Plumbing;
        }
        catch (InvalidConfigurationException invalidConfiguration)
        {
            EmitError(
                commandName,
                code: "INVALID_CONFIG",
                message: $"Invalid configuration: {invalidConfiguration.Message}",
                resolvedSuggestion: "Fix the configuration values and retry.");
            WriteVerbose(config: null, exception: invalidConfiguration, token: null);
            return CliExitCodes.Plumbing;
        }

        if (InsecureTokenTransportException.ShouldRefuse(config.Endpoint, config.ApiToken))
        {
            var refusal = new InsecureTokenTransportException(config.Endpoint);
            EmitError(
                commandName,
                code: "INVALID_CONFIG",
                message: refusal.Message,
                resolvedSuggestion: "Use an https endpoint when supplying an API token, or bind to localhost for local development.");
            WriteVerbose(config, refusal, config.ApiToken);
            return CliExitCodes.Plumbing;
        }

        _mutator.Apply(config.Endpoint, config.ApiToken);

        try
        {
            return await handler(config, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Cancellation is user-initiated and exits 130. It is NOT routed through the error envelope:
            // nothing meaningful can be emitted on a cancelled stream, and JSON consumers can detect
            // cancellation via the exit code alone.
            WriteLine(_console.Error, "Cancelled.");
            return CliExitCodes.Cancelled;
        }
        catch (TaskCanceledException timeout) when (!ct.IsCancellationRequested)
        {
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            ErrorTranslation translation = ErrorMessageCatalog.Resolve("REQUEST_TIMEOUT");
            EmitError(
                commandName,
                code: "REQUEST_TIMEOUT",
                message: $"Request to Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)} timed out after 30s.",
                resolvedSuggestion: translation.CliSuggestion!);
            WriteVerbose(config, timeout, tokenAtSendTime);
            return translation.ExitCode;
        }
        catch (MemoriesRemoteException remote)
        {
            // Snapshot the token at catch entry: if any future IOptionsMutator rotates live options
            // mid-call (e.g., refresh-token flow), sanitization must still scrub the value that was on
            // the wire for THIS call — not the currently-live value.
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            ErrorTranslation translation = ErrorMessageCatalog.Resolve(remote.Error.Code);
            string message = translation.CliMessage
                ?? SanitizeText(remote.Error.Message, config, tokenAtSendTime);

            // Symmetric sanitization: both message AND suggestion flow through SanitizeText when sourced
            // from the server. Catalog-overridden suggestions are compile-time constants and bypass
            // sanitization (cannot contain runtime tokens).
            string suggestion = translation.CliSuggestion
                ?? SanitizeText(remote.Error.Suggestion, config, tokenAtSendTime);

            EmitError(
                commandName,
                code: remote.Error.Code,
                message: message,
                resolvedSuggestion: suggestion);
            WriteVerbose(config, remote, tokenAtSendTime);
            return translation.ExitCode;
        }
        catch (HttpRequestException httpException) when (IsConnectionRefused(httpException))
        {
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            ErrorTranslation translation = ErrorMessageCatalog.Resolve("CONNECTION_REFUSED");
            EmitError(
                commandName,
                code: "CONNECTION_REFUSED",
                message: $"Cannot connect to Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)}. Is the service running? Try: dotnet run --project Hexalith.Memories.AppHost",
                resolvedSuggestion: translation.CliSuggestion!);
            WriteVerbose(config, httpException, tokenAtSendTime);
            return translation.ExitCode;
        }
        catch (HttpRequestException httpException)
        {
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            EmitError(
                commandName,
                code: "UNEXPECTED_ERROR",
                message: $"HTTP request to Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)} failed before a response was received.",
                resolvedSuggestion: "Check DNS/network connectivity for the configured endpoint, or run with --verbose for diagnostic detail.");
            WriteVerbose(config, httpException, tokenAtSendTime);
            return CliExitCodes.Plumbing;
        }
        catch (SocketException socketException) when (IsConnectionRefused(socketException))
        {
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            ErrorTranslation translation = ErrorMessageCatalog.Resolve("CONNECTION_REFUSED");
            EmitError(
                commandName,
                code: "CONNECTION_REFUSED",
                message: $"Cannot connect to Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)}. Is the service running? Try: dotnet run --project Hexalith.Memories.AppHost",
                resolvedSuggestion: translation.CliSuggestion!);
            WriteVerbose(config, socketException, tokenAtSendTime);
            return translation.ExitCode;
        }
        catch (SocketException socketException)
        {
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            EmitError(
                commandName,
                code: "UNEXPECTED_ERROR",
                message: $"Network error contacting Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)}: {socketException.SocketErrorCode}.",
                resolvedSuggestion: "Check network connectivity for the configured endpoint, or run with --verbose for diagnostic detail.");
            WriteVerbose(config, socketException, tokenAtSendTime);
            return CliExitCodes.Plumbing;
        }
        catch (AuthenticationException tlsException)
        {
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            ErrorTranslation translation = ErrorMessageCatalog.Resolve("TLS_ERROR");
            EmitError(
                commandName,
                code: "TLS_ERROR",
                message: $"SSL certificate validation failed for {EndpointDisplayFormatter.Format(config.Endpoint)}. Check the certificate or the endpoint URL.",
                resolvedSuggestion: translation.CliSuggestion!);
            WriteVerbose(config, tlsException, tokenAtSendTime);
            return translation.ExitCode;
        }
        catch (UriFormatException uriFormatException)
        {
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            ErrorTranslation translation = ErrorMessageCatalog.Resolve("INVALID_ENDPOINT");
            EmitError(
                commandName,
                code: "INVALID_ENDPOINT",
                message: $"Configured endpoint '{EndpointDisplayFormatter.Format(config.Endpoint)}' is not a valid URI. Check the --endpoint flag, HEXALITH_MEMORIES_ENDPOINT, or config file.",
                resolvedSuggestion: translation.CliSuggestion!);
            WriteVerbose(config, uriFormatException, tokenAtSendTime);
            return translation.ExitCode;
        }
        catch (Exception unexpected)
        {
            // Outermost landing zone: unknown failures default to plumbing, not the .NET exit-1 default.
            string tokenAtSendTime = config.ApiToken ?? string.Empty;
            ErrorTranslation translation = ErrorMessageCatalog.Resolve("UNEXPECTED_ERROR");
            EmitError(
                commandName,
                code: "UNEXPECTED_ERROR",
                message: $"Unexpected error contacting Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)}: {unexpected.GetType().Name}.",
                resolvedSuggestion: translation.CliSuggestion!);
            WriteVerbose(config, unexpected, tokenAtSendTime);
            return translation.ExitCode;
        }
    }

    private void EmitError(string commandName, string code, string message, string resolvedSuggestion)
    {
        CliErrorWriter.Write(_console, commandName, code, message, resolvedSuggestion);
    }

    private void WriteVerbose(ResolvedConfig? config, Exception exception, string? token)
    {
        if (!_console.Verbose)
        {
            return;
        }

        string message = $"[{exception.GetType().Name}] {SanitizeText(exception.Message, config, token)}";

        _console.Error.WriteLine(message);
        if (config is not null)
        {
            _console.Error.WriteLine($"(endpoint={EndpointDisplayFormatter.Format(config.Endpoint)}, resolvedBy={config.ResolvedBy})");
        }
    }

    private static void WriteLine(TextWriter writer, string message) => writer.WriteLine(message);

    private static string SanitizeText(string message, ResolvedConfig? config, string? token)
    {
        ArgumentNullException.ThrowIfNull(message);

        string sanitized = message;
        if (!string.IsNullOrEmpty(token))
        {
            sanitized = sanitized.Replace(token, "<redacted>", StringComparison.Ordinal);
        }

        if (config is not null)
        {
            sanitized = sanitized.Replace(
                config.Endpoint.ToString(),
                EndpointDisplayFormatter.Format(config.Endpoint),
                StringComparison.Ordinal);
        }

        return sanitized;
    }

    private static bool IsConnectionRefused(HttpRequestException exception)
        => exception.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused };

    private static bool IsConnectionRefused(SocketException exception)
        => exception.SocketErrorCode == SocketError.ConnectionRefused;

    /// <summary>Abstraction the executor uses to push resolved values into <see cref="MemoriesClientOptions"/>.</summary>
    public interface IOptionsMutator
    {
        /// <summary>Applies the resolved endpoint and token to the live options.</summary>
        /// <param name="endpoint">The resolved endpoint.</param>
        /// <param name="apiToken">The resolved API token.</param>
        void Apply(Uri endpoint, string? apiToken);
    }
}
