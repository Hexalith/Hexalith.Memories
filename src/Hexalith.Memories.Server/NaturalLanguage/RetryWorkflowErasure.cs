namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Text.Json;
using Dapr.Workflow;

/// <summary>Physically removes historic raw retry inputs after the unit's admission fence is installed.</summary>
internal static class RetryWorkflowErasure
{
    /// <summary>Purges both scoped and historic addresses only when their authoritative stored input belongs to this unit.</summary>
    internal static async Task PurgeAsync(IDaprWorkflowClient? client, string tenant, string unit, CancellationToken token, bool legacySchedulersDrained = false)
    {
        if (client is null)
        {
            throw new InvalidOperationException("nl-retry-workflow-client-required");
        }
        await PurgeAddressAsync(client,
            NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit),
            tenant, unit, false, legacySchedulersDrained, token).ConfigureAwait(false);
        await PurgeAddressAsync(client,
            NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, unit),
            tenant, unit, true, legacySchedulersDrained, token).ConfigureAwait(false);
    }

    private static async Task PurgeAddressAsync(
        IDaprWorkflowClient client, string id, string tenant, string unit, bool legacy, bool legacySchedulersDrained, CancellationToken token)
    {
        WorkflowState? state = await client.GetWorkflowStateAsync(id, true, token).ConfigureAwait(false);
        if (state is null || !state.Exists)
        {
            return;
        }
        (string actualTenant, string actualUnit) = ReadScope(state);
        if (!StringComparer.Ordinal.Equals(actualTenant, tenant) || !StringComparer.Ordinal.Equals(actualUnit, unit))
        {
            if (legacy)
            {
                // Legacy concatenation aliases different scopes. Foreign history is never ours to mutate.
                return;
            }
            throw new InvalidOperationException("nl-retry-workflow-scope-mismatch");
        }
        if (legacy && !legacySchedulersDrained)
        {
            // Dapr has no execution-conditional mutation; old ID writers must be explicitly quiesced.
            throw new InvalidOperationException("nl-retry-legacy-scheduler-quiescence-required");
        }
        DateTimeOffset createdAt = state.CreatedAt;
        if (!NaturalLanguageEmbeddingRetryHostedService.IsTerminalStatus(state.RuntimeStatus))
        {
            await client.TerminateWorkflowAsync(id, cancellation: token).ConfigureAwait(false);
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            state = await client.WaitForWorkflowCompletionAsync(id, false, deadline.Token).ConfigureAwait(false);
            if (state is null || !NaturalLanguageEmbeddingRetryHostedService.IsTerminalStatus(state.RuntimeStatus))
            {
                throw new InvalidOperationException("nl-retry-workflow-termination-unconfirmed");
            }
        }
        // Re-read authoritative input after termination; a replacement instance must not inherit purge authority.
        state = await client.GetWorkflowStateAsync(id, true, token).ConfigureAwait(false);
        if (state is null || !state.Exists)
        {
            return;
        }
        (actualTenant, actualUnit) = ReadScope(state);
        if (state.CreatedAt != createdAt
            || !StringComparer.Ordinal.Equals(actualTenant, tenant)
            || !StringComparer.Ordinal.Equals(actualUnit, unit)
            || !NaturalLanguageEmbeddingRetryHostedService.IsTerminalStatus(state.RuntimeStatus))
        {
            throw new InvalidOperationException("nl-retry-workflow-instance-changed");
        }
        await client.PurgeInstanceAsync(id, token).ConfigureAwait(false);
        state = await client.GetWorkflowStateAsync(id, true, token).ConfigureAwait(false);
        if (state is { Exists: true })
        {
            throw new InvalidOperationException("nl-retry-workflow-purge-unconfirmed");
        }
    }

    private static (string Tenant, string Unit) ReadScope(WorkflowState state)
    {
        try
        {
            JsonElement input = state.ReadInputAs<JsonElement>();
            string? tenant = null;
            string? unit = null;
            int tenantFields = 0;
            int unitFields = 0;
            if (input.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in input.EnumerateObject())
                {
                    if (property.Name.Equals("TenantId", StringComparison.OrdinalIgnoreCase))
                    {
                        tenantFields++;
                        tenant = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
                    }
                    else if (property.Name.Equals("MemoryUnitId", StringComparison.OrdinalIgnoreCase))
                    {
                        unitFields++;
                        unit = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
                    }
                }
            }
            if (tenantFields == 1 && unitFields == 1
                && !string.IsNullOrWhiteSpace(tenant) && !string.IsNullOrWhiteSpace(unit))
            {
                return (tenant, unit);
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            throw new InvalidOperationException("nl-retry-workflow-scope-unverifiable", exception);
        }
        throw new InvalidOperationException("nl-retry-workflow-scope-unverifiable");
    }
}
