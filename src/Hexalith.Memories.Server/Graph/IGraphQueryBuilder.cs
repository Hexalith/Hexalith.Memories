namespace Hexalith.Memories.Server.Graph;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Safety interface that structurally prevents Cypher injection by requiring
/// parameterized queries for all FalkorDB operations (Decision D9).
/// </summary>
public interface IGraphQueryBuilder
{
    /// <summary>Merges a memory unit node (idempotent — creates or updates).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildMergeMemoryUnitNode(
        string memoryUnitId,
        string caseId,
        string content,
        string contentHash,
        string sourceUri,
        SourceType sourceType,
        string embeddingProvider,
        int embeddingDimensions,
        string ingestedBy,
        DateTimeOffset ingestedAt,
        string metadataJson);

    /// <summary>Creates a case node if it doesn't exist (MERGE pattern).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildMergeCaseNode(string caseId);

    /// <summary>Creates a typed edge between two nodes (idempotent via MERGE).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildMergeEdge(
        string sourceNodeId,
        string targetNodeId,
        EdgeType edgeType,
        float confidence,
        EdgeOrigin origin);

    /// <summary>Creates a stub node for a referenced memory unit that may not be ingested yet.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildMergeStubNode(string memoryUnitId);

    /// <summary>Checks whether a memory unit node exists in the graph.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildCheckMemoryUnitExists(string memoryUnitId);

    /// <summary>Deletes a memory unit node and all its relationships (compensation).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildDeleteMemoryUnitNode(string memoryUnitId);

    /// <summary>Counts memory unit nodes in the tenant graph.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildCountMemoryUnits();

    /// <summary>Builds a bidirectional graph traversal query from a starting node up to depth.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildTraverseFromNode(
        string startNodeId, int depth);
}
