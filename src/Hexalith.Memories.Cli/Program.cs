// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli;

using System.CommandLine;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;

using Microsoft.Extensions.DependencyInjection;

/// <summary>CLI entry point. Plain console tool — no <c>WebApplication.CreateBuilder</c>, no DAPR sidecar at startup (AC #9).</summary>
public static class Program
{
    /// <summary>Program entry point.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            args = ["--help"];
        }

        using ServiceProvider services = CliServices.Build();

        CliGlobalOptions globalOptions = services.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(services, globalOptions);

        // Parse once so we can pre-populate flag source before any subcommand handler executes.
        ParseResult parseResult = root.Parse(args);

        try
        {
            RootCommandFactory.ApplyGlobalOptions(services, parseResult, globalOptions);
        }
        catch (Configuration.InvalidConfigurationException invalidConfiguration)
        {
            return WriteInvalidConfigurationError(services, parseResult, globalOptions, invalidConfiguration.Message);
        }

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            try
            {
                return await parseResult.InvokeAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Console.Error.WriteLine("Cancelled.");
                return CliExitCodes.Cancelled;
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    internal static int WriteInvalidConfigurationError(
        IServiceProvider services,
        ParseResult parseResult,
        CliGlobalOptions globalOptions,
        string message)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(globalOptions);
        ArgumentNullException.ThrowIfNull(message);

        CliConsole console = services.GetRequiredService<CliConsole>();
        if (string.Equals(parseResult.GetValue(globalOptions.FormatOption), "json", StringComparison.OrdinalIgnoreCase))
        {
            console.Format = OutputFormat.Json;
        }

        CliErrorWriter.Write(
            console,
            CliCommandExecutor.RootCommandName,
            code: "INVALID_CONFIG",
            message: $"Invalid configuration: {message}",
            suggestion: "Fix the configuration values and retry.");
        return CliExitCodes.Plumbing;
    }
}
