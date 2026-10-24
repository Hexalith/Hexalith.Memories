namespace Hexalith.Memories.Server.Tests.Hosting;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Memories.EventStore.Domain;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

/// <summary>Executes the actual Memories SDK registration with configured target and independent per-domain authority.</summary>
public sealed class MemoriesGatewayCompositionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfiguredAndLegacyPrefixesRetainWorkloadAndSidecarCredentials(bool legacy)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "synthetic-issuer",
            ["Authentication:JwtBearer:Audience"] = "synthetic-gateway",
            ["Authentication:JwtBearer:SigningKey"] = "synthetic-signing-key-with-at-least-32-bytes",
            [(legacy ? "EventStoreIntegration" : "EventStore") + ":CommandGateway:BaseAddress"] = "http://configured.invalid/target",
            ["EventStore:CommandGateway:AppId"] = "owned-gateway",
            ["EventStore:CommandGateway:Audience"] = "owned-audience",
            ["DAPR_API_TOKEN"] = "owned-sidecar-token",
        });
        builder.AddMemoriesEventStoreGateway();
        var capture = new MemoriesGatewayCapturingHandler();
        builder.Services.AddHttpClient<IEventStoreGatewayClient, EventStoreGatewayClient>().ConfigurePrimaryHttpMessageHandler(() => capture);
        await using ServiceProvider services = builder.Services.BuildServiceProvider();
        IEventStoreGatewayClient client = services.GetRequiredService<IEventStoreGatewayClient>();
#if HEXALITH_EVENTSTORE_MULTI_DOMAIN_WORKLOAD
        string[] domains = [MemoriesDomain.Tenants, MemoriesDomain.Cases, MemoriesDomain.MemoryUnits];
#else
        string[] domains = [MemoriesDomain.Tenants];
#endif
        foreach (string domain in domains)
        {
            _ = await client.SubmitCommandAsync(new SubmitCommandRequest("message", "tenant-a", domain, "unit", "Command",
                JsonSerializer.SerializeToElement(new { })), TestContext.Current.CancellationToken);
            capture.Address.ShouldBe("http://configured.invalid/target/api/v1/commands/workload");
            capture.AppId.ShouldBe("owned-gateway");
            capture.SidecarToken.ShouldBe("owned-sidecar-token");
            capture.Authorization.ShouldBeNull();
            var assertion = new JsonWebToken(capture.Assertion!);
            assertion.Audiences.ShouldBe(["owned-audience"]);
            assertion.GetClaim(EventStoreWorkloadAuthenticationDefaults.CallerClaimType).Value.ShouldBe("memories");
            assertion.GetClaim(EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType).Value.ShouldBe(domain);
            assertion.GetClaim(EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType).Value.ShouldBe("tenant-a");
        }
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LegacyInvocationRootSelectsOneSdkRoutingModeAndRejectsConflictingTarget(bool explicitTarget, bool conflict)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "synthetic-issuer",
            ["Authentication:JwtBearer:SigningKey"] = "synthetic-signing-key-with-at-least-32-bytes",
            ["EventStoreIntegration:CommandGateway:BaseAddress"] = "http://sidecar.invalid/proxy/v1.0/invoke/owned-gateway/method/",
            ["EventStoreIntegration:CommandGateway:AppId"] = explicitTarget ? conflict ? "other-gateway" : "owned-gateway" : null,
            ["EventStoreIntegration:CommandGateway:Audience"] = "owned-audience", ["DAPR_API_TOKEN"] = "owned-sidecar-token",
        });
        if (conflict)
        {
            Should.Throw<InvalidOperationException>(() => builder.AddMemoriesEventStoreGateway()).Message.ShouldContain("conflicts");
            return;
        }
        builder.AddMemoriesEventStoreGateway();
        var capture = new MemoriesGatewayCapturingHandler();
        builder.Services.AddHttpClient<IEventStoreGatewayClient, EventStoreGatewayClient>().ConfigurePrimaryHttpMessageHandler(() => capture);
        await using ServiceProvider services = builder.Services.BuildServiceProvider();
        _ = await services.GetRequiredService<IEventStoreGatewayClient>().SubmitCommandAsync(new SubmitCommandRequest("message", "tenant-a", MemoriesDomain.Tenants,
            "unit", "Command", JsonSerializer.SerializeToElement(new { })), TestContext.Current.CancellationToken);
        capture.Address.ShouldBe("http://sidecar.invalid/proxy/api/v1/commands/workload");
        capture.AppId.ShouldBe("owned-gateway");
        new JsonWebToken(capture.Assertion!).Audiences.ShouldBe(["owned-audience"]);
    }

    [Fact]
    public async Task DefaultDaprRoutingUsesSidecarRootWithSdkOwnedTargetHeader()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "synthetic-issuer",
            ["Authentication:JwtBearer:SigningKey"] = "synthetic-signing-key-with-at-least-32-bytes",
            ["EventStore:CommandGateway:AppId"] = "owned-gateway", ["DAPR_API_TOKEN"] = "owned-sidecar-token",
        });
        builder.AddMemoriesEventStoreGateway();
        var capture = new MemoriesGatewayCapturingHandler();
        builder.Services.AddHttpClient<IEventStoreGatewayClient, EventStoreGatewayClient>().ConfigurePrimaryHttpMessageHandler(() => capture);
        await using ServiceProvider services = builder.Services.BuildServiceProvider();
        _ = await services.GetRequiredService<IEventStoreGatewayClient>().SubmitCommandAsync(new SubmitCommandRequest("message", "tenant-a", MemoriesDomain.Tenants,
            "unit", "Command", JsonSerializer.SerializeToElement(new { })), TestContext.Current.CancellationToken);
        new Uri(capture.Address!).AbsolutePath.ShouldBe("/api/v1/commands/workload");
        capture.AppId.ShouldBe("owned-gateway");
        capture.SidecarToken.ShouldBe("owned-sidecar-token");
    }

#if HEXALITH_EVENTSTORE_MULTI_DOMAIN_WORKLOAD
    [Fact]
    public async Task ActualCommandStoreRejectsMissingPrincipalProofBeforeHttp()
    {
        var gateway = NSubstitute.Substitute.For<IEventStoreGatewayClient>();
        var store = new EventStoreMemoriesCommandStore(gateway);
        await Should.ThrowAsync<InvalidOperationException>(() => store.AcceptAsync("tenant-a",
            new RegisterTenantCommand("tenant-a", "Synthetic", DateTimeOffset.UtcNow), "actor", TestContext.Current.CancellationToken));
        NSubstitute.SubstituteExtensions.ReceivedCalls(gateway).ShouldBeEmpty();
    }

    [Fact]
    public async Task ActualCommandStoreSignsActorSourceAndExactCommandForEveryDomain()
    {
        using RSA rsa = RSA.Create(2048);
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "synthetic-issuer",
            ["Authentication:JwtBearer:SigningKey"] = "synthetic-signing-key-with-at-least-32-bytes",
            ["Authentication:WorkloadIssuer:Workload"] = "memories",
            ["EventStore:CommandGateway:BaseAddress"] = "http://configured.invalid/target/",
            ["EventStore:CommandGateway:BoundCommand:PrivateKeyPkcs8Base64"] = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()),
            ["DAPR_API_TOKEN"] = "owned-sidecar-token",
        });
        builder.AddMemoriesEventStoreGateway();
        var capture = new MemoriesGatewayCapturingHandler();
        builder.Services.AddHttpClient<IEventStoreGatewayClient, EventStoreGatewayClient>().ConfigurePrimaryHttpMessageHandler(() => capture);
        await using ServiceProvider services = builder.Services.BuildServiceProvider();
        var store = new EventStoreMemoriesCommandStore(services.GetRequiredService<IEventStoreGatewayClient>(),
            services.GetRequiredService<IEventStoreGatewayBoundCommandProofSource>());
        await AssertCommand(new RegisterTenantCommand("tenant-a", "Synthetic", DateTimeOffset.UtcNow), MemoriesDomain.Tenants);
        await AssertCommand(new CreateCaseCommand("tenant-a", "case", "Synthetic", null, DateTimeOffset.UtcNow), MemoriesDomain.Cases);
        await AssertCommand(new DeleteMemoryUnitCommand("tenant-a", "case", "unit", [], DateTimeOffset.UtcNow), MemoriesDomain.MemoryUnits);

        async Task AssertCommand<T>(T command, string domain) where T : IMemoriesCommandContract
        {
            (await store.AcceptAsync("tenant-a", command, "actor-a", TestContext.Current.CancellationToken)).ShouldBe("correlation");
            capture.Address.ShouldBe("http://configured.invalid/target/api/v1/commands/bound/workload");
            capture.Authorization.ShouldBeNull();
            capture.SidecarToken.ShouldBe("owned-sidecar-token");
            BoundWorkloadCommandRequest bound = JsonSerializer.Deserialize<BoundWorkloadCommandRequest>(capture.Body!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            bound.PrincipalId.ShouldBe("actor-a");
            bound.Command.Extensions.ShouldNotBeNull()["actorId"].ShouldBe("actor-a");
            bound.Command.Extensions["source"].ShouldBe("Hexalith.Memories.Server");
            string publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            BoundWorkloadCommandProof.Verify(bound, capture.CommandProof, "memories", publicKey, DateTimeOffset.UtcNow).ShouldBeTrue();
            BoundWorkloadCommandProof.Verify(bound with { PrincipalId = "other" }, capture.CommandProof, "memories", publicKey, DateTimeOffset.UtcNow).ShouldBeFalse();
            BoundWorkloadCommandProof.Verify(bound with { Command = bound.Command with { Domain = "other" } }, capture.CommandProof, "memories", publicKey, DateTimeOffset.UtcNow).ShouldBeFalse();
            BoundWorkloadCommandProof.Verify(bound, capture.CommandProof + "x", "memories", publicKey, DateTimeOffset.UtcNow).ShouldBeFalse();
            var assertion = new JsonWebToken(capture.Assertion!);
            assertion.GetClaim(EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType).Value.ShouldBe(domain);
            assertion.GetClaim(EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType).Value.ShouldBe("tenant-a");
        }
    }
#endif

}
