using Hexalith.Memories.Contracts;

using Shouldly;

namespace Hexalith.Memories.Contracts.Tests;

public class MemoriesInfoTests
{
    [Fact]
    public void Name_ShouldBeCorrect()
    {
        MemoriesInfo.Name.ShouldBe("Hexalith.Memories");
    }
}
