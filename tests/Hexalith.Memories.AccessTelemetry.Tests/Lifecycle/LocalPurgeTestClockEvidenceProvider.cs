// <copyright file="LocalPurgeTestClockEvidenceProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Supplies inert evidence consumed only by the fixed offline clock double.</summary>
internal sealed class LocalPurgeTestClockEvidenceProvider : ILifecycleClockEvidenceProvider
{
    /// <inheritdoc/>
    public Task<LifecycleClockEvidence> GetAsync(CancellationToken cancellationToken)
        => Task.FromResult(new LifecycleClockEvidence(null!, "process", "instance"));
}
