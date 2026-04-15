// <copyright file="IngestionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates the full ingestion pipeline: validate → extract → embed → fan-out index → verify → dedup.
/// Uses DAPR Durable Task Framework for durability and automatic replay on sidecar restart.
/// </summary>
public class IngestionWorkflow : Workflow<IngestionInput, IngestionResult>
{
    private const int _compensationRetryAttempts = 3;
    private const int _mainRetryAttempts = 5;

    /// <inheritdoc/>
    public override async Task<IngestionResult> RunAsync(
        WorkflowContext context,
        IngestionInput input)
    {
        var logger = context.CreateReplaySafeLogger<IngestionWorkflow>();
        string memoryUnitId = string.IsNullOrWhiteSpace(context.InstanceId)
            ? context.NewGuid().ToString()
            : context.InstanceId;
        DateTimeOffset ingestedAt = new(context.CurrentUtcDateTime, TimeSpan.Zero);
        string currentStage = "queued";
        MemoryUnitStatus currentStatus = MemoryUnitStatus.Queued;

        LogCurrentStatus(logger, memoryUnitId, currentStatus);

        WorkflowTaskOptions retryOptions = CreateMainRetry();
        WorkflowTaskOptions compensationRetry = CreateCompensationRetry();
        CleanupInput cleanupInput = new(memoryUnitId, input.TenantId);

        try
        {
            logger.LogInformation(
                "Ingestion started for {SourceUri} in tenant {TenantId}, unit {MemoryUnitId}",
                input.SourceUri,
                input.TenantId,
                memoryUnitId);

            currentStage = "idempotency";
            string dedupKey = DedupKeyBuilder.BuildKey(input.TenantId, input.CaseId, input.SourceUri);
            IdempotencyResult idempotency = await context.CallActivityAsync<IdempotencyResult>(
                nameof(CheckIdempotencyActivity),
                new IdempotencyInput(input.SourceUri, input.TenantId, input.CaseId),
                retryOptions);

            if (idempotency.IsDuplicate)
            {
                logger.LogInformation(
                    "Duplicate detected for {SourceUri} in tenant {TenantId}, existing unit: {ExistingId}",
                    input.SourceUri,
                    input.TenantId,
                    idempotency.ExistingMemoryUnitId);

                currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Indexed);

                return new IngestionResult(
                    idempotency.ExistingMemoryUnitId!,
                    currentStatus,
                    ingestedAt,
                    WasDuplicate: true,
                    ConsistencyNote: null);
            }

            currentStage = "validation";
            await context.CallActivityAsync<ValidateResult>(
                nameof(ValidateContentActivity),
                input);

            currentStage = "extracting";
            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Extracting);

            ExtractionResult extraction = await context.CallActivityAsync<ExtractionResult>(
                nameof(ExtractContentActivity),
                new ExtractionInput(input.SourceUri, input.ContentBytes, input.ContentType, input.SourceType),
                retryOptions);

            logger.LogInformation(
                "Content extracted: {ContentHash}, {Length} chars",
                extraction.ContentHash,
                extraction.ExtractedContent.Length);

            currentStage = "embedding";
            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Embedding);

            EmbeddingResult embedding = await context.CallActivityAsync<EmbeddingResult>(
                nameof(GenerateEmbeddingActivity),
                new EmbeddingInput(input.TenantId, extraction.ExtractedContent),
                retryOptions);

            logger.LogInformation(
                "Embedding generated: {Provider}, {Dims} dimensions",
                embedding.Provider,
                embedding.Dimensions);

            currentStage = "indexing";
            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Indexing);

            IndexInput indexInput = new()
            {
                MemoryUnitId = memoryUnitId,
                TenantId = input.TenantId,
                CaseId = input.CaseId,
                Content = extraction.ExtractedContent,
                ContentHash = extraction.ContentHash,
                SourceUri = input.SourceUri,
                SourceType = input.SourceType,
                IngestedBy = input.IngestedBy,
                IngestedAt = ingestedAt,
                EmbeddingVector = embedding.Vector,
                EmbeddingProvider = embedding.Provider,
                // Story 5.5 FR70: thread the model through from EmbeddingResult so it lands in
                // the Redis hash (see IndexSyntacticActivity) and is readable via GET memory-unit.
                // Historical replayed EmbeddingResult payloads may lack the field — fall back to
                // parsing the compound provider string (provider:model) so the durable field still
                // stores only the model identifier rather than the full compound value.
                EmbeddingModel = GetEmbeddingModelIdentifier(embedding),
                EmbeddingDimensions = embedding.Dimensions,
                Metadata = input.Metadata,
                CausationId = input.CausationId,
                CorrelationId = input.CorrelationId,
            };

            Task<IndexResult> syntacticTask = context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity),
                indexInput,
                retryOptions);
            Task<IndexResult> semanticTask = context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity),
                indexInput,
                retryOptions);
            Task<IndexResult> graphTask = context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity),
                indexInput,
                retryOptions);

            try
            {
                await Task.WhenAll(syntacticTask, semanticTask, graphTask);
            }
            catch (Exception ex)
            {
                HashSet<string> completedBackends = GetCompletedBackends(syntacticTask, semanticTask, graphTask);
                await CompensateAsync(context, completedBackends, cleanupInput, compensationRetry, logger, ex);

                try
                {
                    await context.CallActivityAsync<bool>(
                        nameof(RecordCaseActivityActivity),
                        new CaseActivityInput(
                            input.TenantId,
                            input.CaseId,
                            CaseActivityEventType.IngestionFailed,
                            input.IngestedBy,
                            $"Ingestion failed for {input.SourceUri} at stage {currentStage}",
                            memoryUnitId));
                }
                catch
                {
                    // Activity recording failure must not mask the original ingestion failure
                }

                logger.LogInformation(
                    "Indexing failed for {MemoryUnitId}, compensated backends: [{Backends}]",
                    memoryUnitId,
                    string.Join(", ", completedBackends));

                AttachFailureDetails(ex, memoryUnitId, currentStage, _mainRetryAttempts, logger);
                throw;
            }

            logger.LogInformation(
                "Indexing complete, verifying consistency for {MemoryUnitId}",
                memoryUnitId);

            string? consistencyNote;

            try
            {
                currentStage = "verifying";
                ConsistencyResult consistency = await context.CallActivityAsync<ConsistencyResult>(
                    nameof(VerifyConsistencyActivity),
                    new ConsistencyInput(memoryUnitId, input.TenantId),
                    retryOptions);

                consistencyNote = null;
                if (!consistency.SyntacticExists || !consistency.SemanticExists || !consistency.GraphExists)
                {
                    List<string> missing = [];
                    if (!consistency.SyntacticExists)
                    {
                        missing.Add("syntactic");
                    }

                    if (!consistency.SemanticExists)
                    {
                        missing.Add("semantic");
                    }

                    if (!consistency.GraphExists)
                    {
                        missing.Add("graph");
                    }

                    consistencyNote = $"Missing backends: {string.Join(", ", missing)}";
                    logger.LogWarning(
                        "Consistency discrepancy for {MemoryUnitId}: {Note}",
                        memoryUnitId,
                        consistencyNote);
                }

                currentStage = "dedup";
                await context.CallActivityAsync<bool>(
                    nameof(SaveDedupKeyActivity),
                    new DedupKeyInput(dedupKey, memoryUnitId),
                    retryOptions);
            }
            catch (Exception ex)
            {
                await CompensateAsync(
                    context,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" },
                    cleanupInput,
                    compensationRetry,
                    logger,
                    ex);

                logger.LogInformation(
                    "Post-index workflow step failed for {MemoryUnitId}; rolled back indexed backends.",
                    memoryUnitId);

                AttachFailureDetails(ex, memoryUnitId, currentStage, _mainRetryAttempts, logger);
                throw;
            }

            try
            {
                await context.CallActivityAsync<bool>(
                    nameof(RecordCaseActivityActivity),
                    new CaseActivityInput(
                        input.TenantId,
                        input.CaseId,
                        CaseActivityEventType.MemoryUnitIngested,
                        input.IngestedBy,
                        $"Memory unit {memoryUnitId} indexed from {input.SourceUri}",
                        memoryUnitId));
            }
            catch
            {
                // Activity recording is best-effort — failure must not affect ingestion result
            }

            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Indexed);

            return new IngestionResult(
                memoryUnitId,
                currentStatus,
                ingestedAt,
                WasDuplicate: false,
                ConsistencyNote: consistencyNote);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !HasFailureDetails(ex))
        {
            AttachFailureDetails(ex, memoryUnitId, currentStage, GetRetryCountForStage(currentStage), logger);
            throw;
        }
    }

    /// <summary>
    /// Builds the retry policy for main pipeline activities. Story 5.6 AC5 pins these values
    /// (maxNumberOfAttempts=5, firstRetryInterval=2s, backoffCoefficient=1.5, maxRetryInterval=5min).
    /// Worst-case total wait on retry exhaustion: 2 + 3 + 4.5 + 6.75 + 10.125 ≈ 26.4 s per attempt
    /// window. Do NOT lower the retry count or shorten intervals without amending NFR22.
    /// </summary>
    internal static WorkflowTaskOptions CreateMainRetry() => new(
        new WorkflowRetryPolicy(
            maxNumberOfAttempts: _mainRetryAttempts,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 1.5,
            maxRetryInterval: TimeSpan.FromMinutes(5)));

    internal static WorkflowTaskOptions CreateCompensationRetry() => new(
        new WorkflowRetryPolicy(
            maxNumberOfAttempts: _compensationRetryAttempts,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromSeconds(30)));

    private static HashSet<string> GetCompletedBackends(
        Task<IndexResult> syntacticTask,
        Task<IndexResult> semanticTask,
        Task<IndexResult> graphTask)
    {
        HashSet<string> completedBackends = new(StringComparer.OrdinalIgnoreCase);

        if (syntacticTask.IsCompletedSuccessfully)
        {
            completedBackends.Add(syntacticTask.Result.Backend);
        }

        if (semanticTask.IsCompletedSuccessfully)
        {
            completedBackends.Add(semanticTask.Result.Backend);
        }

        if (graphTask.IsCompletedSuccessfully)
        {
            completedBackends.Add(graphTask.Result.Backend);
        }

        return completedBackends;
    }

    private static async Task CompensateAsync(
        WorkflowContext context,
        IReadOnlySet<string> completedBackends,
        CleanupInput cleanupInput,
        WorkflowTaskOptions compensationRetry,
        ILogger logger,
        Exception? originalException = null)
    {
        List<Task> compensationTasks = [];

        if (completedBackends.Contains("syntactic"))
        {
            compensationTasks.Add(
                context.CallActivityAsync<bool>(
                    nameof(CleanupSyntacticActivity),
                    cleanupInput,
                    compensationRetry));
        }

        if (completedBackends.Contains("semantic"))
        {
            compensationTasks.Add(
                context.CallActivityAsync<bool>(
                    nameof(CleanupSemanticActivity),
                    cleanupInput,
                    compensationRetry));
        }

        if (completedBackends.Contains("graph"))
        {
            compensationTasks.Add(
                context.CallActivityAsync<bool>(
                    nameof(CleanupGraphActivity),
                    cleanupInput,
                    compensationRetry));
        }

        if (compensationTasks.Count == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(compensationTasks);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "One or more compensation activities failed for {MemoryUnitId}.",
                cleanupInput.MemoryUnitId);

            originalException?.Data["CompensationFailure"] = ex.Message;
        }
    }

    private static void AttachFailureDetails(
        Exception exception,
        string memoryUnitId,
        string stage,
        int retryCount,
        ILogger logger)
    {
        FailureDetails details = new(stage, exception.GetType().Name, retryCount);

        exception.Data[nameof(FailureDetails)] = details;
        exception.Data[nameof(MemoryUnitStatus)] = MemoryUnitStatus.Failed;
        exception.Data["MemoryUnitId"] = memoryUnitId;

        logger.LogError(
            exception,
            "Ingestion failed for {MemoryUnitId}. Status={Status}; stage={Stage}; errorCode={ErrorCode}; retryCount={RetryCount}",
            memoryUnitId,
            MemoryUnitStatus.Failed,
            details.Stage,
            details.ErrorCode,
            details.RetryCount);
    }

    private static int GetRetryCountForStage(string stage)
        => string.Equals(stage, "validation", StringComparison.OrdinalIgnoreCase)
            ? 0
            : _mainRetryAttempts;

    private static bool HasFailureDetails(Exception exception)
        => exception.Data.Contains(nameof(FailureDetails));

    private static string GetEmbeddingModelIdentifier(EmbeddingResult embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (!string.IsNullOrWhiteSpace(embedding.Model))
        {
            return embedding.Model;
        }

        string provider = embedding.Provider;
        int separatorIndex = provider.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 && separatorIndex < provider.Length - 1
            ? provider[(separatorIndex + 1)..]
            : provider;
    }

    private static void LogCurrentStatus(ILogger logger, string memoryUnitId, MemoryUnitStatus status)
        => logger.LogInformation("Memory unit {MemoryUnitId} status is {Status}", memoryUnitId, status);

    private static MemoryUnitStatus TransitionStatus(
        ILogger logger,
        string memoryUnitId,
        MemoryUnitStatus currentStatus,
        MemoryUnitStatus nextStatus)
    {
        if (currentStatus != nextStatus)
        {
            logger.LogInformation(
                "Memory unit {MemoryUnitId} status transitioned {PreviousStatus} -> {NextStatus}",
                memoryUnitId,
                currentStatus,
                nextStatus);
        }

        return nextStatus;
    }

}
