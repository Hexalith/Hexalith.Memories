// <copyright file="TenantIndexReadinessException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

/// <summary>Story 23.7 (A34): base for structured tenant index-readiness failures raised by
/// <see cref="ITenantIndexReadinessVerifier"/>. Derives from <see cref="InvalidOperationException"/> so existing
/// callers and tests that expect an <see cref="InvalidOperationException"/> on schema drift keep working, while
/// the tenant id and index family remain available for actionable, non-secret diagnostics.</summary>
public abstract class TenantIndexReadinessException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="TenantIndexReadinessException"/> class.</summary>
    /// <param name="tenantId">The tenant whose index failed readiness.</param>
    /// <param name="family">The index family that failed readiness.</param>
    /// <param name="message">The structured, non-secret failure message.</param>
    protected TenantIndexReadinessException(string tenantId, TenantIndexFamily family, string message)
        : base(message)
    {
        TenantId = tenantId;
        Family = family;
    }

    /// <summary>Gets the tenant whose index failed readiness.</summary>
    public string TenantId { get; }

    /// <summary>Gets the index family that failed readiness.</summary>
    public TenantIndexFamily Family { get; }
}
