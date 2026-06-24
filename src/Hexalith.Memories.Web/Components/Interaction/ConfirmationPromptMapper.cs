// <copyright file="ConfirmationPromptMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Builds redaction-safe confirmation copy for safety-gated Story 17.3 commands.</summary>
public static class ConfirmationPromptMapper
{
    /// <summary>Projects a command and context into a confirmation prompt.</summary>
    /// <param name="command">The command being confirmed.</param>
    /// <param name="snapshot">The captured interaction context.</param>
    /// <returns>The prompt.</returns>
    public static ConfirmationPrompt Map(MemoriesCommandView command, InteractionContextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ConfirmationPrompt(
            InteractionResourceKeys.ConfirmationTitle,
            command.LabelKey,
            [
                $"{InteractionResourceKeys.ConfirmationTenantLabel}: {InteractionDisplay.SafeText(snapshot.TenantId, "unknown tenant")}",
                $"{InteractionResourceKeys.ConfirmationCaseLabel}: {InteractionDisplay.SafeText(snapshot.CaseId, "tenant-wide")}",
                $"{InteractionResourceKeys.ConfirmationObjectLabel}: {InteractionDisplay.SafeText(command.Target, "target")}",
                $"{InteractionResourceKeys.ConfirmationConsequenceLabel}: {command.ConsequenceKey}",
                $"{InteractionResourceKeys.ConfirmationRecoveryLabel}: {command.RecoveryExpectationKey}",
            ]);
    }
}
