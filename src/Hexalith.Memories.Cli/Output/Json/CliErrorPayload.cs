// <copyright file="CliErrorPayload.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Json;

/// <summary>
/// JSON-envelope error payload emitted by every <c>--format json</c> error (Story 7.3). Shape is
/// identical to <c>Hexalith.Memories.Contracts.V1.ErrorResponse</c> but the payload lives in the CLI to
/// avoid coupling the envelope to the contracts package's server-projection — the CLI-rendered
/// message/suggestion may differ from the raw server text when the catalog applies an override.
/// </summary>
/// <param name="Code">The server-reported or synthetic CLI error code.</param>
/// <param name="Message">The CLI-rendered (possibly translated) human message.</param>
/// <param name="Suggestion">The CLI-rendered (possibly catalog-overridden) recovery suggestion.</param>
public sealed record CliErrorPayload(string Code, string Message, string Suggestion);
