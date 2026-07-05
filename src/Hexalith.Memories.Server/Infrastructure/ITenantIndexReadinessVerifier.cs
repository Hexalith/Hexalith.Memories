// <copyright file="ITenantIndexReadinessVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

using StackExchange.Redis;

/// <summary>Story 23.7 (A34): verifies that a tenant's RediSearch / Redis Vector index already exists and
/// matches the expected schema, memoizing the result per tenant, index family, and schema-sensitive vector
/// dimensions for the lifetime of the current process.
///
/// <para>Ingestion activities call this once before the first hash/vector write for a tenant/index family and
/// then reuse the cached result for every subsequent write, so the hot path stops issuing per-document
/// <c>FT.CREATE</c> calls. Index creation stays owned by <c>TenantProvisioningWorkflow</c>; a missing index is a
/// provisioning inconsistency that fails with <see cref="TenantIndexNotProvisionedException"/> rather than being
/// created on demand.</para>
///
/// <para>The cache is process-local and never persisted. It does not survive a process restart and must not be
/// used as tenant active-status authorization, nor as a substitute for the per-write embedding migration marker
/// checks that semantic activities still perform on every invocation.</para></summary>
public interface ITenantIndexReadinessVerifier
{
    /// <summary>Ensures the tenant index for <paramref name="family"/> exists and matches the expected schema,
    /// verifying once per process and caching success for the tenant/family/dimensions tuple.</summary>
    /// <param name="database">The Redis database used for the one-time <c>FT.INFO</c> verification.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="family">The index family to verify.</param>
    /// <param name="expectedDimensions">The expected embedding vector dimensions for vector families
    /// (<see cref="TenantIndexFamily.Semantic"/> and <see cref="TenantIndexFamily.NaturalLanguageSemantic"/>);
    /// ignored for <see cref="TenantIndexFamily.Syntactic"/>.</param>
    /// <param name="cancellationToken">The cancellation token used by the asynchronous retry delay.</param>
    /// <returns>A task that completes when the index is verified ready.</returns>
    /// <exception cref="TenantIndexNotProvisionedException">The index is missing (not provisioned).</exception>
    /// <exception cref="TenantIndexSchemaMismatchException">The existing index schema is incompatible.</exception>
    Task EnsureReadyAsync(
        IDatabase database,
        string tenantId,
        TenantIndexFamily family,
        int? expectedDimensions,
        CancellationToken cancellationToken);
}
