// <copyright file="AnnotationRequestedEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Events;


/// <summary>Annotation ingestion intent event.</summary>
public sealed record AnnotationRequestedEvent(
    string TenantId,
    string CaseId,
    string AnnotationMemoryUnitId,
    string TargetMemoryUnitId,
    string SourceUri,
    string Content,
    string? AnnotationType,
    string IngestedBy,
    DateTimeOffset RequestedAt) : IMemoriesEventPayload;
