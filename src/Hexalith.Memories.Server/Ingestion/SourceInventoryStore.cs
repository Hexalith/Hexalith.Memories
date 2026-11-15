namespace Hexalith.Memories.Server.Ingestion;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Queries;
using StackExchange.Redis;

/// <summary>Durable bounded source inventory. Never derives completeness from a dedup pointer or the maintenance registry.</summary>
internal sealed class SourceInventoryStore([FromKeyedServices("redis")] IConnectionMultiplexer redis, IQueryCursorCodec cursorCodec,
    Microsoft.Extensions.Options.IOptions<IngestionSettings>? settings = null)
{
    private readonly IngestionSettings _ownershipSettings = settings?.Value ?? new();

    internal const int Capacity = 1024;
    internal const int MaximumPageBytes = 1024 * 1024;
    private const int MaximumDocumentCharacters = 4 * 1024 * 1024;
    private const string CompareExchange = "if redis.call('GET',KEYS[1]) == ARGV[1] then redis.call('SET',KEYS[1],ARGV[2]); return 1 else return 0 end";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Persists admission before scheduling; no failure or lost acknowledgement removes this obligation.</summary>
    internal Task AdmitAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken, bool requireExistingAdmission = false)
        => AdmitWithOwnershipAsync(instanceId, input, cancellationToken, requireExistingAdmission);

    /// <summary>Reports only an acknowledged admission created by this call's successful CAS.</summary>
    internal async Task<bool> AdmitWithOwnershipAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken, bool requireExistingAdmission = false)
    {
        IngestionIdentityGuard.Input(input, instanceId);
        bool created = false;
        await ChangeAsync(input.TenantId, input.SourceUri, current =>
        {
            created = false;
            if (current.AdmissionClosed) { throw new InvalidOperationException("source-inventory-admission-closed"); }
            SourceInventoryEntry? prior = (current.Entries ?? []).FirstOrDefault(entry => entry.WorkflowInstanceId == instanceId);
            if (prior is not null)
            {
                if (prior.MemoryUnitId is not null || prior.TerminalVerified)
                {
                    throw new InvalidOperationException("source-inventory-producer-identity-terminal");
                }
                if (prior.CaseId != input.CaseId || prior.IdempotencyTokenHash != TokenHash(input.IdempotencyToken)
                    || prior.SourcePayloadReference != IngestionPayloadClaimCheck.GetSourceReference(instanceId, input)
                    || prior.SourceRevisionNumber != (input.RefreshExistingSource ? input.SourceRevisionNumber : null)
                    || prior.RefreshIdentityDigest != (input.RefreshExistingSource ? input.RefreshIdentityDigest : null))
                {
                    throw new InvalidOperationException("source-inventory-admission-mismatch");
                }
                return current;
            }
            if (requireExistingAdmission) { throw new InvalidOperationException("source-inventory-existing-workflow-identity"); }
            if ((current.Entries?.Count ?? 0) >= Capacity) { throw new InvalidOperationException("source-inventory-capacity"); }
            created = true;
            return current with { FreshCandidateWorkflows = [.. current.FreshCandidateWorkflows ?? [], instanceId],
                Entries = [.. current.Entries ?? [], new(input.CaseId, instanceId, TokenHash(input.IdempotencyToken), SchedulingState: "Preparing",
                SourcePayloadReference: IngestionPayloadClaimCheck.GetSourceReference(instanceId, input),
                RefreshProtocolVersion: input.RefreshExistingSource && input.RefreshIdentityDigest is not null ? SourceRefreshProtocol.Version : null,
                SourceRevisionNumber: input.RefreshExistingSource ? input.SourceRevisionNumber : null,
                RefreshIdentityDigest: input.RefreshExistingSource ? input.RefreshIdentityDigest : null)] };
        }, cancellationToken, freshAdmission: instanceId).ConfigureAwait(false);
        return created;
    }

    /// <summary>Finds the newest potential failed producer; only conclusively never-scheduled retries can be skipped.</summary>
    internal async Task<SourceInventoryEntry?> FindFailedRetryAsync(FailedUnitRecord record, CancellationToken token)
    {
        SourceInventorySnapshot current = await ReadSnapshotAsync(record.TenantId, record.SourceUri, token).ConfigureAwait(false);
        if (current.AdmissionClosed) { throw new FailedRetryEligibilityException("source-inventory-admission-closed"); }
        SourceInventoryEntry? entry = LatestRetryPredecessor(current, record.CaseId, record.MemoryUnitId);
        if (entry is null) { return null; }
        if (entry.CaseId != record.CaseId || entry.WorkflowInstanceId is null || entry.SchedulingState is not ("Scheduled" or "Scheduling")
            || entry.TerminalVerified || entry.State == "Deleted" || (entry.CandidateMemoryUnitId ?? entry.FailedRetryMemoryUnitId) != record.MemoryUnitId
            || entry.SourcePayloadReference != record.SourcePayloadReference)
        { throw new FailedRetryEligibilityException("failed-retry-inventory-proof-mismatch"); }
        return entry;
    }

    /// <summary>Adds an independent failed-attempt generation without resetting or removing its predecessor's source/output obligations.</summary>
    internal Task AdmitFailedRetryAsync(string instanceId, IngestionInput input, SourceInventoryEntry? previous, CancellationToken token)
    {
        IngestionIdentityGuard.Input(input, instanceId);
        FailedIngestionRetry retry = input.FailedRetry ?? throw new FailedRetryEligibilityException("failed-retry-context-required");
        return ChangeAsync(input.TenantId, input.SourceUri, current =>
        {
            if (current.AdmissionClosed) { throw new FailedRetryEligibilityException("source-inventory-admission-closed"); }
            SourceInventoryEntry? latest = LatestRetryPredecessor(current, input.CaseId, retry.MemoryUnitId);
            if (latest != previous || (previous is not null && (previous.WorkflowInstanceId != retry.PreviousWorkflowInstanceId || previous.CaseId != input.CaseId))
                || (current.Entries ?? []).Any(entry => entry.WorkflowInstanceId == instanceId))
            { throw new FailedRetryEligibilityException("failed-retry-inventory-changed"); }
            return current with
            {
                CoverageComplete = previous is null ? false : current.CoverageComplete,
                CoverageEvidenceId = previous is null ? null : current.CoverageEvidenceId,
                IncompleteReason = previous is null ? "legacy-failed-retry-reconciliation-required" : current.IncompleteReason,
                Entries = [.. current.Entries ?? [], new(input.CaseId, instanceId, TokenHash(input.IdempotencyToken),
                    SchedulingState: "Preparing",
                    SourcePayloadReference: IngestionPayloadClaimCheck.GetSourceReference(instanceId, input)) { FailedRetryMemoryUnitId = retry.MemoryUnitId }],
            };
        }, token, retry.MemoryUnitId);
    }

    private static SourceInventoryEntry? LatestRetryPredecessor(SourceInventorySnapshot snapshot, string caseId, string unit)
        => (snapshot.Entries ?? []).LastOrDefault(entry =>
            (entry.CandidateMemoryUnitId == unit || entry.MemoryUnitId == unit || entry.WorkflowInstanceId == unit || entry.FailedRetryMemoryUnitId == unit)
            && !(entry.CaseId == caseId && entry.WorkflowInstanceId is not null && entry.FailedRetryMemoryUnitId == unit
                && entry.SchedulingState == "Unscheduled" && entry.CandidateMemoryUnitId is null && entry.MemoryUnitId is null
                && (entry.State == "Admitted" || (entry.State == "Deleted" && entry.TerminalVerified))));

    /// <summary>Commits scheduling provenance before the only remote scheduling call.</summary>
    internal Task SetSchedulingAsync(string tenant, string source, string workflow, string scheduling, CancellationToken token)
        => ChangeAsync(tenant, source, current =>
        {
            SourceInventoryEntry entry = (current.Entries ?? []).Single(item => item.WorkflowInstanceId == workflow);
            bool valid = (entry.SchedulingState, scheduling) is ("Preparing", "Scheduling") or ("Scheduling", "Scheduled")
                or ("Preparing", "Unscheduled");
            if (entry.SchedulingState == scheduling) { return current; }
            if (!valid) { throw new InvalidOperationException("source-inventory-scheduling-conflict"); }
            return current with { Entries = [.. current.Entries!.Select(item => item == entry ? item with { SchedulingState = scheduling } : item)] };
        }, token);

    /// <summary>Settles only an admission whose persisted state proves it never crossed the scheduling boundary.</summary>
    internal Task SetUnscheduledIfPreparingAsync(string workflow, IngestionInput input, CancellationToken token)
    {
        IngestionIdentityGuard.Input(input, workflow);
        if (input.SourceType == SourceType.Url && (input.ContentBytes is { Length: > 0 } || input.PayloadReference is not null)
            || input.SourceType != SourceType.Url && input.ContentBytes is not { Length: > 0 } && input.PayloadReference is null)
            return Task.CompletedTask;
        WorkflowPayloadReference? sourceReference = IngestionPayloadClaimCheck.GetSourceReference(workflow, input);
        if (input.PayloadReference is { } reference && input.ContentBytes is { Length: > 0 })
        {
            WorkflowPayloadReference? inline = IngestionPayloadClaimCheck.GetSourceReference(workflow, input with { PayloadReference = null });
            if (inline is null || inline.ByteLength != reference.ByteLength || !string.Equals(inline.Sha256Hash, reference.Sha256Hash, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;
        }
        return ChangeAsync(input.TenantId, input.SourceUri, current =>
        {
            SourceInventoryEntry? entry = (current.Entries ?? []).SingleOrDefault(item => item.WorkflowInstanceId == workflow);
            if (entry is null || entry.SchedulingState != "Preparing" || entry.State != "Admitted"
                || entry.CandidateMemoryUnitId is not null || entry.MemoryUnitId is not null || entry.TerminalVerified
                || entry.CaseId != input.CaseId || entry.IdempotencyTokenHash != TokenHash(input.IdempotencyToken)
                || entry.SourcePayloadReference != sourceReference
                || entry.SourceRevisionNumber != (input.RefreshExistingSource ? input.SourceRevisionNumber : null)
                || entry.RefreshIdentityDigest != (input.RefreshExistingSource ? input.RefreshIdentityDigest : null)
                || entry.FailedRetryMemoryUnitId != input.FailedRetry?.MemoryUnitId)
                return current;
            return current with { Entries = [.. current.Entries!.Select(item => item == entry ? item with { SchedulingState = "Unscheduled" } : item)] };
        }, token);
    }

    /// <summary>Retains producer artifacts before any activity can materialize content.</summary>
    internal Task RecordCandidateAsync(string tenant, string source, string workflow, string candidate, CancellationToken token)
        => ChangeAsync(tenant, source, current =>
        {
            SourceInventoryEntry entry = (current.Entries ?? []).Single(item => item.WorkflowInstanceId == workflow);
            if ((entry.CandidateMemoryUnitId is not null && entry.CandidateMemoryUnitId != candidate)
                || (entry.FailedRetryMemoryUnitId is not null && entry.FailedRetryMemoryUnitId != candidate))
            {
                throw new InvalidOperationException("source-inventory-candidate-mismatch");
            }
            return entry.CandidateMemoryUnitId == candidate ? current : current with
            { Entries = [.. current.Entries!.Select(item => item == entry ? item with { CandidateMemoryUnitId = candidate } : item)],
                FreshCandidateWorkflows = current.FreshCandidateWorkflows?.Where(item => item != workflow).ToArray() };
        }, token, freshCandidate: (workflow, candidate));

    /// <summary>Retains failed output only after independently scoped scheduler/workflow proof and actual artifact deletion.</summary>
    internal async Task RecordFailedDeletionAsync(string tenant, string source, SourceInventoryEntry expected, string candidate, CancellationToken token)
    {
        await RetainAliasAsync(tenant, source, candidate, token).ConfigureAwait(false);
        await ChangeAsync(tenant, source, current =>
        {
            SourceInventoryEntry entry = (current.Entries ?? []).Single(item => item.WorkflowInstanceId == expected.WorkflowInstanceId);
            if (entry.CaseId != expected.CaseId || entry.CandidateMemoryUnitId != expected.CandidateMemoryUnitId
                || (entry.MemoryUnitId is not null && entry.MemoryUnitId != candidate))
            { throw new InvalidOperationException("source-inventory-failed-identity-mismatch"); }
            return current with { Entries = [.. current.Entries!.Select(item => item == entry ? item with
                { MemoryUnitId = candidate, State = "Deleted", TerminalVerified = true } : item)] };
        }, token).ConfigureAwait(false);
    }

    internal async Task EnsureAdmissionOpenAsync(string tenantId, string sourceUri, CancellationToken cancellationToken)
    {
        SourceInventorySnapshot current = await ReadSnapshotAsync(tenantId, sourceUri, cancellationToken).ConfigureAwait(false);
        if (current.AdmissionClosed) { throw new InvalidOperationException("source-inventory-admission-closed"); }
    }

    /// <summary>Closes producer admission before consumers enumerate obligations.</summary>
    internal Task FenceAsync(string tenantId, string sourceUri, bool permanent, CancellationToken cancellationToken)
        => ChangeAsync(tenantId, sourceUri, current => current with { AdmissionClosed = true, Permanent = current.Permanent || permanent, LastReopenedFromVersion = null }, cancellationToken);

    /// <summary>Reopens only an exactly observed, fully settled transient generation.</summary>
    internal Task ReopenAsync(string tenantId, string sourceUri, string expectedVersion, CancellationToken cancellationToken)
        => ChangeAsync(tenantId, sourceUri, current =>
        {
            if (!current.Permanent && !current.AdmissionClosed && current.CoverageComplete && current.LastReopenedFromVersion == expectedVersion)
            {
                return current;
            }
            if (current.Permanent || !current.AdmissionClosed || !current.CoverageComplete || current.Version != expectedVersion
                || (current.Entries ?? []).Any(entry => entry.State != "Deleted" || (entry.WorkflowInstanceId is not null && !entry.TerminalVerified)))
            {
                throw new InvalidOperationException("source-inventory-reopen-rejected");
            }
            return current with { AdmissionClosed = false, LastReopenedFromVersion = expectedVersion };
        }, cancellationToken);

    /// <summary>Retains the canonical predecessor before refresh can retire any historical output.</summary>
    internal async Task RetainCanonicalAsync(string tenantId, string sourceUri, string caseId, string unitId, CancellationToken cancellationToken)
    {
        await ChangeAsync(tenantId, sourceUri, current =>
        {
            if ((current.Entries ?? []).Any(entry => entry.CaseId == caseId && entry.MemoryUnitId == unitId)) { return current; }
            if ((current.Entries?.Count ?? 0) >= Capacity) { throw new InvalidOperationException("source-inventory-capacity"); }
            return current with { Entries = [.. current.Entries ?? [], new(caseId, null, MemoryUnitId: unitId, State: "Completed")] };
        }, cancellationToken).ConfigureAwait(false);
        await RetainAliasAsync(tenantId, sourceUri, unitId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Restores have no per-output workflow terminal contract. Invalidate coverage and retain the output before writing.</summary>
    internal async Task InvalidateForRestoreAsync(string tenantId, string sourceUri, string caseId, string unitId, CancellationToken cancellationToken)
    {
        await ChangeAsync(tenantId, sourceUri, current =>
        {
            if (current.AdmissionClosed) { throw new InvalidOperationException("source-inventory-admission-closed"); }
            bool retained = (current.Entries ?? []).Any(entry => entry.CaseId == caseId && entry.MemoryUnitId == unitId);
            if (!retained && (current.Entries?.Count ?? 0) >= Capacity) { throw new InvalidOperationException("source-inventory-capacity"); }
            return current with
            {
                CoverageComplete = false, CoverageEvidenceId = null, IncompleteReason = "restore-reconciliation-required",
                Entries = retained ? current.Entries : [.. current.Entries ?? [], new(caseId, null, MemoryUnitId: unitId, State: "Completed")],
            };
        }, cancellationToken).ConfigureAwait(false);
        await RetainAliasAsync(tenantId, sourceUri, unitId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Independently verifies persisted admission that never reached the scheduling boundary.</summary>
    internal async Task<bool> IsNeverScheduledAsync(string tenant, string source, SourceInventoryEntry expected, CancellationToken token)
    {
        RedisValue raw = await redis.GetDatabase().StringGetAsync(BuildKey(tenant, source)).WaitAsync(token).ConfigureAwait(false);
        SourceInventorySnapshot snapshot = Parse(raw, tenant, source);
        return raw.HasValue && snapshot.AdmissionClosed && expected.SchedulingState == "Unscheduled"
            && expected.State == "Admitted" && expected.MemoryUnitId is null && expected.CandidateMemoryUnitId is null
            && expected.WorkflowInstanceId is not null
            && (snapshot.Entries ?? []).Count(entry => entry == expected) == 1;
    }

    /// <summary>Records canonical output before completion acknowledgement; publication cannot erase superseded provenance.</summary>
    internal async Task CompleteAsync(string tenantId, string sourceUri, string workflowId, string caseId, string unitId, CancellationToken cancellationToken)
    {
        IngestionIdentityGuard.Handle(workflowId);
        IngestionIdentityGuard.Handle(caseId);
        await RetainAliasAsync(tenantId, sourceUri, unitId, cancellationToken).ConfigureAwait(false);
        await ChangeAsync(tenantId, sourceUri, current =>
        {
            SourceInventoryEntry prior = (current.Entries ?? []).SingleOrDefault(entry => entry.WorkflowInstanceId == workflowId)
                ?? throw new InvalidOperationException("source-inventory-admission-missing");
            if (prior.CaseId != caseId || (prior.MemoryUnitId is not null && prior.MemoryUnitId != unitId))
            {
                throw new InvalidOperationException("source-inventory-output-mismatch");
            }
            return current with { Entries = [.. current.Entries!.Select(entry => entry == prior
                ? entry with { MemoryUnitId = unitId, State = (current.Entries ?? []).Any(known => known.MemoryUnitId == unitId && known.State == "Deleted") ? "Deleted" : "Completed" } : entry)] };
        }, cancellationToken).ConfigureAwait(false);
        await RetainAliasAsync(tenantId, sourceUri, unitId, cancellationToken).ConfigureAwait(false);
    }

    private async Task RetainAliasAsync(string tenantId, string sourceUri, string unitId, CancellationToken cancellationToken)
    {
        IngestionIdentityGuard.Scope(tenantId, sourceUri);
        IngestionIdentityGuard.Handle(unitId);
        const string retainAlias = "if redis.call('SISMEMBER',KEYS[1],ARGV[1]) == 1 then return 1 end if redis.call('SCARD',KEYS[1]) >= 1024 then return 0 end redis.call('SADD',KEYS[1],ARGV[1]); return 1";
        RedisResult retained = await TenantIngestionOwnership.WriteAsync(redis.GetDatabase(), tenantId, retainAlias, [(RedisKey)UnitSourceKey(tenantId, unitId)],
            [(RedisValue)sourceUri]).WaitAsync(cancellationToken).ConfigureAwait(false);
        if ((long)retained != 1) { throw new InvalidOperationException("source-inventory-alias-capacity"); }
    }

    /// <summary>Retains physical reconciliation of a discarded output without deleting its canonical winner.</summary>
    internal Task RecordDiscardedDeletionAsync(string tenant, string source, SourceInventoryEntry expected, CancellationToken token)
        => ChangeAsync(tenant, source, current =>
        {
            SourceInventoryEntry entry = (current.Entries ?? []).Single(item => item.WorkflowInstanceId == expected.WorkflowInstanceId);
            if (entry.CaseId != expected.CaseId || entry.MemoryUnitId != expected.MemoryUnitId
                || entry.CandidateMemoryUnitId != expected.CandidateMemoryUnitId || entry.CandidateMemoryUnitId is null
                || entry.CandidateMemoryUnitId == entry.MemoryUnitId)
            { throw new InvalidOperationException("source-inventory-discarded-identity-mismatch"); }
            return entry.CandidateArtifactsDeleted ? current : current with
            { Entries = [.. current.Entries!.Select(item => item == entry ? item with { CandidateArtifactsDeleted = true } : item)] };
        }, token);

    /// <summary>Persists exact terminal workflow evidence observed independently from durable workflow state.</summary>
    internal Task RecordTerminalEvidenceAsync(string tenantId, string sourceUri, SourceInventoryEntry expected, CancellationToken cancellationToken)
        => ChangeAsync(tenantId, sourceUri, current =>
        {
            SourceInventoryEntry? entry = (current.Entries ?? []).SingleOrDefault(item => item.WorkflowInstanceId == expected.WorkflowInstanceId);
            if (entry is null || entry.CaseId != expected.CaseId || entry.MemoryUnitId != expected.MemoryUnitId
                || (entry.CandidateMemoryUnitId is { } candidate && candidate != entry.MemoryUnitId && !entry.CandidateArtifactsDeleted))
            {
                throw new InvalidOperationException("source-inventory-terminal-evidence-mismatch");
            }
            return entry.TerminalVerified ? current : current with
            {
                Entries = [.. current.Entries!.Select(item => item == entry ? item with { TerminalVerified = true } : item)],
            };
        }, cancellationToken);

    /// <summary>Records physical projection deletion after every derived-store deletion succeeded.</summary>
    internal async Task MarkDeletedAsync(string tenantId, string unitId, CancellationToken cancellationToken)
    {
        IngestionIdentityGuard.Handle(unitId);
        await TenantIngestionOwnership.WriteAsync(redis.GetDatabase(), tenantId, "return redis.call('SET',KEYS[1],'1')",
            [(RedisKey)DeletionReceiptKey(tenantId, unitId)], [], cancellationToken).ConfigureAwait(false);
        RedisValue[] sources = await redis.GetDatabase().SetMembersAsync(UnitSourceKey(tenantId, unitId)).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (sources.Length > Capacity) { throw new InvalidOperationException("source-inventory-alias-capacity"); }
        foreach (RedisValue source in sources)
        {
            await ChangeAsync(tenantId, source.ToString(), current => current with
            {
                Entries = [.. (current.Entries ?? []).Select(entry => entry.MemoryUnitId == unitId ? entry with { State = "Deleted" } : entry)],
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string DeletionReceiptKey(string tenant, string unit) => "source-inventory-deleted:" + Hash(JsonSerializer.Serialize(new[] { tenant, unit }));

    private static string UnitSourceKey(string tenantId, string unitId) => "source-inventory-output:" + Hash(JsonSerializer.Serialize(new[] { tenantId, unitId }));

    /// <summary>Reads a scope/version-bound bounded page. Mutations invalidate continuations, requiring a fresh delivery.</summary>
    internal async Task<SourceInventoryPage> ReadAsync(string tenantId, string sourceUri, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        if (pageSize is < 1 or > 100) { throw new ArgumentOutOfRangeException(nameof(pageSize)); }
        SourceInventorySnapshot current = await ReadSnapshotAsync(tenantId, sourceUri, cancellationToken).ConfigureAwait(false);
        string[] units = (current.Entries ?? []).Where(entry => entry.MemoryUnitId is not null && entry.State != "Deleted")
            .Select(entry => entry.MemoryUnitId!).Distinct(StringComparer.Ordinal).ToArray();
        RedisValue[] receipts = units.Length == 0 ? [] : await redis.GetDatabase()
            .StringGetAsync(units.Select(unit => (RedisKey)DeletionReceiptKey(tenantId, unit)).ToArray())
            .WaitAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> deleted = units.Where((_, index) => receipts[index].HasValue).ToHashSet(StringComparer.Ordinal);
        if (deleted.Count > 0)
        {
            // One exact multi-get and one CAS repair, independent of the source's page count.
            await ChangeAsync(tenantId, sourceUri, snapshot => snapshot with
            { Entries = [.. (snapshot.Entries ?? []).Select(entry => entry.MemoryUnitId is { } unit && deleted.Contains(unit)
                ? entry with { State = "Deleted" } : entry)] }, cancellationToken).ConfigureAwait(false);
            current = await ReadSnapshotAsync(tenantId, sourceUri, cancellationToken).ConfigureAwait(false);
        }
        string scope = QueryCursorScope.Create().Add("tenant", tenantId).Add("source", sourceUri).Add("version", current.Version).Build();
        int offset = 0;
        if (!cursorCodec.TryDecode(cursor, "memories-source-inventory-v1", scope, out string? position, out _)
            || (position is not null && (!int.TryParse(position, NumberStyles.None, CultureInfo.InvariantCulture, out offset)
                || offset < 1 || offset > (current.Entries?.Count ?? 0))))
        {
            throw new InvalidOperationException("source-inventory-cursor-invalidated");
        }
        SourceInventoryEntry[] entries = (current.Entries ?? []).Skip(offset).Take(pageSize)
            .Select(entry => entry.CandidateMemoryUnitId is { } candidate && candidate != entry.MemoryUnitId && !entry.CandidateArtifactsDeleted
                ? entry with { TerminalVerified = false } : entry).ToArray();
        // Account for the actual Web JSON escaping, UTF-8 bytes and protected continuation.
        // Removing only trailing entries preserves exact snapshot offsets and accepted identities.
        while (true)
        {
            int next = offset + entries.Length;
            SourceInventoryPage page = new(tenantId, sourceUri, current.Version, current.CoverageComplete, current.AdmissionClosed, current.Permanent,
                entries, next < (current.Entries?.Count ?? 0) ? cursorCodec.Encode("memories-source-inventory-v1", scope, next.ToString(CultureInfo.InvariantCulture)) : null,
                current.CoverageComplete ? null : current.IncompleteReason ?? "legacy-coverage-not-reconciled");
            if (JsonSerializer.SerializeToUtf8Bytes(page, Json).Length <= MaximumPageBytes) { return page; }
            if (entries.Length <= 1) { throw new InvalidOperationException("source-inventory-page-byte-capacity"); }
            entries = entries[..^1];
        }
    }

    /// <summary>Explicit owner reconciliation seam. Only a fenced, fully enumerated historical source can gain a coverage marker.</summary>
    /// <remarks>Only the explicitly enabled owned synthetic-test protocol invokes this seam from HTTP; ordinary installation and cleanup never certify legacy data.</remarks>
    internal async Task ReconcileAsync(string tenantId, string sourceUri, string expectedVersion,
        IReadOnlyList<SourceInventoryEntry> historicalEntries, string coverageEvidenceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coverageEvidenceId);
        foreach (SourceInventoryEntry entry in historicalEntries) { IngestionIdentityGuard.Entry(entry); }
        // Retain aliases before the coverage marker: a partial failure cannot certify a source
        // whose canonical outputs cannot subsequently acknowledge physical deletion.
        foreach (SourceInventoryEntry entry in historicalEntries)
        {
            if (!string.IsNullOrWhiteSpace(entry.MemoryUnitId))
            {
                await RetainAliasAsync(tenantId, sourceUri, entry.MemoryUnitId, cancellationToken).ConfigureAwait(false);
            }
        }
        await ChangeAsync(tenantId, sourceUri, current =>
        {
            if (!current.AdmissionClosed || current.Version != expectedVersion) { throw new InvalidOperationException("source-inventory-reconciliation-conflict"); }
            SourceInventoryEntry[] merged = [.. (current.Entries ?? []).Concat(historicalEntries).Distinct()];
            if (merged.Length > Capacity || merged.GroupBy(EntryIdentity).Any(group => group.Count() > 1))
            {
                throw new InvalidOperationException("source-inventory-reconciliation-invalid");
            }
            return current with { CoverageComplete = true, CoverageEvidenceId = coverageEvidenceId, IncompleteReason = null, Entries = merged };
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Seals only an independently enumerated exact unit under the configured operator writer exclusion; source-wide coverage is insufficient.</summary>
    internal async Task ReconcileUnitAsync(string tenant, string unit, IReadOnlyList<string> exactKeys,
        string evidenceId, string? expectedAuthority, CancellationToken token)
    {
        IngestionIdentityGuard.Handle(tenant, 128);
        IngestionIdentityGuard.Handle(unit);
        IngestionIdentityGuard.Handle(evidenceId);
        UnitOwnershipAuthority.ValidateConfiguration(_ownershipSettings);
        if (_ownershipSettings.OwnershipGeneration is not { } generation)
            throw new InvalidOperationException("unit-ownership-generation-and-writer-exclusion-required");
        ArgumentNullException.ThrowIfNull(exactKeys);
        if (exactKeys.Count > SourcePublicationVisibility.KeyCapacity)
            throw new InvalidOperationException("source-publication-key-capacity");
        string[] keys = exactKeys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (keys.Length != exactKeys.Count || keys.Length > SourcePublicationVisibility.KeyCapacity)
            throw new InvalidOperationException("source-publication-key-capacity");
        foreach (string key in keys) { SemanticKeyDiscovery.ValidateKey(tenant, unit, key); }
        string authority = UnitOwnershipAuthority.Serialize(new UnitOwnershipRecord("historical-v2", tenant, unit, generation,
            Hash(JsonSerializer.Serialize(keys)), evidenceId, _ownershipSettings.OwnershipWriterExclusionEvidenceId!));
        const string reconcile = """
            local current=redis.call('GET',KEYS[1]) or ''
            if current~=ARGV[1] and current~=ARGV[2] then return redis.error_reply('unit-ownership-reconciliation-conflict') end
            local generation=redis.call('GET',KEYS[4])
            if generation and generation~=ARGV[5] then return redis.error_reply('unit-ownership-generation-conflict') end
            local exact={}
            for i=6,#ARGV do
                local key=ARGV[i]
                exact[key]=true
                local kind=redis.call('TYPE',key).ok
                local ownerField='memoryUnitId'
                if key==KEYS[5] then ownerField='id' end
                if kind~='none' and (kind~='hash' or redis.call('HGET',key,'tenantId')~=ARGV[3]
                    or redis.call('HGET',key,ownerField)~=ARGV[4]) then
                    return redis.error_reply('unit-ownership-reconciliation-scope')
                end
            end
            if redis.call('SCARD',KEYS[3])>4096 then return redis.error_reply('source-publication-key-capacity') end
            for _,key in ipairs(redis.call('SMEMBERS',KEYS[3])) do
                if not exact[key] then return redis.error_reply('unit-ownership-reconciliation-incomplete') end
            end
            for i=6,#ARGV do redis.call('SADD',KEYS[3],ARGV[i]) end
            redis.call('SET',KEYS[1],ARGV[2])
            redis.call('SET',KEYS[2],ARGV[2])
            redis.call('SET',KEYS[4],ARGV[5])
            return 1
            """;
        await TenantIngestionOwnership.WriteAsync(redis.GetDatabase(), tenant, reconcile,
            [UnitOwnershipAuthority.Key(tenant, unit), SourcePublicationVisibility.KeyProvenance(tenant, unit),
                SourcePublicationVisibility.Keys(tenant, unit), UnitOwnershipAuthority.GenerationKey(generation),
                Infrastructure.IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit)],
            [expectedAuthority ?? string.Empty, authority, tenant, unit, _ownershipSettings.OwnershipWriterExclusionEvidenceId!,
                .. keys.Select(key => (RedisValue)key)], token).ConfigureAwait(false);
    }

    private async Task<SourceInventorySnapshot> ReadSnapshotAsync(string tenantId, string sourceUri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue value = await redis.GetDatabase().StringGetAsync(BuildKey(tenantId, sourceUri)).WaitAsync(cancellationToken).ConfigureAwait(false);
        return Parse(value, tenantId, sourceUri);
    }

    private async Task ChangeAsync(string tenantId, string sourceUri, Func<SourceInventorySnapshot, SourceInventorySnapshot> change, CancellationToken cancellationToken,
        string? liveMemoryUnitId = null, (string Workflow, string Candidate)? freshCandidate = null, string? freshAdmission = null)
    {
        string key = BuildKey(tenantId, sourceUri);
        IDatabase db = redis.GetDatabase();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisValue raw = await db.StringGetAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);
            SourceInventorySnapshot current = Parse(raw, tenantId, sourceUri);
            SourceInventorySnapshot updated = change(current);
            if (updated == current) { return; }
            string? initializeCandidate = freshCandidate is { } fresh && (current.FreshCandidateWorkflows ?? []).Contains(fresh.Workflow, StringComparer.Ordinal)
                && (current.Entries ?? []).Any(entry =>
                entry.WorkflowInstanceId == fresh.Workflow && entry.CandidateMemoryUnitId is null && entry.FailedRetryMemoryUnitId is null)
                ? fresh.Candidate : null;
            SourceInventoryEntry[] entries = [.. updated.Entries ?? []];
            foreach (SourceInventoryEntry entry in entries) { IngestionIdentityGuard.Entry(entry); }
            if (entries.Length > Capacity) { throw new InvalidOperationException("source-inventory-capacity"); }
            if (entries.GroupBy(EntryIdentity).Any(group => group.Count() > 1))
            { throw new InvalidOperationException("source-inventory-duplicate-identity"); }
            string[] units = entries.Where(entry => entry.MemoryUnitId is not null && entry.State != "Deleted")
                .Select(entry => entry.MemoryUnitId!).Distinct(StringComparer.Ordinal).ToArray();
            RedisValue[] receipts = units.Length == 0 ? [] : await db.StringGetAsync(
                units.Select(unit => (RedisKey)DeletionReceiptKey(tenantId, unit)).ToArray()).WaitAsync(cancellationToken).ConfigureAwait(false);
            HashSet<string> deleted = units.Where((_, index) => receipts[index].HasValue).ToHashSet(StringComparer.Ordinal);
            entries = [.. entries.Select(entry => entry.MemoryUnitId is { } unit && deleted.Contains(unit)
                ? entry with { State = "Deleted" } : entry)];
            updated = updated with { Entries = entries };
            string serialized = JsonSerializer.Serialize(updated with { Version = Guid.NewGuid().ToString("N") }, Json);
            if (serialized.Length > MaximumDocumentCharacters) { throw new InvalidOperationException("source-inventory-document-capacity"); }
            const string insert = "if redis.call('EXISTS',KEYS[1]) == 1 then return 0 end redis.call('SET',KEYS[1],ARGV[2]); return 1";
            RedisKey[] keys = liveMemoryUnitId is null ? [(RedisKey)key]
                : [(RedisKey)key, (RedisKey)SourcePublicationVisibility.Tombstone(tenantId, liveMemoryUnitId)];
            string script = raw.HasValue ? CompareExchange : insert;
            string? issuedUnit = initializeCandidate ?? (freshAdmission is not null
                && !(current.Entries ?? []).Any(entry => entry.WorkflowInstanceId == freshAdmission) ? freshAdmission : null);
            if (issuedUnit is not null)
            {
                // Only the configured issuer's reserved namespace can establish virgin ownership.
                // Absence of workflow/base keys and old bare modern markers grant no authority.
                UnitOwnershipAuthority.ValidateConfiguration(_ownershipSettings);
                bool issued = UnitOwnershipAuthority.IsIssuedNamespace(issuedUnit, _ownershipSettings.OwnershipGeneration);
                SourceInventoryEntry producer = entries.Single(entry => entry.WorkflowInstanceId == (freshCandidate?.Workflow ?? freshAdmission));
                string authority = issued ? UnitOwnershipAuthority.Serialize(new UnitOwnershipRecord("issued-v2", tenantId, issuedUnit,
                    _ownershipSettings.OwnershipGeneration!, UnitOwnershipAuthority.Producer(producer.WorkflowInstanceId!, sourceUri, producer.CaseId),
                    "service-issuance:" + _ownershipSettings.OwnershipGeneration, _ownershipSettings.OwnershipWriterExclusionEvidenceId!)) : string.Empty;
                keys = [(RedisKey)key, (RedisKey)SourcePublicationVisibility.KeyProvenance(tenantId, issuedUnit),
                    (RedisKey)SourcePublicationVisibility.Keys(tenantId, issuedUnit),
                    Infrastructure.IndexSchemaDefinitions.BuildSyntacticKey(tenantId, issuedUnit),
                    Infrastructure.IndexSchemaDefinitions.BuildSemanticKey(tenantId, issuedUnit),
                    Infrastructure.IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, issuedUnit),
                    (RedisKey)SourcePublicationVisibility.Tombstone(tenantId, issuedUnit),
                    Infrastructure.IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenantId, issuedUnit),
                    UnitOwnershipAuthority.Key(tenantId, issuedUnit),
                    issued ? UnitOwnershipAuthority.GenerationKey(_ownershipSettings.OwnershipGeneration!) : "__unused-ownership-generation"];
                RedisValue[] authorityArguments = [(RedisValue)(raw.HasValue ? raw.ToString() : string.Empty), (RedisValue)serialized,
                    authority, _ownershipSettings.OwnershipWriterExclusionEvidenceId ?? string.Empty, initializeCandidate is null ? "0" : "1",
                    UnitOwnershipAuthority.Producer(producer.WorkflowInstanceId!, sourceUri, producer.CaseId)];

                script = """
                    local current=redis.call('GET',KEYS[1]) or ''
                    if current~=ARGV[1] then return 0 end
                    local retained=redis.call('GET',KEYS[9])
                    if retained then
                        local ok,owner=pcall(cjson.decode,retained)
                        if not ok or (owner.Kind~='issued-v2' and owner.Kind~='historical-v2')
                            or (owner.Kind=='issued-v2' and owner.ProducerBinding~=ARGV[6]) then
                            return redis.error_reply('unit-ownership-producer-conflict')
                        end
                    end
                    if ARGV[3]~='' then
                        local generation=redis.call('GET',KEYS[10])
                        if generation and generation~=ARGV[4] then return redis.error_reply('unit-ownership-generation-conflict') end
                        local owner=redis.call('GET',KEYS[9])
                        if owner and owner~=ARGV[3] then return redis.error_reply('unit-ownership-producer-conflict') end
                        if not owner then
                            for i=2,8 do
                                if redis.call('EXISTS',KEYS[i])==1 then return redis.error_reply('source-publication-existing-candidate') end
                            end
                        elseif redis.call('EXISTS',KEYS[7])==1 then return redis.error_reply('source-publication-existing-candidate') end
                        redis.call('SET',KEYS[9],ARGV[3])
                        redis.call('SET',KEYS[10],ARGV[4])
                        if ARGV[5]=='1' then redis.call('SET',KEYS[2],ARGV[3]) end
                    elseif ARGV[5]=='1' and redis.call('EXISTS',KEYS[2])==0 then
                        for i=3,8 do
                            if redis.call('EXISTS',KEYS[i])==1 then return redis.error_reply('source-publication-existing-candidate') end
                        end
                    end
                    redis.call('SET',KEYS[1],ARGV[2]); return 1
                    """;
                bool issuedSaved = (long)await TenantIngestionOwnership.WriteAsync(db, tenantId, script, keys, authorityArguments,
                    cancellationToken).ConfigureAwait(false) == 1;
                if (issuedSaved) { return; }
                continue;
            }
            bool saved = (long)await TenantIngestionOwnership.WriteAsync(db, tenantId, script,
                keys, [(RedisValue)(raw.HasValue ? raw.ToString() : string.Empty), (RedisValue)serialized], cancellationToken,
                precondition: liveMemoryUnitId is null ? null : "if redis.call('EXISTS',KEYS[2])==1 then return redis.error_reply('failed-retry-output-erased') end").ConfigureAwait(false) == 1;
            if (saved) { return; }
        }
        throw new InvalidOperationException("source-inventory-concurrency-exhausted");
    }

    private static SourceInventorySnapshot Parse(RedisValue raw, string tenantId, string sourceUri)
    {
        if (raw.HasValue && raw.ToString().Length > MaximumDocumentCharacters) { throw new InvalidOperationException("source-inventory-document-capacity"); }
        SourceInventorySnapshot current = raw.HasValue
            ? JsonSerializer.Deserialize<SourceInventorySnapshot>(raw.ToString(), Json) ?? throw new InvalidOperationException("source-inventory-unreadable")
            : new(tenantId, sourceUri, "absent");
        if (current.TenantId != tenantId || current.SourceUri != sourceUri || string.IsNullOrWhiteSpace(current.Version)
            || (current.Entries?.Count ?? 0) > Capacity
            || (current.FreshCandidateWorkflows?.Count ?? 0) > Capacity
            || (current.FreshCandidateWorkflows ?? []).Distinct(StringComparer.Ordinal).Count() != (current.FreshCandidateWorkflows?.Count ?? 0)
            || (current.Entries ?? []).Any(entry => entry is null || string.IsNullOrWhiteSpace(entry.CaseId)
                || (string.IsNullOrWhiteSpace(entry.WorkflowInstanceId) && string.IsNullOrWhiteSpace(entry.MemoryUnitId)) || entry.State is not ("Admitted" or "Completed" or "Deleted")
                || (entry.State != "Admitted" && string.IsNullOrWhiteSpace(entry.MemoryUnitId)))
            || (current.Entries ?? []).GroupBy(EntryIdentity).Any(group => group.Count() > 1)
            || (current.CoverageComplete && string.IsNullOrWhiteSpace(current.CoverageEvidenceId)))
        {
            throw new InvalidOperationException("source-inventory-unreadable");
        }
        foreach (SourceInventoryEntry entry in current.Entries ?? []) { IngestionIdentityGuard.Entry(entry); }
        return current;
    }

    private static (string Kind, string? Case, string? Id) EntryIdentity(SourceInventoryEntry entry)
        => entry.WorkflowInstanceId is { } workflow ? ("workflow", null, workflow) : ("unit", entry.CaseId, entry.MemoryUnitId);

    internal static string BuildKey(string tenantId, string sourceUri)
    {
        IngestionIdentityGuard.Scope(tenantId, sourceUri);
        return "source-inventory:" + Hash(JsonSerializer.Serialize(new[] { tenantId, sourceUri }));
    }
    internal static string? TokenHash(string? token) => string.IsNullOrWhiteSpace(token) ? null : Hash(token);
    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
