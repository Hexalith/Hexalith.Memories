namespace Hexalith.Memories.TestHelpers.Factories;

using System.Text;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Factory for creating <see cref="ExtractionInput"/> instances with sensible defaults.
/// </summary>
public static class ExtractionInputFactory
{
    private static int _counter;

    public static ExtractionInput Create(
        string? sourceUri = null,
        byte[]? contentBytes = null,
        string? contentType = null,
        SourceType? sourceType = null,
        string? tenantId = null)
    {
        int id = Interlocked.Increment(ref _counter);

        return new ExtractionInput(
            sourceUri ?? $"file:///document-{id}.txt",
            contentBytes ?? Encoding.UTF8.GetBytes($"Sample document content {id}"),
            contentType ?? "text/plain",
            sourceType ?? SourceType.File,
            tenantId ?? $"tenant-{id}");
    }
}
