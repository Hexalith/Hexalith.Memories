// <copyright file="DedupKeyBuilder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using System.Security.Cryptography;
using System.Text;

/// <summary>Builds deterministic dedup keys for ingestion idempotency.</summary>
internal static class DedupKeyBuilder
{
    /// <summary>Builds a dedup key from tenant, case, and source URI (the natural-key identity).</summary>
    internal static string BuildKey(string tenantId, string caseId, string sourceUri)
        => $"dedup:{tenantId}:{caseId}:{ComputeHash(sourceUri)}";

    /// <summary>
    /// Builds the token-keyed dedup key from tenant, case, and an explicit idempotency token (Story 18.4).
    /// Distinct namespace (<c>:tok:</c>) so the token record <em>augments</em> rather than replaces the
    /// <see cref="BuildKey"/> <c>sourceUri → MemoryUnitId</c> mapping that Stories 18.5/18.6 depend on.
    /// </summary>
    internal static string BuildTokenKey(string tenantId, string caseId, string idempotencyToken)
        => $"dedup:{tenantId}:{caseId}:tok:{ComputeHash(idempotencyToken)}";

    /// <summary>
    /// Builds the dedup-identity key honoring token precedence: the token key when a non-blank
    /// <paramref name="idempotencyToken"/> is supplied, otherwise the <paramref name="sourceUri"/> natural key.
    /// Used by the REST ingress reservation so concurrent same-identity ingests collide on one key.
    /// </summary>
    internal static string BuildIdentityKey(string tenantId, string caseId, string sourceUri, string? idempotencyToken)
        => string.IsNullOrWhiteSpace(idempotencyToken)
            ? BuildKey(tenantId, caseId, sourceUri)
            : BuildTokenKey(tenantId, caseId, idempotencyToken);

    internal static string ComputeHash(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}
