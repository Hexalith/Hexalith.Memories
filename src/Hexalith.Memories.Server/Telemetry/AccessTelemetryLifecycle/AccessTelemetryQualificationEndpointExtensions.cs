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
                async (AccessTelemetryQualificationWorkloadRunner runner, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        AccessTelemetryQualificationWorkloadResult result = await runner
                            .RunAsync(cancellationToken)
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
