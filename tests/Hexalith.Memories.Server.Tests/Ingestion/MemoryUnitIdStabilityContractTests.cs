// <copyright file="MemoryUnitIdStabilityContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.IO;

using Shouldly;

/// <summary>
/// Story 18.6 — drift guard for the <c>MemoryUnitId</c> stability contract published at
/// <c>docs/dev/memory-unit-id-stability.md</c>. Mirrors the doc-contract pattern of the Story 18.2
/// <c>DeploymentConfigurationContractTests</c> / Story 18.3 <c>RouteSurfaceContractTests</c>: plain
/// <c>[Fact]</c>s (no Docker/fixture) that read the doc and the authoritative source files via the repo-root
/// marker walk and assert the published guarantee stays tied to the code paths that build, read, and write the
/// source-URI dedup record. A code-side change to the TTL-less write, the dedup-key shapes, or the lookup seam
/// fails the build unless the contract document is reconciled in the same change.
/// </summary>
public sealed class MemoryUnitIdStabilityContractTests
{
    private const string DocRelativePath = "docs/dev/memory-unit-id-stability.md";

    [Fact]
    public void StabilityContractDoc_Exists()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"MemoryUnitId stability contract not found at {path}");
    }

    [Fact]
    public void Doc_StatesMandatoryStabilityClaims_OpaqueNotSourceDerivedNotUlid()
    {
        // AC1 — the precise stability guarantee plus the opaque/not-source-derived/not-ULID framing.
        string doc = ReadDoc();

        doc.ShouldContain("dedup:{tenantId}:{caseId}:{sha256(sourceUri)}", Case.Sensitive, $"{DocRelativePath} must publish the source-URI dedup key form that backs the stability guarantee.");
        doc.ShouldContain("opaque id string", Case.Sensitive, $"{DocRelativePath} must state that MemoryUnitId is an opaque id string.");
        doc.ShouldContain("not derived from", Case.Sensitive, $"{DocRelativePath} must state that MemoryUnitId is NOT derived from sourceUri.");
        doc.ShouldContain("sourceUri", Case.Sensitive, $"{DocRelativePath} must name sourceUri as the natural-key identity it is not derived from.");
        doc.ShouldContain("not guaranteed to be a ULID", Case.Sensitive, $"{DocRelativePath} must state that MemoryUnitId is NOT guaranteed to be a ULID (the stale architecture wording is superseded).");
    }

    [Fact]
    public void Doc_DependsOnPermanentTtlLessDedupRecord()
    {
        // AC2 — the dedup-record lifetime dependency is explicit: the source-URI record is TTL-less.
        string doc = ReadDoc();

        doc.ShouldContain("expiry: null", Case.Sensitive, $"{DocRelativePath} must document that the source-URI dedup record is TTL-less (SaveDedupKeyActivity writes expiry: null).");
        doc.ShouldContain("persists", Case.Sensitive, $"{DocRelativePath} must scope the guarantee to 'while the committed source-URI dedup record persists'.");
    }

    [Fact]
    public void Doc_DocumentsLossAndFailureModes_WithoutHidingRisk()
    {
        // AC4 — loss/failure modes are documented, and the dedup record (not the backend index) is the authority.
        string doc = ReadDoc();

        doc.ShouldContain("Redis eviction", Case.Sensitive, $"{DocRelativePath} must document Redis eviction as a loss mode that can re-mint a MemoryUnitId.");
        doc.ShouldContain("manual deletion", Case.Sensitive, $"{DocRelativePath} must document manual deletion as a loss mode.");
        doc.ShouldContain("id-resolution authority", Case.Sensitive, $"{DocRelativePath} must state the dedup record is the id-resolution authority.");
        doc.ShouldContain("not the backend index", Case.Sensitive, $"{DocRelativePath} must state that backend index presence alone is not the stability source.");
    }

    [Fact]
    public void Doc_RecommendsStory18_5LookupAsAuthoritativeResolutionPath()
    {
        // AC5 — the authoritative consumer resolution path is the Story 18.5 lookup, not unbounded local id lists.
        string doc = ReadDoc();

        doc.ShouldContain("LookupMemoryUnitIdBySourceUriAsync", Case.Sensitive, $"{DocRelativePath} must recommend MemoriesClient.LookupMemoryUnitIdBySourceUriAsync as the resolution path.");
        doc.ShouldContain("memory-units/by-source-uri", Case.Sensitive, $"{DocRelativePath} must document the GET .../memory-units/by-source-uri route as the resolution path.");
        doc.ShouldContain("unbounded", Case.Sensitive, $"{DocRelativePath} must warn against maintaining unbounded historical id lists as the primary identity store.");
    }

    [Fact]
    public void Doc_PreservesStory18_4TokenAugmentNotReplaceSemantics()
    {
        // AC6 — token-keyed records augment, never replace, the source-URI record.
        string doc = ReadDoc();

        doc.ShouldContain("dedup:{tenantId}:{caseId}:tok:{sha256(token)}", Case.Sensitive, $"{DocRelativePath} must document the token-keyed record form.");
        doc.ShouldContain("augment, never replace", Case.Sensitive, $"{DocRelativePath} must state the token record augments, never replaces, the source-URI record.");
    }

    [Fact]
    public void Doc_ResolvesPartiesDecisionD1Confusion()
    {
        // AC3 — the Parties "decision D1" label is unrelated to Memories Architecture Decision D1 (FalkorDB for MVP).
        string doc = ReadDoc();

        doc.ShouldContain("decision D1", Case.Sensitive, $"{DocRelativePath} must reference the Parties 'decision D1' label.");
        doc.ShouldContain("Architecture Decision D1", Case.Sensitive, $"{DocRelativePath} must clarify Memories Architecture Decision D1 is different.");
        doc.ShouldContain("FalkorDB for MVP", Case.Sensitive, $"{DocRelativePath} must state Memories Architecture Decision D1 is 'FalkorDB for MVP'.");
    }

    [Fact]
    public void TtlLessDedupWrite_IsTiedToCodeAndDoc()
    {
        // Doc <-> code tie: SaveDedupKeyActivity must keep writing the source-URI record with expiry: null.
        string activity = ReadRepoFile("src", "Hexalith.Memories.Server", "Activities", "Ingestion", "SaveDedupKeyActivity.cs");
        activity.ShouldContain("expiry: null", Case.Sensitive, "SaveDedupKeyActivity must keep writing the permanent dedup record with expiry: null — a non-null TTL would silently weaken the MemoryUnitId stability guarantee.");

        ReadDoc().ShouldContain("expiry: null", Case.Sensitive, $"{DocRelativePath} must mirror the TTL-less write marker 'expiry: null'.");
    }

    [Fact]
    public void DedupKeyShapes_AreTiedToCodeAndDoc()
    {
        // Doc <-> code tie: the source-URI key prefix and the distinct :tok: namespace must not drift.
        string builder = ReadRepoFile("src", "Hexalith.Memories.Server", "Activities", "Ingestion", "DedupKeyBuilder.cs");
        builder.ShouldContain("dedup:{tenantId}:{caseId}:", Case.Sensitive, "DedupKeyBuilder.BuildKey must keep the dedup:{tenantId}:{caseId}: source-URI key prefix the stability contract documents.");
        builder.ShouldContain(":tok:", Case.Sensitive, "DedupKeyBuilder.BuildTokenKey must keep the distinct :tok: namespace so the token record augments rather than replaces the source-URI record.");

        string doc = ReadDoc();
        doc.ShouldContain("dedup:{tenantId}:{caseId}:{sha256(sourceUri)}", Case.Sensitive, $"{DocRelativePath} must document the source-URI key shape.");
        doc.ShouldContain("dedup:{tenantId}:{caseId}:tok:{sha256(token)}", Case.Sensitive, $"{DocRelativePath} must document the token-keyed shape.");
    }

    [Fact]
    public void SourceUriLookup_ResolvesViaDedupKeyBuilder_TiedToCodeAndDoc()
    {
        // Doc <-> code tie: the lookup seam must resolve over the same permanent dedup key (DedupKeyBuilder.BuildKey).
        string lookup = ReadRepoFile("src", "Hexalith.Memories.Server", "Ingestion", "SourceUriMemoryUnitLookup.cs");
        lookup.ShouldContain("DedupKeyBuilder.BuildKey", Case.Sensitive, "SourceUriMemoryUnitLookup must keep resolving over DedupKeyBuilder.BuildKey — the same permanent record the stability contract relies on.");

        ReadDoc().ShouldContain("LookupMemoryUnitIdBySourceUriAsync", Case.Sensitive, $"{DocRelativePath} must document the consumer-facing lookup method tied to this seam.");
    }

    private static string ReadDoc() => File.ReadAllText(ResolveDocPath());

    private static string ResolveDocPath()
        => Path.Combine(ResolveRepoRoot(), "docs", "dev", "memory-unit-id-stability.md");

    private static string ReadRepoFile(params string[] segments)
    {
        string[] parts = new string[segments.Length + 1];
        parts[0] = ResolveRepoRoot();
        System.Array.Copy(segments, 0, parts, 1, segments.Length);
        string path = Path.Combine(parts);
        File.Exists(path).ShouldBeTrue($"Authoritative source file not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoRoot()
    {
        // Walk up from the test binary to the repo root identified by the Hexalith.Memories.slnx marker.
        string candidate = System.AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return System.AppContext.BaseDirectory;
    }
}
