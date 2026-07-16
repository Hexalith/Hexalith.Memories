// <copyright file="ConsistencyVerifyCommandTests.cs" company="ITANEO">
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

/// <summary>Story 8.2 — <c>memories consistency verify</c> CLI coverage.</summary>
public sealed class ConsistencyVerifyCommandTests
{
    [Fact]
    public async Task Run_HappyPath_PrintsInstanceIdAndStatusUrl()
    {
        ConsistencyStubClient stub = new() { VerifyInstanceId = "verify-consistency-acme-abc" };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["verify", "--tenant", "acme"]);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("verify-consistency-acme-abc");
        stdout.ToString().ShouldContain("Workflow scheduled: verify");
        stderr.ToString().ShouldBeEmpty();
        stub.VerifyStatusCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Run_WithWait_PollsUntilCompletionAndPrintsResult()
    {
        ConsistencyStubClient stub = new()
        {
            VerifyInstanceId = "verify-consistency-acme-abc",
            VerifyStatusSequence =
            [
                CreateVerificationStatus("verify-consistency-acme-abc", "Running", result: null),
                CreateVerificationStatus(
                    "verify-consistency-acme-abc",
                    "Completed",
                    new ConsistencyVerificationResult(
                        "acme",
                        TotalUnits: 3,
                        ConsistentCount: 2,
                        InconsistentCount: 1,
                        Discrepancies:
                        [
                            new ConsistencyDiscrepancy(
                                "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
                                SyntacticPresent: true,
                                SemanticPresent: false,
                                GraphPresent: true,
                                ConsistencyRepairRecommendation.ReIndexSemantic),
                        ],
                        TotalDiscrepancyCount: 1,
                        TruncatedAt: null,
                        EnumerationTruncated: false,
                        StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
                        CompletedAt: DateTimeOffset.UtcNow,
                        Duration: TimeSpan.FromSeconds(5))),
            ],
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["verify", "--tenant", "acme", "--wait"]);

        exit.ShouldBe(CliExitCodes.Success);
        stub.VerifyStatusCalls.ShouldBeGreaterThan(0);
        stdout.ToString().ShouldContain("Consistency verification completed");
        stdout.ToString().ShouldContain("inconsistent units");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_WithWait_NoteOnlyResult_PrintsNotesSection()
    {
        ConsistencyStubClient stub = new()
        {
            VerifyInstanceId = "verify-consistency-acme-notes",
            VerifyStatusSequence =
            [
                CreateVerificationStatus(
                    "verify-consistency-acme-notes",
                    "Completed",
                    new ConsistencyVerificationResult(
                        "acme",
                        TotalUnits: 1,
                        ConsistentCount: 1,
                        InconsistentCount: 0,
                        Discrepancies: [],
                        TotalDiscrepancyCount: 0,
                        TruncatedAt: null,
                        EnumerationTruncated: false,
                        StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
                        CompletedAt: DateTimeOffset.UtcNow,
                        Duration: TimeSpan.FromSeconds(1))
                    {
                        NoteCount = 1,
                        TotalNoteCount = 1,
                        Notes =
                        [
                            new ConsistencyDiscrepancy(
                                "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
                                SyntacticPresent: true,
                                SemanticPresent: true,
                                GraphPresent: true,
                                ConsistencyRepairRecommendation.NoOp)
                            {
                                NaturalLanguageSemanticPresent = false,
                                NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.Indexed,
                                ConsistencyNoteKind = ConsistencyNoteKind.NaturalLanguageEmbeddingMissing,
                                ConsistencyNote = "Missing backends: semantic-nl",
                            },
                        ],
                    }),
            ],
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["verify", "--tenant", "acme", "--wait"]);

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();
        output.ShouldContain("note-only units");
        output.ShouldContain("Notes:");
        output.ShouldContain("NaturalLanguageEmbeddingMissing");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_MissingTenant_ReturnsPlumbingExitCode()
    {
        ConsistencyStubClient stub = new();
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Human, stub);

        // System.CommandLine catches missing required options before our handler runs —
        // it returns non-zero with a diagnostic on stderr (exit code 1 on enforcement).
        int exit = await InvokeAsync(services, ["verify"]);

        exit.ShouldNotBe(CliExitCodes.Success);
        stub.VerifyStartCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Run_JsonFormat_EmitsReceiptEnvelope()
    {
        ConsistencyStubClient stub = new() { VerifyInstanceId = "verify-consistency-acme-xyz" };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Json, stub);

        int exit = await InvokeAsync(services, ["verify", "--tenant", "acme"]);

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe(ConsistencyVerifyCommand.CommandName);
        doc.RootElement.GetProperty("data").GetProperty("workflowInstanceId").GetString().ShouldBe("verify-consistency-acme-xyz");
        doc.RootElement.GetProperty("data").GetProperty("kind").GetString().ShouldBe("verify");
        doc.RootElement.GetProperty("data").GetProperty("statusUrl").GetString().ShouldEndWith("/api/v1/tenants/acme/consistency/verify/verify-consistency-acme-xyz");
    }

    [Fact]
    public async Task Run_JsonFormatWithWait_EmitsResultEnvelope()
    {
        ConsistencyStubClient stub = new()
        {
            VerifyInstanceId = "verify-consistency-acme-json",
            VerifyStatusSequence =
            [
                CreateVerificationStatus(
                    "verify-consistency-acme-json",
                    "Completed",
                    new ConsistencyVerificationResult(
                        "acme",
                        TotalUnits: 1,
                        ConsistentCount: 1,
                        InconsistentCount: 0,
                        Discrepancies: [],
                        TotalDiscrepancyCount: 0,
                        TruncatedAt: null,
                        EnumerationTruncated: false,
                        StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
                        CompletedAt: DateTimeOffset.UtcNow,
                        Duration: TimeSpan.FromSeconds(1))),
            ],
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Json, stub);

        int exit = await InvokeAsync(services, ["verify", "--tenant", "acme", "--wait"]);

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe(ConsistencyVerifyCommand.CommandName);
        doc.RootElement.GetProperty("data").GetProperty("totalUnits").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("data").GetProperty("inconsistentCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Run_JsonFormatWithWait_NoteOnlyResult_EmitsNoteFields()
    {
        ConsistencyStubClient stub = new()
        {
            VerifyInstanceId = "verify-consistency-acme-json-notes",
            VerifyStatusSequence =
            [
                CreateVerificationStatus(
                    "verify-consistency-acme-json-notes",
                    "Completed",
                    new ConsistencyVerificationResult(
                        "acme",
                        TotalUnits: 1,
                        ConsistentCount: 1,
                        InconsistentCount: 0,
                        Discrepancies: [],
                        TotalDiscrepancyCount: 0,
                        TruncatedAt: null,
                        EnumerationTruncated: false,
                        StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
                        CompletedAt: DateTimeOffset.UtcNow,
                        Duration: TimeSpan.FromSeconds(1))
                    {
                        NoteCount = 1,
                        TotalNoteCount = 1,
                        Notes =
                        [
                            new ConsistencyDiscrepancy(
                                "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
                                SyntacticPresent: true,
                                SemanticPresent: true,
                                GraphPresent: true,
                                ConsistencyRepairRecommendation.NoOp)
                            {
                                ConsistencyNoteKind = ConsistencyNoteKind.NaturalLanguageEmbeddingQueued,
                                ConsistencyNote = "Natural-language semantic hash pending queued retry.",
                            },
                        ],
                    }),
            ],
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Json, stub);

        int exit = await InvokeAsync(services, ["verify", "--tenant", "acme", "--wait"]);

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        doc.RootElement.GetProperty("data").GetProperty("noteCount").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("data").GetProperty("totalNoteCount").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("data").GetProperty("notes")[0].GetProperty("consistencyNoteKind").GetString().ShouldBe("naturalLanguageEmbeddingQueued");
    }

    [Fact]
    public async Task Run_TableFormatWithWait_NoteOnlyResult_IncludesNotesColumn()
    {
        ConsistencyStubClient stub = new()
        {
            VerifyInstanceId = "verify-consistency-acme-table-notes",
            VerifyStatusSequence =
            [
                CreateVerificationStatus(
                    "verify-consistency-acme-table-notes",
                    "Completed",
                    new ConsistencyVerificationResult(
                        "acme",
                        TotalUnits: 1,
                        ConsistentCount: 1,
                        InconsistentCount: 0,
                        Discrepancies: [],
                        TotalDiscrepancyCount: 0,
                        TruncatedAt: null,
                        EnumerationTruncated: false,
                        StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
                        CompletedAt: DateTimeOffset.UtcNow,
                        Duration: TimeSpan.FromSeconds(1))
                    {
                        NoteCount = 1,
                        TotalNoteCount = 1,
                    }),
            ],
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            BuildServices(OutputFormat.Table, stub);

        int exit = await InvokeAsync(services, ["verify", "--tenant", "acme", "--wait"]);

        exit.ShouldBe(CliExitCodes.Success);
        string output = stdout.ToString();
        output.ShouldContain("NOTES");
        output.ShouldContain("DISCREPANCIES");
        stderr.ToString().ShouldBeEmpty();
    }

    private static async Task<int> InvokeAsync(IServiceProvider services, string[] args)
    {
        var root = new System.CommandLine.Command("consistency");
        root.Subcommands.Add(ConsistencyVerifyCommand.Build(services));
        return await root.Parse(args).InvokeAsync();
    }

    internal static (IServiceProvider Services, StringWriter Stdout, StringWriter Stderr) BuildServices(
        OutputFormat format,
        ConsistencyStubClient stubClient,
        bool isInteractive = false,
        string? stdin = null)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole
        {
            In = new StringReader(stdin ?? string.Empty),
            Out = stdout,
            Error = stderr,
            Format = format,
            IsInteractive = isInteractive,
        });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ => stubClient));

        // Collapse the --wait polling interval so tests don't pay a real 5-second delay per
        // non-terminal status. Production default is 5s; tests verify loop semantics, not cadence.
        collection.Configure<ConsistencyPollOptions>(o => o.PollInterval = TimeSpan.Zero);

        ServiceProvider provider = collection.BuildServiceProvider();
        FlagConfigurationSource flag = provider.GetRequiredService<FlagConfigurationSource>();
        flag.Endpoint = new Uri("http://127.0.0.1:65001/");
        return (provider, stdout, stderr);
    }

    internal static ConsistencyVerificationStatus CreateVerificationStatus(
        string instanceId,
        string status,
        ConsistencyVerificationResult? result)
        => new(
            instanceId,
            status,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            result is null ? new ConsistencyWorkflowProgress("verifying", 1, 2) : new ConsistencyWorkflowProgress("completed", 1, 1),
            result);

    internal static ConsistencyRepairStatus CreateRepairStatus(
        string instanceId,
        string status,
        ConsistencyRepairResult? result)
        => new(
            instanceId,
            status,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            result is null ? new ConsistencyWorkflowProgress("repairing", 1, 2) : new ConsistencyWorkflowProgress("completed", 1, 1),
            result);
}

/// <summary>
/// Test double for <see cref="MemoriesClient"/> that records calls + produces scripted
/// responses. Shared across the three consistency CLI test classes.
/// </summary>
internal sealed class ConsistencyStubClient : MemoriesClient
{
    public ConsistencyStubClient()
        : base(
            new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
            Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
            NullLogger<MemoriesClient>.Instance)
    {
    }

    public string VerifyInstanceId { get; set; } = "verify-consistency-acme-stub";

    public string RepairInstanceId { get; set; } = "repair-consistency-acme-stub";

    public int VerifyStartCalls { get; private set; }

    public int RepairStartCalls { get; private set; }

    public int VerifyStatusCalls { get; private set; }

    public int RepairStatusCalls { get; private set; }

    public ConsistencyVerificationRequest? LastVerifyRequest { get; private set; }

    public ConsistencyRepairRequest? LastRepairRequest { get; private set; }

    public ConsistencyInspectionResult InspectionResponse { get; set; } = new(
        "tenant-stub",
        "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
        SyntacticPresent: true,
        SemanticPresent: true,
        GraphPresent: true,
        SyntacticDetail: new ConsistencySyntacticDetail(
            "hash", DateTimeOffset.UtcNow, "file:///x", "file", "case-1", "gemini", "gemini-embedding-001"),
        SemanticDetail: new ConsistencySemanticDetail(768, "tenant-stub:vec:01HM5Q9WXGK6T8Q4Z5Y6V7W8X9"),
        GraphDetail: new ConsistencyGraphDetail(1, 2, 1),
        Recommendation: ConsistencyRepairRecommendation.NoOp,
        CheckedAt: DateTimeOffset.UtcNow);

    public Exception? InspectionException { get; set; }

    public IReadOnlyList<ConsistencyVerificationStatus>? VerifyStatusSequence { get; set; }

    public IReadOnlyList<ConsistencyRepairStatus>? RepairStatusSequence { get; set; }

    public override Task<Uri> StartConsistencyVerificationAsync(
        string tenantId, ConsistencyVerificationRequest? request, CancellationToken ct)
    {
        VerifyStartCalls++;
        LastVerifyRequest = request;
        return Task.FromResult(new Uri($"http://127.0.0.1:65001/api/v1/tenants/{tenantId}/consistency/verify/{VerifyInstanceId}"));
    }

    public override Task<ConsistencyVerificationStatus?> GetConsistencyVerificationStatusAsync(
        string tenantId, string instanceId, CancellationToken ct)
    {
        VerifyStatusCalls++;
        ConsistencyVerificationStatus? state = VerifyStatusSequence is { Count: > 0 } seq
            ? seq[Math.Min(VerifyStatusCalls - 1, seq.Count - 1)]
            : ConsistencyVerifyCommandTests.CreateVerificationStatus(
                instanceId,
                "Completed",
                new ConsistencyVerificationResult(
                    tenantId,
                    TotalUnits: 0,
                    ConsistentCount: 0,
                    InconsistentCount: 0,
                    Discrepancies: [],
                    TotalDiscrepancyCount: 0,
                    TruncatedAt: null,
                    EnumerationTruncated: false,
                    StartedAt: DateTimeOffset.UtcNow.AddSeconds(-1),
                    CompletedAt: DateTimeOffset.UtcNow,
                    Duration: TimeSpan.FromSeconds(1)));
        return Task.FromResult<ConsistencyVerificationStatus?>(state);
    }

    public override Task<ConsistencyInspectionResult> InspectConsistencyAsync(
        string tenantId, string memoryUnitId, CancellationToken ct)
    {
        if (InspectionException is not null)
        {
            throw InspectionException;
        }

        return Task.FromResult(InspectionResponse);
    }

    public override Task<Uri> StartConsistencyRepairAsync(
        string tenantId, ConsistencyRepairRequest? request, CancellationToken ct)
    {
        RepairStartCalls++;
        LastRepairRequest = request;
        return Task.FromResult(new Uri($"http://127.0.0.1:65001/api/v1/tenants/{tenantId}/consistency/repair/{RepairInstanceId}"));
    }

    public override Task<ConsistencyRepairStatus?> GetConsistencyRepairStatusAsync(
        string tenantId, string instanceId, CancellationToken ct)
    {
        RepairStatusCalls++;
        ConsistencyRepairStatus? state = RepairStatusSequence is { Count: > 0 } seq
            ? seq[Math.Min(RepairStatusCalls - 1, seq.Count - 1)]
            : ConsistencyVerifyCommandTests.CreateRepairStatus(
                instanceId,
                "Completed",
                new ConsistencyRepairResult(
                    tenantId,
                    TotalDiscrepancies: 0,
                    RepairedCount: 0,
                    UnrepairableCount: 0,
                    Actions: [],
                    PassesExecuted: 1,
                    StartedAt: DateTimeOffset.UtcNow.AddSeconds(-1),
                    CompletedAt: DateTimeOffset.UtcNow,
                    Duration: TimeSpan.FromSeconds(1)));
        return Task.FromResult<ConsistencyRepairStatus?>(state);
    }
}
