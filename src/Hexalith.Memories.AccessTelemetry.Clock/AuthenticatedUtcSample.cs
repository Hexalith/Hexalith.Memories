// <copyright file="AuthenticatedUtcSample.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

/// <summary>Authenticated uncertainty interval from one independent UTC authority.</summary>
internal sealed record AuthenticatedUtcSample(
    string SourceId,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    bool Authenticated);
