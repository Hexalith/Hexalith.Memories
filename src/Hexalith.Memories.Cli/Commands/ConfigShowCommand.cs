// <copyright file="ConfigShowCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builds <c>memories config show</c> — the diagnostic command that prints the resolved endpoint, source,
/// and whether a token is configured (AC #3c). Output format is frozen key=value lines on stdout.
/// </summary>
public static class ConfigShowCommand
{
    /// <summary>Builds the <c>show</c> subcommand.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var command = new Command("show", "Print the resolved endpoint, source, and whether a token is configured.");
        command.SetAction(parseResult =>
        {
            CliConsole console = services.GetRequiredService<CliConsole>();
            ResolvedConfigPipeline pipeline = services.GetRequiredService<ResolvedConfigPipeline>();

            try
            {
                ResolvedConfig resolved = pipeline.Resolve();
                string tokenConfigured = string.IsNullOrEmpty(resolved.ApiToken) ? "false" : "true";

                console.Out.WriteLine($"endpoint={EndpointDisplayFormatter.Format(resolved.Endpoint)}");
                console.Out.WriteLine($"resolvedBy={resolved.ResolvedBy}");
                console.Out.WriteLine($"tokenConfigured={tokenConfigured}");
                return CliExitCodes.Success;
            }
            catch (InvalidConfigurationException invalidConfig)
            {
                console.Error.WriteLine($"Invalid configuration: {invalidConfig.Message}");
                return CliExitCodes.Plumbing;
            }
        });
        return command;
    }
}
