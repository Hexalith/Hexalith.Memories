// <copyright file="EmbeddingProviderDefaultsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class EmbeddingProviderDefaultsTests
{
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
    }

    [Fact]
    public void Validate_ValidConfig_ShouldNotThrow()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();

        Should.NotThrow(() => EmbeddingProviderDefaults.Validate(config));
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
}
