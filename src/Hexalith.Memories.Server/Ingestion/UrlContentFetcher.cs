// <copyright file="UrlContentFetcher.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Options;

/// <summary>
/// Default <see cref="IUrlContentFetcher"/> implementation. Handles redirects manually to re-validate
/// each hop's host against the SSRF allow-list; enforces a byte-level size cap; classifies failures
/// into <see cref="UrlFetchException"/> codes.
/// </summary>
public sealed class UrlContentFetcher : IUrlContentFetcher
{
    public const string HttpClientName = "memories-url-fetcher";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UrlFetcherOptions _options;

    public UrlContentFetcher(IHttpClientFactory httpClientFactory, IOptions<UrlFetcherOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);

        _httpClientFactory = httpClientFactory;
        _options = options.Value ?? new UrlFetcherOptions();
    }

    /// <inheritdoc/>
    public async Task<UrlFetchResult> FetchAsync(Uri url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (url.Scheme is not "http" and not "https")
        {
            throw new UrlFetchException("INVALID_URL", "URL scheme must be http or https.");
        }

        if (!UrlHostValidator.IsAllowedHost(url, _options))
        {
            throw new UrlFetchException("INVALID_URL", "URL host is not permitted.");
        }

        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Hexalith.Memories", "1.0"));
        }

        Uri current = url;
        for (int hop = 0; hop <= _options.MaxRedirects; hop++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, current);
            HttpResponseMessage response;
            try
            {
                response = await client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new UrlFetchException("URL_TIMEOUT", $"URL fetch timed out after {_options.TimeoutSeconds}s.");
            }
            catch (HttpRequestException ex) when (IsConnectionRelated(ex))
            {
                throw new UrlFetchException("URL_NETWORK_ERROR", $"Network failure fetching URL: {ex.Message}", ex);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null)
            {
                throw new UrlFetchException("URL_NETWORK_ERROR", $"Network failure fetching URL: {ex.Message}", ex);
            }

            try
            {
                int status = (int)response.StatusCode;

                if (IsRedirect(response.StatusCode))
                {
                    if (hop >= _options.MaxRedirects)
                    {
                        throw new UrlFetchException("TOO_MANY_REDIRECTS", $"Exceeded {_options.MaxRedirects} redirects.");
                    }

                    Uri? location = response.Headers.Location;
                    if (location is null)
                    {
                        throw new UrlFetchException("URL_NETWORK_ERROR", "Redirect response had no Location header.");
                    }

                    Uri next = location.IsAbsoluteUri ? location : new Uri(current, location);
                    if (next.Scheme is not "http" and not "https")
                    {
                        throw new UrlFetchException("INVALID_URL", "Redirect target scheme must be http(s).");
                    }

                    if (!UrlHostValidator.IsAllowedHost(next, _options))
                    {
                        throw new UrlFetchException("INVALID_URL", "Redirect target host is not permitted.");
                    }

                    current = next;
                    continue;
                }

                if (status >= 500)
                {
                    throw new UrlFetchException("URL_SERVER_ERROR", $"Remote host returned HTTP {status}.", httpStatusCode: status);
                }

                if (status >= 400)
                {
                    throw new UrlFetchException("URL_CLIENT_ERROR", $"Remote host returned HTTP {status}.", httpStatusCode: status);
                }

                long? declared = response.Content.Headers.ContentLength;
                if (declared.HasValue && declared.Value > _options.MaxBytes)
                {
                    throw new UrlFetchException(
                        "PAYLOAD_TOO_LARGE",
                        $"URL body Content-Length {declared.Value} exceeds {_options.MaxBytes} bytes.",
                        httpStatusCode: status);
                }

                string contentType = response.Content.Headers.ContentType?.MediaType
                    ?? "application/octet-stream";

                if (!IngestionContentTypeSupport.IsSupported(contentType))
                {
                    throw new UrlFetchException(
                        "UNSUPPORTED_CONTENT_TYPE",
                        $"Response Content-Type '{contentType}' is not supported.",
                        httpStatusCode: status);
                }

                byte[] body = await ReadBoundedAsync(response, _options.MaxBytes, linked.Token).ConfigureAwait(false);

                return new UrlFetchResult(body, contentType, body.LongLength, current.ToString(), status);
            }
            finally
            {
                response.Dispose();
            }
        }

        // Loop exited without returning — treat as redirect budget exhaustion.
        throw new UrlFetchException("TOO_MANY_REDIRECTS", $"Exceeded {_options.MaxRedirects} redirects.");
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, long maxBytes, CancellationToken cancellationToken)
    {
        using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using MemoryStream sink = new();

        byte[] buffer = new byte[8 * 1024];
        long remaining = maxBytes;

        while (remaining >= 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining + 1);
            if (toRead <= 0)
            {
                break;
            }

            int read;
            try
            {
                read = await source.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                throw new UrlFetchException("URL_NETWORK_ERROR", $"Failed reading response body: {ex.Message}", ex);
            }

            if (read == 0)
            {
                return sink.ToArray();
            }

            if (sink.Length + read > maxBytes)
            {
                throw new UrlFetchException(
                    "PAYLOAD_TOO_LARGE",
                    $"URL body stream exceeded {maxBytes} bytes.");
            }

            await sink.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            remaining -= read;
        }

        return sink.ToArray();
    }

    private static bool IsRedirect(HttpStatusCode code)
        => code is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static bool IsConnectionRelated(HttpRequestException ex)
        => ex.InnerException is SocketException;
}
