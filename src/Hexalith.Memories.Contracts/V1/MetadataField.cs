namespace Hexalith.Memories.Contracts.V1;

/// <summary>Represents extracted metadata with origin and confidence tracking.</summary>
public sealed record MetadataField(string Value, MetadataOrigin Origin, float Confidence);
