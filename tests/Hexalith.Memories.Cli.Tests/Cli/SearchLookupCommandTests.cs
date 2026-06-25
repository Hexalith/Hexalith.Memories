// <copyright file="SearchLookupCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Story 18.5 — <c>memories search lookup</c> CLI coverage.</summary>
public sealed class SearchLookupCommandTests
{
    [Fact]
    public async Task Run_Found_PrintsMemoryUnitIdAndSucceeds()
    {
        LookupStubClient stub = new() { Result = "mu-found" };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["lookup", "--tenant", "acme", "--case", "case-1", "--source-uri", "file:///doc.pdf"]);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("mu-found");
        stderr.ToString().ShouldBeEmpty();
        stub.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Run_Found_JsonFormat_EmitsEnvelopeWithMemoryUnitId()
    {
        LookupStubClient stub = new() { Result = "mu-json" };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Json, stub);

        int exit = await InvokeAsync(services, ["lookup", "--tenant", "acme", "--case", "case-1", "--source-uri", "file:///doc.pdf"]);

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe(SearchLookupCommand.CommandName);
        doc.RootElement.GetProperty("data").GetProperty("memoryUnitId").GetString().ShouldBe("mu-json");
    }

    [Fact]
    public async Task Run_NotFound_ReturnsNotFoundExitAndErrorEnvelope()
    {
        LookupStubClient stub = new() { Result = null };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["lookup", "--tenant", "acme", "--case", "case-1", "--source-uri", "file:///missing.pdf"]);

        exit.ShouldBe(CliExitCodes.NotFound);
        stderr.ToString().ShouldContain("MEMORY_UNIT_NOT_FOUND");
        stub.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Run_BlankSourceUri_ReturnsPlumbingExitCode()
    {
        LookupStubClient stub = new();
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["lookup", "--tenant", "acme", "--case", "case-1", "--source-uri", "   "]);

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("INVALID_INPUT");
        stub.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData("   ", "case-1", "file:///doc.pdf")]
    [InlineData("acme", "   ", "file:///doc.pdf")]
    public async Task Run_BlankTenantOrCase_ReturnsPlumbingExitCode(string tenant, string caseId, string sourceUri)
    {
        // The handler guards all three args (not just --source-uri): a whitespace --tenant or --case is also a
        // plumbing-level input error and must short-circuit before the client is touched.
        LookupStubClient stub = new();
        (IServiceProvider services, _, StringWriter stderr) = BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["lookup", "--tenant", tenant, "--case", caseId, "--source-uri", sourceUri]);

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("INVALID_INPUT");
        stub.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Run_MissingRequiredOption_ReturnsNonSuccess_WithoutCallingClient()
    {
        LookupStubClient stub = new();
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Human, stub);

        // --source-uri omitted: System.CommandLine enforces the required option before our handler runs.
        int exit = await InvokeAsync(services, ["lookup", "--tenant", "acme", "--case", "case-1"]);

        exit.ShouldNotBe(CliExitCodes.Success);
        stub.Calls.ShouldBe(0);
    }

    private static async Task<int> InvokeAsync(IServiceProvider services, string[] args)
    {
        var root = new System.CommandLine.Command("search");
        root.Subcommands.Add(SearchLookupCommand.Build(services));
        return await root.Parse(args).InvokeAsync();
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        LookupStubClient stubClient)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole
        {
            In = new StringReader(string.Empty),
            Out = stdout,
            Error = stderr,
            Format = format,
            IsInteractive = false,
        });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => stubClient));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private sealed class LookupStubClient : MemoriesClient
    {
        public LookupStubClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public string? Result { get; set; }

        public int Calls { get; private set; }

        public override Task<string?> LookupMemoryUnitIdBySourceUriAsync(
            string tenantId, string caseId, string sourceUri, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }
}
