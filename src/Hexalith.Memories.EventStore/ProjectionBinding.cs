// <copyright file="ProjectionBinding.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Runtime projection binding metadata used to cross-check configured EventStore routes.</summary>
/// <param name="TenantId">Tenant boundary for the binding.</param>
/// <param name="SourcePrefix">Optional route source prefix covered by the projection.</param>
/// <param name="AggregateType">Optional aggregate token covered by the projection.</param>
/// <param name="ProjectionName">Sanitized operator-facing projection name or identifier.</param>
/// <param name="ProjectionType">Sanitized projection type name, without DI internals or endpoint details.</param>
/// <param name="SupportedEventTypePatterns">Supported event-name patterns. Empty or <c>*</c> means all events for the aggregate/source.</param>
public sealed record ProjectionBinding(
    string TenantId,
    string? SourcePrefix,
    string? AggregateType,
    string? ProjectionName,
    string? ProjectionType,
    IReadOnlyList<string> SupportedEventTypePatterns);
