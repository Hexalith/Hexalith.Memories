// <copyright file="QuickstartLiveIntegrationTests.cs" company="ITANEO">
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
/// Story 7.4 Task 11.2 / Risk #5 — live wizard against a warm Aspire fixture with prereq/boot
/// checks skipped. Asserts NFR31 CI-gated bound (&lt; 60 seconds total elapsed across the four
/// non-skipped steps) and overall "ok" status. The 30-minute end-to-end NFR31 claim in the PRD is
/// a human-facing UX target measured quarterly (Task 12.6), NOT this gate.
/// <para>
/// Does NOT spawn the <c>memories</c> binary — invokes <see cref="QuickstartCommand.ExecuteAsync"/>
/// via the DI container (anti-pattern #8 / Task 11.3).
/// </para>
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class QuickstartLiveIntegrationTests
{
    private const int NfrCeilingMs = 60_000;

    private readonly AspireIngestionPipelineFixture _fixture;

    public QuickstartLiveIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Quickstart_AgainstLiveFixture_SucceedsWithinSixtySeconds()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection services = CliServices.BuildCollection();
        services.AddSingleton(new CliConsole { Out = stdout, Error = stderr });
        using ServiceProvider provider = services.BuildServiceProvider();

        CliGlobalOptions options = provider.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(provider, options);
        string endpoint = _fixture.MemoriesClient.BaseAddress!.ToString();
        string tenantId = $"quickstart-test-{Guid.NewGuid():N}";

        System.CommandLine.ParseResult parse = root.Parse(
            new[]
            {
                "--format", "json",
                "--endpoint", endpoint,
                "quickstart",
                "--tenant", tenantId,
                "--skip-prereq-check",
                "--skip-boot-check",
            });

        RootCommandFactory.ApplyGlobalOptions(provider, parse, options);

        int exitCode = await parse.InvokeAsync();

        stderr.ToString().ShouldBeEmpty(stdout.ToString());
        exitCode.ShouldBe(CliExitCodes.Success, $"stdout:{Environment.NewLine}{stdout}{Environment.NewLine}stderr:{Environment.NewLine}{stderr}");

        using JsonDocument document = JsonDocument.Parse(stdout.ToString());
        JsonElement data = document.RootElement.GetProperty("data");
        data.GetProperty("overallStatus").GetString().ShouldBe("ok");
        int elapsedMs = data.GetProperty("elapsedMs").GetInt32();
        elapsedMs.ShouldBeLessThan(NfrCeilingMs);

        JsonElement steps = data.GetProperty("steps");
        steps.GetArrayLength().ShouldBe(6);

        // Step 1 + step 3 must be SKIP (flags set); step 2 is the boot hint; steps 4/5/6 must be OK.
        steps[0].GetProperty("status").GetString().ShouldBe("skip");
        steps[2].GetProperty("status").GetString().ShouldBe("skip");
        steps[3].GetProperty("status").GetString().ShouldBe("ok");
        steps[4].GetProperty("status").GetString().ShouldBe("ok");
        steps[5].GetProperty("status").GetString().ShouldBe("ok");
    }
}
