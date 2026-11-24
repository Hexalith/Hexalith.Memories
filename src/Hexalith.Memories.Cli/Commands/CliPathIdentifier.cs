// <copyright file="CliPathIdentifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output.Formatters;

/// <summary>Validates identifiers supplied as individual REST path segments.</summary>
internal static class CliPathIdentifier
{
    /// <summary>Writes a structured input error if an identifier contains a slash.</summary>
    /// <param name="console">The command's output console.</param>
    /// <param name="commandName">The command used in the error envelope.</param>
    /// <param name="identifiers">Values used in REST path segments.</param>
    /// <returns>Whether an invalid identifier was reported.</returns>
    internal static bool RejectSlash(CliConsole console, string commandName, params string?[] identifiers)
    {
        if (!identifiers.Any(identifier => identifier?.Contains('/') == true))
        {
            return false;
        }

        CliErrorWriter.Write(
            console,
            commandName,
            code: "INVALID_INPUT",
            message: "Identifiers in the request path cannot contain '/'.",
            suggestion: "Use identifiers without '/' in the --tenant, --case, or --id options accepted by this command.");
        return true;
    }
}
