// <copyright file="QuickstartTenantProvisionerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Net;

using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

public sealed class QuickstartTenantProvisionerTests
{
    [Fact]
    public async Task EnsureSampleTenant_Existing_ReturnsAlreadyExisted()
    {
        var client = new StubTenantClient();
        client.ExistingTenant = new TenantInfo("acme", "Acme", TenantStatus.Active, DateTimeOffset.UtcNow);

        var provisioner = new QuickstartTenantProvisioner(client, TimeProvider.System);
        QuickstartTenantResult result = await provisioner.EnsureSampleTenantAsync("acme", CancellationToken.None);

        result.AlreadyExisted.ShouldBeTrue();
        result.Created.ShouldBeFalse();
        result.ErrorCode.ShouldBeNull();
        client.CreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task EnsureSampleTenant_Fresh_CreatesAndWaitsForActive()
    {
        var client = new StubTenantClient();
        client.ExistingTenant = null;
        client.BecomeActiveAfterCalls = 2;

        var clock = new FakeTimeProvider();
        var provisioner = new QuickstartTenantProvisioner(client, clock);

        Task<QuickstartTenantResult> task = provisioner.EnsureSampleTenantAsync("acme", CancellationToken.None);

        for (int i = 0; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await Task.Yield();
        }

        QuickstartTenantResult result = await task;

        result.Created.ShouldBeTrue();
        result.AlreadyExisted.ShouldBeFalse();
        result.ErrorCode.ShouldBeNull();
        client.CreateCalls.ShouldBe(1);
    }

    [Fact]
    public async Task EnsureSampleTenant_CustomTimeout_UsesProvidedBudget()
    {
        var client = new StubTenantClient();
        client.ExistingTenant = null;
        client.BecomeActiveAfterCalls = int.MaxValue;

        var clock = new FakeTimeProvider();
        var provisioner = new QuickstartTenantProvisioner(client, clock);

        Task<QuickstartTenantResult> task = provisioner.EnsureSampleTenantAsync(
            "acme",
            TimeSpan.FromSeconds(3),
            CancellationToken.None);

        for (int i = 0; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await Task.Yield();
        }

        QuickstartTenantResult result = await task;

        result.Created.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TENANT_PROVISIONING");
        result.Diagnostic.ShouldContain("3s");
    }

    [Fact]
    public async Task EnsureSampleTenant_RemoteException_BubblesOut()
    {
        var client = new StubTenantClient();
        client.ExistingTenant = null;
        client.ThrowOnCreate = new MemoriesRemoteException(
            HttpStatusCode.BadRequest,
            new ErrorResponse("INVALID_TENANT_ID", "bad id", "fix it"));

        var provisioner = new QuickstartTenantProvisioner(client, TimeProvider.System);

        await Should.ThrowAsync<MemoriesRemoteException>(() => provisioner.EnsureSampleTenantAsync("bad!", CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSampleTenant_DeletingTenant_ReturnsTenantDeleting()
    {
        var client = new StubTenantClient();
        client.ExistingTenant = new TenantInfo("acme", "Acme", TenantStatus.Deleting, DateTimeOffset.UtcNow);

        var provisioner = new QuickstartTenantProvisioner(client, TimeProvider.System);
        QuickstartTenantResult result = await provisioner.EnsureSampleTenantAsync("acme", CancellationToken.None);

        result.Created.ShouldBeFalse();
        result.AlreadyExisted.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TENANT_DELETING");
    }

    private sealed class StubTenantClient : MemoriesClient
    {
        public TenantInfo? ExistingTenant { get; set; }

        public int BecomeActiveAfterCalls { get; set; }

        public MemoriesRemoteException? ThrowOnCreate { get; set; }

        public int CreateCalls { get; private set; }

        public int GetCalls { get; private set; }

        public StubTenantClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public override Task<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken ct)
        {
            GetCalls++;

            if (ExistingTenant is not null)
            {
                return Task.FromResult<TenantInfo?>(ExistingTenant);
            }

            // Fresh-create flow: return null until CreateTenantAsync was called AND the caller has
            // polled at least BecomeActiveAfterCalls times.
            if (CreateCalls == 0)
            {
                return Task.FromResult<TenantInfo?>(null);
            }

            if (GetCalls < BecomeActiveAfterCalls)
            {
                return Task.FromResult<TenantInfo?>(new TenantInfo(tenantId, "Sample", TenantStatus.Provisioning, DateTimeOffset.UtcNow));
            }

            return Task.FromResult<TenantInfo?>(new TenantInfo(tenantId, "Sample", TenantStatus.Active, DateTimeOffset.UtcNow));
        }

#pragma warning disable HXL001
        public override Task<string> CreateTenantAsync(string tenantId, string displayName, CancellationToken ct)
        {
            CreateCalls++;
            if (ThrowOnCreate is not null)
            {
                throw ThrowOnCreate;
            }

            return Task.FromResult("workflow-instance-123");
        }
#pragma warning restore HXL001
    }
}
