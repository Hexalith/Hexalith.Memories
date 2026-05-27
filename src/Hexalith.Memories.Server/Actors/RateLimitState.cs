// <copyright file="RateLimitState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using System.Runtime.Serialization;

/// <summary>State for the per-tenant embedding rate limiter.</summary>
/// <param name="Remaining">The number of requests remaining in the current window.</param>
/// <param name="WindowStart">The start time of the current rate limit window.</param>
/// <param name="CeilingPerMinute">The maximum requests allowed per minute.</param>
[DataContract]
public sealed record RateLimitState(
    [property: DataMember] int Remaining,
    [property: DataMember] DateTime WindowStart,
    [property: DataMember] int CeilingPerMinute);
