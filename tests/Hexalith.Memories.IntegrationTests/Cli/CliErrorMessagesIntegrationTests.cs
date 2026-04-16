// <copyright file="CliErrorMessagesIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Cli;

using System.CommandLine;
using System.Text.Json;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Errors;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Story 7.3 Task 8 / AC #10 — end-to-end guard that the error-translation chain works against a live
/// Aspire stack. Invokes the real CLI command handler in-process against the live stack (no subprocess)
/// and asserts the rendered JSON envelope, suggestion text, and exit behavior. Does NOT spawn the
/// <c>memories</c> binary (anti-pattern #8 / Task 8.3).
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class CliErrorMessagesIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public CliErrorMessagesIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SearchInspect_NonexistentTenantValidFormat_EmitsCliJsonErrorEnvelope()
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
                "search", "inspect",
                "--tenant", "nonexistent-tenant",
                "--case", "anything",
                "--id", "anything",
            });

        RootCommandFactory.ApplyGlobalOptions(provider, parse, options);
        int exitCode = await parse.InvokeAsync();

        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe(SearchInspectCommand.CommandName);
        JsonElement error = doc.RootElement.GetProperty("error");
        string code = error.GetProperty("code").GetString()!;

        code.ShouldBe(
            "CASE_NOT_FOUND",
            customMessage: $"Expected CASE_NOT_FOUND but got {code}. Validation order in Program.cs:1026 may have changed — update the pinned code or the test accordingly.");

        ErrorTranslation translation = ErrorMessageCatalog.Resolve(code);
        exitCode.ShouldBe(
            translation.ExitCode,
            customMessage: "CLI exit behavior must match the catalog-resolved exit code.");

        string suggestion = error.GetProperty("suggestion").GetString()!;
        suggestion.ShouldNotBeNullOrWhiteSpace();
        bool referencesConcreteNextAction =
            suggestion.Contains("memories ", StringComparison.Ordinal)
            || suggestion.Contains("REST API", StringComparison.Ordinal);
        referencesConcreteNextAction.ShouldBeTrue(
            $"Suggestion must point at a concrete next action; got: '{suggestion}'");

        error.GetProperty("message").GetString()!.ShouldNotBeNullOrWhiteSpace();
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();
    }
}
