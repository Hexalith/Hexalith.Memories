namespace Hexalith.Memories.Server.Tests.Graph;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Shouldly;

public class GraphQueryBuilderTests
{
    private readonly GraphQueryBuilder _builder = new();

    [Fact]
    public void BuildMergeMemoryUnitNode_ShouldUseMergeNotCreate()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeMemoryUnitNode(
            "mu-001", "case-001", "test content", "hash123",
            "file:///test.txt", SourceType.File, "google:text-embedding-004",
            768, "user@example.com", DateTimeOffset.UtcNow, "{}");

        query.ShouldContain("MERGE");
        query.ShouldNotContain("CREATE");
        query.ShouldContain("SET");
    }

    [Fact]
    public void BuildMergeMemoryUnitNode_ShouldUseParameterPlaceholders()
    {
        DateTimeOffset ingestedAt = DateTimeOffset.Parse("2026-03-29T10:00:00+00:00");
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeMemoryUnitNode(
            "mu-001", "case-001", "test content", "hash123",
            "file:///test.txt", SourceType.File, "google:text-embedding-004",
            768, "user@example.com", ingestedAt, "{\"priority\":{\"value\":\"high\"}}");

        query.ShouldContain("$id");
        query.ShouldContain("$caseId");
        query.ShouldContain("$content");
        query.ShouldContain("$ingestedBy");
        query.ShouldContain("$ingestedAt");
        query.ShouldContain("$metadataJson");
        parameters["id"].ShouldBe("mu-001");
        parameters["caseId"].ShouldBe("case-001");
        parameters["content"].ShouldBe("test content");
        parameters["contentHash"].ShouldBe("hash123");
        parameters["sourceUri"].ShouldBe("file:///test.txt");
        parameters["sourceType"].ShouldBe("file");
        parameters["provider"].ShouldBe("google:text-embedding-004");
        parameters["dims"].ShouldBe(768);
        parameters["ingestedBy"].ShouldBe("user@example.com");
        parameters["ingestedAt"].ShouldBe(ingestedAt.ToString("o"));
        parameters["metadataJson"].ShouldBe("{\"priority\":{\"value\":\"high\"}}");
    }

    [Fact]
    public void BuildMergeCaseNode_ShouldUseMerge()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeCaseNode("case-001");

        query.ShouldContain("MERGE");
        query.ShouldNotContain("CREATE");
        query.ShouldContain("$caseId");
        parameters["caseId"].ShouldBe("case-001");
    }

    [Fact]
    public void BuildMergeCaseNode_WithMetadata_ShouldUseMergeAndSet()
    {
        DateTimeOffset createdAt = DateTimeOffset.Parse("2026-04-01T10:00:00+00:00");
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeCaseNode(
            "case-001", "Test Case", "tenant-1", createdAt);

        query.ShouldContain("MERGE");
        query.ShouldContain("SET");
        query.ShouldContain("$name");
        query.ShouldContain("$tenantId");
        query.ShouldContain("$createdAt");
        parameters["caseId"].ShouldBe("case-001");
        parameters["name"].ShouldBe("Test Case");
        parameters["tenantId"].ShouldBe("tenant-1");
        parameters["createdAt"].ShouldBe(createdAt.ToString("o"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildMergeCaseNode_WithMetadata_NullOrEmptyName_ShouldThrow(string? name)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildMergeCaseNode("case-001", name!, "tenant-1", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void BuildCountCaseMemoryUnits_ShouldReturnParameterizedCountQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildCountCaseMemoryUnits("case-001");

        query.ShouldContain("MATCH");
        query.ShouldContain("CONTAINS");
        query.ShouldContain("count(m) AS count");
        query.ShouldContain("$caseId");
        parameters["caseId"].ShouldBe("case-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildCountCaseMemoryUnits_NullOrEmptyCaseId_ShouldThrow(string? caseId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildCountCaseMemoryUnits(caseId!));
    }

    [Fact]
    public void InjectionPrevention_BuildMergeCaseNodeWithMetadata_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialName = "INJECT'; MATCH (n) DELETE n;--";

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeCaseNode(
            "case-001", adversarialName, "tenant-1", DateTimeOffset.UtcNow);

        query.ShouldNotContain(adversarialName);
        parameters["name"].ShouldBe(adversarialName);
    }

    [Fact]
    public void InjectionPrevention_BuildCountCaseMemoryUnits_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialCaseId = "INJECT_COUNT_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildCountCaseMemoryUnits(adversarialCaseId);

        query.ShouldNotContain(adversarialCaseId);
        parameters["caseId"].ShouldBe(adversarialCaseId);
    }

    [Theory]
    [InlineData(EdgeType.CausedBy, "CAUSED_BY")]
    [InlineData(EdgeType.CorrelatedWith, "CORRELATED_WITH")]
    [InlineData(EdgeType.Contains, "CONTAINS")]
    [InlineData(EdgeType.References, "REFERENCES")]
    [InlineData(EdgeType.Annotates, "ANNOTATES")]
    public void BuildMergeEdge_ShouldGenerateCorrectEdgeLabel(EdgeType edgeType, string expectedLabel)
    {
        (string query, IDictionary<string, object> _) = _builder.BuildMergeEdge(
            "source-001", "target-001", edgeType, 1.0f, EdgeOrigin.Explicit);

        query.ShouldContain($"[r:{expectedLabel}]");
    }

    [Fact]
    public void BuildMergeEdge_Contains_ShouldHaveCorrectConfidence()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            "case-001", "mu-001", EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);

        query.ShouldContain("MATCH (s:Case {id: $sourceId}), (t:MemoryUnit {id: $targetId})");
        parameters["confidence"].ShouldBe(1.0f);
        parameters["origin"].ShouldBe("explicit");
    }

    [Fact]
    public void BuildMergeEdge_CausedBy_ShouldHaveConfidence1()
    {
        (string _, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            "mu-cause", "mu-effect", EdgeType.CausedBy, EdgeTypeDefaults.CausedBy, EdgeOrigin.Explicit);

        parameters["confidence"].ShouldBe(1.0f);
    }

    [Fact]
    public void BuildMergeEdge_CorrelatedWith_ShouldHaveConfidence08()
    {
        (string _, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            "mu-a", "mu-b", EdgeType.CorrelatedWith, EdgeTypeDefaults.CorrelatedWith, EdgeOrigin.Explicit);

        parameters["confidence"].ShouldBe(0.8f);
    }

    [Fact]
    public void BuildMergeEdge_ShouldUseMerge()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildMergeEdge(
            "source", "target", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit);

        query.ShouldContain("MERGE");
        query.ShouldContain("SET r.confidence = $confidence, r.origin = $origin");
        query.ShouldNotContain("{confidence: $confidence");
    }

    [Fact]
    public void BuildMergeEdge_ShouldUseParameterPlaceholders()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            "source-001", "target-001", EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit);

        query.ShouldContain("$sourceId");
        query.ShouldContain("$targetId");
        query.ShouldContain("$confidence");
        query.ShouldContain("$origin");
        query.ShouldContain("MATCH (s:MemoryUnit {id: $sourceId}), (t:MemoryUnit {id: $targetId})");
        parameters["sourceId"].ShouldBe("source-001");
        parameters["targetId"].ShouldBe("target-001");
    }

    [Fact]
    public void BuildMergeStubNode_ShouldUseMergeWithOnlyId()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeStubNode("mu-stub-001");

        query.ShouldContain("MERGE");
        query.ShouldContain("MemoryUnit");
        query.ShouldContain("$id");
        query.ShouldNotContain("SET");
        parameters["id"].ShouldBe("mu-stub-001");
    }

    [Fact]
    public void BuildCountMemoryUnits_ShouldReturnCountQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildCountMemoryUnits();

        query.ShouldContain("MATCH (m:MemoryUnit)");
        query.ShouldContain("COUNT(m) AS count");
        parameters.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildMergeMemoryUnitNode_NullOrEmptyId_ShouldThrow(string? memoryUnitId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildMergeMemoryUnitNode(
            memoryUnitId!, "case", "content", "hash",
            "uri", SourceType.File, "provider", 768, "user@example.com", DateTimeOffset.UtcNow, "{}"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildMergeCaseNode_NullOrEmptyCaseId_ShouldThrow(string? caseId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildMergeCaseNode(caseId!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildMergeEdge_NullOrEmptySourceNodeId_ShouldThrow(string? sourceNodeId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildMergeEdge(
            sourceNodeId!, "target", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildMergeStubNode_NullOrEmptyId_ShouldThrow(string? memoryUnitId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildMergeStubNode(memoryUnitId!));
    }

    [Fact]
    public void BuildMergeEdge_UnknownEdgeType_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildMergeEdge(
            "source", "target", (EdgeType)999, 1.0f, EdgeOrigin.Explicit));
    }

    [Fact]
    public void InjectionPrevention_BuildMergeMemoryUnitNode_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialId = "INJECT_TEST_ID_12345";
        const string adversarialContent = "Robert'; DROP TABLE Students;--";

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeMemoryUnitNode(
            adversarialId, "case-inject", adversarialContent, "hash-inject",
            "file:///inject.txt", SourceType.File, "inject-provider",
            768, "user@example.com", DateTimeOffset.UtcNow, "{}");

        query.ShouldNotContain(adversarialId);
        query.ShouldNotContain(adversarialContent);
        query.ShouldNotContain("case-inject");
        query.ShouldNotContain("hash-inject");
        query.ShouldNotContain("inject-provider");

        parameters["id"].ShouldBe(adversarialId);
        parameters["content"].ShouldBe(adversarialContent);
    }

    [Fact]
    public void InjectionPrevention_BuildMergeCaseNode_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialCaseId = "INJECT'; MATCH (n) DELETE n;--";

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeCaseNode(adversarialCaseId);

        query.ShouldNotContain(adversarialCaseId);
        parameters["caseId"].ShouldBe(adversarialCaseId);
    }

    [Fact]
    public void InjectionPrevention_BuildMergeEdge_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialSource = "INJECT_SOURCE_12345";
        const string adversarialTarget = "INJECT_TARGET_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            adversarialSource, adversarialTarget, EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit);

        query.ShouldNotContain(adversarialSource);
        query.ShouldNotContain(adversarialTarget);
        parameters["sourceId"].ShouldBe(adversarialSource);
        parameters["targetId"].ShouldBe(adversarialTarget);
    }

    [Fact]
    public void BuildTraverseFromNode_ShouldReturnParameterizedQueryWithStartId()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseFromNode("mu-start-001", 3);

        query.ShouldContain("$startId");
        query.ShouldContain("[*0..3]");
        query.ShouldContain("DISTINCT");
        query.ShouldContain("nodeId");
        query.ShouldContain("hopDistance");
        parameters["startId"].ShouldBe("mu-start-001");
    }

    [Fact]
    public void BuildTraverseFromNode_DepthZero_ShouldReturnQueryWithZeroDepth()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseFromNode("mu-001", 0);

        query.ShouldContain("[*0..0]");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void BuildTraverseFromNode_NegativeDepth_ShouldThrowArgumentOutOfRangeException(int depth)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildTraverseFromNode("mu-001", depth));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(100)]
    public void BuildTraverseFromNode_DepthExceedsMax_ShouldThrowArgumentOutOfRangeException(int depth)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildTraverseFromNode("mu-001", depth));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildTraverseFromNode_NullOrEmptyStartNodeId_ShouldThrow(string? startNodeId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildTraverseFromNode(startNodeId!, 2));
    }

    [Fact]
    public void InjectionPrevention_BuildTraverseFromNode_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialId = "INJECT_TRAVERSE_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseFromNode(adversarialId, 5);

        query.ShouldNotContain(adversarialId);
        parameters["startId"].ShouldBe(adversarialId);
    }

    [Fact]
    public void InjectionPrevention_BuildMergeStubNode_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialId = "INJECT_STUB_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeStubNode(adversarialId);

        query.ShouldNotContain(adversarialId);
        parameters["id"].ShouldBe(adversarialId);
    }

    [Fact]
    public void BuildTraverseFromNode_WithCaseId_ShouldIncludeWhereClause()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseFromNode("mu-start-001", 3, "case-abc");

        query.ShouldContain("WHERE n.caseId = $caseId");
        query.ShouldContain("$startId");
        query.ShouldContain("[*0..3]");
        parameters["startId"].ShouldBe("mu-start-001");
        parameters["caseId"].ShouldBe("case-abc");
    }

    [Fact]
    public void BuildTraverseFromNode_WithNullCaseId_ShouldNotIncludeWhereClause()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseFromNode("mu-start-001", 3, null);

        query.ShouldNotContain("WHERE");
        query.ShouldNotContain("caseId");
        parameters.ShouldNotContainKey("caseId");
    }

    [Fact]
    public void BuildTraverseFromNode_WithEmptyCaseId_ShouldNotIncludeWhereClause()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseFromNode("mu-start-001", 3, "");

        query.ShouldNotContain("WHERE");
        parameters.ShouldNotContainKey("caseId");
    }

    [Fact]
    public void BuildTraverseFromNode_TwoParamOverload_ShouldDelegateToThreeParam()
    {
        (string queryTwo, IDictionary<string, object> paramsTwo) = _builder.BuildTraverseFromNode("mu-001", 2);
        (string queryThree, IDictionary<string, object> paramsThree) = _builder.BuildTraverseFromNode("mu-001", 2, null);

        queryTwo.ShouldBe(queryThree);
        paramsTwo["startId"].ShouldBe(paramsThree["startId"]);
    }

    [Fact]
    public void InjectionPrevention_BuildTraverseFromNode_WithCaseId_ShouldNeverContainRawCaseIdInQuery()
    {
        const string adversarialCaseId = "INJECT_CASE_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseFromNode("mu-001", 3, adversarialCaseId);

        query.ShouldNotContain(adversarialCaseId);
        parameters["caseId"].ShouldBe(adversarialCaseId);
    }

    [Fact]
    public void BuildListCaseMemoryUnitIds_ShouldReturnParameterizedQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildListCaseMemoryUnitIds("case-001");

        query.ShouldContain("MATCH");
        query.ShouldContain("CONTAINS");
        query.ShouldContain("m.id AS memoryUnitId");
        query.ShouldContain("$caseId");
        parameters["caseId"].ShouldBe("case-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildListCaseMemoryUnitIds_NullOrEmptyCaseId_ShouldThrow(string? caseId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildListCaseMemoryUnitIds(caseId!));
    }

    [Fact]
    public void InjectionPrevention_BuildListCaseMemoryUnitIds_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialCaseId = "INJECT_LIST_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildListCaseMemoryUnitIds(adversarialCaseId);

        query.ShouldNotContain(adversarialCaseId);
        parameters["caseId"].ShouldBe(adversarialCaseId);
    }

    [Fact]
    public void BuildDeleteCaseNode_ShouldReturnDetachDeleteQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildDeleteCaseNode("case-001");

        query.ShouldContain("MATCH");
        query.ShouldContain("Case");
        query.ShouldContain("DETACH DELETE");
        query.ShouldContain("$caseId");
        parameters["caseId"].ShouldBe("case-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildDeleteCaseNode_NullOrEmptyCaseId_ShouldThrow(string? caseId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildDeleteCaseNode(caseId!));
    }

    [Fact]
    public void InjectionPrevention_BuildDeleteCaseNode_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialCaseId = "INJECT_DELETE_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildDeleteCaseNode(adversarialCaseId);

        query.ShouldNotContain(adversarialCaseId);
        parameters["caseId"].ShouldBe(adversarialCaseId);
    }
}
