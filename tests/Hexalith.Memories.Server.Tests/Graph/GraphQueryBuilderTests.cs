namespace Hexalith.Memories.Server.Tests.Graph;

#pragma warning disable CS0618 // these tests intentionally exercise the 1-arg BuildMergeStubNode obsolete overload to pin its forward-to-2-arg behavior.

using System.IO;
using System.Text.RegularExpressions;

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
            "gemini-embedding-001",
            768, "user@example.com", DateTimeOffset.UtcNow, "{}");

        query.ShouldContain("MERGE");
        // Assert no Cypher CREATE statement — Story 9.2 adds a 'stubCreatedAt' property whose name
        // contains the 'Create' substring, so we match the keyword only at a word boundary.
        System.Text.RegularExpressions.Regex.IsMatch(query, @"\bCREATE\b").ShouldBeFalse();
        query.ShouldContain("SET");
    }

    [Fact]
    public void BuildMergeMemoryUnitNode_ShouldUseParameterPlaceholders()
    {
        DateTimeOffset ingestedAt = DateTimeOffset.Parse("2026-03-29T10:00:00+00:00");
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeMemoryUnitNode(
            "mu-001", "case-001", "test content", "hash123",
            "file:///test.txt", SourceType.File, "google:text-embedding-004",
            "gemini-embedding-001",
            768, "user@example.com", ingestedAt, "{\"priority\":{\"value\":\"high\"}}");

        query.ShouldContain("$id");
        query.ShouldContain("$caseId");
        query.ShouldContain("$content");
        query.ShouldContain("$ingestedBy");
        query.ShouldContain("$ingestedAt");
        query.ShouldContain("$metadataJson");
        query.ShouldContain("$model");
        parameters["id"].ShouldBe("mu-001");
        parameters["caseId"].ShouldBe("case-001");
        parameters["content"].ShouldBe("test content");
        parameters["contentHash"].ShouldBe("hash123");
        parameters["sourceUri"].ShouldBe("file:///test.txt");
        parameters["sourceType"].ShouldBe("file");
        parameters["provider"].ShouldBe("google:text-embedding-004");
        parameters["model"].ShouldBe("gemini-embedding-001");
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
        query.ShouldContain("SET r.createdAt = coalesce(r.createdAt, $createdAt), r.confidence = $confidence, r.origin = $origin");
        query.ShouldNotContain("{confidence: $confidence");
    }

    [Fact]
    public void BuildMergeEdge_ShouldUseParameterPlaceholders()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            "source-001", "target-001", EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit);

        query.ShouldContain("$sourceId");
        query.ShouldContain("$targetId");
        query.ShouldContain("$createdAt");
        query.ShouldContain("$confidence");
        query.ShouldContain("$origin");
        query.ShouldContain("MATCH (s:MemoryUnit {id: $sourceId}), (t:MemoryUnit {id: $targetId})");
        parameters["sourceId"].ShouldBe("source-001");
        parameters["targetId"].ShouldBe("target-001");
    }

    [Fact]
    public void BuildMergeStubNode_ShouldUseMergeWithIsStubAndStubCreatedAt()
    {
        // Story 9.2 Task 7.1: stub node MERGE must set isStub=true and stubCreatedAt via ON CREATE
        // SET so existing real nodes are not regressed (Risk #12 — coalesce unreliability).
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeStubNode("mu-stub-001");

        query.ShouldContain("MERGE");
        query.ShouldContain("MemoryUnit");
        query.ShouldContain("$id");
        query.ShouldContain("ON CREATE SET");
        query.ShouldContain("isStub = true");
        query.ShouldContain("stubCreatedAt = $stubCreatedAt");
        parameters["id"].ShouldBe("mu-stub-001");
        parameters.ContainsKey("stubCreatedAt").ShouldBeTrue();
    }

    [Fact]
    public void BuildMergeStubNode_ExplicitTimestamp_PersistsTimestampParameter()
    {
        DateTimeOffset stubCreatedAt = new(2026, 4, 23, 10, 0, 0, TimeSpan.Zero);
        (string _, IDictionary<string, object> parameters) = _builder.BuildMergeStubNode("mu-stub-002", stubCreatedAt);

        parameters["stubCreatedAt"].ShouldBe("2026-04-23T10:00:00.0000000+00:00");
    }

    [Fact]
    public void BuildMergeStubNode_AllCallers_PassStubCreatedAt()
    {
        string repoRoot = FindRepoRoot();
        string serverSourceRoot = Path.Combine(repoRoot, "src", "Hexalith.Memories.Server");
        List<string> sourceFiles = Directory
            .EnumerateFiles(serverSourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !path.EndsWith("GraphQueryBuilder.cs", StringComparison.Ordinal)
                && !path.EndsWith("IGraphQueryBuilder.cs", StringComparison.Ordinal))
            .ToList();

        List<string> oneArgumentCallSites = [];
        int totalInvocationCount = 0;

        foreach (string path in sourceFiles)
        {
            string source = File.ReadAllText(path);
            totalInvocationCount += Regex.Matches(source, @"\.BuildMergeStubNode\s*\(").Count;

            MatchCollection invalidCalls = Regex.Matches(
                source,
                @"\.BuildMergeStubNode\s*\(\s*[^,\)\r\n]+\s*\)");

            if (invalidCalls.Count == 0)
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(serverSourceRoot, path);
            oneArgumentCallSites.AddRange(invalidCalls.Select(match => $"{relativePath}: {match.Value}"));
        }

        totalInvocationCount.ShouldBe(
            3,
            "Story 9.2 currently has exactly three production BuildMergeStubNode call sites (CaseService plus two IndexGraphActivity branches). Update this assertion if a new legitimate caller is introduced.");
        oneArgumentCallSites.ShouldBeEmpty(
            "Production callers must use the 2-arg overload and pass a replay-safe stubCreatedAt timestamp.");
    }

    [Fact]
    public void BuildMergeMemoryUnitNode_ReturnsPreviousIsStubAndStubCreatedAt()
    {
        (string query, _) = _builder.BuildMergeMemoryUnitNode(
            "mu-001", "case", "content", "hash", "uri", SourceType.File,
            "provider", "model", 3, "u@example.com", DateTimeOffset.UtcNow, "{}");

        query.ShouldContain("coalesce(m.isStub, false) AS previousIsStub");
        query.ShouldContain("m.stubCreatedAt AS stubCreatedAt");
        query.ShouldContain("m.isStub = false");
        query.ShouldContain("RETURN previousIsStub, stubCreatedAt");
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
            "uri", SourceType.File, "provider", "model", 768, "user@example.com", DateTimeOffset.UtcNow, "{}"));
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
            "inject-model",
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
    public void BuildListEdgesForMemoryUnits_ShouldReturnParameterizedQuery()
    {
        List<string> ids = ["mu-1", "mu-2", "mu-3"];
        (string query, IDictionary<string, object> parameters) = _builder.BuildListEdgesForMemoryUnits(ids);

        query.ShouldContain("UNWIND $ids AS muId");
        query.ShouldContain("MATCH (m:MemoryUnit {id: muId})-[r]-(n:MemoryUnit)");
        query.ShouldContain("id(r) AS edgeId");
        query.ShouldContain("startNode(r).id AS sourceId");
        query.ShouldContain("endNode(r).id AS targetId");
        query.ShouldContain("type(r) AS edgeType");
        query.ShouldContain("r.confidence");
        query.ShouldContain("r.verifiedBy");
        query.ShouldContain("r.previousConfidence");
        parameters["ids"].ShouldBe(ids);
    }

    [Fact]
    public void BuildListEdgesForMemoryUnits_NullIds_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _builder.BuildListEdgesForMemoryUnits(null!));
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

    // --- BuildCountAnnotations tests ---

    [Fact]
    public void BuildCountAnnotations_ShouldReturnParameterizedQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildCountAnnotations("mu-001");

        query.ShouldContain("MATCH");
        query.ShouldContain("ANNOTATES");
        query.ShouldContain("count(a) AS count");
        query.ShouldContain("$memoryUnitId");
        parameters["memoryUnitId"].ShouldBe("mu-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildCountAnnotations_NullOrEmptyId_ShouldThrow(string? memoryUnitId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildCountAnnotations(memoryUnitId!));
    }

    [Fact]
    public void InjectionPrevention_BuildCountAnnotations_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialId = "INJECT_COUNT_ANNOTATIONS_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildCountAnnotations(adversarialId);

        query.ShouldNotContain(adversarialId);
        parameters["memoryUnitId"].ShouldBe(adversarialId);
    }

    // --- BuildListAnnotationIds tests ---

    [Fact]
    public void BuildListAnnotationIds_ShouldReturnParameterizedQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildListAnnotationIds("mu-001");

        query.ShouldContain("MATCH");
        query.ShouldContain("ANNOTATES");
        query.ShouldContain("a.id AS annotationId");
        query.ShouldContain("$memoryUnitId");
        parameters["memoryUnitId"].ShouldBe("mu-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildListAnnotationIds_NullOrEmptyId_ShouldThrow(string? memoryUnitId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildListAnnotationIds(memoryUnitId!));
    }

    [Fact]
    public void InjectionPrevention_BuildListAnnotationIds_ShouldNeverContainRawInputInQuery()
    {
        const string adversarialId = "INJECT_LIST_ANNOTATIONS_12345";

        (string query, IDictionary<string, object> parameters) = _builder.BuildListAnnotationIds(adversarialId);

        query.ShouldNotContain(adversarialId);
        parameters["memoryUnitId"].ShouldBe(adversarialId);
    }

    // --- BuildBatchCountAnnotations tests ---

    [Fact]
    public void BuildBatchCountAnnotations_EmptyList_ShouldReturnQueryWithEmptyParameter()
    {
        List<string> ids = [];
        (string query, IDictionary<string, object> parameters) = _builder.BuildBatchCountAnnotations(ids);

        query.ShouldContain("UNWIND $ids");
        query.ShouldContain("OPTIONAL MATCH");
        query.ShouldContain("ANNOTATES");
        query.ShouldContain("count(a) AS count");
        parameters["ids"].ShouldBe(ids);
    }

    [Fact]
    public void BuildBatchCountAnnotations_SingleId_ShouldReturnParameterizedQuery()
    {
        List<string> ids = ["mu-001"];
        (string query, IDictionary<string, object> parameters) = _builder.BuildBatchCountAnnotations(ids);

        query.ShouldContain("UNWIND $ids");
        query.ShouldContain("muId");
        var paramIds = parameters["ids"] as IReadOnlyList<string>;
        paramIds.ShouldNotBeNull();
        paramIds.Count.ShouldBe(1);
        paramIds[0].ShouldBe("mu-001");
    }

    [Fact]
    public void BuildBatchCountAnnotations_MultipleIds_ShouldReturnParameterizedQuery()
    {
        List<string> ids = ["mu-001", "mu-002", "mu-003"];
        (string query, IDictionary<string, object> parameters) = _builder.BuildBatchCountAnnotations(ids);

        query.ShouldContain("UNWIND $ids");
        var paramIds = parameters["ids"] as IReadOnlyList<string>;
        paramIds.ShouldNotBeNull();
        paramIds.Count.ShouldBe(3);
    }

    [Fact]
    public void BuildBatchCountAnnotations_NullList_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _builder.BuildBatchCountAnnotations(null!));
    }

    // --- BuildTraverseWithEdges tests ---

    [Fact]
    public void BuildTraverseWithEdges_ReturnsParameterizedQueryWithEdgeMetadata()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseWithEdges("mu-start-001", 3);

        query.ShouldContain("$startId");
        query.ShouldContain("edges");
        query.ShouldContain("hopDistance");
        query.ShouldContain("ingestedAt");
        query.ShouldContain("ORDER BY");
        // Default overload uses semantic-only edge types (AC #4 from Story 4.2)
        query.ShouldContain("CAUSED_BY|CORRELATED_WITH|REFERENCES*0..3]");
        query.ShouldContain("content");
        query.ShouldContain("sourceUri");
        query.ShouldContain("sourceType");
        parameters["startId"].ShouldBe("mu-start-001");
    }

    [Fact]
    public void BuildTraverseWithEdges_Depth0_ReturnsValidQuery()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges("mu-001", 0);

        // Default overload uses semantic-only edge types
        query.ShouldContain("CAUSED_BY|CORRELATED_WITH|REFERENCES*0..0]");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void BuildTraverseWithEdges_InvalidDepth_ThrowsArgumentOutOfRange(int depth)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildTraverseWithEdges("mu-001", depth));
    }

    [Fact]
    public void BuildTraverseWithEdges_InjectionPrevention()
    {
        const string adversarialId = "INJECT'; MATCH (n) DELETE n;--";

        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseWithEdges(adversarialId, 3);

        query.ShouldNotContain(adversarialId);
        parameters["startId"].ShouldBe(adversarialId);
    }

    [Fact]
    public void BuildTraverseWithEdges_WithCaseId_AddsWhereClause()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseWithEdges("mu-001", 3, "case-abc");

        query.ShouldContain("WHERE (n.caseId = $caseId OR n.content IS NULL)");
        query.ShouldContain("start.caseId = $caseId");
        query.ShouldContain("(node:MemoryUnit AND (node.caseId = $caseId OR node.content IS NULL)) OR (node:Case AND node.id = $caseId)");
        query.ShouldContain("(m:MemoryUnit AND (m.caseId = $caseId OR m.content IS NULL)) OR (m:Case AND m.id = $caseId)");
        parameters["startId"].ShouldBe("mu-001");
        parameters["caseId"].ShouldBe("case-abc");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithoutCaseId_NoWhereClause()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseWithEdges("mu-001", 3, null);

        query.ShouldNotContain("WHERE n.caseId");
        query.ShouldNotContain("start.caseId = $caseId");
        query.ShouldNotContain("m.caseId = $caseId");
        parameters.ShouldNotContainKey("caseId");
    }

    [Fact]
    public void BuildTraverseWithEdges_TwoParamOverload_DelegatesToThreeParam()
    {
        (string queryTwo, IDictionary<string, object> paramsTwo) = _builder.BuildTraverseWithEdges("mu-001", 2);
        (string queryThree, IDictionary<string, object> paramsThree) = _builder.BuildTraverseWithEdges("mu-001", 2, null);

        queryTwo.ShouldBe(queryThree);
        paramsTwo["startId"].ShouldBe(paramsThree["startId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildTraverseWithEdges_NullOrEmptyStartNodeId_ShouldThrow(string? startNodeId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildTraverseWithEdges(startNodeId!, 2));
    }

    // --- BuildTraverseWithEdges edge type filtering tests (Story 4.2) ---

    [Fact]
    public void BuildTraverseWithEdges_WithSingleEdgeType_FiltersPathByType()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null, [EdgeType.CausedBy]);

        query.ShouldContain("[:CAUSED_BY*0..3]");
        query.ShouldContain("[r:CAUSED_BY]-(m)");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithMultipleEdgeTypes_PipeSeparated()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null, [EdgeType.CausedBy, EdgeType.CorrelatedWith]);

        query.ShouldContain("CAUSED_BY|CORRELATED_WITH");
        query.ShouldContain("[r:CAUSED_BY|CORRELATED_WITH]");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithNullEdgeTypes_DefaultsToSemanticTypes()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null, null);

        query.ShouldContain("CAUSED_BY|CORRELATED_WITH|REFERENCES");
        query.ShouldNotContain("CONTAINS");
        query.ShouldNotContain("ANNOTATES");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithEmptyEdgeTypes_DefaultsToSemanticTypes()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null, []);

        query.ShouldContain("CAUSED_BY|CORRELATED_WITH|REFERENCES");
        query.ShouldNotContain("CONTAINS");
        query.ShouldNotContain("ANNOTATES");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithStructuralEdgeType_Allowed()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null, [EdgeType.Contains]);

        query.ShouldContain("[:CONTAINS*0..3]");
        query.ShouldContain("[r:CONTAINS]-(m)");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithContainsAndCaseId_AllowsCaseBoundaryNodes()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, "case-abc", [EdgeType.Contains]);

        query.ShouldContain("[:CONTAINS*0..3]");
        query.ShouldContain("(node:MemoryUnit AND (node.caseId = $caseId OR node.content IS NULL)) OR (node:Case AND node.id = $caseId)");
        query.ShouldContain("(m:MemoryUnit AND (m.caseId = $caseId OR m.content IS NULL)) OR (m:Case AND m.id = $caseId)");
        parameters["caseId"].ShouldBe("case-abc");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithAllFiveTypes_AllIncluded()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null,
            [EdgeType.CausedBy, EdgeType.CorrelatedWith, EdgeType.References, EdgeType.Contains, EdgeType.Annotates]);

        query.ShouldContain("CAUSED_BY");
        query.ShouldContain("CORRELATED_WITH");
        query.ShouldContain("REFERENCES");
        query.ShouldContain("CONTAINS");
        query.ShouldContain("ANNOTATES");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithEdgeTypes_StillParameterizesStartId()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null, [EdgeType.CausedBy]);

        query.ShouldContain("$startId");
        parameters.ShouldContainKey("startId");
        parameters["startId"].ShouldBe("mu-001");
        // Edge type labels are NOT in parameters — interpolated as validated literals
        parameters.ShouldNotContainKey("edgeTypes");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithEdgeTypesAndCaseId_BothApplied()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, "case-abc", [EdgeType.CausedBy, EdgeType.CorrelatedWith]);

        query.ShouldContain("CAUSED_BY|CORRELATED_WITH");
        query.ShouldContain("WHERE (n.caseId = $caseId OR n.content IS NULL)");
        parameters["caseId"].ShouldBe("case-abc");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithCaseId_AllowsStubNodesInPathAndEdges()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, "case-abc", [EdgeType.CausedBy]);

        query.ShouldContain("(n.caseId = $caseId OR n.content IS NULL)");
        query.ShouldContain("(node:MemoryUnit AND (node.caseId = $caseId OR node.content IS NULL))");
        query.ShouldContain("(m:MemoryUnit AND (m.caseId = $caseId OR m.content IS NULL))");
    }

    [Fact]
    public void BuildTraverseWithEdges_WithDuplicateEdgeTypes_Deduplicated()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges(
            "mu-001", 3, null, [EdgeType.CausedBy, EdgeType.CausedBy]);

        // Should contain single CAUSED_BY, not CAUSED_BY|CAUSED_BY
        query.ShouldContain("[:CAUSED_BY*0..3]");
        query.ShouldNotContain("CAUSED_BY|CAUSED_BY");
    }

    [Fact]
    public void BuildTraverseWithEdges_ThreeParamOverload_DelegatesToFourParamWithNullEdgeTypes()
    {
        (string queryThree, IDictionary<string, object> paramsThree) = _builder.BuildTraverseWithEdges("mu-001", 2, null);
        (string queryFour, IDictionary<string, object> paramsFour) = _builder.BuildTraverseWithEdges("mu-001", 2, null, null);

        queryThree.ShouldBe(queryFour);
        paramsThree["startId"].ShouldBe(paramsFour["startId"]);
    }

    // --- BuildUpdateEdgeConfidence tests (Story 4.3) ---

    [Fact]
    public void BuildUpdateEdgeConfidence_GeneratesCorrectCypher()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildUpdateEdgeConfidence(
            "mu-source", "mu-target", EdgeType.CausedBy, 1.0f, "user@test.com");

        query.ShouldContain("MATCH");
        query.ShouldContain("SET r.previousConfidence = r.confidence");
        query.ShouldContain("r.confidence = $newConfidence");
        query.ShouldContain("r.verifiedBy = $verifiedBy");
        query.ShouldContain("RETURN r.confidence AS newConfidence");
        query.ShouldContain("r.previousConfidence AS previousConfidence");
    }

    [Theory]
    [InlineData(EdgeType.CausedBy, "CAUSED_BY")]
    [InlineData(EdgeType.CorrelatedWith, "CORRELATED_WITH")]
    [InlineData(EdgeType.References, "REFERENCES")]
    [InlineData(EdgeType.Contains, "CONTAINS")]
    [InlineData(EdgeType.Annotates, "ANNOTATES")]
    public void BuildUpdateEdgeConfidence_UsesCorrectEdgeLabel(EdgeType edgeType, string expectedLabel)
    {
        (string query, IDictionary<string, object> _) = _builder.BuildUpdateEdgeConfidence(
            "mu-source", "mu-target", edgeType, 1.0f, "user@test.com");

        query.ShouldContain($"[r:{expectedLabel}]");
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_ContainsUsesCorrectNodeLabels()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildUpdateEdgeConfidence(
            "case-001", "mu-target", EdgeType.Contains, 1.0f, "user");

        query.ShouldContain("(s:Case");
        query.ShouldContain("(t:MemoryUnit");
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_CausedByUsesMemoryUnitLabels()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildUpdateEdgeConfidence(
            "mu-source", "mu-target", EdgeType.CausedBy, 1.0f, "user");

        query.ShouldContain("(s:MemoryUnit");
        query.ShouldContain("(t:MemoryUnit");
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_ParameterizesValues()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildUpdateEdgeConfidence(
            "mu-source", "mu-target", EdgeType.CausedBy, 0.8f, "user@test.com");

        parameters["sourceId"].ShouldBe("mu-source");
        parameters["targetId"].ShouldBe("mu-target");
        parameters["newConfidence"].ShouldBe(0.8f);
        parameters["verifiedBy"].ShouldBe("user@test.com");
        // Edge label NOT in parameters — interpolated from closed enum
        parameters.ShouldNotContainKey("edgeLabel");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildUpdateEdgeConfidence_NullSourceNodeId_Throws(string? sourceNodeId)
    {
        Should.Throw<ArgumentException>(() => _builder.BuildUpdateEdgeConfidence(
            sourceNodeId!, "target", EdgeType.CausedBy, 1.0f, "user"));
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_NegativeConfidence_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildUpdateEdgeConfidence(
            "source", "target", EdgeType.CausedBy, -0.1f, "user"));
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_NaNConfidence_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildUpdateEdgeConfidence(
            "source", "target", EdgeType.CausedBy, float.NaN, "user"));
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_ConfidenceAboveOne_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildUpdateEdgeConfidence(
            "source", "target", EdgeType.CausedBy, 1.1f, "user"));
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_BoundaryValuesAccepted()
    {
        // 0.0 and 1.0 are valid boundary values
        Should.NotThrow(() => _builder.BuildUpdateEdgeConfidence(
            "source", "target", EdgeType.CausedBy, 0.0f, "user"));
        Should.NotThrow(() => _builder.BuildUpdateEdgeConfidence(
            "source", "target", EdgeType.CausedBy, 1.0f, "user"));
    }

    [Fact]
    public void BuildUpdateEdgeConfidence_InjectionPrevention()
    {
        const string adversarialSource = "INJECT'; MATCH (n) DELETE n;--";

        (string query, IDictionary<string, object> parameters) = _builder.BuildUpdateEdgeConfidence(
            adversarialSource, "target", EdgeType.CausedBy, 1.0f, "user");

        query.ShouldNotContain(adversarialSource);
        parameters["sourceId"].ShouldBe(adversarialSource);
    }

    // --- BuildTraverseWithEdges: verifiedBy/previousConfidence in edge map (Story 4.3) ---

    [Fact]
    public void BuildTraverseWithEdges_IncludesVerifiedByInEdgeMap()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges("mu-001", 3);

        query.ShouldContain("verifiedBy: r.verifiedBy");
    }

    [Fact]
    public void BuildTraverseWithEdges_IncludesPreviousConfidenceInEdgeMap()
    {
        (string query, IDictionary<string, object> _) = _builder.BuildTraverseWithEdges("mu-001", 3);

        query.ShouldContain("previousConfidence: r.previousConfidence");
    }

    [Fact]
    public void BuildCountAllNodes_ShouldReturnMatchCountQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildCountAllNodes();

        query.ShouldContain("MATCH (n)");
        query.ShouldContain("count(n)");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void BuildBatchDeleteNodes_ShouldReturnParameterizedQuery()
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildBatchDeleteNodes(500);

        query.ShouldContain("MATCH (n)");
        query.ShouldContain("LIMIT $batchSize");
        query.ShouldContain("DETACH DELETE n");
        parameters["batchSize"].ShouldBe(500);
    }

    [Fact]
    public void BuildBatchDeleteNodes_ZeroBatchSize_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildBatchDeleteNodes(0));
    }

    [Fact]
    public void BuildBatchDeleteNodes_NegativeBatchSize_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildBatchDeleteNodes(-1));
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Memories.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for GraphQueryBuilderTests.");
    }
}
