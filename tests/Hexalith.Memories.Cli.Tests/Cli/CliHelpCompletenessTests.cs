// <copyright file="CliHelpCompletenessTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Text.RegularExpressions;

using Hexalith.Memories.Cli.Commands;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Story 7.4 NFR30 audit — every wired command must have a non-empty description containing at
/// least one usage example (literal "Example" keyword and at least one line starting with four
/// spaces + "memories"). Stubs are filtered via <see cref="NotImplementedCommand.IsStub(Command)"/>.
/// </summary>
public sealed class CliHelpCompletenessTests
{
    private static readonly Regex FourSpaceInvocationPattern = new(
        @"^\s{4}memories\b",
        RegexOptions.Multiline);

    [Fact]
    public void EveryWiredCommand_HasAtLeastOneUsageExample()
    {
        using ServiceProvider services = CliServices.Build();
        CliGlobalOptions globalOptions = services.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(services, globalOptions);

        var auditableCommands = Flatten(root)
            .Where(command => !NotImplementedCommand.IsStub(command))
            .ToArray();

        var failures = new List<string>();
        foreach (Command command in auditableCommands)
        {
            string path = BuildPath(command);

            if (string.IsNullOrWhiteSpace(command.Description))
            {
                failures.Add($"Command '{path}' has an empty description.");
                continue;
            }

            if (!command.Description.Contains("Example", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"Command '{path}' description is missing an 'Example' keyword.");
            }

            if (!FourSpaceInvocationPattern.IsMatch(command.Description))
            {
                failures.Add($"Command '{path}' description has no four-space-indented 'memories ...' usage example. See TenantListCommand.cs:23-27 for the expected pattern.");
            }
        }

        failures.ShouldBeEmpty();
        auditableCommands.Length.ShouldBeGreaterThan(0);
    }

    private static IEnumerable<Command> Flatten(Command root)
    {
        yield return root;
        foreach (Command sub in root.Subcommands)
        {
            foreach (Command nested in Flatten(sub))
            {
                yield return nested;
            }
        }
    }

    private static string BuildPath(Command command)
    {
        var parts = new Stack<string>();
        Symbol? current = command;
        while (current is not null)
        {
            if (current is Command c && !string.IsNullOrEmpty(c.Name))
            {
                if (c is RootCommand)
                {
                    parts.Push("memories");
                }
                else
                {
                    parts.Push(c.Name);
                }
            }

            current = current.Parents.FirstOrDefault() as Symbol;
        }

        return string.Join(' ', parts);
    }
}
