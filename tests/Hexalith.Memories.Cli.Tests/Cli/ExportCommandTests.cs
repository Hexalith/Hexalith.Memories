// <copyright file="ExportCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text;

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

/// <summary>
/// Story 8.3 — exercises <c>memories export case</c> and <c>memories export tenant</c> via the
/// <see cref="ExportStubClient"/> pattern.
/// </summary>
public sealed class ExportCommandTests : IDisposable
{
    private const string ValidCaseUlid = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9";

    private readonly string _scratchDir;

    public ExportCommandTests()
    {
        _scratchDir = Path.Combine(Path.GetTempPath(), "hexalith-export-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scratchDir);
    }

    [Fact]
    public async Task ExportCase_WritesJsonToOutputFile_AndRemovesPartFile()
    {
        string output = Path.Combine(_scratchDir, "case.json");
        ExportStubClient stub = new("{\"manifest\":{\"schemaVersion\":1,\"scope\":\"case\"}}");
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(stub);

        int exit = await InvokeExportCaseAsync(services, ["case", "--tenant", "acme", "--case", ValidCaseUlid, "--output", output, "--allow-absolute-path"]);

        exit.ShouldBe(CliExitCodes.Success);
        File.Exists(output).ShouldBeTrue();
        File.Exists(output + ".part").ShouldBeFalse();
        File.ReadAllText(output).ShouldContain("\"schemaVersion\":1");
    }

    [Fact]
    public async Task ExportCase_ExistingFileWithoutForce_ReturnsPlumbingExitCode()
    {
        string output = Path.Combine(_scratchDir, "case.json");
        File.WriteAllText(output, "old");
        ExportStubClient stub = new("{}");
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(stub);

        int exit = await InvokeExportCaseAsync(services, ["case", "--tenant", "acme", "--case", ValidCaseUlid, "--output", output, "--allow-absolute-path"]);

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("EXPORT_OUTPUT_PATH_INVALID");
        File.ReadAllText(output).ShouldBe("old");
    }

    [Fact]
    public async Task ExportCase_AbsolutePathWithoutOptIn_ReturnsPlumbingExitCode()
    {
        string output = Path.Combine(_scratchDir, "case.json");
        ExportStubClient stub = new("{}");
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(stub);

        // Omit --allow-absolute-path: the temp dir is outside the CWD, so this should refuse.
        int exit = await InvokeExportCaseAsync(services, ["case", "--tenant", "acme", "--case", ValidCaseUlid, "--output", output]);

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("EXPORT_OUTPUT_PATH_INVALID");
    }

    [Fact]
    public async Task ExportTenant_WritesJsonToOutputFile()
    {
        string output = Path.Combine(_scratchDir, "tenant.json");
        ExportStubClient stub = new("{\"manifest\":{\"schemaVersion\":1,\"scope\":\"tenant\"}}");
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) = BuildServices(stub);

        int exit = await InvokeExportTenantAsync(services, ["tenant", "--tenant", "acme", "--output", output, "--allow-absolute-path"]);

        exit.ShouldBe(CliExitCodes.Success);
        File.Exists(output).ShouldBeTrue();
        File.ReadAllText(output).ShouldContain("\"scope\":\"tenant\"");
    }

    [Fact]
    public async Task ExportTenant_MissingTenantOption_ReturnsPlumbing()
    {
        ExportStubClient stub = new("{}");
        (IServiceProvider services, _, StringWriter stderr) = BuildServices(stub);

        var root = new System.CommandLine.Command("export");
        root.Subcommands.Add(ExportTenantCommand.Build(services));
        int exit = await root.Parse(["tenant"]).InvokeAsync();

        // System.CommandLine enforces required options and prints its own diagnostic before dispatch.
        exit.ShouldNotBe(CliExitCodes.Success);
    }

    private static async Task<int> InvokeExportCaseAsync(IServiceProvider services, string[] args)
    {
        var root = new System.CommandLine.Command("export");
        root.Subcommands.Add(ExportCaseCommand.Build(services));
        return await root.Parse(args).InvokeAsync();
    }

    private static async Task<int> InvokeExportTenantAsync(IServiceProvider services, string[] args)
    {
        var root = new System.CommandLine.Command("export");
        root.Subcommands.Add(ExportTenantCommand.Build(services));
        return await root.Parse(args).InvokeAsync();
    }

    private static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        ExportStubClient stubClient)
    {
        StringWriter stdout = new();
        StringWriter stderr = new();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole
        {
            In = new StringReader(string.Empty),
            Out = stdout,
            Error = stderr,
            Format = OutputFormat.Human,
            IsInteractive = false,
        });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => stubClient));

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_scratchDir))
            {
                Directory.Delete(_scratchDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup.
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>Test double for <see cref="MemoriesClient"/> that returns a scripted export stream.</summary>
internal sealed class ExportStubClient : MemoriesClient
{
    private readonly string _body;

    public ExportStubClient(string body)
        : base(
            new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
            Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
            NullLogger<MemoriesClient>.Instance)
    {
        _body = body;
    }

    public int ExportCaseCalls { get; private set; }

    public int ExportTenantCalls { get; private set; }

    public override Task<Stream> ExportCaseAsync(string tenantId, string caseId, CancellationToken ct)
    {
        ExportCaseCalls++;
        return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(_body)));
    }

    public override Task<Stream> ExportTenantAsync(string tenantId, CancellationToken ct)
    {
        ExportTenantCalls++;
        return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(_body)));
    }
}
