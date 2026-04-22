// <copyright file="CloudEventEnvelopeCaptureMiddleware.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json;

using Microsoft.AspNetCore.Http;

/// <summary>Captures the original structured CloudEvents JSON body before Dapr's CloudEvents middleware
/// potentially unwraps it. The controller can then fall back to the preserved envelope if downstream
/// middleware rewrites the request body to only the inner <c>data</c> payload.</summary>
public sealed class CloudEventEnvelopeCaptureMiddleware
{
    /// <summary>HttpContext.Items key containing the cloned structured CloudEvents envelope.</summary>
    public const string CapturedEnvelopeItemKey = "Hexalith.Memories.EventStore.CapturedCloudEventEnvelope";

    private readonly RequestDelegate _next;

    public CloudEventEnvelopeCaptureMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ShouldCapture(context.Request))
        {
            context.Request.EnableBuffering();

            using JsonDocument document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted)
                .ConfigureAwait(false);
            context.Items[CapturedEnvelopeItemKey] = document.RootElement.Clone();
            context.Request.Body.Position = 0;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool ShouldCapture(HttpRequest request)
        => HttpMethods.IsPost(request.Method)
        && request.Path.Equals("/events/ingest", StringComparison.OrdinalIgnoreCase)
        && request.ContentType?.StartsWith("application/cloudevents+json", StringComparison.OrdinalIgnoreCase) == true;
}
