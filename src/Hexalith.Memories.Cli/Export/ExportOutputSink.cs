// <copyright file="ExportOutputSink.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Export;

/// <summary>
/// Story 8.3: abstraction over the destination of a CLI export. For stdout, the inner stream is
/// written directly and <see cref="Commit"/> / <see cref="Abort"/> are no-ops. For <c>--output</c>,
/// writes go to a <c>.part</c> file; <see cref="Commit"/> atomically renames it to the final path
/// and <see cref="Abort"/> deletes it.
/// </summary>
internal sealed class ExportOutputSink : IDisposable
{
    private readonly Stream _stream;
    private readonly string? _partPath;
    private readonly string? _finalPath;
    private readonly bool _force;
    private bool _committed;
    private bool _aborted;
    private bool _disposed;

    private ExportOutputSink(Stream stream, string? partPath, string? finalPath, bool force)
    {
        _stream = stream;
        _partPath = partPath;
        _finalPath = finalPath;
        _force = force;
    }

    public Stream Stream => _stream;

    public static ExportOutputSink ForStdout(Stream stdout)
    {
        ArgumentNullException.ThrowIfNull(stdout);
        return new ExportOutputSink(stdout, partPath: null, finalPath: null, force: false);
    }

    public static ExportOutputSink ForFile(string finalPath, bool force)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(finalPath);
        string partPath = finalPath + ".part";
        FileStream stream = new(partPath, FileMode.Create, FileAccess.Write, FileShare.None);
        return new ExportOutputSink(stream, partPath, finalPath, force);
    }

    public void Commit()
    {
        if (_committed || _aborted)
        {
            return;
        }

        _stream.Flush();
        _stream.Dispose();

        if (_partPath is not null && _finalPath is not null)
        {
            File.Move(_partPath, _finalPath, overwrite: _force);
        }

        _committed = true;
    }

    public void Abort()
    {
        if (_committed || _aborted)
        {
            return;
        }

        try
        {
            _stream.Dispose();
        }
        catch
        {
            // Best effort — we are already in an error path.
        }

        if (_partPath is not null)
        {
            try
            {
                if (File.Exists(_partPath))
                {
                    File.Delete(_partPath);
                }
            }
            catch
            {
                // Best effort — we are already in an error path.
            }
        }

        _aborted = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_committed && !_aborted)
        {
            Abort();
        }
    }
}
