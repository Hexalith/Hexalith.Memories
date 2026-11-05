namespace Hexalith.Memories.DomainService.Tests;

using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Memories.EventStore.Routing;
using Shouldly;

/// <summary>Verifies the ordinary host reaches its real compiled owner adapters through its runtime dependency closure.</summary>
public sealed class ProductionCatalogAdapterTests
{
    /// <summary>Discovers the production adapter through the host reference and invokes its declared factory.</summary>
    [Fact]
    public void OrdinaryHostReachesItsCompleteIntentAdapterFactory()
    {
        Assembly host = typeof(Program).Assembly;
        AssemblyName? reference = host.GetReferencedAssemblies()
            .SingleOrDefault(assembly => assembly.Name == "Hexalith.Memories.EventStoreExtensions");
        reference.ShouldNotBeNull("The ordinary host must reference its real intent adapter assembly.");

        string? path = new AssemblyDependencyResolver(host.Location).ResolveAssemblyToPath(reference);
        path.ShouldNotBeNull("The production adapter must belong to the actual runtime dependency closure.");
        File.Exists(path).ShouldBeTrue();
        Assembly adapters = Assembly.Load(reference);
        Path.GetFullPath(adapters.Location).ShouldBe(Path.GetFullPath(path));
        RouteCatalogAdapterSourceAttribute source = adapters.GetCustomAttributes<RouteCatalogAdapterSourceAttribute>().ShouldHaveSingleItem();
        source.SourceType.FullName.ShouldBe("Hexalith.Memories.EventStoreExtensions.MemoriesCatalogAdapters");
        MethodInfo factory = source.SourceType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes).ShouldNotBeNull();
        var actual = factory.Invoke(null, null).ShouldBeAssignableTo<IEnumerable<IDeclaredIdempotencyIntentAdapter>>().ShouldNotBeNull().ToArray();

        actual.Length.ShouldBe(6);
        foreach (IDeclaredIdempotencyIntentAdapter adapter in actual)
        {
            adapter.GetType().GetGenericTypeDefinition().ShouldBe(typeof(DeclaredIdempotencyIntentAdapter<>));
        }

        actual.Select(adapter => JsonSerializer.Serialize(adapter.Declaration)).Order(StringComparer.Ordinal)
            .ShouldBe(MemoriesCatalogDeclarations.Commands.Select(command => JsonSerializer.Serialize(command)).Order(StringComparer.Ordinal));
        Assembly[] owners = [typeof(MemoriesCatalogDeclarations).Assembly, adapters];
        CompiledRouteCatalogDeclarations.Read(owners)
            .Select(RouteCatalogCodec.ComputeDeclarationDigest).Order(StringComparer.Ordinal)
            .ShouldBe(actual.Select(adapter => RouteCatalogCodec.ComputeDeclarationDigest(adapter.Declaration.Route)).Order(StringComparer.Ordinal));
        IReadOnlyList<RouteOperationContract> operations = CompiledRouteCatalogDeclarations.ReadOperations(owners);
        operations.Count.ShouldBe(6);
        foreach (RouteOperationContract operation in operations)
        {
            operation.Ingress.ShouldBe("internal");
            operation.Digest.ShouldBe(RouteCatalogCodec.ComputeOperationContractDigest(operation));
        }
    }
}
