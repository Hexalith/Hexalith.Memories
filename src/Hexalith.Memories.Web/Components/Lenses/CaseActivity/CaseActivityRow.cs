// <copyright file="CaseActivityRow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.CaseActivity;

using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// A single, sanitized Case Activity Trail row.
/// </summary>
/// <remarks>
/// Story 17.4 — every status carries a localized label and an accessible name, never color or position
/// alone. Missing, redacted, or unauthorized source links render as explicit
/// <see cref="LensFieldAvailability"/> states rather than broken links or silent omissions.
/// </remarks>
/// <param name="Order">Deterministic order index.</param>
/// <param name="Kind">The activity kind.</param>
/// <param name="KindLabelKey">Localization key for the activity kind label.</param>
/// <param name="SafeSummary">Sanitized human-readable summary literal.</param>
/// <param name="TimestampAvailability">Availability of the source activity timestamp.</param>
/// <param name="SafeTimestamp">Sanitized ISO-8601 timestamp, or the unavailable fallback.</param>
/// <param name="LinkAvailability">Availability of the source/relationship link for this row.</param>
/// <param name="SafeLink">Sanitized link target literal, or a documented unavailable fallback.</param>
/// <param name="StatusLabelKey">Localization key for the row status label.</param>
/// <param name="Severity">Severity used for the row's badge and announcement politeness.</param>
public sealed record CaseActivityRow(
    int Order,
    CaseActivityKind Kind,
    string KindLabelKey,
    string SafeSummary,
    LensFieldAvailability TimestampAvailability,
    string SafeTimestamp,
    LensFieldAvailability LinkAvailability,
    string SafeLink,
    string StatusLabelKey,
    RecoverySeverity Severity);
