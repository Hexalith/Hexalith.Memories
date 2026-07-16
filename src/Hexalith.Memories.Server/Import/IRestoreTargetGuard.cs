// <copyright file="IRestoreTargetGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

/// <summary>Rejects restore into a target that already contains unrelated projection data.</summary>
internal interface IRestoreTargetGuard
{
    /// <summary>Fails unless the tenant or case target has no existing case, memory-unit, or graph artifacts.</summary>
    Task EnsureCleanAsync(string tenantId, string? caseId, CancellationToken cancellationToken);
}
