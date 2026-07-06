// <copyright file="Epic17SpecimenRoute.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Specimens;

/// <summary>
/// Describes one stable Story 17 browser specimen route.
/// </summary>
/// <param name="Surface">The human-readable Epic 17 surface name.</param>
/// <param name="Slug">The route slug under the specimen prefix.</param>
/// <param name="ComponentName">The RCL component rendered by the specimen.</param>
/// <param name="FixtureFamily">The shared fixture family used by the specimen.</param>
/// <param name="SelectorAnchor">The required test selector anchor for the rendered surface.</param>
/// <param name="EvidenceArtifactPath">The bounded relative artifact path used by browser evidence.</param>
public sealed record Epic17SpecimenRoute(
    string Surface,
    string Slug,
    string ComponentName,
    string FixtureFamily,
    string SelectorAnchor,
    string EvidenceArtifactPath)
{
    /// <summary>Gets the specimen route relative to the host root.</summary>
    public string Route => $"{Epic17SpecimenManifest.RoutePrefix}/{Slug}";
}
