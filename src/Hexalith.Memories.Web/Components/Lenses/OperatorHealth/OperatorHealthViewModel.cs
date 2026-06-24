// <copyright file="OperatorHealthViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.OperatorHealth;

using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Typed, pure projection of an Evidence Packet into the Operator Health Matrix lens (AC3).
/// </summary>
/// <remarks>
/// Story 17.4 — produced by <see cref="OperatorHealthMatrixMapper.Map"/>. The canonical contract exposes
/// no last-checked timestamp, so <see cref="LastCheckedAvailable"/> is false and the component shows the
/// unavailable note. The most severe check drives the live-region politeness.
/// </remarks>
/// <param name="Checks">The fixed, deterministically ordered set of health checks.</param>
/// <param name="LastCheckedAvailable">Whether the contract exposes a last-checked time (currently false).</param>
/// <param name="LastCheckedNoteKey">Localization key for the last-checked-unavailable note.</param>
/// <param name="HighestSeverity">The highest check severity, used for live-region politeness.</param>
/// <param name="HasTrustBlocking">Whether any check is a trust-blocking state.</param>
public sealed record OperatorHealthViewModel(
    IReadOnlyList<OperatorHealthCheckRow> Checks,
    bool LastCheckedAvailable,
    string LastCheckedNoteKey,
    RecoverySeverity HighestSeverity,
    bool HasTrustBlocking);
