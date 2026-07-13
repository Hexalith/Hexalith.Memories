// <copyright file="ImportEnvelopeReaderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Import;

using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Import;

using Shouldly;

/// <summary>Story 26.2 — Docker-free coverage for reversing the export envelope back into typed data.</summary>
public class ImportEnvelopeReaderTests
{
    private const string TenantEnvelope = """
    {
      "manifest": { "schemaVersion": 1, "scope": "tenant", "tenantId": "acme", "caseId": null, "exportedAt": "2026-07-13T00:00:00+00:00", "snapshotAt": "2026-07-13T00:00:00+00:00" },
      "cases": [
        { "id": "case-1", "tenantId": "acme", "name": "Case One", "status": "active", "createdAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-02T00:00:00+00:00", "memoryUnitCount": 2,
          "members": [ { "memberId": "user-1", "memberType": "user", "addedAt": "2026-07-01T00:00:00+00:00" } ] }
      ],
      "memoryUnits": [
        { "unit": { "id": "mu-1", "tenantId": "acme", "caseId": "case-1", "content": "hello", "contentHash": "h1", "sourceUri": "file:///a.txt", "sourceType": "file", "ingestedBy": "tester", "ingestedAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-01T00:00:00+00:00", "status": "indexed", "metadata": {}, "embeddingProvider": "google:text-embedding-004", "embeddingModel": "text-embedding-004", "embeddingDimensions": 768 }, "annotationTargets": [] },
        { "unit": { "id": "mu-2", "tenantId": "acme", "caseId": "case-1", "content": "world", "contentHash": "h2", "sourceUri": "file:///b.txt", "sourceType": "file", "ingestedBy": "tester", "ingestedAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-01T00:00:00+00:00", "status": "indexed", "metadata": {}, "embeddingProvider": "google:text-embedding-004", "embeddingModel": "text-embedding-004", "embeddingDimensions": 768 }, "annotationTargets": [] }
      ],
      "edges": [
        { "id": "42", "sourceId": "mu-1", "targetId": "mu-2", "edgeType": "causedBy", "confidence": 0.9, "origin": "inferred", "createdAt": "2026-07-03T00:00:00+00:00", "verifiedBy": "reviewer-1", "previousConfidence": 0.5 }
      ],
      "statistics": { "memoryUnitCount": 2, "edgeCount": 1, "caseCount": 1 }
    }
    """;

    private const string CaseEnvelope = """
    {
      "manifest": { "schemaVersion": 1, "scope": "case", "tenantId": "acme", "caseId": "case-1", "exportedAt": "2026-07-13T00:00:00+00:00", "snapshotAt": "2026-07-13T00:00:00+00:00" },
      "case": { "id": "case-1", "tenantId": "acme", "name": "Case One", "status": "active", "createdAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-02T00:00:00+00:00", "memoryUnitCount": 1,
        "members": [ { "memberId": "role-admin", "memberType": "role", "addedAt": "2026-07-01T00:00:00+00:00" } ] },
      "memoryUnits": [
        { "unit": { "id": "mu-1", "tenantId": "acme", "caseId": "case-1", "content": "hello", "contentHash": "h1", "sourceUri": "file:///a.txt", "sourceType": "file", "ingestedBy": "tester", "ingestedAt": "2026-07-01T00:00:00+00:00", "lastUpdated": "2026-07-01T00:00:00+00:00", "status": "indexed", "metadata": {}, "embeddingProvider": "google:text-embedding-004", "embeddingModel": "text-embedding-004", "embeddingDimensions": 768 }, "annotationTargets": [] }
      ],
      "edges": [],
      "statistics": { "memoryUnitCount": 1, "edgeCount": 0, "caseCount": 1 }
    }
    """;

    [Fact]
    public void TryReadManifest_ValidTenantEnvelope_ReadsManifest()
    {
        bool ok = ImportEnvelopeReader.TryReadManifest(Encoding.UTF8.GetBytes(TenantEnvelope), out ExportManifest? manifest, out string? error);

        ok.ShouldBeTrue();
        error.ShouldBeNull();
        manifest.ShouldNotBeNull();
        manifest.SchemaVersion.ShouldBe(1);
        manifest.Scope.ShouldBe(ExportScope.Tenant);
        manifest.TenantId.ShouldBe("acme");
    }

    [Fact]
    public void TryReadManifest_NotJson_ReturnsFalseWithError()
    {
        bool ok = ImportEnvelopeReader.TryReadManifest(Encoding.UTF8.GetBytes("not json at all"), out ExportManifest? manifest, out string? error);

        ok.ShouldBeFalse();
        manifest.ShouldBeNull();
        error.ShouldNotBeNull();
    }

    [Fact]
    public void TryReadManifest_MissingManifest_ReturnsFalse()
    {
        bool ok = ImportEnvelopeReader.TryReadManifest(Encoding.UTF8.GetBytes("""{ "memoryUnits": [] }"""), out ExportManifest? manifest, out string? error);

        ok.ShouldBeFalse();
        manifest.ShouldBeNull();
        error.ShouldNotBeNull();
    }

    [Fact]
    public void Parse_TenantEnvelope_NormalizesEverything()
    {
        ImportEnvelope envelope = ImportEnvelopeReader.Parse(Encoding.UTF8.GetBytes(TenantEnvelope));

        envelope.Manifest.Scope.ShouldBe(ExportScope.Tenant);
        envelope.Cases.Count.ShouldBe(1);
        envelope.Cases[0].Case.Id.ShouldBe("case-1");
        envelope.Cases[0].Members.Count.ShouldBe(1);
        envelope.Cases[0].Members[0].MemberId.ShouldBe("user-1");
        envelope.Cases[0].Members[0].MemberType.ShouldBe(CaseMemberType.User);
        envelope.MemoryUnits.Count.ShouldBe(2);
        envelope.MemoryUnits[0].Unit.Id.ShouldBe("mu-1");
        envelope.Edges.Count.ShouldBe(1);
        envelope.Edges[0].SourceId.ShouldBe("mu-1");
        envelope.Edges[0].TargetId.ShouldBe("mu-2");
        envelope.Edges[0].EdgeType.ShouldBe("causedBy");
        envelope.Edges[0].VerifiedBy.ShouldBe("reviewer-1");
        envelope.Edges[0].PreviousConfidence.ShouldBe(0.5f);
        envelope.Statistics.ShouldNotBeNull();
        envelope.Statistics.CaseCount.ShouldBe(1);
    }

    [Fact]
    public void Parse_CaseEnvelope_NormalizesSingleCaseIntoCasesList()
    {
        ImportEnvelope envelope = ImportEnvelopeReader.Parse(Encoding.UTF8.GetBytes(CaseEnvelope));

        envelope.Manifest.Scope.ShouldBe(ExportScope.Case);
        envelope.Tenant.ShouldBeNull();
        envelope.Cases.Count.ShouldBe(1);
        envelope.Cases[0].Case.Id.ShouldBe("case-1");
        envelope.Cases[0].Members[0].MemberType.ShouldBe(CaseMemberType.Role);
        envelope.MemoryUnits.Count.ShouldBe(1);
        envelope.Edges.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_MissingManifest_Throws()
    {
        ImportEnvelopeException ex = Should.Throw<ImportEnvelopeException>(
            () => ImportEnvelopeReader.Parse(Encoding.UTF8.GetBytes("""{ "memoryUnits": [] }""")));

        ex.Code.ShouldBe("MISSING_MANIFEST");
    }
}
