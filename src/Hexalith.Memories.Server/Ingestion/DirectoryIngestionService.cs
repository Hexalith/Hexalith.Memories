// <copyright file="DirectoryIngestionService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using BaUlid = ByteAether.Ulid.Ulid;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Batch-ingest a directory of files: validate path against the allow-list, enumerate, filter by
/// extension/size, schedule one <see cref="IngestionWorkflow"/> per candidate, persist batch state.
/// </summary>
internal sealed class DirectoryIngestionService
{
    internal const string BatchStateKeyPrefix = "ingestion-batch:";
    internal const string StateStoreName = "statestore";
    internal const int MaxDirectorySchedulingParallelism = 32;
    internal const int MaxDirectoryBatchCheckpointSize = 250;

    private static readonly BaUlid.GenerationOptions UlidOptions = new()
    {
        Monotonicity = BaUlid.GenerationOptions.MonotonicityOptions.MonotonicIncrement,
    };

    private readonly IOptions<IngestionSettings> _settings;
    private readonly IIngestionWorkflowScheduler _workflowScheduler;
    private readonly DaprClient _daprClient;
    private readonly IWorkflowPayloadStore? _payloadStore;
    private readonly ILogger<DirectoryIngestionService> _logger;
    private readonly TimeProvider _timeProvider;

    public DirectoryIngestionService(
        IOptions<IngestionSettings> settings,
        IIngestionWorkflowScheduler workflowScheduler,
        DaprClient daprClient,
        ILogger<DirectoryIngestionService> logger,
        IWorkflowPayloadStore? payloadStore = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(workflowScheduler);
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(logger);

        _settings = settings;
        _workflowScheduler = workflowScheduler;
        _daprClient = daprClient;
        _payloadStore = payloadStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Schedules a directory ingestion batch. Returns a tuple containing an error code (or null) and the outcome (or null).</summary>
    public async Task<DirectoryIngestionResult> IngestAsync(DirectoryIngestionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IngestionSettings settings = _settings.Value;
        string batchId = BaUlid.New(UlidOptions).ToString();
        DateTimeOffset createdAt = _timeProvider.GetUtcNow();
        int ttlSeconds = Math.Max(1, settings.BatchStateTtlHours) * 3600;

        string? validation = ValidateDirectoryPath(request.DirectoryPath, settings.AllowedDirectoryRoots, out string canonical);
        if (validation is not null)
        {
            return DirectoryIngestionResult.Failed(validation);
        }

        int discovered = 0;
        List<string> candidates = [];
        List<SkippedFile> skipped = [];
        bool skippedTruncated = false;
        HashSet<string> supportedExtensions = NormalizeExtensions(settings.SupportedExtensions);
        HashSet<string> unsupportedExtensions = NormalizeExtensions(settings.UnsupportedExtensions);

        EnumerationOptions search = new()
        {
            RecurseSubdirectories = request.Recursive,
            IgnoreInaccessible = true,
            AttributesToSkip = 0,
        };

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(canonical, "*", search);
        }
        catch (UnauthorizedAccessException)
        {
            return DirectoryIngestionResult.Failed("INVALID_DIRECTORY_PATH");
        }
        catch (IOException)
        {
            return DirectoryIngestionResult.Failed("INVALID_DIRECTORY_PATH");
        }

        try
        {
            foreach (string raw in files)
            {
                discovered++;

                string filePath;
                try
                {
                    filePath = ResolvePathThroughReparsePoints(raw);
                }
                catch (Exception)
                {
                    AppendSkipped(batchId, skipped, settings, new SkippedFile(raw, "INVALID_PATH"), ref skippedTruncated);
                    continue;
                }

                if (!IsPathWithinRoot(filePath, canonical))
                {
                    AppendSkipped(batchId, skipped, settings, new SkippedFile(filePath, "OUTSIDE_ROOT"), ref skippedTruncated);
                    continue;
                }

                string ext = NormalizeExtension(Path.GetExtension(filePath));
                if (!supportedExtensions.Contains(ext) || unsupportedExtensions.Contains(ext))
                {
                    AppendSkipped(batchId, skipped, settings, new SkippedFile(filePath, "UNSUPPORTED_EXTENSION"), ref skippedTruncated);
                    continue;
                }

                long size;
                try
                {
                    size = new FileInfo(filePath).Length;
                }
                catch (Exception)
                {
                    AppendSkipped(batchId, skipped, settings, new SkippedFile(filePath, "FILE_UNREADABLE"), ref skippedTruncated);
                    continue;
                }

                if (size <= 0)
                {
                    AppendSkipped(batchId, skipped, settings, new SkippedFile(filePath, "EMPTY_FILE"), ref skippedTruncated);
                    continue;
                }

                if (size > IngestionInputValidator.MaxContentBytes)
                {
                    AppendSkipped(batchId, skipped, settings, new SkippedFile(filePath, "PAYLOAD_TOO_LARGE"), ref skippedTruncated);
                    continue;
                }

                candidates.Add(filePath);
                if (candidates.Count > settings.MaxBatchSize)
                {
                    return DirectoryIngestionResult.Failed("BATCH_TOO_LARGE");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return DirectoryIngestionResult.Failed("INVALID_DIRECTORY_PATH");
        }
        catch (IOException)
        {
            return DirectoryIngestionResult.Failed("INVALID_DIRECTORY_PATH");
        }

        candidates.Sort(StringComparer.Ordinal);

        List<string> instanceIds = new(candidates.Count);
        List<BatchFileRef> scheduledFiles = new(candidates.Count);

        DirectoryBatchState state = CreateBatchState(
            batchId,
            request,
            discovered,
            instanceIds,
            scheduledFiles,
            skipped,
            createdAt);

        if (!await TrySaveBatchStateAsync(state, ttlSeconds, cancellationToken).ConfigureAwait(false))
        {
            return DirectoryIngestionResult.Failed("BATCH_TRACKING_UNAVAILABLE", batchId);
        }

        int parallelism = ClampDirectorySchedulingParallelism(settings.DirectorySchedulingParallelism);
        int checkpointSize = ClampDirectoryBatchCheckpointSize(settings.DirectoryBatchCheckpointSize);
        int scheduledSinceCheckpoint = 0;
        string? failureCode = null;
        using CancellationTokenSource failureCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using SemaphoreSlim stateSaveGate = new(1, 1);
        object progressGate = new();

        async Task<bool> SaveSnapshotAsync(CancellationToken saveCancellationToken)
        {
            await stateSaveGate.WaitAsync(saveCancellationToken).ConfigureAwait(false);
            try
            {
                DirectoryBatchState snapshot;
                lock (progressGate)
                {
                    SortProgress(instanceIds, scheduledFiles);
                    snapshot = CreateBatchState(batchId, request, discovered, instanceIds, scheduledFiles, skipped, createdAt);
                }

                return await TrySaveBatchStateAsync(snapshot, ttlSeconds, saveCancellationToken).ConfigureAwait(false);
            }
            finally
            {
                stateSaveGate.Release();
            }
        }

        async ValueTask ProcessCandidateAsync(string candidatePath, CancellationToken schedulingCancellationToken)
        {
            schedulingCancellationToken.ThrowIfCancellationRequested();

            byte[] bytes;
            try
            {
                bytes = await File.ReadAllBytesAsync(candidatePath, schedulingCancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lock (progressGate)
                {
                    AppendSkipped(batchId, skipped, settings, new SkippedFile(candidatePath, "FILE_UNREADABLE"), ref skippedTruncated);
                }

                if (!await SaveSnapshotAsync(schedulingCancellationToken).ConfigureAwait(false))
                {
                    failureCode = "BATCH_TRACKING_UNAVAILABLE";
                    await failureCancellation.CancelAsync().ConfigureAwait(false);
                }

                return;
            }

            string requestedInstanceId = BaUlid.New(UlidOptions).ToString();
            IngestionInput input = new()
            {
                TenantId = request.TenantId,
                CaseId = request.CaseId,
                SourceUri = candidatePath,
                ContentBytes = bytes,
                ContentType = InferContentType(candidatePath),
                SourceType = SourceType.File,
                IngestedBy = request.IngestedBy,
                Metadata = CloneMetadata(request.Metadata),
                CausationId = request.CausationId,
                CorrelationId = batchId,
            };
            WorkflowPayloadReference? createdPayloadReference = null;
            try
            {
                if (_payloadStore is not null)
                {
                    input = await IngestionPayloadClaimCheck
                        .PrepareAsync(_payloadStore, requestedInstanceId, input, schedulingCancellationToken)
                        .ConfigureAwait(false);
                    createdPayloadReference = input.PayloadReference;
                }

                string instanceId = await _workflowScheduler
                    .ScheduleAsync(requestedInstanceId, input, schedulingCancellationToken)
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(instanceId))
                {
                    instanceId = requestedInstanceId;
                }

                lock (progressGate)
                {
                    instanceIds.Add(instanceId);
                    scheduledFiles.Add(new BatchFileRef(instanceId, candidatePath));
                }

                int checkpointProgress = Interlocked.Increment(ref scheduledSinceCheckpoint);
                if (checkpointProgress % checkpointSize == 0
                    && !await SaveSnapshotAsync(schedulingCancellationToken).ConfigureAwait(false))
                {
                    failureCode = "BATCH_TRACKING_UNAVAILABLE";
                    await failureCancellation.CancelAsync().ConfigureAwait(false);
                }
            }
            catch (Dapr.DaprException ex)
            {
                await DeleteCreatedPayloadAsync(createdPayloadReference, CancellationToken.None).ConfigureAwait(false);
                failureCode = "DAPR_UNAVAILABLE";
                await failureCancellation.CancelAsync().ConfigureAwait(false);
                _logger.LogWarning(
                    ex,
                    "Failed to schedule workflow for batch {BatchId} and file {SourceUri}; returning a non-success result because the batch may be incomplete.",
                    batchId,
                    candidatePath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await DeleteCreatedPayloadAsync(createdPayloadReference, CancellationToken.None).ConfigureAwait(false);
                failureCode = "BATCH_SCHEDULING_FAILED";
                await failureCancellation.CancelAsync().ConfigureAwait(false);
                _logger.LogWarning(
                    ex,
                    "Failed to schedule workflow for batch {BatchId} and file {SourceUri}; returning a non-success result because the batch may be incomplete.",
                    batchId,
                    candidatePath);
            }
            catch (OperationCanceledException)
            {
                await DeleteCreatedPayloadAsync(createdPayloadReference, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        try
        {
            await Parallel.ForEachAsync(
                candidates,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = parallelism,
                    CancellationToken = failureCancellation.Token,
                },
                ProcessCandidateAsync).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && failureCode is not null)
        {
            // A worker recorded the precise non-success result and canceled the remaining bounded work.
        }

        if (failureCode is not null)
        {
            _ = await SaveSnapshotAsync(CancellationToken.None).ConfigureAwait(false);
            return DirectoryIngestionResult.Failed(failureCode, batchId);
        }

        if (!await SaveSnapshotAsync(cancellationToken).ConfigureAwait(false))
        {
            return DirectoryIngestionResult.Failed("BATCH_TRACKING_UNAVAILABLE", batchId);
        }

        lock (progressGate)
        {
            SortProgress(instanceIds, scheduledFiles);
        }

        foreach (SkippedFile item in skipped)
        {
            IngestionEndpointLog.LogDirectoryFileSkipped(_logger, batchId, item.Path, item.Reason);
        }

        DirectoryIngestionOutcome outcome = new(
            batchId,
            discovered,
            instanceIds.Count,
            skipped,
            skippedTruncated,
            instanceIds,
            request.TenantId,
            request.CaseId);
        return DirectoryIngestionResult.Ok(outcome);
    }

    /// <summary>Validates the directory path against the allow-list. Returns an error code or null; sets <paramref name="canonical"/> on success.</summary>
    internal static string? ValidateDirectoryPath(string? path, string[] allowedRoots, out string canonical)
    {
        canonical = string.Empty;

        if (allowedRoots.Length == 0)
        {
            return "DIRECTORY_INGESTION_DISABLED";
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return "INVALID_DIRECTORY_PATH";
        }

        if (!Path.IsPathFullyQualified(path))
        {
            return "INVALID_DIRECTORY_PATH";
        }

        try
        {
            canonical = NormalizePathForComparison(ResolvePathThroughReparsePoints(path));
        }
        catch (Exception)
        {
            return "INVALID_DIRECTORY_PATH";
        }

        if (!Directory.Exists(canonical))
        {
            return "INVALID_DIRECTORY_PATH";
        }

        foreach (string root in allowedRoots)
        {
            string canonicalRoot;
            try
            {
                canonicalRoot = NormalizePathForComparison(ResolvePathThroughReparsePoints(root));
            }
            catch (Exception)
            {
                continue;
            }

            if (IsPathWithinRoot(canonical, canonicalRoot))
            {
                return null;
            }
        }

        return "INVALID_DIRECTORY_PATH";
    }

    internal static string InferContentType(string path)
        => IngestionContentTypeSupport.InferFromPath(path);

    internal static string ResolvePathThroughReparsePoints(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        string? root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            return NormalizePathForComparison(fullPath);
        }

        string current = root;
        string[] segments = fullPath[root.Length..]
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        foreach (string segment in segments)
        {
            current = string.IsNullOrEmpty(current)
                ? segment
                : Path.Combine(current, segment);

            if (!File.Exists(current) && !Directory.Exists(current))
            {
                continue;
            }

            FileAttributes attributes = File.GetAttributes(current);
            if ((attributes & FileAttributes.ReparsePoint) == 0)
            {
                continue;
            }

            FileSystemInfo info = (attributes & FileAttributes.Directory) != 0
                ? new DirectoryInfo(current)
                : new FileInfo(current);
            FileSystemInfo? target = info.ResolveLinkTarget(true);
            if (target is not null)
            {
                current = Path.GetFullPath(target.FullName);
            }
        }

        return NormalizePathForComparison(current);
    }

    internal static bool IsPathWithinRoot(string path, string root)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        string normalizedPath = NormalizePathForComparison(path);
        string normalizedRoot = NormalizePathForComparison(root);

        return normalizedPath.Equals(normalizedRoot, comparison)
            || normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison)
            || normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, comparison);
    }

    private static string NormalizePathForComparison(string path)
        => Path.TrimEndingDirectorySeparator(path);

    internal static DirectoryBatchState CreateBatchState(
        string batchId,
        DirectoryIngestionRequest request,
        int discovered,
        IReadOnlyCollection<string> instanceIds,
        IReadOnlyCollection<BatchFileRef> scheduledFiles,
        IReadOnlyCollection<SkippedFile> skipped,
        DateTimeOffset createdAt)
        => new(
            batchId,
            request.TenantId,
            request.CaseId,
            discovered,
            [.. instanceIds],
            [.. scheduledFiles],
            [.. skipped],
            createdAt);

    private void AppendSkipped(string batchId, List<SkippedFile> skipped, IngestionSettings settings, SkippedFile file, ref bool truncated)
    {
        if (skipped.Count >= settings.MaxSkippedReportSize)
        {
            truncated = true;
            IngestionEndpointLog.LogDirectoryFileSkipped(_logger, batchId, file.Path, file.Reason);
            return;
        }

        skipped.Add(file);
    }

    private async Task<bool> TrySaveBatchStateAsync(DirectoryBatchState state, int ttlSeconds, CancellationToken cancellationToken)
    {
        try
        {
            await _daprClient.SaveStateAsync(
                StateStoreName,
                BatchStateKeyPrefix + state.BatchId,
                state,
                metadata: new Dictionary<string, string>
                {
                    ["ttlInSeconds"] = ttlSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist batch state for batch {BatchId}; returning a non-success result because batch tracking would be incomplete.",
                state.BatchId);
            return false;
        }
    }

    internal static int ClampDirectorySchedulingParallelism(int configured)
        => Math.Clamp(configured, 1, MaxDirectorySchedulingParallelism);

    internal static int ClampDirectoryBatchCheckpointSize(int configured)
        => Math.Clamp(configured, 1, MaxDirectoryBatchCheckpointSize);

    internal static HashSet<string> NormalizeExtensions(IEnumerable<string>? extensions)
    {
        HashSet<string> normalized = new(StringComparer.OrdinalIgnoreCase);
        if (extensions is null)
        {
            return normalized;
        }

        foreach (string extension in extensions)
        {
            string value = NormalizeExtension(extension);
            if (!string.IsNullOrWhiteSpace(value))
            {
                normalized.Add(value);
            }
        }

        return normalized;
    }

    internal static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        string value = extension.Trim().ToLowerInvariant();
        return value.StartsWith(".", StringComparison.Ordinal) ? value : "." + value;
    }

    private async Task DeleteCreatedPayloadAsync(WorkflowPayloadReference? reference, CancellationToken cancellationToken)
    {
        if (reference is null || _payloadStore is null)
        {
            return;
        }

        try
        {
            await _payloadStore.DeleteAsync(reference, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete claim-check payload {PayloadId} after directory workflow scheduling failed.",
                reference.Id);
        }
    }

    private static void SortProgress(List<string> instanceIds, List<BatchFileRef> scheduledFiles)
    {
        List<BatchFileRef> sorted = [.. scheduledFiles.OrderBy(static file => file.SourceUri, StringComparer.Ordinal)];
        scheduledFiles.Clear();
        scheduledFiles.AddRange(sorted);

        instanceIds.Clear();
        instanceIds.AddRange(sorted.Select(static file => file.InstanceId));
    }

    private static Dictionary<string, MetadataField> CloneMetadata(IReadOnlyDictionary<string, MetadataField> source)
    {
        Dictionary<string, MetadataField> clone = new(source.Count);
        foreach ((string key, MetadataField value) in source)
        {
            clone[key] = value;
        }

        return clone;
    }
}

/// <summary>Internal persisted record in the DAPR state store for a batch.</summary>
public sealed record DirectoryBatchState(
    string BatchId,
    string TenantId,
    string CaseId,
    int Discovered,
    string[] InstanceIds,
    BatchFileRef[] Files,
    SkippedFile[] Skipped,
    DateTimeOffset CreatedAt);

/// <summary>Correlation between a scheduled workflow instance and the source file that produced it.</summary>
public sealed record BatchFileRef(string InstanceId, string SourceUri);

/// <summary>Transport record for <see cref="DirectoryIngestionService.IngestAsync"/>.</summary>
public sealed record DirectoryIngestionResult(string? ErrorCode, DirectoryIngestionOutcome? Outcome, string? BatchId = null)
{
    public static DirectoryIngestionResult Ok(DirectoryIngestionOutcome outcome) => new(null, outcome, outcome.BatchId);

    public static DirectoryIngestionResult Failed(string code, string? batchId = null) => new(code, null, batchId);
}
