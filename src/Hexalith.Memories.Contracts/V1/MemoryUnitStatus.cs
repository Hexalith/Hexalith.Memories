namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Tracks the current stage of a memory unit in the ingestion pipeline.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<MemoryUnitStatus>))]
public enum MemoryUnitStatus
{
    Queued,
    Extracting,
    Embedding,
    Indexing,
    Indexed,
    Failed,
}
