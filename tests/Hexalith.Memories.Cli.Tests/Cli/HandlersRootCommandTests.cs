// <copyright file="HandlersRootCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Linq;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>Story 9.3 Risk #6 — guard tests that the <c>handlers</c> command group is a REAL command
/// (not a <see cref="NotImplementedCommand"/> stub) and that both the <c>list</c> and <c>mismatches</c>
/// subcommands are registered exactly once.</summary>
public sealed class HandlersRootCommandTests
{
    [Fact]
    public void RootCommand_ShouldHaveHandlersGroup_WithListAndMismatchesSubcommands()
    {
        IServiceCollection services = CliServices.BuildCollection();
        using ServiceProvider provider = services.BuildServiceProvider();

        CliGlobalOptions options = provider.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(provider, options);

        Command[] handlersGroups = root.Subcommands
            .Where(c => c.Name == "handlers")
            .ToArray();

        handlersGroups.Length.ShouldBe(1);

        Command handlers = handlersGroups[0];
        NotImplementedCommand.IsStub(handlers).ShouldBeFalse();

        string[] subcommandNames = handlers.Subcommands.Select(c => c.Name).OrderBy(n => n).ToArray();
        subcommandNames.ShouldBe(new[] { "list", "mismatches" });
    }

    [Fact]
    public void CommandGroups_ShouldNotIncludeHandlersStub()
    {
        // Risk #6 — CommandGroups (stubbed list) must no longer declare 'handlers' once 9.3 wires it real.
        RootCommandFactory.CommandGroups
            .Any(cg => cg.Name == "handlers")
            .ShouldBeFalse();
    }

    [Fact]
    public void RootCommand_HandlersCommand_IsNotDoubleRegistered()
    {
        // Guard: ensure only one 'handlers' subcommand is registered on the root.
        IServiceCollection services = CliServices.BuildCollection();
        using ServiceProvider provider = services.BuildServiceProvider();

        CliGlobalOptions options = provider.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(provider, options);

        int handlersCount = root.Subcommands.Count(c => c.Name == "handlers");
        handlersCount.ShouldBe(1);
    }
}
