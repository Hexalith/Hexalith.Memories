// <copyright file="IAuthenticatedUtcSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

/// <summary>One independently administered authenticated UTC authority.</summary>
internal interface IAuthenticatedUtcSource
{
    /// <summary>Gets the bounded stable source identity.</summary>
    string SourceId { get; }

    /// <summary>Gets one authenticated UTC uncertainty interval.</summary>
    Task<AuthenticatedUtcSample> GetUtcSampleAsync(CancellationToken cancellationToken);
}
