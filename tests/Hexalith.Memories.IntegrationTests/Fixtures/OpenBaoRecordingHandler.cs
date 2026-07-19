// <copyright file="OpenBaoRecordingHandler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using System.Text;
using System.Text.Json;

/// <summary>Provides deterministic OpenBao bootstrap responses while recording request contracts.</summary>
internal sealed class OpenBaoRecordingHandler : HttpMessageHandler
{
    private int _tokenCreationCount;

    /// <summary>Gets the captured requests in send order.</summary>
    internal List<OpenBaoCapturedRequest> Requests { get; } = [];

    /// <summary>Gets or sets an optional first-response failure payload.</summary>
    internal string? InitializationFailureBody { get; set; }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string? token = request.Headers.TryGetValues("X-Vault-Token", out IEnumerable<string>? values)
            ? values.SingleOrDefault()
            : null;
        string path = request.RequestUri?.AbsolutePath ?? string.Empty;
        Requests.Add(new OpenBaoCapturedRequest(request.Method, path, token, body));

        if (InitializationFailureBody is not null && path == "/v1/sys/init" && request.Method == HttpMethod.Get)
        {
            return Json(HttpStatusCode.InternalServerError, InitializationFailureBody);
        }

        if (path == "/v1/sys/init" && request.Method == HttpMethod.Get)
        {
            return Json(HttpStatusCode.OK, "{\"initialized\":false}");
        }

        if (path == "/v1/sys/init" && request.Method == HttpMethod.Post)
        {
            return Json(
                HttpStatusCode.OK,
                "{\"keys_base64\":[\"unseal-test-key\"],\"root_token\":\"root-test-token\"}");
        }

        if (path == "/v1/sys/unseal")
        {
            return Json(HttpStatusCode.OK, "{\"sealed\":false}");
        }

        if (path == "/v1/sys/mounts" && request.Method == HttpMethod.Get)
        {
            return Json(HttpStatusCode.OK, "{\"data\":{}}");
        }

        if (path == "/v1/auth/token/create-orphan")
        {
            _tokenCreationCount++;
            string createdToken = _tokenCreationCount == 1 ? "runtime-test-token" : "access-test-token";
            string policy = _tokenCreationCount == 1 ? "memories-runtime-read" : "memories-access-telemetry-read";
            return Json(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    auth = new
                    {
                        client_token = createdToken,
                        orphan = true,
                        policies = new[] { policy },
                        token_policies = new[] { policy },
                        token_type = "service",
                        renewable = false,
                        lease_duration = 604800,
                    },
                }));
        }

        if (path == "/v1/auth/token/lookup" && token == "root-test-token" &&
            body.Contains("runtime-test-token", StringComparison.Ordinal))
        {
            return Json(
                HttpStatusCode.OK,
                "{\"data\":{\"orphan\":true,\"renewable\":false,\"ttl\":604799,\"explicit_max_ttl\":604800," +
                "\"type\":\"service\",\"policies\":[\"memories-runtime-read\"]}}");
        }

        if (path == "/v1/auth/token/lookup" && token == "root-test-token" &&
            body.Contains("access-test-token", StringComparison.Ordinal))
        {
            return Json(
                HttpStatusCode.OK,
                "{\"data\":{\"orphan\":true,\"renewable\":false,\"ttl\":604799,\"explicit_max_ttl\":604800," +
                "\"type\":\"service\",\"policies\":[\"memories-access-telemetry-read\"]}}");
        }

        if (path == "/v1/auth/token/lookup-self" && token == "root-test-token")
        {
            return Json(HttpStatusCode.Forbidden, "{\"errors\":[\"permission denied\"]}");
        }

        if (path == "/v1/sys/health")
        {
            return Json(HttpStatusCode.OK, "{\"initialized\":true,\"sealed\":false}");
        }

        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
}
