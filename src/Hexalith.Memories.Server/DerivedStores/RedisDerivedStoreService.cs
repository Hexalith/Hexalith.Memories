// <copyright file="RedisDerivedStoreService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>Tenant-first Redis/FalkorDB backing for diagnostic probes and durable correction state.</summary>
internal sealed class RedisDerivedStoreService(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TerminalDeadline = TimeSpan.FromMinutes(60);

    private readonly IConnectionMultiplexer _falkorDb = falkorDb ?? throw new ArgumentNullException(nameof(falkorDb));
    private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    internal async Task PutDiagnosticEntryAsync(
        string tenantId,
        DiagnosticStoreClass storeClass,
        string resourceId,
        DiagnosticStoreEntry entry,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);
        ValidateSafeToken(resourceId, nameof(resourceId));
        ArgumentNullException.ThrowIfNull(entry);
        ValidateSafeToken(entry.ResourceId, nameof(entry.ResourceId));
        ValidateSafeToken(entry.ContentDigest, nameof(entry.ContentDigest));
        if (!string.Equals(resourceId, entry.ResourceId, StringComparison.Ordinal))
        {
            throw new DerivedStoreStateException("DIAGNOSTIC_RESOURCE_MISMATCH", "The route and entry resource identifiers must match.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _redis.GetDatabase()
            .HashSetAsync(BuildDiagnosticKey(tenantId, storeClass), resourceId, entry.ContentDigest)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<DiagnosticStoreEntry?> GetDiagnosticEntryAsync(
        string tenantId,
        DiagnosticStoreClass storeClass,
        string resourceId,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);
        ValidateSafeToken(resourceId, nameof(resourceId));
        RedisValue value = await _redis.GetDatabase()
            .HashGetAsync(BuildDiagnosticKey(tenantId, storeClass), resourceId)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return value.HasValue ? new DiagnosticStoreEntry(resourceId, value.ToString()) : null;
    }

    internal async Task<IReadOnlyList<DiagnosticStoreEntry>> ListDiagnosticEntriesAsync(
        string tenantId,
        DiagnosticStoreClass storeClass,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);
        HashEntry[] entries = await _redis.GetDatabase()
            .HashGetAllAsync(BuildDiagnosticKey(tenantId, storeClass))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return entries
            .Select(static entry => new DiagnosticStoreEntry(entry.Name.ToString(), entry.Value.ToString()))
            .OrderBy(static entry => entry.ResourceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal async Task<bool> DeleteDiagnosticEntryAsync(
        string tenantId,
        DiagnosticStoreClass storeClass,
        string resourceId,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);
        ValidateSafeToken(resourceId, nameof(resourceId));
        return await _redis.GetDatabase()
            .HashDeleteAsync(BuildDiagnosticKey(tenantId, storeClass), resourceId)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task SaveSourceArtifactAsync(
        DurableDerivedStoreSourceArtifact artifact,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ValidateTenant(artifact.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.MemoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.CaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.SourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.ContentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.EmbeddingProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.EmbeddingModel);
        if (artifact.SourceBytes.Length == 0 || artifact.EmbeddingDimensions <= 0)
        {
            throw new DerivedStoreStateException("SOURCE_ARTIFACT_INVALID", "The durable source artifact is incomplete.");
        }

        string json = JsonSerializer.Serialize(artifact, MemoriesJsonContext.Options);
        await _redis.GetDatabase()
            .StringSetAsync(BuildSourceArtifactKey(artifact.TenantId, artifact.MemoryUnitId), json)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<DerivedStoreBinding> FinalizeBindingAsync(
        string tenantId,
        FinalizeDerivedStoreBindingRequest request,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);
        ValidateFinalizeRequest(request);
        IDatabase database = _redis.GetDatabase();
        string bindingKey = BuildBindingKey(tenantId, request.AssociationId, request.IntakeId);
        RedisValue existingValue = await database.StringGetAsync(bindingKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (existingValue.HasValue)
        {
            DerivedStoreBinding existing = DeserializeRequired<DerivedStoreBinding>(existingValue, "BINDING_CORRUPT");
            if (request.SourceVersion < existing.SourceVersion)
            {
                throw new DerivedStoreStateException("BINDING_SOURCE_VERSION_STALE", "A newer binding source version is already finalized.");
            }

            if (request.SourceVersion == existing.SourceVersion)
            {
                if (BindingMatches(existing, request))
                {
                    return existing;
                }

                throw new DerivedStoreStateException("BINDING_SOURCE_VERSION_CONFLICT", "The source version is already finalized with a different manifest.");
            }
        }

        string caseKey = BuildCaseKey(tenantId, request.PriorCaseId);
        if (!await IsSameTenantCaseAsync(database, caseKey, tenantId, cancellationToken).ConfigureAwait(false))
        {
            throw new DerivedStoreStateException("BINDING_PRIOR_CASE_NOT_FOUND", "The prior case is absent or is not owned by the tenant.");
        }

        ITransaction transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.HashEqual(caseKey, "tenantId", tenantId));
        if (existingValue.HasValue)
        {
            transaction.AddCondition(Condition.StringEqual(bindingKey, existingValue));
        }
        else
        {
            transaction.AddCondition(Condition.KeyNotExists(bindingKey));
        }

        foreach (DerivedStoreBindingEntry entry in request.Entries)
        {
            string syntacticKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, entry.MemoryUnitId);
            string artifactKey = BuildSourceArtifactKey(tenantId, entry.MemoryUnitId);
            RedisValue artifactValue = await database.StringGetAsync(artifactKey).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!artifactValue.HasValue)
            {
                throw new DerivedStoreStateException("BINDING_SOURCE_ARTIFACT_MISSING", "A required durable source artifact is absent.");
            }

            DurableDerivedStoreSourceArtifact artifact = DeserializeRequired<DurableDerivedStoreSourceArtifact>(artifactValue, "BINDING_SOURCE_ARTIFACT_UNREADABLE");
            if (!string.Equals(artifact.TenantId, tenantId, StringComparison.Ordinal)
                || !string.Equals(artifact.CaseId, request.PriorCaseId, StringComparison.Ordinal)
                || !string.Equals(artifact.MemoryUnitId, entry.MemoryUnitId, StringComparison.Ordinal)
                || artifact.SourceBytes.Length == 0)
            {
                throw new DerivedStoreStateException("BINDING_SOURCE_ARTIFACT_MISMATCH", "A durable source artifact does not match the requested tenant, case, or MemoryUnit.");
            }

            RedisValue[] identity = await database.HashGetAsync(syntacticKey, ["tenantId", "caseId", "memoryUnitId"])
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (identity.Any(static value => !value.HasValue)
                || !string.Equals(identity[0].ToString(), tenantId, StringComparison.Ordinal)
                || !string.Equals(identity[1].ToString(), request.PriorCaseId, StringComparison.Ordinal)
                || !string.Equals(identity[2].ToString(), entry.MemoryUnitId, StringComparison.Ordinal))
            {
                throw new DerivedStoreStateException("BINDING_MEMORY_UNIT_MISMATCH", "A MemoryUnit is absent or belongs to another tenant or case.");
            }

            transaction.AddCondition(Condition.StringEqual(artifactKey, artifactValue));
            transaction.AddCondition(Condition.HashEqual(syntacticKey, "tenantId", tenantId));
            transaction.AddCondition(Condition.HashEqual(syntacticKey, "caseId", request.PriorCaseId));
            transaction.AddCondition(Condition.HashEqual(syntacticKey, "memoryUnitId", entry.MemoryUnitId));
        }

        var binding = new DerivedStoreBinding(
            tenantId,
            request.AssociationId,
            request.IntakeId,
            request.SourceVersion,
            request.PriorCaseId,
            request.ExpectedAttachmentCount,
            request.Entries.ToArray(),
            _timeProvider.GetUtcNow());
        _ = transaction.StringSetAsync(bindingKey, JsonSerializer.Serialize(binding, MemoriesJsonContext.Options));
        if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new DerivedStoreStateException("BINDING_CONCURRENT_CHANGE", "The binding inputs changed before atomic publication; retry finalization.");
        }

        return binding;
    }

    internal async Task<DerivedStoreCorrectionStartResult> StartCorrectionAsync(
        string tenantId,
        StartDerivedStoreCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);
        ValidateCorrectionRequest(request);
        IDatabase database = _redis.GetDatabase();
        DerivedStoreBinding binding = await LoadBindingAsync(
            database,
            tenantId,
            request.AssociationId,
            request.IntakeId,
            cancellationToken).ConfigureAwait(false);
        if (request.SourceVersion < binding.SourceVersion)
        {
            throw new DerivedStoreStateException("CORRECTION_BINDING_STALE", "The requested correction source version predates the finalized binding.");
        }

        if (!await IsSameTenantCaseAsync(database, BuildCaseKey(tenantId, request.CorrectedCaseId), tenantId, cancellationToken).ConfigureAwait(false))
        {
            throw new DerivedStoreStateException("CORRECTED_CASE_NOT_FOUND", "The corrected case is absent or is not owned by the tenant.");
        }

        string operationId = BuildOperationId(tenantId, request);
        string statusKey = BuildStatusKey(tenantId, operationId);
        RedisValue existingStatus = await database.StringGetAsync(statusKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (existingStatus.HasValue)
        {
            DerivedStoreCorrectionStatus existing = DeserializeRequired<DerivedStoreCorrectionStatus>(existingStatus, "CORRECTION_STATUS_CORRUPT");
            if (existing.State is DerivedStoreCorrectionState.Failed or DerivedStoreCorrectionState.TimedOut)
            {
                DateTimeOffset retryNow = _timeProvider.GetUtcNow();
                var retryStatus = existing with
                {
                    State = DerivedStoreCorrectionState.Pending,
                    EntriesInvalidated = 0,
                    EntriesRebuilt = 0,
                    VersionGuardSkipped = false,
                    DeadlineUtc = retryNow.Add(TerminalDeadline),
                    CompletedAtUtc = null,
                    FailureReasonCode = null,
                };
                ITransaction retryTransaction = database.CreateTransaction();
#pragma warning disable SER301 // The deployed Redis compatibility floor does not guarantee the Redis 8.4 conditional-set command.
                retryTransaction.AddCondition(Condition.StringEqual(statusKey, existingStatus));
                _ = retryTransaction.StringSetAsync(statusKey, JsonSerializer.Serialize(retryStatus, MemoriesJsonContext.Options));
#pragma warning restore SER301
                if (await retryTransaction.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    return new DerivedStoreCorrectionStartResult(
                        retryStatus,
                        ShouldSchedule: true,
                        BuildWorkflowInstanceId(retryStatus));
                }

                RedisValue racedRetry = await database.StringGetAsync(statusKey).WaitAsync(cancellationToken).ConfigureAwait(false);
                DerivedStoreCorrectionStatus racedStatus = DeserializeRequired<DerivedStoreCorrectionStatus>(racedRetry, "CORRECTION_STATUS_CORRUPT");
                return new DerivedStoreCorrectionStartResult(
                    racedStatus,
                    ShouldSchedule: racedStatus.State == DerivedStoreCorrectionState.Pending,
                    BuildWorkflowInstanceId(racedStatus));
            }

            return new DerivedStoreCorrectionStartResult(
                existing,
                ShouldSchedule: existing.State == DerivedStoreCorrectionState.Pending,
                BuildWorkflowInstanceId(existing));
        }

        RedisValue fenceValue = await database.StringGetAsync(BuildIntakeFenceKey(tenantId, request.AssociationId, request.IntakeId))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        bool alreadyConverged = fenceValue.HasValue
            && long.TryParse(fenceValue.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out long fence)
            && fence >= request.SourceVersion;
        DateTimeOffset now = _timeProvider.GetUtcNow();
        var status = new DerivedStoreCorrectionStatus(
            operationId,
            alreadyConverged ? DerivedStoreCorrectionState.NoOp : DerivedStoreCorrectionState.Pending,
            request.AssociationId,
            request.IntakeId,
            request.CorrectionId,
            request.SourceVersion,
            binding.PriorCaseId,
            request.CorrectedCaseId,
            EntriesInvalidated: 0,
            EntriesRebuilt: 0,
            VersionGuardSkipped: alreadyConverged,
            DeadlineUtc: now.Add(TerminalDeadline),
            CompletedAtUtc: alreadyConverged ? now : null,
            FailureReasonCode: null);
        bool created = await database.StringSetAsync(
                statusKey,
                JsonSerializer.Serialize(status, MemoriesJsonContext.Options),
                when: When.NotExists)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!created)
        {
            RedisValue racedStatus = await database.StringGetAsync(statusKey).WaitAsync(cancellationToken).ConfigureAwait(false);
            return new DerivedStoreCorrectionStartResult(
                DeserializeRequired<DerivedStoreCorrectionStatus>(racedStatus, "CORRECTION_STATUS_CORRUPT"),
                ShouldSchedule: false,
                BuildWorkflowInstanceId(status));
        }

        return new DerivedStoreCorrectionStartResult(
            status,
            ShouldSchedule: !alreadyConverged,
            BuildWorkflowInstanceId(status));
    }

    internal async Task<DerivedStoreCorrectionStatus?> GetCorrectionStatusAsync(
        string tenantId,
        string operationId,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);
        ValidateSafeToken(operationId, nameof(operationId), maxLength: 96);
        RedisValue value = await _redis.GetDatabase().StringGetAsync(BuildStatusKey(tenantId, operationId))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return value.HasValue
            ? DeserializeRequired<DerivedStoreCorrectionStatus>(value, "CORRECTION_STATUS_CORRUPT")
            : null;
    }

    internal async Task<DerivedStoreCorrectionStatus> ApplyCorrectionAsync(
        string tenantId,
        string operationId,
        Func<DurableDerivedStoreSourceArtifact, DerivedStoreCorrectionStatus, CancellationToken, Task> regenerateAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(regenerateAsync);
        IDatabase database = _redis.GetDatabase();
        DerivedStoreCorrectionStatus status = await GetCorrectionStatusAsync(tenantId, operationId, cancellationToken).ConfigureAwait(false)
            ?? throw new DerivedStoreStateException("CORRECTION_NOT_FOUND", "The correction operation does not exist for the tenant.");
        if (IsTerminal(status.State))
        {
            return status;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (now >= status.DeadlineUtc)
        {
            return await SaveStatusAsync(
                database,
                tenantId,
                status with
                {
                    State = DerivedStoreCorrectionState.TimedOut,
                    CompletedAtUtc = now,
                    FailureReasonCode = "correction_deadline_exceeded",
                },
                cancellationToken).ConfigureAwait(false);
        }

        status = await SaveStatusAsync(database, tenantId, status with { State = DerivedStoreCorrectionState.Running }, cancellationToken).ConfigureAwait(false);
        try
        {
            _ = await database.PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            _ = await _falkorDb.GetDatabase().PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            DerivedStoreBinding binding = await LoadBindingAsync(
                database,
                tenantId,
                status.AssociationId,
                status.IntakeId,
                cancellationToken).ConfigureAwait(false);
            if (binding.SourceVersion > status.SourceVersion)
            {
                throw new DerivedStoreStateException("CORRECTION_BINDING_STALE", "The finalized binding advanced beyond this correction operation.");
            }

            RedisValue fenceValue = await database.StringGetAsync(BuildIntakeFenceKey(tenantId, status.AssociationId, status.IntakeId))
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (fenceValue.HasValue
                && long.TryParse(fenceValue.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out long fence)
                && fence >= status.SourceVersion)
            {
                return await SaveStatusAsync(
                    database,
                    tenantId,
                    status with
                    {
                        State = DerivedStoreCorrectionState.NoOp,
                        VersionGuardSkipped = true,
                        CompletedAtUtc = _timeProvider.GetUtcNow(),
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            foreach (DerivedStoreBindingEntry entry in binding.Entries.OrderBy(static entry => entry.Ordinal))
            {
                _ = await MigrateUnitAsync(
                    database,
                    tenantId,
                    status,
                    entry.MemoryUnitId,
                    regenerateAsync,
                    cancellationToken).ConfigureAwait(false);
            }

            if (_timeProvider.GetUtcNow() >= status.DeadlineUtc)
            {
                return await SaveStatusAsync(
                    database,
                    tenantId,
                    status with
                    {
                        State = DerivedStoreCorrectionState.TimedOut,
                        CompletedAtUtc = _timeProvider.GetUtcNow(),
                        FailureReasonCode = "correction_deadline_exceeded",
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            ITransaction fenceTransaction = database.CreateTransaction();
            string bindingKey = BuildBindingKey(tenantId, status.AssociationId, status.IntakeId);
            fenceTransaction.AddCondition(Condition.StringEqual(
                bindingKey,
                JsonSerializer.Serialize(binding, MemoriesJsonContext.Options)));
            foreach (DerivedStoreBindingEntry entry in binding.Entries)
            {
                fenceTransaction.AddCondition(Condition.StringEqual(
                    BuildUnitFenceKey(tenantId, entry.MemoryUnitId),
                    status.SourceVersion.ToString(CultureInfo.InvariantCulture)));
            }

            _ = fenceTransaction.StringSetAsync(
                BuildIntakeFenceKey(tenantId, status.AssociationId, status.IntakeId),
                status.SourceVersion.ToString(CultureInfo.InvariantCulture));
            _ = fenceTransaction.StringSetAsync(
                bindingKey,
                JsonSerializer.Serialize(
                    binding with { PriorCaseId = status.CorrectedCaseId },
                    MemoriesJsonContext.Options));
            if (!await fenceTransaction.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new DerivedStoreStateException("CORRECTION_FENCE_COMMIT_FAILED", "The correction converged but its version fences could not be committed.");
            }

            int count = binding.Entries.Count;
            return await SaveStatusAsync(
                database,
                tenantId,
                status with
                {
                    State = DerivedStoreCorrectionState.Succeeded,
                    EntriesInvalidated = count,
                    EntriesRebuilt = count,
                    CompletedAtUtc = _timeProvider.GetUtcNow(),
                    FailureReasonCode = null,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            string code = exception is DerivedStoreStateException stateException
                ? stateException.Code.ToLowerInvariant()
                : "derived_store_correction_failed";
            return await SaveStatusAsync(
                database,
                tenantId,
                status with
                {
                    State = DerivedStoreCorrectionState.Failed,
                    CompletedAtUtc = _timeProvider.GetUtcNow(),
                    FailureReasonCode = code,
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    internal static string BuildSourceArtifactKey(string tenantId, string memoryUnitId)
        => $"{tenantId}:memories:derived-source:{HashIdentity(memoryUnitId)}";

    private async Task<bool> MigrateUnitAsync(
        IDatabase database,
        string tenantId,
        DerivedStoreCorrectionStatus status,
        string memoryUnitId,
        Func<DurableDerivedStoreSourceArtifact, DerivedStoreCorrectionStatus, CancellationToken, Task> regenerateAsync,
        CancellationToken cancellationToken)
    {
        string unitFenceKey = BuildUnitFenceKey(tenantId, memoryUnitId);
        RedisValue observedFence = await database.StringGetAsync(unitFenceKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (IsCompletedFenceAtOrAbove(observedFence, status.SourceVersion))
        {
            return false;
        }

        string claim = BuildUnitFenceClaim(status);
        if (!string.Equals(observedFence.ToString(), claim, StringComparison.Ordinal))
        {
            if (IsActiveForeignClaim(observedFence, status, _timeProvider.GetUtcNow()))
            {
                throw new DerivedStoreStateException("CORRECTION_UNIT_FENCE_OWNED", "A MemoryUnit correction fence is owned by another active operation.");
            }

            ITransaction claimTransaction = database.CreateTransaction();
            claimTransaction.AddCondition(observedFence.HasValue
                ? Condition.StringEqual(unitFenceKey, observedFence)
                : Condition.KeyNotExists(unitFenceKey));
            _ = claimTransaction.StringSetAsync(unitFenceKey, claim);
            if (!await claimTransaction.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new DerivedStoreStateException("CORRECTION_UNIT_FENCE_CONFLICT", "A MemoryUnit correction fence changed concurrently; retry the operation.");
            }
        }

        string artifactKey = BuildSourceArtifactKey(tenantId, memoryUnitId);
        RedisValue artifactValue = await database.StringGetAsync(artifactKey)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!artifactValue.HasValue)
        {
            throw new DerivedStoreStateException("CORRECTION_SOURCE_ARTIFACT_MISSING", "A required durable source artifact is absent.");
        }

        DurableDerivedStoreSourceArtifact artifact = DeserializeRequired<DurableDerivedStoreSourceArtifact>(artifactValue, "CORRECTION_SOURCE_ARTIFACT_UNREADABLE");
        if (!string.Equals(artifact.TenantId, tenantId, StringComparison.Ordinal)
            || !string.Equals(artifact.MemoryUnitId, memoryUnitId, StringComparison.Ordinal)
            || !string.Equals(artifact.CaseId, status.PriorCaseId, StringComparison.Ordinal)
            || artifact.SourceBytes.Length == 0)
        {
            throw new DerivedStoreStateException("CORRECTION_SOURCE_ARTIFACT_MISMATCH", "A durable source artifact does not match the correction binding.");
        }

        string syntacticKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);
        RedisValue currentCase = await database.HashGetAsync(syntacticKey, "caseId").WaitAsync(cancellationToken).ConfigureAwait(false);
        if (!currentCase.HasValue
            || (!string.Equals(currentCase.ToString(), status.PriorCaseId, StringComparison.Ordinal)
                && !string.Equals(currentCase.ToString(), status.CorrectedCaseId, StringComparison.Ordinal)))
        {
            throw new DerivedStoreStateException("CORRECTION_MEMORY_UNIT_CASE_MISMATCH", "A MemoryUnit is absent or belongs to an unexpected case.");
        }

        await regenerateAsync(artifact, status, cancellationToken).ConfigureAwait(false);
        await MigrateGraphAsync(tenantId, memoryUnitId, status.PriorCaseId, status.CorrectedCaseId, cancellationToken).ConfigureAwait(false);
        await MigrateDedupKeyAsync(database, tenantId, memoryUnitId, artifact.SourceUri, status.PriorCaseId, status.CorrectedCaseId, cancellationToken)
            .ConfigureAwait(false);

        ITransaction completionTransaction = database.CreateTransaction();
#pragma warning disable SER301 // The deployed Redis compatibility floor does not guarantee the Redis 8.4 conditional-set command.
        completionTransaction.AddCondition(Condition.StringEqual(unitFenceKey, claim));
        completionTransaction.AddCondition(Condition.StringEqual(artifactKey, artifactValue));
        _ = completionTransaction.StringSetAsync(
            artifactKey,
            JsonSerializer.Serialize(
                artifact with { CaseId = status.CorrectedCaseId },
                MemoriesJsonContext.Options));
        _ = completionTransaction.StringSetAsync(unitFenceKey, status.SourceVersion.ToString(CultureInfo.InvariantCulture));
#pragma warning restore SER301
        if (!await completionTransaction.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
        {
            RedisValue completedFence = await database.StringGetAsync(unitFenceKey).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!IsCompletedFenceAtOrAbove(completedFence, status.SourceVersion))
            {
                throw new DerivedStoreStateException("CORRECTION_UNIT_FENCE_COMMIT_FAILED", "A regenerated MemoryUnit could not commit its durable version fence.");
            }
        }

        return true;
    }

    private async Task MigrateGraphAsync(
        string tenantId,
        string memoryUnitId,
        string priorCaseId,
        string correctedCaseId,
        CancellationToken cancellationToken)
    {
        const string query = "MATCH (m:MemoryUnit {id: $memoryUnitId}) OPTIONAL MATCH (:Case {id: $priorCaseId})-[old:CONTAINS]->(m) DELETE old WITH m MATCH (next:Case {id: $correctedCaseId}) SET m.caseId = $correctedCaseId MERGE (next)-[:CONTAINS]->(m) RETURN m.id";
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["memoryUnitId"] = memoryUnitId,
            ["priorCaseId"] = priorCaseId,
            ["correctedCaseId"] = correctedCaseId,
        };
        var graph = new FalkorDB(_falkorDb.GetDatabase());
        _ = await graph.SelectGraph(tenantId).QueryAsync(query, parameters)
            .WaitAsync(GraphOperationTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task MigrateDedupKeyAsync(
        IDatabase database,
        string tenantId,
        string memoryUnitId,
        string sourceUri,
        string priorCaseId,
        string correctedCaseId,
        CancellationToken cancellationToken)
    {
        string priorKey = DedupKeyBuilder.BuildKey(tenantId, priorCaseId, sourceUri);
        string correctedKey = DedupKeyBuilder.BuildKey(tenantId, correctedCaseId, sourceUri);
        if (string.Equals(priorKey, correctedKey, StringComparison.Ordinal))
        {
            return;
        }

        RedisValue correctedValue = await database.StringGetAsync(correctedKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (correctedValue.HasValue && !string.Equals(correctedValue.ToString(), memoryUnitId, StringComparison.Ordinal))
        {
            throw new DerivedStoreStateException("CORRECTION_DEDUP_CONFLICT", "The corrected-case source mapping is already owned by another MemoryUnit.");
        }

        RedisValue priorValue = await database.StringGetAsync(priorKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (priorValue.HasValue && !string.Equals(priorValue.ToString(), memoryUnitId, StringComparison.Ordinal))
        {
            throw new DerivedStoreStateException("CORRECTION_DEDUP_PRIOR_MISMATCH", "The prior-case source mapping is owned by another MemoryUnit.");
        }

        _ = await database.StringSetAsync(correctedKey, memoryUnitId, when: When.NotExists)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (priorValue.HasValue)
        {
            _ = await database.KeyDeleteAsync(priorKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<DerivedStoreBinding> LoadBindingAsync(
        IDatabase database,
        string tenantId,
        string associationId,
        string intakeId,
        CancellationToken cancellationToken)
    {
        RedisValue value = await database.StringGetAsync(BuildBindingKey(tenantId, associationId, intakeId))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!value.HasValue)
        {
            throw new DerivedStoreStateException("CORRECTION_BINDING_NOT_FOUND", "No finalized binding exists for the tenant, association, and intake.");
        }

        DerivedStoreBinding binding = DeserializeRequired<DerivedStoreBinding>(value, "CORRECTION_BINDING_UNREADABLE");
        if (!string.Equals(binding.TenantId, tenantId, StringComparison.Ordinal)
            || !string.Equals(binding.AssociationId, associationId, StringComparison.Ordinal)
            || !string.Equals(binding.IntakeId, intakeId, StringComparison.Ordinal))
        {
            throw new DerivedStoreStateException("CORRECTION_BINDING_MISMATCH", "The finalized binding identity does not match the correction request.");
        }

        ValidateManifest(binding.ExpectedAttachmentCount, binding.Entries);
        return binding;
    }

    private static async Task<bool> IsSameTenantCaseAsync(
        IDatabase database,
        string caseKey,
        string tenantId,
        CancellationToken cancellationToken)
    {
        RedisValue value = await database.HashGetAsync(caseKey, "tenantId").WaitAsync(cancellationToken).ConfigureAwait(false);
        return value.HasValue && string.Equals(value.ToString(), tenantId, StringComparison.Ordinal);
    }

    private static async Task<DerivedStoreCorrectionStatus> SaveStatusAsync(
        IDatabase database,
        string tenantId,
        DerivedStoreCorrectionStatus status,
        CancellationToken cancellationToken)
    {
        await database.StringSetAsync(
                BuildStatusKey(tenantId, status.OperationId),
                JsonSerializer.Serialize(status, MemoriesJsonContext.Options))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return status;
    }

    private static bool BindingMatches(DerivedStoreBinding existing, FinalizeDerivedStoreBindingRequest request)
        => string.Equals(existing.AssociationId, request.AssociationId, StringComparison.Ordinal)
        && string.Equals(existing.IntakeId, request.IntakeId, StringComparison.Ordinal)
        && string.Equals(existing.PriorCaseId, request.PriorCaseId, StringComparison.Ordinal)
        && existing.ExpectedAttachmentCount == request.ExpectedAttachmentCount
        && existing.Entries.SequenceEqual(request.Entries);

    private static void ValidateFinalizeRequest(FinalizeDerivedStoreBindingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSafeToken(request.AssociationId, nameof(request.AssociationId));
        ValidateSafeToken(request.IntakeId, nameof(request.IntakeId));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PriorCaseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SourceVersion);
        ArgumentOutOfRangeException.ThrowIfNegative(request.ExpectedAttachmentCount);
        ValidateManifest(request.ExpectedAttachmentCount, request.Entries);
    }

    private static void ValidateManifest(int expectedAttachmentCount, IReadOnlyList<DerivedStoreBindingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count != expectedAttachmentCount + 1
            || entries.Count(static entry => entry.RecordKind == DerivedStoreRecordKind.Message) != 1
            || entries[0].RecordKind != DerivedStoreRecordKind.Message
            || entries[0].Ordinal != 0)
        {
            throw new DerivedStoreStateException("BINDING_MANIFEST_COUNT_INVALID", "The manifest must contain one message at ordinal zero plus the exact expected attachment count.");
        }

        var memoryUnitIds = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < entries.Count; index++)
        {
            DerivedStoreBindingEntry entry = entries[index];
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.MemoryUnitId);
            if (entry.Ordinal != index
                || (index > 0 && entry.RecordKind != DerivedStoreRecordKind.Attachment)
                || !memoryUnitIds.Add(entry.MemoryUnitId))
            {
                throw new DerivedStoreStateException("BINDING_MANIFEST_ORDER_INVALID", "The binding manifest contains a duplicate, missing, or out-of-order entry.");
            }
        }
    }

    private static void ValidateCorrectionRequest(StartDerivedStoreCorrectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSafeToken(request.AssociationId, nameof(request.AssociationId));
        ValidateSafeToken(request.IntakeId, nameof(request.IntakeId));
        ValidateSafeToken(request.CorrectionId, nameof(request.CorrectionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorrectedCaseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SourceVersion);
    }

    private static bool IsTerminal(DerivedStoreCorrectionState state)
        => state is DerivedStoreCorrectionState.Succeeded
            or DerivedStoreCorrectionState.NoOp
            or DerivedStoreCorrectionState.Failed
            or DerivedStoreCorrectionState.TimedOut;

    private static string BuildDiagnosticKey(string tenantId, DiagnosticStoreClass storeClass)
        => $"{tenantId}:memories:diagnostics:derived-store:{NormalizeClass(storeClass)}";

    internal static string BuildBindingKey(string tenantId, string associationId, string intakeId)
        => $"{tenantId}:memories:derived-binding:{HashIdentity(associationId)}:{HashIdentity(intakeId)}";

    private static string BuildCaseKey(string tenantId, string caseId) => $"{tenantId}:case:{caseId}";

    internal static string BuildIntakeFenceKey(string tenantId, string associationId, string intakeId)
        => $"{tenantId}:memories:derived-correction-fence:{HashIdentity(associationId)}:{HashIdentity(intakeId)}";

    internal static string BuildUnitFenceKey(string tenantId, string memoryUnitId)
        => $"{tenantId}:memories:derived-correction-unit-fence:{HashIdentity(memoryUnitId)}";

    internal static string BuildStatusKey(string tenantId, string operationId)
        => $"{tenantId}:memories:derived-correction-status:{operationId}";

    internal static string BuildOperationId(string tenantId, StartDerivedStoreCorrectionRequest request)
        => "derived-correction-" + HashIdentity(string.Join(
            "\n",
            tenantId,
            request.AssociationId,
            request.IntakeId,
            request.CorrectionId,
            request.SourceVersion.ToString(CultureInfo.InvariantCulture),
            request.CorrectedCaseId));

    private static string BuildWorkflowInstanceId(DerivedStoreCorrectionStatus status)
        => $"{status.OperationId}-attempt-{status.DeadlineUtc.UtcTicks.ToString(CultureInfo.InvariantCulture)}";

    private static string BuildUnitFenceClaim(DerivedStoreCorrectionStatus status)
        => string.Join(
            ':',
            "running",
            status.SourceVersion.ToString(CultureInfo.InvariantCulture),
            status.OperationId,
            status.DeadlineUtc.UtcTicks.ToString(CultureInfo.InvariantCulture));

    private static bool IsCompletedFenceAtOrAbove(RedisValue value, long sourceVersion)
        => value.HasValue
        && long.TryParse(value.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out long completedVersion)
        && completedVersion >= sourceVersion;

    private static bool IsActiveForeignClaim(
        RedisValue value,
        DerivedStoreCorrectionStatus status,
        DateTimeOffset now)
    {
        if (!value.HasValue)
        {
            return false;
        }

        string[] parts = value.ToString().Split(':', 4, StringSplitOptions.None);
        if (parts.Length != 4
            || !string.Equals(parts[0], "running", StringComparison.Ordinal)
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long claimedVersion)
            || !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out long deadlineTicks))
        {
            return false;
        }

        return claimedVersion >= status.SourceVersion
            && !string.Equals(parts[2], status.OperationId, StringComparison.Ordinal)
            && now.UtcTicks < deadlineTicks;
    }

    private static string NormalizeClass(DiagnosticStoreClass storeClass)
        => storeClass switch
        {
            DiagnosticStoreClass.VectorIndex => "vector-index",
            DiagnosticStoreClass.EmbeddingStore => "embedding-store",
            DiagnosticStoreClass.PromptContextCache => "prompt-context-cache",
            DiagnosticStoreClass.CandidateRankingCache => "candidate-ranking-cache",
            _ => throw new DerivedStoreStateException("DIAGNOSTIC_CLASS_INVALID", "The diagnostic class is unknown."),
        };

    private static string HashIdentity(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static T DeserializeRequired<T>(RedisValue value, string code)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value.ToString(), MemoriesJsonContext.Options)
                ?? throw new JsonException("The persisted JSON value was null.");
        }
        catch (JsonException exception)
        {
            throw new DerivedStoreStateException(code, exception.Message);
        }
    }

    private static void ValidateTenant(string tenantId)
    {
        try
        {
            TenantIdGuard.Validate(tenantId);
        }
        catch (ArgumentException exception)
        {
            throw new DerivedStoreStateException("TENANT_ID_INVALID", exception.Message);
        }
    }

    private static void ValidateSafeToken(string value, string parameterName, int maxLength = 256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maxLength || value.Any(static character => !IsSafeTokenCharacter(character)))
        {
            throw new ArgumentException("The value must be a bounded metadata-only safe token.", parameterName);
        }
    }

    private static bool IsSafeTokenCharacter(char character)
        => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':';

}
