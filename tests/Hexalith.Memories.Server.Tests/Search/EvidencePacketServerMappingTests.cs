// <copyright file="EvidencePacketServerMappingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.EvidencePackets;

using Shouldly;

/// <summary>
/// Server-side Evidence Packet mapping coverage (Story 2.7 / CR5). Drives the real
/// <see cref="SearchResponseMetadataApplier"/> so the metadata the server actually emits (token-budget
/// truncation, degraded state, axes-used) is proven to map into the canonical packet. Also pins
/// cross-surface parity (CR1) against the shared canonical JSON from the server assembly's view of the
/// contract mapper.
/// </summary>
public sealed class EvidencePacketServerMappingTests
{
    private static EvidencePacketScope AuthorizedScope => EvidencePacketCanonicalFixtures.AuthorizedScope;

    [Fact]
    public void ServerApplied_Complete_ShouldMapScopeAndStrongEvidence()
    {
        SearchResult serverResult = SearchResponseMetadataApplier.ApplySearch(
            BuildResult(resultCount: 1, totalCount: 1),
            axisName: "semantic",
            budget: null);

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(serverResult, AuthorizedScope);

        packet.State.ShouldBe(EvidencePacketState.Complete);
        packet.Scope.TenantId.ShouldBe("tenant-a");
        packet.Scope.CaseId.ShouldBe("case-a");
        packet.Evidence.AxesUsed.ShouldBe(["semantic"]);
        packet.Evidence.EvidenceStrength.ShouldBe(EvidencePacketEvidenceStrength.Strong);
    }

    [Fact]
    public void ServerApplied_Empty_ShouldMapEmptyStateWithBroadenRecovery()
    {
        SearchResult serverResult = SearchResponseMetadataApplier.ApplySearch(
            BuildResult(resultCount: 0, totalCount: 0),
            axisName: "semantic",
            budget: null);

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(serverResult, AuthorizedScope);

        packet.State.ShouldBe(EvidencePacketState.Empty);
        packet.Sources.ShouldBeEmpty();
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.BroadenScope);
    }

    [Fact]
    public void ServerApplied_PartialDegradation_ShouldMapDegradedStateAndUnavailableAxis()
    {
        SearchResult serverResult = SearchResponseMetadataApplier.ApplySearch(
            BuildResult(resultCount: 1, totalCount: 1),
            axisName: "semantic",
            budget: null,
            degraded: true,
            unavailableAxes: ["graph"]);

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(serverResult, AuthorizedScope);

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.Evidence.Degraded.ShouldBeTrue();
        packet.Evidence.UnavailableAxes.ShouldBe(["graph"]);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.BackendUnavailable);
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.InspectBackendHealth);
    }

    [Fact]
    public void ServerApplied_TokenBudget_ShouldMapPendingExpansionWithScopedHandle()
    {
        // Three ~34-token rows with a 40-token budget keep rank 1 and omit the rest (CR5 token-budget metadata).
        SearchResult serverResult = SearchResponseMetadataApplier.ApplySearch(
            BuildResult(resultCount: 3, totalCount: 3),
            axisName: "semantic",
            budget: 40);

        serverResult.OmittedCount.ShouldBeGreaterThan(0);
        serverResult.OmittedReason.ShouldBe(OmittedReason.TokenBudget);

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(serverResult, AuthorizedScope);

        packet.State.ShouldBe(EvidencePacketState.PendingExpansion);
        packet.OmittedDetails.OmittedCount.ShouldBe(serverResult.OmittedCount);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.TokenBudget);
        EvidencePacketExpansionHandle handle = packet.OmittedDetails.ExpansionHandles.ShouldHaveSingleItem();
        handle.TenantId.ShouldBe("tenant-a");
        handle.CaseId.ShouldBe("case-a");
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.IncreaseTokenBudget);
    }

    [Fact]
    public void ServerApplied_TokenBudgetAndDegraded_ShouldCombineOmissionAndPreferDegradedState()
    {
        SearchResult serverResult = SearchResponseMetadataApplier.ApplySearch(
            BuildResult(resultCount: 3, totalCount: 3),
            axisName: "semantic",
            budget: 40,
            degraded: true,
            unavailableAxes: ["graph"]);

        serverResult.OmittedReason.ShouldBe(OmittedReason.Combined);

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(serverResult, AuthorizedScope);

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.Combined);
    }

    [Fact]
    public void ServerApplied_HybridCaseAttribution_ShouldMapEvidenceSourceCaseId()
    {
        HybridSearchResult serverResult = SearchResponseMetadataApplier.ApplyHybrid(
            new HybridSearchResult
            {
                Results =
                [
                    new FusedScoredResult
                    {
                        MemoryUnitId = "mu-001",
                        CompositeScore = 1.0,
                        ContentSnippet = "Snippet body for ranked result number 001.",
                        SourceUri = "mem://tenant-a/case-a/mu-001",
                        SourceType = SourceType.File,
                        SyntacticScore = 1.0,
                        SemanticScore = 1.0,
                        CaseId = "case-a",
                        CaseName = "Case A",
                        AnnotationsCount = 2,
                    },
                ],
                TotalCount = 1,
                Degraded = false,
                UnavailableAxes = [],
                Query = "claim denied",
            },
            budget: null,
            enabledAxes: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic" },
            embeddingConfig: new TenantEmbeddingConfig
            {
                Provider = "google",
                Model = "text-embedding-004",
                Dimensions = 768,
                RateLimitPerMinute = 60,
                ApiSecretKeyName = "test-key",
            },
            graphStart: null);

        EvidencePacket packet = EvidencePacketMapper.FromHybridSearchResult(serverResult, AuthorizedScope);

        EvidencePacketSource source = packet.Sources.ShouldHaveSingleItem();
        source.CaseId.ShouldBe("case-a");
        source.CaseName.ShouldBe("Case A");
        source.AnnotationsCount.ShouldBe(2);
        packet.Evidence.AxesUsed.ShouldBe(["semantic", "syntactic"]);
    }

    [Fact]
    public void ServerError_Forbidden_ShouldMapUnauthorizedWithoutLeak()
    {
        var error = new ErrorResponse(
            "TENANT_FORBIDDEN",
            "Access denied for the requested scope.",
            "Verify your tenant and case authorization.");

        EvidencePacket packet = EvidencePacketMapper.FromError(error, AuthorizedScope, query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Unauthorized);
        packet.Scope.IsolationStatus.ShouldBe(EvidencePacketIsolationStatus.Unauthorized);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.Authorization);
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.CheckAuthorization);
    }

    [Fact]
    public void ServerError_AllBackendUnavailable_ShouldMapDegradedDiagnostics()
    {
        var error = new ErrorResponse(
            "BACKEND_UNAVAILABLE",
            "All search backends are unavailable.",
            "Retry after the backends recover.");

        EvidencePacket packet = EvidencePacketMapper.FromError(error, AuthorizedScope, query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Degraded);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.BackendUnavailable);
    }

    [Fact]
    public void ServerAssembly_MapsCanonicalInputToSharedCanonicalJson()
    {
        // Cross-surface parity (CR1): the contract mapper, as compiled into the server test assembly,
        // produces byte-identical canonical JSON for the shared canonical input.
        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            EvidencePacketCanonicalFixtures.SingleComplete(),
            AuthorizedScope);

        EvidencePacketCanonicalFixtures.Canonicalize(packet)
            .ShouldBe(EvidencePacketCanonicalFixtures.Canonicalize(EvidencePacketCanonicalFixtures.SingleCompletePacket()));
    }

    private static SearchResult BuildResult(int resultCount, long totalCount) => new()
    {
        Results = Enumerable.Range(1, resultCount)
            .Select(rank => new ScoredResult
            {
                MemoryUnitId = $"mu-{rank:000}",
                Score = 0.91 - (0.01 * rank),
                ContentSnippet = $"Snippet body for ranked result number {rank:000}.",
                SourceUri = $"mem://tenant-a/case-a/mu-{rank:000}",
                SourceType = SourceType.File,
                Axis = "semantic",
                CaseId = "case-a",
                CaseName = "Case A",
            })
            .ToArray(),
        TotalCount = totalCount,
        HasIndexedMemoryUnits = true,
        Query = "claim denied",
        AxesUsed = ["semantic"],
    };
}
