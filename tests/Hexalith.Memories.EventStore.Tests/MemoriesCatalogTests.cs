using Shouldly;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Memories.EventStore.Routing;

namespace Hexalith.Memories.EventStore.Tests;

public class MemoriesCatalogTests
{
    [Fact]
    public void ExistingSixCommandsRemainInternalWithTheirActualClrIdentities()
    {
        MemoriesCatalogDeclarations.Commands.Count.ShouldBe(6);
        MemoriesCatalogDeclarations.Operations.Count.ShouldBe(6);
        foreach (var operation in MemoriesCatalogDeclarations.Operations)
        {
            operation.Ingress.ShouldBe("internal");
            operation.Digest.ShouldBe(RouteCatalogCodec.ComputeOperationContractDigest(operation));
        }
        MemoriesCatalogDeclarations.Routes.All(r => r.Kind == "command").ShouldBeTrue();
    }
}
