namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Defines the relationship type between two memory units in the knowledge graph.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EdgeType>))]
public enum EdgeType
{
    CausedBy,
    CorrelatedWith,
    References,
    Contains,
    Annotates,
}
