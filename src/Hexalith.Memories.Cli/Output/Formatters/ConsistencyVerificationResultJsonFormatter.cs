// <copyright file="ConsistencyVerificationResultJsonFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Contracts.V1;

/// <summary>JSON envelope for <see cref="ConsistencyVerificationResult"/>.</summary>
public sealed class ConsistencyVerificationResultJsonFormatter : IOutputFormatter<ConsistencyVerificationResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public void Write(ConsistencyVerificationResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        JsonEnvelopeWriter.Write(writer, ConsistencyVerifyCommand.CommandName, value);
    }
}