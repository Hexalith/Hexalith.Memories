#pragma warning disable CS8620 // Nullability mismatch in NSubstitute Returns() with nullable Dapr client methods.

// <copyright file="TenantIsolationVerifierTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using System.Globalization;
using System.Net;

using Dapr.Actors;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Tests.Deployment;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class TenantIsolationVerifierTests
{
    private const int VectorDimensions = 768;

    [Fact]
    public void Constructor_NullEmbeddingConfigProvider_Throws()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryService registry = new(
            daprClient,
            Substitute.For<ILogger<TenantRegistryService>>());

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() => new TenantIsolationVerifier(
            registry,
            null!,
            Substitute.For<IConnectionMultiplexer>(),
            Substitute.For<IConnectionMultiplexer>(),
            Substitute.For<ILogger<TenantIsolationVerifier>>()));

        exception.ParamName.ShouldBe("embeddingConfigProvider");
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
        semanticCheck.Details.ShouldContain("validated 768-dimension configuration");
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
    [InlineData("dapr")]
    [InlineData("actor")]
    [InlineData("timeout")]
    [InlineData("http")]
    public async Task VerifyAsync_EmbeddingConfigUnavailable_FailsClosed(string failureKind)
    {
        Exception exception = failureKind switch
        {
            "dapr" => new Dapr.DaprException("tenant configuration actor unavailable"),
            "actor" => new ActorMethodInvocationException("sensitive actor payload", isTransient: true),
            "timeout" => new TimeoutException("tenant configuration timed out"),
            "http" => new HttpRequestException("tenant configuration transport unavailable"),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "Unknown failure kind."),
        };
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
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_NullEmbeddingConfigResult_FailsClosed()
    {
        ITenantEmbeddingConfigProvider embeddingConfigProvider = Substitute.For<ITenantEmbeddingConfigProvider>();
        embeddingConfigProvider
            .GetAsync("tenant-a", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantEmbeddingConfig>(null!));
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
        semanticCheck.Details.ShouldContain("unavailable");
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Details.ShouldNotContain("dimensions");
        semanticCheck.Details.Length.ShouldBeLessThan(256);
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("retry verification");
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
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
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Details.ShouldContain($"actual configured dimensions {configuredDimensions}");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("Correct the embedding configuration");
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_InvalidNonDimensionEmbeddingConfig_SanitizesEvidence()
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
        _ = await embeddingConfigProvider.Received(1).GetAsync("tenant-a", cts.Token);
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
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
        semanticCheck.Details.ShouldContain("unavailable");
        semanticCheck.Details.ShouldContain("tenant-a");
        semanticCheck.Remediation.ShouldNotBeNull();
        semanticCheck.Remediation.ShouldContain("retry verification");
        result.AllPassed.ShouldBeFalse();
        embeddingConfigProvider.DidNotReceive().Invalidate(Arg.Any<string>());
        AssertReadOnlyDependencyCalls(redisDb, falkorDb);
    }

    [Fact]
    public async Task VerifyAsync_DetectsSyntacticTenantIdMismatch_ReturnsFailed()
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
        SetupTenantIdField(redisDb, plantedKey, "tenant-b");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Passed.ShouldBeFalse();
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain("tenant-b");
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
        SetupTenantIdField(redisDb, plantedKey, "tenant-b");
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("tenant-b");
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
        SetupTenantIdField(redisDb, plantedKey, "tenant-b");
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
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("missing tenantId field");
    }

    [Fact]
    public async Task VerifyAsync_MultipleRedisEndpoints_ScansAllConnectedServers()
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
            .Throws(new RedisServerException("Unknown index name"));
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
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));
        falkorDb.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

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
            check.Remediation.ShouldNotBeNull();
        }
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
    public async Task VerifyAsync_GraphIsolation_IsStructuralOnlyAndCitesContentProof()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult graphCheck = result.Checks.First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeTrue();
        graphCheck.Details.ShouldNotBeNull();
        graphCheck.Details.ShouldContain("Structural database-existence evidence only");
        graphCheck.Details.ShouldContain("GRAPH.LIST");
        graphCheck.Details.ShouldContain(OperationalRunbookSetTests.GraphContentProofCitation);
        graphCheck.Details.ShouldContain("independent execution");

        // Assert every command-like first argument, without filtering by prefix, so a content command
        // such as KEYS or SCAN cannot evade the structural-only boundary. Inspect nested params-array
        // strings separately so a GRAPH.* token cannot hide outside the command position.
        var receivedCalls = falkorDb.ReceivedCalls().ToArray();
        string[] executedCommands = [.. receivedCalls
            .Select(call => call.GetArguments().FirstOrDefault())
            .OfType<string>()
            .Select(command => command.Trim())];
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
        ITenantEmbeddingConfigProvider? embeddingConfigProvider = null)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IReadOnlyList<IServer> redisServers) =
            CreateVerifierCore(tenants, redisServerCount: 1, embeddingConfigProvider);
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
        ITenantEmbeddingConfigProvider? embeddingConfigProvider = null)
    {
        // Set up TenantRegistryService with mocked DaprClient
        DaprClient daprClient = Substitute.For<DaprClient>();
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

        ILogger<TenantIsolationVerifier> logger = Substitute.For<ILogger<TenantIsolationVerifier>>();

        ITenantEmbeddingConfigProvider effectiveEmbeddingConfigProvider = embeddingConfigProvider
            ?? CreateEmbeddingConfigProvider(EmbeddingProviderDefaults.Google());
        TenantIsolationVerifier verifier = new(registry, effectiveEmbeddingConfigProvider, redis, falkorDb, logger);
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

    private static void AssertReadOnlyDependencyCalls(IDatabase redisDb, IDatabase falkorDb)
    {
        redisDb.ReceivedCalls()
            .Select(call => call.GetMethodInfo().Name)
            .ShouldAllBe(methodName => string.Equals(methodName, nameof(IDatabase.ExecuteAsync), StringComparison.Ordinal));
        string[] redisCommands = [.. redisDb.ReceivedCalls()
            .Select(call => call.GetArguments().FirstOrDefault())
            .OfType<string>()];
        redisCommands.ShouldNotBeEmpty();
        redisCommands.ShouldAllBe(command => string.Equals(command, "FT.INFO", StringComparison.OrdinalIgnoreCase));

        falkorDb.ReceivedCalls()
            .Select(call => call.GetMethodInfo().Name)
            .ShouldAllBe(methodName => string.Equals(methodName, nameof(IDatabase.ExecuteAsync), StringComparison.Ordinal));
        string[] graphCommands = [.. falkorDb.ReceivedCalls()
            .Select(call => call.GetArguments().FirstOrDefault())
            .OfType<string>()];
        graphCommands.ShouldNotBeEmpty();
        graphCommands.ShouldAllBe(command => string.Equals(command, "GRAPH.LIST", StringComparison.OrdinalIgnoreCase));
    }

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

    private static void SetupTenantIdField(IDatabase db, string key, string tenantId)
    {
        db.HashGetAsync(
                Arg.Is<RedisKey>(redisKey => string.Equals(redisKey.ToString(), key, StringComparison.Ordinal)),
                Arg.Is<RedisValue>(field => string.Equals(field.ToString(), "tenantId", StringComparison.Ordinal)),
                Arg.Any<CommandFlags>())
            .Returns(new RedisValue(tenantId));
    }

    private static async IAsyncEnumerable<RedisKey> ToAsyncKeys(params string[] keys)
    {
        foreach (string key in keys)
        {
            await Task.Yield();
            yield return key;
        }
    }

    private static void SetupGraphList(IDatabase falkorDb, params string[] databases)
    {
        RedisResult[] items = databases.Select(d => RedisResult.Create(new RedisValue(d))).ToArray();
        RedisResult graphListResult = RedisResult.Create(items);
        falkorDb.ExecuteAsync(Arg.Is("GRAPH.LIST"), Arg.Any<object[]>())
            .Returns(graphListResult);
    }

}
