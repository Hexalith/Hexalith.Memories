// <copyright file="ITenantEventRouter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Resolves a CloudEvents envelope to a concrete tenant + case + aggregate-type routing outcome.
/// Consumers handle each <see cref="TenantEventRouteResolutionStatus"/> branch explicitly.
/// Kept internal for MVP per ADR 9.1-F — may be promoted to public in a future release when advanced
/// consumers demand custom routers.</summary>
internal interface ITenantEventRouter
{
    /// <summary>Resolves a routing outcome for the given envelope.</summary>
    /// <param name="envelope">The CloudEvents envelope.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A typed resolution describing the routing outcome.</returns>
    Task<TenantEventRouteResolution> ResolveAsync(CloudEventEnvelope envelope, CancellationToken cancellationToken);
}
