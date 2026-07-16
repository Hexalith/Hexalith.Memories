// <copyright file="HandlersListCommandTests.cs" company="ITANEO">
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

public sealed class HandlersListCommandTests
{
    [Fact]
    public async Task Empty_Human_EmitsFormatterLineThenNudgeOnStdout()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Human, BuildSnapshot([]));

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("No handlers registered.");
        stdout.ToString().ShouldContain("Configure EventStoreIntegration:Routing:SourceToTenantMap");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Empty_Table_KeepsHeadersOnStdout_AndNudgeOnStderr()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(OutputFormat.Table, BuildSnapshot([]));

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("TENANT");
        stdout.ToString().ShouldContain("SOURCE");
        stderr.ToString().ShouldContain("Configure EventStoreIntegration:Routing:SourceToTenantMap");
    }

    [Fact]
    public async Task Json_UsesCliEnvelope()
    {
        HandlerRegistrationSnapshot snapshot = BuildSnapshot(
        [
            new HandlerRegistration
            {
                TenantId = "acme",
                SourcePrefix = "acme.events/claims",
                EventTypePatterns = ["Claims"],
                EventsProcessedCount = 5,
                LastEventAt = "2026-04-25T10:00:00.0000000+00:00",
                ObservedEventTypes =
                [
                    new ObservedEventTypeSummary
                    {
                        AggregateType = "Claims",
                        EventType = "MyApp.Claims.ClaimSubmittedV2",
                        Count = 5,
                        LastSeenAt = "2026-04-25T10:00:00.0000000+00:00",
                    },
                ],
            },
        ]);

        (IServiceProvider services, StringWriter stdout, _) = BuildServices(OutputFormat.Json, snapshot);

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("\"command\": \"handlers list\"");
        stdout.ToString().ShouldContain("\"handlers\"");
        stdout.ToString().ShouldContain("\"sourcePrefix\": \"acme.events/claims\"");
    }

    private static async Task<int> InvokeAsync(IServiceProvider services)
    {
        var root = new System.CommandLine.Command("handlers");
        root.Subcommands.Add(HandlersListCommand.Build(services));
        return await root.Parse(new[] { "list" }).InvokeAsync();
    }

    private static HandlerRegistrationSnapshot BuildSnapshot(IReadOnlyList<HandlerRegistration> handlers)
        => new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            AsOf = "2026-04-25T10:00:00.0000000+00:00",
            SubscriptionStatus = HandlerSubscriptionStatus.Active,
            Handlers = handlers,
        };

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        HandlerRegistrationSnapshot snapshot)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => new HandlersListStubClient(snapshot)));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private sealed class HandlersListStubClient : MemoriesClient
    {
        private readonly HandlerRegistrationSnapshot _snapshot;

        public HandlersListStubClient(HandlerRegistrationSnapshot snapshot)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _snapshot = snapshot;
        }

        public override Task<HandlerRegistrationSnapshot> ListHandlersAsync(CancellationToken ct)
            => Task.FromResult(_snapshot);
    }
}