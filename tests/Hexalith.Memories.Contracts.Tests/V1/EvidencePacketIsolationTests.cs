// <copyright file="EvidencePacketIsolationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Tenant/case negative isolation coverage (Story 2.7 / CR4) under the CR9 decision: the mapper trusts
/// upstream source attribution and does not reconcile <c>source.CaseId</c> against the request scope.
/// The isolation invariant the mapper DOES guarantee — and these tests pin — is that
/// <see cref="EvidencePacket.Scope"/> always reflects the REQUEST scope passed by the caller and is never
/// derived from source rows. Cross-scope reconciliation remains the upstream/server boundary (CR9).
/// </summary>
public sealed class EvidencePacketIsolationTests
{
    [Fact]
    public void FromError_Forbidden_ShouldEchoRequestScopeAndNotLeakOtherTenant()
    {
        // Evidence/diagnostics mention tenant-b, but the caller requested tenant-a. The unauthorized
        // packet must echo the caller's scope only and reveal nothing about another scope's existence.
        var error = new ErrorResponse(
            "TENANT_FORBIDDEN",
            "Denied. Resource belongs to tenant-b / case-b.",
            "Switch to tenant-b to view it.");

        EvidencePacket packet = EvidencePacketMapper.FromError(
            error,
            new EvidencePacketScope("tenant-a", "case-a", EvidencePacketIsolationStatus.Authorized, "tenant-case"),
            query: "claim denied");

        packet.State.ShouldBe(EvidencePacketState.Unauthorized);
        packet.Scope.TenantId.ShouldBe("tenant-a");
        packet.Scope.CaseId.ShouldBe("case-a");
        packet.Scope.IsolationStatus.ShouldBe(EvidencePacketIsolationStatus.Unauthorized);
        packet.Sources.ShouldBeEmpty();
        packet.Evidence.AxisEvidence.ShouldBeEmpty();
        packet.OmittedDetails.ExpansionHandles.ShouldBeEmpty();

        string json = JsonSerializer.Serialize(packet, MemoriesJsonContext.Options);
        json.ShouldNotContain("tenant-b", Shouldly.Case.Sensitive);
        json.ShouldNotContain("case-b", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void FromSearchResult_PreSetUnauthorizedScope_ShouldRenderUnauthorizedRequestScope()
    {
        // A caller that already knows the scope is unauthorized (scope.IsolationStatus = Unauthorized) must
        // get an Unauthorized packet that echoes the requested tenant/case, not the source row's case.
        var result = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "claim denied",
            AxesUsed = ["semantic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-b", "case-b", EvidencePacketIsolationStatus.Unauthorized, "tenant-case"));

        packet.State.ShouldBe(EvidencePacketState.Unauthorized);
        packet.Scope.TenantId.ShouldBe("tenant-b");
        packet.Scope.CaseId.ShouldBe("case-b");
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.Authorization);
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.CheckAuthorization);
    }

    [Fact]
    public void FromSearchResult_RequestScopeWinsOverSourceCase()
    {
        // Simulated upstream regression: the request is case-b but a returned source carries case-a.
        // Per CR9 (trust upstream) the mapper does NOT reconcile — but packet.Scope must remain the
        // REQUEST scope (case-b), proving scope is never derived from source attribution.
        var result = new SearchResult
        {
            Results =
            [
                new ScoredResult
                {
                    MemoryUnitId = "mu-001",
                    Score = 0.80,
                    ContentSnippet = "Row attributed to another case by upstream.",
                    SourceUri = "mem://tenant-a/case-a/mu-001",
                    SourceType = SourceType.File,
                    Axis = "semantic",
                    CaseId = "case-a",
                    CaseName = "Case A",
                },
            ],
            TotalCount = 1,
            HasIndexedMemoryUnits = true,
            Query = "claim denied",
            AxesUsed = ["semantic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-a", "case-b", EvidencePacketIsolationStatus.Authorized, "tenant-case"));

        // Scope is request-derived (case-b), never copied from the source row.
        packet.Scope.CaseId.ShouldBe("case-b");

        // Source attribution is preserved verbatim (CR9 trust-upstream boundary).
        packet.Sources[0].CaseId.ShouldBe("case-a");
    }

    [Fact]
    public void FromSearchResult_MissingCase_EmptyResult_ShouldRenderEmptyRequestScope()
    {
        var result = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "claim denied",
            AxesUsed = ["semantic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromSearchResult(
            result,
            new EvidencePacketScope("tenant-a", "case-missing", EvidencePacketIsolationStatus.Authorized, "tenant-case"));

        packet.State.ShouldBe(EvidencePacketState.Empty);
        packet.Scope.CaseId.ShouldBe("case-missing");
        packet.Sources.ShouldBeEmpty();
        packet.Recovery.ShouldContain(action => action.Kind == EvidencePacketRecoveryKind.BroadenScope);
    }

    [Fact]
    public void FromHybridSearchResult_TenantWideScope_ShouldPreserveCrossCaseSources()
    {
        // Cross-case / tenant-wide search legitimately returns rows from multiple cases. The mapper must
        // NOT drop or rewrite them — proving the CR9 decision does not break cross-case search.
        var result = new HybridSearchResult
        {
            Results =
            [
                new FusedScoredResult
                {
                    MemoryUnitId = "mu-001",
                    CompositeScore = 0.80,
                    ContentSnippet = "Case one match.",
                    SourceUri = "mem://tenant-a/case-one/mu-001",
                    SourceType = SourceType.File,
                    SemanticScore = 0.80,
                    CaseId = "case-one",
                    CaseName = "Case One",
                },
                new FusedScoredResult
                {
                    MemoryUnitId = "mu-002",
                    CompositeScore = 0.74,
                    ContentSnippet = "Case two match.",
                    SourceUri = "mem://tenant-a/case-two/mu-002",
                    SourceType = SourceType.File,
                    SemanticScore = 0.74,
                    CaseId = "case-two",
                    CaseName = "Case Two",
                },
            ],
            TotalCount = 2,
            Degraded = false,
            UnavailableAxes = [],
            Query = "claim denied",
            AxesUsed = ["semantic"],
        };

        EvidencePacket packet = EvidencePacketMapper.FromHybridSearchResult(
            result,
            new EvidencePacketScope("tenant-a", null, EvidencePacketIsolationStatus.Authorized, "tenant"));

        packet.Scope.CaseId.ShouldBeNull();
        packet.Scope.PermissionsContext.ShouldBe("tenant");
        packet.Sources.Select(source => source.CaseId).ShouldBe(["case-one", "case-two"]);
    }
}
