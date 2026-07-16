// <copyright file="InitializeTenantRegistryActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

/// <summary>DAPR Workflow activity that atomically checks tenant existence and registers if not found.
/// Handles DAPR Workflow replay safety by treating Provisioning status as an in-flight replay.</summary>
public sealed partial class InitializeTenantRegistryActivity : WorkflowActivity<InitializeTenantRegistryInput, TenantInfo>
{
    private readonly TenantRegistryService _registry;
    private readonly ILogger<InitializeTenantRegistryActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="InitializeTenantRegistryActivity"/> class.</summary>
    /// <param name="registry">The tenant registry service.</param>
    /// <param name="logger">The logger instance.</param>
    public InitializeTenantRegistryActivity(TenantRegistryService registry, ILogger<InitializeTenantRegistryActivity> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<TenantInfo> RunAsync(WorkflowActivityContext context, InitializeTenantRegistryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        TenantRegistryEntry existing = await _registry
            .RegisterOrGetTenantEntryAsync(input.TenantId, input.DisplayName, input.WorkflowInstanceId, CancellationToken.None)
            .ConfigureAwait(false);

        return existing.Tenant.Status switch
        {
            // The workflow that owns the in-flight provisioning is safe to resume.
            TenantStatus.Provisioning when string.Equals(existing.WorkflowInstanceId, input.WorkflowInstanceId, StringComparison.Ordinal) =>
                ReturnInitialized(input.TenantId, existing.Tenant),

            // Allow retry from failed states and claim ownership for the retrying workflow.
            TenantStatus.Failed or TenantStatus.CompensationFailed =>
                await ResetToProvisioningAsync(input, existing.Tenant).ConfigureAwait(false),

            // Any other owner or terminal state means the tenant already exists.
            _ => throw new InvalidOperationException("TENANT_ALREADY_EXISTS"),
        };
    }

    private TenantInfo ReturnInitialized(string tenantId, TenantInfo tenant)
    {
        LogTenantInitialized(_logger, tenantId);
        return tenant;
    }

    private async Task<TenantInfo> ResetToProvisioningAsync(InitializeTenantRegistryInput input, TenantInfo existing)
    {
        LogRetryingAfterFailure(_logger, input.TenantId, existing.Status);
        await _registry.UpdateTenantStatusAsync(
                input.TenantId,
                TenantStatus.Provisioning,
                CancellationToken.None,
                input.WorkflowInstanceId)
            .ConfigureAwait(false);
        return existing with { Status = TenantStatus.Provisioning };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant '{TenantId}' initialized in registry with Provisioning status")]
    private static partial void LogTenantInitialized(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant '{TenantId}' retrying provisioning from {PreviousStatus} status")]
    private static partial void LogRetryingAfterFailure(ILogger logger, string tenantId, TenantStatus previousStatus);
}
