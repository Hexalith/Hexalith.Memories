// <copyright file="QuickstartDryRunIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Cli;

using System.CommandLine;
using System.Text.Json;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Story 7.4 Task 11.1 — dry-run integration check. Invokes <c>memories quickstart --dry-run
/// --format json</c> in-process against the live Aspire fixture and asserts the envelope shape.
/// No side-effects on the fixture (every step runs with <see cref="Hexalith.Memories.Cli.Quickstart.QuickstartStepStatus.DryRun"/>).
/// Does NOT spawn the <c>memories</c> binary (anti-pattern #8 / Task 11.3).
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class QuickstartDryRunIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public QuickstartDryRunIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Quickstart_DryRunJson_EmitsAllDryRunSteps()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection services = CliServices.BuildCollection();
        services.AddSingleton(new CliConsole { Out = stdout, Error = stderr });
        using ServiceProvider provider = services.BuildServiceProvider();

        CliGlobalOptions options = provider.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(provider, options);
        string endpoint = _fixture.MemoriesClient.BaseAddress!.ToString();

        System.CommandLine.ParseResult parse = root.Parse(
            new[]
            {
                "--format", "json",
                "--endpoint", endpoint,
                "quickstart",
                "--dry-run",
            });

        RootCommandFactory.ApplyGlobalOptions(provider, parse, options);
        int exitCode = await parse.InvokeAsync();

        exitCode.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument document = JsonDocument.Parse(stdout.ToString());
        document.RootElement.GetProperty("command").GetString().ShouldBe(QuickstartCommand.CommandName);

        JsonElement data = document.RootElement.GetProperty("data");
        data.GetProperty("overallStatus").GetString().ShouldBe("ok");
        JsonElement steps = data.GetProperty("steps");
        steps.GetArrayLength().ShouldBe(6);
        foreach (JsonElement step in steps.EnumerateArray())
        {
            step.GetProperty("status").GetString().ShouldBe("dry-run");
        }
    }
}
