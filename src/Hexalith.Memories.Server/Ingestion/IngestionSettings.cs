// <copyright file="IngestionSettings.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Ingestion-wide settings bound from the "Ingestion" configuration section.</summary>
public sealed class IngestionSettings
{
    /// <summary>Gets or sets the allow-list of absolute directory roots that POST /api/v1/ingest/directory may traverse. Empty by default — endpoint is disabled until an operator opts in.</summary>
    public string[] AllowedDirectoryRoots { get; set; } = [];

    /// <summary>Gets or sets the maximum number of candidate files per directory batch.</summary>
    public int MaxBatchSize { get; set; } = 500;

    /// <summary>Gets or sets the maximum number of skipped-file entries returned in the response before truncation.</summary>
    public int MaxSkippedReportSize { get; set; } = 100;

    /// <summary>Gets or sets the TTL (in hours) for persisted batch state records.</summary>
    public int BatchStateTtlHours { get; set; } = 24;

    /// <summary>Gets or sets the maximum number of directory files scheduled concurrently.</summary>
    public int DirectorySchedulingParallelism { get; set; } = 4;

    /// <summary>Gets or sets the number of scheduled directory files between persisted progress checkpoints.</summary>
    public int DirectoryBatchCheckpointSize { get; set; } = 50;

    /// <summary>Gets or sets the list of extensions that are always enqueued (lowercase with leading dot).</summary>
    public string[] SupportedExtensions { get; set; } =
    [
        ".md",
        ".txt",
        ".pdf",
        ".docx",
        ".doc",
        ".html",
        ".htm",
        ".xlsx",
        ".xls",
        ".pptx",
        ".ppt",
        ".csv",
        ".json",
        ".rtf",
        ".epub",
    ];

    /// <summary>Gets or sets the maximum number of concurrent <c>ExtractContentActivity</c> + <c>FetchUrlActivity</c>
    /// invocations allowed per tenant (Story 6.2 <see cref="PerTenantConcurrencyGate"/>). Defaults to 4 — leaves
    /// headroom for other tenants and system work on an 8-core box. Raise only after confirming CPU headroom
    /// and <c>ExtractionGateContended</c> (event 6205) firing.</summary>
    public int PerTenantExtractionConcurrency { get; set; } = 4;

    /// <summary>Gets or sets the maximum time (in seconds) <see cref="PerTenantConcurrencyGate"/> waits to acquire
    /// a slot before timing out (Story 6.2). Defaults to 300 (5 min) — long enough to ride through normal batch
    /// queuing, short enough that a stuck gate surfaces as an <c>ExtractionGateTimeout</c> (event 6206) rather
    /// than hanging the workflow indefinitely.</summary>
    public int ExtractionGateAcquireTimeoutSeconds { get; set; } = 300;

    /// <summary>Gets or sets the per-activity retry policy overrides (Story 6.3 FR9). Keys are activity class names
    /// (e.g., <c>"GenerateEmbeddingActivity"</c>); missing entries fall back to the default policy
    /// <c>(MaxAttempts=5, FirstRetryIntervalSeconds=2, BackoffCoefficient=1.5, MaxRetryIntervalSeconds=300)</c>.</summary>
    public Dictionary<string, ActivityRetryPolicy> RetryPolicies { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets the list of extensions that are always skipped as UNSUPPORTED_EXTENSION (lowercase with leading dot).</summary>
    public string[] UnsupportedExtensions { get; set; } =
    [
        ".exe",
        ".dll",
        ".bin",
        ".iso",
        ".dmg",
        ".so",
        ".dylib",
        ".app",
        ".msi",
        ".deb",
        ".rpm",
    ];
}
