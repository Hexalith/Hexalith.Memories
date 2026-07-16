// <copyright file="MemoryUnitIdStabilityContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.IO;
using System.Linq;

using Hexalith.Memories.TestHelpers.Documentation;

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
    private const string ConsistencyDocRelativePath = "docs/dev/consistency.md";
    private const string IngestContractRelativePath = "docs/dev/ingest-contract.md";

    [Fact]
    public void StabilityContractDoc_Exists()
    {
        string path = ResolveDocPath();
        File.Exists(path).ShouldBeTrue($"MemoryUnitId stability contract not found at {path}");
    }

    [Fact]
    public void Doc_StatesMandatoryStabilityClaims_OpaqueNotSourceDerivedNotUlid()
    {
        MarkdownContractDocument document = ReadDocument();
        document.GetTableHeader("1. What `MemoryUnitId` is (and is not)")
            .ShouldBe(["Property", "Value", "Authoritative source"]);
        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows("1. What `MemoryUnitId` is (and is not)");

        rows.Count.ShouldBe(4);
        rows[0].ShouldBe(["Type", "Opaque `string`", "`IngestionWorkflow.ResolveMemoryUnitId`"]);
        rows[1].ShouldBe(["Derived from `sourceUri`?", "**No** — it is **not derived from `sourceUri`**", "`IngestionWorkflow.ResolveMemoryUnitId`"]);
        rows[2].ShouldBe(["Guaranteed ULID / time-sortable?", "**No** — it is **not guaranteed to be a ULID** and carries no ordering guarantee", "`IngestionWorkflow.ResolveMemoryUnitId`"]);
        rows[3].ShouldBe(
        [
            "Today's concrete shape",
            "The Dapr workflow instance id (a GUID/ULID-like string supplied by the host) for ordinary file/url ingests, or a fresh `context.NewGuid().ToString()` for `dedup:`-prefixed EventStore workflows",
            "`IngestionWorkflow.ResolveMemoryUnitId`",
        ]);

        ReadDocument().GetSection("2. The stability guarantee")
            .ShouldContain("dedup:{tenantId}:{caseId}:{sha256(sourceUri)}", Case.Sensitive);
    }

    [Fact]
    public void Doc_DependsOnPermanentTtlLessDedupRecord()
    {
        // AC2 — the dedup-record lifetime dependency is explicit: the source-URI record is TTL-less.
        string guarantee = ReadDocument().GetSection("2. The stability guarantee");
        string lifetime = ReadDocument().GetSection("Lifetime dependency (the load-bearing invariant)");

        guarantee.ShouldContain("persists", Case.Sensitive, $"{DocRelativePath} must scope the guarantee to the owning stability section.");
        lifetime.ShouldContain("expiry", Case.Sensitive, $"{DocRelativePath} must keep the TTL dependency in its exact subsection.");
        guarantee.ShouldContain("expiry: null", Case.Sensitive, $"{DocRelativePath} must keep the source write marker in the owning section.");
    }

    [Fact]
    public void Doc_DocumentsLossAndFailureModes_WithoutHidingRisk()
    {
        // AC4 — loss/failure modes are documented, and the dedup record (not the backend index) is the authority.
        string section = ReadDocument().GetSection("3. Failure / loss modes (risk is documented, not hidden)");

        section.ShouldContain("Redis eviction", Case.Sensitive);
        section.ShouldContain("manual deletion", Case.Sensitive);
        section.ShouldContain("id-resolution authority", Case.Sensitive);
        section.ShouldContain("not the backend index", Case.Sensitive);
    }

    [Fact]
    public void Doc_RecommendsStory18_5LookupAsAuthoritativeResolutionPath()
    {
        // AC5 — the authoritative consumer resolution path is the Story 18.5 lookup, not unbounded local id lists.
        string section = ReadDocument().GetSection("5. Authoritative consumer resolution path (Story 18.5)");

        section.ShouldContain("LookupMemoryUnitIdBySourceUriAsync", Case.Sensitive);
        section.ShouldContain("memory-units/by-source-uri", Case.Sensitive);
        section.ShouldContain("unbounded", Case.Sensitive);
    }

    [Fact]
    public void Doc_PreservesStory18_4TokenAugmentNotReplaceSemantics()
    {
        // AC6 — token-keyed records augment, never replace, the source-URI record.
        string section = ReadDocument().GetSection("4. Token semantics remain intact (Story 18.4)");

        section.ShouldContain("dedup:{tenantId}:{caseId}:tok:{sha256(token)}", Case.Sensitive);
        section.ShouldContain("augment, never replace", Case.Sensitive);
    }

    [Fact]
    public void Doc_ResolvesPartiesDecisionD1Confusion()
    {
        // AC3 — the Parties "decision D1" label is unrelated to Memories Architecture Decision D1 (FalkorDB for MVP).
        string section = ReadDocument().GetSection("6. Parties \"decision D1\" is not Memories Architecture Decision D1");

        section.ShouldContain("decision D1", Case.Sensitive);
        section.ShouldContain("Architecture Decision D1", Case.Sensitive);
        section.ShouldContain("FalkorDB for MVP", Case.Sensitive);
    }

    [Fact]
    public void TtlLessDedupWrite_IsTiedToCodeAndDoc()
    {
        // Doc <-> code tie: SaveDedupKeyActivity must keep writing the source-URI record with expiry: null.
        string activity = ReadRepoFile("src", "Hexalith.Memories.Server", "Activities", "Ingestion", "SaveDedupKeyActivity.cs");
        activity.ShouldContain("expiry: null", Case.Sensitive, "SaveDedupKeyActivity must keep writing the permanent dedup record with expiry: null — a non-null TTL would silently weaken the MemoryUnitId stability guarantee.");
        activity.ShouldContain("When.NotExists", Case.Sensitive, "SaveDedupKeyActivity must keep the permanent dedup record first-writer-wins so a race loser cannot overwrite the winning MemoryUnitId.");

        string guarantee = ReadDocument().GetSection("2. The stability guarantee");
        guarantee.ShouldContain("expiry: null", Case.Sensitive);
        guarantee.ShouldContain("When.NotExists", Case.Sensitive);
    }

    [Fact]
    public void IngestContract_MirrorsPermanentDedupFirstWriterWinsContract()
    {
        var ingestContract = new MarkdownContractDocument(ReadRepoFile("docs", "dev", "ingest-contract.md"));

        ingestContract.GetSection("1. Stable, additive entry point (AC1)").ShouldContain("MemoriesClient.IngestAsync", Case.Sensitive);
        string token = ingestContract.GetSection("2. Idempotency token: precedence and natural-key fallback (AC2)");
        token.ShouldContain("dedup:{tenantId}:{caseId}:{sha256(sourceUri)}", Case.Sensitive);
        token.ShouldContain("dedup:{tenantId}:{caseId}:tok:{sha256(token)}", Case.Sensitive);
        token.ShouldContain("Augment, never replace", Case.Sensitive);

        string atomic = ingestContract.GetSection("3. Atomic dedup — no check-then-act race (AC3)");
        atomic.ShouldContain("TTL-less first-writer-wins", Case.Sensitive);
        atomic.ShouldContain("expiry: null", Case.Sensitive);
        atomic.ShouldContain("When.NotExists", Case.Sensitive);

        ingestContract.GetSection("4. Idempotent under at-least-once, unordered delivery (AC4)")
            .ShouldContain(
                "returns the **same**\n`MemoryUnitId` without creating a second unit",
                Case.Sensitive);
        ingestContract.GetSection("6. `MemoryUnitId` stability — authoritative guarantee (Story 18.6)")
            .ShouldContain("opaque", Case.Sensitive);
    }

    [Fact]
    public void DedupKeyShapes_AreTiedToCodeAndDoc()
    {
        // Doc <-> code tie: the source-URI key prefix and the distinct :tok: namespace must not drift.
        string builder = ReadRepoFile("src", "Hexalith.Memories.Server", "Activities", "Ingestion", "DedupKeyBuilder.cs");
        builder.ShouldContain("dedup:{tenantId}:{caseId}:", Case.Sensitive, "DedupKeyBuilder.BuildKey must keep the dedup:{tenantId}:{caseId}: source-URI key prefix the stability contract documents.");
        builder.ShouldContain(":tok:", Case.Sensitive, "DedupKeyBuilder.BuildTokenKey must keep the distinct :tok: namespace so the token record augments rather than replaces the source-URI record.");

        ReadDocument().GetSection("2. The stability guarantee")
            .ShouldContain("dedup:{tenantId}:{caseId}:{sha256(sourceUri)}", Case.Sensitive);
        ReadDocument().GetSection("4. Token semantics remain intact (Story 18.4)")
            .ShouldContain("dedup:{tenantId}:{caseId}:tok:{sha256(token)}", Case.Sensitive);
    }

    [Fact]
    public void SourceUriLookup_ResolvesViaDedupKeyBuilder_TiedToCodeAndDoc()
    {
        // Doc <-> code tie: the lookup seam must resolve over the same permanent dedup key (DedupKeyBuilder.BuildKey).
        string lookup = ReadRepoFile("src", "Hexalith.Memories.Server", "Ingestion", "SourceUriMemoryUnitLookup.cs");
        lookup.ShouldContain("DedupKeyBuilder.BuildKey", Case.Sensitive, "SourceUriMemoryUnitLookup must keep resolving over DedupKeyBuilder.BuildKey — the same permanent record the stability contract relies on.");

        ReadDocument().GetSection("5. Authoritative consumer resolution path (Story 18.5)")
            .ShouldContain("LookupMemoryUnitIdBySourceUriAsync", Case.Sensitive);
    }

    [Fact]
    public void ConsistencyInspect_PreservesOpaqueExactValueContractAcrossDocsAndSources()
    {
        string stabilityResolution = ReadDocument().GetSection("5. Authoritative consumer resolution path (Story 18.5)");
        stabilityResolution.ShouldContain("MemoriesClient.InspectConsistencyAsync", Case.Sensitive);
        stabilityResolution.ShouldContain("Pass the resolved", Case.Sensitive);
        stabilityResolution.ShouldContain("exactly", Case.Sensitive);
        stabilityResolution.ShouldContain("./consistency.md", Case.Sensitive);

        var consistencyDoc = new MarkdownContractDocument(ReadRepoFile(ConsistencyDocRelativePath.Split('/')));
        string endpointSummary = consistencyDoc.GetSection("Endpoint summary");
        endpointSummary.ShouldContain("opaque, non-blank identifier", Case.Sensitive);
        endpointSummary.ShouldContain("exact non-blank `MemoryUnitId`", Case.Sensitive);
        endpointSummary.ShouldContain("Do not trim, case-fold, parse, or otherwise reformat", Case.Sensitive);
        endpointSummary.ShouldContain("./memory-unit-id-stability.md", Case.Sensitive);

        string inspectionService = ReadRepoFile("src", "Hexalith.Memories.Server", "Consistency", "ConsistencyInspectionService.cs");
        inspectionService.ShouldContain("ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);", Case.Sensitive);
        int exactProbe = inspectionService.IndexOf("ProbeCandidateAsync(tenantId, memoryUnitId, ct)", StringComparison.Ordinal);
        int fallbackProbe = inspectionService.IndexOf("TryGetGuidDAlias(memoryUnitId", StringComparison.Ordinal);
        exactProbe.ShouldBeGreaterThanOrEqualTo(0);
        fallbackProbe.ShouldBeGreaterThan(exactProbe, "InspectAsync must probe the exact opaque identifier before considering the GUID-N compatibility alias.");
        inspectionService.ShouldNotContain("string normalizedMemoryUnitId = NormalizeMemoryUnitId(memoryUnitId);", Case.Sensitive);

        ReadRepoFile("src", "Hexalith.Memories.Server", "Endpoints", "ConsistencyEndpoints.cs")
            .ShouldContain("Pass the exact non-blank MemoryUnitId returned by ingest or source-URI lookup; do not parse or reformat it.", Case.Sensitive);
        ReadRepoFile("src", "Hexalith.Memories.Cli", "Commands", "ConsistencyInspectCommand.cs")
            .ShouldContain("Opaque memory unit identifier; pass the exact value returned by Memories.", Case.Sensitive);
        ReadRepoFile("src", "Hexalith.Memories.Client.Rest", "MemoriesClient.cs")
            .ShouldContain("The opaque memory unit identifier, passed exactly as returned by Memories.", Case.Sensitive);
        ReadRepoFile("src", "Hexalith.Memories.Contracts", "V1", "ConsistencyInspectionResult.cs")
            .ShouldContain("The opaque memory unit identifier.", Case.Sensitive);
        ReadRepoFile("src", "Hexalith.Memories.Contracts", "V1", "ConsistencyDiscrepancy.cs")
            .ShouldContain("The opaque memory unit identifier.", Case.Sensitive);
    }

    [Fact]
    public void ContractDocs_ContainNoLeakedToolCallMarkup()
    {
        string[] paths = [DocRelativePath, IngestContractRelativePath, ConsistencyDocRelativePath];
        foreach (string path in paths)
        {
            string content = ReadRepoFile(path.Split('/'));
            IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(content);
            diagnostics.ShouldBeEmpty($"{path} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
        }
    }

    private static string ReadDoc() => File.ReadAllText(ResolveDocPath());

    private static MarkdownContractDocument ReadDocument() => new(ReadDoc());

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
