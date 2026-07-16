// <copyright file="EventStoreDedupKey.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Security.Cryptography;
using System.Text;

/// <summary>Builds the same dedup key used by the Server's <c>DedupKeyBuilder</c>. Duplicated here rather
/// than shared because the Server version is <c>internal</c> and this package intentionally does not
/// reference Server internals. Format: <c>dedup:{tenantId}:{caseId}:{sha256(sourceUri)}</c>.</summary>
internal static class EventStoreDedupKey
{
    internal static string Build(string tenantId, string caseId, string sourceUri)
        => $"dedup:{tenantId}:{caseId}:{ComputeHash(sourceUri)}";

    private static string ComputeHash(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}
