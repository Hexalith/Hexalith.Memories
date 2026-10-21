// <copyright file="AccessTelemetryDaprHttpClientFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Dapr.Client;

/// <summary>Creates native Dapr invocation clients with the process-scoped API token when enabled.</summary>
internal static class AccessTelemetryDaprHttpClientFactory
{
    /// <summary>Creates an invoke client for one fixed application identity.</summary>
    public static HttpClient Create(string appId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        string? token = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        return DaprClient.CreateInvokeHttpClient(
            appId,
            daprApiToken: string.IsNullOrWhiteSpace(token) ? null : token);
    }
}
