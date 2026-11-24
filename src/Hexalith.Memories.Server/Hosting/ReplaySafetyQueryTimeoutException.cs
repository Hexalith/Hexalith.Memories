// <copyright file="ReplaySafetyQueryTimeoutException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

/// <summary>A replay-safety sidecar query exhausted its own time budget.</summary>
/// <param name="innerException">The cancellation observed by the sidecar query.</param>
internal sealed class ReplaySafetyQueryTimeoutException(OperationCanceledException innerException)
    : TimeoutException("The replay-safety sidecar query timed out.", innerException);
