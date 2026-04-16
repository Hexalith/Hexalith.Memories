// <copyright file="ConfigShowCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Json;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builds <c>memories config show</c>. Story 7.2 routes output through <see cref="OutputFormatterRouter"/>;
/// human format preserves the byte-for-byte key=value form from Story 7.1 (AC #1 / ADR-7.2-002).
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
            OutputFormatterRouter router = services.GetRequiredService<OutputFormatterRouter>();

            try
            {
                ResolvedConfig resolved = pipeline.Resolve();
                var data = new ConfigShowData(
                    Endpoint: EndpointDisplayFormatter.Format(resolved.Endpoint),
                    ResolvedBy: resolved.ResolvedBy,
                    TokenConfigured: !string.IsNullOrEmpty(resolved.ApiToken));

                router.Write(console.Format, data, console.Out);
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
