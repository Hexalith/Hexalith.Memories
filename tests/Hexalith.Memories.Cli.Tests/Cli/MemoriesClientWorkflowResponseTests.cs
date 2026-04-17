// <copyright file="MemoriesClientWorkflowResponseTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Net;
using System.Net.Http;
using System.Text;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

public sealed class MemoriesClientWorkflowResponseTests
{
    [Fact]
    public async Task CreateTenantAsync_ThrowsInvalidResponse_WhenWorkflowBodyMissing()
    {
        using var httpClient = CreateClient(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        });

        var client = new MemoriesClient(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

#pragma warning disable HXL001
        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(() => client.CreateTenantAsync("acme", "Acme", CancellationToken.None));
#pragma warning restore HXL001

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task IngestAsync_ThrowsInvalidResponse_WhenWorkflowBodyMalformed()
    {
        using var httpClient = CreateClient(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("{\"instanceId\":42}", Encoding.UTF8, "application/json"),
        });

        var client = new MemoriesClient(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

#pragma warning disable HXL001
        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(() => client.IngestAsync(
            "acme",
            "case-1",
            "test://acme/case-1",
            Encoding.UTF8.GetBytes("sample"),
            "text/plain",
            "tester",
            metadata: null,
            CancellationToken.None));
#pragma warning restore HXL001

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task CreateCaseAsync_ThrowsInvalidResponse_WhenBodyEmpty()
    {
        using var httpClient = CreateClient(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        });

        var client = new MemoriesClient(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

#pragma warning disable HXL001
        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(() => client.CreateCaseAsync(
            "acme",
            "Sample",
            description: null,
            CancellationToken.None));
#pragma warning restore HXL001

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task GetTenantAsync_ReturnsNull_WhenNotFound()
    {
        using var httpClient = CreateClient(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        });

        var client = new MemoriesClient(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

        TenantInfo? result = await client.GetTenantAsync("missing", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetTenantAsync_ThrowsInvalidResponse_WhenBodyMalformed()
    {
        using var httpClient = CreateClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not-json", Encoding.UTF8, "application/json"),
        });

        var client = new MemoriesClient(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(() => client.GetTenantAsync("acme", CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    private static HttpClient CreateClient(HttpResponseMessage response)
        => new(new StaticResponseHandler(response)) { BaseAddress = new Uri("http://127.0.0.1:65010/") };

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(CloneResponse(response));

        private static HttpResponseMessage CloneResponse(HttpResponseMessage response)
        {
            var clone = new HttpResponseMessage(response.StatusCode);
            if (response.Content is not null)
            {
                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                clone.Content = new StringContent(content, Encoding.UTF8, response.Content.Headers.ContentType?.MediaType ?? "application/json");
            }

            return clone;
        }
    }
}