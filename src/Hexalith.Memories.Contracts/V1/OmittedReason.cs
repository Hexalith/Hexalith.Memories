// <copyright file="OmittedReason.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Identifies why response items were omitted from a response envelope.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<OmittedReason>))]
public enum OmittedReason
{
    /// <summary>No items were omitted.</summary>
    None = 0,

    /// <summary>Items were omitted because the caller supplied a token budget.</summary>
    TokenBudget,

    /// <summary>Items were omitted because a backend was degraded or unavailable.</summary>
    BackendDegraded,

    /// <summary>Items were omitted because both token budget and backend degradation applied.</summary>
    Combined,
}
