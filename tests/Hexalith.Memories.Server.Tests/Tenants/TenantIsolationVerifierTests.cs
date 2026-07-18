#pragma warning disable CS8620 // Nullability mismatch in NSubstitute Returns() with nullable Dapr client methods.

// <copyright file="TenantIsolationVerifierTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using System.Globalization;
using System.Net;

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
    private const int VectorDimensions = 3;

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
        SetupGraphQueryEmpty(falkorDb);

        TenantIsolationVerificationResult result = await verifier.VerifyAsync("tenant-a", CancellationToken.None);

        result.AllPassed.ShouldBeTrue();
        result.TenantId.ShouldBe("tenant-a");
        result.Checks.ShouldNotBeEmpty();
        result.Summary.ShouldContain("checks passed");
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
        SetupGraphQueryEmpty(falkorDb);

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
        SetupGraphQueryEmpty(falkorDb);

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
        SetupGraphQueryEmpty(falkorDb);

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
        SetupGraphQueryEmpty(falkorDb);

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
        SetupGraphQueryEmpty(falkorDb);

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
    public async Task VerifyAsync_DoesNotEmitInputValidationCheck()
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        SetupSuccessfulIndexInfo(redisDb, "tenant-a");
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

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
        SetupGraphQueryEmpty(falkorDb);

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
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, _) = CreateVerifier(
            tenants:
            [
                new TenantInfo("tenant-a", "Tenant A", TenantStatus.Active, DateTimeOffset.UtcNow),
            ]);

        // Make index existence fail
        redisDb.ExecuteAsync(Arg.Is("FT.INFO"), Arg.Any<object[]>())
            .Throws(new RedisServerException("Unknown index name"));
        SetupGraphList(falkorDb, "tenant-a");
        SetupGraphQueryEmpty(falkorDb);

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
        SetupGraphQueryEmpty(falkorDb);

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

        TenantIsolationCheckResult graphCheck = result.Checks.First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeTrue();
        graphCheck.Details.ShouldNotBeNull();
        graphCheck.Details.ShouldContain("Target graph database");
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

    private static (TenantIsolationVerifier Verifier, IDatabase RedisDb, IDatabase FalkorDb, IServer RedisServer) CreateVerifier(
        IReadOnlyList<TenantInfo> tenants)
    {
        (TenantIsolationVerifier verifier, IDatabase redisDb, IDatabase falkorDb, IReadOnlyList<IServer> redisServers) =
            CreateVerifierCore(tenants, redisServerCount: 1);
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
        int redisServerCount)
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

        TenantIsolationVerifier verifier = new(registry, redis, falkorDb, logger);
        return (verifier, redisDb, falkorDatabase, redisServers);
    }

    private static void SetupSuccessfulIndexInfo(IDatabase db, string tenantId, int docCount = 1)
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
                VectorDimensions));
        SetupIndexInfo(
            db,
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId),
            CreateIndexInfo(
                IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId),
                IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers(),
                docCount,
                VectorDimensions));
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

    private static void SetupGraphQueryEmpty(IDatabase falkorDb)
    {
        // GRAPH.QUERY returns error (isolation working) or empty result
        falkorDb.ExecuteAsync(Arg.Is("GRAPH.QUERY"), Arg.Any<object[]>())
            .Throws(new RedisServerException("ERR Graph not found"));
    }

}
