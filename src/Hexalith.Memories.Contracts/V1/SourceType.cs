namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Identifies the origin type of ingested content.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<SourceType>))]
public enum SourceType
{
    File,
    Url,
    Event,
    Command,
    Projection,
    Discussion,
}
