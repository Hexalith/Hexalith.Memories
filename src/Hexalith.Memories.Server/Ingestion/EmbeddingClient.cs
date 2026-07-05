// <copyright file="EmbeddingClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>Singleton facade for generating embeddings via configurable provider APIs. Provider-specific request, auth,
/// and response knowledge lives behind <see cref="IEmbeddingProvider"/> strategies; shared HTTP transport, auth-retry,
/// rate-limit, and redaction behavior lives in <see cref="EmbeddingProviderTransport"/> (Story 23.9).</summary>
public class EmbeddingClient
{
    /// <summary>Minimum length below which a value is not redacted, to avoid masking benign short substrings (e.g.,
    /// common words) that happen to overlap with a sensitive value.</summary>
    internal const int RedactionMinLength = EmbeddingResponseSanitizer.RedactionMinLength;

    private const string FakeEmbeddingConfigKey = "Memories:Testing:UseFakeEmbedding";

    private readonly EmbeddingProviderRegistry _providerRegistry;
    private readonly EmbeddingProviderTransport _transport;
    private readonly bool _useFakeEmbedding;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingClient"/> class.</summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="daprClient">The DAPR client for secret retrieval.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="hostEnvironment">The current host environment.</param>
    public EmbeddingClient(
        IHttpClientFactory httpClientFactory,
        DaprClient daprClient,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
        : this(httpClientFactory, daprClient, configuration, hostEnvironment, oidcTokenProvider: null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingClient"/> class.</summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="daprClient">The DAPR client for secret retrieval.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="hostEnvironment">The current host environment.</param>
    /// <param name="oidcTokenProvider">The OIDC token provider used by Ollama client-credentials authentication, or <c>null</c> when no Ollama tenants are configured.</param>
    public EmbeddingClient(
        IHttpClientFactory httpClientFactory,
        DaprClient daprClient,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IOidcTokenProvider? oidcTokenProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        _useFakeEmbedding = configuration.GetValue<bool>(FakeEmbeddingConfigKey);

        if (_useFakeEmbedding && !hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException("Fake embeddings are only supported in development and test environments.");
        }

        // Compose the provider strategies manually so the existing DI registration and constructor contract are
        // preserved (Story 23.9, Task 6). The secret store is shared across providers so priming and generation reuse
        // one cache keyed by secret key name.
        EmbeddingSecretStore secretStore = new(daprClient);
        _transport = new EmbeddingProviderTransport(httpClientFactory);
        _providerRegistry = new EmbeddingProviderRegistry(
            new GoogleEmbeddingProvider(secretStore),
            new OllamaEmbeddingProvider(secretStore, oidcTokenProvider));
    }

    /// <summary>Loads and caches the embedding API key so configuration failures happen before rate-limit budget is consumed.</summary>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration containing the API secret key name.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task PrimeApiKeyAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        EmbeddingProviderDefaults.Validate(config);

        if (_useFakeEmbedding)
        {
            return;
        }

        IEmbeddingProvider provider = _providerRegistry.Resolve(config.Provider);
        await provider.PrimeAsync(tenantId, config, ct).ConfigureAwait(false);
    }

    /// <summary>Generates an embedding vector for the given text using the tenant's configured provider.</summary>
    /// <param name="text">The text content to embed.</param>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A float array with the configured number of dimensions.</returns>
    public virtual async Task<float[]> GenerateAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(config);
        EmbeddingProviderDefaults.Validate(config);

        if (_useFakeEmbedding)
        {
            return CreateDeterministicVector(text, config.Dimensions);
        }

        IEmbeddingProvider provider = _providerRegistry.Resolve(config.Provider);
        IReadOnlyList<float[]> vectors = await _transport
            .ExecuteAsync(provider, [text], tenantId, config, batch: false, ct)
            .ConfigureAwait(false);
        return vectors[0];
    }

    /// <summary>Generates embedding vectors for a batch of texts using the tenant's configured provider, returning one
    /// vector per input in the same order. Every returned vector is validated against the configured dimension.</summary>
    /// <param name="texts">The ordered text contents to embed. Must be non-empty with no null, empty, or whitespace-only items.</param>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>One float array per input, each with the configured number of dimensions, in input order.</returns>
    public virtual async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        string tenantId,
        TenantEmbeddingConfig config,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(config);

        if (texts.Count == 0)
        {
            throw new ArgumentException("At least one text is required for batch embedding generation.", nameof(texts));
        }

        for (int i = 0; i < texts.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(texts[i]))
            {
                throw new ArgumentException($"Text at index {i} must not be null, empty, or whitespace.", nameof(texts));
            }
        }

        EmbeddingProviderDefaults.Validate(config);

        if (_useFakeEmbedding)
        {
            float[][] fakeVectors = new float[texts.Count][];
            for (int i = 0; i < texts.Count; i++)
            {
                fakeVectors[i] = CreateDeterministicVector(texts[i], config.Dimensions);
            }

            return fakeVectors;
        }

        IEmbeddingProvider provider = _providerRegistry.Resolve(config.Provider);
        return await _transport
            .ExecuteAsync(provider, texts, tenantId, config, batch: true, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Parses a persisted embedding provider identifier using the first colon as separator.</summary>
    /// <param name="embeddingProvider">The persisted embedding provider identifier.</param>
    /// <returns>The parsed provider and model components.</returns>
    /// <exception cref="ArgumentException">Thrown when the identifier is malformed or uses an unsupported provider.</exception>
    internal static EmbeddingProviderIdentifier ParseEmbeddingProviderIdentifier(string embeddingProvider)
    {
        if (string.IsNullOrWhiteSpace(embeddingProvider))
        {
            throw CreateEmbeddingProviderIdentifierException(embeddingProvider);
        }

        int separatorIndex = embeddingProvider.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == embeddingProvider.Length - 1)
        {
            throw CreateEmbeddingProviderIdentifierException(embeddingProvider);
        }

        string provider = embeddingProvider[..separatorIndex].Trim();
        string model = embeddingProvider[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(model) || (!IsGoogle(provider) && !IsOllama(provider)))
        {
            throw CreateEmbeddingProviderIdentifierException(embeddingProvider);
        }

        return new EmbeddingProviderIdentifier(provider.ToLowerInvariant(), model);
    }

    /// <summary>Redacts the supplied sensitive values from <paramref name="text"/> using a length-aware, longest-first
    /// substring replacement so overlapping secrets are fully masked and short benign substrings are not accidentally masked.</summary>
    /// <param name="text">The upstream payload that may contain leaked secrets.</param>
    /// <param name="sensitiveValues">The values that must not appear in the redacted output.</param>
    /// <returns>The redacted string.</returns>
    internal static string RedactSensitiveValues(string text, IReadOnlyCollection<string?> sensitiveValues)
        => EmbeddingResponseSanitizer.Redact(text, sensitiveValues);

    /// <summary>Redacts the supplied sensitive values plus the full input text when present.</summary>
    /// <param name="text">The upstream payload that may contain leaked secrets.</param>
    /// <param name="sensitiveValues">The secret-like values that must not appear in the redacted output.</param>
    /// <param name="fullInputText">The full embedding input text, redacted without the secret-length floor.</param>
    /// <returns>The redacted string.</returns>
    internal static string RedactSensitiveValues(
        string text,
        IReadOnlyCollection<string?> sensitiveValues,
        string? fullInputText)
        => EmbeddingResponseSanitizer.Redact(text, sensitiveValues, fullInputText);

    /// <summary>Parses the <c>Retry-After</c> header per RFC 9110 §10.2.3. Returns <c>0</c> when the header is absent,
    /// malformed, or points at a past date; positive results are clamped to <c>[1, 3600]</c>.</summary>
    /// <param name="header">The parsed Retry-After header value, or <c>null</c>.</param>
    /// <returns>The retry delay in seconds.</returns>
    internal static int ParseRetryAfterSeconds(RetryConditionHeaderValue? header)
        => EmbeddingProviderTransport.ParseRetryAfterSeconds(header);

    private static ArgumentException CreateEmbeddingProviderIdentifierException(string? embeddingProvider)
        => new(
            "EmbeddingProvider must use '{provider}:{model}' with a non-empty model and one of the supported providers: " +
            $"'{EmbeddingProviderDefaults.GoogleProviderName}', '{EmbeddingProviderDefaults.OllamaProviderName}'.",
            nameof(embeddingProvider));

    private static bool IsGoogle(string provider)
        => string.Equals(provider, EmbeddingProviderDefaults.GoogleProviderName, StringComparison.OrdinalIgnoreCase);

    private static bool IsOllama(string provider)
        => string.Equals(provider, EmbeddingProviderDefaults.OllamaProviderName, StringComparison.OrdinalIgnoreCase);

    private static float[] CreateDeterministicVector(string text, int dimensions)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        float[] vector = new float[dimensions];

        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = ((hash[i % hash.Length] / 255f) * 2f) - 1f;
        }

        return vector;
    }
}
