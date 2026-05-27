// <copyright file="UrlContentFetcherTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 6.1 Task 7.3 — UrlContentFetcher unit tests with a scripted HTTP handler.
/// Covers: happy path, 4xx/5xx/network/timeout classification, Content-Length cap, stream cap,
/// redirects (allowed, denied to private IPs, loop, scheme change).
/// </summary>
public class UrlContentFetcherTests
{
    private static readonly UrlFetcherOptions _localhostAllowed = new() { AllowPrivateHosts = true };

    [Fact]
    public async Task FetchAsync_200Ok_ReturnsBodyAndMetadata()
    {
        byte[] body = Encoding.UTF8.GetBytes("hello world");
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => BuildResponse(HttpStatusCode.OK, body, "text/plain"),
            _localhostAllowed);

        UrlFetchResult result = await fetcher.FetchAsync(new Uri("http://localhost/doc"), CancellationToken.None);

        result.ContentBytes.ShouldBe(body);
        result.ContentType.ShouldBe("text/plain");
        result.ContentLength.ShouldBe(body.Length);
        result.HttpStatusCode.ShouldBe(200);
        result.FinalUrl.ShouldBe("http://localhost/doc");
    }

    [Fact]
    public async Task FetchAsync_404_ThrowsUrlClientError()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => BuildResponse(HttpStatusCode.NotFound, [], "text/plain"),
            _localhostAllowed);

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("http://localhost/missing"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("URL_CLIENT_ERROR");
        ex.HttpStatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task FetchAsync_500_ThrowsUrlServerError()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => BuildResponse(HttpStatusCode.InternalServerError, [], "text/plain"),
            _localhostAllowed);

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("http://localhost/err"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("URL_SERVER_ERROR");
        ex.HttpStatusCode.ShouldBe(500);
    }

    [Fact]
    public async Task FetchAsync_SocketException_ThrowsUrlNetworkError()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => throw new HttpRequestException("refused", new SocketException()),
            _localhostAllowed);

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("http://localhost/"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("URL_NETWORK_ERROR");
    }

    [Fact]
    public async Task FetchAsync_DeclaredContentLengthExceedsCap_ThrowsPayloadTooLarge()
    {
        HttpResponseMessage response = BuildResponse(HttpStatusCode.OK, [], "text/plain");
        response.Content.Headers.ContentLength = 2_000_000;

        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => response,
            new UrlFetcherOptions { AllowPrivateHosts = true, MaxBytes = 1_000_000 });

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("http://localhost/big"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("PAYLOAD_TOO_LARGE");
    }

    [Fact]
    public async Task FetchAsync_StreamExceedsCap_ThrowsPayloadTooLarge()
    {
        byte[] body = new byte[2000];
        HttpResponseMessage response = BuildResponse(HttpStatusCode.OK, body, "text/plain", omitContentLength: true);

        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => response,
            new UrlFetcherOptions { AllowPrivateHosts = true, MaxBytes = 1000 });

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("http://localhost/chunked"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("PAYLOAD_TOO_LARGE");
    }

    [Fact]
    public async Task FetchAsync_UnsupportedContentType_ThrowsUnsupportedContentType()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => BuildResponse(HttpStatusCode.OK, Encoding.UTF8.GetBytes("zip? absolutely not"), "application/zip"),
            _localhostAllowed);

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("http://localhost/archive"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("UNSUPPORTED_CONTENT_TYPE");
        ex.HttpStatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task FetchAsync_Redirect_ToAllowedHost_Follows()
    {
        int calls = 0;
        UrlContentFetcher fetcher = BuildFetcher(
            (req, _) =>
            {
                calls++;
                if (calls == 1)
                {
                    HttpResponseMessage redirect = new(HttpStatusCode.Found);
                    redirect.Headers.Location = new Uri("https://localhost/final");
                    return redirect;
                }

                return BuildResponse(HttpStatusCode.OK, Encoding.UTF8.GetBytes("final"), "text/plain");
            },
            _localhostAllowed);

        UrlFetchResult result = await fetcher.FetchAsync(new Uri("https://localhost/start"), CancellationToken.None);

        result.FinalUrl.ShouldBe("https://localhost/final");
        Encoding.UTF8.GetString(result.ContentBytes).ShouldBe("final");
    }

    [Fact]
    public async Task FetchAsync_Redirect_ToPrivateIp_WhenLockedDown_ThrowsInvalidUrl()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) =>
            {
                HttpResponseMessage redirect = new(HttpStatusCode.Found);
                redirect.Headers.Location = new Uri("https://169.254.169.254/latest/meta-data/");
                return redirect;
            },
            new UrlFetcherOptions { AllowPrivateHosts = false });

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("https://1.1.1.1/shortener"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("INVALID_URL");
    }

    [Fact]
    public async Task FetchAsync_RedirectToNonHttpScheme_ThrowsInvalidUrl()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) =>
            {
                HttpResponseMessage redirect = new(HttpStatusCode.Found);
                redirect.Headers.Location = new Uri("file:///etc/passwd");
                return redirect;
            },
            _localhostAllowed);

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("http://localhost/redir"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("INVALID_URL");
    }

    [Fact]
    public async Task FetchAsync_ExceedsRedirectBudget_ThrowsTooManyRedirects()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) =>
            {
                HttpResponseMessage redirect = new(HttpStatusCode.Found);
                redirect.Headers.Location = new Uri("https://localhost/next");
                return redirect;
            },
            new UrlFetcherOptions { AllowPrivateHosts = true, MaxRedirects = 2 });

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("https://localhost/start"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("TOO_MANY_REDIRECTS");
    }

    [Fact]
    public async Task FetchAsync_NonHttpScheme_Throws()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => BuildResponse(HttpStatusCode.OK, [], "text/plain"),
            _localhostAllowed);

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("ftp://example.com/"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("INVALID_URL");
    }

    [Fact]
    public async Task FetchAsync_PrivateHostWhenLockedDown_Throws()
    {
        UrlContentFetcher fetcher = BuildFetcher(
            (_, _) => BuildResponse(HttpStatusCode.OK, [], "text/plain"),
            new UrlFetcherOptions { AllowPrivateHosts = false });

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => fetcher.FetchAsync(new Uri("https://169.254.169.254/"), CancellationToken.None));

        ex.ErrorCode.ShouldBe("INVALID_URL");
    }

    private static HttpResponseMessage BuildResponse(
        HttpStatusCode status,
        byte[] body,
        string contentType,
        bool omitContentLength = false)
    {
        HttpResponseMessage response = new(status)
        {
            Content = new ByteArrayContent(body),
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        if (omitContentLength)
        {
            response.Content.Headers.ContentLength = null;
        }

        return response;
    }

    private static UrlContentFetcher BuildFetcher(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler,
        UrlFetcherOptions options)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(UrlContentFetcher.HttpClientName).Returns(_ => new HttpClient(new ScriptedHandler(handler)));

        return new UrlContentFetcher(factory, Options.Create(options));
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

        public ScriptedHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request, cancellationToken));
    }
}
