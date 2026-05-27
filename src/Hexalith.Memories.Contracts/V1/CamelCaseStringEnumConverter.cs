namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Serializes enum values as camelCase strings and rejects integer tokens.</summary>
/// <typeparam name="TEnum">The enum type to convert.</typeparam>
public sealed class CamelCaseStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Initializes a new instance of the <see cref="CamelCaseStringEnumConverter{TEnum}"/> class.</summary>
    public CamelCaseStringEnumConverter()
        : base(JsonNamingPolicy.CamelCase, false)
    {
    }
}
