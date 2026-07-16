// <copyright file="MemoriesClientImportTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Story 26.2 coverage for the typed backup-import and restore-status client surface.</summary>
public sealed class MemoriesClientImportTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task ImportTenantAsync_Accepted_PostsJsonAndPreservesCallerStream()
    {
        const string Payload = "{\"manifest\":{\"schemaVersion\":1}}";
        HttpMethod? method = null;
        Uri? requestUri = null;
        string? mediaType = null;
        string? uploaded = null;
        RestoreAcceptedResponse accepted = new(
            "restore-1",
            "acme",
            null,
            ExportScope.Tenant,
            "/api/v1/tenants/acme/restore/restore-1");
        TestDelegatingHandler handler = new(async (request, cancellationToken) =>
        {
            method = request.Method;
            requestUri = request.RequestUri;
            mediaType = request.Content?.Headers.ContentType?.MediaType;
            uploaded = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(HttpStatusCode.Accepted, accepted);
        });
        MemoriesClient client = CreateClient(handler);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Payload));

        RestoreAcceptedResponse result = await client.ImportTenantAsync("acme", stream, CancellationToken.None);

        result.ShouldBe(accepted);
        method.ShouldBe(HttpMethod.Post);
        requestUri!.AbsolutePath.ShouldBe("/api/v1/tenants/acme/import");
        mediaType.ShouldBe("application/json");
        uploaded.ShouldBe(Payload);
        stream.CanRead.ShouldBeTrue("Import methods borrow the caller's stream; they must not dispose it.");
    }

    [Fact]
    public async Task ImportCaseAsync_Accepted_UsesEscapedCaseRouteAndDecodesResponse()
    {
        RestoreAcceptedResponse accepted = new(
            "restore-2",
            "acme",
            "case 1",
            ExportScope.Case,
            "/api/v1/tenants/acme/restore/restore-2");
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.Accepted, accepted)));
        MemoriesClient client = CreateClient(handler);
        await using var stream = new MemoryStream("{}"u8.ToArray());

        RestoreAcceptedResponse result = await client.ImportCaseAsync("acme", "case 1", stream, CancellationToken.None);

        result.ShouldBe(accepted);
        handler.Requests.ShouldHaveSingleItem().RequestUri!.AbsolutePath
            .ShouldBe("/api/v1/tenants/acme/cases/case%201/import");
    }

    [Fact]
    public async Task ImportTenantAsync_NonSuccess_ThrowsStructuredRemoteException()
    {
        ErrorResponse error = new("RESTORE_TARGET_BUSY", "busy", "wait");
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.Conflict, error)));
        MemoriesClient client = CreateClient(handler);
        await using var stream = new MemoryStream("{}"u8.ToArray());

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.ImportTenantAsync("acme", stream, CancellationToken.None));

        exception.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        exception.Error.Code.ShouldBe("RESTORE_TARGET_BUSY");
        stream.CanRead.ShouldBeTrue();
    }

    [Fact]
    public async Task ImportTenantAsync_MissingDescriptorFields_ThrowsInvalidResponse()
    {
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        }));
        MemoriesClient client = CreateClient(handler);
        await using var stream = new MemoryStream("{}"u8.ToArray());

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.ImportTenantAsync("acme", stream, CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task GetRestoreStatusAsync_Ok_ReturnsTypedStatus()
    {
        RestoreStatusResponse status = new(
            "restore-1",
            "acme",
            "Completed",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(5),
            3,
            2,
            1,
            0);
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, status)));
        MemoriesClient client = CreateClient(handler);

        RestoreStatusResponse? result = await client.GetRestoreStatusAsync("acme", "restore 1", CancellationToken.None);

        result.ShouldBe(status);
        handler.Requests.ShouldHaveSingleItem().Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldBe("/api/v1/tenants/acme/restore/restore%201");
    }

    [Fact]
    public async Task GetRestoreStatusAsync_NotFound_ReturnsNull()
    {
        TestDelegatingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        MemoriesClient client = CreateClient(handler);

        RestoreStatusResponse? result = await client.GetRestoreStatusAsync("acme", "missing", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ImportTenantAsync_ConfiguredImportTimeout_CancelsLongUpload()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoriesClient(options =>
        {
            options.Endpoint = Endpoint;
            options.ImportTimeout = TimeSpan.FromMilliseconds(100);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new TestDelegatingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }));
        using ServiceProvider provider = services.BuildServiceProvider();
        MemoriesClient client = provider.GetRequiredService<MemoriesClient>();
        await using var stream = new MemoryStream("{}"u8.ToArray());

        await Should.ThrowAsync<TaskCanceledException>(
            () => client.ImportTenantAsync("acme", stream, CancellationToken.None));
        stream.CanRead.ShouldBeTrue();
    }

    private static MemoriesClient CreateClient(TestDelegatingHandler handler)
    {
        HttpClient httpClient = new(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode statusCode, T value)
        => new(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value, MemoriesJsonContext.Options),
                Encoding.UTF8,
                "application/json"),
        };
}
