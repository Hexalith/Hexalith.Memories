// <copyright file="RecoveryActionAvailability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Whether a recovery action can be safely emitted in the current tenant/case authorization context.
/// </summary>
public enum RecoveryActionAvailability
{
    /// <summary>The action is safe to surface and emit in the current context.</summary>
    Available,

    /// <summary>
    /// The action is unsafe, permission-dependent, or scope-expanding in the current context and must
    /// render disabled with a localized reason rather than being hidden or auto-executed.
    /// </summary>
    Unavailable,
}
