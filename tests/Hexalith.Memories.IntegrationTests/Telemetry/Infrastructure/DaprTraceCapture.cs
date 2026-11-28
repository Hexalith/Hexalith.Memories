// <copyright file="DaprTraceCapture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System.Diagnostics;
using System.Net;
using System.Text.Json;

using Hexalith.Memories.IntegrationTests.Fixtures;

using Microsoft.AspNetCore.Http;

/// <summary>Receives Dapr's Zipkin exports on loopback and retains only parent links for the selected trace.</summary>
internal sealed class DaprTraceCapture : IAsyncDisposable
{
    private const string ExportPath = "/api/v2/spans";
    private const int MaxCapturedSpans = 128;
    private readonly Dictionary<string, string> _parents = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private ScriptedHttpServer _server = null!;
    private string? _traceId;

    /// <summary>Gets the listener address for the fixture-owned Dapr sidecars.</summary>
    internal Uri ExportEndpoint => _server.GetUri(ExportPath);

    /// <summary>Starts the receiver before the sidecars read their tracing configuration.</summary>
    /// <returns>The receiver owned by the fixture.</returns>
    internal static async Task<DaprTraceCapture> StartAsync()
    {
        var capture = new DaprTraceCapture();
        capture._server = await ScriptedHttpServer.StartAsync(capture.ReceiveAsync, enableLogging: false).ConfigureAwait(false);
        return capture;
    }

    /// <summary>Resets the capture and selects which trace to retain.</summary>
    /// <param name="traceId">The W3C trace ID of the request under test, or <see langword="null"/> to stop recording.</param>
    internal void SelectTrace(string? traceId)
    {
        lock (_gate)
        {
            _parents.Clear();
            _traceId = traceId;
        }
    }

    /// <summary>Waits for each Dapr edge from the Server's parent to the MCP HTTP span.</summary>
    /// <param name="firstSpanId">The parent ID read from the Server's incoming HTTP span.</param>
    /// <param name="ancestorSpanId">The outbound HTTP span ID read from the MCP process.</param>
    /// <param name="timeout">The maximum wait for batched Dapr exports.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The recorded Dapr span IDs, starting closest to the Server.</returns>
    internal async Task<IReadOnlyList<string>> ReadParentChainAsync(
        string? firstSpanId,
        string ancestorSpanId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        do
        {
            lock (_gate)
            {
                List<string> chain = [];
                string? current = firstSpanId;
                while (current is not null && _parents.TryGetValue(current, out string? parent) && !chain.Contains(current))
                {
                    chain.Add(current);
                    if (parent == ancestorSpanId)
                    {
                        return chain;
                    }

                    current = parent;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
        }
        while (stopwatch.Elapsed < timeout);

        lock (_gate)
        {
            string capturedLinks = string.Join(", ", _parents.Select(pair => $"{pair.Key} parent {pair.Value}"));
            throw new InvalidOperationException(
                $"Dapr trace {_traceId} did not connect Server parent {firstSpanId} to MCP HTTP span {ancestorSpanId}. "
                + $"Captured links: {capturedLinks}.");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _server.DisposeAsync();

    private async ValueTask<ScriptedHttpResponse> ReceiveAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!HttpMethods.IsPost(request.Method) || request.Path != ExportPath)
        {
            return ScriptedHttpResponse.Text(string.Empty, HttpStatusCode.NotFound);
        }

        string? traceId;
        lock (_gate)
        {
            traceId = _traceId;
        }

        if (traceId is null)
        {
            return ScriptedHttpResponse.Text(string.Empty, HttpStatusCode.Accepted);
        }

        try
        {
            using JsonDocument document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return ScriptedHttpResponse.Text(string.Empty, HttpStatusCode.BadRequest);
            }

            lock (_gate)
            {
                if (_traceId == traceId)
                {
                    foreach (JsonElement span in document.RootElement.EnumerateArray())
                    {
                        if (_parents.Count < MaxCapturedSpans
                            && span.ValueKind == JsonValueKind.Object
                            && span.TryGetProperty("traceId", out JsonElement exportedTraceId)
                            && exportedTraceId.ValueKind == JsonValueKind.String
                            && exportedTraceId.ValueEquals(traceId)
                            && TryReadSpanId(span, "id", out string spanId)
                            && TryReadSpanId(span, "parentId", out string parentId))
                        {
                            _parents.TryAdd(spanId, parentId);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            return ScriptedHttpResponse.Text(string.Empty, HttpStatusCode.BadRequest);
        }

        return ScriptedHttpResponse.Text(string.Empty, HttpStatusCode.Accepted);
    }

    private static bool TryReadSpanId(JsonElement span, string property, out string spanId)
    {
        spanId = span.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
        return spanId.Length == 16 && spanId.All(Uri.IsHexDigit);
    }
}
