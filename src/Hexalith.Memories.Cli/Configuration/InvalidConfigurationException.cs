// <copyright file="InvalidConfigurationException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>Thrown when a configuration source reads a malformed config file.</summary>
public sealed class InvalidConfigurationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="InvalidConfigurationException"/> class.</summary>
    /// <param name="filePath">The path of the malformed config file.</param>
    /// <param name="message">The explanatory message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidConfigurationException(string filePath, string message, Exception? innerException = null)
        : base($"Invalid configuration at '{filePath}': {message}", innerException)
    {
        FilePath = filePath;
    }

    /// <summary>Gets the path of the malformed config file.</summary>
    public string FilePath { get; }
}
