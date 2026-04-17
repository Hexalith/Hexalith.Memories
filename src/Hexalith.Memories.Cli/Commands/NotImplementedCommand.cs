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
/// Stubs are tracked in a conditional-weak reference table so NFR30 help-completeness tests (Story 7.4)
/// can exclude them from the "every command has an example" audit without leaking a tag into the public
/// help output.
/// </summary>
public static class NotImplementedCommand
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Command, object> StubRegistry = new();
    private static readonly object StubMarker = new();

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
        StubRegistry.Add(command, StubMarker);
        command.SetAction(parseResult =>
        {
            CliConsole console = services.GetRequiredService<CliConsole>();
            console.Error.WriteLine($"Not yet implemented — tracked in Story {storyId}.");
            return CliExitCodes.Plumbing;
        });
        return command;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="command"/> was built by <see cref="Create"/>.</summary>
    /// <param name="command">The command to inspect.</param>
    /// <returns>True when the command is a stub.</returns>
    public static bool IsStub(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return StubRegistry.TryGetValue(command, out _);
    }
}
