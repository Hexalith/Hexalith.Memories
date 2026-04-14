// <copyright file="RemoveTenantRegistryActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Server.Tenants;

/// <summary>DAPR Workflow activity that removes a tenant from the registry during compensation.</summary>
public sealed class RemoveTenantRegistryActivity : WorkflowActivity<string, bool>
{
    private readonly TenantRegistryService _registry;

    /// <summary>Initializes a new instance of the <see cref="RemoveTenantRegistryActivity"/> class.</summary>
    /// <param name="registry">The tenant registry service.</param>
    public RemoveTenantRegistryActivity(TenantRegistryService registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await _registry.RemoveTenantAsync(tenantId, CancellationToken.None).ConfigureAwait(false);
        return true;
    }
}
