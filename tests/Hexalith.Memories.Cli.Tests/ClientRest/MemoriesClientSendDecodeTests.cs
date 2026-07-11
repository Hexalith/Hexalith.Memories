// <copyright file="MemoriesClientSendDecodeTests.cs" company="ITANEO">
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
/// Story 25.3 (A21) — proves the single generic send/decode path behind the standard client methods (exercised
/// here through <see cref="MemoriesClient.GetCaseAsync"/>): a non-2xx body maps to a
/// <see cref="MemoriesRemoteException"/>, and a 2xx empty/unparseable body maps to a structured
/// <c>INVALID_RESPONSE</c> whose message is derived from <c>typeof(T).Name</c>. Also drift-guards AC3 — no
/// inline <c>api/…</c> path literal survives in <c>MemoriesClient.cs</c>.
/// </summary>
public class MemoriesClientSendDecodeTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task SendDecode_NonSuccess_MapsToMemoriesRemoteExceptionWithDecodedError()
    {
        string body = JsonSerializer.Serialize(
            new ErrorResponse("SERVER_DOWN", "internal", "retry later"),
            MemoriesJsonContext.Options);
        MemoriesClient client = CreateClient(HttpStatusCode.InternalServerError, body);

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.GetCaseAsync("acme", "case-1", CancellationToken.None));

        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.Error.Code.ShouldBe("SERVER_DOWN");
    }

    [Fact]
    public async Task SendDecode_EmptyBody_ThrowsInvalidResponseNamingTheType()
    {
        var content = new ByteArrayContent([]);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        MemoriesClient client = CreateClient(HttpStatusCode.OK, content);

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.GetCaseAsync("acme", "case-1", CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");

        // The message is derived from typeof(T).Name (here Case). Contracts.V1.Case and Shouldly.Case collide,
        // so the case-sensitivity overload is avoided in favour of the plain substring assertion.
        exception.Error.Message.ShouldContain("Case");
    }

    [Fact]
    public async Task SendDecode_UnparseableBody_ThrowsInvalidResponseNamingTheType()
    {
        MemoriesClient client = CreateClient(HttpStatusCode.OK, "not-json-at-all");

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.GetCaseAsync("acme", "case-1", CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");

        // "parsed as Case" proves typeof(T).Name is interpolated into the INVALID_RESPONSE message.
        exception.Error.Message.ShouldContain("parsed as Case");
    }

    [Theory]
    [InlineData("io")]
    [InlineData("http")]
    [InlineData("unsupported")]
    public async Task SendDecode_ContentReadFailure_ThrowsInvalidResponseWithOriginalCause(string failureKind)
    {
        Exception failure = failureKind switch
        {
            "io" => new IOException("read failed"),
            "http" => new HttpRequestException("transport failed"),
            "unsupported" => new NotSupportedException("unsupported content"),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind)),
        };
        MemoriesClient client = CreateClient(HttpStatusCode.OK, new ThrowingHttpContent(() => failure));

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.GetCaseAsync("acme", "case-1", CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
        exception.InnerException.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task SendDecode_CancelledContentRead_RethrowsCancellation()
    {
        using var cts = new CancellationTokenSource();
        MemoriesClient client = CreateClient(
            HttpStatusCode.OK,
            new ThrowingHttpContent(() =>
            {
                cts.Cancel();
                return new OperationCanceledException(cts.Token);
            }));

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => client.GetCaseAsync("acme", "case-1", cts.Token));

        exception.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public void MemoriesClientSource_ContainsNoInlineApiPathLiteral()
    {
        // AC3 drift guard: every request path must be built from MemoriesRoutes, so no inline "api/…" or
        // "/api/v1/…" string literal may survive in the client. (Doc-comment mentions like <c>/api/v1/…</c> are not
        // string literals and are intentionally not matched.)
        string source = ReadClientSource();

        source.Contains("\"api/", StringComparison.Ordinal).ShouldBeFalse(
            "MemoriesClient.cs must build every request path from MemoriesRoutes — no inline \"api/…\" literal may remain (Story 25.3 AC3).");
        source.Contains("\"/api/v1/", StringComparison.Ordinal).ShouldBeFalse(
            "MemoriesClient.cs must build every request path from MemoriesRoutes — no inline \"/api/v1/…\" literal may remain (Story 25.3 AC3).");
    }

    private static string ReadClientSource()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                string clientPath = Path.Combine(candidate, "src", "Hexalith.Memories.Client.Rest", "MemoriesClient.cs");
                File.Exists(clientPath).ShouldBeTrue($"MemoriesClient.cs not found at {clientPath}");
                return File.ReadAllText(clientPath);
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        throw new FileNotFoundException("Could not locate the repo root (Hexalith.Memories.slnx marker) from the test binary.");
    }

    private static MemoriesClient CreateClient(HttpStatusCode status, string body)
        => CreateClient(status, new StringContent(body, Encoding.UTF8, "application/json"));

    private static MemoriesClient CreateClient(HttpStatusCode status, HttpContent content)
    {
        var handler = new TestDelegatingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = content,
            }));
        var httpClient = new HttpClient(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance);
    }

}
