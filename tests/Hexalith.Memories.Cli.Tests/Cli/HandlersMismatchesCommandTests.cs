// <copyright file="HandlersMismatchesCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System;
using System.Threading;
using System.Threading.Tasks;

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

public sealed class HandlersMismatchesCommandTests
{
    [Fact]
    public async Task Table_Format_UsesTableFormatter_NotHumanLines()
    {
        HandlerMismatchReport report = BuildReport(
        [
            new HandlerMismatch
            {
                Category = HandlerMismatchCategory.VersionMismatch,
                Severity = HandlerMismatchSeverity.Warning,
                Subject = "ClaimSubmitted",
                Context = "ctx",
                Suggestion = "review versions",
            },
        ]);

        (IServiceProvider services, StringWriter stdout, _) = BuildServices(OutputFormat.Table, report);

        int exit = await InvokeAsync(services, "acme");

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("SEVERITY");
        stdout.ToString().ShouldContain("CATEGORY");
        stdout.ToString().ShouldNotContain("[warning]");
    }

    [Fact]
    public async Task Json_Format_RemainsUnfiltered()
    {
        HandlerMismatchReport report = BuildReport(
        [
            new HandlerMismatch
            {
                Category = HandlerMismatchCategory.StaleHandler,
                Severity = HandlerMismatchSeverity.Info,
                Subject = "acme.events",
                Context = "ctx",
                Suggestion = "check publisher",
            },
            new HandlerMismatch
            {
                Category = HandlerMismatchCategory.VersionMismatch,
                Severity = HandlerMismatchSeverity.Warning,
                Subject = "ClaimSubmitted",
                Context = "ctx",
                Suggestion = "review versions",
            },
        ]);

        (IServiceProvider services, StringWriter stdout, _) = BuildServices(OutputFormat.Json, report);

        int exit = await InvokeAsync(services, "acme", "--severity", "warning");

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("\"command\": \"handlers mismatches\"");
        stdout.ToString().ShouldContain("\"staleHandler\"");
        stdout.ToString().ShouldContain("\"versionMismatch\"");
    }

    [Fact]
    public async Task Healthy_Human_EmitsSummaryMessage()
    {
        (IServiceProvider services, StringWriter stdout, _) = BuildServices(OutputFormat.Human, BuildReport([]));

        int exit = await InvokeAsync(services, "acme");

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("No handler mismatches detected");
        stdout.ToString().ShouldContain("routes configured");
    }

    [Fact]
    public async Task Human_Format_RendersProjectionBindingMissingCategory()
    {
        HandlerMismatchReport report = BuildReport(
        [
            new HandlerMismatch
            {
                Category = HandlerMismatchCategory.ProjectionBindingMissing,
                Severity = HandlerMismatchSeverity.Warning,
                Subject = "acme/enterprise/claims/claimsubmitted",
                Context = "ctx",
                Suggestion = "register an authoritative projection binding. See: https://docs.hexalith.dev/memories/runbooks/handler-projection-binding-missing.",
            },
        ]);

        (IServiceProvider services, StringWriter stdout, _) = BuildServices(OutputFormat.Human, report);

        int exit = await InvokeAsync(services, "acme", "--severity", "warning", "--exclude-stale");

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("projectionBindingMissing");
        stdout.ToString().ShouldContain("register an authoritative projection binding");
        // Story 16.1 review F15 — assert the exact kebab-cased runbook URL survives the human renderer.
        stdout.ToString().ShouldContain("handler-projection-binding-missing");
    }

    [Fact]
    public async Task Human_Format_ExcludeStaleDoesNotSuppressProjectionBindingMissing()
    {
        // Story 16.1 review F14 — `--exclude-stale` must filter only `StaleHandler`, never `ProjectionBindingMissing`.
        HandlerMismatchReport report = BuildReport(
        [
            new HandlerMismatch
            {
                Category = HandlerMismatchCategory.StaleHandler,
                Severity = HandlerMismatchSeverity.Info,
                Subject = "acme.events",
                Context = "ctx",
                Suggestion = "check publisher",
            },
            new HandlerMismatch
            {
                Category = HandlerMismatchCategory.ProjectionBindingMissing,
                Severity = HandlerMismatchSeverity.Warning,
                Subject = "acme/enterprise/claims/claimsubmitted",
                Context = "ctx",
                Suggestion = "register an authoritative projection binding",
            },
        ]);

        (IServiceProvider services, StringWriter stdout, _) = BuildServices(OutputFormat.Human, report);

        int exit = await InvokeAsync(services, "acme", "--exclude-stale");

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("projectionBindingMissing");
        stdout.ToString().ShouldNotContain("acme.events");
    }

    private static async Task<int> InvokeAsync(IServiceProvider services, params string[] args)
    {
        var root = new System.CommandLine.Command("handlers");
        root.Subcommands.Add(HandlersMismatchesCommand.Build(services));
        string[] invocation = ["mismatches", "--tenant", .. args];
        return await root.Parse(invocation).InvokeAsync();
    }

    private static HandlerMismatchReport BuildReport(IReadOnlyList<HandlerMismatch> mismatches)
        => new()
        {
            TenantId = "acme",
            AsOf = "2026-04-25T10:00:00.0000000+00:00",
            WindowHours = 24,
            Mismatches = mismatches,
            Summary = new HandlerMismatchReportSummary { RoutesConfigured = 1, ObservationsChecked = mismatches.Count },
        };

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        HandlerMismatchReport report)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => new HandlersMismatchesStubClient(report)));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private sealed class HandlersMismatchesStubClient : MemoriesClient
    {
        private readonly HandlerMismatchReport _report;

        public HandlersMismatchesStubClient(HandlerMismatchReport report)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _report = report;
        }

        public override Task<HandlerMismatchReport> GetHandlerMismatchesAsync(string tenantId, CancellationToken ct)
            => Task.FromResult(_report);
    }
}
