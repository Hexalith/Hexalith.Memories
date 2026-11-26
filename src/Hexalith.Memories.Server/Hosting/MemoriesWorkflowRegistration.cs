// <copyright file="MemoriesWorkflowRegistration.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Activities.Restore;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.DerivedStores;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the workflows and activities supported by the Memories Server.</summary>
internal static class MemoriesWorkflowRegistration
{
    /// <summary>Registers the Server workflow runtime and client.</summary>
    internal static void AddMemoriesWorkflows(this IServiceCollection services)
    {
        services.AddDaprWorkflow(options =>
        {
            options.RegisterActivity<ExtractContentActivity>();
            options.RegisterActivity<GenerateEmbeddingActivity>();
            options.RegisterActivity<GenerateChunkEmbeddingsActivity>();
            options.RegisterActivity<IndexSyntacticActivity>();
            options.RegisterActivity<IndexSemanticActivity>();
            options.RegisterActivity<IndexSemanticChunksActivity>();
            options.RegisterActivity<IndexGraphActivity>();

            // Story 9.2 Task 2.6: NL description activity (dual-embedding pipeline, SourceType.Event).
            options.RegisterActivity<GenerateNaturalLanguageDescriptionActivity>();

            // Story 9.2 Task 4.4: NL semantic index activity (writes to {tenant}:memories:vec:nl).
            options.RegisterActivity<IndexNaturalLanguageSemanticActivity>();

            // Story 9.2 Task 5.4: enqueue activity for the degraded path (LLM unavailable → queue for retry).
            options.RegisterActivity<QueueNaturalLanguageEmbeddingRetryActivity>();

            // Story 9.2 Task 8.4a: retry guard activity that prevents recreating orphan NL hashes after delete/rollback.
            options.RegisterActivity<CheckMemoryUnitExistsActivity>();

            // Story 9.2 Task 8.4: retry workflow that re-runs NL description + embedding + index.
            options.RegisterActivity<RetryNaturalLanguageEmbeddingActivity>();
            options.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();

            options.RegisterWorkflow<IngestionWorkflow>();
            options.RegisterWorkflow<DerivedStoreCorrectionWorkflow>();
            options.RegisterActivity<PromoteDerivedStoreSourceArtifactActivity>();
            options.RegisterActivity<ApplyDerivedStoreCorrectionActivity>();
            options.RegisterWorkflow<CaseCreationProjectionWorkflow>();
            options.RegisterWorkflow<AnnotationProjectionWorkflow>();
            options.RegisterWorkflow<MemoryUnitDeletionProjectionWorkflow>();
            options.RegisterWorkflow<CaseDeletionProjectionWorkflow>();
            options.RegisterActivity<ValidateContentActivity>();
            options.RegisterActivity<CheckIdempotencyActivity>();
            options.RegisterActivity<RecordSourceInventoryOutputActivity>();
            options.RegisterActivity<RecordSourceInventoryCandidateActivity>();
            options.RegisterActivity<PauseSourceInventoryProducerActivity>();
            options.RegisterActivity<AdmitSourceInventoryActivity>();
            options.RegisterActivity<SetSourceInventorySchedulingActivity>();
            options.RegisterActivity<PublishRefreshActivity>();
            options.RegisterActivity<ReconcileRefreshPublicationActivity>();
            options.RegisterActivity<SaveDedupKeyActivity>();
            options.RegisterActivity<ReleaseDedupKeyIfOwnedActivity>();
            options.RegisterActivity<VerifyConsistencyActivity>();
            options.RegisterActivity<CleanupSyntacticActivity>();
            options.RegisterActivity<CleanupSemanticActivity>();
            options.RegisterActivity<CleanupGraphActivity>();
            options.RegisterActivity<CleanupWorkflowPayloadsActivity>();
            options.RegisterActivity<RecordCaseActivityActivity>();
            options.RegisterActivity<ProjectCaseHashActivity>();
            options.RegisterActivity<ProjectCaseGraphActivity>();
            options.RegisterActivity<CleanupCaseProjectionActivity>();
            options.RegisterActivity<ProjectAnnotationGraphActivity>();
            options.RegisterActivity<ScheduleAnnotationIngestionActivity>();
            options.RegisterActivity<DeleteMemoryUnitProjectionActivity>();
            options.RegisterActivity<MarkCaseDeletingActivity>();
            options.RegisterActivity<DeleteCaseProjectionActivity>();
            options.RegisterActivity<DeleteCaseRouteMappingsActivity>();

            options.RegisterActivity<FetchUrlActivity>();

            options.RegisterActivity<PersistFailedUnitActivity>();
            options.RegisterActivity<UpdateCaseIngestionCounterActivity>();

            options.RegisterWorkflow<TenantProvisioningWorkflow>();
            options.RegisterActivity<ProvisionRediSearchActivity>();
            options.RegisterActivity<ProvisionRedisVectorActivity>();
            options.RegisterActivity<ProvisionFalkorDbActivity>();
            options.RegisterActivity<VerifyTenantActivity>();
            options.RegisterActivity<DeleteRediSearchIndexActivity>();
            options.RegisterActivity<DeleteRedisVectorIndexActivity>();
            options.RegisterActivity<DeleteFalkorDbGraphActivity>();
            options.RegisterActivity<InitializeTenantRegistryActivity>();
            options.RegisterActivity<UpdateTenantStatusActivity>();
            options.RegisterActivity<RemoveTenantRegistryActivity>();

            options.RegisterWorkflow<TenantDeletionWorkflow>();
            options.RegisterActivity<DeleteRediSearchActivity>();
            options.RegisterActivity<DeleteRedisVectorActivity>();
            options.RegisterActivity<DeleteFalkorDbBatchActivity>();
            options.RegisterActivity<DeleteFalkorDbGraphFinalizerActivity>();
            options.RegisterActivity<DeleteTenantDataKeysActivity>();
            options.RegisterActivity<GetTenantRegistryActivity>();

            // Story 8.2: consistency verification & repair.
            options.RegisterWorkflow<ConsistencyVerificationWorkflow>();
            options.RegisterWorkflow<ConsistencyRepairWorkflow>();
            options.RegisterActivity<EnumerateMemoryUnitIdsActivity>();
            options.RegisterActivity<RepairUnitActivity>();

            // Story 26.2: backup/restore — durable restore orchestration + activities.
            options.RegisterWorkflow<RestoreWorkflow>();
            options.RegisterActivity<RestoreDataPlaneActivity>();
            options.RegisterActivity<RestoreReindexUnitActivity>();
            options.RegisterActivity<RestoreReindexBatchActivity>();
            options.RegisterActivity<DeleteRestoreStagingActivity>();
        });
        services.TryAddSingleton<IDaprWorkflowClient>(sp => sp.GetRequiredService<DaprWorkflowClient>());
    }
}
