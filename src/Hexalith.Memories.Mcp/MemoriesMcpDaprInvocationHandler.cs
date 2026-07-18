// <copyright file="MemoriesMcpDaprInvocationHandler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

/// <summary>
/// Story 10.1 — small helper that adds the <c>dapr-api-token</c> header to the invoke
/// <see cref="HttpClient"/> when DAPR API token mode is enabled. <see cref="HttpClient"/>'s default
/// OTel instrumentation already adds <c>traceparent</c>, and
/// <c>DaprClient.CreateInvokeHttpClient</c> already injects <c>dapr-app-id</c>; the only
/// header left for this layer is the API token, which is startup-fixed in 10.1.
/// </summary>
/// <remarks>
/// Task 3.7 evaluated whether this needed to be a full <see cref="DelegatingHandler"/>. Conclusion:
/// no — token mode is startup-fixed (Story 5.4 AC3 ResolveDaprApiTokens / DAPR_API_TOKEN_MODE), so
/// a static <see cref="HttpRequestHeaders.Add(string, string)"/> at DI time on the singleton invoke
/// client is sufficient and keeps the pipeline simple. If a future story needs hot-reload of the
/// token, this becomes a delegating handler again.
/// </remarks>
internal sealed class MemoriesMcpDaprInvocationHandler
{
    /// <summary>Environment variable that gates token propagation. Mirrors AppHost ResolveDaprApiTokens.</summary>
    internal const string TokenModeEnvVar = "DAPR_API_TOKEN_MODE";

    /// <summary>Environment variable that carries the actual DAPR API token.</summary>
    internal const string TokenEnvVar = "DAPR_API_TOKEN";

    /// <summary>The HTTP header the DAPR sidecar inspects on every inbound app-to-sidecar request.</summary>
    internal const string DaprApiTokenHeader = "dapr-api-token";

    /// <summary>
    /// Adds the <c>dapr-api-token</c> default request header to <paramref name="client"/> when the
    /// process environment has <c>DAPR_API_TOKEN_MODE=enabled</c> and a non-empty
    /// <c>DAPR_API_TOKEN</c>. No-op otherwise.
    /// </summary>
    /// <param name="client">The HTTP client to mutate.</param>
    public static void ApplyDaprApiToken(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        // spec-infrastructure-dependency-abstraction (F7, Decision D30): DAPR_API_TOKEN_MODE /
        // DAPR_API_TOKEN are the sanctioned Dapr-platform token contract — the Dapr runtime and
        // AppHost/K8s own and inject these env vars, so reading them directly here is a documented D30
        // exception (a Dapr-platform env contract), NOT a product-code infrastructure leak. Do not
        // re-flag or route through IConfiguration.
        string? mode = Environment.GetEnvironmentVariable(TokenModeEnvVar);
        if (!string.Equals(mode, "enabled", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string? token = Environment.GetEnvironmentVariable(TokenEnvVar);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        if (client.DefaultRequestHeaders.Contains(DaprApiTokenHeader))
        {
            client.DefaultRequestHeaders.Remove(DaprApiTokenHeader);
        }

        client.DefaultRequestHeaders.Add(DaprApiTokenHeader, token);
    }
}
