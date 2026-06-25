// <copyright file="MemoriesClientLookupTests.cs" company="ITANEO">
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
/// Story 18.5 — <see cref="MemoriesClient.LookupMemoryUnitIdBySourceUriAsync"/> coverage via the supported
/// <see cref="HttpClient"/> mock seam (D9 — no <c>IMemoriesClient</c>): a hit returns the id, a structured
/// 404 returns <see langword="null"/>, any other non-2xx throws, and all path segments + the query value are
/// URL-encoded.
/// </summary>
public class MemoriesClientLookupTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task LookupAsync_200_ReturnsMemoryUnitId()
    {
        string json = JsonSerializer.Serialize(new MemoryUnitIdLookupResponse { MemoryUnitId = "mu-77" }, MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.OK, json);

        string? id = await client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///doc.pdf", CancellationToken.None);

        id.ShouldBe("mu-77");
    }

    [Fact]
    public async Task LookupAsync_404_ReturnsNull()
    {
        string body = JsonSerializer.Serialize(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", "no unit", "verify uri"), MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.NotFound, body);

        string? id = await client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///missing.pdf", CancellationToken.None);

        id.ShouldBeNull();
    }

    [Fact]
    public async Task LookupAsync_503BackendError_ThrowsMemoriesRemoteException_NotNull()
    {
        // A backend error must NOT be flattened into a miss — the consumer must distinguish outage from absence.
        string body = JsonSerializer.Serialize(new ErrorResponse("LOOKUP_BACKEND_UNAVAILABLE", "down", "retry"), MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.ServiceUnavailable, body);

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///doc.pdf", CancellationToken.None));

        exception.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        exception.Error.Code.ShouldBe("LOOKUP_BACKEND_UNAVAILABLE");
    }

    [Fact]
    public async Task LookupAsync_200EmptyBody_ThrowsInvalidResponse_NotNull()
    {
        // A 2xx whose body deserializes to null is a contract violation, NOT a miss — surfacing it as null
        // would let a version skew masquerade as "no unit". It must raise a structured INVALID_RESPONSE.
        MemoriesClient client = CreateClient(HttpStatusCode.OK, "null");

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///doc.pdf", CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task LookupAsync_200UnparseableBody_ThrowsInvalidResponse()
    {
        // A 2xx whose body is not valid MemoryUnitIdLookupResponse JSON must map to a structured
        // INVALID_RESPONSE (the JsonException branch), never a silent miss.
        MemoriesClient client = CreateClient(HttpStatusCode.OK, "not-json-at-all");

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.LookupMemoryUnitIdBySourceUriAsync("acme", "case-1", "file:///doc.pdf", CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task LookupAsync_EncodesPathSegmentsAndQueryValue()
    {
        string json = JsonSerializer.Serialize(new MemoryUnitIdLookupResponse { MemoryUnitId = "mu-1" }, MemoriesJsonContext.Options);
        (MemoriesClient client, Func<Uri?> capturedUri) = CreateCapturingClient(json);

        await client.LookupMemoryUnitIdBySourceUriAsync("te/nant", "ca se", "file:///doc.pdf", CancellationToken.None);

        Uri? uri = capturedUri();
        uri.ShouldNotBeNull();
        string pathAndQuery = uri.PathAndQuery;
        pathAndQuery.ShouldContain("api/tenants/te%2Fnant/cases/ca%20se/memory-units/by-source-uri");
        pathAndQuery.ShouldContain("sourceUri=file%3A%2F%2F%2Fdoc.pdf");
    }

    [Theory]
    [InlineData("", "case-1", "file:///doc.pdf")]
    [InlineData("acme", "", "file:///doc.pdf")]
    [InlineData("acme", "case-1", "  ")]
    public async Task LookupAsync_BlankArgs_ThrowsArgumentException(string tenantId, string caseId, string sourceUri)
    {
        MemoriesClient client = CreateClient(HttpStatusCode.OK, "{}");

        await Should.ThrowAsync<ArgumentException>(
            () => client.LookupMemoryUnitIdBySourceUriAsync(tenantId, caseId, sourceUri, CancellationToken.None));
    }

    private static (MemoriesClient Client, Func<Uri?> CapturedUri) CreateCapturingClient(string responseBody)
    {
        Uri? captured = null;
        var handler = new TestDelegatingHandler((request, _) =>
        {
            captured = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            });
        });
        var httpClient = new HttpClient(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return (new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance), () => captured);
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
}
