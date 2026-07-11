// <copyright file="JsonEnvelopeFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

/// <summary>Emits a typed payload inside the canonical CLI JSON envelope.</summary>
/// <typeparam name="T">The reference-type payload to render.</typeparam>
public sealed class JsonEnvelopeFormatter<T> : IOutputFormatter<T>
    where T : class
{
    private readonly Func<T, string> _commandSelector;

    /// <summary>Initializes a new instance of the <see cref="JsonEnvelopeFormatter{T}"/> class.</summary>
    /// <param name="command">The fixed command name written to every envelope.</param>
    public JsonEnvelopeFormatter(string command)
        : this(CreateFixedCommandSelector(command))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="JsonEnvelopeFormatter{T}"/> class.</summary>
    /// <param name="commandSelector">Selects the envelope command name from the payload.</param>
    public JsonEnvelopeFormatter(Func<T, string> commandSelector)
    {
        ArgumentNullException.ThrowIfNull(commandSelector);
        _commandSelector = commandSelector;
    }

    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(T value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        JsonEnvelopeWriter.Write(writer, _commandSelector(value), value);
    }

    private static Func<T, string> CreateFixedCommandSelector(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        return _ => command;
    }
}
