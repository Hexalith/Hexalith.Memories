// <copyright file="ProgramTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

public sealed class ProgramTests
{
    [Theory]
    [InlineData(new[] { "--telemetry" }, true)]
    [InlineData(new[] { "--telemetry", "true" }, true)]
    [InlineData(new[] { "--telemetry", "false" }, false)]
    [InlineData(new[] { "--telemetry=true" }, true)]
    [InlineData(new[] { "--telemetry=false" }, false)]
    [InlineData(new[] { "search", "query" }, false)]
    public void IsTelemetryEnabled_ParsesSupportedForms(string[] args, bool expected)
        => Program.IsTelemetryEnabled(args).ShouldBe(expected);

    [Fact]
    public void WriteInvalidConfigurationError_JsonRequested_EmitsEnvelopeOnStdout()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection services = CliServices.BuildCollection();
        services.AddSingleton(new CliConsole { Out = stdout, Error = stderr });
        using ServiceProvider provider = services.BuildServiceProvider();

        CliGlobalOptions options = provider.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(provider, options);
        ParseResult parse = root.Parse(new[] { "--format", "json", "--endpoint", "not-a-uri", "tenant", "list" });

        int exitCode = Program.WriteInvalidConfigurationError(
            provider,
            parse,
            options,
            "value 'not-a-uri' is not an absolute URI.");

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe("memories");
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("INVALID_CONFIG");
        doc.RootElement.GetProperty("error").GetProperty("message").GetString()!
            .ShouldContain("Invalid configuration:");
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();
    }
}
