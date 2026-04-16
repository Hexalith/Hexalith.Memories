// <copyright file="MemoriesClientTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

public class MemoriesClientTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task ListTenantsAsync_SuccessStatus_ReturnsDeserializedTenantSummaries()
    {
        // Arrange
        TenantSummary tenant = CreateSummary("tenant-1", "Tenant One");
        string json = JsonSerializer.Serialize(new[] { tenant }, MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.OK, json);

        // Act
        IReadOnlyList<TenantSummary> result = await client.ListTenantsAsync(CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("tenant-1");
        result[0].DisplayName.ShouldBe("Tenant One");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ListTenantsAsync_AuthFailures_ThrowMemoriesRemoteExceptionWithStatus(HttpStatusCode status)
    {
        // Arrange
        string body = JsonSerializer.Serialize(new ErrorResponse("AUTH_FAIL", "denied", "check creds"), MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(status, body);

        // Act
        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.ListTenantsAsync(CancellationToken.None));

        // Assert
        exception.StatusCode.ShouldBe(status);
        exception.Error.Code.ShouldBe("AUTH_FAIL");
    }

    [Fact]
    public async Task ListTenantsAsync_500WithErrorResponseBody_ParsesErrorResponse()
    {
        // Arrange
        string body = JsonSerializer.Serialize(new ErrorResponse("SERVER_DOWN", "internal", "retry later"), MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.InternalServerError, body);

        // Act
        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.ListTenantsAsync(CancellationToken.None));

        // Assert
        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.Error.Code.ShouldBe("SERVER_DOWN");
        exception.Error.Suggestion.ShouldBe("retry later");
    }

    [Fact]
    public async Task ListTenantsAsync_MalformedJsonOn2xx_ThrowsMemoriesRemoteException()
    {
        // Arrange - server returns 200 with malformed payload (not masked as success).
        MemoriesClient client = CreateClient(HttpStatusCode.OK, "not valid json");

        // Act & Assert
        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.ListTenantsAsync(CancellationToken.None));
        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task ProbeHealthAsync_Ok_ReturnsTrue()
    {
        MemoriesClient client = CreateClient(HttpStatusCode.OK, string.Empty);
        bool result = await client.ProbeHealthAsync(CancellationToken.None);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ProbeHealthAsync_Accepted_ReturnsTrue()
    {
        MemoriesClient client = CreateClient(HttpStatusCode.Accepted, string.Empty);
        bool result = await client.ProbeHealthAsync(CancellationToken.None);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ProbeHealthAsync_ServiceUnavailable_ReturnsFalse()
    {
        MemoriesClient client = CreateClient(HttpStatusCode.ServiceUnavailable, string.Empty);
        bool result = await client.ProbeHealthAsync(CancellationToken.None);
        result.ShouldBeFalse();
    }

    private static MemoriesClient CreateClient(HttpStatusCode status, string body)
    {
        var handler = new TestDelegatingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }));
        var httpClient = new HttpClient(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance);
    }

    private static TenantSummary CreateSummary(string id, string displayName)
        => new()
        {
            Id = id,
            DisplayName = displayName,
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            IndexSizes = new TenantIndexSizes(null, null, null),
            IndexStatus = new TenantIndexStatus(IndexHealth.Unknown, IndexHealth.Unknown, IndexHealth.Unknown),
            ReindexRequired = false,
        };
}
