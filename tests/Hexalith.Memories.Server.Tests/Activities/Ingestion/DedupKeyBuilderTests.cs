// <copyright file="DedupKeyBuilderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Hexalith.Memories.Server.Activities.Ingestion;

using Shouldly;

/// <summary>
/// Story 18.4 (AC2) — direct coverage of the dedup-key derivation that is the central design decision:
/// a supplied idempotency token <em>augments</em> (never replaces) the <c>sourceUri</c> natural-key mapping
/// that Stories 18.5/18.6 depend on, via a distinct <c>:tok:</c> namespace and documented token-precedence /
/// sourceUri-fallback. These tests pin the key shapes/invariants the activity, the workflow, and the REST
/// ingress reservation all rely on (and which were previously only asserted indirectly).
/// </summary>
public class DedupKeyBuilderTests
{
    [Fact]
    public void BuildKey_ShouldFormatAsDedupTenantCaseSourceUriHash()
    {
        string key = DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf");

        string expectedHash = DedupKeyBuilder.ComputeHash("file:///doc.pdf");
        key.ShouldBe($"dedup:tenant-1:case-1:{expectedHash}");
    }

    [Fact]
    public void BuildKey_ShouldBeDeterministicForSameInputs()
        => DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf")
            .ShouldBe(DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf"));

    [Fact]
    public void BuildTokenKey_ShouldUseDistinctTokNamespace()
    {
        string key = DedupKeyBuilder.BuildTokenKey("tenant-1", "case-1", "idem-xyz");

        string expectedHash = DedupKeyBuilder.ComputeHash("idem-xyz");
        key.ShouldBe($"dedup:tenant-1:case-1:tok:{expectedHash}");
    }

    [Fact]
    public void BuildTokenKey_ShouldNeverCollideWithSourceUriKey_EvenWhenTokenEqualsSourceUri()
    {
        // Cross-story invariant (18.5/18.6): the token record must AUGMENT, not REPLACE, the sourceUri
        // record. Even if a token literally equals a sourceUri string, the ":tok:" namespace keeps the two
        // records distinct so the permanent sourceUri -> MemoryUnitId mapping is preserved.
        const string identical = "file:///doc.pdf";

        DedupKeyBuilder.BuildTokenKey("tenant-1", "case-1", identical)
            .ShouldNotBe(DedupKeyBuilder.BuildKey("tenant-1", "case-1", identical));
    }

    [Fact]
    public void BuildIdentityKey_WithToken_ShouldReturnTokenKey()
        => DedupKeyBuilder.BuildIdentityKey("tenant-1", "case-1", "file:///doc.pdf", "idem-xyz")
            .ShouldBe(DedupKeyBuilder.BuildTokenKey("tenant-1", "case-1", "idem-xyz"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildIdentityKey_WithoutToken_ShouldFallBackToSourceUriKey(string? token)
        => DedupKeyBuilder.BuildIdentityKey("tenant-1", "case-1", "file:///doc.pdf", token)
            .ShouldBe(DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf"));

    [Fact]
    public void BuildKey_DifferentTenant_ShouldProduceDifferentKey()
        // Tenant isolation (invariant 7): dedup keys stay tenant+case scoped — a token never crosses tenants.
        => DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf")
            .ShouldNotBe(DedupKeyBuilder.BuildKey("tenant-2", "case-1", "file:///doc.pdf"));

    [Fact]
    public void BuildKey_DifferentCase_ShouldProduceDifferentKey()
        => DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf")
            .ShouldNotBe(DedupKeyBuilder.BuildKey("tenant-1", "case-2", "file:///doc.pdf"));

    [Fact]
    public void BuildTokenKey_DifferentTenant_ShouldProduceDifferentKey()
        => DedupKeyBuilder.BuildTokenKey("tenant-1", "case-1", "idem-xyz")
            .ShouldNotBe(DedupKeyBuilder.BuildTokenKey("tenant-2", "case-1", "idem-xyz"));

    [Fact]
    public void ComputeHash_ShouldBeLowercaseHex64Chars()
    {
        string hash = DedupKeyBuilder.ComputeHash("file:///doc.pdf");

        hash.Length.ShouldBe(64); // SHA-256 -> 32 bytes -> 64 hex chars.
        hash.ShouldBe(hash.ToLowerInvariant());
        hash.All(Uri.IsHexDigit).ShouldBeTrue();
    }

    [Fact]
    public void ComputeHash_DifferentInputs_ShouldProduceDifferentHashes()
        => DedupKeyBuilder.ComputeHash("idem-xyz")
            .ShouldNotBe(DedupKeyBuilder.ComputeHash("file:///doc.pdf"));
}
