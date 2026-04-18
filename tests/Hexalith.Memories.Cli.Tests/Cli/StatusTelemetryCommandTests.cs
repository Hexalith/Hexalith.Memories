// <copyright file="StatusTelemetryCommandTests.cs" company="ITANEO">
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
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

public sealed class StatusTelemetryCommandTests
{
    [Fact]
    public async Task Human_PrintsAxisBasedSummary()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Human, BuildSummary());

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("Index sizes:");
        stdout.ToString().ShouldContain("syntactic: 42 (Ready)");
        stdout.ToString().ShouldContain("graph     — requests: 7, errors: 1");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Json_WritesTelemetryEnvelope()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Json, BuildSummary());

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe(StatusTelemetryCommand.CommandName);
        doc.RootElement.GetProperty("data").GetProperty("indexSizes").GetProperty("semantic").GetInt64().ShouldBe(24);
        doc.RootElement.GetProperty("data").GetProperty("searchMetrics").GetProperty("hybrid").GetProperty("errorsLast5m").GetInt64().ShouldBe(2);
    }

    [Fact]
    public async Task Table_WritesStructuredSectionsToStdout()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Table, BuildSummary());

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();
        output.ShouldContain("AXIS");
        output.ShouldContain("HEALTH");
        output.ShouldContain("REQUESTS (5M)");
        output.ShouldContain("documentsLast5m");
        stderr.ToString().ShouldBeEmpty();
    }

    private static async Task<int> InvokeAsync(IServiceProvider services)
    {
        var root = new System.CommandLine.Command("status");
        root.Subcommands.Add(StatusTelemetryCommand.Build(services));
        return await root.Parse(new[] { "telemetry", "--tenant", "tenant-a" }).InvokeAsync();
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        TelemetrySummary summary)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => new TelemetryStubClient(summary)));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private static TelemetrySummary BuildSummary()
        => new()
        {
            TenantId = "tenant-a",
            AsOf = "2026-04-17T10:15:00Z",
            IndexSizes = new TelemetryIndexSizes
            {
                Syntactic = 42,
                Semantic = 24,
                Graph = 12,
            },
            IndexHealth = new TelemetryIndexHealth
            {
                Syntactic = IndexHealth.Ready,
                Semantic = IndexHealth.Degraded,
                Graph = IndexHealth.Ready,
            },
            SearchMetrics = new TelemetrySearchMetrics
            {
                Syntactic = new TelemetryAxisCounters { RequestsLast5m = 11, ErrorsLast5m = 0 },
                Semantic = new TelemetryAxisCounters { RequestsLast5m = 9, ErrorsLast5m = 3 },
                Graph = new TelemetryAxisCounters { RequestsLast5m = 7, ErrorsLast5m = 1 },
                Hybrid = new TelemetryAxisCounters { RequestsLast5m = 5, ErrorsLast5m = 2 },
            },
            IngestionMetrics = new TelemetryIngestionMetrics
            {
                DocumentsLast5m = 17,
                FailuresLast5m = 4,
                QueueDepth = 6,
            },
        };

    private sealed class TelemetryStubClient : MemoriesClient
    {
        private readonly TelemetrySummary _summary;

        public TelemetryStubClient(TelemetrySummary summary)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _summary = summary;
        }

        public override Task<TelemetrySummary> GetTelemetrySummaryAsync(string tenantId, CancellationToken ct)
            => Task.FromResult(_summary with { TenantId = tenantId });
    }
}