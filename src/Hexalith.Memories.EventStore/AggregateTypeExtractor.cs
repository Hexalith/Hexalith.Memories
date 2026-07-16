// <copyright file="AggregateTypeExtractor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Extracts an aggregate type from a CloudEvents <c>type</c> string. Canonical rule (Story 9.1):
/// the second dotted segment wins (e.g. <c>"MyApp.Claims.ClaimSubmittedV2"</c> → <c>"Claims"</c>).
/// When the type has fewer than two dotted segments the full value is returned unchanged.</summary>
internal static class AggregateTypeExtractor
{
    /// <summary>Extracts the aggregate-type segment from a CloudEvents <paramref name="type"/> value.</summary>
    /// <param name="type">The CloudEvents <c>type</c> string.</param>
    /// <returns>The aggregate type — second dotted segment when present, otherwise <paramref name="type"/> unchanged.</returns>
    internal static string Extract(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        int firstDot = type.IndexOf('.');
        if (firstDot < 0)
        {
            return type;
        }

        int secondDot = type.IndexOf('.', firstDot + 1);
        int end = secondDot < 0 ? type.Length : secondDot;

        string segment = type[(firstDot + 1)..end];
        return string.IsNullOrWhiteSpace(segment) ? type : segment;
    }
}
