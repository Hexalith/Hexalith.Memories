// <copyright file="ConsistencyVerifyCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

/// <summary>
/// ATDD RED-phase tests for Story 8.2 — <c>memories consistency verify</c> CLI command.
/// Mirrors the <see cref="StatusTelemetryCommandTests"/> shape: NSubstitute on
/// <c>MemoriesClient</c>, <c>CliCommandExecutor</c> + <c>OutputFormatterRouter</c>
/// stdout capture, exit-code assertions.
/// </summary>
/// <remarks>
/// Skip-gated until Story 8.2 Task 6.1 lands <c>ConsistencyVerifyCommand</c> + formatter
/// registration in <c>CommandPayloadRegistry</c>.
/// </remarks>
public class ConsistencyVerifyCommandTests
{
    // Blueprint — uncomment when target command exists (Task 6.1):
    //
    // using Hexalith.Memories.Cli.Commands;
    // using Hexalith.Memories.Client.Rest;
    // using Hexalith.Memories.Contracts.V1;
    // using NSubstitute;
    // using Shouldly;

    /// <summary>
    /// ATDD RED — Story 8.2 AC #1. Happy path (no <c>--wait</c>): prints the instance ID +
    /// status URL and exits success immediately.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting ConsistencyVerifyCommand (Story 8.2 Task 6.1)")]
    public async Task Run_HappyPath_PrintsInstanceIdAndStatusUrl()
    {
        // Arrange: stub MemoriesClient.StartConsistencyVerificationAsync → returns Uri
        // "/api/tenants/acme/consistency/verify/verify-consistency-acme-GUID".
        // Act: invoke `memories consistency verify --tenant acme`.
        // Expected: exit code 0; stdout contains the instance ID and status URL;
        // MemoriesClient.GetConsistencyVerificationStatusAsync NOT called (no --wait).
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLI-001) — implement ConsistencyVerifyCommand happy path. "
            + "Expected: exit 0; stdout includes instanceId + status URL; no status polling when --wait is absent.");
    }

    /// <summary>
    /// ATDD RED — Story 8.2 AC #1 + AC #8 (progress visibility).
    /// With <c>--wait</c>: polls <c>GetConsistencyVerificationStatusAsync</c> on a 5s interval
    /// (initial) until the workflow reaches <c>Completed</c> / <c>Failed</c>, then prints the
    /// final <c>ConsistencyVerificationResult</c> through the formatter router.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting ConsistencyVerifyCommand (Story 8.2 Task 6.1)")]
    public async Task Run_WithWait_PollsUntilCompletionAndPrintsResult()
    {
        // Arrange: status sequence returns Running, Running, Completed with a ConsistencyVerificationResult.
        // Act: invoke `memories consistency verify --tenant acme --wait`.
        // Expected: MemoriesClient.GetConsistencyVerificationStatusAsync called >= 3 times;
        // stdout contains the Total / Consistent / Inconsistent summary line from the Human formatter.
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLI-002) — implement --wait polling loop. "
            + "Expected: status polled until Completed; final ConsistencyVerificationResult printed "
            + "via OutputFormatterRouter (Human format summary).");
    }

    /// <summary>
    /// ATDD RED — Story 8.2 AC #1.
    /// Missing required <c>--tenant</c> argument → plumbing exit code + JSON error envelope.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting ConsistencyVerifyCommand (Story 8.2 Task 6.1)")]
    public async Task Run_MissingTenant_ReturnsPlumbingExitCodeWithErrorEnvelope()
    {
        // Act: invoke `memories consistency verify` (no --tenant).
        // Expected: exit code = CliExitCodes.ArgumentError (or equivalent plumbing code);
        // stderr contains an error envelope with code describing the missing argument.
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLI-003) — implement missing-argument handling. "
            + "Expected: non-zero exit (plumbing tier); stderr carries the error envelope; "
            + "MemoriesClient never called.");
    }

    /// <summary>
    /// ATDD RED — Story 8.2 AC #1.
    /// <c>--format json</c> emits the <c>ConsistencyVerificationResult</c> as structured JSON
    /// with the expected <c>command</c> / <c>data</c> envelope shape.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting ConsistencyVerifyCommand (Story 8.2 Task 6.1)")]
    public async Task Run_JsonFormat_EmitsVerificationResultEnvelope()
    {
        // Act: invoke `memories consistency verify --tenant acme --wait --format json`.
        // Expected: stdout is valid JSON; root contains "command" = "consistency verify" (or
        // ConsistencyVerifyCommand.CommandName) and "data" = ConsistencyVerificationResult contents.
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLI-004) — implement JSON formatter registration in CommandPayloadRegistry. "
            + "Expected: JSON envelope with command + data keys; data deserializes as ConsistencyVerificationResult.");
    }
}
