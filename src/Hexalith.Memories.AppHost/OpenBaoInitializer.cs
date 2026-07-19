// <copyright file="OpenBaoInitializer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>Initializes one AppHost-owned disposable OpenBao generation.</summary>
internal sealed class OpenBaoInitializer
{
    private const string AccessPolicyName = "memories-access-telemetry-read";
    private const string AccessPrefix = "hexalith/memories/access-telemetry";
    private const string ClockSecretName = "access-telemetry-clock-key";
    private const string MarkerSecretName = "access-telemetry-marker-key";
    private const string RuntimePolicyName = "memories-runtime-read";
    private const string RuntimePrefix = "hexalith/memories/runtime";
    private const string TokenLifetime = "168h";
    private const long TokenLifetimeSeconds = 604800;

    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of the <see cref="OpenBaoInitializer"/> class.</summary>
    /// <param name="httpClient">The client used only for the loopback development endpoint.</param>
    internal OpenBaoInitializer(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    /// <summary>Initializes, seeds, isolates, and verifies a disposable OpenBao generation.</summary>
    /// <param name="endpoint">The allocated loopback endpoint.</param>
    /// <param name="seeds">The protected seed inputs.</param>
    /// <param name="cancellationToken">A token that cancels initialization.</param>
    /// <returns>The two scoped runtime identities.</returns>
    internal async Task<OpenBaoInitializationResult> InitializeAsync(
        Uri endpoint,
        OpenBaoSeedInputs seeds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(seeds);
        if (!endpoint.IsLoopback || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal))
        {
            throw new ArgumentException("The development OpenBao endpoint must use loopback HTTP.", nameof(endpoint));
        }

        using JsonDocument initializationStatus = await SendForJsonAsync(
            endpoint,
            HttpMethod.Get,
            "/v1/sys/init",
            token: null,
            body: null,
            "read initialization status",
            cancellationToken).ConfigureAwait(false);

        if (initializationStatus.RootElement.GetProperty("initialized").GetBoolean())
        {
            throw new InvalidOperationException(
                "The disposable OpenBao generation was already initialized outside its AppHost readiness gate.");
        }

        using JsonDocument bootstrap = await SendForJsonAsync(
            endpoint,
            HttpMethod.Post,
            "/v1/sys/init",
            token: null,
            new { secret_shares = 1, secret_threshold = 1 },
            "initialize OpenBao",
            cancellationToken).ConfigureAwait(false);

        string unsealKey = GetRequiredString(bootstrap.RootElement.GetProperty("keys_base64")[0], "unseal key");
        string rootToken = GetRequiredString(bootstrap.RootElement.GetProperty("root_token"), "bootstrap token");

        await SendAsync(
            endpoint,
            HttpMethod.Post,
            "/v1/sys/unseal",
            token: null,
            new { key = unsealKey },
            "unseal OpenBao",
            cancellationToken).ConfigureAwait(false);

        await EnsureKvV2MountAsync(endpoint, rootToken, cancellationToken).ConfigureAwait(false);
        await PutPolicyAsync(endpoint, rootToken, RuntimePolicyName, RuntimePrefix, cancellationToken).ConfigureAwait(false);
        await PutPolicyAsync(endpoint, rootToken, AccessPolicyName, AccessPrefix, cancellationToken).ConfigureAwait(false);

        foreach ((string name, string value) in seeds.RuntimeSecrets)
        {
            await PutSecretAsync(endpoint, rootToken, RuntimePrefix, name, name, value, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach ((string name, string value) in seeds.AccessTelemetrySecrets)
        {
            string field = name switch
            {
                MarkerSecretName => MarkerSecretName,
                ClockSecretName => "signing-key-pkcs8",
                _ => throw new InvalidOperationException("An unexpected access-telemetry seed name reached initialization."),
            };
            await PutSecretAsync(endpoint, rootToken, AccessPrefix, name, field, value, cancellationToken)
                .ConfigureAwait(false);
        }

        string runtimeToken = await CreateScopedTokenAsync(
            endpoint,
            rootToken,
            RuntimePolicyName,
            cancellationToken).ConfigureAwait(false);
        string accessToken = await CreateScopedTokenAsync(
            endpoint,
            rootToken,
            AccessPolicyName,
            cancellationToken).ConfigureAwait(false);

        await VerifyScopedTokenAsync(endpoint, rootToken, runtimeToken, RuntimePolicyName, cancellationToken)
            .ConfigureAwait(false);
        await VerifyScopedTokenAsync(endpoint, rootToken, accessToken, AccessPolicyName, cancellationToken)
            .ConfigureAwait(false);

        await SendAsync(
            endpoint,
            HttpMethod.Post,
            "/v1/auth/token/revoke-self",
            rootToken,
            body: null,
            "revoke bootstrap token",
            cancellationToken).ConfigureAwait(false);
        await VerifyBootstrapTokenRevokedAsync(endpoint, rootToken, cancellationToken).ConfigureAwait(false);
        await VerifyHealthAsync(endpoint, cancellationToken).ConfigureAwait(false);

        return new OpenBaoInitializationResult(
            runtimeToken,
            accessToken,
            Fingerprint(rootToken),
            Fingerprint(unsealKey));
    }

    private async Task EnsureKvV2MountAsync(Uri endpoint, string rootToken, CancellationToken cancellationToken)
    {
        using JsonDocument mounts = await SendForJsonAsync(
            endpoint,
            HttpMethod.Get,
            "/v1/sys/mounts",
            rootToken,
            body: null,
            "read secret-engine mounts",
            cancellationToken).ConfigureAwait(false);

        JsonElement data = mounts.RootElement.TryGetProperty("data", out JsonElement nestedData)
            ? nestedData
            : mounts.RootElement;
        if (data.TryGetProperty("secret/", out JsonElement secretMount))
        {
            bool isKv = secretMount.TryGetProperty("type", out JsonElement type) && type.GetString() == "kv";
            bool isVersionTwo = secretMount.TryGetProperty("options", out JsonElement options) &&
                options.TryGetProperty("version", out JsonElement version) &&
                version.GetString() == "2";
            if (!isKv || !isVersionTwo)
            {
                throw new InvalidOperationException("The existing secret mount is not KV v2.");
            }

            return;
        }

        await SendAsync(
            endpoint,
            HttpMethod.Post,
            "/v1/sys/mounts/secret",
            rootToken,
            new { type = "kv", options = new { version = "2" } },
            "enable the KV v2 secret engine",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task PutPolicyAsync(
        Uri endpoint,
        string rootToken,
        string policyName,
        string prefix,
        CancellationToken cancellationToken)
    {
        string policy = $"path \"secret/data/{prefix}/*\" {{\n  capabilities = [\"read\"]\n}}";
        await SendAsync(
            endpoint,
            HttpMethod.Put,
            $"/v1/sys/policies/acl/{policyName}",
            rootToken,
            new { policy },
            $"install the {policyName} policy",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task PutSecretAsync(
        Uri endpoint,
        string rootToken,
        string prefix,
        string secretName,
        string fieldName,
        string value,
        CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal) { [fieldName] = value };
        await SendAsync(
            endpoint,
            HttpMethod.Post,
            $"/v1/secret/data/{prefix}/{secretName}",
            rootToken,
            new { data = fields },
            $"seed the validated {prefix} secret",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> CreateScopedTokenAsync(
        Uri endpoint,
        string rootToken,
        string policyName,
        CancellationToken cancellationToken)
    {
        using JsonDocument response = await SendForJsonAsync(
            endpoint,
            HttpMethod.Post,
            "/v1/auth/token/create-orphan",
            rootToken,
            new
            {
                policies = new[] { policyName },
                no_default_policy = true,
                renewable = false,
                ttl = TokenLifetime,
                explicit_max_ttl = TokenLifetime,
                type = "service",
            },
            $"create the {policyName} identity",
            cancellationToken).ConfigureAwait(false);
        return GetRequiredString(response.RootElement.GetProperty("auth").GetProperty("client_token"), "scoped token");
    }

    private async Task VerifyScopedTokenAsync(
        Uri endpoint,
        string rootToken,
        string scopedToken,
        string policyName,
        CancellationToken cancellationToken)
    {
        using JsonDocument response = await SendForJsonAsync(
            endpoint,
            HttpMethod.Post,
            "/v1/auth/token/lookup",
            rootToken,
            new { token = scopedToken },
            $"verify the {policyName} identity",
            cancellationToken).ConfigureAwait(false);
        JsonElement data = response.RootElement.GetProperty("data");
        bool orphan = data.GetProperty("orphan").GetBoolean();
        bool renewable = data.GetProperty("renewable").GetBoolean();
        long remainingLifetimeSeconds = data.GetProperty("ttl").GetInt64();
        long explicitMaximumLifetimeSeconds = data.GetProperty("explicit_max_ttl").GetInt64();
        string? tokenType = data.GetProperty("type").GetString();
        string[] policies = data.GetProperty("policies").EnumerateArray()
            .Select(policy => policy.GetString())
            .OfType<string>()
            .ToArray();
        if (!orphan ||
            renewable ||
            remainingLifetimeSeconds <= OpenBaoSessionLifetimeGuard.MaximumSession.TotalSeconds ||
            remainingLifetimeSeconds > TokenLifetimeSeconds ||
            explicitMaximumLifetimeSeconds != TokenLifetimeSeconds ||
            !string.Equals(tokenType, "service", StringComparison.Ordinal) ||
            policies.Length != 1 ||
            !string.Equals(policies[0], policyName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The {policyName} identity did not satisfy the isolation contract.");
        }
    }

    private async Task VerifyBootstrapTokenRevokedAsync(
        Uri endpoint,
        string rootToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(endpoint, HttpMethod.Get, "/v1/auth/token/lookup-self", rootToken, body: null);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                $"The revoked bootstrap token probe returned HTTP {(int)response.StatusCode} instead of denial.");
        }
    }

    private async Task VerifyHealthAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        using JsonDocument response = await SendForJsonAsync(
            endpoint,
            HttpMethod.Get,
            "/v1/sys/health",
            token: null,
            body: null,
            "verify initialized OpenBao health",
            cancellationToken).ConfigureAwait(false);
        if (!response.RootElement.GetProperty("initialized").GetBoolean() ||
            response.RootElement.GetProperty("sealed").GetBoolean())
        {
            throw new InvalidOperationException("OpenBao did not become initialized and unsealed.");
        }
    }

    private async Task<JsonDocument> SendForJsonAsync(
        Uri endpoint,
        HttpMethod method,
        string path,
        string? token,
        object? body,
        string operation,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendCoreAsync(
            endpoint,
            method,
            path,
            token,
            body,
            operation,
            cancellationToken).ConfigureAwait(false);
        try
        {
            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"OpenBao could not {operation}: the response was not valid JSON.", exception);
        }
    }

    private async Task SendAsync(
        Uri endpoint,
        HttpMethod method,
        string path,
        string? token,
        object? body,
        string operation,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendCoreAsync(
            endpoint,
            method,
            path,
            token,
            body,
            operation,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        Uri endpoint,
        HttpMethod method,
        string path,
        string? token,
        object? body,
        string operation,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(endpoint, method, path, token, body);
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            HttpStatusCode statusCode = response.StatusCode;
            response.Dispose();
            throw new InvalidOperationException($"OpenBao could not {operation}: HTTP {(int)statusCode}.");
        }

        return response;
    }

    private static HttpRequestMessage CreateRequest(
        Uri endpoint,
        HttpMethod method,
        string path,
        string? token,
        object? body)
    {
        var request = new HttpRequestMessage(method, new Uri(endpoint, path));
        if (token is not null)
        {
            request.Headers.Add("X-Vault-Token", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static string GetRequiredString(JsonElement element, string fieldDescription)
    {
        string? value = element.GetString();
        return !string.IsNullOrEmpty(value)
            ? value
            : throw new InvalidOperationException($"OpenBao omitted the required {fieldDescription}.");
    }

    private static string Fingerprint(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
