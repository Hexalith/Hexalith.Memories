// <copyright file="MemoriesCommandView.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Rendered command/action row for command palette or command surface use.</summary>
/// <param name="Kind">The command kind.</param>
/// <param name="LabelKey">Localized label key.</param>
/// <param name="Target">Sanitized target object label or id.</param>
/// <param name="IsAvailable">Whether the command can currently execute.</param>
/// <param name="DisabledReasonKey">Localized disabled reason when unavailable.</param>
/// <param name="RequiresConfirmation">Whether activation must pass through confirmation.</param>
/// <param name="ConsequenceKey">Localized consequence key for confirmation.</param>
/// <param name="RecoveryExpectationKey">Localized recovery or undo expectation key for confirmation.</param>
public sealed record MemoriesCommandView(
    MemoriesCommandKind Kind,
    string LabelKey,
    string Target,
    bool IsAvailable,
    string? DisabledReasonKey,
    bool RequiresConfirmation,
    string ConsequenceKey,
    string RecoveryExpectationKey);
