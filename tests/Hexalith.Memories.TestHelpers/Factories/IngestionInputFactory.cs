// <copyright file="IngestionInputFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

// ATDD Red Phase: This factory is ready for Story 1.6 implementation.
// Uncomment once IngestionInput contract is created in Contracts/V1/IngestionInput.cs.
// See: _bmad-output/test-artifacts/atdd-checklist-epic-1.md

#if false // ATDD: Enable when IngestionInput contract exists (Story 1.6, Task 1.1)

namespace Hexalith.Memories.TestHelpers.Factories;

using System.Text;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Factory for creating <see cref="IngestionInput"/> instances with sensible defaults.
/// Override specific properties per test to make intent explicit.
/// </summary>
public static class IngestionInputFactory
{
    private static int _counter;

    /// <summary>
    /// Creates an IngestionInput with sensible defaults. Override specific fields per test.
    /// </summary>
    public static IngestionInput Create(
        string? tenantId = null,
        string? caseId = null,
        string? sourceUri = null,
        byte[]? contentBytes = null,
        string? contentType = null,
        SourceType? sourceType = null,
        string? ingestedBy = null,
        Dictionary<string, MetadataField>? metadata = null,
        string? causationId = null,
        string? correlationId = null)
    {
        int id = Interlocked.Increment(ref _counter);

        return new IngestionInput
        {
            TenantId = tenantId ?? "test-tenant",
            CaseId = caseId ?? $"case-{id:D6}",
            SourceUri = sourceUri ?? $"file:///document-{id}.txt",
            ContentBytes = contentBytes ?? Encoding.UTF8.GetBytes($"Sample content for ingestion {id}"),
            ContentType = contentType ?? "text/plain",
            SourceType = sourceType ?? SourceType.File,
            IngestedBy = ingestedBy ?? "test-user@example.com",
            Metadata = metadata ?? [],
            CausationId = causationId,
            CorrelationId = correlationId,
        };
    }
}

#endif
