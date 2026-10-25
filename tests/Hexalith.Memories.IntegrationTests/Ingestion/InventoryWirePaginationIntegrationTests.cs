namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Text.Json;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using System.Security.Claims;

/// <summary>Accepted escaped identifiers remain enumerable through the real producer and bounded public REST client.</summary>
[Collection("RedisStack")]
public sealed class InventoryWirePaginationIntegrationTests(RedisStackFixture redis)
{
    [Fact]
    public async Task MaximallyEscapedAcceptedEntriesUseByteAdaptiveContinuationsThroughFullCapacity()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "wire-pages-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:wire-pagination";
        string escapedCase = new('漢', 512), escapedWorkflow = new('漢', 500), escapedReference = new('漢', 2048);
        SourceInventoryEntry[] entries = Enumerable.Range(0, SourceInventoryStore.Capacity).Select(index => index < 120
            ? new SourceInventoryEntry(escapedCase, escapedWorkflow + index, SourcePayloadReference:
                new(escapedReference, new string('a', 64), 1, WorkflowPayloadKind.SourceBytes, tenant, new string('漢', 512)))
            : new SourceInventoryEntry("case", "workflow-" + index)).ToArray();
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "WirePageTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        // Owner reconciliation validates and persists the same accepted maximally escaped inputs.
        await inventory.FenceAsync(tenant, source, false, token);
        SourceInventoryPage initial = await inventory.ReadAsync(tenant, source, null, 100, token);
        await inventory.ReconcileAsync(tenant, source, initial.Version, entries, "owned-wire-test", token);
        var first = await inventory.ReadAsync(tenant, source, null, 100, token);
        first.Entries.Count.ShouldBeLessThan(100);
        JsonSerializer.SerializeToUtf8Bytes(first, MemoriesJsonContext.Options).Length.ShouldBeLessThanOrEqualTo(SourceInventoryStore.MaximumPageBytes);
        // Reopen so endpoint terminal observation cannot mutate these intentionally pending entries.
        // Coverage and cursor authority remain real; no synthetic endpoint pagination implementation.
        WebApplicationBuilder builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(inventory);
        builder.Services.AddSingleton(Substitute.For<IIngestionWorkflowStateReader>());
        builder.Services.AddSingleton(new SourceInventoryFailureReconciler(redis.Connection, redis.Connection, new GraphQueryBuilder(), Substitute.For<IWorkflowPayloadStore>(), inventory));
        await using WebApplication app = builder.Build();
        app.Use((context, next) => { context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ServerTenantClaimsTransformation.TenantClaimType, tenant)], "owned-test")); return next(context); });
        app.MapSourceInventoryEndpoints(); await app.StartAsync(token);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var client = new MemoriesClient(http);
        List<SourceInventoryEntry> actual = []; string? cursor = null; string? version = null; int pages = 0;
        do
        {
            SourceInventoryPage page = await client.GetSourceInventoryAsync(tenant, source, cursor, 100, token);
            page.Version.ShouldBe(version ?? page.Version); version = page.Version;
            JsonSerializer.SerializeToUtf8Bytes(page, MemoriesJsonContext.Options).Length.ShouldBeLessThanOrEqualTo(SourceInventoryStore.MaximumPageBytes);
            page.Entries.ShouldNotBeEmpty(); actual.AddRange(page.Entries); cursor = page.NextCursor; pages++;
        } while (cursor is not null);
        actual.Count.ShouldBe(SourceInventoryStore.Capacity); actual.ShouldBe(entries); pages.ShouldBeGreaterThan(11);
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        (await new SourceInventoryStore(restarted, codec).ReadAsync(tenant, source, first.NextCursor, 100, token)).Version.ShouldBe(first.Version);
    }
}
