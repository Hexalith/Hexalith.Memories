// <copyright file="IngestionLifecycleViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Ingestion;

using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Typed, pure projection of an Evidence Packet into the Ingestion Lifecycle Tracker lens (AC2).
/// </summary>
/// <remarks>
/// Story 17.4 — produced by <see cref="IngestionLifecycleMapper.Map"/>. The fine-grained ingestion stage
/// taxonomy is not exposed by the canonical contract, so <see cref="StageTaxonomyAvailable"/> is false and
/// the component shows the stage-unavailable note. The most severe row drives the live-region politeness so
/// only meaningful transitions (failure, degraded backend) announce assertively.
/// </remarks>
/// <param name="Units">The sanitized per-unit ingestion rows.</param>
/// <param name="StageTaxonomyAvailable">Whether the contract exposes ingestion stages (currently false).</param>
/// <param name="StageNoteKey">Localization key for the stage-unavailable note.</param>
/// <param name="HighestSeverity">The highest row severity, used for live-region politeness.</param>
/// <param name="IsEmpty">Whether the tracker has no ingestion units to show.</param>
/// <param name="EmptyReasonKey">Localization key shown when the tracker is empty.</param>
public sealed record IngestionLifecycleViewModel(
    IReadOnlyList<IngestionUnitRow> Units,
    bool StageTaxonomyAvailable,
    string StageNoteKey,
    RecoverySeverity HighestSeverity,
    bool IsEmpty,
    string EmptyReasonKey);
