// <copyright file="EvidencePacketSanitizationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.TestHelpers.EvidencePackets;

using Shouldly;

/// <summary>
/// Table-driven sanitization coverage (Story 2.7 / CR3). Proves the mapper never lets sensitive
/// diagnostic material reach the packet across the spec'd categories: unauthorized, backend failure,
/// partial degradation, token-budget compression, and raw server diagnostics. Each category is a
/// scenario; the rows enumerate the secret shapes the mapper must refuse to forward.
/// </summary>
public sealed class EvidencePacketSanitizationTests
{
    private const string Fallback = "Retry the authorized request or inspect service health.";

    private static EvidencePacketScope AuthorizedScope => EvidencePacketCanonicalFixtures.AuthorizedScope;

    public static TheoryData<string, string> SensitiveSuggestions() => new()
    {
        { "Reconnect using Bearer abc123def456ghi789jkl012mno345pqr678 and retry.", "Bearer abc123def456ghi789jkl012mno345pqr678" },
        { "Inspect redis://backend-host:6379/0 before retrying.", "redis://backend-host" },
        { "Restart falkordb-primary then retry.", "falkordb-primary" },
        { "See the log at C:\\secret\\trace.txt for details.", "C:\\secret" },
        { "Read /home/svc/secret.log for the failure.", "/home/svc/secret.log" },
        { "Read /users/svc/secret.log for the failure.", "/users/svc/secret.log" },
        { "Server stack trace was written to the host log.", "stack trace" },
        { "Failure at Hexalith.Server.SearchEndpoint.Handle in the pipeline.", "at Hexalith.Server.SearchEndpoint" },
        { "Token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload is expired.", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." },
        { "Object hash deadbeefdeadbeefdeadbeefdeadbeef0001 mismatch on retry.", "deadbeefdeadbeefdeadbeefdeadbeef0001" },
        { "Reconnect to postgres://svc:pw@db-primary:5432/memories and retry.", "postgres://svc" },
        { "Inspect mongodb://svc:pw@mongo-primary:27017 before retrying.", "mongodb://svc" },
        { "Graph backend bolt://neo4j-primary:7687 is unreachable.", "bolt://neo4j-primary" },
        { "Copy logs from \\\\fileserver\\share\\trace.log first.", "\\\\fileserver\\share" },
        { "See /var/log/memories/trace.log for the failure.", "/var/log/memories" },
        { "Read /etc/memories/secrets.conf for credentials.", "/etc/memories" },
        { "Connection used Server=db;Password=hunter2;Database=app.", "Password=hunter2" },
    };

    [Theory]
    [MemberData(nameof(SensitiveSuggestions))]
    public void FromError_NonUnauthorized_ShouldReplaceSensitiveSuggestionAndNeverLeak(string suggestion, string leakMarker)
    {
        // BACKEND_DEGRADED routes to the Retry branch where SanitizeGuidance runs against error.Suggestion.
        var error = new ErrorResponse("BACKEND_DEGRADED", "Backend is degraded.", suggestion);

        EvidencePacket packet = EvidencePacketMapper.FromError(error, AuthorizedScope, query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.Recovery.ShouldHaveSingleItem();
        packet.Recovery[0].Guidance.ShouldBe(Fallback);

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain(leakMarker, Shouldly.Case.Sensitive);
    }

    [Theory]
    [MemberData(nameof(SensitiveSuggestions))]
    public void FromError_Unauthorized_ShouldNeverCopyErrorFieldsRegardlessOfPayload(string suggestion, string leakMarker)
    {
        // The unauthorized branch hardcodes recovery and must never copy error.Message/error.Suggestion,
        // even when SanitizeGuidance would otherwise have caught the same payload.
        var error = new ErrorResponse(
            "TENANT_FORBIDDEN",
            $"Denied for tenant-b. {suggestion}",
            suggestion);

        EvidencePacket packet = EvidencePacketMapper.FromError(error, AuthorizedScope, query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Unauthorized);
        packet.Recovery.ShouldHaveSingleItem();
        packet.Recovery[0].Guidance.ShouldBe("Use an authorized tenant and case scope.");

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain(leakMarker, Shouldly.Case.Sensitive);
        json.ShouldNotContain("tenant-b", Shouldly.Case.Sensitive);
    }

    [Theory]
    [InlineData("Increase the token budget and retry.")]
    [InlineData("Broaden your query terms and retry.")]
    [InlineData("Re-run the request with a larger maxResults value.")]
    [InlineData("Look at the logs and retry the authorized request.")]
    [InlineData("We arrived at noon; retry afterward.")]
    public void FromError_NonUnauthorized_ShouldPreserveBenignGuidance(string suggestion)
    {
        // Regression guard for the over-broad sanitization regex: benign operator prose must survive,
        // including ordinary "at <word>" phrasing that must NOT trip the stack-frame pattern.
        var error = new ErrorResponse("BACKEND_DEGRADED", "Backend is degraded.", suggestion);

        EvidencePacket packet = EvidencePacketMapper.FromError(error, AuthorizedScope, query: "claim denied");

        packet.Recovery[0].Guidance.ShouldBe(suggestion);
    }

    [Theory]
    [InlineData("Inspect redis://backend-host:6379/0 then retry.")]
    [InlineData("Reconnect using Bearer abc123def456ghi789jkl012mno345pqr678.")]
    [InlineData("See the log at C:\\secret\\trace.txt.")]
    [InlineData("Connect via postgres://svc:pw@db-primary:5432/app.")]
    [InlineData("Copy from \\\\fileserver\\share\\trace.log.")]
    [InlineData("Read /var/log/memories/trace.log.")]
    [InlineData("Connection used Server=db;Password=hunter2.")]
    [InlineData("Failure at Hexalith.Server.SearchEndpoint.Handle.")]
    public void SanitizeFreeText_SensitiveInput_ShouldReturnFallback(string value)
    {
        // SanitizeFreeText is the public entry point CLI/MCP surfaces call to scrub server-sourced text.
        EvidencePacketMapper.SanitizeFreeText(value, Fallback).ShouldBe(Fallback);
    }

    [Theory]
    [InlineData("Retry the authorized request.")]
    [InlineData("Increase the token budget and retry.")]
    [InlineData("Look at the logs and broaden your query.")]
    [InlineData("We arrived at noon; retry afterward.")]
    public void SanitizeFreeText_BenignInput_ShouldPreserveValue(string value)
    {
        EvidencePacketMapper.SanitizeFreeText(value, Fallback).ShouldBe(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeFreeText_NullOrWhitespace_ShouldReturnFallback(string? value)
    {
        EvidencePacketMapper.SanitizeFreeText(value, Fallback).ShouldBe(Fallback);
    }

    [Fact]
    public void FromError_EmptySuggestion_ShouldUseFallback()
    {
        var error = new ErrorResponse("BACKEND_DEGRADED", "Backend is degraded.", string.Empty);

        EvidencePacket packet = EvidencePacketMapper.FromError(error, AuthorizedScope, query: "claim denied");

        packet.Recovery[0].Guidance.ShouldBe(Fallback);
    }

    [Fact]
    public void FromError_AllBackendFailure_ShouldNotLeakInternalDiagnostics()
    {
        // Scenario: server reports a total backend outage with a diagnostic-laden suggestion.
        var error = new ErrorResponse(
            "BACKEND_UNAVAILABLE",
            "All search backends are unavailable.",
            "redis://primary:6379 and falkordb-primary are both down; see stack trace in host log.");

        EvidencePacket packet = EvidencePacketMapper.FromError(error, AuthorizedScope, query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.BackendUnavailable);

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain("redis://primary", Shouldly.Case.Sensitive);
        json.ShouldNotContain("falkordb-primary", Shouldly.Case.Sensitive);
        json.ShouldNotContain("stack trace", Shouldly.Case.Sensitive);
    }
}
