// <copyright file="InteractionTraceability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Story 17.3 traceability table for every implemented interaction family.
/// </summary>
/// <remarks>
/// The table is intentionally code-owned so tests can fail when a family loses its named contract,
/// FrontComposer, authorization, localization, or unavailable-fallback source.
/// </remarks>
public static class InteractionTraceability
{
    /// <summary>Gets all interaction traceability rows.</summary>
    public static IReadOnlyList<InteractionTrace> Entries { get; } =
    [
        Row(
            InteractionFamily.Forms,
            ["EvidencePacket.Scope", "EvidencePacket.IsolationStatus"],
            ["FrontComposer command lifecycle", "Fluent input primitives"],
            "EvidencePacket.Scope.IsolationStatus",
            ["Form_*", "Interaction_Severity_*"],
            "Block dispatch with a field-associated validation message."),
        Row(
            InteractionFamily.Filters,
            ["EvidencePacket.Evidence", "EvidencePacket.Sources", "EvidencePacket.Graph", "EvidencePacket.State"],
            ["FcFilterSummary", "FcStatusFilterChips", "FcColumnFilterCell", "FcFilterResetButton", "DataGridNavigationState"],
            "EvidencePacket.Scope.IsolationStatus",
            ["Filter_*", "Interaction_Severity_*"],
            "Render an unavailable contract-boundary chip or safe empty reason."),
        Row(
            InteractionFamily.Navigation,
            ["EvidencePacket.Scope", "EvidencePacket.Result.Query", "EvidencePacket.Sources", "EvidencePacket.Graph"],
            ["FrontComposer State/Navigation", "SessionRouteHelper"],
            "InteractionContextValidator",
            ["Interaction_*"],
            "Disable stale target and keep a generic return path."),
        Row(
            InteractionFamily.Overlays,
            ["EvidencePacket.Scope", "EvidencePacket.Sources", "EvidencePacket.Graph", "EvidencePacket.OmittedDetails"],
            ["Fluent panels/dialogs", "FrontComposer navigation state"],
            "InteractionContextValidator",
            ["Interaction_*"],
            "Render a disabled overlay summary with a localized reason."),
        Row(
            InteractionFamily.Confirmations,
            ["EvidencePacket.Scope", "EvidencePacket.Recovery", "EvidencePacket.OmittedDetails"],
            ["FcDestructiveConfirmationDialog"],
            "InteractionContextValidator",
            ["Confirmation_*", "Command_*"],
            "Require cancellation-safe confirmation or disable the action."),
        Row(
            InteractionFamily.Commands,
            ["EvidencePacket.Scope", "EvidencePacket.Sources", "EvidencePacket.Graph", "EvidencePacket.Recovery"],
            ["FcCommandPalette", "PaletteResult", "CommandPaletteEffects"],
            "InteractionContextValidator",
            ["Command_*", "Interaction_*"],
            "Render command disabled with a bounded reason."),
        Row(
            InteractionFamily.Grids,
            ["EvidencePacket.Sources", "EvidencePacket.Scope", "EvidencePacket.Evidence", "EvidencePacket.State"],
            ["FluentDataGrid", "DataGridNavigationState", "FcFilterSummary"],
            "EvidencePacket.Scope.IsolationStatus",
            ["Grid_*", "Filter_*"],
            "Suppress unsafe rows and keep trust-critical fields visible or reachable."),
    ];

    /// <summary>Gets a traceability row for one family.</summary>
    /// <param name="family">The interaction family.</param>
    /// <returns>The traceability row.</returns>
    public static InteractionTrace For(InteractionFamily family)
        => Entries.Single(e => e.Family == family);

    private static InteractionTrace Row(
        InteractionFamily family,
        IReadOnlyList<string> contractSources,
        IReadOnlyList<string> frontComposerSources,
        string authorizationSource,
        IReadOnlyList<string> resourceKeys,
        string unavailableFallback)
        => new(family, contractSources, frontComposerSources, authorizationSource, resourceKeys, unavailableFallback);
}
