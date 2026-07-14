// <copyright file="ImportJsonStreamReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using System.Buffers;
using System.Text.Json;

/// <summary>Small bounded-buffer JSON framing reader for the canonical streaming export envelope.</summary>
internal sealed class ImportJsonStreamReader
{
    private const int BufferBytes = 64 * 1024;
    private const int MaxSingleValueBytes = 64 * 1024 * 1024;

    private readonly byte[] _buffer = new byte[BufferBytes];
    private readonly Stream _stream;
    private int _bufferLength;
    private int _bufferOffset;
    private int _pushedBack = -1;

    internal ImportJsonStreamReader(Stream stream) => _stream = stream;

    internal async ValueTask ExpectAsync(byte expected, CancellationToken cancellationToken)
    {
        int actual = await ReadNonWhitespaceAsync(cancellationToken).ConfigureAwait(false);
        if (actual != expected)
        {
            throw new ImportEnvelopeException(
                "MALFORMED_IMPORT",
                $"Expected JSON token '{(char)expected}' but found '{Describe(actual)}'.");
        }
    }

    internal async ValueTask<int> PeekNonWhitespaceAsync(CancellationToken cancellationToken)
    {
        int value = await ReadNonWhitespaceAsync(cancellationToken).ConfigureAwait(false);
        PushBack(value);
        return value;
    }

    internal async ValueTask<string> ReadPropertyNameAsync(CancellationToken cancellationToken)
    {
        byte[] raw = await ReadRawValueAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<string>(raw)
                ?? throw new ImportEnvelopeException("MALFORMED_IMPORT", "A top-level property name cannot be null.");
        }
        catch (JsonException ex)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", "A top-level property name is not a valid JSON string.", ex);
        }
    }

    internal async ValueTask<byte[]> ReadRawValueAsync(CancellationToken cancellationToken)
    {
        int first = await ReadNonWhitespaceAsync(cancellationToken).ConfigureAwait(false);
        if (first < 0)
        {
            throw new ImportEnvelopeException("MALFORMED_IMPORT", "Import payload ended before a JSON value.");
        }

        ArrayBufferWriter<byte> output = new();
        Append(output, (byte)first);
        if (first is '{' or '[')
        {
            int depth = 1;
            bool inString = false;
            bool escaped = false;
            while (depth > 0)
            {
                int current = await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                if (current < 0)
                {
                    throw new ImportEnvelopeException("MALFORMED_IMPORT", "Import payload ended inside a JSON object or array.");
                }

                Append(output, (byte)current);
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                }
                else if (current is '{' or '[')
                {
                    depth++;
                }
                else if (current is '}' or ']')
                {
                    depth--;
                }

                EnsureBounded(output);
            }
        }
        else if (first == '"')
        {
            bool escaped = false;
            while (true)
            {
                int current = await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                if (current < 0)
                {
                    throw new ImportEnvelopeException("MALFORMED_IMPORT", "Import payload ended inside a JSON string.");
                }

                Append(output, (byte)current);
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    break;
                }

                EnsureBounded(output);
            }
        }
        else
        {
            while (true)
            {
                int current = await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                if (current < 0 || current is ',' or ']' or '}')
                {
                    PushBack(current);
                    break;
                }

                Append(output, (byte)current);
                EnsureBounded(output);
            }
        }

        return output.WrittenSpan.ToArray();
    }

    private static void Append(ArrayBufferWriter<byte> writer, byte value)
    {
        Span<byte> target = writer.GetSpan(1);
        target[0] = value;
        writer.Advance(1);
    }

    private static string Describe(int value) => value < 0 ? "end of input" : ((char)value).ToString();

    private static void EnsureBounded(ArrayBufferWriter<byte> output)
    {
        if (output.WrittenCount > MaxSingleValueBytes)
        {
            throw new ImportEnvelopeException(
                "IMPORT_RECORD_TOO_LARGE",
                $"A single import record exceeds the {MaxSingleValueBytes} byte bounded-processing limit.");
        }
    }

    private async ValueTask<int> ReadByteAsync(CancellationToken cancellationToken)
    {
        if (_pushedBack >= 0)
        {
            int value = _pushedBack;
            _pushedBack = -1;
            return value;
        }

        if (_bufferOffset >= _bufferLength)
        {
            _bufferLength = await _stream.ReadAsync(_buffer, cancellationToken).ConfigureAwait(false);
            _bufferOffset = 0;
            if (_bufferLength == 0)
            {
                return -1;
            }
        }

        return _buffer[_bufferOffset++];
    }

    private async ValueTask<int> ReadNonWhitespaceAsync(CancellationToken cancellationToken)
    {
        int value;
        do
        {
            value = await ReadByteAsync(cancellationToken).ConfigureAwait(false);
        }
        while (value is ' ' or '\t' or '\r' or '\n');

        return value;
    }

    private void PushBack(int value)
    {
        if (value < 0)
        {
            return;
        }

        if (_pushedBack >= 0)
        {
            throw new InvalidOperationException("Only one byte of JSON lookahead is supported.");
        }

        _pushedBack = value;
    }
}
