// <copyright file="FormatterNotRegisteredException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output;

/// <summary>Thrown when no <see cref="IOutputFormatter{T}"/> is registered for the requested format.</summary>
public sealed class FormatterNotRegisteredException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="FormatterNotRegisteredException"/> class.</summary>
    /// <param name="modelType">The payload type.</param>
    /// <param name="format">The format that was requested.</param>
    public FormatterNotRegisteredException(Type modelType, OutputFormat format)
        : base($"No IOutputFormatter<{FormatTypeName(modelType)}> registered for format '{format}'.")
    {
        ArgumentNullException.ThrowIfNull(modelType);
        ModelType = modelType;
        Format = format;
    }

    /// <summary>Gets the payload type.</summary>
    public Type ModelType { get; }

    /// <summary>Gets the requested format.</summary>
    public OutputFormat Format { get; }

    private static string FormatTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string baseName = type.Name;
        int backtick = baseName.IndexOf('`', StringComparison.Ordinal);
        if (backtick > 0)
        {
            baseName = baseName[..backtick];
        }

        string arguments = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
        return $"{baseName}<{arguments}>";
    }
}
