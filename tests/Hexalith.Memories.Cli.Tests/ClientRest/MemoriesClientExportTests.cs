// <copyright file="MemoriesClientExportTests.cs" company="ITANEO">
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

/// <summary>
/// Story 8.3 — <see cref="MemoriesClient"/> export methods (Task 4). Exercises streaming over a
/// <see cref="TestDelegatingHandler"/>.
/// </summary>
public class MemoriesClientExportTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task ExportCaseAsync_200Response_ReturnsReadableStream()
    {
        const string Body = "{\"manifest\":{\"schemaVersion\":1}}";
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Body, Encoding.UTF8, "application/json"),
        }));
        MemoriesClient client = CreateClient(handler);

        await using Stream stream = await client.ExportCaseAsync("acme", "case-1", CancellationToken.None);
        using StreamReader reader = new(stream);
        string body = await reader.ReadToEndAsync();

        body.ShouldBe(Body);
    }

    [Fact]
    public async Task ExportTenantAsync_200Response_ReturnsReadableStream()
    {
        const string Body = "{\"manifest\":{\"schemaVersion\":1,\"scope\":\"tenant\"}}";
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Body, Encoding.UTF8, "application/json"),
        }));
        MemoriesClient client = CreateClient(handler);

        await using Stream stream = await client.ExportTenantAsync("acme", CancellationToken.None);
        using StreamReader reader = new(stream);
        string body = await reader.ReadToEndAsync();

        body.ShouldBe(Body);
    }

    [Fact]
    public async Task ExportCaseAsync_NotFound_ThrowsRemoteException()
    {
        ErrorResponse error = new("CASE_NOT_FOUND", "Case 'missing' not found in tenant 'acme'.", "List cases with ...");
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(JsonSerializer.Serialize(error, MemoriesJsonContext.Options), Encoding.UTF8, "application/json"),
        }));
        MemoriesClient client = CreateClient(handler);

        MemoriesRemoteException thrown = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.ExportCaseAsync("acme", "missing", CancellationToken.None));

        thrown.Error.Code.ShouldBe("CASE_NOT_FOUND");
    }

    [Fact]
    public async Task ExportTenantAsync_IssuesGetRequestToExpectedPath()
    {
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        }));
        MemoriesClient client = CreateClient(handler);

        await using Stream _ = await client.ExportTenantAsync("acme", CancellationToken.None);

        handler.Requests.ShouldHaveSingleItem();
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldBe("/api/v1/tenants/acme/export");
    }

    private static MemoriesClient CreateClient(TestDelegatingHandler handler)
    {
        HttpClient httpClient = new(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance);
    }
}
