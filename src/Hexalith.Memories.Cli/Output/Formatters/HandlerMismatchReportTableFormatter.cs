// <copyright file="HandlerMismatchReportTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Memories.Contracts.V1;

/// <summary>Tabular rendering of <see cref="HandlerMismatchReport"/>.</summary>
public sealed class HandlerMismatchReportTableFormatter : IOutputFormatter<HandlerMismatchReport>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(HandlerMismatchReport value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        IEnumerable<IReadOnlyList<string>> rows = value.Mismatches.Select(m => (IReadOnlyList<string>)
        [
            m.Severity.ToString().ToLowerInvariant(),
            ToCamelCase(m.Category.ToString()),
            m.Subject,
            m.Suggestion,
        ]);

        TableWriter.Write(writer, ["SEVERITY", "CATEGORY", "SUBJECT", "SUGGESTION"], rows);
    }

    private static string ToCamelCase(string enumValue)
    {
        if (string.IsNullOrEmpty(enumValue))
        {
            return enumValue;
        }

        return char.ToLowerInvariant(enumValue[0]) + enumValue[1..];
    }
}