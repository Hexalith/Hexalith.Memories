# Hexalith.Memories.Redis

Compatibility-only Redis and FalkorDB surface for existing Hexalith.Memories consumers.

The backend implementations live in `Hexalith.Memories.Server`; this package does not provide
storage adapters. New code should reference `NFalkorDB`, `NRedisStack`, and `StackExchange.Redis`
directly. Replace the legacy graph-id extension call:

```csharp
await falkorDb.QueryAsync(graphId, query, parameters, flags, timeout);
```

with the native selected-graph API:

```csharp
await falkorDb.SelectGraph(graphId).QueryAsync(query, parameters, flags, timeout);
```

The package ID, `FalkorDbCompatibilityExtensions`, and `RedisPlaceholder` port constants remain
available for wire and source compatibility. They are deprecated and may be removed only in an
owned breaking major release after downstream consumers have migrated to the native packages.

```powershell
dotnet add package Hexalith.Memories.Redis
```

Application consumers normally use the REST client, CLI, or server deployment surface instead.
