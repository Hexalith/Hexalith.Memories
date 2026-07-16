// <copyright file="RestoreLeaseResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

/// <summary>Result of acquiring a clean-target restore lease.</summary>
/// <param name="Acquired">Whether this request owns the target lease.</param>
/// <param name="InstanceId">The owning workflow instance.</param>
/// <param name="SameOperation">Whether an existing lease represents the same staged content.</param>
internal sealed record RestoreLeaseResult(bool Acquired, string InstanceId, bool SameOperation);
