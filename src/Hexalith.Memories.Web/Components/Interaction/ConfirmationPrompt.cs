// <copyright file="ConfirmationPrompt.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Sanitized confirmation prompt projected for <c>FcDestructiveConfirmationDialog</c>.</summary>
/// <param name="TitleKey">Localized title key.</param>
/// <param name="ConfirmLabelKey">Localized confirm-label key.</param>
/// <param name="BodyLines">Sanitized body lines naming tenant, case, target, consequence, and recovery expectation.</param>
public sealed record ConfirmationPrompt(
    string TitleKey,
    string ConfirmLabelKey,
    IReadOnlyList<string> BodyLines);
