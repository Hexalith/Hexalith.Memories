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
        string embeddingModel,
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

    /// <summary>Creates a stub node for a referenced memory unit that may not be ingested yet. The 1-arg
    /// overload samples wall-clock <c>DateTimeOffset.UtcNow</c> internally and is therefore NOT safe to
    /// call from workflow bodies (replay non-determinism). Use the 2-arg overload and pass
    /// <c>WorkflowContext.CurrentUtcDateTime</c> from workflows.</summary>
    [Obsolete("Use the 2-arg overload and pass WorkflowContext.CurrentUtcDateTime. The 1-arg form samples wall-clock time internally and is non-deterministic under workflow replay.")]
    (string Query, IDictionary<string, object> Parameters) BuildMergeStubNode(string memoryUnitId);

    /// <summary>Story 9.2 Task 7.1 — creates a stub node with an explicit <c>stubCreatedAt</c> timestamp
    /// used for orphan-detection queries (<c>MATCH (m:MemoryUnit) WHERE m.isStub = true AND
    /// m.stubCreatedAt &lt; threshold</c>). <c>ON CREATE SET</c> ensures the flag is applied only on
    /// first creation; existing real nodes are never regressed to <c>isStub = true</c>.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildMergeStubNode(
        string memoryUnitId,
        DateTimeOffset stubCreatedAt);

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

    /// <summary>
    /// Story 8.2: enumerates all memory unit IDs in the tenant graph. Used by
    /// <c>EnumerateMemoryUnitIdsActivity</c> to detect graph-only orphans.
    /// </summary>
    (string Query, IDictionary<string, object> Parameters) BuildEnumerateMemoryUnitIds();

    /// <summary>
    /// Story 8.2: returns outgoing, incoming and CONTAINS-edge counts for a single
    /// memory unit in one roundtrip. Used by <c>ConsistencyInspectionService</c>.
    /// </summary>
    (string Query, IDictionary<string, object> Parameters) BuildCountMemoryUnitEdges(string memoryUnitId);

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

    /// <summary>Builds a traversal query filtered by edge types, with optional case scoping.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildTraverseWithEdges(
        string startNodeId, int depth, string? caseId, IReadOnlyList<EdgeType>? edgeTypes);

    /// <summary>Builds a query to update an edge's confidence and record the promotion audit trail.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildUpdateEdgeConfidence(
        string sourceNodeId,
        string targetNodeId,
        EdgeType edgeType,
        float newConfidence,
        string verifiedBy);

    /// <summary>Counts annotations linked to a memory unit via ANNOTATES edges.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildCountAnnotations(string memoryUnitId);

    /// <summary>Lists annotation memory unit IDs linked to a memory unit via ANNOTATES edges.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildListAnnotationIds(string memoryUnitId);

    /// <summary>Batch-counts annotations for multiple memory units in a single query.</summary>
    (string Query, IDictionary<string, object> Parameters) BuildBatchCountAnnotations(IReadOnlyList<string> memoryUnitIds);

    /// <summary>
    /// Story 8.3: lists edges incident to a batch of memory units in one roundtrip. Returns the
    /// full edge shape needed by the export envelope (see <c>ExportedEdge</c>): internal edge id,
    /// source, target, type label, confidence, origin, creation timestamp, and the promotion audit
    /// fields populated by Story 4.3. Edge id is returned as the FalkorDB internal numeric id
    /// (<c>id(r)</c>) — stable within a graph lifetime only.
    /// </summary>
    /// <param name="memoryUnitIds">Memory unit identifiers; incoming and outgoing edges are both returned.</param>
    (string Query, IDictionary<string, object> Parameters) BuildListEdgesForMemoryUnits(IReadOnlyList<string> memoryUnitIds);

    /// <summary>Counts all nodes in the tenant graph (used by batched tenant deletion).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildCountAllNodes();

    /// <summary>Deletes a batch of nodes with DETACH DELETE and returns the count deleted (used by batched tenant deletion).</summary>
    (string Query, IDictionary<string, object> Parameters) BuildBatchDeleteNodes(int batchSize);
}
