// <copyright file="JsonEnvelopeWriter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Memories.Cli.Output.Json;

/// <summary>Helpers that wrap a payload in <see cref="CliOutputEnvelope{T}"/> and write it to a writer.</summary>
internal static class JsonEnvelopeWriter
{
    public static void Write<T>(TextWriter writer, string command, T data)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(data);

        var envelope = new CliOutputEnvelope<T>(CliOutputEnvelope<T>.CurrentSchemaVersion, command, data);

        // Route through JsonTypeInfo so the source-generated CliJsonSourceGenerationContext metadata is
        // used rather than the reflection fallback (Task 2.2 — AOT/trim-safe).
        JsonTypeInfo typeInfo = CliJsonContext.Options.GetTypeInfo(typeof(CliOutputEnvelope<T>));
        string json = JsonSerializer.Serialize(envelope, typeInfo);
        writer.WriteLine(json);
    }
}
