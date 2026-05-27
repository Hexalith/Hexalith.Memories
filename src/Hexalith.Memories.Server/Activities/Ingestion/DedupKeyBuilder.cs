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
    /// <summary>Builds a dedup key from tenant, case, and source URI.</summary>
    internal static string BuildKey(string tenantId, string caseId, string sourceUri)
        => $"dedup:{tenantId}:{caseId}:{ComputeHash(sourceUri)}";

    internal static string ComputeHash(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}
