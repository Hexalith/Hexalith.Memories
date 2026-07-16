// <copyright file="QuickstartCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Net.Sockets;
using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Configuration;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Case = Hexalith.Memories.Contracts.V1.Case;

public sealed class QuickstartCommandTests
{
    [Fact]
    public async Task Execute_DryRun_MakesNoClientCalls_AndExitsZero()
    {
        using var harness = new Harness();
        int exitCode = await harness.RunAsync(new QuickstartOptions(null, SkipBootCheck: false, SkipPrereqCheck: false, DryRun: true));

        exitCode.ShouldBe(CliExitCodes.Success);
        harness.Client.TotalCalls.ShouldBe(0);
        harness.Stdout.ToString().ShouldContain("Quickstart complete");
    }

    [Fact]
    public async Task Execute_DryRun_JsonFormat_EmitsEnvelopeWithAllDryRun()
    {
        using var harness = new Harness(format: OutputFormat.Json);
        int exitCode = await harness.RunAsync(new QuickstartOptions(null, SkipBootCheck: false, SkipPrereqCheck: false, DryRun: true));

        exitCode.ShouldBe(CliExitCodes.Success);

        using JsonDocument document = JsonDocument.Parse(harness.Stdout.ToString());
        document.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("command").GetString().ShouldBe("quickstart");
        JsonElement data = document.RootElement.GetProperty("data");
        data.GetProperty("overallStatus").GetString().ShouldBe("ok");
        data.GetProperty("steps").GetArrayLength().ShouldBe(6);

        foreach (JsonElement step in data.GetProperty("steps").EnumerateArray())
        {
            step.GetProperty("status").GetString().ShouldBe("dry-run");
            step.TryGetProperty("durationMs", out _).ShouldBeTrue();
            step.TryGetProperty("duration", out _).ShouldBeFalse();
        }

        document.RootElement.TryGetProperty("error", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Execute_CancelledBeforeStart_ReturnsCancelledExitCode()
    {
        using var harness = new Harness();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await harness.RunAsync(new QuickstartOptions(null, SkipBootCheck: false, SkipPrereqCheck: false, DryRun: false), cts.Token);

        exitCode.ShouldBe(CliExitCodes.Cancelled);
        harness.Stderr.ToString().ShouldContain("Cancelled.");
    }

    [Fact]
    public async Task Execute_SkipFlags_SkipsSteps1And3()
    {
        using var harness = new Harness();
        harness.Client.AlwaysHealthy = true;
        int exitCode = await harness.RunAsync(new QuickstartOptions(
            TenantId: "unit-test-tenant",
            SkipBootCheck: true,
            SkipPrereqCheck: true,
            DryRun: false));

        exitCode.ShouldBe(CliExitCodes.Success);
        string output = harness.Stdout.ToString();
        output.ShouldContain("[1/6] SKIP:");
        output.ShouldContain("[3/6] SKIP:");
    }

    [Fact]
    public async Task Execute_HappyPath_ReturnsOkWithAllSteps()
    {
        using var harness = new Harness();
        harness.Client.AlwaysHealthy = true;
        int exitCode = await harness.RunAsync(new QuickstartOptions(
            TenantId: "unit-test-tenant",
            SkipBootCheck: true,
            SkipPrereqCheck: true,
            DryRun: false));

        exitCode.ShouldBe(CliExitCodes.Success);
        harness.Client.IngestCalls.ShouldBe(1);
        harness.Client.HybridCalls.ShouldBeGreaterThanOrEqualTo(1); // positive validation
        harness.Client.SearchCalls.ShouldBeGreaterThanOrEqualTo(1); // syntactic canary
    }

    [Fact]
    public async Task Execute_TableFormat_WritesTableToStdoutAndDetailsToStderr()
    {
        using var harness = new Harness(format: OutputFormat.Table);
        int exitCode = await harness.RunAsync(new QuickstartOptions(null, SkipBootCheck: false, SkipPrereqCheck: false, DryRun: true));

        exitCode.ShouldBe(CliExitCodes.Success);
        string stdout = harness.Stdout.ToString();
        stdout.ShouldContain("STEP");
        stdout.ShouldContain("STATUS");
        harness.Stderr.ToString().ShouldContain("[1/6]");
    }

    [Fact]
    public async Task Execute_TransportFailureInStepFive_StaysInsideQuickstartEnvelope()
    {
        using var harness = new Harness(format: OutputFormat.Json);
        harness.Client.AlwaysHealthy = true;
        harness.Client.IngestException = new HttpRequestException(
            "connection refused",
            new SocketException((int)SocketError.ConnectionRefused));

        int exitCode = await harness.RunAsync(new QuickstartOptions(
            TenantId: "unit-test-tenant",
            SkipBootCheck: true,
            SkipPrereqCheck: true,
            DryRun: false));

        exitCode.ShouldBe(CliExitCodes.Plumbing);

        using JsonDocument document = JsonDocument.Parse(harness.Stdout.ToString());
        document.RootElement.TryGetProperty("error", out _).ShouldBeFalse();
        JsonElement data = document.RootElement.GetProperty("data");
        data.GetProperty("overallStatus").GetString().ShouldBe("fail");
        JsonElement steps = data.GetProperty("steps");
        steps[4].GetProperty("status").GetString().ShouldBe("fail");
        steps[4].GetProperty("errorCode").GetString().ShouldBe("CONNECTION_REFUSED");
        steps[5].GetProperty("status").GetString().ShouldBe("skip");
    }

    private sealed class Harness : IDisposable
    {
        public StringWriter Stdout { get; } = new();

        public StringWriter Stderr { get; } = new();

        public StubClient Client { get; }

        private readonly ServiceProvider _provider;
        private readonly CliConsole _console;

        public Harness(OutputFormat format = OutputFormat.Human)
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var client = new StubClient();
            Client = client;

            _console = new CliConsole
            {
                Out = Stdout,
                Error = Stderr,
                Format = format,
            };

            services.AddSingleton(_console);
            services.AddSingleton<CliGlobalOptions>();
            services.AddSingleton<FlagConfigurationSource>();
            services.AddSingleton<IConfigurationSource>(sp => sp.GetRequiredService<FlagConfigurationSource>());
            services.AddSingleton<IConfigurationSource>(new DefaultConfigurationSource());
            services.AddSingleton<ResolvedConfigPipeline>();
            services.AddSingleton<MemoriesClientOptionsMutator>();
            services.AddSingleton<CliCommandExecutor.IOptionsMutator>(sp => sp.GetRequiredService<MemoriesClientOptionsMutator>());
            services.AddSingleton<IOptionsMonitor<MemoriesClientOptions>>(sp =>
            {
                MemoriesClientOptionsMutator mutator = sp.GetRequiredService<MemoriesClientOptionsMutator>();
                return new LiveOptionsMonitor<MemoriesClientOptions>(mutator.Options);
            });

            services.AddSingleton<MemoriesClient>(client);
            services.AddSingleton<CliCommandExecutor>();

            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddSingleton<IProcessRunner>(new FakeProcessRunner());
            services.AddSingleton<PrerequisiteChecks>();
            services.AddSingleton<HealthProbe>();
            services.AddSingleton<QuickstartTenantProvisioner>();
            services.AddSingleton<QuickstartSampleFlow>();

            _provider = services.BuildServiceProvider();
            FlagConfigurationSource flag = _provider.GetRequiredService<FlagConfigurationSource>();
            flag.Endpoint = new Uri("http://127.0.0.1:65000/");
        }

        public Task<int> RunAsync(QuickstartOptions options, CancellationToken ct = default)
            => QuickstartCommand.ExecuteAsync(_provider, options, ct);

        public void Dispose()
        {
            _provider.Dispose();
            Stdout.Dispose();
            Stderr.Dispose();
        }
    }

    private sealed class StubClient : MemoriesClient
    {
        public StubClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65000/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65000/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public bool AlwaysHealthy { get; set; }

        public int HealthCalls { get; private set; }

        public int ListCaseCalls { get; private set; }

        public int CreateCaseCalls { get; private set; }

        public int IngestCalls { get; private set; }

        public int HybridCalls { get; private set; }

        public int SearchCalls { get; private set; }

        public int GetTenantCalls { get; private set; }

        public int CreateTenantCalls { get; private set; }

        public string? LastRunToken { get; private set; }

        public Exception? IngestException { get; set; }

        public int TotalCalls =>
            HealthCalls + ListCaseCalls + CreateCaseCalls + IngestCalls + HybridCalls + SearchCalls + GetTenantCalls + CreateTenantCalls;

        public override Task<bool> ProbeHealthAsync(CancellationToken ct)
        {
            HealthCalls++;
            return Task.FromResult(AlwaysHealthy);
        }

        public override Task<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken ct)
        {
            GetTenantCalls++;
            return Task.FromResult<TenantInfo?>(new TenantInfo(tenantId, "Sample", TenantStatus.Active, DateTimeOffset.UtcNow));
        }

#pragma warning disable HXL001
        public override Task<string> CreateTenantAsync(string tenantId, string displayName, CancellationToken ct)
        {
            CreateTenantCalls++;
            return Task.FromResult("instance-1");
        }

        public override Task<Case> CreateCaseAsync(string tenantId, string name, string? description, CancellationToken ct)
        {
            CreateCaseCalls++;
            var created = new Case(
                "case-stub",
                tenantId,
                name,
                description,
                CaseStatus.Active,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                0);
            return Task.FromResult(created);
        }

        public override Task<string> IngestAsync(
            string tenantId,
            string caseId,
            string sourceUri,
            byte[] content,
            string contentType,
            string ingestedBy,
            IReadOnlyDictionary<string, MetadataField>? metadata,
            CancellationToken ct)
        {
            IngestCalls++;
            if (IngestException is not null)
            {
                throw IngestException;
            }

            if (metadata is not null
                && metadata.TryGetValue("runToken", out MetadataField? runToken)
                && runToken is not null)
            {
                LastRunToken = runToken.Value;
            }

            return Task.FromResult("workflow-1");
        }
#pragma warning restore HXL001

        public override Task<IReadOnlyList<Case>> ListCasesAsync(string tenantId, CancellationToken ct)
        {
            ListCaseCalls++;
            return Task.FromResult<IReadOnlyList<Case>>([]);
        }

        public override Task<HybridSearchResult> HybridSearchAsync(HybridSearchRequest request, CancellationToken ct)
        {
            HybridCalls++;
            string snippet = LastRunToken is null
                ? "snippet"
                : $"Quickstart validation token: {LastRunToken}. (sample snippet)";
            List<FusedScoredResult> results =
            [
                new FusedScoredResult
                {
                    MemoryUnitId = "mu-sample",
                    CompositeScore = 0.9,
                    ContentSnippet = snippet,
                    SourceUri = "quickstart://sample",
                    SourceType = SourceType.File,
                    SyntacticScore = 0.88,
                },
            ];

            return Task.FromResult(new HybridSearchResult
            {
                Results = results,
                TotalCount = results.Count,
                Degraded = false,
                UnavailableAxes = [],
                Query = request.Query,
            });
        }

        public override Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
        {
            SearchCalls++;
            return Task.FromResult(new SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = true,
                Query = request.Query ?? string.Empty,
            });
        }
    }
}
