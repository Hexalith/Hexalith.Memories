// <copyright file="SearchQueryCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Text.Json;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

public sealed class SearchQueryCommandTests
{
    [Fact]
    public async Task Invoke_MaxResultsAboveCeiling_WritesMessageAndExitsPlumbing()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices();
        Command query = SearchQueryCommand.Build(services);
        int exceed = SearchQueryCommand.MaxResultsCeiling + 1;

        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--query", "x", "--max-results", exceed.ToString() })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("ceiling of 1000");
        stdout.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Invoke_MissingQueryOnHybrid_WritesErrorAndExitsPlumbing()
    {
        (IServiceProvider services, _, StringWriter stderr) = BuildServices();
        Command query = SearchQueryCommand.Build(services);

        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--axis", "hybrid" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("--query is required");
    }

    [Fact]
    public async Task Invoke_MissingQueryOnGraph_DoesNotTriggerQueryValidation()
    {
        // Graph axis is allowed without a query. We don't have a server, so the command will still fail at
        // the HTTP layer — that's fine. What we care about is that validation doesn't short-circuit before
        // the HTTP call when axis=graph and query is empty.
        (IServiceProvider services, _, StringWriter stderr) = BuildServices();
        Command query = SearchQueryCommand.Build(services);

        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--axis", "graph" })
            .InvokeAsync();

        stderr.ToString().ShouldNotContain("--query is required");
        exit.ShouldBe(CliExitCodes.Plumbing);
    }

    [Fact]
    public async Task Invoke_MissingQueryOnNaturalLanguage_WritesErrorAndExitsPlumbing()
    {
        (IServiceProvider services, _, StringWriter stderr) = BuildServices();
        Command query = SearchQueryCommand.Build(services);

        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--axis", "nl" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("--query is required for --axis nl");
    }

    [Fact]
    public async Task Invoke_MissingQueryOnHybrid_Json_EmitsErrorEnvelopeOnStdout()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Json);
        Command query = SearchQueryCommand.Build(services);

        int exit = await query
            .Parse(new[] { "query", "--tenant", "t1", "--axis", "hybrid" })
            .InvokeAsync();

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe(SearchQueryCommand.CommandName);
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("INVALID_INPUT");
        doc.RootElement.GetProperty("error").GetProperty("message").GetString()!
            .ShouldContain("--query is required");
        doc.RootElement.GetProperty("error").GetProperty("suggestion").GetString()!
            .ShouldContain("memories search query --help");
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(OutputFormat format = OutputFormat.Human)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });
        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65000/");
        return (provider, stdout, stderr);
    }
}
