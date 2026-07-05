// <copyright file="AgentPacketInspectorViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.AgentPacket;

using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Typed, pure projection of an Evidence Packet into the Agent Packet Inspector lens (AC5).
/// </summary>
/// <remarks>
/// Story 17.4 — produced by <see cref="AgentPacketInspectorMapper.Map"/>. The readable schema view is the
/// primary inspection path; <see cref="SafeCopyText"/> is the single sanitized payload shared by the copy
/// control and the secondary JSON view, so visible text, copied text, and the JSON view can never diverge
/// or leak. A user never needs to read raw JSON to learn whether the packet is valid, compressed, failed,
/// expandable, or tied to MCP schema metadata.
/// </remarks>
/// <param name="SafeQuery">Sanitized request query.</param>
/// <param name="SafeCounts">Sanitized returned/total counts, or the unavailable fallback under a restrictive scope.</param>
/// <param name="CountsAvailability">Availability of the result counts.</param>
/// <param name="SafeTokenBudget">Sanitized estimated token budget text.</param>
/// <param name="TokenBudgetStateKey">Localization key for the compressed / within-budget token state.</param>
/// <param name="TokenBudgetAvailability">Availability of the token budget.</param>
/// <param name="SchemaFields">The readable, sanitized schema fields (primary inspection path).</param>
/// <param name="OmittedFieldNames">Sanitized omitted field/detail-group names (empty under a restrictive scope).</param>
/// <param name="Expansions">Sanitized expansion handles (empty under a restrictive scope).</param>
/// <param name="HasError">Whether the packet represents a structured error/non-supported state.</param>
/// <param name="ErrorStateKey">Localization key for the structured error/state title.</param>
/// <param name="Severity">Severity of the packet state.</param>
/// <param name="SafeDiagnosticCode">Sanitized, whitelisted diagnostic clue.</param>
/// <param name="ToolNameAvailability">Availability of the MCP tool/resource name.</param>
/// <param name="SafeCopyText">The single sanitized, bounded payload shared by the copy control and JSON view.</param>
/// <param name="Restrictive">Whether the scope is restrictive.</param>
public sealed record AgentPacketInspectorViewModel(
    string SafeQuery,
    string SafeCounts,
    LensFieldAvailability CountsAvailability,
    string SafeTokenBudget,
    string TokenBudgetStateKey,
    LensFieldAvailability TokenBudgetAvailability,
    IReadOnlyList<PacketSchemaField> SchemaFields,
    IReadOnlyList<string> OmittedFieldNames,
    IReadOnlyList<RecoveryExpansionView> Expansions,
    bool HasError,
    string ErrorStateKey,
    RecoverySeverity Severity,
    string SafeDiagnosticCode,
    LensFieldAvailability ToolNameAvailability,
    string SafeCopyText,
    bool Restrictive);
