using Shouldly;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Memories.EventStore.Routing;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStoreExtensions;

namespace Hexalith.Memories.EventStore.Tests;

public class MemoriesCatalogTests
{
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
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
#endif

    [Fact]
    public void DeclarationInventoryKeepsAllSixCommandTypesAndUniqueProcessRoutes()
    {
        Type[] expectedTypes = [typeof(RegisterTenantCommand), typeof(UpdateTenantLifecycleStatusCommand),
            typeof(CreateCaseCommand), typeof(DeleteCaseCommand), typeof(RequestAnnotationCommand), typeof(DeleteMemoryUnitCommand)];
        MemoriesCatalogDeclarations.Commands.Count.ShouldBe(6);
        MemoriesCatalogDeclarations.Routes.Count.ShouldBe(6);
        MemoriesCatalogDeclarations.Routes.Select(route => route.RouteId).Distinct(StringComparer.Ordinal).Count().ShouldBe(6);
        MemoriesCatalogDeclarations.Commands.Select(command => command.Route.Execution!.HandlerType)
            .ShouldBe(expectedTypes.Select(type => type.FullName));
        foreach (var route in MemoriesCatalogDeclarations.Routes)
        {
            route.Kind.ShouldBe("command");
            MemoriesCatalogDeclarations.ReceiverMethods[route.RouteId].ShouldBe("process");
            route.Execution.ShouldNotBeNull().Aliases.ShouldContain(route.MessageType!);
        }
    }

    [Fact]
    public void AdapterEnrollmentRequiresCanonicalHashingFromTheSelectedEventStore()
    {
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        var adapters = MemoriesCatalogAdapters.Create();
        adapters.Count.ShouldBe(6);
        adapters.Select(adapter => adapter.Declaration.Route.Execution!.HandlerType).ShouldBe(
            MemoriesCatalogDeclarations.Commands.Select(command => command.Route.Execution!.HandlerType));
#else
        Should.Throw<NotSupportedException>(() => MemoriesCatalogAdapters.Create())
            .Message.ShouldContain("canonical payload hashing");
#endif
    }
}
