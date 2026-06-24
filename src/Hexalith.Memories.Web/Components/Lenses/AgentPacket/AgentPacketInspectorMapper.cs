// <copyright file="AgentPacketInspectorMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.AgentPacket;

using System.Globalization;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Pure, deterministic projection of a canonical Evidence Packet into the Agent Packet Inspector (AC5).
/// </summary>
/// <remarks>
/// Story 17.4 — request summary, token budget, omitted fields, expansion handles, and structured errors all
/// come from named contract fields and the shared recovery grammar. The readable schema is the primary
/// path; the copy payload and the secondary JSON view share the single sanitized
/// <see cref="AgentPacketInspectorViewModel.SafeCopyText"/> so they can never diverge or leak. The raw
/// serialized packet is never reconstructed. The MCP tool/resource name is not exposed by the canonical
/// contract and renders an unavailable boundary.
/// </remarks>
public static class AgentPacketInspectorMapper
{
    private const int MaxCopyLength = 2000;
    private const string Unavailable = "unavailable";

    /// <summary>Maps a packet into the Agent Packet Inspector view model.</summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <param name="role">The active role-density profile (affects the shared shell only).</param>
    /// <returns>The typed, sanitized agent packet inspector.</returns>
    public static AgentPacketInspectorViewModel Map(EvidencePacket packet, LensRole role)
    {
        ArgumentNullException.ThrowIfNull(packet);
        _ = role;

        RecoveryStateViewModel recovery = RecoveryStateMapper.Map(packet);
        bool restrictive = EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized;

        string safeQuery = EvidenceDisplay.SafeText(packet.Result.Query, "no query");

        // Counts can leak whether matching evidence exists, so they are suppressed under a restrictive scope.
        string safeCounts = restrictive
            ? Unavailable
            : string.Create(CultureInfo.InvariantCulture, $"{packet.Result.ReturnedCount}/{packet.Result.TotalCount}");

        bool compressed = packet.OmittedDetails.Reason
            is EvidencePacketOmissionReason.TokenBudget or EvidencePacketOmissionReason.Combined;
        string safeTokenBudget = restrictive
            ? Unavailable
            : string.Create(CultureInfo.InvariantCulture, $"{packet.OmittedDetails.EstimatedTokensTotal} tokens");

        IReadOnlyList<PacketSchemaField> fields = BuildSchemaFields(packet, safeQuery, safeCounts, safeTokenBudget, restrictive);
        string copyText = BuildCopyText(fields, recovery);

        return new AgentPacketInspectorViewModel(
            SafeQuery: safeQuery,
            SafeCounts: safeCounts,
            CountsAvailability: restrictive ? LensFieldAvailability.Unauthorized : LensFieldAvailability.Available,
            SafeTokenBudget: safeTokenBudget,
            TokenBudgetStateKey: compressed
                ? AgentPacketResourceKeys.TokenBudgetCompressed
                : AgentPacketResourceKeys.TokenBudgetWithin,
            TokenBudgetAvailability: restrictive ? LensFieldAvailability.Unauthorized : LensFieldAvailability.Available,
            SchemaFields: fields,
            OmittedFieldNames: recovery.OmittedDetailNames,
            Expansions: recovery.Expansions,
            HasError: recovery.StateKind != RecoveryStateKind.Supported,
            ErrorStateKey: recovery.TitleKey,
            Severity: recovery.Severity,
            SafeDiagnosticCode: recovery.DiagnosticClueCode,
            ToolNameAvailability: LensFieldAvailability.Unavailable,
            SafeCopyText: copyText,
            Restrictive: restrictive);
    }

    private static IReadOnlyList<PacketSchemaField> BuildSchemaFields(
        EvidencePacket packet,
        string safeQuery,
        string safeCounts,
        string safeTokenBudget,
        bool restrictive)
    {
        // Fields that could reveal evidence existence are suppressed to an unauthorized boundary under a
        // restrictive scope; scope/isolation/state stay visible because they are the point of the inspection.
        LensFieldAvailability gated = restrictive ? LensFieldAvailability.Unauthorized : LensFieldAvailability.Available;

        return
        [
            Field(PacketSchemaFieldKind.ScopeTenant, LensFieldAvailability.Available, EvidenceDisplay.SafeText(packet.Scope.TenantId, "unknown tenant")),
            Field(
                PacketSchemaFieldKind.ScopeCase,
                string.IsNullOrWhiteSpace(packet.Scope.CaseId) ? LensFieldAvailability.Unavailable : LensFieldAvailability.Available,
                string.IsNullOrWhiteSpace(packet.Scope.CaseId) ? "tenant scope" : EvidenceDisplay.SafeText(packet.Scope.CaseId, "tenant scope")),
            Field(PacketSchemaFieldKind.ScopeIsolation, LensFieldAvailability.Available, EvidenceDisplay.Label(packet.Scope.IsolationStatus)),
            Field(PacketSchemaFieldKind.ResultQuery, LensFieldAvailability.Available, safeQuery),
            Field(PacketSchemaFieldKind.ResultCounts, gated, restrictive ? Unavailable : safeCounts),
            Field(PacketSchemaFieldKind.EvidenceStrength, gated, restrictive ? Unavailable : EvidenceDisplay.Label(packet.Evidence.EvidenceStrength)),
            Field(PacketSchemaFieldKind.EvidenceAxes, gated, restrictive ? Unavailable : string.Create(CultureInfo.InvariantCulture, $"{packet.Evidence.AxesUsed.Count}")),
            Field(PacketSchemaFieldKind.State, LensFieldAvailability.Available, EvidenceDisplay.Label(packet.State)),
            Field(PacketSchemaFieldKind.OmissionReason, LensFieldAvailability.Available, EvidenceDisplay.Label(packet.OmittedDetails.Reason)),
            Field(PacketSchemaFieldKind.TokenBudget, gated, restrictive ? Unavailable : safeTokenBudget),

            // The MCP tool/resource name is not exposed by the canonical contract — deferred to Story 2.7.
            Field(PacketSchemaFieldKind.ToolName, LensFieldAvailability.Unavailable, Unavailable),
        ];
    }

    private static PacketSchemaField Field(PacketSchemaFieldKind kind, LensFieldAvailability availability, string safeValue)
        => new(kind, AgentPacketResourceKeys.Field(kind), availability, safeValue);

    private static string BuildCopyText(IReadOnlyList<PacketSchemaField> fields, RecoveryStateViewModel recovery)
    {
        // One sanitized payload, assembled only from already-sanitized field values and the whitelisted
        // diagnostic clue — never from a raw packet serialization, DOM text, or backend diagnostics.
        StringBuilder builder = new();
        foreach (PacketSchemaField field in fields)
        {
            builder.Append(field.Kind).Append('=').Append(field.SafeValue).Append('\n');
        }

        builder.Append("diagnostic=").Append(recovery.DiagnosticClueCode);

        // Defense in depth: pass the assembled payload back through the shared sanitizer and bound its size.
        string safe = EvidenceDisplay.SafeText(builder.ToString(), "packet summary unavailable");
        return safe.Length > MaxCopyLength ? safe[..MaxCopyLength] : safe;
    }
}
