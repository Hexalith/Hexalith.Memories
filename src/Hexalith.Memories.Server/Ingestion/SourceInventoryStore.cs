namespace Hexalith.Memories.Server.Ingestion;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Queries;
using StackExchange.Redis;

/// <summary>Durable bounded source inventory. Never derives completeness from a dedup pointer or the maintenance registry.</summary>
internal sealed class SourceInventoryStore([FromKeyedServices("redis")] IConnectionMultiplexer redis, IQueryCursorCodec cursorCodec)
{
    internal const int Capacity = 1024;
    internal const int MaximumPageBytes = 1024 * 1024;
    private const int MaximumDocumentCharacters = 4 * 1024 * 1024;
    private const string CompareExchange = "if redis.call('GET',KEYS[1]) == ARGV[1] then redis.call('SET',KEYS[1],ARGV[2]); return 1 else return 0 end";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Persists admission before scheduling; no failure or lost acknowledgement removes this obligation.</summary>
    internal Task AdmitAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken)
    {
        IngestionIdentityGuard.Input(input, instanceId);
        return ChangeAsync(input.TenantId, input.SourceUri, current =>
        {
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
            if ((current.Entries?.Count ?? 0) >= Capacity) { throw new InvalidOperationException("source-inventory-capacity"); }
            return current with { Entries = [.. current.Entries ?? [], new(input.CaseId, instanceId, TokenHash(input.IdempotencyToken), SchedulingState: "Preparing",
                SourcePayloadReference: IngestionPayloadClaimCheck.GetSourceReference(instanceId, input),
                RefreshProtocolVersion: input.RefreshExistingSource && input.RefreshIdentityDigest is not null ? SourceRefreshProtocol.Version : null,
                SourceRevisionNumber: input.RefreshExistingSource ? input.SourceRevisionNumber : null,
                RefreshIdentityDigest: input.RefreshExistingSource ? input.RefreshIdentityDigest : null)] };
        }, cancellationToken);
    }

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

    /// <summary>Retains producer artifacts before any activity can materialize content.</summary>
    internal Task RecordCandidateAsync(string tenant, string source, string workflow, string candidate, CancellationToken token)
        => ChangeAsync(tenant, source, current =>
        {
            SourceInventoryEntry entry = (current.Entries ?? []).Single(item => item.WorkflowInstanceId == workflow);
            if (entry.CandidateMemoryUnitId is not null && entry.CandidateMemoryUnitId != candidate)
            {
                throw new InvalidOperationException("source-inventory-candidate-mismatch");
            }
            return entry.CandidateMemoryUnitId == candidate ? current : current with
            { Entries = [.. current.Entries!.Select(item => item == entry ? item with { CandidateMemoryUnitId = candidate } : item)] };
        }, token);

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

    private async Task<SourceInventorySnapshot> ReadSnapshotAsync(string tenantId, string sourceUri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue value = await redis.GetDatabase().StringGetAsync(BuildKey(tenantId, sourceUri)).WaitAsync(cancellationToken).ConfigureAwait(false);
        return Parse(value, tenantId, sourceUri);
    }

    private async Task ChangeAsync(string tenantId, string sourceUri, Func<SourceInventorySnapshot, SourceInventorySnapshot> change, CancellationToken cancellationToken)
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
            bool saved = (long)await TenantIngestionOwnership.WriteAsync(db, tenantId, raw.HasValue ? CompareExchange : insert,
                [(RedisKey)key], [(RedisValue)(raw.HasValue ? raw.ToString() : string.Empty), (RedisValue)serialized], cancellationToken).ConfigureAwait(false) == 1;
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
