// <copyright file="OllamaOidcFakeServer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;

/// <summary>Loopback Ollama and OIDC fake used by Story 13.7 provider integration tests.</summary>
public sealed class OllamaOidcFakeServer : IAsyncDisposable
{
    /// <summary>The canonical Story 13 Ollama model.</summary>
    public const string DefaultModel = "qwen3-embedding:4b";

    /// <summary>The canonical Story 13 Ollama vector dimensions.</summary>
    public const int OllamaDimensions = 2560;

    /// <summary>The committed default OIDC client id.</summary>
    public const string ClientId = "memories-embedding";

    /// <summary>The committed optional OIDC scope.</summary>
    public const string Scope = "openid";

    /// <summary>The committed default DAPR secret-name reference.</summary>
    public const string SecretName = "memories-embedding-client-secret";

    /// <summary>The deterministic bearer-token literal returned by the fake token endpoint.</summary>
    /// <remarks>Internal so redaction tests can assert the exact value never leaks into logs.</remarks>
    internal const string AccessToken = "example-bearer-token";

    private readonly string _clientSecret;
    private readonly object _gate = new();
    private readonly List<FakeHttpRequestEvidence> _requestEvidence = [];
    private ScriptedHttpServer _server = null!;
    private int _embedRequestCount;
    private int _tokenRequestCount;

    private OllamaOidcFakeServer(string clientSecret) => _clientSecret = clientSecret;

    /// <summary>Gets the fake Ollama gateway base URL.</summary>
    public Uri OllamaBaseUrl => _server.BaseAddress;

    /// <summary>Gets the fake OIDC token endpoint.</summary>
    public Uri OidcTokenEndpoint => _server.GetUri("/realms/example/protocol/openid-connect/token");

    /// <summary>Gets the number of valid embed requests handled by the fake.</summary>
    public int EmbedRequestCount => Volatile.Read(ref _embedRequestCount);

    /// <summary>Gets the number of valid token requests handled by the fake.</summary>
    public int TokenRequestCount => Volatile.Read(ref _tokenRequestCount);

    /// <summary>Gets sanitized request evidence.</summary>
    public IReadOnlyList<FakeHttpRequestEvidence> RequestEvidence
    {
        get
        {
            lock (_gate)
            {
                return [.. _requestEvidence];
            }
        }
    }

    /// <summary>Starts the fake server.</summary>
    /// <param name="clientSecret">Expected OIDC client secret value.</param>
    /// <returns>The running fake.</returns>
    public static async Task<OllamaOidcFakeServer> StartAsync(string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        // Construct the instance first so the dispatcher closure cannot dereference null
        // if ScriptedHttpServer accepts a request before the assignment completes.
        OllamaOidcFakeServer fake = new(clientSecret);
        ScriptedHttpServer server = await ScriptedHttpServer
            .StartAsync(fake.HandleAsync)
            .ConfigureAwait(false);
        fake._server = server;
        return fake;
    }

    /// <summary>Builds a URI under the fake server.</summary>
    /// <param name="relativePath">Relative path.</param>
    /// <returns>The absolute URI.</returns>
    public Uri GetUri(string relativePath) => _server.GetUri(relativePath);

    /// <summary>Gets the canonical embed endpoint URI.</summary>
    /// <returns>The absolute embed endpoint URI.</returns>
    public Uri GetEmbedUri() => _server.GetUri("/api/embed");

    /// <summary>Creates a deterministic vector from model and input text.</summary>
    /// <param name="model">The model name.</param>
    /// <param name="input">The input text.</param>
    /// <param name="dimensions">The vector dimension count.</param>
    /// <returns>The deterministic vector.</returns>
    public static float[] CreateDeterministicVector(string model, string input, int dimensions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensions);

        byte[] seed = SHA256.HashData(Encoding.UTF8.GetBytes(model + "\n" + input));
        float[] vector = new float[dimensions];
        for (int i = 0; i < vector.Length; i++)
        {
            int mixed = seed[i % seed.Length] + (i * 31);
            vector[i] = ((mixed % 2048) / 1024f) - 1f;
        }

        return vector;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _server.DisposeAsync();

    private async ValueTask<ScriptedHttpResponse> HandleAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.Path.Value, "/realms/example/protocol/openid-connect/token", StringComparison.Ordinal))
        {
            return await HandleTokenAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(request.Path.Value, "/api/embed", StringComparison.Ordinal))
        {
            return await HandleEmbedAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return ScriptedHttpResponse.Text(
            "Unsupported fake endpoint. Expected /api/embed or the configured token endpoint.",
            HttpStatusCode.BadRequest);
    }

    private async ValueTask<ScriptedHttpResponse> HandleTokenAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return ScriptedHttpResponse.Text("Token endpoint requires POST.", HttpStatusCode.BadRequest);
        }

        if (!request.HasFormContentType)
        {
            return ScriptedHttpResponse.Text("Token endpoint requires form content.", HttpStatusCode.BadRequest);
        }

        IFormCollection form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        if (!TryReadSingle(form, "grant_type", out string? grantType) ||
            !TryReadSingle(form, "client_id", out string? clientId) ||
            !TryReadSingle(form, "client_secret", out string? clientSecret))
        {
            return ScriptedHttpResponse.Text(
                "Token endpoint requires exactly one value each for grant_type, client_id, and client_secret.",
                HttpStatusCode.BadRequest);
        }

        if (!string.Equals(grantType, "client_credentials", StringComparison.Ordinal) ||
            !string.Equals(clientId, ClientId, StringComparison.Ordinal) ||
            !string.Equals(clientSecret, _clientSecret, StringComparison.Ordinal))
        {
            return ScriptedHttpResponse.Text("Malformed client credentials request.", HttpStatusCode.BadRequest);
        }

        // Scope is optional per RFC 6749 §4.4.2; reject only when present and wrong, but require single-value form.
        if (form.ContainsKey("scope"))
        {
            if (!TryReadSingle(form, "scope", out string? scopeValue) ||
                !string.Equals(scopeValue, Scope, StringComparison.Ordinal))
            {
                return ScriptedHttpResponse.Text(
                    $"Token endpoint scope must be exactly '{Scope}' when supplied.",
                    HttpStatusCode.BadRequest);
            }
        }

        AddEvidence(new FakeHttpRequestEvidence(
            request.Method,
            request.Path.Value ?? string.Empty,
            ClientId: clientId,
            HasClientSecret: true));
        _ = Interlocked.Increment(ref _tokenRequestCount);

        return Json(
            $$"""{"access_token":"{{AccessToken}}","expires_in":300,"token_type":"Bearer"}""");
    }

    private async ValueTask<ScriptedHttpResponse> HandleEmbedAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return ScriptedHttpResponse.Text("Ollama embed endpoint requires POST.", HttpStatusCode.BadRequest);
        }

        string authorization = request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return ScriptedHttpResponse.Text("Ollama embed endpoint requires bearer authorization.", HttpStatusCode.Unauthorized);
        }

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ScriptedHttpResponse.Text("Ollama embed request body must be valid JSON.", HttpStatusCode.BadRequest);
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("model", out JsonElement modelElement) ||
                !doc.RootElement.TryGetProperty("input", out JsonElement inputElement) ||
                modelElement.ValueKind != JsonValueKind.String ||
                inputElement.ValueKind != JsonValueKind.String)
            {
                return ScriptedHttpResponse.Text(
                    "Ollama embed request requires string model and input properties.",
                    HttpStatusCode.BadRequest);
            }

            string model = modelElement.GetString()!;
            string input = inputElement.GetString()!;
            if (!string.Equals(model, DefaultModel, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(input))
            {
                return ScriptedHttpResponse.Text("Ollama embed request has invalid model or input.", HttpStatusCode.BadRequest);
            }

            return BuildEmbedResponse(model, input, request.Method, request.Path.Value);
        }
    }

    private ScriptedHttpResponse BuildEmbedResponse(string model, string input, string method, string? path)
    {
        float[] vector = CreateDeterministicVector(model, input, OllamaDimensions);
        string values = string.Join(
            ",",
            vector.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));

        AddEvidence(new FakeHttpRequestEvidence(
            method,
            path ?? string.Empty,
            Model: model,
            HasBearerToken: true));
        _ = Interlocked.Increment(ref _embedRequestCount);

        return Json($$"""{"model":"{{DefaultModel}}","embeddings":[[{{values}}]]}""");
    }

    private static bool TryReadSingle(IFormCollection form, string key, out string? value)
    {
        Microsoft.Extensions.Primitives.StringValues raw = form[key];
        if (raw.Count != 1)
        {
            value = null;
            return false;
        }

        value = raw[0];
        return value is not null;
    }

    private void AddEvidence(FakeHttpRequestEvidence evidence)
    {
        lock (_gate)
        {
            _requestEvidence.Add(evidence);
        }
    }

    private static ScriptedHttpResponse Json(string json)
        => ScriptedHttpResponse.Create(HttpStatusCode.OK, json, "application/json; charset=utf-8");
}
