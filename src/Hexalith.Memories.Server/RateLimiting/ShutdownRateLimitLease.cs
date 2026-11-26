// <copyright file="ShutdownRateLimitLease.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.RateLimiting;

using System.Threading.RateLimiting;

/// <summary>Represents a permit denied because the shared limiter is shutting down.</summary>
internal sealed class ShutdownRateLimitLease : RateLimitLease
{
    private ShutdownRateLimitLease()
    {
    }

    /// <summary>Gets the reusable failed lease.</summary>
    internal static ShutdownRateLimitLease Instance { get; } = new();

    /// <inheritdoc />
    public override bool IsAcquired => false;

    /// <inheritdoc />
    public override IEnumerable<string> MetadataNames => [];

    /// <inheritdoc />
    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;
        return false;
    }
}
