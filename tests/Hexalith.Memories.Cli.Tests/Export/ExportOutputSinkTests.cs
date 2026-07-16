// <copyright file="ExportOutputSinkTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Export;

using System.Text;

using Hexalith.Memories.Cli.Export;

using Shouldly;

/// <summary>Story 8.3 — atomic-write behavior of the CLI export sink.</summary>
public sealed class ExportOutputSinkTests : IDisposable
{
    private readonly string _scratchDir;

    public ExportOutputSinkTests()
    {
        _scratchDir = Path.Combine(Path.GetTempPath(), "hexalith-export-sink-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scratchDir);
    }

    [Fact]
    public void Commit_RenamesPartToFinal()
    {
        string finalPath = Path.Combine(_scratchDir, "export.json");
        using ExportOutputSink sink = ExportOutputSink.ForFile(finalPath, force: false);

        byte[] data = Encoding.UTF8.GetBytes("{\"ok\":true}");
        sink.Stream.Write(data);
        sink.Commit();

        File.Exists(finalPath).ShouldBeTrue();
        File.Exists(finalPath + ".part").ShouldBeFalse();
        File.ReadAllText(finalPath).ShouldBe("{\"ok\":true}");
    }

    [Fact]
    public void Abort_DeletesPartFile()
    {
        string finalPath = Path.Combine(_scratchDir, "export.json");
        using ExportOutputSink sink = ExportOutputSink.ForFile(finalPath, force: false);
        sink.Stream.WriteByte(1);
        sink.Abort();

        File.Exists(finalPath).ShouldBeFalse();
        File.Exists(finalPath + ".part").ShouldBeFalse();
    }

    [Fact]
    public void DisposeWithoutCommit_AbortsAndDeletesPartFile()
    {
        string finalPath = Path.Combine(_scratchDir, "export.json");
        using (ExportOutputSink sink = ExportOutputSink.ForFile(finalPath, force: false))
        {
            sink.Stream.WriteByte(1);
        }

        File.Exists(finalPath).ShouldBeFalse();
        File.Exists(finalPath + ".part").ShouldBeFalse();
    }

    [Fact]
    public void ForceTrue_AllowsOverwrite()
    {
        string finalPath = Path.Combine(_scratchDir, "export.json");
        File.WriteAllText(finalPath, "old");

        using ExportOutputSink sink = ExportOutputSink.ForFile(finalPath, force: true);
        sink.Stream.Write(Encoding.UTF8.GetBytes("new"));
        sink.Commit();

        File.ReadAllText(finalPath).ShouldBe("new");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_scratchDir))
            {
                Directory.Delete(_scratchDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup.
        }

        GC.SuppressFinalize(this);
    }
}
