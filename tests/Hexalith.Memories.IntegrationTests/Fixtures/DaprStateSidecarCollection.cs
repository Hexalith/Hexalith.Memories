namespace Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>Defines the existing shared DaprStateSidecarFixture collection.</summary>
[CollectionDefinition("DaprStateSidecar", DisableParallelization = true)]
public sealed class DaprStateSidecarCollection : ICollectionFixture<DaprStateSidecarFixture>;
