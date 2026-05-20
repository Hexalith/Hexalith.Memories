// <copyright file="EvidenceDisplay.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Evidence;

using System.Globalization;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Memories.Contracts.V1;

internal static partial class EvidenceDisplay
{
    public static string Label(Enum value)
        => string.Concat(value.ToString().Select((c, index) =>
            index > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    public static string SourceCountLabel(int count)
        => count == 1 ? "1 source" : string.Create(CultureInfo.InvariantCulture, $"{count} sources");

    public static string TokenBudgetLabel(EvidencePacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        return packet.OmittedDetails.Reason is EvidencePacketOmissionReason.TokenBudget or EvidencePacketOmissionReason.Combined
            || packet.OmittedDetails.OmittedCount > 0
            ? "compressed"
            : "within budget";
    }

    public static string FreshnessLabel() => "unknown";

    public static BadgeSlot SlotForState(EvidencePacketState state)
        => state switch
        {
            EvidencePacketState.Complete => BadgeSlot.Success,
            EvidencePacketState.Partial or EvidencePacketState.PendingExpansion or EvidencePacketState.Weak => BadgeSlot.Warning,
            EvidencePacketState.Empty or EvidencePacketState.Stale or EvidencePacketState.Degraded => BadgeSlot.Warning,
            EvidencePacketState.Unauthorized => BadgeSlot.Danger,
            _ => BadgeSlot.Neutral,
        };

    public static BadgeSlot SlotForIsolation(EvidencePacketIsolationStatus status)
        => status switch
        {
            EvidencePacketIsolationStatus.Authorized => BadgeSlot.Success,
            EvidencePacketIsolationStatus.Unauthorized => BadgeSlot.Danger,
            _ => BadgeSlot.Warning,
        };

    public static string SafeText(string? value, string fallback = "unavailable")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return SensitiveTextRegex().IsMatch(value) ? "redacted source" : value;
    }

    public static string ScoreLabel(double? score)
        => score.HasValue ? score.Value.ToString("0.###", CultureInfo.InvariantCulture) : "score unavailable";

    [GeneratedRegex("(bearer\\s+\\S+|redis://\\S+|falkor\\S*|[A-Za-z]:\\\\|/home/|/users/|stack\\s*trace|\\bat\\s+\\w+\\.|eyJ[A-Za-z0-9_/+=-]+\\.|\\b[a-f0-9]{32,}\\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveTextRegex();
}
