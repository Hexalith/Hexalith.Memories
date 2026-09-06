// <copyright file="AccessTelemetryQualificationEndpointExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Microsoft.AspNetCore.Authorization;

/// <summary>Maps the single no-input qualification workload surface.</summary>
internal static class AccessTelemetryQualificationEndpointExtensions
{
    /// <summary>The fixed route invoked separately inside each reviewed Server pod.</summary>
    public const string Route = "/operations/access-telemetry/qualification/fixed-workload";

    /// <summary>The verifier-owned bounded run-correlation header.</summary>
    public const string RunHeader = "X-Hexalith-Qualification-Run";

    /// <summary>The verifier-owned canonical segment-correlation header.</summary>
    public const string SegmentHeader = "X-Hexalith-Qualification-Segment";

    /// <summary>The verifier-owned retry-stable segment emission timestamp header.</summary>
    public const string EmittedUtcMsHeader = "X-Hexalith-Qualification-Emitted-Utc-Ms";

    /// <summary>Maps the route only when the process is explicitly running as Qualification.</summary>
    /// <param name="app">The Server application.</param>
    /// <returns>The application for chaining.</returns>
    public static WebApplication MapAccessTelemetryQualificationEndpoint(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!app.Environment.IsEnvironment("Qualification"))
        {
            return app;
        }

        _ = app.MapPost(
                Route,
                async (HttpContext context, AccessTelemetryQualificationWorkloadRunner runner, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        string runId = context.Request.Headers[RunHeader].ToString();
                        string segmentId = context.Request.Headers[SegmentHeader].ToString();
                        if (!long.TryParse(
                            context.Request.Headers[EmittedUtcMsHeader].ToString(),
                            System.Globalization.NumberStyles.None,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out long emittedUtcMs))
                        {
                            throw new InvalidOperationException("qualification_segment_timestamp_invalid");
                        }

                        AccessTelemetryQualificationWorkloadResult result = await runner
                            .RunAsync(runId, segmentId, emittedUtcMs, cancellationToken)
                            .ConfigureAwait(false);
                        // HTTP success means the closed observation was returned. The
                        // host-side verifier, which owns the fault scenario, decides
                        // whether its exact accounting is a checkpoint pass or fail.
                        return Results.Ok(result);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Qualification workload is unavailable.",
                            detail: exception.Message);
                    }
                })
            .AllowAnonymous();
        return app;
    }
}
