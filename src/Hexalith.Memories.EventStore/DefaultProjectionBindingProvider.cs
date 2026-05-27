// <copyright file="DefaultProjectionBindingProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Default projection registry provider. It is deliberately non-noisy until a host supplies authoritative bindings.</summary>
public sealed class DefaultProjectionBindingProvider : IProjectionBindingProvider
{
    /// <inheritdoc />
    public ValueTask<ProjectionBindingSnapshot> GetBindingsAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new ProjectionBindingSnapshot(
            tenantId,
            ProjectionBindingRegistryAuthority.Unknown,
            Array.Empty<ProjectionBinding>()));
    }
}
