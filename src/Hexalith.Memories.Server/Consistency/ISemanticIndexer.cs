// <copyright file="ISemanticIndexer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

/// <summary>
/// Testability seam over <see cref="SemanticIndexer"/>. Injected into
/// <c>RepairUnitActivity</c>; mocked in unit tests.
/// </summary>
public interface ISemanticIndexer
{
    /// <inheritdoc cref="SemanticIndexer.ReIndexFromSyntacticAsync"/>
    Task ReIndexFromSyntacticAsync(string tenantId, string memoryUnitId, CancellationToken ct);
}
