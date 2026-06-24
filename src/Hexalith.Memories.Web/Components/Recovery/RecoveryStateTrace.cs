// <copyright file="RecoveryStateTrace.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// One row of the recovery-state traceability table: a presentation state with its localization keys,
/// severity, affected capability, and the named Evidence Packet fields that justify it.
/// </summary>
/// <param name="Kind">The recovery state.</param>
/// <param name="TitleKey">Localization key for the title.</param>
/// <param name="ExplanationKey">Localization key for the explanation.</param>
/// <param name="AffectedCapability">The capability the state affects.</param>
/// <param name="AffectedCapabilityKey">Localization key for the affected capability label.</param>
/// <param name="Severity">Severity of the state.</param>
/// <param name="ContractSources">
/// Named Evidence Packet fields (or the no-source sentinel) that justify rendering this state.
/// </param>
public sealed record RecoveryStateTrace(
    RecoveryStateKind Kind,
    string TitleKey,
    string ExplanationKey,
    RecoveryCapability AffectedCapability,
    string AffectedCapabilityKey,
    RecoverySeverity Severity,
    IReadOnlyList<string> ContractSources);
