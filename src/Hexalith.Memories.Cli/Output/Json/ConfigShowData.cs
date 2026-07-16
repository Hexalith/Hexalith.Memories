// <copyright file="ConfigShowData.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Json;

/// <summary>
/// Projection of <see cref="Hexalith.Memories.Cli.Configuration.ResolvedConfig"/> that deliberately drops the
/// raw API token (replaced with a <see cref="TokenConfigured"/> boolean — ADR-7.1-003). Never reuse
/// <see cref="Hexalith.Memories.Cli.Configuration.ResolvedConfig"/> as a JSON payload.
/// </summary>
/// <param name="Endpoint">The resolved endpoint URI (already sanitized for display).</param>
/// <param name="ResolvedBy">The configuration source that produced the endpoint.</param>
/// <param name="TokenConfigured"><see langword="true"/> when a non-empty token was resolved.</param>
public sealed record ConfigShowData(string Endpoint, string ResolvedBy, bool TokenConfigured);
