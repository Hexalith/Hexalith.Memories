// <copyright file="DaprApplicationTokenMiddleware.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.ServiceDefaults.Security;

using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.ServiceDefaults.Health;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

/// <summary>Validates the token DAPR attaches to every sidecar-to-application request.</summary>
public sealed class DaprApplicationTokenMiddleware(RequestDelegate next, IConfiguration configuration)
{
    /// <summary>The environment variable containing the sidecar-to-application token.</summary>
    public const string AppApiTokenEnvironmentVariable = "APP_API_TOKEN";

    /// <summary>
    /// Host configuration key that overrides <see cref="AppApiTokenEnvironmentVariable"/>.
    /// Test hosts set this to empty so process-wide environment mutations cannot 401 anonymous Dapr routes.
    /// Do not reuse <c>APP_API_TOKEN</c> as the key: default host configuration already binds that
    /// environment variable, so an empty in-memory overlay would not win.
    /// </summary>
    public const string AppApiTokenOverrideConfigurationKey = "Hexalith:Dapr:ApplicationTokenOverride";

    /// <summary>The HTTP header used by DAPR for application-token authentication.</summary>
    public const string DaprApiTokenHeader = "dapr-api-token";

    /// <summary>Validates the DAPR application token before invoking the next middleware.</summary>
    /// <param name="context">The current request context.</param>
    /// <returns>A task representing request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        if (IsPodProbe(context.Request.Path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        string? expectedToken = ResolveExpectedAppApiToken();
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        string suppliedToken = context.Request.Headers[DaprApiTokenHeader].ToString();
        if (!TokensMatch(expectedToken, suppliedToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(
                new
                {
                    type = "https://hexalith.io/problems/dapr-application-token",
                    title = "Unauthorized",
                    status = StatusCodes.Status401Unauthorized,
                    detail = "The DAPR application token is missing or invalid.",
                },
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsPodProbe(PathString path)
        => path.StartsWithSegments(HealthEndpointPaths.Health)
        || path.StartsWithSegments(HealthEndpointPaths.Alive)
        || path.StartsWithSegments(HealthEndpointPaths.Ready);

    private static bool TryGetApplicationTokenOverride(IConfiguration hostConfiguration, out string? overlay)
    {
        foreach (KeyValuePair<string, string?> pair in hostConfiguration.AsEnumerable())
        {
            if (string.Equals(pair.Key, AppApiTokenOverrideConfigurationKey, StringComparison.OrdinalIgnoreCase))
            {
                overlay = pair.Value;
                return true;
            }
        }

        overlay = null;
        return false;
    }

    private static bool TokensMatch(string expectedToken, string suppliedToken)
    {
        byte[] expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedToken));
        byte[] suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken));
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
    }

    /// <summary>
    /// Prefers an explicit host override so a test host can disable token checks without racing
    /// process-wide <c>APP_API_TOKEN</c> mutations. Production hosts omit the override and use the
    /// environment variable Dapr already injects.
    /// </summary>
    /// <returns>The override value when configured; otherwise the process environment token.</returns>
    private string? ResolveExpectedAppApiToken()
    {
        if (TryGetApplicationTokenOverride(configuration, out string? overlay))
        {
            return overlay;
        }

        return Environment.GetEnvironmentVariable(AppApiTokenEnvironmentVariable);
    }
}
