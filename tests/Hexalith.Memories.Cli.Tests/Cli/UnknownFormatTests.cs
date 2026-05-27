// <copyright file="UnknownFormatTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

public sealed class UnknownFormatTests
{
    [Fact]
    public void ApplyGlobalOptions_UnknownFormat_ThrowsInvalidConfigurationException()
    {
        using ServiceProvider services = CliServices.Build();
        CliGlobalOptions options = services.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(services, options);
        ParseResult parse = root.Parse(new[] { "--format", "xml", "tenant", "list" });

        InvalidConfigurationException ex = Should.Throw<InvalidConfigurationException>(() =>
            RootCommandFactory.ApplyGlobalOptions(services, parse, options));

        ex.Message.ShouldContain("Unknown format 'xml'. Use human, json, or table.");
    }

    [Fact]
    public void ApplyGlobalOptions_UnknownFormatWithHelp_DoesNotThrow()
    {
        using ServiceProvider services = CliServices.Build();
        CliGlobalOptions options = services.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(services, options);
        ParseResult parse = root.Parse(new[] { "--format", "xml", "tenant", "list", "--help" });

        Should.NotThrow(() => RootCommandFactory.ApplyGlobalOptions(services, parse, options));
    }

    [Fact]
    public void ApplyGlobalOptions_ValidFormat_AssignsCliConsoleFormat()
    {
        using ServiceProvider services = CliServices.Build();
        CliGlobalOptions options = services.GetRequiredService<CliGlobalOptions>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        RootCommand root = RootCommandFactory.Build(services, options);

        ParseResult parse = root.Parse(new[] { "--format", "json", "tenant", "list" });
        RootCommandFactory.ApplyGlobalOptions(services, parse, options);

        console.Format.ShouldBe(OutputFormat.Json);
    }

    [Fact]
    public void ApplyGlobalOptions_NoFormatFlag_DefaultsToHuman()
    {
        using ServiceProvider services = CliServices.Build();
        CliGlobalOptions options = services.GetRequiredService<CliGlobalOptions>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        RootCommand root = RootCommandFactory.Build(services, options);

        ParseResult parse = root.Parse(new[] { "tenant", "list" });
        RootCommandFactory.ApplyGlobalOptions(services, parse, options);

        console.Format.ShouldBe(OutputFormat.Human);
    }
}
