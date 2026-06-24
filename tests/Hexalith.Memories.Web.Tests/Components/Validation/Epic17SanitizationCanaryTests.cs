// <copyright file="Epic17SanitizationCanaryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System.Collections.Generic;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Lenses.AgentPacket;
using Hexalith.Memories.Web.Tests.Components.Evidence;
using Hexalith.Memories.Web.Tests.Components.Lenses;

using Shouldly;

/// <summary>
/// Story 17.5 Task 5 — accessibility surfaces are a security surface. Seeded redaction canaries are placed
/// in every contract field that flows into visible text, accessible names, recovery guidance, and copy
/// payloads. The sweep fails closed if any canary appears in any rendered surface, and proves restrictive
/// scope suppresses source existence rather than disclosing it across tenants.
/// </summary>
public sealed class Epic17SanitizationCanaryTests : Epic17ValidationTestBase
{
    // Canaries shaped like the categories the redaction contract is designed to scrub.
    private const string CanaryBearer = "Bearer canary0bearertoken123";
    private const string CanaryWinPath = "C:\\Users\\canary\\canary1secret.txt";
    private const string CanaryPosixPath = "/home/canary/canary2secret";
    private const string CanaryConn = "redis://canary3host:6379";

    // The distinctive secret fragments that must never survive into any rendered surface.
    private static readonly string[] CanarySecrets =
    [
        "canary0bearertoken123",
        "canary1secret",
        "canary2secret",
        "canary3host",
        "Bearer ",
        "C:\\Users\\canary",
        "/home/canary",
        "redis://canary3host",
    ];

    // Restricted source markers the upstream fixtures intentionally leave populated.
    private static readonly string[] RestrictedSourceMarkers =
    [
        "memory-secret",
        "memory-secret-a",
        "memory-secret-b",
        "secret-supports",
        "secret-gap",
        "secret-axis-evidence",
        "https://docs.example/restricted",
    ];

    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_SensitiveSourceContent_NeverLeaksCanariesIntoMarkup(string surface)
    {
        string markup = RenderSurface(surface, SensitiveSourcesComplete());

        foreach (string secret in CanarySecrets)
        {
            markup.ShouldNotContain(secret);
        }
    }

    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_SensitiveScopeAndRecovery_NeverLeaksCanariesIntoMarkup(string surface)
    {
        string markup = RenderSurface(surface, SensitiveRecoveryCompressed());

        foreach (string secret in CanarySecrets)
        {
            markup.ShouldNotContain(secret);
        }
    }

    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_SensitiveContent_NeverLeaksCanariesIntoAccessibleNames(string surface)
    {
        string markup = RenderSurface(surface, SensitiveSourcesComplete());

        foreach (IElement labelled in QueryAll(markup, "[aria-label]"))
        {
            string ariaLabel = labelled.GetAttribute("aria-label") ?? string.Empty;
            foreach (string secret in CanarySecrets)
            {
                ariaLabel.ShouldNotContain(secret);
            }
        }
    }

    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_UnauthorizedScope_SuppressesRestrictedSourceExistence(string surface)
    {
        // Tenant isolation: an unauthorized packet keeps secret-marked sources/graph populated upstream;
        // no surface may disclose whether that evidence exists outside the authorized scope.
        string markup = RenderSurface(surface, EvidencePacketFixtures.UnauthorizedPacket());

        foreach (string marker in RestrictedSourceMarkers)
        {
            markup.ShouldNotContain(marker);
        }
    }

    [Fact]
    public void AgentPacketInspector_CopyPayload_IsSanitizedAndMatchesJsonView()
    {
        string? copied = null;
        IRenderedComponent<MemoriesAgentPacketInspector> component = Render<MemoriesAgentPacketInspector>(parameters => parameters
            .Add(p => p.Packet, SensitiveSourcesComplete())
            .Add(p => p.OnCopy, (string text) => copied = text));

        component.Find("[data-testid='mem-packet-copy']").Click();

        copied.ShouldNotBeNull();
        copied.ShouldBe(component.Find("[data-testid='mem-packet-json']").TextContent);
        foreach (string secret in CanarySecrets)
        {
            copied!.ShouldNotContain(secret);
        }
    }

    [Fact]
    public void CrossTenantPacket_CaseActivityTrail_RepartitionsScopeWithNoActiveTenantResidue()
    {
        // Switching to a foreign-tenant packet must repartition scope; no active-tenant residue may remain.
        string markup = RenderSurface("CaseActivityTrail", Hexalith.Memories.Web.Tests.Components.Lenses.LensPacketFixtures.CrossTenant());

        markup.ShouldContain("tenant-b");
        markup.ShouldNotContain("tenant-a");
    }

    [Theory]
    [MemberData(nameof(AdditionalCanonicalStateCanaryCases))]
    public void Surface_AdditionalCanonicalState_NeverLeaksCanariesIntoMarkup(string surface, string baseState)
    {
        // Task 5 fixture families: extend the canary sweep to degraded, stale, and redacted packets so the
        // no-leak guarantee covers the full canonical state set, not only happy/compressed/unauthorized.
        string markup = RenderSurface(surface, WithCanaries(BaseStateByName(baseState)));

        foreach (string secret in CanarySecrets)
        {
            markup.ShouldNotContain(secret);
        }
    }

    [Theory]
    [InlineData("SchemaMismatch")]
    [InlineData("MissingSource")]
    public void AgentPacketInspector_InvalidOrMissingSourceState_KeepsCopyAndJsonCanarySafe(string baseState)
    {
        // Task 5: invalid/schema-mismatch and missing-source packets are the highest-risk diagnostics/JSON
        // surface; the copy payload and JSON view must share one sanitized text with no canary leakage.
        EvidencePacket packet = WithCanaries(baseState switch
        {
            "SchemaMismatch" => LensPacketFixtures.SchemaMismatch(),
            "MissingSource" => LensPacketFixtures.MissingSource(),
            _ => throw new System.InvalidOperationException($"Unknown base state '{baseState}'."),
        });

        string? copied = null;
        IRenderedComponent<MemoriesAgentPacketInspector> component = Render<MemoriesAgentPacketInspector>(parameters => parameters
            .Add(p => p.Packet, packet)
            .Add(p => p.OnCopy, (string text) => copied = text));

        component.Find("[data-testid='mem-packet-copy']").Click();

        copied.ShouldNotBeNull();
        copied.ShouldBe(component.Find("[data-testid='mem-packet-json']").TextContent);
        foreach (string secret in CanarySecrets)
        {
            copied!.ShouldNotContain(secret);
        }
    }

    public static IEnumerable<object[]> AdditionalCanonicalStateCanaryCases()
    {
        foreach (string surface in PacketSurfaces)
        {
            yield return [surface, "Degraded"];
            yield return [surface, "Stale"];
            yield return [surface, "Redacted"];
        }
    }

    private static EvidencePacket BaseStateByName(string baseState)
        => baseState switch
        {
            "Degraded" => EvidencePacketFixtures.DegradedPacket(),
            "Stale" => EvidencePacketFixtures.StalePacket(),
            "Redacted" => EvidencePacketFixtures.RedactedPacket(),
            _ => throw new System.InvalidOperationException($"Unknown base state '{baseState}'."),
        };

    private static EvidencePacket WithCanaries(EvidencePacket basePacket)
        => basePacket with
        {
            Scope = new EvidencePacketScope(
                TenantId: $"tenant {CanaryBearer}",
                CaseId: CanaryWinPath,
                IsolationStatus: basePacket.Scope.IsolationStatus,
                PermissionsContext: basePacket.Scope.PermissionsContext),
            Sources =
            [
                new EvidencePacketSource(
                    Rank: 1,
                    MemoryUnitId: "memory-a",
                    SourceUri: CanaryWinPath,
                    SourceType: SourceType.File,
                    Snippet: $"Context {CanaryBearer} {CanaryPosixPath} {CanaryConn} trailing",
                    Score: 0.81d,
                    CaseId: "case-a",
                    CaseName: "Case A",
                    AnnotationsCount: 0),
            ],
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.IncreaseTokenBudget,
                    "increaseTokenBudget",
                    $"Retry with {CanaryBearer} from {CanaryWinPath}",
                    CanaryPosixPath),
            ],
        };

    private static EvidencePacket SensitiveSourcesComplete()
        => EvidencePacketFixtures.CompletePacket() with
        {
            Scope = new EvidencePacketScope(
                TenantId: $"tenant {CanaryBearer}",
                CaseId: CanaryWinPath,
                IsolationStatus: EvidencePacketIsolationStatus.Authorized,
                PermissionsContext: "tenant-case"),
            Sources =
            [
                new EvidencePacketSource(
                    Rank: 1,
                    MemoryUnitId: "memory-a",
                    SourceUri: CanaryWinPath,
                    SourceType: SourceType.File,
                    Snippet: $"Context {CanaryBearer} {CanaryPosixPath} {CanaryConn} trailing",
                    Score: 0.81d,
                    CaseId: "case-a",
                    CaseName: "Case A",
                    AnnotationsCount: 0),
            ],
        };

    private static EvidencePacket SensitiveRecoveryCompressed()
        => EvidencePacketFixtures.CompressedPacket() with
        {
            Scope = new EvidencePacketScope(
                TenantId: $"tenant {CanaryBearer}",
                CaseId: CanaryWinPath,
                IsolationStatus: EvidencePacketIsolationStatus.Authorized,
                PermissionsContext: "tenant-case"),
            Recovery =
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.IncreaseTokenBudget,
                    "increaseTokenBudget",
                    $"Retry with {CanaryBearer} from {CanaryWinPath}",
                    CanaryPosixPath),
            ],
        };
}
