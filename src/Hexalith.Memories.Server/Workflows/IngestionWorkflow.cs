// <copyright file="IngestionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates the full ingestion pipeline: validate → extract → embed → fan-out index → verify → dedup.
/// Uses DAPR Durable Task Framework for durability and automatic replay on sidecar restart.
/// </summary>
public class IngestionWorkflow : Workflow<IngestionInput, IngestionResult>
{
    private const int _compensationRetryAttempts = 3;
    private const int _embeddingProviderRateLimitMaxDurableRetries = 5;
    private const int _mainRetryAttempts = 5;
    private const string DefaultRetryKey = "__default";
    private const string DedupWorkflowInstancePrefix = "dedup:";

    /// <inheritdoc/>
    public override async Task<IngestionResult> RunAsync(
        WorkflowContext context,
        IngestionInput input)
    {
        var logger = context.CreateReplaySafeLogger<IngestionWorkflow>();
        string memoryUnitId = ResolveMemoryUnitId(context, input);
        WorkflowTraceContext? traceContext = input.TraceContext;
        DateTimeOffset ingestedAt = new(context.CurrentUtcDateTime, TimeSpan.Zero);
        string currentStage = "queued";
        MemoryUnitStatus currentStatus = MemoryUnitStatus.Queued;
        List<WorkflowPayloadReference> transientPayloads = [];

        LogCurrentStatus(logger, memoryUnitId, currentStatus);

        IReadOnlyDictionary<string, WorkflowTaskOptions> retry = CreateActivityRetryOptions(input.WorkflowConfiguration?.Retry);
        WorkflowTaskOptions For(string activityName) =>
            retry.TryGetValue(activityName, out WorkflowTaskOptions? opts) ? opts : retry[DefaultRetryKey];
        WorkflowTaskOptions compensationRetry = CreateCompensationRetry();
        CleanupInput cleanupInput = new(memoryUnitId, input.TenantId, traceContext);

        // Story 6.3 FR10: monotonic transition id used by CaseIngestionCounterActor for replay-idempotency.
        // The sequence is deterministic across replays because the workflow re-executes from the top.
        int counterSeq = 0;
        Task<bool> UpdateCounter(string previous, string next) =>
            context.CallActivityAsync<bool>(
                nameof(UpdateCaseIngestionCounterActivity),
                new CounterTransitionInput(
                    input.TenantId,
                    input.CaseId,
                    previous,
                    next,
                    $"{context.InstanceId}:{System.Threading.Interlocked.Increment(ref counterSeq)}",
                    traceContext),
                compensationRetry);

        try
        {
            logger.LogInformation(
                "Ingestion started for {SourceUri} in tenant {TenantId}, unit {MemoryUnitId}",
                input.SourceUri,
                input.TenantId,
                memoryUnitId);

            await UpdateCounter("none", "queued");
            context.SetCustomStatus("queued");

            WorkflowPayloadReference? sourcePayloadReference = input.PayloadReference;
            AddPayloadReference(transientPayloads, sourcePayloadReference);

            currentStage = "idempotency";
            string dedupKey = DedupKeyBuilder.BuildKey(input.TenantId, input.CaseId, input.SourceUri);
            IdempotencyResult idempotency = await context.CallActivityAsync<IdempotencyResult>(
                nameof(CheckIdempotencyActivity),
                new IdempotencyInput(input.SourceUri, input.TenantId, input.CaseId, input.IdempotencyToken, traceContext),
                For(nameof(CheckIdempotencyActivity)));

            if (idempotency.IsDuplicate)
            {
                logger.LogInformation(
                    "Duplicate detected for {SourceUri} in tenant {TenantId}, existing unit: {ExistingId}",
                    input.SourceUri,
                    input.TenantId,
                    idempotency.ExistingMemoryUnitId);

                currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Indexed);

                await UpdateCounter("queued", "none");
                context.SetCustomStatus("duplicate");
                await CleanupTransientPayloadsAsync(context, input.TenantId, memoryUnitId, transientPayloads, compensationRetry, traceContext: traceContext);

                // Story 9.2 Task 5.6: dedup returns NotApplicable — the existing memory unit was
                // already indexed by an earlier ingest and its NL status is owned by that prior run.
                return new IngestionResult(
                    idempotency.ExistingMemoryUnitId!,
                    currentStatus,
                    ingestedAt,
                    WasDuplicate: true,
                    ConsistencyNote: null)
                {
                    NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.NotApplicable,
                };
            }

            currentStage = "validation";
            await context.CallActivityAsync<ValidateResult>(
                nameof(ValidateContentActivity),
                input);

            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Extracting);
            await UpdateCounter("queued", "extracting");
            context.SetCustomStatus("extracting");

            byte[] contentBytes = input.ContentBytes ?? [];
            string contentType = input.ContentType;
            string effectiveUrl = input.SourceUri;
            long fetchedContentLength = contentBytes.LongLength;
            UrlFetchResult? urlFetch = null;

            // Story 6.1: when SourceType=Url, fetch the body via FetchUrlActivity before extraction.
            // The outer retry policy applies per activity (5 attempts, exponential backoff).
            if (input.SourceType == SourceType.Url)
            {
                currentStage = "fetching";
                urlFetch = await context.CallActivityAsync<UrlFetchResult>(
                    nameof(FetchUrlActivity),
                    new FetchUrlInput(input.SourceUri, memoryUnitId, input.TenantId, traceContext),
                    For(nameof(FetchUrlActivity)));
                contentBytes = urlFetch.PayloadReference is null ? urlFetch.ContentBytes : [];
                sourcePayloadReference = urlFetch.PayloadReference;
                AddPayloadReference(transientPayloads, sourcePayloadReference);
                if (!string.IsNullOrWhiteSpace(urlFetch.ContentType))
                {
                    contentType = urlFetch.ContentType;
                }

                effectiveUrl = urlFetch.FinalUrl;
                fetchedContentLength = urlFetch.ContentLength;
            }

            currentStage = "extracting";

            ExtractionResult extraction = await context.CallActivityAsync<ExtractionResult>(
                nameof(ExtractContentActivity),
                new ExtractionInput(
                    input.SourceUri,
                    contentBytes,
                    contentType,
                    input.SourceType,
                    input.TenantId,
                    memoryUnitId,
                    sourcePayloadReference,
                    traceContext),
                For(nameof(ExtractContentActivity)));
            AddPayloadReference(transientPayloads, extraction.ExtractedContentReference);

            logger.LogInformation(
                "Content extracted: {ContentHash}, {Length} chars",
                extraction.ContentHash,
                GetExtractedLength(extraction));

            currentStage = "embedding";
            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Embedding);
            await UpdateCounter("extracting", "embedding");
            context.SetCustomStatus("embedding");

            ChunkEmbeddingBatchResult embedding = await CallEmbeddingActivityWithDurableRateLimitAsync<ChunkEmbeddingBatchResult>(
                context,
                nameof(GenerateChunkEmbeddingsActivity),
                new EmbeddingInput(
                    input.TenantId,
                    extraction.ExtractedContent,
                    EmbeddingContentKind.Payload,
                    extraction.ExtractedContentReference,
                    traceContext),
                For(nameof(GenerateChunkEmbeddingsActivity)));
            AddPayloadReferences(transientPayloads, embedding);

            logger.LogInformation(
                "Embedding generated: {Provider}, {Dims} dimensions across {ChunkCount} chunks",
                embedding.Provider,
                embedding.Dimensions,
                embedding.Chunks.Count);

            // Story 9.2 Task 5.3: SourceType.Event-gated dual-embedding block. Generates the NL
            // description via the DAPR Conversation API (alpha), embeds it through the SAME
            // GenerateEmbeddingActivity (ContentKind = NaturalLanguageDescription), and primes a
            // NaturalLanguageIndexInput for the fourth fan-out task. On LLM unavailability, the block
            // catches NaturalLanguageDescriptionUnavailableException ONLY (wider catches would mask
            // real bugs per Anti-Patterns) and queues the memory unit for background retry. Memory
            // units remain searchable via the three non-NL axes.
            NaturalLanguageEmbeddingStatus nlStatus = NaturalLanguageEmbeddingStatus.NotApplicable;
            EmbeddingResult? nlEmbedding = null;
            NaturalLanguageDescriptionResult? nlResult = null;
            if (input.SourceType == SourceType.Event)
            {
                string rawJsonPayload = sourcePayloadReference is null && input.ContentBytes is { Length: > 0 }
                    ? System.Text.Encoding.UTF8.GetString(input.ContentBytes)
                    : extraction.ExtractedContent;
                string eventType = input.Metadata.TryGetValue("cloudevent.type", out MetadataField? et)
                    ? et.Value
                    : "(unknown)";
                string? aggregateType = input.Metadata.TryGetValue("event.aggregateType", out MetadataField? at)
                    ? at.Value
                    : null;

                try
                {
                    nlResult = await context.CallActivityAsync<NaturalLanguageDescriptionResult>(
                        nameof(GenerateNaturalLanguageDescriptionActivity),
                        new NaturalLanguageDescriptionInput(
                            input.TenantId,
                            memoryUnitId,
                            rawJsonPayload,
                            eventType,
                            aggregateType,
                            sourcePayloadReference,
                            traceContext),
                        For(nameof(GenerateNaturalLanguageDescriptionActivity)));

                    nlEmbedding = await CallEmbeddingActivityWithDurableRateLimitAsync<EmbeddingResult>(
                        context,
                        nameof(GenerateEmbeddingActivity),
                        new EmbeddingInput(
                            input.TenantId,
                            nlResult.Description,
                            EmbeddingContentKind.NaturalLanguageDescription,
                            TraceContext: traceContext),
                        For(nameof(GenerateEmbeddingActivity)));

                    nlStatus = NaturalLanguageEmbeddingStatus.Indexed;
                }
                catch (NaturalLanguageDescriptionUnavailableException)
                {
                    logger.LogInformation(
                        "NL description unavailable for {MemoryUnitId}; queueing for retry (event 9152).",
                        memoryUnitId);

                    await context.CallActivityAsync<bool>(
                        nameof(QueueNaturalLanguageEmbeddingRetryActivity),
                        new QueueNaturalLanguageEmbeddingRetryInput(
                            input.TenantId,
                            memoryUnitId,
                            rawJsonPayload,
                            eventType,
                            aggregateType,
                            input.CaseId,
                            embedding.Provider,
                            GetEmbeddingModelIdentifier(embedding),
                            embedding.Dimensions,
                            context.CurrentUtcDateTime.Ticks,
                            sourcePayloadReference,
                            traceContext),
                        compensationRetry);

                    nlStatus = NaturalLanguageEmbeddingStatus.Queued;
                }
            }

            currentStage = "indexing";
            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Indexing);
            await UpdateCounter("embedding", "indexing");
            context.SetCustomStatus("indexing");

            Dictionary<string, MetadataField> metadataForIndex = BuildIndexMetadata(input, urlFetch, effectiveUrl, fetchedContentLength);

            if (input.SourceType == SourceType.Event)
            {
                metadataForIndex["event.naturalLanguageEmbeddingStatus"] = new MetadataField(
                    nlStatus.ToString(),
                    MetadataOrigin.Ai,
                    1.0f);
            }

            // Story 9.2 Task 5.7: optionally persist the NL description to metadata so it's visible
            // via FT.SEARCH on the syntactic index and via GET memory-unit. Default is false (ADR 9.2-F
            // — storage economy); operators opt in via appsettings NaturalLanguage:PersistInMetadata.
            //
            // When the LLM did not provide a logprobs-derived confidence (EstimatedConfidence is null),
            // we SKIP the metadata entry rather than coercing to `0.0f`. Coercion re-introduces the
            // pseudo-numeric antipattern the design explicitly rejected: the UI would render "0%
            // confidence" — worse than absent. ConfidenceSource.Constant implies "unmeasured"; the
            // UI affordance is "no confidence signal," which the absence models correctly.
            if (nlResult is not null
                && nlResult.EstimatedConfidence is float measuredConfidence
                && ShouldPersistNaturalLanguageDescription(input.WorkflowConfiguration))
            {
                metadataForIndex["event.naturalLanguageDescription"] = new MetadataField(
                    nlResult.Description,
                    MetadataOrigin.Ai,
                    measuredConfidence);
            }

            IndexInput indexInput = new()
            {
                MemoryUnitId = memoryUnitId,
                TenantId = input.TenantId,
                CaseId = input.CaseId,
                Content = extraction.ExtractedContent,
                ContentReference = extraction.ExtractedContentReference,
                ContentHash = extraction.ContentHash,
                SourceUri = input.SourceUri,
                SourceType = input.SourceType,
                IngestedBy = input.IngestedBy,
                IngestedAt = ingestedAt,
                EmbeddingVector = embedding.Chunks[0].Vector,
                EmbeddingVectorReference = embedding.Chunks[0].VectorReference,
                EmbeddingProvider = embedding.Provider,
                // Story 5.5 FR70: thread the model through from EmbeddingResult so it lands in
                // the Redis hash (see IndexSyntacticActivity) and is readable via GET memory-unit.
                // Historical replayed EmbeddingResult payloads may lack the field — fall back to
                // parsing the compound provider string (provider:model) so the durable field still
                // stores only the model identifier rather than the full compound value.
                EmbeddingModel = GetEmbeddingModelIdentifier(embedding),
                EmbeddingDimensions = embedding.Dimensions,
                Metadata = metadataForIndex,
                CausationId = input.CausationId,
                CorrelationId = input.CorrelationId,
                TraceContext = traceContext,
            };

            Task<IndexResult> syntacticTask = context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity),
                indexInput,
                For(nameof(IndexSyntacticActivity)));
            SemanticChunkIndexInput semanticIndexInput = new()
            {
                MemoryUnitId = memoryUnitId,
                TenantId = input.TenantId,
                CaseId = input.CaseId,
                Chunks = embedding.Chunks,
                EmbeddingProvider = embedding.Provider,
                EmbeddingModel = GetEmbeddingModelIdentifier(embedding),
                EmbeddingDimensions = embedding.Dimensions,
                Metadata = metadataForIndex,
                TraceContext = traceContext,
            };

            Task<IndexResult> semanticTask = context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticChunksActivity),
                semanticIndexInput,
                For(nameof(IndexSemanticChunksActivity)));
            Task<IndexResult> graphTask = context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity),
                indexInput,
                For(nameof(IndexGraphActivity)));

            // Story 9.2 Task 5.5: fourth fan-out task when the NL embedding succeeded. Cleanup
            // compensation (Task 4.7) handles both the raw and NL hashes via the single
            // CleanupSemanticActivity — no additional compensation activity is needed.
            Task<IndexResult>? nlSemanticTask = null;
            if (nlEmbedding is not null && nlResult is not null)
            {
                NaturalLanguageIndexInput nlIndexInput = new()
                {
                    MemoryUnitId = memoryUnitId,
                    TenantId = input.TenantId,
                    CaseId = input.CaseId,
                    EmbeddingVector = nlEmbedding.Vector,
                    EmbeddingProvider = nlEmbedding.Provider,
                    EmbeddingModel = GetEmbeddingModelIdentifier(nlEmbedding),
                    EmbeddingDimensions = nlEmbedding.Dimensions,
                    NaturalLanguageDescription = nlResult.Description,
                    DescriptionConfidence = nlResult.EstimatedConfidence,
                    ConfidenceSource = nlResult.ConfidenceSource,
                    TraceContext = traceContext,
                };

                nlSemanticTask = context.CallActivityAsync<IndexResult>(
                    nameof(IndexNaturalLanguageSemanticActivity),
                    nlIndexInput,
                    For(nameof(IndexNaturalLanguageSemanticActivity)));
            }

            try
            {
                if (nlSemanticTask is not null)
                {
                    await Task.WhenAll(syntacticTask, semanticTask, graphTask, nlSemanticTask);
                }
                else
                {
                    await Task.WhenAll(syntacticTask, semanticTask, graphTask);
                }
            }
            catch (Exception ex)
            {
                HashSet<string> completedBackends = GetCompletedBackends(syntacticTask, semanticTask, graphTask, nlSemanticTask);
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
                            memoryUnitId,
                            traceContext));
                }
                catch
                {
                    // Activity recording failure must not mask the original ingestion failure
                }

                logger.LogInformation(
                    "Indexing failed for {MemoryUnitId}, compensated backends: [{Backends}]",
                    memoryUnitId,
                    string.Join(", ", completedBackends));

                AttachFailureDetails(
                    ex,
                    memoryUnitId,
                    currentStage,
                    _mainRetryAttempts,
                    new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero),
                    logger);
                try { await UpdateCounter("indexing", "none"); } catch { /* counter drift documented */ }
                context.SetCustomStatus("failed");
                WorkflowPayloadReference? retainedSourcePayload = GetRetainedSourcePayloadReference(input, memoryUnitId);
                await TryPersistFailedUnit(context, input, memoryUnitId, currentStage, ex, compensationRetry, logger, retainedSourcePayload);
                await CleanupTransientPayloadsAsync(context, input.TenantId, memoryUnitId, transientPayloads, compensationRetry, retainedSourcePayload, traceContext);
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
                    new ConsistencyInput(memoryUnitId, input.TenantId, traceContext),
                    For(nameof(VerifyConsistencyActivity)));

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
                DedupKeySaveResult sourceSave = await context.CallActivityAsync<DedupKeySaveResult>(
                    nameof(SaveDedupKeyActivity),
                    new DedupKeyInput(dedupKey, memoryUnitId, traceContext),
                    For(nameof(SaveDedupKeyActivity)));

                if (IsDuplicateOwnedByAnother(sourceSave, memoryUnitId))
                {
                    return await CompletePostIndexDuplicateAsync(
                        context,
                        memoryUnitId,
                        sourceSave.MemoryUnitId,
                        nlSemanticTask,
                        cleanupInput,
                        transientPayloads,
                        compensationRetry,
                        logger,
                        UpdateCounter);
                }

                // Story 18.4 — when an explicit idempotency token was supplied, also persist a token-keyed
                // permanent record pointing at the SAME MemoryUnitId. This augments (never replaces) the
                // sourceUri mapping written above, so token-based redelivery stays idempotent while
                // Stories 18.5/18.6's sourceUri → MemoryUnitId lookup and stability remain intact.
                if (!string.IsNullOrWhiteSpace(input.IdempotencyToken))
                {
                    string tokenDedupKey = DedupKeyBuilder.BuildTokenKey(input.TenantId, input.CaseId, input.IdempotencyToken);
                    DedupKeySaveResult tokenSave = await context.CallActivityAsync<DedupKeySaveResult>(
                        nameof(SaveDedupKeyActivity),
                        new DedupKeyInput(tokenDedupKey, memoryUnitId, traceContext),
                        For(nameof(SaveDedupKeyActivity)));

                    if (IsDuplicateOwnedByAnother(tokenSave, memoryUnitId))
                    {
                        if (sourceSave.IsSaved)
                        {
                            await context.CallActivityAsync<bool>(
                                nameof(ReleaseDedupKeyIfOwnedActivity),
                                new DedupKeyInput(dedupKey, memoryUnitId, traceContext),
                                compensationRetry);
                        }

                        return await CompletePostIndexDuplicateAsync(
                            context,
                            memoryUnitId,
                            tokenSave.MemoryUnitId,
                            nlSemanticTask,
                            cleanupInput,
                            transientPayloads,
                            compensationRetry,
                            logger,
                            UpdateCounter);
                    }
                }
            }
            catch (Exception ex)
            {
                // Story 9.2 Task 5.5: include "semantic-nl" so CleanupSemanticActivity is dispatched
                // even when only the NL hash was written. CleanupSemanticActivity then removes both.
                HashSet<string> postIndexBackends = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
                if (nlSemanticTask is { IsCompletedSuccessfully: true })
                {
                    postIndexBackends.Add("semantic-nl");
                }

                await CompensateAsync(
                    context,
                    postIndexBackends,
                    cleanupInput,
                    compensationRetry,
                    logger,
                    ex);

                logger.LogInformation(
                    "Post-index workflow step failed for {MemoryUnitId}; rolled back indexed backends.",
                    memoryUnitId);

                AttachFailureDetails(
                    ex,
                    memoryUnitId,
                    currentStage,
                    _mainRetryAttempts,
                    new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero),
                    logger);
                try { await UpdateCounter("indexing", "none"); } catch { /* counter drift documented */ }
                context.SetCustomStatus("failed");
                WorkflowPayloadReference? retainedSourcePayload = GetRetainedSourcePayloadReference(input, memoryUnitId);
                await TryPersistFailedUnit(context, input, memoryUnitId, currentStage, ex, compensationRetry, logger, retainedSourcePayload);
                await CleanupTransientPayloadsAsync(context, input.TenantId, memoryUnitId, transientPayloads, compensationRetry, retainedSourcePayload, traceContext);
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
                        memoryUnitId,
                        traceContext));
            }
            catch
            {
                // Activity recording is best-effort — failure must not affect ingestion result
            }

            currentStatus = TransitionStatus(logger, memoryUnitId, currentStatus, MemoryUnitStatus.Indexed);
            await UpdateCounter("indexing", "none");
            context.SetCustomStatus("indexed");
            await CleanupTransientPayloadsAsync(context, input.TenantId, memoryUnitId, transientPayloads, compensationRetry, traceContext: traceContext);

            // Story 9.2 Task 5.6: surface the NL-embedding outcome — Indexed on the healthy path,
            // Queued on the degraded path (LLM unavailable), NotApplicable for SourceType != Event.
            return new IngestionResult(
                memoryUnitId,
                currentStatus,
                ingestedAt,
                WasDuplicate: false,
                ConsistencyNote: consistencyNote)
            {
                NaturalLanguageEmbeddingStatus = nlStatus,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !HasFailureDetails(ex))
        {
            AttachFailureDetails(
                ex,
                memoryUnitId,
                currentStage,
                GetRetryCountForStage(currentStage),
                new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero),
                logger);
            try { await UpdateCounter(MapStageToBucket(currentStage), "none"); } catch { /* counter drift documented */ }
            context.SetCustomStatus("failed");
            WorkflowPayloadReference? retainedSourcePayload = GetRetainedSourcePayloadReference(input, memoryUnitId);
            await TryPersistFailedUnit(context, input, memoryUnitId, currentStage, ex, compensationRetry, logger, retainedSourcePayload);
            await CleanupTransientPayloadsAsync(context, input.TenantId, memoryUnitId, transientPayloads, compensationRetry, retainedSourcePayload, traceContext);
            throw;
        }
    }

    internal static WorkflowTaskOptions CreateCompensationRetry() => new(
        new WorkflowRetryPolicy(
            maxNumberOfAttempts: _compensationRetryAttempts,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromSeconds(30)));

    private static IReadOnlyDictionary<string, WorkflowTaskOptions> CreateActivityRetryOptions(
        IngestionActivityRetryConfiguration? retryConfiguration)
    {
        IngestionActivityRetryConfiguration retry = retryConfiguration ?? new IngestionActivityRetryConfiguration();
        Dictionary<string, WorkflowTaskOptions> map = new(StringComparer.Ordinal)
        {
            [DefaultRetryKey] = ToWorkflowTaskOptions(retry.Default, "workflowConfiguration.retry.default"),
        };

        foreach ((string activityName, WorkflowActivityRetryPolicy policy) in retry.ActivityOverrides)
        {
            map[activityName] = ToWorkflowTaskOptions(policy, $"workflowConfiguration.retry.activityOverrides.{activityName}");
        }

        return map;
    }

    private static WorkflowTaskOptions ToWorkflowTaskOptions(WorkflowActivityRetryPolicy policy, string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.MaxAttempts <= 0)
        {
            throw new InvalidOperationException(
                $"RETRY_CONFIG_INVALID: {configurationPath}.maxAttempts must be > 0 (was {policy.MaxAttempts}).");
        }

        return new WorkflowTaskOptions(new WorkflowRetryPolicy(
            maxNumberOfAttempts: policy.MaxAttempts,
            firstRetryInterval: TimeSpan.FromSeconds(policy.FirstRetryIntervalSeconds),
            backoffCoefficient: policy.BackoffCoefficient,
            maxRetryInterval: TimeSpan.FromSeconds(policy.MaxRetryIntervalSeconds)));
    }

    private static bool ShouldPersistNaturalLanguageDescription(IngestionWorkflowConfiguration? workflowConfiguration)
        => workflowConfiguration?.NaturalLanguage.PersistInMetadata == true;

    private static async Task<T> CallEmbeddingActivityWithDurableRateLimitAsync<T>(
        WorkflowContext context,
        string activityName,
        EmbeddingInput input,
        WorkflowTaskOptions retryOptions)
    {
        WorkflowRetryPolicy? retryPolicy = retryOptions.RetryPolicy;
        WorkflowTaskOptions singleAttemptOptions = new(
            RetryPolicy: null,
            TargetAppId: retryOptions.TargetAppId,
            PropagationScope: retryOptions.PropagationScope);
        int maxGenericAttempts = retryPolicy?.MaxNumberOfAttempts ?? 1;
        int genericAttempt = 0;
        int providerRateLimitRetryCount = 0;

        while (true)
        {
            try
            {
                return await context.CallActivityAsync<T>(activityName, input, singleAttemptOptions);
            }
            catch (WorkflowTaskFailedException ex) when (TryGetProviderRateLimitDelay(ex, out TimeSpan delay))
            {
                providerRateLimitRetryCount++;
                if (providerRateLimitRetryCount > _embeddingProviderRateLimitMaxDurableRetries)
                {
                    throw;
                }

                await context.CreateTimer(delay, CancellationToken.None);
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                && ShouldRetryGenericActivityFailure(++genericAttempt, maxGenericAttempts))
            {
                await context.CreateTimer(GetGenericRetryDelay(retryPolicy!, genericAttempt), CancellationToken.None);
            }
        }
    }

    private static TimeSpan GetGenericRetryDelay(WorkflowRetryPolicy retryPolicy, int failedAttemptNumber)
    {
        double multiplier = Math.Pow(retryPolicy.BackoffCoefficient, failedAttemptNumber - 1);
        double ticks = retryPolicy.FirstRetryInterval.Ticks * multiplier;
        TimeSpan delay = TimeSpan.FromTicks((long)Math.Min(ticks, TimeSpan.MaxValue.Ticks));
        TimeSpan maxRetryInterval = retryPolicy.MaxRetryInterval ?? TimeSpan.FromHours(1);
        return delay <= maxRetryInterval ? delay : maxRetryInterval;
    }

    private static bool ShouldRetryGenericActivityFailure(int failedAttemptNumber, int maxAttempts)
        => failedAttemptNumber < maxAttempts;

    private static bool TryGetProviderRateLimitDelay(WorkflowTaskFailedException exception, out TimeSpan delay)
    {
        delay = default;
        WorkflowTaskFailureDetails? details = exception.FailureDetails;
        if (details is null
            || !details.IsCausedBy<EmbeddingRateLimitException>()
            || !EmbeddingRateLimitRetryAfter.TryExtractProviderSeconds(details.ErrorMessage, out int retryAfterSeconds))
        {
            return false;
        }

        delay = TimeSpan.FromSeconds(retryAfterSeconds);
        return true;
    }

    private static string ResolveMemoryUnitId(WorkflowContext context, IngestionInput input)
    {
        if (!string.IsNullOrWhiteSpace(context.InstanceId)
            && !RequiresIndependentMemoryUnitId(context.InstanceId, input.SourceType))
        {
            return context.InstanceId;
        }

        return context.NewGuid().ToString();
    }

    private static bool RequiresIndependentMemoryUnitId(string workflowInstanceId, SourceType sourceType)
        => sourceType == SourceType.Event
            && workflowInstanceId.StartsWith(DedupWorkflowInstancePrefix, StringComparison.Ordinal);

    private static HashSet<string> GetCompletedBackends(
        Task<IndexResult> syntacticTask,
        Task<IndexResult> semanticTask,
        Task<IndexResult> graphTask,
        Task<IndexResult>? nlSemanticTask = null)
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

        // Story 9.2 Task 5.5: cleanup for the NL hash is coupled into CleanupSemanticActivity
        // (Task 4.7), so "semantic" already dispatches both hashes. The "semantic-nl" marker here is
        // purely informational — it tells compensation that the NL hash was written even if the raw
        // hash somehow was not (e.g., NL task succeeded while syntactic failed).
        if (nlSemanticTask is not null && nlSemanticTask.IsCompletedSuccessfully)
        {
            completedBackends.Add(nlSemanticTask.Result.Backend);
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

        // Story 9.2 Task 4.7 + 5.5: CleanupSemanticActivity now deletes BOTH the raw and NL hashes.
        // Dispatch it if either backend completed — we never want a half-cleaned compensation state
        // where the raw hash was removed but the NL hash survived (or vice-versa).
        if (completedBackends.Contains("semantic") || completedBackends.Contains("semantic-nl"))
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

    private static bool IsDuplicateOwnedByAnother(DedupKeySaveResult result, string memoryUnitId)
        => result.Status == DedupKeySaveStatus.DuplicateExisting
            && !string.Equals(result.MemoryUnitId, memoryUnitId, StringComparison.Ordinal);

    private static async Task<IngestionResult> CompletePostIndexDuplicateAsync(
        WorkflowContext context,
        string loserMemoryUnitId,
        string winnerMemoryUnitId,
        Task<IndexResult>? nlSemanticTask,
        CleanupInput cleanupInput,
        IReadOnlyList<WorkflowPayloadReference> transientPayloads,
        WorkflowTaskOptions compensationRetry,
        ILogger logger,
        Func<string, string, Task<bool>> updateCounter)
    {
        HashSet<string> postIndexBackends = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
        if (nlSemanticTask is { IsCompletedSuccessfully: true })
        {
            postIndexBackends.Add("semantic-nl");
        }

        await CompensateAsync(
            context,
            postIndexBackends,
            cleanupInput,
            compensationRetry,
            logger);

        logger.LogInformation(
            "Post-index dedup race resolved for loser {LoserMemoryUnitId}; winner is {WinnerMemoryUnitId}.",
            loserMemoryUnitId,
            winnerMemoryUnitId);

        try { await updateCounter("indexing", "none"); } catch { /* counter drift documented */ }
        context.SetCustomStatus("duplicate");
        await CleanupTransientPayloadsAsync(
            context,
            cleanupInput.TenantId,
            loserMemoryUnitId,
            transientPayloads,
            compensationRetry,
            traceContext: cleanupInput.TraceContext);

        return new IngestionResult(
            winnerMemoryUnitId,
            MemoryUnitStatus.Indexed,
            new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero),
            WasDuplicate: true,
            ConsistencyNote: null)
        {
            NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.NotApplicable,
        };
    }

    private static void AttachFailureDetails(
        Exception exception,
        string memoryUnitId,
        string stage,
        int retryCount,
        DateTimeOffset now,
        ILogger logger)
    {
        string? message = exception.Message;
        if (message is { Length: > 1024 })
        {
            message = message[..1024];
        }

        FailureDetails details = new(stage, GetErrorCode(exception), retryCount, message, now);

        exception.Data[nameof(FailureDetails)] = details;
        exception.Data[nameof(MemoryUnitStatus)] = MemoryUnitStatus.Failed;
        exception.Data["MemoryUnitId"] = memoryUnitId;

        logger.LogError(
            exception,
            "Ingestion failed for {MemoryUnitId}. Status={Status}; stage={Stage}; errorCode={ErrorCode}; retryCount={RetryCount}; message={ErrorMessage}",
            memoryUnitId,
            MemoryUnitStatus.Failed,
            details.Stage,
            details.ErrorCode,
            details.RetryCount,
            details.ErrorMessage);
    }

    private static string GetErrorCode(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is UrlFetchException fetchException)
        {
            return fetchException.ErrorCode;
        }

        if (exception is WorkflowTaskFailedException workflowException)
        {
            if (UrlFetchException.TryExtractErrorCode(workflowException.FailureDetails?.ErrorMessage, out string wrappedCode))
            {
                return wrappedCode;
            }

            if (UrlFetchException.TryExtractErrorCode(workflowException.Message, out string workflowMessageCode))
            {
                return workflowMessageCode;
            }
        }

        if (UrlFetchException.TryExtractErrorCode(exception.Message, out string messageCode))
        {
            return messageCode;
        }

        if (exception.InnerException is not null)
        {
            string innerCode = GetErrorCode(exception.InnerException);
            if (!string.Equals(innerCode, exception.InnerException.GetType().Name, StringComparison.Ordinal))
            {
                return innerCode;
            }
        }

        return exception.GetType().Name;
    }

    private static int GetRetryCountForStage(string stage)
        => string.Equals(stage, "validation", StringComparison.OrdinalIgnoreCase)
            ? 0
            : _mainRetryAttempts;

    private static bool HasFailureDetails(Exception exception)
        => exception.Data.Contains(nameof(FailureDetails));

    /// <summary>Story 6.3: maps the workflow's pipeline-stage string to the counter-actor bucket name.
    /// Stages BEFORE the queued bucket is incremented (idempotency, validation) map to <c>"none"</c>; stages
    /// AFTER indexing began (verifying, dedup) map to <c>"indexing"</c> because that's the last bucket the
    /// workflow occupied.</summary>
    private static string MapStageToBucket(string stage) => stage switch
    {
        "queued" => "queued",
        "fetching" or "extracting" => "extracting",
        "embedding" => "embedding",
        "indexing" or "verifying" or "dedup" => "indexing",
        _ => "none",
    };

    /// <summary>Story 6.3 NFR19: persists the failed unit to Redis as a best-effort step. A persistence
    /// failure logs event 6309 but never masks the original failure.</summary>
    private static async Task TryPersistFailedUnit(
        WorkflowContext context,
        IngestionInput input,
        string memoryUnitId,
        string stage,
        Exception failure,
        WorkflowTaskOptions retry,
        ILogger logger,
        WorkflowPayloadReference? retainedSourcePayloadReference = null)
    {
        try
        {
            FailureDetails? details = failure.Data[nameof(FailureDetails)] as FailureDetails;
            FailedUnitInput failedInput = new(
                input.TenantId,
                input.CaseId,
                memoryUnitId,
                input.SourceUri,
                input.SourceType,
                input.IngestedBy,
                input.SourceType == SourceType.Url ? null : input.ContentType,
                stage,
                details?.ErrorCode ?? failure.GetType().Name,
                details?.ErrorMessage,
                details?.RetryCount ?? 0,
                details?.LastRetryAt,
                new DateTimeOffset(context.CurrentUtcDateTime, TimeSpan.Zero),
                retainedSourcePayloadReference,
                input.Metadata.Count == 0
                    ? null
                    : new Dictionary<string, MetadataField>(input.Metadata, StringComparer.Ordinal),
                input.CausationId,
                input.CorrelationId,
                input.TraceContext);
            await context.CallActivityAsync<bool>(nameof(PersistFailedUnitActivity), failedInput, retry);
        }
        catch (Exception persistEx)
        {
            RetryFailureLog.LogFailedUnitPersistenceFailed(logger, memoryUnitId, persistEx.Message);
        }
    }

    /// <summary>
    /// Story 6.1 AC10: attach http.* metadata as AI-origin fields (confidence 1.0) when the ingestion
    /// source is a URL. Caller-supplied metadata is preserved verbatim.
    /// </summary>
    private static Dictionary<string, MetadataField> BuildIndexMetadata(
        IngestionInput input,
        UrlFetchResult? urlFetch,
        string effectiveUrl,
        long contentLength)
    {
        // Decision D6: `new Dictionary<TKey, TValue>(IDictionary<...>)` does NOT carry the source's
        // comparer; explicitly pass StringComparer.Ordinal to preserve the pinned lookup semantics.
        Dictionary<string, MetadataField> metadata = new(input.Metadata, StringComparer.Ordinal);

        if (input.SourceType != SourceType.Url || urlFetch is null)
        {
            return metadata;
        }

        metadata["http.finalUrl"] = new MetadataField(effectiveUrl, MetadataOrigin.Ai, 1.0f);

        if (!string.IsNullOrWhiteSpace(urlFetch.ContentType))
        {
            metadata["http.contentType"] = new MetadataField(urlFetch.ContentType, MetadataOrigin.Ai, 1.0f);
        }

        metadata["http.contentLength"] = new MetadataField(
            contentLength.ToString(System.Globalization.CultureInfo.InvariantCulture),
            MetadataOrigin.Ai,
            1.0f);

        return metadata;
    }

    private static int GetExtractedLength(ExtractionResult extraction)
        => extraction.ExtractedContentReference is not null
            ? checked((int)Math.Min(int.MaxValue, extraction.ExtractedContentReference.ByteLength))
            : extraction.ExtractedContent.Length;

    private static void AddPayloadReferences(List<WorkflowPayloadReference> references, ChunkEmbeddingBatchResult embedding)
    {
        foreach (ChunkEmbeddingResult chunk in embedding.Chunks)
        {
            AddPayloadReference(references, chunk.TextReference);
            AddPayloadReference(references, chunk.VectorReference);
        }
    }

    private static void AddPayloadReference(List<WorkflowPayloadReference> references, WorkflowPayloadReference? reference)
    {
        if (reference is not null && !references.Contains(reference))
        {
            references.Add(reference);
        }
    }

    private static WorkflowPayloadReference? GetRetainedSourcePayloadReference(IngestionInput input, string memoryUnitId)
    {
        WorkflowPayloadReference? reference = input.PayloadReference;
        return input.SourceType != SourceType.Url
            && reference is not null
            && reference.ContentKind == WorkflowPayloadKind.SourceBytes
            && string.Equals(reference.TenantId, input.TenantId, StringComparison.Ordinal)
            && IsExpectedSourcePayloadScope(input, memoryUnitId, reference.MemoryUnitId)
            ? reference
            : null;
    }

    private static bool IsExpectedSourcePayloadScope(IngestionInput input, string memoryUnitId, string referenceMemoryUnitId)
        => string.Equals(referenceMemoryUnitId, memoryUnitId, StringComparison.Ordinal)
            || (input.SourceType == SourceType.Event
                && string.Equals(
                    referenceMemoryUnitId,
                    DedupKeyBuilder.BuildKey(input.TenantId, input.CaseId, input.SourceUri),
                    StringComparison.Ordinal));

    private static async Task CleanupTransientPayloadsAsync(
        WorkflowContext context,
        string tenantId,
        string memoryUnitId,
        IReadOnlyList<WorkflowPayloadReference> references,
        WorkflowTaskOptions retry,
        WorkflowPayloadReference? retainedSourcePayloadReference = null,
        WorkflowTraceContext? traceContext = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        IReadOnlyList<WorkflowPayloadReference> cleanupReferences = retainedSourcePayloadReference is null
            ? references
            : references
                .Where(reference => !reference.Equals(retainedSourcePayloadReference))
                .ToArray();

        foreach (IGrouping<string, WorkflowPayloadReference> group in cleanupReferences
            .Distinct()
            .GroupBy(static reference => reference.MemoryUnitId, StringComparer.Ordinal))
        {
            await context.CallActivityAsync<bool>(
                nameof(CleanupWorkflowPayloadsActivity),
                new CleanupWorkflowPayloadsInput(tenantId, group.Key, group.ToArray(), traceContext),
                retry);
        }
    }

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

    private static string GetEmbeddingModelIdentifier(ChunkEmbeddingBatchResult embedding)
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
