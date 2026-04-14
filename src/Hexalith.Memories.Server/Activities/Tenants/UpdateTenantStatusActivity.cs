// <copyright file="UpdateTenantStatusActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

/// <summary>DAPR Workflow activity that updates the tenant status in the registry.</summary>
public sealed class UpdateTenantStatusActivity : WorkflowActivity<TenantStatusUpdateInput, bool>
{
    private readonly TenantRegistryService _registry;

    /// <summary>Initializes a new instance of the <see cref="UpdateTenantStatusActivity"/> class.</summary>
    /// <param name="registry">The tenant registry service.</param>
    public UpdateTenantStatusActivity(TenantRegistryService registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, TenantStatusUpdateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        await _registry.UpdateTenantStatusAsync(input.TenantId, input.Status, CancellationToken.None).ConfigureAwait(false);
        return true;
    }
}
