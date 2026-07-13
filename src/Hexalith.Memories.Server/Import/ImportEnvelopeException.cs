// <copyright file="ImportEnvelopeException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

/// <summary>Raised when an import/restore payload cannot be parsed or fails structural validation (Story 26.2).</summary>
internal sealed class ImportEnvelopeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ImportEnvelopeException"/> class.</summary>
    /// <param name="code">A stable, machine-readable error code (surfaced in the <c>ErrorResponse</c>).</param>
    /// <param name="message">A human-readable description of the failure.</param>
    public ImportEnvelopeException(string code, string message)
        : base(message) => Code = code;

    /// <summary>Initializes a new instance of the <see cref="ImportEnvelopeException"/> class.</summary>
    /// <param name="code">A stable, machine-readable error code.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public ImportEnvelopeException(string code, string message, Exception innerException)
        : base(message, innerException) => Code = code;

    /// <summary>Gets the stable, machine-readable error code.</summary>
    public string Code { get; }
}
