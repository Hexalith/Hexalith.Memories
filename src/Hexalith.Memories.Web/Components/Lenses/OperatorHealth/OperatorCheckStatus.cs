// <copyright file="OperatorCheckStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.OperatorHealth;

/// <summary>Status of a single operator health check.</summary>
public enum OperatorCheckStatus
{
    /// <summary>The check passed.</summary>
    Healthy = 0,

    /// <summary>The check warrants attention but does not block trust.</summary>
    Caution,

    /// <summary>The check is degraded; the affected capability is partially available.</summary>
    Degraded,

    /// <summary>The check is trust-blocking; the affected capability cannot be trusted.</summary>
    Blocked,

    /// <summary>The contract cannot safely determine the status; render a safe unknown state.</summary>
    Unknown,
}
