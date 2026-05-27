// <copyright file="TenantIsolationVerificationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Aggregated result of all tenant isolation verification checks.</summary>
/// <param name="TenantId">The tenant that was verified.</param>
/// <param name="VerifiedAt">The timestamp when verification was performed.</param>
/// <param name="AllPassed">Whether all checks passed.</param>
/// <param name="Summary">A human-readable overview of the verification results.</param>
/// <param name="Checks">The individual check results.</param>
public sealed record TenantIsolationVerificationResult(
    string TenantId,
    DateTimeOffset VerifiedAt,
    bool AllPassed,
    string Summary,
    IReadOnlyList<TenantIsolationCheckResult> Checks);
