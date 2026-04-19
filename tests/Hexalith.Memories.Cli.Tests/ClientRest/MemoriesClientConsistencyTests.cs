// <copyright file="MemoriesClientConsistencyTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

/// <summary>
/// ATDD RED-phase seminal tests for Story 8.2 — <c>MemoriesClient</c> consistency methods
/// (Task 5). Five tests mirror the existing <see cref="MemoriesClientTests"/> pattern
/// (<c>TestDelegatingHandler</c> + <c>MemoriesJsonContext.Options</c>).
/// </summary>
/// <remarks>
/// Story AC #9 references a <c>Hexalith.Memories.Client.Rest.Tests</c> project that does
/// not exist today — new client tests are colocated here alongside
/// <see cref="MemoriesClientTests"/> to match the project's current convention. Flag for
/// the dev agent if the separate project is preferred.
///
/// Skip-gated until Story 8.2 Task 5 lands the five client methods
/// (<c>StartConsistencyVerificationAsync</c>, <c>GetConsistencyVerificationStatusAsync</c>,
/// <c>InspectConsistencyAsync</c>, <c>StartConsistencyRepairAsync</c>,
/// <c>GetConsistencyRepairStatusAsync</c>).
/// </remarks>
public class MemoriesClientConsistencyTests
{
    // Blueprint — uncomment when target client methods exist (Task 5.1):
    //
    // using System.Net;
    // using System.Text;
    // using System.Text.Json;
    // using Hexalith.Memories.Client.Rest;
    // using Hexalith.Memories.Contracts.V1;
    // using Microsoft.Extensions.Logging.Abstractions;
    // using Microsoft.Extensions.Options;
    // using Shouldly;
    //
    // private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    /// <summary>
    /// ATDD RED — Story 8.2 AC #1 + Task 5.2.
    /// <c>StartConsistencyVerificationAsync</c> parses the <c>Location</c> header from a
    /// <c>202 Accepted</c> response and returns it as the status <see cref="Uri"/>.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting MemoriesClient.StartConsistencyVerificationAsync (Story 8.2 Task 5.1)")]
    public async Task StartConsistencyVerificationAsync_202Response_ParsesLocationHeader()
    {
        // Seed: handler returns 202 with Location = /api/tenants/t1/consistency/verify/verify-consistency-t1-GUID.
        // Expected: returned Uri matches the Location header.
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLIENT-001) — implement StartConsistencyVerificationAsync. "
            + "Expected: returns Uri from 202 response Location header; throws MemoriesRemoteException "
            + "with INVALID_RESPONSE when Location is missing.");
    }

    /// <summary>
    /// ATDD RED — Story 8.2 AC #1.
    /// <c>GetConsistencyVerificationStatusAsync</c> deserializes the <c>WorkflowState</c>
    /// body via <c>MemoriesJsonContext.Options</c>.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting MemoriesClient.GetConsistencyVerificationStatusAsync (Story 8.2 Task 5.1)")]
    public async Task GetConsistencyVerificationStatusAsync_200Response_DeserializesWorkflowState()
    {
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLIENT-002) — implement GetConsistencyVerificationStatusAsync. "
            + "Expected: deserialize WorkflowState (or ConsistencyWorkflowState projection) from 200 body "
            + "using MemoriesJsonContext.Options.");
    }

    /// <summary>
    /// ATDD RED — Story 8.2 AC #3.
    /// <c>InspectConsistencyAsync</c> deserializes a <c>ConsistencyInspectionResult</c>
    /// from a 200 response.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting MemoriesClient.InspectConsistencyAsync (Story 8.2 Task 5.1)")]
    public async Task InspectConsistencyAsync_200Response_DeserializesInspectionResult()
    {
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLIENT-003) — implement InspectConsistencyAsync. "
            + "Expected: 200 OK → ConsistencyInspectionResult; 404 → MemoriesRemoteException with code "
            + "MEMORY_UNIT_NOT_FOUND; 400 → MemoriesRemoteException with code INVALID_MEMORY_UNIT_ID.");
    }

    /// <summary>
    /// ATDD RED — Story 8.2 AC #4.
    /// <c>StartConsistencyRepairAsync</c> parses the repair <c>Location</c> header.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting MemoriesClient.StartConsistencyRepairAsync (Story 8.2 Task 5.1)")]
    public async Task StartConsistencyRepairAsync_202Response_ParsesLocationHeader()
    {
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLIENT-004) — implement StartConsistencyRepairAsync. "
            + "Expected: returns Uri from 202 response Location header; path prefix includes /consistency/repair/.");
    }

    /// <summary>
    /// ATDD RED — Story 8.2 AC #4.
    /// <c>GetConsistencyRepairStatusAsync</c> deserializes repair <c>WorkflowState</c>.
    /// </summary>
    [Fact(Skip = "ATDD RED — awaiting MemoriesClient.GetConsistencyRepairStatusAsync (Story 8.2 Task 5.1)")]
    public async Task GetConsistencyRepairStatusAsync_200Response_DeserializesWorkflowState()
    {
        await Task.Yield();
        Assert.Fail(
            "ATDD RED (8.2-CLIENT-005) — implement GetConsistencyRepairStatusAsync. "
            + "Expected: deserialize WorkflowState from 200 body; error-path parity with the verify status method.");
    }
}
