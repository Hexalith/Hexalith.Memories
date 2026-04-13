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

    /// <summary>Creates or updates a case node with full metadata (used by CaseService).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildMergeCaseNode(
        string caseId, string name, string tenantId, DateTimeOffset createdAt);

    /// <summary>Counts memory units linked to a case via CONTAINS edges.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildCountCaseMemoryUnits(string caseId);

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

    /// <summary>Lists all memory unit IDs linked to a case via CONTAINS edges.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildListCaseMemoryUnitIds(string caseId);

    /// <summary>Deletes a case node and all its remaining relationships.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildDeleteCaseNode(string caseId);

    /// <summary>Deletes a memory unit node and all its relationships (compensation).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildDeleteMemoryUnitNode(string memoryUnitId);

    /// <summary>Counts memory unit nodes in the tenant graph.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildCountMemoryUnits();

    /// <summary>Builds a bidirectional graph traversal query from a starting node up to depth.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildTraverseFromNode(
        string startNodeId, int depth);

    /// <summary>Builds a bidirectional graph traversal query with optional case scoping.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildTraverseFromNode(
        string startNodeId, int depth, string? caseId);

    /// <summary>Builds a traversal query that returns both node properties and edge metadata.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildTraverseWithEdges(
        string startNodeId, int depth);

    /// <summary>Builds a traversal query that returns both node properties and edge metadata, with optional case scoping.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildTraverseWithEdges(
        string startNodeId, int depth, string? caseId);

    /// <summary>Counts annotations linked to a memory unit via ANNOTATES edges.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildCountAnnotations(string memoryUnitId);

    /// <summary>Lists annotation memory unit IDs linked to a memory unit via ANNOTATES edges.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildListAnnotationIds(string memoryUnitId);

    /// <summary>Batch-counts annotations for multiple memory units in a single query.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildBatchCountAnnotations(IReadOnlyList<string> memoryUnitIds);
}
