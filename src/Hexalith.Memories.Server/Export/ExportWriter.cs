// <copyright file="ExportWriter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Export;

using System.IO.Pipelines;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Internal streaming JSON writer for data exports (Story 8.3). Wraps a <see cref="Utf8JsonWriter"/>
/// over a <see cref="PipeWriter"/> so response bytes flow to the client as they are emitted — the
/// full export is never materialized server-side.
/// <para>
/// Flush cadence: whichever comes first of every 1000 memory units or 1 MiB of unflushed bytes.
/// Callers invoke <see cref="MaybeFlushAsync"/> at logical boundaries and <see cref="FlushAsync"/>
/// at the very end. <see cref="DisposeAsync"/> flushes and releases the underlying writer.
/// </para>
/// </summary>
internal sealed class ExportWriter : IAsyncDisposable
{
    private const long UnflushedByteThreshold = 1024L * 1024L; // 1 MiB
    private const int UnflushedUnitThreshold = 1000;

    private readonly Utf8JsonWriter _writer;
    private readonly PipeWriter _pipeWriter;
    private int _unitsSinceFlush;

    public ExportWriter(PipeWriter pipeWriter)
    {
        ArgumentNullException.ThrowIfNull(pipeWriter);
        _pipeWriter = pipeWriter;
        _writer = new Utf8JsonWriter(pipeWriter, new JsonWriterOptions
        {
            Indented = false,
        });
    }

    /// <summary>Writes the opening top-level JSON object.</summary>
    public void StartDocument() => _writer.WriteStartObject();

    /// <summary>Writes the closing top-level JSON object.</summary>
    public void EndDocument() => _writer.WriteEndObject();

    /// <summary>Emits the manifest as the first top-level field.</summary>
    public void WriteManifest(ExportManifest manifest)
    {
        _writer.WritePropertyName("manifest");
        JsonSerializer.Serialize(_writer, manifest, MemoriesJsonContext.Options);
    }

    /// <summary>Emits the case-scope <c>case</c> section (case record + members).</summary>
    public void WriteCaseSection(Case caseRecord, IReadOnlyList<CaseMember> members)
    {
        _writer.WritePropertyName("case");
        WriteCaseObject(caseRecord, members);
    }

    /// <summary>Emits the tenant-scope <c>tenant</c> section.</summary>
    public void WriteTenantSection(ExportedTenantConfig tenant)
    {
        _writer.WritePropertyName("tenant");
        JsonSerializer.Serialize(_writer, tenant, MemoriesJsonContext.Options);
    }

    /// <summary>Starts the <c>cases</c> array (tenant-scope).</summary>
    public void StartCasesArray()
    {
        _writer.WritePropertyName("cases");
        _writer.WriteStartArray();
    }

    /// <summary>Writes one case element into the <c>cases</c> array.</summary>
    public void WriteCase(Case caseRecord, IReadOnlyList<CaseMember> members)
        => WriteCaseObject(caseRecord, members);

    private void WriteCaseObject(Case caseRecord, IReadOnlyList<CaseMember> members)
    {
        // Merge the Case record's fields with the export-only `members` projection so re-importers
        // see them as siblings. Serialize-to-element + re-emit keeps us on the source-gen JSON path.
        JsonElement caseElement = JsonSerializer.SerializeToElement(caseRecord, MemoriesJsonContext.Options);
        _writer.WriteStartObject();
        foreach (JsonProperty property in caseElement.EnumerateObject())
        {
            property.WriteTo(_writer);
        }

        _writer.WritePropertyName("members");
        JsonSerializer.Serialize(_writer, members, MemoriesJsonContext.Options);
        _writer.WriteEndObject();
    }

    /// <summary>Closes the <c>cases</c> array.</summary>
    public void EndCasesArray() => _writer.WriteEndArray();

    /// <summary>Starts the <c>memoryUnits</c> array.</summary>
    public void StartMemoryUnitsArray()
    {
        _writer.WritePropertyName("memoryUnits");
        _writer.WriteStartArray();
    }

    /// <summary>Writes one memory unit wrapper element into the <c>memoryUnits</c> array.</summary>
    public void WriteMemoryUnit(ExportedMemoryUnit entry)
    {
        JsonSerializer.Serialize(_writer, entry, MemoriesJsonContext.Options);
        _unitsSinceFlush++;
    }

    /// <summary>Closes the <c>memoryUnits</c> array.</summary>
    public void EndMemoryUnitsArray() => _writer.WriteEndArray();

    /// <summary>Starts the <c>edges</c> array.</summary>
    public void StartEdgesArray()
    {
        _writer.WritePropertyName("edges");
        _writer.WriteStartArray();
    }

    /// <summary>Writes one edge element.</summary>
    public void WriteEdge(ExportedEdge edge) => JsonSerializer.Serialize(_writer, edge, MemoriesJsonContext.Options);

    /// <summary>Closes the <c>edges</c> array.</summary>
    public void EndEdgesArray() => _writer.WriteEndArray();

    /// <summary>Emits the final <c>statistics</c> field.</summary>
    public void WriteStatistics(ExportStatistics statistics)
    {
        _writer.WritePropertyName("statistics");
        JsonSerializer.Serialize(_writer, statistics, MemoriesJsonContext.Options);
    }

    /// <summary>
    /// Conditionally flushes when either the unit-count or byte threshold is crossed. Safe to call
    /// between logical items; cheap when nothing needs flushing.
    /// </summary>
    public async ValueTask MaybeFlushAsync(CancellationToken ct)
    {
        if (_unitsSinceFlush >= UnflushedUnitThreshold || _writer.BytesPending >= UnflushedByteThreshold)
        {
            await FlushAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>Unconditionally flushes both the Utf8JsonWriter and the underlying PipeWriter.</summary>
    public async ValueTask FlushAsync(CancellationToken ct)
    {
        await _writer.FlushAsync(ct).ConfigureAwait(false);
        FlushResult result = await _pipeWriter.FlushAsync(ct).ConfigureAwait(false);
        _unitsSinceFlush = 0;
        if (result.IsCanceled || result.IsCompleted)
        {
            throw new OperationCanceledException("The response pipe completed before the export finished.", ct);
        }
    }

    /// <summary>Flushes and disposes the underlying writer.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _writer.FlushAsync().ConfigureAwait(false);
        }
        catch
        {
            // Best-effort flush on dispose; connection may already be torn down.
        }

        await _writer.DisposeAsync().ConfigureAwait(false);
    }
}
