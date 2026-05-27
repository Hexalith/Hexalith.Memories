// <copyright file="TenantListCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

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

/// <summary>
/// Story 7.3 Task 7.10 — handler-level assertions for the empty-tenant nudge (AC #2 tenant-list case).
/// Formatter-level assertions stay in <c>TenantListFormatterTests</c> (ADR-7.2-002 byte-for-byte
/// parity); this file tests the two-line output the handler appends.
/// </summary>
public sealed class TenantListCommandTests
{
    [Fact]
    public async Task Empty_Human_EmitsFormatterLineThenNudgeOnStdout()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            OutputFormat.Human,
            tenants: []);

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();

        int firstLineBreak = output.IndexOf('\n');
        firstLineBreak.ShouldBeGreaterThan(0);
        output[..firstLineBreak].ShouldContain("No tenants found.");

        string remainder = output[(firstLineBreak + 1)..];
        remainder.ShouldContain("Get started:");
        remainder.ShouldContain("memories quickstart");

        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Empty_Json_EmptyDataArrayNoNudge()
    {
        (IServiceProvider services, StringWriter stdout, _) = BuildServices(
            OutputFormat.Json,
            tenants: []);

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("\"data\": []");
        stdout.ToString().ShouldNotContain("Get started:");
    }

    [Fact]
    public async Task Empty_Table_NudgeOnStderrKeepsTableStdoutAligned()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            OutputFormat.Table,
            tenants: []);

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("TENANT ID");
        stdout.ToString().ShouldNotContain("Get started:");
        stderr.ToString().ShouldContain("Get started:");
    }

    [Fact]
    public async Task NonEmpty_Human_NoNudgeAppended()
    {
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(
            OutputFormat.Human,
            tenants: [BuildSummary("t-1", "Tenant One")]);

        int exit = await InvokeAsync(services);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("t-1");
        stdout.ToString().ShouldNotContain("No tenants found.");
        stdout.ToString().ShouldNotContain("Get started:");
        stderr.ToString().ShouldBeEmpty();
    }

    private static async Task<int> InvokeAsync(IServiceProvider services)
    {
        var root = new System.CommandLine.Command("tenant");
        root.Subcommands.Add(TenantListCommand.Build(services));
        return await root.Parse(new[] { "list" }).InvokeAsync();
    }

    private static TenantSummary BuildSummary(string id, string displayName)
        => new()
        {
            Id = id,
            DisplayName = displayName,
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            IndexSizes = new TenantIndexSizes(null, null, null),
            IndexStatus = new TenantIndexStatus(IndexHealth.Unknown, IndexHealth.Unknown, IndexHealth.Unknown),
            ReindexRequired = false,
        };

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        IReadOnlyList<TenantSummary> tenants)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole { Out = stdout, Error = stderr, Format = format });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ =>
            new TenantStubClient(tenants)));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    private sealed class TenantStubClient : MemoriesClient
    {
        private readonly IReadOnlyList<TenantSummary> _tenants;

        public TenantStubClient(IReadOnlyList<TenantSummary> tenants)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _tenants = tenants;
        }

        public override Task<IReadOnlyList<TenantSummary>> ListTenantsAsync(CancellationToken ct)
            => Task.FromResult(_tenants);
    }
}
