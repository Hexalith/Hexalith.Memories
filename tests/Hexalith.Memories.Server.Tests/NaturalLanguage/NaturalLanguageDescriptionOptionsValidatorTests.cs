// <copyright file="NaturalLanguageDescriptionOptionsValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.IO;

using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>Story 9.2 Task 1.9 — tests for <see cref="NaturalLanguageDescriptionOptionsValidator"/>.
/// Covers the two validator gates (Production echo-component + cross-tenant cache acknowledgment)
/// per AC #12 + Risk #10 + Risk #16.</summary>
public sealed class NaturalLanguageDescriptionOptionsValidatorTests
{
    [Fact]
    public void ProductionWithEchoComponent_ReturnsFailure_Emits9161()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator("Production", ttl: null);

        NaturalLanguageDescriptionOptions options = new() { DaprComponentName = "conversation.echo" };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldNotBeNull().ShouldContain(
            f => f.Contains("9161 EchoComponentNotAllowedInProduction", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionWithRealComponent_ReturnsSuccess()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator("Production", ttl: TimeSpan.Zero);

        NaturalLanguageDescriptionOptions options = new() { DaprComponentName = "llm-openai" };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ProductionWithoutComponentMaterial_ReturnsFailure()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator("Production", ttl: null);

        ValidateOptionsResult result = sut.Validate(
            null,
            new NaturalLanguageDescriptionOptions { DaprComponentName = "llm-openai" });

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldNotBeNull().ShouldContain(
            failure => failure.Contains("9165 ConversationComponentMaterialUnavailable", StringComparison.Ordinal));
    }

    [Fact]
    public void DevelopmentWithEchoComponent_ReturnsSuccess()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator("Development", ttl: null);

        NaturalLanguageDescriptionOptions options = new() { DaprComponentName = "conversation.echo" };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void CacheTtlNonZero_WithoutAcknowledgment_ReturnsFailure_Emits9164()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator(
            "Development",
            ttl: TimeSpan.FromSeconds(60));

        NaturalLanguageDescriptionOptions options = new()
        {
            DaprComponentName = "llm",
            AcceptCrossTenantCacheSharing = false,
        };

        using EnvironmentVariableScope _ = EnvironmentVariableScope.Clear(
            NaturalLanguageDescriptionOptionsValidator.CacheAckEnvVar);

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldNotBeNull().ShouldContain(
            f => f.Contains("9164 ResponseCacheEnabledWithoutAcknowledgment", StringComparison.Ordinal));
    }

    [Fact]
    public void CacheTtlNonZero_WithConfigAcknowledgment_ReturnsSuccess()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator(
            "Development",
            ttl: TimeSpan.FromSeconds(60));

        NaturalLanguageDescriptionOptions options = new()
        {
            DaprComponentName = "llm",
            AcceptCrossTenantCacheSharing = true,
        };

        using EnvironmentVariableScope _ = EnvironmentVariableScope.Clear(
            NaturalLanguageDescriptionOptionsValidator.CacheAckEnvVar);

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void CacheTtlNonZero_WithEnvVarAcknowledgment_ReturnsSuccess()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator(
            "Development",
            ttl: TimeSpan.FromSeconds(60));

        NaturalLanguageDescriptionOptions options = new()
        {
            DaprComponentName = "llm",
            AcceptCrossTenantCacheSharing = false,
        };

        using EnvironmentVariableScope _ = EnvironmentVariableScope.Set(
            NaturalLanguageDescriptionOptionsValidator.CacheAckEnvVar,
            NaturalLanguageDescriptionOptionsValidator.CacheAckEnvVarExpectedValue);

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void CacheTtlZero_NoAcknowledgmentNeeded_ReturnsSuccess()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator(
            "Development",
            ttl: TimeSpan.Zero);

        NaturalLanguageDescriptionOptions options = new()
        {
            DaprComponentName = "llm",
            AcceptCrossTenantCacheSharing = false,
        };

        using EnvironmentVariableScope _ = EnvironmentVariableScope.Clear(
            NaturalLanguageDescriptionOptionsValidator.CacheAckEnvVar);

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void CacheTtlNull_NoAcknowledgmentNeeded_ReturnsSuccess()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator(
            "Development",
            ttl: null);

        NaturalLanguageDescriptionOptions options = new()
        {
            DaprComponentName = "llm",
            AcceptCrossTenantCacheSharing = false,
        };

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ProductionWithEchoAndNonZeroTtl_ReportsBothFailures()
    {
        NaturalLanguageDescriptionOptionsValidator sut = BuildValidator(
            "Production",
            ttl: TimeSpan.FromSeconds(60));

        NaturalLanguageDescriptionOptions options = new()
        {
            DaprComponentName = "conversation.echo",
            AcceptCrossTenantCacheSharing = false,
        };

        using EnvironmentVariableScope _ = EnvironmentVariableScope.Clear(
            NaturalLanguageDescriptionOptionsValidator.CacheAckEnvVar);

        ValidateOptionsResult result = sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldNotBeNull().Count().ShouldBe(2);
        result.Failures.ShouldContain(
            f => f.Contains("9161 EchoComponentNotAllowedInProduction", StringComparison.Ordinal));
        result.Failures.ShouldContain(
            f => f.Contains("9164 ResponseCacheEnabledWithoutAcknowledgment", StringComparison.Ordinal));
    }

    [Fact]
    public void FileSystemComponentYamlReader_ParsesCompoundResponseCacheTtl()
    {
        using TemporaryDirectoryScope directory = new();
        File.WriteAllText(
                Path.Combine(directory.DirectoryPath, "conversation-llm.yaml"),
                """
                        apiVersion: dapr.io/v1alpha1
                        kind: Component
                        metadata:
                            name: llm
                        spec:
                            metadata:
                            - name: responseCacheTTL
                                value: "1m30s"
                        """);

        FileSystemComponentYamlReader reader = new(directory.DirectoryPath);

        reader.TryReadResponseCacheTtl("llm").ShouldBe(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void FileSystemComponentYamlReader_ParsesCacheTtlAlias()
    {
        using TemporaryDirectoryScope directory = new();
        File.WriteAllText(
                Path.Combine(directory.DirectoryPath, "conversation-llm.yaml"),
                """
                        apiVersion: dapr.io/v1alpha1
                        kind: Component
                        metadata:
                            name: llm
                        spec:
                            metadata:
                            - name: cacheTTL
                                value: "500ms"
                        """);

        FileSystemComponentYamlReader reader = new(directory.DirectoryPath);

        reader.TryReadResponseCacheTtl("llm").ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    private static NaturalLanguageDescriptionOptionsValidator BuildValidator(
        string environmentName,
        TimeSpan? ttl)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        IComponentYamlReader yamlReader = Substitute.For<IComponentYamlReader>();
        yamlReader.TryReadResponseCacheTtl(Arg.Any<string>()).Returns(ttl);

        return new NaturalLanguageDescriptionOptionsValidator(
            environment,
            yamlReader,
            NullLogger<NaturalLanguageDescriptionOptionsValidator>.Instance);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _originalValue;

        private EnvironmentVariableScope(string name, string? newValue)
        {
            _name = name;
            _originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, newValue);
        }

        public static EnvironmentVariableScope Set(string name, string value)
            => new(name, value);

        public static EnvironmentVariableScope Clear(string name)
            => new(name, null);

        public void Dispose()
            => Environment.SetEnvironmentVariable(_name, _originalValue);
    }

    private sealed class TemporaryDirectoryScope : IDisposable
    {
        public TemporaryDirectoryScope()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"hexalith-nl-validator-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
