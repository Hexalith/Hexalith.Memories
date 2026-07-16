// <copyright file="QuickstartEnvelopeData.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Quickstart;

/// <summary>
/// Payload for the <c>memories quickstart</c> JSON envelope. Consumers read
/// <see cref="OverallStatus"/> for pass/fail and enumerate <see cref="Steps"/> for per-step detail
/// (ADR-7.4-003).
/// </summary>
/// <param name="Steps">The six wizard step results in execution order.</param>
/// <param name="OverallStatus">Either <c>"ok"</c> or <c>"fail"</c>. Kept as a string (not enum) so JSON consumers can switch on stable lowercase literals without naming-policy configuration.</param>
/// <param name="ElapsedMs">Total wall-clock elapsed across all steps, in milliseconds.</param>
public sealed record QuickstartEnvelopeData(
    IReadOnlyList<QuickstartStepResult> Steps,
    string OverallStatus,
    int ElapsedMs);
