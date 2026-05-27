// <copyright file="NotImplementedCommandTaggingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Story 7.4 Task 7.5 — guards against the loophole where a future contributor removes the stub
/// marker from <see cref="NotImplementedCommand.Create"/> and <see cref="CliHelpCompletenessTests"/>
/// silently starts auditing stubs.
/// </summary>
public sealed class NotImplementedCommandTaggingTests
{
    [Fact]
    public void Create_AlwaysTagsStub()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CliConsole>();
        using ServiceProvider provider = services.BuildServiceProvider();

        Command stub = NotImplementedCommand.Create(provider, "test-name", "test-desc", "7.X");

        NotImplementedCommand.IsStub(stub).ShouldBeTrue();
    }

    [Fact]
    public void IsStub_ReturnsFalseForRegularCommand()
    {
        var command = new Command("regular", "A regular command.");
        NotImplementedCommand.IsStub(command).ShouldBeFalse();
    }
}
