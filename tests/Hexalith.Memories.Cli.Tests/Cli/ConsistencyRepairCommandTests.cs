// <copyright file="ConsistencyRepairCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 8.2 — <c>memories consistency repair</c> CLI coverage.</summary>
public sealed class ConsistencyRepairCommandTests
{
    [Fact]
    public async Task Run_NonTtyWithoutYes_FailsPlumbingWithSafetyEnvelope()
    {
        ConsistencyStubClient stub = new();
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["repair", "--tenant", "acme"]);

        exit.ShouldBe(CliExitCodes.Plumbing);
        stderr.ToString().ShouldContain("CONFIRMATION_REQUIRED");
        stub.RepairStartCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Run_InteractiveWithoutYes_PromptsAndSchedulesRepair()
    {
        ConsistencyStubClient stub = new() { RepairInstanceId = "repair-consistency-acme-confirm" };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub, isInteractive: true, stdin: "y\n");

        int exit = await InvokeAsync(services, ["repair", "--tenant", "acme"]);

        exit.ShouldBe(CliExitCodes.Success);
        stub.RepairStartCalls.ShouldBe(1);
        stderr.ToString().ShouldContain("Repair tenant 'acme' now?");
        stdout.ToString().ShouldContain("repair-consistency-acme-confirm");
    }

    [Fact]
    public async Task Run_HappyPathWithYes_PrintsInstanceIdAndStatusUrl()
    {
        ConsistencyStubClient stub = new() { RepairInstanceId = "repair-consistency-acme-abc" };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["repair", "--tenant", "acme", "--yes"]);

        exit.ShouldBe(CliExitCodes.Success);
        stub.RepairStartCalls.ShouldBe(1);
        stdout.ToString().ShouldContain("repair-consistency-acme-abc");
        stdout.ToString().ShouldContain("Workflow scheduled: repair");
    }

    [Fact]
    public async Task Run_JsonFormat_EmitsRepairReceiptCommandName()
    {
        ConsistencyStubClient stub = new() { RepairInstanceId = "repair-consistency-acme-json" };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Json, stub);

        int exit = await InvokeAsync(services, ["repair", "--tenant", "acme", "--yes"]);

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        using JsonDocument document = JsonDocument.Parse(stdout.ToString());
        document.RootElement.GetProperty("command").GetString().ShouldBe(ConsistencyRepairCommand.CommandName);
        document.RootElement.GetProperty("data").GetProperty("kind").GetString().ShouldBe("repair");
    }

    [Fact]
    public async Task Run_WithWaitAndYes_PollsUntilCompletionAndPrintsResult()
    {
        ConsistencyStubClient stub = new()
        {
            RepairInstanceId = "repair-consistency-acme-wait",
            RepairStatusSequence =
            [
                ConsistencyVerifyCommandTests.CreateRepairStatus(
                    "repair-consistency-acme-wait",
                    "Completed",
                    new ConsistencyRepairResult(
                        "acme",
                        TotalDiscrepancies: 3,
                        RepairedCount: 2,
                        UnrepairableCount: 1,
                        Actions:
                        [
                            new RepairActionRecord(
                                "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
                                ConsistencyRepairRecommendation.ReIndexSemantic,
                                Succeeded: true,
                                FailureReason: null,
                                BeforeState: new Dictionary<string, string>(),
                                AfterState: new Dictionary<string, string>()),
                        ],
                            PassesExecuted: 2,
                        StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
                            CompletedAt: DateTimeOffset.UtcNow,
                            Duration: TimeSpan.FromSeconds(4))),
            ],
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["repair", "--tenant", "acme", "--yes", "--wait"]);

        exit.ShouldBe(CliExitCodes.Success);
        stub.RepairStatusCalls.ShouldBeGreaterThan(0);
        stdout.ToString().ShouldContain("Consistency repair completed");
        stdout.ToString().ShouldContain("repaired count");
    }

    [Fact]
    public async Task Run_JsonFormatWithWait_EmitsRepairResultCommandName()
    {
        ConsistencyStubClient stub = new()
        {
            RepairInstanceId = "repair-consistency-acme-json-wait",
            RepairStatusSequence =
            [
                ConsistencyVerifyCommandTests.CreateRepairStatus(
                    "repair-consistency-acme-json-wait",
                    "Completed",
                    new ConsistencyRepairResult(
                        "acme",
                        TotalDiscrepancies: 1,
                        RepairedCount: 1,
                        UnrepairableCount: 0,
                        Actions: [],
                        PassesExecuted: 1,
                        StartedAt: DateTimeOffset.UtcNow.AddSeconds(-1),
                        CompletedAt: DateTimeOffset.UtcNow,
                        Duration: TimeSpan.FromSeconds(1))),
            ],
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Json, stub);

        int exit = await InvokeAsync(services, ["repair", "--tenant", "acme", "--yes", "--wait"]);

        exit.ShouldBe(CliExitCodes.Success);
        stderr.ToString().ShouldBeEmpty();
        using JsonDocument document = JsonDocument.Parse(stdout.ToString());
        document.RootElement.GetProperty("command").GetString().ShouldBe(ConsistencyRepairCommand.CommandName);
        document.RootElement.GetProperty("data").GetProperty("repairedCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Run_IncludeUnrepairableFlag_ForwardedToRequest()
    {
        ConsistencyStubClient stub = new();
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["repair", "--tenant", "acme", "--yes", "--include-unrepairable", "--batch-size", "250"]);

        exit.ShouldBe(CliExitCodes.Success);
        stub.LastRepairRequest.ShouldNotBeNull();
        stub.LastRepairRequest.IncludeUnrepairable.ShouldBeTrue();
        stub.LastRepairRequest.BatchSize.ShouldBe(250);
    }

    private static async Task<int> InvokeAsync(IServiceProvider services, string[] args)
    {
        var root = new System.CommandLine.Command("consistency");
        root.Subcommands.Add(ConsistencyRepairCommand.Build(services));
        return await root.Parse(args).InvokeAsync();
    }
}
