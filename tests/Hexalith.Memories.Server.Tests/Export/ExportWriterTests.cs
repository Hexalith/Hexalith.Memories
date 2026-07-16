// <copyright file="ExportWriterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Export;

using System.IO.Pipelines;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Export;

using Shouldly;

/// <summary>
/// Story 8.3 — exercises <see cref="ExportWriter"/> in isolation, driving it with a
/// <see cref="MemoryStream"/>-backed <see cref="PipeWriter"/> and asserting the emitted JSON
/// token sequence.
/// </summary>
public class ExportWriterTests
{
    [Fact]
    public async Task EmitsManifestAsFirstTopLevelField()
    {
        MemoryStream stream = new();
        PipeWriter pipeWriter = PipeWriter.Create(stream);

        await using (ExportWriter writer = new(pipeWriter))
        {
            writer.StartDocument();
            writer.WriteManifest(NewManifest());
            writer.WriteStatistics(new ExportStatistics(0, 0, 0));
            writer.EndDocument();
            await writer.FlushAsync(CancellationToken.None);
        }

        JsonDocument doc = JsonDocument.Parse(stream.ToArray());
        JsonElement.ObjectEnumerator enumerator = doc.RootElement.EnumerateObject();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.Name.ShouldBe("manifest");
        enumerator.Current.Value.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task CaseSection_EmitsCaseRecordAndMembers()
    {
        MemoryStream stream = new();
        PipeWriter pipeWriter = PipeWriter.Create(stream);

        Hexalith.Memories.Contracts.V1.Case caseRecord = new(
            Id: "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            TenantId: "acme",
            Name: "Q1 Planning",
            Description: null,
            Status: CaseStatus.Active,
            CreatedAt: DateTimeOffset.UtcNow,
            LastUpdated: DateTimeOffset.UtcNow,
            MemoryUnitCount: 0);
        List<CaseMember> members = new()
        {
            new CaseMember("alice@acme.com", CaseMemberType.User, DateTimeOffset.UtcNow),
        };

        await using (ExportWriter writer = new(pipeWriter))
        {
            writer.StartDocument();
            writer.WriteManifest(NewManifest());
            writer.WriteCaseSection(caseRecord, members);
            writer.EndDocument();
            await writer.FlushAsync(CancellationToken.None);
        }

        JsonDocument doc = JsonDocument.Parse(stream.ToArray());
        JsonElement caseEl = doc.RootElement.GetProperty("case");
        caseEl.GetProperty("id").GetString().ShouldBe("01HM5Q9WXGK6T8Q4Z5Y6V7W8X9");
        caseEl.GetProperty("members").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task MemoryUnitsArray_WrapsUnitsWithAnnotationTargets()
    {
        MemoryStream stream = new();
        PipeWriter pipeWriter = PipeWriter.Create(stream);

        MemoryUnit unit = new()
        {
            Id = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            TenantId = "acme",
            CaseId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X0",
            Content = "hello",
            ContentHash = "sha256:x",
            SourceUri = "file:///a.md",
            SourceType = SourceType.File,
            IngestedBy = "alice",
            IngestedAt = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            Status = MemoryUnitStatus.Indexed,
        };

        await using (ExportWriter writer = new(pipeWriter))
        {
            writer.StartDocument();
            writer.WriteManifest(NewManifest());
            writer.StartMemoryUnitsArray();
            writer.WriteMemoryUnit(new ExportedMemoryUnit(unit, new[] { "anno1" }));
            writer.EndMemoryUnitsArray();
            writer.EndDocument();
            await writer.FlushAsync(CancellationToken.None);
        }

        JsonDocument doc = JsonDocument.Parse(stream.ToArray());
        JsonElement items = doc.RootElement.GetProperty("memoryUnits");
        items.GetArrayLength().ShouldBe(1);
        items[0].GetProperty("unit").GetProperty("id").GetString().ShouldBe(unit.Id);
        items[0].GetProperty("annotationTargets").GetArrayLength().ShouldBe(1);
        items[0].GetProperty("annotationTargets")[0].GetString().ShouldBe("anno1");
    }

    [Fact]
    public async Task EdgeEmission_PreservesConfidencePromotionFields()
    {
        MemoryStream stream = new();
        PipeWriter pipeWriter = PipeWriter.Create(stream);

        ExportedEdge edge = new(
            Id: "4273",
            SourceId: "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
            TargetId: "01HM5Q9WXGK6T8Q4Z5Y6V7W8X0",
            EdgeType: "causedBy",
            Confidence: 0.9f,
            Origin: "inferred",
            CreatedAt: DateTimeOffset.UtcNow,
            VerifiedBy: "bob@acme.com",
            PreviousConfidence: 0.6f);

        await using (ExportWriter writer = new(pipeWriter))
        {
            writer.StartDocument();
            writer.WriteManifest(NewManifest());
            writer.StartEdgesArray();
            writer.WriteEdge(edge);
            writer.EndEdgesArray();
            writer.EndDocument();
            await writer.FlushAsync(CancellationToken.None);
        }

        JsonDocument doc = JsonDocument.Parse(stream.ToArray());
        JsonElement edges = doc.RootElement.GetProperty("edges");
        edges.GetArrayLength().ShouldBe(1);
        edges[0].GetProperty("edgeType").GetString().ShouldBe("causedBy");
        edges[0].GetProperty("verifiedBy").GetString().ShouldBe("bob@acme.com");
        edges[0].GetProperty("previousConfidence").GetSingle().ShouldBe(0.6f);
    }

    [Fact]
    public async Task FlushAsync_WhenPipeCompletes_ThrowsOperationCanceledException()
    {
        PipeWriter pipeWriter = new CompletedPipeWriter();

        await using ExportWriter writer = new(pipeWriter);
        writer.StartDocument();
        writer.EndDocument();

        await Should.ThrowAsync<OperationCanceledException>(() => writer.FlushAsync(CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ExportScope_EmittedAsCamelCaseString()
    {
        MemoryStream stream = new();
        PipeWriter pipeWriter = PipeWriter.Create(stream);

        await using (ExportWriter writer = new(pipeWriter))
        {
            writer.StartDocument();
            writer.WriteManifest(new ExportManifest(
                SchemaVersion: 1,
                Scope: ExportScope.Tenant,
                TenantId: "acme",
                CaseId: null,
                ExportedAt: DateTimeOffset.UtcNow,
                SnapshotAt: DateTimeOffset.UtcNow));
            writer.EndDocument();
            await writer.FlushAsync(CancellationToken.None);
        }

        JsonDocument doc = JsonDocument.Parse(stream.ToArray());
        string scope = doc.RootElement.GetProperty("manifest").GetProperty("scope").GetString()!;
        scope.ShouldBe("tenant");
    }

    [Fact]
    public async Task Statistics_EmittedAsFinalTopLevelField()
    {
        MemoryStream stream = new();
        PipeWriter pipeWriter = PipeWriter.Create(stream);

        await using (ExportWriter writer = new(pipeWriter))
        {
            writer.StartDocument();
            writer.WriteManifest(NewManifest());
            writer.StartMemoryUnitsArray();
            writer.EndMemoryUnitsArray();
            writer.StartEdgesArray();
            writer.EndEdgesArray();
            writer.WriteStatistics(new ExportStatistics(7, 13, 1));
            writer.EndDocument();
            await writer.FlushAsync(CancellationToken.None);
        }

        JsonDocument doc = JsonDocument.Parse(stream.ToArray());
        string lastProperty = string.Empty;
        foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
        {
            lastProperty = prop.Name;
        }

        lastProperty.ShouldBe("statistics");
        doc.RootElement.GetProperty("statistics").GetProperty("memoryUnitCount").GetInt32().ShouldBe(7);
    }

    private static ExportManifest NewManifest() => new(
        SchemaVersion: 1,
        Scope: ExportScope.Case,
        TenantId: "acme",
        CaseId: "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
        ExportedAt: DateTimeOffset.UtcNow,
        SnapshotAt: DateTimeOffset.UtcNow);

    private sealed class CompletedPipeWriter : PipeWriter
    {
        public override void Advance(int bytes)
        {
        }

        public override void CancelPendingFlush()
        {
        }

        public override void Complete(Exception? exception = null)
        {
        }

        public override ValueTask CompleteAsync(Exception? exception = null) => ValueTask.CompletedTask;

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FlushResult(isCanceled: false, isCompleted: true));

        public override Memory<byte> GetMemory(int sizeHint = 0)
            => new byte[Math.Max(sizeHint, 1)];

        public override Span<byte> GetSpan(int sizeHint = 0)
            => new byte[Math.Max(sizeHint, 1)];
    }
}
