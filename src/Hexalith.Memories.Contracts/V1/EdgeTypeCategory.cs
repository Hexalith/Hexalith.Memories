namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Classifies edge types as structural (organizational) or semantic (meaning/causal).</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EdgeTypeCategory>))]
public enum EdgeTypeCategory
{
    Structural,
    Semantic,
}
