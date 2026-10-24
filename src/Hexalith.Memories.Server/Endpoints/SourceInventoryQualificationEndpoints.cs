namespace Hexalith.Memories.Server.Endpoints;

using System.Diagnostics;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.DerivedStores;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

/// <summary>Explicitly enabled non-Production owner controls for fresh, synthetic source-inventory qualification.</summary>
internal static class SourceInventoryQualificationEndpoints
{
    internal static WebApplication MapSourceInventoryQualificationEndpoints(this WebApplication app)
    {
        if (app.Environment.IsProduction() || !app.Configuration.GetValue("Memories:Testing:EnableSourceInventoryQualification", false)
            || !app.Configuration.GetValue("Memories:Testing:UseFakeEmbedding", false)) { return app; }
        string route = SourceInventoryEndpoints.Route;
        app.MapPost(route + "/qualify-owned-test-scope", async (SourceInventoryStore inventory, TenantRegistryService tenants,
            [FromKeyedServices("redis")] IConnectionMultiplexer redis, string tenantId, [FromQuery] string sourceUri, CancellationToken token) =>
        {
            if (!OwnedScope(tenantId, sourceUri)) { return Results.BadRequest(); }
            TenantInfo? tenant = await tenants.GetTenantAsync(tenantId, token).ConfigureAwait(false);
            using Process serverProcess = Process.GetCurrentProcess();
            if (tenant is null || tenant.CreatedAt.UtcDateTime < serverProcess.StartTime.ToUniversalTime())
            {
                return Results.Conflict(new ErrorResponse("OWNED_TEST_SCOPE_REQUIRED", "The controlled tenant must have been created by this owned server session.", "Use a fresh synthetic tenant; legacy coverage cannot be initialized here."));
            }
            SourceInventoryPage before = await inventory.ReadAsync(tenantId, sourceUri, null, 100, token).ConfigureAwait(false);
            if (before.Entries.Count != 0 || before.NextCursor is not null || before.CoverageComplete || before.Permanent) { return Results.Conflict(); }
            int inspected = 0;
            foreach (System.Net.EndPoint endpoint in redis.GetEndPoints())
            {
                IServer server = redis.GetServer(endpoint);
                if (!server.IsConnected) { return Results.StatusCode(503); }
                await foreach (RedisKey key in server.KeysAsync(pattern: IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "*"), pageSize: 100).WithCancellation(token))
                {
                    if (++inspected > 2048) { return Results.Conflict(); }
                    RedisValue candidateSource = await redis.GetDatabase().HashGetAsync(key, "sourceUri").WaitAsync(token).ConfigureAwait(false);
                    if (candidateSource.ToString() == sourceUri) { return Results.Conflict(); }
                }
            }
            await inventory.FenceAsync(tenantId, sourceUri, false, token).ConfigureAwait(false);
            SourceInventoryPage fenced = await inventory.ReadAsync(tenantId, sourceUri, null, 100, token).ConfigureAwait(false);
            if (fenced.Entries.Count != 0 || fenced.NextCursor is not null || fenced.Permanent) { return Results.Conflict(); }
            await inventory.ReconcileAsync(tenantId, sourceUri, fenced.Version, [], "owned-synthetic-empty-scope:" + Guid.NewGuid().ToString("N"), token).ConfigureAwait(false);
            SourceInventoryPage reconciled = await inventory.ReadAsync(tenantId, sourceUri, null, 100, token).ConfigureAwait(false);
            await inventory.ReopenAsync(tenantId, sourceUri, reconciled.Version, token).ConfigureAwait(false);
            return Results.NoContent();
        }).AddEndpointFilter<TenantAuthorizationEndpointFilter>()
          .RequireAuthorization(policy => policy.RequireClaim("owned_source_inventory_test", "true"));

        app.MapGet(route + "/owned-derived-state", async (SourceInventoryStore inventory, IWorkflowPayloadStore payloads,
            [FromKeyedServices("redis")] IConnectionMultiplexer redis, [FromKeyedServices("falkordb")] IConnectionMultiplexer graph,
            string tenantId, [FromQuery] string sourceUri, CancellationToken token) =>
        {
            if (!OwnedScope(tenantId, sourceUri)) { return Results.BadRequest(); }
            SourceInventoryPage page = await inventory.ReadAsync(tenantId, sourceUri, null, 100, token).ConfigureAwait(false);
            if (page.NextCursor is not null) { return Results.Conflict(); }
            string[] ids = page.Entries.SelectMany(entry => new[] { entry.MemoryUnitId, entry.CandidateMemoryUnitId, entry.WorkflowInstanceId, entry.SourcePayloadReference?.MemoryUnitId }).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
            IDatabase db = redis.GetDatabase();
            int syntactic = 0, semantic = 0, naturalLanguage = 0, artifacts = 0;
            long graphCount = 0;
            int failedUnits = 0, sourcePayloads = 0;
            long retainedPayloadReferences = 0;
            foreach (WorkflowPayloadReference reference in page.Entries.Select(entry => entry.SourcePayloadReference).OfType<WorkflowPayloadReference>().Distinct())
            {
                if (reference.TenantId != tenantId) { return Results.Conflict(); }
                try { _ = await payloads.ReadAsync(reference, tenantId, reference.MemoryUnitId, reference.ContentKind, token).ConfigureAwait(false); sourcePayloads++; }
                catch (WorkflowPayloadException error) when (error.ErrorCode == "PAYLOAD_NOT_FOUND") { }
            }
            foreach (string id in ids)
            {
                retainedPayloadReferences += await db.SetLengthAsync(WorkflowPayloadLedger.Key(tenantId, id)).WaitAsync(token).ConfigureAwait(false);
                if (await db.KeyExistsAsync(Hexalith.Memories.Server.Activities.Ingestion.PersistFailedUnitActivity.BuildHashKey(tenantId, id)).WaitAsync(token).ConfigureAwait(false)) { failedUnits++; }
                if (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, id)).WaitAsync(token).ConfigureAwait(false)) { syntactic++; }
                if (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticKey(tenantId, id)).WaitAsync(token).ConfigureAwait(false)) { semantic++; }
                if (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, id)).WaitAsync(token).ConfigureAwait(false)) { naturalLanguage++; }
                if (await db.KeyExistsAsync(RedisDerivedStoreService.BuildSourceArtifactKey(tenantId, id)).WaitAsync(token).ConfigureAwait(false)) { artifacts++; }
                int chunks = 0;
                foreach (System.Net.EndPoint endpoint in redis.GetEndPoints())
                {
                    IServer server = redis.GetServer(endpoint);
                    if (!server.IsConnected) { return Results.StatusCode(503); }
                    await foreach (RedisKey key in server.KeysAsync(pattern: IndexSchemaDefinitions.BuildSemanticChunkKeyPattern(tenantId, id), pageSize: 100).WithCancellation(token))
                    {
                        if (++chunks > SourcePublicationVisibility.KeyCapacity) { return Results.Conflict(); }
                        semantic++;
                    }
                }
                NFalkorDB.ResultSet result = await new NFalkorDB.FalkorDB(graph.GetDatabase()).SelectGraph(tenantId)
                    .QueryAsync("MATCH (m:MemoryUnit {id:$id}) RETURN count(m)", new Dictionary<string, object> { ["id"] = id }).WaitAsync(token).ConfigureAwait(false);
                graphCount += Convert.ToInt64(result.First().Values[0], System.Globalization.CultureInfo.InvariantCulture);
            }
            return Results.Ok(new { SourceInventoryVersion = page.Version, UnitCount = ids.Length, Syntactic = syntactic,
                Semantic = semantic, NaturalLanguage = naturalLanguage, SourceArtifacts = artifacts, Graph = graphCount,
                FailedUnits = failedUnits, SourcePayloads = sourcePayloads, RetainedPayloadReferences = retainedPayloadReferences,
                AdmissionDelayMilliseconds = Math.Clamp(app.Configuration.GetValue("Memories:Testing:SourceInventoryAdmissionDelayMilliseconds", 0), 0, 5000),
                OutputDelayMilliseconds = Math.Clamp(app.Configuration.GetValue("Memories:Testing:SourceInventoryOutputDelayMilliseconds", 0), 0, 5000),
                DeletionDelayMilliseconds = Math.Clamp(app.Configuration.GetValue("Memories:Testing:SourceInventoryDeletionDelayMilliseconds", 0), 0, 5000) });
        }).AddEndpointFilter<TenantAuthorizationEndpointFilter>()
          .RequireAuthorization(policy => policy.RequireClaim("owned_source_inventory_test", "true"));
        return app;
    }

    private static bool OwnedScope(string tenant, string source)
        => tenant.StartsWith("parties-recovery-", StringComparison.Ordinal)
            && source.StartsWith("urn:hexalith:parties:" + tenant + ":party:", StringComparison.Ordinal);
}
