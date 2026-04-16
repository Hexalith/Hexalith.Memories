// <copyright file="TableWriter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

/// <summary>Minimal ASCII table renderer — <see cref="string.PadRight(int)"/> only, no layout library (ADR-7.2-003).</summary>
internal static class TableWriter
{
    private const int ColumnSeparatorWidth = 2;

    /// <summary>Writes a header/separator/rows table to <paramref name="writer"/>.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="headers">Column headers.</param>
    /// <param name="rows">Row data; each row must have the same length as <paramref name="headers"/>.</param>
    public static void Write(TextWriter writer, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        // Sanitize every cell once up-front so column-width math and alignment see the same content the
        // writer will emit (embedded \n/\r/\t would otherwise split rows across terminal lines and defeat
        // PadRight).
        IReadOnlyList<string> sanitizedHeaders = SanitizeRow(headers);
        IReadOnlyList<IReadOnlyList<string>> materialized =
            rows.Select(SanitizeRow).ToArray();

        int[] widths = new int[sanitizedHeaders.Count];
        for (int col = 0; col < sanitizedHeaders.Count; col++)
        {
            widths[col] = sanitizedHeaders[col].Length;
        }

        foreach (IReadOnlyList<string> row in materialized)
        {
            for (int col = 0; col < sanitizedHeaders.Count && col < row.Count; col++)
            {
                string cell = row[col] ?? string.Empty;
                if (cell.Length > widths[col])
                {
                    widths[col] = cell.Length;
                }
            }
        }

        WriteRow(writer, sanitizedHeaders, widths);
        writer.WriteLine(new string('-', TotalWidth(widths)));
        foreach (IReadOnlyList<string> row in materialized)
        {
            WriteRow(writer, row, widths);
        }
    }

    /// <summary>
    /// Replaces CR / LF / TAB with a single space so that a cell never breaks row alignment. Applied
    /// centrally so every formatter benefits without re-implementing the rule.
    /// </summary>
    private static string SanitizeCell(string? cell)
    {
        if (string.IsNullOrEmpty(cell))
        {
            return string.Empty;
        }

        if (cell.AsSpan().IndexOfAny('\n', '\r', '\t') < 0)
        {
            return cell;
        }

        Span<char> buffer = cell.Length <= 512 ? stackalloc char[cell.Length] : new char[cell.Length];
        for (int i = 0; i < cell.Length; i++)
        {
            char c = cell[i];
            buffer[i] = c == '\n' || c == '\r' || c == '\t' ? ' ' : c;
        }

        return new string(buffer);
    }

    private static IReadOnlyList<string> SanitizeRow(IReadOnlyList<string> row)
    {
        string[] sanitized = new string[row.Count];
        for (int i = 0; i < row.Count; i++)
        {
            sanitized[i] = SanitizeCell(row[i]);
        }

        return sanitized;
    }

    private static int TotalWidth(int[] widths)
    {
        int total = 0;
        foreach (int w in widths)
        {
            total += w;
        }

        return total + (Math.Max(widths.Length - 1, 0) * ColumnSeparatorWidth);
    }

    private static void WriteRow(TextWriter writer, IReadOnlyList<string> cells, int[] widths)
    {
        for (int col = 0; col < widths.Length; col++)
        {
            string cell = col < cells.Count ? cells[col] ?? string.Empty : string.Empty;
            writer.Write(cell.PadRight(widths[col]));
            if (col < widths.Length - 1)
            {
                writer.Write("  ");
            }
        }

        writer.WriteLine();
    }
}
