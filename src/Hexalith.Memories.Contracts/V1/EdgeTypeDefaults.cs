namespace Hexalith.Memories.Contracts.V1;

/// <summary>Default confidence values for each graph edge type.</summary>
public static class EdgeTypeDefaults
{
    public const float CausedBy = 1.0f;
    public const float CorrelatedWith = 0.8f;
    public const float References = 0.5f;
    public const float Contains = 1.0f;
    public const float Annotates = 1.0f;
}
