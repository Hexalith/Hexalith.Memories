// <copyright file="AuthenticatedUtcResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

/// <summary>Bounded HTTPS UTC-source response.</summary>
internal sealed record AuthenticatedUtcResponse
{
    /// <summary>Gets the asserted Unix milliseconds.</summary>
    public required long UnixMilliseconds { get; init; }

    /// <summary>Gets the symmetric uncertainty, capped by the clock gate.</summary>
    public required int UncertaintyMilliseconds { get; init; }
}
