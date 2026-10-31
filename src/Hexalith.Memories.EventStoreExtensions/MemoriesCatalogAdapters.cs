using Hexalith.EventStore.DomainService;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStore.Routing;

[assembly: RouteCatalogAdapterSource(typeof(Hexalith.Memories.EventStoreExtensions.MemoriesCatalogAdapters))]
#endif

namespace Hexalith.Memories.EventStoreExtensions;

/// <summary>Typed local policy adapters owned by the existing Memories command contracts.</summary>
public static class MemoriesCatalogAdapters
{
    /// <summary>Creates the exact six internal command adapters without enabling hosted admission.</summary>
    /// <exception cref="NotSupportedException">The selected EventStore packages do not expose canonical payload hashing.</exception>
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    public static IReadOnlyList<IDeclaredIdempotencyIntentAdapter> Create() => [
        Adapter<RegisterTenantCommand>(),
        Adapter<UpdateTenantLifecycleStatusCommand>(),
        Adapter<CreateCaseCommand>(),
        Adapter<DeleteCaseCommand>(),
        Adapter<RequestAnnotationCommand>(),
        Adapter<DeleteMemoryUnitCommand>()];

    private static DeclaredIdempotencyIntentAdapter<T> Adapter<T>() => new(
        MemoriesCatalogDeclarations.Commands.Single(c => c.Route.Execution!.HandlerType == typeof(T).FullName),
        payload => new Dictionary<string, object?> { ["payload-sha256"] = CanonicalIntentPayload.Sha256(payload) },
        completePayloadFields: payload => new Dictionary<string, object?> { ["payload-sha256"] = CanonicalIntentPayload.Sha256CanonicalJson(payload) });
#else
    public static IReadOnlyList<IDeclaredIdempotencyIntentAdapter> Create() =>
        throw new NotSupportedException("Memories catalog adapter enrollment requires source-selected EventStore canonical payload hashing APIs.");
#endif
}

