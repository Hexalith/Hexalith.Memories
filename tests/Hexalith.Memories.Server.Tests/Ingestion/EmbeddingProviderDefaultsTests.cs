// <copyright file="EmbeddingProviderDefaultsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Reflection;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class EmbeddingProviderDefaultsTests
{
    [Fact]
    public void CreateOllamaDefault_ShouldSourceEndpointsFromOptions()
    {
        // spec-infrastructure-dependency-abstraction (F1, Decision D30): the Ollama default endpoints
        // are config-sourced. Feeding non-default options must flow through to the produced config,
        // proving BaseUrl / OidcTokenEndpoint / OidcClientId / OidcScope are no longer compiled literals.
        // Uses the pure creator (not the static seam) so no global state is mutated — keeps the value-
        // pinning tests and parallel test classes that call Ollama()/Google() unaffected.
        EmbeddingProviderDefaultsOptions options = new()
        {
            Ollama = new OllamaProviderDefaults
            {
                BaseUrl = "https://ollama.internal",
                OidcTokenEndpoint = "https://idp.internal/token",
                OidcClientId = "custom-client",
                OidcScope = "custom-scope",
            },
        };

        TenantEmbeddingConfig config = EmbeddingProviderDefaults.CreateOllamaDefault(options);

        config.BaseUrl.ShouldBe("https://ollama.internal");
        config.OidcTokenEndpoint.ShouldBe("https://idp.internal/token");
        config.OidcClientId.ShouldBe("custom-client");
        config.OidcScope.ShouldBe("custom-scope");
        // Non-endpoint fields stay pinned to the built-in Ollama identity.
        config.Provider.ShouldBe("ollama");
        config.Model.ShouldBe("qwen3-embedding:4b");
        config.Dimensions.ShouldBe(2560);
        config.ApiSecretKeyName.ShouldBe("memories-embedding-client-secret");
    }

    [Fact]
    public void EmbeddingProviderDefaults_ShouldNotEmbedInfrastructureEndpointLiterals()
    {
        // spec-infrastructure-dependency-abstraction (F1, Decision D30; review D4): no infrastructure
        // endpoint host/URL literal may remain baked into the registry/validation type. The sanctioned
        // home for overridable defaults is EmbeddingProviderDefaultsOptions property initializers.
        // Provider/model/auth-mode name constants are allowed; endpoint hosts and URL schemes are not.
        string[] forbiddenFragments = ["tache.ai", "googleapis.com", "://"];

        IEnumerable<string> stringLiterals = typeof(EmbeddingProviderDefaults)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(string))
            .Select(f =>
            {
                if (f.IsLiteral)
                {
                    return (string?)f.GetRawConstantValue() ?? string.Empty;
                }

                if (f.IsStatic && f.GetValue(null) is string staticValue)
                {
                    return staticValue;
                }

                return string.Empty;
            });

        IEnumerable<string> staticReadonlyStrings = typeof(EmbeddingProviderDefaults)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsInitOnly)
            .Select(f => (string?)f.GetValue(null) ?? string.Empty);

        foreach (string value in stringLiterals.Concat(staticReadonlyStrings))
        {
            foreach (string forbidden in forbiddenFragments)
            {
                value.ShouldNotContain(forbidden);
            }
        }
    }

    [Fact]
    public void Configure_ThenOllama_UsesConfiguredEndpointsThroughLiveSeam()
    {
        // review P4: drive Configure(...) → registry → Ollama() so deleting the composition-root
        // Configure call cannot silently leave every test green.
        EmbeddingProviderDefaultsOptions previous = EmbeddingProviderDefaults.CurrentOptions;
        try
        {
            EmbeddingProviderDefaults.Configure(new EmbeddingProviderDefaultsOptions
            {
                Ollama = new OllamaProviderDefaults
                {
                    BaseUrl = "https://ollama.seam-test",
                    OidcTokenEndpoint = "https://idp.seam-test/token",
                    OidcClientId = "seam-client",
                    OidcScope = "seam-scope",
                },
            });

            TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama();

            config.BaseUrl.ShouldBe("https://ollama.seam-test");
            config.OidcTokenEndpoint.ShouldBe("https://idp.seam-test/token");
            config.OidcClientId.ShouldBe("seam-client");
            config.OidcScope.ShouldBe("seam-scope");
        }
        finally
        {
            EmbeddingProviderDefaults.Configure(previous);
        }
    }

    [Theory]
    [InlineData(null, "openid")]
    [InlineData("", "openid")]
    [InlineData("   ", "openid")]
    [InlineData("memories-embedding", null)]
    [InlineData("memories-embedding", "")]
    [InlineData("memories-embedding", "   ")]
    public void Configure_WithEmptyOidcClientIdOrScope_Throws(string? clientId, string? scope)
    {
        // review patch #15
        Should.Throw<ArgumentException>(() =>
            EmbeddingProviderDefaults.Configure(new EmbeddingProviderDefaultsOptions
            {
                Ollama = new OllamaProviderDefaults
                {
                    OidcClientId = clientId,
                    OidcScope = scope,
                },
            }));
    }

    [Fact]
    public void Google_ShouldReturnCorrectDefaults()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        config.Provider.ShouldBe("google");
        config.Model.ShouldBe("gemini-embedding-001");
        config.Dimensions.ShouldBe(768);
        config.RateLimitPerMinute.ShouldBe(1500);
        config.ApiSecretKeyName.ShouldBe("google-embedding-api-key");
        config.ReindexRequired.ShouldBeFalse();
        config.BaseUrl.ShouldBeNull();
        config.AuthMode.ShouldBe("api-key");
        config.OidcTokenEndpoint.ShouldBeNull();
        config.OidcClientId.ShouldBeNull();
        config.OidcScope.ShouldBeNull();
    }

    [Fact]
    public void Validate_ValidConfig_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_NullConfig_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => EmbeddingProviderDefaults.Validate(null!));
    }

    [Fact]
    public void Validate_UnsupportedProvider_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Provider = "openai" };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_DimensionsZero_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 0 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_NegativeDimensions_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = -1 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_RateLimitExceedsMaximum_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 3001 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_RateLimitZero_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 0 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_ApiSecretKeyNameWithSpecialChars_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { ApiSecretKeyName = "../secret-key" };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData("key with spaces")]
    [InlineData("KEY_UPPER")]
    [InlineData("key/slash")]
    [InlineData("key\\backslash")]
    [InlineData("")]
    public void Validate_InvalidApiSecretKeyNames_ShouldThrow(string invalidKeyName)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { ApiSecretKeyName = invalidKeyName };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyProvider_ShouldThrow(string provider)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Provider = provider };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyModel_ShouldThrow(string model)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Model = model };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_RateLimitAtMaximum_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 3000 };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData(768)]
    [InlineData(1536)]
    [InlineData(3072)]
    public void Validate_GoogleSupportedDimensions_ShouldNotThrow(int dimensions)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = dimensions };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_GoogleUnsupportedDimension_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 42 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_ModelWithUnsafeCharacters_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Model = "gemini/embedding/001" };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void GetBreakingChangeFields_ShouldReportProviderModelAndDimensions()
    {
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig proposed = current with
        {
            Provider = "openai",
            Model = "other-model",
            Dimensions = 1536,
        };

        string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(current, proposed);

        affectedFields.ShouldBe(["provider", "model", "dimensions"]);
    }

    [Theory]
    [InlineData("valid-key-name")]
    [InlineData("key123")]
    [InlineData("a")]
    [InlineData("my-secret-key-01")]
    public void Validate_ValidApiSecretKeyNames_ShouldNotThrow(string validKeyName)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { ApiSecretKeyName = validKeyName };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Ollama_ShouldReturnCorrectDefaults()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama();

        config.Provider.ShouldBe("ollama");
        config.Model.ShouldBe("qwen3-embedding:4b");
        config.Dimensions.ShouldBe(2560);
        config.RateLimitPerMinute.ShouldBe(6000);
        config.ApiSecretKeyName.ShouldBe("memories-embedding-client-secret");
        config.ReindexRequired.ShouldBeFalse();
        config.BaseUrl.ShouldBe("https://llm.tache.ai");
        config.AuthMode.ShouldBe("oidc-client-credentials");
        config.OidcTokenEndpoint.ShouldBe("https://auth.tache.ai/realms/tache/protocol/openid-connect/token");
        config.OidcClientId.ShouldBe("memories-embedding");
        config.OidcScope.ShouldBe("openid");
    }

    [Fact]
    public void Validate_OllamaProvider_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama();

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_OllamaWithEmptyModel_ShouldThrow(string model)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { Model = model };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_OllamaUnsupportedDimension_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { Dimensions = 768 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_OllamaWithModelColon_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama();

        config.Model.ShouldBe("qwen3-embedding:4b");
        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData("model/with/slash")]
    [InlineData("model with space")]
    [InlineData("model;semi")]
    public void Validate_OllamaModelWithUnsafeCharacters_ShouldThrow(string unsafeModel)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { Model = unsafeModel };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_UnsupportedProvider_ErrorMessageListsSupportedProviders()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Provider = "openai" };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("google");
        ex.Message.ShouldContain("ollama");
    }

    [Theory]
    [InlineData("google", "qwen3-embedding:4b", 2560)]
    [InlineData("ollama", "gemini-embedding-001", 768)]
    public void Validate_CrossProviderModelPairs_ShouldThrow(string provider, string model, int dimensions)
    {
        TenantEmbeddingConfig config = provider == EmbeddingProviderDefaults.GoogleProviderName
            ? EmbeddingProviderDefaults.Google() with { Provider = provider, Model = model, Dimensions = dimensions }
            : EmbeddingProviderDefaults.Ollama() with { Provider = provider, Model = model, Dimensions = dimensions };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(model);
        ex.Message.ShouldContain(provider);
    }

    [Theory]
    [InlineData("google", "totally-fake")]
    [InlineData("ollama", "totally-fake")]
    public void Validate_UnknownModelForProvider_ShouldThrowAndListProviderModels(string provider, string model)
    {
        TenantEmbeddingConfig config = provider == EmbeddingProviderDefaults.GoogleProviderName
            ? EmbeddingProviderDefaults.Google() with { Model = model }
            : EmbeddingProviderDefaults.Ollama() with { Model = model };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(model);
        ex.Message.ShouldContain(provider);

        if (provider == EmbeddingProviderDefaults.GoogleProviderName)
        {
            ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleModelName);
            ex.Message.ShouldNotContain(EmbeddingProviderDefaults.OllamaModelName);
        }
        else
        {
            ex.Message.ShouldContain(EmbeddingProviderDefaults.OllamaModelName);
            ex.Message.ShouldNotContain(EmbeddingProviderDefaults.GoogleModelName);
        }
    }

    [Fact]
    public void Validate_UnknownModelAtSharedDimensionMaximum_ShouldThrowUnknownModelError()
    {
        // The unknown-model error is expected — not the dimension-cap error — because the dimension
        // value 16_384 is exactly at the shared maximum (inclusive) and clears the cap check, then
        // the closed-registry model lookup rejects "future-model" before any index path can run.
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with
        {
            Model = "future-model",
            Dimensions = 16_384,
        };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("future-model");
        ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleModelName);
        ex.Message.ShouldNotContain("16384");
    }

    [Fact]
    public void Validate_DimensionsAboveSharedMaximum_ShouldThrowAtConfigTime()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = int.MaxValue };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("Dimensions");
        ex.Message.ShouldContain("16384");
    }

    [Fact]
    public void Validate_OllamaRateLimitAtMaximum_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { RateLimitPerMinute = 60_000 };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_OllamaRateLimitExceedsMaximum_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { RateLimitPerMinute = 60_001 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 5000 };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with
        {
            Model = "qwen3-embedding:4b",
            Dimensions = 768,
        };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void GetBreakingChangeFields_GoogleToOllama_ShouldReportProviderModelAndDimensions()
    {
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig proposed = EmbeddingProviderDefaults.Ollama();

        string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(current, proposed);

        affectedFields.ShouldBe(["provider", "model", "dimensions"]);
    }

    [Theory]
    [InlineData(2559)]
    [InlineData(2561)]
    [InlineData(768)]
    [InlineData(1024)]
    [InlineData(1536)]
    public void Validate_OllamaQwen3_AcceptsExactly2560(int dimensions)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { Dimensions = dimensions };

        Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_GoogleLegacyConfigWithoutOidcFields_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with
        {
            BaseUrl = null,
            AuthMode = "api-key",
            OidcTokenEndpoint = null,
            OidcClientId = null,
            OidcScope = null,
        };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" api-key")]
    [InlineData("api-key ")]
    [InlineData("api_key")]
    [InlineData("api key")]
    [InlineData("oidcClientCredentials")]
    [InlineData("bearer")]
    public void Validate_UnsupportedAuthMode_ShouldThrowAndListSupportedValues(string? authMode)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { AuthMode = authMode! };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("api-key");
        ex.Message.ShouldContain("oidc-client-credentials");
    }

    [Theory]
    [InlineData("API-KEY")]
    [InlineData("Oidc-Client-Credentials")]
    public void Validate_AuthModeComparison_ShouldIgnoreCase(string authMode)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { AuthMode = authMode };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_OllamaApiKeyModeWithoutBaseUrl_ShouldThrowAndNameBaseUrl(string? baseUrl)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with
        {
            AuthMode = "api-key",
            BaseUrl = baseUrl,
            OidcTokenEndpoint = null,
            OidcClientId = null,
            OidcScope = null,
        };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("BaseUrl");
    }

    [Theory]
    [InlineData(null, "BaseUrl")]
    [InlineData("", "BaseUrl")]
    [InlineData("   ", "BaseUrl")]
    public void Validate_OidcModeWithoutBaseUrl_ShouldThrowAndNameBaseUrl(string? baseUrl, string fieldName)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { BaseUrl = baseUrl };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(fieldName);
    }

    [Theory]
    [InlineData(null, "OidcTokenEndpoint")]
    [InlineData("", "OidcTokenEndpoint")]
    [InlineData("   ", "OidcTokenEndpoint")]
    public void Validate_OidcModeWithoutTokenEndpoint_ShouldThrowAndNameOidcTokenEndpoint(string? tokenEndpoint, string fieldName)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { OidcTokenEndpoint = tokenEndpoint };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(fieldName);
    }

    [Theory]
    [InlineData(null, "OidcClientId")]
    [InlineData("", "OidcClientId")]
    [InlineData("   ", "OidcClientId")]
    public void Validate_OidcModeWithoutClientId_ShouldThrowAndNameOidcClientId(string? clientId, string fieldName)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { OidcClientId = clientId };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(fieldName);
    }

    [Fact]
    public void Validate_OidcModeWithoutScope_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { OidcScope = null };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData("BaseUrl", "http://localhost")]
    [InlineData("BaseUrl", "http://127.0.0.1:11434")]
    [InlineData("BaseUrl", "https://llm.tache.ai")]
    [InlineData("BaseUrl", "https://llm.tache.ai/ollama")]
    [InlineData("OidcTokenEndpoint", "http://localhost/realms/tache/protocol/openid-connect/token")]
    [InlineData("OidcTokenEndpoint", "http://localhost:8080/realms/tache/protocol/openid-connect/token")]
    [InlineData("OidcTokenEndpoint", "http://127.0.0.1:8080/realms/tache/protocol/openid-connect/token")]
    [InlineData("OidcTokenEndpoint", "http://[::1]/realms/tache/protocol/openid-connect/token")]
    [InlineData("OidcTokenEndpoint", "http://[::1]:8080/realms/tache/protocol/openid-connect/token")]
    [InlineData("OidcTokenEndpoint", "https://auth.tache.ai/realms/tache/protocol/openid-connect/token")]
    public void Validate_AbsoluteHttpUrls_ShouldNotThrow(string fieldName, string value)
    {
        TenantEmbeddingConfig config = SetUrlField(EmbeddingProviderDefaults.Ollama(), fieldName, value);

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Theory]
    [InlineData("http://auth.tache.ai/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://10.0.0.5/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://172.16.0.5/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://192.168.1.20/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://169.254.169.254/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://host.docker.internal/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://localtest.me/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://keycloak.internal/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://127.0.0.2/realms/tache/protocol/openid-connect/token")]
    [InlineData("http://[::ffff:127.0.0.1]/realms/tache/protocol/openid-connect/token")] // Story 15.4 P2: IPv4-mapped IPv6 is not the literal [::1].
    [InlineData("http://[::ffff:7f00:1]/realms/tache/protocol/openid-connect/token")] // Story 15.4 P2: compressed IPv4-mapped IPv6 form.
    [InlineData("http://localhost./realms/tache/protocol/openid-connect/token")] // Story 15.4 P3: trailing-dot host is not the literal "localhost".
    public void Validate_NonLoopbackHttpOidcTokenEndpoint_ShouldThrowAndNotEchoEndpoint(string endpoint)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { OidcTokenEndpoint = endpoint };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));

        AssertSanitizedTransportPolicyMessage(ex, nameof(config.OidcTokenEndpoint), endpoint);
    }

    // Story 15.4 D2 pinning: .NET `Uri` canonicalizes alternative literal forms of loopback to the
    // allowed `127.0.0.1` / `[::1]` host values. These forms are intentionally accepted; pinning
    // them with tests means a future refactor that introduces a stricter literal-string match (and
    // therefore changes user-visible behavior) will surface in CI rather than silently break local
    // operator setups.
    [Theory]
    [InlineData("http://2130706433/realms/tache/protocol/openid-connect/token")] // decimal IPv4 form of 127.0.0.1
    [InlineData("http://127.0.0.001/realms/tache/protocol/openid-connect/token")] // octal-style leading zeros for 127.0.0.1
    [InlineData("http://[0:0:0:0:0:0:0:1]/realms/tache/protocol/openid-connect/token")] // expanded IPv6 loopback
    [InlineData("http://[::0001]/realms/tache/protocol/openid-connect/token")] // padded compressed IPv6 loopback
    public void Validate_UriCanonicalizedLoopbackForms_ShouldNotThrow(string endpoint)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { OidcTokenEndpoint = endpoint };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_NonLoopbackHttpOidcTokenEndpointWithSecretLikePath_ShouldNotEchoEndpoint()
    {
        const string endpoint = "http://auth.tache.ai/realms/Bearer%20abc.def.ghi/client-secret-value/token";
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { OidcTokenEndpoint = endpoint };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));

        AssertSanitizedTransportPolicyMessage(ex, nameof(config.OidcTokenEndpoint), endpoint);
        ex.Message.ShouldNotContain("Bearer");
        ex.Message.ShouldNotContain("abc.def.ghi");
        ex.Message.ShouldNotContain("client-secret-value");
    }

    [Theory]
    [InlineData("BaseUrl", "/relative")]
    [InlineData("BaseUrl", "llm.tache.ai")]
    [InlineData("BaseUrl", "not a url")]
    [InlineData("BaseUrl", "ftp://llm.tache.ai")]
    [InlineData("OidcTokenEndpoint", "/token")]
    [InlineData("OidcTokenEndpoint", "auth.tache.ai/token")]
    [InlineData("OidcTokenEndpoint", "not a url")]
    [InlineData("OidcTokenEndpoint", "ftp://auth.tache.ai/token")]
    public void Validate_InvalidUrls_ShouldThrowAndNameField(string fieldName, string value)
    {
        TenantEmbeddingConfig config = SetUrlField(EmbeddingProviderDefaults.Ollama(), fieldName, value);

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(fieldName);
    }

    [Fact]
    public void Validate_GoogleWithValidOidcMetadata_ShouldNotRequireOidcMode()
    {
        // Google + api-key with populated OIDC metadata is metadata-only per AC4. The values still
        // have to be shape-valid (AC9 strict), so a real absolute URL is used here rather than a
        // relative-path placeholder.
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with
        {
            BaseUrl = "https://metadata.example.test",
            OidcTokenEndpoint = "https://auth.example.test/token",
            OidcClientId = null,
            OidcScope = "openid",
        };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_GoogleWithOidcClientCredentialsAuthMode_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with
        {
            AuthMode = "oidc-client-credentials",
            BaseUrl = "https://metadata.example.test",
            OidcTokenEndpoint = "https://auth.example.test/token",
            OidcClientId = "google-oidc",
        };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("oidc-client-credentials");
        ex.Message.ShouldContain("ollama");
    }

    [Theory]
    [InlineData("BaseUrl", "not a url")]
    [InlineData("BaseUrl", "/relative")]
    [InlineData("BaseUrl", "ftp://example.test")]
    [InlineData("OidcTokenEndpoint", "relative-token-endpoint")]
    [InlineData("OidcTokenEndpoint", "/path")]
    [InlineData("OidcTokenEndpoint", "ftp://auth.example.test/token")]
    public void Validate_GoogleWithMalformedUrlMetadata_ShouldThrow(string fieldName, string value)
    {
        TenantEmbeddingConfig config = SetUrlField(EmbeddingProviderDefaults.Google(), fieldName, value);

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(fieldName);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Abc")]
    [InlineData("a-b")]
    [InlineData("a.b")]
    [InlineData("a:b")]
    [InlineData("a_b")]
    [InlineData("0a")]
    [InlineData("9-x")]
    [InlineData("text-embedding-ada-002")]
    public void Validate_SyntacticallyValidButUnregisteredModel_ShouldThrow(string model)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Model = model };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleProviderName);
        ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleModelName);
    }

    [Theory]
    [InlineData("Google", "Gemini-Embedding-001", EmbeddingProviderDefaults.GoogleProviderName, EmbeddingProviderDefaults.GoogleModelName)]
    [InlineData("Ollama", "Qwen3-Embedding:4B", EmbeddingProviderDefaults.OllamaProviderName, EmbeddingProviderDefaults.OllamaModelName)]
    public void Validate_MixedCaseProviderAndModel_ShouldUseCaseInsensitiveRegistryLookup(
        string provider,
        string model,
        string canonicalProvider,
        string canonicalModel)
    {
        // Establish that the test inputs differ from canonical casing so we are exercising the
        // case-insensitive lookup, not a coincidental match.
        provider.ShouldNotBe(canonicalProvider);
        model.ShouldNotBe(canonicalModel);

        TenantEmbeddingConfig config = string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase)
            ? EmbeddingProviderDefaults.Google() with { Provider = provider, Model = model }
            : EmbeddingProviderDefaults.Ollama() with { Provider = provider, Model = model };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));

        // Caller-provided casing is preserved on the config; the registry only normalizes for
        // lookup, not for persistence.
        config.Provider.ShouldBe(provider);
        config.Model.ShouldBe(model);
    }

    [Fact]
    public void Validate_OllamaRateLimitAboveGoogleCeilingButBelowOllamaCeiling_ShouldNotFallBackToGoogle()
    {
        // 30_000 is well above Google's 3_000 ceiling and well below Ollama's 60_000 ceiling, so the
        // assertion would fail if the registry silently routed Ollama lookups through Google's limit.
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama() with { RateLimitPerMinute = 30_000 };

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
    }

    [Fact]
    public void Validate_GoogleRateLimitAboveGoogleCeilingButBelowOllamaCeiling_ShouldThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 6000 };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("3000");
        ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleProviderName);
    }

    [Fact]
    public void Validate_UnknownProviderError_ShouldNotLeakModelNames()
    {
        // Unsupported-provider error must list providers only; leaking model names would imply
        // the registry could be probed via crafted provider inputs.
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Provider = "unknown-provider" };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("unknown-provider");
        ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleProviderName);
        ex.Message.ShouldContain(EmbeddingProviderDefaults.OllamaProviderName);
        ex.Message.ShouldNotContain(EmbeddingProviderDefaults.GoogleModelName);
        ex.Message.ShouldNotContain(EmbeddingProviderDefaults.OllamaModelName);
    }

    [Fact]
    public void Validate_GoogleConfigWithOllamaModel_ShouldNotLeakOllamaModelNameInSupportedSuffix()
    {
        // Unsupported-model error for provider P must echo the offending input (so the operator
        // knows what was rejected) and list only P's supported models in the "Supported models …"
        // suffix. The Ollama model name therefore appears exactly once — as the echoed input —
        // never inside the supported-models suffix that follows.
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with
        {
            Model = EmbeddingProviderDefaults.OllamaModelName,
            Dimensions = 2560,
        };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleProviderName);
        ex.Message.ShouldContain(EmbeddingProviderDefaults.GoogleModelName);

        const string suffixMarker = "Supported models for provider";
        int suffixStart = ex.Message.IndexOf(suffixMarker, StringComparison.Ordinal);
        suffixStart.ShouldBeGreaterThan(-1);
        string supportedSuffix = ex.Message[suffixStart..];
        supportedSuffix.ShouldNotContain(EmbeddingProviderDefaults.OllamaModelName);
    }

    [Fact]
    public void Validate_OrderingContract_AuthModeFiresAfterRateLimitWhenBothInvalid()
    {
        // Pins the new validation order documented on EmbeddingProviderDefaults.Validate: when a
        // config is invalid on both rate-limit and auth-mode, rate-limit wins. Telemetry and
        // operator runbooks that pattern-match on error sequence depend on this contract.
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with
        {
            RateLimitPerMinute = 999_999,
            AuthMode = "not-a-real-mode",
        };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.ParamName.ShouldBe(nameof(config.RateLimitPerMinute));
        ex.Message.ShouldNotContain("AuthMode");
    }

    [Fact]
    public void Validate_OrderingContract_ProviderRegistryFiresBeforeModelShapeRegex()
    {
        // Pins ordering: unsupported provider beats malformed-model-regex when both are wrong.
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with
        {
            Provider = "unknown-provider",
            Model = ".bad-model-starts-with-punctuation",
        };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.ParamName.ShouldBe(nameof(config.Provider));
        ex.Message.ShouldContain("unknown-provider");
    }

    [Theory]
    [InlineData(".abc")]
    [InlineData(":xyz")]
    [InlineData("_x")]
    [InlineData("-test")]
    [InlineData(":")]
    [InlineData(".")]
    [InlineData("-")]
    [InlineData("_")]
    [InlineData(":::")]
    [InlineData("---")]
    [InlineData("...")]
    public void Validate_ModelNameStartsWithPunctuation_ShouldThrow(string model)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Model = model };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain("Model");
    }

    [Theory]
    [InlineData(null, "<null>")]
    [InlineData("", "<empty>")]
    [InlineData("   ", "<whitespace>")]
    public void Validate_BlankAuthMode_ShouldDescribeBlankClassInMessage(string? authMode, string expectedMarker)
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { AuthMode = authMode! };

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));
        ex.Message.ShouldContain(expectedMarker);
    }

    [Fact]
    public void GetBreakingChangeFields_OllamaBaseUrlChanged_ShouldIncludeBaseUrl()
    {
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Ollama() with { BaseUrl = "https://llm.tache.ai/" };
        TenantEmbeddingConfig proposed = current with { BaseUrl = "https://other-llm.tache.ai" };

        string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(current, proposed);

        affectedFields.ShouldContain("baseUrl");
    }

    [Fact]
    public void GetBreakingChangeFields_OllamaBaseUrlEquivalentAfterNormalization_ShouldNotIncludeBaseUrl()
    {
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Ollama() with { BaseUrl = " https://llm.tache.ai/ " };
        TenantEmbeddingConfig proposed = current with { BaseUrl = "https://LLM.TACHE.AI" };

        string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(current, proposed);

        affectedFields.ShouldBeEmpty();
    }

    [Fact]
    public void GetBreakingChangeFields_OidcMetadataChanged_ShouldNotRequireReindex()
    {
        TenantEmbeddingConfig current = EmbeddingProviderDefaults.Ollama();
        TenantEmbeddingConfig proposed = current with
        {
            AuthMode = "api-key",
            OidcTokenEndpoint = "https://auth2.tache.ai/token",
            OidcClientId = "other-client",
            ApiSecretKeyName = "other-client-secret",
            OidcScope = "openid profile",
        };

        string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(current, proposed);

        affectedFields.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("BaseUrl", "https://user:pw@llm.tache.ai")]
    [InlineData("BaseUrl", "https://user@llm.tache.ai")]
    [InlineData("OidcTokenEndpoint", "https://user:pw@auth.tache.ai/token")]
    [InlineData("OidcTokenEndpoint", "https://creds@auth.tache.ai/token")]
    public void Validate_UrlsWithEmbeddedCredentials_ShouldThrowAndNotEchoCredentials(string fieldName, string value)
    {
        // Story 14.3 AC3: provider URLs and OIDC token endpoints must reject userinfo and the
        // resulting exception must not echo any embedded user/password values.
        TenantEmbeddingConfig config = SetUrlField(EmbeddingProviderDefaults.Ollama(), fieldName, value);

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));

        ex.Message.ShouldContain(fieldName);
        ex.Message.ShouldContain("user-info");
        ex.Message.ShouldNotContain("user:pw");
        ex.Message.ShouldNotContain("creds");
    }

    [Theory]
    [InlineData("BaseUrl", "https://llm.tache.ai/?secret=value")]
    [InlineData("OidcTokenEndpoint", "https://auth.tache.ai/realms/tache/token?client_secret=leaked")]
    public void Validate_UrlsWithQuery_ShouldThrowAndNotEchoQueryValues(string fieldName, string value)
    {
        TenantEmbeddingConfig config = SetUrlField(EmbeddingProviderDefaults.Ollama(), fieldName, value);

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));

        ex.Message.ShouldContain(fieldName);
        ex.Message.ShouldContain("query string");
        ex.Message.ShouldNotContain("leaked");
        ex.Message.ShouldNotContain("secret=value");
    }

    [Theory]
    [InlineData("BaseUrl", "https://llm.tache.ai/#section")]
    [InlineData("OidcTokenEndpoint", "https://auth.tache.ai/realms/tache/token#bookmark")]
    public void Validate_UrlsWithFragment_ShouldThrow(string fieldName, string value)
    {
        TenantEmbeddingConfig config = SetUrlField(EmbeddingProviderDefaults.Ollama(), fieldName, value);

        ArgumentException ex = Should.Throw<ArgumentException>(() => EmbeddingProviderDefaults.Validate(config));

        ex.Message.ShouldContain(fieldName);
        ex.Message.ShouldContain("fragment");
    }

    private static TenantEmbeddingConfig SetUrlField(TenantEmbeddingConfig config, string fieldName, string value)
        => fieldName switch
        {
            "BaseUrl" => config with { BaseUrl = value },
            "OidcTokenEndpoint" => config with { OidcTokenEndpoint = value },
            _ => throw new ArgumentOutOfRangeException(nameof(fieldName), fieldName, "Unsupported URL field."),
        };

    private static void AssertSanitizedTransportPolicyMessage(
        ArgumentException ex,
        string parameterName,
        string endpoint)
    {
        ex.ParamName.ShouldBe(parameterName);
        ex.Message.ShouldContain("HTTPS");
        ex.Message.ShouldContain("loopback");
        ex.Message.ShouldContain("localhost");
        ex.Message.ShouldContain("127.0.0.1");
        ex.Message.ShouldContain("[::1]");
        ex.Message.ShouldNotContain(endpoint);

        // Story 15.4 P4: strengthen leak guard. ShouldNotContain(endpoint) only catches the full
        // URL; a regression that echoes just the host, path, or known credential markers would
        // still pass. Probe each segment explicitly. The literal allowlist tokens shared with the
        // policy message (`localhost`, `127.0.0.1`, `[::1]`) are excluded from the host-leak guard
        // because they appear in the legitimate policy text.
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? parsed))
        {
            string host = parsed.Host;
            if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(host, "127.0.0.1", StringComparison.Ordinal) &&
                !string.Equals(host, "[::1]", StringComparison.Ordinal))
            {
                ex.Message.ShouldNotContain(host);
            }

            if (!string.IsNullOrEmpty(parsed.AbsolutePath) && parsed.AbsolutePath != "/")
            {
                ex.Message.ShouldNotContain(parsed.AbsolutePath);
            }
        }

        ex.Message.ShouldNotContain("Bearer");
        ex.Message.ShouldNotContain("client_secret");
        ex.Message.ShouldNotContain("client-secret");
    }
}
