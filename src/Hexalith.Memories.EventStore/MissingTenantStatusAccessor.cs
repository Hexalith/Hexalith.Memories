// <copyright file="MissingTenantStatusAccessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Placeholder tenant-status accessor used until the host supplies a concrete adapter.</summary>
internal sealed class MissingTenantStatusAccessor : ITenantStatusAccessor
{
    public Task<EventStoreTenantStatus?> GetStatusAsync(string tenantId, CancellationToken cancellationToken)
        => Task.FromException<EventStoreTenantStatus?>(new InvalidOperationException(
            "EventStore integration requires a concrete ITenantStatusAccessor. "
            + "Register one by calling AddMemoriesEventStoreIntegration(..., builder => builder.AddTenantStatusAccessor<TImplementation>())."));
}
