namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using StackExchange.Redis;

/// <summary>Explicit fixture-owner reconciliation for the exact synthetic keys created by these controls.</summary>
internal static class OwnedSemanticInventory
{
    /// <summary>Attests only fixture-known unit inventories; it grants no production coverage authority.</summary>
    internal static async Task ReconcileAsync(IDatabase db, string tenant, params string[] units)
    {
        foreach (string unit in units)
        {
            await db.SetAddAsync(SourcePublicationVisibility.Keys(tenant, unit),
                [(RedisValue)IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit),
                    (RedisValue)IndexSchemaDefinitions.BuildSemanticKey(tenant, unit),
                    (RedisValue)IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, unit, 0),
                    (RedisValue)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit),
                    (RedisValue)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit)]);
            await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit), "legacy-owner-reconciled-v1");
        }
    }
}
