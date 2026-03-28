namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Indicates whether a graph edge was explicitly declared or inferred by the system.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EdgeOrigin>))]
public enum EdgeOrigin
{
    Explicit,
    Inferred,
}
