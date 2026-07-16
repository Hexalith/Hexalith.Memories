// <copyright file="EvidenceDisplay.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Evidence;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Resources;

using Microsoft.Extensions.Localization;

internal static partial class EvidenceDisplay
{
    // Fallback for packet producers that have not yet attached optional Story 2.7 freshness metadata.
    public const string FreshnessUnavailable = "Unavailable";

    public const string RedactedMarker = "[REDACTED]";

    public static string Label(Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string raw = value.ToString();
        if (raw.Length == 0)
        {
            return raw;
        }

        StringBuilder buffer = new(raw.Length + 4);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (i > 0 && char.IsUpper(c) && !IsContinuingAcronym(raw, i))
            {
                buffer.Append(' ').Append(char.ToLowerInvariant(c));
            }
            else
            {
                buffer.Append(c);
            }
        }

        return buffer.ToString();
    }

    public static string Label(Enum value, IStringLocalizer<MemoriesWebResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(localizer);

        string key = value switch
        {
            EvidencePacketState state => EvidenceResourceKeys.State(state),
            EvidencePacketEvidenceStrength strength => EvidenceResourceKeys.Strength(strength),
            EvidencePacketIsolationStatus isolation => EvidenceResourceKeys.Isolation(isolation),
            EvidencePacketFreshnessState freshness => EvidenceResourceKeys.Freshness(freshness),
            SourceType sourceType => EvidenceResourceKeys.SourceType(sourceType),
            _ => EvidenceResourceKeys.Unavailable,
        };
        LocalizedString localized = localizer[key];
        return localized.ResourceNotFound
            ? localizer[EvidenceResourceKeys.Unavailable]
            : localized;
    }

    public static string SourceCountLabel(int count, bool sourcesAvailable)
    {
        if (!sourcesAvailable)
        {
            return "sources unavailable";
        }

        return count == 1 ? "1 source" : string.Create(CultureInfo.InvariantCulture, $"{count} sources");
    }

    public static string SourceCountLabel(
        int count,
        bool sourcesAvailable,
        IStringLocalizer<MemoriesWebResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return !sourcesAvailable
            ? localizer[EvidenceResourceKeys.SourceCountUnavailable]
            : count == 1
                ? localizer[EvidenceResourceKeys.SourceCountOne]
                : localizer[EvidenceResourceKeys.SourceCountMany, count];
    }

    public static string TokenBudgetLabel(EvidencePacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        return packet.OmittedDetails.Reason is EvidencePacketOmissionReason.TokenBudget or EvidencePacketOmissionReason.Combined
            ? "compressed"
            : "within budget";
    }

    public static string TokenBudgetLabel(
        EvidencePacket packet,
        IStringLocalizer<MemoriesWebResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(localizer);

        string key = packet.OmittedDetails.Reason is EvidencePacketOmissionReason.TokenBudget or EvidencePacketOmissionReason.Combined
            ? EvidenceResourceKeys.TokenBudgetCompressed
            : EvidenceResourceKeys.TokenBudgetWithin;
        return localizer[key];
    }

    public static string FreshnessLabel(EvidencePacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        EvidencePacketFreshness? freshness = packet.Metadata?.Freshness
            ?? packet.Sources.FirstOrDefault(static source => source.Freshness is not null)?.Freshness;
        return FreshnessLabel(freshness);
    }

    public static string FreshnessLabel(
        EvidencePacket packet,
        IStringLocalizer<MemoriesWebResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(packet);

        EvidencePacketFreshness? freshness = packet.Metadata?.Freshness
            ?? packet.Sources.FirstOrDefault(static source => source.Freshness is not null)?.Freshness;
        return FreshnessLabel(freshness, localizer);
    }

    public static string FreshnessLabel(EvidencePacketFreshness? freshness)
    {
        if (freshness is null)
        {
            return FreshnessUnavailable;
        }

        string state = Label(freshness.State);
        if (freshness.LastCheckedAt.HasValue)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{state}; checked {TimestampLabel(freshness.LastCheckedAt)}");
        }

        return freshness.AgeSeconds.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"{state}; age {freshness.AgeSeconds.Value}s")
            : state;
    }

    public static string FreshnessLabel(
        EvidencePacketFreshness? freshness,
        IStringLocalizer<MemoriesWebResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        if (freshness is null)
        {
            return localizer[EvidenceResourceKeys.FreshnessUnavailable];
        }

        string state = Label(freshness.State, localizer);
        if (freshness.LastCheckedAt.HasValue)
        {
            return localizer[
                EvidenceResourceKeys.FreshnessChecked,
                state,
                TimestampLabel(freshness.LastCheckedAt, localizer)];
        }

        if (!freshness.AgeSeconds.HasValue)
        {
            return state;
        }

        return freshness.AgeSeconds.Value < 0
            ? localizer[EvidenceResourceKeys.FreshnessUnavailable]
            : localizer[EvidenceResourceKeys.FreshnessAge, state, freshness.AgeSeconds.Value];
    }

    public static string TimestampLabel(DateTimeOffset? timestamp, string fallback = "timestamp unavailable")
        => timestamp.HasValue
            ? timestamp.Value.ToString("O", CultureInfo.InvariantCulture)
            : fallback;

    public static string TimestampLabel(
        DateTimeOffset? timestamp,
        IStringLocalizer<MemoriesWebResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return timestamp.HasValue
            ? localizer[
                EvidenceResourceKeys.TimestampValue,
                timestamp.Value.ToString("O", CultureInfo.InvariantCulture)]
            : localizer[EvidenceResourceKeys.TimestampUnavailable];
    }

    public static BadgeSlot SlotForState(EvidencePacketState state)
        => state switch
        {
            EvidencePacketState.Complete => BadgeSlot.Success,
            EvidencePacketState.Partial or EvidencePacketState.PendingExpansion or EvidencePacketState.Weak => BadgeSlot.Warning,
            EvidencePacketState.Empty or EvidencePacketState.Stale or EvidencePacketState.Degraded => BadgeSlot.Warning,
            EvidencePacketState.Unauthorized => BadgeSlot.Danger,
            _ => BadgeSlot.Neutral,
        };

    public static BadgeSlot SlotForEvidenceStrength(EvidencePacketEvidenceStrength strength)
        => strength switch
        {
            EvidencePacketEvidenceStrength.Strong => BadgeSlot.Success,
            EvidencePacketEvidenceStrength.Moderate => BadgeSlot.Warning,
            EvidencePacketEvidenceStrength.Weak => BadgeSlot.Warning,
            EvidencePacketEvidenceStrength.None or EvidencePacketEvidenceStrength.Unknown => BadgeSlot.Neutral,
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

        return SensitiveTextRegex().Replace(value, RedactedMarker);
    }

    public static string ScoreLabel(double? score)
    {
        if (!score.HasValue || !double.IsFinite(score.Value))
        {
            return "score unavailable";
        }

        return score.Value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    public static string ScoreLabel(
        double? score,
        IStringLocalizer<MemoriesWebResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return !score.HasValue || !double.IsFinite(score.Value)
            ? localizer[EvidenceResourceKeys.ScoreUnavailable]
            : localizer[
                EvidenceResourceKeys.ScoreValue,
                score.Value.ToString("0.###", CultureInfo.CurrentCulture)];
    }

    // Restrictive scope semantics: Story 17.1 treats EvidencePacketIsolationStatus.Unknown the same
    // as Unauthorized at the UI layer. Producers must set Authorized explicitly; anything else is a
    // signal the cockpit cannot trust to reveal contract content.
    public static bool IsRestrictiveScope(EvidencePacketIsolationStatus status)
        => status is EvidencePacketIsolationStatus.Unauthorized or EvidencePacketIsolationStatus.Unknown;

    private static bool IsContinuingAcronym(string text, int index)
    {
        // Returns true when the uppercase char at `index` continues an acronym run that
        // started in the preceding uppercase position. Examples:
        //   "MCP"        → C and P stay together with M.
        //   "MCPHandler" → C stays with M; P breaks (next is uppercase H starting a word)
        //                   so the result is "MCP handler".
        if (index == 0 || !char.IsUpper(text[index]))
        {
            return false;
        }

        if (!char.IsUpper(text[index - 1]))
        {
            return false;
        }

        // End of string: this uppercase char is the tail of the acronym run.
        if (index + 1 >= text.Length)
        {
            return true;
        }

        // Next is uppercase: still inside the acronym run (e.g., the C in "MCP" before P).
        return char.IsUpper(text[index + 1]);
    }

    [GeneratedRegex(
        "(bearer\\s+\\S+|authorization:\\s*\\S+|api[_-]?key=\\S+|sk_live_[A-Za-z0-9]+|sk_test_[A-Za-z0-9]+|ghp_[A-Za-z0-9]+|xoxb-[A-Za-z0-9-]+|AKIA[0-9A-Z]{16}|redis://\\S+|falkor\\S*|\\\\\\\\[A-Za-z0-9_.-]+\\\\[A-Za-z0-9_./\\\\-]+|[A-Za-z]:\\\\[A-Za-z0-9_./\\\\-]*|/home/\\S+|/users/\\S+|/etc/\\S+|/var/\\S+|/tmp/\\S+|/opt/\\S+|stack\\s*trace|\\bat\\s+[A-Z][A-Za-z0-9_.]+\\.[A-Z][A-Za-z0-9_]+\\(|eyJ[A-Za-z0-9_/+=-]+\\.[A-Za-z0-9_/+=.-]+|\\b[a-f0-9]{32,}\\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveTextRegex();
}
