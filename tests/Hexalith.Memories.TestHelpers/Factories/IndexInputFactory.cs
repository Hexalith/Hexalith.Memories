namespace Hexalith.Memories.TestHelpers.Factories;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Factory for creating <see cref="IndexInput"/> instances with sensible defaults.
/// Override specific properties per test to make intent explicit.
/// </summary>
public static class IndexInputFactory
{
    private static int _counter;

    public static IndexInput Create(
        string? memoryUnitId = null,
        string? tenantId = null,
        string? caseId = null,
        string? content = null,
        string? contentHash = null,
        string? sourceUri = null,
        SourceType? sourceType = null,
        float[]? embeddingVector = null,
        string? embeddingProvider = null,
        int? embeddingDimensions = null,
        string? causationId = null,
        string? correlationId = null)
    {
        int id = Interlocked.Increment(ref _counter);

        return new IndexInput
        {
            MemoryUnitId = memoryUnitId ?? $"mu-{id:D6}",
            TenantId = tenantId ?? "test-tenant",
            CaseId = caseId ?? $"case-{id:D6}",
            Content = content ?? $"Test content for memory unit {id}",
            ContentHash = contentHash ?? $"hash-{id:D6}",
            SourceUri = sourceUri ?? $"file:///test-{id}.txt",
            SourceType = sourceType ?? SourceType.File,
            EmbeddingVector = embeddingVector ?? CreateDefaultVector(),
            EmbeddingProvider = embeddingProvider ?? "google:text-embedding-004",
            EmbeddingDimensions = embeddingDimensions ?? 3,
            CausationId = causationId,
            CorrelationId = correlationId,
        };
    }

    /// <summary>Creates a minimal 3-dimensional vector for tests that don't care about vector content.</summary>
    public static float[] CreateDefaultVector() => [0.1f, 0.2f, 0.3f];

    /// <summary>Creates a realistic 768-dimensional vector for dimension-sensitive tests.</summary>
    public static float[] CreateRealisticVector(int dimensions = 768)
    {
        float[] vector = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = (float)(Math.Sin(i) * 0.5);
        }

        return vector;
    }
}
