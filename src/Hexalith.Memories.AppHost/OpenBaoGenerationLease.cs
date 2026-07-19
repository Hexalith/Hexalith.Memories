// <copyright file="OpenBaoGenerationLease.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Owns the cancellation and completion signals for one OpenBao process generation.</summary>
internal sealed class OpenBaoGenerationLease
{
    private readonly CancellationTokenSource _cancellation = new();

    /// <summary>Initializes a new instance of the <see cref="OpenBaoGenerationLease"/> class.</summary>
    /// <param name="generationNumber">The monotonically increasing generation number.</param>
    /// <param name="readiness">The readiness signal reserved for this generation.</param>
    internal OpenBaoGenerationLease(int generationNumber, TaskCompletionSource readiness)
    {
        GenerationNumber = generationNumber;
        Readiness = readiness;
    }

    /// <summary>Gets the generation number.</summary>
    internal int GenerationNumber { get; }

    /// <summary>Gets the cancellation token invalidated when this generation stops.</summary>
    internal CancellationToken CancellationToken => _cancellation.Token;

    /// <summary>Gets the readiness signal reserved for this generation.</summary>
    internal TaskCompletionSource Readiness { get; }

    /// <summary>Invalidates this lease.</summary>
    internal void Cancel() => _cancellation.Cancel();
}
