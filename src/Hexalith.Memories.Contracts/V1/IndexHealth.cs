// <copyright file="IndexHealth.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>
/// Health state of a retrieval axis for a tenant (Story 5.5 AC1/AC2).
/// Distinguishes <em>data state</em> (<see cref="Ready"/>, <see cref="Missing"/>, <see cref="Degraded"/>)
/// from <em>availability state</em> (<see cref="Unknown"/>).
/// </summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<IndexHealth>))]
public enum IndexHealth
{
    /// <summary>The retrieval axis responded, its index exists, and its item count was parseable.</summary>
    Ready,

    /// <summary>
    /// The retrieval axis responded, but the expected index is absent.
    /// Signal for "provisioning incomplete" or "index dropped after deletion".
    /// </summary>
    Missing,

    /// <summary>
    /// The retrieval axis responded, but the response indicates reduced capability:
    /// payload is well-formed but the count field is absent/unparseable, OR
    /// the server returned a <c>LOADING</c>/<c>BUSY</c> error response.
    /// NOT used for timeouts or connection failures (those are <see cref="Unknown"/>).
    /// </summary>
    Degraded,

    /// <summary>
    /// The retrieval axis is unreachable because of a connection timeout or unavailable service.
    /// Indicates availability failure, not data state. Parallels <c>IndexSizes.* == null</c>.
    /// </summary>
    Unknown,
}
