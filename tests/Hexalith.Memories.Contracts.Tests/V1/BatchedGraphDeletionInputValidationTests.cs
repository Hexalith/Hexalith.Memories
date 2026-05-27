namespace Hexalith.Memories.Contracts.Tests.V1;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class BatchedGraphDeletionInputValidationTests
{
    [Fact]
    public void Constructor_InvalidTenantId_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new BatchedGraphDeletionInput("tenant with spaces"));
    }

    [Fact]
    public void Constructor_ZeroBatchSize_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new BatchedGraphDeletionInput("tenant-1", 0));
    }

    [Fact]
    public void Constructor_NegativeBatchNumber_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new BatchedGraphDeletionInput("tenant-1", 500, -1));
    }
}
