// <copyright file="IOutputFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output;

/// <summary>Renders a typed payload to a target writer in a specific <see cref="OutputFormat"/>.</summary>
/// <typeparam name="T">The payload type.</typeparam>
public interface IOutputFormatter<in T>
{
    /// <summary>Gets the format this implementation serves.</summary>
    OutputFormat Format { get; }

    /// <summary>Writes <paramref name="value"/> to <paramref name="writer"/>.</summary>
    /// <param name="value">The payload.</param>
    /// <param name="writer">The destination writer.</param>
    void Write(T value, TextWriter writer);
}
