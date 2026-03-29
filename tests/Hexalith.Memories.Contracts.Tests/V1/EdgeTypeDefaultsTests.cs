namespace Hexalith.Memories.Contracts.Tests.V1;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Validates that EdgeTypeDefaults confidence constants match the architecture specification.
/// These values are consumed by GraphQueryBuilder to set edge confidence scores.
/// A silent change here would corrupt graph edge weights across all tenants.
/// </summary>
public class EdgeTypeDefaultsTests
{
    [Theory]
    [InlineData(nameof(EdgeTypeDefaults.CausedBy), 1.0f)]
    [InlineData(nameof(EdgeTypeDefaults.Contains), 1.0f)]
    [InlineData(nameof(EdgeTypeDefaults.Annotates), 1.0f)]
    [InlineData(nameof(EdgeTypeDefaults.CorrelatedWith), 0.8f)]
    [InlineData(nameof(EdgeTypeDefaults.References), 0.5f)]
    public void DefaultConfidence_ShouldMatchSpecifiedValue(string fieldName, float expected)
    {
        float actual = fieldName switch
        {
            nameof(EdgeTypeDefaults.CausedBy) => EdgeTypeDefaults.CausedBy,
            nameof(EdgeTypeDefaults.Contains) => EdgeTypeDefaults.Contains,
            nameof(EdgeTypeDefaults.Annotates) => EdgeTypeDefaults.Annotates,
            nameof(EdgeTypeDefaults.CorrelatedWith) => EdgeTypeDefaults.CorrelatedWith,
            nameof(EdgeTypeDefaults.References) => EdgeTypeDefaults.References,
            _ => throw new ArgumentException($"Unknown field: {fieldName}"),
        };

        actual.ShouldBe(expected, $"EdgeTypeDefaults.{fieldName}");
    }

    [Fact]
    public void AllEdgeTypes_ShouldHaveCorrespondingDefault()
    {
        // Every EdgeType enum value should have a matching constant in EdgeTypeDefaults.
        // If a new EdgeType is added without a default, this test forces the decision.
        EdgeType[] allTypes = Enum.GetValues<EdgeType>();

        allTypes.Length.ShouldBe(5, "If you added a new EdgeType, add a matching EdgeTypeDefaults constant and update this test.");
    }

    [Fact]
    public void ConfidenceValues_ShouldBeInZeroToOneRange()
    {
        float[] allDefaults =
        [
            EdgeTypeDefaults.CausedBy,
            EdgeTypeDefaults.CorrelatedWith,
            EdgeTypeDefaults.References,
            EdgeTypeDefaults.Contains,
            EdgeTypeDefaults.Annotates,
        ];

        foreach (float value in allDefaults)
        {
            value.ShouldBeGreaterThanOrEqualTo(0.0f);
            value.ShouldBeLessThanOrEqualTo(1.0f);
        }
    }
}
