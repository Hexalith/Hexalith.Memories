// <copyright file="RestoreReindexBatchInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Bounded re-index page input; identifiers remain in staging rather than workflow history.</summary>
/// <param name="TenantId">The target tenant.</param>
/// <param name="StagingKey">The restore staging key that owns the identifier list.</param>
/// <param name="Offset">Zero-based identifier offset.</param>
/// <param name="BatchSize">Maximum identifiers processed by this activity.</param>
public sealed record RestoreReindexBatchInput(
    string TenantId,
    string StagingKey,
    long Offset,
    int BatchSize);
