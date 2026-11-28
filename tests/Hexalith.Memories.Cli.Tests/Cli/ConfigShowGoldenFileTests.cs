// <copyright file="ConfigShowGoldenFileTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Locks the 7.1 byte-for-byte <c>config show</c> output. AC #1 / ADR-7.2-002: any drift of the
/// human-format default surface is a silent breaking change for every script written against 7.1.
/// Per Task 9.2, the test exercises the full <c>ConfigShowCommand.Build(...)</c> →
/// <c>ResolvedConfigPipeline.Resolve()</c> → <c>EndpointDisplayFormatter.Format(Uri)</c> chain against
/// a fixture configuration source so the <c>Uri → string</c> conversion (where trailing-slash /
/// default-port normalization lives) is part of the regression guard.
/// </summary>
public sealed class ConfigShowGoldenFileTests
{
    [Fact]
    public void ConfigShowCommand_NoFormat_ExactMatchesStory71Default()
    {
        string output = InvokeConfigShow(
            endpoint: new Uri("http://127.0.0.1:5000/"),
            apiToken: null,
            sourceName: "DefaultConfigurationSource");

        const string expected =
            "endpoint=http://127.0.0.1:5000/\n"
            + "resolvedBy=DefaultConfigurationSource\n"
            + "tokenConfigured=false\n";

        output.ShouldBe(expected);
    }

    [Fact]
    public void ConfigShowCommand_WithToken_EmitsTokenConfiguredTrue()
    {
        string output = InvokeConfigShow(
            endpoint: new Uri("https://memories.example.com/"),
            apiToken: "synthetic-" + Guid.NewGuid().ToString("N"),
            sourceName: "FlagConfigurationSource");

        output.ShouldBe(
            "endpoint=https://memories.example.com/\n"
            + "resolvedBy=FlagConfigurationSource\n"
            + "tokenConfigured=true\n");

    }

    [Fact]
    public void ConfigShowCommand_JsonFormat_WithToken_EmitsTokenConfiguredTrueWithoutLeakingSecret()
    {
        string output = InvokeConfigShow(
            endpoint: new Uri("https://memories.example.com/"),
            apiToken: "synthetic-" + Guid.NewGuid().ToString("N"),
            sourceName: "FlagConfigurationSource",
            format: OutputFormat.Json);

        using var document = System.Text.Json.JsonDocument.Parse(output);
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("tokenConfigured").GetBoolean().ShouldBeTrue();
        data.GetProperty("endpoint").GetString().ShouldBe("https://memories.example.com/");
    }

    [Fact]
    public void HumanFormatter_Direct_MatchesCommandOutput()
    {
        // Cross-check: formatter in isolation should produce the same bytes as the end-to-end invocation.
        // If this assertion fails, either ConfigShowCommand.Build or EndpointDisplayFormatter has drifted
        // from the pure-formatter path and the regression guard above is still the authoritative surface.
        var data = new ConfigShowData(
            Endpoint: "http://127.0.0.1:5000/",
            ResolvedBy: "DefaultConfigurationSource",
            TokenConfigured: false);
        using var writer = new StringWriter() { NewLine = "\n" };

        new ConfigShowHumanFormatter().Write(data, writer);

        const string expected =
            "endpoint=http://127.0.0.1:5000/\n"
            + "resolvedBy=DefaultConfigurationSource\n"
            + "tokenConfigured=false\n";

        writer.ToString().ShouldBe(expected);
    }

    private static string InvokeConfigShow(Uri endpoint, string? apiToken, string sourceName, OutputFormat format = OutputFormat.Human)
    {
        IServiceCollection services = CliServices.BuildCollection();

        // Remove the default IConfigurationSource tier stack and inject a single fixture source so the
        // test is deterministic and does not read environment variables or on-disk config files.
        for (int i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(IConfigurationSource))
            {
                services.RemoveAt(i);
            }
        }

        services.AddSingleton<IConfigurationSource>(new FixtureConfigurationSource(endpoint, apiToken, sourceName));

        using ServiceProvider provider = services.BuildServiceProvider();

        CliConsole console = provider.GetRequiredService<CliConsole>();
        using var stdout = new StringWriter() { NewLine = "\n" };
        using var stderr = new StringWriter() { NewLine = "\n" };
        console.Out = stdout;
        console.Error = stderr;
        console.Format = format;

        Command command = ConfigShowCommand.Build(provider);
        int exitCode = command.Parse(Array.Empty<string>()).Invoke();

        // Check both streams before any golden-file diff or assertion can render candidate bytes.
        if (apiToken is not null && (stdout.ToString().Contains(apiToken, StringComparison.Ordinal)
            || stderr.ToString().Contains(apiToken, StringComparison.Ordinal)))
        {
            throw new ShouldAssertException("Configured credential reached CLI output.");
        }

        exitCode.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();

        return stdout.ToString();
    }

    private sealed class FixtureConfigurationSource(Uri endpoint, string? apiToken, string sourceName)
        : IConfigurationSource
    {
        public string SourceName { get; } = sourceName;

        public bool TryResolve(out Uri? endpoint2, out string? apiToken2)
        {
            endpoint2 = endpoint;
            apiToken2 = apiToken;
            return true;
        }
    }
}
