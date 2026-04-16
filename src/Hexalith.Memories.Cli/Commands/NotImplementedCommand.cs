// <copyright file="NotImplementedCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;

using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Helper that produces a placeholder command printing "Not yet implemented — tracked in Story 7.X" to
/// stderr and exiting with code <see cref="CliExitCodes.Plumbing"/>. Story 7.1 stubs most groups this way.
/// </summary>
public static class NotImplementedCommand
{
    /// <summary>Creates a stub command group with a single default action.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <param name="name">The command name.</param>
    /// <param name="description">The command description (shown in root help).</param>
    /// <param name="storyId">The owning story id (e.g., "7.2").</param>
    /// <returns>The configured stub command.</returns>
    public static Command Create(IServiceProvider services, string name, string description, string storyId)
    {
        ArgumentNullException.ThrowIfNull(services);

        var command = new Command(name, description);
        command.SetAction(parseResult =>
        {
            CliConsole console = services.GetRequiredService<CliConsole>();
            console.Error.WriteLine($"Not yet implemented — tracked in Story {storyId}.");
            return CliExitCodes.Plumbing;
        });
        return command;
    }
}
