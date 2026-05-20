// <copyright file="IProjectionBindingProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Provides tenant-scoped runtime projection binding metadata for handler mismatch diagnostics.</summary>
public interface IProjectionBindingProvider
{
    /// <summary>Gets runtime projection bindings for the selected tenant boundary.</summary>
    /// <param name="tenantId">The tenant identifier whose bindings are being inspected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider snapshot and authority posture for the selected tenant.</returns>
    ValueTask<ProjectionBindingSnapshot> GetBindingsAsync(string tenantId, CancellationToken cancellationToken);
}
