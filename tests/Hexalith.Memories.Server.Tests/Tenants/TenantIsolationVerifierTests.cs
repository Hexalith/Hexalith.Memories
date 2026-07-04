#pragma warning disable CS8620 // Nullability mismatch in NSubstitute Returns() with nullable Dapr client methods.

// <copyright file="TenantIsolationVerifierTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class TenantIsolationVerifierTests
{
    [Fact]
    public async Task VerifyAsync_AllChecksPassed_ReturnsAllPassed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        result.TenantId.ShouldBe("tenant-a");
        result.Checks.ShouldNotBeEmpty();
        result.Summary.ShouldContain("checks passed");
    }

    [Fact]
    public async Task VerifyAsync_DetectsSyntacticLeakage_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        // Simulate leakage: tenant-a's syntactic index contains a key with tenant-b's prefix
        SetupSearchWithForeignKey(
            redisDb,
            IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a"),
            IndexSchemaDefinitions.BuildSyntacticKey("tenant-b", "leaked-doc"));
        SetupEmptySearchExcept(redisDb, IndexSchemaDefinitions.GetSyntacticIndexName("tenant-a"));
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Passed.ShouldBeFalse();
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain("tenant-b");
    }

    [Fact]
    public async Task VerifyAsync_DetectsSemanticLeakage_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb); // syntactic clean
        // Override semantic search to simulate leakage in tenant-a's semantic index
        SetupSearchWithForeignKeyForIndex(
            redisDb,
            IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"),
            IndexSchemaDefinitions.BuildSemanticKey("tenant-b", "leaked-vec"));
        SetupGraphList(falkorDb, "tenant-a", "tenant-b");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeFalse();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("tenant-b");
    }

    [Fact]
    public async Task VerifyAsync_MissingPeerGraphDatabase_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a");

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult graphCheck = result.Checks.First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeFalse();
        graphCheck.Details.ShouldNotBeNull();
        graphCheck.Details.ShouldContain("tenant-b");
    }

    [Fact]
    public async Task VerifyAsync_DetectsOrphanedDatabases_ReturnsFailed()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        // GRAPH.LIST returns an extra database not in registry
        SetupGraphList(falkorDb, "tenant-a", "ghost-tenant");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult orphanCheck = result.Checks.First(c => c.CheckName == "OrphanedDatabases");
        orphanCheck.Passed.ShouldBeFalse();
        orphanCheck.Details.ShouldNotBeNull();
        orphanCheck.Details.ShouldContain("ghost-tenant");
        orphanCheck.Remediation.ShouldNotBeNull();
    }

    [Fact]
    public async Task VerifyAsync_RejectsMalformedTenantIds_InputValidationPasses()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult inputCheck = result.Checks.First(c => c.CheckName == "InputValidation");
        inputCheck.Passed.ShouldBeTrue();
        inputCheck.Details.ShouldNotBeNull();
        inputCheck.Details.ShouldContain("correctly rejected");
    }

    [Fact]
    public async Task VerifyAsync_RejectsReservedTenantIds_InputValidationPasses()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        TenantIsolationCheckResult inputCheck = result.Checks.First(c => c.CheckName == "InputValidation");
        inputCheck.Passed.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_IncludesPerCheckTiming()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        foreach (TenantIsolationCheckResult check in result.Checks)
        {
            check.DurationMs.ShouldBeGreaterThanOrEqualTo(0.0);
        }
    }

    [Fact]
    public async Task VerifyAsync_AllPassedFalseWhenAnyCheckFails()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        // Make index existence fail
        redisDb.ExecuteAsync(Arg.Is("FT.INFO"), Arg.Any<object[]>())
            .Throws(new RedisServerException("Unknown index name"));
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeFalse();
        result.Summary.ShouldContain("failed");
    }

    [Fact]
    public async Task VerifyAsync_BackendUnavailable_ReturnsFailedCheckNotException()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
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

        // InputValidation doesn't depend on backend
        TenantIsolationCheckResult inputCheck = result.Checks.First(c => c.CheckName == "InputValidation");
        inputCheck.Passed.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_SingleTenant_CrossChecksReportSkipped()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();

        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Passed.ShouldBeTrue();
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain("no other tenants to check against");

        TenantIsolationCheckResult semanticCheck = result.Checks.First(c => c.CheckName == "SemanticIsolation");
        semanticCheck.Passed.ShouldBeTrue();
        semanticCheck.Details.ShouldNotBeNull();
        semanticCheck.Details.ShouldContain("no other tenants to check against");

        TenantIsolationCheckResult graphCheck = result.Checks.First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeTrue();
        graphCheck.Details.ShouldNotBeNull();
        graphCheck.Details.ShouldContain("no other tenants to check against");
    }

    [Fact]
    public async Task VerifyAsync_NonActiveTenantsSkippedInCrossChecks()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-provisioning", "Provisioning Tenant", TenantStatus.Provisioning, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-deleting", "Deleting Tenant", TenantStatus.Deleting, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

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

        // Cross-checks for syntactic/semantic/graph should skip since no active other tenants
        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain("no other tenants to check against");
    }

    [Fact]
    public async Task VerifyAsync_EmptyTenant_PassesWithVacuousDetails()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb) = CreateVerifier(
            tenants:
            [
                new TenantInfo("empty-tenant", "Empty", TenantStatus.Active, DateTimeOffset.UtcNow),
                new TenantInfo("tenant-b", "Tenant B", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, 0);
        SetupEmptySearch(redisDb);
        SetupGraphList(falkorDb, "empty-tenant", "tenant-b");
        SetupGraphQueryEmpty(falkorDb);

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

    private static (TenantIsolationVerifier Verifier, IDatabase RedisDb, IDatabase FalkorDb) CreateVerifier(
        IReadOnlyList<TenantInfo> tenants)
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
            daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", $"tenant-registry-{tenant.Id}", cancellationToken: Arg.Any<CancellationToken>())
                .Returns(entry);
        }

        // Set up Redis mocks
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase redisDb = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        // Set up FalkorDB mocks
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        IDatabase falkorDatabase = Substitute.For<IDatabase>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(falkorDatabase);

        ILogger<TenantIsolationVerifier> logger = Substitute.For<ILogger<TenantIsolationVerifier>>();

        TenantIsolationVerifier verifier = new(registry, redis, falkorDb, logger);
        return (verifier, redisDb, falkorDatabase);
    }

    private static void SetupSuccessfulIndexInfo(IDatabase db, int docCount = 1)
    {
        RedisResult infoResult = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("num_docs")),
            RedisResult.Create(new RedisValue(docCount.ToString(System.Globalization.CultureInfo.InvariantCulture))),
        ]);

        db.ExecuteAsync(Arg.Is("FT.INFO"), Arg.Any<object[]>())
            .Returns(infoResult);
    }

    private static void SetupEmptySearch(IDatabase db)
    {
        // FT.SEARCH NOCONTENT returns [0] — no documents
        RedisResult emptySearchResult = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("0")),
        ]);
        db.ExecuteAsync(Arg.Is("FT.SEARCH"), Arg.Any<object[]>())
            .Returns(emptySearchResult);
    }

    private static void SetupEmptySearchExcept(IDatabase db, string leakingIndex)
    {
        // For non-leaking indexes, return empty results
        RedisResult emptySearchResult = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("0")),
        ]);
        db.ExecuteAsync(Arg.Is("FT.SEARCH"), Arg.Is<object[]>(args =>
                args.Length > 0 && args[0].ToString() != leakingIndex))
            .Returns(emptySearchResult);
    }

    private static void SetupSearchWithForeignKey(IDatabase db, string indexName, string foreignKey)
    {
        // FT.SEARCH NOCONTENT returns [1, foreignKey] — one document with foreign prefix
        RedisResult leakyResult = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("1")),
            RedisResult.Create(new RedisValue(foreignKey)),
        ]);
        db.ExecuteAsync(Arg.Is("FT.SEARCH"), Arg.Is<object[]>(args =>
                args.Length > 0 && args[0].ToString() == indexName))
            .Returns(leakyResult);
    }

    private static void SetupSearchWithForeignKeyForIndex(IDatabase db, string indexName, string foreignKey)
    {
        // Override just for the specific semantic index
        RedisResult leakyResult = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("1")),
            RedisResult.Create(new RedisValue(foreignKey)),
        ]);

        // First set up default empty for all FT.SEARCH, then override for specific index
        db.ExecuteAsync(Arg.Is("FT.SEARCH"), Arg.Is<object[]>(args =>
                args.Length > 0 && args[0].ToString() == indexName))
            .Returns(leakyResult);
    }

    private static void SetupGraphList(IDatabase falkorDb, params string[] databases)
    {
        RedisResult[] items = databases.Select(d => RedisResult.Create(new RedisValue(d))).ToArray();
        RedisResult graphListResult = RedisResult.Create(items);
        falkorDb.ExecuteAsync(Arg.Is("GRAPH.LIST"), Arg.Any<object[]>())
            .Returns(graphListResult);
    }

    private static void SetupGraphQueryEmpty(IDatabase falkorDb)
    {
        // GRAPH.QUERY returns error (isolation working) or empty result
        falkorDb.ExecuteAsync(Arg.Is("GRAPH.QUERY"), Arg.Any<object[]>())
            .Throws(new RedisServerException("ERR Graph not found"));
    }

}
