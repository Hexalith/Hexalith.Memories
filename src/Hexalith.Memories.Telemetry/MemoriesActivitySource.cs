// <copyright file="MemoriesActivitySource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Telemetry;

using System.Diagnostics;

/// <summary>
/// Provides a single static <see cref="ActivitySource"/> for OpenTelemetry distributed tracing across the
/// Memories pipeline (search, ingest, traverse, case-access, and CLI-originated invocations).
/// Mirrors the EventStore submodule's <c>EventStoreActivitySource</c> const-heavy pattern — operators
/// see the same shape on both submodules and tag-key drift is prevented by compile-time constants.
/// </summary>
public static class MemoriesActivitySource
{
    /// <summary>The source name registered with OpenTelemetry.</summary>
    public const string SourceName = "Hexalith.Memories";

    /// <summary>Search endpoint activity name.</summary>
    public const string SearchRequest = "memories.search";

    /// <summary>Ingest endpoint activity name (covers file, URL, and directory ingest variants).</summary>
    public const string IngestRequest = "memories.ingest";

    /// <summary>Traverse endpoint activity name.</summary>
    public const string TraverseRequest = "memories.traverse";

    /// <summary>Case-access (memory-unit read) endpoint activity name.</summary>
    public const string CaseAccess = "memories.case-access";

    /// <summary>Delete operation activity name.</summary>
    public const string DeleteRequest = "memories.delete";

    /// <summary>Optional child span wrapping the audit log write.</summary>
    public const string AuditEmit = "memories.audit.emit";

    /// <summary>CLI root activity name for opt-in telemetry.</summary>
    public const string CliInvoke = "memories.cli.invoke";

    /// <summary>Story 9.2 / Risk #2 — span wrapping a single LLM call for natural-language description
    /// generation. Emitted from <c>GenerateNaturalLanguageDescriptionActivity</c>. Carries
    /// <see cref="TagTenantId"/>, <see cref="TagMemoryUnitId"/>, and outcome attributes so operators
    /// can attribute LLM latency and failure rates per tenant in distributed traces.</summary>
    public const string NaturalLanguageDescriptionGeneration = "memories.natural_language.description";

    /// <summary>Tag key for tenant id.</summary>
    public const string TagTenantId = "memories.tenant_id";

    /// <summary>Tag key for case id.</summary>
    public const string TagCaseId = "memories.case_id";

    /// <summary>Tag key for memory-unit id.</summary>
    public const string TagMemoryUnitId = "memories.memory_unit_id";

    /// <summary>Tag key for the operation type (search | ingest | traverse | case-access | delete).</summary>
    public const string TagOperation = "memories.operation";

    /// <summary>Tag key for the resolved search axis.</summary>
    public const string TagAxis = "memories.axis";

    /// <summary>Tag key for ingest source type.</summary>
    public const string TagSourceType = "memories.source_type";

    /// <summary>Tag key for the completion outcome (ok | partial | error).</summary>
    public const string TagOutcome = "memories.outcome";

    /// <summary>Tag key for the error code when outcome is error.</summary>
    public const string TagErrorCode = "memories.error_code";

    /// <summary>Tag key for the CLI command name on CLI-root spans.</summary>
    public const string TagCommand = "memories.command";

    /// <summary>Tag key for the wizard-invocation flag on quickstart-originated spans.</summary>
    public const string TagWizardOrigin = "memories.wizard_origin";

    /// <summary>Gets the singleton <see cref="ActivitySource"/> instance.</summary>
    public static ActivitySource Instance { get; } = new(SourceName);
}
