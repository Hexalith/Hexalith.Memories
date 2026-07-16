// <copyright file="EmbeddingVectorMigrationService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using System.Diagnostics;
using System.Net.Http;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Coordinates dry-run, live, resume, and guarded rollback for Path A embedding vector migration.</summary>
/// <param name="store">The migration storage boundary.</param>
/// <param name="vectorGenerator">The provider-aware embedding generator.</param>
public sealed class EmbeddingVectorMigrationService(
    IEmbeddingMigrationStore store,
    IEmbeddingMigrationVectorGenerator vectorGenerator)
{
    private const string RawContentKind = "payload";
    private const string NaturalLanguageContentKind = "naturalLanguageDescription";

    /// <summary>Runs the migration command.</summary>
    /// <param name="options">The command options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The migration result.</returns>
    public async Task<EmbeddingMigrationResult> RunAsync(EmbeddingMigrationOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<EmbeddingMigrationTenantReport> reports = [];
        List<EmbeddingMigrationUnitFailure> failures = [];
        List<EmbeddingMigrationProgress> progress = [];

        string? validationError = ValidateOptions(options);
        if (validationError is not null)
        {
            return new EmbeddingMigrationResult(
                options.Mode,
                EmbeddingMigrationExitCodes.Plumbing,
                validationError,
                stopwatch.Elapsed,
                reports,
                failures,
                progress);
        }

        (TenantEmbeddingConfig? configOrNull, string? targetError) = TryBuildTargetConfig(options);
        if (configOrNull is null)
        {
            return new EmbeddingMigrationResult(
                options.Mode,
                EmbeddingMigrationExitCodes.Plumbing,
                targetError ?? "Invalid target embedding configuration.",
                stopwatch.Elapsed,
                reports,
                failures,
                progress);
        }

        TenantEmbeddingConfig targetConfig = configOrNull;

        try
        {
            return options.Mode switch
            {
                EmbeddingMigrationMode.DryRun => await DryRunAsync(options, targetConfig, stopwatch, reports, failures, progress, ct).ConfigureAwait(false),
                EmbeddingMigrationMode.Live => await LiveAsync(options, targetConfig, stopwatch, reports, failures, progress, ct).ConfigureAwait(false),
                EmbeddingMigrationMode.Rollback => await RollbackAsync(options, targetConfig, stopwatch, reports, failures, progress, ct).ConfigureAwait(false),
                EmbeddingMigrationMode.Abort => await AbortAsync(options, targetConfig, stopwatch, reports, failures, progress, ct).ConfigureAwait(false),
                _ => CreateInvalidModeResult(options, stopwatch, reports, failures, progress),
            };
        }
        catch (OperationCanceledException)
        {
            return new EmbeddingMigrationResult(
                options.Mode,
                EmbeddingMigrationExitCodes.Cancelled,
                "Embedding vector migration cancelled.",
                stopwatch.Elapsed,
                reports,
                failures,
                progress);
        }
    }

    private const int MaxBatchSize = 10_000;

    private static string? ValidateOptions(EmbeddingMigrationOptions options)
    {
        if (options.Mode is EmbeddingMigrationMode.None)
        {
            return "Select exactly one mode: --dry-run, --live, --rollback, or --abort.";
        }

        if (options.Mode is (EmbeddingMigrationMode.Live or EmbeddingMigrationMode.Rollback or EmbeddingMigrationMode.Abort)
            && string.IsNullOrWhiteSpace(options.TenantId))
        {
            return $"--{options.Mode.ToString().ToLowerInvariant()} requires --tenant <tenantId>.";
        }

        if (options.Mode is not EmbeddingMigrationMode.DryRun && !options.Yes)
        {
            return "Mutation requires explicit confirmation via --yes (interactive prompt must promote to --yes on 'y'/'yes').";
        }

        if (options.BatchSize <= 0)
        {
            return "--batch-size must be greater than zero.";
        }

        if (options.BatchSize > MaxBatchSize)
        {
            return $"--batch-size must not exceed {MaxBatchSize}.";
        }

        return null;
    }

    private static (TenantEmbeddingConfig? Config, string? Error) TryBuildTargetConfig(EmbeddingMigrationOptions options)
    {
        string provider = options.TargetProvider ?? EmbeddingProviderDefaults.OllamaProviderName;
        TenantEmbeddingConfig defaults = string.Equals(provider, EmbeddingProviderDefaults.GoogleProviderName, StringComparison.OrdinalIgnoreCase)
            ? EmbeddingProviderDefaults.Google()
            : EmbeddingProviderDefaults.Ollama();

        TenantEmbeddingConfig target = defaults with
        {
            Provider = provider,
            Model = options.TargetModel ?? defaults.Model,
            Dimensions = options.TargetDimensions ?? defaults.Dimensions,
            ReindexRequired = false,
        };

        try
        {
            EmbeddingProviderDefaults.Validate(target);
        }
        catch (ArgumentException ex)
        {
            return (null, $"Invalid target embedding configuration: {EmbeddingMigrationRedactor.Redact(ex.Message)}");
        }

        return (target, null);
    }

    private async Task<EmbeddingMigrationResult> DryRunAsync(
        EmbeddingMigrationOptions options,
        TenantEmbeddingConfig targetConfig,
        Stopwatch stopwatch,
        List<EmbeddingMigrationTenantReport> reports,
        List<EmbeddingMigrationUnitFailure> failures,
        List<EmbeddingMigrationProgress> progress,
        CancellationToken ct)
    {
        IReadOnlyList<string> tenantIds = await ResolveTenantIdsAsync(options.TenantId, ct).ConfigureAwait(false);
        foreach (string tenantId in tenantIds)
        {
            TenantEmbeddingConfig currentConfig = await store.GetEmbeddingConfigAsync(tenantId, ct).ConfigureAwait(false);
            EmbeddingMigrationTenantCounts counts = await store.GetCountsAsync(tenantId, targetConfig, ct).ConfigureAwait(false);
            EmbeddingMigrationIndexInfo indexInfo = await store.GetIndexInfoAsync(tenantId, ct).ConfigureAwait(false);

            bool dimensionMismatch = IsDimensionMismatch(indexInfo, targetConfig.Dimensions);
            bool affected = ConfigDiffers(currentConfig, targetConfig)
                || dimensionMismatch
                || counts.RawStaleMetadataCount > 0
                || counts.NaturalLanguageStaleMetadataCount > 0;

            if (affected)
            {
                reports.Add(new EmbeddingMigrationTenantReport(
                    tenantId,
                    true,
                    currentConfig,
                    targetConfig,
                    counts,
                    indexInfo,
                    dimensionMismatch,
                    EmbeddingMigrationUnitCounters.Empty,
                    EmbeddingMigrationUnitCounters.Empty,
                    ManualFollowUpRequired: false));
            }
        }

        return new EmbeddingMigrationResult(
            EmbeddingMigrationMode.DryRun,
            EmbeddingMigrationExitCodes.Success,
            $"Dry-run found {reports.Count} affected tenant(s).",
            stopwatch.Elapsed,
            reports,
            failures,
            progress);
    }

    private async Task<EmbeddingMigrationResult> LiveAsync(
        EmbeddingMigrationOptions options,
        TenantEmbeddingConfig targetConfig,
        Stopwatch stopwatch,
        List<EmbeddingMigrationTenantReport> reports,
        List<EmbeddingMigrationUnitFailure> failures,
        List<EmbeddingMigrationProgress> progress,
        CancellationToken ct)
    {
        string tenantId = options.TenantId!;
        TenantEmbeddingConfig currentConfig = await store.GetEmbeddingConfigAsync(tenantId, ct).ConfigureAwait(false);
        EmbeddingMigrationTenantCounts counts = await store.GetCountsAsync(tenantId, targetConfig, ct).ConfigureAwait(false);
        EmbeddingMigrationIndexInfo indexInfo = await store.GetIndexInfoAsync(tenantId, ct).ConfigureAwait(false);

        EmbeddingMigrationUnitCounters raw = EmbeddingMigrationUnitCounters.Empty;
        EmbeddingMigrationUnitCounters naturalLanguage = EmbeddingMigrationUnitCounters.Empty;
        string? tenantLevelError = null;
        bool markerStarted = false;
        EmbeddingMigrationLease? lease = null;

        try
        {
            lease = await store.StartMigrationMarkerAsync(
                tenantId,
                currentConfig,
                targetConfig,
                options.OwnerId,
                options.MarkerLockTtl,
                options.Resume,
                options.RecoverStaleLock,
                ct).ConfigureAwait(false);
            markerStarted = true;

            await store.PrepareStagingSemanticIndexesAsync(tenantId, targetConfig, lease.Version, ct).ConfigureAwait(false);
            await store.HeartbeatMigrationMarkerAsync(tenantId, targetConfig, lease, options.MarkerLockTtl, ct).ConfigureAwait(false);

            raw = await MigrateRawAsync(
                options,
                targetConfig,
                tenantId,
                stopwatch,
                failures,
                progress,
                ct).ConfigureAwait(false);

            await store.HeartbeatMigrationMarkerAsync(tenantId, targetConfig, lease, options.MarkerLockTtl, ct).ConfigureAwait(false);

            naturalLanguage = await MigrateNaturalLanguageAsync(
                options,
                targetConfig,
                tenantId,
                stopwatch,
                failures,
                progress,
                ct).ConfigureAwait(false);

            if (raw.Failed == 0 && naturalLanguage.Failed == 0)
            {
                await store.HeartbeatMigrationMarkerAsync(tenantId, targetConfig, lease, options.MarkerLockTtl, ct).ConfigureAwait(false);
                await store.VerifyStagingSemanticIndexesAsync(tenantId, targetConfig, lease.Version, ct).ConfigureAwait(false);
                await store.CutoverStagingSemanticIndexesAsync(tenantId, currentConfig, targetConfig, lease, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tenantLevelError = EmbeddingMigrationRedactor.Redact(ex.Message);
            await AddFailureAsync(tenantId, string.Empty, "tenant", NormalizeErrorCategory(ex), ex.Message, failures, ct).ConfigureAwait(false);
        }

        // Skip CompleteMigrationMarkerAsync when marker start failed (e.g. --resume without prior
        // marker), tenant-level work failed, or per-unit failures require resume. The marker is
        // only "completed" after a clean live run.
        bool hasUnitFailures = raw.Failed > 0 || naturalLanguage.Failed > 0;
        if (markerStarted && lease is not null && tenantLevelError is null && !hasUnitFailures)
        {
            try
            {
                await store.HeartbeatMigrationMarkerAsync(tenantId, targetConfig, lease, options.MarkerLockTtl, ct).ConfigureAwait(false);
                await store.CompleteMigrationMarkerAsync(tenantId, targetConfig, lease, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                tenantLevelError = EmbeddingMigrationRedactor.Redact(ex.Message);
                await AddFailureAsync(tenantId, string.Empty, "tenant", NormalizeErrorCategory(ex), ex.Message, failures, ct).ConfigureAwait(false);
            }
        }

        bool manualFollowUp = tenantLevelError is not null || hasUnitFailures;
        reports.Add(new EmbeddingMigrationTenantReport(
            tenantId,
            true,
            currentConfig,
            targetConfig,
            counts,
            indexInfo,
            IsDimensionMismatch(indexInfo, targetConfig.Dimensions),
            raw,
            naturalLanguage,
            manualFollowUp));

        int exitCode = manualFollowUp ? EmbeddingMigrationExitCodes.DomainError : EmbeddingMigrationExitCodes.Success;
        string message = tenantLevelError is not null
            ? $"Live migration aborted on tenant-level error: {tenantLevelError}"
            : manualFollowUp
                ? "Live migration completed staging with failed units; active indexes were not cut over. Rerun with --resume after resolving failures."
                : "Live blue/green migration completed successfully.";

        return new EmbeddingMigrationResult(
            EmbeddingMigrationMode.Live,
            exitCode,
            message,
            stopwatch.Elapsed,
            reports,
            failures,
            progress);
    }

    private async Task<EmbeddingMigrationResult> RollbackAsync(
        EmbeddingMigrationOptions options,
        TenantEmbeddingConfig targetConfig,
        Stopwatch stopwatch,
        List<EmbeddingMigrationTenantReport> reports,
        List<EmbeddingMigrationUnitFailure> failures,
        List<EmbeddingMigrationProgress> progress,
        CancellationToken ct)
    {
        string tenantId = options.TenantId!;
        try
        {
            EmbeddingMigrationLease lease = await store.StartMigrationMarkerAsync(
                tenantId,
                await store.GetEmbeddingConfigAsync(tenantId, ct).ConfigureAwait(false),
                targetConfig,
                options.OwnerId,
                options.MarkerLockTtl,
                resume: true,
                recoverStaleLock: options.RecoverStaleLock,
                ct).ConfigureAwait(false);
            await store.RollbackMigrationAsync(tenantId, targetConfig, lease, ct).ConfigureAwait(false);

            return new EmbeddingMigrationResult(
                EmbeddingMigrationMode.Rollback,
                EmbeddingMigrationExitCodes.Success,
                "Rollback restored the previous blue/green embedding search targets and tenant embedding configuration.",
                stopwatch.Elapsed,
                reports,
                failures,
                progress);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            string message = EmbeddingMigrationRedactor.Redact(ex.Message);
            await AddFailureAsync(tenantId, string.Empty, "tenant", NormalizeErrorCategory(ex), ex.Message, failures, ct).ConfigureAwait(false);

            return new EmbeddingMigrationResult(
                EmbeddingMigrationMode.Rollback,
                EmbeddingMigrationExitCodes.DomainError,
                $"Rollback failed closed: {message}",
                stopwatch.Elapsed,
                reports,
                failures,
                progress);
        }
    }

    private async Task<EmbeddingMigrationResult> AbortAsync(
        EmbeddingMigrationOptions options,
        TenantEmbeddingConfig targetConfig,
        Stopwatch stopwatch,
        List<EmbeddingMigrationTenantReport> reports,
        List<EmbeddingMigrationUnitFailure> failures,
        List<EmbeddingMigrationProgress> progress,
        CancellationToken ct)
    {
        string tenantId = options.TenantId!;
        try
        {
            EmbeddingMigrationLease lease = await store.StartMigrationMarkerAsync(
                tenantId,
                await store.GetEmbeddingConfigAsync(tenantId, ct).ConfigureAwait(false),
                targetConfig,
                options.OwnerId,
                options.MarkerLockTtl,
                resume: true,
                recoverStaleLock: true,
                ct).ConfigureAwait(false);
            await store.AbortMigrationAsync(tenantId, targetConfig, lease, ct).ConfigureAwait(false);

            return new EmbeddingMigrationResult(
                EmbeddingMigrationMode.Abort,
                EmbeddingMigrationExitCodes.Success,
                "Abort completed. Staging state was cleaned or previous active targets were restored when cutover had begun.",
                stopwatch.Elapsed,
                reports,
                failures,
                progress);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            string message = EmbeddingMigrationRedactor.Redact(ex.Message);
            await AddFailureAsync(tenantId, string.Empty, "tenant", NormalizeErrorCategory(ex), ex.Message, failures, ct).ConfigureAwait(false);

            return new EmbeddingMigrationResult(
                EmbeddingMigrationMode.Abort,
                EmbeddingMigrationExitCodes.DomainError,
                $"Abort failed closed: {message}",
                stopwatch.Elapsed,
                reports,
                failures,
                progress);
        }
    }

    private static EmbeddingMigrationResult CreateInvalidModeResult(
        EmbeddingMigrationOptions options,
        Stopwatch stopwatch,
        List<EmbeddingMigrationTenantReport> reports,
        List<EmbeddingMigrationUnitFailure> failures,
        List<EmbeddingMigrationProgress> progress)
        => new(
            options.Mode,
            EmbeddingMigrationExitCodes.Plumbing,
            "Unsupported migration mode.",
            stopwatch.Elapsed,
            reports,
            failures,
            progress);

    private async Task<IReadOnlyList<string>> ResolveTenantIdsAsync(string? tenantId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return [tenantId];
        }

        return await store.ListTenantIdsAsync(ct).ConfigureAwait(false);
    }

    private async Task<EmbeddingMigrationUnitCounters> MigrateRawAsync(
        EmbeddingMigrationOptions options,
        TenantEmbeddingConfig targetConfig,
        string tenantId,
        Stopwatch stopwatch,
        List<EmbeddingMigrationUnitFailure> failures,
        List<EmbeddingMigrationProgress> progress,
        CancellationToken ct)
    {
        EmbeddingMigrationUnitCounters counters = EmbeddingMigrationUnitCounters.Empty;
        int batchNumber = 0;
        await foreach (SyntacticMigrationUnit unit in store.EnumerateSyntacticUnitsAsync(tenantId, options.BatchSize, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                SemanticMigrationState? state = await store.GetRawSemanticStateAsync(tenantId, unit.MemoryUnitId, ct).ConfigureAwait(false);
                if (IsTargetState(state, targetConfig))
                {
                    counters = counters.AddSkipped();
                }
                else if (string.IsNullOrWhiteSpace(unit.Content) || string.IsNullOrWhiteSpace(unit.CaseId))
                {
                    counters = counters.AddMissing();
                    await AddFailureAsync(tenantId, unit.MemoryUnitId, RawContentKind, "Validation", "Syntactic hash is missing content or caseId.", failures, ct).ConfigureAwait(false);
                }
                else
                {
                    float[] vector = await vectorGenerator.GenerateAsync(unit.Content, tenantId, targetConfig, ct).ConfigureAwait(false);
                    EnsureVectorDimensions(vector, targetConfig, unit.MemoryUnitId);
                    await store.WriteRawSemanticAsync(
                        tenantId,
                        targetConfig,
                        new RawSemanticMigrationWrite(unit.MemoryUnitId, unit.CaseId, unit.CloudEventSubject, vector),
                        ct).ConfigureAwait(false);
                    counters = counters.AddProcessed();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                counters = counters.AddFailed();
                await AddFailureAsync(tenantId, unit.MemoryUnitId, RawContentKind, NormalizeErrorCategory(ex), ex.Message, failures, ct).ConfigureAwait(false);
            }

            if (counters.Completed > 0 && counters.Completed % options.BatchSize == 0)
            {
                batchNumber++;
                await ReportProgressAsync(options, tenantId, RawContentKind, batchNumber, counters, counters.Completed, stopwatch, progress, ct).ConfigureAwait(false);
            }
        }

        if (counters.Completed > 0 && counters.Completed % options.BatchSize != 0)
        {
            batchNumber++;
            await ReportProgressAsync(options, tenantId, RawContentKind, batchNumber, counters, counters.Completed, stopwatch, progress, ct).ConfigureAwait(false);
        }

        return counters;
    }

    private async Task<EmbeddingMigrationUnitCounters> MigrateNaturalLanguageAsync(
        EmbeddingMigrationOptions options,
        TenantEmbeddingConfig targetConfig,
        string tenantId,
        Stopwatch stopwatch,
        List<EmbeddingMigrationUnitFailure> failures,
        List<EmbeddingMigrationProgress> progress,
        CancellationToken ct)
    {
        EmbeddingMigrationUnitCounters counters = EmbeddingMigrationUnitCounters.Empty;
        int batchNumber = 0;
        await foreach (SyntacticMigrationUnit unit in store.EnumerateSyntacticUnitsAsync(tenantId, options.BatchSize, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                NaturalLanguageMigrationUnit? nl = await store.GetNaturalLanguageSemanticUnitAsync(tenantId, unit.MemoryUnitId, ct).ConfigureAwait(false);
                if (nl is null || string.IsNullOrWhiteSpace(nl.NaturalLanguageDescription))
                {
                    counters = counters.AddMissing();
                }
                else if (IsTargetState(nl.State, targetConfig))
                {
                    counters = counters.AddSkipped();
                }
                else if (string.IsNullOrWhiteSpace(nl.CaseId))
                {
                    counters = counters.AddFailed();
                    await AddFailureAsync(tenantId, unit.MemoryUnitId, NaturalLanguageContentKind, "Validation", "NL semantic hash is missing caseId.", failures, ct).ConfigureAwait(false);
                }
                else
                {
                    float[] vector = await vectorGenerator.GenerateAsync(nl.NaturalLanguageDescription, tenantId, targetConfig, ct).ConfigureAwait(false);
                    EnsureVectorDimensions(vector, targetConfig, unit.MemoryUnitId);
                    await store.WriteNaturalLanguageSemanticAsync(
                        tenantId,
                        targetConfig,
                        new NaturalLanguageSemanticMigrationWrite(
                            unit.MemoryUnitId,
                            nl.CaseId,
                            nl.NaturalLanguageDescription,
                            nl.DescriptionOrigin,
                            nl.DescriptionConfidence,
                            nl.DescriptionConfidenceSource,
                            vector),
                        ct).ConfigureAwait(false);
                    counters = counters.AddProcessed();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                counters = counters.AddFailed();
                await AddFailureAsync(tenantId, unit.MemoryUnitId, NaturalLanguageContentKind, NormalizeErrorCategory(ex), ex.Message, failures, ct).ConfigureAwait(false);
            }

            if (counters.Completed > 0 && counters.Completed % options.BatchSize == 0)
            {
                batchNumber++;
                await ReportProgressAsync(options, tenantId, NaturalLanguageContentKind, batchNumber, counters, counters.Completed, stopwatch, progress, ct).ConfigureAwait(false);
            }
        }

        if (counters.Completed > 0 && counters.Completed % options.BatchSize != 0)
        {
            batchNumber++;
            await ReportProgressAsync(options, tenantId, NaturalLanguageContentKind, batchNumber, counters, counters.Completed, stopwatch, progress, ct).ConfigureAwait(false);
        }

        return counters;
    }

    private static string NormalizeErrorCategory(Exception ex) => ex switch
    {
        ArgumentException => "Validation",
        InvalidOperationException => "InvalidOperation",
        TimeoutException => "Timeout",
        HttpRequestException => "Transport",
        _ => "ProviderOrInfrastructure",
    };

    private async Task AddFailureAsync(
        string tenantId,
        string memoryUnitId,
        string contentKind,
        string category,
        string message,
        List<EmbeddingMigrationUnitFailure> failures,
        CancellationToken ct)
    {
        EmbeddingMigrationUnitFailure failure = new(
            tenantId,
            memoryUnitId,
            contentKind,
            category,
            EmbeddingMigrationRedactor.Redact(message));
        failures.Add(failure);
        await store.RecordFailureAsync(failure, ct).ConfigureAwait(false);
    }

    private static async Task ReportProgressAsync(
        EmbeddingMigrationOptions options,
        string tenantId,
        string contentKind,
        int batchNumber,
        EmbeddingMigrationUnitCounters counters,
        int total,
        Stopwatch stopwatch,
        List<EmbeddingMigrationProgress> progress,
        CancellationToken ct)
    {
        double percent = total <= 0 ? 100 : Math.Round((counters.Completed * 100d) / total, 2);
        EmbeddingMigrationProgress item = new(
            tenantId,
            contentKind,
            batchNumber,
            counters.Processed,
            counters.Skipped,
            counters.Missing,
            counters.Failed,
            total,
            percent,
            stopwatch.Elapsed);
        progress.Add(item);

        if (options.ProgressHandler is not null)
        {
            await options.ProgressHandler(item, ct).ConfigureAwait(false);
        }
    }

    private static bool ConfigDiffers(TenantEmbeddingConfig currentConfig, TenantEmbeddingConfig targetConfig)
        => !string.Equals(currentConfig.Provider, targetConfig.Provider, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(currentConfig.Model, targetConfig.Model, StringComparison.OrdinalIgnoreCase)
            || currentConfig.Dimensions != targetConfig.Dimensions;

    private static bool IsDimensionMismatch(EmbeddingMigrationIndexInfo info, int targetDimensions)
        => (info.RawSemanticDimensions is not null && info.RawSemanticDimensions != targetDimensions)
            || (info.NaturalLanguageSemanticDimensions is not null && info.NaturalLanguageSemanticDimensions != targetDimensions);

    private static bool IsTargetState(SemanticMigrationState? state, TenantEmbeddingConfig targetConfig)
        => state is not null
            && !string.IsNullOrEmpty(state.Provider)
            && !string.IsNullOrEmpty(state.Model)
            && state.Dimensions is not null
            && string.Equals(state.Provider, targetConfig.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(state.Model, targetConfig.Model, StringComparison.OrdinalIgnoreCase)
            && state.Dimensions == targetConfig.Dimensions;

    private static void EnsureVectorDimensions(float[] vector, TenantEmbeddingConfig targetConfig, string memoryUnitId)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Length != targetConfig.Dimensions)
        {
            throw new InvalidOperationException(
                $"Generated embedding for memory unit '{memoryUnitId}' returned {vector.Length} dimensions, expected {targetConfig.Dimensions}.");
        }
    }
}
