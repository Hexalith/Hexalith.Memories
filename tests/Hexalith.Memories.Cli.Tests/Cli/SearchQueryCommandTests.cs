// <copyright file="SearchQueryCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.CommandLine;
using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

    [Theory]
    [InlineData("abc")]
    [InlineData("2147483648")]
    public async Task Invoke_NonIntegerMaxResults_EmitsStructuredValidationError(string value)
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Json);
        Command query = SearchQueryCommand.Build(services);

        int exit = await query.Parse(["query", "--tenant", "t1", "--query", "x", "--max-results", value]).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldBeEmpty();
        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe(SearchQueryCommand.CommandName);
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("INVALID_INPUT");
        doc.RootElement.GetProperty("error").GetProperty("message").GetString()!.ShouldContain("--max-results must be a whole number");
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(OutputFormat.Human)]
    [InlineData(OutputFormat.Table)]
    [InlineData(OutputFormat.Json)]
    public async Task Invoke_NonFiniteUpstreamScore_EmitsOnlyTheError(OutputFormat format)
    {
        const string json = """
            {"results":[{"memoryUnitId":"mu","compositeScore":1e999,"contentSnippet":"hidden-result","sourceUri":"source","sourceType":"file"}],
             "totalCount":1,"degraded":false,"unavailableAxes":[],"query":"needle"}
            """;
        using var handler = new TestDelegatingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:65000/") };
        var client = new MemoriesClient(http, Options.Create(new MemoriesClientOptions()), NullLogger<MemoriesClient>.Instance);
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(format, client);
        Command query = SearchQueryCommand.Build(services);

        int exit = await query.Parse(["query", "--tenant", "t1", "--query", "needle"]).InvokeAsync();

        exit.ShouldBe(CliExitCodes.Plumbing);
        if (format == OutputFormat.Json)
        {
            stderr.ToString().ShouldBeEmpty();
            using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("INVALID_RESPONSE");
            doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();
        }
        else
        {
            stdout.ToString().ShouldBeEmpty();
            stderr.ToString().ShouldContain("INVALID_RESPONSE");
            stderr.ToString().ShouldNotContain("hidden-result");
        }
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format = OutputFormat.Human,
        MemoriesClient? client = null)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });
        if (client is not null)
        {
            collection.AddSingleton(client);
        }

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65000/");
        return (provider, stdout, stderr);
    }
}
