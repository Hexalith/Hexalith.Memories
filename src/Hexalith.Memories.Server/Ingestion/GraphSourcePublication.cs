namespace Hexalith.Memories.Server.Ingestion;

using StackExchange.Redis;

/// <summary>Native graph filtering uses a monotonic source pointer; late candidate writes never change publication authority.</summary>
internal static class GraphSourcePublication
{
    internal const string NativeFilter = " UNWIND nodes(p) AS pathNode OPTIONAL MATCH (publication:SourcePublication) WHERE publication.caseId = pathNode.caseId AND publication.sourceUri = pathNode.sourceUri WITH n, p, collect(publication IS NULL OR coalesce(publication.managed, false) = false OR publication.currentId = pathNode.id) AS visiblePath WHERE all(visible IN visiblePath WHERE visible) ";

    internal static async Task SetCurrentAsync(IConnectionMultiplexer graph, string tenant, string caseId, string source, string unit, long revision)
    {
        await new NFalkorDB.FalkorDB(graph.GetDatabase()).SelectGraph(tenant).QueryAsync(
            "MERGE (s:SourcePublication {caseId:$case,sourceUri:$source}) ON CREATE SET s.revision=-1 WITH s OPTIONAL MATCH (erased:MemoryUnitTombstone) WHERE erased.id=s.currentId WITH s,erased WHERE coalesce(s.managed,false)=false OR coalesce(s.revision,-1) < $revision OR (s.revision=$revision AND (s.currentId=$unit OR erased IS NOT NULL)) SET s.managed=true,s.revision=$revision,s.currentId=$unit",
            new Dictionary<string, object> { ["unit"] = unit, ["case"] = caseId, ["source"] = source, ["revision"] = revision }).ConfigureAwait(false);
    }
}
