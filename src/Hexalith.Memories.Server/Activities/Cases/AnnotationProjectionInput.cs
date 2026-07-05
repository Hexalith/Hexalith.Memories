// <copyright file="AnnotationProjectionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Hexalith.Memories.Contracts.V1;

/// <summary>Input for annotation stub projection and ingestion scheduling.</summary>
internal sealed record AnnotationProjectionInput(
    string TenantId,
    string CaseId,
    string AnnotationMemoryUnitId,
    string TargetMemoryUnitId,
    string SourceUri,
    string Content,
    string? AnnotationType,
    string IngestedBy,
    IReadOnlyDictionary<string, MetadataField> Metadata,
    IngestionWorkflowConfiguration? WorkflowConfiguration = null,
    WorkflowTraceContext? TraceContext = null) : IWorkflowTraceContextCarrier;
