// <copyright file="DerivedStoreCorrectionState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Identifies the durable lifecycle state of a correction operation.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<DerivedStoreCorrectionState>))]
public enum DerivedStoreCorrectionState
{
    /// <summary>The operation is durably accepted but has not begun applying units.</summary>
    Pending,

    /// <summary>The operation is applying or verifying unit migrations.</summary>
    Running,

    /// <summary>The operation converged successfully.</summary>
    Succeeded,

    /// <summary>The operation was already converged for an equal or newer source version.</summary>
    NoOp,

    /// <summary>The operation failed and is safe to retry.</summary>
    Failed,

    /// <summary>The operation did not reach a terminal result before its sixty-minute deadline.</summary>
    TimedOut,
}
