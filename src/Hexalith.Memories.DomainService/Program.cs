namespace Hexalith.Memories.DomainService;

using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Client.Registration;

/// <summary>Dedicated authenticated SDK command/replay host; the native Memories Server keeps its existing surface.</summary>
public partial class Program
{
    /// <summary>Starts the three-domain authoritative decision service.</summary>
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddEventStoreDomainService(typeof(MemoriesTenantAggregateAdapter).Assembly, typeof(Hexalith.Memories.EventStore.Domain.Events.IMemoriesEventPayload).Assembly);
        builder.Services.AddEventStoreReadModelStore();
        builder.Services.AddEventStoreDevelopmentDomainCatalog(builder.Configuration, typeof(Hexalith.Memories.EventStore.Routing.MemoriesCatalogDeclarations).Assembly);
        WebApplication app = builder.Build();
        app.UseEventStoreDomainService();
        app.Run();
    }
}
