namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Captured refresh input and the candidate whose derived-store writes have completed.</summary>
internal sealed record RefreshPublicationInput(IngestionInput Input, string MemoryUnitId);
