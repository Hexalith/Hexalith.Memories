// <copyright file="LensPacketFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Tests.Components.Evidence;

/// <summary>
/// Story 17.4 — the canonical bounded fixture inventory shared by every lens test. Built on the
/// Story 2.7-aligned Story 17.1 fixtures plus the Story 17.2/17.3 recovery and interaction examples; this
/// slice defines no new evidence, state, recovery, benchmark, activity, or MCP schema semantics.
/// </summary>
internal static class LensPacketFixtures
{
    /// <summary>Happy packet: a confident, complete answer.</summary>
    public static EvidencePacket Happy() => EvidencePacketFixtures.CompletePacket();

    /// <summary>Degraded packet: an answer exists but a backend or axis was degraded.</summary>
    public static EvidencePacket Degraded() => EvidencePacketFixtures.DegradedPacket();

    /// <summary>Unauthorized packet: the caller is not authorized for the requested scope.</summary>
    public static EvidencePacket Unauthorized() => EvidencePacketFixtures.UnauthorizedPacket();

    /// <summary>Unknown-scope packet: isolation is unknown and treated as restrictively as unauthorized.</summary>
    public static EvidencePacket UnknownScope() => EvidencePacketFixtures.UnknownScopePacket();

    /// <summary>Redacted packet: a field was redacted upstream.</summary>
    public static EvidencePacket Redacted() => EvidencePacketFixtures.RedactedPacket();

    /// <summary>Omitted/compressed packet: details were omitted under a token budget.</summary>
    public static EvidencePacket Compressed() => EvidencePacketFixtures.CompressedPacket();

    /// <summary>Stale packet: available evidence may be old or superseded.</summary>
    public static EvidencePacket Stale() => EvidencePacketFixtures.StalePacket();

    /// <summary>Empty packet: search ran in scope and matched nothing.</summary>
    public static EvidencePacket Empty() => EvidencePacketFixtures.EmptyPacket();

    /// <summary>Not-ingested packet: no indexed memory units exist yet.</summary>
    public static EvidencePacket NotIngested()
    {
        EvidencePacket packet = EvidencePacketFixtures.EmptyPacket();
        return packet with { Result = packet.Result with { HasIndexedMemoryUnits = false } };
    }

    /// <summary>Invalid/schema-mismatch packet: an out-of-range state value.</summary>
    public static EvidencePacket SchemaMismatch()
        => EvidencePacketFixtures.CompletePacket() with { State = (EvidencePacketState)999 };

    /// <summary>Cross-tenant packet: scoped to a different tenant/case than the active one.</summary>
    public static EvidencePacket CrossTenant()
        => EvidencePacketFixtures.CompletePacket() with
        {
            Scope = new EvidencePacketScope(
                TenantId: "tenant-b",
                CaseId: "case-b",
                IsolationStatus: EvidencePacketIsolationStatus.Authorized,
                PermissionsContext: "tenant-case"),
        };

    /// <summary>Missing-source packet: a source with no source URI or memory unit identifier.</summary>
    public static EvidencePacket MissingSource()
        => EvidencePacketFixtures.CompletePacket() with
        {
            Sources =
            [
                new EvidencePacketSource(
                    Rank: 1,
                    MemoryUnitId: string.Empty,
                    SourceUri: string.Empty,
                    SourceType: SourceType.File,
                    Snippet: "Relevant source snippet",
                    Score: 0.5d,
                    CaseId: "case-a",
                    CaseName: "Case A",
                    AnnotationsCount: 1),
            ],
        };

    /// <summary>Sensitive packet: secrets and paths embedded in source URI and snippet.</summary>
    public static EvidencePacket Sensitive() => EvidencePacketFixtures.SensitivePacket();

    /// <summary>Tenant/case-sensitive packet: secrets and paths embedded in tenant and case identifiers.</summary>
    public static EvidencePacket TenantCaseSensitive() => EvidencePacketFixtures.TenantCaseSensitivePacket();

    /// <summary>Every fixture in the bounded inventory, for cross-cutting sweeps.</summary>
    public static IEnumerable<EvidencePacket> All()
    {
        yield return Happy();
        yield return Degraded();
        yield return Unauthorized();
        yield return UnknownScope();
        yield return Redacted();
        yield return Compressed();
        yield return Stale();
        yield return Empty();
        yield return NotIngested();
        yield return SchemaMismatch();
        yield return CrossTenant();
        yield return MissingSource();
        yield return Sensitive();
        yield return TenantCaseSensitive();
    }
}
