// <copyright file="CaseService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

using BaUlid = ByteAether.Ulid.Ulid;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Serialization;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Manages case lifecycle: create, list, and get operations backed by Redis and FalkorDB.</summary>
internal sealed class CaseService
{
    private const int MaxMembersPerCase = 1000;

    private static readonly BaUlid.GenerationOptions UlidOptions = new()
    {
        Monotonicity = BaUlid.GenerationOptions.MonotonicityOptions.MonotonicIncrement,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly CaseActivityService _activityService;
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly IMemoriesCommandStore _commandStore;
    private readonly ICaseProjectionWorkflowScheduler _projectionWorkflowScheduler;
    private readonly IngestionWorkflowConfigurationCapture? _workflowConfigurationCapture;
    private readonly WorkflowTraceContextCapture? _workflowTraceContextCapture;
    private readonly ILogger<CaseService> _logger;

    public CaseService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        CaseActivityService activityService,
        DaprWorkflowClient workflowClient,
        IActorProxyFactory actorProxyFactory,
        ILogger<CaseService> logger,
        IMemoriesCommandStore? commandStore = null,
        ICaseProjectionWorkflowScheduler? projectionWorkflowScheduler = null,
        IngestionWorkflowConfigurationCapture? workflowConfigurationCapture = null,
        WorkflowTraceContextCapture? workflowTraceContextCapture = null)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _activityService = activityService;
        _actorProxyFactory = actorProxyFactory;
        _commandStore = commandStore ?? new InMemoryMemoriesCommandStore();
        _projectionWorkflowScheduler = projectionWorkflowScheduler
            ?? (workflowClient is null
                ? new InMemoryCaseProjectionWorkflowScheduler()
                : new DaprCaseProjectionWorkflowScheduler(workflowClient));
        _workflowConfigurationCapture = workflowConfigurationCapture;
        _workflowTraceContextCapture = workflowTraceContextCapture;
        _logger = logger;
    }

    public async Task<Case> CreateCaseAsync(CreateCaseInput input, CancellationToken cancellationToken)
    {
        string caseId = BaUlid.New(UlidOptions).ToString();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await _commandStore.AcceptAsync(
            input.TenantId,
            new CreateCaseCommand(input.TenantId, caseId, input.Name, input.Description, now),
            "system",
            cancellationToken).ConfigureAwait(false);

        await _projectionWorkflowScheduler.ScheduleAsync(
            nameof(CaseCreationProjectionWorkflow),
            $"case-create-{caseId}",
            new ProjectCaseCreatedInput(input.TenantId, caseId, input.Name, input.Description, now),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Created case {CaseId} in tenant {TenantId}",
            caseId,
            input.TenantId);

        return new Case(
            caseId,
            input.TenantId,
            input.Name,
            input.Description,
            CaseStatus.Active,
            now,
            now,
            MemoryUnitCount: 0);
    }

    /// <summary>Creates an annotation on an existing memory unit by scheduling an ingestion workflow.</summary>
    /// <param name="input">The annotation creation input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A tuple of the annotation MemoryUnit (status: Queued) and the workflow instance ID, or null if the target MU was not found/invalid.</returns>
    public async Task<(MemoryUnit Annotation, string WorkflowInstanceId)?> CreateAnnotationAsync(
        CreateAnnotationInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        MemoryUnit? targetMemoryUnit = await GetMemoryUnitAsync(
            input.TenantId,
            input.TargetMemoryUnitId,
            cancellationToken).ConfigureAwait(false);
        if (targetMemoryUnit is null)
        {
            return null;
        }

        if (!string.Equals(targetMemoryUnit.CaseId, input.CaseId, StringComparison.Ordinal))
        {
            return null;
        }

        if (!await IsMemoryUnitIndexedAsync(input.TenantId, input.TargetMemoryUnitId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("MEMORY_UNIT_NOT_INDEXED");
        }

        ErrorResponse? nestedError = CaseValidator.ValidateNotNestedAnnotation(targetMemoryUnit.Metadata);
        if (nestedError is not null)
        {
            throw new InvalidOperationException("NESTED_ANNOTATION_NOT_ALLOWED");
        }

        // Generate annotation MU ID
        string annotationMuId = BaUlid.New(UlidOptions).ToString();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string sourceUri = BuildAnnotationSourceUri(input.TargetMemoryUnitId, annotationMuId);

        // Build annotation metadata
        Dictionary<string, MetadataField> metadata = new()
        {
            ["_system.annotation_target"] = new MetadataField(input.TargetMemoryUnitId, MetadataOrigin.Human, 1.0f),
        };

        if (input.AnnotationType is not null)
        {
            metadata["_system.annotation_type"] = new MetadataField(input.AnnotationType, MetadataOrigin.Human, 1.0f);
        }

        await _commandStore.AcceptAsync(
            input.TenantId,
            new RequestAnnotationCommand(
                input.TenantId,
                input.CaseId,
                annotationMuId,
                input.TargetMemoryUnitId,
                sourceUri,
                input.Content,
                input.AnnotationType,
                input.IngestedBy,
                now),
            input.IngestedBy,
            cancellationToken).ConfigureAwait(false);

        string workflowInstanceId = await _projectionWorkflowScheduler.ScheduleAsync(
            nameof(AnnotationProjectionWorkflow),
            $"annotation-project-{annotationMuId}",
            new AnnotationProjectionInput(
                input.TenantId,
                input.CaseId,
                annotationMuId,
                input.TargetMemoryUnitId,
                sourceUri,
                input.Content,
                input.AnnotationType,
                input.IngestedBy,
                metadata,
                _workflowConfigurationCapture?.Capture() ?? new IngestionWorkflowConfiguration(),
                _workflowTraceContextCapture?.Capture()),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Created annotation {AnnotationMuId} on memory unit {TargetMuId} in case {CaseId} tenant {TenantId}",
            annotationMuId, input.TargetMemoryUnitId, input.CaseId, input.TenantId);

        var annotationMu = new MemoryUnit
        {
            Id = annotationMuId,
            TenantId = input.TenantId,
            CaseId = input.CaseId,
            Content = input.Content,
            ContentHash = string.Empty,
            SourceUri = sourceUri,
            SourceType = SourceType.Annotation,
            IngestedBy = input.IngestedBy,
            IngestedAt = now,
            LastUpdated = now,
            Status = MemoryUnitStatus.Queued,
            Metadata = metadata,
        };

        return (annotationMu, workflowInstanceId);
    }

    /// <summary>Gets a memory unit from the syntactic Redis hash store.</summary>
    /// <remarks>
    /// Story 5.4 AC2 — tertiary tenant-mismatch detection: after the hash is parsed, the record's
    /// stored <c>tenantId</c> field is compared to the requested <paramref name="tenantId"/>. A mismatch
    /// indicates data corruption or isolation breach; it is logged at Critical via
    /// <see cref="TenantMismatchMonitor"/> and the method returns <see langword="null"/> so the
    /// caller surfaces a standard 404 with no internal-state leakage.
    /// </remarks>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The parsed <see cref="MemoryUnit"/>, or <see langword="null"/> when not found or tenant mismatched.</returns>
    public async Task<MemoryUnit?> GetMemoryUnitAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string muKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);
        HashEntry[] entries = await db.HashGetAllAsync(muKey).ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return null;
        }

        string? storedTenantId = ReadStoredTenantId(entries);
        if (storedTenantId is not null && !string.Equals(storedTenantId, tenantId, StringComparison.Ordinal))
        {
            TenantMismatchMonitor.RecordMismatch(_logger, tenantId, storedTenantId, nameof(MemoryUnit), memoryUnitId);
            return null;
        }

        return ParseMemoryUnitFromHash(entries, tenantId, memoryUnitId);
    }

    /// <summary>Lists annotation memory units for a given target memory unit.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="memoryUnitId">The target memory unit identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of annotation MemoryUnit records.</returns>
    public async Task<List<MemoryUnit>> ListAnnotationsAsync(
        string tenantId, string memoryUnitId, CancellationToken cancellationToken)
    {
        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildListAnnotationIds(memoryUnitId);
        NFalkorDB.ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).ConfigureAwait(false);

        List<string> annotationIds = result
            .Select(record => record.Values[0].ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();

        if (annotationIds.Count == 0)
        {
            return [];
        }

        IDatabase db = _redis.GetDatabase();
        List<MemoryUnit> annotations = [];
        foreach (string annotationId in annotationIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MemoryUnit? mu = await GetMemoryUnitAsync(tenantId, annotationId, cancellationToken).ConfigureAwait(false);
            if (mu is not null)
            {
                annotations.Add(mu);
            }
        }

        return annotations;
    }

    public async Task<List<Case>> ListCasesAsync(string tenantId, int maxResults = 100, CancellationToken cancellationToken = default)
    {
        IDatabase db = _redis.GetDatabase();
        string pattern = $"{tenantId}:case:*";
        List<Case> candidateCases = [];

        IServer server = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints()[0]);
        foreach (RedisKey key in server.Keys(pattern: pattern, pageSize: maxResults))
        {
            string keyStr = key.ToString();
            if (keyStr.EndsWith(":activity", StringComparison.Ordinal) ||
                keyStr.EndsWith(":members", StringComparison.Ordinal))
            {
                continue;
            }

            HashEntry[] entries = await db.HashGetAllAsync(key).ConfigureAwait(false);
            if (entries.Length == 0)
            {
                continue;
            }

            string? storedTenantId = ReadStoredTenantId(entries);
            if (storedTenantId is not null && !string.Equals(storedTenantId, tenantId, StringComparison.Ordinal))
            {
                TenantMismatchMonitor.RecordMismatch(_logger, tenantId, storedTenantId, nameof(Case), keyStr);
                continue;
            }

            Case? parsed = ParseCaseFromHash(entries, tenantId);
            if (parsed is not null)
            {
                candidateCases.Add(parsed);
            }
        }

        List<Case> orderedCases = candidateCases
            .OrderByDescending(item => item.CreatedAt)
            .Take(maxResults)
            .ToList();

        if (orderedCases.Count == 0)
        {
            return orderedCases;
        }

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        for (int i = 0; i < orderedCases.Count; i++)
        {
            Case parsed = orderedCases[i];
            orderedCases[i] = parsed with
            {
                MemoryUnitCount = await GetMemoryUnitCountSafe(falkor, tenantId, parsed.Id).ConfigureAwait(false),
            };
        }

        return orderedCases;
    }

    /// <summary>Gets a case from the Redis hash store.</summary>
    /// <remarks>
    /// Story 5.4 AC2 — tertiary tenant-mismatch detection: after the hash is loaded, the record's
    /// stored <c>tenantId</c> field is compared to the requested <paramref name="tenantId"/>. A mismatch
    /// is logged Critical via <see cref="TenantMismatchMonitor"/> and the method returns
    /// <see langword="null"/> so callers return a standard 404.
    /// </remarks>
    public async Task<Case?> GetCaseAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string redisKey = $"{tenantId}:case:{caseId}";
        HashEntry[] entries = await db.HashGetAllAsync(redisKey).ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return null;
        }

        string? storedTenantId = ReadStoredTenantId(entries);
        if (storedTenantId is not null && !string.Equals(storedTenantId, tenantId, StringComparison.Ordinal))
        {
            TenantMismatchMonitor.RecordMismatch(_logger, tenantId, storedTenantId, nameof(Case), caseId);
            return null;
        }

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());

        // Graph ID is tenantId, NOT caseId — each tenant has one FalkorDB database
        int memoryUnitCount = await GetMemoryUnitCountSafe(falkor, tenantId, caseId).ConfigureAwait(false);

        Case? parsed = ParseCaseFromHash(entries, tenantId);
        return parsed is null ? null : parsed with { MemoryUnitCount = memoryUnitCount };
    }

    public async Task<CaseStatusDetail?> GetCaseStatusAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string caseKey = $"{tenantId}:case:{caseId}";
        HashEntry[] entries = await db.HashGetAllAsync(caseKey).ConfigureAwait(false);
        if (entries.Length == 0)
        {
            return null;
        }

        string? storedTenantId = ReadStoredTenantId(entries);
        if (storedTenantId is not null && !string.Equals(storedTenantId, tenantId, StringComparison.Ordinal))
        {
            TenantMismatchMonitor.RecordMismatch(_logger, tenantId, storedTenantId, nameof(Case), caseId);
            return null;
        }

        Case? parsedCase = ParseCaseFromHash(entries, tenantId);
        if (parsedCase is null)
        {
            return null;
        }

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        Case caseResult = parsedCase with
        {
            MemoryUnitCount = await GetMemoryUnitCountSafe(falkor, tenantId, caseId).ConfigureAwait(false),
        };

        Task<DateTimeOffset?> lastActivityTask = _activityService.GetLastActivityTimestampAsync(tenantId, caseId, cancellationToken);
        Task<int> failedCountTask = _activityService.GetFailedCountAsync(tenantId, caseId, cancellationToken);
        Task<int> memberCountTask = GetMemberCountAsync(tenantId, caseId, cancellationToken);
        Task<CaseIngestionCounts> countsTask = GetIngestionCountsSafe(tenantId, caseId);
        await Task.WhenAll(lastActivityTask, failedCountTask, memberCountTask, countsTask).ConfigureAwait(false);

        CaseIngestionCounts counts = countsTask.Result;
        return new CaseStatusDetail(
            caseResult.Id,
            caseResult.TenantId,
            caseResult.Name,
            caseResult.Description,
            caseResult.Status,
            caseResult.CreatedAt,
            caseResult.LastUpdated,
            caseResult.MemoryUnitCount,
            lastActivityTask.Result,
            IndexedCount: caseResult.MemoryUnitCount,
            FailedCount: failedCountTask.Result,
            MemberCount: memberCountTask.Result,
            DeletionStartedAt: ReadOptionalDateTimeOffset(entries, "deletionStartedAt"),
            QueuedCount: counts.Queued,
            ExtractingCount: counts.Extracting,
            EmbeddingCount: counts.Embedding,
            IndexingCount: counts.Indexing);
    }

    /// <summary>Story 6.3 FR10: O(1) actor read for in-flight counts. Actor unreachable → zero counts +
    /// warning log; never fails the whole status endpoint.</summary>
    private async Task<CaseIngestionCounts> GetIngestionCountsSafe(string tenantId, string caseId)
    {
        try
        {
            ICaseIngestionCounterActor? counter = _actorProxyFactory.CreateActorProxy<ICaseIngestionCounterActor>(
                new ActorId($"{tenantId}:{caseId}"),
                nameof(CaseIngestionCounterActor));
            if (counter is null)
            {
                return new CaseIngestionCounts(0, 0, 0, 0);
            }

            return await counter.GetCountsAsync().ConfigureAwait(false) ?? new CaseIngestionCounts(0, 0, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CaseIngestionCounterActor unreachable for {TenantId}:{CaseId}; reporting zero in-flight counts.", tenantId, caseId);
            return new CaseIngestionCounts(0, 0, 0, 0);
        }
    }

    /// <summary>Adds a member to a case using atomic HSETNX for idempotency.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="input">The member details.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A tuple of the member and whether it was newly created.</returns>
    public async Task<(CaseMember Member, bool Created)> AddMemberAsync(
        string tenantId, string caseId, AddCaseMemberInput input, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";

        // Enforce member count limit before attempting add
        long currentCount = await db.HashLengthAsync(membersKey).ConfigureAwait(false);
        if (currentCount >= MaxMembersPerCase)
        {
            RedisValue existingAtLimit = await db.HashGetAsync(membersKey, input.MemberId).ConfigureAwait(false);
            if (existingAtLimit.HasValue)
            {
                return (DeserializeStoredMemberOrThrow(existingAtLimit, tenantId, caseId, input.MemberId), false);
            }

            throw new InvalidOperationException($"Case '{caseId}' has reached the maximum of {MaxMembersPerCase} members.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var member = new CaseMember(input.MemberId, input.MemberType, now);
        string json = JsonSerializer.Serialize(
            PersistenceModelMapper.ToStored(member),
            MemoriesPersistenceJsonContext.Options);

        // Atomic idempotent add via HSETNX -- no TOCTOU race
        bool created = await db.HashSetAsync(membersKey, input.MemberId, json, When.NotExists).ConfigureAwait(false);

        if (created)
        {
            // Activity event ONLY for new members -- await to match CreateCaseAsync pattern
            _ = await _activityService.RecordEventAsync(
                tenantId, caseId, CaseActivityEventType.MemberAdded, "system",
                $"Member '{input.MemberId}' ({input.MemberType}) added", null, cancellationToken).ConfigureAwait(false);

            return (member, true);
        }

        // HSETNX returned false -- member already existed. Read the stored version.
        // Edge case: member could have been deleted between HSETNX and HashGet (rare race).
        RedisValue existing = await db.HashGetAsync(membersKey, input.MemberId).ConfigureAwait(false);
        if (!existing.HasValue)
        {
            // Member was deleted between HSETNX check and read. Retry the add.
            bool retriedCreated = await db.HashSetAsync(membersKey, input.MemberId, json, When.NotExists).ConfigureAwait(false);
            if (retriedCreated)
            {
                _ = await _activityService.RecordEventAsync(
                    tenantId, caseId, CaseActivityEventType.MemberAdded, "system",
                    $"Member '{input.MemberId}' ({input.MemberType}) added", null, cancellationToken).ConfigureAwait(false);
                return (member, true);
            }

            existing = await db.HashGetAsync(membersKey, input.MemberId).ConfigureAwait(false);
            if (!existing.HasValue)
            {
                throw new InvalidDataException(
                    $"Stored member '{input.MemberId}' for case '{caseId}' in tenant '{tenantId}' was unavailable during idempotency recovery.");
            }
        }

        CaseMember existingMember = DeserializeStoredMemberOrThrow(existing, tenantId, caseId, input.MemberId);
        return (existingMember, false);
    }

    /// <summary>Removes a member from a case.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="memberId">The member identifier to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the member was removed; <see langword="false"/> if not found.</returns>
    public async Task<bool> RemoveMemberAsync(
        string tenantId, string caseId, string memberId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";

        bool removed = await db.HashDeleteAsync(membersKey, memberId).ConfigureAwait(false);
        if (removed)
        {
            _ = await _activityService.RecordEventAsync(
                tenantId, caseId, CaseActivityEventType.MemberRemoved, "system",
                $"Member '{memberId}' removed", null, cancellationToken).ConfigureAwait(false);
        }

        return removed;
    }

    /// <summary>Lists all members of a case ordered by when they were added.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The list of case members ordered by <see cref="CaseMember.AddedAt"/>.</returns>
    public async Task<List<CaseMember>> ListMembersAsync(
        string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";

        HashEntry[] entries = await db.HashGetAllAsync(membersKey).ConfigureAwait(false);
        List<CaseMember> members = new(entries.Length);
        foreach (HashEntry entry in entries)
        {
            if (TryDeserializeStoredMember(entry.Value, tenantId, caseId, entry.Name.ToString(), out CaseMember? parsed) && parsed is not null)
            {
                members.Add(parsed);
            }
        }

        return members.OrderBy(m => m.AddedAt).ToList();
    }

    /// <summary>Batch-resolves case names from Redis hashes for a set of case IDs.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseIds">The case IDs to resolve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary mapping each case ID to its name (falls back to case ID if name is missing).</returns>
    public async Task<Dictionary<string, string>> ResolveNamesAsync(
        string tenantId, IEnumerable<string> caseIds, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        List<string> uniqueIds = caseIds.Distinct().ToList();
        if (uniqueIds.Count == 0)
        {
            return [];
        }

        IBatch batch = db.CreateBatch();
        Task<RedisValue>[] tasks = uniqueIds.Select(id =>
            batch.HashGetAsync($"{tenantId}:case:{id}", "name")).ToArray();
        batch.Execute();
        RedisValue[] names = await Task.WhenAll(tasks).ConfigureAwait(false);

        Dictionary<string, string> result = new(uniqueIds.Count);
        for (int i = 0; i < uniqueIds.Count; i++)
        {
            result[uniqueIds[i]] = names[i].HasValue ? (string)names[i]! : uniqueIds[i];
        }

        return result;
    }

    /// <summary>Gets the number of members in a case via HashLengthAsync.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The member count.</returns>
    public async Task<int> GetMemberCountAsync(
        string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";
        long count = await db.HashLengthAsync(membersKey).ConfigureAwait(false);
        return (int)count;
    }

    /// <summary>Deletes a memory unit from all three backends and records an activity event.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the memory unit was found and deleted; <see langword="false"/> if not found or belongs to a different case.</returns>
    public async Task<bool> DeleteMemoryUnitAsync(
        string tenantId, string caseId, string memoryUnitId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string muKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);

        // Verify MU exists by checking the syntactic hash key
        RedisValue storedCaseId = await db.HashGetAsync(muKey, "caseId").ConfigureAwait(false);
        if (!storedCaseId.HasValue)
        {
            return false;
        }

        // Verify MU belongs to the specified case
        if (!string.Equals(storedCaseId.ToString(), caseId, StringComparison.Ordinal))
        {
            return false;
        }

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());

        // Cascade: delete annotations before deleting the target MU
        (string listQuery, IDictionary<string, object> listParams) = _graphQueryBuilder.BuildListAnnotationIds(memoryUnitId);
        NFalkorDB.ResultSet annotationResult = await falkor.SelectGraph(tenantId).QueryAsync(listQuery, listParams).ConfigureAwait(false);
        List<string> annotationIds = annotationResult
            .Select(record => record.Values[0].ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();

        await _commandStore.AcceptAsync(
            tenantId,
            new DeleteMemoryUnitCommand(tenantId, caseId, memoryUnitId, annotationIds, DateTimeOffset.UtcNow),
            "system",
            cancellationToken).ConfigureAwait(false);

        await _projectionWorkflowScheduler.ScheduleAsync(
            nameof(MemoryUnitDeletionProjectionWorkflow),
            $"memory-unit-delete-{memoryUnitId}",
            new MemoryUnitDeletionProjectionInput(tenantId, caseId, memoryUnitId, annotationIds),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Deleted memory unit {MemoryUnitId} from case {CaseId} in tenant {TenantId}",
            memoryUnitId, caseId, tenantId);

        return true;
    }

    /// <summary>Deletes a case and all its memory units from all backends.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the case was found and deleted; <see langword="false"/> if not found.</returns>
    public async Task<bool> DeleteCaseAsync(
        string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string caseKey = $"{tenantId}:case:{caseId}";

        // Verify case exists
        bool exists = await db.KeyExistsAsync(caseKey).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Find all memory unit IDs from graph
        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string listQuery, IDictionary<string, object> listParams) = _graphQueryBuilder.BuildListCaseMemoryUnitIds(caseId);
        NFalkorDB.ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(listQuery, listParams).ConfigureAwait(false);
        List<string> memoryUnitIds = result
            .Select(record => record.Values[0].ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Select(value => value!)
            .ToList();

        await _commandStore.AcceptAsync(
            tenantId,
            new DeleteCaseCommand(tenantId, caseId, memoryUnitIds, DateTimeOffset.UtcNow),
            "system",
            cancellationToken).ConfigureAwait(false);

        await _projectionWorkflowScheduler.ScheduleAsync(
            nameof(CaseDeletionProjectionWorkflow),
            $"case-delete-{caseId}",
            new CaseDeletionProjectionInput(tenantId, caseId, memoryUnitIds),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Deleted case {CaseId} with {MemoryUnitCount} memory units from tenant {TenantId}",
            caseId, memoryUnitIds.Count, tenantId);

        return true;
    }

    private CaseMember DeserializeStoredMemberOrThrow(
        RedisValue value,
        string tenantId,
        string caseId,
        string memberId)
    {
        if (TryDeserializeStoredMember(value, tenantId, caseId, memberId, out CaseMember? member) && member is not null)
        {
            return member;
        }

        throw new InvalidDataException(
            $"Stored member '{memberId}' for case '{caseId}' in tenant '{tenantId}' contains invalid JSON.");
    }

    private bool TryDeserializeStoredMember(
        RedisValue value,
        string tenantId,
        string caseId,
        string memberId,
        out CaseMember? member)
    {
        string payload = value.ToString();

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetRequiredJsonString(root, "memberId", out string? storedMemberId) ||
                !TryGetRequiredJsonString(root, "memberType", out _) ||
                !TryGetRequiredJsonString(root, "addedAt", out _))
            {
                LogCorruptMemberRecord(tenantId, caseId, memberId, "Required properties are missing.");
                member = null;
                return false;
            }

            StoredCaseMember? storedMember = JsonSerializer.Deserialize<StoredCaseMember>(
                payload,
                MemoriesPersistenceJsonContext.Options);
            member = storedMember is null ? null : PersistenceModelMapper.ToContract(storedMember);
            if (member is null ||
                !string.Equals(member.MemberId, storedMemberId, StringComparison.Ordinal) ||
                !string.Equals(member.MemberId, memberId, StringComparison.Ordinal) ||
                member.AddedAt == default)
            {
                LogCorruptMemberRecord(tenantId, caseId, memberId, "Stored JSON does not match the hash entry.");
                member = null;
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            LogCorruptMemberRecord(tenantId, caseId, memberId, "Stored JSON is invalid.", ex);
            member = null;
            return false;
        }
    }

    private void LogCorruptMemberRecord(
        string tenantId,
        string caseId,
        string memberId,
        string reason,
        Exception? exception = null)
    {
        if (exception is null)
        {
            _logger.LogWarning(
                "Skipping corrupt member record {MemberId} for case {CaseId} in tenant {TenantId}: {Reason}",
                memberId,
                caseId,
                tenantId,
                reason);
            return;
        }

        _logger.LogWarning(
            exception,
            "Skipping corrupt member record {MemberId} for case {CaseId} in tenant {TenantId}: {Reason}",
            memberId,
            caseId,
            tenantId,
            reason);
    }

    private static bool TryGetRequiredJsonString(JsonElement root, string propertyName, out string? value)
    {
        if (root.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private async Task<int> GetMemoryUnitCountSafe(NFalkorDB.FalkorDB falkor, string tenantId, string caseId)
    {
        try
        {
            (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildCountCaseMemoryUnits(caseId);
            NFalkorDB.ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).ConfigureAwait(false);
            if (result.Count > 0)
            {
                NFalkorDB.Record record = result.First();
                return Convert.ToInt32(record.Values[0]);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory unit count for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return 0;
        }
    }

    private async Task<bool> IsMemoryUnitIndexedAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        bool semanticExists = await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId)).ConfigureAwait(false)
            || await AnySemanticChunkExistsAsync(tenantId, memoryUnitId).ConfigureAwait(false);
        if (!semanticExists)
        {
            return false;
        }

        return await MemoryUnitGraphNodeExistsAsync(tenantId, memoryUnitId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> AnySemanticChunkExistsAsync(string tenantId, string memoryUnitId)
    {
        foreach (EndPoint endpoint in _redis.GetEndPoints())
        {
            IServer server = _redis.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            await foreach (RedisKey key in server.KeysAsync(pattern: IndexSchemaDefinitions.BuildSemanticChunkKeyPattern(tenantId, memoryUnitId), pageSize: 100))
            {
                if (IndexSchemaDefinitions.TryParseSemanticChunkKey(tenantId, key, out string parsedId, out _)
                    && string.Equals(parsedId, memoryUnitId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task<bool> MemoryUnitGraphNodeExistsAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildCheckMemoryUnitExists(memoryUnitId);
        NFalkorDB.ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).ConfigureAwait(false);
        return result.Count > 0;
    }

    // Promoted to internal static (Story 8.3 Task 1.2) so TenantExportService can reuse the
    // canonical hash->record mapping without drifting from the read path.
    internal static Case? ParseCaseFromHash(HashEntry[] entries, string tenantId)
    {
        Dictionary<string, string> fields = [];
        foreach (HashEntry entry in entries)
        {
            fields[entry.Name!] = entry.Value!;
        }

        if (!fields.TryGetValue("id", out string? id) || string.IsNullOrEmpty(id))
        {
            return null;
        }

        _ = fields.TryGetValue("tenantId", out string? storedTenantId);
        _ = fields.TryGetValue("name", out string? name);
        _ = fields.TryGetValue("description", out string? description);
        _ = fields.TryGetValue("status", out string? statusStr);
        _ = fields.TryGetValue("createdAt", out string? createdAtStr);
        _ = fields.TryGetValue("lastUpdated", out string? lastUpdatedStr);

        CaseStatus status = statusStr switch
        {
            _ when string.Equals(statusStr, "deleting", StringComparison.OrdinalIgnoreCase) => CaseStatus.Deleting,
            _ when string.Equals(statusStr, "closed", StringComparison.OrdinalIgnoreCase) => CaseStatus.Closed,
            _ => CaseStatus.Active,
        };

        _ = DateTimeOffset.TryParse(createdAtStr, out DateTimeOffset createdAt);
        _ = DateTimeOffset.TryParse(lastUpdatedStr, out DateTimeOffset lastUpdated);

        return new Case(
            id,
            storedTenantId ?? tenantId,
            name ?? string.Empty,
            string.IsNullOrEmpty(description) ? null : description,
            status,
            createdAt,
            lastUpdated,
            MemoryUnitCount: 0);
    }

    // Promoted to internal static (Story 8.3 Task 1.2) so TenantExportService can reuse the
    // canonical hash->record mapping without drifting from the read path.
    internal static MemoryUnit? ParseMemoryUnitFromHash(HashEntry[] entries, string tenantId, string fallbackId)
    {
        Dictionary<string, string> fields = [];
        foreach (HashEntry entry in entries)
        {
            fields[entry.Name!] = entry.Value!;
        }

        string id = fields.TryGetValue("id", out string? storedId) && !string.IsNullOrWhiteSpace(storedId)
            ? storedId
            : fallbackId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        _ = fields.TryGetValue("caseId", out string? caseId);
        _ = fields.TryGetValue("content", out string? content);
        _ = fields.TryGetValue("contentHash", out string? contentHash);
        _ = fields.TryGetValue("sourceUri", out string? sourceUri);
        _ = fields.TryGetValue("sourceType", out string? sourceTypeStr);
        _ = fields.TryGetValue("ingestedBy", out string? ingestedBy);
        _ = fields.TryGetValue("ingestedAt", out string? ingestedAtStr);
        _ = fields.TryGetValue("lastUpdated", out string? lastUpdatedStr);
        _ = fields.TryGetValue("status", out string? statusStr);
        _ = fields.TryGetValue("metadataJson", out string? metadataJson);
        _ = fields.TryGetValue("embeddingProvider", out string? embeddingProvider);
        // Story 5.5 FR70: memory units indexed before 5.5 have no embeddingModel field;
        // missing → null (not a mismatch — legacy data pre-dates the field).
        _ = fields.TryGetValue("embeddingModel", out string? embeddingModel);
        _ = fields.TryGetValue("embeddingDimensions", out string? embeddingDimensionsStr);
        // Story 6.3: future-extension hook — failed-units written by PersistFailedUnitActivity write
        // a failureDetailsJson field; the indexed-MU hash never has one today, but reading it here lets
        // the same parser serve both code paths if dual-write is added in Phase 2.
        _ = fields.TryGetValue("failureDetailsJson", out string? failureDetailsJson);

        _ = Enum.TryParse(sourceTypeStr, ignoreCase: true, out SourceType sourceType);
        MemoryUnitStatus status = Enum.TryParse(statusStr, ignoreCase: true, out MemoryUnitStatus parsedStatus)
            ? parsedStatus
            : MemoryUnitStatus.Indexed;
        _ = DateTimeOffset.TryParse(ingestedAtStr, out DateTimeOffset ingestedAt);
        _ = DateTimeOffset.TryParse(lastUpdatedStr, out DateTimeOffset lastUpdated);
        int? embeddingDimensions = int.TryParse(embeddingDimensionsStr, out int parsedDimensions)
            ? parsedDimensions
            : null;

        return new MemoryUnit
        {
            Id = id,
            TenantId = tenantId,
            CaseId = caseId ?? string.Empty,
            Content = content ?? string.Empty,
            ContentHash = contentHash ?? string.Empty,
            SourceUri = sourceUri ?? string.Empty,
            SourceType = sourceType,
            IngestedBy = ingestedBy ?? string.Empty,
            IngestedAt = ingestedAt,
            LastUpdated = lastUpdated,
            Status = status,
            Metadata = ParseMetadata(metadataJson),
            EmbeddingProvider = string.IsNullOrWhiteSpace(embeddingProvider) ? null : embeddingProvider,
            EmbeddingModel = string.IsNullOrWhiteSpace(embeddingModel) ? null : embeddingModel,
            EmbeddingDimensions = embeddingDimensions,
            FailureDetails = ParseFailureDetails(failureDetailsJson),
        };
    }

    private static FailureDetails? ParseFailureDetails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            StoredFailureDetails? stored = JsonSerializer.Deserialize<StoredFailureDetails>(
                json,
                MemoriesPersistenceJsonContext.Options);
            return stored is null ? null : PersistenceModelMapper.ToContract(stored);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, MetadataField> ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return [];
        }

        try
        {
            Dictionary<string, StoredMetadataField>? stored = JsonSerializer.Deserialize<Dictionary<string, StoredMetadataField>>(
                metadataJson,
                MemoriesPersistenceJsonContext.Options);
            return stored is null ? [] : PersistenceModelMapper.ToContract(stored);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string BuildAnnotationSourceUri(string targetMemoryUnitId, string annotationMemoryUnitId)
        => $"annotation:{targetMemoryUnitId}:{annotationMemoryUnitId}";

    /// <summary>Reads the <c>tenantId</c> field from a parsed hash, or returns <see langword="null"/>
    /// when the field is absent (legacy records written before Story 5.4) or empty.
    /// Used by mismatch detection in <see cref="GetMemoryUnitAsync"/> and <see cref="GetCaseAsync"/>.</summary>
    private static string? ReadStoredTenantId(HashEntry[] entries)
    {
        foreach (HashEntry entry in entries)
        {
            if (!string.Equals(entry.Name.ToString(), "tenantId", StringComparison.Ordinal))
            {
                continue;
            }

            string value = entry.Value.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private static DateTimeOffset? ReadOptionalDateTimeOffset(HashEntry[] entries, string fieldName)
    {
        foreach (HashEntry entry in entries)
        {
            if (!string.Equals(entry.Name.ToString(), fieldName, StringComparison.Ordinal))
            {
                continue;
            }

            return DateTimeOffset.TryParse(entry.Value.ToString(), out DateTimeOffset parsed)
                ? parsed
                : null;
        }

        return null;
    }
}
