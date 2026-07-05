// <copyright file="TenantIndexFamily.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

/// <summary>Story 23.7 (A34): the tenant-scoped RediSearch / Redis Vector index families whose readiness
/// is memoized by <see cref="ITenantIndexReadinessVerifier"/>. Provisioning of these indexes remains owned by
/// <c>TenantProvisioningWorkflow</c>; ingestion only verifies they already exist and match the expected schema.</summary>
public enum TenantIndexFamily
{
    /// <summary>The RediSearch syntactic (full-text) index (<c>{tenantId}:memories:idx</c>).</summary>
    Syntactic,

    /// <summary>The raw Redis Vector semantic index (<c>{tenantId}:memories:vec</c>).</summary>
    Semantic,

    /// <summary>The natural-language (LLM-authored) Redis Vector semantic index (<c>{tenantId}:memories:vec:nl</c>).</summary>
    NaturalLanguageSemantic,
}
