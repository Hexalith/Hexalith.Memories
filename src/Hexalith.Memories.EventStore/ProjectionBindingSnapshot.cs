// <copyright file="ProjectionBindingSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Tenant-scoped projection binding snapshot returned by <see cref="IProjectionBindingProvider"/>.</summary>
/// <param name="TenantId">The tenant boundary represented by the snapshot.</param>
/// <param name="Authority">Whether the snapshot can authoritatively prove binding absence.</param>
/// <param name="Bindings">Projection bindings exposed for the selected tenant boundary.</param>
public sealed record ProjectionBindingSnapshot(
    string TenantId,
    ProjectionBindingRegistryAuthority Authority,
    IReadOnlyList<ProjectionBinding> Bindings);
