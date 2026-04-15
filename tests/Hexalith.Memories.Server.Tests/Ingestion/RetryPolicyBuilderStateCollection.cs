namespace Hexalith.Memories.Server.Tests.Ingestion;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RetryPolicyBuilderStateCollection
{
    public const string Name = "RetryPolicyBuilderState";
}