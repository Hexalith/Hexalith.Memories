// <copyright file="InteractionContextValidationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Result of revalidating a captured interaction context against the current packet and scope.</summary>
/// <param name="IsValid">Whether the interaction can execute.</param>
/// <param name="Reason">The reason it is unavailable, or <see cref="InteractionContextValidationReason.Valid"/>.</param>
/// <param name="DisabledReasonKey">Localized disabled-reason key, or null when valid.</param>
public sealed record InteractionContextValidationResult(
    bool IsValid,
    InteractionContextValidationReason Reason,
    string? DisabledReasonKey);
