// <copyright file="FormValidationTrace.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

using Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// A traceability row binding a <see cref="FormValidationCode"/> to its classification, severity,
/// localization key, and the named contract fields that justify it.
/// </summary>
/// <remarks>
/// Story 17.3 (Task 0) — the form-validation half of the interaction traceability table. Every produced
/// message must resolve to a row here, keeping the contract-aware validation vocabulary tied to named
/// <c>Contracts.V1</c> fields rather than web-only invention.
/// </remarks>
/// <param name="Code">The validation code.</param>
/// <param name="Classification">Whether the code blocks dispatch, requires acknowledgement, or is advisory.</param>
/// <param name="Severity">The display severity tier.</param>
/// <param name="MessageKey">The localization key for the message text.</param>
/// <param name="ContractSources">The named contract fields that justify this code.</param>
public sealed record FormValidationTrace(
    FormValidationCode Code,
    FormMessageClassification Classification,
    InteractionSeverity Severity,
    string MessageKey,
    IReadOnlyList<string> ContractSources);
