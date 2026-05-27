// <copyright file="ReIngestRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Body for POST /failed-units/re-ingest. Either <c>MemoryUnitIds</c> is non-empty (process those)
/// or <c>All=true</c> (process up to <c>Limit</c> most-recent failed units).</summary>
public sealed record ReIngestRequest(
    IReadOnlyList<string>? MemoryUnitIds,
    bool All = false,
    int Limit = 500);
