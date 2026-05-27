namespace Hexalith.Memories.Contracts.Tests.V1;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantDeletionInputValidationTests
{
    [Fact]
    public void Constructor_InvalidTenantId_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new TenantDeletionInput("tenant with spaces"));
    }

    [Fact]
    public void Constructor_ReservedTenantId_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new TenantDeletionInput("system"));
    }
}
