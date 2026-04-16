// <copyright file="CliTenantListIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Cli;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 7.1 Task 7 — golden-path integration test that exercises <see cref="MemoriesClient"/> against the
/// full Aspire topology. Does NOT spawn the <c>memories</c> binary (packaging validation lives in the
/// dev-only script per Task 8).
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class CliTenantListIntegrationTests
{
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);

    private readonly AspireIngestionPipelineFixture _fixture;

    public CliTenantListIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ListTenantsAsync_AfterProvisioning_ReturnsCreatedTenant()
    {
        // Arrange
        string tenantId = $"tenant-cli-{Guid.NewGuid():N}";
        string displayName = $"Tenant CLI {tenantId}";

        await EnsureTenantActiveAsync(tenantId, displayName);

        // Build the MemoriesClient against the fixture's endpoint (no packaging, no external process).
        using var http = new HttpClient
        {
            BaseAddress = _fixture.MemoriesClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions
        {
            Endpoint = _fixture.MemoriesClient.BaseAddress,
        });
        var client = new MemoriesClient(http, options, NullLogger<MemoriesClient>.Instance);

        // Act
        IReadOnlyList<TenantSummary> tenants = await client.ListTenantsAsync(CancellationToken.None);

        // Assert
        TenantSummary created = tenants.Single(t => t.Id == tenantId);
        created.DisplayName.ShouldBe(displayName);
    }

    private async Task EnsureTenantActiveAsync(string tenantId, string displayName)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/tenants",
                new TenantProvisioningInput(tenantId, displayName),
                MemoriesJsonContext.Options)
            ;

        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(ActivationTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient
                .GetAsync($"/api/tenants/{tenantId}")
                ;

            if (tenantResponse.StatusCode == HttpStatusCode.OK)
            {
                TenantInfo? tenant = await tenantResponse.Content
                    .ReadFromJsonAsync<TenantInfo>(MemoriesJsonContext.Options)
                    ;

                if (tenant?.Status == TenantStatus.Active)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Tenant '{tenantId}' did not reach Active state within {ActivationTimeout}.");
    }
}
