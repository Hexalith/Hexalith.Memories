// <copyright file="HandlerMismatchReportHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System;

using Hexalith.Memories.Contracts.V1;

/// <summary>Human-readable rendering of <see cref="HandlerMismatchReport"/>.</summary>
public sealed class HandlerMismatchReportHumanFormatter : IOutputFormatter<HandlerMismatchReport>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(HandlerMismatchReport value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (HandlerMismatch mismatch in value.Mismatches)
        {
            writer.WriteLine(
                $"[{mismatch.Severity.ToString().ToLowerInvariant()}] {ToCamelCase(mismatch.Category.ToString())}: {mismatch.Subject} — {mismatch.Suggestion}");
        }
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