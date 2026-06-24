// <copyright file="LensFieldTrace.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// A single row of the Story 17.4 lens field-trace table.
/// </summary>
/// <remarks>
/// Each lens must record a field trace before UI behavior is added: the displayed field or state, the
/// upstream contract/component source, the absent/redacted/degraded/unauthorized rendering, the test
/// level, and the evidence artifact. This record is the typed form of that obligation, consumed by
/// <see cref="LensFieldTraceability"/> and asserted by tests.
/// </remarks>
/// <param name="Lens">The lens that displays the field.</param>
/// <param name="DisplayedField">The displayed field or state name.</param>
/// <param name="UpstreamSource">
/// The upstream contract/component source, or <see cref="LensFieldTraceability.NoContractSource"/> when
/// the canonical contract does not yet expose a field for it.
/// </param>
/// <param name="AbsentBehavior">How absent, redacted, degraded, or unauthorized data renders.</param>
/// <param name="TestLevel">The test level that covers the field (unit or bUnit).</param>
/// <param name="EvidenceArtifact">The fixture or test that proves the behavior.</param>
public sealed record LensFieldTrace(
    LensKind Lens,
    string DisplayedField,
    string UpstreamSource,
    string AbsentBehavior,
    string TestLevel,
    string EvidenceArtifact);
