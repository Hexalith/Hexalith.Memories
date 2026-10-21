// <copyright file="AccessTelemetryDaprHttpClientFactoryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLifecycle;

using System.Net;
using System.Reflection;

using Dapr.Client;

using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Shouldly;

/// <summary>Verifies credentials at the actual pinned SDK invocation handler's transport boundary.</summary>
[Collection("DaprTokenEnvironment")]
public sealed class AccessTelemetryDaprHttpClientFactoryTests
{
    [Theory]
    [InlineData(null, "scoped-offline-token")]
    [InlineData("enabled", "scoped-offline-token")]
    [InlineData("disabled", "scoped-offline-token")]
    [InlineData(null, null)]
    public async Task Factory_PassesOnlyTheScopedTokenThroughTheSdkHandler(string? mode, string? token)
    {
        string? previousMode = Environment.GetEnvironmentVariable("DAPR_API_TOKEN_MODE");
        string? previousToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        string? previousEndpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN_MODE", mode);
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN", token);
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", "http://127.0.0.1:3507");
            using HttpClient client = AccessTelemetryDaprHttpClientFactory.Create("memories-access-telemetry-clock");
            // Replace only the network transport. Keep the real SDK handler and its credential/URI rewriting.
            FieldInfo field = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)
                .ShouldNotBeNull();
            var invocation = field.GetValue(client).ShouldBeOfType<InvocationHandler>();
            invocation.InnerHandler!.Dispose();
            var transport = new RecordingTransport();
            invocation.InnerHandler = transport;

            using HttpResponseMessage response = await client.PostAsync("v1/time/attest", null);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            transport.RequestUri.ShouldBe("http://127.0.0.1:3507/v1.0/invoke/memories-access-telemetry-clock/method/v1/time/attest");
            transport.Tokens.ShouldBe(token is null ? [] : new[] { token });
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN_MODE", previousMode);
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN", previousToken);
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", previousEndpoint);
        }
    }

    private sealed class RecordingTransport : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }

        public string[] Tokens { get; private set; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri!.AbsoluteUri;
            Tokens = request.Headers.TryGetValues("dapr-api-token", out IEnumerable<string>? values) ? values.ToArray() : [];
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
