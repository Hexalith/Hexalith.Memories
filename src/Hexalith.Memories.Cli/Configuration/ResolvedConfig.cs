// <copyright file="ResolvedConfig.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

/// <summary>
/// The outcome of endpoint resolution: the resolved endpoint URI, optional API token, and the short name
/// of the <see cref="IConfigurationSource"/> class that supplied the values. <c>ResolvedBy</c> feeds the
/// <c>memories config show</c> diagnostic surface (AC #3c).
/// </summary>
/// <param name="Endpoint">The resolved endpoint URI.</param>
/// <param name="ApiToken">The resolved API token, if any.</param>
/// <param name="ResolvedBy">Short name of the source class that contributed these values.</param>
public sealed record ResolvedConfig(Uri Endpoint, string? ApiToken, string ResolvedBy);
