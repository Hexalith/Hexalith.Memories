// <copyright file="GetTenantRegistryActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

/// <summary>DAPR Workflow activity that retrieves tenant info from the registry.</summary>
public sealed class GetTenantRegistryActivity : WorkflowActivity<string, TenantInfo?>
{
    private readonly TenantRegistryService _registry;

    /// <summary>Initializes a new instance of the <see cref="GetTenantRegistryActivity"/> class.</summary>
    /// <param name="registry">The tenant registry service.</param>
    public GetTenantRegistryActivity(TenantRegistryService registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public override async Task<TenantInfo?> RunAsync(WorkflowActivityContext context, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await _registry.GetTenantAsync(tenantId, CancellationToken.None).ConfigureAwait(false);
    }
}
