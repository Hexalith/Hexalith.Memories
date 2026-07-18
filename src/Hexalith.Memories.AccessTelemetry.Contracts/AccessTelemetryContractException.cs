// <copyright file="AccessTelemetryContractException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Exception raised when internal lifecycle data violates the ratified bounded contract.</summary>
public sealed class AccessTelemetryContractException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="AccessTelemetryContractException"/> class.</summary>
    /// <param name="message">Bounded diagnostic message.</param>
    public AccessTelemetryContractException(string message)
        : base(message)
    {
    }
}
