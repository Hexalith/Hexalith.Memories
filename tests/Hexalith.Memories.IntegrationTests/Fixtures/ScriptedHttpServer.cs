// <copyright file="ScriptedHttpServer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Minimal local HTTP server used by integration tests to script delayed and failing URL responses.</summary>
public sealed class ScriptedHttpServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly RequestCounter _counter;
    private readonly CancellationTokenSource _shutdownCts;

    private ScriptedHttpServer(
        WebApplication app,
        Uri baseAddress,
        RequestCounter counter,
        CancellationTokenSource shutdownCts)
    {
        _app = app;
        _counter = counter;
        _shutdownCts = shutdownCts;
        BaseAddress = baseAddress;
    }

    /// <summary>Gets the server base address.</summary>
    public Uri BaseAddress { get; }

    /// <summary>Gets the number of requests observed since startup.</summary>
    public int RequestCount => _counter.Read();

    /// <summary>Starts the scripted server on a random loopback port.</summary>
    /// <param name="handler">Request handler that produces the scripted response.</param>
    /// <returns>The running server instance.</returns>
    public static async Task<ScriptedHttpServer> StartAsync(
        Func<HttpRequest, CancellationToken, ValueTask<ScriptedHttpResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions());
        _ = builder.WebHost.ConfigureKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.Protocols = HttpProtocols.Http1));

        WebApplication app = builder.Build();
        RequestCounter counter = new();
        CancellationTokenSource shutdownCts = new();

        _ = app.Map("/{**path}", async context =>
        {
            counter.Increment();

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                context.RequestAborted,
                shutdownCts.Token);

            ScriptedHttpResponse response = await handler(context.Request, linkedCts.Token).ConfigureAwait(false);
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.ContentType;

            foreach (KeyValuePair<string, string> header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value;
            }

            byte[] body = response.Body;
            context.Response.ContentLength = body.Length;
            if (body.Length > 0)
            {
                await context.Response.Body.WriteAsync(body, linkedCts.Token).ConfigureAwait(false);
            }
        });

        await app.StartAsync().ConfigureAwait(false);

        IServerAddressesFeature? addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        string? address = addresses?.Addresses
            .FirstOrDefault(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? addresses?.Addresses.FirstOrDefault();

        if (string.IsNullOrEmpty(address))
        {
            throw new InvalidOperationException("Kestrel did not expose a bound address for the scripted HTTP server.");
        }

        string prefix = address.EndsWith('/') ? address : address + "/";

        return new ScriptedHttpServer(app, new Uri(prefix), counter, shutdownCts);
    }

    /// <summary>Builds an absolute URI under the server base address.</summary>
    /// <param name="relativePath">Relative path, with or without a leading slash.</param>
    /// <returns>The absolute URI.</returns>
    public Uri GetUri(string relativePath)
    {
        string normalized = relativePath.StartsWith('/')
            ? relativePath[1..]
            : relativePath;
        return new Uri(BaseAddress, normalized);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _shutdownCts.CancelAsync().ConfigureAwait(false);

        using CancellationTokenSource stopCts = new(TimeSpan.FromSeconds(5));
        try
        {
            await _app.StopAsync(stopCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        await _app.DisposeAsync().ConfigureAwait(false);
        _shutdownCts.Dispose();
    }

    private sealed class RequestCounter
    {
        private int _value;

        public void Increment() => Interlocked.Increment(ref _value);

        public int Read() => Volatile.Read(ref _value);
    }
}

/// <summary>Serialized response emitted by <see cref="ScriptedHttpServer"/>.</summary>
/// <param name="StatusCode">HTTP status code.</param>
/// <param name="Body">UTF-8 response body bytes.</param>
/// <param name="ContentType">Content type header.</param>
/// <param name="Headers">Additional response headers.</param>
public sealed record ScriptedHttpResponse(
    HttpStatusCode StatusCode,
    byte[] Body,
    string ContentType,
    IReadOnlyDictionary<string, string> Headers)
{
    /// <summary>Creates a text response.</summary>
    /// <param name="content">UTF-8 text body.</param>
    /// <param name="statusCode">Status code.</param>
    /// <param name="contentType">Content type header.</param>
    /// <returns>The scripted response.</returns>
    public static ScriptedHttpResponse Text(
        string content,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string contentType = "text/plain; charset=utf-8")
        => new(
            statusCode,
            Encoding.UTF8.GetBytes(content),
            contentType,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>Creates a response with optional headers and text content.</summary>
    /// <param name="statusCode">Status code.</param>
    /// <param name="content">UTF-8 text body.</param>
    /// <param name="contentType">Content type header.</param>
    /// <param name="headers">Additional response headers.</param>
    /// <returns>The scripted response.</returns>
    public static ScriptedHttpResponse Create(
        HttpStatusCode statusCode,
        string content,
        string contentType,
        IReadOnlyDictionary<string, string>? headers = null)
        => new(
            statusCode,
            Encoding.UTF8.GetBytes(content),
            contentType,
            headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
}
