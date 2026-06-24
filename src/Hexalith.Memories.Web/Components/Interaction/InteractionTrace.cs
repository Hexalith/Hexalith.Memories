// <copyright file="InteractionTrace.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Traceability row binding a Story 17.3 interaction family to its upstream sources and unavailable
/// fallback.
/// </summary>
/// <param name="Family">The interaction family.</param>
/// <param name="ContractSources">Canonical contract fields consumed by the family.</param>
/// <param name="FrontComposerSources">FrontComposer component or state sources reused by the family.</param>
/// <param name="AuthorizationSource">Authorization or scope source that gates the family.</param>
/// <param name="ResourceKeys">Localization resource keys or key families used by the family.</param>
/// <param name="UnavailableFallback">The fallback rendered when contract data is missing, stale, or unsafe.</param>
public sealed record InteractionTrace(
    InteractionFamily Family,
    IReadOnlyList<string> ContractSources,
    IReadOnlyList<string> FrontComposerSources,
    string AuthorizationSource,
    IReadOnlyList<string> ResourceKeys,
    string UnavailableFallback);
