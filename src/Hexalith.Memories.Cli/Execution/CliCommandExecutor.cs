// <copyright file="CliCommandExecutor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

using System.Net.Sockets;
using System.Security.Authentication;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Options;

/// <summary>
/// Owner of every network-touching subcommand. Resolves endpoint/token once, applies the insecure-transport
/// guard, updates <see cref="MemoriesClientOptions"/> so <see cref="MemoriesAuthHandler"/> sees the live
/// values, and maps all transport exceptions to a single-line message + exit code 2 (AC #11).
/// </summary>
public sealed class CliCommandExecutor
{
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

    /// <summary>Executes a network-touching handler with unified endpoint resolution and exception mapping.</summary>
    /// <param name="handler">The handler body, receiving the resolved config.</param>
    /// <param name="ct">Cancellation token (driven by the CLI SIGINT wiring).</param>
    /// <returns>The process exit code.</returns>
    public async Task<int> ExecuteAsync(
        Func<ResolvedConfig, CancellationToken, Task<int>> handler,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(handler);

        ResolvedConfig config;
        try
        {
            config = _pipeline.Resolve();
        }
        catch (UriFormatException uriFormatException)
        {
            WriteLine(_console.Error, $"Configured endpoint is not a valid URI. Check the --endpoint flag, HEXALITH_MEMORIES_ENDPOINT, or config file.");
            WriteVerbose(config: null, exception: uriFormatException, token: null);
            return CliExitCodes.Plumbing;
        }
        catch (InvalidConfigurationException invalidConfiguration)
        {
            WriteLine(_console.Error, $"Invalid configuration: {invalidConfiguration.Message}");
            WriteVerbose(config: null, exception: invalidConfiguration, token: null);
            return CliExitCodes.Plumbing;
        }

        if (InsecureTokenTransportException.ShouldRefuse(config.Endpoint, config.ApiToken))
        {
            var refusal = new InsecureTokenTransportException(config.Endpoint);
            WriteLine(_console.Error, refusal.Message);
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
            WriteLine(_console.Error, "Cancelled.");
            return CliExitCodes.Cancelled;
        }
        catch (TaskCanceledException timeout) when (!ct.IsCancellationRequested)
        {
            WriteLine(_console.Error, $"Request to Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)} timed out after 30s.");
            WriteVerbose(config, timeout, config.ApiToken);
            return CliExitCodes.Plumbing;
        }
        catch (MemoriesRemoteException remote)
        {
            // 7.1 treats server-reported errors as plumbing too — Story 7.3 will split domain vs plumbing via exit code 1/2.
            WriteLine(
                _console.Error,
                $"Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)} returned an error: {remote.Error.Code} - {SanitizeText(remote.Error.Message, config, config.ApiToken)}");
            WriteVerbose(config, remote, config.ApiToken);
            return CliExitCodes.Plumbing;
        }
        catch (HttpRequestException httpException)
        {
            WriteLine(_console.Error, $"Cannot reach Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)}. Check that the service is running.");
            WriteVerbose(config, httpException, config.ApiToken);
            return CliExitCodes.Plumbing;
        }
        catch (SocketException socketException)
        {
            WriteLine(_console.Error, $"Cannot reach Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)}. Check that the service is running.");
            WriteVerbose(config, socketException, config.ApiToken);
            return CliExitCodes.Plumbing;
        }
        catch (AuthenticationException tlsException)
        {
            WriteLine(_console.Error, $"SSL certificate validation failed for {EndpointDisplayFormatter.Format(config.Endpoint)}. Check the certificate or the endpoint URL.");
            WriteVerbose(config, tlsException, config.ApiToken);
            return CliExitCodes.Plumbing;
        }
        catch (UriFormatException uriFormatException)
        {
            WriteLine(_console.Error, $"Configured endpoint '{EndpointDisplayFormatter.Format(config.Endpoint)}' is not a valid URI. Check the --endpoint flag, HEXALITH_MEMORIES_ENDPOINT, or config file.");
            WriteVerbose(config, uriFormatException, config.ApiToken);
            return CliExitCodes.Plumbing;
        }
        catch (Exception unexpected)
        {
            // Outermost landing zone: unknown failures default to plumbing, not the .NET exit-1 default.
            WriteLine(_console.Error, $"Unexpected error contacting Memories Server at {EndpointDisplayFormatter.Format(config.Endpoint)}: {unexpected.GetType().Name}.");
            WriteVerbose(config, unexpected, config.ApiToken);
            return CliExitCodes.Plumbing;
        }
    }

    /// <summary>Gets the live <see cref="MemoriesClientOptions"/> snapshot (for diagnostics).</summary>
    public MemoriesClientOptions CurrentOptions => _optionsMonitor.CurrentValue;

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

    /// <summary>Abstraction the executor uses to push resolved values into <see cref="MemoriesClientOptions"/>.</summary>
    public interface IOptionsMutator
    {
        /// <summary>Applies the resolved endpoint and token to the live options.</summary>
        /// <param name="endpoint">The resolved endpoint.</param>
        /// <param name="apiToken">The resolved API token.</param>
        void Apply(Uri endpoint, string? apiToken);
    }
}
