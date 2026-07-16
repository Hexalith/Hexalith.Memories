// <copyright file="RecoveryRiskMarker.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// A secondary risk marker that decorates the primary recovery state without hiding it.
/// </summary>
/// <remarks>
/// Stale, compressed, and degraded conditions remain visible as secondary markers when another
/// higher-risk state owns the primary recovery action, per the state-precedence rules.
/// </remarks>
/// <param name="LabelKey">Localization key for the marker label.</param>
/// <param name="Severity">Severity conveyed by the marker.</param>
/// <param name="Code">Stable whitelisted code used for stable test selectors.</param>
public sealed record RecoveryRiskMarker(
    string LabelKey,
    RecoverySeverity Severity,
    string Code);
