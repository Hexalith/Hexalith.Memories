// <copyright file="ProgramHelpTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Threading;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

public sealed class ProgramHelpTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Fact]
    public async Task Main_WithNoArgs_ReturnsSuccessAndPrintsHelpSurface()
    {
        await ConsoleGate.WaitAsync();
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            int exitCode = await Hexalith.Memories.Cli.Program.Main([]);

            exitCode.ShouldBe(CliExitCodes.Success);
            string combined = stdout.ToString() + stderr.ToString();
            combined.ShouldContain("tenant");
            combined.ShouldContain("memories tenant list");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            _ = ConsoleGate.Release();
        }
    }

    [Fact]
    public void CommandDescriptions_ContainTenantListExamples()
    {
        using ServiceProvider services = CliServices.Build();
        CliGlobalOptions globalOptions = services.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(services, globalOptions);

        root.Description.ShouldNotBeNull();
        root.Description.ShouldContain("memories tenant list");

        Command tenantCommand = root.Subcommands.Single(command => command.Name == "tenant");
        tenantCommand.Description.ShouldNotBeNull();
        tenantCommand.Description.ShouldContain("memories tenant list");

        Command listCommand = tenantCommand.Subcommands.Single(command => command.Name == "list");
        listCommand.Description.ShouldNotBeNull();
        listCommand.Description.ShouldContain("memories tenant list");
    }
}
