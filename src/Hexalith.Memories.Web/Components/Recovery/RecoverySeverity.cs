// <copyright file="RecoverySeverity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Severity of a recovery state. Drives the visual treatment and the accessible announcement
/// politeness, but is always paired with a text label so color is never the sole signal.
/// </summary>
public enum RecoverySeverity
{
    /// <summary>No risk; informational completeness state.</summary>
    None,

    /// <summary>Informational, non-blocking.</summary>
    Info,

    /// <summary>Low-risk caution that warrants attention but does not block.</summary>
    Caution,

    /// <summary>Elevated risk that affects answer trust or capability availability.</summary>
    Warning,

    /// <summary>Blocking or safety-critical state, such as an authorization failure.</summary>
    Critical,
}
