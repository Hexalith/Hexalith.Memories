namespace Hexalith.Memories.Contracts.Tests.V1;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class EdgeTypeTaxonomyTests
{
    [Theory]
    [InlineData(EdgeType.CausedBy, EdgeTypeCategory.Semantic)]
    [InlineData(EdgeType.CorrelatedWith, EdgeTypeCategory.Semantic)]
    [InlineData(EdgeType.References, EdgeTypeCategory.Semantic)]
    public void GetCategory_SemanticTypes_ReturnsSemantic(EdgeType edgeType, EdgeTypeCategory expected)
    {
        EdgeTypeTaxonomy.GetCategory(edgeType).ShouldBe(expected);
    }

    [Theory]
    [InlineData(EdgeType.Contains, EdgeTypeCategory.Structural)]
    [InlineData(EdgeType.Annotates, EdgeTypeCategory.Structural)]
    public void GetCategory_StructuralTypes_ReturnsStructural(EdgeType edgeType, EdgeTypeCategory expected)
    {
        EdgeTypeTaxonomy.GetCategory(edgeType).ShouldBe(expected);
    }

    [Fact]
    public void GetCategory_InvalidEnum_ThrowsArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => EdgeTypeTaxonomy.GetCategory((EdgeType)999));
    }

    [Fact]
    public void SemanticTypes_ContainsExactly_CausedBy_CorrelatedWith_References()
    {
        EdgeTypeTaxonomy.SemanticTypes.ShouldBe(
            new[] { EdgeType.CausedBy, EdgeType.CorrelatedWith, EdgeType.References });
    }

    [Fact]
    public void StructuralTypes_ContainsExactly_Contains_Annotates()
    {
        EdgeTypeTaxonomy.StructuralTypes.ShouldBe(
            new[] { EdgeType.Contains, EdgeType.Annotates });
    }

    [Fact]
    public void AllTypes_ContainsAllFiveTypes()
    {
        EdgeTypeTaxonomy.AllTypes.Count.ShouldBe(5);
        EdgeTypeTaxonomy.AllTypes.ShouldContain(EdgeType.CausedBy);
        EdgeTypeTaxonomy.AllTypes.ShouldContain(EdgeType.CorrelatedWith);
        EdgeTypeTaxonomy.AllTypes.ShouldContain(EdgeType.References);
        EdgeTypeTaxonomy.AllTypes.ShouldContain(EdgeType.Contains);
        EdgeTypeTaxonomy.AllTypes.ShouldContain(EdgeType.Annotates);
    }

    [Fact]
    public void SemanticTypes_DoNotOverlap_StructuralTypes()
    {
        EdgeTypeTaxonomy.SemanticTypes
            .Intersect(EdgeTypeTaxonomy.StructuralTypes)
            .ShouldBeEmpty();
    }

    [Fact]
    public void AllTypes_Equals_SemanticPlusStructural()
    {
        HashSet<EdgeType> combined = [.. EdgeTypeTaxonomy.SemanticTypes, .. EdgeTypeTaxonomy.StructuralTypes];
        combined.SetEquals(EdgeTypeTaxonomy.AllTypes).ShouldBeTrue();
    }
}
