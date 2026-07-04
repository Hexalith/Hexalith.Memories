// <copyright file="InboundRateLimitOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.RateLimiting;

/// <summary>Configuration for inbound ASP.NET Core request rate limiting.</summary>
internal sealed class InboundRateLimitOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "InboundRateLimiting";

    /// <summary>Gets or sets the permitted requests per partition during the configured window.</summary>
    public int PermitLimit { get; set; } = 120;

    /// <summary>Gets or sets the fixed-window duration in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Gets or sets the queued request limit. Defaults to zero to reject excess load.</summary>
    public int QueueLimit { get; set; }
}
