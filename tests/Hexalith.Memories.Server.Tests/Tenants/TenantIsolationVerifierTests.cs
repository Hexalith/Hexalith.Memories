#pragma warning disable CS8620 // Nullability mismatch in NSubstitute Returns() with nullable Dapr client methods.

// <copyright file="TenantIsolationVerifierTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Dapr.Actors;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.TestHelpers.Documentation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class TenantIsolationVerifierTests
{
    private const int VectorDimensions = 768;

    [Theory]
    [InlineData("registry")]
    [InlineData("embeddingConfigProvider")]
    [InlineData("redis")]
    [InlineData("falkorDb")]
    [InlineData("logger")]
    public void Constructor_NullDependency_ThrowsForThatParameter(string parameterName)
    {
        TenantRegistryService registry = new(
            Substitute.For<DaprClient>(),
            Substitute.For<ILogger<TenantRegistryService>>());

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() => new TenantIsolationVerifier(
            parameterName == "registry" ? null! : registry,
            parameterName == "embeddingConfigProvider" ? null! : Substitute.For<ITenantEmbeddingConfigProvider>(),
            parameterName == "redis" ? null! : Substitute.For<IConnectionMultiplexer>(),
            parameterName == "falkorDb" ? null! : Substitute.For<IConnectionMultiplexer>(),
            parameterName == "logger" ? null! : Substitute.For<ILogger<TenantIsolationVerifier>>()));

        exception.ParamName.ShouldBe(parameterName);
    }

    [Fact]
    public async Task VerifyAsync_AllChecksPassed_ReturnsAllPassed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        result.TenantId.ShouldBe("tenant-a");
        result.Checks.ShouldNotBeEmpty();
        result.Summary.ShouldContain("checks passed");
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"validated {VectorDimensions}-dimension configuration");
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData(VectorDimensions, 1536)]
    [InlineData(1536, VectorDimensions)]
    public async Task VerifyAsync_OneSemanticIndexHasWrongDimensions_ReturnsFailed(
        int rawDimensions,
        int naturalLanguageDimensions)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(
            redisDb,
            "tenant-a",
            rawDimensions: rawDimensions,
            naturalLanguageDimensions: naturalLanguageDimensions);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"expected {VectorDimensions} dimensions");
        semanticCheck.Details.ShouldContain("found 1536");
        semanticCheck.Details.ShouldContain(
            rawDimensions == 1536
                ? IndexSchemaDefinitions.GetSemanticIndexName("tenant-a")
                : IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"));
        semanticCheck.Details.ShouldContain(
            $"Raw semantic index '{IndexSchemaDefinitions.GetSemanticIndexName("tenant-a")}' has {rawDimensions} dimensions "
            + $"but natural-language semantic index '{IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")}' has {naturalLanguageDimensions}");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("tenant-a");
        semanticCheck.Remediation.ShouldContain("reindex");
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_EqualIndexDimensionsDifferentFromTenantConfig_ReturnsFailed()
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = CreateEmbeddingConfigProvider(
            EmbeddingProviderDefaults.Google());
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(
            redisDb,
            "tenant-a",
            rawDimensions: 1536,
            naturalLanguageDimensions: 1536);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"expected {VectorDimensions} dimensions");
        semanticCheck.Details.ShouldContain("found 1536");
        semanticCheck.Details.ShouldContain(IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"));
        semanticCheck.Details.ShouldContain(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"));
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("tenant-a");
        semanticCheck.Remediation.ShouldContain("reindex");
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_ConfigRawAndNaturalLanguageDimensionsAllDiffer_ReturnsEveryDiagnostic()
    {
        const int RawDimensions = 1536;
        const int NaturalLanguageDimensions = 3072;
        ITenantEmbeddingConfigProvider embeddingConfigProvider = CreateEmbeddingConfigProvider(
            EmbeddingProviderDefaults.Google());
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(
            redisDb,
            "tenant-a",
            rawDimensions: RawDimensions,
            naturalLanguageDimensions: NaturalLanguageDimensions);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain(
            $"Index '{IndexSchemaDefinitions.GetSemanticIndexName("tenant-a")}' for tenant 'tenant-a' "
            + $"expected {VectorDimensions} dimensions from embedding configuration but found {RawDimensions}");
        semanticCheck.Details.ShouldContain(
            $"Index '{IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")}' for tenant 'tenant-a' "
            + $"expected {VectorDimensions} dimensions from embedding configuration but found {NaturalLanguageDimensions}");
        semanticCheck.Details.ShouldContain(
            $"Raw semantic index '{IndexSchemaDefinitions.GetSemanticIndexName("tenant-a")}' has {RawDimensions} dimensions "
            + $"but natural-language semantic index '{IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")}' has {NaturalLanguageDimensions}");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain($"validated {VectorDimensions}-dimension embedding configuration");
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_UsesRequestedTenantConfigurationOnly()
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", Arg.Any<CancellationToken>())
            .Returns(EmbeddingProviderDefaults.Google());
        embeddingConfigProvider
            .GetAsync("tenant-b", Arg.Any<CancellationToken>())
            .Returns(EmbeddingProviderDefaults.Google() with { Dimensions = 1536 });
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        _ = await embeddingConfigProvider.Received(1).GetAsync("tenant-a", Arg.Any<CancellationToken>());
        _ = await embeddingConfigProvider.Received(1).GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _ = await embeddingConfigProvider.DidNotReceive().GetAsync("tenant-b", Arg.Any<CancellationToken>());
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData("dapr", nameof(Dapr.DaprException))]
    [InlineData("actor", nameof(ActorMethodInvocationException))]
    [InlineData("timeout", nameof(TimeoutException))]
    [InlineData("http", nameof(HttpRequestException))]
    [InlineData("io", nameof(IOException))]
    [InlineData("rpc", nameof(Grpc.Core.RpcException))]
    public async Task VerifyAsync_EmbeddingConfigUnavailable_FailsClosed(
        string failureKind,
        string expectedFailureType)
    {
        Exception exception = failureKind switch
        {
            "dapr" => new Dapr.DaprException("tenant configuration actor unavailable"),
            "actor" => new ActorMethodInvocationException("sensitive actor payload", isTransient: true),
            "timeout" => new TimeoutException("tenant configuration timed out"),
            "http" => new HttpRequestException("tenant configuration transport unavailable"),
            "io" => new IOException("sensitive transport detail"),
            "rpc" => new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, "sensitive transport detail")),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "Unknown failure kind."),
        };
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TenantEmbeddingConfig>(exception));
        CapturingLogger<TenantIsolationVerifier> logger = new();
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider,
            logger);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("unavailable");
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Details.ShouldNotContain(exception.Message);
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("tenant-a");
        semanticCheck.Remediation.ShouldNotContain(exception.Message);
        result.AllPassed.ShouldBeFalse();
        _ = await embeddingConfigProvider.Received(1).GetAsync("tenant-a", Arg.Any<CancellationToken>());
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        result.Checks.Where(check => check.CheckName != "SemanticIsolation").Count(check => check.Passed).ShouldBe(4);
        AssertEmbeddingConfigurationFailureLogged(logger, expectedFailureType, exception.Message);
        if (failureKind is "rpc" or "io")
        {
            semanticCheck.Details.ShouldNotContain("sensitive transport detail");
            semanticCheck.Remediation.ShouldNotContain("sensitive transport detail");
            AssertEmbeddingConfigurationFailureLogged(logger, expectedFailureType, "sensitive transport detail");
        }
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_ValidNonDefaultConfiguredDimensions_AllChecksPass()
    {
        const int ConfiguredDimensions = 1536;
        ITenantEmbeddingConfigProvider embeddingConfigProvider = CreateEmbeddingConfigProvider(
            EmbeddingProviderDefaults.Google() with { Dimensions = ConfiguredDimensions });
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(
            redisDb,
            "tenant-a",
            rawDimensions: ConfiguredDimensions,
            naturalLanguageDimensions: ConfiguredDimensions);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeTrue();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"validated {ConfiguredDimensions}-dimension configuration");
        _ = await embeddingConfigProvider.Received(1).GetAsync("tenant-a", Arg.Any<CancellationToken>());
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_UnrecognizedEmbeddingConfigFailure_Propagates()
    {
        InvalidOperationException exception = new("unexpected provider failure");
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TenantEmbeddingConfig>(exception));
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        InvalidOperationException actual = await Should.ThrowAsync<InvalidOperationException>(
            () => verifier.VerifyAsync("tenant-a", CancellationToken.None));

        actual.ShouldBeSameAs(exception);
        _ = await embeddingConfigProvider.Received(1).GetAsync("tenant-a", Arg.Any<CancellationToken>());
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_NullEmbeddingConfigLookupTask_FailsClosed()
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", Arg.Any<CancellationToken>())
            .Returns(_ => (Task<TenantEmbeddingConfig>)null!);
        CapturingLogger<TenantIsolationVerifier> logger = new();
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider,
            logger);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("unavailable");
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Details.Length.ShouldBeLessThan(256);
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("retry verification");
        result.AllPassed.ShouldBeFalse();
        _ = await embeddingConfigProvider.Received(1).GetAsync("tenant-a", Arg.Any<CancellationToken>());
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertEmbeddingConfigurationFailureLogged(logger, "NullLookupTask");
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_NullEmbeddingConfigResult_FailsClosed()
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantEmbeddingConfig>(null!));
        CapturingLogger<TenantIsolationVerifier> logger = new();
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider,
            logger);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("unavailable");
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Details.ShouldNotContain("dimensions");
        semanticCheck.Details.Length.ShouldBeLessThan(256);
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("retry verification");
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertEmbeddingConfigurationFailureLogged(logger, "NullConfigurationResult");
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(42)]
    public async Task VerifyAsync_InvalidEmbeddingConfigDimensions_FailsClosed(int configuredDimensions)
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = CreateEmbeddingConfigProvider(
            EmbeddingProviderDefaults.Google() with { Dimensions = configuredDimensions });
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("invalid");
        semanticCheck.Details.ShouldContain("invalid in field 'dimensions'");
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Details.ShouldContain($"actual configured dimensions {configuredDimensions}");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("Correct the embedding configuration");
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [MemberData(nameof(InvalidNonProviderEmbeddingConfigurations))]
    public async Task VerifyAsync_InvalidNonProviderEmbeddingConfig_SanitizesEvidence(
        string expectedField,
        TenantEmbeddingConfig invalidConfig)
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = CreateEmbeddingConfigProvider(invalidConfig);
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"invalid in field '{expectedField}'");
        semanticCheck.Details.ShouldContain($"actual configured dimensions {invalidConfig.Dimensions}");
        semanticCheck.Details.Length.ShouldBeLessThan(256);
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("Correct the embedding configuration");
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_InvalidProviderEmbeddingConfig_SanitizesEvidence()
    {
        const string SensitiveProviderValue = "sensitive-provider-payload";
        ITenantEmbeddingConfigProvider embeddingConfigProvider = CreateEmbeddingConfigProvider(
            EmbeddingProviderDefaults.Google() with { Provider = SensitiveProviderValue });
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("invalid in field 'provider'");
        semanticCheck.Details.ShouldContain($"actual configured dimensions {VectorDimensions}");
        semanticCheck.Details.ShouldNotContain(SensitiveProviderValue);
        semanticCheck.Details.Length.ShouldBeLessThan(256);
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("Correct the embedding configuration");
        semanticCheck.Remediation.ShouldNotContain(SensitiveProviderValue);
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_EmbeddingConfigLookupCancelled_PropagatesCancellation()
    {
        using CancellationTokenSource cts = new();
        TaskCompletionSource<bool> providerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<TenantEmbeddingConfig> pendingConfiguration = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", cts.Token)
            .Returns(_ =>
            {
                providerEntered.TrySetResult(true);
                return pendingConfiguration.Task;
            });
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        Task<TenantIsolationVerificationResult> verification = verifier.VerifyAsync("tenant-a", cts.Token);
        _ = await providerEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        cts.Cancel();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() => verification);

        exception.CancellationToken.ShouldBe(cts.Token);
        pendingConfiguration.Task.IsCompleted.ShouldBeFalse();
        pendingConfiguration.TrySetCanceled(cts.Token).ShouldBeTrue();
        pendingConfiguration.Task.IsCanceled.ShouldBeTrue();
        _ = await embeddingConfigProvider.Received(1).GetAsync("tenant-a", cts.Token);
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_EmbeddingConfigLookupDeadline_FailsClosedAndContinuesIndependentChecks()
    {
        FakeTimeProvider time = new();
        TaskCompletionSource<TenantEmbeddingConfig> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantEmbeddingConfigProvider provider = Substitute.For<ITenantEmbeddingConfigProvider>();
        provider.GetAsync("tenant-a", CancellationToken.None).Returns(pending.Task);
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)],
            provider,
            timeProvider: time);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        Task<TenantIsolationVerificationResult> verification = verifier.VerifyAsync("tenant-a", CancellationToken.None);
        _ = await provider.Received(1).GetAsync("tenant-a", CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(4));
        verification.IsCompleted.ShouldBeFalse();

        time.Advance(TimeSpan.FromSeconds(1));
        TenantIsolationVerificationResult result = await verification
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        pending.Task.IsCompleted.ShouldBeFalse();
        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semantic = result.Checks.Single(check => check.CheckName == "SemanticIsolation");
        semantic.Passed.ShouldBeFalse();
        semantic.Details.ShouldBe("Embedding configuration is unavailable for requested tenant 'tenant-a'");
        result.Checks.Where(check => check.CheckName != "SemanticIsolation").Count(check => check.Passed).ShouldBe(4);
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
        _ = await redisDb.Received(4).ExecuteAsync("FT.INFO", Arg.Any<object[]>());
    }

    [Fact]
    public async Task VerifyAsync_ProviderSideCancellationWithoutCallerCancellation_FailsClosed()
    {
        using CancellationTokenSource providerCts = new();
        providerCts.Cancel();
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", CancellationToken.None)
            .Returns(Task.FromCanceled<TenantEmbeddingConfig>(providerCts.Token));
        CapturingLogger<TenantIsolationVerifier> logger = new();
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ],
            embeddingConfigProvider,
            logger);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("unavailable");
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("retry verification");
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertEmbeddingConfigurationFailureLogged(logger, nameof(TaskCanceledException), "A task was canceled.");
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VerifyAsync_MissingSyntacticIndex_RetainsSurvivingKeyFindings(bool scanInterrupted)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string missingIndex = IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a");
        SetupMissingIndex(redisDb, missingIndex);
        string markerKey = IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "survivor");
        string secondKey = IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "second");
        string wrongType = IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "non-hash");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a"), markerKey, wrongType, secondKey);
        redisDb.HashGetAsync((RedisKey)wrongType, "tenantId", CommandFlags.DemandMaster)
            .Throws(RedisExceptionFactory.CreateServerException("WRONGTYPE key is not a hash"));
        SetupTenantIdField(redisDb, markerKey, "tenant-b");
        if (scanInterrupted)
        {
            redisDb.HashGetAsync((RedisKey)secondKey, "tenantId", CommandFlags.DemandMaster)
                .Throws(new RedisTimeoutException(CommandFlags.None, "internal-key-context", CommandStatus.Unknown));
        }
        else
        {
            SetupTenantIdField(redisDb, secondKey, "tenant-a");
        }

        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.Checks.Single(check => check.CheckName == "IndexExistence").Remediation.ShouldNotBeNull()
            .ShouldContain("Review SyntacticIsolation for surviving keys before provisioning");
        TenantIsolationCheckResult syntactic = result.Checks.Single(check => check.CheckName == "SyntacticIsolation");
        syntactic.Passed.ShouldBeFalse();
        syntactic.Details.ShouldNotBeNull();
        syntactic.Details.ShouldContain($"Missing tenant isolation resources: {missingIndex}");
        syntactic.Details.ShouldContain($"key '{markerKey}' under tenant 'tenant-a' has tenantId field 'tenant-b'");
        syntactic.Details.ShouldContain($"key '{wrongType}' under tenant 'tenant-a' has the wrong Redis value type");
        syntactic.Remediation.ShouldNotBeNull();
        syntactic.Remediation.ShouldContain("Verify their provenance");
        syntactic.Remediation.ShouldContain("identify their writer");
        if (scanInterrupted)
        {
            syntactic.Details.ShouldContain("Syntactic key scan incomplete. Backend unavailable");
            syntactic.Details.ShouldNotContain("internal-key-context");
            syntactic.Remediation.ShouldContain("connectivity and retry");
        }
        else
        {
            syntactic.Details.ShouldContain("Examined 3 surviving target-prefix key(s)");
            syntactic.Remediation.ShouldContain("Verify the provenance of the surviving keys before recreating the index");
            syntactic.Remediation.IndexOf("re-provision", StringComparison.Ordinal)
                .ShouldBeGreaterThan(syntactic.Remediation.IndexOf("identify their writer", StringComparison.Ordinal));
        }

        result.Checks.Single(check => check.CheckName == "SemanticIsolation").Passed.ShouldBeTrue();
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_OversizedStoredEvidence_AbbreviatesKeysAndMarkersByUtf8Bytes()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");
        string longId = string.Concat(Enumerable.Repeat("🌍", 100));
        string longMarker = longId + "-foreign";
        string syntacticKey = IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", longId);
        string semanticKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", longId);
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a"), syntacticKey);
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), semanticKey);
        SetupTenantIdField(redisDb, syntacticKey, longMarker);
        SetupSemanticRecord(redisDb, semanticKey, longId, longMarker);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        foreach ((string checkName, string key) in new[] { ("SyntacticIsolation", syntacticKey), ("SemanticIsolation", semanticKey) })
        {
            TenantIsolationCheckResult check = result.Checks.Single(check => check.CheckName == checkName);
            check.Passed.ShouldBeFalse();
            check.Details.ShouldNotBeNull();
            check.Remediation.ShouldNotBeNull().ShouldContain("Match abbreviated key references by the SHA256");
            foreach (string value in new[] { key, longMarker })
            {
                byte[] fullValue = System.Text.Encoding.UTF8.GetBytes(value);
                string digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fullValue));
                string display = check.Details.Split('\'').First(part => part.EndsWith($"[SHA256:{digest}]", StringComparison.Ordinal));
                display.ShouldNotContain("�");
                System.Text.Encoding.UTF8.GetByteCount(display).ShouldBeLessThanOrEqualTo(256);
                check.Details.ShouldNotContain(value);
                check.Remediation.ShouldNotContain(value);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VerifyAsync_EmptyGraphListing_DoesNotReportTenantGraphExists(bool nullArray)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        RedisResult graphList = nullArray
            ? RedisResult.Create((RedisResult[])null!)
            : RedisResult.Create(Array.Empty<RedisResult>());
        falkorDb.ExecuteAsync("GRAPH.LIST").Returns(graphList);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.Checks.Single(check => check.CheckName == "GraphIsolation").Passed.ShouldBeFalse();
        result.Checks.Single(check => check.CheckName == "IndexExistence").Passed.ShouldBeFalse();
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_MissingSyntacticAndSemanticIndexes_ReportsEmptyScanBeforeProvisioning()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupMissingIndex(redisDb, IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a"));
        SetupMissingIndex(redisDb, IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"));
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult syntactic = result.Checks.Single(check => check.CheckName == "SyntacticIsolation");
        syntactic.Passed.ShouldBeFalse();
        syntactic.Details.ShouldNotBeNull().ShouldContain("Examined 0 surviving target-prefix key(s)");
        TenantIsolationCheckResult semantic = result.Checks.Single(check => check.CheckName == "SemanticIsolation");
        semantic.Passed.ShouldBeFalse();
        semantic.Remediation.ShouldNotBeNull().ShouldContain("Review SyntacticIsolation for surviving keys before running tenant provisioning");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_LargeSyntacticScan_BoundsAllThreeCategoriesAndCountsEveryKey()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer server) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupMissingIndex(redisDb, IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a"));
        SetupGraphList(falkorDb, "tenant-a");
        string prefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a");
        List<string> keys = [];
        for (int index = 1; index <= 12; index++)
        {
            string missingKey = $"{prefix}missing-{index:D2}";
            string foreignKey = $"{prefix}foreign-{index:D2}";
            string wrongTypeKey = $"{prefix}non-hash-{index:D2}";
            keys.AddRange([missingKey, foreignKey, wrongTypeKey]);
            SetupTenantIdField(redisDb, missingKey, null);
            SetupTenantIdField(redisDb, foreignKey, "tenant-b");
            redisDb.HashGetAsync((RedisKey)wrongTypeKey, "tenantId", CommandFlags.DemandMaster)
                .Throws(RedisExceptionFactory.CreateServerException("WRONGTYPE key is not a hash"));
        }

        SetupRedisKeyScan(server, prefix, [.. keys]);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult syntactic = result.Checks.Single(check => check.CheckName == "SyntacticIsolation");
        syntactic.Passed.ShouldBeFalse();
        syntactic.Details.ShouldNotBeNull();
        syntactic.Details.ShouldContain("Examined 36 surviving target-prefix key(s)");
        foreach (string category in new[] { "missing-marker", "foreign-marker", "non-hash" })
        {
            syntactic.Details.ShouldContain($"Omitted 2 additional {category} finding(s)");
        }

        foreach (string sampled in keys.Take(30))
        {
            syntactic.Details.ShouldContain(sampled);
        }

        foreach (string omitted in keys.Skip(30))
        {
            syntactic.Details.ShouldNotContain(omitted);
            _ = await redisDb.Received(1).HashGetAsync((RedisKey)omitted, "tenantId", CommandFlags.DemandMaster);
        }

        syntactic.Remediation.ShouldNotBeNull().ShouldContain("Review the remaining findings");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData("tenant-b", "has tenantId field 'tenant-b'")]
    [InlineData(null, "is missing tenantId field")]
    [InlineData(" \t", "is missing tenantId field")]
    public async Task VerifyAsync_SyntacticMarkerDefect_ReportsNamedKeyAndProvenanceGuidance(
        string? storedTenantId,
        string expectedFinding)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string plantedKey = IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "leaked-doc");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a"), plantedKey);
        SetupTenantIdField(redisDb, plantedKey, storedTenantId);
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Passed.ShouldBeFalse();
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain(plantedKey);
        syntacticCheck.Details.ShouldContain(expectedFinding);
        syntacticCheck.Remediation.ShouldNotBeNull();
        syntacticCheck.Remediation.ShouldContain("Inspect and quarantine the keys named in the tenantId findings");
        syntacticCheck.Remediation.ShouldContain("Verify their provenance");
        syntacticCheck.Remediation.ShouldNotContain("remove mismatched");
        syntacticCheck.Remediation.ShouldNotContain("re-provision");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task VerifyAsync_SyntacticWrongType_ReportsKeyAndContinuesCheckingThePrefix(
        bool hasForeignMarker,
        bool hasInvalidIndexPrefix)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");
        string prefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a");
        if (hasInvalidIndexPrefix)
        {
            SetupIndexInfo(
                redisDb,
                IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a"),
                CreateIndexInfo(
                    IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-b"),
                    IndexSchemaDefinitions.GetSyntacticFieldIdentifiers(),
                    1));
        }

        RedisKey wrongTypeKey = prefix + "non-hash";
        string foreignMarkerKey = prefix + "foreign-marker";
        SetupRedisKeyScan(redisServer, prefix, wrongTypeKey.ToString(), foreignMarkerKey);
        redisDb.HashGetAsync(wrongTypeKey, (RedisValue)"tenantId", Arg.Any<CommandFlags>())
            .Returns(Task.FromException<RedisValue>(RedisExceptionFactory.CreateServerException(
                "WRONGTYPE Operation against a key holding the wrong kind of value")));
        SetupTenantIdField(redisDb, foreignMarkerKey, hasForeignMarker ? "tenant-b" : "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult syntactic = result.Checks.Single(check => check.CheckName == "SyntacticIsolation");
        syntactic.Passed.ShouldBeFalse();
        syntactic.Details.ShouldNotBeNull();
        syntactic.Details.ShouldContain($"key '{wrongTypeKey}' under tenant 'tenant-a' has the wrong Redis value type");
        if (hasForeignMarker)
        {
            syntactic.Details.ShouldContain($"key '{foreignMarkerKey}' under tenant 'tenant-a' has tenantId field 'tenant-b'");
        }
        else
        {
            syntactic.Details.ShouldNotContain(foreignMarkerKey);
        }

        syntactic.Details.ShouldNotContain("Backend unavailable");
        syntactic.Remediation.ShouldNotBeNull();
        syntactic.Remediation.ShouldContain("Inspect the reported non-hash keys");
        syntactic.Remediation.Contains("Verify their provenance", StringComparison.Ordinal).ShouldBe(hasForeignMarker);
        syntactic.Remediation.Contains("re-provision", StringComparison.Ordinal).ShouldBe(hasInvalidIndexPrefix);
        syntactic.Remediation.ShouldNotContain("remove mismatched");
        result.AllPassed.ShouldBeFalse();
        _ = await redisDb.Received(1).HashGetAsync((RedisKey)foreignMarkerKey, (RedisValue)"tenantId", Arg.Any<CommandFlags>());
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_PendingSyntacticHashRead_StopsWaitingWhenCancelled()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<RedisValue> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");
        string prefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a");
        string blockedKey = prefix + "blocked";
        string laterKey = prefix + "later";
        SetupRedisKeyScan(redisServer, prefix, blockedKey, laterKey);
        TaskCompletionSource readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        redisDb.HashGetAsync((RedisKey)blockedKey, (RedisValue)"tenantId", Arg.Any<CommandFlags>())
            .Returns(_ =>
            {
                readStarted.SetResult();
                return pending.Task;
            });

        Task<TenantIsolationVerificationResult> verification = verifier.VerifyAsync("tenant-a", cancellation.Token);
        await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        cancellation.Cancel();

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(() =>
            verification.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        error.CancellationToken.ShouldBe(cancellation.Token);
        pending.Task.IsCompleted.ShouldBeFalse();
        _ = await redisDb.DidNotReceive().HashGetAsync((RedisKey)laterKey, Arg.Any<RedisValue>(), Arg.Any<CommandFlags>());
        _ = await falkorDb.Received(1).ExecuteAsync("GRAPH.LIST");
    }

    [Fact]
    public async Task VerifyAsync_DetectsSemanticTenantIdMismatch_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string plantedKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "leaked-vec");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), plantedKey);
        SetupSemanticRecord(redisDb, plantedKey, "leaked-vec", "tenant-b");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("tenant-b");
        semanticCheck.Details.ShouldContain("confirmed marker mismatch (possible contamination)");
        semanticCheck.Details.ShouldContain("expected tenant 'tenant-a', observed tenant 'tenant-b'");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldBe(
            $"For the confirmed marker mismatch (possible contamination) on '{plantedKey}', inspect and quarantine the named key(s), then run tenant-scoped marker repair or reindex for tenant 'tenant-a' only after provenance is verified — never delete the prefix.");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
    }

    [Fact]
    public async Task VerifyAsync_DetectsNaturalLanguageSemanticTenantIdMismatch_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string plantedKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "leaked-nl");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a"), plantedKey);
        SetupSemanticRecord(
            redisDb,
            plantedKey,
            "leaked-nl",
            "tenant-b",
            hasNaturalLanguageDescription: true);
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("natural-language semantic");
        semanticCheck.Details.ShouldContain("tenant-b");
    }

    [Fact]
    public async Task VerifyAsync_DetectsMissingSemanticTenantId_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string plantedKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "missing-tenant-marker");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), plantedKey);
        SetupSemanticRecord(redisDb, plantedKey, "missing-tenant-marker", tenantId: null);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("is missing its tenantId marker: incomplete evidence, not confirmed cross-tenant leakage");
        semanticCheck.Details.ShouldNotContain("confirmed marker mismatch");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldBe(
            $"For the incomplete evidence (missing marker, not confirmed leakage) on '{plantedKey}', inspect and quarantine the named key(s) before any tenant-scoped marker repair or reindex for tenant 'tenant-a', applied only after provenance is verified — never delete the prefix.");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
    }

    [Theory]
    [InlineData(nameof(SemanticKeyFamily.ActiveRawBase), null)]
    [InlineData(nameof(SemanticKeyFamily.ActiveRawBase), "")]
    [InlineData(nameof(SemanticKeyFamily.ActiveRawBase), " \t")]
    [InlineData(nameof(SemanticKeyFamily.ActiveRawBase), "tenant-b")]
    [InlineData(nameof(SemanticKeyFamily.ActiveRawChunk), null)]
    [InlineData(nameof(SemanticKeyFamily.ActiveRawChunk), "\u00A0")]
    [InlineData(nameof(SemanticKeyFamily.ActiveRawChunk), "tenant-b")]
    [InlineData(nameof(SemanticKeyFamily.ActiveNaturalLanguage), null)]
    [InlineData(nameof(SemanticKeyFamily.ActiveNaturalLanguage), "\t")]
    [InlineData(nameof(SemanticKeyFamily.ActiveNaturalLanguage), "tenant-b")]
    public async Task VerifyAsync_ActiveFamilyMarkerEvidence_MissingAndForeignFailClosed(
        string familyName,
        string? storedTenantId)
    {
        SemanticKeyFamily family = Enum.Parse<SemanticKeyFamily>(familyName);
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        const string MemoryUnitId = "marker-matrix";
        string key = family switch
        {
            SemanticKeyFamily.ActiveRawBase => IndexSchemaDefinitions.BuildSemanticKey("tenant-a", MemoryUnitId),
            SemanticKeyFamily.ActiveRawChunk => IndexSchemaDefinitions.BuildSemanticChunkKey("tenant-a", MemoryUnitId, 3),
            SemanticKeyFamily.ActiveNaturalLanguage => IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", MemoryUnitId),
            _ => throw new InvalidOperationException($"Unexpected active family '{family}'."),
        };
        string keyPrefix = family == SemanticKeyFamily.ActiveNaturalLanguage
            ? IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a")
            : IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a");
        SetupRedisKeyScan(redisServer, keyPrefix, key);
        SetupSemanticRecord(
            redisDb,
            key,
            MemoryUnitId,
            storedTenantId,
            hasNaturalLanguageDescription: family == SemanticKeyFamily.ActiveNaturalLanguage,
            chunkSequence: family == SemanticKeyFamily.ActiveRawChunk ? 3 : null,
            chunkStartOffset: family == SemanticKeyFamily.ActiveRawChunk ? 0 : null,
            chunkEndOffset: family == SemanticKeyFamily.ActiveRawChunk ? 12 : null);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain(family switch
        {
            SemanticKeyFamily.ActiveRawBase => "raw semantic base",
            SemanticKeyFamily.ActiveRawChunk => "raw semantic chunk",
            SemanticKeyFamily.ActiveNaturalLanguage => "natural-language semantic",
            _ => throw new InvalidOperationException($"Unexpected active family '{family}'."),
        });
        semanticCheck.Details.ShouldContain(string.IsNullOrWhiteSpace(storedTenantId)
            ? "is missing its tenantId marker: incomplete evidence, not confirmed cross-tenant leakage"
            : "has a foreign tenantId marker 'tenant-b': confirmed marker mismatch (possible contamination)");
        semanticCheck.Details.ShouldNotContain("evidence-classification gap");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain(string.IsNullOrWhiteSpace(storedTenantId)
            ? "incomplete evidence (missing marker, not confirmed leakage)"
            : "confirmed marker mismatch (possible contamination)");
        semanticCheck.Remediation.ShouldContain($"'{key}'");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_MissingActiveMarker_ReturnsIncompleteEvidenceWithoutDeleteGuidance()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string plantedKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "incomplete-evidence");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), plantedKey);
        SetupSemanticRecord(redisDb, plantedKey, "incomplete-evidence", tenantId: null);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"key '{plantedKey}' under tenant 'tenant-a' is missing its tenantId marker: incomplete evidence, not confirmed cross-tenant leakage");
        semanticCheck.Details.ShouldNotContain("confirmed marker mismatch");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain($"'{plantedKey}'");
        semanticCheck.Remediation.ShouldContain("incomplete evidence (missing marker, not confirmed leakage)");
        semanticCheck.Remediation.ShouldNotContain("confirmed marker mismatch");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string plantedKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "possible-contamination");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), plantedKey);
        SetupSemanticRecord(redisDb, plantedKey, "possible-contamination", "tenant-b");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"key '{plantedKey}' under tenant 'tenant-a' has a foreign tenantId marker 'tenant-b': confirmed marker mismatch (possible contamination)");
        semanticCheck.Details.ShouldContain("expected tenant 'tenant-a', observed tenant 'tenant-b'");
        semanticCheck.Details.ShouldNotContain("incomplete evidence");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain($"'{plantedKey}'");
        semanticCheck.Remediation.ShouldContain("confirmed marker mismatch (possible contamination)");
        semanticCheck.Remediation.ShouldNotContain("incomplete evidence");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_MixedMissingAndForeignActiveMarkers_PreservesBothDistinctDiagnoses()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string missingKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mixed-missing");
        string foreignKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mixed-foreign");
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            missingKey,
            foreignKey);
        SetupSemanticRecord(redisDb, missingKey, "mixed-missing", tenantId: null);
        SetupSemanticRecord(redisDb, foreignKey, "mixed-foreign", "tenant-b");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"key '{missingKey}' under tenant 'tenant-a' is missing its tenantId marker: incomplete evidence, not confirmed cross-tenant leakage");
        semanticCheck.Details.ShouldContain($"key '{foreignKey}' under tenant 'tenant-a' has a foreign tenantId marker 'tenant-b': confirmed marker mismatch (possible contamination)");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain($"'{missingKey}'");
        semanticCheck.Remediation.ShouldContain($"'{foreignKey}'");
        semanticCheck.Remediation.ShouldContain("confirmed marker mismatch (possible contamination)");
        semanticCheck.Remediation.ShouldContain("incomplete evidence (missing marker, not confirmed leakage)");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_TwoForeignActiveMarkersSameCheck_JoinsBothKeysInOneSentence()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string firstForeignKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "foreign-one");
        string secondForeignKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "foreign-two");
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            firstForeignKey,
            secondForeignKey);
        SetupSemanticRecord(redisDb, firstForeignKey, "foreign-one", "tenant-b");
        SetupSemanticRecord(redisDb, secondForeignKey, "foreign-two", "tenant-b");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldBe(
            $"For the confirmed marker mismatch (possible contamination) on '{firstForeignKey}', '{secondForeignKey}', inspect and quarantine the named key(s), then run tenant-scoped marker repair or reindex for tenant 'tenant-a' only after provenance is verified — never delete the prefix.");
        semanticCheck.Remediation.ShouldNotContain("incomplete evidence");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_TwoMissingActiveMarkersSameCheck_JoinsBothKeysInOneSentence()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string firstMissingKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "missing-one");
        string secondMissingKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "missing-two");
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            firstMissingKey,
            secondMissingKey);
        SetupSemanticRecord(redisDb, firstMissingKey, "missing-one", tenantId: null);
        SetupSemanticRecord(redisDb, secondMissingKey, "missing-two", tenantId: null);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldBe(
            $"For the incomplete evidence (missing marker, not confirmed leakage) on '{firstMissingKey}', '{secondMissingKey}', inspect and quarantine the named key(s) before any tenant-scoped marker repair or reindex for tenant 'tenant-a', applied only after provenance is verified — never delete the prefix.");
        semanticCheck.Remediation.ShouldNotContain("confirmed marker mismatch");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_DimensionMismatchAndForeignActiveMarkerSameCheck_ComposesBothRemediationSentences()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a", rawDimensions: 1536);
        string plantedKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "dimension-and-marker");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), plantedKey);
        SetupSemanticRecord(redisDb, plantedKey, "dimension-and-marker", "tenant-b");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"expected {VectorDimensions} dimensions");
        semanticCheck.Details.ShouldContain($"key '{plantedKey}'");
        semanticCheck.Remediation.ShouldNotBeNull();

        // Both the non-marker (dimension) sentence and the marker-specific sentence must be composed together,
        // since a single check can surface both problem kinds at once.
        semanticCheck.Remediation.ShouldContain(
            $"Repair or re-provision tenant 'tenant-a' Redis Vector indexes and reindex its semantic data using the validated {VectorDimensions}-dimension embedding configuration.");
        semanticCheck.Remediation.ShouldContain(
            $"For the confirmed marker mismatch (possible contamination) on '{plantedKey}', inspect and quarantine the named key(s), then run tenant-scoped marker repair or reindex for tenant 'tenant-a' only after provenance is verified — never delete the prefix.");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_DimensionMismatchWithForeignAndMissingActiveMarkersSameCheck_ComposesAllThreeRemediationSentences()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a", rawDimensions: 1536);
        string foreignKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "triple-foreign");
        string missingKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "triple-missing");
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            foreignKey,
            missingKey);
        SetupSemanticRecord(redisDb, foreignKey, "triple-foreign", "tenant-b");
        SetupSemanticRecord(redisDb, missingKey, "triple-missing", tenantId: null);
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Remediation.ShouldNotBeNull();

        // All three problem kinds (non-marker dimension mismatch, a foreign marker, and a missing marker) can
        // surface in the same check at once; every sentence must be composed together, not collapsed to one.
        semanticCheck.Remediation.ShouldContain(
            $"Repair or re-provision tenant 'tenant-a' Redis Vector indexes and reindex its semantic data using the validated {VectorDimensions}-dimension embedding configuration.");
        semanticCheck.Remediation.ShouldContain(
            $"For the confirmed marker mismatch (possible contamination) on '{foreignKey}', inspect and quarantine the named key(s), then run tenant-scoped marker repair or reindex for tenant 'tenant-a' only after provenance is verified — never delete the prefix.");
        semanticCheck.Remediation.ShouldContain(
            $"For the incomplete evidence (missing marker, not confirmed leakage) on '{missingKey}', inspect and quarantine the named key(s) before any tenant-scoped marker repair or reindex for tenant 'tenant-a', applied only after provenance is verified — never delete the prefix.");
        semanticCheck.Remediation.ShouldNotContain("remove mismatched target-prefix hashes");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_ForeignActiveMarkerWithExtraPayloadFields_DoesNotExposePayloadContent()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string prefix = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a");
        string plantedKey = prefix + "payload-safety";
        const string SensitivePayloadContent = "SENSITIVE-DOCUMENT-BODY-must-not-leak-into-diagnostics";
        SetupRedisKeyScan(redisServer, prefix, plantedKey);
        SetupSemanticRecord(
            redisDb,
            plantedKey,
            "payload-safety",
            "tenant-b",
            hasNaturalLanguageDescription: true,
            sensitivePayloadContent: SensitivePayloadContent);
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"'{plantedKey}'");
        semanticCheck.Details.ShouldContain("tenant-b");
        semanticCheck.Details.ShouldNotContain(SensitivePayloadContent);
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain($"'{plantedKey}'");
        semanticCheck.Remediation.ShouldNotContain(SensitivePayloadContent);
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_MarkerlessStagingAndLegacyKeys_DoNotReportActiveMismatch()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string opaqueActiveRawId = "staging:run-1:active-raw";
        string activeRaw = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", opaqueActiveRawId);
        string activeChunk = IndexSchemaDefinitions.BuildSemanticChunkKey("tenant-a", "nl:active-chunk:9", 3);
        string rawStaging = IndexSchemaDefinitions.BuildSemanticStagingKey("tenant-a", "run-1", "staged-raw");
        string legacyNaturalLanguage = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "legacy-nl");
        string activeNaturalLanguage = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(
            "tenant-a",
            "staging:run-1:active-nl");
        string naturalLanguageStaging = IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey(
            "tenant-a",
            "run-1",
            "staged-nl");
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            activeRaw,
            activeChunk,
            rawStaging,
            legacyNaturalLanguage);
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a"),
            activeNaturalLanguage,
            naturalLanguageStaging);
        SetupSemanticRecord(redisDb, activeRaw, opaqueActiveRawId, "tenant-a");
        SetupSemanticRecord(
            redisDb,
            activeChunk,
            "nl:active-chunk:9",
            "tenant-a",
            chunkSequence: 3,
            chunkStartOffset: 0,
            chunkEndOffset: 12);
        SetupSemanticRecord(redisDb, rawStaging, "staged-raw", tenantId: null);
        SetupSemanticRecord(
            redisDb,
            legacyNaturalLanguage,
            "legacy-nl",
            tenantId: null,
            hasNaturalLanguageDescription: true);
        SetupSemanticRecord(
            redisDb,
            activeNaturalLanguage,
            "staging:run-1:active-nl",
            "tenant-a",
            hasNaturalLanguageDescription: true);
        SetupSemanticRecord(
            redisDb,
            naturalLanguageStaging,
            "staged-nl",
            tenantId: null,
            hasNaturalLanguageDescription: true);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeTrue();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("2 raw base/chunk");
        semanticCheck.Details.ShouldContain("1 current natural-language");
        semanticCheck.Details.ShouldContain("excluding 3 proven non-active");
        semanticCheck.Details.ShouldNotContain("is missing its tenantId marker");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_UnregisteredSemanticShape_ReportsClassificationGapNotMarkerMismatch()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        const string FutureKey = "tenant-a:vec:future:run-1:mu-1";
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), FutureKey);
        SetupSemanticRecord(redisDb, FutureKey, "mu-1", tenantId: null);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("evidence-classification gap (unknown)");
        semanticCheck.Details.ShouldNotContain("is missing its tenantId marker");
        semanticCheck.Remediation.ShouldBe(
            "Register or migrate the reported semantic key family for tenant 'tenant-a', then retry verification; no data or indexes were changed");
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData(VectorDimensions)]
    [InlineData(1536)]
    public async Task VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesAllRemediation(int rawDimensions)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a", rawDimensions: rawDimensions);
        const string FutureKey = "tenant-a:vec:future:run-1:mu-1";
        string activeChunk = IndexSchemaDefinitions.BuildSemanticChunkKey("tenant-a", "active-chunk", 2);
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            FutureKey,
            activeChunk);
        SetupSemanticRecord(redisDb, FutureKey, "mu-1", tenantId: null);
        SetupSemanticRecord(
            redisDb,
            activeChunk,
            "active-chunk",
            "tenant-b",
            chunkSequence: 2,
            chunkStartOffset: 0,
            chunkEndOffset: 12);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"Semantic key '{FutureKey}' under tenant 'tenant-a' has an evidence-classification gap (unknown)");
        semanticCheck.Details.ShouldContain($"raw semantic chunk key '{activeChunk}' under tenant 'tenant-a' has a foreign tenantId marker 'tenant-b': confirmed marker mismatch (possible contamination)");

        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("Register or migrate the reported semantic key family for tenant 'tenant-a'");
        semanticCheck.Remediation.ShouldContain($"on '{activeChunk}', inspect and quarantine");
        semanticCheck.Remediation.ShouldContain("after provenance is verified");
        semanticCheck.Remediation.Contains($"validated {VectorDimensions}-dimension", StringComparison.Ordinal)
            .ShouldBe(rawDimensions != VectorDimensions);
        if (rawDimensions != VectorDimensions)
        {
            semanticCheck.Remediation.IndexOf("Repair or re-provision", StringComparison.Ordinal)
                .ShouldBeGreaterThan(semanticCheck.Remediation.IndexOf("Register or migrate", StringComparison.Ordinal));
        }

        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_WrongTypeSemanticDiscriminatorRead_ReportsClassificationGapAndContinues()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string wrongTypeKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "wrong-type");
        string healthyKey = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "healthy");
        SetupRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            wrongTypeKey,
            healthyKey);
        ExpectSemanticRead(redisDb, wrongTypeKey)
            .Throws(RedisExceptionFactory.CreateServerException("WRONGTYPE Operation against a key holding the wrong kind of value"));

        SetupSemanticRecord(redisDb, healthyKey, "healthy", "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain($"Semantic key '{wrongTypeKey}' under tenant 'tenant-a' has an evidence-classification gap (wrong Redis value type)");
        semanticCheck.Details.ShouldNotContain("Backend unavailable");
        semanticCheck.Details.ShouldNotContain("is missing its tenantId marker");
        await ExpectSemanticRead(redisDb.Received(1), healthyKey);
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_NaturalLanguageScanTimeout_RetainsAllPreviouslyCollectedEvidence()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");
        string rawPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a");
        string naturalLanguagePrefix = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a");
        string foreignMarkerKey = rawPrefix + "foreign-marker";
        string unknownShapeKey = rawPrefix + "unknown:family";
        string naturalLanguageMarkerKey = naturalLanguagePrefix + "nl-foreign";
        string failingKey = naturalLanguagePrefix + "failing";
        SetupRedisKeyScan(redisServer, rawPrefix, foreignMarkerKey, unknownShapeKey);
        SetupRedisKeyScan(redisServer, naturalLanguagePrefix, naturalLanguageMarkerKey, failingKey);
        SetupSemanticRecord(redisDb, foreignMarkerKey, "foreign-marker", "tenant-b");
        SetupSemanticRecord(redisDb, unknownShapeKey, "different-unit", "tenant-a");
        SetupSemanticRecord(redisDb, naturalLanguageMarkerKey, "nl-foreign", "tenant-b", hasNaturalLanguageDescription: true);
        ExpectSemanticRead(redisDb, failingKey)
            .Returns(Task.FromException<RedisResult>(new RedisTimeoutException(CommandFlags.None, "internal-key-context", CommandStatus.Unknown)));

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semantic = result.Checks.Single(check => check.CheckName == "SemanticIsolation");
        semantic.Passed.ShouldBeFalse();
        semantic.Details.ShouldNotBeNull();
        semantic.Details.ShouldContain($"key '{foreignMarkerKey}' under tenant 'tenant-a' has a foreign tenantId marker 'tenant-b'");
        semantic.Details.ShouldContain($"key '{unknownShapeKey}' under tenant 'tenant-a' has an evidence-classification gap");
        semantic.Details.ShouldContain($"key '{naturalLanguageMarkerKey}' under tenant 'tenant-a' has a foreign tenantId marker 'tenant-b'");
        semantic.Details.ShouldContain("Natural-language semantic key scan incomplete. Backend unavailable: storage command timed out");
        semantic.Details.ShouldNotContain("internal-key-context");
        semantic.Remediation.ShouldBe("Check Redis/FalkorDB connectivity and retry");
        result.AllPassed.ShouldBeFalse();
        result.Checks.Single(check => check.CheckName == "GraphIsolation").Passed.ShouldBeTrue();
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task VerifyAsync_LargeSemanticScan_BoundsEachEvidenceCategoryAndKeepsScanning(
        bool interrupted,
        bool hasClassificationGaps)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");
        string rawPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a");
        string naturalLanguagePrefix = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a");
        List<string> rawKeys = [];
        for (int i = 1; i <= 12; i++)
        {
            string foreignUnit = $"foreign-{i:D2}";
            string foreignKey = rawPrefix + foreignUnit;
            string missingUnit = $"missing-{i:D2}";
            string missingKey = rawPrefix + missingUnit;
            rawKeys.AddRange([foreignKey, missingKey]);
            SetupSemanticRecord(redisDb, foreignKey, foreignUnit, "tenant-b");
            SetupSemanticRecord(redisDb, missingKey, missingUnit, tenantId: null);
            if (hasClassificationGaps)
            {
                string gapKey = $"{rawPrefix}gap:{i:D2}";
                rawKeys.Add(gapKey);
                SetupSemanticRecord(redisDb, gapKey, "different-unit", "tenant-a");
            }
        }

        SetupRedisKeyScan(redisServer, rawPrefix, [.. rawKeys]);
        string finalKey = naturalLanguagePrefix + "last-foreign";
        SetupSemanticRecord(redisDb, finalKey, "last-foreign", "tenant-b", hasNaturalLanguageDescription: true);
        if (interrupted)
        {
            string failingKey = naturalLanguagePrefix + "failing";
            SetupRedisKeyScan(redisServer, naturalLanguagePrefix, finalKey, failingKey);
            ExpectSemanticRead(redisDb, failingKey)
                .Throws(new RedisTimeoutException(CommandFlags.None, "internal-key-context", CommandStatus.Unknown));
        }
        else
        {
            SetupRedisKeyScan(redisServer, naturalLanguagePrefix, finalKey);
        }

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult semantic = result.Checks.Single(check => check.CheckName == "SemanticIsolation");
        semantic.Passed.ShouldBeFalse();
        semantic.Details.ShouldNotBeNull();
        string[] details = semantic.Details.Split("; ", StringSplitOptions.None);
        details.Count(detail => detail.Contains("foreign tenantId marker", StringComparison.Ordinal)).ShouldBe(10);
        details.Count(detail => detail.Contains("missing its tenantId marker", StringComparison.Ordinal)).ShouldBe(10);
        details.Count(detail => detail.Contains("has an evidence-classification gap", StringComparison.Ordinal))
            .ShouldBe(hasClassificationGaps ? 10 : 0);
        details.ShouldContain("Omitted 3 additional foreign-marker finding(s)");
        details.ShouldContain("Omitted 2 additional missing-marker finding(s)");
        details.Contains("Omitted 2 additional classification-gap finding(s)", StringComparer.Ordinal)
            .ShouldBe(hasClassificationGaps);
        semantic.Details.ShouldNotContain(finalKey);
        if (!interrupted)
        {
            semantic.Remediation.ShouldNotBeNull();
            semantic.Remediation.ShouldContain("including those omitted from the sample");
            semantic.Remediation.ShouldContain(rawPrefix + "foreign-01");
            semantic.Remediation.ShouldContain(rawPrefix + "missing-01");
            semantic.Remediation.ShouldNotContain(rawPrefix + "foreign-11");
            semantic.Remediation.ShouldNotContain(rawPrefix + "missing-11");
            semantic.Remediation.ShouldNotContain(finalKey);
        }

        semantic.Details.Contains("Natural-language semantic key scan incomplete", StringComparison.Ordinal).ShouldBe(interrupted);
        _ = await ExpectSemanticRead(redisDb.Received(1), finalKey);
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_SemanticCursorMoveNextCancelled_PropagatesAndStopsLaterReads()
    {
        using CancellationTokenSource cts = new();
        TaskCompletionSource cursorMoveNextEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseCursor = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupPendingRedisKeyScan(
            redisServer,
            IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"),
            cursorMoveNextEntered,
            releaseCursor);

        Task<TenantIsolationVerificationResult> verification = verifier.VerifyAsync("tenant-a", cts.Token);
        await cursorMoveNextEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        cts.Cancel();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() => verification);

        exception.CancellationToken.ShouldBe(cts.Token);
        await redisDb.DidNotReceive().ScriptEvaluateAsync(
            Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
        await redisDb.DidNotReceive().HashExistsAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<CommandFlags>());
        _ = redisServer.DidNotReceive().KeysAsync(
            Arg.Any<int>(),
            Arg.Is<RedisValue>(pattern => string.Equals(
                pattern.ToString(),
                IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a") + "*",
                StringComparison.Ordinal)),
            Arg.Any<int>(),
            Arg.Any<long>(),
            Arg.Any<int>(),
            Arg.Any<CommandFlags>());
        await falkorDb.Received(1).ExecuteAsync(
            Arg.Is("GRAPH.LIST"),
            Arg.Is<object[]>(arguments => arguments.Length == 0));
        releaseCursor.TrySetResult().ShouldBeTrue();
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_SemanticDiscriminatorReadCancelled_PropagatesAndStopsLaterReads()
    {
        using CancellationTokenSource cts = new();
        TaskCompletionSource discriminatorReadEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<RedisResult> pendingDiscriminatorRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string activeRaw = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-1");
        SetupRedisKeyScan(redisServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), activeRaw);
        ExpectSemanticRead(redisDb, activeRaw)
            .Returns(_ =>
            {
                discriminatorReadEntered.TrySetResult();
                return pendingDiscriminatorRead.Task;
            });
        SetupGraphList(falkorDb, "tenant-a");

        Task<TenantIsolationVerificationResult> verification = verifier.VerifyAsync("tenant-a", cts.Token);
        await discriminatorReadEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        cts.Cancel();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() => verification);

        exception.CancellationToken.ShouldBe(cts.Token);
        pendingDiscriminatorRead.Task.IsCompleted.ShouldBeFalse();
        await redisDb.DidNotReceive().HashExistsAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<CommandFlags>());
        pendingDiscriminatorRead.TrySetCanceled(cts.Token).ShouldBeTrue();
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_DuplicateSemanticDiscoveryAcrossEndpoints_EvaluatesMarkerOnce()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer firstServer, IServer secondServer) = CreateVerifierWithTwoRedisServers(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string activeRaw = IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-1");
        SetupRedisKeyScan(firstServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), activeRaw);
        SetupRedisKeyScan(secondServer, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), activeRaw);
        SetupSemanticRecord(redisDb, activeRaw, "mu-1", "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("1 raw base/chunk");
        await ExpectSemanticRead(redisDb.Received(1), activeRaw);
        await redisDb.DidNotReceive().HashExistsAsync(
            Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task VerifyAsync_MultipleRedisEndpoints_ScansAllConnectedPrimaries()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer firstServer, IServer secondServer) = CreateVerifierWithTwoRedisServers(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        string firstKey = IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "first");
        string secondKey = IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "second");
        SetupRedisKeyScan(firstServer, IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a"), firstKey);
        SetupRedisKeyScan(secondServer, IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a"), secondKey);
        SetupTenantIdField(redisDb, firstKey, "tenant-a");
        SetupTenantIdField(redisDb, secondKey, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain("2 target-prefix hash(es)");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyAsync_RedisReplica_DisregardsReplicaKeysAndRequiresConnectedPrimary(bool primaryConnected)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer primary, IServer replica) =
            CreateVerifierWithTwoRedisServers(
                [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        primary.IsConnected.Returns(primaryConnected);
        replica.IsReplica.Returns(true);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");
        string prefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a");
        string primaryKey = prefix + "primary";
        string replicaKey = prefix + "stale-replica";
        SetupRedisKeyScan(primary, prefix, primaryKey);
        SetupRedisKeyScan(replica, prefix, replicaKey);
        SetupTenantIdField(redisDb, primaryKey, "tenant-a");
        SetupTenantIdField(redisDb, replicaKey, "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult syntactic = result.Checks.Single(check => check.CheckName == "SyntacticIsolation");
        TenantIsolationCheckResult semantic = result.Checks.Single(check => check.CheckName == "SemanticIsolation");
        result.AllPassed.ShouldBe(primaryConnected);
        syntactic.Passed.ShouldBe(primaryConnected);
        semantic.Passed.ShouldBe(primaryConnected);
        if (primaryConnected)
        {
            syntactic.Details!.ShouldContain("1 target-prefix hash(es)");
        }
        else
        {
            syntactic.Details!.ShouldContain("Backend unavailable");
            semantic.Details!.ShouldContain("Backend unavailable");
        }

        _ = replica.DidNotReceive().KeysAsync(
            Arg.Any<int>(), Arg.Any<RedisValue>(), Arg.Any<int>(),
            Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>());
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_PrimaryLostAfterEnumeration_ReportsUnavailableMarkerRead()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer primary) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");
        string prefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a");
        RedisKey key = prefix + "primary-lost";
        SetupRedisKeyScan(primary, prefix, key.ToString());
        redisDb.HashGetAsync(key, (RedisValue)"tenantId", CommandFlags.DemandMaster)
            .Throws(new RedisConnectionException(
                ConnectionFailureType.UnableToConnect, CommandFlags.DemandMaster, "Primary disappeared"));

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult syntactic = result.Checks.Single(check => check.CheckName == "SyntacticIsolation");
        syntactic.Passed.ShouldBeFalse();
        syntactic.Details.ShouldNotBeNull();
        syntactic.Details.ShouldContain("Backend unavailable: storage connection failed");
        result.Checks.Single(check => check.CheckName == "GraphIsolation").Passed.ShouldBeTrue();
        AssertNoMutationDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_MissingPeerGraphDatabase_DoesNotFailTargetGraphIsolation()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        TenantIsolationCheckResult graphCheck = result.Checks.First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeTrue();
        graphCheck.Details.ShouldNotBeNull();
        graphCheck.Details.ShouldContain("tenant-a");
    }

    [Fact]
    public async Task VerifyAsync_DetectsOrphanedDatabases_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        // GRAPH.LIST returns an extra database not in registry
        SetupGraphList(falkorDb, "tenant-a", "ghost-tenant");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult orphanCheck = result.Checks.First(c => c.CheckName == "OrphanedDatabases");
        orphanCheck.Passed.ShouldBeFalse();
        orphanCheck.Details.ShouldNotBeNull();
        orphanCheck.Details.ShouldContain("ghost-tenant");
        orphanCheck.Remediation.ShouldNotBeNull();
    }

    [Fact]
    public async Task VerifyAsync_DoesNotEmitInputValidationCheck()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.Checks.ShouldNotContain(c => c.CheckName == "InputValidation");
    }

    [Fact]
    public async Task VerifyAsync_ManyPeerTenants_DoesNotIssueSearchScans()
    {
        List<TenantInfo> tenants =
        [
            new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
        ];
        tenants.AddRange(Enumerable.Range(0, 50)
            .Select(i => new TenantInfo($"tenant-peer-{i}", $"Peer {i}", TenantStatus.Active, DateTimeOffset.UtcNow)));

        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(tenants);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        _ = redisDb.DidNotReceive().ExecuteAsync(Arg.Is("FT.SEARCH"), Arg.Any<object[]>());
    }

    [Fact]
    public async Task VerifyAsync_IncludesPerCheckTiming()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        foreach (TenantIsolationCheckResult check in result.Checks)
        {
            check.DurationMs.ShouldBeGreaterThanOrEqualTo(0.0);
        }
    }

    [Fact]
    public async Task VerifyAsync_AllPassedFalseWhenAnyCheckFails()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        // Make index existence fail
        redisDb.ExecuteAsync(Arg.Is("FT.INFO"), Arg.Any<object[]>())
            .Throws(Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Unknown index name"));
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        result.Summary.ShouldContain("failed");
    }

    [Fact]
    public async Task VerifyAsync_BackendUnavailable_ReturnsFailedCheckNotException()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        // Simulate Redis connection failure
        redisDb.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "Connection refused"));
        falkorDb.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "Connection refused"));

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        result.Checks.ShouldNotBeEmpty();

        // Backend-dependent checks should fail with "Backend unavailable"
        string[] backendChecks = ["IndexExistence", "SyntacticIsolation", "SemanticIsolation", "GraphIsolation", "OrphanedDatabases"];
        foreach (string checkName in backendChecks)
        {
            TenantIsolationCheckResult check = result.Checks.First(c => c.CheckName == checkName);
            check.Passed.ShouldBeFalse();
            check.Details.ShouldNotBeNull();
            check.Details.ShouldContain("Backend unavailable");
            if (string.Equals(checkName, "GraphIsolation", StringComparison.Ordinal))
            {
                check.Details.ShouldStartWith("Structural database-existence evidence only");
            }

            check.Remediation.ShouldNotBeNull();
        }
    }

    [Theory]
    [InlineData(false, "storage command timed out")]
    [InlineData(true, "storage connection failed")]
    public async Task VerifyAsync_CommandTimeouts_ReportEveryAffectedCheckWithoutDriverDetails(
        bool disconnectedBacklog,
        string expectedReason)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        Exception error = disconnectedBacklog
            ? new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Backlog timed out: internal-key-context")
            : new RedisTimeoutException(CommandFlags.None, "internal-key-context", CommandStatus.Unknown);
        Task<RedisResult> timeout = Task.FromException<RedisResult>(error);
        redisDb.ExecuteAsync("FT.INFO", Arg.Any<object[]>()).Returns(timeout);
        falkorDb.ExecuteAsync("GRAPH.LIST").Returns(timeout);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        result.Checks.Select(check => check.CheckName).ShouldBe(
            ["IndexExistence", "SyntacticIsolation", "SemanticIsolation", "GraphIsolation", "OrphanedDatabases"]);
        foreach (TenantIsolationCheckResult check in result.Checks)
        {
            check.Passed.ShouldBeFalse();
            check.Details.ShouldNotBeNull();
            check.Details.ShouldContain($"Backend unavailable: {expectedReason}");
            check.Details.ShouldNotContain("internal-key-context");
            check.Remediation.ShouldBe("Check Redis/FalkorDB connectivity and retry");
        }

        result.Checks.Single(check => check.CheckName == "GraphIsolation").Details!
            .ShouldStartWith("Structural database-existence evidence only");
    }

    [Theory]
    [InlineData("cursor", "storage command timed out")]
    [InlineData("hash", "storage command timed out")]
    [InlineData("server error", "storage server rejected the command")]
    public async Task VerifyAsync_SyntacticScanAndInitialGraphFailures_AffectOnlyThoseChecks(
        string failure,
        string expectedReason)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IServer redisServer) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        RedisTimeoutException timeout = new(CommandFlags.None, "driver diagnostic", CommandStatus.Unknown);
        RedisResult graphList = RedisResult.Create([RedisResult.Create(new RedisValue("tenant-a"))]);
        falkorDb.ExecuteAsync("GRAPH.LIST")
            .Returns(Task.FromException<RedisResult>(timeout), Task.FromResult(graphList));
        string prefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a");
        if (failure == "cursor")
        {
            TaskCompletionSource failedMoveNext = new(TaskCreationOptions.RunContinuationsAsynchronously);
            failedMoveNext.SetException(timeout);
            SetupPendingRedisKeyScan(redisServer, prefix, new TaskCompletionSource(), failedMoveNext);
        }
        else
        {
            string key = prefix + "unit";
            SetupRedisKeyScan(redisServer, prefix, key);
            Exception readFailure = failure == "hash"
                ? timeout
                : RedisExceptionFactory.CreateServerException("ERR backend diagnostic referencing WRONGTYPE");
            redisDb.HashGetAsync((RedisKey)key, (RedisValue)"tenantId", Arg.Any<CommandFlags>())
                .Returns(Task.FromException<RedisValue>(readFailure));
        }

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult existence = result.Checks.Single(check => check.CheckName == "IndexExistence");
        existence.Passed.ShouldBeFalse();
        existence.Details.ShouldBe("Backend unavailable: storage command timed out");
        TenantIsolationCheckResult syntactic = result.Checks.Single(check => check.CheckName == "SyntacticIsolation");
        syntactic.Passed.ShouldBeFalse();
        syntactic.Details.ShouldBe($"Backend unavailable: {expectedReason}");
        result.Checks.Skip(2).Count(check => check.Passed).ShouldBe(3);
        _ = await falkorDb.Received(3).ExecuteAsync("GRAPH.LIST");
    }

    [Theory]
    [InlineData("FT.INFO")]
    [InlineData("GRAPH.LIST")]
    public async Task VerifyAsync_PendingIndexOrGraphRead_StopsWaitingWhenCancelled(string command)
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<RedisResult> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)]);
        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        IDatabase blockedDatabase = command == "FT.INFO" ? redisDb : falkorDb;
        TaskCompletionSource readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        blockedDatabase.ExecuteAsync(command, Arg.Any<object[]>())
            .Returns(_ =>
            {
                readStarted.SetResult();
                return pending.Task;
            });

        Task<TenantIsolationVerificationResult> verification = verifier.VerifyAsync("tenant-a", cancellation.Token);
        await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        cancellation.Cancel();

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(() =>
            verification.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        error.CancellationToken.ShouldBe(cancellation.Token);
        pending.Task.IsCompleted.ShouldBeFalse();
        _ = await redisDb.Received(command == "FT.INFO" ? 1 : 3).ExecuteAsync("FT.INFO", Arg.Any<object[]>());
        _ = await falkorDb.Received(command == "FT.INFO" ? 0 : 1).ExecuteAsync("GRAPH.LIST");
    }

    [Fact]
    public async Task VerifyAsync_AlreadyCancelled_DoesNotDispatchBackendReads()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        ITenantEmbeddingConfigProvider provider = Substitute.For<ITenantEmbeddingConfigProvider>();
        DaprClient daprClient = Substitute.For<DaprClient>();
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            [new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow)],
            provider,
            daprClient: daprClient);

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(() =>
            verifier.VerifyAsync("tenant-a", cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        daprClient.ReceivedCalls().ShouldBeEmpty();
        provider.ReceivedCalls().ShouldBeEmpty();
        redisDb.ReceivedCalls().ShouldBeEmpty();
        falkorDb.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_SingleTenant_PerformsTargetStructuralChecks()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();

        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Passed.ShouldBeTrue();
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain("Target syntactic index metadata");

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeTrue();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("raw and natural-language vector index metadata");
    }

    [Fact]
    public void GraphIsolationSource_DocumentsNarrowScopeAndOnlyListsDatabases()
    {
        string serverProjectDirectory = Path.Combine(TestRepository.Root, "src", "Hexalith.Memories.Server");
        string[] verifierFiles = [.. Directory.GetFiles(
                serverProjectDirectory,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file =>
            {
                string relativePath = Path.GetRelativePath(serverProjectDirectory, file);
                string[] segments = relativePath.Split(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                return !segments.Any(segment =>
                    string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
            })
            .Where(file => Regex.IsMatch(
                File.ReadAllText(file),
                @"\bpartial(?:\s+sealed)?\s+class\s+TenantIsolationVerifier\b",
                RegexOptions.CultureInvariant))];
        verifierFiles.ShouldNotBeEmpty();
        string verifierSource = string.Join(Environment.NewLine, verifierFiles.Select(File.ReadAllText));
        string verifierDefinition = File.ReadAllText(Path.Combine(serverProjectDirectory, "Tenants", "TenantIsolationVerifier.cs"));
        Match classSummary = Regex.Match(
            verifierDefinition,
            @"(?m)^/// <summary>(?<summary>[^<]+)</summary>\s*public sealed partial class TenantIsolationVerifier\b",
            RegexOptions.CultureInvariant);
        classSummary.Success.ShouldBeTrue("TenantIsolationVerifier must have a class summary.");
        classSummary.Groups["summary"].Value.ShouldContain("verifies tenant data isolation across all storage backends");
        classSummary.Groups["summary"].Value.ShouldContain("architectural isolation guarantees hold");
        classSummary.Groups["summary"].Value.ShouldNotContain("structural", Shouldly.Case.Insensitive);

        Match graphMethod = Regex.Match(
            verifierDefinition,
            @"private async Task<TenantIsolationCheckResult> CheckGraphIsolationAsync\([^)]*\)\s*\{(?<opening>[^{}]*)",
            RegexOptions.CultureInvariant);
        graphMethod.Success.ShouldBeTrue("The graph-specific scope must be documented inside CheckGraphIsolationAsync.");
        graphMethod.Groups["opening"].Value.ShouldContain("deliberately structural-only");

        string[] graphTokens = [.. Regex.Matches(
                verifierSource,
                @"\bGRAPH\.[A-Z][A-Z0-9_.]*\b",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        string[] graphCallSiteCommands = [.. Regex.Matches(
                verifierSource,
                @"\bExecute(?:Async)?\s*\(\s*""(?<command>GRAPH\.[A-Z][A-Z0-9_.]*)""",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match => match.Groups["command"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        // Discover every partial declaration across the Server project. The call-site set makes the
        // guard non-vacuous even when comments or result prose still mention GRAPH.LIST; the broader
        // token set rejects alternate graph commands. Dynamic construction is covered by received calls.
        graphCallSiteCommands.ShouldNotBeEmpty();
        graphCallSiteCommands.ShouldAllBe(command => string.Equals(
            command,
            "GRAPH.LIST",
            StringComparison.OrdinalIgnoreCase));
        graphTokens.ShouldAllBe(command => string.Equals(
            command,
            "GRAPH.LIST",
            StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyAsync_GraphIsolation_IsStructuralOnlyAndCitesContentProof()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult graphCheck = result.Checks.First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeTrue();
        graphCheck.Details.ShouldNotBeNull();
        graphCheck.Details.ShouldContain("Structural database-existence evidence only");
        graphCheck.Details.ShouldContain("target graph database 'tenant-a' appears in GRAPH.LIST");
        graphCheck.Details.ShouldNotContain("tenant-b");
        graphCheck.Details.ShouldNotContain("2 graph database");
        TenantIsolationCheckResult orphanedCheck = result.Checks.Single(c => c.CheckName == "OrphanedDatabases");
        orphanedCheck.Passed.ShouldBeTrue();
        orphanedCheck.Details.ShouldBe("All FalkorDB databases correspond to registered tenants");
        graphCheck.Details.ShouldMatch(GraphContentProof.CitationPattern);
        graphCheck.Details.ShouldContain("independent execution");

        // Assert every command-like first argument after stringifying it, without filtering by
        // prefix, so a content command such as KEYS or SCAN — including a typed RedisKey first
        // argument — cannot evade the structural-only boundary. Inspect nested params-array
        // strings separately so a GRAPH.* token cannot hide outside the command position.
        var receivedCalls = falkorDb.ReceivedCalls().ToArray();
        string[] executedCommands = [.. FirstArgumentStrings(receivedCalls)];
        executedCommands.ShouldNotBeEmpty();
        executedCommands.ShouldAllBe(command => string.Equals(
            command,
            "GRAPH.LIST",
            StringComparison.OrdinalIgnoreCase));

        string[] receivedGraphTokens = [.. receivedCalls
            .SelectMany(call => call.GetArguments())
            .SelectMany(argument => argument switch
            {
                string value => [value],
                object[] values => values.OfType<string>(),
                _ => [],
            })
            .Select(value => value.Trim())
            .Where(value => value.StartsWith("GRAPH.", StringComparison.OrdinalIgnoreCase))];
        receivedGraphTokens.ShouldNotBeEmpty();
        receivedGraphTokens.ShouldAllBe(command => string.Equals(
            command,
            "GRAPH.LIST",
            StringComparison.OrdinalIgnoreCase));

        RedisKey typedFirstArgument = "graph-isolation-typed-first-arg";
        _ = await falkorDb.KeyExistsAsync(typedFirstArgument);

        ICall[] callsAfterTypedArgument = falkorDb.ReceivedCalls().ToArray();
        object?[] firstArguments = [.. callsAfterTypedArgument.Select(call => call.GetArguments().FirstOrDefault())];
        firstArguments.ShouldContain(argument => argument is RedisKey);

        string[] stringifiedFirstArguments = [.. FirstArgumentStrings(callsAfterTypedArgument)];
        stringifiedFirstArguments.ShouldContain(typedFirstArgument.ToString());

        string[] ofTypeOnly = [.. firstArguments.OfType<string>()];
        ofTypeOnly.ShouldNotContain(typedFirstArgument.ToString());
    }

    [Fact]
    public async Task VerifyAsync_TargetGraphDatabaseMissing_FailsClosed()
    {
        // The spec's I/O matrix requires "Missing database fails closed". Every other GRAPH.LIST setup in
        // this class includes the tenant under verification, so without this case the
        // !graphDatabases.Contains(tenantId) branch in CheckGraphIsolationAsync is never exercised.
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult graphCheck = result.Checks.First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeFalse();
        graphCheck.Details.ShouldNotBeNull();
        graphCheck.Details.ShouldStartWith("Structural database-existence evidence only");
        graphCheck.Details.ShouldContain("graph database is missing from GRAPH.LIST");
        result.AllPassed.ShouldBeFalse();
    }

    [Fact]
    public async Task VerifyAsync_NonActiveTenantsSkippedInCrossChecks()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-provisioning", "Provisioning Tenant", TenantStatus.Provisioning, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-deleting", "Deleting Tenant", TenantStatus.Deleting, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        // Non-active tenants should be reported as skipped
        TenantIsolationCheckResult? skipProvisioning = result.Checks.FirstOrDefault(c => c.CheckName == "CrossCheck-tenant-provisioning");
        skipProvisioning.ShouldNotBeNull();
        skipProvisioning.Passed.ShouldBeTrue();
        skipProvisioning.Details.ShouldNotBeNull();
        skipProvisioning.Details.ShouldContain("Provisioning");

        TenantIsolationCheckResult? skipDeleting = result.Checks.FirstOrDefault(c => c.CheckName == "CrossCheck-tenant-deleting");
        skipDeleting.ShouldNotBeNull();
        skipDeleting.Passed.ShouldBeTrue();
        skipDeleting.Details.ShouldNotBeNull();
        skipDeleting.Details.ShouldContain("Deleting");
    }

    [Fact]
    public async Task VerifyAsync_EmptyTenant_PassesWithVacuousDetails()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("empty-tenant", "Empty", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "empty-tenant", 0);
        SetupGraphList(falkorDb, "empty-tenant", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("empty-tenant", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();

        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain("vacuously true");

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("vacuously true");
    }

    // --- Helper methods ---

    private static (TenantIsolationVerifier Verifier, IDatabase RedisDb, IDatabase FalkorDb, IServer RedisServer) CreateVerifier(
        IReadOnlyList<TenantInfo> tenants,
        ITenantEmbeddingConfigProvider? embeddingConfigProvider = null,
        ILogger<TenantIsolationVerifier>? logger = null,
        TimeProvider? timeProvider = null,
        DaprClient? daprClient = null)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IReadOnlyList<IServer> redisServers) =
            CreateVerifierCore(tenants, redisServerCount: 1, embeddingConfigProvider, logger, timeProvider, daprClient);
        return (verifier, redisDb, falkorDb, redisServers[0]);
    }

    private static (TenantIsolationVerifier Verifier, IDatabase RedisDb, IDatabase FalkorDb, IServer FirstRedisServer, IServer SecondRedisServer) CreateVerifierWithTwoRedisServers(
        IReadOnlyList<TenantInfo> tenants)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IReadOnlyList<IServer> redisServers) =
            CreateVerifierCore(tenants, redisServerCount: 2);
        return (verifier, redisDb, falkorDb, redisServers[0], redisServers[1]);
    }

    private static (TenantIsolationVerifier Verifier, IDatabase RedisDb, IDatabase FalkorDb, IReadOnlyList<IServer> RedisServers) CreateVerifierCore(
        IReadOnlyList<TenantInfo> tenants,
        int redisServerCount,
        ITenantEmbeddingConfigProvider? embeddingConfigProvider = null,
        ILogger<TenantIsolationVerifier>? logger = null,
        TimeProvider? timeProvider = null,
        DaprClient? daprClient = null)
    {
        // Set up TenantRegistryService with mocked DaprClient
        daprClient ??= Substitute.For<DaprClient>();
        ILogger<TenantRegistryService> registryLogger = Substitute.For<ILogger<TenantRegistryService>>();
        TenantRegistryService registry = new(daprClient, registryLogger);

        // Mock ListTenantsAsync: return tenant index, then individual entries
        List<string> tenantIds = tenants.Select(t => t.Id).ToList();
        daprClient.GetStateAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(tenantIds);

        foreach (TenantInfo tenant in tenants)
        {
            TenantRegistryEntry entry = new(tenant, null);
            daprClient.GetStateAsync<StoredTenantRegistryEntry?>("statestore", $"tenant-registry-{tenant.Id}", cancellationToken: Arg.Any<CancellationToken>())
                .Returns(entry);
        }

        // Set up Redis mocks
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase redisDb = Substitute.For<IDatabase>();
        List<IServer> redisServers = [];
        EndPoint[] redisEndpoints = Enumerable.Range(0, redisServerCount)
            .Select(i => new DnsEndPoint("localhost", 6379 + i))
            .Cast<EndPoint>()
            .ToArray();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);
        redis.GetEndPoints().Returns(redisEndpoints);
        foreach (EndPoint redisEndpoint in redisEndpoints)
        {
            IServer redisServer = Substitute.For<IServer>();
            redis.GetServer(redisEndpoint).Returns(redisServer);
            redisServer.IsConnected.Returns(true);
            redisServer.KeysAsync(
                Arg.Any<int>(),
                Arg.Any<RedisValue>(),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
                .Returns(_ => ToAsyncKeys());
            redisServers.Add(redisServer);
        }

        // Set up FalkorDB mocks
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase falkorDatabase = Substitute.For<IDatabase>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(falkorDatabase);

        ILogger<TenantIsolationVerifier> effectiveLogger = logger
            ?? Substitute.For<ILogger<TenantIsolationVerifier>>();

        ITenantEmbeddingConfigProvider effectiveEmbeddingConfigProvider = embeddingConfigProvider
            ?? CreateEmbeddingConfigProvider(EmbeddingProviderDefaults.Google());
        TenantIsolationVerifier verifier = new(
            registry, effectiveEmbeddingConfigProvider, redis, falkorDb, effectiveLogger, timeProvider ?? TimeProvider.System);
        return (verifier, redisDb, falkorDatabase, redisServers);
    }

    private static ITenantEmbeddingConfigProvider CreateEmbeddingConfigProvider(TenantEmbeddingConfig config)
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);
        return embeddingConfigProvider;
    }

    public static TheoryData<string, TenantEmbeddingConfig> InvalidNonProviderEmbeddingConfigurations()
        => new()
        {
            { "model", EmbeddingProviderDefaults.Google() with { Model = "invalid model" } },
            { "rateLimitPerMinute", EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 0 } },
            { "apiSecretKeyName", EmbeddingProviderDefaults.Google() with { ApiSecretKeyName = "INVALID_SECRET" } },
            { "baseUrl", EmbeddingProviderDefaults.Google() with { BaseUrl = "not-a-url" } },
            { "authMode", EmbeddingProviderDefaults.Google() with { AuthMode = "invalid-auth-mode" } },
            { "oidcTokenEndpoint", EmbeddingProviderDefaults.Google() with { OidcTokenEndpoint = "not-a-url" } },
            {
                "oidcClientId",
                EmbeddingProviderDefaults.Ollama() with
                {
                    OidcClientId = string.Empty,
                    OidcTokenEndpoint = "https://identity.example.test/token",
                }
            },
        };

    private static void AssertEmbeddingConfigurationFailureLogged(
        CapturingLogger<TenantIsolationVerifier> logger,
        string expectedFailureType,
        params string[] sensitiveValues)
    {
        LogEntry warning = logger.Entries
            .Where(entry => entry.Level == LogLevel.Warning)
            .ShouldHaveSingleItem();

        warning.Message.ShouldBe(
            $"Embedding configuration lookup failed for requested tenant 'tenant-a' with {expectedFailureType}; "
            + "semantic isolation verification will fail closed");
        warning.Exception.ShouldBeNull();
        foreach (string sensitiveValue in sensitiveValues)
        {
            warning.Message.ShouldNotContain(sensitiveValue);
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private static void AssertReadOnlyDependencyCalls(IDatabase redisDb, IDatabase falkorDb)
    {
        redisDb.ReceivedCalls()
            .Select(call => call.GetMethodInfo().Name)
            .ShouldAllBe(methodName => string.Equals(methodName, nameof(IDatabase.ExecuteAsync), StringComparison.Ordinal));
        string[] redisCommands = [.. FirstArgumentStrings(redisDb.ReceivedCalls().ToArray())];
        redisCommands.ShouldNotBeEmpty();
        redisCommands.ShouldAllBe(command => string.Equals(command, "FT.INFO", StringComparison.OrdinalIgnoreCase));

        falkorDb.ReceivedCalls()
            .Select(call => call.GetMethodInfo().Name)
            .ShouldAllBe(methodName => string.Equals(methodName, nameof(IDatabase.ExecuteAsync), StringComparison.Ordinal));
        string[] graphCommands = [.. FirstArgumentStrings(falkorDb.ReceivedCalls().ToArray())];
        graphCommands.ShouldNotBeEmpty();
        graphCommands.ShouldAllBe(command => string.Equals(command, "GRAPH.LIST", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertNoMutationDependencyCalls(IDatabase redisDb, IDatabase falkorDb)
    {
        string[] expectedRedisIndexNames =
        [
            IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"),
        ];
        List<string> actualRedisIndexNames = [];
        foreach (ICall call in redisDb.ReceivedCalls())
        {
            object?[] arguments = call.GetArguments();
            switch (call.GetMethodInfo().Name)
            {
                case nameof(IDatabase.ExecuteAsync):
                    arguments.Length.ShouldBe(2);
                    arguments[0].ShouldBe("FT.INFO");
                    object[]? commandArguments = arguments[1] as object[];
                    commandArguments.ShouldNotBeNull();
                    commandArguments.Length.ShouldBe(1);
                    string? indexName = commandArguments[0]?.ToString();
                    indexName.ShouldNotBeNullOrWhiteSpace();
                    expectedRedisIndexNames.ShouldContain(indexName);
                    actualRedisIndexNames.Add(indexName);
                    break;
                case nameof(IDatabase.HashGetAsync):
                    arguments.Length.ShouldBe(3);
                    arguments[0]?.ToString().ShouldStartWith(IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenant-a"));
                    arguments[1].ShouldBe((RedisValue)"tenantId");
                    arguments[2].ShouldBe(CommandFlags.DemandMaster);
                    break;
                case nameof(IDatabase.ScriptEvaluateAsync):
                    arguments.Length.ShouldBe(4);
                    arguments[0].ShouldBe(SemanticReadScript);
                    RedisKey[]? keys = arguments[1] as RedisKey[];
                    string key = keys.ShouldNotBeNull().ShouldHaveSingleItem().ToString();
                    (key.StartsWith(IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a"), StringComparison.Ordinal)
                        || key.StartsWith(IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a"), StringComparison.Ordinal)).ShouldBeTrue();
                    (arguments[2] as RedisValue[]).ShouldNotBeNull().ShouldBeEmpty();
                    arguments[3].ShouldBe(CommandFlags.DemandMaster);
                    break;
                default:
                    throw new ShouldAssertException(
                        $"Unexpected Redis dependency call '{call.GetMethodInfo().Name}' was observed.");
            }
        }

        actualRedisIndexNames.OrderBy(static name => name, StringComparer.Ordinal).ShouldBe(
            expectedRedisIndexNames.OrderBy(static name => name, StringComparer.Ordinal));

        foreach (ICall call in falkorDb.ReceivedCalls())
        {
            call.GetMethodInfo().Name.ShouldBe(nameof(IDatabase.ExecuteAsync));
            object?[] arguments = call.GetArguments();
            arguments.Length.ShouldBe(2);
            arguments[0].ShouldBe("GRAPH.LIST");
            object[]? commandArguments = arguments[1] as object[];
            commandArguments.ShouldNotBeNull();
            commandArguments.ShouldBeEmpty();
        }
    }

    private static IEnumerable<string> FirstArgumentStrings(ICall[] calls)
        => calls
            .Select(call => call.GetArguments().FirstOrDefault())
            .Select(argument => argument switch
            {
                null => null,
                string value => value,
                _ => argument.ToString(),
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());

    private static void SetupMissingIndex(IDatabase db, string indexName) =>
        db.ExecuteAsync("FT.INFO", Arg.Is<object[]>(args => args.Length == 1 && Equals(args[0], indexName)))
            .Throws(RedisExceptionFactory.CreateServerException("Unknown index name"));

    private static void SetupSuccessfulIndexInfo(
        IDatabase db,
        string tenantId,
        int docCount = 1,
        int rawDimensions = VectorDimensions,
        int naturalLanguageDimensions = VectorDimensions)
    {
        SetupIndexInfo(
            db,
            IndexSchemaDefinitions.GetSyntacticIndexName(tenantId),
            CreateIndexInfo(
                IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId),
                IndexSchemaDefinitions.GetSyntacticFieldIdentifiers(),
                docCount));
        SetupIndexInfo(
            db,
            IndexSchemaDefinitions.GetSemanticIndexName(tenantId),
            CreateIndexInfo(
                IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId),
                IndexSchemaDefinitions.GetSemanticFieldIdentifiers(),
                docCount,
                rawDimensions));
        SetupIndexInfo(
            db,
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId),
            CreateIndexInfo(
                IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId),
                IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers(),
                docCount,
                naturalLanguageDimensions));
    }

    private static void SetupIndexInfo(IDatabase db, string indexName, RedisResult info)
    {
        db.ExecuteAsync(Arg.Is("FT.INFO"), Arg.Is<object[]>(args =>
                args!.Length > 0 && string.Equals(args[0].ToString(), indexName, StringComparison.Ordinal)))
            .Returns(info);
    }

    private static RedisResult CreateIndexInfo(
        string prefix,
        IReadOnlyList<string> fields,
        int docCount,
        int? dimensions = null)
    {
        RedisResult[] attributes = fields
            .Select(field => CreateAttribute(field, dimensions))
            .ToArray();

        return RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("num_docs")),
            RedisResult.Create(new RedisValue(docCount.ToString(CultureInfo.InvariantCulture))),
            RedisResult.Create(new RedisValue("index_definition")),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("prefixes")),
                RedisResult.Create([RedisResult.Create(new RedisValue(prefix))]),
            ]),
            RedisResult.Create(new RedisValue("attributes")),
            RedisResult.Create(attributes),
        ]);
    }

    private static RedisResult CreateAttribute(string field, int? dimensions)
    {
        string type = string.Equals(field, "embedding", StringComparison.Ordinal)
            ? "VECTOR"
            : "TAG";
        List<RedisResult> values =
        [
            RedisResult.Create(new RedisValue("identifier")),
            RedisResult.Create(new RedisValue(field)),
            RedisResult.Create(new RedisValue("type")),
            RedisResult.Create(new RedisValue(type)),
        ];

        if (string.Equals(field, "embedding", StringComparison.Ordinal) && dimensions is not null)
        {
            values.Add(RedisResult.Create(new RedisValue("dim")));
            values.Add(RedisResult.Create(new RedisValue(dimensions.Value.ToString(CultureInfo.InvariantCulture))));
        }

        return RedisResult.Create([.. values]);
    }

    private static void SetupRedisKeyScan(IServer server, string keyPrefix, params string[] keys)
    {
        string pattern = keyPrefix + "*";
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(value => string.Equals(value.ToString(), pattern, StringComparison.Ordinal)),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(_ => ToAsyncKeys(keys));
    }

    private static void SetupPendingRedisKeyScan(
        IServer server,
        string keyPrefix,
        TaskCompletionSource moveNextEntered,
        TaskCompletionSource releaseMoveNext)
    {
        string pattern = keyPrefix + "*";
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(value => string.Equals(value.ToString(), pattern, StringComparison.Ordinal)),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(_ => ToPendingAsyncKeys(moveNextEntered, releaseMoveNext));
    }

    private static void SetupTenantIdField(IDatabase db, string key, string? tenantId)
    {
        db.HashGetAsync(
                Arg.Is<RedisKey>(redisKey => string.Equals(redisKey.ToString(), key, StringComparison.Ordinal)),
                Arg.Is<RedisValue>(field => string.Equals(field.ToString(), "tenantId", StringComparison.Ordinal)),
                Arg.Any<CommandFlags>())
            .Returns(tenantId is null ? RedisValue.Null : new RedisValue(tenantId));
    }

    private static void SetupSemanticRecord(
        IDatabase db,
        string key,
        string memoryUnitId,
        string? tenantId,
        bool hasNaturalLanguageDescription = false,
        int? chunkSequence = null,
        int? chunkStartOffset = null,
        int? chunkEndOffset = null,
        string? sensitivePayloadContent = null)
    {
        RedisResult[] fields =
        [
            RedisResult.Create((RedisValue)memoryUnitId),
            RedisResult.Create((RedisValue)tenantId),
            RedisResult.Create(chunkSequence is int sequence ? (RedisValue)sequence : RedisValue.Null),
            RedisResult.Create(chunkStartOffset is int start ? (RedisValue)start : RedisValue.Null),
            RedisResult.Create(chunkEndOffset is int end ? (RedisValue)end : RedisValue.Null),
            RedisResult.Create(hasNaturalLanguageDescription ? 1L : 0L),
        ];
        ExpectSemanticRead(db, key).Returns(RedisResult.Create(fields));

        if (sensitivePayloadContent is not null)
        {
            // A full read would include payload fields that the verifier never requests.
            HashEntry[] fullRecord =
            [
                new HashEntry("memoryUnitId", memoryUnitId),
                new HashEntry("tenantId", tenantId ?? string.Empty),
                new HashEntry("content", sensitivePayloadContent),
                new HashEntry("embedding", sensitivePayloadContent),
            ];
            db.HashGetAllAsync(
                    Arg.Is<RedisKey>(redisKey => string.Equals(redisKey.ToString(), key, StringComparison.Ordinal)),
                    Arg.Any<CommandFlags>())
                .Returns(fullRecord);
        }
    }

    private const string SemanticReadScript = """
        local fields = redis.call('HMGET', KEYS[1], 'memoryUnitId', 'tenantId', 'chunkSequence', 'chunkStartOffset', 'chunkEndOffset')
        fields[6] = redis.call('HEXISTS', KEYS[1], 'naturalLanguageDescription')
        return fields
        """;

    private static Task<RedisResult> ExpectSemanticRead(IDatabase db, string key)
        => db.ScriptEvaluateAsync(
            SemanticReadScript,
            Arg.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0] == (RedisKey)key),
            Arg.Is<RedisValue[]>(values => values.Length == 0),
            CommandFlags.DemandMaster);

    private static async IAsyncEnumerable<RedisKey> ToAsyncKeys(params string[] keys)
    {
        foreach (string key in keys)
        {
            await Task.Yield();
            yield return key;
        }
    }

    private static async IAsyncEnumerable<RedisKey> ToPendingAsyncKeys(
        TaskCompletionSource moveNextEntered,
        TaskCompletionSource releaseMoveNext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        moveNextEntered.TrySetResult();
        await releaseMoveNext.Task.WaitAsync(cancellationToken);
        yield break;
    }

    private static void SetupGraphList(IDatabase falkorDb, params string[] databases)
    {
        RedisResult[] items = databases.Select(d => RedisResult.Create(new RedisValue(d))).ToArray();
        RedisResult graphListResult = RedisResult.Create(items);
        falkorDb.ExecuteAsync(Arg.Is("GRAPH.LIST"), Arg.Any<object[]>())
            .Returns(graphListResult);
    }

}
