namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json;

/// <summary>Shared JSON serialization options for all Memories contracts.</summary>
public static class MemoriesJsonContext
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
